using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AraonMC.Core.Application.Ports;
using AraonMC.Core.Domain.Entities;
using AraonMC.Core.Domain.Repositories;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AraonMC.ViewModels.Pages;

public partial class HomeViewModel : PageViewModelBase
{
    private readonly IGameLauncher _launcher;
    private readonly IAccountService _accounts;

    public HomeViewModel(IGameLauncher launcher, IInstanceRepository instances, IAccountService accounts)
    {
        _launcher = launcher;
        _accounts = accounts;

        Title = "Play";
        Instances = new ObservableCollection<GameInstance>(instances.GetAll());
        SelectedInstance = Instances.FirstOrDefault();
        News = new ObservableCollection<NewsItem>(BuildNews());
    }

    public ObservableCollection<GameInstance> Instances { get; }
    public ObservableCollection<NewsItem> News { get; }

    [ObservableProperty] private GameInstance? _selectedInstance;

    [RelayCommand]
    private async Task PlayAsync()
    {
        if (SelectedInstance is null) return;
        try { await _launcher.LaunchAsync(SelectedInstance, _accounts.GetActive()!); }
        catch (NotImplementedException) { /* launch backend pending */ }
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
