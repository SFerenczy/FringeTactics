using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// Mission outcome type.
/// </summary>
public enum MissionOutcome
{
    /// <summary>Primary objectives complete.</summary>
    Victory,
    
    /// <summary>All crew eliminated.</summary>
    Defeat,
    
    /// <summary>Player voluntarily extracted via entry zone.</summary>
    Retreat,
    
    /// <summary>Mission cancelled before meaningful progress.</summary>
    Abort
}

/// <summary>
/// Status of an individual objective.
/// </summary>
public enum ObjectiveStatus
{
    /// <summary>Not started.</summary>
    Pending,
    
    /// <summary>Being worked on.</summary>
    InProgress,
    
    /// <summary>Successfully completed.</summary>
    Complete,
    
    /// <summary>Cannot be completed.</summary>
    Failed
}

/// <summary>
/// Complete results from a tactical mission.
/// This is the formal contract for what tactical returns to campaign.
/// </summary>
public class MissionOutput
{
    // === Outcome ===
    
    /// <summary>
    /// How the mission ended.
    /// </summary>
    public MissionOutcome Outcome { get; set; }
    
    /// <summary>
    /// Mission ID matching the input.
    /// </summary>
    public string MissionId { get; set; }

    // === Per-Crew Results ===
    
    /// <summary>
    /// Outcome for each crew member who participated.
    /// </summary>
    public List<CrewOutcome> CrewOutcomes { get; set; } = new();

    // === Objective Results ===
    
    /// <summary>
    /// Status of each objective by ID.
    /// </summary>
    public Dictionary<string, ObjectiveStatus> ObjectiveResults { get; set; } = new();

    // === Statistics ===
    
    public int EnemiesKilled { get; set; }
    public int EnemiesRemaining { get; set; }
    public bool AlarmTriggered { get; set; }
    public int TicksElapsed { get; set; }
    public float MissionDurationSeconds { get; set; }

    // === Loot (future) ===
    
    /// <summary>
    /// Items acquired during the mission.
    /// </summary>
    public List<LootItem> Loot { get; set; } = new();

    // === World Deltas (future) ===
    
    /// <summary>
    /// Changes to world state caused by mission outcome.
    /// </summary>
    public List<WorldDelta> WorldDeltas { get; set; } = new();
}

/// <summary>
/// Outcome for a single crew member.
/// </summary>
public class CrewOutcome
{
    /// <summary>
    /// ID linking back to campaign crew.
    /// </summary>
    public int CampaignCrewId { get; set; }
    
    public string Name { get; set; }

    // === Status ===
    
    /// <summary>
    /// Final status of this crew member.
    /// </summary>
    public CrewFinalStatus Status { get; set; }

    // === Health ===
    
    public int FinalHp { get; set; }
    public int MaxHp { get; set; }
    public int DamageTaken { get; set; }

    // === Ammo ===
    
    public int AmmoRemaining { get; set; }
    public int AmmoUsed { get; set; }

    // === Combat Stats ===
    
    public int Kills { get; set; }
    public int ShotsFired { get; set; }
    public int ShotsHit { get; set; }

    // === XP ===
    
    /// <summary>
    /// Suggested XP based on tactical performance.
    /// Campaign layer may adjust this.
    /// </summary>
    public int SuggestedXp { get; set; }

    // === Injuries ===
    
    /// <summary>
    /// New injuries acquired during mission.
    /// </summary>
    public List<string> NewInjuries { get; set; } = new();
}

/// <summary>
/// Final status of a crew member after mission.
/// </summary>
public enum CrewFinalStatus
{
    /// <summary>Survived, healthy.</summary>
    Alive,
    
    /// <summary>Survived, took significant damage.</summary>
    Wounded,
    
    /// <summary>Survived, near death.</summary>
    Critical,
    
    /// <summary>Killed in action.</summary>
    Dead,
    
    /// <summary>Left behind during retreat (didn't reach extraction).</summary>
    MIA
}

/// <summary>
/// Type of loot item.
/// </summary>
public enum LootType
{
    /// <summary>Currency reward.</summary>
    Credits,
    
    /// <summary>Inventory item.</summary>
    Item,
    
    /// <summary>Campaign resource (fuel, ammo, parts, meds).</summary>
    Resource
}

/// <summary>
/// Loot item acquired during mission.
/// </summary>
public class LootItem
{
    /// <summary>
    /// Type of loot.
    /// </summary>
    public LootType Type { get; set; }
    
    /// <summary>
    /// Item definition ID (for Type == Item).
    /// </summary>
    public string ItemDefId { get; set; }
    
    /// <summary>
    /// Display name.
    /// </summary>
    public string Name { get; set; }
    
    /// <summary>
    /// Amount (for credits/resources) or quantity (for items).
    /// </summary>
    public int Quantity { get; set; } = 1;
    
    /// <summary>
    /// Resource type enum (for Type == Resource).
    /// </summary>
    public ResourceType? ResourceKind { get; set; }
    
    /// <summary>
    /// Tags for filtering.
    /// </summary>
    public List<string> Tags { get; set; } = new();
    
    // === Factory Methods ===
    
    /// <summary>
    /// Create a credits loot item.
    /// </summary>
    public static LootItem Credits(int amount) => new()
    {
        Type = LootType.Credits,
        Name = $"{amount} Credits",
        Quantity = amount
    };
    
    /// <summary>
    /// Create an item loot.
    /// </summary>
    public static LootItem Item(string defId, int quantity = 1)
    {
        var def = ItemRegistry.Get(defId);
        return new LootItem
        {
            Type = LootType.Item,
            ItemDefId = defId,
            Name = def?.Name ?? defId,
            Quantity = quantity
        };
    }
    
    /// <summary>
    /// Create a resource loot (type-safe enum version).
    /// </summary>
    public static LootItem Resource(ResourceType resourceType, int amount) => new()
    {
        Type = LootType.Resource,
        ResourceKind = resourceType,
        Name = $"{amount} {resourceType}",
        Quantity = amount
    };
    
    /// <summary>
    /// Create a resource loot (string version for compatibility).
    /// </summary>
    public static LootItem Resource(string resourceType, int amount)
    {
        var parsed = ResourceTypes.ToEnum(resourceType);
        return new LootItem
        {
            Type = LootType.Resource,
            ResourceKind = parsed,
            Name = $"{amount} {resourceType}",
            Quantity = amount
        };
    }
}

/// <summary>
/// A change to world state caused by mission outcome.
/// </summary>
public class WorldDelta
{
    /// <summary>
    /// Type of change (e.g., "reputation", "security", "faction_relation").
    /// </summary>
    public string Type { get; set; }
    
    /// <summary>
    /// Target of the change (faction ID, location ID, etc.).
    /// </summary>
    public string TargetId { get; set; }
    
    /// <summary>
    /// Magnitude of change.
    /// </summary>
    public int Delta { get; set; }
    
    /// <summary>
    /// Human-readable reason for the change.
    /// </summary>
    public string Reason { get; set; }
}
