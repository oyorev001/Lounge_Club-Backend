namespace LoungeClubsApi.Features.Levies.GetLevyById;

public record GetLevyByIdQuery([Required] Guid Id) : IQuery<LevyDetailResponse>;
