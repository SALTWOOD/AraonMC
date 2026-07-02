using System.Threading;
using System.Threading.Tasks;
using AraonMC.Models;

namespace AraonMC.Services;

/// <summary>
/// Launches a game instance. Backend not implemented — see <c>Impl.StubGameLauncher</c>.
/// </summary>
public interface IGameLauncher
{
    Task LaunchAsync(GameInstance instance, MinecraftAccount account, CancellationToken ct = default);
}
