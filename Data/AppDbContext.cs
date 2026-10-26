using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using LoungeClubsApi.Entities;

namespace LoungeClubsApi.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<User>(options)
{
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<Establishment> Establishments { get; set; }
    public DbSet<EstablishmentDocument> EstablishmentDocuments { get; set; }
    public DbSet<LicenseTier> LicenseTiers { get; set; }
    public DbSet<LevyAssessment> LevyAssessments { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<Certificate> Certificates { get; set; }
    public DbSet<Inspection> Inspections { get; set; }
    public DbSet<Fine> Fines { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<WebhookLog> WebhookLogs { get; set; }
    public DbSet<EmailOutbox> EmailOutbox { get; set; }
    public DbSet<SmsOutbox> SmsOutbox { get; set; }
    public DbSet<Complaint> Complaints { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
