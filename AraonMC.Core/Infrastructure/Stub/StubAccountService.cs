using AraonMC.Core.Application.Ports;
using AraonMC.Core.Domain.Entities;

namespace AraonMC.Core.Infrastructure.Stub;

/// <summary>
/// Stub <see cref="IAccountService"/>. Returns hardcoded mock accounts so the UI is populated.
/// Login / add / remove throw <see cref="NotImplementedException"/> — real backend pending.
/// </summary>
public sealed class StubAccountService : IAccountService
{
    private readonly List<MinecraftAccount> _accounts =
    [
        new()
        {
            Id = "ms-0001",
            Username = "SaltWood_233",
            Uuid = "00000000-0000-1000-8000-000000000001",
            IsOnline = true,
            IsActive = true,
            AvatarKey = "S",
        },
        new()
        {
            Id = "off-0002",
            Username = "Steve",
            Uuid = "00000000-0000-1000-8000-000000000002",
            IsOnline = false,
            IsActive = false,
            AvatarKey = "St",
        },
    ];

    public IReadOnlyList<MinecraftAccount> GetAccounts() => _accounts;

    public MinecraftAccount? GetActive() => _accounts.FirstOrDefault(a => a.IsActive);

    public Task<MinecraftAccount> LoginMicrosoftAsync(CancellationToken ct = default) =>
        throw new NotImplementedException("Microsoft login backend is not implemented yet.");

    public Task<MinecraftAccount> AddOfflineAsync(string username, CancellationToken ct = default) =>
        throw new NotImplementedException("Offline account creation backend is not implemented yet.");

    public Task SetActiveAsync(MinecraftAccount account, CancellationToken ct = default) =>
        throw new NotImplementedException("Account switching backend is not implemented yet.");

    public Task RemoveAsync(MinecraftAccount account, CancellationToken ct = default) =>
        throw new NotImplementedException("Account removal backend is not implemented yet.");
}
