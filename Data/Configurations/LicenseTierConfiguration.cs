using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LoungeClubsApi.Entities;

namespace LoungeClubsApi.Data.Configurations;

public class LicenseTierConfiguration : IEntityTypeConfiguration<LicenseTier>
{
    public void Configure(EntityTypeBuilder<LicenseTier> builder)
    {
        builder.ToTable("LicenseTiers");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code).HasMaxLength(20).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();

        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.AnnualFee).HasPrecision(18, 2);
    }
}
