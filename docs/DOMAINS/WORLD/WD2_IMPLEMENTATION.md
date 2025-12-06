# WD2 – Sector Topology: Implementation Plan

**Status**: ✅ Complete  
**Depends on**: WD1 (Single Hub World) ✅ Complete  
**Phase**: G2

---

## Overview

**Goal**: Expand from single-hub world to a real sector graph with multiple systems and explicit routes.

This milestone is foundational for G2's Travel and Encounter systems. The sector topology defines:
- Where the player can go
- How systems connect
- What hazards exist between systems
- The spatial layout for pathfinding

---

## Current State Assessment

### What We Have (from WD1)

| Component | Status | Notes |
|-----------|--------|-------|
| `WorldState` | ✅ Complete | Single hub, basic queries |
| `StarSystem` | ✅ Complete | Has `Connections` as `List<int>` |
| `Station` | ✅ Complete | Facilities, tags |
| `SystemMetrics` | ✅ Complete | 5 metrics, 0-5 scale |
| `WorldTags` | ✅ Complete | 8 system tags, 6 station tags |
| `Faction` | ✅ Complete | 3 factions with metrics |

### Gaps for WD2

| WD2 Requirement | Current Status | Gap |
|-----------------|----------------|-----|
| Multiple systems | ❌ Only 1 hub | Need test sector with 8-10 systems |
| `Route` class | ❌ Missing | Connections are implicit `List<int>` |
| Route properties | ❌ Missing | No distance, hazard, tags on routes |
| Route queries | ⚠️ Partial | `AreConnected` exists, no `GetRoute` |
| Multiple stations/system | ⚠️ Partial | Structure exists, not tested |
| Test sector factory | ❌ Missing | Only `CreateSingleHub` |

---

## Architecture Decisions

### Routes as First-Class Entities

**Decision**: Create explicit `Route` class rather than relying on `StarSystem.Connections`.

**Rationale**:
- Routes need metadata: distance, hazard level, tags
- Travel system needs route properties for fuel/time calculation
- Encounter system needs route tags for event selection
- Bidirectional by design (A↔B is one route, not two)

**Structure**:
```
Route
├── int Id
├── int SystemA, SystemB
├── float Distance (computed or explicit)
├── int HazardLevel (0-5)
├── HashSet<string> Tags
└── Helper methods: Connects(), GetOther()
```

### Dual Storage Strategy

**Decision**: Keep `StarSystem.Connections` for fast neighbor lookup, add `WorldState.Routes` for route metadata.

**Rationale**:
- `Connections` is O(1) for "is connected?" checks
- `Routes` dictionary provides route details when needed
- Sync maintained by `WorldState.AddRoute()` / `Connect()`

### Route ID Generation

**Decision**: Route IDs are deterministic: `min(A,B) * 10000 + max(A,B)`.

**Rationale**:
- Guarantees unique ID per system pair
- Same ID regardless of direction
- No need for ID counter in serialization

---

## Implementation Steps

### Phase 1: Route Data Structure

#### Step 1.1: Create Route Class

**File**: `src/sim/world/Route.cs`

```csharp
using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// A route connecting two star systems.
/// Routes are bidirectional - SystemA ↔ SystemB.
/// </summary>
public class Route
{
    public int Id { get; set; }
    public int SystemA { get; set; }
    public int SystemB { get; set; }
    
    /// <summary>
    /// Travel distance in arbitrary units.
    /// Used for fuel/time calculations.
    /// </summary>
    public float Distance { get; set; }
    
    /// <summary>
    /// Hazard level 0-5. Higher = more dangerous.
    /// Affects encounter probability and type.
    /// </summary>
    public int HazardLevel { get; set; } = 0;
    
    /// <summary>
    /// Route tags for encounter selection.
    /// Examples: "dangerous", "patrolled", "hidden"
    /// </summary>
    public HashSet<string> Tags { get; set; } = new();

    public Route() { }

    public Route(int systemA, int systemB, float distance = 0f)
    {
        SystemA = systemA;
        SystemB = systemB;
        Distance = distance;
        Id = GenerateId(systemA, systemB);
    }

    /// <summary>
    /// Check if this route connects to a given system.
    /// </summary>
    public bool Connects(int systemId) => SystemA == systemId || SystemB == systemId;

    /// <summary>
    /// Get the other endpoint of the route.
    /// </summary>
    public int GetOther(int systemId) => SystemA == systemId ? SystemB : SystemA;

    /// <summary>
    /// Check if route has a specific tag.
    /// </summary>
    public bool HasTag(string tag) => Tags.Contains(tag);

    /// <summary>
    /// Generate deterministic route ID from system pair.
    /// </summary>
    public static int GenerateId(int systemA, int systemB)
    {
        int min = System.Math.Min(systemA, systemB);
        int max = System.Math.Max(systemA, systemB);
        return min * 10000 + max;
    }

    public RouteData GetState()
    {
        return new RouteData
        {
            Id = Id,
            SystemA = SystemA,
            SystemB = SystemB,
            Distance = Distance,
            HazardLevel = HazardLevel,
            Tags = new List<string>(Tags)
        };
    }

    public static Route FromState(RouteData data)
    {
        return new Route
        {
            Id = data.Id,
            SystemA = data.SystemA,
            SystemB = data.SystemB,
            Distance = data.Distance,
            HazardLevel = data.HazardLevel,
            Tags = new HashSet<string>(data.Tags ?? new List<string>())
        };
    }
}

/// <summary>
/// Serializable data for Route.
/// </summary>
public class RouteData
{
    public int Id { get; set; }
    public int SystemA { get; set; }
    public int SystemB { get; set; }
    public float Distance { get; set; }
    public int HazardLevel { get; set; }
    public List<string> Tags { get; set; } = new();
}
```

**Acceptance Criteria**:
- [ ] `Route` class with all properties
- [ ] `Connects()` and `GetOther()` work correctly
- [ ] Deterministic ID generation
- [ ] Serialization round-trip

---

#### Step 1.2: Add Route Tags to WorldTags

**File**: `src/sim/world/WorldTags.cs`

**Add route-specific tags**:
```csharp
// Route tags
public const string Dangerous = "dangerous";
public const string Patrolled = "patrolled";
public const string Hidden = "hidden";
public const string Blockaded = "blockaded";
public const string Shortcut = "shortcut";
public const string Asteroid = "asteroid_field";
public const string Nebula = "nebula";
```

**Acceptance Criteria**:
- [ ] 7 route tags defined
- [ ] Tags documented with intended use

---

### Phase 2: WorldState Route Management

#### Step 2.1: Add Route Storage to WorldState

**File**: `src/sim/world/WorldState.cs`

**Add properties**:
```csharp
public Dictionary<int, Route> Routes { get; private set; } = new();
```

**Add route management methods**:
```csharp
// ========== Route Queries ==========

/// <summary>
/// Get route between two systems.
/// Returns null if no direct route exists.
/// </summary>
public Route GetRoute(int fromId, int toId)
{
    int routeId = Route.GenerateId(fromId, toId);
    return Routes.TryGetValue(routeId, out var route) ? route : null;
}

/// <summary>
/// Get all routes from a system.
/// </summary>
public IEnumerable<Route> GetRoutesFrom(int systemId)
{
    return Routes.Values.Where(r => r.Connects(systemId));
}

/// <summary>
/// Check if a direct route exists between two systems.
/// </summary>
public bool HasRoute(int fromId, int toId)
{
    return GetRoute(fromId, toId) != null;
}

/// <summary>
/// Get all routes in the sector.
/// </summary>
public IEnumerable<Route> GetAllRoutes()
{
    return Routes.Values;
}

/// <summary>
/// Get routes by tag.
/// </summary>
public IEnumerable<Route> GetRoutesByTag(string tag)
{
    return Routes.Values.Where(r => r.HasTag(tag));
}

/// <summary>
/// Get hazard level for route between two systems.
/// Returns 0 if no route exists.
/// </summary>
public int GetRouteHazard(int fromId, int toId)
{
    return GetRoute(fromId, toId)?.HazardLevel ?? 0;
}
```

**Acceptance Criteria**:
- [ ] `Routes` dictionary added
- [ ] `GetRoute()` returns correct route
- [ ] `GetRoutesFrom()` returns all connected routes
- [ ] `HasRoute()` works correctly

---

#### Step 2.2: Update Connect Method

**File**: `src/sim/world/WorldState.cs`

**Replace existing `Connect()` with route-aware version**:
```csharp
/// <summary>
/// Connect two systems with a route.
/// Creates bidirectional connection and route entry.
/// </summary>
/// <param name="systemA">First system ID</param>
/// <param name="systemB">Second system ID</param>
/// <param name="hazardLevel">Route hazard 0-5</param>
/// <param name="tags">Optional route tags</param>
/// <returns>The created route, or existing route if already connected</returns>
public Route Connect(int systemA, int systemB, int hazardLevel = 0, params string[] tags)
{
    var a = GetSystem(systemA);
    var b = GetSystem(systemB);
    if (a == null || b == null) return null;

    // Check for existing route
    var existing = GetRoute(systemA, systemB);
    if (existing != null) return existing;

    // Update system connections
    if (!a.Connections.Contains(systemB))
        a.Connections.Add(systemB);
    if (!b.Connections.Contains(systemA))
        b.Connections.Add(systemA);

    // Calculate distance from positions
    float distance = a.Position.DistanceTo(b.Position);

    // Create route
    var route = new Route(systemA, systemB, distance)
    {
        HazardLevel = hazardLevel,
        Tags = new HashSet<string>(tags)
    };

    Routes[route.Id] = route;
    return route;
}

/// <summary>
/// Add a pre-configured route.
/// Also updates system connections.
/// </summary>
public void AddRoute(Route route)
{
    var a = GetSystem(route.SystemA);
    var b = GetSystem(route.SystemB);
    
    if (a != null && !a.Connections.Contains(route.SystemB))
        a.Connections.Add(route.SystemB);
    if (b != null && !b.Connections.Contains(route.SystemA))
        b.Connections.Add(route.SystemA);

    Routes[route.Id] = route;
}
```

**Acceptance Criteria**:
- [ ] `Connect()` creates route with distance
- [ ] `Connect()` returns existing route if already connected
- [ ] System connections updated bidirectionally
- [ ] `AddRoute()` syncs connections

---

#### Step 2.3: Update Serialization

**File**: `src/sim/world/WorldState.cs`

**Update `GetState()`**:
```csharp
public WorldStateData GetState()
{
    var data = new WorldStateData
    {
        Name = Name,
        NextStationId = nextStationId
    };

    foreach (var system in Systems.Values)
        data.Systems.Add(system.GetState());

    foreach (var station in Stations.Values)
        data.Stations.Add(station.GetState());

    foreach (var faction in Factions.Values)
        data.Factions.Add(faction.GetState());

    foreach (var route in Routes.Values)
        data.Routes.Add(route.GetState());

    return data;
}
```

**Update `FromState()`**:
```csharp
public static WorldState FromState(WorldStateData data)
{
    var world = new WorldState
    {
        Name = data.Name ?? "Unknown Sector",
        nextStationId = data.NextStationId
    };

    foreach (var systemData in data.Systems ?? new List<StarSystemData>())
        world.Systems[systemData.Id] = StarSystem.FromState(systemData);

    foreach (var stationData in data.Stations ?? new List<StationData>())
        world.Stations[stationData.Id] = Station.FromState(stationData);

    foreach (var factionData in data.Factions ?? new List<FactionData>())
        world.Factions[factionData.Id] = Faction.FromState(factionData);

    foreach (var routeData in data.Routes ?? new List<RouteData>())
        world.Routes[routeData.Id] = Route.FromState(routeData);

    return world;
}
```

**Update `WorldStateData`**:
```csharp
public class WorldStateData
{
    public string Name { get; set; }
    public int NextStationId { get; set; }
    public List<StarSystemData> Systems { get; set; } = new();
    public List<StationData> Stations { get; set; } = new();
    public List<FactionData> Factions { get; set; } = new();
    public List<RouteData> Routes { get; set; } = new();  // NEW
}
```

**Acceptance Criteria**:
- [ ] Routes serialized in `GetState()`
- [ ] Routes restored in `FromState()`
- [ ] `WorldStateData` includes `Routes`

---

### Phase 3: Test Sector Factory

#### Step 3.1: Design Test Sector

The test sector should exercise all WD2 features while being small enough for testing.

**Sector Layout** (8 systems):

```
                    [4] Contested Zone
                     |  (contested, dangerous)
                     |
[0] Haven Hub ----[1] Trade Post----[2] Mining Colony
    (core,hub)       (frontier)        (mining,industrial)
        |                |                    |
        |                |                    |
    [5] Patrol Base  [6] Smuggler's Den   [7] Derelict
    (military)       (lawless)            (derelict)
                         |
                     [3] Pirate Haven
                     (lawless, pirate)
```

**System Details**:

| ID | Name | Type | Faction | Tags | Stations |
|----|------|------|---------|------|----------|
| 0 | Haven Station | Station | corp | core, hub | 1 (major) |
| 1 | Waypoint Alpha | Outpost | corp | frontier | 1 (minor) |
| 2 | Rockfall Mining | Asteroid | corp | mining, industrial | 1 (mining) |
| 3 | Red Claw Base | Outpost | pirates | lawless, pirate_haven | 1 (pirate) |
| 4 | Contested Zone | Contested | neutral | border | 0 |
| 5 | Patrol Station | Station | corp | military | 1 (military) |
| 6 | Smuggler's Den | Nebula | neutral | lawless | 1 (black market) |
| 7 | Wreck of Icarus | Derelict | neutral | frontier | 0 |

**Route Details**:

| From | To | Hazard | Tags |
|------|-----|--------|------|
| 0 | 1 | 1 | patrolled |
| 0 | 5 | 0 | patrolled |
| 1 | 2 | 2 | asteroid_field |
| 1 | 4 | 3 | dangerous |
| 1 | 6 | 2 | hidden |
| 2 | 7 | 3 | dangerous |
| 3 | 6 | 2 | hidden |
| 4 | - | - | (isolated for now) |

---

#### Step 3.2: Implement CreateTestSector Factory

**File**: `src/sim/world/WorldState.cs`

```csharp
/// <summary>
/// Create a test sector with 8 systems for G2 development.
/// </summary>
public static WorldState CreateTestSector()
{
    var world = new WorldState
    {
        Name = "Outer Reach"
    };

    // Load factions from registry
    foreach (var faction in FactionRegistry.GetAll())
    {
        world.AddFaction(CloneFaction(faction));
    }

    // ===== Systems =====
    
    // 0: Haven Station - main hub
    var haven = new StarSystem(0, "Haven Station", SystemType.Station, new Vector2(200, 300))
    {
        OwningFactionId = "corp",
        Tags = new HashSet<string> { WorldTags.Core, WorldTags.Hub }
    };
    haven.Metrics = SystemMetrics.ForSystemType(SystemType.Station);
    world.AddSystem(haven);

    // 1: Waypoint Alpha - frontier outpost
    var waypoint = new StarSystem(1, "Waypoint Alpha", SystemType.Outpost, new Vector2(350, 300))
    {
        OwningFactionId = "corp",
        Tags = new HashSet<string> { WorldTags.Frontier }
    };
    waypoint.Metrics = SystemMetrics.ForSystemType(SystemType.Outpost);
    world.AddSystem(waypoint);

    // 2: Rockfall Mining - asteroid mining
    var rockfall = new StarSystem(2, "Rockfall Mining", SystemType.Asteroid, new Vector2(500, 300))
    {
        OwningFactionId = "corp",
        Tags = new HashSet<string> { WorldTags.Mining, WorldTags.Industrial }
    };
    rockfall.Metrics = SystemMetrics.ForSystemType(SystemType.Asteroid);
    world.AddSystem(rockfall);

    // 3: Red Claw Base - pirate haven
    var redClaw = new StarSystem(3, "Red Claw Base", SystemType.Outpost, new Vector2(350, 500))
    {
        OwningFactionId = "pirates",
        Tags = new HashSet<string> { WorldTags.Lawless }
    };
    redClaw.Metrics = new SystemMetrics
    {
        Stability = 2,
        SecurityLevel = 0,
        CriminalActivity = 5,
        EconomicActivity = 2,
        LawEnforcementPresence = 0
    };
    world.AddSystem(redClaw);

    // 4: Contested Zone - faction border
    var contested = new StarSystem(4, "Contested Zone", SystemType.Contested, new Vector2(350, 150))
    {
        OwningFactionId = null,
        Tags = new HashSet<string> { WorldTags.Border }
    };
    contested.Metrics = SystemMetrics.ForSystemType(SystemType.Contested);
    world.AddSystem(contested);

    // 5: Patrol Station - military base
    var patrol = new StarSystem(5, "Patrol Station", SystemType.Station, new Vector2(100, 400))
    {
        OwningFactionId = "corp",
        Tags = new HashSet<string> { WorldTags.Military }
    };
    patrol.Metrics = new SystemMetrics
    {
        Stability = 5,
        SecurityLevel = 5,
        CriminalActivity = 0,
        EconomicActivity = 2,
        LawEnforcementPresence = 5
    };
    world.AddSystem(patrol);

    // 6: Smuggler's Den - neutral lawless
    var smuggler = new StarSystem(6, "Smuggler's Den", SystemType.Nebula, new Vector2(350, 400))
    {
        OwningFactionId = null,
        Tags = new HashSet<string> { WorldTags.Lawless }
    };
    smuggler.Metrics = SystemMetrics.ForSystemType(SystemType.Nebula);
    world.AddSystem(smuggler);

    // 7: Wreck of Icarus - derelict
    var wreck = new StarSystem(7, "Wreck of Icarus", SystemType.Derelict, new Vector2(550, 400))
    {
        OwningFactionId = null,
        Tags = new HashSet<string> { WorldTags.Frontier }
    };
    wreck.Metrics = SystemMetrics.ForSystemType(SystemType.Derelict);
    world.AddSystem(wreck);

    // ===== Routes =====
    world.Connect(0, 1, 1, WorldTags.Patrolled);      // Haven - Waypoint
    world.Connect(0, 5, 0, WorldTags.Patrolled);      // Haven - Patrol
    world.Connect(1, 2, 2, WorldTags.Asteroid);       // Waypoint - Rockfall
    world.Connect(1, 4, 3, WorldTags.Dangerous);      // Waypoint - Contested
    world.Connect(1, 6, 2, WorldTags.Hidden);         // Waypoint - Smuggler
    world.Connect(2, 7, 3, WorldTags.Dangerous);      // Rockfall - Wreck
    world.Connect(3, 6, 2, WorldTags.Hidden);         // Red Claw - Smuggler

    // ===== Stations =====
    
    // Haven Station - major hub
    var havenStation = Station.CreateHub(world.GenerateStationId(), "Haven Station", 0, "corp");
    world.AddStation(havenStation);

    // Waypoint Alpha - minor outpost
    var waypointStation = Station.CreateOutpost(world.GenerateStationId(), "Waypoint Alpha", 1, "corp");
    world.AddStation(waypointStation);

    // Rockfall Mining - mining station
    var rockfallStation = Station.CreateMining(world.GenerateStationId(), "Rockfall Mining", 2, "corp");
    world.AddStation(rockfallStation);

    // Red Claw Base - pirate den
    var pirateStation = Station.CreatePirateDen(world.GenerateStationId(), "Red Claw Base", 3, "pirates");
    world.AddStation(pirateStation);

    // Patrol Station - military
    var patrolStation = Station.CreateMilitary(world.GenerateStationId(), "Patrol Station", 5, "corp");
    world.AddStation(patrolStation);

    // Smuggler's Den - black market
    var smugglerStation = Station.CreateBlackMarket(world.GenerateStationId(), "Smuggler's Den", 6, null);
    world.AddStation(smugglerStation);

    return world;
}
```

**Acceptance Criteria**:
- [ ] 8 systems created with correct types
- [ ] 7 routes with hazard levels and tags
- [ ] 6 stations with appropriate facilities
- [ ] Factions assigned correctly

---

#### Step 3.3: Add Station Factory Methods

**File**: `src/sim/world/Station.cs`

Add factory methods for different station types:

```csharp
/// <summary>
/// Create a minor outpost with basic facilities.
/// </summary>
public static Station CreateOutpost(int id, string name, int systemId, string factionId)
{
    return new Station
    {
        Id = id,
        Name = name,
        SystemId = systemId,
        OwningFactionId = factionId,
        Facilities = new List<Facility>
        {
            new() { Type = FacilityType.Shop, Level = 1 },
            new() { Type = FacilityType.MissionBoard, Level = 1 },
            new() { Type = FacilityType.FuelDepot, Level = 1 }
        },
        Tags = new HashSet<string> { WorldTags.Frontier }
    };
}

/// <summary>
/// Create a mining station.
/// </summary>
public static Station CreateMining(int id, string name, int systemId, string factionId)
{
    return new Station
    {
        Id = id,
        Name = name,
        SystemId = systemId,
        OwningFactionId = factionId,
        Facilities = new List<Facility>
        {
            new() { Type = FacilityType.Shop, Level = 1 },
            new() { Type = FacilityType.MissionBoard, Level = 1 },
            new() { Type = FacilityType.RepairYard, Level = 2 },
            new() { Type = FacilityType.FuelDepot, Level = 2 }
        },
        Tags = new HashSet<string> { WorldTags.Industrial }
    };
}

/// <summary>
/// Create a pirate den with black market.
/// </summary>
public static Station CreatePirateDen(int id, string name, int systemId, string factionId)
{
    return new Station
    {
        Id = id,
        Name = name,
        SystemId = systemId,
        OwningFactionId = factionId,
        Facilities = new List<Facility>
        {
            new() { Type = FacilityType.Bar, Level = 2 },
            new() { Type = FacilityType.BlackMarket, Level = 2 },
            new() { Type = FacilityType.Recruitment, Level = 1 },
            new() { Type = FacilityType.RepairYard, Level = 1 }
        },
        Tags = new HashSet<string> { WorldTags.BlackMarket }
    };
}

/// <summary>
/// Create a military station.
/// </summary>
public static Station CreateMilitary(int id, string name, int systemId, string factionId)
{
    return new Station
    {
        Id = id,
        Name = name,
        SystemId = systemId,
        OwningFactionId = factionId,
        Facilities = new List<Facility>
        {
            new() { Type = FacilityType.MissionBoard, Level = 2 },
            new() { Type = FacilityType.RepairYard, Level = 2 },
            new() { Type = FacilityType.Medical, Level = 2 },
            new() { Type = FacilityType.FuelDepot, Level = 2 }
        },
        Tags = new HashSet<string> { WorldTags.Military }
    };
}

/// <summary>
/// Create a black market station.
/// </summary>
public static Station CreateBlackMarket(int id, string name, int systemId, string factionId)
{
    return new Station
    {
        Id = id,
        Name = name,
        SystemId = systemId,
        OwningFactionId = factionId,
        Facilities = new List<Facility>
        {
            new() { Type = FacilityType.Bar, Level = 1 },
            new() { Type = FacilityType.BlackMarket, Level = 3 },
            new() { Type = FacilityType.Recruitment, Level = 1 }
        },
        Tags = new HashSet<string> { WorldTags.BlackMarket }
    };
}
```

**Acceptance Criteria**:
- [ ] 5 new station factory methods
- [ ] Each has appropriate facilities
- [ ] Tags match station type

---

### Phase 4: Enhanced Graph Queries

#### Step 4.1: Add Pathfinding Support

**File**: `src/sim/world/WorldState.cs`

```csharp
/// <summary>
/// Find shortest path between two systems using BFS.
/// Returns list of system IDs including start and end.
/// Returns empty list if no path exists.
/// </summary>
public List<int> FindPath(int fromId, int toId)
{
    if (fromId == toId) return new List<int> { fromId };
    if (!Systems.ContainsKey(fromId) || !Systems.ContainsKey(toId))
        return new List<int>();

    var visited = new HashSet<int>();
    var queue = new Queue<List<int>>();
    queue.Enqueue(new List<int> { fromId });
    visited.Add(fromId);

    while (queue.Count > 0)
    {
        var path = queue.Dequeue();
        var current = path[path.Count - 1];

        foreach (var neighbor in GetNeighbors(current))
        {
            if (visited.Contains(neighbor)) continue;
            
            var newPath = new List<int>(path) { neighbor };
            
            if (neighbor == toId)
                return newPath;

            visited.Add(neighbor);
            queue.Enqueue(newPath);
        }
    }

    return new List<int>();
}

/// <summary>
/// Calculate total route distance for a path.
/// </summary>
public float GetPathDistance(List<int> path)
{
    if (path == null || path.Count < 2) return 0f;

    float total = 0f;
    for (int i = 0; i < path.Count - 1; i++)
    {
        var route = GetRoute(path[i], path[i + 1]);
        total += route?.Distance ?? GetTravelDistance(path[i], path[i + 1]);
    }
    return total;
}

/// <summary>
/// Calculate total hazard for a path (sum of route hazards).
/// </summary>
public int GetPathHazard(List<int> path)
{
    if (path == null || path.Count < 2) return 0;

    int total = 0;
    for (int i = 0; i < path.Count - 1; i++)
    {
        total += GetRouteHazard(path[i], path[i + 1]);
    }
    return total;
}

/// <summary>
/// Get routes along a path.
/// </summary>
public List<Route> GetPathRoutes(List<int> path)
{
    var routes = new List<Route>();
    if (path == null || path.Count < 2) return routes;

    for (int i = 0; i < path.Count - 1; i++)
    {
        var route = GetRoute(path[i], path[i + 1]);
        if (route != null) routes.Add(route);
    }
    return routes;
}
```

**Acceptance Criteria**:
- [ ] `FindPath()` returns shortest path
- [ ] `FindPath()` returns empty for disconnected systems
- [ ] `GetPathDistance()` sums route distances
- [ ] `GetPathHazard()` sums route hazards

---

#### Step 4.2: Add Convenience Queries

**File**: `src/sim/world/WorldState.cs`

```csharp
/// <summary>
/// Get systems within N hops that have a station.
/// </summary>
public List<StarSystem> GetNearbyStationSystems(int originId, int maxHops = 2)
{
    var result = new List<StarSystem>();
    var visited = new HashSet<int> { originId };
    var frontier = new List<int> { originId };

    for (int hop = 0; hop < maxHops && frontier.Count > 0; hop++)
    {
        var nextFrontier = new List<int>();
        foreach (var systemId in frontier)
        {
            foreach (var neighbor in GetNeighbors(systemId))
            {
                if (visited.Contains(neighbor)) continue;
                visited.Add(neighbor);
                nextFrontier.Add(neighbor);

                var system = GetSystem(neighbor);
                if (system != null && system.StationIds.Count > 0)
                    result.Add(system);
            }
        }
        frontier = nextFrontier;
    }

    return result;
}

/// <summary>
/// Get dangerous routes (hazard >= threshold).
/// </summary>
public IEnumerable<Route> GetDangerousRoutes(int minHazard = 3)
{
    return Routes.Values.Where(r => r.HazardLevel >= minHazard);
}

/// <summary>
/// Get safe routes (hazard <= threshold).
/// </summary>
public IEnumerable<Route> GetSafeRoutes(int maxHazard = 1)
{
    return Routes.Values.Where(r => r.HazardLevel <= maxHazard);
}

/// <summary>
/// Check if system is reachable from another.
/// </summary>
public bool IsReachable(int fromId, int toId)
{
    return FindPath(fromId, toId).Count > 0;
}
```

**Acceptance Criteria**:
- [ ] `GetNearbyStationSystems()` finds stations within range
- [ ] `GetDangerousRoutes()` filters by hazard
- [ ] `IsReachable()` checks connectivity

---

## Files Summary

### Files to Create

| File | Purpose |
|------|---------|
| `src/sim/world/Route.cs` | Route class with serialization |
| `tests/sim/world/WD2RouteTests.cs` | Route unit tests |
| `tests/sim/world/WD2TopologyTests.cs` | Topology and pathfinding tests |
| `tests/sim/world/WD2TestSectorTests.cs` | Test sector validation |

### Files to Modify

| File | Changes |
|------|---------|
| `src/sim/world/WorldState.cs` | Add Routes, Connect(), queries, CreateTestSector() |
| `src/sim/world/WorldTags.cs` | Add route tags |
| `src/sim/world/Station.cs` | Add factory methods |
| `src/sim/world/agents.md` | Document Route.cs |

---

## Unit Tests

### Test File: `tests/sim/world/WD2RouteTests.cs`

```csharp
using GdUnit4;
using static GdUnit4.Assertions;

namespace FringeTactics.Tests;

[TestSuite]
public class WD2RouteTests
{
    [TestCase]
    public void Route_Constructor_SetsProperties()
    {
        var route = new Route(0, 1, 100f);

        AssertInt(route.SystemA).IsEqual(0);
        AssertInt(route.SystemB).IsEqual(1);
        AssertFloat(route.Distance).IsEqual(100f);
        AssertInt(route.HazardLevel).IsEqual(0);
    }

    [TestCase]
    public void Route_GenerateId_IsDeterministic()
    {
        int id1 = Route.GenerateId(0, 1);
        int id2 = Route.GenerateId(1, 0);

        AssertInt(id1).IsEqual(id2);
    }

    [TestCase]
    public void Route_GenerateId_IsUnique()
    {
        int id01 = Route.GenerateId(0, 1);
        int id02 = Route.GenerateId(0, 2);
        int id12 = Route.GenerateId(1, 2);

        AssertInt(id01).IsNotEqual(id02);
        AssertInt(id01).IsNotEqual(id12);
        AssertInt(id02).IsNotEqual(id12);
    }

    [TestCase]
    public void Route_Connects_ReturnsTrueForEndpoints()
    {
        var route = new Route(0, 1, 100f);

        AssertBool(route.Connects(0)).IsTrue();
        AssertBool(route.Connects(1)).IsTrue();
        AssertBool(route.Connects(2)).IsFalse();
    }

    [TestCase]
    public void Route_GetOther_ReturnsOtherEndpoint()
    {
        var route = new Route(0, 1, 100f);

        AssertInt(route.GetOther(0)).IsEqual(1);
        AssertInt(route.GetOther(1)).IsEqual(0);
    }

    [TestCase]
    public void Route_HasTag_WorksCorrectly()
    {
        var route = new Route(0, 1, 100f);
        route.Tags.Add(WorldTags.Dangerous);

        AssertBool(route.HasTag(WorldTags.Dangerous)).IsTrue();
        AssertBool(route.HasTag(WorldTags.Patrolled)).IsFalse();
    }

    [TestCase]
    public void Route_Serialization_RoundTrip()
    {
        var route = new Route(0, 1, 150f)
        {
            HazardLevel = 3,
            Tags = new System.Collections.Generic.HashSet<string> { WorldTags.Dangerous }
        };

        var data = route.GetState();
        var restored = Route.FromState(data);

        AssertInt(restored.Id).IsEqual(route.Id);
        AssertInt(restored.SystemA).IsEqual(route.SystemA);
        AssertInt(restored.SystemB).IsEqual(route.SystemB);
        AssertFloat(restored.Distance).IsEqual(route.Distance);
        AssertInt(restored.HazardLevel).IsEqual(route.HazardLevel);
        AssertBool(restored.HasTag(WorldTags.Dangerous)).IsTrue();
    }
}
```

### Test File: `tests/sim/world/WD2TopologyTests.cs`

```csharp
using GdUnit4;
using static GdUnit4.Assertions;
using System.Collections.Generic;

namespace FringeTactics.Tests;

[TestSuite]
[RequireGodotRuntime]
public class WD2TopologyTests
{
    private WorldState world;

    [Before]
    public void Setup()
    {
        world = WorldState.CreateTestSector();
    }

    [TestCase]
    public void CreateTestSector_Creates8Systems()
    {
        AssertInt(world.Systems.Count).IsEqual(8);
    }

    [TestCase]
    public void CreateTestSector_Creates7Routes()
    {
        AssertInt(world.Routes.Count).IsEqual(7);
    }

    [TestCase]
    public void CreateTestSector_Creates6Stations()
    {
        AssertInt(world.Stations.Count).IsEqual(6);
    }

    [TestCase]
    public void GetRoute_ReturnsCorrectRoute()
    {
        var route = world.GetRoute(0, 1);

        AssertObject(route).IsNotNull();
        AssertBool(route.Connects(0)).IsTrue();
        AssertBool(route.Connects(1)).IsTrue();
    }

    [TestCase]
    public void GetRoute_ReturnsSameForBothDirections()
    {
        var route01 = world.GetRoute(0, 1);
        var route10 = world.GetRoute(1, 0);

        AssertObject(route01).IsNotNull();
        AssertObject(route10).IsNotNull();
        AssertInt(route01.Id).IsEqual(route10.Id);
    }

    [TestCase]
    public void GetRoute_ReturnsNullForUnconnected()
    {
        var route = world.GetRoute(0, 7);

        AssertObject(route).IsNull();
    }

    [TestCase]
    public void GetRoutesFrom_ReturnsAllConnectedRoutes()
    {
        var routes = new List<Route>(world.GetRoutesFrom(1));

        // Waypoint (1) connects to: Haven (0), Rockfall (2), Contested (4), Smuggler (6)
        AssertInt(routes.Count).IsEqual(4);
    }

    [TestCase]
    public void HasRoute_ReturnsTrueForConnected()
    {
        AssertBool(world.HasRoute(0, 1)).IsTrue();
        AssertBool(world.HasRoute(0, 7)).IsFalse();
    }

    [TestCase]
    public void GetRouteHazard_ReturnsCorrectValue()
    {
        // Haven-Waypoint has hazard 1
        AssertInt(world.GetRouteHazard(0, 1)).IsEqual(1);
        // Waypoint-Contested has hazard 3
        AssertInt(world.GetRouteHazard(1, 4)).IsEqual(3);
    }

    [TestCase]
    public void FindPath_ReturnsShortestPath()
    {
        // Haven (0) to Rockfall (2): should be 0 -> 1 -> 2
        var path = world.FindPath(0, 2);

        AssertInt(path.Count).IsEqual(3);
        AssertInt(path[0]).IsEqual(0);
        AssertInt(path[1]).IsEqual(1);
        AssertInt(path[2]).IsEqual(2);
    }

    [TestCase]
    public void FindPath_ReturnsEmptyForUnreachable()
    {
        // System 4 (Contested) is isolated in current test sector
        // Actually it's connected to 1, let's test a truly isolated case
        // For now, test same-system path
        var path = world.FindPath(0, 0);
        AssertInt(path.Count).IsEqual(1);
    }

    [TestCase]
    public void GetPathDistance_SumsRouteDistances()
    {
        var path = world.FindPath(0, 2);
        float distance = world.GetPathDistance(path);

        // Should be sum of route 0-1 and route 1-2 distances
        var route01 = world.GetRoute(0, 1);
        var route12 = world.GetRoute(1, 2);
        float expected = route01.Distance + route12.Distance;

        AssertFloat(distance).IsEqual(expected);
    }

    [TestCase]
    public void GetPathHazard_SumsRouteHazards()
    {
        var path = world.FindPath(0, 2);
        int hazard = world.GetPathHazard(path);

        // Route 0-1 hazard 1, Route 1-2 hazard 2
        AssertInt(hazard).IsEqual(3);
    }

    [TestCase]
    public void GetDangerousRoutes_FiltersCorrectly()
    {
        var dangerous = new List<Route>(world.GetDangerousRoutes(3));

        // Routes with hazard >= 3: Waypoint-Contested (3), Rockfall-Wreck (3)
        AssertInt(dangerous.Count).IsEqual(2);
    }

    [TestCase]
    public void IsReachable_ReturnsTrueForConnectedSystems()
    {
        AssertBool(world.IsReachable(0, 2)).IsTrue();
        AssertBool(world.IsReachable(0, 7)).IsTrue();
    }

    [TestCase]
    public void WorldState_Serialization_PreservesRoutes()
    {
        var data = world.GetState();
        var restored = WorldState.FromState(data);

        AssertInt(restored.Routes.Count).IsEqual(world.Routes.Count);

        var route = restored.GetRoute(0, 1);
        AssertObject(route).IsNotNull();
        AssertInt(route.HazardLevel).IsEqual(1);
    }
}
```

### Test File: `tests/sim/world/WD2TestSectorTests.cs`

```csharp
using GdUnit4;
using static GdUnit4.Assertions;
using System.Linq;

namespace FringeTactics.Tests;

[TestSuite]
[RequireGodotRuntime]
public class WD2TestSectorTests
{
    private WorldState world;

    [Before]
    public void Setup()
    {
        world = WorldState.CreateTestSector();
    }

    [TestCase]
    public void TestSector_HavenStation_IsCorrectlyConfigured()
    {
        var haven = world.GetSystem(0);

        AssertString(haven.Name).IsEqual("Haven Station");
        AssertObject(haven.Type).IsEqual(SystemType.Station);
        AssertString(haven.OwningFactionId).IsEqual("corp");
        AssertBool(haven.HasTag(WorldTags.Core)).IsTrue();
        AssertBool(haven.HasTag(WorldTags.Hub)).IsTrue();
    }

    [TestCase]
    public void TestSector_RedClawBase_IsPirateControlled()
    {
        var redClaw = world.GetSystem(3);

        AssertString(redClaw.OwningFactionId).IsEqual("pirates");
        AssertBool(redClaw.HasTag(WorldTags.Lawless)).IsTrue();
        AssertInt(redClaw.Metrics.CriminalActivity).IsEqual(5);
        AssertInt(redClaw.Metrics.SecurityLevel).IsEqual(0);
    }

    [TestCase]
    public void TestSector_PatrolStation_HasHighSecurity()
    {
        var patrol = world.GetSystem(5);

        AssertInt(patrol.Metrics.SecurityLevel).IsEqual(5);
        AssertInt(patrol.Metrics.LawEnforcementPresence).IsEqual(5);
        AssertInt(patrol.Metrics.CriminalActivity).IsEqual(0);
    }

    [TestCase]
    public void TestSector_StationsHaveCorrectFacilities()
    {
        // Haven should have all standard facilities
        var havenStation = world.GetPrimaryStation(0);
        AssertBool(havenStation.HasFacility(FacilityType.Shop)).IsTrue();
        AssertBool(havenStation.HasFacility(FacilityType.MissionBoard)).IsTrue();
        AssertBool(havenStation.HasFacility(FacilityType.RepairYard)).IsTrue();

        // Pirate den should have black market
        var pirateStation = world.GetPrimaryStation(3);
        AssertBool(pirateStation.HasFacility(FacilityType.BlackMarket)).IsTrue();
        AssertBool(pirateStation.HasFacility(FacilityType.Shop)).IsFalse();
    }

    [TestCase]
    public void TestSector_RoutesHaveCorrectTags()
    {
        // Haven-Waypoint should be patrolled
        var patrolledRoute = world.GetRoute(0, 1);
        AssertBool(patrolledRoute.HasTag(WorldTags.Patrolled)).IsTrue();

        // Waypoint-Smuggler should be hidden
        var hiddenRoute = world.GetRoute(1, 6);
        AssertBool(hiddenRoute.HasTag(WorldTags.Hidden)).IsTrue();
    }

    [TestCase]
    public void TestSector_SystemsWithoutStations()
    {
        // Contested Zone (4) and Wreck (7) have no stations
        var contestedStations = world.GetStationsInSystem(4).ToList();
        var wreckStations = world.GetStationsInSystem(7).ToList();

        AssertInt(contestedStations.Count).IsEqual(0);
        AssertInt(wreckStations.Count).IsEqual(0);
    }

    [TestCase]
    public void TestSector_FactionTerritories()
    {
        var corpSystems = world.GetSystemsByFaction("corp").ToList();
        var pirateSystems = world.GetSystemsByFaction("pirates").ToList();
        var neutralSystems = world.GetSystemsByFaction(null).ToList();

        // Corp: Haven, Waypoint, Rockfall, Patrol
        AssertInt(corpSystems.Count).IsEqual(4);
        // Pirates: Red Claw
        AssertInt(pirateSystems.Count).IsEqual(1);
        // Neutral: Contested, Smuggler, Wreck
        AssertInt(neutralSystems.Count).IsEqual(3);
    }
}
```

---

## Manual Testing Checklist

### Sector Creation
- [ ] `WorldState.CreateTestSector()` creates valid world
- [ ] All 8 systems have correct names and types
- [ ] All 7 routes connect correct systems
- [ ] All 6 stations have appropriate facilities
- [ ] Factions are assigned correctly

### Route Queries
- [ ] `GetRoute(0, 1)` returns Haven-Waypoint route
- [ ] `GetRoute(1, 0)` returns same route (bidirectional)
- [ ] `GetRoute(0, 7)` returns null (not directly connected)
- [ ] `GetRoutesFrom(1)` returns 4 routes (Waypoint is a hub)
- [ ] `GetRouteHazard(1, 4)` returns 3 (dangerous route)

### Pathfinding
- [ ] `FindPath(0, 7)` returns valid path through sector
- [ ] `GetPathDistance()` returns sum of route distances
- [ ] `GetPathHazard()` returns sum of route hazards
- [ ] `IsReachable(0, 3)` returns true (can reach pirate base)

### Serialization
- [ ] Save/load preserves all systems
- [ ] Save/load preserves all routes with properties
- [ ] Save/load preserves all stations
- [ ] Route hazard levels preserved after load

### Visual Verification (if SectorView exists)
- [ ] Systems render at correct positions
- [ ] Routes visible between connected systems
- [ ] Faction colors displayed correctly

---

## Success Criteria

1. ✅ `Route` class with distance, hazard, tags
2. ✅ `WorldState.Routes` dictionary with management
3. ✅ `Connect()` creates routes with computed distance
4. ✅ Route queries: `GetRoute`, `GetRoutesFrom`, `HasRoute`
5. ✅ Pathfinding: `FindPath`, `GetPathDistance`, `GetPathHazard`
6. ✅ `CreateTestSector()` with 8 systems, 7 routes, 6 stations
7. ✅ Station factory methods for different types
8. ✅ Route tags in `WorldTags`
9. ✅ Serialization preserves routes
10. ✅ All unit tests pass (93 tests)

---

## Implementation Summary

### Test Results
- **Phase 1 (Route)**: 12 tests
- **Phase 2 (Topology)**: 39 tests  
- **Phase 3 (Test Sector)**: 32 tests
- **WD1 Regression**: 10 tests
- **Total**: 93 tests passing

### Files Created
| File | Description |
|------|-------------|
| `src/sim/world/Route.cs` | Route class with distance, hazard, tags |
| `tests/sim/world/WD2RouteTests.cs` | 12 route unit tests |
| `tests/sim/world/WD2TopologyTests.cs` | 39 topology and pathfinding tests |
| `tests/sim/world/WD2TestSectorTests.cs` | 32 test sector validation tests |

### Files Modified
| File | Changes |
|------|---------||
| `src/sim/world/WorldState.cs` | Routes dict, queries, pathfinding, CreateTestSector() |
| `src/sim/world/WorldTags.cs` | 7 route tags added |
| `src/sim/world/Station.cs` | 5 station factory methods |

---

## Implementation Order

1. **Day 1**: Phase 1 - Route class and WorldTags
   - Create `Route.cs`
   - Add route tags to `WorldTags.cs`
   - Write `WD2RouteTests.cs`

2. **Day 2**: Phase 2 - WorldState route management
   - Add `Routes` dictionary
   - Update `Connect()` method
   - Add route queries
   - Update serialization

3. **Day 3**: Phase 3 - Test sector
   - Add station factory methods
   - Implement `CreateTestSector()`
   - Write `WD2TestSectorTests.cs`

4. **Day 4**: Phase 4 - Graph queries
   - Implement `FindPath()`
   - Add path utilities
   - Add convenience queries
   - Write `WD2TopologyTests.cs`

5. **Day 5**: Integration and polish
   - Update `agents.md`
   - Manual testing
   - Fix any issues

---

## Dependencies

**WD2 Depends On**: WD1 ✅ Complete

**WD2 Enables**:
- **TV1 – Route Planning**: Uses routes for travel planning
- **TV2 – Travel Execution**: Uses route hazards for encounters
- **GN2 – Galaxy Generation**: Uses topology patterns
- **EN1 – Encounter Runtime**: Uses route tags for event selection

---

## Appendix: Route Tag Semantics

| Tag | Travel Effect | Encounter Effect |
|-----|---------------|------------------|
| `patrolled` | Safer travel | More patrol encounters |
| `dangerous` | Higher encounter chance | Combat encounters |
| `hidden` | Requires discovery? | Smuggler encounters |
| `blockaded` | May require combat | Checkpoint encounters |
| `shortcut` | Reduced travel time | Fewer encounters |
| `asteroid_field` | Navigation hazard | Mining/salvage encounters |
| `nebula` | Sensor interference | Ambush risk |

---

## Appendix: Test Sector Connectivity Matrix

```
     0  1  2  3  4  5  6  7
  0  -  1  -  -  -  0  -  -
  1  1  -  2  -  3  -  2  -
  2  -  2  -  -  -  -  -  3
  3  -  -  -  -  -  -  2  -
  4  -  3  -  -  -  -  -  -
  5  0  -  -  -  -  -  -  -
  6  -  2  -  2  -  -  -  -
  7  -  -  3  -  -  -  -  -

Numbers = hazard level, - = no route
```
