using System;
using System.IO;
using System.Text.Json;
using PixelWorldsInjector.Models;

namespace PixelWorldsInjector.Services;

/// <summary>
/// Persists <see cref="AppSettings"/> to %AppData%\PixelWorldsInjector\settings.json.
/// </summary>
public sealed class ConfigStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public string ConfigDirectory { get; }
    public string SettingsPath { get; }
    public string InstancesDirectory { get; }

    public ConfigStore()
    {
        ConfigDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PixelWorldsInjector");
        Directory.CreateDirectory(ConfigDirectory);

        SettingsPath = Path.Combine(ConfigDirectory, "settings.json");
        InstancesDirectory = Path.Combine(ConfigDirectory, "instances");
        Directory.CreateDirectory(InstancesDirectory);
    }

    public AppSettings Load()
    {
        if (!File.Exists(SettingsPath))
        {
            return new AppSettings();
        }

        try
        {
            var json = File.ReadAllText(SettingsPath);
            return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to load settings from {SettingsPath}, starting with defaults", ex);
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        var json = JsonSerializer.Serialize(settings, JsonOptions);
        // Write atomically by writing to a temp file then renaming.
        var tmp = SettingsPath + ".tmp";
        File.WriteAllText(tmp, json);
        File.Move(tmp, SettingsPath, overwrite: true);
    }

    /// <summary>Resolve the data directory for a given instance id.</summary>
    public string GetInstanceDataDirectory(string instanceId)
    {
        var path = Path.Combine(InstancesDirectory, instanceId);
        Directory.CreateDirectory(path);
        return path;
    }
}
