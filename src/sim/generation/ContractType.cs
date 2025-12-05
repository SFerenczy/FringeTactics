namespace FringeTactics;

/// <summary>
/// Contract archetypes defining mission structure and objectives.
/// Currently implemented: Assault, Extraction.
/// Future types are commented out until tactical layer supports them.
/// </summary>
public enum ContractType
{
    /// <summary>
    /// Eliminate all hostiles at target location.
    /// Primary: Kill all enemies. Stretch: Time bonus, no casualties.
    /// </summary>
    Assault,

    /// <summary>
    /// Locate and extract target person(s).
    /// Primary: At least one target extracted. Stretch: All targets, no casualties.
    /// </summary>
    Extraction

    // === FUTURE CONTRACT TYPES ===
    // Uncomment when tactical layer implementation is ready.
    // Adding a type here will cause compiler errors in switch statements,
    // ensuring all code paths are updated.

    // /// <summary>
    // /// Transport cargo from spawn to extraction zone.
    // /// Primary: Cargo reaches extraction. Stretch: Cargo undamaged, time bonus.
    // /// </summary>
    // Delivery,

    // /// <summary>
    // /// Keep VIP alive until extraction.
    // /// Primary: VIP survives. Stretch: VIP uninjured, eliminate all threats.
    // /// </summary>
    // Escort,

    // /// <summary>
    // /// Destroy or steal specific target object.
    // /// Primary: Target acquired/destroyed + crew extracts. Stretch: Secondary targets.
    // /// </summary>
    // Raid,

    // /// <summary>
    // /// Acquire target without triggering alarm.
    // /// Primary: Target acquired + crew extracts. Stretch: Ghost (no alarm), no kills.
    // /// </summary>
    // Heist,
}

/// <summary>
/// Extension methods for ContractType.
/// </summary>
public static class ContractTypeExtensions
{
    /// <summary>
    /// Get display name for contract type.
    /// </summary>
    public static string GetDisplayName(this ContractType type) => type switch
    {
        ContractType.Assault => "Assault",
        ContractType.Extraction => "Extraction",
        _ => "Unknown"
    };

    /// <summary>
    /// Get reward multiplier for contract type.
    /// </summary>
    public static float GetRewardMultiplier(this ContractType type) => type switch
    {
        ContractType.Assault => 1.0f,
        ContractType.Extraction => 1.2f,
        _ => 1.0f
    };

}
