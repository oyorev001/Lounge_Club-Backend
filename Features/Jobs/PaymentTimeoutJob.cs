using Microsoft.Extensions.DependencyInjection;
using LoungeClubsApi.Attributes;
using TickerQ.Utilities.Base;
using LoungeClubsApi.Data;
using LoungeClubsApi.Entities.Enums;

namespace LoungeClubsApi.Features.Jobs;

/// <summary>
/// Hourly job that fails pending payment transactions whose virtual-account
/// session has expired.
/// The cron expression auto-seeds a CronTicker on startup.
/// </summary>
[ScopedService]
public class PaymentTimeoutJob(IServiceScopeFactory scopeFactory, ILogger<PaymentTimeoutJob> logger)
{
    [TickerFunction("PaymentTimeout", cronExpression: "0 0 * * * *")]
    public async Task Run(
        TickerFunctionContext context,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Atomic update with the Pending predicate — a webhook can settle the
        // transaction between our SELECT and write; without the predicate we'd
        // clobber a Successful payment back to Failed.
        var count = await db.Transactions
            .Where(t => t.Status == PaymentStatus.Pending && t.ExpiresAt != null && t.ExpiresAt < now)
            .ExecuteUpdateAsync(u => u
                .SetProperty(t => t.Status, PaymentStatus.Failed)
                .SetProperty(t => t.FailureReason, "Payment session expired"),
                cancellationToken);

        if (count == 0)
        {
            logger.LogInformation("PaymentTimeout job ran — no expired transactions.");
            return;
        }
        var expired = new List<int>(new int[count]); // keep the log shape simple

        logger.LogInformation("PaymentTimeout job failed {Count} expired transactions.", count);
    }
}
