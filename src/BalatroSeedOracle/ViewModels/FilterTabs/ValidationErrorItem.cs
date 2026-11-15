using Avalonia.Media;
using BalatroSeedOracle.Constants;

namespace BalatroSeedOracle.ViewModels.FilterTabs
{
    /// <summary>
    /// Represents a validation error or warning in the YAML editor
    /// </summary>
    public class ValidationErrorItem
    {
        public int LineNumber { get; set; }
        public int Column { get; set; }
        public string Message { get; set; } = "";
        public ErrorSeverity Severity { get; set; }

        public string SeverityIcon =>
            Severity switch
            {
                ErrorSeverity.Error => "✗",
                ErrorSeverity.Warning => "⚠",
                ErrorSeverity.Info => "ℹ",
                _ => "•",
            };

        public IBrush SeverityColor =>
            Severity switch
            {
                ErrorSeverity.Error => UIColors.StatusBad,
                ErrorSeverity.Warning => UIColors.StatusWarn,
                ErrorSeverity.Info => Brushes.Blue,
                _ => UIColors.StatusIdle,
            };

        public enum ErrorSeverity
        {
            Error,
            Warning,
            Info,
        }
    }
}
