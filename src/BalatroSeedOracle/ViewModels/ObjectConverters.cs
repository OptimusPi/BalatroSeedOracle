using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace BalatroSeedOracle.ViewModels
{
    public static class ObjectConverters
    {
        public static readonly IValueConverter IsNotNull = new FuncValueConverter<object?, bool>(
            static x => x is not null
        );
    }
}
