namespace LoungeClubsApi.Entities;

using LoungeClubsApi.Entities.Base;
using Microsoft.AspNetCore.Identity;

public class User : IdentityUser, IAuditableEntity
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? BusinessName { get; set; }
    public bool IsVerified { get; set; }
    public string? EmailVerificationCodeHash { get; set; }
    public DateTimeOffset? EmailVerificationExpiry { get; set; }
    /// <summary>Failed verification attempts — code is wiped after the cap.</summary>
    public int EmailVerificationAttempts { get; set; }
    /// <summary>LGAs an Officer may act on — empty list means statewide.
    /// Admins are never scoped.</summary>
    public List<string> AssignedLgas { get; set; } = [];
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiryTime { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    public ICollection<Establishment> Establishments { get; set; } = [];
}
