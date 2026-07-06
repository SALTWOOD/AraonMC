using Avalonia;
using System;
using System.Runtime.InteropServices;

namespace AraonMC;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        DebugLog.Info("Application starting!");
        DebugLog.Info($"Runtime: {RuntimeInformation.FrameworkDescription}.");
        DebugLog.Info($"OS: {RuntimeInformation.OSDescription}; OS arch={RuntimeInformation.OSArchitecture}, process arch={RuntimeInformation.ProcessArchitecture}.");
        DebugLog.Info($"Command-line args ({args.Length}): {(args.Length == 0 ? "(none)" : string.Join(' ', args))}.");

        try
        {
            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            DebugLog.Error($"Fatal: unhandled exception brought down the app — {ex.GetType().FullName}: {ex.Message}");
            DebugLog.Error($"Stack trace:\n{ex.StackTrace}");
            throw;
        }
        finally
        {
            DebugLog.Info("Application main loop exited.");
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}