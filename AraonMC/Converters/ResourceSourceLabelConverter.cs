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
