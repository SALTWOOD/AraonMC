using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AraonMC.Models;

namespace AraonMC.Services;

/// <summary>
/// Local game-instance store. Backend not implemented — see <c>Impl.StubInstanceRepository</c>.
/// </summary>
public interface IInstanceRepository
{
    IReadOnlyList<GameInstance> GetAll();

    Task<GameInstance> CreateAsync(string name, MinecraftVersion version, LoaderType loader, CancellationToken ct = default);
    Task SaveAsync(GameInstance instance, CancellationToken ct = default);
    Task DeleteAsync(GameInstance instance, CancellationToken ct = default);
}
