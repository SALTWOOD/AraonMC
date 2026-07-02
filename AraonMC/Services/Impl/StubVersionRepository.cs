using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AraonMC.Models;

namespace AraonMC.Services.Impl;

/// <summary>
/// Stub <see cref="IVersionRepository"/>. Returns a hardcoded sample version manifest.
/// </summary>
public sealed class StubVersionRepository : IVersionRepository
{
    private static readonly MinecraftVersion[] Versions =
    [
        New("1.21.4", VersionType.Release, 2024, 12, 3),
        New("1.21", VersionType.Release, 2024, 6, 13),
        New("1.20.6", VersionType.Release, 2024, 4, 29),
        New("1.20.4", VersionType.Release, 2023, 12, 7),
        New("1.20.1", VersionType.Release, 2023, 6, 12),
        New("1.19.2", VersionType.Release, 2022, 8, 5),
        New("1.18.2", VersionType.Release, 2022, 2, 28),
        New("1.16.5", VersionType.Release, 2021, 1, 15),
        New("24w14a", VersionType.Snapshot, 2024, 4, 3),
    ];

    public Task<IReadOnlyList<MinecraftVersion>> GetAvailableAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<MinecraftVersion>>(Versions);

    private static MinecraftVersion New(string id, VersionType type, int y, int m, int d) =>
        new()
        {
            Id = id,
            Type = type,
            ReleaseTime = new DateTimeOffset(y, m, d, 0, 0, 0, TimeSpan.Zero),
        };
}
