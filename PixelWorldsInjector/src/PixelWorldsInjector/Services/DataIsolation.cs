using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace PixelWorldsInjector.Services;

/// <summary>
/// Manages per-instance isolation of Unity persistent data (the <c>%LocalAppData%Low</c>
/// folder Pixel Worlds writes save data into).
///
/// Approach (junction swap):
///   1. Pixel Worlds (Unity) reads/writes data under
///      %USERPROFILE%\AppData\LocalLow\Kukouri\Pixel Worlds.
///   2. Before launching instance X, we move the real LocalLow\Kukouri folder aside
///      (if present and not already a junction) and then create a junction at that
///      path pointing to the per-instance folder under
///      %AppData%\PixelWorldsInjector\instances\{id}\LocalLow.
///   3. The game now reads/writes into the per-instance folder.
///
/// Limitations:
///   - Junctions are filesystem-global. Only one instance can be "active" via
///     junction at any moment. If you want two instances running concurrently with
///     separate data, you must rely on the in-game login form (mutex bypass) and
///     accept that they share Unity PlayerPrefs / cache. This is the simplest
///     non-kernel-driver approach. True parallel filesystem virtualization would
///     require a Sandboxie-style kernel driver or DLL-injection-based API hooks.
///
/// This module is intentionally conservative: it never deletes the user's real
/// save folder. On <see cref="Restore"/>, it removes only the junction it created
/// and moves the backup back into place.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class DataIsolation
{
    private const string KukouriRelative = @"Kukouri\Pixel Worlds";

    private readonly string _instanceDataRoot;
    private readonly string _realLocalLow;
    private string? _activeJunction;
    private string? _activeBackup;

    public DataIsolation(string instanceDataDirectory)
    {
        _instanceDataRoot = instanceDataDirectory;
        var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        _realLocalLow = Path.Combine(profile, "AppData", "LocalLow", KukouriRelative);
    }

    public string InstanceLocalLow => Path.Combine(_instanceDataRoot, "LocalLow", KukouriRelative);

    public void Apply()
    {
        Directory.CreateDirectory(InstanceLocalLow);
        var parent = Path.GetDirectoryName(_realLocalLow)!;
        Directory.CreateDirectory(parent);

        // If the real path already exists and is NOT a reparse point, back it up.
        if (Directory.Exists(_realLocalLow))
        {
            var info = new DirectoryInfo(_realLocalLow);
            if ((info.Attributes & FileAttributes.ReparsePoint) != 0)
            {
                Directory.Delete(_realLocalLow);
            }
            else
            {
                var backup = _realLocalLow + ".pwinjector.bak." + Guid.NewGuid().ToString("N").Substring(0, 8);
                Directory.Move(_realLocalLow, backup);
                _activeBackup = backup;
                Logger.Info($"Backed up real save folder to {backup}");
            }
        }

        if (!CreateJunction(_realLocalLow, InstanceLocalLow))
        {
            throw new InvalidOperationException($"Failed to create junction from {_realLocalLow} to {InstanceLocalLow}");
        }

        _activeJunction = _realLocalLow;
        Logger.Info($"Junction active: {_realLocalLow} -> {InstanceLocalLow}");
    }

    public void Restore()
    {
        if (_activeJunction is not null && Directory.Exists(_activeJunction))
        {
            try
            {
                Directory.Delete(_activeJunction);
                Logger.Info($"Removed junction {_activeJunction}");
            }
            catch (Exception ex)
            {
                Logger.Warn($"Failed to remove junction {_activeJunction}", ex);
            }
        }

        if (_activeBackup is not null && Directory.Exists(_activeBackup) && !Directory.Exists(_realLocalLow))
        {
            try
            {
                Directory.Move(_activeBackup, _realLocalLow);
                Logger.Info($"Restored real save folder from {_activeBackup}");
            }
            catch (Exception ex)
            {
                Logger.Warn($"Failed to restore real save folder from {_activeBackup}", ex);
            }
        }

        _activeJunction = null;
        _activeBackup = null;
    }

    /// <summary>
    /// Creates a directory junction using the Win32 CreateSymbolicLinkW API.
    /// Falls back to invoking <c>cmd /c mklink /J</c> if direct creation fails
    /// (e.g. when the process lacks SeCreateSymbolicLinkPrivilege without admin).
    /// </summary>
    private static bool CreateJunction(string linkPath, string targetPath)
    {
        const int SYMBOLIC_LINK_FLAG_DIRECTORY = 0x1;
        const int SYMBOLIC_LINK_FLAG_ALLOW_UNPRIVILEGED_CREATE = 0x2;

        if (CreateSymbolicLinkW(linkPath, targetPath, SYMBOLIC_LINK_FLAG_DIRECTORY | SYMBOLIC_LINK_FLAG_ALLOW_UNPRIVILEGED_CREATE))
        {
            return true;
        }

        // Fallback: shell out to mklink (only runs from cmd.exe builtin).
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo("cmd.exe", $"/c mklink /J \"{linkPath}\" \"{targetPath}\"")
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            using var proc = System.Diagnostics.Process.Start(psi);
            if (proc is null)
            {
                return false;
            }

            proc.WaitForExit(5000);
            return proc.ExitCode == 0 && Directory.Exists(linkPath);
        }
        catch (Exception ex)
        {
            Logger.Warn("mklink fallback failed", ex);
            return false;
        }
    }

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "CreateSymbolicLinkW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CreateSymbolicLinkW(string lpSymlinkFileName, string lpTargetFileName, int dwFlags);
}
