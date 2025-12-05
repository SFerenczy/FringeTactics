namespace FringeTactics;

/// <summary>
/// Contract archetypes defining mission structure and objectives.
/// Per GN0 design: 6 core archetypes with distinct gameplay.
/// </summary>
public enum ContractType
{
    /// <summary>
    /// Eliminate all hostiles at target location.
    /// Primary: Kill all enemies. Stretch: Time bonus, no casualties.
    /// </summary>
    Assault,

    /// <summary>
    /// Transport cargo from spawn to extraction zone.
    /// Primary: Cargo reaches extraction. Stretch: Cargo undamaged, time bonus.
    /// </summary>
    Delivery,

    /// <summary>
    /// Keep VIP alive until extraction.
    /// Primary: VIP survives. Stretch: VIP uninjured, eliminate all threats.
    /// </summary>
    Escort,

    /// <summary>
    /// Destroy or steal specific target object.
    /// Primary: Target acquired/destroyed + crew extracts. Stretch: Secondary targets.
    /// </summary>
    Raid,

    /// <summary>
    /// Acquire target without triggering alarm.
    /// Primary: Target acquired + crew extracts. Stretch: Ghost (no alarm), no kills.
    /// </summary>
    Heist,

    /// <summary>
    /// Locate and extract target person(s).
    /// Primary: At least one target extracted. Stretch: All targets, no casualties.
    /// </summary>
    Extraction
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
        ContractType.Delivery => "Delivery",
        ContractType.Escort => "Escort",
        ContractType.Raid => "Raid",
        ContractType.Heist => "Heist",
        ContractType.Extraction => "Extraction",
        _ => "Unknown"
    };

    /// <summary>
    /// Get reward multiplier for contract type (per GN0 design).
    /// </summary>
    public static float GetRewardMultiplier(this ContractType type) => type switch
    {
        ContractType.Assault => 1.0f,
        ContractType.Delivery => 1.1f,
        ContractType.Escort => 1.2f,
        ContractType.Raid => 1.3f,
        ContractType.Heist => 1.4f,
        ContractType.Extraction => 1.2f,
        _ => 1.0f
    };

    /// <summary>
    /// Check if contract type is currently implemented in tactical layer.
    /// </summary>
    public static bool IsImplemented(this ContractType type) => type switch
    {
        ContractType.Assault => true,
        ContractType.Extraction => true,
        _ => false
    };
}
