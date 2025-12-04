using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FringeTactics;

/// <summary>
/// Loads game data from JSON files.
/// Uses Godot's FileAccess for res:// path support.
/// </summary>
public static class DataLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>
    /// Load a dictionary of items from a JSON file.
    /// </summary>
    public static Dictionary<string, T> LoadDictionary<T>(string path)
    {
        var json = LoadJsonString(path);
        if (string.IsNullOrEmpty(json))
        {
            GD.PrintErr($"[DataLoader] Failed to load: {path}");
            return new Dictionary<string, T>();
        }

        try
        {
            var result = JsonSerializer.Deserialize<Dictionary<string, T>>(json, JsonOptions);
            GD.Print($"[DataLoader] Loaded {result?.Count ?? 0} items from {path}");
            return result ?? new Dictionary<string, T>();
        }
        catch (JsonException ex)
        {
            GD.PrintErr($"[DataLoader] JSON parse error in {path}: {ex.Message}");
            return new Dictionary<string, T>();
        }
    }

    /// <summary>
    /// Load raw JSON string from a file path.
    /// Supports both res:// paths and absolute paths.
    /// </summary>
    private static string LoadJsonString(string path)
    {
        // Try Godot's FileAccess first (works for res:// paths)
        if (FileAccess.FileExists(path))
        {
            using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
            if (file != null)
            {
                return file.GetAsText();
            }
        }

        // Fallback to System.IO for absolute paths (useful for tests)
        if (System.IO.File.Exists(path))
        {
            return System.IO.File.ReadAllText(path);
        }

        return null;
    }

    /// <summary>
    /// Check if a data file exists.
    /// </summary>
    public static bool FileExists(string path)
    {
        return FileAccess.FileExists(path) || System.IO.File.Exists(path);
    }
}
