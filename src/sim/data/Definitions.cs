using Godot;
using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// Central registry for all game data definitions.
/// Loads from JSON files in data/ folder, with hardcoded fallbacks.
/// </summary>
public static class Definitions
{
    private const string WeaponsPath = "res://data/weapons.json";
    private const string EnemiesPath = "res://data/enemies.json";
    private const string AbilitiesPath = "res://data/abilities.json";

    private static bool isLoaded = false;

    public static WeaponDefinitions Weapons { get; private set; } = new();
    public static EnemyDefinitions Enemies { get; private set; } = new();
    public static AbilityDefinitions Abilities { get; private set; } = new();

    /// <summary>
    /// Load definitions from JSON files. Called automatically on first access.
    /// </summary>
    public static void Load()
    {
        if (isLoaded)
        {
            return;
        }

        Weapons = new WeaponDefinitions();
        Enemies = new EnemyDefinitions();
        Abilities = new AbilityDefinitions();

        // Load from JSON if files exist
        if (DataLoader.FileExists(WeaponsPath))
        {
            var weaponData = DataLoader.LoadDictionary<WeaponDef>(WeaponsPath);
            if (weaponData.Count > 0)
            {
                Weapons = new WeaponDefinitions(weaponData);
            }
        }

        if (DataLoader.FileExists(EnemiesPath))
        {
            var enemyData = DataLoader.LoadDictionary<EnemyDef>(EnemiesPath);
            if (enemyData.Count > 0)
            {
                Enemies = new EnemyDefinitions(enemyData);
            }
        }

        if (DataLoader.FileExists(AbilitiesPath))
        {
            var abilityData = DataLoader.LoadDictionary<AbilityDef>(AbilitiesPath);
            if (abilityData.Count > 0)
            {
                Abilities = new AbilityDefinitions(abilityData);
            }
        }

        isLoaded = true;
        GD.Print("[Definitions] Data loaded");
    }

    /// <summary>
    /// Force reload definitions from JSON files.
    /// Useful for hot-reloading during development.
    /// </summary>
    public static void Reload()
    {
        isLoaded = false;
        Load();
        GD.Print("[Definitions] Data reloaded");
    }

    /// <summary>
    /// Ensure definitions are loaded. Call this before accessing any definitions.
    /// </summary>
    public static void EnsureLoaded()
    {
        if (!isLoaded)
        {
            Load();
        }
    }
}

// ============================================================================
// WEAPONS
// ============================================================================

public class WeaponDef
{
    public string Id { get; set; }
    public string Name { get; set; }
    public int Damage { get; set; }
    public int Range { get; set; }
    public int CooldownTicks { get; set; }
    public float Accuracy { get; set; } = 0.70f;
    public int MagazineSize { get; set; } = 30;
    public int ReloadTicks { get; set; } = 40; // 2 seconds at 20 ticks/sec
}

public class WeaponDefinitions
{
    private readonly Dictionary<string, WeaponDef> weapons;

    /// <summary>
    /// Create with hardcoded defaults (fallback).
    /// </summary>
    public WeaponDefinitions()
    {
        weapons = new Dictionary<string, WeaponDef>
        {
            ["rifle"] = new WeaponDef
            {
                Id = "rifle",
                Name = "Assault Rifle",
                Damage = 25,
                Range = 8,
                CooldownTicks = 10,
                Accuracy = 0.70f,
                MagazineSize = 30,
                ReloadTicks = 40
            }
        };
    }

    /// <summary>
    /// Create from loaded JSON data.
    /// </summary>
    public WeaponDefinitions(Dictionary<string, WeaponDef> data)
    {
        weapons = data;
    }

    public WeaponDef Get(string id) => weapons.TryGetValue(id, out var w) ? w : weapons.GetValueOrDefault("rifle");
    public bool Has(string id) => weapons.ContainsKey(id);
    public IEnumerable<WeaponDef> All => weapons.Values;
}

// ============================================================================
// ENEMIES
// ============================================================================

public enum EnemyBehavior
{
    Aggressive,  // Rush toward player, attack when in range
    Defensive,   // Hold position, attack if player approaches
    Flanker      // Try to get behind player (future)
}

public class EnemyDef
{
    public string Id { get; set; }
    public string Name { get; set; }
    public int Hp { get; set; }
    public string WeaponId { get; set; }
    public EnemyBehavior Behavior { get; set; }
}

public class EnemyDefinitions
{
    private readonly Dictionary<string, EnemyDef> enemies;

    /// <summary>
    /// Create with hardcoded defaults (fallback).
    /// </summary>
    public EnemyDefinitions()
    {
        enemies = new Dictionary<string, EnemyDef>
        {
            ["grunt"] = new EnemyDef
            {
                Id = "grunt",
                Name = "Grunt",
                Hp = 80,
                WeaponId = "rifle",
                Behavior = EnemyBehavior.Aggressive
            }
        };
    }

    /// <summary>
    /// Create from loaded JSON data.
    /// </summary>
    public EnemyDefinitions(Dictionary<string, EnemyDef> data)
    {
        enemies = data;
    }

    public EnemyDef Get(string id) => enemies.TryGetValue(id, out var e) ? e : enemies.GetValueOrDefault("grunt");
    public bool Has(string id) => enemies.ContainsKey(id);
    public IEnumerable<EnemyDef> All => enemies.Values;
}

// ============================================================================
// ABILITIES
// ============================================================================

public enum AbilityType
{
    Grenade,    // AoE damage after delay
    Shot,       // Single target with effect
    Buff,       // Self-buff (future)
    Heal        // Heal self or ally (future)
}

public class AbilityDef
{
    public string Id { get; set; }
    public string Name { get; set; }
    public AbilityType Type { get; set; }
    public AbilityTargetType TargetType { get; set; }
    public int Range { get; set; }
    public int Cooldown { get; set; }
    public int Delay { get; set; }
    public int Radius { get; set; }
    public int Damage { get; set; }
    public string EffectId { get; set; }
    public int EffectDuration { get; set; }

    /// <summary>
    /// Convert to AbilityData for use with AbilitySystem.
    /// </summary>
    public AbilityData ToAbilityData() => new()
    {
        Id = Id,
        Name = Name,
        TargetType = TargetType,
        Range = Range,
        Cooldown = Cooldown,
        Delay = Delay,
        Radius = Radius,
        Damage = Damage,
        EffectId = EffectId,
        EffectDuration = EffectDuration
    };
}

public class AbilityDefinitions
{
    private readonly Dictionary<string, AbilityDef> abilities;

    /// <summary>
    /// Create with hardcoded defaults (fallback).
    /// </summary>
    public AbilityDefinitions()
    {
        abilities = new Dictionary<string, AbilityDef>
        {
            ["frag_grenade"] = new AbilityDef
            {
                Id = "frag_grenade",
                Name = "Frag Grenade",
                Type = AbilityType.Grenade,
                TargetType = AbilityTargetType.Tile,
                Range = 6,
                Cooldown = 60,
                Delay = 20,
                Radius = 2,
                Damage = 40,
                EffectId = null,
                EffectDuration = 0
            }
        };
    }

    /// <summary>
    /// Create from loaded JSON data.
    /// </summary>
    public AbilityDefinitions(Dictionary<string, AbilityDef> data)
    {
        abilities = data;
    }

    public AbilityDef Get(string id) => abilities.TryGetValue(id, out var a) ? a : null;
    public bool Has(string id) => abilities.ContainsKey(id);
    public IEnumerable<AbilityDef> All => abilities.Values;
}
