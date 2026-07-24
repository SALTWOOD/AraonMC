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

/// <summary>Modal text-prompt dialog (e.g. offline-account username).</summary>
public partial class InputDialog : Window
{
    public InputDialog()
    {
        InitializeComponent();
    }

    private InputDialog(string title, string placeholder, string defaultValue)
    {
        InitializeComponent();
        DialogTitle.Text = title;
        InputBox.Watermark = placeholder;
        InputBox.Text = defaultValue;
    }

    private void Ok_Click(object? sender, RoutedEventArgs e)
    {
        var value = InputBox.Text?.Trim();
        Close(string.IsNullOrEmpty(value) ? null : value);
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(null);

    public static async Task<string?> PromptAsync(string title, string placeholder = "", string defaultValue = "")
    {
        var owner = ResolveMainWindow();
        if (owner is null) return null;

        var dialog = new InputDialog(title, placeholder, defaultValue)
        {
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };
        return await dialog.ShowDialog<string?>(owner);
    }

    private static Window? ResolveMainWindow() =>
        Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null;
}
