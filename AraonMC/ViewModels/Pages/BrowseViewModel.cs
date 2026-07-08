using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AraonMC.Core.Application.Notifications;
using AraonMC.Core.Domain.Entities;
using AraonMC.Core.Domain.Enums;
using AraonMC.Core.Domain.Repositories;
using AraonMC.Downloads;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AraonMC.ViewModels.Pages;

/// <summary>
/// Browse page: searches Modrinth and/or CurseForge for Minecraft content (mods, modpacks, resource
/// packs, shader packs, world saves, datapacks), with filtering by loader / game version and sorting.
/// A search is re-run (debounced for text, immediate for dropdown/chip changes) whenever any filter moves.
/// The Install button downloads the entry's primary file to a user-chosen folder (no real install yet).
/// </summary>
public partial class BrowseViewModel : PageViewModelBase
{
    private const int SearchDebounceMs = 350;
    private const int PageSize = 30;

    private readonly IResourceRepository _repo;
    private readonly IDownloadManager _downloads;
    private readonly INotificationService _notifications;
    private readonly Func<string?, Task<string?>> _pickSaveFile;
    private readonly Func<ResourceInfo, Task<ResourceVersion?>> _pickVersion;
    private readonly bool _suppressSearch = true; // suppress during ctor defaults
    private CancellationTokenSource? _searchCts;
    private bool _installing; // re-entry guard for the detail/download flow.
    private bool _curseForgeHintShown;

    public BrowseViewModel(
        IResourceRepository repo,
        IDownloadManager downloads,
        INotificationService notifications,
        Func<string?, Task<string?>> pickSaveFile,
        Func<ResourceInfo, Task<ResourceVersion?>> pickVersion)
    {
        _repo = repo;
        _downloads = downloads;
        _notifications = notifications;
        _pickSaveFile = pickSaveFile;
        _pickVersion = pickVersion;
        Title = "Browse";

        ResourceTypes = Enum.GetValues<ResourceType>().Select(t => new Option<ResourceType>(LabelFor(t), t)).ToList();
        Sources =
        [
            new Option<ResourceSourceFilter>("Modrinth", ResourceSourceFilter.Modrinth),
            new Option<ResourceSourceFilter>("CurseForge", ResourceSourceFilter.CurseForge),
            new Option<ResourceSourceFilter>("All", ResourceSourceFilter.All),
        ];
        Loaders =
        [
            new Option<LoaderType?>("Any loader", null),
            new Option<LoaderType?>("Fabric", LoaderType.Fabric),
            new Option<LoaderType?>("Forge", LoaderType.Forge),
            new Option<LoaderType?>("NeoForge", LoaderType.NeoForge),
            new Option<LoaderType?>("Quilt", LoaderType.Quilt),
        ];
        Sorts =
        [
            new Option<ResourceSort>("Relevance", ResourceSort.Relevance),
            new Option<ResourceSort>("Most downloaded", ResourceSort.Downloads),
            new Option<ResourceSort>("Recently updated", ResourceSort.Updated),
            new Option<ResourceSort>("Newest", ResourceSort.Newest),
        ];

        SelectedTypeOption = ResourceTypes[0];
        SelectedSourceOption = Sources.First(o => o.Value == ResourceSourceFilter.All);
        SelectedLoaderOption = Loaders[0];
        SelectedSortOption = Sorts[0];

        _suppressSearch = false;
        TriggerSearch(immediate: false); // initial browse
    }

    public ObservableCollection<ResourceInfo> Items { get; } = new();

    public bool IsCurseForgeConfigured => _repo.IsCurseForgeConfigured;

    public IReadOnlyList<Option<ResourceType>> ResourceTypes { get; }
    public IReadOnlyList<Option<ResourceSourceFilter>> Sources { get; }
    public IReadOnlyList<Option<LoaderType?>> Loaders { get; }
    public IReadOnlyList<Option<ResourceSort>> Sorts { get; }

    [ObservableProperty] private Option<ResourceType>? _selectedTypeOption;
    [ObservableProperty] private Option<ResourceSourceFilter>? _selectedSourceOption;
    [ObservableProperty] private Option<LoaderType?>? _selectedLoaderOption;
    [ObservableProperty] private Option<ResourceSort>? _selectedSortOption;

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string _gameVersion = string.Empty;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private bool _showEmptyHint;

    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private int _totalPages = 1;

    partial void OnSelectedTypeOptionChanged(Option<ResourceType>? value) { CurrentPage = 1; TriggerSearchIfReady(true); }
    partial void OnSelectedSourceOptionChanged(Option<ResourceSourceFilter>? value) { CurrentPage = 1; TriggerSearchIfReady(true); }
    partial void OnSelectedLoaderOptionChanged(Option<LoaderType?>? value) { CurrentPage = 1; TriggerSearchIfReady(true); }
    partial void OnSelectedSortOptionChanged(Option<ResourceSort>? value) { CurrentPage = 1; TriggerSearchIfReady(true); }
    partial void OnSearchTextChanged(string value) { CurrentPage = 1; TriggerSearchIfReady(false); }
    partial void OnGameVersionChanged(string value) { CurrentPage = 1; TriggerSearchIfReady(false); }
    partial void OnCurrentPageChanged(int value) => NotifyPaginationCanExecuteChanged();
    partial void OnTotalPagesChanged(int value) => NotifyPaginationCanExecuteChanged();

    private void TriggerSearchIfReady(bool immediate)
    {
        if (_suppressSearch) return;
        // Don't run before all dropdowns have a selection (avoids firing during ctor setup).
        if (SelectedTypeOption is null || SelectedSourceOption is null
            || SelectedLoaderOption is null || SelectedSortOption is null) return;
        TriggerSearch(immediate);
    }

    private void TriggerSearch(bool immediate)
    {
        _searchCts?.Cancel();
        var cts = _searchCts = new CancellationTokenSource();
        _ = SearchEventuallyAsync(cts, immediate);
    }

    private async Task SearchEventuallyAsync(CancellationTokenSource cts, bool immediate)
    {
        try
        {
            if (!immediate) await Task.Delay(SearchDebounceMs, cts.Token);
            await SearchAsync(cts.Token);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            DebugLog.Error($"Browse: search failed — {ex.GetType().Name}: {ex.Message}");
            await _notifications.ShowAsync(NotificationRequest.Toast(
                "Search failed", ex.Message, NotificationLevel.Error));
        }
    }

    private async Task SearchAsync(CancellationToken ct)
    {
        var source = SelectedSourceOption!.Value;
        var offset = (CurrentPage - 1) * PageSize;
        var query = new ResourceSearchQuery
        {
            Type = SelectedTypeOption!.Value,
            Text = SearchText ?? string.Empty,
            Loader = SelectedLoaderOption!.Value,
            GameVersion = string.IsNullOrWhiteSpace(GameVersion) ? null : GameVersion.Trim(),
            Sort = SelectedSortOption!.Value,
            Sources = source,
            Limit = PageSize,
            Offset = offset,
        };

        // CurseForge-only with no key configured: explain instead of silently showing nothing.
        if (source == ResourceSourceFilter.CurseForge && !IsCurseForgeConfigured)
        {
            Items.Clear();
            IsBusy = false;
            ShowEmptyHint = true;
            TotalPages = 1;
            CurrentPage = 1;
            ShowCurseForgeConfigHint();
            return;
        }

        IsBusy = true;
        ShowEmptyHint = false;
        try
        {
            var results = await _repo.SearchAsync(query, ct);
            ct.ThrowIfCancellationRequested();
            Items.Clear();
            foreach (var r in results) Items.Add(r);
            ShowEmptyHint = Items.Count == 0;

            TotalPages = query.TotalCount > 0
                ? (query.TotalCount + PageSize - 1) / PageSize
                : (Items.Count < PageSize ? 1 : CurrentPage + 1);
            if (CurrentPage > TotalPages) CurrentPage = TotalPages;

            DebugLog.Info($"Browse: showing {Items.Count} result(s) (total: {query.TotalCount}, page {CurrentPage}/{TotalPages}) for type={query.Type} source={query.Sources}.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ShowCurseForgeConfigHint()
    {
        if (_curseForgeHintShown) return;
        _curseForgeHintShown = true;
        DebugLog.Info("Browse: CurseForge source selected but no API key is configured.");
        _ = _notifications.ShowAsync(NotificationRequest.Toast(
            "CurseForge not configured",
            "No CurseForge API key is set, so CurseForge results are unavailable. Set CF_API_KEY and rebuild, or switch the source to Modrinth.",
            NotificationLevel.Info));
    }

    /// <summary>
    /// Opens the version-select window for the resource; on a pick, asks where to save the file (the user
    /// can rename it), then queues it as a managed download. The button stays available afterward — there is
    /// no one-time "installed" lock, so any version can be re-downloaded.
    /// </summary>
    [RelayCommand]
    private async Task Detail(ResourceInfo? resource)
    {
        if (resource is null || _installing) return;
        _installing = true;

        try
        {
            // 1. Pick a version (grouped by game version). Modal — returns null on cancel.
            var version = await _pickVersion(resource);
            if (version is null)
            {
                DebugLog.Info($"Detail: cancelled version selection for '{resource.Name}'.");
                return;
            }

            // 2. Pick a save path (the user can rename the file). Pre-filled with the version's filename.
            var destPath = await _pickSaveFile(string.IsNullOrWhiteSpace(version.FileName) ? resource.Name : version.FileName);
            if (string.IsNullOrEmpty(destPath))
            {
                DebugLog.Info($"Detail: cancelled save dialog for '{resource.Name}'.");
                return;
            }

            // 3. Queue the download through the download manager.
            var job = await _downloads.EnqueueFileDownloadAsync(resource.Name, version.DownloadUrl, destPath);
            DebugLog.Info($"Detail: enqueued '{resource.Name}' {version.Name} as job {job.Id} → {destPath}.");
            await _notifications.ShowAsync(NotificationRequest.Toast(
                "Download queued",
                $"'{resource.Name}' ({version.Name}) is downloading — see the Downloads page.",
                NotificationLevel.Info));
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            DebugLog.Error($"Detail: failed to queue '{resource.Name}' — {ex.GetType().Name}: {ex.Message}");
            await _notifications.ShowAsync(NotificationRequest.Toast(
                "Download failed", $"{resource.Name}: {ex.Message}", NotificationLevel.Error));
        }
        finally
        {
            _installing = false;
        }
    }

    [RelayCommand]
    private async Task OpenPage(ResourceInfo? resource)
    {
        if (resource is null || string.IsNullOrEmpty(resource.PageUrl)) return;
        if (!Uri.TryCreate(resource.PageUrl, UriKind.Absolute, out var uri)) return;

        try
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                && TopLevel.GetTopLevel(desktop.MainWindow)?.Launcher is { } launcher
                && await launcher.LaunchUriAsync(uri))
            {
                DebugLog.Info($"OpenPage: launched {uri} for '{resource.Name}'.");
                return;
            }
            // Fallback for non-desktop hosts: hand the URL to the OS shell.
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(uri.AbsoluteUri)
            {
                UseShellExecute = true,
            });
            DebugLog.Info($"OpenPage: shell-launched {uri} for '{resource.Name}'.");
        }
        catch (Exception ex)
        {
            DebugLog.Warn($"OpenPage: failed for '{resource.Name}' — {ex.Message}");
            await _notifications.ShowAsync(NotificationRequest.Toast(
                "Couldn't open page", ex.Message, NotificationLevel.Warning));
        }
    }

    [RelayCommand(CanExecute = nameof(CanGoToPreviousPage))]
    private void GoToPreviousPage()
    {
        if (CurrentPage > 1)
        {
            CurrentPage--;
            TriggerSearch(true);
        }
    }

    private bool CanGoToPreviousPage() => CurrentPage > 1;

    [RelayCommand(CanExecute = nameof(CanGoToNextPage))]
    private void GoToNextPage()
    {
        if (CurrentPage < TotalPages)
        {
            CurrentPage++;
            TriggerSearch(true);
        }
    }

    private bool CanGoToNextPage() => CurrentPage < TotalPages;

    private void NotifyPaginationCanExecuteChanged()
    {
        GoToPreviousPageCommand.NotifyCanExecuteChanged();
        GoToNextPageCommand.NotifyCanExecuteChanged();
    }

    private static string LabelFor(ResourceType t) => t switch
    {
        ResourceType.Mod => "Mods",
        ResourceType.Modpack => "Modpacks",
        ResourceType.ResourcePack => "Resource Packs",
        ResourceType.ShaderPack => "Shader Packs",
        ResourceType.WorldSave => "World Saves",
        ResourceType.DataPack => "Datapacks",
        _ => t.ToString(),
    };
}

/// <summary>A labeled value bound to ComboBox / ListBox item templates (the <c>Label</c> is shown).</summary>
public sealed record Option<T>(string Label, T Value);
