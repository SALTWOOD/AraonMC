using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using AraonMC.Accounts;
using AraonMC.Auth;
using AraonMC.Core.Application.Notifications;
using AraonMC.Core.Config;
using AraonMC.Core.Domain.Entities;
using AraonMC.Core.Infrastructure.Catalog;
using AraonMC.Downloads;
using AraonMC.Instances;
using AraonMC.Launching;
using AraonMC.Notifications;
using AraonMC.Styles;
using AraonMC.UI.Theme;
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
        // Lucide 图标（Assets/Icons/lucide/*.svg）→ Geometry 资源，按 slug 注册。
        var n = 0;
        foreach (var (slug, geom) in LucideIconLoader.Load())
        {
            Resources[slug] = geom;
            n++;
        }
        DebugLog.Info($"Icons: loaded {n} lucide icon(s) from Assets/Icons/lucide.");
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var notifications = new NotificationService();

            CoreConfig.Initialize(new TomlConfigStore(
                ConfigPaths.GlobalConfigFile(),
                ConfigPaths.InstancesConfigFile(),
                onWarning: msg => _ = notifications.ShowAsync(NotificationRequest.Toast(
                    "Config file reset", msg, NotificationLevel.Warning))));

            ThemeService.Initialize();

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
            var modrinth = new ModrinthClient(http);
            var curseForge = new CurseForgeClient(http, Secrets.CurseForgeApiKey);
            var resources = new ResourceRepository(modrinth, curseForge, notifications);
            var launcher = new MinecraftGameLauncher(accounts, natives, notifications);
            var downloads = new DownloadManager(installer, natives, http, notifications);

            var window = new MainWindow();
            Func<Task<string?>> pickFolder = async () =>
            {
                var result = await window.StorageProvider.OpenFolderPickerAsync(
                    new FolderPickerOpenOptions { Title = "Select .minecraft folder" });
                return result.Count > 0 ? result[0].Path.LocalPath : null;
            };
            Func<string?, Task<string?>> pickSaveFile = async suggestedName =>
            {
                var file = await window.StorageProvider.SaveFilePickerAsync(
                    new FilePickerSaveOptions
                    {
                        Title = "Save file",
                        SuggestedFileName = string.IsNullOrEmpty(suggestedName) ? null : suggestedName,
                        ShowOverwritePrompt = true,
                    });
                return file?.Path.LocalPath;
            };
            Func<ResourceInfo, Task<ResourceVersion?>> pickVersion = async resource =>
            {
                var vm = new ResourceVersionViewModel(resource, resources);
                var versionWindow = new ResourceVersionWindow { DataContext = vm };
                vm.RequestClose += () => versionWindow.Close();
                await versionWindow.ShowDialog(window);
                return vm.SelectedVersion;
            };
            window.DataContext = new MainWindowViewModel(accounts, instances, versions, downloads, resources, launcher, notifications, pickFolder, pickSaveFile, pickVersion);
            desktop.MainWindow = window;
        }

        base.OnFrameworkInitializationCompleted();
    }
}
