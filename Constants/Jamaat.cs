namespace BurhaniGuards.Api.Constants;

/// <summary>
/// Jamaat constants for different Jamaats in Poona
/// </summary>
public static class Jamaat
{
    public const int Baramati = 1;
    public const int FakhriMohalla = 2;
    public const int ZainiMohalla = 3;
    public const int KalimiMohalla = 4;
    public const int Ahmednagar = 5;
    public const int ImadiMohalla = 6;
    public const int Kasarwadi = 7;
    public const int Khadki = 8;
    public const int Lonavala = 9;
    public const int MufaddalMohalla = 10;
    public const int Poona = 11;
    public const int SaifeeMohallah = 12;
    public const int TaiyebiMohalla = 13;
    public const int FatemiMohalla = 14;

    /// <summary>
    /// Gets jamaat text from ID
    /// </summary>
    public static string GetJamaatText(int? jamaatId)
    {
        return jamaatId switch
        {
            Baramati => "BARAMATI",
            FakhriMohalla => "FAKHRI MOHALLA (POONA)",
            ZainiMohalla => "ZAINI MOHALLA (POONA)",
            KalimiMohalla => "KALIMI MOHALLA (POONA)",
            Ahmednagar => "AHMEDNAGAR",
            ImadiMohalla => "IMADI MOHALLA (POONA)",
            Kasarwadi => "KASARWADI",
            Khadki => "KHADKI (POONA)",
            Lonavala => "LONAVALA",
            MufaddalMohalla => "MUFADDAL MOHALLA (POONA)",
            Poona => "POONA",
            SaifeeMohallah => "SAIFEE MOHALLAH (POONA)",
            TaiyebiMohalla => "TAIYEBI MOHALLA (POONA)",
            FatemiMohalla => "FATEMI MOHALLA (POONA)",
            _ => "Unknown"
        };
    }

    /// <summary>
    /// Gets jamaat ID from text
    /// </summary>
    public static int? GetJamaatId(string? jamaatText)
    {
        return jamaatText?.Trim() switch
        {
            "BARAMATI" => Baramati,
            "FAKHRI MOHALLA (POONA)" => FakhriMohalla,
            "ZAINI MOHALLA (POONA)" => ZainiMohalla,
            "KALIMI MOHALLA (POONA)" => KalimiMohalla,
            "AHMEDNAGAR" => Ahmednagar,
            "IMADI MOHALLA (POONA)" => ImadiMohalla,
            "KASARWADI" => Kasarwadi,
            "KHADKI (POONA)" => Khadki,
            "LONAVALA" => Lonavala,
            "MUFADDAL MOHALLA (POONA)" => MufaddalMohalla,
            "POONA" => Poona,
            "SAIFEE MOHALLAH (POONA)" => SaifeeMohallah,
            "TAIYEBI MOHALLA (POONA)" => TaiyebiMohalla,
            "FATEMI MOHALLA (POONA)" => FatemiMohalla,
            _ => null
        };
    }
}

