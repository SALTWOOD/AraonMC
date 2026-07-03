using AraonMC.Core.Domain.Entities;

namespace AraonMC.Core.Application.Ports;

/// <summary>
/// Launches a game instance (application port). Backend not implemented yet —
/// see the Stub implementation in Infrastructure.
/// </summary>
public interface IGameLauncher
{
    Task LaunchAsync(GameInstance instance, MinecraftAccount account, CancellationToken ct = default);
}
