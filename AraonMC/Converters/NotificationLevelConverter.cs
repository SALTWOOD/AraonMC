using System;
using System.Globalization;
using AraonMC.Core.Application.Notifications;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace AraonMC.Converters;

/// <summary>
/// Maps a <see cref="NotificationLevel"/> to its accent brush, or — when passed the
/// converter parameter <c>"Glyph"</c> — to a single-character icon glyph.
/// </summary>
public sealed class NotificationLevelConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not NotificationLevel level)
        {
            return null;
        }

        if (parameter is string p && p.Equals("Glyph", StringComparison.OrdinalIgnoreCase))
        {
            return level switch
            {
                NotificationLevel.Info => "i",
                NotificationLevel.Success => "✓",
                NotificationLevel.Warning => "!",
                NotificationLevel.Error => "×",
                _ => "i",
            };
        }

        return level switch
        {
            NotificationLevel.Info => Brush("ColorBrushInfo", Brushes.RoyalBlue),
            NotificationLevel.Success => Brush("ColorBrushSuccess", Brushes.LimeGreen),
            NotificationLevel.Warning => Brush("ColorBrushWarning", Brushes.Orange),
            NotificationLevel.Error => Brush("ColorBrushError", Brushes.IndianRed),
            _ => Brushes.SlateGray,
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();

    private static IBrush Brush(string key, IBrush fallback) =>
        Application.Current!.TryFindResource(key, out var b) && b is IBrush brush ? brush : fallback;
}
