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
        IVersionListService versionList,
        IDownloadManager downloads,
        IResourceRepository resources,
        IGameLauncher launcher,
        INotificationService notifications,
        Func<Task<string?>> pickFolder,
        Func<string?, Task<string?>> pickSaveFile,
        Func<ResourceInfo, Task<ResourceVersion?>> pickVersion)
    {
        _accounts = accounts;
        var home = new HomeViewModel(launcher, instances, accounts);
        var instancesPage = new InstancesViewModel(instances, launcher, accounts, notifications, ShowVersionSelect);
        var downloadsPage = new DownloadsViewModel(downloads);
        _versionSelectPage = new VersionSelectViewModel(versions, instances, downloads, NavigateToDownloads);
        var browsePage = new BrowseViewModel(resources, downloads, versionList, notifications, pickSaveFile, pickVersion);
        var accountsPage = new AccountsViewModel(accounts, notifications);
        var settings = new SettingsViewModel(notifications, pickFolder);

        var downloadsItem = new NavItemViewModel(this, "download", "Downloads", downloadsPage);
        _downloadsItem = downloadsItem;

        NavItems =
        [
            new NavItemViewModel(this, "house", "Home", home),
            new NavItemViewModel(this, "package", "Instances", instancesPage),
            downloadsItem,
            new NavItemViewModel(this, "puzzle", "Browse", browsePage),
            new NavItemViewModel(this, "user", "Accounts", accountsPage),
            new NavItemViewModel(this, "settings", "Settings", settings),
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
        DebugLog.Info($"Nav: selecting sidebar item '{item.Label}'.");
        foreach (var n in NavItems) n.IsActive = n == item;
        CurrentPage = item.Page;
    }

    /// <summary>展示版本选择页（非常驻 nav；安装后跳转到 Downloads）。</summary>
    public void ShowVersionSelect()
    {
        DebugLog.Info("Nav: opening the transient 'New Instance' (version select) page.");
        foreach (var n in NavItems) n.IsActive = false;
        CurrentPage = _versionSelectPage;
    }

    private void NavigateToDownloads()
    {
        if (_downloadsItem is not null)
        {
            DebugLog.Info("Nav: jumping to the Downloads page after install.");
            Navigate(_downloadsItem);
        }
    }

    [RelayCommand]
    private void ToggleAccountSwitcher()
    {
        IsAccountSwitcherOpen = !IsAccountSwitcherOpen;
        DebugLog.Info($"Nav: account switcher toggled → open={IsAccountSwitcherOpen}.");
    }

    [RelayCommand]
    private async Task SwitchAccount(MinecraftAccount? account)
    {
        if (account is null)
        {
            DebugLog.Info("Nav: account switch requested with no target; ignored.");
            return;
        }
        DebugLog.Info($"Nav: switching active account → '{account.Username}' (uuid={account.Uuid}).");
        await _accounts.SetActiveAsync(account);
        ActiveAccount = account;
        IsAccountSwitcherOpen = false;
    }
}
