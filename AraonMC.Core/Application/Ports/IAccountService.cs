using AraonMC.Core.Domain.Entities;

namespace AraonMC.Core.Application.Ports;

/// <summary>
/// Account login / identity management (application port). Backend not implemented
/// yet — see the Stub implementation in Infrastructure.
/// </summary>
public interface IAccountService
{
    IReadOnlyList<MinecraftAccount> GetAccounts();
    MinecraftAccount? GetActive();

    Task<MinecraftAccount> LoginMicrosoftAsync(CancellationToken ct = default);
    Task<MinecraftAccount> AddOfflineAsync(string username, CancellationToken ct = default);
    Task SetActiveAsync(MinecraftAccount account, CancellationToken ct = default);
    Task RemoveAsync(MinecraftAccount account, CancellationToken ct = default);
}
