using System;
using System.IO;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using PixelWorldsInjector.Models;

namespace PixelWorldsInjector.Services;

/// <summary>
/// Manages GoldBerg Steam Emulator integration for Pixel Worlds.
///
/// GoldBerg (https://gitlab.com/Mr_Goldberg/goldberg_emulator and its forks) is a
/// drop-in <c>steam_api64.dll</c> replacement that provides a fake-but-format-valid
/// Steamworks SDK. It is needed for games like Pixel Worlds whose servers require a
/// non-empty Steam auth ticket before they will let the client connect to the game
/// world server. The classic <c>steam_appid.txt</c> trick on its own is not enough.
///
/// The emulator is NOT bundled with this project. The user must download it from
/// upstream and point <see cref="AppSettings.GoldbergDllPath"/> at the resulting DLL.
///
/// Install model:
///   - The original <c>steam_api64.dll</c> shipped with the game is renamed to
///     <c>steam_api64.original.dll</c> (only on first install).
///   - The user-supplied GoldBerg DLL is copied in as <c>steam_api64.dll</c>.
///   - <c>Restore</c> reverses the rename.
/// Per-instance settings (account name, SteamID) are written into the game's
/// <c>steam_settings/</c> directory right before launching.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class SteamEmu
{
    private const string BackupSuffix = ".original.dll";
    private static readonly string[] CandidateDllNames = { "steam_api64.dll", "steam_api.dll" };

    public sealed record InstallStatus(bool Installed, string? OriginalDllPath, string? ActiveDllPath, string? Detail);

    /// <summary>
    /// Inspect the game directory and report whether GoldBerg appears to be installed.
    ///
    /// We consider it installed when:
    ///  1. A <c>steam_api64.original.dll</c> backup is present (proof of an earlier install), AND
    ///  2. The current <c>steam_api64.dll</c> contains a GoldBerg signature string.
    /// </summary>
    public static InstallStatus GetStatus(string gameExePath)
    {
        if (string.IsNullOrWhiteSpace(gameExePath) || !File.Exists(gameExePath))
        {
            return new InstallStatus(false, null, null, "PixelWorlds.exe path not configured.");
        }

        var gameDir = Path.GetDirectoryName(gameExePath)!;
        var (activeDll, backupDll) = LocateDllPair(gameDir);
        if (activeDll is null)
        {
            return new InstallStatus(false, null, null, "Could not find steam_api64.dll inside the game directory.");
        }

        var backupExists = backupDll is not null && File.Exists(backupDll);
        var isGoldberg = LooksLikeGoldberg(activeDll);
        return new InstallStatus(
            Installed: backupExists && isGoldberg,
            OriginalDllPath: backupDll,
            ActiveDllPath: activeDll,
            Detail: $"active={Path.GetFileName(activeDll)} (goldberg={isGoldberg}) backup={(backupExists ? "yes" : "no")}");
    }

    /// <summary>
    /// Back up the original Steam API DLL (if not already backed up) and copy the
    /// user-supplied GoldBerg DLL in its place.
    /// </summary>
    /// <returns>The active DLL path after installation.</returns>
    public static string Install(string gameExePath, string goldbergDllPath)
    {
        if (!File.Exists(goldbergDllPath))
        {
            throw new FileNotFoundException("GoldBerg steam_api64.dll path is invalid or file is missing.", goldbergDllPath);
        }

        var gameDir = Path.GetDirectoryName(gameExePath) ?? throw new ArgumentException("Could not derive game directory.", nameof(gameExePath));
        var (activeDll, backupDll) = LocateDllPair(gameDir);
        if (activeDll is null || backupDll is null)
        {
            throw new FileNotFoundException("Game directory does not contain steam_api64.dll - this might not be a Steam build of the game.", Path.Combine(gameDir, "steam_api64.dll"));
        }

        if (!File.Exists(backupDll))
        {
            if (LooksLikeGoldberg(activeDll))
            {
                throw new InvalidOperationException(
                    $"The current {Path.GetFileName(activeDll)} already looks like GoldBerg but no original backup exists. " +
                    "Reinstall the game via Steam (Properties → Installed Files → Verify integrity) to restore the real Steam API DLL, then try Install again.");
            }
            File.Move(activeDll, backupDll, overwrite: false);
            Logger.Info($"Backed up original {Path.GetFileName(activeDll)} to {Path.GetFileName(backupDll)}");
        }
        else
        {
            Logger.Info($"Backup {Path.GetFileName(backupDll)} already exists - leaving it untouched.");
        }

        File.Copy(goldbergDllPath, activeDll, overwrite: true);
        try
        {
            File.SetAttributes(activeDll, FileAttributes.Normal);
        }
        catch
        {
            // Best-effort attribute clear; not fatal.
        }
        Logger.Info($"Installed GoldBerg {Path.GetFileName(activeDll)} from {goldbergDllPath}");
        return activeDll;
    }

    /// <summary>
    /// Restore the original Steam API DLL from the <c>*.original.dll</c> backup, if present.
    /// Throws when no backup is found.
    /// </summary>
    public static void Restore(string gameExePath)
    {
        var gameDir = Path.GetDirectoryName(gameExePath) ?? throw new ArgumentException("Could not derive game directory.", nameof(gameExePath));
        var (activeDll, backupDll) = LocateDllPair(gameDir);
        if (activeDll is null || backupDll is null || !File.Exists(backupDll))
        {
            throw new FileNotFoundException("No GoldBerg backup found. The game's steam_api64.dll has not been replaced by this tool.", backupDll ?? "<unknown>");
        }

        // Replace active with backup.
        if (File.Exists(activeDll))
        {
            File.Delete(activeDll);
        }
        File.Move(backupDll, activeDll);
        Logger.Info($"Restored {Path.GetFileName(activeDll)} from backup.");
    }

    /// <summary>
    /// Write the per-instance GoldBerg settings into <c>{gameDir}/steam_settings/</c>.
    /// These files are read by GoldBerg at process startup, so they must be written
    /// before each launch.
    /// </summary>
    public static void WriteInstanceSettings(string gameExePath, Instance instance)
    {
        var gameDir = Path.GetDirectoryName(gameExePath) ?? throw new ArgumentException("Could not derive game directory.", nameof(gameExePath));
        var settingsDir = Path.Combine(gameDir, "steam_settings");
        Directory.CreateDirectory(settingsDir);

        var accountName = string.IsNullOrWhiteSpace(instance.SteamAccountName) ? instance.Name : instance.SteamAccountName;
        var steamId = string.IsNullOrWhiteSpace(instance.SteamId) ? DeriveSteamId(instance.Id) : instance.SteamId.Trim();

        // GoldBerg honors these files: the values it returns from SteamUser()->GetPersonaName(),
        // GetSteamID(), etc., come from these.
        File.WriteAllText(Path.Combine(settingsDir, "account_name.txt"), accountName);
        File.WriteAllText(Path.Combine(settingsDir, "force_account_name.txt"), accountName);
        File.WriteAllText(Path.Combine(settingsDir, "user_steam_id.txt"), steamId);
        File.WriteAllText(Path.Combine(settingsDir, "force_steam_id.txt"), steamId);
        Logger.Info($"Wrote GoldBerg per-instance settings (name='{accountName}' steamid={steamId}) to {settingsDir}");
    }

    /// <summary>
    /// Derive a stable 17-digit SteamID64 from an instance id, so each instance keeps the
    /// same identity across launches without the user having to supply one.
    /// </summary>
    private static string DeriveSteamId(string instanceId)
    {
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(instanceId));
        // Take the first 8 bytes as a little-endian ulong, mask to 32 bits (low account id),
        // and combine with the standard individual / public-universe prefix (76561197960265728).
        // This produces a "looks normal" SteamID64 that's deterministic per instance.
        ulong accountId = BitConverter.ToUInt32(hashBytes, 0);
        const ulong SteamIdBase = 76561197960265728UL;
        var steamId64 = SteamIdBase + accountId;
        return steamId64.ToString();
    }

    private static (string? activeDll, string? backupDll) LocateDllPair(string gameDir)
    {
        foreach (var name in CandidateDllNames)
        {
            var candidate = Path.Combine(gameDir, name);
            if (File.Exists(candidate) || File.Exists(Path.ChangeExtension(candidate, null) + BackupSuffix))
            {
                var backup = Path.ChangeExtension(candidate, null) + BackupSuffix;
                return (candidate, backup);
            }
        }
        return (null, null);
    }

    /// <summary>
    /// Heuristic detection: read the first ~512KB of the DLL and look for any of a few
    /// known GoldBerg / Steam-emu strings. Cheap and good enough to tell a real Valve
    /// <c>steam_api64.dll</c> from a GoldBerg one without parsing PE metadata.
    /// </summary>
    private static bool LooksLikeGoldberg(string dllPath)
    {
        try
        {
            using var fs = File.OpenRead(dllPath);
            var len = (int)Math.Min(fs.Length, 512 * 1024);
            var buffer = new byte[len];
            var read = fs.Read(buffer, 0, len);
            var text = Encoding.ASCII.GetString(buffer, 0, read);
            string[] markers =
            {
                "Goldberg SteamEmu",
                "goldberg_emulator",
                "Mr_Goldberg",
                "force_account_name.txt",
                "GBE_FORK",
                "gbe_fork",
            };
            foreach (var m in markers)
            {
                if (text.IndexOf(m, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }
            return false;
        }
        catch (Exception ex)
        {
            Logger.Warn($"Failed to inspect DLL '{dllPath}'", ex);
            return false;
        }
    }
}
