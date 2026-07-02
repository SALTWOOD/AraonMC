using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AraonMC.Models;

namespace AraonMC.Services;

/// <summary>
/// Remote Minecraft version manifest source. Backend not implemented — see <c>Impl.StubVersionRepository</c>.
/// </summary>
public interface IVersionRepository
{
    Task<IReadOnlyList<MinecraftVersion>> GetAvailableAsync(CancellationToken ct = default);
}
