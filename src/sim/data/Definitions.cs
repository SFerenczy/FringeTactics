using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// Central registry for all game data definitions.
/// Static facade that delegates to ConfigRegistry for backward compatibility.
/// </summary>
public static class Definitions
{
    private static ConfigRegistry registry;

    public static WeaponDefinitions Weapons => EnsureLoaded().Weapons;
    public static EnemyDefinitions Enemies => EnsureLoaded().Enemies;
    public static AbilityDefinitions Abilities => EnsureLoaded().Abilities;

    /// <summary>
    /// Ensure definitions are loaded and return the registry.
    /// </summary>
    private static ConfigRegistry EnsureLoaded()
    {
        if (registry == null)
        {
            registry = new ConfigRegistry();
            registry.Load();
        }
        return registry;
    }

    /// <summary>
    /// Force reload definitions from JSON files.
    /// Useful for hot-reloading during development.
    /// </summary>
    public static void Reload()
    {
        registry = new ConfigRegistry();
        registry.Load();
        SimLog.Log("[Definitions] Data reloaded");
    }

    /// <summary>
    /// Get the last load result for inspection.
    /// </summary>
    public static ConfigLoadResult GetLastLoadResult()
    {
        return registry?.LastLoadResult;
    }

    /// <summary>
    /// Get the underlying ConfigRegistry instance.
    /// </summary>
    public static ConfigRegistry GetRegistry()
    {
        return EnsureLoaded();
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

    public ValidationResult Validate()
    {
        var result = new ValidationResult();

        if (string.IsNullOrEmpty(Id))
            result.AddError("WeaponDef: Id is required");
        if (string.IsNullOrEmpty(Name))
            result.AddError($"WeaponDef[{Id}]: Name is required");
        if (Damage <= 0)
            result.AddError($"WeaponDef[{Id}]: Damage must be positive");
        if (Range <= 0)
            result.AddError($"WeaponDef[{Id}]: Range must be positive");
        if (CooldownTicks < 0)
            result.AddError($"WeaponDef[{Id}]: CooldownTicks cannot be negative");
        if (Accuracy < 0 || Accuracy > 1)
            result.AddError($"WeaponDef[{Id}]: Accuracy must be between 0 and 1");
        if (MagazineSize <= 0)
            result.AddError($"WeaponDef[{Id}]: MagazineSize must be positive");
        if (ReloadTicks < 0)
            result.AddError($"WeaponDef[{Id}]: ReloadTicks cannot be negative");

        return result;
    }
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

    public WeaponDef Get(string id) => weapons.TryGetValue(id, out var w) ? w : null;
    public bool Has(string id) => weapons.ContainsKey(id);
    public IEnumerable<WeaponDef> All => weapons.Values;
    public int Count => weapons.Count;
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

    public ValidationResult Validate()
    {
        var result = new ValidationResult();

        if (string.IsNullOrEmpty(Id))
            result.AddError("EnemyDef: Id is required");
        if (string.IsNullOrEmpty(Name))
            result.AddError($"EnemyDef[{Id}]: Name is required");
        if (Hp <= 0)
            result.AddError($"EnemyDef[{Id}]: Hp must be positive");
        if (string.IsNullOrEmpty(WeaponId))
            result.AddError($"EnemyDef[{Id}]: WeaponId is required");

        return result;
    }
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

    public EnemyDef Get(string id) => enemies.TryGetValue(id, out var e) ? e : null;
    public bool Has(string id) => enemies.ContainsKey(id);
    public IEnumerable<EnemyDef> All => enemies.Values;
    public int Count => enemies.Count;
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

    public ValidationResult Validate()
    {
        var result = new ValidationResult();

        if (string.IsNullOrEmpty(Id))
            result.AddError("AbilityDef: Id is required");
        if (string.IsNullOrEmpty(Name))
            result.AddError($"AbilityDef[{Id}]: Name is required");
        if (Range < 0)
            result.AddError($"AbilityDef[{Id}]: Range cannot be negative");
        if (Cooldown < 0)
            result.AddError($"AbilityDef[{Id}]: Cooldown cannot be negative");
        if (Delay < 0)
            result.AddError($"AbilityDef[{Id}]: Delay cannot be negative");
        if (Radius < 0)
            result.AddError($"AbilityDef[{Id}]: Radius cannot be negative");

        return result;
    }

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
    public int Count => abilities.Count;
}
