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
        var index = await parser.GetVersionManifestIndexAsync(ct).ConfigureAwait(false);
        return index.Versions
            .Select(e => new MinecraftVersion
            {
                Id = e.Id,
                Type = ParseType(e.Type),
                ReleaseTime = e.ReleaseTime,
            })
            .ToList();
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
