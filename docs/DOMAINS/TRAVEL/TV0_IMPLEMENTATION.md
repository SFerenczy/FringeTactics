# TV0 – Concept Finalization: Implementation Plan

**Status**: ✅ Complete  
**Depends on**: WD2 (Sector Topology) ✅ Complete  
**Phase**: G2

---

## Overview

**Goal**: Finalize design decisions for travel mechanics before implementation, ensuring alignment with DOMAIN.md, CAMPAIGN_FOUNDATIONS.md, and the existing World domain infrastructure.

This is primarily a **conceptual milestone**. The deliverable is a locked design that TV1 and TV2 can implement without ambiguity.

---

## Current State Assessment

### What We Have (from WD2)

| Component | Status | Location | Notes |
|-----------|--------|----------|-------|
| `Route` | ✅ Complete | `src/sim/world/Route.cs` | Distance, HazardLevel, Tags |
| `WorldState.FindPath()` | ✅ Complete | `src/sim/world/WorldState.cs` | BFS pathfinding |
| `WorldState.GetPathDistance()` | ✅ Complete | `src/sim/world/WorldState.cs` | Sum route distances |
| `WorldState.GetPathHazard()` | ✅ Complete | `src/sim/world/WorldState.cs` | Sum route hazards |
| `WorldState.GetRoute()` | ✅ Complete | `src/sim/world/WorldState.cs` | Query route between systems |
| `WorldTags` (route tags) | ✅ Complete | `src/sim/world/WorldTags.cs` | Dangerous, Patrolled, Hidden, etc. |
| Test sector | ✅ Complete | `WorldState.CreateTestSector()` | 8 systems, 7 routes |

### What We Have (from Management/Campaign)

| Component | Status | Location | Notes |
|-----------|--------|----------|-------|
| `CampaignState.Fuel` | ✅ Exists | `src/sim/campaign/CampaignState.cs` | Current fuel resource |
| `CampaignState.CurrentNodeId` | ✅ Exists | `src/sim/campaign/CampaignState.cs` | Current system location |
| `Ship.FuelCapacity` | ✅ Exists | `src/sim/campaign/Ship.cs` | Max fuel tank |
| `CampaignTime` | ✅ Exists | `src/sim/CampaignTime.cs` | Day-based time tracking |
| `RngService` | ✅ Exists | `src/sim/RngService.cs` | Deterministic RNG |
| `EventBus` | ✅ Exists | `src/sim/EventBus.cs` | Cross-domain events |

### Gaps for TV0

| Requirement | Current Status | Gap |
|-------------|----------------|-----|
| Travel cost formulas | ❌ Not defined | Need fuel/time formulas |
| Risk calculation model | ❌ Not defined | Need encounter probability formula |
| `TravelPlan` structure | ❌ Not defined | Need data structure design |
| `TravelState` structure | ❌ Not defined | Need in-progress travel design |
| Route preference options | ❌ Not defined | Need preference vocabulary |
| Encounter trigger points | ❌ Not defined | Need when/how encounters fire |

---

## TV0 Deliverables Checklist

### Phase 1: Travel Cost Model
- [ ] **1.1** Define fuel consumption formula
- [ ] **1.2** Define time cost formula
- [ ] **1.3** Define ship stat modifiers
- [ ] **1.4** Document cost examples for test sector

### Phase 2: Risk & Encounter Model
- [ ] **2.1** Define base encounter chance formula
- [ ] **2.2** Define modifiers (route tags, system metrics, cargo)
- [ ] **2.3** Define encounter trigger timing (per-segment vs per-route)
- [ ] **2.4** Define encounter type weighting

### Phase 3: Data Structures
- [ ] **3.1** Design `TravelSegment` structure
- [ ] **3.2** Design `TravelPlan` structure
- [ ] **3.3** Design `TravelPreferences` structure
- [ ] **3.4** Design `TravelState` structure
- [ ] **3.5** Design `TravelResult` structure
- [ ] **3.6** Design `TravelContext` structure (for Encounter domain)

### Phase 4: Integration Contracts
- [ ] **4.1** Define Management integration (fuel consumption)
- [ ] **4.2** Define Time integration (day advancement)
- [ ] **4.3** Define Encounter integration (trigger and resume)
- [ ] **4.4** Define event vocabulary

### Phase 5: Edge Cases & Failure Modes
- [ ] **5.1** Define out-of-fuel behavior
- [ ] **5.2** Define interrupted travel behavior
- [ ] **5.3** Define blocked route behavior

---

## Phase 1: Travel Cost Model

### 1.1 Fuel Consumption Formula

**Decision**: Linear fuel consumption based on route distance and ship efficiency.

```
fuelCost = ceil(distance * fuelRate / shipEfficiency)
```

| Variable | Description | Default |
|----------|-------------|---------|
| `distance` | Route.Distance (from WD2) | varies |
| `fuelRate` | Base fuel per distance unit | 0.1 |
| `shipEfficiency` | Ship stat (1.0 = normal) | 1.0 |

**Examples** (using test sector):

| Route | Distance | Fuel Cost |
|-------|----------|-----------|
| Haven → Waypoint | ~150 | 15 |
| Waypoint → Rockfall | ~150 | 15 |
| Waypoint → Contested | ~150 | 15 |
| Rockfall → Wreck | ~112 | 12 |

**Ship Efficiency Modifiers**:

| Ship Type | Efficiency | Notes |
|-----------|------------|-------|
| Scout | 1.2 | Fuel-efficient |
| Corvette | 1.0 | Baseline |
| Freighter | 0.8 | Fuel-hungry |
| Gunship | 0.7 | Combat-focused, inefficient |

**Cargo Load Penalty** (optional for G2):
```
effectiveEfficiency = shipEfficiency * (1 - cargoLoad * 0.1)
```
Where `cargoLoad` is 0.0-1.0 (percentage of cargo capacity used).

---

### 1.2 Time Cost Formula

**Decision**: Linear time based on distance and ship speed.

```
travelDays = ceil(distance / shipSpeed)
```

| Variable | Description | Default |
|----------|-------------|---------|
| `distance` | Route.Distance | varies |
| `shipSpeed` | Ship stat (units per day) | 100 |

**Examples** (using test sector):

| Route | Distance | Travel Days |
|-------|----------|-------------|
| Haven → Waypoint | ~150 | 2 |
| Waypoint → Rockfall | ~150 | 2 |
| Multi-hop: Haven → Rockfall | ~300 | 3 |

**Ship Speed Modifiers**:

| Ship Type | Speed | Notes |
|-----------|-------|-------|
| Scout | 120 | Fast |
| Corvette | 100 | Baseline |
| Freighter | 80 | Slow |
| Gunship | 90 | Moderate |

---

### 1.3 Ship Stat Integration

**Current Ship Stats** (from `Ship.cs`):
- `FuelCapacity` - max fuel tank
- `Hull` / `MaxHull` - ship health

**Stats to Add** (in TV1 or MG2):

| Stat | Type | Default | Purpose |
|------|------|---------|---------|
| `FuelEfficiency` | float | 1.0 | Multiplier for fuel consumption |
| `Speed` | float | 100 | Units per day |
| `SensorRange` | int | 1 | Affects encounter detection (future) |
| `StealthRating` | int | 0 | Reduces encounter chance (future) |

**Decision**: For TV1, use hardcoded defaults. Ship stat integration is MG2+.

---

### 1.4 Cost Examples for Test Sector

Using default ship stats (efficiency=1.0, speed=100):

| From | To | Distance | Fuel | Days | Hazard |
|------|-----|----------|------|------|--------|
| Haven (0) | Waypoint (1) | 150 | 15 | 2 | 1 |
| Haven (0) | Patrol (5) | 100 | 10 | 1 | 0 |
| Waypoint (1) | Rockfall (2) | 150 | 15 | 2 | 2 |
| Waypoint (1) | Contested (4) | 150 | 15 | 2 | 3 |
| Waypoint (1) | Smuggler (6) | 100 | 10 | 1 | 2 |
| Rockfall (2) | Wreck (7) | 112 | 12 | 2 | 3 |
| Red Claw (3) | Smuggler (6) | 100 | 10 | 1 | 2 |

**Multi-hop Example**: Haven → Rockfall (via Waypoint)
- Total Distance: 300
- Total Fuel: 30
- Total Days: 4
- Total Hazard: 3

---

## Phase 2: Risk & Encounter Model

### 2.1 Base Encounter Chance Formula

**Decision**: Per-segment encounter roll with base chance from route hazard.

```
baseChance = hazardLevel * 0.1  // 0-5 hazard → 0-50% base
```

| Hazard Level | Base Chance | Description |
|--------------|-------------|-------------|
| 0 | 0% | Safe (patrolled core) |
| 1 | 10% | Low risk |
| 2 | 20% | Moderate risk |
| 3 | 30% | Dangerous |
| 4 | 40% | Very dangerous |
| 5 | 50% | Extremely dangerous |

---

### 2.2 Encounter Chance Modifiers

**Route Tag Modifiers**:

| Tag | Modifier | Notes |
|-----|----------|-------|
| `patrolled` | -10% | Reduced risk |
| `dangerous` | +10% | Increased risk |
| `hidden` | -5% | Less traffic, fewer encounters |
| `blockaded` | +20% | Active conflict |
| `asteroid_field` | +5% | Navigation hazards |
| `nebula` | +5% | Sensor interference |

**System Metric Modifiers** (from WD3, when available):

| Metric | Condition | Modifier |
|--------|-----------|----------|
| `SecurityLevel` | ≥4 | -10% |
| `SecurityLevel` | ≤1 | +10% |
| `CriminalActivity` | ≥4 | +15% |
| `CriminalActivity` | ≤1 | -5% |

**Cargo Modifiers** (future):

| Condition | Modifier | Notes |
|-----------|----------|-------|
| High-value cargo | +10% | Pirates attracted |
| Illegal cargo | +5% | Patrol attention |
| Empty cargo | -5% | Not worth attacking |

**Final Formula**:
```
encounterChance = clamp(baseChance + tagModifiers + metricModifiers + cargoModifiers, 0, 0.8)
```

Maximum 80% to avoid guaranteed encounters.

---

### 2.3 Encounter Trigger Timing

**Decision**: Roll once per day of travel, not per segment.

**Rationale**:
- Longer routes should have more encounter opportunities
- A 4-day journey has 4 chances for encounters
- More granular than per-segment, scales with distance

**Formula**:
```
rollsPerSegment = segment.TimeDays
for each day in segment:
    roll = rng.NextFloat()
    if roll < encounterChance:
        pause travel
        trigger encounter with TravelContext
        if encounter result is "abort":
            return TravelResult.Interrupted
        resume travel
    advance 1 day
    consume proportional fuel (segment.FuelCost / segment.TimeDays)
```

**Flow**:
```
for each segment in travelPlan:
    for each day in segment.TimeDays:
        roll = rng.NextFloat()
        if roll < encounterChance:
            pause travel, trigger encounter
            if abort: return Interrupted
        consume daily fuel portion
        advance 1 day
```

**Example**: Haven → Rockfall (4 days total)
- Day 1: Roll for encounter (segment 1)
- Day 2: Roll for encounter (segment 1)
- Day 3: Roll for encounter (segment 2)
- Day 4: Roll for encounter (segment 2)

---

### 2.4 Encounter Type Weighting

**Decision**: Encounter type selected based on route/system context.

**Base Weights** (per route hazard):

| Hazard | Pirate | Patrol | Trader | Anomaly | Distress |
|--------|--------|--------|--------|---------|----------|
| 0-1 | 10% | 40% | 30% | 10% | 10% |
| 2-3 | 30% | 20% | 20% | 15% | 15% |
| 4-5 | 50% | 10% | 10% | 15% | 15% |

**Tag-Based Adjustments**:

| Tag | Effect |
|-----|--------|
| `patrolled` | +20% patrol, -20% pirate |
| `lawless` | +30% pirate, -30% patrol |
| `hidden` | +20% smuggler, -10% patrol |
| `derelict` | +30% anomaly/distress |

**System Metric Adjustments** (WD3):

| Metric High | Effect |
|-------------|--------|
| `CriminalActivity` | +20% pirate |
| `SecurityLevel` | +20% patrol |
| `EconomicActivity` | +20% trader |

**Output**: `TravelContext.SuggestedEncounterType` for Generation domain.

---

## Phase 3: Data Structures

### 3.1 TravelSegment Structure

```csharp
public class TravelSegment
{
    public int FromSystemId { get; set; }
    public int ToSystemId { get; set; }
    public Route Route { get; set; }  // Reference to world route
    
    // Computed costs
    public int FuelCost { get; set; }
    public int TimeDays { get; set; }
    
    // Risk
    public float EncounterChance { get; set; }
    public string SuggestedEncounterType { get; set; }
}
```

---

### 3.2 TravelPlan Structure

```csharp
public class TravelPlan
{
    public int OriginSystemId { get; set; }
    public int DestinationSystemId { get; set; }
    public List<TravelSegment> Segments { get; set; } = new();
    
    // Aggregates
    public int TotalFuelCost { get; set; }
    public int TotalTimeDays { get; set; }
    public int TotalHazard { get; set; }
    public float AverageEncounterChance { get; set; }
    
    // Validation
    public bool IsValid { get; set; }
    public string InvalidReason { get; set; }  // "no_route", "insufficient_fuel", etc.
}
```

---

### 3.3 Route Selection Model

**Decision**: Player clicks destination, system calculates shortest weighted path. Player chains clicks for custom routes.

**UI Flow**:
1. Player clicks on destination system
2. `TravelPlanner.PlanRoute(current, destination)` returns shortest weighted path
3. UI shows path, fuel cost, time, hazard summary
4. Player can:
   - **Confirm**: Execute travel to destination
   - **Chain**: Click intermediate system first to force a waypoint
   - **Cancel**: Abort planning

**Example - Custom Route**:
- Player at Haven (0), wants to reach Red Claw (3)
- Direct path might go through dangerous Contested Zone
- Player clicks Smuggler's Den (6) first → plans Haven → Smuggler's Den
- Then clicks Red Claw (3) → plans Smuggler's Den → Red Claw
- Result: Two separate travel plans, player controls the route

**TravelPlanner API**:
```csharp
public class TravelPlanner
{
    /// <summary>
    /// Plan shortest weighted path from origin to destination.
    /// Uses A* with cost = distance + (hazard * 50).
    /// </summary>
    public TravelPlan PlanRoute(int originId, int destinationId);
    
    /// <summary>
    /// Validate if a plan is executable (enough fuel, route exists).
    /// </summary>
    public bool ValidatePlan(TravelPlan plan, CampaignState campaign);
}
```

**No TravelPreferences class needed for G2** - shortest path is always calculated. Player controls route by chaining destinations.

---

### 3.4 TravelState Structure

```csharp
public class TravelState
{
    public TravelPlan Plan { get; set; }
    
    /// <summary>
    /// Current segment index (0-based).
    /// </summary>
    public int CurrentSegmentIndex { get; set; } = 0;
    
    /// <summary>
    /// Whether we're paused for an encounter.
    /// </summary>
    public bool IsPausedForEncounter { get; set; } = false;
    
    /// <summary>
    /// Encounter instance if paused.
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
    public List<string> EncounterHistory { get; set; } = new();
    
    // Helpers
    public bool IsComplete => CurrentSegmentIndex >= Plan.Segments.Count;
    public TravelSegment CurrentSegment => Plan.Segments[CurrentSegmentIndex];
}
```

---

### 3.5 TravelResult Structure

```csharp
public class TravelResult
{
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
    public string InterruptReason { get; set; }
}

public enum TravelResultStatus
{
    Completed,      // Arrived at destination
    Interrupted,    // Stopped mid-travel (encounter abort, out of fuel)
    Cancelled       // Player cancelled before starting
}

public class TravelEncounterRecord
{
    public int SegmentIndex { get; set; }
    public int SystemId { get; set; }
    public string EncounterType { get; set; }
    public string EncounterId { get; set; }
    public string Outcome { get; set; }  // "completed", "fled", "combat", etc.
}
```

---

### 3.6 TravelContext Structure (for Encounter Domain)

```csharp
/// <summary>
/// Context passed to Encounter/Generation when travel triggers an encounter.
/// </summary>
public class TravelContext
{
    // Location
    public int CurrentSystemId { get; set; }
    public int DestinationSystemId { get; set; }
    public Route CurrentRoute { get; set; }
    
    // System info
    public StarSystem CurrentSystem { get; set; }
    public HashSet<string> SystemTags { get; set; }
    public SystemMetrics SystemMetrics { get; set; }
    
    // Route info
    public HashSet<string> RouteTags { get; set; }
    public int RouteHazard { get; set; }
    
    // Suggested encounter type (from weighting)
    public string SuggestedEncounterType { get; set; }
    
    // Player state summary
    public int CargoValue { get; set; }
    public bool HasIllegalCargo { get; set; }
    public int CrewCount { get; set; }
    public List<string> CrewTraits { get; set; }  // Aggregated traits
    
    // Faction context
    public string SystemOwnerFactionId { get; set; }
    public int PlayerRepWithOwner { get; set; }
}
```

---

## Phase 4: Integration Contracts

### 4.1 Management Integration (Fuel Consumption)

**Contract**: Travel requests fuel consumption via Management.

```csharp
// TravelExecutor calls:
bool success = campaignState.TryConsumeFuel(amount);

// If false, travel is interrupted with "out_of_fuel"
```

**Required Methods** (add to CampaignState if missing):

| Method | Purpose |
|--------|---------|
| `TryConsumeFuel(int amount) -> bool` | Consume fuel, return false if insufficient |
| `GetAvailableFuel() -> int` | Query current fuel |

**Events**:
- `ResourceChangedEvent` (already exists) emitted on fuel change

---

### 4.2 Time Integration (Day Advancement)

**Contract**: Travel advances campaign time per segment.

```csharp
// TravelExecutor calls:
campaignState.Time.AdvanceDays(days);
```

**Events**:
- `DayAdvancedEvent` (already exists) emitted per day

---

### 4.3 Encounter Integration (Trigger and Resume)

**Contract**: Travel triggers encounters via Generation, pauses for Encounter runtime.

**Trigger Flow**:
```
1. TravelExecutor rolls encounter
2. If triggered, create TravelContext
3. Call EncounterGenerator.CreateFromTravel(TravelContext) -> EncounterInstance
4. Pause TravelState
5. Return control to caller (UI/GameState handles encounter)
6. After encounter resolves, caller resumes travel with EncounterResult
7. TravelExecutor continues or aborts based on result
```

**Resume Contract**:
```csharp
TravelResult ResumeTravel(TravelState state, EncounterResult encounterResult);
```

**EncounterResult Effects on Travel**:
| Outcome | Travel Effect |
|---------|---------------|
| `completed` | Continue travel |
| `fled` | Continue travel (maybe time penalty) |
| `combat_victory` | Continue travel |
| `combat_defeat` | Interrupt travel, return to last system |
| `abort` | Interrupt travel |
| `captured` | Special handling (game over or rescue mission) |

---

### 4.4 Event Vocabulary

**New Events for Travel** (add to `Events.cs`):

```csharp
// Travel lifecycle
public record TravelStartedEvent(int FromSystemId, int ToSystemId, int EstimatedDays, int EstimatedFuel);
public record TravelSegmentStartedEvent(int FromSystemId, int ToSystemId, int SegmentIndex);
public record TravelSegmentCompletedEvent(int FromSystemId, int ToSystemId, int FuelConsumed, int DaysElapsed);
public record TravelCompletedEvent(int DestinationSystemId, int TotalDays, int TotalFuel);
public record TravelInterruptedEvent(int CurrentSystemId, string Reason);

// Encounter trigger
public record TravelEncounterTriggeredEvent(int SystemId, string EncounterType, string EncounterId);
public record TravelEncounterResolvedEvent(string EncounterId, string Outcome);
```

---

## Phase 5: Edge Cases & Failure Modes

### 5.1 Out-of-Fuel Behavior

**Decision**: Travel fails gracefully if fuel runs out mid-travel. Recovery via fuel runner service.

**Behavior**:
1. Before each segment, check if fuel is sufficient
2. If insufficient:
   - Do NOT consume any fuel for this segment
   - Interrupt travel at current system
   - Return `TravelResult` with `Status = Interrupted`, `InterruptReason = "out_of_fuel"`
3. Player is stranded at current system
4. Must acquire fuel before continuing

**Stranded State**:
- Player can still access station facilities (if at station)
- Can accept jobs, sell cargo, etc.
- Cannot travel until fuel acquired

**Fuel Recovery Mechanism**:
- **Fuel Runner Service**: Player can pay credits to have fuel delivered
  - Cost: `baseCost + (distanceFromNearestStation * multiplier)`
  - Takes time (1-3 days depending on distance)
- **Distress Signal**: Sending distress signal triggers encounter roll
  - Increased encounter chance while waiting
  - Could attract help, pirates, or patrol
  - Encounter types weighted toward: rescue, pirate, patrol inspection
- **Salvage Option** (future): If near derelict, may scavenge fuel

---

### 5.2 Interrupted Travel Behavior

**Decision**: Travel can be interrupted by encounters or player choice.

**Encounter Interruption**:
- Combat defeat → return to last safe system
- Capture → special game state
- Player abort → stay at current position

**Player Cancellation**:
- Before travel starts: full refund (no fuel consumed)
- During travel: no refund, stay at current position

**Resume Capability**:
- `TravelState` is serializable
- Can save mid-travel
- Can resume after encounter resolution

---

### 5.3 Blocked Route Behavior

**Decision**: Routes can be blocked by events or faction state.

**Blocked Route Sources** (future):
- Active blockade (faction conflict)
- Hazard event (asteroid storm, pirate fleet)
- Player reputation (faction denies passage)

**Behavior**:
1. `TravelPlanner` checks route availability
2. Blocked routes excluded from pathfinding
3. If no valid path exists:
   - `TravelPlan.IsValid = false`
   - `TravelPlan.InvalidReason = "no_route"` or `"blocked_by_faction"`

**For G2**: No dynamic blocking. All routes in test sector are always available.

---

## Resolved Design Decisions

These questions were answered during TV0 finalization:

### Design Decisions

1. **Encounter frequency tuning**: ✅ **Roll per day of travel, not per segment**
   - Longer routes have more encounter opportunities
   - A 4-day journey = 4 encounter rolls
   - Scales naturally with distance

2. **Fuel scarcity**: ✅ **Stranded but recoverable via fuel runner**
   - Player can pay for fuel delivery (costs credits + time)
   - Sending distress signal increases encounter chance
   - Not game-over, but has consequences

3. **Multi-path options**: ✅ **Single shortest path per click, player chains destinations**
   - Clicking a system calculates shortest path to that system
   - Players can click intermediate systems to plot custom routes
   - No need for "show me 3 options" UI complexity

4. **Nested encounters**: ✅ **Disallowed in G2**
   - Encounter completes before travel resumes
   - No encounter-within-encounter complexity

5. **Time granularity**: ✅ **Days only**
   - Minimum travel time: 1 day (even for short routes)
   - No hour tracking needed

### Technical Decisions

6. **Pathfinding algorithm**: ✅ **Weighted A* for TV1**
   - Cost = `distance * distanceWeight + hazard * safetyWeight * 50`
   - Replaces current BFS which only counts hops

7. **State serialization**: ✅ **Add TravelState to CampaignStateData**
   - Serialize alongside other campaign state
   - Enables save/load mid-travel

8. **Encounter generation coupling**: ✅ **Loose coupling via TravelContext**
   - Travel creates `TravelContext`, passes to Generation
   - Generation returns `EncounterInstance`
   - Travel doesn't know encounter internals

---

## Manual Test Setup

Once TV1 is implemented, use this test scenario:

### Test Scenario: Haven to Rockfall

**Setup**:
1. Create campaign with test sector (`WorldState.CreateTestSector()`)
2. Start at Haven Station (system 0)
3. Set fuel to 50

**Test Cases**:

| Test | Action | Expected |
|------|--------|----------|
| **Route calculation** | Plan route Haven → Rockfall | Path: [0, 1, 2], Fuel: 30, Days: 4 |
| **Chained route** | Click Waypoint, then Rockfall | Two plans: Haven→Waypoint, Waypoint→Rockfall |
| **Insufficient fuel** | Set fuel to 10, plan route | `IsValid = false`, `InvalidReason = "insufficient_fuel"` |
| **Execute travel** | Execute plan with fuel=50 | Arrive at Rockfall, fuel=20, days+=4 |
| **Encounter trigger** | Seed RNG for guaranteed encounter | Encounter fires on one of 4 daily rolls |
| **Out of fuel mid-travel** | Set fuel to 20, execute | Interrupt mid-journey |

### Test Scenario: Encounter Integration

**Setup**:
1. Create campaign with test sector
2. Start at Waypoint (system 1)
3. Plan route to Red Claw Base (system 3) via Smuggler's Den (system 6)

**Test Cases**:

| Test | Action | Expected |
|------|--------|----------|
| **High-risk route** | Check encounter chance | ~20-30% per segment (hidden routes) |
| **TravelContext creation** | Trigger encounter | Context has route tags, system metrics |
| **Encounter pause** | Encounter triggers | `TravelState.IsPausedForEncounter = true` |
| **Resume after encounter** | Complete encounter | Travel continues to next segment |

---

## Automated Tests (TV1)

### Unit Tests: TravelPlanner

```csharp
[TestSuite]
public class TV1TravelPlannerTests
{
    [TestCase]
    public void PlanRoute_DirectConnection_ReturnsSingleSegment()
    {
        var world = WorldState.CreateTestSector();
        var planner = new TravelPlanner(world);
        
        var plan = planner.PlanRoute(0, 1);
        
        AssertBool(plan.IsValid).IsTrue();
        AssertInt(plan.Segments.Count).IsEqual(1);
        AssertInt(plan.Segments[0].FromSystemId).IsEqual(0);
        AssertInt(plan.Segments[0].ToSystemId).IsEqual(1);
    }
    
    [TestCase]
    public void PlanRoute_MultiHop_ReturnsCorrectPath()
    {
        var world = WorldState.CreateTestSector();
        var planner = new TravelPlanner(world);
        
        var plan = planner.PlanRoute(0, 2);
        
        AssertBool(plan.IsValid).IsTrue();
        AssertInt(plan.Segments.Count).IsEqual(2);
        // Haven → Waypoint → Rockfall
    }
    
    [TestCase]
    public void PlanRoute_NoPath_ReturnsInvalid()
    {
        var world = WorldState.CreateTestSector();
        var planner = new TravelPlanner(world);
        
        // Contested Zone (4) is isolated in test sector
        var plan = planner.PlanRoute(0, 4);
        
        AssertBool(plan.IsValid).IsFalse();
        AssertString(plan.InvalidReason).IsEqual("no_route");
    }
    
    [TestCase]
    public void PlanRoute_CalculatesFuelCost()
    {
        var world = WorldState.CreateTestSector();
        var planner = new TravelPlanner(world);
        
        var plan = planner.PlanRoute(0, 1);
        
        AssertInt(plan.TotalFuelCost).IsGreater(0);
    }
    
    [TestCase]
    public void PlanRoute_CalculatesTimeCost()
    {
        var world = WorldState.CreateTestSector();
        var planner = new TravelPlanner(world);
        
        var plan = planner.PlanRoute(0, 1);
        
        AssertInt(plan.TotalTimeDays).IsGreater(0);
    }
}
```

### Unit Tests: TravelSegment

```csharp
[TestSuite]
public class TV1TravelSegmentTests
{
    [TestCase]
    public void CalculateEncounterChance_ZeroHazard_ReturnsZero()
    {
        var segment = new TravelSegment
        {
            Route = new Route(0, 5, 100) { HazardLevel = 0 }
        };
        
        float chance = segment.CalculateEncounterChance();
        
        AssertFloat(chance).IsEqual(0f);
    }
    
    [TestCase]
    public void CalculateEncounterChance_MaxHazard_ReturnsFiftyPercent()
    {
        var segment = new TravelSegment
        {
            Route = new Route(0, 1, 100) { HazardLevel = 5 }
        };
        
        float chance = segment.CalculateEncounterChance();
        
        AssertFloat(chance).IsEqual(0.5f);
    }
    
    [TestCase]
    public void CalculateEncounterChance_PatrolledTag_ReducesChance()
    {
        var route = new Route(0, 1, 100) { HazardLevel = 3 };
        route.Tags.Add(WorldTags.Patrolled);
        
        var segment = new TravelSegment { Route = route };
        
        float chance = segment.CalculateEncounterChance();
        
        // Base 30% - 10% patrolled = 20%
        AssertFloat(chance).IsEqual(0.2f);
    }
}
```

### Integration Tests: TravelExecutor (TV2)

```csharp
[TestSuite]
public class TV2TravelExecutorTests
{
    [TestCase]
    public void ExecuteTravel_ConsumeFuel()
    {
        var campaign = CampaignState.CreateNew();
        campaign.Fuel = 100;
        var plan = CreateTestPlan(fuelCost: 30);
        
        var executor = new TravelExecutor(campaign);
        var result = executor.Execute(plan);
        
        AssertInt(campaign.Fuel).IsEqual(70);
        AssertInt(result.FuelConsumed).IsEqual(30);
    }
    
    [TestCase]
    public void ExecuteTravel_AdvanceTime()
    {
        var campaign = CampaignState.CreateNew();
        int startDay = campaign.Time.CurrentDay;
        var plan = CreateTestPlan(timeDays: 3);
        
        var executor = new TravelExecutor(campaign);
        var result = executor.Execute(plan);
        
        AssertInt(campaign.Time.CurrentDay).IsEqual(startDay + 3);
        AssertInt(result.DaysElapsed).IsEqual(3);
    }
    
    [TestCase]
    public void ExecuteTravel_InsufficientFuel_Interrupts()
    {
        var campaign = CampaignState.CreateNew();
        campaign.Fuel = 10;
        var plan = CreateTestPlan(fuelCost: 30);
        
        var executor = new TravelExecutor(campaign);
        var result = executor.Execute(plan);
        
        AssertThat(result.Status).IsEqual(TravelResultStatus.Interrupted);
        AssertString(result.InterruptReason).IsEqual("insufficient_fuel");
    }
}
```

---

## Files Summary

### Files to Create (TV1)

| File | Purpose |
|------|---------|
| `src/sim/travel/TravelSegment.cs` | Single route step with costs |
| `src/sim/travel/TravelPlan.cs` | Complete route with aggregates |
| `src/sim/travel/TravelPlanner.cs` | A* pathfinding and plan creation |
| `src/sim/travel/agents.md` | Directory documentation |
| `tests/sim/travel/TV1TravelPlannerTests.cs` | Planner tests |
| `tests/sim/travel/TV1TravelSegmentTests.cs` | Segment tests |

### Files to Create (TV2)

| File | Purpose |
|------|---------|
| `src/sim/travel/TravelState.cs` | In-progress travel state |
| `src/sim/travel/TravelResult.cs` | Completed travel outcome |
| `src/sim/travel/TravelContext.cs` | Context for encounter generation |
| `src/sim/travel/TravelExecutor.cs` | Main execution logic |
| `tests/sim/travel/TV2TravelExecutorTests.cs` | Executor tests |

### Files to Modify

| File | Changes |
|------|---------|
| `src/sim/Events.cs` | Add travel events |
| `src/sim/campaign/CampaignState.cs` | Add `TryConsumeFuel()` if missing |
| `src/sim/campaign/Ship.cs` | Add `FuelEfficiency`, `Speed` stats |
| `src/sim/agents.md` | Document travel directory |

---

## Success Criteria for TV0

When TV0 is complete:

1. ✅ Fuel consumption formula is documented and agreed
2. ✅ Time cost formula is documented and agreed
3. ✅ Encounter chance formula is documented and agreed
4. ✅ All data structures are designed
5. ✅ Integration contracts are defined
6. ✅ Edge cases are documented
7. ✅ Open questions are listed (answers not required for TV0)
8. ✅ Test scenarios are defined
9. ✅ TV1 implementation path is clear

**Natural Pause Point**: After TV0, the design is locked. TV1 begins implementation.

---

## Appendix: Formula Reference

### Fuel Cost
```
fuelCost = ceil(distance * 0.1 / shipEfficiency)
```

### Time Cost
```
travelDays = ceil(distance / shipSpeed)
```

### Encounter Chance
```
baseChance = hazardLevel * 0.1
tagModifier = sum of tag modifiers
metricModifier = sum of metric modifiers (WD3)
encounterChance = clamp(baseChance + tagModifier + metricModifier, 0, 0.8)
```

### Pathfinding Cost (for A*)
```
segmentCost = distance * distanceWeight + hazard * safetyWeight * 50
```
