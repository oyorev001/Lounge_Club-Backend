using System.Text.Json;
using Microsoft.EntityFrameworkCore.Diagnostics;
using LoungeClubsApi.Attributes;
using LoungeClubsApi.Entities;
using LoungeClubsApi.Entities.Base;
using Microsoft.AspNetCore.Identity;

namespace LoungeClubsApi.Interceptors;

/// <summary>
/// EF Core interceptor that automatically populates audit fields and generates audit logs.
/// Captures device info (IP, UserAgent) and detailed property changes (OldValues/NewValues).
/// </summary>
[SingletonService]
public class AuditInterceptor(IHttpContextAccessor httpContextAccessor) : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        UpdateAuditFieldsAndLog(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        UpdateAuditFieldsAndLog(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    /// <summary>Properties never written to the audit trail — credentials,
    /// session tokens, and EF bookkeeping columns have no forensic value and
    /// leaking them into a readable log is a security exposure.</summary>
    private static readonly HashSet<string> NeverAudit = new(StringComparer.Ordinal)
    {
        // credentials & tokens
        "PasswordHash", "RefreshToken", "RefreshTokenExpiryTime",
        "SecurityStamp", "ConcurrencyStamp",
        "EmailVerificationCodeHash", "PasswordResetCodeHash",
        "EmailVerificationExpiry", "EmailVerificationAttempts",
        "TwoFactorSecret", "TwoFactorRecoveryCodes",
        // derived/noise columns
        "NormalizedEmail", "NormalizedUserName",
    };

    /// <summary>Entities whose lifecycle is housekeeping, not a business
    /// decision — notifications and outbox rows are generated *by* other
    /// audited actions, so auditing them too just doubles every entry.</summary>
    private static readonly HashSet<string> HousekeepingTables = new(StringComparer.Ordinal)
    {
        "Notifications", "EmailOutbox", "SmsOutbox", "WebhookLogs", "AuditLogs",
    };

    private void UpdateAuditFieldsAndLog(DbContext? context)
    {
        if (context is null) return;

        var httpContext = httpContextAccessor.HttpContext;
        var currentUser = httpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? httpContext?.User?.Identity?.Name
            ?? "System";
        var ipAddress = httpContext?.Connection?.RemoteIpAddress?.ToString();
        var userAgent = httpContext?.Request?.Headers["User-Agent"].ToString();
        var now = DateTimeOffset.UtcNow;

        var auditEntries = new List<AuditLog>();

        // 1. Handle AuditableEntity fields (CreatedAt, UpdatedAt, etc.)
        foreach (var entry in context.ChangeTracker.Entries<IAuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.CreatedBy = currentUser;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
                entry.Entity.UpdatedBy = currentUser;
            }
        }

        // 2. Generate Audit Logs for detailed tracking
        // We now iterate over all entries and filter for BaseEntity or User (IdentityUser)
        foreach (var entry in context.ChangeTracker.Entries())
        {
            // Skip AuditLog entity itself to prevent infinite recursion, plus
            // housekeeping entities — notifications/outbox are generated *by*
            // the audited action, auditing them doubles every entry.
            if (entry.Entity is AuditLog or EmailOutbox or SmsOutbox or WebhookLog or Notification
                || entry.State == EntityState.Detached || entry.State == EntityState.Unchanged)
                continue;

            // Filter for supported entities: BaseEntity (Guid Id) or User (string Id)
            if (entry.Entity is not BaseEntity && entry.Entity is not User)
                continue;

            var primaryKey = entry.Entity switch
            {
                BaseEntity baseEntity => baseEntity.Id.ToString(),
                IdentityUser identityUser => identityUser.Id,
                _ => null
            };

            if (primaryKey is null) continue;

            var auditEntry = new AuditLog
            {
                UserId = currentUser,
                Type = entry.State.ToString(),
                TableName = entry.Metadata.GetTableName() ?? entry.Entity.GetType().Name,
                PrimaryKey = primaryKey,
                Timestamp = now,
                IpAddress = ipAddress,
                UserAgent = userAgent
            };

            var oldValues = new Dictionary<string, object?>();
            var newValues = new Dictionary<string, object?>();

            foreach (var property in entry.Properties)
            {
                string propertyName = property.Metadata.Name;
                if (property.IsTemporary) continue;

                // Don't audit the ID as it's the key
                if (property.Metadata.IsPrimaryKey()) continue;

                // Never write credentials, tokens, or EF bookkeeping values
                if (NeverAudit.Contains(propertyName)) continue;

                switch (entry.State)
                {
                    case EntityState.Added:
                        newValues[propertyName] = property.CurrentValue;
                        break;
                    case EntityState.Deleted:
                        oldValues[propertyName] = property.OriginalValue;
                        break;
                    case EntityState.Modified:
                        if (property.IsModified)
                        {
                            oldValues[propertyName] = property.OriginalValue;
                            newValues[propertyName] = property.CurrentValue;
                        }
                        break;
                }
            }

            // Skip entries where the only changes were excluded/noise columns
            // (e.g. a User login only rotating RefreshToken + stamps).
            if (oldValues.Count == 0 && newValues.Count == 0)
                continue;

            // Serialize dictionaries to JSON
            if (oldValues.Count > 0)
                auditEntry.OldValues = JsonSerializer.Serialize(oldValues);

            if (newValues.Count > 0)
                auditEntry.NewValues = JsonSerializer.Serialize(newValues);

            auditEntries.Add(auditEntry);
        }

        // Add generated audit logs to context
        if (auditEntries.Count > 0)
        {
            context.Set<AuditLog>().AddRange(auditEntries);
        }
    }
}
