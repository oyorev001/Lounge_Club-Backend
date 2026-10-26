namespace LoungeClubsApi.Helpers;

public static class NinRules
{
    // Known dummy/test values owners might type just to pass validation.
    private static readonly HashSet<string> Blocklist =
    [
        "00000000000", "11111111111", "12345678901", "12345678900",
        "01234567890", "09876543210", "98765432109", "99999999999",
        "55555555555", "22222222222", "33333333333", "44444444444",
        "66666666666", "77777777777", "88888888888",
    ];

    public static bool IsValidFormat(string? nin) =>
        !string.IsNullOrEmpty(nin) && nin.Length == 11 && nin.All(char.IsDigit);

    /// <summary>
    /// Format is right AND the value isn't an obvious dummy: not all-identical
    /// digits, not a simple ascending/descending run, not a known test number.
    /// </summary>
    public static bool IsPlausible(string? nin)
    {
        if (!IsValidFormat(nin))
            return false;

        if (Blocklist.Contains(nin!))
            return false;

        // All identical digits (e.g. 11111111111) — covered by blocklist too,
        // but guard any future repeated digit not in the list.
        if (nin!.Distinct().Count() == 1)
            return false;

        // Strictly ascending or descending runs (e.g. 12345678901, 54321098765)
        var ascending = true;
        var descending = true;
        for (var i = 1; i < nin.Length; i++)
        {
            ascending &= nin[i] == nin[i - 1] + 1;
            descending &= nin[i] == nin[i - 1] - 1;
        }

        return !(ascending || descending);
    }
}
