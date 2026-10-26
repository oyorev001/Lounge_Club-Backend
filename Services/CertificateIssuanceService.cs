using System.Text.Json;
using Microsoft.Extensions.Options;
using LoungeClubsApi.Configuration;
using LoungeClubsApi.Data;
using LoungeClubsApi.Entities;
using LoungeClubsApi.Entities.Enums;
using LoungeClubsApi.Extensions;
using LoungeClubsApi.Helpers;
using LoungeClubsApi.Interfaces;

namespace LoungeClubsApi.Services;

/// <summary>
/// Issues a compliance certificate once a levy payment succeeds.
/// Also marks fines paid and flips establishment compliance status.
/// </summary>
public class CertificateIssuanceService(
    AppDbContext db,
    NotificationService notificationService,
    DocumentService documentService,
    IOptions<AppSettings> appSettings,
    ILogger<CertificateIssuanceService> logger)
{
    /// <summary>
    /// Called when a levy transaction succeeds. Marks the assessment paid,
    /// issues the certificate, and sets the establishment compliant.
    /// </summary>
    public async Task<Certificate?> IssueForPaidLevyAsync(Transaction transaction, CancellationToken ct = default)
    {
        if (transaction.Context != TransactionContext.Levy || transaction.ContextId is null)
            return null;

        var assessment = await db.LevyAssessments
            .Include(a => a.Establishment)
            .ThenInclude(e => e.Owner)
            .Include(a => a.Tier)
            .FirstOrDefaultAsync(a => a.Id == transaction.ContextId.Value, ct);

        if (assessment is null)
        {
            logger.LogWarning("Paid transaction {Ref} references missing levy assessment {Id}", transaction.Reference, transaction.ContextId);
            return null;
        }

        if (assessment.Status == LevyStatus.Paid)
        {
            // This money arrived for a debt another txn already covered —
            // legit business-wise (owner double-paid) but it's a refund
            // candidate. Flag it for the reconciliation view.
            transaction.Narration = (transaction.Narration is { Length: > 0 } n ? n + " | " : "")
                + "Duplicate settlement — refund candidate";
            await db.SaveChangesAsync(ct);
            return await db.Certificates.FirstOrDefaultAsync(c => c.LevyAssessmentId == assessment.Id, ct);
        }

        // A waived assessment must never be flipped back to Paid by a late
        // webhook on a checkout opened before the waiver.
        if (assessment.Status is not (LevyStatus.Pending or LevyStatus.Overdue))
            return null;

        assessment.Status = LevyStatus.Paid;
        assessment.PaidAt = transaction.PaidAt ?? DateTimeOffset.UtcNow;

        var est = assessment.Establishment;

        // Outstanding levies stay payable even on suspended/deregistered venues —
        // but only a venue that can legally operate gets a certificate.
        if (est.Status is not (EstablishmentStatus.Approved or EstablishmentStatus.NonCompliant))
        {
            await db.SaveChangesAsync(ct); // record the levy payment, skip the cert
            logger.LogWarning(
                "Levy {Ref} settled for {Status} establishment {Name} — payment recorded, certificate withheld",
                transaction.Reference, est.Status, est.Name);
            await notificationService.NotifyAsync(
                est.OwnerId,
                NotificationType.PaymentConfirmed,
                "Levy payment recorded",
                $"Payment of NGN {transaction.Amount:N2} for levy {assessment.Reference} was recorded. No certificate was issued because the venue is {est.Status}.",
                "/dashboard/levies",
                null,
                ct,
                est.Owner?.PhoneNumber);
            return null;
        }

        var countForYear = await db.Certificates.CountAsync(c => c.LevyYear == assessment.Year, ct) + 1;

        // Renewal: paying before the previous cert expires must extend from the
        // old expiry, not from today — otherwise an early renewal silently
        // shortens validity (and leaves two Active certs).
        var prevExpiry = await db.Certificates
            .Where(c => c.EstablishmentId == est.Id
                && c.Status == CertificateStatus.Active
                && c.Id != Guid.Empty)
            .Select(c => (DateOnly?)c.ExpiryDate)
            .MaxAsync(ct);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var expiryBase = prevExpiry is { } pe && pe > today ? pe : today;

        var certificate = new Certificate
        {
            CertificateNumber = ReferenceGenerator.Yearly("OYS-LC", assessment.Year, countForYear),
            QrToken = QrCodeHelper.GenerateQrToken(),
            EstablishmentId = assessment.EstablishmentId,
            LevyAssessmentId = assessment.Id,
            LevyYear = assessment.Year,
            AmountPaid = transaction.Amount,
            IssueDate = today,
            ExpiryDate = expiryBase.AddYears(1),
            Status = CertificateStatus.Active
        };

        db.Certificates.Add(certificate);

        // Paying restores compliance ONLY for levy-related flags — a venue
        // flagged by a critical inspection or cert revocation needs an officer
        // to clear it via /compliance-clear, not buy its way back.
        if (est.Status == EstablishmentStatus.NonCompliant
            && est.NonCompliantReason is null or "UnpaidLevy")
        {
            est.Status = EstablishmentStatus.Approved;
            est.NonCompliantReason = null;
        }

        // Queue the settlement email + in-app notification BEFORE the cert is
        // saved — one transaction. A save failure rolls back together, and the
        // unique-violation retry re-attempts all of them.
        QueueSettlementEmail(transaction, assessment, certificate, est, ct);
        await QueueCertNotification(transaction, assessment, certificate, est, ct);

        // Certificate numbers are sequential, so concurrent settlements can
        // collide; also, webhook + status-poll can race on the same levy
        // (unique index on LevyAssessmentId). Retry the number, and if the
        // race was on the levy itself just return the already-issued cert.
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                await db.SaveChangesAsync(ct);
                break;
            }
            catch (DbUpdateException ex) when (attempt < 4 && ex.IsUniqueViolation())
            {
                var existing = await db.Certificates
                    .FirstOrDefaultAsync(c => c.LevyAssessmentId == assessment.Id, ct);
                if (existing is not null)
                {
                    // The cert exists — drop the phantom cert AND the queued
                    // notifications referencing the number that never landed.
                    db.Entry(certificate).State = EntityState.Detached;
                    DetachQueuedSettlementEntities();
                    return existing;
                }

                var next = await db.Certificates.CountAsync(c => c.LevyYear == assessment.Year, ct) + 1;
                certificate.CertificateNumber = ReferenceGenerator.Yearly("OYS-LC", assessment.Year, next);

                // The queued email/notification embed the number — rebuild them
                // so they match the cert that actually gets stored.
                DetachQueuedSettlementEntities();
                QueueSettlementEmail(transaction, assessment, certificate, est, ct);
                await QueueCertNotification(transaction, assessment, certificate, est, ct);
            }
        }

        return certificate;
    }

    /// <summary>
    /// Queues the payment-settled email: branded body with the certificate
    /// number front and centre, an inline verification QR, and the official
    /// certificate + payment receipt PDFs attached.
    /// </summary>
    private void QueueSettlementEmail(
        Transaction transaction,
        LevyAssessment assessment,
        Certificate certificate,
        Establishment est,
        CancellationToken ct)
    {
        var ownerEmail = est.Owner?.Email;
        if (string.IsNullOrWhiteSpace(ownerEmail))
            return;

        var portal = appSettings.Value.PortalUrl?.TrimEnd('/');
        var verifyUrl = string.IsNullOrWhiteSpace(portal)
            ? $"/verify/{certificate.QrToken}"
            : $"{portal}/verify/{certificate.QrToken}";

        var receiptPdf = documentService.GenerateReceiptPdf(
            transaction, est, $"Annual levy — {assessment.Year}", assessment.Reference);
        var certPdf = documentService.GenerateCertificatePdf(
            certificate, est, assessment.Tier?.Name ?? "Licensed", appSettings.Value.PortalUrl);
        var qrPng = QrCodeHelper.GenerateQrCodePng(verifyUrl, 10);

        var attachments = new[]
        {
            new EmailAttachment($"{certificate.CertificateNumber}.pdf", "application/pdf", Convert.ToBase64String(certPdf)),
            new EmailAttachment($"receipt-{transaction.Reference}.pdf", "application/pdf", Convert.ToBase64String(receiptPdf)),
            new EmailAttachment("verify-qr.png", "image/png", Convert.ToBase64String(qrPng), "qr"),
        };

        var htmlBody = $"""
        <div style="font-family:Arial,sans-serif;max-width:600px;margin:auto">
          <div style="background:#700002;padding:24px;text-align:center">
            {(string.IsNullOrWhiteSpace(portal) ? "" : $"""<img src="{portal}/images/oyo-crest.png" alt="Oyo State crest" width="56" style="display:block;margin:0 auto 10px" />""")}
            <h1 style="color:#fff;margin:0;font-size:20px">Oyo State Lounge &amp; Clubs Portal</h1>
            <p style="color:#f5d0c5;margin:6px 0 0;font-size:12px">Ministry of Culture and Tourism — Compliance Directorate</p>
          </div>
          <div style="padding:28px;border:1px solid #ecdcd2">
            <h2 style="margin:0 0 6px;font-size:18px;color:#1a1a1a">Payment confirmed — certificate issued</h2>
            <p style="margin:0 0 20px;font-size:14px;color:#555;line-height:1.7">
              Your payment was received and your compliance certificate is attached to this email.
            </p>
            <div style="background:#faf6f0;border:1px solid #ecdcd2;border-radius:12px;padding:18px;margin-bottom:20px">
              <div style="font-size:11px;color:#999;text-transform:uppercase;letter-spacing:2px;margin-bottom:4px">Certificate number</div>
              <div style="font-size:22px;font-weight:800;color:#700002;letter-spacing:1px">{certificate.CertificateNumber}</div>
            </div>
            <table width="100%" cellpadding="0" cellspacing="0" style="font-size:13px">
              {Row("Establishment", est.Name)}
              {Row("Registration", est.RegistrationNumber)}
              {Row("Levy year", assessment.Year.ToString())}
              {Row("Reference", transaction.Reference)}
              {Row("Amount paid", $"&#8358;{transaction.Amount:N2}")}
              {Row("Paid at", $"{(transaction.PaidAt ?? DateTimeOffset.UtcNow):dd MMM yyyy, HH:mm}")}
              {Row("Valid until", $"{certificate.ExpiryDate:dd MMM yyyy}")}
            </table>
            <div style="text-align:center;margin:24px 0">
              <img src="cid:qr" alt="Certificate verification QR" width="150" style="display:inline-block;border:1px solid #ecdcd2;border-radius:8px;padding:8px" />
              <p style="font-size:11px;color:#999;margin:8px 0 0">Scan to verify this certificate — or visit {verifyUrl}</p>
            </div>
            <p style="margin:0;font-size:12px;color:#999;text-align:center">
              Attached: your official certificate and payment receipt (PDF). Display the certificate at your premises.
            </p>
          </div>
          <div style="background:#faf6f0;border:1px solid #ecdcd2;border-top:0;padding:14px;text-align:center;font-size:11px;color:#999">
            This is an automated message from the Oyo State Government compliance portal — please do not reply.
          </div>
        </div>
        """;

        db.EmailOutbox.Add(new EmailOutbox
        {
            To = ownerEmail,
            Subject = $"Payment confirmed — certificate {certificate.CertificateNumber} issued",
            HtmlBody = htmlBody,
            Attachments = JsonSerializer.Serialize(attachments)
        });

        static string Row(string label, string value) =>
            $"""<tr><td style="padding:9px 0;color:#888;border-bottom:1px solid #f3ece7">{label}</td><td style="padding:9px 0;text-align:right;font-weight:600;color:#1a1a1a;border-bottom:1px solid #f3ece7">{value}</td></tr>""";
    }

    /// <summary>Detaches any queued outbox/notification entities added this call —
    /// used when the certificate is re-numbered or already exists.</summary>
    private void DetachQueuedSettlementEntities()
    {
        foreach (var entry in db.ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added
                && (e.Entity is EmailOutbox or SmsOutbox or Notification))
            .ToList())
        {
            entry.State = EntityState.Detached;
        }
    }

    /// <summary>Stages the certificate-issued in-app/SMS notification without
    /// saving — the caller's SaveChanges commits it with the certificate.</summary>
    private async Task QueueCertNotification(
        Transaction transaction,
        LevyAssessment assessment,
        Certificate certificate,
        Establishment est,
        CancellationToken ct)
    {
        var title = "Compliance certificate issued";
        var message = $"Payment of NGN {transaction.Amount:N2} confirmed. Certificate {certificate.CertificateNumber} has been issued to {est.Name}, valid until {certificate.ExpiryDate:dd MMM yyyy}.";
        db.Notifications.Add(new Notification
        {
            UserId = est.OwnerId,
            Type = NotificationType.CertificateIssued,
            Title = title,
            Message = message,
            Link = "/dashboard/certificates",
        });

        // Mirror NotifyAsync's SMS path so cert-issued alerts still hit the phone.
        var smsTo = est.Owner?.PhoneNumber;
        if (!string.IsNullOrWhiteSpace(smsTo))
        {
            var msisdn = TermiiSmsService.NormalizeMsisdn(smsTo);
            if (msisdn.Length >= 10)
            {
                var body = $"{title}: {message}";
                if (body.Length > 459) body = body[..456] + "...";
                db.SmsOutbox.Add(new SmsOutbox { To = msisdn, Message = body });
            }
        }
        await Task.CompletedTask;
    }

    /// <summary>Called when a fine transaction succeeds.</summary>
    public async Task MarkFinePaidAsync(Transaction transaction, CancellationToken ct = default)
    {
        if (transaction.Context != TransactionContext.Fine || transaction.ContextId is null)
            return;

        var fine = await db.Fines
            .Include(f => f.Establishment)
            .ThenInclude(e => e.Owner)
            .FirstOrDefaultAsync(f => f.Id == transaction.ContextId.Value, ct);

        // Only settle unpaid fines — a waived fine must not be re-marked Paid
        // by a checkout that was already in flight when it was waived.
        if (fine is null)
            return;
        if (fine.Status == FineStatus.Paid)
        {
            transaction.Narration = (transaction.Narration is { Length: > 0 } n ? n + " | " : "")
                + "Duplicate settlement — refund candidate";
            await db.SaveChangesAsync(ct);
            return;
        }
        if (fine.Status != FineStatus.Unpaid)
            return;

        fine.Status = FineStatus.Paid;
        fine.PaidAt = transaction.PaidAt ?? DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        await notificationService.NotifyAsync(
            fine.Establishment.OwnerId,
            NotificationType.PaymentConfirmed,
            "Fine payment confirmed",
            $"Payment of NGN {transaction.Amount:N2} received for fine {fine.Reference} ({fine.FineType}).",
            "/dashboard/fines",
            null,
            ct,
            fine.Establishment.Owner?.PhoneNumber);

        var ownerEmail = fine.Establishment.Owner?.Email;
        if (!string.IsNullOrWhiteSpace(ownerEmail))
        {
            var receiptPdf = documentService.GenerateReceiptPdf(
                transaction, fine.Establishment, $"Fine — {fine.FineType}", fine.Reference);
            var attachments = new[]
            {
                new EmailAttachment($"receipt-{transaction.Reference}.pdf", "application/pdf", Convert.ToBase64String(receiptPdf)),
            };
            db.EmailOutbox.Add(new EmailOutbox
            {
                To = ownerEmail,
                Subject = $"Fine payment confirmed — {fine.Reference}",
                HtmlBody = $"""
                <div style="font-family:Arial,sans-serif;max-width:600px;margin:auto;border:1px solid #ecdcd2;padding:28px">
                  <h2 style="margin:0 0 8px;font-size:18px;color:#1a1a1a">Fine payment confirmed</h2>
                  <p style="margin:0;font-size:14px;color:#555;line-height:1.7">
                    Payment of &#8358;{transaction.Amount:N2} received for fine {fine.Reference} ({fine.FineType}) at {fine.Establishment.Name}. Your official receipt is attached.
                  </p>
                </div>
                """,
                Attachments = JsonSerializer.Serialize(attachments)
            });
            await db.SaveChangesAsync(ct);
        }
    }
}
