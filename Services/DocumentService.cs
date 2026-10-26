using LoungeClubsApi.Entities;
using LoungeClubsApi.Helpers;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LoungeClubsApi.Services;

/// <summary>
/// Generates compliance certificates and payment receipts as PDFs (QuestPDF).
/// </summary>
public class DocumentService(IWebHostEnvironment env)
{
    private const string Maroon = "#700002";
    private const string MaroonDark = "#450a0a";
    private const string Gold = "#c8a27a";

    /// <summary>
    /// A4 landscape compliance certificate — framed border, Oyo State crest,
    /// faded watermark, venue name plate, gold tier seal, verification QR,
    /// and dual signature lines. Reads like a real government certificate.
    /// </summary>
    public byte[] GenerateCertificatePdf(Certificate cert, Establishment establishment, string tierName, string verifyBaseUrl)
    {
        var verifyUrl = $"{verifyBaseUrl.TrimEnd('/')}/verify/{cert.QrToken}";
        var qrPng = QrCodeHelper.GenerateQrCodePng(verifyUrl, 8);
        var crestPath = Path.Combine(env.WebRootPath, "images", "oyo-crest.png");
        var crest = File.Exists(crestPath) ? File.ReadAllBytes(crestPath) : null;
        var wmPath = Path.Combine(env.WebRootPath, "images", "oyo-crest-wm.png");
        var watermark = File.Exists(wmPath) ? File.ReadAllBytes(wmPath) : null;

        var typeLabel = establishment.AdditionalTypes.Count > 0
            ? $"{establishment.Type} + {string.Join(" + ", establishment.AdditionalTypes)}"
            : establishment.Type.ToString();

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(20);
                page.DefaultTextStyle(x => x.FontFamily("Helvetica"));

                // Faded crest watermark behind everything.
                if (watermark is not null)
                    page.Background().AlignCenter().AlignMiddle().Width(320).Image(watermark);

                page.Content()
                    .Border(4).BorderColor(Maroon)
                    .Padding(4).Border(1).BorderColor(Gold)
                    .Padding(3).Border(2).BorderColor(Maroon)
                    .Padding(26)
                    .Column(col =>
                    {
                        // ── Header: crest + state ministry ──────────────────
                        if (crest is not null)
                            col.Item().AlignCenter().Height(62).Image(crest);

                        col.Item().PaddingTop(6).AlignCenter()
                            .Text("OYO STATE GOVERNMENT")
                            .FontSize(16).FontColor(Maroon).Bold().LetterSpacing(0.28f);

                        col.Item().AlignCenter()
                            .Text("MINISTRY OF CULTURE AND TOURISM")
                            .FontSize(9.5f).FontColor("#6b5b57").LetterSpacing(0.14f);

                        col.Item().AlignCenter()
                            .Text("OYO STATE TOURISM BOARD · COMPLIANCE DIRECTORATE")
                            .FontSize(8).FontColor(Gold).LetterSpacing(0.18f);

                        // Title
                        col.Item().PaddingTop(10).AlignCenter()
                            .Text("CERTIFICATE OF COMPLIANCE")
                            .FontSize(26).FontColor(MaroonDark).Bold();

                        // Gold rule with centre diamond accent
                        col.Item().PaddingTop(4).AlignCenter().Width(220).Row(r =>
                        {
                            r.RelativeItem().AlignMiddle().BorderTop(1).BorderColor(Gold).Text("");
                            r.ConstantItem(10).AlignCenter().Text("◆").FontSize(7).FontColor(Gold);
                            r.RelativeItem().AlignMiddle().BorderTop(1).BorderColor(Gold).Text("");
                        });

                        // ── Certification body ──────────────────────────────
                        col.Item().PaddingTop(14).AlignCenter()
                            .Text("This is to certify that")
                            .FontSize(12).Italic().FontColor("#555");

                        col.Item().AlignCenter().PaddingTop(4)
                            .Text(establishment.Name.ToUpperInvariant())
                            .FontSize(25).Bold().FontColor(MaroonDark);

                        col.Item().AlignCenter().PaddingBottom(8).Width(320)
                            .BorderBottom(1).BorderColor(Gold).Text("").FontSize(1);

                        col.Item().AlignCenter().PaddingTop(6)
                            .Text($"of {establishment.Address}, {establishment.Lga} LGA, Oyo State")
                            .FontSize(11.5f).FontColor("#333");

                        col.Item().AlignCenter().PaddingTop(8).Text(
                                $"having met the requirements of the Oyo State Lounge & Clubs Compliance, Standardization\n" +
                                $"and Regulation programme and paid the prescribed annual levy, is hereby licensed as a\n" +
                                $"{tierName} establishment, operating as a {typeLabel}, for the levy year {cert.LevyYear}.")
                            .FontSize(11).LineHeight(1.6f).FontColor("#333");

                        // ── Footer: dates · gold seal + QR · signatures ─────
                        col.Item().Height(58); // fills the frame so the footer band sits at the bottom
                        col.Item().Row(row =>
                        {
                            // Left — issue data block
                            row.RelativeItem().AlignLeft().Column(c =>
                            {
                                c.Item().Text("CERTIFICATE NO.")
                                    .FontSize(7.5f).FontColor("#999").LetterSpacing(0.1f);
                                c.Item().Text(cert.CertificateNumber)
                                    .FontSize(11).Bold().FontColor(MaroonDark);
                                c.Item().PaddingTop(5).Text(
                                        $"Issue Date: {cert.IssueDate:dd MMMM yyyy}")
                                    .FontSize(9.5f).FontColor("#555");
                                c.Item().Text($"Valid Until: {cert.ExpiryDate:dd MMMM yyyy}")
                                    .FontSize(9.5f).Bold().FontColor(Maroon);
                            });

                            // Centre — QR with the gold seal overlapping its corner,
                            // like a stamp on the verification block.
                            row.ConstantItem(150).AlignCenter().Column(c =>
                            {
                                c.Item().Row(inner =>
                                {
                                    if (crest is not null)
                                        inner.ConstantItem(48).AlignMiddle()
                                            .Height(48)
                                            .Border(2).BorderColor(Gold).CornerRadius(24)
                                            .Background("#fdf8f3").Padding(6)
                                            .Image(crest);
                                    inner.RelativeItem().PaddingLeft(10).AlignMiddle()
                                        .AlignCenter().Width(84).Image(qrPng);
                                });
                                c.Item().PaddingTop(5).AlignCenter()
                                    .Text("Scan QR to verify authenticity")
                                    .FontSize(6.5f).FontColor("#777");
                            });

                            // Right — signature lines
                            row.RelativeItem().AlignRight().Column(c =>
                            {
                                c.Item().Width(170).LineHorizontal(0.8f).LineColor("#666");
                                c.Item().PaddingTop(2).Text("Compliance Officer")
                                    .FontSize(9).Bold();
                                c.Item().PaddingTop(12)
                                    .Width(170).LineHorizontal(0.8f).LineColor("#666");
                                c.Item().PaddingTop(2).Text("Director-General")
                                    .FontSize(9).Bold();
                                c.Item().Text("Compliance Directorate").FontSize(7.5f).FontColor("#999");
                            });
                        });

                        // Legal footer strip — fills the lower band and states validity terms.
                        var verifyLine = string.IsNullOrWhiteSpace(verifyBaseUrl)
                            ? $"Verify via the portal — Certificate No. {cert.CertificateNumber}"
                            : $"Verify this certificate at {verifyBaseUrl.TrimEnd('/')}/verify — Certificate No. {cert.CertificateNumber}";
                        col.Item().PaddingTop(18).AlignCenter().Text(verifyLine)
                            .FontSize(7.5f).FontColor("#8a7d78");
                        col.Item().PaddingTop(3).AlignCenter()
                            .Text("This certificate is the property of the Oyo State Government and is valid only while unrevoked and unexpired.")
                            .FontSize(7).FontColor("#a99").Italic();
                    });
            });
        }).GeneratePdf();
    }

    /// <summary>
    /// A4 portrait payment receipt — framed page, crest letterhead, line-item
    /// table, amount in words, maroon total band and an officer signature block.
    /// </summary>
    public byte[] GenerateReceiptPdf(Transaction tx, Establishment establishment, string contextLabel, string contextDetail)
    {
        var crestPath = Path.Combine(env.WebRootPath, "images", "oyo-crest.png");
        var crest = File.Exists(crestPath) ? File.ReadAllBytes(crestPath) : null;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(28);
                page.DefaultTextStyle(x => x.FontFamily("Helvetica"));

                page.Content()
                    .Border(2).BorderColor(Maroon).Padding(2)
                    .Border(0.5f).BorderColor(Gold)
                    .Padding(28)
                    .Column(col =>
                    {
                        // ── Letterhead ──────────────────────────────────────
                        if (crest is not null)
                            col.Item().AlignCenter().Height(56).Image(crest);
                        col.Item().PaddingTop(6).AlignCenter().Text("OYO STATE GOVERNMENT")
                            .FontSize(14).FontColor(Maroon).Bold().LetterSpacing(0.22f);
                        col.Item().AlignCenter().Text("MINISTRY OF CULTURE AND TOURISM")
                            .FontSize(9).FontColor("#6b5b57").LetterSpacing(0.14f);
                        col.Item().AlignCenter().Text("OYO STATE TOURISM BOARD · COMPLIANCE DIRECTORATE")
                            .FontSize(7.5f).FontColor(Gold).LetterSpacing(0.16f);

                        col.Item().PaddingTop(12).AlignCenter().Text("OFFICIAL PAYMENT RECEIPT")
                            .FontSize(21).FontColor(MaroonDark).Bold();
                        col.Item().AlignCenter().PaddingTop(3)
                            .Text($"Receipt for {contextLabel}").FontSize(10.5f).FontColor("#6b5b57");

                        // Gold rule
                        col.Item().PaddingTop(8).AlignCenter().Width(180).Row(r =>
                        {
                            r.RelativeItem().AlignMiddle().BorderTop(1).BorderColor(Gold).Text("");
                            r.ConstantItem(10).AlignCenter().Text("◆").FontSize(7).FontColor(Gold);
                            r.RelativeItem().AlignMiddle().BorderTop(1).BorderColor(Gold).Text("");
                        });

                        // ── Line items ──────────────────────────────────────
                        col.Item().PaddingTop(20).Border(1).BorderColor("#e8ddd6").Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.RelativeColumn(2);
                                cols.RelativeColumn(3);
                            });

                            void Row(string label, string value)
                            {
                                table.Cell().BorderBottom(0.5f).BorderColor("#f0e4da")
                                    .PaddingVertical(11).PaddingHorizontal(14)
                                    .Text(label).FontSize(10).FontColor("#6b5b57");
                                table.Cell().BorderBottom(0.5f).BorderColor("#f0e4da")
                                    .PaddingVertical(11).PaddingHorizontal(14)
                                    .AlignRight().Text(value).FontSize(11).SemiBold();
                            }

                            Row("Payment Reference", tx.Reference);
                            Row("Establishment", establishment.Name);
                            Row("Registration No.", establishment.RegistrationNumber);
                            Row("Address", $"{establishment.Address}, {establishment.Lga} LGA");
                            Row("Payment For", contextDetail);
                            Row("Amount Paid", $"NGN {tx.Amount:N2}");
                            Row("Channel", tx.Channel.ToString());
                            Row("Processor Reference", tx.GatewayReference ?? tx.WebhookId ?? "-");
                            Row("Paid At", tx.PaidAt?.ToString("dd MMMM yyyy, HH:mm") ?? "-");
                            Row("Payer Bank", tx.SenderBank ?? "-");
                            Row("Payer Name", tx.SenderName ?? "-");
                        });

                        // Amount in words — standard on Nigerian official receipts.
                        col.Item().PaddingTop(10).PaddingHorizontal(6).Row(r =>
                        {
                            r.RelativeItem().Text("Amount in words:")
                                .FontSize(9.5f).FontColor("#6b5b57").Italic();
                            r.RelativeItem(3).AlignRight().Text(NairaToWords(tx.Amount))
                                .FontSize(9.5f).Italic().FontColor("#444");
                        });

                        // Maroon total band
                        col.Item().PaddingTop(14).Background(Maroon).Padding(16).Row(r =>
                        {
                            r.RelativeItem().AlignMiddle().Text("TOTAL PAID")
                                .FontSize(11).FontColor("#f5d0c5").LetterSpacing(0.15f);
                            r.RelativeItem().AlignMiddle().AlignRight().Text($"NGN {tx.Amount:N2}")
                                .FontSize(17).Bold().FontColor("#ffffff");
                        });

                        // ── Verification + signature block pushed low ───────
                        col.Item().PaddingTop(34).Row(r =>
                        {
                            r.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Electronic receipt")
                                    .FontSize(8).FontColor("#999").LetterSpacing(0.1f);
                                c.Item().PaddingTop(2).Text(
                                        "Generated electronically by the Oyo State Lounge & Clubs Compliance Portal.")
                                    .FontSize(8.5f).FontColor("#6b5b57");
                                c.Item().Text("Valid without a physical signature or stamp.")
                                    .FontSize(8.5f).FontColor("#6b5b57");
                            });
                            r.RelativeItem().AlignRight().Column(c =>
                            {
                                c.Item().Width(170).LineHorizontal(0.8f).LineColor("#666");
                                c.Item().PaddingTop(2).Text("Receiving Officer")
                                    .FontSize(9).Bold();
                                c.Item().Text("Compliance Directorate")
                                    .FontSize(7.5f).FontColor("#999");
                            });
                        });
                    });

                page.Footer().AlignCenter().PaddingTop(10).Text(x =>
                {
                    x.Span("Oyo State Lounge & Clubs Compliance Portal · ")
                        .FontSize(8.5f).FontColor("#999");
                    x.Span($"{tx.Reference} · ").FontSize(8.5f).FontColor("#999");
                    x.Span(DateTime.UtcNow.Year.ToString()).FontSize(8.5f).FontColor("#999");
                });
            });
        }).GeneratePdf();
    }

    /// <summary>Naira figure to words, e.g. "Fifteen Thousand Naira Only".</summary>
    private static string NairaToWords(decimal amount)
    {
        string[] ones = ["Zero","One","Two","Three","Four","Five","Six","Seven","Eight","Nine","Ten",
            "Eleven","Twelve","Thirteen","Fourteen","Fifteen","Sixteen","Seventeen","Eighteen","Nineteen"];
        string[] tens = ["","","Twenty","Thirty","Forty","Fifty","Sixty","Seventy","Eighty","Ninety"];

        static string Chunk(int n, string[] o, string[] t) =>
            n < 20 ? o[n]
            : n < 100 ? t[n / 10] + (n % 10 > 0 ? "-" + o[n % 10] : "")
            : o[n / 100] + " Hundred" + (n % 100 > 0 ? " " + Chunk(n % 100, o, t) : "");

        static string Int(long n, string[] o, string[] t)
        {
            if (n == 0) return "";
            if (n < 1000) return Chunk((int)n, o, t);
            if (n < 1_000_000) return Int(n / 1000, o, t) + " Thousand" + (n % 1000 > 0 ? " " + Chunk((int)(n % 1000), o, t) : "");
            return Int(n / 1_000_000, o, t) + " Million" + (n % 1_000_000 > 0 ? " " + Int(n % 1_000_000, o, t) : "");
        }

        var naira = (long)Math.Floor(amount);
        var kobo = (int)Math.Round((amount - naira) * 100);
        var words = Int(naira, ones, tens) + " Naira";
        if (kobo > 0) words += $", {Int(kobo, ones, tens)} Kobo";
        return words + " Only";
    }
}
