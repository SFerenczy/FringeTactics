# WD1 – Single Hub World: Implementation Plan

**Status**: ✅ Complete

**Goal**: Implement minimal world with one system and one station for the G1 jobbing loop.

**Phase**: G1

---

## Current State Assessment

### What We Have

| Component | Status | Notes |
|-----------|--------|-------|
| `Sector` | ✅ Exists | Graph of nodes with connections |
| `SectorNode` | ✅ Exists | Basic node with type, faction, position |
| `NodeType` enum | ✅ Exists | Station, Outpost, Derelict, etc. |
| `Sector.Factions` | ✅ Exists | Dictionary of factionId → name |

### Gaps

| WD1 Requirement | Current Status |
|-----------------|----------------|
| `WorldState` class | ❌ Missing |
| `Station` class | ❌ Missing |
| `Facility` class | ❌ Missing |
| `SystemMetrics` | ❌ Missing |
| `Faction` class (full) | ⚠️ Partial |

---

## Architecture Decisions

### WorldState as Wrapper

Create `WorldState` as a wrapper that owns the existing `Sector` and adds new capabilities.

```
WorldState
├── Dictionary<int, StarSystem> Systems
├── Dictionary<int, Station> Stations
├── Dictionary<string, Faction> Factions
└── Query APIs
```

### Minimal G1 Scope

**In scope**: One system, one station, 6 facilities, static metrics, basic queries.

**Out of scope**: Multiple systems, routes, dynamic metrics.

---

## Implementation Steps

### Phase 1: Core Data Structures

#### Step 1.1: SystemMetrics Class

**File**: `src/sim/world/SystemMetrics.cs`

```csharp
public class SystemMetrics
{
    public int Stability { get; set; } = 3;           // 0-5
    public int SecurityLevel { get; set; } = 3;       // 0-5
    public int CriminalActivity { get; set; } = 2;    // 0-5
    public int EconomicActivity { get; set; } = 3;    // 0-5
    public int LawEnforcementPresence { get; set; } = 3; // 0-5
    
    public static SystemMetrics ForSystemType(SystemType type);
    public SystemMetricsData GetState();
    public static SystemMetrics FromState(SystemMetricsData data);
}
```

**Acceptance Criteria**:
- [ ] 5 metric properties with 0-5 range
- [ ] `ForSystemType()` factory with defaults per WD0
- [ ] Serialization support

---

#### Step 1.2: SystemType Enum

**File**: `src/sim/world/SystemType.cs`

```csharp
public enum SystemType
{
    Station, Outpost, Derelict, Asteroid, Nebula, Contested
}

public static class SystemTypeExtensions
{
    public static SystemType ToSystemType(this NodeType nodeType);
    public static NodeType ToNodeType(this SystemType systemType);
}
```

---

#### Step 1.3: Facility Class

**File**: `src/sim/world/Facility.cs`

```csharp
public enum FacilityType
{
    Shop, Bar, MissionBoard, RepairYard, 
    Recruitment, Medical, BlackMarket, FuelDepot
}

public class Facility
{
    public FacilityType Type { get; set; }
    public int Level { get; set; } = 1;           // 1-3
    public HashSet<string> Tags { get; set; }
    public bool Available { get; set; } = true;
}
```

---

#### Step 1.4: Station Class

**File**: `src/sim/world/Station.cs`

```csharp
public class Station
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int SystemId { get; set; }
    public string OwningFactionId { get; set; }
    public List<Facility> Facilities { get; set; }
    public HashSet<string> Tags { get; set; }
    
    public bool HasFacility(FacilityType type);
    public Facility GetFacility(FacilityType type);
    public static Station CreateHub(int id, string name, int systemId, string factionId);
}
```

---

#### Step 1.5: Faction Class

**File**: `src/sim/world/Faction.cs`

```csharp
public enum FactionType { Corporate, Government, Criminal, Independent, Neutral }

public class Faction
{
    public string Id { get; set; }
    public string Name { get; set; }
    public FactionType Type { get; set; }
    public Color Color { get; set; }
    public int HostilityDefault { get; set; } = 50;
    public FactionMetrics Metrics { get; set; }
}

public class FactionMetrics
{
    public int MilitaryStrength { get; set; } = 3;
    public int EconomicPower { get; set; } = 3;
    public int Influence { get; set; } = 3;
    public int Desperation { get; set; } = 1;
    public int Corruption { get; set; } = 2;
}
```

---

#### Step 1.6: WorldTags Constants

**File**: `src/sim/world/WorldTags.cs`

```csharp
public static class WorldTags
{
    // System tags
    public const string Frontier = "frontier";
    public const string Core = "core";
    public const string Hub = "hub";
    public const string Lawless = "lawless";
    // ... etc
    
    // Station tags
    public const string TradeHub = "trade_hub";
    public const string BlackMarket = "black_market";
    // ... etc
}
```

---

### Phase 2: WorldState Implementation

#### Step 2.1: StarSystem Class

**File**: `src/sim/world/StarSystem.cs`

```csharp
public class StarSystem
{
    public int Id { get; set; }
    public string Name { get; set; }
    public SystemType Type { get; set; }
    public Vector2 Position { get; set; }
    public List<int> Connections { get; set; }
    public string OwningFactionId { get; set; }
    public SystemMetrics Metrics { get; set; }
    public HashSet<string> Tags { get; set; }
    public List<int> StationIds { get; set; }
    
    public bool HasTag(string tag);
    public static StarSystem FromSectorNode(SectorNode node);
}
```

---

#### Step 2.2: WorldState Class

**File**: `src/sim/world/WorldState.cs`

```csharp
public class WorldState
{
    public string Name { get; set; }
    public Dictionary<int, StarSystem> Systems { get; }
    public Dictionary<int, Station> Stations { get; }
    public Dictionary<string, Faction> Factions { get; }
    
    // System queries
    public StarSystem GetSystem(int systemId);
    public IEnumerable<StarSystem> GetAllSystems();
    public IEnumerable<StarSystem> GetSystemsByFaction(string factionId);
    public IEnumerable<StarSystem> GetSystemsByTag(string tag);
    public SystemMetrics GetSystemMetrics(int systemId);
    public int GetSecurityLevel(int systemId);
    public bool HasTag(int systemId, string tag);
    
    // Station queries
    public Station GetStation(int stationId);
    public IEnumerable<Station> GetStationsInSystem(int systemId);
    public IEnumerable<Facility> GetFacilities(int stationId);
    public bool HasFacility(int stationId, FacilityType type);
    public Station GetPrimaryStation(int systemId);
    
    // Faction queries
    public Faction GetFaction(string factionId);
    public IEnumerable<Faction> GetAllFactions();
    
    // Factory methods
    public static WorldState CreateSingleHub(string hubName = "Haven Station", string factionId = "corp");
    public static WorldState FromSector(Sector sector);
}
```

---

### Phase 3: Integration

#### Step 3.1: Add WorldState to CampaignState

**File**: `src/sim/campaign/CampaignState.cs`

**Changes**:
```csharp
// Add property
public WorldState World { get; set; }

// In CreateNew()
campaign.World = WorldState.CreateSingleHub("Haven Station", "corp");

// In GetState()
data.World = World?.GetState();

// In FromState()
campaign.World = data.World != null ? WorldState.FromState(data.World) : null;
```

---

#### Step 3.2: Update SaveData

**File**: `src/sim/data/SaveData.cs`

```csharp
// Add to CampaignStateData
public WorldStateData World { get; set; }
```

---

## Files to Create

| File | Purpose |
|------|---------|
| `src/sim/world/WorldState.cs` | Main world container |
| `src/sim/world/StarSystem.cs` | System with metrics |
| `src/sim/world/Station.cs` | Station with facilities |
| `src/sim/world/Facility.cs` | Facility type and properties |
| `src/sim/world/Faction.cs` | Full faction data |
| `src/sim/world/SystemMetrics.cs` | System-level metrics |
| `src/sim/world/SystemType.cs` | System type enum |
| `src/sim/world/WorldTags.cs` | Tag constants |
| `src/sim/world/agents.md` | Directory documentation |
| `tests/sim/world/WorldStateTests.cs` | Unit tests |

---

## Files to Modify

| File | Changes |
|------|---------|
| `src/sim/campaign/CampaignState.cs` | Add `WorldState World` property |
| `src/sim/data/SaveData.cs` | Add `WorldStateData` to save data |

---

## Unit Tests

**File**: `tests/sim/world/WorldStateTests.cs`

```csharp
[TestClass]
public class WorldStateTests
{
    [TestMethod]
    public void CreateSingleHub_CreatesValidWorld()
    {
        var world = WorldState.CreateSingleHub("Test Hub", "corp");
        Assert.AreEqual(1, world.Systems.Count);
        Assert.AreEqual(1, world.Stations.Count);
        Assert.AreEqual(3, world.Factions.Count);
    }
    
    [TestMethod]
    public void CreateSingleHub_StationHasFacilities()
    {
        var world = WorldState.CreateSingleHub();
        var station = world.GetPrimaryStation(0);
        Assert.IsTrue(station.HasFacility(FacilityType.Shop));
        Assert.IsTrue(station.HasFacility(FacilityType.MissionBoard));
        Assert.IsTrue(station.HasFacility(FacilityType.RepairYard));
    }
    
    [TestMethod]
    public void GetSystemMetrics_ReturnsCorrectValues()
    {
        var world = WorldState.CreateSingleHub();
        Assert.AreEqual(4, world.GetSecurityLevel(0));
    }
    
    [TestMethod]
    public void Serialization_RoundTrip_PreservesData()
    {
        var world = WorldState.CreateSingleHub();
        var data = world.GetState();
        var restored = WorldState.FromState(data);
        Assert.AreEqual(world.Systems.Count, restored.Systems.Count);
    }
}
```

---

## Manual Testing Checklist

### World Creation
- [ ] `WorldState` created with new campaign
- [ ] Single hub system exists with metrics
- [ ] Station has 6 facilities
- [ ] 3 factions initialized

### Save/Load
- [ ] WorldState serialized correctly
- [ ] WorldState restored correctly
- [ ] Metrics and facilities preserved

---

## Success Criteria

1. ✅ `WorldState` class with all query APIs
2. ✅ `CreateSingleHub()` creates valid single-hub world
3. ✅ Hub has one system with metrics and tags
4. ✅ Hub has one station with 6 facilities
5. ✅ Three factions with metrics
6. ✅ `CampaignState.World` initialized and serialized
7. ✅ All unit tests pass

---

## Implementation Order

1. **Phase 1** (Days 1-3): Core data structures
   - SystemType, WorldTags, SystemMetrics
   - Facility, Station, Faction

2. **Phase 2** (Days 4-5): WorldState
   - StarSystem, WorldState with all APIs

3. **Phase 3** (Days 6-7): Integration & Testing
   - CampaignState integration
   - Unit tests, manual testing

---

## Dependencies

**WD1 Depends On**: SF0-SF3 (complete)

**WD1 Enables**: MG1, GN1, MG3

---

## Appendix: Default Metrics by System Type

| SystemType | Stability | Security | Criminal | Economic | LawEnforcement |
|------------|-----------|----------|----------|----------|----------------|
| Station | 4 | 4 | 1 | 4 | 4 |
| Outpost | 3 | 2 | 2 | 2 | 2 |
| Derelict | 1 | 0 | 3 | 0 | 0 |
| Asteroid | 2 | 1 | 2 | 3 | 1 |
| Nebula | 2 | 0 | 3 | 1 | 0 |
| Contested | 1 | 1 | 4 | 2 | 1 |

---

## Appendix: Hub Station Facilities

| Facility | Level | Notes |
|----------|-------|-------|
| Shop | 2 | Buy/sell equipment |
| MissionBoard | 2 | Accept contracts |
| RepairYard | 1 | Repair hull |
| Bar | 1 | Rumors, morale |
| Recruitment | 1 | Hire crew |
| FuelDepot | 1 | Buy fuel |
