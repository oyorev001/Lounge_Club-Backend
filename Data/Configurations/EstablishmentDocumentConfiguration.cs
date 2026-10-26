using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LoungeClubsApi.Entities;

namespace LoungeClubsApi.Data.Configurations;

public class EstablishmentDocumentConfiguration : IEntityTypeConfiguration<EstablishmentDocument>
{
    public void Configure(EntityTypeBuilder<EstablishmentDocument> builder)
    {
        builder.ToTable("EstablishmentDocuments");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.DocumentType).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.FileUrl).HasMaxLength(500).IsRequired();
        builder.Property(x => x.FileName).HasMaxLength(255);

        builder.HasOne(x => x.Establishment)
            .WithMany(x => x.Documents)
            .HasForeignKey(x => x.EstablishmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
