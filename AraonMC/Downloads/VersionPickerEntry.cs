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

using AraonMC.Core.Domain.Entities;

namespace AraonMC.Downloads;

/// <summary>Entry in the version picker ComboBox — either a group header or a clickable version item.</summary>
public sealed record VersionPickerEntry
{
    /// <summary>Display text shown to the user (group name or version id).</summary>
    public string Label { get; init; } = string.Empty;

    /// <summary>The Minecraft version string (e.g. "1.20.1"), null for group headers.</summary>
    public string? Version { get; init; }

    /// <summary>Version type for the badge color (Release, Snapshot, OldBeta, OldAlpha).</summary>
    public VersionType Type { get; init; }

    /// <summary>True if this entry is a non-selectable group header.</summary>
    public bool IsGroup { get; init; }

    /// <summary>Release date, used for sorting.</summary>
    public DateTimeOffset ReleaseTime { get; init; }

    /// <summary>Short label for the version type badge.</summary>
    public string TypeLabel => Type switch
    {
        VersionType.Release => "release",
        VersionType.Snapshot => "snapshot",
        VersionType.OldBeta => "beta",
        VersionType.OldAlpha => "alpha",
        _ => "release",
    };
}
