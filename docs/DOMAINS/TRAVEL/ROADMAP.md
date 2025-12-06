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
4. **TV3 – Simulation Integration (G3)**

---

## TV0 – Concept Finalization (G2)

**Goal:**  
Finalize design decisions for travel mechanics before implementation.

**Key deliverables:**

- Review DOMAIN.md and CAMPAIGN_FOUNDATIONS.md sections 1, 4, 5.
- Define travel cost model:
  - Fuel consumption formula (distance × ship efficiency).
  - Time cost formula (distance / ship speed).
  - Supplies consumption (optional for G2).
- Define risk model:
  - Base encounter chance per route segment.
  - Modifiers from route tags, system metrics, cargo.
- Define `TravelPlan` structure:
  - Route segments, total cost, risk estimate.
- Define `TravelState` structure:
  - Current position, progress, pending encounters.
- Document route preference options:
  - Fast vs safe, avoid faction, etc.

**Why first:**  
Travel touches World, Management, and Encounter. Clear contracts prevent rework.

**Implementation:** See `TV0_IMPLEMENTATION.md` for detailed breakdown.

**Status:** ✅ Complete

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

**Status:** ⬜ Pending

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

**Status:** ⬜ Pending

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

| Milestone | Phase | Notes |
|-----------|-------|-------|
| TV0 | G2 | Concept finalization |
| TV1 | G2 | Route planning |
| TV2 | G2 | Travel execution |
| TV3 | G3 | Simulation integration |

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

### TV3
- [ ] Risk reflects live simulation state
- [ ] Travel events affect simulation metrics
