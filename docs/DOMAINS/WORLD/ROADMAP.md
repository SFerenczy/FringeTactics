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
Expand to a real sector graph with multiple systems and routes.

**Key capabilities:**

- Multiple `System` instances.
- `Route` class:
  - Connects two systems.
  - Distance, hazard modifiers, tags.
- Graph queries:
  - `GetNeighbors(systemId)`.
  - `GetRoute(fromId, toId)`.
  - `GetAllSystems()`.
- Multiple stations per system.

**Deliverables:**
- Graph representation (adjacency list).
- Route data structure.
- Test sector with 5-10 systems.
- Pathfinding support (or data for Travel to pathfind).

---

## WD3 – Metrics & Tags (G2)

**Goal:**  
Attach live metrics and tags to systems and stations.

**Key capabilities:**

- System metrics:
  - `stability`, `security_level`, `criminal_activity`, `economic_activity`.
  - Stored as int (0-5 tiers) per CAMPAIGN_FOUNDATIONS.
- System/station tags:
  - `frontier`, `core`, `industrial`, `lawless`, etc.
- Metric queries:
  - `GetSecurityLevel(systemId)`.
  - `HasTag(systemId, tag)`.
- Metric updates (from Simulation):
  - `SetMetric(systemId, metric, value)`.

**Deliverables:**
- Metric storage and queries.
- Tag storage and queries.
- Integration points for Simulation writes.

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
