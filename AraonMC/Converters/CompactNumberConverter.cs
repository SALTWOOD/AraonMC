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
using Avalonia.Data.Converters;

namespace AraonMC.Converters;

/// <summary>Formats large download counts compactly: 1.2K / 3.4M / 5.6B.</summary>
public sealed class CompactNumberConverter : IValueConverter
{
    public static readonly CompactNumberConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not long n) return value?.ToString();
        return n switch
        {
            >= 1_000_000_000 => (n / 1_000_000_000.0).ToString("0.#B", CultureInfo.InvariantCulture) + " downloads",
            >= 1_000_000 => (n / 1_000_000.0).ToString("0.#M", CultureInfo.InvariantCulture) + " downloads",
            >= 1_000 => (n / 1_000.0).ToString("0.#K", CultureInfo.InvariantCulture) + " downloads",
            _ => n.ToString("0", CultureInfo.InvariantCulture) + " downloads",
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
