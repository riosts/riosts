using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
///   - Any companion files in the source folder (e.g. <c>steamclient64.dll</c>) are
///     copied to the same target directory; existing copies are backed up with the
///     same <c>.original.dll</c> suffix.
///   - <c>Restore</c> reverses everything.
/// Per-instance settings (account name, SteamID) are written into a
/// <c>steam_settings/</c> directory next to the active DLL just before launch.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class SteamEmu
{
    private const string BackupSuffix = ".original.dll";
    private static readonly string[] CandidateDllNames = { "steam_api64.dll", "steam_api.dll" };
    private static readonly string[] CompanionDllNames = { "steamclient64.dll", "steamclient.dll" };

    public sealed record InstallStatus(bool Installed, string? OriginalDllPath, string? ActiveDllPath, string? Detail);

    /// <summary>
    /// Inspect the game directory and report whether GoldBerg appears to be installed.
    ///
    /// We consider it installed when:
    ///  1. A <c>*.original.dll</c> backup of the Steam API DLL is present (proof of an earlier install), AND
    ///  2. The current Steam API DLL contains a GoldBerg signature string.
    /// </summary>
    public static InstallStatus GetStatus(string gameExePath)
    {
        if (string.IsNullOrWhiteSpace(gameExePath) || !File.Exists(gameExePath))
        {
            return new InstallStatus(false, null, null, "PixelWorlds.exe path not configured.");
        }

        var gameDir = Path.GetDirectoryName(gameExePath)!;
        var pair = LocateDllPair(gameDir);
        if (pair is null)
        {
            return new InstallStatus(false, null, null, "Could not find steam_api64.dll inside the game directory.");
        }

        var backupExists = File.Exists(pair.BackupPath);
        var activeExists = File.Exists(pair.ActivePath);
        var isGoldberg = activeExists && LooksLikeGoldberg(pair.ActivePath);
        var rel = Path.GetRelativePath(gameDir, pair.ActivePath);
        return new InstallStatus(
            Installed: backupExists && isGoldberg,
            OriginalDllPath: pair.BackupPath,
            ActiveDllPath: pair.ActivePath,
            Detail: $"active='{rel}' (goldberg={isGoldberg}) backup={(backupExists ? "yes" : "no")}");
    }

    /// <summary>
    /// Back up the original Steam API DLL (if not already backed up), copy the
    /// user-supplied GoldBerg DLL in its place, and do the same for any companion
    /// DLLs (e.g. <c>steamclient64.dll</c>) found in the GoldBerg source folder.
    /// </summary>
    /// <returns>The active DLL path after installation.</returns>
    public static string Install(string gameExePath, string goldbergDllPath)
    {
        if (!File.Exists(goldbergDllPath))
        {
            throw new FileNotFoundException("GoldBerg steam_api64.dll path is invalid or file is missing.", goldbergDllPath);
        }

        var gameDir = Path.GetDirectoryName(gameExePath) ?? throw new ArgumentException("Could not derive game directory.", nameof(gameExePath));
        var pair = LocateDllPair(gameDir)
            ?? throw new FileNotFoundException(
                "Could not find steam_api64.dll inside the game directory (searched root and all subfolders). " +
                "This might not be a Steam build of the game, or the game files are incomplete - verify integrity via Steam.",
                Path.Combine(gameDir, "steam_api64.dll"));

        // 1. Main DLL swap.
        if (!File.Exists(pair.BackupPath))
        {
            if (File.Exists(pair.ActivePath) && LooksLikeGoldberg(pair.ActivePath))
            {
                throw new InvalidOperationException(
                    $"The current {Path.GetFileName(pair.ActivePath)} already looks like GoldBerg but no original backup exists. " +
                    "Reinstall the game via Steam (Properties → Installed Files → Verify integrity) to restore the real Steam API DLL, then try Install again.");
            }
            if (File.Exists(pair.ActivePath))
            {
                File.Move(pair.ActivePath, pair.BackupPath, overwrite: false);
                Logger.Info($"Backed up original {pair.ActivePath} to {Path.GetFileName(pair.BackupPath)}");
            }
        }
        else
        {
            Logger.Info($"Backup {pair.BackupPath} already exists - leaving it untouched.");
        }

        CopyFileNormalized(goldbergDllPath, pair.ActivePath);
        Logger.Info($"Installed GoldBerg DLL into {pair.ActivePath}");

        // 2. Companion DLLs (e.g. steamclient64.dll lives alongside steam_api64.dll in
        //    the GoldBerg experimental release and is needed for full Steam emulation).
        var sourceDir = Path.GetDirectoryName(goldbergDllPath)!;
        var targetDir = Path.GetDirectoryName(pair.ActivePath)!;
        foreach (var companion in CompanionDllNames)
        {
            var src = Path.Combine(sourceDir, companion);
            if (!File.Exists(src))
            {
                continue;
            }

            var dst = Path.Combine(targetDir, companion);
            var dstBackup = Path.ChangeExtension(dst, null) + BackupSuffix;
            if (File.Exists(dst) && !File.Exists(dstBackup) && !LooksLikeGoldberg(dst))
            {
                File.Move(dst, dstBackup, overwrite: false);
                Logger.Info($"Backed up original {dst} to {Path.GetFileName(dstBackup)}");
            }
            CopyFileNormalized(src, dst);
            Logger.Info($"Installed GoldBerg companion {companion} into {dst}");
        }

        return pair.ActivePath;
    }

    /// <summary>
    /// Restore the original Steam API DLL (and any backed-up companion DLLs) from
    /// their <c>*.original.dll</c> backups.
    /// </summary>
    public static void Restore(string gameExePath)
    {
        var gameDir = Path.GetDirectoryName(gameExePath) ?? throw new ArgumentException("Could not derive game directory.", nameof(gameExePath));
        var pair = LocateDllPair(gameDir)
            ?? throw new FileNotFoundException("Could not locate the Steam API DLL inside the game directory.", Path.Combine(gameDir, "steam_api64.dll"));

        if (!File.Exists(pair.BackupPath))
        {
            throw new FileNotFoundException("No GoldBerg backup found. The game's steam_api64.dll has not been replaced by this tool.", pair.BackupPath);
        }

        // Main DLL.
        if (File.Exists(pair.ActivePath))
        {
            File.Delete(pair.ActivePath);
        }
        File.Move(pair.BackupPath, pair.ActivePath);
        Logger.Info($"Restored {pair.ActivePath} from backup.");

        // Companion DLLs: if a backup exists, restore it. If only the goldberg copy
        // exists (no backup because the game shipped without it), delete the goldberg
        // copy.
        var targetDir = Path.GetDirectoryName(pair.ActivePath)!;
        foreach (var companion in CompanionDllNames)
        {
            var dst = Path.Combine(targetDir, companion);
            var dstBackup = Path.ChangeExtension(dst, null) + BackupSuffix;
            if (File.Exists(dstBackup))
            {
                if (File.Exists(dst))
                {
                    File.Delete(dst);
                }
                File.Move(dstBackup, dst);
                Logger.Info($"Restored companion {dst} from backup.");
            }
            else if (File.Exists(dst) && LooksLikeGoldberg(dst))
            {
                File.Delete(dst);
                Logger.Info($"Removed orphan GoldBerg companion {dst} (no backup to restore).");
            }
        }
    }

    /// <summary>
    /// Write the per-instance GoldBerg settings into <c>{dllDir}/steam_settings/</c>.
    /// GoldBerg reads these files at process startup, so they must be written before
    /// each launch.
    /// </summary>
    public static void WriteInstanceSettings(string gameExePath, Instance instance)
    {
        var gameDir = Path.GetDirectoryName(gameExePath) ?? throw new ArgumentException("Could not derive game directory.", nameof(gameExePath));
        var pair = LocateDllPair(gameDir)
            ?? throw new InvalidOperationException("Cannot write GoldBerg settings: Steam API DLL not located in game directory.");

        // GoldBerg reads steam_settings/ relative to the DLL, not the exe.
        var settingsDir = Path.Combine(Path.GetDirectoryName(pair.ActivePath)!, "steam_settings");
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
        // Take the first 4 bytes as a uint (low account id) and combine with the standard
        // individual / public-universe prefix (76561197960265728) to produce a "looks normal"
        // SteamID64 that's deterministic per instance.
        ulong accountId = BitConverter.ToUInt32(hashBytes, 0);
        const ulong SteamIdBase = 76561197960265728UL;
        return (SteamIdBase + accountId).ToString();
    }

    private sealed record DllPair(string ActivePath, string BackupPath);

    /// <summary>
    /// Locate the Steam API DLL anywhere under <paramref name="gameDir"/>. Unity games
    /// commonly place native plugins under <c>&lt;Game&gt;_Data\Plugins\x86_64\</c>, so we
    /// can't assume the DLL sits next to the .exe.
    /// </summary>
    /// <remarks>
    /// We prefer a current <c>steam_api64.dll</c>. If only a <c>steam_api64.original.dll</c>
    /// backup exists (because a previous install removed the active file), use its location
    /// to reconstruct the pair.
    /// </remarks>
    private static DllPair? LocateDllPair(string gameDir)
    {
        if (!Directory.Exists(gameDir))
        {
            return null;
        }

        // Pass 1: current Steam API DLL.
        foreach (var name in CandidateDllNames)
        {
            var hit = SafeEnumerate(gameDir, name).FirstOrDefault();
            if (hit is not null)
            {
                var backup = Path.ChangeExtension(hit, null) + BackupSuffix;
                return new DllPair(hit, backup);
            }
        }

        // Pass 2: only a backup exists (DLL was renamed but the GoldBerg copy was deleted
        // by AV or by the user). Reconstruct the active path next to the backup.
        foreach (var name in CandidateDllNames)
        {
            var backupName = Path.GetFileNameWithoutExtension(name) + BackupSuffix;
            var hit = SafeEnumerate(gameDir, backupName).FirstOrDefault();
            if (hit is not null)
            {
                var active = Path.Combine(Path.GetDirectoryName(hit)!, name);
                return new DllPair(active, hit);
            }
        }

        return null;
    }

    private static IEnumerable<string> SafeEnumerate(string dir, string filename)
    {
        // Directory.EnumerateFiles with AllDirectories throws on the first unreadable
        // subdir. Walk manually so we can keep going past permission errors.
        var stack = new Stack<string>();
        stack.Push(dir);
        while (stack.Count > 0)
        {
            var current = stack.Pop();
            string[] files;
            try
            {
                files = Directory.GetFiles(current, filename, SearchOption.TopDirectoryOnly);
            }
            catch (Exception ex)
            {
                Logger.Warn($"Could not enumerate '{current}' while looking for {filename}", ex);
                continue;
            }
            foreach (var f in files)
            {
                yield return f;
            }

            string[] subdirs;
            try
            {
                subdirs = Directory.GetDirectories(current);
            }
            catch
            {
                continue;
            }
            foreach (var sd in subdirs)
            {
                stack.Push(sd);
            }
        }
    }

    private static void CopyFileNormalized(string source, string destination)
    {
        File.Copy(source, destination, overwrite: true);
        try
        {
            File.SetAttributes(destination, FileAttributes.Normal);
        }
        catch
        {
            // Best-effort attribute clear; not fatal.
        }
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
