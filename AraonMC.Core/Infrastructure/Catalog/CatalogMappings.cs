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
using System.Collections.Generic;
using AraonMC.Core.Domain.Enums;
using AraonMC.Core.Domain.Repositories;

namespace AraonMC.Core.Infrastructure.Catalog;

/// <summary>
/// Static mappings between the launcher's <see cref="ResourceType"/> / <see cref="LoaderType"/> /
/// <see cref="ResourceSort"/> enums and each platform's native notion of project type, loader, and sort.
/// A <c>null</c> result means the platform does not carry that type/loader — clients return no results.
/// </summary>
internal static class CatalogMappings
{
    // ResourceType → Modrinth project_type. WorldSave is not a Modrinth project type.
    public static string? ModrinthProjectType(ResourceType t) => t switch
    {
        ResourceType.Mod => "mod",
        ResourceType.Modpack => "modpack",
        ResourceType.ResourcePack => "resourcepack",
        ResourceType.ShaderPack => "shader",
        ResourceType.DataPack => "datapack",
        _ => null,
    };

    // ResourceType → CurseForge classId (verified: Mods 6, Modpacks 4471, Resource Packs 12,
    // Shader Packs 6552, Worlds 17). DataPack has no verified top-level class on CurseForge.
    public static int? CurseForgeClassId(ResourceType t) => t switch
    {
        ResourceType.Mod => 6,
        ResourceType.Modpack => 4471,
        ResourceType.ResourcePack => 12,
        ResourceType.ShaderPack => 6552,
        ResourceType.WorldSave => 17,
        _ => null,
    };

    // LoaderType → Modrinth loader category. Vanilla has no loader category.
    public static string? ModrinthLoaderCategory(LoaderType l) => l switch
    {
        LoaderType.Fabric => "fabric",
        LoaderType.Forge => "forge",
        LoaderType.NeoForge => "neoforge",
        LoaderType.Quilt => "quilt",
        _ => null,
    };

    // LoaderType → CurseForge modLoaderType enum. CurseForge requires this to be paired with a gameVersion.
    public static int? CurseForgeLoaderType(LoaderType l) => l switch
    {
        LoaderType.Fabric => 4,
        LoaderType.Forge => 1,
        LoaderType.NeoForge => 6,
        LoaderType.Quilt => 5,
        _ => null,
    };

    // ResourceSort → Modrinth search "index".
    public static string ModrinthIndex(ResourceSort s) => s switch
    {
        ResourceSort.Downloads => "downloads",
        ResourceSort.Updated => "updated",
        ResourceSort.Newest => "newest",
        _ => "relevance",
    };

    // ResourceSort → CurseForge sortField enum (2 Popularity, 6 TotalDownloads, 3 LastUpdated).
    // CurseForge exposes no "created" sort; Newest maps to LastUpdated as the closest equivalent.
    public static int CurseForgeSortField(ResourceSort s) => s switch
    {
        ResourceSort.Downloads => 6,
        ResourceSort.Updated => 3,
        ResourceSort.Newest => 3,
        _ => 2,
    };

    /// <summary>A 1–2 character uppercase initials string derived from a project name, for the icon fallback.</summary>
    public static string Initials(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "?";
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1)
        {
            var w = parts[0];
            return (w.Length >= 2 ? w[..2] : w).ToUpperInvariant();
        }
        return (parts[0][..1] + parts[1][..1]).ToUpperInvariant();
    }
}
