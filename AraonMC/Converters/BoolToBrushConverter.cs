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
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Styling;

namespace AraonMC.Converters;

/// <summary>
/// Picks a brush by resource key from a boolean value. Use <c>ConverterParameter="TrueKey|FalseKey"</c>.
/// This avoids trying to assign DynamicResource bindings to plain CLR properties on the converter.
/// </summary>
public sealed class BoolToBrushConverter : IValueConverter
{
    public static readonly BoolToBrushConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (parameter is not string p) return null;
        var parts = p.Split('|', 2);
        var key = value is true ? parts[0] : parts.Length > 1 ? parts[1] : parts[0];
        if (Application.Current?.TryGetResource(key, ThemeVariant.Default, out var resource) == true
            && resource is IBrush brush)
            return brush;
        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
