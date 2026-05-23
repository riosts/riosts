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
