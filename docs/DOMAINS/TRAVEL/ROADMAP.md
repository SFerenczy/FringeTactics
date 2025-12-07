# Travel Domain Roadmap

This document defines the **implementation order** for the Travel domain.

- G0/G1 have no Travel implementation (player is docked at single hub).
- Travel comes online in G2.
- Each milestone is a **vertical slice**.

---

## Overview of Milestones

1. **TV0 – Concept Finalization (G2)**
2. **TV1 – Route Planning (G2)**
3. **TV2 – Travel Execution (G2)**
4. **TV-UI – Travel Visibility (G2)**
5. **TV3 – Simulation Integration (G3)**

---

## TV0 – Concept Finalization (G2)

**Goal:**  
Finalize design decisions for travel mechanics before implementation.

**Depends on:** WD2 (Sector Topology) ✅, WD3 (Metrics & Tags) ✅

**Status:** ✅ Complete

**Implementation:** See `TV0_IMPLEMENTATION.md` for detailed breakdown.

**Key deliverables:**

- [x] Review DOMAIN.md and CAMPAIGN_FOUNDATIONS.md sections 1, 4, 5
- [x] Define travel cost model:
  - Fuel: `ceil(distance * 0.1 / efficiency)`
  - Time: `max(1, ceil(distance / speed))`
- [x] Define risk model:
  - Base: `hazard * 0.1` per day
  - Tag modifiers (patrolled -10%, dangerous +10%, etc.)
  - Metric modifiers (security, criminal activity)
- [x] Define data structures:
  - `TravelSegment`, `TravelPlan`, `TravelState`, `TravelContext`
- [x] Document edge cases:
  - Out of fuel, no route, encounter outcomes

**Why first:**  
Travel touches World, Management, and Encounter. Clear contracts prevent rework.

---

## TV1 – Route Planning (G2)

**Goal:**  
Implement route calculation and travel plan creation.

**Depends on:** WD2 (Sector Topology) ✅

**Key capabilities:**

- `TravelPlanner` class:
  - Input: origin system, destination system, preferences.
  - Output: `TravelPlan` with segments, costs, risk.
- Pathfinding over world graph:
  - A* or Dijkstra with configurable cost function.
  - Cost function weights: distance, time, fuel, risk.
- `TravelSegment` class:
  - From/to system IDs.
  - Distance, base time, base fuel cost.
  - Route tags and hazard modifiers.
- `TravelPlan` class:
  - Ordered list of segments.
  - Computed totals: time, fuel, risk score.
  - Validity check (enough fuel, route exists).
- Route preference support:
  - `TravelPreferences`: prioritize speed, safety, or fuel efficiency.
  - Avoid specific factions or system tags.

**Deliverables:**
- `TravelPlanner`, `TravelPlan`, `TravelSegment` classes.
- Pathfinding implementation.
- Unit tests for route calculation.
- Test sector with 5-10 systems for validation.

**Files to create:**
| File | Purpose |
|------|---------|
| `src/sim/travel/TravelSegment.cs` | Single route step |
| `src/sim/travel/TravelPlan.cs` | Complete route with costs |
| `src/sim/travel/TravelPreferences.cs` | Route preference options |
| `src/sim/travel/TravelPlanner.cs` | Pathfinding and plan creation |
| `tests/sim/travel/TV1*.cs` | Test files |

**Status:** ✅ Complete

**Implementation:** See `TV1_IMPLEMENTATION.md` for detailed breakdown.

---

## TV2 – Travel Execution (G2)

**Goal:**  
Execute travel plans: consume resources, advance time, trigger encounters.

**Depends on:** TV1 ✅, EN1 (Encounter Runtime) 🔄, MG4 (Encounter Integration) 🔄

**Key capabilities:**

- `TravelExecutor` class:
  - Input: `TravelPlan`, `CampaignState`.
  - Output: `TravelResult` with events that occurred.
- Resource consumption:
  - Deduct fuel per segment via Management.
  - Fail gracefully if fuel runs out mid-travel.
- Time advancement:
  - Advance campaign time per segment.
  - Use `CampaignTime` from Systems Foundation.
- Encounter triggering:
  - Roll for encounters per segment based on risk.
  - Pass `TravelContext` to Generation for encounter instantiation.
  - Pause travel when encounter fires.
- `TravelState` class:
  - Current segment index, progress within segment.
  - Pending encounter (if any).
  - Resume capability after encounter resolution.
- `TravelResult` class:
  - Success/failure/interrupted status.
  - List of events (encounters, fuel consumed, time passed).
  - Final position.
- `TravelContext` class:
  - Current route, segment, system tags.
  - Player cargo, crew state summary.
  - Used by Generation to select encounters.

**Deliverables:**
- `TravelExecutor`, `TravelState`, `TravelResult`, `TravelContext` classes.
- Integration with Management (fuel consumption).
- Integration with CampaignTime (time advancement).
- Encounter trigger hooks (calls Generation, pauses for Encounter).
- Unit tests for execution flow.
- Integration tests with mock encounters.

**Files to create:**
| File | Purpose |
|------|---------|
| `src/sim/travel/TravelState.cs` | In-progress travel state |
| `src/sim/travel/TravelResult.cs` | Completed travel outcome |
| `src/sim/travel/TravelContext.cs` | Context for encounter generation |
| `src/sim/travel/TravelExecutor.cs` | Main execution logic |
| `tests/sim/travel/TV2*.cs` | Test files |

**Events to add:**
```csharp
public record TravelStartedEvent(int FromSystemId, int ToSystemId, int EstimatedDays);
public record TravelSegmentCompletedEvent(int FromSystemId, int ToSystemId, int FuelConsumed);
public record TravelEncounterTriggeredEvent(int SystemId, string EncounterType);
public record TravelCompletedEvent(int DestinationSystemId, int TotalDays, int TotalFuel);
public record TravelInterruptedEvent(int CurrentSystemId, string Reason);
```

**Status:** ✅ Complete

**Implementation:** See `TV2_IMPLEMENTATION.md` for detailed breakdown.

---

## TV-UI – Travel Visibility (G2)

**Goal:**  
Expose travel and world systems to the player through UI enhancements.

**Depends on:** TV2 ✅, WD3 ✅, GN2 ✅

**Status:** 🔄 In Progress

**Implementation:** See `TV-UI_IMPLEMENTATION.md` for detailed breakdown.

**Key capabilities:**

- **Travel animation** ✅:
  - Visual dot moving along route during travel
  - Animation stops at random point (0.2-0.8) if encounter triggers
  - Brief but visible feedback (~0.8s duration)
- **System info panel** (SectorView) ✅:
  - Display system metrics (security, crime, stability, economy, law)
  - Display system tags (Hub, Frontier, Lawless, etc.)
  - Display owning faction with reputation
  - Display station facilities
- **Route info** (when selecting destination) ✅:
  - Show route hazard level
  - Show estimated encounter chance
  - Show route tags (Dangerous, Patrolled, etc.)
- **Campaign time display** ✅:
  - Show current campaign day in sector view header
  - Show day advancement during travel
- **Travel feedback** ✅:
  - Travel log shows recent journeys
  - Show fuel consumption per segment
- **Ship status** ✅:
  - Show hull integrity in resources panel

**Deliverables:**
- ✅ `TravelAnimator.cs` - Visual travel animation component
- ✅ Enhanced `SectorView.cs` with system info panel
- ✅ Campaign day display in header
- ✅ Travel log/feedback panel
- ✅ Ship hull display
- No new sim code required (display only)

**Files:**
| File | Purpose |
|------|---------|  
| `src/scenes/sector/SectorView.cs` | System info, campaign day, travel feedback |
| `src/scenes/sector/TravelAnimator.cs` | Animated travel dot |

---

## TV3 – Simulation Integration (G3)

**Goal:**  
Travel risk responds to live simulation metrics.

**Key capabilities:**

- Risk calculation uses Simulation metrics:
  - `security_level` reduces encounter chance.
  - `criminal_activity` increases pirate encounters.
  - `patrol_intensity` affects patrol encounter types.
- Travel events feed back to Simulation:
  - Piracy by player → increases `criminal_activity`.
  - Successful patrol encounters → decreases `criminal_activity`.
- Dynamic route hazards:
  - Routes can become more/less dangerous based on sim state.
  - Blockades, active conflicts affect route availability.

**Deliverables:**
- Simulation-aware risk calculation.
- Travel event emission for Simulation consumption.
- Integration tests with Simulation.

**Status:** ⬜ Pending (G3)

---

## G2 Scope Summary

| Milestone | Phase | Status | Notes |
|-----------|-------|--------|-------|
| TV0 | G2 | ✅ Complete | Concept finalization |
| TV1 | G2 | ✅ Complete | Route planning |
| TV2 | G2 | ✅ Complete | Travel execution |
| TV-UI | G2 | 🔄 In Progress | Travel visibility (animation done) |
| TV3 | G3 | ⬜ Pending | Simulation integration |

---

## Implementation Notes

### Pathfinding

- Use A* over the world graph.
- Cost function: `baseCost + (distance * distanceWeight) + (risk * riskWeight)`.
- Preferences modify weights (safe route = high risk weight).

### Fuel Model (G2)

Simple linear model:
```
fuelCost = distance * shipFuelEfficiency
```

Where `shipFuelEfficiency` is a ship stat (default 1.0).

### Time Model (G2)

Simple linear model:
```
travelDays = ceil(distance / shipSpeed)
```

Where `shipSpeed` is a ship stat (default 10 units/day).

### Encounter Triggering

Per segment:
1. Calculate base encounter chance from route tags.
2. Apply modifiers from system metrics (security, criminal activity).
3. Apply modifiers from player state (cargo value, reputation).
4. Roll against final chance.
5. If triggered, create `TravelContext` and call `EncounterGenerator`.

### Integration Points

- **World**: Query systems, routes, distances, tags.
- **Management**: Consume fuel, check fuel availability.
- **CampaignTime**: Advance time during travel.
- **Generation**: Request encounter instantiation.
- **Encounter**: Pause travel, run encounter, resume.

---

## Dependencies

| Milestone | Depends On |
|-----------|------------|
| TV1 | WD2 (Sector Topology) |
| TV2 | TV1, EN1 (partial), MG4 (partial) |
| TV-UI | TV2, WD3, GN2 |
| TV3 | TV2, Simulation domain |

---

## Success Criteria

### TV1
- [ ] Routes calculated between any two connected systems
- [ ] Multiple route options with different cost/risk tradeoffs
- [ ] Preferences affect route selection
- [ ] Deterministic given same inputs

### TV2
- [ ] Fuel consumed during travel
- [ ] Time advances during travel
- [ ] Encounters trigger based on risk
- [ ] Travel can be interrupted and resumed
- [ ] Out-of-fuel handled gracefully

### TV-UI
- [ ] System metrics displayed when selecting node
- [ ] System tags displayed (Hub, Frontier, Lawless, etc.)
- [ ] Route hazard shown when planning travel
- [ ] Campaign day shown in sector view
- [ ] Ship hull shown in resources panel
- [ ] Travel feedback shows fuel/time consumed

### TV3
- [ ] Risk reflects live simulation state
- [ ] Travel events affect simulation metrics
