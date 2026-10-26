using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LoungeClubsApi.Entities;

namespace LoungeClubsApi.Data.Configurations;

public class WebhookLogConfiguration : IEntityTypeConfiguration<WebhookLog>
{
    public void Configure(EntityTypeBuilder<WebhookLog> builder)
    {
        builder.ToTable("WebhookLogs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Provider).HasMaxLength(30).IsRequired();
        builder.Property(x => x.EventId).HasMaxLength(150);
        builder.Property(x => x.EventType).HasMaxLength(100);
        builder.Property(x => x.Signature).HasMaxLength(255);
        builder.Property(x => x.ProcessingResult).HasMaxLength(2000);

        // Provider retries deliver the same event — the unique index turns
        // duplicate inserts into a no-op instead of double settlement.
        builder.HasIndex(x => new { x.Provider, x.EventId })
            .IsUnique()
            .HasFilter("\"EventId\" IS NOT NULL");
        builder.HasIndex(x => x.ReceivedAt);
    }
}
