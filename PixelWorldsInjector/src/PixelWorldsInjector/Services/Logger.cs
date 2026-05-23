using System;
using System.IO;

namespace PixelWorldsInjector.Services;

/// <summary>
/// Minimal file logger that writes to %AppData%\PixelWorldsInjector\injector.log.
/// Thread-safe via lock on a static object.
/// </summary>
public static class Logger
{
    private static readonly object Sync = new();
    private static string? _logPath;

    public static string LogPath
    {
        get
        {
            if (_logPath is not null)
            {
                return _logPath;
            }

            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "PixelWorldsInjector");
            Directory.CreateDirectory(dir);
            _logPath = Path.Combine(dir, "injector.log");
            return _logPath;
        }
    }

    public static void Info(string message) => Write("INFO", message, null);
    public static void Warn(string message, Exception? ex = null) => Write("WARN", message, ex);
    public static void Error(string message, Exception? ex = null) => Write("ERROR", message, ex);

    private static void Write(string level, string message, Exception? ex)
    {
        var line = $"[{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {message}";
        if (ex is not null)
        {
            line += Environment.NewLine + ex;
        }

        lock (Sync)
        {
            try
            {
                File.AppendAllText(LogPath, line + Environment.NewLine);
            }
            catch
            {
                // Logging must never crash the app.
            }
        }
    }
}
