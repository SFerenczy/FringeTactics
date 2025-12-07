# World Domain Roadmap

This document defines the **implementation order** for the World domain.

- G0 is concept/design only. Implementation starts in G1.
- Each milestone is a **vertical slice**.

---

## Overview of Milestones

1. **WD0 – Concept Finalization (G0)**
2. **WD1 – Single Hub World (G1)**
3. **WD2 – Sector Topology (G2)**
4. **WD3 – Metrics & Tags (G2)**
5. **WD4 – Simulation Integration (G3)**

---

## WD0 – Concept Finalization (G0)

**Goal:**  
Finalize design decisions for world structure, metrics, and factions.

**Key deliverables:**

- Review and finalize CAMPAIGN_FOUNDATIONS.md section 4 (World Metrics).
- Define initial metric set:
  - System-level: stability, security_level, criminal_activity, economic_activity.
  - Faction-level: military_strength, economic_power, influence.
- Define initial tag vocabulary:
  - frontier, core, industrial, lawless, hub, border.
- Define station facility types:
  - shop, bar, mission_board, repair_yard, recruitment.
- Document `WorldState` structure (design only).

**Why first:**  
World is the shared data layer. Getting the schema right avoids cascading changes.

**Status:** ⚠️ Partially complete (CAMPAIGN_FOUNDATIONS covers metrics, needs facility/tag details)

---

## WD1 – Single Hub World (G1)

**Goal:**  
Implement minimal world with one system and one station for the G1 jobbing loop.

**Key capabilities:**

- `WorldState` class:
  - Single `System` with ID, name, position.
  - Single `Station` with facilities.
  - Owning faction (or neutral).
- `Station` class:
  - List of `Facility` (shop, mission_board, repair_yard).
  - Attached metrics (can be static for G1).
- Query APIs:
  - `GetStation(stationId)`.
  - `GetFacilities(stationId)`.
  - `GetSystemMetrics(systemId)`.

**Deliverables:**
- `WorldState`, `System`, `Station`, `Facility` classes.
- Test world with one hub.
- Unit tests for queries.

**Implementation:** See `WD1_IMPLEMENTATION.md` for detailed breakdown.

---

## WD2 – Sector Topology (G2)

**Goal:**  
Expand to a real sector graph with multiple systems and explicit routes.

**Depends on:** WD1 ✅

**Status:** ✅ Complete

**Implementation:** See `WD2_IMPLEMENTATION.md` for detailed breakdown.

**Key capabilities:**

- Multiple `StarSystem` instances with varied types (8 in test sector)
- `Route` class for explicit connections:
  - Bidirectional (A↔B is one route)
  - Distance (computed from positions)
  - Hazard level (0-5 scale)
  - Route tags (dangerous, patrolled, hidden, etc.)
- Route queries:
  - `GetRoute(fromId, toId) -> Route`
  - `GetRoutesFrom(systemId) -> IEnumerable<Route>`
  - `HasRoute(fromId, toId) -> bool`
  - `GetRouteHazard(fromId, toId) -> int`
- Pathfinding:
  - `FindPath(fromId, toId) -> List<int>`
  - `GetPathDistance(path) -> float`
  - `GetPathHazard(path) -> int`
- Station factory methods for different types
- Test sector with 8 systems, 7 routes, 6 stations

**Deliverables:**
- `Route` class with serialization
- Route management in `WorldState`
- `WorldState.CreateTestSector()` factory
- Station factory methods (CreateOutpost, CreateMining, etc.)
- Pathfinding and graph queries
- Route tags in `WorldTags`
- Unit tests for topology and routes

**Files to create/modify:**
| File | Changes |
|------|---------|
| `src/sim/world/Route.cs` | New route class |
| `src/sim/world/WorldState.cs` | Add routes, queries, CreateTestSector() |
| `src/sim/world/WorldTags.cs` | Add route tags |
| `src/sim/world/Station.cs` | Add factory methods |
| `tests/sim/world/WD2*.cs` | Test files |

---

## WD3 – Metrics & Tags (G2)

**Goal:**  
Enhance the metrics and tag system to support Travel, Encounter, and Generation domains.

**Depends on:** WD2 ✅

**Status:** ✅ Complete

**Implementation:** See `WD3_IMPLEMENTATION.md` for detailed breakdown.

**Key capabilities:**

- **Typed metric access** via `SystemMetricType` enum
- **Metric mutation API** with clamping and logging
- **Expanded tag vocabulary** (13 system, 8 station, 8 route tags)
- **Tag category validation** for consistency
- **Composite queries** for Travel and Encounter domains
- **Encounter context classes** for template selection

### Phase 1: Metric Type System

- Create `SystemMetricType` enum for type-safe access
- Add `Get()`, `Set()`, `Modify()`, `ClampAll()`, `Clone()` to `SystemMetrics`

### Phase 2: WorldState Metric APIs

- Add `GetSystemMetric(systemId, metric)` query
- Add `GetSystemsByMetric(metric, min, max)` filter
- Add `SetSystemMetric()` and `ModifySystemMetric()` mutations
- Add convenience methods: `GetHighSecuritySystems()`, `GetLawlessSystems()`, etc.

### Phase 3: Tag System Enhancement

- Expand `WorldTags` with organized categories
- Add `SystemTags`, `StationTags`, `RouteTags` sets for validation
- Add `GetSystemsWithAllTags()`, `GetSystemsWithAnyTag()` queries
- Add tag mutation methods: `AddSystemTag()`, `RemoveSystemTag()`, etc.

### Phase 4: Composite Queries for Travel/Encounter

- Add `GetEffectiveRouteDanger()` combining route hazard and endpoint metrics
- Add `GetSmugglingRoutes()`, `GetHideoutSystems()` for gameplay queries
- Create `RouteEncounterContext` and `SystemEncounterContext` classes
- Add `GetRouteEncounterContext()`, `GetSystemEncounterContext()` methods

### Phase 5: Test Sector Enhancement

- Update `CreateTestSector()` with richer, varied metrics
- Ensure each system has distinct, thematic metric values

**Deliverables:**
- `SystemMetricType` enum
- `EncounterContext.cs` with context classes
- Enhanced `SystemMetrics` with typed accessors
- Expanded `WorldTags` vocabulary with validation
- Metric and tag query/mutation APIs
- Unit tests (3 test files, ~30 tests)

**Files to create:**
| File | Purpose |
|------|---------|
| `src/sim/world/SystemMetricType.cs` | Enum for typed metric access |
| `src/sim/world/EncounterContext.cs` | Context classes for Encounter domain |
| `tests/sim/world/WD3MetricTests.cs` | Metric system tests |
| `tests/sim/world/WD3TagTests.cs` | Tag system tests |
| `tests/sim/world/WD3QueryTests.cs` | Query API tests |

**Files to modify:**
| File | Changes |
|------|---------|
| `src/sim/world/SystemMetrics.cs` | Add typed accessors |
| `src/sim/world/WorldTags.cs` | Expand vocabulary |
| `src/sim/world/WorldState.cs` | Add queries and mutations |
| `src/sim/world/agents.md` | Document new files |

---

## WD4 – Simulation Integration (G3)

**Goal:**  
World becomes the shared storage that Simulation reads and writes.

**Key capabilities:**

- Simulation can update metrics via events or direct API.
- Faction territory changes:
  - `SetSystemOwner(systemId, factionId)`.
  - `SetStationOwner(stationId, factionId)`.
- Event emission for world changes:
  - `StationDestroyedEvent`.
  - `TerritoryChangedEvent`.

**Deliverables:**
- Mutation APIs for Simulation.
- Event emission on changes.
- Integration tests with Simulation.

---

## G0/G1/G2 Scope Summary

| Milestone | Phase | Notes |
|-----------|-------|-------|
| WD0 | G0 | Concept only |
| WD1 | G1 | Single hub implementation |
| WD2 | G2 | Sector graph |
| WD3 | G2 | Metrics and tags |
| WD4 | G3 | Simulation integration |

---

## Backlog (G2.5 – Playtest & Polish)

### WD-TERR1 – Static faction territory display (G2.5)

**Goal:** Make faction ownership visible without simulation.

**Status:** ⬜ Pending

- Each system has an owning faction from existing world state.
- Sector view:
  - Color-code systems/nodes by faction.
  - Tooltip: faction name + a few key metrics (security, piracy, wealth) from G2.

---

### WD-FAC1 – Station facilities surfaced (G2.5)

**Goal:** Connect world data to campaign/station UI.

**Status:** ⬜ Pending

- Ensure stations expose their facilities:
  - Shop, bar, recruitment, repair, etc.
- Provide simple queries like:
  - `HasRecruitment(stationId)`
  - `GetShopInventoryId(stationId)`
- Used by:
  - Campaign/station UI to show available actions.
  - Generation to bias encounters (e.g. bars, shady contacts).

---

### Future Backlog Items

| Item | Priority | Notes |
|------|----------|-------|
| Faction reputation display | Medium | Show player standing with each faction |
| System info panel | Medium | Detailed view of system metrics/tags |
| Route hazard indicators | Low | Visual feedback on dangerous routes |
