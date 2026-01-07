using DailyReflection.Services.Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace DailyReflection.Avalonia.Services;

/// <summary>
/// Desktop implementation of ISettingsService using local JSON file storage
/// </summary>
public class DesktopSettingsService : ISettingsService
{
    private readonly string _settingsFilePath;
    private Dictionary<string, object> _settings;

    public DesktopSettingsService()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appFolder = Path.Combine(appDataPath, "DailyReflection");
        
        if (!Directory.Exists(appFolder))
        {
            Directory.CreateDirectory(appFolder);
        }

        _settingsFilePath = Path.Combine(appFolder, "settings.json");
        _settings = LoadSettings();
    }

    public T Get<T>(string key, T defaultValue)
    {
        if (_settings.TryGetValue(key, out var value))
        {
            try
            {
                if (value is JsonElement jsonElement)
                {
                    return JsonSerializer.Deserialize<T>(jsonElement.GetRawText()) ?? defaultValue;
                }
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }
        return defaultValue;
    }

    public void Set<T>(string key, T value)
    {
        _settings[key] = value!;
        SaveSettings();
    }

    public void MigrateOldPreferences()
    {
        // No migration needed for new Avalonia implementation
    }

    private Dictionary<string, object> LoadSettings()
    {
        try
        {
            if (File.Exists(_settingsFilePath))
            {
                var json = File.ReadAllText(_settingsFilePath);
                return JsonSerializer.Deserialize<Dictionary<string, object>>(json) ?? new Dictionary<string, object>();
            }
        }
        catch
        {
            // If there's any error loading, start fresh
        }
        return new Dictionary<string, object>();
    }

    private void SaveSettings()
    {
        try
        {
            var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsFilePath, json);
        }
        catch
        {
            // Silently fail if we can't save
        }
    }
}
