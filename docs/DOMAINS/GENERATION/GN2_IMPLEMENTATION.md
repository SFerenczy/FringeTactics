# GN2 – Galaxy Generation: Implementation Plan

**Status**: ⬜ Pending  
**Depends on**: GN1 ✅, WD2 ✅, WD3 ✅  
**Phase**: G2

**Goal**: Generate the initial sector at campaign start, replacing the hardcoded `CreateTestSector()` with a procedural, seed-driven galaxy generator.

---

## Overview

GN2 transforms the static test sector into a dynamically generated galaxy. This is foundational for replayability—each campaign should feel different based on its seed.

**Key outcomes**:
- Procedural system placement with spatial constraints
- Connectivity graph ensuring all systems are reachable
- Faction territory assignment with coherent regions
- Station generation based on system type
- Initial metrics derived from system context
- Deterministic: same seed = same galaxy

---

## Current State Assessment

### What We Have

| Component | Status | Location | Notes |
|-----------|--------|----------|-------|
| `WorldState` | ✅ Complete | `src/sim/world/WorldState.cs` | Full sector representation |
| `StarSystem` | ✅ Complete | `src/sim/world/StarSystem.cs` | Type, position, metrics, tags |
| `Route` | ✅ Complete | `src/sim/world/Route.cs` | Distance, hazard, tags |
| `Station` | ✅ Complete | `src/sim/world/Station.cs` | Factory methods for types |
| `SystemMetrics` | ✅ Complete | `src/sim/world/SystemMetrics.cs` | 5 metrics, `ForSystemType()` |
| `WorldTags` | ✅ Complete | `src/sim/world/WorldTags.cs` | System, station, route tags |
| `FactionRegistry` | ✅ Complete | `src/sim/world/FactionRegistry.cs` | Faction definitions |
| `RngService` | ✅ Complete | `src/sim/RngService.cs` | Deterministic RNG streams |
| `CreateTestSector()` | ✅ Hardcoded | `WorldState.cs` | 8 systems, manual layout |

### What GN2 Requires

| Requirement | Current Status | Gap |
|-------------|----------------|-----|
| `GalaxyConfig` | ❌ Missing | No configuration for generation parameters |
| `GalaxyGenerator` | ❌ Missing | No procedural generation |
| Position algorithm | ❌ Missing | Hardcoded positions in test sector |
| Route algorithm | ❌ Missing | Hardcoded connections |
| Faction assignment | ❌ Missing | Hardcoded faction ownership |
| Name generation | ❌ Missing | Hardcoded names |
| Metric initialization | ⚠️ Partial | `ForSystemType()` exists, needs context |

---

## Architecture Decisions

### 1. Generator as Stateless Service

**Decision**: `GalaxyGenerator` is a stateless service that takes config + RNG and returns a `WorldState`.

**Rationale**:
- Follows architecture guidelines (stateless services)
- Easy to test with different configs
- No hidden state between generations

### 2. Two-Phase Generation

**Decision**: Generate in two phases:
1. **Topology**: Systems, positions, connections
2. **Content**: Stations, metrics, names

**Rationale**:
- Topology must be valid before adding content
- Allows validation between phases
- Easier to debug and test

### 3. MST + Random Edges for Connectivity

**Decision**: Use Minimum Spanning Tree for guaranteed connectivity, then add random extra edges.

**Rationale**:
- MST guarantees all systems reachable
- Extra edges add variety and alternate routes
- Well-understood algorithm, easy to implement

### 4. Faction Territories via Flood Fill

**Decision**: Assign faction "capitals" first, then flood-fill ownership outward.

**Rationale**:
- Creates coherent faction regions
- Avoids checkerboard ownership patterns
- Contested zones naturally emerge at boundaries

---

## GN2 Deliverables Checklist

### Phase 1: Configuration ✅
- [x] **1.1** Create `GalaxyConfig` class
- [x] **1.2** Define system count, connection limits
- [x] **1.3** Define faction and system type distributions
- [x] **1.4** Define spatial constraints (map size, min distance)

### Phase 2: Position Generation ✅
- [x] **2.1** Implement Poisson disk sampling (or rejection sampling)
- [x] **2.2** Validate minimum distance constraints
- [x] **2.3** Add edge margin to avoid systems at map boundaries

### Phase 3: Route Generation ✅
- [x] **3.1** Implement Minimum Spanning Tree (Prim's algorithm)
- [x] **3.2** Add random extra edges within distance threshold
- [x] **3.3** Calculate route distances from positions
- [x] **3.4** Assign route hazards based on endpoint systems

### Phase 4: Faction Assignment ✅
- [x] **4.1** Place faction capitals (one per faction)
- [x] **4.2** Flood-fill ownership from capitals
- [x] **4.3** Mark contested/neutral systems at boundaries
- [x] **4.4** Validate faction distribution

### Phase 5: System Content ✅
- [x] **5.1** Assign system types based on position and faction
- [x] **5.2** Generate system names
- [x] **5.3** Initialize metrics from type and context
- [x] **5.4** Assign system tags

### Phase 6: Station Generation ✅
- [x] **6.1** Create stations for inhabited systems
- [x] **6.2** Select station type based on system type
- [x] **6.3** Assign facilities based on station type

### Phase 7: Integration ✅
- [x] **7.1** Replace `CreateTestSector()` with `GalaxyGenerator`
- [x] **7.2** Update `CampaignState.CreateNew()` to use generator
- [x] **7.3** Ensure save/load works with generated worlds

### Phase 8: Testing ✅
- [x] **8.1** Determinism tests (same seed = same galaxy)
- [x] **8.2** Connectivity tests (all systems reachable)
- [x] **8.3** Constraint tests (distances, connections)
- [x] **8.4** Distribution tests (faction balance, system types)

---

## Phase 1: Configuration

### Step 1.1: GalaxyConfig Class

**New File**: `src/sim/generation/GalaxyConfig.cs`

```csharp
using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// Configuration for procedural galaxy generation.
/// </summary>
public class GalaxyConfig
{
    // ===== Topology =====
    
    /// <summary>Number of star systems to generate.</summary>
    public int SystemCount { get; set; } = 12;
    
    /// <summary>Minimum connections per system (MST guarantees at least 1).</summary>
    public int MinConnections { get; set; } = 1;
    
    /// <summary>Maximum connections per system.</summary>
    public int MaxConnections { get; set; } = 4;
    
    /// <summary>Maximum distance for adding extra routes (beyond MST).</summary>
    public float MaxRouteDistance { get; set; } = 200f;
    
    // ===== Spatial =====
    
    /// <summary>Map width in arbitrary units.</summary>
    public float MapWidth { get; set; } = 800f;
    
    /// <summary>Map height in arbitrary units.</summary>
    public float MapHeight { get; set; } = 600f;
    
    /// <summary>Minimum distance between systems.</summary>
    public float MinSystemDistance { get; set; } = 80f;
    
    /// <summary>Margin from map edges.</summary>
    public float EdgeMargin { get; set; } = 50f;
    
    // ===== Factions =====
    
    /// <summary>Faction IDs to include in generation.</summary>
    public List<string> FactionIds { get; set; } = new() { "corp", "syndicate", "militia" };
    
    /// <summary>Fraction of systems that should be neutral/contested.</summary>
    public float NeutralFraction { get; set; } = 0.2f;
    
    // ===== System Types =====
    
    /// <summary>Weights for system type selection.</summary>
    public Dictionary<SystemType, float> SystemTypeWeights { get; set; } = new()
    {
        [SystemType.Station] = 0.25f,
        [SystemType.Outpost] = 0.30f,
        [SystemType.Asteroid] = 0.15f,
        [SystemType.Nebula] = 0.10f,
        [SystemType.Derelict] = 0.10f,
        [SystemType.Contested] = 0.10f
    };
    
    // ===== Stations =====
    
    /// <summary>System types that get stations.</summary>
    public HashSet<SystemType> InhabitedTypes { get; set; } = new()
    {
        SystemType.Station,
        SystemType.Outpost,
        SystemType.Asteroid
    };
    
    // ===== Presets =====
    
    /// <summary>Default configuration for standard campaigns.</summary>
    public static GalaxyConfig Default => new();
    
    /// <summary>Small sector for testing.</summary>
    public static GalaxyConfig Small => new()
    {
        SystemCount = 8,
        MapWidth = 600f,
        MapHeight = 450f
    };
    
    /// <summary>Large sector for extended campaigns.</summary>
    public static GalaxyConfig Large => new()
    {
        SystemCount = 20,
        MapWidth = 1000f,
        MapHeight = 800f,
        MaxConnections = 5
    };
}
```

**Acceptance Criteria**:
- [ ] All configuration parameters documented
- [ ] Default, Small, Large presets defined
- [ ] System type weights sum to ~1.0

---

## Phase 2: Position Generation

### Step 2.1: Position Generator

Position generation uses rejection sampling with minimum distance constraints.

**In `GalaxyGenerator.cs`**:

```csharp
/// <summary>
/// Generate system positions using rejection sampling.
/// </summary>
private List<Vector2> GeneratePositions()
{
    var positions = new List<Vector2>();
    int attempts = 0;
    int maxAttempts = config.SystemCount * 100;
    
    float minX = config.EdgeMargin;
    float maxX = config.MapWidth - config.EdgeMargin;
    float minY = config.EdgeMargin;
    float maxY = config.MapHeight - config.EdgeMargin;
    
    while (positions.Count < config.SystemCount && attempts < maxAttempts)
    {
        var pos = new Vector2(
            rng.NextFloat(minX, maxX),
            rng.NextFloat(minY, maxY)
        );
        
        if (IsValidPosition(pos, positions))
        {
            positions.Add(pos);
        }
        
        attempts++;
    }
    
    if (positions.Count < config.SystemCount)
    {
        SimLog.Warn($"[GalaxyGenerator] Only placed {positions.Count}/{config.SystemCount} systems after {maxAttempts} attempts");
    }
    
    return positions;
}

/// <summary>
/// Check if position is valid (far enough from existing positions).
/// </summary>
private bool IsValidPosition(Vector2 pos, List<Vector2> existing)
{
    float minDistSq = config.MinSystemDistance * config.MinSystemDistance;
    
    foreach (var other in existing)
    {
        if (pos.DistanceSquaredTo(other) < minDistSq)
            return false;
    }
    
    return true;
}
```

**Acceptance Criteria**:
- [ ] Generates requested number of systems
- [ ] All systems respect minimum distance
- [ ] No systems within edge margin
- [ ] Handles failure gracefully (logs warning)

---

## Phase 3: Route Generation

### Step 3.1: Minimum Spanning Tree

Use Prim's algorithm for MST:

```csharp
/// <summary>
/// Build minimum spanning tree to guarantee connectivity.
/// </summary>
private List<(int, int)> BuildMST(List<Vector2> positions)
{
    int n = positions.Count;
    var edges = new List<(int, int)>();
    var inTree = new HashSet<int> { 0 };
    
    while (inTree.Count < n)
    {
        float bestDist = float.MaxValue;
        int bestFrom = -1;
        int bestTo = -1;
        
        foreach (int from in inTree)
        {
            for (int to = 0; to < n; to++)
            {
                if (inTree.Contains(to)) continue;
                
                float dist = positions[from].DistanceTo(positions[to]);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestFrom = from;
                    bestTo = to;
                }
            }
        }
        
        if (bestTo >= 0)
        {
            edges.Add((bestFrom, bestTo));
            inTree.Add(bestTo);
        }
    }
    
    return edges;
}
```

### Step 3.2: Add Extra Routes

```csharp
/// <summary>
/// Add random extra routes for variety (beyond MST).
/// </summary>
private List<(int, int)> AddExtraRoutes(
    List<Vector2> positions, 
    List<(int, int)> mstEdges)
{
    var extraEdges = new List<(int, int)>();
    var existingEdges = new HashSet<(int, int)>();
    
    foreach (var (a, b) in mstEdges)
    {
        existingEdges.Add((Math.Min(a, b), Math.Max(a, b)));
    }
    
    // Count connections per system
    var connectionCount = new int[positions.Count];
    foreach (var (a, b) in mstEdges)
    {
        connectionCount[a]++;
        connectionCount[b]++;
    }
    
    // Try to add extra edges
    for (int i = 0; i < positions.Count; i++)
    {
        if (connectionCount[i] >= config.MaxConnections) continue;
        
        for (int j = i + 1; j < positions.Count; j++)
        {
            if (connectionCount[j] >= config.MaxConnections) continue;
            
            var key = (i, j);
            if (existingEdges.Contains(key)) continue;
            
            float dist = positions[i].DistanceTo(positions[j]);
            if (dist > config.MaxRouteDistance) continue;
            
            // Random chance to add edge (closer = more likely)
            float chance = 1.0f - (dist / config.MaxRouteDistance);
            if (rng.NextFloat() < chance * 0.5f)
            {
                extraEdges.Add(key);
                existingEdges.Add(key);
                connectionCount[i]++;
                connectionCount[j]++;
            }
        }
    }
    
    return extraEdges;
}
```

### Step 3.3: Create Route Objects

```csharp
/// <summary>
/// Create Route objects from edge list.
/// </summary>
private void CreateRoutes(
    WorldState world, 
    List<Vector2> positions,
    List<(int, int)> edges)
{
    foreach (var (a, b) in edges)
    {
        float distance = positions[a].DistanceTo(positions[b]);
        
        // Hazard based on endpoint system types (set later)
        int hazard = 1; // Default, updated after systems created
        
        world.Connect(a, b, hazard);
    }
}
```

**Acceptance Criteria**:
- [ ] All systems connected (MST property)
- [ ] No system exceeds max connections
- [ ] Extra routes add variety
- [ ] Route distances calculated correctly

---

## Phase 4: Faction Assignment

### Step 4.1: Place Faction Capitals

```csharp
/// <summary>
/// Place faction capitals at well-spaced positions.
/// </summary>
private Dictionary<string, int> PlaceFactionCapitals(List<Vector2> positions)
{
    var capitals = new Dictionary<string, int>();
    var used = new HashSet<int>();
    
    foreach (var factionId in config.FactionIds)
    {
        // Find system furthest from existing capitals
        int bestSystem = -1;
        float bestMinDist = -1;
        
        for (int i = 0; i < positions.Count; i++)
        {
            if (used.Contains(i)) continue;
            
            float minDist = float.MaxValue;
            foreach (var capitalIdx in capitals.Values)
            {
                float dist = positions[i].DistanceTo(positions[capitalIdx]);
                minDist = Math.Min(minDist, dist);
            }
            
            // For first capital, use distance from center
            if (capitals.Count == 0)
            {
                var center = new Vector2(config.MapWidth / 2, config.MapHeight / 2);
                minDist = positions[i].DistanceTo(center);
            }
            
            if (minDist > bestMinDist)
            {
                bestMinDist = minDist;
                bestSystem = i;
            }
        }
        
        if (bestSystem >= 0)
        {
            capitals[factionId] = bestSystem;
            used.Add(bestSystem);
        }
    }
    
    return capitals;
}
```

### Step 4.2: Flood Fill Ownership

```csharp
/// <summary>
/// Assign faction ownership via flood fill from capitals.
/// </summary>
private void AssignFactionOwnership(
    WorldState world,
    Dictionary<string, int> capitals)
{
    var ownership = new Dictionary<int, string>();
    var distance = new Dictionary<int, int>();
    
    // Initialize capitals
    foreach (var (factionId, systemId) in capitals)
    {
        ownership[systemId] = factionId;
        distance[systemId] = 0;
    }
    
    // BFS from all capitals simultaneously
    var queue = new Queue<int>(capitals.Values);
    
    while (queue.Count > 0)
    {
        int current = queue.Dequeue();
        int currentDist = distance[current];
        string currentFaction = ownership[current];
        
        foreach (int neighbor in world.GetNeighbors(current))
        {
            if (!ownership.ContainsKey(neighbor))
            {
                ownership[neighbor] = currentFaction;
                distance[neighbor] = currentDist + 1;
                queue.Enqueue(neighbor);
            }
            else if (ownership[neighbor] != currentFaction && 
                     distance[neighbor] == currentDist + 1)
            {
                // Contested: equidistant from two factions
                ownership[neighbor] = null; // Mark as contested
            }
        }
    }
    
    // Apply ownership to systems
    foreach (var system in world.GetAllSystems())
    {
        if (ownership.TryGetValue(system.Id, out var factionId))
        {
            system.OwningFactionId = factionId;
            if (factionId == null)
            {
                system.Tags.Add(WorldTags.Contested);
            }
        }
    }
    
    // Mark some systems as neutral based on config
    MarkNeutralSystems(world, capitals);
}

/// <summary>
/// Mark frontier systems as neutral.
/// </summary>
private void MarkNeutralSystems(WorldState world, Dictionary<string, int> capitals)
{
    int neutralCount = (int)(world.Systems.Count * config.NeutralFraction);
    var candidates = world.GetAllSystems()
        .Where(s => !capitals.ContainsValue(s.Id))
        .OrderByDescending(s => GetMinDistanceToCapital(s, capitals, world))
        .Take(neutralCount)
        .ToList();
    
    foreach (var system in candidates)
    {
        system.OwningFactionId = null;
        if (!system.HasTag(WorldTags.Contested))
        {
            system.Tags.Add(WorldTags.Frontier);
        }
    }
}
```

**Acceptance Criteria**:
- [ ] Each faction has exactly one capital
- [ ] Capitals are well-spaced
- [ ] Faction territories are contiguous
- [ ] Contested zones at faction boundaries
- [ ] Neutral fraction matches config

---

## Phase 5: System Content

### Step 5.1: Assign System Types

```csharp
/// <summary>
/// Assign system types based on position and faction.
/// </summary>
private void AssignSystemTypes(WorldState world, Dictionary<string, int> capitals)
{
    foreach (var system in world.GetAllSystems())
    {
        // Capitals are always stations
        if (capitals.ContainsValue(system.Id))
        {
            system.Type = SystemType.Station;
            system.Tags.Add(WorldTags.Hub);
            continue;
        }
        
        // Contested systems
        if (system.OwningFactionId == null && system.HasTag(WorldTags.Contested))
        {
            system.Type = SystemType.Contested;
            continue;
        }
        
        // Random type based on weights
        system.Type = SelectSystemType();
    }
}

/// <summary>
/// Select system type using weighted random.
/// </summary>
private SystemType SelectSystemType()
{
    float total = config.SystemTypeWeights.Values.Sum();
    float roll = rng.NextFloat() * total;
    float cumulative = 0f;
    
    foreach (var (type, weight) in config.SystemTypeWeights)
    {
        cumulative += weight;
        if (roll <= cumulative)
            return type;
    }
    
    return SystemType.Outpost; // Fallback
}
```

### Step 5.2: Name Generation

**New File**: `src/sim/generation/NameGenerator.cs`

```csharp
using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// Generates names for systems, stations, and NPCs.
/// </summary>
public static class NameGenerator
{
    private static readonly string[] SystemPrefixes = 
    {
        "New", "Port", "Fort", "Station", "Outpost", "Camp", "Point", "Base"
    };
    
    private static readonly string[] SystemNames = 
    {
        "Haven", "Reach", "Frontier", "Prospect", "Terminus", "Horizon",
        "Vanguard", "Sentinel", "Bastion", "Refuge", "Waypoint", "Crossroads",
        "Anchor", "Beacon", "Gateway", "Threshold", "Meridian", "Apex",
        "Nexus", "Vertex", "Zenith", "Nadir", "Eclipse", "Corona"
    };
    
    private static readonly string[] SystemSuffixes = 
    {
        "Prime", "Alpha", "Beta", "Gamma", "Delta", "VII", "IX", "XII",
        "Station", "Hub", "Post", "Colony", "Depot", "Base"
    };
    
    private static readonly string[] DerelictNames = 
    {
        "Wreck of", "Ruins of", "Remains of", "Hulk of", "Ghost of"
    };
    
    private static readonly string[] AsteroidNames = 
    {
        "Rockfall", "Ironvein", "Dustcloud", "Shatter", "Gravel", "Ore"
    };
    
    private static readonly string[] NebulaNames = 
    {
        "Shroud", "Veil", "Mist", "Haze", "Cloud", "Shadow"
    };
    
    /// <summary>
    /// Generate a system name based on type.
    /// </summary>
    public static string GenerateSystemName(SystemType type, RngStream rng)
    {
        return type switch
        {
            SystemType.Derelict => GenerateDerelictName(rng),
            SystemType.Asteroid => GenerateAsteroidName(rng),
            SystemType.Nebula => GenerateNebulaName(rng),
            _ => GenerateStandardName(rng)
        };
    }
    
    private static string GenerateStandardName(RngStream rng)
    {
        bool usePrefix = rng.NextFloat() < 0.3f;
        bool useSuffix = rng.NextFloat() < 0.4f;
        
        string name = SystemNames[rng.NextInt(SystemNames.Length)];
        
        if (usePrefix)
            name = $"{SystemPrefixes[rng.NextInt(SystemPrefixes.Length)]} {name}";
        
        if (useSuffix)
            name = $"{name} {SystemSuffixes[rng.NextInt(SystemSuffixes.Length)]}";
        
        return name;
    }
    
    private static string GenerateDerelictName(RngStream rng)
    {
        string prefix = DerelictNames[rng.NextInt(DerelictNames.Length)];
        string name = SystemNames[rng.NextInt(SystemNames.Length)];
        return $"{prefix} {name}";
    }
    
    private static string GenerateAsteroidName(RngStream rng)
    {
        string name = AsteroidNames[rng.NextInt(AsteroidNames.Length)];
        string suffix = SystemSuffixes[rng.NextInt(SystemSuffixes.Length)];
        return $"{name} {suffix}";
    }
    
    private static string GenerateNebulaName(RngStream rng)
    {
        string name = NebulaNames[rng.NextInt(NebulaNames.Length)];
        string baseName = SystemNames[rng.NextInt(SystemNames.Length)];
        return $"{baseName} {name}";
    }
    
    /// <summary>
    /// Generate station name (usually matches system name).
    /// </summary>
    public static string GenerateStationName(string systemName, RngStream rng)
    {
        // 70% chance to match system name
        if (rng.NextFloat() < 0.7f)
            return systemName;
        
        // Otherwise generate variant
        string suffix = new[] { "Station", "Dock", "Port", "Hub" }[rng.NextInt(4)];
        return $"{systemName} {suffix}";
    }
}
```

### Step 5.3: Initialize Metrics

```csharp
/// <summary>
/// Initialize system metrics based on type and context.
/// </summary>
private void InitializeMetrics(WorldState world, Dictionary<string, int> capitals)
{
    foreach (var system in world.GetAllSystems())
    {
        // Start with type-based defaults
        system.Metrics = SystemMetrics.ForSystemType(system.Type);
        
        // Capitals get boosted metrics
        if (capitals.ContainsValue(system.Id))
        {
            system.Metrics.Stability = 5;
            system.Metrics.SecurityLevel = 4;
            system.Metrics.EconomicActivity = 4;
            system.Metrics.LawEnforcementPresence = 4;
            system.Metrics.CriminalActivity = 1;
        }
        
        // Frontier systems are less stable
        if (system.HasTag(WorldTags.Frontier))
        {
            system.Metrics.Stability = Math.Max(1, system.Metrics.Stability - 1);
            system.Metrics.SecurityLevel = Math.Max(0, system.Metrics.SecurityLevel - 1);
        }
        
        // Contested systems are unstable
        if (system.HasTag(WorldTags.Contested))
        {
            system.Metrics.Stability = 1;
            system.Metrics.CriminalActivity = Math.Min(5, system.Metrics.CriminalActivity + 2);
        }
        
        // Add some random variance
        ApplyMetricVariance(system.Metrics);
    }
}

/// <summary>
/// Apply small random variance to metrics.
/// </summary>
private void ApplyMetricVariance(SystemMetrics metrics)
{
    // ±1 variance on each metric
    metrics.Stability = Clamp(metrics.Stability + rng.NextInt(-1, 2), 0, 5);
    metrics.SecurityLevel = Clamp(metrics.SecurityLevel + rng.NextInt(-1, 2), 0, 5);
    metrics.CriminalActivity = Clamp(metrics.CriminalActivity + rng.NextInt(-1, 2), 0, 5);
    metrics.EconomicActivity = Clamp(metrics.EconomicActivity + rng.NextInt(-1, 2), 0, 5);
    metrics.LawEnforcementPresence = Clamp(metrics.LawEnforcementPresence + rng.NextInt(-1, 2), 0, 5);
}

private static int Clamp(int value, int min, int max) => 
    Math.Max(min, Math.Min(max, value));
```

### Step 5.4: Assign System Tags

```csharp
/// <summary>
/// Assign tags based on system properties.
/// </summary>
private void AssignSystemTags(WorldState world)
{
    foreach (var system in world.GetAllSystems())
    {
        // Type-based tags
        switch (system.Type)
        {
            case SystemType.Asteroid:
                system.Tags.Add(WorldTags.Mining);
                if (system.Metrics.EconomicActivity >= 4)
                    system.Tags.Add(WorldTags.Industrial);
                break;
                
            case SystemType.Derelict:
                system.Tags.Add(WorldTags.Frontier);
                break;
                
            case SystemType.Nebula:
                // Nebulas often hide illegal activity
                if (system.Metrics.CriminalActivity >= 3)
                    system.Tags.Add(WorldTags.Lawless);
                break;
        }
        
        // Metric-based tags
        if (system.Metrics.SecurityLevel >= 4)
            system.Tags.Add(WorldTags.Military);
        
        if (system.Metrics.CriminalActivity >= 4 && system.Metrics.SecurityLevel <= 1)
            system.Tags.Add(WorldTags.Lawless);
        
        if (system.Metrics.CriminalActivity >= 5)
            system.Tags.Add(WorldTags.PirateHaven);
    }
}
```

**Acceptance Criteria**:
- [ ] All systems have names
- [ ] Names match system type
- [ ] Metrics initialized from type
- [ ] Capitals have boosted metrics
- [ ] Tags assigned based on metrics

---

## Phase 6: Station Generation

### Step 6.1: Create Stations

```csharp
/// <summary>
/// Create stations for inhabited systems.
/// </summary>
private void GenerateStations(WorldState world)
{
    foreach (var system in world.GetAllSystems())
    {
        if (!config.InhabitedTypes.Contains(system.Type))
            continue;
        
        // Skip derelicts and contested zones
        if (system.Type == SystemType.Derelict || system.Type == SystemType.Contested)
            continue;
        
        var station = CreateStationForSystem(world, system);
        if (station != null)
        {
            world.AddStation(station);
        }
    }
}

/// <summary>
/// Create appropriate station for system type.
/// </summary>
private Station CreateStationForSystem(WorldState world, StarSystem system)
{
    int stationId = world.GenerateStationId();
    string stationName = NameGenerator.GenerateStationName(system.Name, rng);
    
    return system.Type switch
    {
        SystemType.Station when system.HasTag(WorldTags.Hub) => 
            Station.CreateHub(stationId, stationName, system.Id, system.OwningFactionId),
            
        SystemType.Station when system.HasTag(WorldTags.Military) =>
            Station.CreateMilitary(stationId, stationName, system.Id, system.OwningFactionId),
            
        SystemType.Station =>
            Station.CreateOutpost(stationId, stationName, system.Id, system.OwningFactionId),
            
        SystemType.Outpost when system.HasTag(WorldTags.Lawless) =>
            Station.CreatePirateDen(stationId, stationName, system.Id, system.OwningFactionId),
            
        SystemType.Outpost =>
            Station.CreateOutpost(stationId, stationName, system.Id, system.OwningFactionId),
            
        SystemType.Asteroid =>
            Station.CreateMining(stationId, stationName, system.Id, system.OwningFactionId),
            
        SystemType.Nebula when system.HasTag(WorldTags.Lawless) =>
            Station.CreateBlackMarket(stationId, stationName, system.Id, system.OwningFactionId),
            
        SystemType.Nebula =>
            Station.CreateOutpost(stationId, stationName, system.Id, system.OwningFactionId),
            
        _ => null
    };
}
```

**Acceptance Criteria**:
- [ ] All inhabited systems have stations
- [ ] Station type matches system type
- [ ] Station names generated
- [ ] Facilities appropriate for type

---

## Phase 7: Main Generator Class

### Step 7.1: GalaxyGenerator Class

**New File**: `src/sim/generation/GalaxyGenerator.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace FringeTactics;

/// <summary>
/// Procedurally generates a galaxy/sector for a new campaign.
/// </summary>
public class GalaxyGenerator
{
    private readonly GalaxyConfig config;
    private readonly RngStream rng;
    
    public GalaxyGenerator(GalaxyConfig config, RngStream rng)
    {
        this.config = config ?? throw new ArgumentNullException(nameof(config));
        this.rng = rng ?? throw new ArgumentNullException(nameof(rng));
    }
    
    /// <summary>
    /// Generate a complete world state.
    /// </summary>
    public WorldState Generate()
    {
        SimLog.Log($"[GalaxyGenerator] Generating sector with {config.SystemCount} systems");
        
        var world = new WorldState
        {
            Name = GenerateSectorName()
        };
        
        // Load factions from registry
        foreach (var faction in FactionRegistry.GetAll())
        {
            world.AddFaction(CloneFaction(faction));
        }
        
        // Phase 1: Generate positions
        var positions = GeneratePositions();
        SimLog.Log($"[GalaxyGenerator] Placed {positions.Count} systems");
        
        // Phase 2: Create systems (without content yet)
        for (int i = 0; i < positions.Count; i++)
        {
            var system = new StarSystem(i, $"System_{i}", SystemType.Outpost, positions[i]);
            world.AddSystem(system);
        }
        
        // Phase 3: Generate routes
        var mstEdges = BuildMST(positions);
        var extraEdges = AddExtraRoutes(positions, mstEdges);
        var allEdges = mstEdges.Concat(extraEdges).ToList();
        CreateRoutes(world, positions, allEdges);
        SimLog.Log($"[GalaxyGenerator] Created {allEdges.Count} routes");
        
        // Phase 4: Assign factions
        var capitals = PlaceFactionCapitals(positions);
        AssignFactionOwnership(world, capitals);
        
        // Phase 5: Assign system content
        AssignSystemTypes(world, capitals);
        AssignSystemNames(world);
        InitializeMetrics(world, capitals);
        AssignSystemTags(world);
        
        // Phase 6: Update route hazards based on systems
        UpdateRouteHazards(world);
        
        // Phase 7: Generate stations
        GenerateStations(world);
        SimLog.Log($"[GalaxyGenerator] Created {world.Stations.Count} stations");
        
        return world;
    }
    
    /// <summary>
    /// Generate a sector name.
    /// </summary>
    private string GenerateSectorName()
    {
        string[] prefixes = { "Outer", "Inner", "Far", "Near", "Deep", "High" };
        string[] names = { "Reach", "Frontier", "Expanse", "Sector", "Rim", "Cluster" };
        
        return $"{prefixes[rng.NextInt(prefixes.Length)]} {names[rng.NextInt(names.Length)]}";
    }
    
    /// <summary>
    /// Assign names to all systems.
    /// </summary>
    private void AssignSystemNames(WorldState world)
    {
        var usedNames = new HashSet<string>();
        
        foreach (var system in world.GetAllSystems())
        {
            string name;
            int attempts = 0;
            
            do
            {
                name = NameGenerator.GenerateSystemName(system.Type, rng);
                attempts++;
            } while (usedNames.Contains(name) && attempts < 10);
            
            usedNames.Add(name);
            system.Name = name;
        }
    }
    
    /// <summary>
    /// Update route hazards based on endpoint systems.
    /// </summary>
    private void UpdateRouteHazards(WorldState world)
    {
        foreach (var route in world.GetAllRoutes())
        {
            var systemA = world.GetSystem(route.SystemA);
            var systemB = world.GetSystem(route.SystemB);
            
            if (systemA == null || systemB == null) continue;
            
            // Base hazard from system types
            int hazard = 0;
            
            // Dangerous system types increase hazard
            if (systemA.Type == SystemType.Contested || systemB.Type == SystemType.Contested)
                hazard += 2;
            if (systemA.Type == SystemType.Derelict || systemB.Type == SystemType.Derelict)
                hazard += 1;
            if (systemA.Type == SystemType.Nebula || systemB.Type == SystemType.Nebula)
                hazard += 1;
            
            // Criminal activity increases hazard
            int maxCrime = Math.Max(
                systemA.Metrics?.CriminalActivity ?? 0,
                systemB.Metrics?.CriminalActivity ?? 0);
            hazard += maxCrime / 2;
            
            // Security reduces hazard
            int minSecurity = Math.Min(
                systemA.Metrics?.SecurityLevel ?? 0,
                systemB.Metrics?.SecurityLevel ?? 0);
            hazard -= minSecurity / 2;
            
            route.HazardLevel = Math.Clamp(hazard, 0, 5);
            
            // Add route tags
            if (route.HazardLevel >= 3)
                route.Tags.Add(WorldTags.Dangerous);
            if (minSecurity >= 4)
                route.Tags.Add(WorldTags.Patrolled);
            if (systemA.Type == SystemType.Asteroid || systemB.Type == SystemType.Asteroid)
                route.Tags.Add(WorldTags.Asteroid);
            if (systemA.Type == SystemType.Nebula || systemB.Type == SystemType.Nebula)
                route.Tags.Add(WorldTags.Hidden);
        }
    }
    
    private static Faction CloneFaction(Faction source)
    {
        return new Faction
        {
            Id = source.Id,
            Name = source.Name,
            Description = source.Description,
            Metrics = source.Metrics?.Clone()
        };
    }
    
    // ... (include all the helper methods from previous phases)
}
```

### Step 7.2: Integration with CampaignState

**Update `CampaignState.CreateNew()`**:

```csharp
/// <summary>
/// Create a new campaign with generated world.
/// </summary>
public static CampaignState CreateNew(int sectorSeed = 12345)
{
    var starting = Config.Starting;
    var campaign = new CampaignState
    {
        Money = starting.Money,
        Fuel = starting.Fuel,
        Parts = starting.Parts,
        Meds = starting.Meds,
        Ammo = starting.Ammo,
        Time = new CampaignTime(),
        Rng = new RngService(sectorSeed)
    };
    
    // Generate world using GalaxyGenerator
    var galaxyConfig = GalaxyConfig.Default;
    var generator = new GalaxyGenerator(galaxyConfig, campaign.Rng.Campaign);
    campaign.World = generator.Generate();
    
    // Find starting system (first hub)
    var startSystem = campaign.World.GetAllSystems()
        .FirstOrDefault(s => s.HasTag(WorldTags.Hub));
    campaign.CurrentNodeId = startSystem?.Id ?? 0;
    
    // Create starter ship
    campaign.Ship = Ship.CreateStarter();
    
    // Initialize faction reputation
    foreach (var factionId in campaign.World.Factions.Keys)
    {
        campaign.FactionRep[factionId] = 50;
    }
    
    // Add starting crew
    campaign.AddCrew("Alex", CrewRole.Soldier);
    campaign.AddCrew("Jordan", CrewRole.Soldier);
    campaign.AddCrew("Morgan", CrewRole.Medic);
    campaign.AddCrew("Casey", CrewRole.Tech);
    
    // Generate initial jobs
    campaign.RefreshJobsAtCurrentNode();
    
    return campaign;
}
```

**Acceptance Criteria**:
- [ ] `GalaxyGenerator.Generate()` returns valid `WorldState`
- [ ] `CampaignState.CreateNew()` uses generator
- [ ] Starting location is a hub system
- [ ] All existing tests still pass

---

## Phase 8: Testing

### Test File: `tests/sim/generation/GN2GalaxyGeneratorTests.cs`

```csharp
using GdUnit4;
using static GdUnit4.Assertions;
using System.Linq;

namespace FringeTactics.Tests;

[TestSuite]
public class GN2GalaxyGeneratorTests
{
    [TestCase]
    public void Generate_CreatesRequestedSystemCount()
    {
        var config = new GalaxyConfig { SystemCount = 10 };
        var rng = new RngService(12345).Campaign;
        var generator = new GalaxyGenerator(config, rng);
        
        var world = generator.Generate();
        
        AssertInt(world.Systems.Count).IsEqual(10);
    }
    
    [TestCase]
    public void Generate_AllSystemsConnected()
    {
        var config = GalaxyConfig.Default;
        var rng = new RngService(12345).Campaign;
        var generator = new GalaxyGenerator(config, rng);
        
        var world = generator.Generate();
        
        // Check all systems reachable from system 0
        var firstSystem = world.GetAllSystems().First();
        foreach (var system in world.GetAllSystems())
        {
            AssertBool(world.IsReachable(firstSystem.Id, system.Id)).IsTrue();
        }
    }
    
    [TestCase]
    public void Generate_SystemsRespectMinDistance()
    {
        var config = new GalaxyConfig { MinSystemDistance = 80f };
        var rng = new RngService(12345).Campaign;
        var generator = new GalaxyGenerator(config, rng);
        
        var world = generator.Generate();
        var systems = world.GetAllSystems().ToList();
        
        for (int i = 0; i < systems.Count; i++)
        {
            for (int j = i + 1; j < systems.Count; j++)
            {
                float dist = systems[i].Position.DistanceTo(systems[j].Position);
                AssertFloat(dist).IsGreaterEqual(config.MinSystemDistance);
            }
        }
    }
    
    [TestCase]
    public void Generate_EachFactionHasCapital()
    {
        var config = GalaxyConfig.Default;
        var rng = new RngService(12345).Campaign;
        var generator = new GalaxyGenerator(config, rng);
        
        var world = generator.Generate();
        
        foreach (var factionId in config.FactionIds)
        {
            var factionSystems = world.GetSystemsByFaction(factionId).ToList();
            var hubs = factionSystems.Where(s => s.HasTag(WorldTags.Hub)).ToList();
            AssertInt(hubs.Count).IsGreaterEqual(1);
        }
    }
    
    [TestCase]
    public void Generate_InhabitedSystemsHaveStations()
    {
        var config = GalaxyConfig.Default;
        var rng = new RngService(12345).Campaign;
        var generator = new GalaxyGenerator(config, rng);
        
        var world = generator.Generate();
        
        foreach (var system in world.GetAllSystems())
        {
            if (config.InhabitedTypes.Contains(system.Type))
            {
                var stations = world.GetStationsInSystem(system.Id).ToList();
                AssertInt(stations.Count).IsGreaterEqual(1);
            }
        }
    }
}
```

### Test File: `tests/sim/generation/GN2DeterminismTests.cs`

```csharp
using GdUnit4;
using static GdUnit4.Assertions;
using System.Linq;

namespace FringeTactics.Tests;

[TestSuite]
public class GN2DeterminismTests
{
    [TestCase]
    public void SameSeed_ProducesSameGalaxy()
    {
        var config = GalaxyConfig.Default;
        
        var rng1 = new RngService(12345).Campaign;
        var generator1 = new GalaxyGenerator(config, rng1);
        var world1 = generator1.Generate();
        
        var rng2 = new RngService(12345).Campaign;
        var generator2 = new GalaxyGenerator(config, rng2);
        var world2 = generator2.Generate();
        
        // Same number of systems
        AssertInt(world1.Systems.Count).IsEqual(world2.Systems.Count);
        
        // Same system names and positions
        var systems1 = world1.GetAllSystems().OrderBy(s => s.Id).ToList();
        var systems2 = world2.GetAllSystems().OrderBy(s => s.Id).ToList();
        
        for (int i = 0; i < systems1.Count; i++)
        {
            AssertString(systems1[i].Name).IsEqual(systems2[i].Name);
            AssertFloat(systems1[i].Position.X).IsEqual(systems2[i].Position.X);
            AssertFloat(systems1[i].Position.Y).IsEqual(systems2[i].Position.Y);
        }
    }
    
    [TestCase]
    public void DifferentSeeds_ProducesDifferentGalaxies()
    {
        var config = GalaxyConfig.Default;
        
        var rng1 = new RngService(12345).Campaign;
        var generator1 = new GalaxyGenerator(config, rng1);
        var world1 = generator1.Generate();
        
        var rng2 = new RngService(54321).Campaign;
        var generator2 = new GalaxyGenerator(config, rng2);
        var world2 = generator2.Generate();
        
        // Should have different system names
        var names1 = world1.GetAllSystems().Select(s => s.Name).ToHashSet();
        var names2 = world2.GetAllSystems().Select(s => s.Name).ToHashSet();
        
        // Not all names should match
        int matching = names1.Intersect(names2).Count();
        AssertInt(matching).IsLess(names1.Count);
    }
    
    [TestCase]
    public void SameSeed_SameRoutes()
    {
        var config = GalaxyConfig.Default;
        
        var rng1 = new RngService(99999).Campaign;
        var generator1 = new GalaxyGenerator(config, rng1);
        var world1 = generator1.Generate();
        
        var rng2 = new RngService(99999).Campaign;
        var generator2 = new GalaxyGenerator(config, rng2);
        var world2 = generator2.Generate();
        
        // Same number of routes
        AssertInt(world1.Routes.Count).IsEqual(world2.Routes.Count);
        
        // Same route IDs
        var routeIds1 = world1.Routes.Keys.OrderBy(k => k).ToList();
        var routeIds2 = world2.Routes.Keys.OrderBy(k => k).ToList();
        
        for (int i = 0; i < routeIds1.Count; i++)
        {
            AssertInt(routeIds1[i]).IsEqual(routeIds2[i]);
        }
    }
}
```

### Test File: `tests/sim/generation/GN2ConfigTests.cs`

```csharp
using GdUnit4;
using static GdUnit4.Assertions;

namespace FringeTactics.Tests;

[TestSuite]
public class GN2ConfigTests
{
    [TestCase]
    public void DefaultConfig_HasValidValues()
    {
        var config = GalaxyConfig.Default;
        
        AssertInt(config.SystemCount).IsGreater(0);
        AssertFloat(config.MinSystemDistance).IsGreater(0);
        AssertFloat(config.MapWidth).IsGreater(config.MinSystemDistance);
        AssertFloat(config.MapHeight).IsGreater(config.MinSystemDistance);
    }
    
    [TestCase]
    public void SmallConfig_HasFewerSystems()
    {
        var small = GalaxyConfig.Small;
        var def = GalaxyConfig.Default;
        
        AssertInt(small.SystemCount).IsLess(def.SystemCount);
    }
    
    [TestCase]
    public void LargeConfig_HasMoreSystems()
    {
        var large = GalaxyConfig.Large;
        var def = GalaxyConfig.Default;
        
        AssertInt(large.SystemCount).IsGreater(def.SystemCount);
    }
    
    [TestCase]
    public void SystemTypeWeights_SumToApproximatelyOne()
    {
        var config = GalaxyConfig.Default;
        float sum = 0f;
        
        foreach (var weight in config.SystemTypeWeights.Values)
        {
            sum += weight;
        }
        
        AssertFloat(sum).IsBetween(0.9f, 1.1f);
    }
}
```

---

## Manual Test Setup

### Scenario 1: Basic Generation

1. Start new campaign with seed 12345
2. Open sector map
3. **Verify**:
   - Multiple systems visible (default: 12)
   - Systems have unique names
   - Routes connect systems
   - Player starts at a hub system

### Scenario 2: Determinism

1. Start campaign with seed 12345
2. Note first 3 system names and positions
3. Restart with same seed
4. **Verify**: Same 3 systems appear at same positions

### Scenario 3: Connectivity

1. Start new campaign
2. Open sector map
3. **Verify**: All systems reachable from starting location
4. Use pathfinding to verify routes exist

### Scenario 4: Faction Territories

1. Start new campaign
2. Open sector map with faction overlay
3. **Verify**:
   - Each faction has contiguous territory
   - Faction capitals are hubs
   - Contested zones at faction boundaries

### Scenario 5: Different Seeds

1. Start campaign with seed 12345
2. Note sector layout
3. Start new campaign with seed 54321
4. **Verify**: Different sector layout, different names

---

## Files Summary

### Files to Create

| File | Purpose |
|------|---------|
| `src/sim/generation/GalaxyConfig.cs` | Generation configuration |
| `src/sim/generation/GalaxyGenerator.cs` | Main generator class |
| `src/sim/generation/NameGenerator.cs` | Name generation utilities |
| `tests/sim/generation/GN2GalaxyGeneratorTests.cs` | Generator tests |
| `tests/sim/generation/GN2DeterminismTests.cs` | Determinism tests |
| `tests/sim/generation/GN2ConfigTests.cs` | Config tests |

### Files to Modify

| File | Changes |
|------|---------|
| `src/sim/campaign/CampaignState.cs` | Use `GalaxyGenerator` in `CreateNew()` |
| `src/sim/generation/agents.md` | Document new files |

---

## Implementation Order

1. **GalaxyConfig.cs** - Configuration class
2. **NameGenerator.cs** - Name generation utilities
3. **GalaxyGenerator.cs** - Main generator (phases 2-6)
4. **Update CampaignState.cs** - Integration
5. **GN2ConfigTests.cs** - Config tests
6. **GN2GalaxyGeneratorTests.cs** - Generator tests
7. **GN2DeterminismTests.cs** - Determinism tests
8. **Manual verification**

---

## Success Criteria

- [ ] `GalaxyConfig` with presets (Default, Small, Large)
- [ ] `GalaxyGenerator` produces valid `WorldState`
- [ ] All systems connected (MST guarantee)
- [ ] Systems respect minimum distance
- [ ] Each faction has capital hub
- [ ] Faction territories are contiguous
- [ ] Inhabited systems have stations
- [ ] Deterministic (same seed = same galaxy)
- [ ] `CampaignState.CreateNew()` uses generator
- [ ] All existing tests still pass
- [ ] All new tests pass

---

## Open Questions

### For Implementation

- Should we support multiple station per system? (Current: 1 per inhabited system)
- Should derelict systems sometimes have stations? (Current: no)
- How many extra routes beyond MST? (Current: probabilistic based on distance)

### For Future (GN3+)

- How do we seed initial contracts across the galaxy?
- Should some routes be "hidden" until discovered?
- How do faction relationships affect initial layout?

---

## Appendix: Algorithm Complexity

| Algorithm | Complexity | Notes |
|-----------|------------|-------|
| Position generation | O(n²) | Rejection sampling with distance checks |
| MST (Prim's) | O(n²) | Simple implementation, n = system count |
| Extra routes | O(n²) | Check all pairs |
| Faction flood fill | O(n) | BFS from capitals |
| Total | O(n²) | Acceptable for n < 100 |

For larger galaxies (n > 50), consider spatial indexing for position generation.
