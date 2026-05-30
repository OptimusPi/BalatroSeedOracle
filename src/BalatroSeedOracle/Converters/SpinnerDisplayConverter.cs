using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;

namespace BalatroSeedOracle.Converters
{
    /// <summary>
    /// Produces the badge text for a SpinnerControl from [Value, DisplayValues, AllowAuto].
    /// This is the MVVM home for that view-side transform — it replaces the old imperative
    /// ValueText.Text assignments in the control's code-behind, so the badge can never silently
    /// desync from state.
    /// </summary>
    public class SpinnerDisplayConverter : IMultiValueConverter
    {
        public static readonly SpinnerDisplayConverter Instance = new();

        public object Convert(
            IList<object?> values,
            Type targetType,
            object? parameter,
            CultureInfo culture
        )
        {
            int value = values.Count > 0 && values[0] is int v ? v : 0;
            string[]? displayValues = values.Count > 1 ? values[1] as string[] : null;
            bool allowAuto = values.Count > 2 && values[2] is bool b && b;

            // -1 is the "Auto" sentinel so that 0 stays a valid explicit value.
            if (allowAuto && value < 0)
            {
                return "Auto";
            }

            if (displayValues != null && displayValues.Length > 0)
            {
                var index = Math.Max(0, Math.Min(value, displayValues.Length - 1));
                return displayValues[index];
            }

            return value.ToString(CultureInfo.InvariantCulture);
        }
    }
}
