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
