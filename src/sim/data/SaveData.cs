using System;
using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// Current save format version.
/// </summary>
public static class SaveVersion
{
    public const int Current = 3;

    // Version history:
    // 1 - Initial save format (SF3)
    // 2 - MG1: Expanded crew stats (Grit, Tech, Savvy, Resolve), stat points
    // 3 - MG2: Ship with modules, inventory, equipment
}

/// <summary>
/// Top-level save file structure with versioning.
/// </summary>
public class SaveData
{
    /// <summary>
    /// Save format version. Increment when structure changes.
    /// </summary>
    public int Version { get; set; } = SaveVersion.Current;

    /// <summary>
    /// When the save was created (UTC).
    /// </summary>
    public DateTime SavedAt { get; set; }

    /// <summary>
    /// Display name for the save (e.g., sector name + day).
    /// </summary>
    public string DisplayName { get; set; }

    /// <summary>
    /// The campaign state data.
    /// </summary>
    public CampaignStateData Campaign { get; set; }
}

/// <summary>
/// Serializable campaign state for save/load.
/// </summary>
public class CampaignStateData
{
    // Time
    public CampaignTimeState Time { get; set; }

    // RNG
    public RngServiceState Rng { get; set; }

    // Resources
    public ResourcesData Resources { get; set; }

    // Location
    public int CurrentNodeId { get; set; }

    // Crew
    public List<CrewMemberData> Crew { get; set; } = new();
    public int NextCrewId { get; set; }

    // Sector (world state)
    public SectorData Sector { get; set; }

    // World state (WD1)
    public WorldStateData World { get; set; }

    // Jobs
    public List<JobData> AvailableJobs { get; set; } = new();
    public JobData CurrentJob { get; set; }
    public int NextJobId { get; set; }

    // Faction reputation
    public Dictionary<string, int> FactionRep { get; set; } = new();

    // Statistics
    public CampaignStatsData Stats { get; set; }

    // Ship (MG2)
    public ShipData Ship { get; set; }

    // Inventory (MG2)
    public InventoryData Inventory { get; set; }
}

/// <summary>
/// Campaign resources snapshot.
/// </summary>
public class ResourcesData
{
    public int Money { get; set; }
    public int Fuel { get; set; }
    public int Parts { get; set; }
    public int Meds { get; set; }
    public int Ammo { get; set; }
}

/// <summary>
/// Campaign statistics snapshot.
/// </summary>
public class CampaignStatsData
{
    public int MissionsCompleted { get; set; }
    public int MissionsFailed { get; set; }
    public int TotalMoneyEarned { get; set; }
    public int TotalCrewDeaths { get; set; }
}

/// <summary>
/// Serializable crew member state.
/// </summary>
public class CrewMemberData
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Role { get; set; }

    // Status
    public bool IsDead { get; set; }
    public List<string> Injuries { get; set; } = new();

    // Progression
    public int Level { get; set; }
    public int Xp { get; set; }
    public int UnspentStatPoints { get; set; }

    // Primary stats (MG1)
    public int Grit { get; set; }
    public int Reflexes { get; set; }
    public int Aim { get; set; }
    public int Tech { get; set; }
    public int Savvy { get; set; }
    public int Resolve { get; set; }

    // Legacy field for backward compatibility (v1 saves)
    public int Toughness { get; set; }

    // Traits (MG1 Phase 2)
    public List<string> TraitIds { get; set; } = new();

    // Equipment preference (legacy)
    public string PreferredWeaponId { get; set; }

    // Equipment slots (MG2) - item instance IDs
    public string EquippedWeaponId { get; set; }
    public string EquippedArmorId { get; set; }
    public string EquippedGadgetId { get; set; }
}

/// <summary>
/// Serializable sector state.
/// </summary>
public class SectorData
{
    public string Name { get; set; }
    public List<SectorNodeData> Nodes { get; set; } = new();
    public Dictionary<string, string> Factions { get; set; } = new();
}

/// <summary>
/// Serializable sector node state.
/// </summary>
public class SectorNodeData
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Type { get; set; }
    public string FactionId { get; set; }
    public float PositionX { get; set; }
    public float PositionY { get; set; }
    public List<int> Connections { get; set; } = new();
    public bool HasJob { get; set; }
}

/// <summary>
/// Serializable job state.
/// </summary>
public class JobData
{
    public string Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string Type { get; set; }
    public string Difficulty { get; set; }

    // Location
    public int OriginNodeId { get; set; }
    public int TargetNodeId { get; set; }

    // Faction
    public string EmployerFactionId { get; set; }
    public string TargetFactionId { get; set; }

    // Rewards
    public JobRewardData Reward { get; set; }
    public int RepGain { get; set; }
    public int RepLoss { get; set; }
    public int FailureRepLoss { get; set; }

    // Deadline
    public int DeadlineDays { get; set; }
    public int DeadlineDay { get; set; }

    // Mission config seed (for deterministic regeneration)
    public int MissionConfigSeed { get; set; }
}

/// <summary>
/// Serializable job reward.
/// </summary>
public class JobRewardData
{
    public int Money { get; set; }
    public int Parts { get; set; }
    public int Fuel { get; set; }
    public int Ammo { get; set; }
}

/// <summary>
/// Serializable ship state (MG2).
/// </summary>
public class ShipData
{
    public string ChassisId { get; set; }
    public string Name { get; set; }
    public int Hull { get; set; }
    public int MaxHull { get; set; }
    public int EngineSlots { get; set; }
    public int WeaponSlots { get; set; }
    public int CargoSlots { get; set; }
    public int UtilitySlots { get; set; }
    public List<ShipModuleData> Modules { get; set; } = new();
}

/// <summary>
/// Serializable ship module state (MG2).
/// </summary>
public class ShipModuleData
{
    public string Id { get; set; }
    public string DefId { get; set; }
    public string Name { get; set; }
    public string SlotType { get; set; }
    public int CargoBonus { get; set; }
    public int FuelEfficiency { get; set; }
}

/// <summary>
/// Serializable inventory state (MG2).
/// </summary>
public class InventoryData
{
    public List<ItemData> Items { get; set; } = new();
    public int NextItemId { get; set; }
}

/// <summary>
/// Serializable item instance (MG2).
/// </summary>
public class ItemData
{
    public string Id { get; set; }
    public string DefId { get; set; }
    public int Quantity { get; set; }
}

/// <summary>
/// Lightweight save metadata for UI display.
/// </summary>
public class SaveMetadata
{
    public string DisplayName { get; set; }
    public DateTime SavedAt { get; set; }
    public int Version { get; set; }
    public int Day { get; set; }
    public int CrewCount { get; set; }
}

/// <summary>
/// Save slot information for UI.
/// </summary>
public class SaveSlotInfo
{
    public int Slot { get; set; }
    public bool IsAutosave { get; set; }
    public bool Exists { get; set; }
    public SaveMetadata Metadata { get; set; }
}
