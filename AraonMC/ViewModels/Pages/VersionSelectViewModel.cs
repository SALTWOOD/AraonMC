using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AraonMC.Core.Domain.Entities;
using AraonMC.Core.Domain.Enums;
using AraonMC.Core.Domain.Repositories;
using AraonMC.Downloads;
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
            var list = await _versions.GetVersionsAsync();
            _all.Clear();
            _all.AddRange(list);
            ApplyFilter();
        }
        catch (Exception)
        {
            // TODO: 通过 INotificationService 上报；当前静默，避免 ctor 抛异常。
        }
    }

    [RelayCommand]
    private async Task InstallAsync(MinecraftVersion? version)
    {
        if (version is null) return;
        var instance = await _repo.CreateAsync($"Minecraft {version.Id}", version, LoaderType.Vanilla);
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
