namespace FringeTactics;

/// <summary>
/// Configuration for contract generation weights and rewards.
/// Extracted from ContractGenerator to enable data-driven balancing.
/// </summary>
public class GenerationConfig
{
    // === Contract Type Base Weights ===
    public int AssaultBaseWeight { get; set; } = 30;
    public int ExtractionBaseWeight { get; set; } = 20;
    public int DeliveryBaseWeight { get; set; } = 15;
    public int EscortBaseWeight { get; set; } = 10;
    public int RaidBaseWeight { get; set; } = 10;
    public int HeistBaseWeight { get; set; } = 5;

    // === Hub Metric Multipliers (0-5 scale) ===
    public int CrimeWeightMultiplier { get; set; } = 5;
    public int EconomyWeightMultiplier { get; set; } = 5;
    public int SecurityWeightMultiplier { get; set; } = 3;

    // === Base Rewards by Difficulty ===
    public int EasyBaseReward { get; set; } = 100;
    public int MediumBaseReward { get; set; } = 200;
    public int HardBaseReward { get; set; } = 400;

    // === Faction Modifiers ===
    public float FriendlyFactionBonus { get; set; } = 1.15f;
    public float HostileFactionPenalty { get; set; } = 0.85f;

    /// <summary>
    /// Default configuration with balanced values.
    /// </summary>
    public static GenerationConfig Default { get; } = new();
}
