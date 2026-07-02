using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AraonMC.Models;

namespace AraonMC.Services.Impl;

/// <summary>
/// Stub <see cref="IModRepository"/>. Returns hardcoded sample mods; ignores the query string.
/// </summary>
public sealed class StubModRepository : IModRepository
{
    private static readonly ModInfo[] Mods =
    [
        New("sodium", "Sodium", "jellysquid3", "Rendering engine rewrite for smooth FPS.", "Performance", 184_000_000, "So"),
        New("iris", "Iris Shaders", "coderbot", "Modern shader loader compatible with Sodium.", "Shaders", 72_000_000, "Ir"),
        New("jei", "Just Enough Items", "mezz", "View items, recipes, and usages.", "Utility", 260_000_000, "JE"),
        New("create", "Create", "simibubi", "Kinetic machinery and contraptions.", "Technology", 96_000_000, "Cr"),
        New("appleskin", "AppleSkin", "squeek502", "Hunger and saturation HUD overlays.", "Utility", 51_000_000, "Ap"),
        New("fabric-api", "Fabric API", "FabricMC", "Hooks and shared code for Fabric mods.", "Library", 210_000_000, "FA"),
        New("optifine", "OptiFine", "sp614x", "Performance, zoom, and shader support.", "Performance", 150_000_000, "OP"),
        New("replaymod", "Replay Mod", "johni0702", "Record and replay your gameplay from any angle.", "Utility", 28_000_000, "Re"),
    ];

    public Task<IReadOnlyList<ModInfo>> SearchAsync(string query, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<ModInfo>>(Mods);

    private static ModInfo New(string slug, string name, string author, string summary, string category, long downloads, string icon) =>
        new()
        {
            Slug = slug,
            Name = name,
            Author = author,
            Summary = summary,
            Category = category,
            Downloads = downloads,
            IconKey = icon,
        };
}
