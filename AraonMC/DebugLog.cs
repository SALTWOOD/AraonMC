using System.Diagnostics;

namespace AraonMC;

/// <summary>
/// Debug sink: mirrors every line to the console and <see cref="Debug"/> (IDE output window).
/// The app is WinExe with no console, so under the debugger look in the IDE output panel.
/// </summary>
public static class DebugLog
{
    public static void Info(string message)
    {
        var line = $"[AraonMC {DateTime.Now:HH:mm:ss.fff}] {message}";
        Console.WriteLine(line);
        Debug.WriteLine(line);
    }
}
