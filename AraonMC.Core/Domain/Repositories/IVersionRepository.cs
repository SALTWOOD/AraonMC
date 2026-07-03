using AraonMC.Core.Domain.Entities;

namespace AraonMC.Core.Domain.Repositories;

/// <summary>
/// Remote Minecraft version manifest source (repository contract). Real network
/// backend is not implemented yet — see the Stub implementation in Infrastructure.
/// </summary>
public interface IVersionRepository
{
    Task<IReadOnlyList<MinecraftVersion>> GetAvailableAsync(CancellationToken ct = default);
}
