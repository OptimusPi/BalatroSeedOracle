namespace BalatroSeedOracle.Constants;

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

    // Animation/Timing - Core intervals
    public const int DefaultDelayMs = 100;
    public const int SearchUpdateIntervalMs = 100;
    public const double AnimationFrameRateMs = 16.67; // 60 FPS standard
    public const int AnimationUpdateRateMs = 16; // ~60 FPS for timers

    // Animation Durations (in milliseconds)
    public const int QuickAnimationDurationMs = 125; // Card flip pinch
    public const int FastAnimationDurationMs = 200; // Modal overlay fade
    public const int StandardAnimationDurationMs = 250; // Standard transitions
    public const int MediumAnimationDurationMs = 300; // Card flip reveal
    public const int SlowAnimationDurationMs = 320; // Modal content slide
    public const int JuiceDurationMs = 400; // Juice effect on card grab
    public const int BounceAnimationDurationMs = 600; // Modal rise with bounce
    public const int GravityAnimationDurationMs = 800; // Modal gravity fall
    public const double JuiceDurationSeconds = 0.4; // Juice duration in seconds for math

    // Animation Durations (in seconds for TimeSpan)
    public const double StandardTimeoutSeconds = 1.0;
    public const double NetworkTimeoutSeconds = 10.0;
    public const double WidgetTimerIntervalSeconds = 5.0;

    // Scale Factors
    public const double DefaultScaleFactor = 1.0;
    public const double CardJuiceScaleFactor = 0.4; // Scale bounce on card grab
    public const double CardJuiceRotationFactor = 0.6; // Rotation wobble multiplier
    public const double CardFlipJuiceScalePeak = 1.3; // Peak scale during flip juice animation

    // Balatro card animation multipliers (unitless)
    public const double CardDragLeanMultiplier = 0.3; // From card.lua - drag lean strength
    public const double CardAmbientTiltMultiplier = 0.2; // From card.lua - idle breathing strength
    public const double CardRotationToDegrees = 10.0; // Multiplier to convert to degrees
    public const double CardSwayRotationAmplitude = 0.02; // ~1.15° max wobble

    // Physics Constants
    public const double JuiceBounceFrequency = 50.8; // Scale oscillation Hz
    public const double JuiceWobbleFrequency = 40.8; // Rotation wobble Hz
    public const double FloatingFrequency = 0.666; // ~1.5 second cycle
    public const double FloatingVerticalAmplitude = 0.3; // 30% breathing variation
    public const double FloatingHorizontalAmplitude = 0.2; // 20% sway variation

    // Opacity Values
    public const double FullOpacity = 1.0;
    public const double DisabledOpacity = 0.8;
    public const double InvisibleOpacity = 0.0;

    // Modal Animation Offsets
    public const double ModalSlideOffsetY = -24; // Initial Y offset for slide-up
    public const double ModalSlideOffsetBottomMargin = 24; // Bottom margin during animation

    // Shadow Offsets (in pixels)
    public const double ShadowOffsetSmallX = 1;
    public const double ShadowOffsetSmallY = 2;
    public const double ShadowOffsetMediumX = 2;
    public const double ShadowOffsetMediumY = 4;
    public const double ShadowOffsetLargeX = 3;

    // File Extensions
    public static readonly string[] ConfigFilePatterns = ["*.json"];
    public static readonly string[] CsvFilePatterns = ["*.csv"];

    // UI Text
    public const string ConfigFileTypeDescription = "Config Files";
    public const string CsvFileTypeDescription = "CSV Files";
    public const string LoadConfigTitle = "Select Motely Config File";
    public const string ExportResultsTitle = "Export Search Results";
    public const string SearchModalTitle = "SEED SEARCH - MAXIMIZED";
}
