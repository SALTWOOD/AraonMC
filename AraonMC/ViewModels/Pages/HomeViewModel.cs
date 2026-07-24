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
        if (SelectedInstance is null)
        {
            DebugLog.Info("Home: Play pressed but no instance is selected.");
            return;
        }
        var active = _accounts.GetActive();
        DebugLog.Info($"Home: Play '{SelectedInstance.Name}' (version='{SelectedInstance.MinecraftVersion}') with account '{active?.Username ?? "(none)"}'.");
        await _launcher.LaunchAsync(SelectedInstance, active!);
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
