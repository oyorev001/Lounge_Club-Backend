using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LoungeClubsApi.Entities;

namespace LoungeClubsApi.Data.Configurations;

public class LevyAssessmentConfiguration : IEntityTypeConfiguration<LevyAssessment>
{
    public void Configure(EntityTypeBuilder<LevyAssessment> builder)
    {
        builder.ToTable("LevyAssessments");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Reference).HasMaxLength(30).IsRequired();
        builder.HasIndex(x => x.Reference).IsUnique();

        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.WaiverReason).HasMaxLength(500);
        builder.Property(x => x.WaivedById).HasMaxLength(450);

        builder.HasOne(x => x.Establishment)
            .WithMany(x => x.LevyAssessments)
            .HasForeignKey(x => x.EstablishmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Tier)
            .WithMany()
            .HasForeignKey(x => x.TierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.EstablishmentId, x.Year }).IsUnique();
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.Year);
    }
}
