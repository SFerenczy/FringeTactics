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
    public float Accuracy { get; set; } = 0.70f;
    public int MagazineSize { get; set; } = 30;
    public int ReloadTicks { get; set; } = 40; // 2 seconds at 20 ticks/sec
}

public class WeaponDefinitions
{
    // Weapon balance tuned for cover gameplay. See CombatBalance.cs for design rationale.
    private readonly Dictionary<string, WeaponDef> weapons = new()
    {
        // Rifle: Balanced all-rounder
        // 4 hits to kill (100 HP), reliable at medium range
        ["rifle"] = new WeaponDef
        {
            Id = "rifle",
            Name = "Assault Rifle",
            Damage = 25,
            Range = 8,
            CooldownTicks = 10,    // 0.5s between shots
            Accuracy = 0.70f,      // 70% base, ~42% vs cover
            MagazineSize = 30,
            ReloadTicks = 40       // 2 sec
        },
        // Pistol: Backup weapon, accurate but weak
        // 6 hits to kill, good for finishing wounded targets
        ["pistol"] = new WeaponDef
        {
            Id = "pistol",
            Name = "Pistol",
            Damage = 18,
            Range = 5,
            CooldownTicks = 6,     // 0.3s between shots
            Accuracy = 0.75f,      // More accurate at short range
            MagazineSize = 12,
            ReloadTicks = 20       // 1 sec
        },
        // SMG: Spray and pray, volume of fire
        // 7 hits to kill, but very fast fire rate
        ["smg"] = new WeaponDef
        {
            Id = "smg",
            Name = "SMG",
            Damage = 15,
            Range = 6,
            CooldownTicks = 4,     // 0.2s between shots (very fast)
            Accuracy = 0.55f,      // Less accurate, relies on volume
            MagazineSize = 25,
            ReloadTicks = 30       // 1.5 sec
        },
        // Shotgun: High risk/reward, devastating up close
        // 2 hits to kill! But must close distance
        ["shotgun"] = new WeaponDef
        {
            Id = "shotgun",
            Name = "Shotgun",
            Damage = 50,
            Range = 4,             // Very short range
            CooldownTicks = 18,    // 0.9s between shots (slow)
            Accuracy = 0.85f,      // Very accurate when in range
            MagazineSize = 6,
            ReloadTicks = 60       // 3 sec
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
