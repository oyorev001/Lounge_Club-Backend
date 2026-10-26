using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LoungeClubsApi.Entities;

namespace LoungeClubsApi.Data.Configurations;

public class EmailOutboxConfiguration : IEntityTypeConfiguration<EmailOutbox>
{
    public void Configure(EntityTypeBuilder<EmailOutbox> builder)
    {
        builder.ToTable("EmailOutbox");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.To).HasMaxLength(255).IsRequired();
        builder.Property(x => x.Subject).HasMaxLength(255).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.LastError).HasMaxLength(2000);

        builder.HasIndex(x => new { x.Status, x.NextAttemptAt });
    }
}
