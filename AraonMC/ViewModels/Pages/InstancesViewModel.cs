using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AraonMC.Core.Application.Notifications;
using AraonMC.Core.Application.Ports;
using AraonMC.Core.Domain.Entities;
using AraonMC.Core.Domain.Enums;
using AraonMC.Core.Domain.Repositories;
using AraonMC.Versions;
using AraonMC.Versions.Install;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AraonMC.ViewModels.Pages;

public partial class InstancesViewModel : PageViewModelBase
{
    private readonly IInstanceRepository _repo;
    private readonly IVersionList _versions;
    private readonly VersionInstaller _installer;
    private readonly IGameLauncher _launcher;
    private readonly IAccountService _accounts;
    private readonly INotificationService _notifications;
    private readonly List<GameInstance> _all;

    public InstancesViewModel(
        IInstanceRepository repo,
        IVersionList versions,
        VersionInstaller installer,
        IGameLauncher launcher,
        IAccountService accounts,
        INotificationService notifications)
    {
        _repo = repo;
        _versions = versions;
        _installer = installer;
        _launcher = launcher;
        _accounts = accounts;
        _notifications = notifications;
        _all = [.. repo.GetAll()];

        Title = "Instances";
        Items = new ObservableCollection<GameInstance>(_all);
    }

    public ObservableCollection<GameInstance> Items { get; }

    [ObservableProperty] private string _searchText = string.Empty;

    [RelayCommand]
    private async Task PlayAsync(GameInstance? instance)
    {
        if (instance is null) return;
        try { await _launcher.LaunchAsync(instance, _accounts.GetActive()!); }
        catch (NotImplementedException) { /* backend pending */ }
    }

    [RelayCommand]
    private async Task NewAsync()
    {
        try
        {
            var versions = await _versions.GetVersionsAsync();
            var version = versions.FirstOrDefault(v => v.Type == VersionType.Release)
                       ?? versions.FirstOrDefault();
            if (version is null) return;

            var instance = await _repo.CreateAsync("New Instance", version, LoaderType.Vanilla);
            _all.Add(instance);
            Items.Add(instance);

            // 后台安装，不阻塞 UI。
            _ = InstallAsync(instance);
        }
        catch (Exception ex)
        {
            await _notifications.ShowAsync(NotificationRequest.Toast(
                "New instance failed", ex.Message, NotificationLevel.Error));
        }
    }

    private async Task InstallAsync(GameInstance instance)
    {
        try
        {
            await _notifications.ShowAsync(NotificationRequest.Toast(
                "Downloading", $"Installing Minecraft {instance.MinecraftVersion}…", NotificationLevel.Info));
            var progress = new Progress<InstallProgress>(p =>
                DebugLog.Info($"install {instance.MinecraftVersion}: {p.Phase} {p.Done}/{p.Total} {p.CurrentFile}"));
            await _installer.InstallAsync(instance.MinecraftVersion, instance.Path, progress);
            await _notifications.ShowAsync(NotificationRequest.Toast(
                "Download complete", $"{instance.Name} is ready to play.", NotificationLevel.Success));
        }
        catch (Exception ex)
        {
            await _notifications.ShowAsync(NotificationRequest.Toast(
                "Download failed", $"{instance.Name}: {ex.Message}", NotificationLevel.Error));
        }
    }

    partial void OnSearchTextChanged(string value)
    {
        var q = value.Trim();
        Items.Clear();
        foreach (var i in _all.Where(x => x.Name.Contains(q, StringComparison.OrdinalIgnoreCase)))
            Items.Add(i);
    }
}
