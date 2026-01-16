using DailyReflection.Services.Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace DailyReflection.Avalonia.Services;

/// <summary>
/// Avalonia implementation of ISettingsService using JSON file storage.
/// Settings are persisted to a local file in the app data directory.
/// </summary>
public class SettingsService : ISettingsService
{
    private readonly string _settingsPath;
    private Dictionary<string, object> _settings;

    public SettingsService()
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DailyReflection");
        
        Directory.CreateDirectory(appDataPath);
        _settingsPath = Path.Combine(appDataPath, "settings.json");
        
        _settings = LoadSettings();
    }

    public T Get<T>(string key, T defaultValue)
    {
        if (_settings.TryGetValue(key, out var value))
        {
            try
            {
                if (value is JsonElement element)
                {
                    return DeserializeJsonElement<T>(element, defaultValue);
                }
                
                if (value is T typedValue)
                {
                    return typedValue;
                }
                
                // Try to convert
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
        // No migration needed for Avalonia version
    }

    private Dictionary<string, object> LoadSettings()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                var json = File.ReadAllText(_settingsPath);
                return JsonSerializer.Deserialize<Dictionary<string, object>>(json) 
                       ?? new Dictionary<string, object>();
            }
        }
        catch
        {
            // Ignore errors, return empty dictionary
        }
        
        return new Dictionary<string, object>();
    }

    private void SaveSettings()
    {
        try
        {
            var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(_settingsPath, json);
        }
        catch
        {
            // Ignore save errors
        }
    }

    private static T DeserializeJsonElement<T>(JsonElement element, T defaultValue)
    {
        var targetType = typeof(T);
        
        // Handle nullable types
        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;
        
        try
        {
            if (underlyingType == typeof(bool))
            {
                return (T)(object)element.GetBoolean();
            }
            if (underlyingType == typeof(int))
            {
                return (T)(object)element.GetInt32();
            }
            if (underlyingType == typeof(long))
            {
                return (T)(object)element.GetInt64();
            }
            if (underlyingType == typeof(double))
            {
                return (T)(object)element.GetDouble();
            }
            if (underlyingType == typeof(string))
            {
                return (T)(object)element.GetString()!;
            }
            if (underlyingType == typeof(DateTime))
            {
                if (element.ValueKind == JsonValueKind.String)
                {
                    return (T)(object)DateTime.Parse(element.GetString()!);
                }
            }
            
            // Fallback: try to deserialize using the target type
            return element.Deserialize<T>() ?? defaultValue;
        }
        catch
        {
            return defaultValue;
        }
    }
}
