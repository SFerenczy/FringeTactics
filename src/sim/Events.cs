using Godot;

namespace FringeTactics;

// ============================================================================
// EVENT TYPES FOR EVENTBUS
// 
// Active events (currently published):
//   - MissionCompletedEvent (CombatState.EndMission)
//   - MissionStartedEvent (GameState.StartMission) [MG3]
//   - MissionEndedEvent (GameState.EndMission) [MG3]
//   - ActorDiedEvent (CombatState.OnActorDied)
//   - DayAdvancedEvent (CampaignTime.AdvanceDays)
//   - ResourceChangedEvent (TravelSystem, CampaignState)
//   - JobAcceptedEvent (CampaignState.AcceptJob)
//   - JobCompletedEvent (CampaignState.ApplyMissionOutput) [MG3]
//   - TravelCompletedEvent (TravelExecutor)
//   - FactionRepChangedEvent (CampaignState.ModifyFactionRep)
//   - CrewDiedEvent (CampaignState.ApplyMissionOutput) [MG3]
//   - CrewInjuredEvent (CampaignState.ApplyMissionOutput) [MG3]
//   - CrewLeveledUpEvent (CampaignState.ApplyMissionOutput) [MG3]
//   - LootAcquiredEvent (CampaignState.ApplyMissionOutput) [MG3]
//
// TV2 Travel Execution events (TravelExecutor):
//   - TravelStartedEvent
//   - TravelSegmentStartedEvent
//   - TravelSegmentCompletedEvent
//   - TravelEncounterTriggeredEvent
//   - TravelEncounterResolvedEvent
//   - TravelCompletedEvent (consolidated)
//   - TravelInterruptedEvent
//   - PlayerMovedEvent
//
// EN1 Encounter events (EncounterRunner):
//   - EncounterStartedEvent
//   - EncounterNodeEnteredEvent
//   - EncounterOptionSelectedEvent
//   - EncounterCompletedEvent
//
// Planned events (defined but not yet published):
//   - MissionPhaseChangedEvent
//   - AlarmStateChangedEvent
// ============================================================================

// ============================================================================
// MISSION EVENTS
// ============================================================================

/// <summary>
/// Published when a mission ends (victory, defeat, or retreat).
/// </summary>
public readonly record struct MissionCompletedEvent(
    MissionOutcome Outcome,
    int EnemiesKilled,
    int CrewDeaths,
    int CrewInjured,
    float DurationSeconds
);

/// <summary>
/// Published when a mission phase changes.
/// </summary>
public readonly record struct MissionPhaseChangedEvent(
    MissionPhase OldPhase,
    MissionPhase NewPhase
);

// ============================================================================
// COMBAT EVENTS (cross-domain relevant)
// ============================================================================

/// <summary>
/// Published when an actor dies during combat.
/// </summary>
public readonly record struct ActorDiedEvent(
    int ActorId,
    ActorType ActorType,
    string ActorName,
    int KillerId,
    Vector2I Position
);

/// <summary>
/// Published when the alarm state changes.
/// </summary>
public readonly record struct AlarmStateChangedEvent(
    AlarmState OldState,
    AlarmState NewState
);

// ============================================================================
// CAMPAIGN EVENTS
// ============================================================================

/// <summary>
/// Resource type enum for type-safe resource handling.
/// </summary>
public enum ResourceType
{
    Money,
    Fuel,
    Ammo,
    Parts,
    Meds
}

/// <summary>
/// Resource type constants for ResourceChangedEvent (legacy compatibility).
/// </summary>
public static class ResourceTypes
{
    public const string Money = "money";
    public const string Fuel = "fuel";
    public const string Ammo = "ammo";
    public const string Parts = "parts";
    public const string Meds = "meds";
    
    /// <summary>
    /// Convert enum to string constant.
    /// </summary>
    public static string FromEnum(ResourceType type) => type switch
    {
        ResourceType.Money => Money,
        ResourceType.Fuel => Fuel,
        ResourceType.Ammo => Ammo,
        ResourceType.Parts => Parts,
        ResourceType.Meds => Meds,
        _ => null
    };
    
    /// <summary>
    /// Parse string to enum (case-insensitive).
    /// </summary>
    public static ResourceType? ToEnum(string type) => type?.ToLower() switch
    {
        "money" or "credits" => ResourceType.Money,
        "fuel" => ResourceType.Fuel,
        "ammo" => ResourceType.Ammo,
        "parts" => ResourceType.Parts,
        "meds" => ResourceType.Meds,
        _ => null
    };
}

/// <summary>
/// Published when campaign day advances.
/// </summary>
public readonly record struct DayAdvancedEvent(
    int OldDay,
    int NewDay,
    int DaysAdvanced
);

/// <summary>
/// Published when a campaign resource changes.
/// </summary>
public readonly record struct ResourceChangedEvent(
    string ResourceType,
    int OldValue,
    int NewValue,
    int Delta,
    string Reason
);

/// <summary>
/// Published when a job is accepted.
/// </summary>
public readonly record struct JobAcceptedEvent(
    string JobId,
    string JobTitle,
    int TargetNodeId,
    int DeadlineDay
);

/// <summary>
/// Published when a job is completed (success or failure).
/// </summary>
public readonly record struct JobCompletedEvent(
    string JobId,
    string JobTitle,
    bool Success,
    int MoneyReward
);

/// <summary>
/// Published when travel execution completes successfully.
/// </summary>
public readonly record struct TravelCompletedEvent(
    int FromSystemId,
    int ToSystemId,
    int TotalDays,
    int TotalFuel,
    int EncounterCount
);

/// <summary>
/// Published when faction reputation changes.
/// </summary>
public readonly record struct FactionRepChangedEvent(
    string FactionId,
    string FactionName,
    int OldRep,
    int NewRep,
    int Delta
);

// ============================================================================
// CREW EVENTS
// ============================================================================

/// <summary>
/// Published when a crew member levels up.
/// </summary>
public readonly record struct CrewLeveledUpEvent(
    int CrewId,
    string CrewName,
    int OldLevel,
    int NewLevel
);

/// <summary>
/// Published when a crew member gains an injury.
/// </summary>
public readonly record struct CrewInjuredEvent(
    int CrewId,
    string CrewName,
    string InjuryType
);

/// <summary>
/// Published when a crew member dies.
/// </summary>
public readonly record struct CrewDiedEvent(
    int CrewId,
    string CrewName,
    string Cause
);

/// <summary>
/// Published when a crew member is hired.
/// </summary>
public readonly record struct CrewHiredEvent(
    int CrewId,
    string CrewName,
    CrewRole Role,
    int Cost
);

/// <summary>
/// Published when a crew member is fired.
/// </summary>
public readonly record struct CrewFiredEvent(
    int CrewId,
    string CrewName
);

/// <summary>
/// Published when a crew member gains or loses a trait.
/// </summary>
public readonly record struct CrewTraitChangedEvent(
    int CrewId,
    string CrewName,
    string TraitId,
    string TraitName,
    bool Gained
);

// ============================================================================
// INVENTORY EVENTS (MG2)
// ============================================================================

/// <summary>
/// Published when an item is added to inventory.
/// </summary>
public readonly record struct ItemAddedEvent(
    string ItemId,
    string DefId,
    string ItemName,
    int Quantity
);

/// <summary>
/// Published when an item is removed from inventory.
/// </summary>
public readonly record struct ItemRemovedEvent(
    string ItemId,
    string DefId,
    string ItemName,
    int Quantity
);

/// <summary>
/// Published when an item is equipped by a crew member.
/// </summary>
public readonly record struct ItemEquippedEvent(
    int CrewId,
    string CrewName,
    string ItemId,
    string DefId,
    string Slot
);

/// <summary>
/// Published when an item is unequipped by a crew member.
/// </summary>
public readonly record struct ItemUnequippedEvent(
    int CrewId,
    string CrewName,
    string DefId,
    string Slot
);

// ============================================================================
// SHIP EVENTS (MG2)
// ============================================================================

/// <summary>
/// Published when ship hull changes.
/// </summary>
public readonly record struct ShipHullChangedEvent(
    int OldHull,
    int NewHull,
    int MaxHull,
    string Reason
);

/// <summary>
/// Published when a ship module is installed.
/// </summary>
public readonly record struct ShipModuleInstalledEvent(
    string ModuleId,
    string ModuleDefId,
    string ModuleName,
    string SlotType
);

/// <summary>
/// Published when a ship module is removed.
/// </summary>
public readonly record struct ShipModuleRemovedEvent(
    string ModuleId,
    string ModuleDefId,
    string ModuleName,
    string SlotType
);

// ============================================================================
// MISSION INTEGRATION EVENTS (MG3)
// ============================================================================

/// <summary>
/// Published when a mission starts.
/// </summary>
public readonly record struct MissionStartedEvent(
    string MissionId,
    string MissionName,
    int CrewCount,
    int EnemyCount
);

/// <summary>
/// Published when a mission ends with detailed results.
/// </summary>
public readonly record struct MissionEndedEvent(
    string MissionId,
    MissionOutcome Outcome,
    int CrewSurvived,
    int CrewLost,
    int EnemiesKilled
);

/// <summary>
/// Published when loot is acquired from a mission.
/// </summary>
public readonly record struct LootAcquiredEvent(
    string ItemDefId,
    string ItemName,
    int Quantity,
    string Source
);

// ============================================================================
// TRAVEL EXECUTION EVENTS (TV2)
// ============================================================================

/// <summary>
/// Published when travel execution begins.
/// </summary>
public readonly record struct TravelStartedEvent(
    int FromSystemId,
    int ToSystemId,
    int EstimatedDays,
    int EstimatedFuel
);

/// <summary>
/// Published when a travel segment begins.
/// </summary>
public readonly record struct TravelSegmentStartedEvent(
    int FromSystemId,
    int ToSystemId,
    int SegmentIndex,
    int SegmentDays
);

/// <summary>
/// Published when a travel segment completes.
/// </summary>
public readonly record struct TravelSegmentCompletedEvent(
    int FromSystemId,
    int ToSystemId,
    int FuelConsumed,
    int DaysElapsed
);

/// <summary>
/// Published when an encounter is triggered during travel.
/// </summary>
public readonly record struct TravelEncounterTriggeredEvent(
    int SystemId,
    string EncounterType,
    string EncounterId
);

/// <summary>
/// Published when a travel encounter is resolved.
/// </summary>
public readonly record struct TravelEncounterResolvedEvent(
    string EncounterId,
    string Outcome
);

/// <summary>
/// Published when travel execution is interrupted.
/// </summary>
public readonly record struct TravelInterruptedEvent(
    int CurrentSystemId,
    string Reason
);

/// <summary>
/// Published when player position changes during travel.
/// </summary>
public readonly record struct PlayerMovedEvent(
    int FromSystemId,
    int ToSystemId,
    string SystemName
);

// ============================================================================
// ENCOUNTER EVENTS (EN1)
// ============================================================================

/// <summary>
/// Published when an encounter starts.
/// </summary>
public readonly record struct EncounterStartedEvent(
    string EncounterId,
    string TemplateId,
    string TemplateName
);

/// <summary>
/// Published when entering a new node in an encounter.
/// </summary>
public readonly record struct EncounterNodeEnteredEvent(
    string EncounterId,
    string NodeId,
    bool RequiresInput
);

/// <summary>
/// Published when the player selects an option in an encounter.
/// </summary>
public readonly record struct EncounterOptionSelectedEvent(
    string EncounterId,
    string NodeId,
    string OptionId,
    int OptionIndex
);

/// <summary>
/// Published when an encounter completes.
/// </summary>
public readonly record struct EncounterCompletedEvent(
    string EncounterId,
    string TemplateId,
    int EffectCount,
    int NodesVisited
);

/// <summary>
/// Published when a skill check is resolved during an encounter (EN2).
/// </summary>
public readonly record struct SkillCheckResolvedEvent(
    string EncounterId,
    string CrewName,
    string StatName,
    int Difficulty,
    int Roll,
    int StatValue,
    int TraitBonus,
    int Total,
    bool Success,
    int Margin,
    bool IsCriticalSuccess,
    bool IsCriticalFailure
);
