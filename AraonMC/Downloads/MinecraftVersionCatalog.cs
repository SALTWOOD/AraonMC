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
using MinecraftDownloader.Core.Abstractions;

namespace AraonMC.Downloads;

/// <summary>版本清单源：拉取可下载版本列表供选择页展示。</summary>
public interface IVersionList
{
    /// <summary>获取全部可用版本。</summary>
    Task<IReadOnlyList<MinecraftVersion>> GetVersionsAsync(CancellationToken ct = default);
}

/// <summary>
///   包装上游 <see cref="IManifestParser"/>：抓取 Mojang 官方版本清单，映射成
///   <see cref="MinecraftVersion"/>。安装时由上游 installer 内部按 versionId 自行解析，
///   故此处不再暴露 Resolve。
/// </summary>
public sealed class MinecraftVersionCatalog(IManifestParser parser) : IVersionList
{
    public async Task<IReadOnlyList<MinecraftVersion>> GetVersionsAsync(CancellationToken ct = default)
    {
        DebugLog.Info("Versions: fetching Mojang version manifest...");
        var index = await parser.GetVersionManifestIndexAsync(ct).ConfigureAwait(false);
        var latest = index.Latest;
        DebugLog.Info($"Versions: manifest received — {index.Versions.Count} version(s) total"
            + (latest is null ? "." : $", latest release='{latest.Release}', snapshot='{latest.Snapshot}'."));

        var mapped = index.Versions
            .Select(e => new MinecraftVersion
            {
                Id = e.Id,
                Type = ParseType(e.Type),
                ReleaseTime = e.ReleaseTime,
            })
            .ToList();

        var breakdown = string.Join(", ", mapped.GroupBy(v => v.Type).Select(g => $"{g.Key}={g.Count()}"));
        DebugLog.Info($"Versions: parsed {mapped.Count} entries" + (string.IsNullOrEmpty(breakdown) ? "." : $" ({breakdown})."));
        return mapped;
    }

    private static VersionType ParseType(string? t) => t switch
    {
        "release" => VersionType.Release,
        "snapshot" => VersionType.Snapshot,
        "old_beta" => VersionType.OldBeta,
        "old_alpha" => VersionType.OldAlpha,
        _ => VersionType.Release,
    };
}
