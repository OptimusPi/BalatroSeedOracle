namespace BalatroSeedOracle.Constants
{
    /// <summary>
    /// Color constants matching App.axaml StaticResource definitions.
    /// Use these instead of magic color strings to maintain consistency.
    /// </summary>
    public static class UIColors
    {
        // Primary Colors
        public const string Red = "#ff4c40";
        public const string RedHover = "#a02721";
        public const string Blue = "#0093ff";
        public const string BlueHover = "#0057a1";
        public const string Orange = "#ff9800";
        public const string OrangeHover = "#a05b00";
        public const string Gold = "#eac058";
        public const string GoldText = "#f5b244";

        // Grayscale/Teal (Balatro theme)
        public const string White = "#FFFFFF";
        public const string Black = "#000000";
        public const string VeryLightGrey = "#D8D8D8";
        public const string LightTextGrey = "#bfc7d5";
        public const string LightGrey = "#708386";
        public const string TealGrey = "#5f8a8d";
        public const string ModalGrey = "#3a5055";
        public const string MediumGrey = "#33464b";
        public const string DarkGrey = "#2e3f42";
        public const string DarkTealGrey = "#1e2b2d";  // Also used as VeryDarkGrey

        // Stats/Status Colors (semantic aliases)
        public const string MustHaveColor = Red;      // Red for required items
        public const string ShouldHaveColor = Blue;   // Blue for optional items
        public const string BannedColor = Orange;     // Orange for banned items
        public const string InfoColor = Gold;          // Gold for info/stats
    }
}
