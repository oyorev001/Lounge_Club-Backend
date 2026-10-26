using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LoungeClubsApi.Entities;

namespace LoungeClubsApi.Data.Configurations;

public class CertificateConfiguration : IEntityTypeConfiguration<Certificate>
{
    public void Configure(EntityTypeBuilder<Certificate> builder)
    {
        builder.ToTable("Certificates");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.CertificateNumber).HasMaxLength(30).IsRequired();
        builder.HasIndex(x => x.CertificateNumber).IsUnique();

        builder.Property(x => x.QrToken).HasMaxLength(100).IsRequired();
        builder.HasIndex(x => x.QrToken).IsUnique();

        builder.Property(x => x.AmountPaid).HasPrecision(18, 2);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.RevokedReason).HasMaxLength(500);
        builder.Property(x => x.RevokedById).HasMaxLength(450);

        builder.HasOne(x => x.Establishment)
            .WithMany(x => x.Certificates)
            .HasForeignKey(x => x.EstablishmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.LevyAssessment)
            .WithOne(x => x.Certificate)
            .HasForeignKey<Certificate>(x => x.LevyAssessmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.ExpiryDate);
        builder.HasIndex(x => new { x.Status, x.ExpiryDate }); // reminder/expiry job scans
    }
}
