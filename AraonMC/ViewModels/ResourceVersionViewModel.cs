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

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AraonMC.Core.Domain.Entities;
using AraonMC.Core.Domain.Repositories;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AraonMC.ViewModels;

/// <summary>
/// Backs the version-select window: loads a resource's versions and groups them by game version into a
/// collapsible accordion. The owner opens the window modally; when the user picks a version (or cancels),
/// <see cref="RequestClose"/> fires and <see cref="SelectedVersion"/> holds the choice (null = cancelled).
/// </summary>
public partial class ResourceVersionViewModel : ObservableObject
{
    private readonly IResourceRepository _repo;

    public ResourceInfo Resource { get; }

    public ObservableCollection<VersionGroup> Groups { get; } = new();

    [ObservableProperty] private bool _isLoading = true;
    [ObservableProperty] private bool _isEmpty;
    [ObservableProperty] private bool _hasError;
    [ObservableProperty] private string? _errorMessage;

    /// <summary>The version the user picked, or null if they cancelled / hadn't picked.</summary>
    public ResourceVersion? SelectedVersion { get; private set; }

    /// <summary>Raised when the user picks a version or cancels; the owner closes the window in response.</summary>
    public event Action? RequestClose;

    public ResourceVersionViewModel(ResourceInfo resource, IResourceRepository repo)
    {
        Resource = resource;
        _repo = repo;
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            var versions = await _repo.GetVersionsAsync(Resource);
            Groups.Clear();
            foreach (var group in GroupByGameVersion(versions)) Groups.Add(group);
            IsEmpty = Groups.Count == 0;
            DebugLog.Info($"VersionSelect: '{Resource.Name}' → {Groups.Count} game-version group(s) across {versions.Count} version(s).");
        }
        catch (Exception ex)
        {
            DebugLog.Error($"VersionSelect: failed to load versions for '{Resource.Name}' — {ex.Message}");
            HasError = true;
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void Pick(ResourceVersion? version)
    {
        if (version is null || string.IsNullOrWhiteSpace(version.DownloadUrl)) return;
        SelectedVersion = version;
        DebugLog.Info($"VersionSelect: picked '{version.Name}' for '{Resource.Name}'.");
        RequestClose?.Invoke();
    }

    [RelayCommand]
    private void Cancel() => RequestClose?.Invoke();

    /// <summary>
    /// Buckets versions by each game version they support (a single jar often covers several). Groups are
    /// sorted newest-game-version first (numeric releases above snapshots); within a group, newest first.
    /// </summary>
    private static List<VersionGroup> GroupByGameVersion(IReadOnlyList<ResourceVersion> versions)
    {
        var byGame = new Dictionary<string, List<ResourceVersion>>(StringComparer.OrdinalIgnoreCase);
        foreach (var v in versions)
        {
            var gvs = v.GameVersions.Count > 0 ? v.GameVersions : new[] { "Any version" };
            foreach (var gv in gvs)
            {
                if (!byGame.TryGetValue(gv, out var list)) { list = new List<ResourceVersion>(); byGame[gv] = list; }
                list.Add(v);
            }
        }

        return byGame
            .OrderByDescending(kv => kv.Key, GameVersionComparer.Instance)
            .Select(kv => new VersionGroup(kv.Key,
                new ObservableCollection<ResourceVersion>(
                    kv.Value.OrderByDescending(v => v.PublishedAt ?? DateTimeOffset.MinValue))))
            .ToList();
    }

    /// <summary>Best-effort game-version ordering: numeric dotted versions (1.20.1) compared as integer tuples,
    /// ranked above non-numeric (snapshots like "24w14potato"), which fall back to lexical order.</summary>
    private sealed class GameVersionComparer : IComparer<string>
    {
        public static readonly GameVersionComparer Instance = new();

        public int Compare(string? x, string? y)
        {
            if (ReferenceEquals(x, y)) return 0;
            if (x is null) return -1;
            if (y is null) return 1;

            var nx = Parse(x);
            var ny = Parse(y);
            if (nx is not null && ny is not null) return CompareInts(nx, ny);
            if (nx is not null) return 1;  // release (numeric) ranks above snapshot (non-numeric)
            if (ny is not null) return -1;
            return string.Compare(x, y, StringComparison.OrdinalIgnoreCase);
        }

        private static int[]? Parse(string s)
        {
            var parts = s.Split('.');
            var nums = new int[parts.Length];
            for (var i = 0; i < parts.Length; i++)
                if (!int.TryParse(parts[i], out nums[i])) return null;
            return nums;
        }

        private static int CompareInts(int[] a, int[] b)
        {
            for (var i = 0; i < Math.Max(a.Length, b.Length); i++)
            {
                var av = i < a.Length ? a[i] : 0;
                var bv = i < b.Length ? b[i] : 0;
                if (av != bv) return av.CompareTo(bv);
            }
            return 0;
        }
    }
}

/// <summary>One game-version bucket in the accordion: a collapsible header + its mod versions.</summary>
public partial class VersionGroup : ObservableObject
{
    public string GameVersion { get; }
    public ObservableCollection<ResourceVersion> Versions { get; }

    [ObservableProperty] private bool _isExpanded;

    public VersionGroup(string gameVersion, ObservableCollection<ResourceVersion> versions)
    {
        GameVersion = gameVersion;
        Versions = versions;
    }

    [RelayCommand]
    private void Toggle() => IsExpanded = !IsExpanded;
}
