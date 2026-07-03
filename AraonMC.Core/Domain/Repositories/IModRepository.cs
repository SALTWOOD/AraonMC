using AraonMC.Core.Domain.Entities;

namespace AraonMC.Core.Domain.Repositories;

/// <summary>
/// Remote mod / modpack search source (repository contract). Real network backend
/// is not implemented yet — see the Stub implementation in Infrastructure.
/// </summary>
public interface IModRepository
{
    Task<IReadOnlyList<ModInfo>> SearchAsync(string query, CancellationToken ct = default);
}
