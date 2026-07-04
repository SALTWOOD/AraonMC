using AraonMC.Core.Domain.Enums;
using AraonMC.Versions.Mirror;

namespace AraonMC.Versions;

/// <summary>Mojang 官方源（piston-meta.mojang.com）。</summary>
public sealed class OfficialVersionList : HttpVersionList
{
    public OfficialVersionList(HttpClient http) : base(DownloadMirror.Official, http) { }
}
