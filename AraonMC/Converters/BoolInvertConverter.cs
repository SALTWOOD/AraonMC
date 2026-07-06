using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace AraonMC.Converters;

public sealed class BoolInvertConverter : IValueConverter
{
    public static readonly BoolInvertConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool b ? !b : value;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool b ? !b : value;
    }
}
