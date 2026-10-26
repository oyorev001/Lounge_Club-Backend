using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LoungeClubsApi.Entities;
using LoungeClubsApi.Entities.Enums;

namespace LoungeClubsApi.Data.Configurations;

public class EstablishmentConfiguration : IEntityTypeConfiguration<Establishment>
{
    public void Configure(EntityTypeBuilder<Establishment> builder)
    {
        builder.ToTable("Establishments");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.RegistrationNumber)
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(x => x.RegistrationNumber).IsUnique();

        builder.Property(x => x.Name).HasMaxLength(255).IsRequired();
        builder.Property(x => x.Address).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Lga).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Landmark).HasMaxLength(255);
        builder.Property(x => x.Latitude);
        builder.Property(x => x.Longitude);
        builder.Property(x => x.ContactPhone).HasMaxLength(20).IsRequired();
        builder.Property(x => x.ContactEmail).HasMaxLength(255);
        builder.Property(x => x.CacNumber).HasMaxLength(50);
        builder.Property(x => x.OwnerNin).HasMaxLength(20).IsRequired();

        builder.Property(x => x.AdditionalTypes)
            .HasConversion(
                v => string.Join(',', (v ?? new List<EstablishmentType>()).Select(t => t.ToString())),
                v => string.IsNullOrWhiteSpace(v)
                    ? new List<EstablishmentType>()
                    : v.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(ParseType)
                        .Where(t => t.HasValue)
                        .Select(t => t!.Value)
                        .ToList())
            .HasMaxLength(100)
            .IsRequired()
            .Metadata.SetValueComparer(new ValueComparer<List<EstablishmentType>>(
                (a, b) => a!.SequenceEqual(b!),
                v => v.Aggregate(0, (h, t) => HashCode.Combine(h, t.GetHashCode())),
                v => v.ToList()));
        builder.Property(x => x.OperatingHours).HasMaxLength(100);
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.Property(x => x.RejectionReason).HasMaxLength(1000);

        builder.Property(x => x.Type).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.NonCompliantReason).HasMaxLength(30);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);

        builder.HasOne(x => x.Owner)
            .WithMany()
            .HasForeignKey(x => x.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Tier)
            .WithMany(x => x.Establishments)
            .HasForeignKey(x => x.TierId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.Lga);
        builder.HasIndex(x => x.TierId);
    }

    private static EstablishmentType? ParseType(string s) =>
        Enum.TryParse<EstablishmentType>(s, out var t) ? t : null;
}
