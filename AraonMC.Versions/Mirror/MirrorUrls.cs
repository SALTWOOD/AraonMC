using AraonMC.Core.Domain.Enums;

namespace AraonMC.Versions.Mirror;

/// <summary>各镜像源的 URL 策略（纯函数，无状态）。</summary>
public static class MirrorUrls
{
    public const string BmclapiHost = "bmclapi2.bangbang93.com";

    public static Uri ManifestUrl(DownloadMirror mirror) => mirror switch
    {
        DownloadMirror.Official => new("https://piston-meta.mojang.com/mc/game/version_manifest_v2.json"),
        DownloadMirror.Bmclapi => new($"https://{BmclapiHost}/mc/game/version_manifest_v2.json"),
        _ => throw new ArgumentOutOfRangeException(nameof(mirror)),
    };

    /// <summary>把 official url 改写到目标镜像；Official 原样返回。未知 host 不冒险改写。</summary>
    public static Uri Rewrite(DownloadMirror mirror, Uri official)
    {
        if (mirror == DownloadMirror.Official || !official.IsAbsoluteUri) return official;

        var host = official.Host;
        var path = official.AbsolutePath;

        // assets 资源站：BMCLAPI 在路径前加 /assets。
        if (host == "resources.download.minecraft.net")
            return new($"https://{BmclapiHost}/assets{path}");

        // 已知 Mojang/官方 host：直接换 host。
        if (IsKnownMojangHost(host))
            return new($"https://{BmclapiHost}{path}");

        return official;
    }

    /// <summary>构造 asset object 的 url（hash 前 2 位为子目录）。</summary>
    public static Uri AssetUrl(DownloadMirror mirror, string hash)
    {
        if (hash.Length < 2) throw new ArgumentException("hash too short", nameof(hash));
        var prefix = hash[..2];
        return mirror switch
        {
            DownloadMirror.Official => new($"https://resources.download.minecraft.net/{prefix}/{hash}"),
            DownloadMirror.Bmclapi => new($"https://{BmclapiHost}/assets/{prefix}/{hash}"),
            _ => throw new ArgumentOutOfRangeException(nameof(mirror)),
        };
    }

    private static bool IsKnownMojangHost(string host) =>
        host is "piston-meta.mojang.com"
            or "piston-data.mojang.com"
            or "launchermeta.mojang.com"
            or "launcher.mojang.com"
            or "libraries.minecraft.net";
}
