using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AraonMC.Models;

namespace AraonMC.Services;

/// <summary>
/// Account login / identity management. Backend not implemented — see <c>Impl.StubAccountService</c>.
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
