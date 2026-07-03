using AraonMC.Core.Application.Ports;
using AraonMC.Core.Domain.Entities;

namespace AraonMC.Core.Infrastructure.Stub;

/// <summary>
/// Stub <see cref="IGameLauncher"/>. Launch is not implemented yet.
/// </summary>
public sealed class StubGameLauncher : IGameLauncher
{
    public Task LaunchAsync(GameInstance instance, MinecraftAccount account, CancellationToken ct = default) =>
        throw new NotImplementedException("Game launch backend is not implemented yet.");
}
