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

namespace AraonMC.LaunchArgs.Version;

/// <summary>对应 <c>versions/&lt;id&gt;/&lt;id&gt;.json</c>（client.json），表示合并继承链后的最终版本。</summary>
public sealed class VersionMetadata
{
    public string Id { get; set; } = string.Empty;
    public string MainClass { get; set; } = string.Empty;
    public string Type { get; set; } = "release";
    public string AssetsIndexName { get; set; } = string.Empty;
    public VersionAssetIndex? AssetIndex { get; set; }

    /// <summary>旧式（&lt;1.6）资产键名。</summary>
    public string? Assets { get; set; }

    public JavaVersion? JavaVersion { get; set; }
    public int MinimumLauncherVersion { get; set; }
    public IReadOnlyList<VersionArgumentEntry> JvmArguments { get; set; } = [];
    public IReadOnlyList<VersionArgumentEntry> GameArguments { get; set; } = [];

    /// <summary>旧格式（≤1.12.2）的整串游戏参数。</summary>
    public string? LegacyMinecraftArguments { get; set; }

    public IReadOnlyList<VersionLibrary> Libraries { get; set; } = [];
    public VersionLogging? Logging { get; set; }

    /// <summary>顶层 downloads（client/server 制品）。</summary>
    public VersionDownloads? Downloads { get; set; }

    /// <summary>父版本 id（模组版本继承原版）；合并后为 null。</summary>
    public string? InheritsFrom { get; set; }
}

public sealed class VersionAssetIndex
{
    public string Id { get; set; } = string.Empty;
    public string Sha1 { get; set; } = string.Empty;
    public long Size { get; set; }
    public long TotalSize { get; set; }
    public string Url { get; set; } = string.Empty;
}

public sealed class JavaVersion
{
    public string Component { get; set; } = string.Empty;
    public int MajorVersion { get; set; }
}

public sealed class VersionLogging
{
    public VersionLoggingFile? Client { get; set; }
}

public sealed class VersionLoggingFile
{
    public string Id { get; set; } = string.Empty;
    public string Sha1 { get; set; } = string.Empty;
    public long Size { get; set; }
    public string Url { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;

    /// <summary>JVM 参数模板，如 <c>-Dlog4j.configurationFile=${path}</c>。</summary>
    public string? Argument { get; set; }
}

public sealed class VersionDownloads
{
    public VersionArtifact? Client { get; set; }
}
