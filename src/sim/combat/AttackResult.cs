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
    public int Damage { get; set; }
    public float HitChance { get; set; }
    public CoverHeight TargetCoverHeight { get; set; }
    
    /// <summary>Convenience property: true if target has any cover.</summary>
    public bool TargetInCover => TargetCoverHeight != CoverHeight.None;
}
