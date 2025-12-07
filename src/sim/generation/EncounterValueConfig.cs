namespace FringeTactics;

/// <summary>
/// Configuration for encounter effect values.
/// Allows balancing rewards/costs without code changes.
/// </summary>
public class EncounterValueConfig
{
    // === Combat Rewards/Costs ===
    public int FightDamage { get; set; } = 15;
    public int FightReward { get; set; } = 100;
    public int FleeFailDamage { get; set; } = 25;
    public int SurrenderCost { get; set; } = 50;

    // === Patrol Costs ===
    public int BribeCost { get; set; } = 30;
    public int BribeFailFine { get; set; } = 50;
    public int FleeingFine { get; set; } = 100;

    // === Rescue Rewards ===
    public int RescueReward { get; set; } = 75;
    public int RescueXp { get; set; } = 15;
    public int AmbushDamage { get; set; } = 20;

    // === Trade Values ===
    public int FuelPrice { get; set; } = 50;
    public int FuelAmount { get; set; } = 20;
    public int DiscountFuelPrice { get; set; } = 35;
    public int SuppliesPrice { get; set; } = 30;

    // === Smuggler Values ===
    public int SmugglerPayment { get; set; } = 150;
    public int IntimidationReward { get; set; } = 50;
    public int HostileDamage { get; set; } = 10;

    // === Salvage Values ===
    public int SalvageCredits { get; set; } = 100;
    public int SalvageFuel { get; set; } = 10;
    public int SafeSalvageCredits { get; set; } = 120;
    public int SafeSalvageFuel { get; set; } = 15;
    public int SafeSalvageXp { get; set; } = 10;
    public int HazardCredits { get; set; } = 50;

    // === Faction Values ===
    public int FactionBaseReward { get; set; } = 100;
    public int FactionNegotiatedReward { get; set; } = 150;

    // === Mystery Values ===
    public int MysteryReward { get; set; } = 200;
    public int MysteryXp { get; set; } = 25;
    public int DecodedReward { get; set; } = 250;
    public int DecodedXp { get; set; } = 30;

    // === Mechanical Values ===
    public int JuryRigDamage { get; set; } = 5;
    public int PartialRepairDamage { get; set; } = 10;
    public int RepairXp { get; set; } = 10;
    public int PartsRepairCost { get; set; } = 40;

    // === Refugee Values ===
    public int HelpCost { get; set; } = 25;
    public int HelpXp { get; set; } = 15;
    public int PassageFuelCost { get; set; } = 10;
    public int PassagePayment { get; set; } = 50;

    // === Skill Check Difficulties ===
    public int EasyDifficulty { get; set; } = 5;
    public int MediumDifficulty { get; set; } = 6;
    public int HardDifficulty { get; set; } = 7;
    public int VeryHardDifficulty { get; set; } = 8;

    /// <summary>Default configuration.</summary>
    public static EncounterValueConfig Default { get; } = new();
}
