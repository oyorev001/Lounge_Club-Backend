using Npgsql;

namespace LoungeClubsApi.Extensions;

public static class DbContextExtensions
{
    /// <summary>
    /// Saves changes, retrying when a unique-index violation indicates a
    /// sequence-based reference collided under concurrency. On each retry the
    /// caller's <paramref name="regenerate"/> delegate re-assigns the reference(s).
    /// </summary>
    public static async Task SaveChangesWithUniqueRetryAsync(
        this DbContext db,
        Func<Task> regenerate,
        CancellationToken ct = default,
        int maxAttempts = 5)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await db.SaveChangesAsync(ct);
                return;
            }
            catch (DbUpdateException ex) when (attempt < maxAttempts && IsUniqueViolation(ex))
            {
                await regenerate();
            }
        }
    }

    public static bool IsUniqueViolation(this DbUpdateException ex)
    {
        for (var inner = ex.InnerException; inner is not null; inner = inner.InnerException)
        {
            // PostgreSQL unique violation
            if (inner is PostgresException { SqlState: "23505" })
                return true;

            // SQL Server unique index/constraint violation
            if (inner.GetType().Name == "SqlException"
                && inner.GetType().GetProperty("Number")?.GetValue(inner) is int n
                && n is 2601 or 2627)
                return true;

            // MySQL duplicate entry
            if (inner.GetType().Name == "MySqlException"
                && inner.GetType().GetProperty("Number")?.GetValue(inner) is int m
                && m == 1062)
                return true;
        }

        return false;
    }
}
