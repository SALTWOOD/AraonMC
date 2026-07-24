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
using AraonMC.Core.Domain.Enums;
using Avalonia.Data.Converters;

namespace AraonMC.Converters;

/// <summary>Maps a <see cref="ResourceSource"/> to a short badge label ("Modrinth" / "CurseForge").</summary>
public sealed class ResourceSourceLabelConverter : IValueConverter
{
    public static readonly ResourceSourceLabelConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not ResourceSource source) return null;
        return source switch
        {
            ResourceSource.Modrinth => "Modrinth",
            ResourceSource.CurseForge => "CurseForge",
            _ => source.ToString(),
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
