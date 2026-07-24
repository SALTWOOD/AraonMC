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

using AraonMC.Core.Domain.Enums;

namespace AraonMC.Core.Domain.Entities;

/// <summary>
/// A single browsable content entry (mod, modpack, resource pack, shader pack, world save, or datapack)
/// shown on the Browse page. Aggregated from one or more remote catalogs (<see cref="Source"/>).
/// </summary>
public sealed class ResourceInfo
{
    /// <summary>Platform-internal id of the project (Modrinth project id or CurseForge mod id), as a string.</summary>
    public string Id { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;

    /// <summary>A short human-readable category label (e.g. "Performance", "World Gen &amp; Mobs").</summary>
    public string Category { get; set; } = string.Empty;

    public long Downloads { get; set; }

    /// <summary>2-letter initials derived from <see cref="Name"/>; shown when <see cref="IconUrl"/> fails to load.</summary>
    public string IconKey { get; set; } = string.Empty;

    /// <summary>Remote icon URL (project logo). Empty when the project has no icon.</summary>
    public string IconUrl { get; set; } = string.Empty;

    /// <summary>Web URL of the project page on its origin platform.</summary>
    public string PageUrl { get; set; } = string.Empty;

    public DateTimeOffset? UpdatedAt { get; set; }

    public ResourceType Type { get; set; }

    /// <summary>Which catalog this entry came from.</summary>
    public ResourceSource Source { get; set; }
}
