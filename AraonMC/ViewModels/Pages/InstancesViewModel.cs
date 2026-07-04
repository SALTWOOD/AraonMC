using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using AraonMC.Core.Application.Ports;
using AraonMC.Core.Domain.Entities;
using AraonMC.Core.Domain.Repositories;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AraonMC.ViewModels.Pages;

public partial class InstancesViewModel : PageViewModelBase
{
    private readonly IInstanceRepository _repo;
    private readonly IGameLauncher _launcher;
    private readonly IAccountService _accounts;
    private readonly Action _showVersionSelect;
    private readonly List<GameInstance> _all;

    public InstancesViewModel(
        IInstanceRepository repo,
        IGameLauncher launcher,
        IAccountService accounts,
        Action showVersionSelect)
    {
        _repo = repo;
        _launcher = launcher;
        _accounts = accounts;
        _showVersionSelect = showVersionSelect;
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
        await _launcher.LaunchAsync(instance, _accounts.GetActive()!);
    }

    [RelayCommand]
    private void New() => _showVersionSelect();

    partial void OnSearchTextChanged(string value)
    {
        var q = value.Trim();
        Items.Clear();
        foreach (var i in _all.Where(x => x.Name.Contains(q, StringComparison.OrdinalIgnoreCase)))
            Items.Add(i);
    }
}
