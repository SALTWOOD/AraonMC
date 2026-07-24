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
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;

namespace AraonMC.Accounts;

/// <summary>Modal yes/no confirm dialog. Returns <c>true</c> when the confirm button is clicked.</summary>
public partial class ConfirmDialog : Window
{
    public ConfirmDialog()
    {
        InitializeComponent();
    }

    private ConfirmDialog(string title, string message, string confirmLabel)
    {
        InitializeComponent();
        DialogTitle.Text = title;
        DialogMessage.Text = message;
        ConfirmLabel.Text = confirmLabel;
    }

    private void Confirm_Click(object? sender, RoutedEventArgs e) => Close(true);

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(false);

    /// <summary>Opens the dialog modal to the main window. Returns the user's choice, or <c>false</c> if dismissed.</summary>
    public static async Task<bool> ShowAsync(string title, string message, string confirmLabel = "Confirm")
    {
        var owner = ResolveMainWindow();
        if (owner is null) return false;

        var dialog = new ConfirmDialog(title, message, confirmLabel)
        {
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        return await dialog.ShowDialog<bool>(owner);
    }

    private static Window? ResolveMainWindow() =>
        Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null;
}
