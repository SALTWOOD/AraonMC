// Copyright (C) 2026 SALTWOOD and contributors
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

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
