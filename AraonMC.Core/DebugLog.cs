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

using System;
using System.Diagnostics;

// Intentionally in the root `AraonMC` namespace (not AraonMC.Core) so existing app callers
// (AraonMC.Accounts, AraonMC.Launching, App, …) resolve it unchanged via namespace nesting, and
// Core adapters (AraonMC.Core.Infrastructure.*) reach it the same way. Lives in this assembly so the
// catalog clients / repository can log network activity alongside the rest of the app.

namespace AraonMC;

/// <summary>
/// Debug sink: mirrors every line to the console and <see cref="Debug"/> (IDE output window). The app is
/// WinExe with no console, so under the debugger look in the IDE output panel.
/// </summary>
public static class DebugLog
{
    public static void Info(string message) => Write("INFO", message);

    public static void Warn(string message) => Write("WARN", message);

    public static void Error(string message) => Write("ERROR", message);

    private static void Write(string level, string message)
    {
        var line = $"[AraonMC {DateTime.Now:HH:mm:ss.fff}] [{level}] {message}";
        Console.WriteLine(line);
        Debug.WriteLine(line);
    }
}
