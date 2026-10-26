using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LoungeClubsApi.Entities;

namespace LoungeClubsApi.Data.Configurations;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("Transactions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Reference).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => x.Reference).IsUnique();

        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.Channel).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.Context).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.Processor).HasMaxLength(30);
        builder.Property(x => x.IdempotencyKey).HasMaxLength(100);
        builder.Property(x => x.GatewayReference).HasMaxLength(255);
        builder.Property(x => x.WebhookId).HasMaxLength(100);
        builder.Property(x => x.SenderName).HasMaxLength(255);
        builder.Property(x => x.SenderBank).HasMaxLength(100);
        builder.Property(x => x.Narration).HasMaxLength(500);
        builder.Property(x => x.VirtualAccountNumber).HasMaxLength(20);
        builder.Property(x => x.VirtualAccountName).HasMaxLength(255);
        builder.Property(x => x.VirtualAccountBankName).HasMaxLength(100);
        builder.Property(x => x.CheckoutUrl).HasMaxLength(2000);
        builder.Property(x => x.AccessCode).HasMaxLength(100);
        builder.Property(x => x.FailureReason).HasMaxLength(500);

        // Unique only while Pending — an expired/failed transaction frees the
        // key so a legitimate retry can open a fresh transaction.
        builder.HasIndex(x => x.IdempotencyKey)
            .IsUnique()
            .HasFilter("\"IdempotencyKey\" IS NOT NULL AND \"Status\" = 'Pending'");
        builder.HasIndex(x => x.VirtualAccountNumber);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => new { x.Status, x.ExpiresAt }); // PaymentTimeoutJob scan

        // At most ONE open checkout per levy/fine — prevents concurrent
        // payment initiations creating multiple payable transactions.
        builder.HasIndex(x => new { x.Context, x.ContextId })
            .IsUnique()
            .HasFilter("\"Status\" = 'Pending'");

        builder.HasOne(x => x.Establishment)
            .WithMany()
            .HasForeignKey(x => x.EstablishmentId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
