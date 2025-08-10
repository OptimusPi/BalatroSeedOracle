namespace Oracle.Constants;

/// <summary>
/// UI constants to eliminate magic numbers and hardcoded values throughout the application
/// </summary>
public static class UIConstants
{
    // Sprite Dimensions
    public const int JokerSpriteWidth = 71;
    public const int JokerSpriteHeight = 95;
    public const int TagSpriteWidth = 34;
    public const int TagSpriteHeight = 34;
    public const int TarotSpriteWidth = 71;
    public const int TarotSpriteHeight = 95;
    public const int SpectralSpriteWidth = 71;
    public const int SpectralSpriteHeight = 95;
    public const int VoucherSpriteWidth = 71;
    public const int VoucherSpriteHeight = 95;
    public const int UIAssetSpriteWidth = 18;
    public const int UIAssetSpriteHeight = 18;


    // Font Sizes
    public const int SmallFontSize = 12;
    public const int DefaultFontSize = 14;
    public const int LargeFontSize = 16;
    public const int HeaderFontSize = 18;

    // Layout Constants
    public const double DefaultCornerRadius = 6.0;
    public const double DefaultPadding = 5.0;
    public const double DefaultMargin = 10.0;
    public const int MinModalHeight = 120;
    public const int MinFilterHeight = 60;

    public const string BorderGold = "#EAC058"; // RGB(234, 192, 88)

    // Grid Proportions
    public const string StandardGridProportion = "0.95*";

    // Animation/Timing
    public const int DefaultDelayMs = 100;
    public const int SearchUpdateIntervalMs = 100;

    // File Extensions
    public static readonly string[] ConfigFilePatterns = ["*.json"];
    public static readonly string[] CsvFilePatterns = ["*.csv"];

    // UI Text
    public const string ConfigFileTypeDescription = "Config Files";
    public const string CsvFileTypeDescription = "CSV Files";
    public const string LoadConfigTitle = "Select Motely Config File";
    public const string ExportResultsTitle = "Export Search Results";
    public const string SearchModalTitle = "SEED SEARCH - MAXIMIZED";

    // Tab left side nav buttons for filter builder modal
    public const string TarotColor1 = "#a58547";
    public const string TarotColor2 = "#ffe5b4";

    public const string SpectralColor1 = "#c7b24a";
    public const string SpectralColor2 = "#5e7297";

    public const string VoucherColor1 = "#7ecace";
    public const string VoucherColor2 = "#50a8ac";

    public const string JokerColor1 = "#E74C3C";
    public const string JokerColor2 = "#C0392B";

    public const string BossColor1 = "#ac3232";
    public const string BossColor2 = "#4f6367";

    public const string TagColor1 = "#30394a";
    public const string TagColor2 = "#000000";

    public const string CelestialColor1 = "#5b9baa";
    public const string CelestialColor2 = "#dff5fc";


}
