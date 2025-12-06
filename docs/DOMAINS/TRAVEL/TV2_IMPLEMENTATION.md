# TV2 – Travel Execution: Implementation Plan

**Status**: ✅ Complete  
**Depends on**: TV1 (Route Planning) ✅ Complete  
**Soft dependencies**: EN1 (Encounter Runtime), MG4 (Encounter Integration) – stubbed  
**Phase**: G2

---

## Overview

**Goal**: Execute travel plans by consuming resources, advancing time, and triggering encounters. This milestone transforms route planning into actual gameplay by making travel have consequences.

TV2 provides:
- Travel execution with fuel consumption and time advancement
- Encounter triggering based on risk calculations from TV1
- Pause/resume capability for encounter resolution
- Stranded state handling when fuel runs out
- Travel events for UI and logging

---

## Current State Assessment

### What We Have (from TV0, TV1)

| Component | Status | Location | Notes |
|-----------|--------|----------|-------|
| `TravelPlanner` | ✅ Complete | `src/sim/travel/TravelPlanner.cs` | A* pathfinding, plan creation |
| `TravelPlan` | ✅ Complete | `src/sim/travel/TravelPlan.cs` | Segments, aggregates, validation |
| `TravelSegment` | ✅ Complete | `src/sim/travel/TravelSegment.cs` | Per-hop costs and risk |
| `TravelCosts` | ✅ Complete | `src/sim/travel/TravelCosts.cs` | Cost formulas, encounter chance |
| `CampaignState.SpendFuel()` | ✅ Complete | `src/sim/campaign/CampaignState.cs` | Fuel consumption with events |
| `CampaignState.Fuel` | ✅ Complete | `src/sim/campaign/CampaignState.cs` | Current fuel resource |
| `CampaignState.CurrentNodeId` | ✅ Complete | `src/sim/campaign/CampaignState.cs` | Current system location |
| `CampaignTime` | ✅ Complete | `src/sim/CampaignTime.cs` | Day-based time tracking |
| `RngService` | ✅ Complete | `src/sim/RngService.cs` | Deterministic RNG |
| `EventBus` | ✅ Complete | `src/sim/EventBus.cs` | Cross-domain events |
| TV0 formulas | ✅ Complete | `TV0_IMPLEMENTATION.md` | Encounter trigger timing, edge cases |

### TV2 Implementation Status

| Requirement | Status | Location |
|-------------|--------|----------|
| `TravelExecutor` class | ✅ Complete | `src/sim/travel/TravelExecutor.cs` |
| `TravelState` class | ✅ Complete | `src/sim/travel/TravelState.cs` |
| `TravelExecutionResult` class | ✅ Complete | `src/sim/travel/TravelResult.cs` |
| `TravelContext` class | ✅ Complete | `src/sim/travel/TravelContext.cs` |
| Travel events | ✅ Complete | `src/sim/Events.cs` (8 events) |
| Encounter stub | ✅ Complete | Auto-resolves in `TravelExecutor` |
| Unit tests | ✅ Complete | `tests/sim/travel/TV2*.cs` (22 tests) |

---

## Architecture Decisions

### AD1: TravelExecutor as Stateless Service

**Decision**: `TravelExecutor` is a stateless service that takes `TravelPlan` and `CampaignState`, returns `TravelResult`.

**Rationale**:
- Follows existing patterns (`MissionInputBuilder`, `ContractGenerator`, `TravelPlanner`)
- Pure functions for testability
- State is held in `TravelState` (passed in/out), not in the executor

### AD2: Day-by-Day Execution with Encounter Rolls

**Decision**: Execute travel day-by-day, rolling for encounters each day (per TV0 design).

**Rationale**:
- Longer routes have more encounter opportunities
- Allows granular fuel consumption and time tracking
- Matches TV0 specification

### AD3: Encounter Stub for TV2

**Decision**: TV2 implements encounter triggering with a stub. Full encounter resolution waits for EN1.

**Stub behavior**:
- When encounter triggers, create `TravelContext`
- Log the encounter trigger
- Auto-resolve as "completed" (no actual encounter runs)
- Continue travel

**Rationale**:
- Allows TV2 to be fully testable without EN1
- Encounter integration is a separate concern
- Stub can be replaced when EN1 is ready

### AD4: Stranded State as Interrupt

**Decision**: Running out of fuel mid-travel interrupts travel at current system.

**Rationale**:
- Player is not game-over, just stuck
- Can still access station facilities if at a station
- Must acquire fuel before continuing (fuel runner service, distress signal)

---

## Implementation Steps

### Phase 1: Data Structures

#### Step 1.1: Create TravelState Class

**File**: `src/sim/travel/TravelState.cs`

```csharp
using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// In-progress travel state. Tracks position within a travel plan.
/// Serializable for save/load mid-travel.
/// </summary>
public class TravelState
{
    /// <summary>
    /// The travel plan being executed.
    /// </summary>
    public TravelPlan Plan { get; set; }
    
    /// <summary>
    /// Current segment index (0-based).
    /// </summary>
    public int CurrentSegmentIndex { get; set; } = 0;
    
    /// <summary>
    /// Current day within the current segment (0-based).
    /// </summary>
    public int CurrentDayInSegment { get; set; } = 0;
    
    /// <summary>
    /// Whether travel is paused for an encounter.
    /// </summary>
    public bool IsPausedForEncounter { get; set; } = false;
    
    /// <summary>
    /// Pending encounter ID if paused.
    /// </summary>
    public string PendingEncounterId { get; set; }
    
    /// <summary>
    /// Fuel consumed so far.
    /// </summary>
    public int FuelConsumed { get; set; } = 0;
    
    /// <summary>
    /// Days elapsed so far.
    /// </summary>
    public int DaysElapsed { get; set; } = 0;
    
    /// <summary>
    /// Encounters that occurred during travel.
    /// </summary>
    public List<TravelEncounterRecord> EncounterHistory { get; set; } = new();
    
    /// <summary>
    /// Current system ID (updated as we complete segments).
    /// </summary>
    public int CurrentSystemId { get; set; }
    
    /// <summary>
    /// Whether travel is complete.
    /// </summary>
    public bool IsComplete => CurrentSegmentIndex >= Plan?.Segments.Count;
    
    /// <summary>
    /// Get current segment (null if complete).
    /// </summary>
    public TravelSegment CurrentSegment => 
        IsComplete ? null : Plan?.Segments[CurrentSegmentIndex];
    
    /// <summary>
    /// Create initial state for a travel plan.
    /// </summary>
    public static TravelState Create(TravelPlan plan, int startSystemId)
    {
        return new TravelState
        {
            Plan = plan,
            CurrentSystemId = startSystemId,
            CurrentSegmentIndex = 0,
            CurrentDayInSegment = 0
        };
    }
}

/// <summary>
/// Record of an encounter that occurred during travel.
/// </summary>
public class TravelEncounterRecord
{
    public int SegmentIndex { get; set; }
    public int DayInSegment { get; set; }
    public int SystemId { get; set; }
    public string EncounterType { get; set; }
    public string EncounterId { get; set; }
    public string Outcome { get; set; }
}
```

**Acceptance Criteria**:
- [ ] `TravelState` tracks position within plan
- [ ] `IsComplete` correctly identifies finished travel
- [ ] `CurrentSegment` returns correct segment
- [ ] Factory method creates valid initial state

---

#### Step 1.2: Create TravelExecutionResult Class

**File**: `src/sim/travel/TravelResult.cs`

> **Note**: Named `TravelExecutionResult` to avoid conflict with existing `TravelResult` enum in `TravelSystem.cs` (legacy G1 code).

```csharp
using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// Result of travel execution (TV2).
/// Distinct from TravelResult enum in TravelSystem.cs (legacy G1 code).
/// </summary>
public class TravelExecutionResult
{
    /// <summary>
    /// Final status of the travel.
    /// </summary>
    public TravelResultStatus Status { get; set; }
    
    /// <summary>
    /// Final system ID (destination if complete, current if interrupted).
    /// </summary>
    public int FinalSystemId { get; set; }
    
    /// <summary>
    /// Total fuel consumed.
    /// </summary>
    public int FuelConsumed { get; set; }
    
    /// <summary>
    /// Total days elapsed.
    /// </summary>
    public int DaysElapsed { get; set; }
    
    /// <summary>
    /// Encounters that occurred.
    /// </summary>
    public List<TravelEncounterRecord> Encounters { get; set; } = new();
    
    /// <summary>
    /// Reason for interruption (if Status != Completed).
    /// </summary>
    public TravelInterruptReason InterruptReason { get; set; }
    
    /// <summary>
    /// Travel state for resumption (if paused for encounter).
    /// </summary>
    public TravelState PausedState { get; set; }
    
    /// <summary>
    /// Create a completed result.
    /// </summary>
    public static TravelResult Completed(int destinationId, int fuelConsumed, int daysElapsed, List<TravelEncounterRecord> encounters = null)
    {
        return new TravelResult
        {
            Status = TravelResultStatus.Completed,
            FinalSystemId = destinationId,
            FuelConsumed = fuelConsumed,
            DaysElapsed = daysElapsed,
            Encounters = encounters ?? new List<TravelEncounterRecord>()
        };
    }
    
    /// <summary>
    /// Create an interrupted result.
    /// </summary>
    public static TravelResult Interrupted(int currentSystemId, TravelInterruptReason reason, int fuelConsumed, int daysElapsed, TravelState state = null)
    {
        return new TravelResult
        {
            Status = TravelResultStatus.Interrupted,
            FinalSystemId = currentSystemId,
            InterruptReason = reason,
            FuelConsumed = fuelConsumed,
            DaysElapsed = daysElapsed,
            PausedState = state
        };
    }
    
    /// <summary>
    /// Create a paused result (for encounter).
    /// </summary>
    public static TravelResult Paused(TravelState state, int fuelConsumed, int daysElapsed)
    {
        return new TravelResult
        {
            Status = TravelResultStatus.PausedForEncounter,
            FinalSystemId = state.CurrentSystemId,
            FuelConsumed = fuelConsumed,
            DaysElapsed = daysElapsed,
            PausedState = state,
            Encounters = new List<TravelEncounterRecord>(state.EncounterHistory)
        };
    }
}

/// <summary>
/// Status of travel execution.
/// </summary>
public enum TravelResultStatus
{
    /// <summary>
    /// Arrived at destination.
    /// </summary>
    Completed,
    
    /// <summary>
    /// Stopped mid-travel (out of fuel, player abort, etc.).
    /// </summary>
    Interrupted,
    
    /// <summary>
    /// Paused for encounter resolution.
    /// </summary>
    PausedForEncounter,
    
    /// <summary>
    /// Cancelled before starting.
    /// </summary>
    Cancelled
}

/// <summary>
/// Reason for travel interruption.
/// </summary>
public enum TravelInterruptReason
{
    None,
    InsufficientFuel,
    PlayerAbort,
    EncounterDefeat,
    EncounterCapture,
    RouteBlocked
}
```

**Acceptance Criteria**:
- [ ] `TravelResult` captures all travel outcomes
- [ ] Factory methods create correct result types
- [ ] `PausedState` enables resume capability

---

#### Step 1.3: Create TravelContext Class

**File**: `src/sim/travel/TravelContext.cs`

```csharp
using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// Context passed to Encounter/Generation when travel triggers an encounter.
/// Contains all information needed to select and parameterize an encounter.
/// </summary>
public class TravelContext
{
    // === Location ===
    
    /// <summary>
    /// Current system ID where encounter occurs.
    /// </summary>
    public int CurrentSystemId { get; set; }
    
    /// <summary>
    /// Destination system ID of the travel.
    /// </summary>
    public int DestinationSystemId { get; set; }
    
    /// <summary>
    /// Current route being traveled.
    /// </summary>
    public Route CurrentRoute { get; set; }
    
    // === System Info ===
    
    /// <summary>
    /// Current system reference.
    /// </summary>
    public StarSystem CurrentSystem { get; set; }
    
    /// <summary>
    /// Tags on the current system.
    /// </summary>
    public HashSet<string> SystemTags { get; set; } = new();
    
    /// <summary>
    /// Metrics for the current system.
    /// </summary>
    public SystemMetrics SystemMetrics { get; set; }
    
    // === Route Info ===
    
    /// <summary>
    /// Tags on the current route.
    /// </summary>
    public HashSet<string> RouteTags { get; set; } = new();
    
    /// <summary>
    /// Hazard level of the current route.
    /// </summary>
    public int RouteHazard { get; set; }
    
    // === Suggested Encounter ===
    
    /// <summary>
    /// Suggested encounter type based on route/system context.
    /// Values: "pirate", "patrol", "trader", "smuggler", "anomaly", "distress", "random"
    /// </summary>
    public string SuggestedEncounterType { get; set; }
    
    // === Player State Summary ===
    
    /// <summary>
    /// Total value of cargo being carried.
    /// </summary>
    public int CargoValue { get; set; }
    
    /// <summary>
    /// Whether player has illegal cargo.
    /// </summary>
    public bool HasIllegalCargo { get; set; }
    
    /// <summary>
    /// Number of crew members.
    /// </summary>
    public int CrewCount { get; set; }
    
    /// <summary>
    /// Aggregated traits from crew (for skill checks).
    /// </summary>
    public List<string> CrewTraits { get; set; } = new();
    
    // === Faction Context ===
    
    /// <summary>
    /// Faction that owns the current system.
    /// </summary>
    public string SystemOwnerFactionId { get; set; }
    
    /// <summary>
    /// Player's reputation with the owning faction.
    /// </summary>
    public int PlayerRepWithOwner { get; set; }
    
    /// <summary>
    /// Create context from travel state and campaign.
    /// </summary>
    public static TravelContext Create(TravelState state, CampaignState campaign)
    {
        var segment = state.CurrentSegment;
        var world = campaign.World;
        var system = world?.GetSystem(state.CurrentSystemId);
        var route = segment?.Route;
        
        var context = new TravelContext
        {
            CurrentSystemId = state.CurrentSystemId,
            DestinationSystemId = state.Plan?.DestinationSystemId ?? 0,
            CurrentRoute = route,
            CurrentSystem = system,
            SystemTags = system?.Tags != null ? new HashSet<string>(system.Tags) : new HashSet<string>(),
            SystemMetrics = system?.Metrics,
            RouteTags = route?.Tags != null ? new HashSet<string>(route.Tags) : new HashSet<string>(),
            RouteHazard = route?.HazardLevel ?? 0,
            SuggestedEncounterType = segment?.SuggestedEncounterType ?? "random",
            CargoValue = campaign.Inventory?.GetTotalValue() ?? 0,
            HasIllegalCargo = false, // TODO: Check for illegal items when implemented
            CrewCount = campaign.GetAliveCrew()?.Count ?? 0,
            SystemOwnerFactionId = system?.OwningFactionId,
            PlayerRepWithOwner = campaign.GetFactionRep(system?.OwningFactionId ?? "")
        };
        
        // Aggregate crew traits
        foreach (var crew in campaign.GetAliveCrew() ?? new List<CrewMember>())
        {
            foreach (var trait in crew.Traits ?? new List<string>())
            {
                if (!context.CrewTraits.Contains(trait))
                    context.CrewTraits.Add(trait);
            }
        }
        
        return context;
    }
}
```

**Acceptance Criteria**:
- [ ] `TravelContext` captures all relevant data for encounter selection
- [ ] Factory method populates from `TravelState` and `CampaignState`
- [ ] Context is self-contained (no references to mutable state)

---

### Phase 2: Travel Events

#### Step 2.1: Add Travel Events

**File**: `src/sim/Events.cs` (add to existing file)

```csharp
// === Travel Events ===

/// <summary>
/// Travel has started.
/// </summary>
public record TravelStartedEvent(
    int FromSystemId,
    int ToSystemId,
    int EstimatedDays,
    int EstimatedFuel
);

/// <summary>
/// A travel segment has started.
/// </summary>
public record TravelSegmentStartedEvent(
    int FromSystemId,
    int ToSystemId,
    int SegmentIndex,
    int SegmentDays
);

/// <summary>
/// A travel segment has completed.
/// </summary>
public record TravelSegmentCompletedEvent(
    int FromSystemId,
    int ToSystemId,
    int FuelConsumed,
    int DaysElapsed
);

/// <summary>
/// An encounter has been triggered during travel.
/// </summary>
public record TravelEncounterTriggeredEvent(
    int SystemId,
    string EncounterType,
    string EncounterId
);

/// <summary>
/// An encounter during travel has been resolved.
/// </summary>
public record TravelEncounterResolvedEvent(
    string EncounterId,
    string Outcome
);

/// <summary>
/// Travel has completed successfully.
/// </summary>
public record TravelCompletedEvent(
    int DestinationSystemId,
    int TotalDays,
    int TotalFuel
);

/// <summary>
/// Travel has been interrupted.
/// </summary>
public record TravelInterruptedEvent(
    int CurrentSystemId,
    string Reason
);

/// <summary>
/// Player position has changed.
/// </summary>
public record PlayerMovedEvent(
    int FromSystemId,
    int ToSystemId,
    string SystemName
);
```

**Acceptance Criteria**:
- [ ] All travel lifecycle events defined
- [ ] Events are records (immutable)
- [ ] Events contain sufficient data for UI/logging

---

### Phase 3: TravelExecutor Implementation

#### Step 3.1: Create TravelExecutor Class

**File**: `src/sim/travel/TravelExecutor.cs`

```csharp
using System;

namespace FringeTactics;

/// <summary>
/// Executes travel plans, consuming resources and triggering encounters.
/// Stateless service - all state is passed in/out via TravelState.
/// </summary>
public class TravelExecutor
{
    private readonly RngService rng;
    
    public TravelExecutor(RngService rng)
    {
        this.rng = rng ?? throw new ArgumentNullException(nameof(rng));
    }
    
    /// <summary>
    /// Execute a travel plan from start to finish (or until interrupted).
    /// </summary>
    public TravelResult Execute(TravelPlan plan, CampaignState campaign)
    {
        if (plan == null || !plan.IsValid)
        {
            return TravelResult.Interrupted(
                campaign.CurrentNodeId, 
                TravelInterruptReason.RouteBlocked, 
                0, 0);
        }
        
        // Check fuel before starting
        if (campaign.Fuel < plan.TotalFuelCost)
        {
            SimLog.Log($"[Travel] Cannot start: insufficient fuel ({campaign.Fuel}/{plan.TotalFuelCost})");
            return TravelResult.Interrupted(
                campaign.CurrentNodeId,
                TravelInterruptReason.InsufficientFuel,
                0, 0);
        }
        
        // Create initial state
        var state = TravelState.Create(plan, campaign.CurrentNodeId);
        
        // Emit start event
        campaign.EventBus?.Publish(new TravelStartedEvent(
            plan.OriginSystemId,
            plan.DestinationSystemId,
            plan.TotalTimeDays,
            plan.TotalFuelCost
        ));
        
        SimLog.Log($"[Travel] Starting journey: {campaign.World?.GetSystem(plan.OriginSystemId)?.Name} → {campaign.World?.GetSystem(plan.DestinationSystemId)?.Name}");
        SimLog.Log($"[Travel] Estimated: {plan.TotalTimeDays} days, {plan.TotalFuelCost} fuel, {plan.Segments.Count} segment(s)");
        
        // Execute travel
        return ExecuteFromState(state, campaign);
    }
    
    /// <summary>
    /// Resume travel from a paused state (after encounter resolution).
    /// </summary>
    public TravelResult Resume(TravelState state, CampaignState campaign, string encounterOutcome = "completed")
    {
        if (state == null || state.Plan == null)
        {
            return TravelResult.Interrupted(
                campaign.CurrentNodeId,
                TravelInterruptReason.RouteBlocked,
                0, 0);
        }
        
        // Record encounter outcome
        if (state.IsPausedForEncounter && !string.IsNullOrEmpty(state.PendingEncounterId))
        {
            // Update the last encounter record with outcome
            if (state.EncounterHistory.Count > 0)
            {
                state.EncounterHistory[^1].Outcome = encounterOutcome;
            }
            
            campaign.EventBus?.Publish(new TravelEncounterResolvedEvent(
                state.PendingEncounterId,
                encounterOutcome
            ));
            
            // Check if encounter result should abort travel
            if (encounterOutcome == "defeat" || encounterOutcome == "captured")
            {
                var reason = encounterOutcome == "captured" 
                    ? TravelInterruptReason.EncounterCapture 
                    : TravelInterruptReason.EncounterDefeat;
                    
                return TravelResult.Interrupted(
                    state.CurrentSystemId,
                    reason,
                    state.FuelConsumed,
                    state.DaysElapsed,
                    state);
            }
        }
        
        // Clear pause state
        state.IsPausedForEncounter = false;
        state.PendingEncounterId = null;
        
        // Continue execution
        return ExecuteFromState(state, campaign);
    }
    
    /// <summary>
    /// Core execution loop.
    /// </summary>
    private TravelResult ExecuteFromState(TravelState state, CampaignState campaign)
    {
        while (!state.IsComplete)
        {
            var segment = state.CurrentSegment;
            
            // Emit segment start event (only on first day of segment)
            if (state.CurrentDayInSegment == 0)
            {
                campaign.EventBus?.Publish(new TravelSegmentStartedEvent(
                    segment.FromSystemId,
                    segment.ToSystemId,
                    state.CurrentSegmentIndex,
                    segment.TimeDays
                ));
                
                SimLog.Log($"[Travel] Segment {state.CurrentSegmentIndex + 1}/{state.Plan.Segments.Count}: {campaign.World?.GetSystem(segment.FromSystemId)?.Name} → {campaign.World?.GetSystem(segment.ToSystemId)?.Name}");
            }
            
            // Process remaining days in current segment
            while (state.CurrentDayInSegment < segment.TimeDays)
            {
                // Calculate fuel for this day (proportional)
                int dailyFuel = CalculateDailyFuel(segment, state.CurrentDayInSegment);
                
                // Check fuel
                if (campaign.Fuel < dailyFuel)
                {
                    SimLog.Log($"[Travel] Out of fuel at day {state.DaysElapsed + 1}!");
                    campaign.EventBus?.Publish(new TravelInterruptedEvent(
                        state.CurrentSystemId,
                        "out_of_fuel"
                    ));
                    
                    return TravelResult.Interrupted(
                        state.CurrentSystemId,
                        TravelInterruptReason.InsufficientFuel,
                        state.FuelConsumed,
                        state.DaysElapsed,
                        state);
                }
                
                // Consume fuel
                campaign.SpendFuel(dailyFuel, "travel");
                state.FuelConsumed += dailyFuel;
                
                // Advance time
                campaign.Time.AdvanceDays(1);
                state.DaysElapsed++;
                state.CurrentDayInSegment++;
                
                // Roll for encounter
                var encounterResult = TryTriggerEncounter(state, campaign, segment);
                if (encounterResult != null)
                {
                    return encounterResult;
                }
            }
            
            // Segment complete - move to destination
            state.CurrentSystemId = segment.ToSystemId;
            campaign.CurrentNodeId = segment.ToSystemId;
            
            campaign.EventBus?.Publish(new TravelSegmentCompletedEvent(
                segment.FromSystemId,
                segment.ToSystemId,
                segment.FuelCost,
                segment.TimeDays
            ));
            
            campaign.EventBus?.Publish(new PlayerMovedEvent(
                segment.FromSystemId,
                segment.ToSystemId,
                campaign.World?.GetSystem(segment.ToSystemId)?.Name ?? "Unknown"
            ));
            
            SimLog.Log($"[Travel] Arrived at {campaign.World?.GetSystem(segment.ToSystemId)?.Name}");
            
            // Move to next segment
            state.CurrentSegmentIndex++;
            state.CurrentDayInSegment = 0;
        }
        
        // Travel complete
        campaign.EventBus?.Publish(new TravelCompletedEvent(
            state.Plan.DestinationSystemId,
            state.DaysElapsed,
            state.FuelConsumed
        ));
        
        SimLog.Log($"[Travel] Journey complete! {state.DaysElapsed} days, {state.FuelConsumed} fuel, {state.EncounterHistory.Count} encounter(s)");
        
        return TravelResult.Completed(
            state.Plan.DestinationSystemId,
            state.FuelConsumed,
            state.DaysElapsed,
            state.EncounterHistory);
    }
    
    /// <summary>
    /// Calculate fuel consumption for a specific day of a segment.
    /// Distributes fuel evenly across days, with remainder on last day.
    /// </summary>
    private int CalculateDailyFuel(TravelSegment segment, int dayIndex)
    {
        if (segment.TimeDays <= 0) return segment.FuelCost;
        
        int baseDailyFuel = segment.FuelCost / segment.TimeDays;
        int remainder = segment.FuelCost % segment.TimeDays;
        
        // Add remainder to last day
        if (dayIndex == segment.TimeDays - 1)
            return baseDailyFuel + remainder;
        
        return baseDailyFuel;
    }
    
    /// <summary>
    /// Roll for encounter and handle if triggered.
    /// Returns TravelResult if travel should pause/stop, null to continue.
    /// </summary>
    private TravelResult TryTriggerEncounter(TravelState state, CampaignState campaign, TravelSegment segment)
    {
        float encounterChance = segment.EncounterChance;
        float roll = rng.Campaign.NextFloat();
        
        if (roll >= encounterChance)
        {
            return null; // No encounter
        }
        
        // Encounter triggered!
        string encounterId = $"enc_{state.CurrentSystemId}_{state.DaysElapsed}_{rng.Campaign.NextInt(10000)}";
        string encounterType = segment.SuggestedEncounterType ?? "random";
        
        SimLog.Log($"[Travel] Encounter triggered! Type: {encounterType}, Roll: {roll:F2} < {encounterChance:F2}");
        
        // Record encounter
        var record = new TravelEncounterRecord
        {
            SegmentIndex = state.CurrentSegmentIndex,
            DayInSegment = state.CurrentDayInSegment,
            SystemId = state.CurrentSystemId,
            EncounterType = encounterType,
            EncounterId = encounterId,
            Outcome = "pending"
        };
        state.EncounterHistory.Add(record);
        
        // Create context for encounter generation
        var context = TravelContext.Create(state, campaign);
        
        campaign.EventBus?.Publish(new TravelEncounterTriggeredEvent(
            state.CurrentSystemId,
            encounterType,
            encounterId
        ));
        
        // === STUB: Auto-resolve encounter ===
        // When EN1 is implemented, this will pause and return control to caller.
        // For now, auto-resolve as "completed" and continue.
        
        record.Outcome = "completed";
        campaign.EventBus?.Publish(new TravelEncounterResolvedEvent(encounterId, "completed"));
        SimLog.Log($"[Travel] Encounter {encounterId} auto-resolved (stub)");
        
        // Continue travel (no pause)
        return null;
        
        // === FUTURE: Real encounter handling ===
        // Uncomment when EN1 is ready:
        /*
        state.IsPausedForEncounter = true;
        state.PendingEncounterId = encounterId;
        
        return TravelResult.Paused(state, state.FuelConsumed, state.DaysElapsed);
        */
    }
}
```

**Acceptance Criteria**:
- [ ] `Execute()` runs travel from start to finish
- [ ] `Resume()` continues from paused state
- [ ] Fuel consumed proportionally per day
- [ ] Time advances correctly
- [ ] Encounters trigger based on chance
- [ ] Position updates on segment completion
- [ ] Events emitted at all lifecycle points

---

### Phase 4: Integration

#### Step 4.1: Update Travel agents.md

**File**: `src/sim/travel/agents.md`

```markdown
# Travel Domain (`src/sim/travel/`)

This directory contains the Travel domain simulation layer.

## Purpose

Handle route planning and travel execution across the sector map.

## Files

| File | Purpose |
|------|---------|
| `TravelCosts.cs` | Cost calculation utilities (fuel, time, encounter chance) |
| `TravelSegment.cs` | Single route step with costs |
| `TravelPlan.cs` | Complete route with aggregates |
| `TravelPlanner.cs` | A* pathfinding and plan creation |
| `TravelState.cs` | In-progress travel state (TV2) |
| `TravelResult.cs` | Completed travel outcome (TV2) |
| `TravelContext.cs` | Context for encounter generation (TV2) |
| `TravelExecutor.cs` | Main execution logic (TV2) |

## Dependencies

- **Imports from**: `src/sim/world/` (WorldState, Route, SystemMetrics), `src/sim/campaign/` (CampaignState)
- **Imported by**: `src/core/` (GameState)

## Key Patterns

- Stateless services with explicit parameters
- Pure functions for cost calculations
- No Godot Node dependencies
- Day-by-day execution with encounter rolls
```

---

## Phase 5: Unit Tests

### Test File: `tests/sim/travel/TV2TravelStateTests.cs`

```csharp
using GdUnit4;
using static GdUnit4.Assertions;

namespace FringeTactics.Tests;

[TestSuite]
public class TV2TravelStateTests
{
    [TestCase]
    public void Create_InitializesCorrectly()
    {
        var world = WorldState.CreateTestSector();
        var planner = new TravelPlanner(world);
        var plan = planner.PlanRoute(0, 2); // Haven → Rockfall
        
        var state = TravelState.Create(plan, 0);
        
        AssertInt(state.CurrentSegmentIndex).IsEqual(0);
        AssertInt(state.CurrentDayInSegment).IsEqual(0);
        AssertInt(state.CurrentSystemId).IsEqual(0);
        AssertInt(state.FuelConsumed).IsEqual(0);
        AssertInt(state.DaysElapsed).IsEqual(0);
        AssertBool(state.IsComplete).IsFalse();
    }
    
    [TestCase]
    public void IsComplete_FalseWhenSegmentsRemain()
    {
        var world = WorldState.CreateTestSector();
        var planner = new TravelPlanner(world);
        var plan = planner.PlanRoute(0, 2);
        
        var state = TravelState.Create(plan, 0);
        
        AssertBool(state.IsComplete).IsFalse();
    }
    
    [TestCase]
    public void IsComplete_TrueWhenAllSegmentsProcessed()
    {
        var world = WorldState.CreateTestSector();
        var planner = new TravelPlanner(world);
        var plan = planner.PlanRoute(0, 2);
        
        var state = TravelState.Create(plan, 0);
        state.CurrentSegmentIndex = plan.Segments.Count;
        
        AssertBool(state.IsComplete).IsTrue();
    }
    
    [TestCase]
    public void CurrentSegment_ReturnsCorrectSegment()
    {
        var world = WorldState.CreateTestSector();
        var planner = new TravelPlanner(world);
        var plan = planner.PlanRoute(0, 2);
        
        var state = TravelState.Create(plan, 0);
        
        AssertObject(state.CurrentSegment).IsNotNull();
        AssertInt(state.CurrentSegment.FromSystemId).IsEqual(0);
    }
    
    [TestCase]
    public void CurrentSegment_NullWhenComplete()
    {
        var world = WorldState.CreateTestSector();
        var planner = new TravelPlanner(world);
        var plan = planner.PlanRoute(0, 2);
        
        var state = TravelState.Create(plan, 0);
        state.CurrentSegmentIndex = plan.Segments.Count;
        
        AssertObject(state.CurrentSegment).IsNull();
    }
}
```

### Test File: `tests/sim/travel/TV2TravelExecutorTests.cs`

```csharp
using GdUnit4;
using static GdUnit4.Assertions;

namespace FringeTactics.Tests;

[TestSuite]
public class TV2TravelExecutorTests
{
    private WorldState CreateTestWorld()
    {
        return WorldState.CreateTestSector();
    }
    
    private CampaignState CreateTestCampaign(WorldState world, int fuel = 100)
    {
        var campaign = new CampaignState
        {
            World = world,
            CurrentNodeId = 0,
            Fuel = fuel,
            Time = new CampaignTime(),
            Rng = new RngService(12345)
        };
        return campaign;
    }
    
    [TestCase]
    public void Execute_CompletesSuccessfully()
    {
        var world = CreateTestWorld();
        var campaign = CreateTestCampaign(world, 100);
        var planner = new TravelPlanner(world);
        var plan = planner.PlanRoute(0, 1); // Haven → Waypoint (single segment)
        var executor = new TravelExecutor(campaign.Rng);
        
        var result = executor.Execute(plan, campaign);
        
        AssertThat(result.Status).IsEqual(TravelResultStatus.Completed);
        AssertInt(result.FinalSystemId).IsEqual(1);
        AssertInt(result.FuelConsumed).IsGreater(0);
        AssertInt(result.DaysElapsed).IsGreater(0);
    }
    
    [TestCase]
    public void Execute_ConsumesFuel()
    {
        var world = CreateTestWorld();
        var campaign = CreateTestCampaign(world, 100);
        int startFuel = campaign.Fuel;
        var planner = new TravelPlanner(world);
        var plan = planner.PlanRoute(0, 1);
        var executor = new TravelExecutor(campaign.Rng);
        
        var result = executor.Execute(plan, campaign);
        
        AssertInt(campaign.Fuel).IsLess(startFuel);
        AssertInt(result.FuelConsumed).IsEqual(startFuel - campaign.Fuel);
    }
    
    [TestCase]
    public void Execute_AdvancesTime()
    {
        var world = CreateTestWorld();
        var campaign = CreateTestCampaign(world, 100);
        int startDay = campaign.Time.CurrentDay;
        var planner = new TravelPlanner(world);
        var plan = planner.PlanRoute(0, 1);
        var executor = new TravelExecutor(campaign.Rng);
        
        var result = executor.Execute(plan, campaign);
        
        AssertInt(campaign.Time.CurrentDay).IsGreater(startDay);
        AssertInt(result.DaysElapsed).IsEqual(campaign.Time.CurrentDay - startDay);
    }
    
    [TestCase]
    public void Execute_UpdatesPosition()
    {
        var world = CreateTestWorld();
        var campaign = CreateTestCampaign(world, 100);
        var planner = new TravelPlanner(world);
        var plan = planner.PlanRoute(0, 1);
        var executor = new TravelExecutor(campaign.Rng);
        
        executor.Execute(plan, campaign);
        
        AssertInt(campaign.CurrentNodeId).IsEqual(1);
    }
    
    [TestCase]
    public void Execute_InsufficientFuel_InterruptsBeforeStart()
    {
        var world = CreateTestWorld();
        var campaign = CreateTestCampaign(world, 5); // Very low fuel
        var planner = new TravelPlanner(world);
        var plan = planner.PlanRoute(0, 1);
        var executor = new TravelExecutor(campaign.Rng);
        
        var result = executor.Execute(plan, campaign);
        
        AssertThat(result.Status).IsEqual(TravelResultStatus.Interrupted);
        AssertThat(result.InterruptReason).IsEqual(TravelInterruptReason.InsufficientFuel);
        AssertInt(campaign.CurrentNodeId).IsEqual(0); // Didn't move
    }
    
    [TestCase]
    public void Execute_MultiSegment_CompletesAllSegments()
    {
        var world = CreateTestWorld();
        var campaign = CreateTestCampaign(world, 100);
        var planner = new TravelPlanner(world);
        var plan = planner.PlanRoute(0, 2); // Haven → Waypoint → Rockfall
        var executor = new TravelExecutor(campaign.Rng);
        
        AssertInt(plan.Segments.Count).IsGreater(1);
        
        var result = executor.Execute(plan, campaign);
        
        AssertThat(result.Status).IsEqual(TravelResultStatus.Completed);
        AssertInt(result.FinalSystemId).IsEqual(2);
        AssertInt(campaign.CurrentNodeId).IsEqual(2);
    }
    
    [TestCase]
    public void Execute_InvalidPlan_ReturnsInterrupted()
    {
        var world = CreateTestWorld();
        var campaign = CreateTestCampaign(world, 100);
        var executor = new TravelExecutor(campaign.Rng);
        
        var result = executor.Execute(null, campaign);
        
        AssertThat(result.Status).IsEqual(TravelResultStatus.Interrupted);
    }
    
    [TestCase]
    public void Execute_SameSystem_ReturnsInterrupted()
    {
        var world = CreateTestWorld();
        var campaign = CreateTestCampaign(world, 100);
        var planner = new TravelPlanner(world);
        var plan = planner.PlanRoute(0, 0); // Same system
        var executor = new TravelExecutor(campaign.Rng);
        
        var result = executor.Execute(plan, campaign);
        
        AssertThat(result.Status).IsEqual(TravelResultStatus.Interrupted);
    }
}
```

### Test File: `tests/sim/travel/TV2TravelResultTests.cs`

```csharp
using GdUnit4;
using static GdUnit4.Assertions;

namespace FringeTactics.Tests;

[TestSuite]
public class TV2TravelResultTests
{
    [TestCase]
    public void Completed_CreatesCorrectResult()
    {
        var result = TravelResult.Completed(5, 30, 4);
        
        AssertThat(result.Status).IsEqual(TravelResultStatus.Completed);
        AssertInt(result.FinalSystemId).IsEqual(5);
        AssertInt(result.FuelConsumed).IsEqual(30);
        AssertInt(result.DaysElapsed).IsEqual(4);
    }
    
    [TestCase]
    public void Interrupted_CreatesCorrectResult()
    {
        var result = TravelResult.Interrupted(3, TravelInterruptReason.InsufficientFuel, 15, 2);
        
        AssertThat(result.Status).IsEqual(TravelResultStatus.Interrupted);
        AssertInt(result.FinalSystemId).IsEqual(3);
        AssertThat(result.InterruptReason).IsEqual(TravelInterruptReason.InsufficientFuel);
        AssertInt(result.FuelConsumed).IsEqual(15);
        AssertInt(result.DaysElapsed).IsEqual(2);
    }
    
    [TestCase]
    public void Paused_IncludesState()
    {
        var state = new TravelState { CurrentSystemId = 2, FuelConsumed = 10, DaysElapsed = 1 };
        
        var result = TravelResult.Paused(state, 10, 1);
        
        AssertThat(result.Status).IsEqual(TravelResultStatus.PausedForEncounter);
        AssertObject(result.PausedState).IsNotNull();
        AssertInt(result.PausedState.CurrentSystemId).IsEqual(2);
    }
}
```

---

## Manual Test Setup

### Test Scenario 1: Basic Travel

**Setup**:
1. Create campaign with test sector (`WorldState.CreateTestSector()`)
2. Start at Haven Station (system 0)
3. Set fuel to 100

**Steps**:
1. Plan route: Haven (0) → Waypoint (1)
2. Execute travel
3. Verify arrival

**Expected Results**:

| Check | Expected |
|-------|----------|
| `result.Status` | `Completed` |
| `result.FinalSystemId` | 1 |
| `campaign.CurrentNodeId` | 1 |
| `campaign.Fuel` | < 100 (fuel consumed) |
| `campaign.Time.CurrentDay` | > 1 (time advanced) |

### Test Scenario 2: Multi-Hop Travel

**Setup**:
1. Create campaign with test sector
2. Start at Haven Station (system 0)
3. Set fuel to 100

**Steps**:
1. Plan route: Haven (0) → Rockfall (2) (goes via Waypoint)
2. Execute travel
3. Verify multi-segment completion

**Expected Results**:

| Check | Expected |
|-------|----------|
| `result.Status` | `Completed` |
| `result.FinalSystemId` | 2 |
| `plan.Segments.Count` | 2 |
| Position updates | 0 → 1 → 2 |

### Test Scenario 3: Insufficient Fuel

**Setup**:
1. Create campaign with test sector
2. Start at Haven Station (system 0)
3. Set fuel to 5 (very low)

**Steps**:
1. Plan route: Haven (0) → Rockfall (2)
2. Execute travel
3. Verify interruption

**Expected Results**:

| Check | Expected |
|-------|----------|
| `result.Status` | `Interrupted` |
| `result.InterruptReason` | `InsufficientFuel` |
| `campaign.CurrentNodeId` | 0 (didn't move) |

### Test Scenario 4: Encounter Trigger (with seeded RNG)

**Setup**:
1. Create campaign with test sector
2. Start at Waypoint (system 1)
3. Set fuel to 100
4. Use RNG seed that guarantees encounter

**Steps**:
1. Plan route through high-hazard area
2. Execute travel
3. Check encounter history

**Expected Results**:

| Check | Expected |
|-------|----------|
| `result.Encounters.Count` | ≥ 1 |
| Encounter logged | Yes |
| Travel continues | Yes (stub auto-resolves) |

---

## Files Summary

### Files to Create

| File | Purpose |
|------|---------|
| `src/sim/travel/TravelState.cs` | In-progress travel state |
| `src/sim/travel/TravelResult.cs` | Completed travel outcome |
| `src/sim/travel/TravelContext.cs` | Context for encounter generation |
| `src/sim/travel/TravelExecutor.cs` | Main execution logic |
| `tests/sim/travel/TV2TravelStateTests.cs` | State tests |
| `tests/sim/travel/TV2TravelExecutorTests.cs` | Executor tests |
| `tests/sim/travel/TV2TravelResultTests.cs` | Result tests |

### Files to Modify

| File | Changes |
|------|---------|
| `src/sim/Events.cs` | Add travel events |
| `src/sim/travel/agents.md` | Document new files |
| `tests/sim/travel/agents.md` | Document new test files |

---

## Success Criteria

When TV2 is complete:

- [ ] Travel consumes fuel proportionally per day
- [ ] Travel advances campaign time
- [ ] Player position updates on segment completion
- [ ] Encounters trigger based on calculated chance
- [ ] Insufficient fuel interrupts travel gracefully
- [ ] Travel events emitted for UI/logging
- [ ] Resume capability works after encounter (stub)
- [ ] All unit tests pass
- [ ] Manual test scenarios verified

---

## Future Integration Points

### EN1 Integration (when ready)

Replace the encounter stub in `TryTriggerEncounter()`:

```csharp
// Current stub:
record.Outcome = "completed";
return null;

// Replace with:
state.IsPausedForEncounter = true;
state.PendingEncounterId = encounterId;
return TravelResult.Paused(state, state.FuelConsumed, state.DaysElapsed);
```

The caller (GameState/UI) will then:
1. Receive `PausedForEncounter` result
2. Run encounter via EN1
3. Call `executor.Resume(state, campaign, encounterOutcome)`

### MG4 Integration (when ready)

Add encounter outcome effects in `Resume()`:
- Resource changes from encounter
- Crew injuries
- Reputation changes
- Time delays

---

## Appendix: Execution Flow Diagram

```
Execute(plan, campaign)
    │
    ├─► Check fuel sufficient
    │   └─► If not: return Interrupted(InsufficientFuel)
    │
    ├─► Create TravelState
    │
    └─► ExecuteFromState(state, campaign)
            │
            ├─► For each segment:
            │   │
            │   ├─► For each day in segment:
            │   │   │
            │   │   ├─► Check fuel for day
            │   │   │   └─► If not: return Interrupted(InsufficientFuel)
            │   │   │
            │   │   ├─► Consume fuel
            │   │   ├─► Advance time
            │   │   │
            │   │   └─► Roll for encounter
            │   │       └─► If triggered: [STUB: auto-resolve]
            │   │           └─► [FUTURE: return Paused]
            │   │
            │   └─► Update position to segment destination
            │
            └─► return Completed
```
