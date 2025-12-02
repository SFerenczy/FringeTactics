namespace FringeTactics;

/// <summary>
/// Targeting mode for abilities.
/// </summary>
public enum AbilityTargetType
{
    None,       // No targeting needed (self-buff)
    Tile,       // Target a tile (AoE, grenade)
    Actor       // Target an actor (single-target)
}

/// <summary>
/// Data definition for an ability. Data-driven design.
/// </summary>
public class AbilityData
{
    public string Id { get; set; }
    public string Name { get; set; }
    public AbilityTargetType TargetType { get; set; }
    public int Range { get; set; }          // Max range in tiles
    public int Cooldown { get; set; }       // Cooldown in ticks
    public int Delay { get; set; }          // Delay before effect (for grenades)
    public int Radius { get; set; }         // AoE radius (0 = single target)
    public int Damage { get; set; }         // Damage dealt
    public string EffectId { get; set; }    // Status effect to apply (if any)
    public int EffectDuration { get; set; } // Duration of effect in ticks

    // Predefined abilities
    public static AbilityData FragGrenade => new()
    {
        Id = "frag_grenade",
        Name = "Frag Grenade",
        TargetType = AbilityTargetType.Tile,
        Range = 6,
        Cooldown = 60,      // 3 seconds
        Delay = 20,         // 1 second fuse
        Radius = 2,         // 2 tile radius
        Damage = 40,
        EffectId = null,
        EffectDuration = 0
    };

    public static AbilityData StunShot => new()
    {
        Id = "stun_shot",
        Name = "Stun Shot",
        TargetType = AbilityTargetType.Actor,
        Range = 6,
        Cooldown = 40,      // 2 seconds
        Delay = 0,
        Radius = 0,
        Damage = 10,
        EffectId = "stunned",
        EffectDuration = 40 // 2 seconds stun
    };
}
