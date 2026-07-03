using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AraonMC.Core.Application.Ports;
using AraonMC.Core.Domain.Entities;
using AraonMC.Core.Domain.Enums;
using AraonMC.Core.Domain.Repositories;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AraonMC.ViewModels.Pages;

public partial class InstancesViewModel : PageViewModelBase
{
    private readonly IInstanceRepository _repo;
    private readonly IGameLauncher _launcher;
    private readonly IAccountService _accounts;
    private readonly List<GameInstance> _all;

    public InstancesViewModel(IInstanceRepository repo, IGameLauncher launcher, IAccountService accounts)
    {
        _repo = repo;
        _launcher = launcher;
        _accounts = accounts;
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
        try { await _repo.CreateAsync("New Instance", new MinecraftVersion { Id = "1.21.4" }, LoaderType.Vanilla); }
        catch (NotImplementedException) { /* backend pending */ }
    }

    partial void OnSearchTextChanged(string value)
    {
        var q = value.Trim();
        Items.Clear();
        foreach (var i in _all.Where(x => x.Name.Contains(q, StringComparison.OrdinalIgnoreCase)))
            Items.Add(i);
    }
}
