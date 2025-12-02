using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// Central registry for all game data definitions.
/// This is the "data layer" - no logic, just lookups.
/// </summary>
public static class Definitions
{
    public static WeaponDefinitions Weapons { get; } = new();
    public static EnemyDefinitions Enemies { get; } = new();
    public static AbilityDefinitions Abilities { get; } = new();
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
    public float Accuracy { get; set; } = 0.70f; // Base hit chance modifier
}

public class WeaponDefinitions
{
    private readonly Dictionary<string, WeaponDef> weapons = new()
    {
        ["rifle"] = new WeaponDef
        {
            Id = "rifle",
            Name = "Assault Rifle",
            Damage = 25,
            Range = 8,
            CooldownTicks = 10,
            Accuracy = 0.70f
        },
        ["pistol"] = new WeaponDef
        {
            Id = "pistol",
            Name = "Pistol",
            Damage = 15,
            Range = 5,
            CooldownTicks = 6,
            Accuracy = 0.75f
        },
        ["smg"] = new WeaponDef
        {
            Id = "smg",
            Name = "SMG",
            Damage = 18,
            Range = 6,
            CooldownTicks = 5,
            Accuracy = 0.60f
        },
        ["shotgun"] = new WeaponDef
        {
            Id = "shotgun",
            Name = "Shotgun",
            Damage = 40,
            Range = 4,
            CooldownTicks = 15,
            Accuracy = 0.80f
        }
    };

    public WeaponDef Get(string id) => weapons.TryGetValue(id, out var w) ? w : weapons["rifle"];
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
    private readonly Dictionary<string, EnemyDef> enemies = new()
    {
        ["grunt"] = new EnemyDef
        {
            Id = "grunt",
            Name = "Grunt",
            Hp = 80,
            WeaponId = "rifle",
            Behavior = EnemyBehavior.Aggressive
        },
        ["gunner"] = new EnemyDef
        {
            Id = "gunner",
            Name = "Gunner",
            Hp = 100,
            WeaponId = "smg",
            Behavior = EnemyBehavior.Aggressive
        },
        ["sniper"] = new EnemyDef
        {
            Id = "sniper",
            Name = "Sniper",
            Hp = 60,
            WeaponId = "rifle",
            Behavior = EnemyBehavior.Defensive
        },
        ["heavy"] = new EnemyDef
        {
            Id = "heavy",
            Name = "Heavy",
            Hp = 150,
            WeaponId = "shotgun",
            Behavior = EnemyBehavior.Aggressive
        }
    };

    public EnemyDef Get(string id) => enemies.TryGetValue(id, out var e) ? e : enemies["grunt"];
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
    private readonly Dictionary<string, AbilityDef> abilities = new()
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
        },
        ["stun_grenade"] = new AbilityDef
        {
            Id = "stun_grenade",
            Name = "Stun Grenade",
            Type = AbilityType.Grenade,
            TargetType = AbilityTargetType.Tile,
            Range = 6,
            Cooldown = 50,
            Delay = 20,
            Radius = 2,
            Damage = 10,
            EffectId = "stunned",
            EffectDuration = 40
        },
        ["stun_shot"] = new AbilityDef
        {
            Id = "stun_shot",
            Name = "Stun Shot",
            Type = AbilityType.Shot,
            TargetType = AbilityTargetType.Actor,
            Range = 6,
            Cooldown = 40,
            Delay = 0,
            Radius = 0,
            Damage = 10,
            EffectId = "stunned",
            EffectDuration = 40
        }
    };

    public AbilityDef Get(string id) => abilities.TryGetValue(id, out var a) ? a : null;
    public bool Has(string id) => abilities.ContainsKey(id);
    public IEnumerable<AbilityDef> All => abilities.Values;
}
