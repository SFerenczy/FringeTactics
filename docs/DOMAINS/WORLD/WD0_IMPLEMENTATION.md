# WD0 – Concept Finalization: Implementation Plan

This document breaks down **Milestone WD0** from `ROADMAP.md` into concrete design decisions and documentation deliverables.

**Goal**: Finalize design decisions for world structure, metrics, tags, facilities, and factions before implementation begins in WD1.

**Phase**: G0 (Concept/Design only – no code implementation)

---

## Current State Assessment

### What We Have (Existing Code)

| Component | Status | Notes |
|-----------|--------|-------|
| `Sector` | ✅ Exists | Graph of nodes with connections |
| `SectorNode` | ✅ Exists | Basic node with type, faction, position |
| `NodeType` enum | ✅ Exists | Station, Outpost, Derelict, Asteroid, Nebula, Contested |
| `CampaignState.FactionRep` | ✅ Exists | Simple faction reputation (0-100) |
| `Sector.Factions` | ✅ Exists | Dictionary of factionId → name |

### What WD0 Requires vs What We Have

| WD0 Requirement | Current Status | Gap |
|-----------------|----------------|-----|
| World metrics (system-level) | ❌ Missing | No stability, security, economic activity |
| World metrics (faction-level) | ❌ Missing | No military_strength, influence, etc. |
| Tag vocabulary | ❌ Missing | NodeType exists but no semantic tags |
| Station facilities | ❌ Missing | No shop, bar, mission_board concept |
| `WorldState` design | ❌ Missing | Currently using `Sector` directly |
| Faction model | ⚠️ Partial | Only name + reputation, no properties |

---

## Conceptual Decisions

### 1. World vs Sector Terminology

**Decision**: Introduce `WorldState` as the top-level container that owns the `Sector`.

**Rationale**:
- `Sector` is the graph topology (nodes + connections)
- `WorldState` is the full world state including metrics, factions, and global data
- This separation allows future multi-sector support
- Aligns with DOMAIN.md: "World domain owns the canonical representation of the galaxy"

**Structure**:
```
WorldState
├── Sector (topology)
│   ├── Systems/Nodes
│   └── Routes/Connections
├── Factions (full faction data)
├── GlobalMetrics (sector-wide state)
└── Time reference (for simulation)
```

### 2. System vs Node Terminology

**Decision**: Rename `SectorNode` to `StarSystem` for clarity, keep `Node` as internal graph term.

**Rationale**:
- "System" is the player-facing term (per DOMAIN.md: "Systems, routes, regions")
- "Node" is implementation detail for graph algorithms
- Stations are locations *within* systems, not separate nodes

**Migration Path** (for WD1):
- `SectorNode` → `StarSystem`
- `NodeType` → `SystemType` (or keep as-is if only used internally)
- Add `Station` as a child entity of `StarSystem`

---

## Deliverable 1: System-Level Metrics

Per CAMPAIGN_FOUNDATIONS.md 4.1 and DOMAIN.md, systems have attached metrics.

### 1.1 Metric Definitions

| Metric | Type | Range | Description |
|--------|------|-------|-------------|
| `stability` | int | 0-5 | Political/social stability. 0 = chaos, 5 = rock solid |
| `security_level` | int | 0-5 | Law enforcement presence. 0 = lawless, 5 = heavily patrolled |
| `criminal_activity` | int | 0-5 | Piracy, smuggling, black market. 0 = clean, 5 = rampant |
| `economic_activity` | int | 0-5 | Trade volume, wealth. 0 = dead, 5 = booming |
| `law_enforcement_presence` | int | 0-5 | Patrol frequency. 0 = none, 5 = constant |

### 1.2 Metric Interactions (Design Notes)

These are **design guidelines** for future Simulation domain, not WD0 scope:

- High `security_level` tends to suppress `criminal_activity`
- High `criminal_activity` can reduce `stability` over time
- High `economic_activity` attracts both trade and pirates
- `law_enforcement_presence` is the "input" that drives `security_level`

### 1.3 Initial Values by System Type

| SystemType | stability | security | criminal | economic | law_enforcement |
|------------|-----------|----------|----------|----------|-----------------|
| Station | 4 | 4 | 1 | 4 | 4 |
| Outpost | 3 | 2 | 2 | 2 | 2 |
| Derelict | 1 | 0 | 3 | 0 | 0 |
| Asteroid | 2 | 1 | 2 | 3 | 1 |
| Nebula | 2 | 0 | 3 | 1 | 0 |
| Contested | 1 | 1 | 4 | 2 | 1 |

### 1.4 Metric Display Tiers

For UI, map metrics to human-readable labels:

| Value | Label | Color Hint |
|-------|-------|------------|
| 0 | None / Absent | Red |
| 1 | Minimal | Orange |
| 2 | Low | Yellow |
| 3 | Moderate | White |
| 4 | High | Light Green |
| 5 | Maximum | Green |

---

## Deliverable 2: Faction-Level Metrics

Per CAMPAIGN_FOUNDATIONS.md 4.1, factions have their own metrics.

### 2.1 Faction Metric Definitions

| Metric | Type | Range | Description |
|--------|------|-------|-------------|
| `military_strength` | int | 0-5 | Combat capability, fleet size |
| `economic_power` | int | 0-5 | Wealth, trade influence |
| `influence` | int | 0-5 | Political reach, soft power |
| `desperation` | int | 0-5 | How cornered they are (affects aggression) |
| `corruption` | int | 0-5 | Internal rot, bribability |

### 2.2 Faction Properties (Static)

Beyond metrics, factions have static properties:

| Property | Type | Description |
|----------|------|-------------|
| `id` | string | Unique identifier (e.g., "corp", "rebels") |
| `name` | string | Display name (e.g., "Helix Corp") |
| `type` | FactionType | Corporate, Government, Criminal, Independent, etc. |
| `hostility_default` | int | Base hostility to player (0-100, 50 = neutral) |
| `color` | Color | UI color for territory display |

### 2.3 Initial Faction Set

| ID | Name | Type | military | economic | influence | desperation | corruption |
|----|------|------|----------|----------|-----------|-------------|------------|
| `corp` | Helix Corp | Corporate | 3 | 5 | 4 | 1 | 3 |
| `rebels` | Free Colonies | Independent | 2 | 2 | 2 | 3 | 1 |
| `pirates` | Red Claw | Criminal | 3 | 2 | 1 | 2 | 4 |

---

## Deliverable 3: Tag Vocabulary

Tags are reusable classification bits for systems and stations.

### 3.1 System Tags

| Tag | Description | Gameplay Effect |
|-----|-------------|-----------------|
| `frontier` | Edge of settled space | Higher encounter variety, lower law |
| `core` | Central, well-established | Higher security, better prices |
| `industrial` | Manufacturing focus | Parts cheaper, tech jobs available |
| `lawless` | No effective law enforcement | Smuggling safe, pirates common |
| `hub` | Major trade crossroads | More jobs, higher traffic |
| `border` | Faction boundary | Contested, political tension |
| `mining` | Resource extraction | Asteroid-related jobs |
| `military` | Faction military presence | Restricted, high security |

### 3.2 Station Tags

| Tag | Description | Gameplay Effect |
|-----|-------------|-----------------|
| `trade_hub` | Major commerce center | Better shop inventory |
| `black_market` | Illegal goods available | Contraband, higher prices |
| `repair_yard` | Ship maintenance focus | Cheaper repairs |
| `recruitment` | Crew hiring available | More crew options |
| `medical` | Medical facilities | Better healing, med supplies |
| `entertainment` | Bars, gambling | Crew morale, rumors |

### 3.3 Tag Implementation Notes

- Tags are stored as `HashSet<string>` or `List<string>`
- Tags are **additive** (a system can have multiple)
- Tags should be **queryable**: `HasTag(systemId, "frontier")`
- Tags can be **dynamic** (added/removed by Simulation) but most are static

---

## Deliverable 4: Station Facilities

Stations have facilities that provide services.

### 4.1 Facility Types

| Facility | Description | Services |
|----------|-------------|----------|
| `shop` | General store | Buy/sell equipment, supplies |
| `bar` | Social hub | Rumors, crew morale, recruitment hints |
| `mission_board` | Job listings | Accept contracts |
| `repair_yard` | Ship maintenance | Repair hull, fix systems |
| `recruitment` | Crew hiring | Hire new crew members |
| `medical` | Medical bay | Heal injuries, remove conditions |
| `black_market` | Illegal trade | Contraband, stolen goods |
| `fuel_depot` | Refueling | Buy fuel (if fuel mechanic added) |

### 4.2 Facility Availability by Station Type

| Station Type | Default Facilities |
|--------------|-------------------|
| Major Station | shop, bar, mission_board, repair_yard, recruitment, medical |
| Minor Station | shop, mission_board, repair_yard |
| Outpost | shop, mission_board |
| Pirate Den | bar, black_market, recruitment |

### 4.3 Facility Properties

| Property | Type | Description |
|----------|------|-------------|
| `type` | FacilityType | Enum of facility types |
| `level` | int (1-3) | Quality/inventory tier |
| `tags` | List<string> | Special modifiers (e.g., "faction_exclusive") |
| `available` | bool | Can be temporarily closed |

---

## Deliverable 5: WorldState Structure

Design the `WorldState` class that will be implemented in WD1.

### 5.1 Class Diagram (Conceptual)

```
WorldState
├── string Name
├── Sector Topology
│   ├── List<StarSystem> Systems
│   └── List<Route> Routes (implicit in connections for now)
├── Dictionary<string, Faction> Factions
├── GlobalMetrics GlobalState (optional, for sector-wide data)
└── Methods:
    ├── GetSystem(id) → StarSystem
    ├── GetStation(id) → Station
    ├── GetFaction(id) → Faction
    ├── GetSystemMetrics(id) → SystemMetrics
    ├── GetFacilities(stationId) → List<Facility>
    ├── HasTag(systemId, tag) → bool
    └── GetSystemsByTag(tag) → List<StarSystem>

StarSystem (renamed from SectorNode)
├── int Id
├── string Name
├── SystemType Type
├── Vector2 Position
├── List<int> Connections
├── string OwningFactionId
├── SystemMetrics Metrics
├── HashSet<string> Tags
└── List<Station> Stations

Station
├── int Id
├── string Name
├── int SystemId (parent)
├── string OwningFactionId
├── List<Facility> Facilities
├── HashSet<string> Tags
└── StationMetrics Metrics (optional, can inherit from system)

Faction
├── string Id
├── string Name
├── FactionType Type
├── Color Color
├── FactionMetrics Metrics
├── int PlayerReputation (0-100)
└── List<string> ControlledSystemIds

SystemMetrics
├── int Stability
├── int SecurityLevel
├── int CriminalActivity
├── int EconomicActivity
└── int LawEnforcementPresence

FactionMetrics
├── int MilitaryStrength
├── int EconomicPower
├── int Influence
├── int Desperation
└── int Corruption

Facility
├── FacilityType Type
├── int Level
├── HashSet<string> Tags
└── bool Available
```

### 5.2 Query API Design

```csharp
// System queries
StarSystem GetSystem(int systemId);
IEnumerable<StarSystem> GetAllSystems();
IEnumerable<StarSystem> GetSystemsByFaction(string factionId);
IEnumerable<StarSystem> GetSystemsByTag(string tag);
IEnumerable<int> GetNeighbors(int systemId);

// Station queries
Station GetStation(int stationId);
IEnumerable<Station> GetStationsInSystem(int systemId);
IEnumerable<Facility> GetFacilities(int stationId);
bool HasFacility(int stationId, FacilityType type);

// Metric queries
SystemMetrics GetSystemMetrics(int systemId);
int GetSecurityLevel(int systemId);
int GetCriminalActivity(int systemId);
bool HasTag(int systemId, string tag);

// Faction queries
Faction GetFaction(string factionId);
IEnumerable<Faction> GetAllFactions();
int GetPlayerReputation(string factionId);
```

### 5.3 Mutation API Design (for Simulation, WD4)

```csharp
// Metric updates
void SetSystemMetric(int systemId, string metric, int value);
void ModifySystemMetric(int systemId, string metric, int delta);

// Ownership changes
void SetSystemOwner(int systemId, string factionId);
void SetStationOwner(int stationId, string factionId);

// Tag changes
void AddTag(int systemId, string tag);
void RemoveTag(int systemId, string tag);

// Facility changes
void AddFacility(int stationId, Facility facility);
void RemoveFacility(int stationId, FacilityType type);
void SetFacilityAvailable(int stationId, FacilityType type, bool available);
```

---

## Deliverable 6: Migration Plan from Current Code

### 6.1 What Changes

| Current | WD1 Target | Notes |
|---------|------------|-------|
| `Sector` | `WorldState.Topology` | Sector becomes internal |
| `SectorNode` | `StarSystem` | Rename + add metrics/tags |
| `NodeType` | `SystemType` | Rename for clarity |
| `Sector.Factions` (dict) | `WorldState.Factions` (full objects) | Upgrade to Faction class |
| `CampaignState.FactionRep` | `Faction.PlayerReputation` | Move into Faction |
| (none) | `Station` | New class |
| (none) | `Facility` | New class |
| (none) | `SystemMetrics` | New class |
| (none) | `FactionMetrics` | New class |

### 6.2 Backward Compatibility

For WD1 (G1), we need minimal disruption:
- Keep `Sector` working internally
- Add `WorldState` as a wrapper
- Gradually migrate consumers to use `WorldState` queries

### 6.3 Files to Create (WD1)

| File | Purpose |
|------|---------|
| `src/sim/world/WorldState.cs` | Main world container |
| `src/sim/world/StarSystem.cs` | System (node) with metrics |
| `src/sim/world/Station.cs` | Station with facilities |
| `src/sim/world/Facility.cs` | Facility type and properties |
| `src/sim/world/Faction.cs` | Full faction data |
| `src/sim/world/SystemMetrics.cs` | System-level metrics |
| `src/sim/world/FactionMetrics.cs` | Faction-level metrics |
| `src/sim/world/WorldTags.cs` | Tag constants/vocabulary |

---

## Acceptance Criteria for WD0

### Documentation Complete

- [x] System-level metrics defined (5 metrics with ranges)
- [x] Faction-level metrics defined (5 metrics with ranges)
- [x] Tag vocabulary defined (8 system tags, 6 station tags)
- [x] Facility types defined (8 types with services)
- [x] `WorldState` structure documented
- [x] Query API designed
- [x] Mutation API designed (for future)
- [x] Migration plan from current code

### Design Decisions Recorded

- [x] World vs Sector terminology clarified
- [x] System vs Node terminology clarified
- [x] Metric ranges standardized (0-5 tiers)
- [x] Initial values by system type defined
- [x] Initial faction set defined

### Ready for WD1

- [x] Clear list of files to create
- [x] Clear class structure
- [x] No ambiguity in data model

---

## Open Questions for Future Milestones

### For WD2 (Sector Topology)

- How do we represent multi-hop routes vs direct connections?
- Do routes have their own metrics (hazard, distance)?
- How do we handle pathfinding across the sector?

### For WD3 (Metrics & Tags)

- How do metrics decay/change over time?
- What events trigger metric changes?
- How do tags affect encounter generation?

### For WD4 (Simulation Integration)

- What's the tick rate for simulation updates?
- How do faction AI decisions affect metrics?
- How do we handle cascading effects (e.g., faction collapse)?

---

## Appendix: Alignment with CAMPAIGN_FOUNDATIONS.md

| CAMPAIGN_FOUNDATIONS Section | WD0 Coverage |
|------------------------------|--------------|
| §4.1 World Metrics | ✅ System metrics, faction metrics |
| §4.2 Implementation Notes | ✅ Coarse 0-5 tiers, UI labels |
| §2 Contracts | ⚠️ Facilities include mission_board |
| §3 Crew | ⚠️ Facilities include recruitment |
| §1 Resources | ⚠️ Facilities include shop, repair_yard |

---

## Appendix: Alignment with DOMAIN.md

| DOMAIN.md Responsibility | WD0 Coverage |
|--------------------------|--------------|
| Galaxy topology | ✅ StarSystem, connections |
| Persistent locations | ✅ Station, Facility |
| Territorial control | ✅ OwningFactionId |
| World-attached metrics | ✅ SystemMetrics, tags |
| Query APIs | ✅ Designed |
| Stable identifiers | ✅ Int IDs for systems/stations |
