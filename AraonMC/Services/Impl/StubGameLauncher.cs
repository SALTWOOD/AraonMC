using System;
using System.Threading;
using System.Threading.Tasks;
using AraonMC.Models;

namespace AraonMC.Services.Impl;

/// <summary>
/// Stub <see cref="IGameLauncher"/>. Launch is not implemented yet.
/// </summary>
public sealed class StubGameLauncher : IGameLauncher
{
    public Task LaunchAsync(GameInstance instance, MinecraftAccount account, CancellationToken ct = default) =>
        throw new NotImplementedException("Game launch backend is not implemented yet.");
}
