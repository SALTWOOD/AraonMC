using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using AraonMC.Accounts;
using AraonMC.Auth;
using AraonMC.Core.Application.Notifications;
using AraonMC.Core.Config;
using AraonMC.Core.Infrastructure.Stub;
using AraonMC.Downloads;
using AraonMC.Instances;
using AraonMC.Launching;
using AraonMC.Notifications;
using AraonMC.ViewModels;
using AraonMC.Views;
using MinecraftDownloader.Core.Manifest;
using MinecraftDownloader.Core.Orchestration;
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

            var http = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
            var installer = InstallerFactory.Create(http);
            IVersionList versions = new MinecraftVersionCatalog(new MojangManifestParser(http));
            var natives = new NativeLibraryExtractor(http);

            var instances = new JsonInstanceRepository(notifications);
            var mods = new StubModRepository();
            var launcher = new MinecraftGameLauncher(accounts, notifications);
            var downloads = new DownloadManager(installer, natives, notifications);

            var window = new MainWindow();
            Func<Task<string?>> pickFolder = async () =>
            {
                var result = await window.StorageProvider.OpenFolderPickerAsync(
                    new FolderPickerOpenOptions { Title = "Select .minecraft folder" });
                return result.Count > 0 ? result[0].Path.LocalPath : null;
            };
            window.DataContext = new MainWindowViewModel(accounts, instances, versions, downloads, mods, launcher, notifications, pickFolder);
            desktop.MainWindow = window;
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
