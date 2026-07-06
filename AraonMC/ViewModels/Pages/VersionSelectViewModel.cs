using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AraonMC.Core.Domain.Entities;
using AraonMC.Core.Domain.Enums;
using AraonMC.Core.Domain.Repositories;
using AraonMC.Downloads;
using AraonMC.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AraonMC.ViewModels.Pages;

public partial class VersionSelectViewModel : PageViewModelBase
{
    private readonly IVersionList _versions;
    private readonly IInstanceRepository _repo;
    private readonly IDownloadManager _downloads;
    private readonly Action _onInstalled;
    private readonly List<MinecraftVersion> _all = new();

    public VersionSelectViewModel(
        IVersionList versions,
        IInstanceRepository repo,
        IDownloadManager downloads,
        Action onInstalled)
    {
        _versions = versions;
        _repo = repo;
        _downloads = downloads;
        _onInstalled = onInstalled;
        Title = "New Instance";
        _ = LoadAsync();
    }

    public ObservableCollection<MinecraftVersion> Items { get; } = new();

    /// <summary>可选的版本类型过滤项。</summary>
    public VersionType[] Filters { get; } = [VersionType.Release, VersionType.Snapshot, VersionType.OldBeta, VersionType.OldAlpha];

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private VersionType _filter = VersionType.Release;

    private async Task LoadAsync()
    {
        try
        {
            DebugLog.Info("VersionSelect: fetching available Minecraft versions...");
            var list = await _versions.GetVersionsAsync();
            _all.Clear();
            _all.AddRange(list);
            ApplyFilter();
            DebugLog.Info($"VersionSelect: loaded {list.Count} version(s); {Items.Count} shown after the release filter.");
        }
        catch (Exception ex)
        {
            DebugLog.Error($"VersionSelect: failed to load versions — {ex.GetType().Name}: {ex.Message}");
            // TODO: 通过 INotificationService 上报；当前静默，避免 ctor 抛异常。
        }
    }

    [RelayCommand]
    private async Task InstallAsync(MinecraftVersion? version)
    {
        if (version is null) return;
        DebugLog.Info($"VersionSelect: install requested for version '{version.Id}' (type={version.Type}).");

        // Confirm the instance name (and loader, a placeholder for now) before installing.
        var name = await InstallConfirmWindow.ShowAsync(version, _repo);
        if (string.IsNullOrWhiteSpace(name))
        {
            DebugLog.Info("VersionSelect: install cancelled at the name-confirm dialog.");
            return; // cancelled.
        }

        DebugLog.Info($"VersionSelect: creating instance '{name}' for version '{version.Id}' and enqueuing the install.");
        var instance = await _repo.CreateAsync(name, version, LoaderType.Vanilla);
        await _downloads.EnqueueAsync(instance);
        _onInstalled();
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();
    partial void OnFilterChanged(VersionType value) => ApplyFilter();

    private void ApplyFilter()
    {
        var q = SearchText.Trim();
        Items.Clear();
        foreach (var v in _all.Where(x => x.Type == Filter
                                          && x.Id.Contains(q, StringComparison.OrdinalIgnoreCase)))
            Items.Add(v);
    }
}
