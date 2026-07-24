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

using System.Threading.Tasks;
using AraonMC.Core.Domain.Entities;
using AraonMC.Core.Domain.Repositories;
using AraonMC.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;

namespace AraonMC.Views;

/// <summary>
/// Modal install-confirmation dialog shown after picking a Minecraft version. Card-styled like the resource
/// version-select window. Returns the chosen instance name via <see cref="ShowAsync"/> (null if cancelled);
/// the caller installs vanilla with it. Loader selection is a visual placeholder.
/// </summary>
public partial class InstallConfirmWindow : Window
{
    public InstallConfirmWindow()
    {
        InitializeComponent();
    }

    private void Close_Click(object? sender, RoutedEventArgs e) => Close();

    /// <summary>Opens the dialog modal to the main window; returns the confirmed instance name, or null if cancelled.</summary>
    public static async Task<string?> ShowAsync(MinecraftVersion version, IInstanceRepository repo)
    {
        var owner = ResolveMainWindow();
        if (owner is null) return null;

        var vm = new InstallConfirmViewModel(version, repo);
        var window = new InstallConfirmWindow { DataContext = vm };
        vm.RequestClose += () => window.Close();
        await window.ShowDialog(owner);
        return vm.Result;
    }

    private static Window? ResolveMainWindow() =>
        Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null;
}
