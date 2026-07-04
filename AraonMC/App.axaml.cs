using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using AraonMC.Accounts;
using AraonMC.Auth;
using AraonMC.Core.Application.Notifications;
using AraonMC.Core.Config;
using AraonMC.Core.Domain.Enums;
using AraonMC.Core.Infrastructure.Stub;
using AraonMC.Instances;
using AraonMC.Notifications;
using AraonMC.ViewModels;
using AraonMC.Versions;
using AraonMC.Views;
// Alias (non-clashing name): the app's Config/ folder exposes namespace AraonMC.Config, which
// would shadow the bare name `Config` over the generated facade.
using CoreConfig = AraonMC.Core.Config.Config;
using TomlConfigStore = AraonMC.Config.TomlConfigStore;

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

            var notifications = new NotificationService();

            CoreConfig.Initialize(new TomlConfigStore(
                ConfigPaths.GlobalConfigFile(),
                ConfigPaths.InstancesConfigFile(),
                onWarning: msg => _ = notifications.ShowAsync(NotificationRequest.Toast(
                    "Config file reset", msg, NotificationLevel.Warning))));

            var deviceCodeUi = new AvaloniaDeviceCodeUI();
            var authenticator = new MinecraftAuthenticator(new MinecraftAuthOptions
            {
                ClientId = Secrets.MsOAuthClientId,
                DeviceCodeUI = deviceCodeUi,
                AccessTokenCacheTtl = null, // multi-account would otherwise cross-contaminate
                Logger = DebugLog.Info,
            });
            var accountStore = new JsonAccountStore(notifications);
            var accounts = new AccountService(authenticator, deviceCodeUi, accountStore);

            var http = new HttpClient();
            var mirror = CoreConfig.Download.Mirror;
            IVersionList versions = mirror == DownloadMirror.Bmclapi
                ? new BmclapiVersionList(http)
                : new OfficialVersionList(http);
            var installer = new VersionInstaller(versions, mirror, http);

            var instances = new JsonInstanceRepository(notifications);
            var mods = new StubModRepository();
            var launcher = new StubGameLauncher();

            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(accounts, instances, versions, installer, mods, launcher, notifications),
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
