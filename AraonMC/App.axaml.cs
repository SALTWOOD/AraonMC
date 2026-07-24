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

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
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
            DebugLog.Info("App: framework initialized; wiring up services (DI composition root).");
            DebugLog.Info($"App: config root = '{ConfigPaths.GlobalRoot()}', default game directory = '{ConfigPaths.DefaultGameDirectory()}'.");
            // NB: ConfigPaths.GameDirectory() reads Config.Game.GameDirectory, so it can only be logged AFTER Initialize below.

            var notifications = new NotificationService();
            DebugLog.Info("App: NotificationService ready.");

            DebugLog.Info("App: initializing TOML config store...");
            CoreConfig.Initialize(new TomlConfigStore(
                ConfigPaths.GlobalConfigFile(),
                ConfigPaths.InstancesConfigFile(),
                onWarning: msg => _ = notifications.ShowAsync(NotificationRequest.Toast(
                    "Config file reset", msg, NotificationLevel.Warning))));
            DebugLog.Info($"App: config loaded — global='{ConfigPaths.GlobalConfigFile()}', instances='{ConfigPaths.InstancesConfigFile()}', active game directory='{ConfigPaths.GameDirectory()}'.");

            ThemeService.Initialize();
            ApplyThemeVariant();
            ThemeService.ColorModeChanged += (isDark, _) => ApplyThemeVariant();
            DebugLog.Info($"App: theme initialized — mode={CoreConfig.Theme.ColorMode}, theme={ThemeService.CurrentTheme}.");

            var deviceCodeUi = new AvaloniaDeviceCodeUI();
            var hasMsClient = !string.IsNullOrWhiteSpace(Secrets.MsOAuthClientId);
            DebugLog.Info($"App: MS OAuth client id configured = {hasMsClient}.");
            var authenticator = new MinecraftAuthenticator(new MinecraftAuthOptions
            {
                ClientId = Secrets.MsOAuthClientId,
                DeviceCodeUI = deviceCodeUi,
                AccessTokenCacheTtl = null, // multi-account would otherwise cross-contaminate
                Logger = DebugLog.Info,
            });
            var accountStore = new JsonAccountStore(notifications);
            var accounts = new AccountService(authenticator, deviceCodeUi, accountStore);
            DebugLog.Info($"App: AccountService ready — {accounts.Accounts.Count} account(s), active='{accounts.GetActive()?.Username ?? "(none)"}'.");

            var http = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
            DebugLog.Info($"App: HttpClient created (timeout={http.Timeout}).");
            var installer = InstallerFactory.Create(http);
            DebugLog.Info($"App: Minecraft installer created.");
            IVersionList versions = new MinecraftVersionCatalog(new MojangManifestParser(http));
            IVersionListService versionList = new VersionListService(http);
            var natives = new NativeLibraryExtractor(http);
            DebugLog.Info("App: version catalog + version list service + native extractor ready.");

            var instances = new JsonInstanceRepository(notifications);
            DebugLog.Info($"App: instance repository ready — {instances.GetAll().Count} instance(s).");

            var modrinth = new ModrinthClient(http);
            var curseForge = new CurseForgeClient(http, Secrets.CurseForgeApiKey);
            DebugLog.Info($"App: CurseForge API key configured = {!string.IsNullOrWhiteSpace(Secrets.CurseForgeApiKey)}.");
            var resources = new ResourceRepository(modrinth, curseForge, notifications);
            var launcher = new MinecraftGameLauncher(accounts, natives, notifications);
            var downloads = new DownloadManager(installer, natives, http, notifications);
            DebugLog.Info("App: resource repository, launcher, download manager wired up.");

            var window = new MainWindow();
            DebugLog.Info("App: main window created; registering storage-provider pickers.");
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
            window.DataContext = new MainWindowViewModel(accounts, instances, versions, versionList, downloads, resources, launcher, notifications, pickFolder, pickSaveFile, pickVersion);
            desktop.MainWindow = window;
            DebugLog.Info("App: main window data-bound and shown; startup complete. Ready.");
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void ApplyThemeVariant()
    { 
        Current!.RequestedThemeVariant = 
            ThemeService.IsDarkMode ? ThemeVariant.Dark : ThemeVariant.Light;
    }
}
