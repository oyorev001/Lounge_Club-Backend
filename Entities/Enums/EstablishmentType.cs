namespace LoungeClubsApi.Entities.Enums;

public enum EstablishmentType
{
    Lounge,
    Bar,
    Club,
    Nightclub
}

public static class EstablishmentTypeExtensions
{
    // Higher rank = heavier compliance class. The highest declared
    // type becomes the primary classification for levy purposes.
    private static int Rank(EstablishmentType type) => type switch
    {
        EstablishmentType.Nightclub => 4,
        EstablishmentType.Club => 3,
        EstablishmentType.Lounge => 2,
        EstablishmentType.Bar => 1,
        _ => 0
    };

    public static EstablishmentType PrimaryOf(this IEnumerable<EstablishmentType> types) =>
        types.OrderByDescending(Rank).First();
}
