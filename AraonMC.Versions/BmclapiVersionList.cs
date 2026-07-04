using AraonMC.Core.Domain.Enums;
using AraonMC.Versions.Mirror;

namespace AraonMC.Versions;

/// <summary>BMCLAPI 国内镜像源（bmclapi2.bangbang93.com）。</summary>
public sealed class BmclapiVersionList : HttpVersionList
{
    public BmclapiVersionList(HttpClient http) : base(DownloadMirror.Bmclapi, http) { }
}
