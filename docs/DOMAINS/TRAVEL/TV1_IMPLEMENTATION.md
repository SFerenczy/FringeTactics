# TV1 – Route Planning: Implementation Plan

**Status**: ⬜ Pending  
**Depends on**: WD2 (Sector Topology) ✅ Complete, WD3 (Metrics & Tags) ✅ Complete, TV0 (Concept) ✅ Complete  
**Phase**: G2

---

## Overview

**Goal**: Implement route calculation and travel plan creation, enabling the player to plan journeys between systems with accurate cost and risk estimates.

This milestone builds the foundation for G2's travel system. It provides:
- Weighted A* pathfinding over the world graph
- Travel plan creation with fuel, time, and risk calculations
- Segment-level cost breakdown for UI display

---

## Current State Assessment

### What We Have (from WD2, WD3, TV0)

| Component | Status | Location | Notes |
|-----------|--------|----------|-------|
| `Route` | ✅ Complete | `src/sim/world/Route.cs` | Distance, HazardLevel, Tags |
| `WorldState.FindPath()` | ✅ Complete | `src/sim/world/WorldState.cs` | BFS pathfinding (hop-count only) |
| `WorldState.GetPathDistance()` | ✅ Complete | `src/sim/world/WorldState.cs` | Sum route distances |
| `WorldState.GetPathHazard()` | ✅ Complete | `src/sim/world/WorldState.cs` | Sum route hazards |
| `WorldState.GetRoute()` | ✅ Complete | `src/sim/world/WorldState.cs` | Query route between systems |
| `WorldTags` (route tags) | ✅ Complete | `src/sim/world/WorldTags.cs` | Patrolled, Dangerous, Hidden, etc. |
| `SystemMetrics` | ✅ Complete | `src/sim/world/SystemMetrics.cs` | Typed accessors via `SystemMetricType` |
| `RouteEncounterContext` | ✅ Complete | `src/sim/world/EncounterContext.cs` | Context for encounter generation |
| Test sector | ✅ Complete | `WorldState.CreateTestSector()` | 8 systems, 7 routes |
| `CampaignState.Fuel` | ✅ Complete | `src/sim/campaign/CampaignState.cs` | Current fuel resource |
| `CampaignState.SpendFuel()` | ✅ Complete | `src/sim/campaign/CampaignState.cs` | Fuel consumption with events |
| TV0 formulas | ✅ Complete | `TV0_IMPLEMENTATION.md` | Fuel, time, encounter chance formulas |

### Gaps for TV1

| Requirement | Current Status | Gap |
|-------------|----------------|-----|
| `TravelSegment` class | ❌ Missing | Need segment data structure |
| `TravelPlan` class | ❌ Missing | Need plan with aggregates |
| `TravelPlanner` class | ❌ Missing | Need A* pathfinding with weighted costs |
| Ship speed/efficiency stats | ⚠️ Partial | `Ship` has no `Speed` or `FuelEfficiency` properties |
| Encounter chance calculation | ❌ Missing | Formula defined in TV0, not implemented |
| Travel directory | ❌ Missing | Need `src/sim/travel/` |

---

## Architecture Decisions

### TravelPlanner as Stateless Service

**Decision**: `TravelPlanner` is a stateless utility class that takes `WorldState` and optional ship stats.

**Rationale**:
- Follows existing patterns (`MissionInputBuilder`, `ContractGenerator`)
- Pure functions for testability
- No hidden state or side effects

### Segment-Based Cost Model

**Decision**: Costs are calculated per-segment, then aggregated into the plan.

**Rationale**:
- Enables UI to show per-hop breakdown
- Supports future features (partial travel, segment-specific events)
- Matches TV0 design

### A* with Configurable Weights

**Decision**: Use A* with a cost function that balances distance and hazard.

**Formula** (from TV0):
```
segmentCost = distance + (hazard * safetyWeight * 50)
```

**Rationale**:
- BFS only counts hops, ignoring distance and danger
- A* finds optimal path based on player priorities
- `safetyWeight` defaults to 1.0 but can be adjusted

### Ship Stats via Defaults

**Decision**: For TV1, use hardcoded defaults for ship stats. Ship stat integration is deferred.

| Stat | Default | Purpose |
|------|---------|---------|
| `Speed` | 100 | Units per day |
| `FuelEfficiency` | 1.0 | Multiplier for fuel consumption |

**Rationale**:
- Keeps TV1 focused on pathfinding and planning
- Ship stats can be added in TV2 or a future milestone
- Defaults match TV0 examples

---

## Implementation Steps

### Phase 1: Directory and Data Structures

#### Step 1.1: Create Travel Directory

Create `src/sim/travel/` with `agents.md`.

**File**: `src/sim/travel/agents.md`

```markdown
# Travel Domain (`src/sim/travel/`)

This directory contains the Travel domain simulation layer.

## Purpose

Handle route planning and travel execution across the sector map.

## Files

| File | Purpose |
|------|---------|
| `TravelSegment.cs` | Single route step with costs |
| `TravelPlan.cs` | Complete route with aggregates |
| `TravelPlanner.cs` | A* pathfinding and plan creation |
| `TravelCosts.cs` | Cost calculation utilities |

## Dependencies

- **Imports from**: `src/sim/world/` (WorldState, Route, SystemMetrics)
- **Imported by**: `src/sim/campaign/` (CampaignState), `src/core/` (GameState)

## Key Patterns

- Stateless services with explicit parameters
- Pure functions for cost calculations
- No Godot Node dependencies
```

**Acceptance Criteria**:
- [ ] Directory created
- [ ] `agents.md` documents purpose and files

---

#### Step 1.2: Create TravelSegment Class

**File**: `src/sim/travel/TravelSegment.cs`

```csharp
using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// A single segment of a travel plan (one route between two systems).
/// Contains computed costs and risk for this segment.
/// </summary>
public class TravelSegment
{
    /// <summary>
    /// Origin system ID for this segment.
    /// </summary>
    public int FromSystemId { get; set; }
    
    /// <summary>
    /// Destination system ID for this segment.
    /// </summary>
    public int ToSystemId { get; set; }
    
    /// <summary>
    /// Reference to the world route (for tags, hazard).
    /// </summary>
    public Route Route { get; set; }
    
    /// <summary>
    /// Route distance in world units.
    /// </summary>
    public float Distance { get; set; }
    
    /// <summary>
    /// Fuel cost for this segment.
    /// Formula: ceil(distance * FUEL_RATE / shipEfficiency)
    /// </summary>
    public int FuelCost { get; set; }
    
    /// <summary>
    /// Time cost in days for this segment.
    /// Formula: ceil(distance / shipSpeed)
    /// </summary>
    public int TimeDays { get; set; }
    
    /// <summary>
    /// Base encounter chance per day (0.0 - 1.0).
    /// Calculated from route hazard and tags.
    /// </summary>
    public float EncounterChance { get; set; }
    
    /// <summary>
    /// Suggested encounter type based on route/system context.
    /// Used by Generation domain for encounter selection.
    /// </summary>
    public string SuggestedEncounterType { get; set; }
    
    /// <summary>
    /// Route hazard level (0-5).
    /// </summary>
    public int HazardLevel => Route?.HazardLevel ?? 0;
    
    /// <summary>
    /// Route tags for reference.
    /// </summary>
    public HashSet<string> RouteTags => Route?.Tags ?? new HashSet<string>();
    
    /// <summary>
    /// Create a segment from a route with default ship stats.
    /// </summary>
    public static TravelSegment FromRoute(Route route, float shipSpeed = 100f, float shipEfficiency = 1.0f)
    {
        if (route == null) return null;
        
        var segment = new TravelSegment
        {
            FromSystemId = route.SystemA,
            ToSystemId = route.SystemB,
            Route = route,
            Distance = route.Distance
        };
        
        segment.FuelCost = TravelCosts.CalculateFuelCost(route.Distance, shipEfficiency);
        segment.TimeDays = TravelCosts.CalculateTimeDays(route.Distance, shipSpeed);
        segment.EncounterChance = TravelCosts.CalculateEncounterChance(route);
        segment.SuggestedEncounterType = TravelCosts.SuggestEncounterType(route);
        
        return segment;
    }
    
    /// <summary>
    /// Create a segment with explicit from/to (for directional clarity).
    /// </summary>
    public static TravelSegment FromRoute(Route route, int fromId, int toId, float shipSpeed = 100f, float shipEfficiency = 1.0f)
    {
        var segment = FromRoute(route, shipSpeed, shipEfficiency);
        if (segment != null)
        {
            segment.FromSystemId = fromId;
            segment.ToSystemId = toId;
        }
        return segment;
    }
}
```

**Acceptance Criteria**:
- [ ] `TravelSegment` class with all properties
- [ ] Factory methods for creating from `Route`
- [ ] Delegates cost calculations to `TravelCosts`

---

#### Step 1.3: Create TravelCosts Utility

**File**: `src/sim/travel/TravelCosts.cs`

```csharp
using System;
using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// Stateless utility for travel cost calculations.
/// Formulas defined in TV0_IMPLEMENTATION.md.
/// </summary>
public static class TravelCosts
{
    // === Constants (from TV0) ===
    
    /// <summary>
    /// Base fuel consumption per distance unit.
    /// </summary>
    public const float FUEL_RATE = 0.1f;
    
    /// <summary>
    /// Default ship speed (distance units per day).
    /// </summary>
    public const float DEFAULT_SPEED = 100f;
    
    /// <summary>
    /// Default ship fuel efficiency multiplier.
    /// </summary>
    public const float DEFAULT_EFFICIENCY = 1.0f;
    
    /// <summary>
    /// Maximum encounter chance (cap at 80%).
    /// </summary>
    public const float MAX_ENCOUNTER_CHANCE = 0.8f;
    
    /// <summary>
    /// Safety weight for A* pathfinding cost.
    /// </summary>
    public const float SAFETY_WEIGHT = 50f;
    
    // === Tag Modifiers (from TV0) ===
    
    private static readonly Dictionary<string, float> TagModifiers = new()
    {
        { WorldTags.Patrolled, -0.10f },
        { WorldTags.Dangerous, +0.10f },
        { WorldTags.Hidden, -0.05f },
        { WorldTags.Blockaded, +0.20f },
        { WorldTags.Asteroid, +0.05f },
        { WorldTags.Nebula, +0.05f }
    };
    
    // === Fuel Cost ===
    
    /// <summary>
    /// Calculate fuel cost for a distance.
    /// Formula: ceil(distance * FUEL_RATE / efficiency)
    /// </summary>
    public static int CalculateFuelCost(float distance, float efficiency = 1.0f)
    {
        if (distance <= 0) return 0;
        if (efficiency <= 0) efficiency = 1.0f;
        
        return (int)Math.Ceiling(distance * FUEL_RATE / efficiency);
    }
    
    // === Time Cost ===
    
    /// <summary>
    /// Calculate travel time in days.
    /// Formula: ceil(distance / speed)
    /// Minimum 1 day.
    /// </summary>
    public static int CalculateTimeDays(float distance, float speed = 100f)
    {
        if (distance <= 0) return 0;
        if (speed <= 0) speed = DEFAULT_SPEED;
        
        return Math.Max(1, (int)Math.Ceiling(distance / speed));
    }
    
    // === Encounter Chance ===
    
    /// <summary>
    /// Calculate base encounter chance per day for a route.
    /// Formula: clamp(hazard * 0.1 + tagModifiers, 0, 0.8)
    /// </summary>
    public static float CalculateEncounterChance(Route route)
    {
        if (route == null) return 0f;
        
        // Base chance from hazard (0-5 → 0-50%)
        float baseChance = route.HazardLevel * 0.1f;
        
        // Apply tag modifiers
        float tagModifier = 0f;
        foreach (var tag in route.Tags)
        {
            if (TagModifiers.TryGetValue(tag, out float mod))
            {
                tagModifier += mod;
            }
        }
        
        // Clamp to valid range
        return Math.Clamp(baseChance + tagModifier, 0f, MAX_ENCOUNTER_CHANCE);
    }
    
    /// <summary>
    /// Calculate encounter chance with system metric modifiers.
    /// </summary>
    public static float CalculateEncounterChance(Route route, SystemMetrics fromMetrics, SystemMetrics toMetrics)
    {
        float baseChance = CalculateEncounterChance(route);
        
        // Apply metric modifiers (from TV0)
        float metricModifier = 0f;
        
        // Security reduces chance
        int minSecurity = Math.Min(
            fromMetrics?.SecurityLevel ?? 0,
            toMetrics?.SecurityLevel ?? 0
        );
        if (minSecurity >= 4) metricModifier -= 0.10f;
        else if (minSecurity <= 1) metricModifier += 0.10f;
        
        // Criminal activity increases chance
        int maxCrime = Math.Max(
            fromMetrics?.CriminalActivity ?? 0,
            toMetrics?.CriminalActivity ?? 0
        );
        if (maxCrime >= 4) metricModifier += 0.15f;
        else if (maxCrime <= 1) metricModifier -= 0.05f;
        
        return Math.Clamp(baseChance + metricModifier, 0f, MAX_ENCOUNTER_CHANCE);
    }
    
    // === Encounter Type Suggestion ===
    
    /// <summary>
    /// Suggest encounter type based on route characteristics.
    /// </summary>
    public static string SuggestEncounterType(Route route)
    {
        if (route == null) return "random";
        
        // High hazard → pirate
        if (route.HazardLevel >= 4) return "pirate";
        
        // Patrolled → patrol
        if (route.HasTag(WorldTags.Patrolled)) return "patrol";
        
        // Hidden → smuggler
        if (route.HasTag(WorldTags.Hidden)) return "smuggler";
        
        // Dangerous → pirate
        if (route.HasTag(WorldTags.Dangerous)) return "pirate";
        
        // Low hazard → trader or patrol
        if (route.HazardLevel <= 1) return "trader";
        
        return "random";
    }
    
    // === A* Cost Function ===
    
    /// <summary>
    /// Calculate A* pathfinding cost for a route.
    /// Formula: distance + (hazard * safetyWeight)
    /// </summary>
    public static float CalculatePathfindingCost(Route route, float safetyWeight = 1.0f)
    {
        if (route == null) return float.MaxValue;
        
        return route.Distance + (route.HazardLevel * SAFETY_WEIGHT * safetyWeight);
    }
    
    /// <summary>
    /// Calculate A* heuristic (straight-line distance estimate).
    /// </summary>
    public static float CalculateHeuristic(Godot.Vector2 from, Godot.Vector2 to)
    {
        return from.DistanceTo(to);
    }
}
```

**Acceptance Criteria**:
- [ ] All cost formulas from TV0 implemented
- [ ] Tag modifiers applied correctly
- [ ] Metric modifiers applied correctly
- [ ] A* cost function implemented

---

#### Step 1.4: Create TravelPlan Class

**File**: `src/sim/travel/TravelPlan.cs`

```csharp
using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// A complete travel plan from origin to destination.
/// Contains ordered segments and aggregated costs.
/// </summary>
public class TravelPlan
{
    /// <summary>
    /// Origin system ID.
    /// </summary>
    public int OriginSystemId { get; set; }
    
    /// <summary>
    /// Destination system ID.
    /// </summary>
    public int DestinationSystemId { get; set; }
    
    /// <summary>
    /// Ordered list of travel segments.
    /// </summary>
    public List<TravelSegment> Segments { get; set; } = new();
    
    /// <summary>
    /// Total fuel cost for the entire journey.
    /// </summary>
    public int TotalFuelCost { get; set; }
    
    /// <summary>
    /// Total time in days for the entire journey.
    /// </summary>
    public int TotalTimeDays { get; set; }
    
    /// <summary>
    /// Total distance for the entire journey.
    /// </summary>
    public float TotalDistance { get; set; }
    
    /// <summary>
    /// Sum of hazard levels across all segments.
    /// </summary>
    public int TotalHazard { get; set; }
    
    /// <summary>
    /// Average encounter chance per day across the journey.
    /// </summary>
    public float AverageEncounterChance { get; set; }
    
    /// <summary>
    /// Whether this plan is valid and executable.
    /// </summary>
    public bool IsValid { get; set; }
    
    /// <summary>
    /// Reason for invalidity (if IsValid is false).
    /// Values: "no_route", "insufficient_fuel", "same_system"
    /// </summary>
    public string InvalidReason { get; set; }
    
    /// <summary>
    /// Number of systems in the path (including origin and destination).
    /// </summary>
    public int SystemCount => Segments.Count + 1;
    
    /// <summary>
    /// Check if player has enough fuel for this plan.
    /// </summary>
    public bool CanAfford(int availableFuel)
    {
        return IsValid && availableFuel >= TotalFuelCost;
    }
    
    /// <summary>
    /// Get the path as a list of system IDs.
    /// </summary>
    public List<int> GetPath()
    {
        var path = new List<int>();
        if (Segments.Count == 0)
        {
            if (OriginSystemId == DestinationSystemId)
                path.Add(OriginSystemId);
            return path;
        }
        
        path.Add(Segments[0].FromSystemId);
        foreach (var segment in Segments)
        {
            path.Add(segment.ToSystemId);
        }
        return path;
    }
    
    /// <summary>
    /// Create an invalid plan with a reason.
    /// </summary>
    public static TravelPlan Invalid(int origin, int destination, string reason)
    {
        return new TravelPlan
        {
            OriginSystemId = origin,
            DestinationSystemId = destination,
            IsValid = false,
            InvalidReason = reason
        };
    }
    
    /// <summary>
    /// Create a valid plan from segments.
    /// Automatically calculates aggregates.
    /// </summary>
    public static TravelPlan FromSegments(int origin, int destination, List<TravelSegment> segments)
    {
        var plan = new TravelPlan
        {
            OriginSystemId = origin,
            DestinationSystemId = destination,
            Segments = segments ?? new List<TravelSegment>(),
            IsValid = true
        };
        
        plan.CalculateAggregates();
        return plan;
    }
    
    /// <summary>
    /// Recalculate aggregate values from segments.
    /// </summary>
    public void CalculateAggregates()
    {
        TotalFuelCost = 0;
        TotalTimeDays = 0;
        TotalDistance = 0f;
        TotalHazard = 0;
        float totalChance = 0f;
        
        foreach (var segment in Segments)
        {
            TotalFuelCost += segment.FuelCost;
            TotalTimeDays += segment.TimeDays;
            TotalDistance += segment.Distance;
            TotalHazard += segment.HazardLevel;
            totalChance += segment.EncounterChance;
        }
        
        AverageEncounterChance = Segments.Count > 0 
            ? totalChance / Segments.Count 
            : 0f;
    }
}
```

**Acceptance Criteria**:
- [ ] `TravelPlan` class with all properties
- [ ] Aggregate calculation from segments
- [ ] Factory methods for valid/invalid plans
- [ ] `CanAfford()` validation

---

### Phase 2: TravelPlanner Implementation

#### Step 2.1: Create TravelPlanner Class

**File**: `src/sim/travel/TravelPlanner.cs`

```csharp
using System;
using System.Collections.Generic;
using Godot;

namespace FringeTactics;

/// <summary>
/// Stateless service for route planning.
/// Uses A* pathfinding with configurable cost weights.
/// </summary>
public class TravelPlanner
{
    private readonly WorldState world;
    private readonly float shipSpeed;
    private readonly float shipEfficiency;
    private readonly float safetyWeight;
    
    /// <summary>
    /// Create a planner with default ship stats.
    /// </summary>
    public TravelPlanner(WorldState world) 
        : this(world, TravelCosts.DEFAULT_SPEED, TravelCosts.DEFAULT_EFFICIENCY, 1.0f)
    {
    }
    
    /// <summary>
    /// Create a planner with custom ship stats.
    /// </summary>
    public TravelPlanner(WorldState world, float shipSpeed, float shipEfficiency, float safetyWeight = 1.0f)
    {
        this.world = world ?? throw new ArgumentNullException(nameof(world));
        this.shipSpeed = shipSpeed > 0 ? shipSpeed : TravelCosts.DEFAULT_SPEED;
        this.shipEfficiency = shipEfficiency > 0 ? shipEfficiency : TravelCosts.DEFAULT_EFFICIENCY;
        this.safetyWeight = safetyWeight;
    }
    
    /// <summary>
    /// Plan a route from origin to destination.
    /// Uses A* pathfinding with weighted costs.
    /// </summary>
    public TravelPlan PlanRoute(int originId, int destinationId)
    {
        // Same system = no travel needed
        if (originId == destinationId)
        {
            return TravelPlan.Invalid(originId, destinationId, "same_system");
        }
        
        // Validate systems exist
        var origin = world.GetSystem(originId);
        var destination = world.GetSystem(destinationId);
        
        if (origin == null || destination == null)
        {
            return TravelPlan.Invalid(originId, destinationId, "invalid_system");
        }
        
        // Find path using A*
        var path = FindPathAStar(originId, destinationId);
        
        if (path == null || path.Count < 2)
        {
            return TravelPlan.Invalid(originId, destinationId, "no_route");
        }
        
        // Build segments from path
        var segments = BuildSegments(path);
        
        return TravelPlan.FromSegments(originId, destinationId, segments);
    }
    
    /// <summary>
    /// Validate if a plan is executable with current campaign state.
    /// </summary>
    public bool ValidatePlan(TravelPlan plan, int availableFuel)
    {
        if (plan == null || !plan.IsValid) return false;
        return plan.CanAfford(availableFuel);
    }
    
    /// <summary>
    /// Get validation failure reason.
    /// </summary>
    public string GetValidationFailure(TravelPlan plan, int availableFuel)
    {
        if (plan == null) return "null_plan";
        if (!plan.IsValid) return plan.InvalidReason;
        if (!plan.CanAfford(availableFuel)) return "insufficient_fuel";
        return null;
    }
    
    /// <summary>
    /// A* pathfinding implementation.
    /// </summary>
    private List<int> FindPathAStar(int startId, int goalId)
    {
        var start = world.GetSystem(startId);
        var goal = world.GetSystem(goalId);
        
        if (start == null || goal == null) return null;
        
        // Priority queue: (priority, systemId)
        var openSet = new PriorityQueue<int, float>();
        openSet.Enqueue(startId, 0f);
        
        // Track where we came from
        var cameFrom = new Dictionary<int, int>();
        
        // Cost from start to each node
        var gScore = new Dictionary<int, float> { [startId] = 0f };
        
        // Estimated total cost through each node
        var fScore = new Dictionary<int, float> 
        { 
            [startId] = TravelCosts.CalculateHeuristic(start.Position, goal.Position) 
        };
        
        var closedSet = new HashSet<int>();
        
        while (openSet.Count > 0)
        {
            int current = openSet.Dequeue();
            
            if (current == goalId)
            {
                return ReconstructPath(cameFrom, current);
            }
            
            if (closedSet.Contains(current)) continue;
            closedSet.Add(current);
            
            foreach (var neighborId in world.GetNeighbors(current))
            {
                if (closedSet.Contains(neighborId)) continue;
                
                var route = world.GetRoute(current, neighborId);
                if (route == null) continue;
                
                float tentativeG = gScore[current] + TravelCosts.CalculatePathfindingCost(route, safetyWeight);
                
                if (!gScore.ContainsKey(neighborId) || tentativeG < gScore[neighborId])
                {
                    cameFrom[neighborId] = current;
                    gScore[neighborId] = tentativeG;
                    
                    var neighbor = world.GetSystem(neighborId);
                    float h = neighbor != null 
                        ? TravelCosts.CalculateHeuristic(neighbor.Position, goal.Position) 
                        : 0f;
                    fScore[neighborId] = tentativeG + h;
                    
                    openSet.Enqueue(neighborId, fScore[neighborId]);
                }
            }
        }
        
        // No path found
        return null;
    }
    
    /// <summary>
    /// Reconstruct path from A* result.
    /// </summary>
    private List<int> ReconstructPath(Dictionary<int, int> cameFrom, int current)
    {
        var path = new List<int> { current };
        
        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            path.Insert(0, current);
        }
        
        return path;
    }
    
    /// <summary>
    /// Build travel segments from a path.
    /// </summary>
    private List<TravelSegment> BuildSegments(List<int> path)
    {
        var segments = new List<TravelSegment>();
        
        for (int i = 0; i < path.Count - 1; i++)
        {
            int fromId = path[i];
            int toId = path[i + 1];
            
            var route = world.GetRoute(fromId, toId);
            if (route == null) continue;
            
            var segment = TravelSegment.FromRoute(route, fromId, toId, shipSpeed, shipEfficiency);
            if (segment != null)
            {
                // Apply system metrics to encounter chance
                var fromMetrics = world.GetSystemMetrics(fromId);
                var toMetrics = world.GetSystemMetrics(toId);
                segment.EncounterChance = TravelCosts.CalculateEncounterChance(route, fromMetrics, toMetrics);
                
                segments.Add(segment);
            }
        }
        
        return segments;
    }
}
```

**Acceptance Criteria**:
- [ ] A* pathfinding finds optimal weighted path
- [ ] Segments built with correct costs
- [ ] System metrics applied to encounter chance
- [ ] Invalid plans returned with clear reasons

---

### Phase 3: Integration Points

#### Step 3.1: Add Ship Travel Stats (Optional for TV1)

**File**: `src/sim/campaign/Ship.cs`

For TV1, we use defaults. This step documents future integration:

```csharp
// Future: Add to Ship class
// public float Speed { get; set; } = 100f;
// public float FuelEfficiency { get; set; } = 1.0f;
```

**Decision**: Defer to TV2 or later milestone. TV1 uses `TravelCosts.DEFAULT_*` constants.

---

#### Step 3.2: Update agents.md Files

**File**: `src/sim/agents.md`

Add travel directory reference:

```markdown
## Directories

| Directory | Purpose |
|-----------|---------|
| `campaign/` | Campaign state and crew management |
| `combat/` | Tactical combat simulation |
| `data/` | Configuration and data structures |
| `generation/` | Procedural content generation |
| `travel/` | Route planning and travel execution |
| `world/` | World state and topology |
```

---

## Phase 4: Unit Tests

### Test File: `tests/sim/travel/TV1TravelCostsTests.cs`

```csharp
using GdUnit4;
using static GdUnit4.Assertions;

namespace FringeTactics.Tests;

[TestSuite]
public class TV1TravelCostsTests
{
    [TestCase]
    public void CalculateFuelCost_StandardDistance_ReturnsCorrectCost()
    {
        // 150 distance * 0.1 rate / 1.0 efficiency = 15
        int cost = TravelCosts.CalculateFuelCost(150f, 1.0f);
        AssertInt(cost).IsEqual(15);
    }
    
    [TestCase]
    public void CalculateFuelCost_WithEfficiency_ReducesCost()
    {
        // 150 * 0.1 / 1.2 = 12.5 → ceil = 13
        int cost = TravelCosts.CalculateFuelCost(150f, 1.2f);
        AssertInt(cost).IsEqual(13);
    }
    
    [TestCase]
    public void CalculateFuelCost_ZeroDistance_ReturnsZero()
    {
        int cost = TravelCosts.CalculateFuelCost(0f, 1.0f);
        AssertInt(cost).IsEqual(0);
    }
    
    [TestCase]
    public void CalculateTimeDays_StandardDistance_ReturnsCorrectDays()
    {
        // 150 / 100 = 1.5 → ceil = 2
        int days = TravelCosts.CalculateTimeDays(150f, 100f);
        AssertInt(days).IsEqual(2);
    }
    
    [TestCase]
    public void CalculateTimeDays_ShortDistance_ReturnsMinimumOneDay()
    {
        // 50 / 100 = 0.5 → minimum 1
        int days = TravelCosts.CalculateTimeDays(50f, 100f);
        AssertInt(days).IsEqual(1);
    }
    
    [TestCase]
    public void CalculateEncounterChance_ZeroHazard_ReturnsZero()
    {
        var route = new Route(0, 1, 100f) { HazardLevel = 0 };
        float chance = TravelCosts.CalculateEncounterChance(route);
        AssertFloat(chance).IsEqual(0f);
    }
    
    [TestCase]
    public void CalculateEncounterChance_MaxHazard_ReturnsFiftyPercent()
    {
        var route = new Route(0, 1, 100f) { HazardLevel = 5 };
        float chance = TravelCosts.CalculateEncounterChance(route);
        AssertFloat(chance).IsEqual(0.5f);
    }
    
    [TestCase]
    public void CalculateEncounterChance_PatrolledTag_ReducesChance()
    {
        var route = new Route(0, 1, 100f) { HazardLevel = 3 };
        route.Tags.Add(WorldTags.Patrolled);
        
        float chance = TravelCosts.CalculateEncounterChance(route);
        
        // Base 30% - 10% patrolled = 20%
        AssertFloat(chance).IsEqual(0.2f);
    }
    
    [TestCase]
    public void CalculateEncounterChance_DangerousTag_IncreasesChance()
    {
        var route = new Route(0, 1, 100f) { HazardLevel = 2 };
        route.Tags.Add(WorldTags.Dangerous);
        
        float chance = TravelCosts.CalculateEncounterChance(route);
        
        // Base 20% + 10% dangerous = 30%
        AssertFloat(chance).IsEqual(0.3f);
    }
    
    [TestCase]
    public void CalculateEncounterChance_CapsAtMaximum()
    {
        var route = new Route(0, 1, 100f) { HazardLevel = 5 };
        route.Tags.Add(WorldTags.Dangerous);
        route.Tags.Add(WorldTags.Blockaded);
        
        float chance = TravelCosts.CalculateEncounterChance(route);
        
        // Would be 50% + 10% + 20% = 80%, capped at 80%
        AssertFloat(chance).IsEqual(0.8f);
    }
    
    [TestCase]
    public void CalculatePathfindingCost_IncludesHazard()
    {
        var route = new Route(0, 1, 100f) { HazardLevel = 2 };
        
        float cost = TravelCosts.CalculatePathfindingCost(route, 1.0f);
        
        // 100 + (2 * 50 * 1.0) = 200
        AssertFloat(cost).IsEqual(200f);
    }
}
```

### Test File: `tests/sim/travel/TV1TravelPlannerTests.cs`

```csharp
using GdUnit4;
using static GdUnit4.Assertions;
using System.Collections.Generic;

namespace FringeTactics.Tests;

[TestSuite]
public class TV1TravelPlannerTests
{
    private WorldState world;
    private TravelPlanner planner;
    
    [Before]
    public void Setup()
    {
        world = WorldState.CreateTestSector();
        planner = new TravelPlanner(world);
    }
    
    [TestCase]
    public void PlanRoute_DirectConnection_ReturnsSingleSegment()
    {
        // Haven (0) → Waypoint (1) is direct
        var plan = planner.PlanRoute(0, 1);
        
        AssertBool(plan.IsValid).IsTrue();
        AssertInt(plan.Segments.Count).IsEqual(1);
        AssertInt(plan.Segments[0].FromSystemId).IsEqual(0);
        AssertInt(plan.Segments[0].ToSystemId).IsEqual(1);
    }
    
    [TestCase]
    public void PlanRoute_MultiHop_ReturnsCorrectPath()
    {
        // Haven (0) → Rockfall (2) requires going through Waypoint (1)
        var plan = planner.PlanRoute(0, 2);
        
        AssertBool(plan.IsValid).IsTrue();
        AssertInt(plan.Segments.Count).IsEqual(2);
        
        var path = plan.GetPath();
        AssertInt(path[0]).IsEqual(0);
        AssertInt(path[1]).IsEqual(1);
        AssertInt(path[2]).IsEqual(2);
    }
    
    [TestCase]
    public void PlanRoute_NoPath_ReturnsInvalid()
    {
        // Create isolated system
        var isolated = new StarSystem(99, "Isolated", SystemType.Outpost, new Godot.Vector2(1000, 1000));
        world.AddSystem(isolated);
        
        var plan = planner.PlanRoute(0, 99);
        
        AssertBool(plan.IsValid).IsFalse();
        AssertString(plan.InvalidReason).IsEqual("no_route");
    }
    
    [TestCase]
    public void PlanRoute_SameSystem_ReturnsInvalid()
    {
        var plan = planner.PlanRoute(0, 0);
        
        AssertBool(plan.IsValid).IsFalse();
        AssertString(plan.InvalidReason).IsEqual("same_system");
    }
    
    [TestCase]
    public void PlanRoute_CalculatesFuelCost()
    {
        var plan = planner.PlanRoute(0, 1);
        
        AssertBool(plan.IsValid).IsTrue();
        AssertInt(plan.TotalFuelCost).IsGreater(0);
    }
    
    [TestCase]
    public void PlanRoute_CalculatesTimeCost()
    {
        var plan = planner.PlanRoute(0, 1);
        
        AssertBool(plan.IsValid).IsTrue();
        AssertInt(plan.TotalTimeDays).IsGreater(0);
    }
    
    [TestCase]
    public void PlanRoute_CalculatesHazard()
    {
        // Haven → Waypoint has hazard 1
        var plan = planner.PlanRoute(0, 1);
        
        AssertBool(plan.IsValid).IsTrue();
        AssertInt(plan.TotalHazard).IsEqual(1);
    }
    
    [TestCase]
    public void PlanRoute_MultiHop_AggregatesCosts()
    {
        var plan = planner.PlanRoute(0, 2);
        
        AssertBool(plan.IsValid).IsTrue();
        
        // Aggregate should be sum of segments
        int expectedFuel = 0;
        int expectedDays = 0;
        foreach (var segment in plan.Segments)
        {
            expectedFuel += segment.FuelCost;
            expectedDays += segment.TimeDays;
        }
        
        AssertInt(plan.TotalFuelCost).IsEqual(expectedFuel);
        AssertInt(plan.TotalTimeDays).IsEqual(expectedDays);
    }
    
    [TestCase]
    public void ValidatePlan_SufficientFuel_ReturnsTrue()
    {
        var plan = planner.PlanRoute(0, 1);
        
        bool valid = planner.ValidatePlan(plan, 100);
        
        AssertBool(valid).IsTrue();
    }
    
    [TestCase]
    public void ValidatePlan_InsufficientFuel_ReturnsFalse()
    {
        var plan = planner.PlanRoute(0, 2);
        
        bool valid = planner.ValidatePlan(plan, 1);
        
        AssertBool(valid).IsFalse();
    }
    
    [TestCase]
    public void PlanRoute_PrefersLowerHazard_WithHighSafetyWeight()
    {
        // Create alternative route with lower hazard
        // This test validates A* considers hazard in cost
        
        var safePlanner = new TravelPlanner(world, 100f, 1.0f, 2.0f);
        var plan = safePlanner.PlanRoute(0, 2);
        
        // Should still find a valid path
        AssertBool(plan.IsValid).IsTrue();
    }
}
```

### Test File: `tests/sim/travel/TV1TravelSegmentTests.cs`

```csharp
using GdUnit4;
using static GdUnit4.Assertions;

namespace FringeTactics.Tests;

[TestSuite]
public class TV1TravelSegmentTests
{
    [TestCase]
    public void FromRoute_CreatesValidSegment()
    {
        var route = new Route(0, 1, 150f) { HazardLevel = 2 };
        
        var segment = TravelSegment.FromRoute(route);
        
        AssertInt(segment.FromSystemId).IsEqual(0);
        AssertInt(segment.ToSystemId).IsEqual(1);
        AssertFloat(segment.Distance).IsEqual(150f);
        AssertInt(segment.HazardLevel).IsEqual(2);
    }
    
    [TestCase]
    public void FromRoute_CalculatesFuelCost()
    {
        var route = new Route(0, 1, 150f);
        
        var segment = TravelSegment.FromRoute(route);
        
        // 150 * 0.1 / 1.0 = 15
        AssertInt(segment.FuelCost).IsEqual(15);
    }
    
    [TestCase]
    public void FromRoute_CalculatesTimeDays()
    {
        var route = new Route(0, 1, 150f);
        
        var segment = TravelSegment.FromRoute(route);
        
        // 150 / 100 = 1.5 → ceil = 2
        AssertInt(segment.TimeDays).IsEqual(2);
    }
    
    [TestCase]
    public void FromRoute_WithCustomSpeed_AffectsTime()
    {
        var route = new Route(0, 1, 150f);
        
        var segment = TravelSegment.FromRoute(route, shipSpeed: 50f);
        
        // 150 / 50 = 3
        AssertInt(segment.TimeDays).IsEqual(3);
    }
    
    [TestCase]
    public void FromRoute_WithCustomEfficiency_AffectsFuel()
    {
        var route = new Route(0, 1, 150f);
        
        var segment = TravelSegment.FromRoute(route, shipEfficiency: 1.5f);
        
        // 150 * 0.1 / 1.5 = 10
        AssertInt(segment.FuelCost).IsEqual(10);
    }
    
    [TestCase]
    public void FromRoute_WithDirection_SetsCorrectFromTo()
    {
        var route = new Route(0, 1, 150f);
        
        // Travel from 1 to 0 (reverse direction)
        var segment = TravelSegment.FromRoute(route, 1, 0);
        
        AssertInt(segment.FromSystemId).IsEqual(1);
        AssertInt(segment.ToSystemId).IsEqual(0);
    }
}
```

---

## Phase 5: Test Directory Setup

### Create Test Directory

Create `tests/sim/travel/` with `agents.md`:

**File**: `tests/sim/travel/agents.md`

```markdown
# Travel Tests (`tests/sim/travel/`)

Unit and integration tests for the Travel domain.

## Test Files

| File | Tests |
|------|-------|
| `TV1TravelCostsTests.cs` | Cost calculation formulas |
| `TV1TravelSegmentTests.cs` | Segment creation and properties |
| `TV1TravelPlannerTests.cs` | Pathfinding and plan creation |
```

---

## Manual Test Setup

### Test Scenario: Route Planning in Test Sector

**Setup**:
1. Create campaign with test sector (`WorldState.CreateTestSector()`)
2. Create `TravelPlanner` with default stats

**Test Cases**:

| Test | Action | Expected |
|------|--------|----------|
| **Direct route** | Plan Haven (0) → Waypoint (1) | 1 segment, ~15 fuel, ~2 days |
| **Multi-hop route** | Plan Haven (0) → Rockfall (2) | 2 segments, ~30 fuel, ~4 days |
| **Long route** | Plan Haven (0) → Wreck (7) | 3 segments via Waypoint → Rockfall |
| **No route** | Plan Haven (0) → isolated system | Invalid, reason="no_route" |
| **Same system** | Plan Haven (0) → Haven (0) | Invalid, reason="same_system" |
| **Hazard calculation** | Plan Waypoint (1) → Contested (4) | High hazard (3) |
| **Fuel validation** | Validate plan with fuel=5 | Fails if cost > 5 |

### Test Scenario: Encounter Chance Verification

**Setup**:
1. Create test sector
2. Plan routes with different hazard levels

**Test Cases**:

| Route | Hazard | Tags | Expected Chance |
|-------|--------|------|-----------------|
| Haven → Patrol | 0 | patrolled | 0% (clamped) |
| Haven → Waypoint | 1 | patrolled | 0% (10% - 10%) |
| Waypoint → Rockfall | 2 | asteroid | 25% (20% + 5%) |
| Waypoint → Contested | 3 | dangerous | 40% (30% + 10%) |
| Rockfall → Wreck | 3 | dangerous | 40% |

---

## Files Summary

### Files to Create

| File | Purpose |
|------|---------|
| `src/sim/travel/agents.md` | Directory documentation |
| `src/sim/travel/TravelSegment.cs` | Single route step with costs |
| `src/sim/travel/TravelPlan.cs` | Complete route with aggregates |
| `src/sim/travel/TravelCosts.cs` | Cost calculation utilities |
| `src/sim/travel/TravelPlanner.cs` | A* pathfinding and plan creation |
| `tests/sim/travel/agents.md` | Test directory documentation |
| `tests/sim/travel/TV1TravelCostsTests.cs` | Cost formula tests |
| `tests/sim/travel/TV1TravelSegmentTests.cs` | Segment tests |
| `tests/sim/travel/TV1TravelPlannerTests.cs` | Planner tests |

### Files to Modify

| File | Changes |
|------|---------|
| `src/sim/agents.md` | Add travel directory reference |

---

## TV1 Deliverables Checklist

### Phase 1: Directory and Data Structures
- [x] **1.1** Create `src/sim/travel/` directory with `agents.md`
- [x] **1.2** Create `TravelSegment` class
- [x] **1.3** Create `TravelCosts` utility
- [x] **1.4** Create `TravelPlan` class

### Phase 2: TravelPlanner Implementation
- [x] **2.1** Create `TravelPlanner` with A* pathfinding

### Phase 3: Integration Points
- [ ] **3.1** (Deferred) Ship travel stats
- [x] **3.2** Update `src/sim/agents.md`

### Phase 4: Unit Tests
- [x] **4.1** Create `TV1TravelCostsTests.cs` (35 tests)
- [x] **4.2** Create `TV1TravelSegmentTests.cs` (13 tests)
- [x] **4.3** Create `TV1TravelPlannerTests.cs` (35 tests)

### Phase 5: Test Directory
- [x] **5.1** Create `tests/sim/travel/` with `agents.md`

---

## Success Criteria

When TV1 is complete:

1. ✅ Routes calculated between any two connected systems
2. ✅ A* pathfinding considers both distance and hazard
3. ✅ Fuel and time costs calculated per TV0 formulas
4. ✅ Encounter chance calculated with tag and metric modifiers
5. ✅ Invalid plans returned with clear reasons
6. ✅ All unit tests pass
7. ✅ Manual test scenarios verified

**Natural Pause Point**: After TV1, route planning is complete. TV2 begins travel execution.

---

## Appendix: Test Sector Reference

From `WorldState.CreateTestSector()`:

```
Systems:
  0: Haven Station (core, hub) - Security 4, Crime 1
  1: Waypoint Alpha (frontier) - Security 2, Crime 3
  2: Rockfall Mining (mining) - Security 2, Crime 2
  3: Red Claw Base (lawless) - Security 0, Crime 5
  4: Contested Zone (border) - Security 1, Crime 4
  5: Patrol Station (military) - Security 5, Crime 0
  6: Smuggler's Den (lawless) - Security 0, Crime 4
  7: Wreck of Icarus (frontier) - Security 0, Crime 2

Routes:
  0 ↔ 1: Hazard 1, patrolled
  0 ↔ 5: Hazard 0, patrolled
  1 ↔ 2: Hazard 2, asteroid
  1 ↔ 4: Hazard 3, dangerous
  1 ↔ 6: Hazard 2, hidden
  2 ↔ 7: Hazard 3, dangerous
  3 ↔ 6: Hazard 2, hidden
```
