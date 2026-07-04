using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Threading.Tasks;
using AraonMC.Core.Application.Notifications;
using AraonMC.Core.Application.Ports;
using AraonMC.Core.Domain.Entities;
using AraonMC.Core.Domain.Repositories;
using AraonMC.Downloads;
using AraonMC.ViewModels.Pages;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AraonMC.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IAccountService _accounts;
    private readonly HomeViewModel _homePage;
    private readonly InstancesViewModel _instancesPage;
    private readonly VersionSelectViewModel _versionSelectPage;
    private NavItemViewModel? _downloadsItem;

    public MainWindowViewModel(
        IAccountService accounts,
        IInstanceRepository instances,
        IVersionList versions,
        IDownloadManager downloads,
        IResourceRepository resources,
        IGameLauncher launcher,
        INotificationService notifications,
        Func<Task<string?>> pickFolder,
        Func<string?, Task<string?>> pickSaveFile,
        Func<ResourceInfo, Task<ResourceVersion?>> pickVersion)
    {
        _accounts = accounts;
        _homePage = new HomeViewModel(launcher, instances, accounts, notifications);
        _instancesPage = new InstancesViewModel(instances, launcher, accounts, notifications, ShowVersionSelect);
        var downloadsPage = new DownloadsViewModel(downloads);
        _versionSelectPage = new VersionSelectViewModel(versions, instances, downloads, NavigateToDownloads, RefreshInstancePages);
        var browsePage = new BrowseViewModel(resources, downloads, notifications, pickSaveFile, pickVersion);
        var accountsPage = new AccountsViewModel(accounts, notifications);
        var settings = new SettingsViewModel(notifications, pickFolder);

        var downloadsItem = new NavItemViewModel(this, "download", "Downloads", downloadsPage);
        _downloadsItem = downloadsItem;

        NavItems =
        [
            new NavItemViewModel(this, "house", "Home", _homePage),
            new NavItemViewModel(this, "package", "Instances", _instancesPage),
            downloadsItem,
            new NavItemViewModel(this, "puzzle", "Browse", browsePage),
            new NavItemViewModel(this, "user", "Accounts", accountsPage),
            new NavItemViewModel(this, "settings", "Settings", settings),
        ];

        // Share the service-owned live list so account add/remove stays in sync with this switcher.
        Accounts = accounts.Accounts;
        Accounts.CollectionChanged += Accounts_CollectionChanged;
        ActiveAccount = accounts.GetActive();
        SyncAccountFooter();

        Navigate(NavItems[0]);
    }

    public IReadOnlyList<NavItemViewModel> NavItems { get; }
    public ObservableCollection<MinecraftAccount> Accounts { get; }

    [ObservableProperty] private PageViewModelBase? _currentPage;
    [ObservableProperty] private MinecraftAccount? _activeAccount;
    [ObservableProperty] private bool _isAccountSwitcherOpen;
    [ObservableProperty] private bool _hasAccounts;
    [ObservableProperty] private string _accountFooterName = "No Accounts";
    [ObservableProperty] private string _accountFooterStatus = "Go to Accounts page to add one";
    [ObservableProperty] private string _accountFooterAvatarKey = "?";

    public void Navigate(NavItemViewModel item)
    {
        foreach (var n in NavItems) n.IsActive = n == item;
        CurrentPage = item.Page;
    }

    /// <summary>展示版本选择页（非常驻 nav；安装后跳转到 Downloads）。</summary>
    public void ShowVersionSelect()
    {
        foreach (var n in NavItems) n.IsActive = false;
        CurrentPage = _versionSelectPage;
    }

    private void NavigateToDownloads()
    {
        if (_downloadsItem is not null) Navigate(_downloadsItem);
    }

    /// <summary>Refreshes all pages that present the live instance list after create/rename/delete.</summary>
    private void RefreshInstancePages()
    {
        _homePage.RefreshInstances();
        _instancesPage.RefreshInstances();
    }

    private void Accounts_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        ActiveAccount = _accounts.GetActive();
        SyncAccountFooter();
    }

    private void SyncAccountFooter()
    {
        var active = _accounts.GetActive();
        HasAccounts = active is not null;
        AccountFooterName = active?.Username ?? "No Accounts";
        AccountFooterStatus = active is null ? "Go to Accounts page to add one" : (active.IsOnline ? "Online" : "Offline");
        AccountFooterAvatarKey = active?.AvatarKey ?? "?";
    }

    [RelayCommand]
    private void ToggleAccountSwitcher() => IsAccountSwitcherOpen = !IsAccountSwitcherOpen;

    [RelayCommand]
    private async Task SwitchAccount(MinecraftAccount? account)
    {
        if (account is null) return;
        await _accounts.SetActiveAsync(account);
        ActiveAccount = account;
        SyncAccountFooter();
        IsAccountSwitcherOpen = false;
    }
}
