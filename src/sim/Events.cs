using Godot;

namespace FringeTactics;

// ============================================================================
// EVENT TYPES FOR EVENTBUS
// 
// Active events (currently published):
//   - MissionCompletedEvent (CombatState.EndMission)
//   - ActorDiedEvent (CombatState.OnActorDied)
//   - DayAdvancedEvent (CampaignTime.AdvanceDays)
//   - ResourceChangedEvent (TravelSystem, CampaignState)
//   - JobAcceptedEvent (CampaignState.AcceptJob)
//   - TravelCompletedEvent (TravelSystem.Travel)
//   - FactionRepChangedEvent (CampaignState.ModifyFactionRep)
//
// Planned events (defined but not yet published):
//   - MissionPhaseChangedEvent
//   - AlarmStateChangedEvent
//   - JobCompletedEvent
//   - CrewLeveledUpEvent, CrewInjuredEvent, CrewDiedEvent
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
/// Resource type constants for ResourceChangedEvent.
/// </summary>
public static class ResourceTypes
{
    public const string Money = "money";
    public const string Fuel = "fuel";
    public const string Ammo = "ammo";
    public const string Parts = "parts";
    public const string Meds = "meds";
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
/// Published when player travels to a new node.
/// </summary>
public readonly record struct TravelCompletedEvent(
    int FromNodeId,
    int ToNodeId,
    string ToNodeName,
    int FuelCost,
    int DaysCost
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
