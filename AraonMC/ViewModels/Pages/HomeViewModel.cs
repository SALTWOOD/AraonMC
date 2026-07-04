using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using AraonMC.Core.Application.Notifications;
using AraonMC.Core.Application.Ports;
using AraonMC.Core.Domain.Entities;
using AraonMC.Core.Domain.Repositories;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AraonMC.ViewModels.Pages;

public partial class HomeViewModel : PageViewModelBase
{
    private readonly IGameLauncher _launcher;
    private readonly IInstanceRepository _instances;
    private readonly IAccountService _accounts;
    private readonly INotificationService _notifications;

    public HomeViewModel(
        IGameLauncher launcher,
        IInstanceRepository instances,
        IAccountService accounts,
        INotificationService notifications)
    {
        _launcher = launcher;
        _instances = instances;
        _accounts = accounts;
        _notifications = notifications;

        Title = "Play";
        Instances = new ObservableCollection<GameInstance>();
        RefreshInstances();
        News = new ObservableCollection<NewsItem>(BuildNews());
    }

    public ObservableCollection<GameInstance> Instances { get; }
    public ObservableCollection<NewsItem> News { get; }

    [ObservableProperty] private GameInstance? _selectedInstance;

    /// <summary>Reloads the instance list from the repository and preserves the current selection when possible.</summary>
    public void RefreshInstances()
    {
        var selectedId = SelectedInstance?.Id;
        Instances.Clear();
        foreach (var i in _instances.GetAll()) Instances.Add(i);
        SelectedInstance = selectedId is not null
            ? Instances.FirstOrDefault(i => i.Id == selectedId) ?? Instances.FirstOrDefault()
            : Instances.FirstOrDefault();
    }

    [RelayCommand]
    private async Task PlayAsync()
    {
        if (SelectedInstance is null) return;

        // Always resolve the active account from the shared account service so launch uses the same
        // account the left-bottom switcher currently points at.
        var account = _accounts.GetActive();
        if (account is null)
        {
            await _notifications.ShowAsync(NotificationRequest.Toast(
                "No account",
                "Add or select an account before launching.",
                NotificationLevel.Warning));
            return;
        }

        DebugLog.Info($"Home: launching '{SelectedInstance.Name}' with active account '{account.Username}' (uuid={account.Uuid}).");
        await _launcher.LaunchAsync(SelectedInstance, account);
    }

    private static IEnumerable<NewsItem> BuildNews() =>
    [
        new()
        {
            Title = "Minecraft 1.21.4",
            Tag = "Release",
            Body = "Winter Drop features pale garden biome & creaking mob.",
            Date = DateTimeOffset.Now - TimeSpan.FromDays(7),
        },
        new()
        {
            Title = "The Garden Awakens",
            Tag = "Event",
            Body = "Limited-time pale garden event now live across realms.",
            Date = DateTimeOffset.Now - TimeSpan.FromDays(3),
        },
        new()
        {
            Title = "Java 21 bundled",
            Tag = "Patch",
            Body = "Runtime now ships Java 21 LTS by default.",
            Date = DateTimeOffset.Now - TimeSpan.FromHours(20),
        },
    ];
}
