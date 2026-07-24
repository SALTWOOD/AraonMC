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
using System.Linq;
using AraonMC.Core.Domain.Entities;
using AraonMC.Core.Domain.Repositories;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AraonMC.ViewModels;

/// <summary>
/// Backs the install-confirmation window shown after picking a Minecraft version. Lets the user name the
/// instance (default = the version id) and pick a loader card. <see cref="StartInstall"/> closes the
/// window and exposes the chosen name via <see cref="Result"/>; the caller then installs vanilla with it.
/// Loader selection is a placeholder for now (no per-loader install path yet), so it isn't exposed.
/// </summary>
public partial class InstallConfirmViewModel : ObservableObject
{
    private readonly IInstanceRepository _repo;

    public MinecraftVersion Version { get; }

    /// <summary>Loader cards shown as a placeholder grid; selection is not wired to install logic yet.</summary>
    public IReadOnlyList<string> Loaders { get; } = ["Vanilla", "Forge", "NeoForge", "Fabric", "Quilt", "OptiFine"];

    [ObservableProperty] private string _name;
    [ObservableProperty] private bool _isNameDuplicate;
    [ObservableProperty] private string _selectedLoader = "Vanilla";

    /// <summary>The confirmed name when <see cref="StartInstall"/> fired; null if cancelled.</summary>
    public string? Result { get; private set; }

    /// <summary>Raised on confirm or cancel; the owner closes the window in response.</summary>
    public event Action? RequestClose;

    public InstallConfirmViewModel(MinecraftVersion version, IInstanceRepository repo)
    {
        Version = version;
        _repo = repo;
        _name = version.Id; // default name = version number.
        RecheckDuplicate();
    }

    partial void OnNameChanged(string value)
    {
        RecheckDuplicate();
        StartInstallCommand.NotifyCanExecuteChanged();
    }

    private void RecheckDuplicate()
    {
        var trimmed = Name?.Trim();
        IsNameDuplicate = !string.IsNullOrWhiteSpace(trimmed)
            && _repo.GetAll().Any(i => string.Equals(i.Name, trimmed, StringComparison.OrdinalIgnoreCase));
    }

    private bool CanStartInstall() => !string.IsNullOrWhiteSpace(Name) && !IsNameDuplicate;

    [RelayCommand(CanExecute = nameof(CanStartInstall))]
    private void StartInstall()
    {
        Result = Name.Trim();
        DebugLog.Info($"InstallConfirm: confirmed '{Result}' (loader={SelectedLoader}); caller will install vanilla.");
        RequestClose?.Invoke();
    }

    [RelayCommand]
    private void Cancel() => RequestClose?.Invoke();
}
