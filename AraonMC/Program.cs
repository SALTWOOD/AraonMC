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