using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AraonMC.Models;

namespace AraonMC.Services.Impl;

/// <summary>
/// Stub <see cref="IInstanceRepository"/>. Returns hardcoded mock instances so the UI is populated.
/// Create / Save / Delete throw <see cref="NotImplementedException"/> — real backend pending.
/// </summary>
public sealed class StubInstanceRepository : IInstanceRepository
{
    private static readonly GameInstance[] Instances =
    [
        new()
        {
            Id = "vanilla-1.21.4",
            Name = "Vanilla 1.21.4",
            MinecraftVersion = "1.21.4",
            Loader = LoaderType.Vanilla,
            LoaderVersion = "",
            CoverKey = "V",
            Group = "Vanilla",
            LastPlayed = DateTimeOffset.Now - TimeSpan.FromHours(3),
            PlayTime = TimeSpan.FromHours(12.5),
        },
        new()
        {
            Id = "atm10",
            Name = "All The Mods 10",
            MinecraftVersion = "1.21.4",
            Loader = LoaderType.NeoForge,
            LoaderVersion = "21.4.30",
            CoverKey = "A",
            Group = "Modpacks",
            LastPlayed = DateTimeOffset.Now - TimeSpan.FromDays(2),
            PlayTime = TimeSpan.FromHours(64),
        },
        new()
        {
            Id = "cobblemon",
            Name = "Cobblemon",
            MinecraftVersion = "1.21.1",
            Loader = LoaderType.Fabric,
            LoaderVersion = "0.16.10",
            CoverKey = "C",
            Group = "Modpacks",
            LastPlayed = DateTimeOffset.Now - TimeSpan.FromDays(8),
            PlayTime = TimeSpan.FromHours(31),
        },
        new()
        {
            Id = "speedrun-1.16.5",
            Name = "Speedrun 1.16.5",
            MinecraftVersion = "1.16.5",
            Loader = LoaderType.Fabric,
            LoaderVersion = "0.11.7",
            CoverKey = "R",
            Group = "Vanilla",
            LastPlayed = DateTimeOffset.Now - TimeSpan.FromDays(40),
            PlayTime = TimeSpan.FromHours(120),
        },
    ];

    public IReadOnlyList<GameInstance> GetAll() => Instances;

    public Task<GameInstance> CreateAsync(string name, MinecraftVersion version, LoaderType loader, CancellationToken ct = default) =>
        throw new NotImplementedException("Instance creation backend is not implemented yet.");

    public Task SaveAsync(GameInstance instance, CancellationToken ct = default) =>
        throw new NotImplementedException("Instance persistence backend is not implemented yet.");

    public Task DeleteAsync(GameInstance instance, CancellationToken ct = default) =>
        throw new NotImplementedException("Instance deletion backend is not implemented yet.");
}
