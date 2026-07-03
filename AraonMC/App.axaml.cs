using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using AraonMC.Core.Infrastructure.Stub;
using AraonMC.Notifications;
using AraonMC.ViewModels;
using AraonMC.Views;

namespace AraonMC;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit.
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();

            // Compose stub backend services. Real implementations replace these later.
            var accounts = new StubAccountService();
            var instances = new StubInstanceRepository();
            var versions = new StubVersionRepository();
            var mods = new StubModRepository();
            var launcher = new StubGameLauncher();
            var notifications = new NotificationService();

            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(accounts, instances, versions, mods, launcher, notifications),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}
