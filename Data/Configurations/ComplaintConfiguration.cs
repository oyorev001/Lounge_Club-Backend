using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LoungeClubsApi.Entities;

namespace LoungeClubsApi.Data.Configurations;

public class ComplaintConfiguration : IEntityTypeConfiguration<Complaint>
{
    public void Configure(EntityTypeBuilder<Complaint> builder)
    {
        builder.ToTable("Complaints");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Category).HasMaxLength(60).IsRequired();
        builder.Property(x => x.Details).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.VenueDescription).HasMaxLength(500);
        builder.Property(x => x.ComplainantName).HasMaxLength(200);
        builder.Property(x => x.ComplainantPhone).HasMaxLength(30);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.ResolutionNote).HasMaxLength(1000);
        builder.HasOne(x => x.Establishment).WithMany().HasForeignKey(x => x.EstablishmentId).OnDelete(DeleteBehavior.SetNull);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.EstablishmentId);
    }
}
