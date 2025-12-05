namespace FringeTactics;

/// <summary>
/// Central location for combat balance parameters.
/// Adjust these to tune lethality and cover effectiveness.
/// </summary>
public static class CombatBalance
{
    // === Hit Chance ===
    
    /// <summary>At max weapon range, accuracy is reduced by this factor.</summary>
    public const float RangePenaltyFactor = 0.30f;
    
    /// <summary>Minimum hit chance floor (10%). Prevents impossible shots.</summary>
    public const float MinHitChance = 0.10f;
    
    /// <summary>Maximum hit chance cap (95%). Prevents guaranteed hits.</summary>
    public const float MaxHitChance = 0.95f;
    
    // === Cover Heights ===
    
    /// <summary>Hit reduction for low cover (0.25 height). 15% reduction.</summary>
    public const float LowCoverReduction = 0.15f;
    
    /// <summary>Hit reduction for half cover (0.50 height). 30% reduction.</summary>
    public const float HalfCoverReduction = 0.30f;
    
    /// <summary>Hit reduction for high cover (0.75 height). 45% reduction.</summary>
    public const float HighCoverReduction = 0.45f;
    
    /// <summary>
    /// Get hit reduction for a given cover height.
    /// </summary>
    public static float GetCoverReduction(CoverHeight height)
    {
        return height switch
        {
            CoverHeight.Low => LowCoverReduction,
            CoverHeight.Half => HalfCoverReduction,
            CoverHeight.High => HighCoverReduction,
            _ => 0f
        };
    }
    
    // === Actor Stats ===
    
    /// <summary>Default crew HP.</summary>
    public const int DefaultCrewHp = 100;
    
    /// <summary>Default crew vision radius in tiles.</summary>
    public const int DefaultVisionRadius = 8;
    
    // === Lethality Design Targets ===
    // These are design goals, not enforced values. Use for reference when tuning.
    //
    // EXPOSED vs EXPOSED:
    //   - Rifle: 25 dmg, 70% acc → ~3.5 shots to kill (100 HP)
    //   - Should feel dangerous, punish bad positioning
    //
    // EXPOSED vs COVERED:
    //   - Rifle: 25 dmg, 70% * 0.60 = 42% acc → ~5.7 shots to kill
    //   - Cover provides meaningful protection
    //
    // COVERED vs COVERED:
    //   - Both at 42% acc → ~9.5 shots each to kill
    //   - Creates stalemate, encourages flanking
    //
    // FLANKING:
    //   - Attacker bypasses cover → back to 70% acc
    //   - Rewards tactical movement
    
    // === Weapon Balance Philosophy ===
    //
    // Rifle: Balanced all-rounder
    //   - 25 dmg (4 hits to kill)
    //   - 70% accuracy
    //   - 8 range
    //   - 0.5s between shots
    //
    // Pistol: Backup weapon, fast but weak
    //   - 18 dmg (6 hits to kill)
    //   - 75% accuracy (more accurate at short range)
    //   - 5 range
    //   - 0.3s between shots
    //
    // SMG: Spray and pray
    //   - 15 dmg (7 hits to kill)
    //   - 55% accuracy (less accurate)
    //   - 6 range
    //   - 0.2s between shots (very fast)
    //
    // Shotgun: High risk/reward
    //   - 50 dmg (2 hits to kill!)
    //   - 85% accuracy
    //   - 4 range (must close distance)
    //   - 0.9s between shots (slow)
    
    // === Tuning Notes ===
    //
    // If combat feels too slow:
    //   - Increase weapon damage
    //   - Increase base accuracy
    //   - Decrease cover reduction values
    //
    // If combat feels too fast:
    //   - Decrease weapon damage
    //   - Increase HP values
    //   - Increase cover reduction values
    //
    // If cover feels useless:
    //   - Increase HalfCoverReduction (try 0.50)
    //
    // If cover feels too strong:
    //   - Decrease HalfCoverReduction (try 0.25)
    //   - Add more flanking routes to maps
}
