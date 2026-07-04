using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    private readonly VersionSelectViewModel _versionSelectPage;
    private NavItemViewModel? _downloadsItem;

    public MainWindowViewModel(
        IAccountService accounts,
        IInstanceRepository instances,
        IVersionList versions,
        IDownloadManager downloads,
        IModRepository mods,
        IGameLauncher launcher,
        INotificationService notifications,
        Func<Task<string?>> pickFolder)
    {
        _accounts = accounts;
        var home = new HomeViewModel(launcher, instances, accounts);
        var instancesPage = new InstancesViewModel(instances, launcher, accounts, ShowVersionSelect);
        var downloadsPage = new DownloadsViewModel(downloads);
        _versionSelectPage = new VersionSelectViewModel(versions, instances, downloads, NavigateToDownloads);
        var modsPage = new ModsViewModel(mods);
        var accountsPage = new AccountsViewModel(accounts, notifications);
        var settings = new SettingsViewModel(notifications, pickFolder);

        var downloadsItem = new NavItemViewModel(this, "DownloadIcon", "Downloads", downloadsPage);
        _downloadsItem = downloadsItem;

        NavItems =
        [
            new NavItemViewModel(this, "HomeIcon", "Home", home),
            new NavItemViewModel(this, "BoxIcon", "Instances", instancesPage),
            downloadsItem,
            new NavItemViewModel(this, "PuzzleIcon", "Mods", modsPage),
            new NavItemViewModel(this, "PersonIcon", "Accounts", accountsPage),
            new NavItemViewModel(this, "GearIcon", "Settings", settings),
        ];

        // Share the service-owned live list so account add/remove stays in sync with this switcher.
        Accounts = accounts.Accounts;
        ActiveAccount = accounts.GetActive();

        Navigate(NavItems[0]);
    }

    public IReadOnlyList<NavItemViewModel> NavItems { get; }
    public ObservableCollection<MinecraftAccount> Accounts { get; }

    [ObservableProperty] private PageViewModelBase? _currentPage;
    [ObservableProperty] private MinecraftAccount? _activeAccount;
    [ObservableProperty] private bool _isAccountSwitcherOpen;

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

    [RelayCommand]
    private void ToggleAccountSwitcher() => IsAccountSwitcherOpen = !IsAccountSwitcherOpen;

    [RelayCommand]
    private async Task SwitchAccount(MinecraftAccount? account)
    {
        if (account is null) return;
        await _accounts.SetActiveAsync(account);
        ActiveAccount = account;
        IsAccountSwitcherOpen = false;
    }
}
