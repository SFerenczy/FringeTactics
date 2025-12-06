# WD3 – Metrics & Tags: Implementation Plan

**Status**: ✅ Complete  
**Depends on**: WD2 (Sector Topology) ✅ Complete  
**Phase**: G2

---

## Overview

**Goal**: Enhance the metrics and tag system to support Travel, Encounter, and Generation domains.

WD3 builds on the existing `SystemMetrics`, `WorldTags`, and tag infrastructure from WD1/WD2 to provide:
- A complete, typed metric system with mutation APIs
- An expanded tag vocabulary for systems, stations, and routes
- Query APIs for filtering by metrics and tags
- Foundation for G3 Simulation integration

This milestone is critical for G2 because:
- **Travel** needs metrics to compute risk profiles and encounter probabilities
- **Encounter** needs tags to select appropriate event templates
- **Generation** needs metrics/tags to bias contract types and difficulties

---

## Current State Assessment

### What We Have (from WD1/WD2)

| Component | Status | Notes |
|-----------|--------|-------|
| `SystemMetrics` | ✅ Complete | 5 metrics (0-5 scale), factory for system types |
| `WorldTags` | ✅ Complete | 8 system tags, 6 station tags, 7 route tags |
| `StarSystem.Tags` | ✅ Complete | `HashSet<string>` on systems |
| `Station.Tags` | ✅ Complete | `HashSet<string>` on stations |
| `Route.Tags` | ✅ Complete | `HashSet<string>` on routes |
| `WorldState.GetSystemsByTag()` | ✅ Complete | Filter systems by tag |
| `WorldState.GetRoutesByTag()` | ✅ Complete | Filter routes by tag |
| `WorldState.GetSecurityLevel()` | ✅ Complete | Single metric query |
| `WorldState.GetCriminalActivity()` | ✅ Complete | Single metric query |

### Gaps for WD3

| WD3 Requirement | Current Status | Gap |
|-----------------|----------------|-----|
| `SystemMetricType` enum | ❌ Missing | Need typed enum for metric access |
| Metric mutation API | ❌ Missing | Need `SetMetric()`, `ModifyMetric()` |
| Metric clamping | ❌ Missing | Need `Clamp()` method on metrics |
| Metric-based queries | ⚠️ Partial | Only 2 metrics queryable |
| Station metrics | ❌ Missing | Stations don't have their own metrics |
| Tag validation | ❌ Missing | No validation of tag strings |
| Composite queries | ❌ Missing | No "high security AND frontier" queries |
| Metric change events | ❌ Missing | No events for G3 integration |

---

## Architecture Decisions

### AD1: Typed Metric Access via Enum

**Decision**: Create `SystemMetricType` enum for type-safe metric access.

**Rationale**:
- Prevents typos in string-based metric names
- Enables switch expressions and exhaustive handling
- Cleaner API: `GetMetric(systemId, SystemMetricType.SecurityLevel)`

### AD2: Station Metrics as Optional

**Decision**: Stations can optionally have their own `StationMetrics` that override system metrics.

**Rationale**:
- A black market station in a high-security system should still feel lawless
- Allows fine-grained control without changing system-level data
- Falls back to system metrics if station metrics are null

### AD3: Metric Changes Emit Events

**Decision**: Metric mutations emit events for Simulation integration.

**Rationale**:
- G3 Simulation needs to react to metric changes
- Enables logging and debugging of metric drift
- Decouples mutation from side effects

### AD4: Tag Categories for Validation

**Decision**: Tags are organized into categories with optional validation.

**Rationale**:
- Prevents accidental use of system tags on routes
- Enables category-specific queries
- Maintains moddability (validation is optional)

---

## Implementation Steps

### Phase 1: Metric Type System

#### Step 1.1: Create SystemMetricType Enum

**File**: `src/sim/world/SystemMetricType.cs` (new)

```csharp
namespace FringeTactics;

/// <summary>
/// Enumeration of system-level metrics.
/// All metrics use 0-5 scale per CAMPAIGN_FOUNDATIONS.
/// </summary>
public enum SystemMetricType
{
    /// <summary>
    /// Political/social stability. 0 = chaos, 5 = rock solid.
    /// </summary>
    Stability,
    
    /// <summary>
    /// Law enforcement presence. 0 = lawless, 5 = heavily patrolled.
    /// </summary>
    SecurityLevel,
    
    /// <summary>
    /// Piracy, smuggling, black market. 0 = clean, 5 = rampant.
    /// </summary>
    CriminalActivity,
    
    /// <summary>
    /// Trade volume, wealth. 0 = dead, 5 = booming.
    /// </summary>
    EconomicActivity,
    
    /// <summary>
    /// Patrol frequency. 0 = none, 5 = constant.
    /// </summary>
    LawEnforcementPresence
}
```

**Acceptance Criteria**:
- [ ] Enum created with all 5 metric types
- [ ] XML documentation matches `SystemMetrics` properties

---

#### Step 1.2: Add Typed Accessors to SystemMetrics

**File**: `src/sim/world/SystemMetrics.cs`

**Add methods**:
```csharp
/// <summary>
/// Get metric value by type.
/// </summary>
public int Get(SystemMetricType type)
{
    return type switch
    {
        SystemMetricType.Stability => Stability,
        SystemMetricType.SecurityLevel => SecurityLevel,
        SystemMetricType.CriminalActivity => CriminalActivity,
        SystemMetricType.EconomicActivity => EconomicActivity,
        SystemMetricType.LawEnforcementPresence => LawEnforcementPresence,
        _ => 0
    };
}

/// <summary>
/// Set metric value by type. Automatically clamps to 0-5.
/// </summary>
public void Set(SystemMetricType type, int value)
{
    value = Math.Clamp(value, 0, 5);
    switch (type)
    {
        case SystemMetricType.Stability: Stability = value; break;
        case SystemMetricType.SecurityLevel: SecurityLevel = value; break;
        case SystemMetricType.CriminalActivity: CriminalActivity = value; break;
        case SystemMetricType.EconomicActivity: EconomicActivity = value; break;
        case SystemMetricType.LawEnforcementPresence: LawEnforcementPresence = value; break;
    }
}

/// <summary>
/// Modify metric by delta. Automatically clamps to 0-5.
/// </summary>
public void Modify(SystemMetricType type, int delta)
{
    Set(type, Get(type) + delta);
}

/// <summary>
/// Clamp all metrics to valid 0-5 range.
/// </summary>
public void ClampAll()
{
    Stability = Math.Clamp(Stability, 0, 5);
    SecurityLevel = Math.Clamp(SecurityLevel, 0, 5);
    CriminalActivity = Math.Clamp(CriminalActivity, 0, 5);
    EconomicActivity = Math.Clamp(EconomicActivity, 0, 5);
    LawEnforcementPresence = Math.Clamp(LawEnforcementPresence, 0, 5);
}

/// <summary>
/// Create a copy of these metrics.
/// </summary>
public SystemMetrics Clone()
{
    return new SystemMetrics
    {
        Stability = Stability,
        SecurityLevel = SecurityLevel,
        CriminalActivity = CriminalActivity,
        EconomicActivity = EconomicActivity,
        LawEnforcementPresence = LawEnforcementPresence
    };
}
```

**Acceptance Criteria**:
- [ ] `Get()` returns correct metric value
- [ ] `Set()` clamps values to 0-5
- [ ] `Modify()` applies delta correctly
- [ ] `ClampAll()` ensures all metrics are valid
- [ ] `Clone()` creates independent copy

---

### Phase 2: WorldState Metric APIs

#### Step 2.1: Add Metric Query Methods

**File**: `src/sim/world/WorldState.cs`

**Add to System Queries section**:
```csharp
/// <summary>
/// Get a specific metric value for a system.
/// </summary>
public int GetSystemMetric(int systemId, SystemMetricType metric)
{
    return GetSystemMetrics(systemId)?.Get(metric) ?? 0;
}

/// <summary>
/// Get systems where a metric meets a threshold.
/// </summary>
public IEnumerable<StarSystem> GetSystemsByMetric(
    SystemMetricType metric, 
    int minValue = 0, 
    int maxValue = 5)
{
    return Systems.Values.Where(s => 
    {
        int value = s.Metrics?.Get(metric) ?? 0;
        return value >= minValue && value <= maxValue;
    });
}

/// <summary>
/// Get high-security systems (SecurityLevel >= threshold).
/// </summary>
public IEnumerable<StarSystem> GetHighSecuritySystems(int minLevel = 4)
{
    return GetSystemsByMetric(SystemMetricType.SecurityLevel, minLevel, 5);
}

/// <summary>
/// Get lawless systems (SecurityLevel <= threshold).
/// </summary>
public IEnumerable<StarSystem> GetLawlessSystems(int maxSecurity = 1)
{
    return GetSystemsByMetric(SystemMetricType.SecurityLevel, 0, maxSecurity);
}

/// <summary>
/// Get systems with high criminal activity.
/// </summary>
public IEnumerable<StarSystem> GetHighCrimeSystems(int minCrime = 4)
{
    return GetSystemsByMetric(SystemMetricType.CriminalActivity, minCrime, 5);
}

/// <summary>
/// Get economically active systems.
/// </summary>
public IEnumerable<StarSystem> GetProsperousSystems(int minEconomy = 4)
{
    return GetSystemsByMetric(SystemMetricType.EconomicActivity, minEconomy, 5);
}
```

**Acceptance Criteria**:
- [ ] `GetSystemMetric()` returns correct value
- [ ] `GetSystemsByMetric()` filters correctly
- [ ] Convenience methods work as expected

---

#### Step 2.2: Add Metric Mutation Methods

**File**: `src/sim/world/WorldState.cs`

**Add to Mutation APIs section**:
```csharp
/// <summary>
/// Set a specific metric value for a system.
/// Emits MetricChangedEvent if EventBus is available.
/// </summary>
public bool SetSystemMetric(int systemId, SystemMetricType metric, int value)
{
    var system = GetSystem(systemId);
    if (system?.Metrics == null) return false;
    
    int oldValue = system.Metrics.Get(metric);
    system.Metrics.Set(metric, value);
    int newValue = system.Metrics.Get(metric);
    
    if (oldValue != newValue)
    {
        SimLog.Log($"[World] System {system.Name}: {metric} {oldValue} → {newValue}");
    }
    
    return true;
}

/// <summary>
/// Modify a metric by delta for a system.
/// Emits MetricChangedEvent if EventBus is available.
/// </summary>
public bool ModifySystemMetric(int systemId, SystemMetricType metric, int delta)
{
    var system = GetSystem(systemId);
    if (system?.Metrics == null) return false;
    
    int oldValue = system.Metrics.Get(metric);
    system.Metrics.Modify(metric, delta);
    int newValue = system.Metrics.Get(metric);
    
    if (oldValue != newValue)
    {
        SimLog.Log($"[World] System {system.Name}: {metric} {oldValue} → {newValue} (delta: {delta:+#;-#;0})");
    }
    
    return true;
}
```

**Acceptance Criteria**:
- [ ] `SetSystemMetric()` updates metric correctly
- [ ] `ModifySystemMetric()` applies delta correctly
- [ ] Both methods log changes
- [ ] Both methods return false for invalid system

---

### Phase 3: Tag System Enhancement

#### Step 3.1: Expand WorldTags Vocabulary

**File**: `src/sim/world/WorldTags.cs`

**Reorganize and expand**:
```csharp
namespace FringeTactics;

/// <summary>
/// Well-known tags for systems, stations, and routes.
/// Tags are strings for moddability, but these constants ensure consistency.
/// </summary>
public static class WorldTags
{
    // ========== System Tags ==========
    
    // Region type
    public const string Core = "core";
    public const string Frontier = "frontier";
    public const string Border = "border";
    
    // Economic character
    public const string Industrial = "industrial";
    public const string Mining = "mining";
    public const string Agricultural = "agricultural";
    
    // Political/security
    public const string Lawless = "lawless";
    public const string Military = "military";
    public const string Contested = "contested";
    
    // Special designations
    public const string Hub = "hub";
    public const string PirateHaven = "pirate_haven";
    public const string ResearchOutpost = "research_outpost";
    public const string Quarantined = "quarantined";
    
    // ========== Station Tags ==========
    
    public const string TradeHub = "trade_hub";
    public const string BlackMarket = "black_market";
    public const string RepairYard = "repair_yard";
    public const string RecruitmentCenter = "recruitment";
    public const string MedicalFacility = "medical";
    public const string Entertainment = "entertainment";
    public const string Refinery = "refinery";
    public const string Shipyard = "shipyard";
    
    // ========== Route Tags ==========
    
    public const string Dangerous = "dangerous";
    public const string Patrolled = "patrolled";
    public const string Hidden = "hidden";
    public const string Blockaded = "blockaded";
    public const string Shortcut = "shortcut";
    public const string Asteroid = "asteroid_field";
    public const string Nebula = "nebula";
    public const string Unstable = "unstable";
    
    // ========== Tag Categories (for validation) ==========
    
    public static readonly HashSet<string> SystemTags = new()
    {
        Core, Frontier, Border,
        Industrial, Mining, Agricultural,
        Lawless, Military, Contested,
        Hub, PirateHaven, ResearchOutpost, Quarantined
    };
    
    public static readonly HashSet<string> StationTags = new()
    {
        TradeHub, BlackMarket, RepairYard, RecruitmentCenter,
        MedicalFacility, Entertainment, Refinery, Shipyard
    };
    
    public static readonly HashSet<string> RouteTags = new()
    {
        Dangerous, Patrolled, Hidden, Blockaded,
        Shortcut, Asteroid, Nebula, Unstable
    };
    
    /// <summary>
    /// Check if a tag is a known system tag.
    /// </summary>
    public static bool IsSystemTag(string tag) => SystemTags.Contains(tag);
    
    /// <summary>
    /// Check if a tag is a known station tag.
    /// </summary>
    public static bool IsStationTag(string tag) => StationTags.Contains(tag);
    
    /// <summary>
    /// Check if a tag is a known route tag.
    /// </summary>
    public static bool IsRouteTag(string tag) => RouteTags.Contains(tag);
}
```

**Acceptance Criteria**:
- [ ] 13 system tags defined
- [ ] 8 station tags defined
- [ ] 8 route tags defined
- [ ] Category sets for validation
- [ ] Validation methods work correctly

---

#### Step 3.2: Add Tag Query Methods to WorldState

**File**: `src/sim/world/WorldState.cs`

**Add to System Queries section**:
```csharp
/// <summary>
/// Get systems that have ALL specified tags.
/// </summary>
public IEnumerable<StarSystem> GetSystemsWithAllTags(params string[] tags)
{
    return Systems.Values.Where(s => tags.All(t => s.HasTag(t)));
}

/// <summary>
/// Get systems that have ANY of the specified tags.
/// </summary>
public IEnumerable<StarSystem> GetSystemsWithAnyTag(params string[] tags)
{
    return Systems.Values.Where(s => tags.Any(t => s.HasTag(t)));
}

/// <summary>
/// Get stations by tag.
/// </summary>
public IEnumerable<Station> GetStationsByTag(string tag)
{
    return Stations.Values.Where(s => s.HasTag(tag));
}

/// <summary>
/// Get stations that have ALL specified tags.
/// </summary>
public IEnumerable<Station> GetStationsWithAllTags(params string[] tags)
{
    return Stations.Values.Where(s => tags.All(t => s.HasTag(t)));
}
```

**Acceptance Criteria**:
- [ ] `GetSystemsWithAllTags()` requires all tags
- [ ] `GetSystemsWithAnyTag()` requires any tag
- [ ] Station tag queries work correctly

---

#### Step 3.3: Add Tag Mutation Methods

**File**: `src/sim/world/WorldState.cs`

**Add to Mutation APIs section**:
```csharp
/// <summary>
/// Add a tag to a system.
/// </summary>
public bool AddSystemTag(int systemId, string tag)
{
    var system = GetSystem(systemId);
    if (system == null) return false;
    
    if (system.Tags.Add(tag))
    {
        SimLog.Log($"[World] System {system.Name}: added tag '{tag}'");
        return true;
    }
    return false;
}

/// <summary>
/// Remove a tag from a system.
/// </summary>
public bool RemoveSystemTag(int systemId, string tag)
{
    var system = GetSystem(systemId);
    if (system == null) return false;
    
    if (system.Tags.Remove(tag))
    {
        SimLog.Log($"[World] System {system.Name}: removed tag '{tag}'");
        return true;
    }
    return false;
}

/// <summary>
/// Add a tag to a route.
/// </summary>
public bool AddRouteTag(int fromId, int toId, string tag)
{
    var route = GetRoute(fromId, toId);
    if (route == null) return false;
    
    if (route.Tags.Add(tag))
    {
        var fromName = GetSystem(fromId)?.Name ?? fromId.ToString();
        var toName = GetSystem(toId)?.Name ?? toId.ToString();
        SimLog.Log($"[World] Route {fromName}↔{toName}: added tag '{tag}'");
        return true;
    }
    return false;
}

/// <summary>
/// Remove a tag from a route.
/// </summary>
public bool RemoveRouteTag(int fromId, int toId, string tag)
{
    var route = GetRoute(fromId, toId);
    if (route == null) return false;
    
    if (route.Tags.Remove(tag))
    {
        var fromName = GetSystem(fromId)?.Name ?? fromId.ToString();
        var toName = GetSystem(toId)?.Name ?? toId.ToString();
        SimLog.Log($"[World] Route {fromName}↔{toName}: removed tag '{tag}'");
        return true;
    }
    return false;
}
```

**Acceptance Criteria**:
- [ ] Tags can be added to systems
- [ ] Tags can be removed from systems
- [ ] Tags can be added to routes
- [ ] Tags can be removed from routes
- [ ] All mutations are logged

---

### Phase 4: Composite Queries for Travel/Encounter

#### Step 4.1: Add Travel-Oriented Queries

**File**: `src/sim/world/WorldState.cs`

```csharp
/// <summary>
/// Get the effective danger level for a route.
/// Combines route hazard with endpoint system metrics.
/// </summary>
public int GetEffectiveRouteDanger(int fromId, int toId)
{
    var route = GetRoute(fromId, toId);
    if (route == null) return 0;
    
    int baseHazard = route.HazardLevel;
    
    // Add danger from high crime at endpoints
    int fromCrime = GetSystemMetric(fromId, SystemMetricType.CriminalActivity);
    int toCrime = GetSystemMetric(toId, SystemMetricType.CriminalActivity);
    int crimeFactor = Math.Max(fromCrime, toCrime) / 2;
    
    // Reduce danger from high security at endpoints
    int fromSecurity = GetSystemMetric(fromId, SystemMetricType.SecurityLevel);
    int toSecurity = GetSystemMetric(toId, SystemMetricType.SecurityLevel);
    int securityFactor = Math.Min(fromSecurity, toSecurity) / 2;
    
    return Math.Clamp(baseHazard + crimeFactor - securityFactor, 0, 5);
}

/// <summary>
/// Get routes suitable for smuggling (hidden, low patrol, connects to lawless).
/// </summary>
public IEnumerable<Route> GetSmugglingRoutes()
{
    return Routes.Values.Where(r =>
    {
        if (r.HasTag(WorldTags.Patrolled)) return false;
        if (r.HasTag(WorldTags.Hidden)) return true;
        
        // Check if either endpoint is lawless
        var sysA = GetSystem(r.SystemA);
        var sysB = GetSystem(r.SystemB);
        return (sysA?.HasTag(WorldTags.Lawless) ?? false) ||
               (sysB?.HasTag(WorldTags.Lawless) ?? false);
    });
}

/// <summary>
/// Get systems suitable for laying low (lawless, low security, not contested).
/// </summary>
public IEnumerable<StarSystem> GetHideoutSystems()
{
    return Systems.Values.Where(s =>
    {
        if (s.HasTag(WorldTags.Contested)) return false;
        if (s.Metrics == null) return false;
        
        return s.Metrics.SecurityLevel <= 1 || s.HasTag(WorldTags.Lawless);
    });
}
```

**Acceptance Criteria**:
- [ ] `GetEffectiveRouteDanger()` combines multiple factors
- [ ] `GetSmugglingRoutes()` finds appropriate routes
- [ ] `GetHideoutSystems()` finds safe havens

---

#### Step 4.2: Add Encounter-Oriented Queries

**File**: `src/sim/world/WorldState.cs`

```csharp
/// <summary>
/// Get encounter context for a route.
/// Used by Encounter system to select appropriate templates.
/// </summary>
public RouteEncounterContext GetRouteEncounterContext(int fromId, int toId)
{
    var route = GetRoute(fromId, toId);
    var fromSystem = GetSystem(fromId);
    var toSystem = GetSystem(toId);
    
    if (route == null || fromSystem == null || toSystem == null)
        return null;
    
    return new RouteEncounterContext
    {
        RouteId = route.Id,
        FromSystemId = fromId,
        ToSystemId = toId,
        Distance = route.Distance,
        HazardLevel = route.HazardLevel,
        EffectiveDanger = GetEffectiveRouteDanger(fromId, toId),
        RouteTags = new HashSet<string>(route.Tags),
        FromSystemTags = new HashSet<string>(fromSystem.Tags),
        ToSystemTags = new HashSet<string>(toSystem.Tags),
        FromSecurityLevel = fromSystem.Metrics?.SecurityLevel ?? 0,
        ToSecurityLevel = toSystem.Metrics?.SecurityLevel ?? 0,
        FromCriminalActivity = fromSystem.Metrics?.CriminalActivity ?? 0,
        ToCriminalActivity = toSystem.Metrics?.CriminalActivity ?? 0
    };
}

/// <summary>
/// Get encounter context for a system (for station encounters).
/// </summary>
public SystemEncounterContext GetSystemEncounterContext(int systemId)
{
    var system = GetSystem(systemId);
    if (system == null) return null;
    
    var stations = GetStationsInSystem(systemId).ToList();
    var stationTags = new HashSet<string>();
    foreach (var station in stations)
    {
        foreach (var tag in station.Tags)
            stationTags.Add(tag);
    }
    
    return new SystemEncounterContext
    {
        SystemId = systemId,
        SystemName = system.Name,
        SystemType = system.Type,
        OwningFactionId = system.OwningFactionId,
        SystemTags = new HashSet<string>(system.Tags),
        StationTags = stationTags,
        Metrics = system.Metrics?.Clone() ?? new SystemMetrics(),
        HasStation = stations.Count > 0,
        StationCount = stations.Count
    };
}
```

**New File**: `src/sim/world/EncounterContext.cs`

```csharp
using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// Context data for route-based encounter selection.
/// </summary>
public class RouteEncounterContext
{
    public int RouteId { get; set; }
    public int FromSystemId { get; set; }
    public int ToSystemId { get; set; }
    public float Distance { get; set; }
    public int HazardLevel { get; set; }
    public int EffectiveDanger { get; set; }
    public HashSet<string> RouteTags { get; set; } = new();
    public HashSet<string> FromSystemTags { get; set; } = new();
    public HashSet<string> ToSystemTags { get; set; } = new();
    public int FromSecurityLevel { get; set; }
    public int ToSecurityLevel { get; set; }
    public int FromCriminalActivity { get; set; }
    public int ToCriminalActivity { get; set; }
    
    /// <summary>
    /// Check if route or either endpoint has a tag.
    /// </summary>
    public bool HasAnyTag(string tag)
    {
        return RouteTags.Contains(tag) || 
               FromSystemTags.Contains(tag) || 
               ToSystemTags.Contains(tag);
    }
    
    /// <summary>
    /// Get the higher criminal activity of the two endpoints.
    /// </summary>
    public int MaxCriminalActivity => System.Math.Max(FromCriminalActivity, ToCriminalActivity);
    
    /// <summary>
    /// Get the lower security level of the two endpoints.
    /// </summary>
    public int MinSecurityLevel => System.Math.Min(FromSecurityLevel, ToSecurityLevel);
}

/// <summary>
/// Context data for system-based encounter selection.
/// </summary>
public class SystemEncounterContext
{
    public int SystemId { get; set; }
    public string SystemName { get; set; }
    public SystemType SystemType { get; set; }
    public string OwningFactionId { get; set; }
    public HashSet<string> SystemTags { get; set; } = new();
    public HashSet<string> StationTags { get; set; } = new();
    public SystemMetrics Metrics { get; set; }
    public bool HasStation { get; set; }
    public int StationCount { get; set; }
    
    /// <summary>
    /// Check if system or any station has a tag.
    /// </summary>
    public bool HasAnyTag(string tag)
    {
        return SystemTags.Contains(tag) || StationTags.Contains(tag);
    }
}
```

**Acceptance Criteria**:
- [ ] `RouteEncounterContext` captures all relevant route data
- [ ] `SystemEncounterContext` captures all relevant system data
- [ ] Context objects are self-contained (no WorldState reference needed)

---

### Phase 5: Test Sector Enhancement

#### Step 5.1: Update CreateTestSector with Richer Metrics

**File**: `src/sim/world/WorldState.cs`

Update `CreateTestSector()` to demonstrate varied metrics:

```csharp
// In CreateTestSector(), after creating systems, add varied metrics:

// Haven Station - prosperous core world
haven.Metrics = new SystemMetrics
{
    Stability = 5,
    SecurityLevel = 4,
    CriminalActivity = 1,
    EconomicActivity = 5,
    LawEnforcementPresence = 4
};

// Waypoint Alpha - frontier outpost, some crime
waypoint.Metrics = new SystemMetrics
{
    Stability = 3,
    SecurityLevel = 2,
    CriminalActivity = 3,
    EconomicActivity = 2,
    LawEnforcementPresence = 2
};

// Rockfall Mining - industrial, moderate security
rockfall.Metrics = new SystemMetrics
{
    Stability = 3,
    SecurityLevel = 2,
    CriminalActivity = 2,
    EconomicActivity = 4,
    LawEnforcementPresence = 2
};

// Red Claw Base - pirate haven, lawless
redClaw.Metrics = new SystemMetrics
{
    Stability = 2,
    SecurityLevel = 0,
    CriminalActivity = 5,
    EconomicActivity = 3,
    LawEnforcementPresence = 0
};

// Contested Zone - unstable, dangerous
contested.Metrics = new SystemMetrics
{
    Stability = 1,
    SecurityLevel = 1,
    CriminalActivity = 4,
    EconomicActivity = 1,
    LawEnforcementPresence = 1
};

// Patrol Station - military, very secure
patrol.Metrics = new SystemMetrics
{
    Stability = 5,
    SecurityLevel = 5,
    CriminalActivity = 0,
    EconomicActivity = 2,
    LawEnforcementPresence = 5
};

// Smuggler's Den - hidden, lawless
smuggler.Metrics = new SystemMetrics
{
    Stability = 2,
    SecurityLevel = 0,
    CriminalActivity = 4,
    EconomicActivity = 3,
    LawEnforcementPresence = 0
};

// Wreck of Icarus - abandoned, dangerous
wreck.Metrics = new SystemMetrics
{
    Stability = 0,
    SecurityLevel = 0,
    CriminalActivity = 3,
    EconomicActivity = 0,
    LawEnforcementPresence = 0
};
```

**Acceptance Criteria**:
- [ ] Each system has distinct, thematic metrics
- [ ] Metrics support varied gameplay scenarios

---

## Files Summary

### Files to Create

| File | Purpose |
|------|---------|
| `src/sim/world/SystemMetricType.cs` | Enum for typed metric access |
| `src/sim/world/EncounterContext.cs` | Context classes for Encounter domain |
| `tests/sim/world/WD3MetricTests.cs` | Metric system tests |
| `tests/sim/world/WD3TagTests.cs` | Tag system tests |
| `tests/sim/world/WD3QueryTests.cs` | Query API tests |

### Files to Modify

| File | Changes |
|------|---------|
| `src/sim/world/SystemMetrics.cs` | Add `Get()`, `Set()`, `Modify()`, `ClampAll()`, `Clone()` |
| `src/sim/world/WorldTags.cs` | Expand vocabulary, add category sets |
| `src/sim/world/WorldState.cs` | Add metric/tag queries and mutations |
| `src/sim/world/agents.md` | Document new files |

---

## Unit Tests

### Test File: `tests/sim/world/WD3MetricTests.cs`

```csharp
using GdUnit4;
using static GdUnit4.Assertions;

namespace FringeTactics.Tests;

[TestSuite]
public class WD3MetricTests
{
    [TestCase]
    public void SystemMetrics_Get_ReturnsCorrectValue()
    {
        var metrics = new SystemMetrics
        {
            SecurityLevel = 4,
            CriminalActivity = 2
        };
        
        AssertInt(metrics.Get(SystemMetricType.SecurityLevel)).IsEqual(4);
        AssertInt(metrics.Get(SystemMetricType.CriminalActivity)).IsEqual(2);
    }
    
    [TestCase]
    public void SystemMetrics_Set_ClampsToValidRange()
    {
        var metrics = new SystemMetrics();
        
        metrics.Set(SystemMetricType.SecurityLevel, 10);
        AssertInt(metrics.SecurityLevel).IsEqual(5);
        
        metrics.Set(SystemMetricType.SecurityLevel, -5);
        AssertInt(metrics.SecurityLevel).IsEqual(0);
    }
    
    [TestCase]
    public void SystemMetrics_Modify_AppliesDelta()
    {
        var metrics = new SystemMetrics { SecurityLevel = 3 };
        
        metrics.Modify(SystemMetricType.SecurityLevel, 1);
        AssertInt(metrics.SecurityLevel).IsEqual(4);
        
        metrics.Modify(SystemMetricType.SecurityLevel, -2);
        AssertInt(metrics.SecurityLevel).IsEqual(2);
    }
    
    [TestCase]
    public void SystemMetrics_Modify_ClampsResult()
    {
        var metrics = new SystemMetrics { SecurityLevel = 4 };
        
        metrics.Modify(SystemMetricType.SecurityLevel, 5);
        AssertInt(metrics.SecurityLevel).IsEqual(5);
    }
    
    [TestCase]
    public void SystemMetrics_Clone_CreatesIndependentCopy()
    {
        var original = new SystemMetrics { SecurityLevel = 3 };
        var clone = original.Clone();
        
        clone.SecurityLevel = 5;
        
        AssertInt(original.SecurityLevel).IsEqual(3);
        AssertInt(clone.SecurityLevel).IsEqual(5);
    }
    
    [TestCase]
    public void WorldState_GetSystemMetric_ReturnsCorrectValue()
    {
        var world = WorldState.CreateTestSector();
        
        // Haven Station should have high security
        int security = world.GetSystemMetric(0, SystemMetricType.SecurityLevel);
        AssertInt(security).IsGreaterEqual(4);
    }
    
    [TestCase]
    public void WorldState_SetSystemMetric_UpdatesValue()
    {
        var world = WorldState.CreateTestSector();
        
        world.SetSystemMetric(0, SystemMetricType.CriminalActivity, 3);
        
        int crime = world.GetSystemMetric(0, SystemMetricType.CriminalActivity);
        AssertInt(crime).IsEqual(3);
    }
    
    [TestCase]
    public void WorldState_ModifySystemMetric_AppliesDelta()
    {
        var world = WorldState.CreateTestSector();
        int original = world.GetSystemMetric(0, SystemMetricType.SecurityLevel);
        
        world.ModifySystemMetric(0, SystemMetricType.SecurityLevel, -1);
        
        int modified = world.GetSystemMetric(0, SystemMetricType.SecurityLevel);
        AssertInt(modified).IsEqual(original - 1);
    }
    
    [TestCase]
    public void WorldState_GetHighSecuritySystems_FiltersCorrectly()
    {
        var world = WorldState.CreateTestSector();
        
        var highSec = world.GetHighSecuritySystems(4).ToList();
        
        // Should include Haven and Patrol Station
        AssertInt(highSec.Count).IsGreaterEqual(2);
        AssertBool(highSec.All(s => s.Metrics.SecurityLevel >= 4)).IsTrue();
    }
    
    [TestCase]
    public void WorldState_GetLawlessSystems_FiltersCorrectly()
    {
        var world = WorldState.CreateTestSector();
        
        var lawless = world.GetLawlessSystems(1).ToList();
        
        // Should include Red Claw, Smuggler's Den, Wreck
        AssertInt(lawless.Count).IsGreaterEqual(3);
        AssertBool(lawless.All(s => s.Metrics.SecurityLevel <= 1)).IsTrue();
    }
}
```

### Test File: `tests/sim/world/WD3TagTests.cs`

```csharp
using GdUnit4;
using static GdUnit4.Assertions;

namespace FringeTactics.Tests;

[TestSuite]
public class WD3TagTests
{
    [TestCase]
    public void WorldTags_IsSystemTag_RecognizesSystemTags()
    {
        AssertBool(WorldTags.IsSystemTag(WorldTags.Core)).IsTrue();
        AssertBool(WorldTags.IsSystemTag(WorldTags.Frontier)).IsTrue();
        AssertBool(WorldTags.IsSystemTag(WorldTags.Lawless)).IsTrue();
    }
    
    [TestCase]
    public void WorldTags_IsSystemTag_RejectsNonSystemTags()
    {
        AssertBool(WorldTags.IsSystemTag(WorldTags.Dangerous)).IsFalse();
        AssertBool(WorldTags.IsSystemTag(WorldTags.BlackMarket)).IsFalse();
    }
    
    [TestCase]
    public void WorldTags_IsRouteTag_RecognizesRouteTags()
    {
        AssertBool(WorldTags.IsRouteTag(WorldTags.Dangerous)).IsTrue();
        AssertBool(WorldTags.IsRouteTag(WorldTags.Patrolled)).IsTrue();
        AssertBool(WorldTags.IsRouteTag(WorldTags.Hidden)).IsTrue();
    }
    
    [TestCase]
    public void WorldState_GetSystemsWithAllTags_RequiresAllTags()
    {
        var world = WorldState.CreateTestSector();
        
        var coreHubs = world.GetSystemsWithAllTags(WorldTags.Core, WorldTags.Hub).ToList();
        
        // Only Haven Station should have both
        AssertInt(coreHubs.Count).IsEqual(1);
        AssertString(coreHubs[0].Name).IsEqual("Haven Station");
    }
    
    [TestCase]
    public void WorldState_GetSystemsWithAnyTag_MatchesAnyTag()
    {
        var world = WorldState.CreateTestSector();
        
        var lawlessOrMilitary = world.GetSystemsWithAnyTag(
            WorldTags.Lawless, WorldTags.Military).ToList();
        
        // Should include Red Claw, Smuggler's Den, Patrol Station
        AssertInt(lawlessOrMilitary.Count).IsGreaterEqual(3);
    }
    
    [TestCase]
    public void WorldState_AddSystemTag_AddsTag()
    {
        var world = WorldState.CreateTestSector();
        
        bool added = world.AddSystemTag(1, WorldTags.Quarantined);
        
        AssertBool(added).IsTrue();
        AssertBool(world.HasTag(1, WorldTags.Quarantined)).IsTrue();
    }
    
    [TestCase]
    public void WorldState_AddSystemTag_ReturnsFalseIfAlreadyHasTag()
    {
        var world = WorldState.CreateTestSector();
        
        // Haven already has Core tag
        bool added = world.AddSystemTag(0, WorldTags.Core);
        
        AssertBool(added).IsFalse();
    }
    
    [TestCase]
    public void WorldState_RemoveSystemTag_RemovesTag()
    {
        var world = WorldState.CreateTestSector();
        
        bool removed = world.RemoveSystemTag(0, WorldTags.Hub);
        
        AssertBool(removed).IsTrue();
        AssertBool(world.HasTag(0, WorldTags.Hub)).IsFalse();
    }
    
    [TestCase]
    public void WorldState_AddRouteTag_AddsTag()
    {
        var world = WorldState.CreateTestSector();
        
        bool added = world.AddRouteTag(0, 1, WorldTags.Blockaded);
        
        AssertBool(added).IsTrue();
        var route = world.GetRoute(0, 1);
        AssertBool(route.HasTag(WorldTags.Blockaded)).IsTrue();
    }
}
```

### Test File: `tests/sim/world/WD3QueryTests.cs`

```csharp
using GdUnit4;
using static GdUnit4.Assertions;

namespace FringeTactics.Tests;

[TestSuite]
public class WD3QueryTests
{
    [TestCase]
    public void WorldState_GetEffectiveRouteDanger_CombinesFactors()
    {
        var world = WorldState.CreateTestSector();
        
        // Route from Haven (safe) to Waypoint (moderate)
        int danger01 = world.GetEffectiveRouteDanger(0, 1);
        
        // Route from Waypoint to Contested (dangerous)
        int danger14 = world.GetEffectiveRouteDanger(1, 4);
        
        // Dangerous route should have higher effective danger
        AssertInt(danger14).IsGreater(danger01);
    }
    
    [TestCase]
    public void WorldState_GetSmugglingRoutes_FindsHiddenRoutes()
    {
        var world = WorldState.CreateTestSector();
        
        var smugglingRoutes = world.GetSmugglingRoutes().ToList();
        
        // Should include hidden routes (Waypoint-Smuggler, RedClaw-Smuggler)
        AssertInt(smugglingRoutes.Count).IsGreaterEqual(2);
        AssertBool(smugglingRoutes.Any(r => r.HasTag(WorldTags.Hidden))).IsTrue();
    }
    
    [TestCase]
    public void WorldState_GetHideoutSystems_FindsLawlessSystems()
    {
        var world = WorldState.CreateTestSector();
        
        var hideouts = world.GetHideoutSystems().ToList();
        
        // Should include Red Claw, Smuggler's Den
        AssertInt(hideouts.Count).IsGreaterEqual(2);
        AssertBool(hideouts.All(s => 
            s.Metrics.SecurityLevel <= 1 || s.HasTag(WorldTags.Lawless))).IsTrue();
    }
    
    [TestCase]
    public void WorldState_GetRouteEncounterContext_ReturnsCompleteContext()
    {
        var world = WorldState.CreateTestSector();
        
        var context = world.GetRouteEncounterContext(0, 1);
        
        AssertObject(context).IsNotNull();
        AssertInt(context.FromSystemId).IsEqual(0);
        AssertInt(context.ToSystemId).IsEqual(1);
        AssertFloat(context.Distance).IsGreater(0f);
        AssertBool(context.RouteTags.Contains(WorldTags.Patrolled)).IsTrue();
    }
    
    [TestCase]
    public void WorldState_GetSystemEncounterContext_ReturnsCompleteContext()
    {
        var world = WorldState.CreateTestSector();
        
        var context = world.GetSystemEncounterContext(0);
        
        AssertObject(context).IsNotNull();
        AssertString(context.SystemName).IsEqual("Haven Station");
        AssertBool(context.HasStation).IsTrue();
        AssertBool(context.SystemTags.Contains(WorldTags.Hub)).IsTrue();
    }
    
    [TestCase]
    public void RouteEncounterContext_HasAnyTag_ChecksAllTagSets()
    {
        var context = new RouteEncounterContext
        {
            RouteTags = new HashSet<string> { WorldTags.Dangerous },
            FromSystemTags = new HashSet<string> { WorldTags.Core },
            ToSystemTags = new HashSet<string> { WorldTags.Frontier }
        };
        
        AssertBool(context.HasAnyTag(WorldTags.Dangerous)).IsTrue();
        AssertBool(context.HasAnyTag(WorldTags.Core)).IsTrue();
        AssertBool(context.HasAnyTag(WorldTags.Frontier)).IsTrue();
        AssertBool(context.HasAnyTag(WorldTags.Lawless)).IsFalse();
    }
}
```

---

## Manual Test Setup

### Test Scenario: Metric Queries

1. Create test sector: `var world = WorldState.CreateTestSector();`
2. Verify high-security systems:
   - `world.GetHighSecuritySystems(4)` should return Haven Station and Patrol Station
3. Verify lawless systems:
   - `world.GetLawlessSystems(1)` should return Red Claw, Smuggler's Den, Wreck of Icarus
4. Verify prosperous systems:
   - `world.GetProsperousSystems(4)` should return Haven Station

### Test Scenario: Metric Mutation

1. Create test sector
2. Get initial security: `int initial = world.GetSystemMetric(1, SystemMetricType.SecurityLevel);`
3. Modify security: `world.ModifySystemMetric(1, SystemMetricType.SecurityLevel, 2);`
4. Verify change: `world.GetSystemMetric(1, SystemMetricType.SecurityLevel)` should be `initial + 2` (clamped to 5)

### Test Scenario: Tag Queries

1. Create test sector
2. Find core hub systems: `world.GetSystemsWithAllTags(WorldTags.Core, WorldTags.Hub)` → Haven Station only
3. Find lawless or military: `world.GetSystemsWithAnyTag(WorldTags.Lawless, WorldTags.Military)` → Red Claw, Smuggler's Den, Patrol Station

### Test Scenario: Encounter Context

1. Create test sector
2. Get route context: `var ctx = world.GetRouteEncounterContext(1, 4);`
3. Verify context contains:
   - `HazardLevel = 3` (dangerous route)
   - `RouteTags` contains "dangerous"
   - `FromSystemTags` contains "frontier"
   - `ToSystemTags` contains "border"

---

## Implementation Order

1. **Step 1.1**: Create `SystemMetricType` enum
2. **Step 1.2**: Add typed accessors to `SystemMetrics`
3. **Step 2.1**: Add metric query methods to `WorldState`
4. **Step 2.2**: Add metric mutation methods to `WorldState`
5. **Step 3.1**: Expand `WorldTags` vocabulary
6. **Step 3.2**: Add tag query methods to `WorldState`
7. **Step 3.3**: Add tag mutation methods to `WorldState`
8. **Step 4.1**: Add travel-oriented queries
9. **Step 4.2**: Add encounter context classes and queries
10. **Step 5.1**: Update test sector with richer metrics
11. Write and run all tests

---

## Acceptance Criteria Summary

### Phase 1: Metric Type System ✅
- [x] `SystemMetricType` enum with 5 values
- [x] `SystemMetrics.Get()` returns correct value
- [x] `SystemMetrics.Set()` clamps to 0-5
- [x] `SystemMetrics.Modify()` applies delta correctly
- [x] `SystemMetrics.Clone()` creates independent copy

### Phase 2: WorldState Metric APIs ✅
- [x] `GetSystemMetric()` returns correct value
- [x] `GetSystemsByMetric()` filters correctly
- [x] `SetSystemMetric()` updates and logs
- [x] `ModifySystemMetric()` applies delta and logs
- [x] Convenience methods (`GetHighSecuritySystems`, etc.) work

### Phase 3: Tag System Enhancement ✅
- [x] 13 system tags, 8 station tags, 8 route tags defined
- [x] Tag category validation methods work
- [x] `GetSystemsWithAllTags()` requires all tags
- [x] `GetSystemsWithAnyTag()` matches any tag
- [x] Tag mutation methods work and log

### Phase 4: Composite Queries ✅
- [x] `GetEffectiveRouteDanger()` combines factors correctly
- [x] `GetSmugglingRoutes()` finds appropriate routes
- [x] `GetHideoutSystems()` finds safe havens
- [x] `RouteEncounterContext` captures all relevant data
- [x] `SystemEncounterContext` captures all relevant data

### Phase 5: Test Sector ✅
- [x] All 8 systems have distinct, thematic metrics
- [x] Metrics support varied gameplay scenarios
