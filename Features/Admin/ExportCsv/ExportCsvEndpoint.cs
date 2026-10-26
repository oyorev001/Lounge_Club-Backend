using System.Text;
using LoungeClubsApi.Authorization.Enums;
using LoungeClubsApi.Data;
using LoungeClubsApi.Entities.Enums;
using LoungeClubsApi.Extensions;

namespace LoungeClubsApi.Features.Admin.ExportCsv;

/// <summary>CSV exports for ministry reporting — levies, defaulters, certificates.</summary>
public class ExportCsvEndpoint : ICarterModule
{
    private static readonly UTF8Encoding Utf8 = new(encoderShouldEmitUTF8Identifier: true);

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("admin/levies.csv", async (AppDbContext db, CancellationToken ct) =>
        {
            var rows = await db.LevyAssessments.AsNoTracking()
                .OrderBy(l => l.Year).ThenBy(l => l.Reference)
                .Select(l => new
                {
                    l.Reference, Name = l.Establishment.Name, l.Establishment.Lga,
                    l.Year, l.Amount, Status = l.Status.ToString(),
                    l.DueDate, l.PaidAt
                })
                .ToListAsync(ct);

            var csv = BuildCsv(
                "Reference,Establishment,LGA,Year,Amount,Status,DueDate,PaidAt",
                rows.Select(r => new[]
                {
                    r.Reference, r.Name, r.Lga, r.Year.ToString(), r.Amount.ToString("F2"),
                    r.Status, r.DueDate.ToString("yyyy-MM-dd"), r.PaidAt?.ToString("yyyy-MM-dd HH:mm") ?? ""
                }));
            return CsvResult(csv, $"levies-{DateTime.UtcNow:yyyyMMdd}.csv");
        })
        .HasApiVersion(1).WithName("ExportLeviesCsv").WithTags("Exports")
        .RequireAuthorization().HasPermissions(AppPermission.LeviesViewAll);

        app.MapGet("admin/defaulters.csv", async (AppDbContext db, CancellationToken ct) =>
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var rows = await db.LevyAssessments.AsNoTracking()
                .Where(l => l.Status == LevyStatus.Overdue)
                .OrderBy(l => l.DueDate)
                .Select(l => new
                {
                    l.Reference, Name = l.Establishment.Name, l.Establishment.Lga,
                    l.Establishment.Address, l.Establishment.ContactPhone,
                    l.Amount, l.DueDate
                })
                .ToListAsync(ct);

            var csv = BuildCsv(
                "Reference,Establishment,LGA,Address,Phone,Amount,DueDate,DaysOverdue",
                rows.Select(r => new[]
                {
                    r.Reference, r.Name, r.Lga, r.Address ?? "", r.ContactPhone ?? "",
                    r.Amount.ToString("F2"), r.DueDate.ToString("yyyy-MM-dd"),
                    (today.DayNumber - r.DueDate.DayNumber).ToString()
                }));
            return CsvResult(csv, $"defaulters-{DateTime.UtcNow:yyyyMMdd}.csv");
        })
        .HasApiVersion(1).WithName("ExportDefaultersCsv").WithTags("Exports")
        .RequireAuthorization().HasAnyPermission(AppPermission.LeviesViewAll, AppPermission.ReportsView);

        app.MapGet("admin/certificates.csv", async (AppDbContext db, CancellationToken ct) =>
        {
            var rows = await db.Certificates.AsNoTracking()
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new
                {
                    c.CertificateNumber, Name = c.Establishment.Name, c.Establishment.Lga,
                    Type = c.Establishment.Type.ToString(),
                    Status = c.Status.ToString(), c.CreatedAt, c.ExpiryDate, c.LevyYear
                })
                .ToListAsync(ct);

            var csv = BuildCsv(
                "CertificateNumber,Establishment,LGA,Type,Status,IssuedAt,ExpiryDate,LevyYear",
                rows.Select(r => new[]
                {
                    r.CertificateNumber, r.Name, r.Lga, r.Type, r.Status,
                    r.CreatedAt.ToString("yyyy-MM-dd"), r.ExpiryDate.ToString("yyyy-MM-dd"),
                    r.LevyYear.ToString()
                }));
            return CsvResult(csv, $"certificates-{DateTime.UtcNow:yyyyMMdd}.csv");
        })
        .HasApiVersion(1).WithName("ExportCertificatesCsv").WithTags("Exports")
        .RequireAuthorization().HasPermissions(AppPermission.CertificatesViewAll);
    }

    private static string BuildCsv(string header, IEnumerable<string[]> rows)
    {
        var sb = new StringBuilder(header).AppendLine();
        foreach (var row in rows)
            sb.AppendLine(string.Join(",", row.Select(Escape)));
        return sb.ToString();
    }

    private static string Escape(string? v)
        => string.IsNullOrEmpty(v) ? "" : $"\"{v.Replace("\"", "\"\"")}\"";

    private static IResult CsvResult(string csv, string filename) =>
        Results.File(Utf8.GetBytes(csv), "text/csv", fileDownloadName: filename);
}
