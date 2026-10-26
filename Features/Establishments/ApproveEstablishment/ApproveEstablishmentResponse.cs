using LoungeClubsApi.Entities.Enums;

namespace LoungeClubsApi.Features.Establishments.ApproveEstablishment;

public record ApproveEstablishmentResponse(
    Guid EstablishmentId,
    EstablishmentStatus Status,
    string TierName,
    Guid? LevyAssessmentId,
    decimal? LevyAmount);
