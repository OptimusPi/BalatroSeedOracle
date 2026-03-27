using Avalonia.Media;

namespace BalatroSeedOracle.Theme;

/// <summary>
/// Authentic Balatro color palette. Single source of truth — no AXAML, no duplicates.
/// </summary>
public static class BalatroColors
{
    // ── Primary Brand Colors ──
    public static readonly Color White = Color.Parse("#FFFFFF");
    public static readonly Color Black = Color.Parse("#000000");
    public static readonly Color Red = Color.Parse("#ff4c40");
    public static readonly Color DarkRed = Color.Parse("#a02721");
    public static readonly Color DarkestRed = Color.Parse("#70150f");
    public static readonly Color Blue = Color.Parse("#0093ff");
    public static readonly Color DarkBlue = Color.Parse("#0057a1");
    public static readonly Color Orange = Color.Parse("#ff9800");
    public static readonly Color DarkOrange = Color.Parse("#a05b00");
    public static readonly Color DarkGold = Color.Parse("#b8883a");
    public static readonly Color Green = Color.Parse("#429f79");
    public static readonly Color DarkGreen = Color.Parse("#215f46");
    public static readonly Color Purple = Color.Parse("#7d60e0");
    public static readonly Color DarkPurple = Color.Parse("#292189");

    // ── Special / Text Colors ──
    public static readonly Color BrightGold = Color.Parse("#eaba44");
    public static readonly Color BrightGreen = Color.Parse("#35bd86");

    // ── Modal / Panel Colors ──
    public static readonly Color Grey = Color.Parse("#3a5055");
    public static readonly Color MediumGrey = Color.Parse("#33464b");
    public static readonly Color DarkGrey = Color.Parse("#1e2b2d");
    public static readonly Color BrightSilver = Color.Parse("#b9c2d2");
    public static readonly Color LightGrey = Color.Parse("#777e89");
    public static readonly Color FadedGrey = Color.Parse("#565b5c");

    // ── Shadow Colors ──
    public static readonly Color MediumShadow = Color.Parse("#1e2e32");
    public static readonly Color DarkShadow = Color.Parse("#0b1415");
    public static readonly Color TranslucentBlack33 = Color.Parse("#55000000");

    // ── Disabled Control Colors ──
    public static readonly Color DarkDullGrey = Color.Parse("#2d2d2d");
    public static readonly Color DullGrey = Color.Parse("#5c5c5c");
    public static readonly Color LightDullGrey = Color.Parse("#6b6b6b");
    public static readonly Color LightDullWashGrey = Color.Parse("#545454");

    // ── Other UI Colors ──
    public static readonly Color BurntRed = Color.Parse("#8f3b36");
    public static readonly Color LightSilver = Color.Parse("#a3acb9");
    public static readonly Color GreySilver = Color.Parse("#686e78");
    public static readonly Color PurpleViolet = Color.Parse("#9B7EDE");
    public static readonly Color PurpleMuted = Color.Parse("#6B5AA8");

    // ── Overlay Colors ──
    public static readonly Color SemiTransparentBlack = Color.Parse("#80000000");
    public static readonly Color SemiTransparentDark = Color.Parse("#66000000");
    public static readonly Color TransparentDark3 = Color.Parse("#33000000");
}

/// <summary>
/// Pre-built brushes for direct use in C# markup. Frozen for perf.
/// </summary>
public static class BalatroBrushes
{
    // ── Brand Action Brushes ──
    public static readonly ISolidColorBrush White = Brush(BalatroColors.White);
    public static readonly ISolidColorBrush Black = Brush(BalatroColors.Black);
    public static readonly ISolidColorBrush Red = Brush(BalatroColors.Red);
    public static readonly ISolidColorBrush RedHover = Brush(BalatroColors.DarkRed);
    public static readonly ISolidColorBrush Blue = Brush(BalatroColors.Blue);
    public static readonly ISolidColorBrush BlueHover = Brush(BalatroColors.DarkBlue);
    public static readonly ISolidColorBrush Orange = Brush(BalatroColors.Orange);
    public static readonly ISolidColorBrush OrangeHover = Brush(BalatroColors.DarkOrange);
    public static readonly ISolidColorBrush Green = Brush(BalatroColors.Green);
    public static readonly ISolidColorBrush GreenHover = Brush(BalatroColors.DarkGreen);
    public static readonly ISolidColorBrush Purple = Brush(BalatroColors.Purple);
    public static readonly ISolidColorBrush PurpleHover = Brush(BalatroColors.DarkPurple);

    // ── Text Brushes ──
    public static readonly ISolidColorBrush GoldText = Brush(BalatroColors.BrightGold);
    public static readonly ISolidColorBrush GreenText = Brush(BalatroColors.BrightGreen);

    // ── Modal / Panel Brushes ──
    public static readonly ISolidColorBrush ModalBackground = Brush(BalatroColors.Grey);
    public static readonly ISolidColorBrush ModalInnerPanel = Brush(BalatroColors.DarkGrey);
    public static readonly ISolidColorBrush ModalBorder = Brush(BalatroColors.BrightSilver);
    public static readonly ISolidColorBrush DarkBackground = Brush(BalatroColors.DarkGrey);
    public static readonly ISolidColorBrush MediumGrey = Brush(BalatroColors.MediumGrey);
    public static readonly ISolidColorBrush LightGrey = Brush(BalatroColors.LightGrey);
    public static readonly ISolidColorBrush FadedGrey = Brush(BalatroColors.FadedGrey);

    // ── Shadow Brushes ──
    public static readonly ISolidColorBrush ButtonShadow = Brush(BalatroColors.MediumShadow);
    public static readonly ISolidColorBrush DarkShadow = Brush(BalatroColors.DarkShadow);

    // ── Disabled Brushes ──
    public static readonly ISolidColorBrush DisabledFace = Brush(BalatroColors.LightDullWashGrey);
    public static readonly ISolidColorBrush DisabledText = Brush(BalatroColors.LightDullGrey);

    // ── Overlay Brushes ──
    public static readonly ISolidColorBrush SemiTransparentBlack = Brush(BalatroColors.SemiTransparentBlack);
    public static readonly ISolidColorBrush SemiTransparentDark = Brush(BalatroColors.SemiTransparentDark);

    private static ImmutableSolidColorBrush Brush(Color c) => new(c);
}

/// <summary>
/// Balatro font and sizing constants.
/// </summary>
public static class BalatroFonts
{
    public const string FontFamilyUri = "avares://BalatroSeedOracle/m6x11plusplus.otf#m6x11plusplus";
    public static readonly FontFamily Primary = new(FontFamilyUri);

    public const double SizeSmall = 12;
    public const double SizeNormal = 14;
    public const double SizeLarge = 16;
    public const double SizeTitle = 24;
    public const double SizeHuge = 72;
}
