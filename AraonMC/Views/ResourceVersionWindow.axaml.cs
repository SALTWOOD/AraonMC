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

using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AraonMC.Views;

/// <summary>
/// Modal version-select dialog. Owned by the <c>ResourceVersionViewModel</c>; closing (X button, a pick,
/// or Alt+F4) ends <see cref="Avalonia.Controls.Window.ShowDialog"/> and lets the caller read the VM's
/// <c>SelectedVersion</c>.
/// </summary>
public partial class ResourceVersionWindow : Window
{
    public ResourceVersionWindow()
    {
        InitializeComponent();
    }

    private void Close_Click(object? sender, RoutedEventArgs e) => Close();
}
