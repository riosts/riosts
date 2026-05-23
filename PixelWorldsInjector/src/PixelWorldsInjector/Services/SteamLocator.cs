using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Versioning;
using Microsoft.Win32;

namespace PixelWorldsInjector.Services;

/// <summary>
/// Locates the Pixel Worlds installation on disk by reading Steam registry keys
/// and parsing Steam's libraryfolders.vdf.
/// </summary>
[SupportedOSPlatform("windows")]
public static class SteamLocator
{
    private const int PixelWorldsAppId = 533020;
    private const string PixelWorldsFolderName = "Pixel Worlds";
    private const string PixelWorldsExeName = "PixelWorlds.exe";

    /// <summary>
    /// Returns full path to Steam install directory or null if Steam isn't installed.
    /// </summary>
    public static string? GetSteamPath()
    {
        // HKCU first (per-user install), then HKLM (system-wide).
        try
        {
            using var hkcu = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
            if (hkcu?.GetValue("SteamPath") is string userPath && Directory.Exists(userPath))
            {
                return userPath.Replace('/', Path.DirectorySeparatorChar);
            }
        }
        catch (Exception ex)
        {
            Logger.Warn("Failed reading HKCU Steam registry", ex);
        }

        foreach (var view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
        {
            try
            {
                using var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view);
                using var key = hklm.OpenSubKey(@"SOFTWARE\Valve\Steam") ?? hklm.OpenSubKey(@"SOFTWARE\Wow6432Node\Valve\Steam");
                if (key?.GetValue("InstallPath") is string installPath && Directory.Exists(installPath))
                {
                    return installPath.Replace('/', Path.DirectorySeparatorChar);
                }
            }
            catch (Exception ex)
            {
                Logger.Warn($"Failed reading HKLM Steam registry ({view})", ex);
            }
        }

        return null;
    }

    /// <summary>
    /// Attempts to auto-locate PixelWorlds.exe across all Steam library folders.
    /// Returns null if not found.
    /// </summary>
    public static string? FindPixelWorldsExe()
    {
        var steamPath = GetSteamPath();
        if (steamPath is null)
        {
            return null;
        }

        foreach (var library in EnumerateSteamLibraries(steamPath))
        {
            var candidate = Path.Combine(library, "steamapps", "common", PixelWorldsFolderName, PixelWorldsExeName);
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        // Last-ditch: check the manifest acf file to learn the real folder name.
        foreach (var library in EnumerateSteamLibraries(steamPath))
        {
            var manifest = Path.Combine(library, "steamapps", $"appmanifest_{PixelWorldsAppId}.acf");
            if (!File.Exists(manifest))
            {
                continue;
            }

            var installDir = ParseAcfString(File.ReadAllText(manifest), "installdir");
            if (string.IsNullOrEmpty(installDir))
            {
                continue;
            }

            var exe = Path.Combine(library, "steamapps", "common", installDir, PixelWorldsExeName);
            if (File.Exists(exe))
            {
                return exe;
            }
        }

        return null;
    }

    private static IEnumerable<string> EnumerateSteamLibraries(string steamPath)
    {
        yield return steamPath;

        var vdfPath = Path.Combine(steamPath, "steamapps", "libraryfolders.vdf");
        if (!File.Exists(vdfPath))
        {
            yield break;
        }

        string content;
        try
        {
            content = File.ReadAllText(vdfPath);
        }
        catch (Exception ex)
        {
            Logger.Warn($"Failed reading {vdfPath}", ex);
            yield break;
        }

        // Very lightweight VDF parsing: look for `"path"  "<value>"` lines.
        var lines = content.Split('\n');
        foreach (var raw in lines)
        {
            var line = raw.Trim();
            if (!line.StartsWith("\"path\"", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // Format: "path"		"C:\\SteamLibrary"
            var parts = line.Split('"');
            if (parts.Length >= 5)
            {
                var path = parts[3].Replace("\\\\", "\\").Replace('/', Path.DirectorySeparatorChar);
                if (Directory.Exists(path))
                {
                    yield return path;
                }
            }
        }
    }

    private static string? ParseAcfString(string content, string key)
    {
        var needle = $"\"{key}\"";
        var idx = content.IndexOf(needle, StringComparison.OrdinalIgnoreCase);
        if (idx < 0)
        {
            return null;
        }

        idx = content.IndexOf('"', idx + needle.Length);
        if (idx < 0)
        {
            return null;
        }

        var end = content.IndexOf('"', idx + 1);
        if (end < 0)
        {
            return null;
        }

        return content.Substring(idx + 1, end - idx - 1);
    }
}
