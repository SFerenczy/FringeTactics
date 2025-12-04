# SF2 – Event Bus (Minimal): Implementation Plan

**Status**: ✅ **COMPLETE**

This document breaks down **Milestone SF2** from `ROADMAP.md` into concrete implementation steps.

**Goal**: Decouple domains via a simple typed event system, enabling domain-to-domain communication without direct dependencies.

## Implementation Summary

| Phase | Status | Tests |
|-------|--------|-------|
| Phase 1: Core Event Bus | ✅ Complete | 30 unit tests |
| Phase 2: Integration Points | ✅ Complete | 9 integration tests |
| Phase 3: TravelSystem Integration | ✅ Complete | 3 integration tests |
| Phase 4: Testing | ✅ Complete | 42 total SF2 tests |

**Total Tests**: 211 (all passing)

---

## Current State Assessment

### What We Have (Existing Patterns)

| Component | Status | Notes |
|-----------|--------|-------|
| C# events on sim classes | ✅ Scattered | `CombatState.ActorDied`, `TimeSystem.TickAdvanced`, etc. |
| `CampaignTime.DayAdvanced` | ✅ Exists | C# event `Action<int, int>` |
| `SimLog.OnLog` | ✅ Exists | Static event for log messages |
| Direct method calls | ✅ Common | Domains call each other directly |

### Existing Event Patterns in Codebase

**Combat Layer** (tactical):
- `CombatState`: `ActorAdded`, `ActorRemoved`, `AttackResolved`, `ActorDied`, `MissionEnded`, `PhaseChanged`, `RetreatInitiated`, `RetreatCancelled`, `MissionCompleted`
- `TimeSystem`: `TickAdvanced`, `PauseChanged`, `TimeScaleChanged`
- `AttackSystem`: `AttackResolved`, `ActorDied`
- `AbilitySystem`: `AbilityCast`, `AbilityDetonated`, `StatusEffectApplied`
- `PerceptionSystem`: `AlarmStateChanged`, `EnemyDetectedCrew`, `EnemyBecameAlerted`
- `InteractionSystem`: `InteractableAdded`, `InteractableRemoved`, `InteractableStateChanged`, `InteractionStarted`, `InteractionCompleted`, `HazardTriggered`
- `VisibilitySystem`: `VisibilityChanged`
- `Actor`: `ModifiersChanged`, `PositionChanged`, `ArrivedAtTarget`, `DamageTaken`, `Died`, `ReloadCompleted`, `ChannelStarted`, `ChannelCompleted`, `ChannelInterrupted`

**Campaign Layer** (strategic):
- `CampaignTime`: `DayAdvanced`

### SF2 Requirements vs What We Have

| SF2 Requirement | Current Status | Gap |
|-----------------|----------------|-----|
| Typed event registration | ❌ Missing | No central bus, events are scattered |
| `Subscribe<TEvent>` / `Unsubscribe<TEvent>` | ❌ Missing | Each class manages its own subscribers |
| `Publish<TEvent>` | ❌ Missing | Events are invoked directly on source |
| Domain isolation | ⚠️ Partial | Some coupling exists (e.g., CampaignState directly calls systems) |
| `MissionCompletedEvent` | ⚠️ Partial | `CombatState.MissionCompleted` exists but not as bus event |
| `ActorDiedEvent` | ⚠️ Partial | `CombatState.ActorDied` exists but not as bus event |
| `ResourceChangedEvent` | ❌ Missing | No event when campaign resources change |

---

## Architecture Decisions

### Event Bus Design Philosophy

**Decision**: Create a **minimal, typed event bus** that complements (not replaces) existing C# events.

**Rationale**:
- Existing C# events work well for intra-system communication (e.g., `Actor.Died` → `AttackSystem`)
- Event bus is for **cross-domain** communication (e.g., Tactical → Campaign, Campaign → Simulation)
- Keep it simple: no async, no priority, no cancellation for SF2
- Can evolve to more sophisticated patterns if needed (SF2 is "minimal")

### When to Use Event Bus vs C# Events

| Use Case | Mechanism | Example |
|----------|-----------|---------|
| Same-system notification | C# event | `Actor.Died` → `AttackSystem` updates stats |
| Cross-domain notification | Event Bus | `MissionCompletedEvent` → Campaign updates state |
| UI notification | Event Bus | `ResourceChangedEvent` → UI updates display |
| High-frequency updates | C# event | `TickAdvanced` (20/sec) |
| Low-frequency, significant | Event Bus | `DayAdvancedEvent`, `MissionCompletedEvent` |

### Event Bus Location

**Decision**: `EventBus` lives in `src/sim/` as a core infrastructure class.

**Rationale**:
- Per architecture: sim layer is self-contained
- Event bus is infrastructure, not game logic
- Domains in sim can publish events
- Adapters (scenes) can subscribe to events

### Event Type Design

**Decision**: Events are **small, immutable record structs** with all relevant data.

**Rationale**:
- Per DOMAIN.md: "Event types are small and serializable"
- Records provide value equality and immutability
- Structs avoid heap allocation for high-frequency events
- Include all data needed by subscribers (no callbacks to source)

### Ownership and Lifecycle

**Decision**: Single `EventBus` instance owned by `GameState`, passed to domains.

**Rationale**:
- Avoids global/static state
- Clear ownership for save/load
- Domains receive bus via constructor or setup method
- Adapters access via `GameState.EventBus`

---

## Implementation Steps

### Phase 1: Event Bus Core (Priority: Critical)

#### Step 1.1: Create Event Bus Class

**New File**: `src/sim/EventBus.cs`

**Purpose**: Central hub for typed event registration and dispatch.

```csharp
using System;
using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// Minimal typed event bus for cross-domain communication.
/// Domains publish events, adapters and other domains subscribe.
/// </summary>
public class EventBus
{
    private readonly Dictionary<Type, List<Delegate>> subscribers = new();
    
    /// <summary>
    /// Subscribe to events of type TEvent.
    /// </summary>
    public void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : struct
    {
        var type = typeof(TEvent);
        if (!subscribers.TryGetValue(type, out var handlers))
        {
            handlers = new List<Delegate>();
            subscribers[type] = handlers;
        }
        
        if (!handlers.Contains(handler))
        {
            handlers.Add(handler);
        }
    }
    
    /// <summary>
    /// Unsubscribe from events of type TEvent.
    /// </summary>
    public void Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : struct
    {
        var type = typeof(TEvent);
        if (subscribers.TryGetValue(type, out var handlers))
        {
            handlers.Remove(handler);
        }
    }
    
    /// <summary>
    /// Publish an event to all subscribers.
    /// </summary>
    public void Publish<TEvent>(TEvent evt) where TEvent : struct
    {
        var type = typeof(TEvent);
        if (!subscribers.TryGetValue(type, out var handlers))
        {
            return;
        }
        
        // Iterate over a copy to allow unsubscribe during handling
        foreach (var handler in handlers.ToArray())
        {
            try
            {
                ((Action<TEvent>)handler)(evt);
            }
            catch (Exception ex)
            {
                SimLog.Log($"[EventBus] Error handling {type.Name}: {ex.Message}");
            }
        }
    }
    
    /// <summary>
    /// Clear all subscribers. Used for cleanup/testing.
    /// </summary>
    public void Clear()
    {
        subscribers.Clear();
    }
    
    /// <summary>
    /// Get subscriber count for a specific event type. For testing/debugging.
    /// </summary>
    public int GetSubscriberCount<TEvent>() where TEvent : struct
    {
        var type = typeof(TEvent);
        return subscribers.TryGetValue(type, out var handlers) ? handlers.Count : 0;
    }
}
```

**Acceptance Criteria**:
- [x] `EventBus` class with generic `Subscribe<T>`, `Unsubscribe<T>`, `Publish<T>`
- [x] Type constraint ensures events are structs (value types)
- [x] Duplicate subscription prevention
- [x] Safe iteration during publish (copy list)
- [x] Exception handling doesn't break other subscribers
- [x] `Clear()` for cleanup
- [x] `GetSubscriberCount<T>()` for testing

---

#### Step 1.2: Define Initial Event Types

**New File**: `src/sim/Events.cs`

**Purpose**: Define event structs for cross-domain communication.

```csharp
using Godot;

namespace FringeTactics;

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
    string ResourceType,  // "money", "fuel", "parts", "meds", "ammo"
    int OldValue,
    int NewValue,
    int Delta,
    string Reason         // "mission_cost", "job_reward", "travel", "rest", etc.
);

/// <summary>
/// Published when a job is accepted.
/// </summary>
public readonly record struct JobAcceptedEvent(
    int JobId,
    string JobTitle,
    int TargetNodeId,
    int DeadlineDay
);

/// <summary>
/// Published when a job is completed (success or failure).
/// </summary>
public readonly record struct JobCompletedEvent(
    int JobId,
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
    string Cause  // "combat", "mia", etc.
);
```

**Acceptance Criteria**:
- [x] All events are `readonly record struct`
- [x] Events contain all data needed by subscribers
- [x] No references to mutable objects
- [x] Clear naming convention: `<Subject><Verb>Event`
- [x] Events grouped by domain

---

### Phase 2: Integration Points (Priority: High)

#### Step 2.1: Add EventBus to GameState

**File**: `src/core/GameState.cs`

**Changes**:

```csharp
public class GameState
{
    // NEW: Central event bus
    public EventBus EventBus { get; private set; } = new();
    
    // ... existing code ...
    
    /// <summary>
    /// Reset event bus (for new game or cleanup).
    /// </summary>
    public void ResetEventBus()
    {
        EventBus.Clear();
    }
}
```

**Acceptance Criteria**:
- [x] `GameState.EventBus` property exists
- [x] `EventBus.Clear()` clears all subscribers (called in StartNewCampaign and GoToMainMenu)

---

#### Step 2.2: Publish Mission Events from CombatState

**File**: `src/sim/combat/state/CombatState.cs`

**Changes**: Add event bus reference and publish events.

```csharp
public class CombatState
{
    // NEW: Event bus reference (optional, set by GameState)
    public EventBus EventBus { get; set; }
    
    // ... existing code ...
    
    private void EndMission(MissionOutcome outcome)
    {
        FinalOutcome = outcome;
        Victory = (outcome == MissionOutcome.Victory);
        Phase = MissionPhase.Complete;
        TimeSystem.Pause();
        PhaseChanged?.Invoke(Phase);
        MissionEnded?.Invoke(Victory);
        MissionCompleted?.Invoke(outcome);
        
        // NEW: Publish to event bus
        EventBus?.Publish(new MissionCompletedEvent(
            Outcome: outcome,
            EnemiesKilled: Stats.EnemiesKilled,
            CrewDeaths: Stats.CrewDeaths,
            CrewInjured: Stats.CrewInjured,
            DurationSeconds: TimeSystem.GetCurrentTime()
        ));
    }
    
    private void OnActorDied(Actor actor)
    {
        ActorDied?.Invoke(actor);
        
        // NEW: Publish to event bus
        EventBus?.Publish(new ActorDiedEvent(
            ActorId: actor.Id,
            ActorType: actor.Type,
            ActorName: actor.Name ?? $"{actor.Type}#{actor.Id}",
            KillerId: 0, // TODO: Track killer
            Position: actor.GridPosition
        ));
    }
}
```

**Acceptance Criteria**:
- [x] `CombatState.EventBus` property exists
- [x] `MissionCompletedEvent` published on mission end
- [x] `ActorDiedEvent` published on actor death
- [x] Existing C# events still work (backward compatible)

---

#### Step 2.3: Publish Campaign Events from CampaignState

**File**: `src/sim/campaign/CampaignState.cs`

**Changes**: Add event bus reference and publish events.

```csharp
public class CampaignState
{
    // NEW: Event bus reference (optional, set by GameState)
    public EventBus EventBus { get; set; }
    
    // ... existing code ...
    
    // Helper to publish resource changes
    private void PublishResourceChange(string resourceType, int oldValue, int newValue, string reason)
    {
        EventBus?.Publish(new ResourceChangedEvent(
            ResourceType: resourceType,
            OldValue: oldValue,
            NewValue: newValue,
            Delta: newValue - oldValue,
            Reason: reason
        ));
    }
    
    public bool AcceptJob(Job job)
    {
        // ... existing validation ...
        
        CurrentJob = job;
        AvailableJobs.Remove(job);
        
        // Set absolute deadline
        if (job.DeadlineDays > 0)
        {
            job.DeadlineDay = Time.CurrentDay + job.DeadlineDays;
        }
        
        // Generate mission config
        CurrentJob.MissionConfig = JobSystem.GenerateMissionConfig(job, CreateSeededRandom());
        
        // NEW: Publish event
        EventBus?.Publish(new JobAcceptedEvent(
            JobId: job.Id,
            JobTitle: job.Title,
            TargetNodeId: job.TargetNodeId,
            DeadlineDay: job.DeadlineDay
        ));
        
        SimLog.Log($"[Campaign] Accepted job: {job.Title}");
        return true;
    }
    
    public void ModifyFactionRep(string factionId, int delta)
    {
        // ... existing code ...
        
        int oldRep = FactionRep.GetValueOrDefault(factionId, 50);
        FactionRep[factionId] = Math.Clamp(oldRep + delta, 0, 100);
        int newRep = FactionRep[factionId];
        
        var factionName = Sector.Factions.GetValueOrDefault(factionId, factionId);
        
        // NEW: Publish event
        EventBus?.Publish(new FactionRepChangedEvent(
            FactionId: factionId,
            FactionName: factionName,
            OldRep: oldRep,
            NewRep: newRep,
            Delta: delta
        ));
        
        SimLog.Log($"[Campaign] {factionName} rep: {newRep} ({(delta >= 0 ? "+" : "")}{delta})");
    }
}
```

**Acceptance Criteria**:
- [x] `CampaignState.EventBus` property exists
- [x] `JobAcceptedEvent` published when job accepted
- [x] `FactionRepChangedEvent` published when rep changes
- [x] `ResourceChangedEvent` published when resources change (mission cost, job reward)

---

#### Step 2.4: Publish Day Advanced Events

**File**: `src/sim/CampaignTime.cs`

**Changes**: Add event bus reference and publish events.

```csharp
public class CampaignTime
{
    // NEW: Event bus reference (optional)
    public EventBus EventBus { get; set; }
    
    // ... existing code ...
    
    public int AdvanceDays(int days)
    {
        if (days <= 0)
        {
            SimLog.Log($"[CampaignTime] Warning: Attempted to advance by {days} days (ignored)");
            return CurrentDay;
        }

        int oldDay = CurrentDay;
        CurrentDay += days;

        SimLog.Log($"[CampaignTime] Day {oldDay} -> Day {CurrentDay} (+{days} days)");
        DayAdvanced?.Invoke(oldDay, CurrentDay);
        
        // NEW: Publish to event bus
        EventBus?.Publish(new DayAdvancedEvent(
            OldDay: oldDay,
            NewDay: CurrentDay,
            DaysAdvanced: days
        ));

        return CurrentDay;
    }
}
```

**Acceptance Criteria**:
- [x] `CampaignTime.EventBus` property exists
- [x] `DayAdvancedEvent` published when days advance
- [x] Existing C# event still works

---

#### Step 2.5: Wire Up Event Bus in GameState

**File**: `src/core/GameState.cs`

**Changes**: Connect event bus to campaign and combat states.

```csharp
public class GameState
{
    public EventBus EventBus { get; private set; } = new();
    
    // When creating a new campaign
    public void StartNewCampaign(int seed)
    {
        Campaign = CampaignState.CreateNew(seed);
        Campaign.EventBus = EventBus;
        Campaign.Time.EventBus = EventBus;
    }
    
    // When starting a mission
    public void StartMission(MissionConfig config)
    {
        CurrentCombat = new CombatState(/* ... */);
        CurrentCombat.EventBus = EventBus;
    }
}
```

**Acceptance Criteria**:
- [x] New campaigns have event bus wired
- [x] New combat states have event bus wired
- [x] CampaignTime has event bus wired

---

### Phase 3: Integration with TravelSystem (Priority: Medium)

#### Step 3.1: Publish Travel Events

**File**: `src/sim/campaign/TravelSystem.cs`

**Changes**: Add event publishing for travel completion.

```csharp
public static class TravelSystem
{
    public static bool Travel(CampaignState campaign, int targetNodeId)
    {
        // ... existing validation and cost calculation ...
        
        var from = campaign.GetCurrentNode();
        var to = campaign.Sector.GetNode(targetNodeId);
        
        int fuelCost = CalculateFuelCost(from, to);
        int timeCost = CalculateTravelDays(from, to);
        
        if (campaign.Fuel < fuelCost)
        {
            return false;
        }
        
        // Apply costs
        int oldFuel = campaign.Fuel;
        campaign.Fuel -= fuelCost;
        campaign.Time.AdvanceDays(timeCost);
        campaign.CurrentNodeId = targetNodeId;
        
        // Publish resource change
        campaign.EventBus?.Publish(new ResourceChangedEvent(
            ResourceType: "fuel",
            OldValue: oldFuel,
            NewValue: campaign.Fuel,
            Delta: -fuelCost,
            Reason: "travel"
        ));
        
        // Publish travel completion
        campaign.EventBus?.Publish(new TravelCompletedEvent(
            FromNodeId: from.Id,
            ToNodeId: to.Id,
            ToNodeName: to.Name,
            FuelCost: fuelCost,
            DaysCost: timeCost
        ));
        
        SimLog.Log($"[Travel] Traveled to {to.Name}. Cost: {fuelCost} fuel, {timeCost} days.");
        return true;
    }
}
```

**Acceptance Criteria**:
- [x] `TravelCompletedEvent` published on successful travel
- [x] `ResourceChangedEvent` published for fuel consumption

---

### Phase 4: Testing (Priority: High)

#### Step 4.1: Create Event Bus Unit Tests

**New File**: `tests/sim/foundation/SF2EventBusTests.cs`

```csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FringeTactics;
using System.Collections.Generic;

namespace FringeTactics.Tests;

[TestClass]
public class EventBusTests
{
    [TestMethod]
    public void Subscribe_ReceivesPublishedEvents()
    {
        var bus = new EventBus();
        var received = new List<DayAdvancedEvent>();
        
        bus.Subscribe<DayAdvancedEvent>(e => received.Add(e));
        bus.Publish(new DayAdvancedEvent(1, 2, 1));
        
        Assert.AreEqual(1, received.Count);
        Assert.AreEqual(1, received[0].OldDay);
        Assert.AreEqual(2, received[0].NewDay);
    }
    
    [TestMethod]
    public void Subscribe_MultipleHandlers_AllReceive()
    {
        var bus = new EventBus();
        int count1 = 0, count2 = 0;
        
        bus.Subscribe<DayAdvancedEvent>(_ => count1++);
        bus.Subscribe<DayAdvancedEvent>(_ => count2++);
        bus.Publish(new DayAdvancedEvent(1, 2, 1));
        
        Assert.AreEqual(1, count1);
        Assert.AreEqual(1, count2);
    }
    
    [TestMethod]
    public void Subscribe_DifferentEventTypes_Isolated()
    {
        var bus = new EventBus();
        int dayCount = 0, missionCount = 0;
        
        bus.Subscribe<DayAdvancedEvent>(_ => dayCount++);
        bus.Subscribe<MissionCompletedEvent>(_ => missionCount++);
        
        bus.Publish(new DayAdvancedEvent(1, 2, 1));
        
        Assert.AreEqual(1, dayCount);
        Assert.AreEqual(0, missionCount);
    }
    
    [TestMethod]
    public void Unsubscribe_StopsReceivingEvents()
    {
        var bus = new EventBus();
        int count = 0;
        Action<DayAdvancedEvent> handler = _ => count++;
        
        bus.Subscribe(handler);
        bus.Publish(new DayAdvancedEvent(1, 2, 1));
        Assert.AreEqual(1, count);
        
        bus.Unsubscribe(handler);
        bus.Publish(new DayAdvancedEvent(2, 3, 1));
        Assert.AreEqual(1, count); // Still 1, not 2
    }
    
    [TestMethod]
    public void Publish_NoSubscribers_DoesNotThrow()
    {
        var bus = new EventBus();
        bus.Publish(new DayAdvancedEvent(1, 2, 1)); // Should not throw
    }
    
    [TestMethod]
    public void Subscribe_DuplicateHandler_OnlyCalledOnce()
    {
        var bus = new EventBus();
        int count = 0;
        Action<DayAdvancedEvent> handler = _ => count++;
        
        bus.Subscribe(handler);
        bus.Subscribe(handler); // Duplicate
        bus.Publish(new DayAdvancedEvent(1, 2, 1));
        
        Assert.AreEqual(1, count); // Only called once
    }
    
    [TestMethod]
    public void Clear_RemovesAllSubscribers()
    {
        var bus = new EventBus();
        int count = 0;
        
        bus.Subscribe<DayAdvancedEvent>(_ => count++);
        bus.Clear();
        bus.Publish(new DayAdvancedEvent(1, 2, 1));
        
        Assert.AreEqual(0, count);
    }
    
    [TestMethod]
    public void GetSubscriberCount_ReturnsCorrectCount()
    {
        var bus = new EventBus();
        
        Assert.AreEqual(0, bus.GetSubscriberCount<DayAdvancedEvent>());
        
        bus.Subscribe<DayAdvancedEvent>(_ => { });
        Assert.AreEqual(1, bus.GetSubscriberCount<DayAdvancedEvent>());
        
        bus.Subscribe<DayAdvancedEvent>(_ => { });
        Assert.AreEqual(2, bus.GetSubscriberCount<DayAdvancedEvent>());
    }
    
    [TestMethod]
    public void Publish_HandlerThrows_OtherHandlersStillCalled()
    {
        var bus = new EventBus();
        int count = 0;
        
        bus.Subscribe<DayAdvancedEvent>(_ => throw new System.Exception("Test"));
        bus.Subscribe<DayAdvancedEvent>(_ => count++);
        
        bus.Publish(new DayAdvancedEvent(1, 2, 1));
        
        Assert.AreEqual(1, count); // Second handler still called
    }
    
    [TestMethod]
    public void Publish_UnsubscribeDuringHandler_Safe()
    {
        var bus = new EventBus();
        int count = 0;
        Action<DayAdvancedEvent> handler = null;
        handler = _ => {
            count++;
            bus.Unsubscribe(handler);
        };
        
        bus.Subscribe(handler);
        bus.Publish(new DayAdvancedEvent(1, 2, 1));
        bus.Publish(new DayAdvancedEvent(2, 3, 1));
        
        Assert.AreEqual(1, count); // Only first publish received
    }
}

[TestClass]
public class EventTypesTests
{
    [TestMethod]
    public void DayAdvancedEvent_ContainsCorrectData()
    {
        var evt = new DayAdvancedEvent(5, 8, 3);
        
        Assert.AreEqual(5, evt.OldDay);
        Assert.AreEqual(8, evt.NewDay);
        Assert.AreEqual(3, evt.DaysAdvanced);
    }
    
    [TestMethod]
    public void MissionCompletedEvent_ContainsCorrectData()
    {
        var evt = new MissionCompletedEvent(
            Outcome: MissionOutcome.Victory,
            EnemiesKilled: 5,
            CrewDeaths: 1,
            CrewInjured: 2,
            DurationSeconds: 120.5f
        );
        
        Assert.AreEqual(MissionOutcome.Victory, evt.Outcome);
        Assert.AreEqual(5, evt.EnemiesKilled);
        Assert.AreEqual(1, evt.CrewDeaths);
        Assert.AreEqual(2, evt.CrewInjured);
        Assert.AreEqual(120.5f, evt.DurationSeconds);
    }
    
    [TestMethod]
    public void ResourceChangedEvent_ContainsCorrectData()
    {
        var evt = new ResourceChangedEvent(
            ResourceType: "fuel",
            OldValue: 100,
            NewValue: 85,
            Delta: -15,
            Reason: "travel"
        );
        
        Assert.AreEqual("fuel", evt.ResourceType);
        Assert.AreEqual(100, evt.OldValue);
        Assert.AreEqual(85, evt.NewValue);
        Assert.AreEqual(-15, evt.Delta);
        Assert.AreEqual("travel", evt.Reason);
    }
}
```

**Acceptance Criteria**:
- [x] All unit tests pass
- [x] Tests cover subscribe, unsubscribe, publish
- [x] Tests cover edge cases (no subscribers, duplicate handlers, exceptions)
- [x] Tests verify event data integrity

---

#### Step 4.2: Create Integration Tests

**New File**: `tests/sim/foundation/SF2IntegrationTests.cs`

```csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FringeTactics;
using System.Collections.Generic;

namespace FringeTactics.Tests;

[TestClass]
public class EventBusIntegrationTests
{
    [TestMethod]
    public void CampaignTime_PublishesDayAdvancedEvent()
    {
        var bus = new EventBus();
        var time = new CampaignTime();
        time.EventBus = bus;
        
        var received = new List<DayAdvancedEvent>();
        bus.Subscribe<DayAdvancedEvent>(e => received.Add(e));
        
        time.AdvanceDays(3);
        
        Assert.AreEqual(1, received.Count);
        Assert.AreEqual(1, received[0].OldDay);
        Assert.AreEqual(4, received[0].NewDay);
        Assert.AreEqual(3, received[0].DaysAdvanced);
    }
    
    [TestMethod]
    public void CampaignTime_BothCSharpEventAndBusWork()
    {
        var bus = new EventBus();
        var time = new CampaignTime();
        time.EventBus = bus;
        
        int csharpEventCount = 0;
        int busEventCount = 0;
        
        time.DayAdvanced += (_, _) => csharpEventCount++;
        bus.Subscribe<DayAdvancedEvent>(_ => busEventCount++);
        
        time.AdvanceDays(1);
        
        Assert.AreEqual(1, csharpEventCount);
        Assert.AreEqual(1, busEventCount);
    }
    
    [TestMethod]
    public void CampaignTime_NoBus_StillWorks()
    {
        var time = new CampaignTime();
        // EventBus is null
        
        time.AdvanceDays(5); // Should not throw
        
        Assert.AreEqual(6, time.CurrentDay);
    }
}
```

**Acceptance Criteria**:
- [x] Integration tests verify event bus wiring
- [x] Tests confirm backward compatibility (null bus doesn't break)
- [x] Tests confirm both C# events and bus events fire

---

## Implementation Order

1. **Day 1: Core Event Bus**
   - Step 1.1: Create EventBus class
   - Step 1.2: Define initial event types
   - Step 4.1: Unit tests for EventBus

2. **Day 2: Campaign Integration**
   - Step 2.1: Add EventBus to GameState
   - Step 2.4: Publish DayAdvancedEvent from CampaignTime
   - Step 2.3: Publish events from CampaignState
   - Step 4.2: Integration tests

3. **Day 3: Combat Integration**
   - Step 2.2: Publish events from CombatState
   - Step 2.5: Wire up in GameState

4. **Day 4: Travel & Polish**
   - Step 3.1: Publish travel events
   - Complete all tests
   - Documentation updates

---

## Success Criteria for SF2

When SF2 is complete, you should be able to:

1. ✅ Create an `EventBus` and subscribe to typed events
2. ✅ Publish events from sim layer classes
3. ✅ Subscribe to `DayAdvancedEvent` and receive notifications
4. ✅ Subscribe to `MissionCompletedEvent` and receive notifications
5. ✅ Subscribe to `ResourceChangedEvent` and receive notifications
6. ✅ Existing C# events still work (backward compatible)
7. ✅ Event bus is accessible via `GameState.EventBus`

**Natural Pause Point**: After SF2, domains can communicate via events without direct coupling. This enables the Simulation domain (G3) to subscribe to campaign/tactical events.

---

## File Summary

| File | Action | Description |
|------|--------|-------------|
| `src/sim/EventBus.cs` | NEW | Central event bus with typed subscribe/publish |
| `src/sim/Events.cs` | NEW | Event type definitions (structs) |
| `src/sim/CampaignTime.cs` | MODIFIED | Add EventBus property, publish DayAdvancedEvent |
| `src/sim/campaign/CampaignState.cs` | MODIFIED | Add EventBus property, publish campaign events |
| `src/sim/campaign/TravelSystem.cs` | MODIFIED | Publish TravelCompletedEvent |
| `src/sim/combat/state/CombatState.cs` | MODIFIED | Add EventBus property, publish combat events |
| `src/core/GameState.cs` | MODIFIED | Own EventBus, wire to campaign/combat |
| `tests/sim/foundation/SF2EventBusTests.cs` | NEW | Unit tests |
| `tests/sim/foundation/SF2IntegrationTests.cs` | NEW | Integration tests |

---

## Integration Points

### How Events Flow Through the System

```
┌─────────────────────────────────────────────────────────────────┐
│                        EVENT BUS                                 │
│                                                                 │
│  GameState.EventBus (single instance)                           │
│  └── Subscribe<TEvent>(handler)                                 │
│  └── Publish<TEvent>(event)                                     │
│                                                                 │
│  Subscribers:                                                   │
│  • UI/Scenes (adapter layer)                                    │
│  • Future: Simulation domain (G3)                               │
│  • Future: Achievement system                                   │
│  • Future: Analytics/logging                                    │
└─────────────────────────────────────────────────────────────────┘
                              ▲
                              │ Publish
                              │
┌─────────────────────────────────────────────────────────────────┐
│                     CAMPAIGN LAYER                               │
│                                                                 │
│  CampaignState.EventBus → publishes:                            │
│  • JobAcceptedEvent                                             │
│  • JobCompletedEvent                                            │
│  • FactionRepChangedEvent                                       │
│  • ResourceChangedEvent                                         │
│                                                                 │
│  CampaignTime.EventBus → publishes:                             │
│  • DayAdvancedEvent                                             │
│                                                                 │
│  TravelSystem → publishes (via CampaignState.EventBus):         │
│  • TravelCompletedEvent                                         │
│  • ResourceChangedEvent (fuel)                                  │
└─────────────────────────────────────────────────────────────────┘
                              ▲
                              │ Publish
                              │
┌─────────────────────────────────────────────────────────────────┐
│                     TACTICAL LAYER                               │
│                                                                 │
│  CombatState.EventBus → publishes:                              │
│  • MissionCompletedEvent                                        │
│  • MissionPhaseChangedEvent                                     │
│  • ActorDiedEvent                                               │
│  • AlarmStateChangedEvent                                       │
│                                                                 │
│  (Existing C# events remain for intra-system use)               │
└─────────────────────────────────────────────────────────────────┘
```

### Event Bus vs C# Events Decision Matrix

| Scenario | Use Event Bus | Use C# Event |
|----------|---------------|--------------|
| Cross-domain notification | ✅ | ❌ |
| UI needs to react | ✅ | ❌ |
| Same-system internal | ❌ | ✅ |
| High frequency (>10/sec) | ❌ | ✅ |
| Needs multiple subscribers | ✅ | ✅ |
| Subscriber unknown at compile time | ✅ | ❌ |

---

## Notes for Future Milestones

### SF3 Dependencies (Save/Load)
- EventBus itself is not serialized (it's runtime infrastructure)
- Event bus should be cleared and re-wired on load
- Consider: replay events from save for debugging?

### G3 Dependencies (Simulation)
- Simulation domain will subscribe to:
  - `DayAdvancedEvent` → tick world simulation
  - `MissionCompletedEvent` → update faction states
  - `TravelCompletedEvent` → trigger encounters
- Event bus enables this without Simulation knowing about Campaign/Tactical internals

### Future Extensions
- **Event history**: Store recent events for debugging/replay
- **Event filtering**: Subscribe with predicates
- **Async events**: For long-running handlers
- **Event priority**: Control handler order
- **Event cancellation**: Allow handlers to stop propagation

---

## Open Questions

1. **Event Naming**: Should events use past tense (`DayAdvanced`) or present (`DayAdvancing`)?
   - *Decision*: Past tense. Events represent facts that have happened.

2. **Event Granularity**: Should `ResourceChangedEvent` be one event or separate events per resource?
   - *Decision*: One event with `ResourceType` field. Simpler, subscribers can filter.

3. **Null EventBus**: Should we require EventBus or allow null?
   - *Decision*: Allow null for backward compatibility and testing. Use `EventBus?.Publish()`.

4. **Thread Safety**: Should EventBus be thread-safe?
   - *Decision*: No for SF2. Game runs single-threaded. Can add if needed later.

5. **Event Ordering**: Should events be queued or dispatched immediately?
   - *Decision*: Immediate dispatch for SF2. Simpler, no hidden state.

---

## Glossary

- **Event Bus**: Central hub for publishing and subscribing to typed events
- **Event**: Immutable data struct representing something that happened
- **Publisher**: Code that calls `EventBus.Publish<T>(event)`
- **Subscriber**: Code that calls `EventBus.Subscribe<T>(handler)`
- **Handler**: Callback function invoked when matching event is published
- **Cross-domain**: Communication between Campaign, Tactical, and Simulation layers
