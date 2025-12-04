using System;
using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// Configuration loading result with validation status.
/// </summary>
public class ConfigLoadResult
{
    public bool Success { get; set; } = true;
    public List<string> Errors { get; } = new();
    public List<string> Warnings { get; } = new();
    public int ItemsLoaded { get; set; }

    public void AddError(string message)
    {
        Errors.Add(message);
        Success = false;
    }

    public void AddWarning(string message) => Warnings.Add(message);

    public void Merge(ValidationResult validation)
    {
        Errors.AddRange(validation.Errors);
        Warnings.AddRange(validation.Warnings);
        if (validation.Errors.Count > 0)
        {
            Success = false;
        }
    }
}

/// <summary>
/// Central registry for game configuration data.
/// Loads from JSON with validation and clear error reporting.
/// </summary>
public class ConfigRegistry
{
    private const string WeaponsPath = "res://data/weapons.json";
    private const string EnemiesPath = "res://data/enemies.json";
    private const string AbilitiesPath = "res://data/abilities.json";

    public WeaponDefinitions Weapons { get; private set; } = new();
    public EnemyDefinitions Enemies { get; private set; } = new();
    public AbilityDefinitions Abilities { get; private set; } = new();

    public bool IsLoaded { get; private set; }
    public ConfigLoadResult LastLoadResult { get; private set; }

    /// <summary>
    /// If true, throw on validation errors. Set to true for development.
    /// </summary>
    public bool FailFastOnErrors { get; set; } = false;

    /// <summary>
    /// Load all configuration files with validation.
    /// </summary>
    public ConfigLoadResult Load()
    {
        var result = new ConfigLoadResult();

        // Load weapons first (enemies reference weapons)
        var weaponResult = LoadWeapons();
        result.Merge(weaponResult);
        result.ItemsLoaded += Weapons.Count;

        // Load enemies (validates weapon references)
        var enemyResult = LoadEnemies();
        result.Merge(enemyResult);
        result.ItemsLoaded += Enemies.Count;

        // Load abilities
        var abilityResult = LoadAbilities();
        result.Merge(abilityResult);
        result.ItemsLoaded += Abilities.Count;

        result.Success = result.Errors.Count == 0;
        LastLoadResult = result;
        IsLoaded = true;

        LogLoadResult(result);

        if (FailFastOnErrors && !result.Success)
        {
            throw new InvalidOperationException(
                $"Config validation failed with {result.Errors.Count} errors:\n" +
                string.Join("\n", result.Errors));
        }

        return result;
    }

    private ValidationResult LoadWeapons()
    {
        var result = new ValidationResult();

        if (!DataLoader.FileExists(WeaponsPath))
        {
            result.AddWarning($"Weapons file not found: {WeaponsPath}, using defaults");
            Weapons = new WeaponDefinitions();
            return result;
        }

        var data = DataLoader.LoadDictionary<WeaponDef>(WeaponsPath);
        if (data.Count == 0)
        {
            result.AddWarning("No weapons loaded, using defaults");
            Weapons = new WeaponDefinitions();
            return result;
        }

        foreach (var weapon in data.Values)
        {
            var validation = weapon.Validate();
            result.Merge(validation);
        }

        Weapons = new WeaponDefinitions(data);
        return result;
    }

    private ValidationResult LoadEnemies()
    {
        var result = new ValidationResult();

        if (!DataLoader.FileExists(EnemiesPath))
        {
            result.AddWarning($"Enemies file not found: {EnemiesPath}, using defaults");
            Enemies = new EnemyDefinitions();
            return result;
        }

        var data = DataLoader.LoadDictionary<EnemyDef>(EnemiesPath);
        if (data.Count == 0)
        {
            result.AddWarning("No enemies loaded, using defaults");
            Enemies = new EnemyDefinitions();
            return result;
        }

        foreach (var enemy in data.Values)
        {
            var validation = enemy.Validate();
            result.Merge(validation);

            // Cross-reference validation: check weapon exists
            if (!string.IsNullOrEmpty(enemy.WeaponId) && !Weapons.Has(enemy.WeaponId))
            {
                result.AddWarning($"EnemyDef[{enemy.Id}]: WeaponId '{enemy.WeaponId}' not found in weapons");
            }
        }

        Enemies = new EnemyDefinitions(data);
        return result;
    }

    private ValidationResult LoadAbilities()
    {
        var result = new ValidationResult();

        if (!DataLoader.FileExists(AbilitiesPath))
        {
            result.AddWarning($"Abilities file not found: {AbilitiesPath}, using defaults");
            Abilities = new AbilityDefinitions();
            return result;
        }

        var data = DataLoader.LoadDictionary<AbilityDef>(AbilitiesPath);
        if (data.Count == 0)
        {
            result.AddWarning("No abilities loaded, using defaults");
            Abilities = new AbilityDefinitions();
            return result;
        }

        foreach (var ability in data.Values)
        {
            var validation = ability.Validate();
            result.Merge(validation);
        }

        Abilities = new AbilityDefinitions(data);
        return result;
    }

    private void LogLoadResult(ConfigLoadResult result)
    {
        SimLog.Log($"[ConfigRegistry] Loaded {result.ItemsLoaded} items");

        foreach (var warning in result.Warnings)
        {
            SimLog.Log($"[ConfigRegistry] WARNING: {warning}");
        }

        foreach (var error in result.Errors)
        {
            SimLog.Log($"[ConfigRegistry] ERROR: {error}");
        }
    }
}
