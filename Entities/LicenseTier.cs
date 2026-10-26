using LoungeClubsApi.Entities.Base;

namespace LoungeClubsApi.Entities;

public class LicenseTier : AuditableEntity
{
    public string Code { get; set; } = string.Empty; // PLATINUM, GOLD, SILVER, BRONZE
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal AnnualFee { get; set; }
    public int SortOrder { get; set; }
    public int MinScore { get; set; } // tier suggestion threshold
    public bool IsActive { get; set; } = true;

    public ICollection<Establishment> Establishments { get; set; } = [];
}
