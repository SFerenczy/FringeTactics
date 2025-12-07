using Godot;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace FringeTactics;

/// <summary>
/// Validates localization files against required keys from encounter templates.
/// Can be run at startup or via DevTools to catch missing translations.
/// </summary>
public static class LocalizationValidator
{
    /// <summary>
    /// Result of a validation run.
    /// </summary>
    public class ValidationResult
    {
        public string Locale { get; set; }
        public List<string> MissingKeys { get; set; } = new();
        public List<string> UnusedKeys { get; set; } = new();
        public int TotalRequired { get; set; }
        public int TotalInFile { get; set; }
        
        public bool IsValid => MissingKeys.Count == 0;
        
        public override string ToString()
        {
            if (IsValid)
                return $"[{Locale}] OK - {TotalInFile} keys, {UnusedKeys.Count} unused";
            return $"[{Locale}] MISSING {MissingKeys.Count} keys, {UnusedKeys.Count} unused";
        }
    }
    
    /// <summary>
    /// Validate a locale file against all encounter templates.
    /// </summary>
    public static ValidationResult Validate(string locale = "en")
    {
        var result = new ValidationResult { Locale = locale };
        
        // Get all required keys from encounters
        var requiredKeys = CollectRequiredKeys();
        result.TotalRequired = requiredKeys.Count;
        
        // Load the locale file
        string path = $"res://data/localization/{locale}.json";
        var fileKeys = new HashSet<string>();
        
        if (FileAccess.FileExists(path))
        {
            using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
            if (file != null)
            {
                try
                {
                    var data = JsonSerializer.Deserialize<Dictionary<string, string>>(file.GetAsText());
                    if (data != null)
                    {
                        fileKeys = data.Keys.ToHashSet();
                    }
                }
                catch (JsonException ex)
                {
                    GD.PrintErr($"[LocalizationValidator] JSON error in {locale}: {ex.Message}");
                }
            }
        }
        
        result.TotalInFile = fileKeys.Count;
        
        // Find missing keys
        foreach (var key in requiredKeys)
        {
            if (!fileKeys.Contains(key))
            {
                result.MissingKeys.Add(key);
            }
        }
        
        // Find unused keys (in file but not required)
        foreach (var key in fileKeys)
        {
            if (!requiredKeys.Contains(key))
            {
                result.UnusedKeys.Add(key);
            }
        }
        
        return result;
    }
    
    /// <summary>
    /// Validate all locale files in data/localization/.
    /// </summary>
    public static List<ValidationResult> ValidateAll()
    {
        var results = new List<ValidationResult>();
        
        string dir = "res://data/localization";
        if (!DirAccess.DirExistsAbsolute(dir))
        {
            GD.PrintErr($"[LocalizationValidator] Directory not found: {dir}");
            return results;
        }
        
        using var access = DirAccess.Open(dir);
        if (access == null)
        {
            GD.PrintErr($"[LocalizationValidator] Cannot open: {dir}");
            return results;
        }
        
        access.ListDirBegin();
        string fileName;
        while ((fileName = access.GetNext()) != "")
        {
            if (fileName.EndsWith(".json"))
            {
                string locale = fileName.Replace(".json", "");
                results.Add(Validate(locale));
            }
        }
        access.ListDirEnd();
        
        return results;
    }
    
    /// <summary>
    /// Print validation results to console.
    /// </summary>
    public static void PrintValidation(string locale = "en")
    {
        var result = Validate(locale);
        
        GD.Print($"=== Localization Validation: {locale} ===");
        GD.Print($"Required keys: {result.TotalRequired}");
        GD.Print($"Keys in file: {result.TotalInFile}");
        
        if (result.MissingKeys.Count > 0)
        {
            GD.Print($"\nMISSING KEYS ({result.MissingKeys.Count}):");
            foreach (var key in result.MissingKeys.OrderBy(k => k))
            {
                GD.Print($"  - {key}");
            }
        }
        
        if (result.UnusedKeys.Count > 0)
        {
            GD.Print($"\nUNUSED KEYS ({result.UnusedKeys.Count}):");
            foreach (var key in result.UnusedKeys.OrderBy(k => k))
            {
                GD.Print($"  - {key}");
            }
        }
        
        GD.Print(result.IsValid ? "\n✓ Validation PASSED" : "\n✗ Validation FAILED");
    }
    
    /// <summary>
    /// Collect all TextKey values from encounter templates.
    /// </summary>
    private static HashSet<string> CollectRequiredKeys()
    {
        var keys = new HashSet<string>();
        
        // Production encounters
        foreach (var template in ProductionEncounters.GetAllTemplates())
        {
            CollectKeysFromTemplate(template, keys);
        }
        
        // Test encounters
        CollectKeysFromTemplate(TestEncounters.CreateSimpleEncounter(), keys);
        CollectKeysFromTemplate(TestEncounters.CreateSkillCheckEncounter(), keys);
        
        return keys;
    }
    
    private static void CollectKeysFromTemplate(EncounterTemplate template, HashSet<string> keys)
    {
        if (template?.Nodes == null) return;
        
        foreach (var node in template.Nodes.Values)
        {
            if (!string.IsNullOrEmpty(node.TextKey))
            {
                keys.Add(node.TextKey);
            }
            
            if (node.Options == null) continue;
            
            foreach (var option in node.Options)
            {
                if (!string.IsNullOrEmpty(option.TextKey))
                {
                    keys.Add(option.TextKey);
                }
            }
        }
    }
    
    /// <summary>
    /// Generate a stub localization file with all required keys.
    /// Useful for creating new locale files.
    /// </summary>
    public static string GenerateStubJson()
    {
        var keys = CollectRequiredKeys().OrderBy(k => k).ToList();
        var stub = new Dictionary<string, string>();
        
        foreach (var key in keys)
        {
            stub[key] = $"[TODO: {key}]";
        }
        
        return JsonSerializer.Serialize(stub, new JsonSerializerOptions 
        { 
            WriteIndented = true 
        });
    }
}
