using AraonMC.Core.Domain.Entities;

namespace AraonMC.Versions;

/// <summary>版本清单源（可下载版本列表）。</summary>
public interface IVersionList
{
    /// <summary>获取全部可用版本。</summary>
    Task<IReadOnlyList<MinecraftVersion>> GetVersionsAsync(CancellationToken ct = default);

    /// <summary>解析单个版本的 client.json 来源（供下载器使用）。</summary>
    Task<VersionManifest.Entry?> ResolveAsync(string versionId, CancellationToken ct = default);
}
