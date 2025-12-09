namespace FringeTactics;

/// <summary>
/// Result of an attack resolution.
/// </summary>
public struct AttackResult
{
    public int AttackerId { get; set; }
    public int TargetId { get; set; }
    public string WeaponName { get; set; }
    public bool Hit { get; set; }
    
    /// <summary>Final damage after armor reduction.</summary>
    public int Damage { get; set; }
    
    /// <summary>Raw weapon damage before armor.</summary>
    public int RawDamage { get; set; }
    
    /// <summary>Target's armor value.</summary>
    public int TargetArmor { get; set; }
    
    public float HitChance { get; set; }
    public CoverHeight TargetCoverHeight { get; set; }
    
    /// <summary>Convenience property: true if target has any cover.</summary>
    public bool TargetInCover => TargetCoverHeight != CoverHeight.None;
    
    /// <summary>Convenience property: damage absorbed by armor.</summary>
    public int ArmorAbsorbed => RawDamage - Damage;
}
