using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AraonMC.Core.Application.Notifications;
using AraonMC.Core.Domain.Entities;
using AraonMC.Core.Domain.Enums;
using AraonMC.Core.Domain.Repositories;

namespace AraonMC.Core.Infrastructure.Catalog;

/// <summary>
/// <see cref="IResourceRepository"/> that fans each query out to Modrinth and/or CurseForge, merges the
/// results, and resolves a single resource's primary file URL for the download manager to fetch. A search
/// failure from one source (e.g. an invalid CurseForge key) is swallowed so the other source's results
/// still show; a CurseForge failure raises a single one-shot warning toast. If every selected source
/// errors, the exception propagates so the view model can surface an error.
/// </summary>
public sealed class ResourceRepository : IResourceRepository
{
    private readonly ModrinthClient _modrinth;
    private readonly CurseForgeClient _curseForge;
    private readonly INotificationService? _notifications;
    private int _curseForgeErrorToastShown; // 0/1 latch so we don't toast on every search.

    public ResourceRepository(ModrinthClient modrinth, CurseForgeClient curseForge, INotificationService? notifications = null)
    {
        _modrinth = modrinth;
        _curseForge = curseForge;
        _notifications = notifications;
    }

    public bool IsCurseForgeConfigured => _curseForge.IsConfigured;

    public async Task<IReadOnlyList<ResourceInfo>> SearchAsync(ResourceSearchQuery query, CancellationToken ct = default)
    {
        var sources = query.Sources == ResourceSourceFilter.None ? ResourceSourceFilter.All : query.Sources;
        DebugLog.Info($"Search: type={query.Type} sources={sources} loader={(query.Loader?.ToString() ?? "any")} version={query.GameVersion ?? "any"} sort={query.Sort} text='{query.Text}'.");

        var tasks = new List<Task<(IReadOnlyList<ResourceInfo> Results, bool Ok)>>();
        if (sources.HasFlag(ResourceSourceFilter.Modrinth))
            tasks.Add(RunSafeAsync("Modrinth", () => _modrinth.SearchAsync(query, ct), isCurseForge: false));
        if (sources.HasFlag(ResourceSourceFilter.CurseForge))
            tasks.Add(RunSafeAsync("CurseForge", () => _curseForge.SearchAsync(query, ct), isCurseForge: true));

        if (tasks.Count == 0) return Array.Empty<ResourceInfo>();

        var outcomes = await Task.WhenAll(tasks).ConfigureAwait(false);

        // No selected source returned at all (all errored) — surface it so the UI can toast.
        if (!outcomes.Any(o => o.Ok))
            throw new InvalidOperationException("Resource search failed for all selected sources (network or configuration error).");

        var all = outcomes.SelectMany(o => o.Results).ToList();
        var merged = Merge(all, query.Sort, query.Limit);
        DebugLog.Info($"Search: merged {merged.Count} result(s) from {outcomes.Count(o => o.Ok)} source(s) (total: {query.TotalCount}).");
        return merged;
    }

    public async Task<IReadOnlyList<ResourceVersion>> GetVersionsAsync(ResourceInfo resource, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(resource);

        IReadOnlyList<ResourceVersion> raw;
        if (resource.Source == ResourceSource.CurseForge)
        {
            raw = int.TryParse(resource.Id, CultureInfo.InvariantCulture, out var modId)
                ? await _curseForge.GetFilesAsync(modId, ct).ConfigureAwait(false)
                : Array.Empty<ResourceVersion>();
        }
        else
        {
            var key = string.IsNullOrWhiteSpace(resource.Id) ? resource.Slug : resource.Id;
            raw = await _modrinth.GetVersionsAsync(key, ct).ConfigureAwait(false);
        }

        // Normalize to newest-first (Modrinth already is; CurseForge's order isn't guaranteed).
        var sorted = raw.OrderByDescending(v => v.PublishedAt ?? DateTimeOffset.MinValue).ToList();
        DebugLog.Info($"Versions: '{resource.Name}' → {sorted.Count} version(s) from {resource.Source}.");
        return sorted;
    }

    private async Task<(IReadOnlyList<ResourceInfo> Results, bool Ok)> RunSafeAsync(
        string label, Func<Task<IReadOnlyList<ResourceInfo>>> start, bool isCurseForge)
    {
        try { return (await start().ConfigureAwait(false), true); }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) when (isCurseForge && _curseForge.IsConfigured)
        {
            DebugLog.Warn($"Search: {label} failed ({ex.GetType().Name}: {ex.Message}); continuing without it.");
            ShowCurseForgeErrorToast();
            return (Array.Empty<ResourceInfo>(), false);
        }
        catch (Exception ex)
        {
            DebugLog.Warn($"Search: {label} failed ({ex.GetType().Name}: {ex.Message}); continuing without it.");
            return (Array.Empty<ResourceInfo>(), false);
        }
    }

    private void ShowCurseForgeErrorToast()
    {
        if (Interlocked.Exchange(ref _curseForgeErrorToastShown, 1) != 0) return;
        if (_notifications is null) return;
        _ = _notifications.ShowAsync(NotificationRequest.Toast(
            "CurseForge unavailable",
            "CurseForge search failed — the API key may be invalid or the service is unreachable. Showing Modrinth results only. Set CF_API_KEY and rebuild to reconfigure.",
            NotificationLevel.Warning));
    }

    private static IReadOnlyList<ResourceInfo> Merge(IList<ResourceInfo> all, ResourceSort sort, int limit)
    {
        if (all.Count == 0) return Array.Empty<ResourceInfo>();
        var cap = Math.Max(limit, 20) * 2;

        if (sort == ResourceSort.Downloads)
            return all.OrderByDescending(r => r.Downloads).Take(cap).ToList();

        if (sort == ResourceSort.Updated || sort == ResourceSort.Newest)
            return all.OrderByDescending(r => r.UpdatedAt ?? DateTimeOffset.MinValue).Take(cap).ToList();

        // Relevance: round-robin interleave of the two platforms' own relevance orderings (Modrinth first),
        // so neither source crowds the other out of the top of the grid.
        var modrinth = all.Where(r => r.Source == ResourceSource.Modrinth).ToList();
        var curseForge = all.Where(r => r.Source == ResourceSource.CurseForge).ToList();
        var merged = new List<ResourceInfo>(all.Count);
        for (var i = 0; i < Math.Max(modrinth.Count, curseForge.Count); i++)
        {
            if (i < modrinth.Count) merged.Add(modrinth[i]);
            if (i < curseForge.Count) merged.Add(curseForge[i]);
        }
        return merged.Count <= cap ? merged : merged.GetRange(0, cap);
    }
}
