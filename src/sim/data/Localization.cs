using Godot;
using System.Collections.Generic;
using System.Text.Json;

namespace FringeTactics;

/// <summary>
/// Localization system for loading and retrieving translated text.
/// Loads JSON files from data/localization/{locale}.json
/// </summary>
public static class Localization
{
    private static Dictionary<string, string> strings = new();
    private static string currentLocale = "en";
    private static bool isLoaded = false;
    
    /// <summary>
    /// Current locale code (e.g., "en", "de", "fr").
    /// </summary>
    public static string CurrentLocale => currentLocale;
    
    /// <summary>
    /// Whether localization data has been loaded.
    /// </summary>
    public static bool IsLoaded => isLoaded;
    
    /// <summary>
    /// Load localization data for the specified locale.
    /// </summary>
    public static bool Load(string locale = "en")
    {
        currentLocale = locale;
        strings.Clear();
        isLoaded = false;
        
        string path = $"res://data/localization/{locale}.json";
        
        if (!FileAccess.FileExists(path))
        {
            GD.PrintErr($"[Localization] File not found: {path}");
            return false;
        }
        
        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        if (file == null)
        {
            GD.PrintErr($"[Localization] Failed to open: {path}");
            return false;
        }
        
        string json = file.GetAsText();
        
        try
        {
            var data = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            if (data != null)
            {
                strings = data;
                isLoaded = true;
                GD.Print($"[Localization] Loaded {strings.Count} strings for locale '{locale}'");
                return true;
            }
        }
        catch (JsonException ex)
        {
            GD.PrintErr($"[Localization] JSON parse error: {ex.Message}");
        }
        
        return false;
    }
    
    /// <summary>
    /// Get localized string for a key. Returns the key itself if not found.
    /// </summary>
    public static string Get(string key)
    {
        if (string.IsNullOrEmpty(key))
            return "";
            
        if (!isLoaded)
            Load();
            
        if (strings.TryGetValue(key, out var value))
            return value;
            
        return key;
    }
    
    /// <summary>
    /// Get localized string with parameter substitution.
    /// Parameters are replaced using {key} syntax.
    /// </summary>
    public static string Get(string key, Dictionary<string, string> parameters)
    {
        string text = Get(key);
        
        if (parameters != null)
        {
            foreach (var (paramKey, paramValue) in parameters)
            {
                text = text.Replace($"{{{paramKey}}}", paramValue);
            }
        }
        
        return text;
    }
    
    /// <summary>
    /// Check if a key exists in the current locale.
    /// </summary>
    public static bool HasKey(string key)
    {
        if (!isLoaded)
            Load();
            
        return strings.ContainsKey(key);
    }
    
    /// <summary>
    /// Get all loaded keys (for validation).
    /// </summary>
    public static IEnumerable<string> GetAllKeys()
    {
        if (!isLoaded)
            Load();
            
        return strings.Keys;
    }
    
    /// <summary>
    /// Clear loaded data (for testing or locale switching).
    /// </summary>
    public static void Clear()
    {
        strings.Clear();
        isLoaded = false;
    }
}
