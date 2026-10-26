using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LoungeClubsApi.Entities;

namespace LoungeClubsApi.Data.Configurations;

public class FineConfiguration : IEntityTypeConfiguration<Fine>
{
    public void Configure(EntityTypeBuilder<Fine> builder)
    {
        builder.ToTable("Fines");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Reference).HasMaxLength(30).IsRequired();
        builder.HasIndex(x => x.Reference).IsUnique();

        builder.Property(x => x.FineType).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.Reason).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.WaivedById).HasMaxLength(450);
        builder.Property(x => x.WaiverReason).HasMaxLength(500);

        builder.HasOne(x => x.Establishment)
            .WithMany(x => x.Fines)
            .HasForeignKey(x => x.EstablishmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.IssuedBy)
            .WithMany()
            .HasForeignKey(x => x.IssuedById)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Inspection)
            .WithMany()
            .HasForeignKey(x => x.InspectionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => x.Status);
    }
}
