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
using System.Text.Json.Serialization;

namespace AraonMC.Core.Domain.Entities;

/// <summary>
/// A locally installed (or installable) game instance / profile.
/// The user-facing instance name is bound to the Minecraft <c>versions/&lt;name&gt;/</c> folder name.
/// </summary>
public sealed class GameInstance
{
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// User-facing instance name and the directory name under <c>.minecraft/versions</c>.
    /// Renaming an instance must rename this directory and its <c>.jar</c>/<c>.json</c> files.
    /// </summary>
    public string MinecraftVersion { get; set; } = string.Empty;

    /// <summary>The base Mojang version to install/download (e.g. 1.20.1). Distinct from the custom instance name.</summary>
    public string BaseMinecraftVersion { get; set; } = string.Empty;

    /// <summary>Display name derived from <see cref="MinecraftVersion"/>; not independently persisted.</summary>
    [JsonIgnore]
    public string Name => MinecraftVersion;

    /// <summary>The actual Mojang version shown in UI (e.g. 1.20.1), distinct from the custom instance name.</summary>
    [JsonIgnore]
    public string DisplayVersion => string.IsNullOrWhiteSpace(BaseMinecraftVersion) ? MinecraftVersion : BaseMinecraftVersion;

    public LoaderType Loader { get; set; } = LoaderType.Vanilla;
    public string LoaderVersion { get; set; } = string.Empty;

    /// <summary>共享的 .minecraft 游戏根目录绝对路径（实例只是「版本 + 设置」的引用，不拥有独立目录）。</summary>
    public string Path { get; set; } = string.Empty;

    public string? CoverKey { get; set; }
    public string? Group { get; set; }
    public DateTimeOffset LastPlayed { get; set; }
    public TimeSpan PlayTime { get; set; }

    public string DisplayLoader =>
        Loader == LoaderType.Vanilla ? "Vanilla" : $"{Loader} {LoaderVersion}";
}
