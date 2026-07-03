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

    private InputDialog(string title, string placeholder)
    {
        InitializeComponent();
        DialogTitle.Text = title;
        InputBox.Watermark = placeholder;
    }

    private void Ok_Click(object? sender, RoutedEventArgs e)
    {
        var value = InputBox.Text?.Trim();
        Close(string.IsNullOrEmpty(value) ? null : value);
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(null);

    public static async Task<string?> PromptAsync(string title, string placeholder = "")
    {
        var owner = ResolveMainWindow();
        if (owner is null) return null;

        var dialog = new InputDialog(title, placeholder)
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
