using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LoungeClubsApi.Entities;

namespace LoungeClubsApi.Data.Configurations;

public class SmsOutboxConfiguration : IEntityTypeConfiguration<SmsOutbox>
{
    public void Configure(EntityTypeBuilder<SmsOutbox> builder)
    {
        builder.ToTable("SmsOutbox");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.To).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Message).HasMaxLength(459).IsRequired(); // 3 SMS segments
        builder.Property(x => x.Status).HasMaxLength(20).IsRequired();
        builder.Property(x => x.LastError).HasMaxLength(2000);
        builder.HasIndex(x => new { x.Status, x.NextAttemptAt });
    }
}
