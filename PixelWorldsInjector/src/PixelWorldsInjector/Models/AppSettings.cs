using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PixelWorldsInjector.Models;

/// <summary>
/// Top-level persisted settings for the injector.
/// Lives at %AppData%\PixelWorldsInjector\settings.json.
/// </summary>
public sealed class AppSettings
{
    /// <summary>
    /// Full path to PixelWorlds.exe. Auto-detected from Steam library on first run.
    /// </summary>
    [JsonPropertyName("gameExePath")]
    public string GameExePath { get; set; } = string.Empty;

    /// <summary>
    /// Steam App ID for Pixel Worlds. Used when dropping steam_appid.txt.
    /// 533020 = official Steam App ID for Pixel Worlds.
    /// </summary>
    [JsonPropertyName("steamAppId")]
    public int SteamAppId { get; set; } = 533020;

    /// <summary>
    /// Named mutex string the game uses for single-instance lock.
    /// Pixel Worlds (Unity) typically uses a UnityCrashHandler / generated GUID mutex.
    /// We scan and close any mutex owned by the spawned process by default,
    /// so this is only used as a hint / override.
    /// </summary>
    [JsonPropertyName("mutexNameHint")]
    public string MutexNameHint { get; set; } = string.Empty;

    /// <summary>
    /// When true, the launcher always drops steam_appid.txt next to the game exe
    /// so the game runs without Steam client. Disable if launching via Steam.
    /// </summary>
    [JsonPropertyName("bypassSteam")]
    public bool BypassSteam { get; set; } = true;

    [JsonPropertyName("instances")]
    public List<Instance> Instances { get; set; } = new();
}
