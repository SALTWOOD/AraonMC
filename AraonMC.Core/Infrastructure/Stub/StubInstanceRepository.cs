// Copyright (C) 2026 SALTWOOD and contributors
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

using AraonMC.Core.Domain.Entities;
using AraonMC.Core.Domain.Enums;
using AraonMC.Core.Domain.Repositories;

namespace AraonMC.Core.Infrastructure.Stub;

/// <summary>
/// Stub <see cref="IInstanceRepository"/>. Returns hardcoded mock instances so the UI is populated.
/// Create / Save / Rename / Delete throw <see cref="NotImplementedException"/> — real backend pending.
/// </summary>
public sealed class StubInstanceRepository : IInstanceRepository
{
    private static readonly GameInstance[] Instances =
    [
        new()
        {
            Id = "vanilla-1.21.4",
            MinecraftVersion = "Vanilla 1.21.4",   // instance name / versions folder name
            BaseMinecraftVersion = "1.21.4",       // real Mojang version
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
            MinecraftVersion = "All The Mods 10",
            BaseMinecraftVersion = "1.21.4",
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
            MinecraftVersion = "Cobblemon",
            BaseMinecraftVersion = "1.21.1",
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
            MinecraftVersion = "Speedrun 1.16.5",
            BaseMinecraftVersion = "1.16.5",
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

    public Task RenameAsync(GameInstance instance, string newName, CancellationToken ct = default) =>
        throw new NotImplementedException("Instance rename backend is not implemented yet.");

    public Task DeleteAsync(GameInstance instance, CancellationToken ct = default) =>
        throw new NotImplementedException("Instance deletion backend is not implemented yet.");
}
