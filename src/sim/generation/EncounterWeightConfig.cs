namespace FringeTactics;

/// <summary>
/// Configuration for encounter selection weights.
/// Allows balancing without code changes.
/// </summary>
public class EncounterWeightConfig
{
    // === Base Weights ===
    public float BaseWeight { get; set; } = 1.0f;
    public float MinWeight { get; set; } = 0.1f;

    // === Type Modifiers ===
    // Formula: baseMultiplier + (metricValue * metricMultiplier)

    /// <summary>Pirate weight = 0.5 + (criminalActivity * 0.3)</summary>
    public float PirateBaseMultiplier { get; set; } = 0.5f;
    public float PirateMetricMultiplier { get; set; } = 0.3f;

    /// <summary>Patrol weight = 0.5 + (securityLevel * 0.3)</summary>
    public float PatrolBaseMultiplier { get; set; } = 0.5f;
    public float PatrolMetricMultiplier { get; set; } = 0.3f;

    /// <summary>Trader weight = 0.5 + (economicActivity * 0.25)</summary>
    public float TraderBaseMultiplier { get; set; } = 0.5f;
    public float TraderMetricMultiplier { get; set; } = 0.25f;

    /// <summary>Smuggler security penalty = (5 - security) * 0.2</summary>
    public float SmugglerSecurityMultiplier { get; set; } = 0.2f;
    public float SmugglerCrimeBaseMultiplier { get; set; } = 0.5f;
    public float SmugglerCrimeMultiplier { get; set; } = 0.2f;

    // === Context Modifiers ===

    /// <summary>Cargo encounters get this boost when player has valuable cargo (>100).</summary>
    public float CargoValueBoost { get; set; } = 1.5f;
    public int CargoValueThreshold { get; set; } = 100;

    /// <summary>Combat weight = 0.8 + (routeHazard * 0.15)</summary>
    public float CombatBaseMultiplier { get; set; } = 0.8f;
    public float CombatHazardMultiplier { get; set; } = 0.15f;

    /// <summary>Rare encounters get this multiplier (lower = less likely).</summary>
    public float RareMultiplier { get; set; } = 0.3f;

    /// <summary>Suggested type gets this boost.</summary>
    public float SuggestedTypeBoost { get; set; } = 2.0f;

    /// <summary>Faction encounters get this boost in faction territory.</summary>
    public float FactionTerritoryBoost { get; set; } = 1.3f;

    /// <summary>Distress signals get this multiplier (slightly less common).</summary>
    public float DistressMultiplier { get; set; } = 0.7f;

    /// <summary>Default configuration.</summary>
    public static EncounterWeightConfig Default { get; } = new();
}
