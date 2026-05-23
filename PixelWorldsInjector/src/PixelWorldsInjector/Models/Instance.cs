using System;
using System.Text.Json.Serialization;

namespace PixelWorldsInjector.Models;

/// <summary>
/// Configuration record for a single Pixel Worlds instance managed by the injector.
/// Each instance gets its own isolated data directory and an optional account label.
/// </summary>
public sealed class Instance
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    [JsonPropertyName("name")]
    public string Name { get; set; } = "Instance";

    /// <summary>
    /// Optional human label for the account associated with this instance.
    /// Stored purely for the user's reference. The injector never stores credentials.
    /// </summary>
    [JsonPropertyName("accountLabel")]
    public string AccountLabel { get; set; } = string.Empty;

    /// <summary>
    /// Optional extra command-line arguments appended to PixelWorlds.exe.
    /// </summary>
    [JsonPropertyName("extraArgs")]
    public string ExtraArgs { get; set; } = string.Empty;

    /// <summary>
    /// Whether to isolate Unity persistent data (LocalLow + HKCU registry hive)
    /// per instance using junctions. When false, all instances share the game's
    /// default data folder (mutex bypass only).
    /// </summary>
    [JsonPropertyName("isolateData")]
    public bool IsolateData { get; set; } = true;

    /// <summary>
    /// When true, the launcher activates the GoldBerg Steam Emulator for this instance
    /// (the emulator dll must be configured globally in <see cref="AppSettings.GoldbergDllPath"/>
    /// and installed into the game directory at least once).
    /// </summary>
    [JsonPropertyName("useSteamEmu")]
    public bool UseSteamEmu { get; set; }

    /// <summary>
    /// Per-instance Steam display name used by the emulator. Optional — defaults to the
    /// instance name if empty. This has no link to a real Steam account.
    /// </summary>
    [JsonPropertyName("steamAccountName")]
    public string SteamAccountName { get; set; } = string.Empty;

    /// <summary>
    /// Per-instance 17-digit SteamID64 used by the emulator. Optional — when empty a
    /// deterministic fake ID is derived from <see cref="Id"/> so each instance is stable
    /// across launches. Has no link to any real Steam account.
    /// </summary>
    [JsonPropertyName("steamId")]
    public string SteamId { get; set; } = string.Empty;

    [JsonPropertyName("createdUtc")]
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;

    [JsonPropertyName("lastLaunchedUtc")]
    public DateTimeOffset? LastLaunchedUtc { get; set; }

    /// <summary>
    /// PID of the most recent process spawned for this instance, if still tracked.
    /// Not persisted across launcher restarts (transient state).
    /// </summary>
    [JsonIgnore]
    public int? RunningPid { get; set; }

    public override string ToString() => Name;
}
