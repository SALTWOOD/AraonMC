using AraonMC.Core.Domain.Entities;
using AraonMC.Core.Domain.Enums;

namespace AraonMC.Core.Domain.Repositories;

/// <summary>
/// Local game-instance store (repository contract). Real persistence backend
/// is not implemented yet — see the Stub implementation in Infrastructure.
/// </summary>
public interface IInstanceRepository
{
    IReadOnlyList<GameInstance> GetAll();

    Task<GameInstance> CreateAsync(string name, MinecraftVersion version, LoaderType loader, CancellationToken ct = default);
    Task SaveAsync(GameInstance instance, CancellationToken ct = default);
    Task DeleteAsync(GameInstance instance, CancellationToken ct = default);
}
