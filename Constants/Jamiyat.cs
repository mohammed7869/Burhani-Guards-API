namespace BurhaniGuards.Api.Constants;

/// <summary>
/// Jamiyat constants - Currently only Poona exists
/// </summary>
public static class Jamiyat
{
    public const int Poona = 1;

    /// <summary>
    /// Gets jamiyat text from ID
    /// </summary>
    public static string GetJamiyatText(int? jamiyatId)
    {
        return jamiyatId switch
        {
            Poona => "Poona",
            _ => "Unknown"
        };
    }

    /// <summary>
    /// Gets jamiyat ID from text
    /// </summary>
    public static int? GetJamiyatId(string? jamiyatText)
    {
        return jamiyatText?.Trim() switch
        {
            "Poona" => Poona,
            _ => null
        };
    }
}


