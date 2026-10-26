using LoungeClubsApi.Entities.Enums;
using LoungeClubsApi.Models.Shared;

namespace LoungeClubsApi.Features.Establishments.GetAllEstablishments;

public class GetAllEstablishmentsRequest : PaginationRequest
{
    [FromQuery(Name = "search")]
    public string? Search { get; set; }

    [FromQuery(Name = "status")]
    public EstablishmentStatus? Status { get; set; }

    [FromQuery(Name = "lga")]
    public string? Lga { get; set; }

    [FromQuery(Name = "tier_id")]
    public Guid? TierId { get; set; }

    [FromQuery(Name = "type")]
    public EstablishmentType? Type { get; set; }
}
