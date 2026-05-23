using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using PixelWorldsInjector.Models;

namespace PixelWorldsInjector.Services;

/// <summary>
/// Launches a single Pixel Worlds instance with optional data isolation and
/// post-launch mutex bypass.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class InstanceLauncher
{
    private readonly AppSettings _settings;
    private readonly ConfigStore _configStore;

    public InstanceLauncher(AppSettings settings, ConfigStore configStore)
    {
        _settings = settings;
        _configStore = configStore;
    }

    public sealed record LaunchResult(int Pid, int MutexesClosed);

    /// <summary>
    /// Launch the given <paramref name="instance"/>. Throws on fatal errors.
    /// </summary>
    public async Task<LaunchResult> LaunchAsync(Instance instance, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.GameExePath) || !File.Exists(_settings.GameExePath))
        {
            throw new FileNotFoundException("PixelWorlds.exe path is not configured or file is missing. Set it in Settings.", _settings.GameExePath);
        }

        var gameDir = Path.GetDirectoryName(_settings.GameExePath)!;

        // 1. Drop steam_appid.txt next to the exe so the Steamworks SDK does not require
        //    a Steam client to be running. (Official Valve trick documented in Steamworks SDK.)
        if (_settings.BypassSteam)
        {
            try
            {
                var appIdFile = Path.Combine(gameDir, "steam_appid.txt");
                File.WriteAllText(appIdFile, _settings.SteamAppId.ToString());
                Logger.Info($"Dropped steam_appid.txt with {_settings.SteamAppId} into {gameDir}");
            }
            catch (Exception ex)
            {
                Logger.Warn("Failed to write steam_appid.txt (continuing without Steam bypass)", ex);
            }
        }

        // 2. Optionally apply per-instance data isolation BEFORE launching.
        DataIsolation? isolation = null;
        if (instance.IsolateData)
        {
            try
            {
                var instanceDir = _configStore.GetInstanceDataDirectory(instance.Id);
                isolation = new DataIsolation(instanceDir);
                isolation.Apply();
            }
            catch (Exception ex)
            {
                Logger.Warn("Data isolation failed - launching without it. Multi-instance still works via mutex bypass.", ex);
                isolation = null;
            }
        }

        // 3. Start the game process.
        var psi = new ProcessStartInfo(_settings.GameExePath)
        {
            WorkingDirectory = gameDir,
            UseShellExecute = false,
            CreateNoWindow = false,
        };

        if (!string.IsNullOrWhiteSpace(instance.ExtraArgs))
        {
            psi.Arguments = instance.ExtraArgs;
        }

        // Tag the spawned process with the instance id via env var so future tooling
        // (e.g. a DLL injector) could pick it up. Harmless if unused.
        psi.EnvironmentVariables["PWINJECTOR_INSTANCE_ID"] = instance.Id;
        psi.EnvironmentVariables["PWINJECTOR_INSTANCE_NAME"] = instance.Name;

        Process proc;
        try
        {
            proc = Process.Start(psi) ?? throw new InvalidOperationException("Process.Start returned null");
            Logger.Info($"Started PixelWorlds.exe pid={proc.Id} for instance '{instance.Name}' ({instance.Id})");
        }
        catch
        {
            isolation?.Restore();
            throw;
        }

        instance.RunningPid = proc.Id;
        instance.LastLaunchedUtc = DateTimeOffset.UtcNow;

        // 4. Wait briefly for the game to create its single-instance mutex, then close it.
        //    Unity games typically create the mutex within the first 1-2 seconds.
        var closed = 0;
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(2.5), ct).ConfigureAwait(false);

            Func<string, bool>? nameFilter = null;
            if (!string.IsNullOrWhiteSpace(_settings.MutexNameHint))
            {
                var hint = _settings.MutexNameHint;
                nameFilter = name => name.IndexOf(hint, StringComparison.OrdinalIgnoreCase) >= 0;
            }

            closed = MutexBypass.CloseMutexesInProcess(proc.Id, nameFilter);
            Logger.Info($"Closed {closed} mutex handle(s) in pid={proc.Id}");
        }
        catch (Exception ex)
        {
            Logger.Warn("Mutex bypass step failed", ex);
        }

        // 5. Detach a background task to restore data isolation when the game closes.
        if (isolation is not null)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await proc.WaitForExitAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Logger.Warn($"WaitForExitAsync failed for pid={proc.Id}", ex);
                }
                finally
                {
                    isolation.Restore();
                }
            });
        }

        return new LaunchResult(proc.Id, closed);
    }
}
