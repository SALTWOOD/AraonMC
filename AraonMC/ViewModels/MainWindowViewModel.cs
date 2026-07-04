using System.Collections.Generic;
using System.Collections.ObjectModel;
using AraonMC.Core.Application.Notifications;
using AraonMC.Core.Application.Ports;
using AraonMC.Core.Domain.Entities;
using AraonMC.Core.Domain.Repositories;
using AraonMC.Downloads;
using AraonMC.ViewModels.Pages;
using AraonMC.Versions;
using AraonMC.Versions.Install;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AraonMC.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IAccountService _accounts;

    public MainWindowViewModel(
        IAccountService accounts,
        IInstanceRepository instances,
        IVersionList versions,
        VersionInstaller installer,
        IModRepository mods,
        IGameLauncher launcher,
        INotificationService notifications,
        IDownloadManager downloads)
    {
        _accounts = accounts;
        var home = new HomeViewModel(launcher, instances, accounts);
        var instancesPage = new InstancesViewModel(instances, versions, installer, launcher, accounts, notifications);
        var downloadsPage = new DownloadsViewModel(downloads);
        var modsPage = new ModsViewModel(mods);
        var accountsPage = new AccountsViewModel(accounts, notifications);
        var settings = new SettingsViewModel(notifications);

        NavItems =
        [
            new NavItemViewModel(this, "HomeIcon", "Home", home),
            new NavItemViewModel(this, "BoxIcon", "Instances", instancesPage),
            new NavItemViewModel(this, "DownloadIcon", "Downloads", downloadsPage),
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
