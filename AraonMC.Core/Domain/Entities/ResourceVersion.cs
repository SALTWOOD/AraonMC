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

namespace AraonMC.Core.Domain.Entities;

/// <summary>
/// One downloadable version of a <see cref="ResourceInfo"/> (a mod/modpack/etc.), as listed by Modrinth or
/// CurseForge. A single version often targets multiple game versions (and loaders); the version-select
/// window groups these by game version.
/// </summary>
public sealed class ResourceVersion
{
    public string Id { get; set; } = string.Empty;

    /// <summary>Display label, e.g. "Sodium 0.5.3" or a CurseForge file displayName.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>release / beta / alpha (normalized from each platform's enum).</summary>
    public string ReleaseType { get; set; } = "release";

    public DateTimeOffset? PublishedAt { get; set; }

    public long Downloads { get; set; }

    /// <summary>Direct CDN URL of the version's primary file.</summary>
    public string DownloadUrl { get; set; } = string.Empty;

    /// <summary>Suggested filename for the file.</summary>
    public string FileName { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    /// <summary>Game versions this file supports (e.g. ["1.20.1", "1.20.2"]).</summary>
    public IReadOnlyList<string> GameVersions { get; set; } = Array.Empty<string>();

    /// <summary>Loaders this file supports (e.g. ["fabric"]); may be empty for CurseForge.</summary>
    public IReadOnlyList<string> Loaders { get; set; } = Array.Empty<string>();
}
