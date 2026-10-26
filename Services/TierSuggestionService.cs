using LoungeClubsApi.Data;
using LoungeClubsApi.Entities;

namespace LoungeClubsApi.Services;

/// <summary>
/// Computes a suggested license tier from an establishment's declared stats.
/// The score guides the reviewing officer — officers make the binding decision.
/// </summary>
public class TierSuggestionService(AppDbContext db)
{
    public int ComputeScore(Establishment e)
    {
        var score = 0;

        score += e.Capacity switch
        {
            >= 500 => 40,
            >= 200 => 30,
            >= 50 => 15,
            _ => 5
        };

        score += e.StaffCount switch
        {
            >= 50 => 15,
            >= 20 => 10,
            >= 5 => 5,
            _ => 0
        };

        if (e.FloorAreaSqm >= 500) score += 10;
        else if (e.FloorAreaSqm >= 200) score += 5;

        if (e.HasLiveBand) score += 10;
        if (e.HasDj) score += 10;
        if (e.HasVipArea) score += 8;
        if (e.HasLodging) score += 8;
        if (e.HasRestaurant) score += 5;
        if (e.HasOutdoorSpace) score += 5;
        if (e.HasRooftop) score += 5;
        if (e.HasShisha) score += 3;
        if (e.HasPoolTable) score += 2;
        if (e.HasParking && e.ParkingSpaces >= 20) score += 5;

        return score;
    }

    public async Task<LicenseTier?> SuggestTierAsync(int score, CancellationToken ct = default)
    {
        return await db.LicenseTiers
            .Where(t => t.IsActive && score >= t.MinScore)
            .OrderByDescending(t => t.MinScore)
            .FirstOrDefaultAsync(ct);
    }
}
