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
