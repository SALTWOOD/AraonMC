using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace AraonMC.Converters;

/// <summary>Formats a byte count as a compact human-readable size ("1.2 MB", "340 KB", "512 B").</summary>
public sealed class FileSizeConverter : IValueConverter
{
    public static readonly FileSizeConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not long bytes || bytes <= 0) return "—";
        return bytes switch
        {
            >= 1L << 20 => (bytes / (double)(1L << 20)).ToString("0.#", CultureInfo.InvariantCulture) + " MB",
            >= 1L << 10 => (bytes / (double)(1L << 10)).ToString("0.#", CultureInfo.InvariantCulture) + " KB",
            _ => bytes.ToString("0 B", CultureInfo.InvariantCulture),
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
