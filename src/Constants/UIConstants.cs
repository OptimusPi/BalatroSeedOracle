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

    // Colors
    public const string PrimaryBackground = "#374244";
    public const string AccentColor = "#FEB95F";
    public const string SecondaryColor = "#FF2D3436";
    public const string ButtonPrimary = "#0095f0";
    public const string TarotColor = "#3498DB";
    public const string DefaultGray = "#95A5A6";
    public const string BorderGold = "#EAC058"; // RGB(234, 192, 88)

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

    // Grid Proportions
    public const string StandardGridProportion = "0.95*";

    // Animation/Timing
    public const int DefaultDelayMs = 100;
    public const int SearchUpdateIntervalMs = 100;

    // File Extensions
    public static readonly string[] ConfigFilePatterns = { "*.ouija.json", "*.json" };
    public static readonly string[] CsvFilePatterns = { "*.csv" };

    // UI Text
    public const string ConfigFileTypeDescription = "Config Files";
    public const string CsvFileTypeDescription = "CSV Files";
    public const string LoadConfigTitle = "Select Motely Config File";
    public const string ExportResultsTitle = "Export Search Results";
    public const string SearchModalTitle = "SEED SEARCH - MAXIMIZED";
}