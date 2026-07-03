using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using AraonMC.Core.Domain.Entities;
using AraonMC.Core.Domain.Repositories;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AraonMC.ViewModels.Pages;

public partial class ModsViewModel : PageViewModelBase
{
    private readonly IModRepository _repo;
    private readonly List<ModInfo> _all;

    public ModsViewModel(IModRepository repo)
    {
        _repo = repo;
        _all = [.. repo.SearchAsync(string.Empty).GetAwaiter().GetResult()];

        Title = "Mods";
        Items = new ObservableCollection<ModInfo>(_all);
    }

    public ObservableCollection<ModInfo> Items { get; }

    [ObservableProperty] private string _searchText = string.Empty;

    [RelayCommand]
    private void Install(ModInfo? mod)
    {
        if (mod is null) return;
        mod.Installed = true; // UI-only toggle; real install backend pending
    }

    partial void OnSearchTextChanged(string value)
    {
        var q = value.Trim();
        Items.Clear();
        foreach (var m in _all.Where(x => string.IsNullOrEmpty(q)
                                         || x.Name.Contains(q, StringComparison.OrdinalIgnoreCase)
                                         || x.Category.Contains(q, StringComparison.OrdinalIgnoreCase)))
            Items.Add(m);
    }
}
