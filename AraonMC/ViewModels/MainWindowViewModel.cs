using System.Collections.Generic;
using System.Collections.ObjectModel;
using AraonMC.Core.Application.Notifications;
using AraonMC.Core.Application.Ports;
using AraonMC.Core.Domain.Entities;
using AraonMC.Core.Domain.Repositories;
using AraonMC.ViewModels.Pages;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AraonMC.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public MainWindowViewModel(
        IAccountService accounts,
        IInstanceRepository instances,
        IVersionRepository versions,
        IModRepository mods,
        IGameLauncher launcher,
        INotificationService notifications)
    {
        var home = new HomeViewModel(launcher, instances, accounts);
        var instancesPage = new InstancesViewModel(instances, launcher, accounts);
        var modsPage = new ModsViewModel(mods);
        var accountsPage = new AccountsViewModel(accounts);
        var settings = new SettingsViewModel(notifications);

        NavItems =
        [
            new NavItemViewModel(this, "HomeIcon", "Home", home),
            new NavItemViewModel(this, "BoxIcon", "Instances", instancesPage),
            new NavItemViewModel(this, "PuzzleIcon", "Mods", modsPage),
            new NavItemViewModel(this, "PersonIcon", "Accounts", accountsPage),
            new NavItemViewModel(this, "GearIcon", "Settings", settings),
        ];

        Accounts = new ObservableCollection<MinecraftAccount>(accounts.GetAccounts());
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
    private void SwitchAccount(MinecraftAccount? account)
    {
        if (account is null) return;
        foreach (var a in Accounts) a.IsActive = a == account;
        ActiveAccount = account;
        IsAccountSwitcherOpen = false;
    }
}
