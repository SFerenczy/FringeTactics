# M2 – Visibility & Fog of War: Implementation Plan

This document breaks down **Milestone 2** from `ROADMAP.md` into concrete implementation steps.

**Goal**: Add the information layer (what the player knows) without yet worrying about combat.

---

## Current State Assessment

### What We Have (From M0–M1)

| Component | Status | Notes |
|-----------|--------|-------|
| `MapState` | ✅ Complete | TileType (Floor/Wall/Void), `BlocksLOS()` method |
| `CombatResolver` | ✅ Complete | `HasLineOfSight()` using Bresenham's algorithm |
| `Actor` | ✅ Complete | Position, movement, no vision radius yet |
| `MissionView` | ✅ Complete | Grid rendering, actor views, selection |
| `AIController` | ✅ Complete | Uses LOS for targeting (but no fog awareness) |

### What M2 Requires vs What We Have

| M2 Requirement | Current Status | Gap |
|----------------|----------------|-----|
| Line-of-sight blocked by walls/doors | ✅ Partial | `HasLineOfSight()` exists but uses `IsWalkable()` not `BlocksLOS()` |
| Per-unit vision radius | ❌ Missing | Need `VisionRadius` property on Actor |
| Fog-of-war states per cell | ❌ Missing | Need visibility state tracking in sim layer |
| Unknown / Seen / Visible states | ❌ Missing | Need `VisibilityState` enum and per-tile tracking |
| Visual representation of fog | ❌ Missing | Need fog overlay in MissionView |

---

## Architecture Decisions

### Where Does Visibility State Live?

**Decision**: Visibility state lives in the **sim layer** (`src/sim/combat/`), not the view.

**Rationale**:
- AI needs to know what the player can see (for stealth in M6)
- Combat resolution may need visibility checks
- Fog state is game state, not presentation
- Follows architecture: "sim doesn't know about Godot nodes"

**Implementation**:
- New `VisibilitySystem` in `src/sim/combat/`
- `CombatState` owns a `VisibilitySystem` instance
- `MissionView` reads visibility state for rendering fog

### Per-Tile vs Per-Actor Visibility

**Decision**: Track visibility **per-tile** from the **player's perspective**.

**Options Considered**:
1. **Per-actor visibility** - Each actor tracks what they can see → complex, needed for AI stealth
2. **Per-tile player visibility** - Single "what can the player see" map → simpler, sufficient for M2 ✅
3. **Both** - Per-actor for AI, aggregated for player → future extension

**Chosen Approach**: Per-tile player visibility for M2
- Each tile has a `VisibilityState`: Unknown, Revealed, Visible
- Visible = currently in LOS of any crew member
- Revealed = was visible at some point, but not currently
- Unknown = never seen

**Future Extension Point**: M6 (Stealth) will need per-actor visibility for enemies.

### LOS Algorithm

**Decision**: Keep Bresenham's line algorithm, but fix it to use `BlocksLOS()`.

**Current Issue**: `CombatResolver.HasLineOfSight()` checks `IsWalkable()` which is wrong:
- A closed door is not walkable but might not block LOS (if glass)
- Future: some cover might be walkable but block LOS

**Fix**: Use `MapState.BlocksLOS()` instead of `IsWalkable()`.

---

## Implementation Steps

### Phase 1: Sim Layer Foundation (Priority: Critical)

#### Step 1.1: Add Vision Radius to Actor

**File**: `src/sim/combat/Actor.cs`

**Changes**:
```csharp
// Add to Actor class
public int VisionRadius { get; set; } = 8; // tiles
```

**Notes**:
- Default of 8 tiles is reasonable for indoor maps
- Can be modified per-actor for scouts, snipers, etc.
- Future: may vary by lighting conditions

**Acceptance Criteria**:
- [ ] `Actor.VisionRadius` property exists with default value
- [ ] Can be set during actor creation

---

#### Step 1.2: Create VisibilityState Enum

**New File**: `src/sim/combat/VisibilityState.cs`

```csharp
namespace FringeTactics;

/// <summary>
/// Visibility state of a tile from the player's perspective.
/// </summary>
public enum VisibilityState
{
    /// <summary>
    /// Never seen by any crew member. Contents unknown.
    /// </summary>
    Unknown,
    
    /// <summary>
    /// Was visible at some point, but not currently in LOS.
    /// Player remembers terrain but not dynamic elements (enemies).
    /// </summary>
    Revealed,
    
    /// <summary>
    /// Currently in LOS of at least one crew member.
    /// All contents visible and targetable.
    /// </summary>
    Visible
}
```

**Acceptance Criteria**:
- [ ] `VisibilityState` enum exists with three states
- [ ] XML documentation explains each state

---

#### Step 1.3: Create VisibilitySystem

**New File**: `src/sim/combat/VisibilitySystem.cs`

```csharp
using Godot; // For Vector2I only
using System;
using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// Tracks fog-of-war visibility state for the player.
/// Updated each tick based on crew positions and LOS.
/// </summary>
public class VisibilitySystem
{
    private readonly MapState map;
    private VisibilityState[] tileStates;
    private HashSet<Vector2I> currentlyVisible = new();
    
    // Event fired when visibility changes (for view updates)
    public event Action VisibilityChanged;
    
    public VisibilitySystem(MapState map)
    {
        this.map = map;
        InitializeTileStates();
    }
    
    private void InitializeTileStates()
    {
        var totalTiles = map.GridSize.X * map.GridSize.Y;
        tileStates = new VisibilityState[totalTiles];
        
        // All tiles start as Unknown
        for (int i = 0; i < totalTiles; i++)
        {
            tileStates[i] = VisibilityState.Unknown;
        }
    }
    
    /// <summary>
    /// Get the visibility state of a tile.
    /// </summary>
    public VisibilityState GetVisibility(Vector2I pos)
    {
        if (!map.IsInBounds(pos))
        {
            return VisibilityState.Unknown;
        }
        return tileStates[GetIndex(pos)];
    }
    
    /// <summary>
    /// Check if a tile is currently visible (in LOS of any crew).
    /// </summary>
    public bool IsVisible(Vector2I pos)
    {
        return GetVisibility(pos) == VisibilityState.Visible;
    }
    
    /// <summary>
    /// Check if a tile has ever been seen (Revealed or Visible).
    /// </summary>
    public bool IsRevealed(Vector2I pos)
    {
        var state = GetVisibility(pos);
        return state == VisibilityState.Visible || state == VisibilityState.Revealed;
    }
    
    /// <summary>
    /// Update visibility based on current crew positions.
    /// Called each tick by CombatState.
    /// </summary>
    public void UpdateVisibility(IEnumerable<Actor> actors)
    {
        // Mark all currently visible tiles as revealed
        foreach (var pos in currentlyVisible)
        {
            if (map.IsInBounds(pos))
            {
                tileStates[GetIndex(pos)] = VisibilityState.Revealed;
            }
        }
        currentlyVisible.Clear();
        
        // Calculate new visibility from all crew actors
        foreach (var actor in actors)
        {
            if (actor.Type != "crew" || actor.State != ActorState.Alive)
            {
                continue;
            }
            
            CalculateActorVisibility(actor);
        }
        
        // Mark newly visible tiles
        foreach (var pos in currentlyVisible)
        {
            tileStates[GetIndex(pos)] = VisibilityState.Visible;
        }
        
        VisibilityChanged?.Invoke();
    }
    
    private void CalculateActorVisibility(Actor actor)
    {
        var origin = actor.GridPosition;
        var radius = actor.VisionRadius;
        
        // Check all tiles within vision radius
        for (int dy = -radius; dy <= radius; dy++)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                var target = origin + new Vector2I(dx, dy);
                
                if (!map.IsInBounds(target))
                {
                    continue;
                }
                
                // Check if within circular radius
                if (dx * dx + dy * dy > radius * radius)
                {
                    continue;
                }
                
                // Check line of sight
                if (HasLineOfSight(origin, target))
                {
                    currentlyVisible.Add(target);
                }
            }
        }
    }
    
    /// <summary>
    /// Check if there's clear line of sight between two positions.
    /// Uses Bresenham's line algorithm.
    /// </summary>
    public bool HasLineOfSight(Vector2I from, Vector2I to)
    {
        // Same tile is always visible
        if (from == to)
        {
            return true;
        }
        
        var points = GetLinePoints(from, to);
        
        // Check all intermediate points (skip start, include end)
        for (int i = 1; i < points.Length; i++)
        {
            var point = points[i];
            
            // If this point blocks LOS, we can't see past it
            // But we CAN see the blocking tile itself
            if (map.BlocksLOS(point))
            {
                // We can see up to and including this tile, but not beyond
                return i == points.Length - 1; // Only true if this is the target
            }
        }
        
        return true;
    }
    
    /// <summary>
    /// Bresenham's line algorithm to get points between two grid positions.
    /// </summary>
    private Vector2I[] GetLinePoints(Vector2I from, Vector2I to)
    {
        var points = new List<Vector2I>();
        
        int x0 = from.X, y0 = from.Y;
        int x1 = to.X, y1 = to.Y;
        
        int dx = Math.Abs(x1 - x0);
        int dy = Math.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;
        
        while (true)
        {
            points.Add(new Vector2I(x0, y0));
            
            if (x0 == x1 && y0 == y1)
            {
                break;
            }
            
            int e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x0 += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                y0 += sy;
            }
        }
        
        return points.ToArray();
    }
    
    private int GetIndex(Vector2I pos)
    {
        return pos.Y * map.GridSize.X + pos.X;
    }
    
    /// <summary>
    /// Reveal a specific tile (e.g., from hacked camera in future).
    /// </summary>
    public void RevealTile(Vector2I pos)
    {
        if (!map.IsInBounds(pos))
        {
            return;
        }
        
        var index = GetIndex(pos);
        if (tileStates[index] == VisibilityState.Unknown)
        {
            tileStates[index] = VisibilityState.Revealed;
            VisibilityChanged?.Invoke();
        }
    }
    
    /// <summary>
    /// Reveal all tiles (debug/cheat).
    /// </summary>
    public void RevealAll()
    {
        for (int i = 0; i < tileStates.Length; i++)
        {
            if (tileStates[i] == VisibilityState.Unknown)
            {
                tileStates[i] = VisibilityState.Revealed;
            }
        }
        VisibilityChanged?.Invoke();
    }
    
    /// <summary>
    /// Get all currently visible tiles (for efficient rendering).
    /// </summary>
    public IReadOnlyCollection<Vector2I> GetVisibleTiles()
    {
        return currentlyVisible;
    }
}
```

**Acceptance Criteria**:
- [ ] `VisibilitySystem` class exists
- [ ] `GetVisibility()` returns correct state for any tile
- [ ] `UpdateVisibility()` correctly calculates LOS from crew
- [ ] Tiles transition: Unknown → Visible → Revealed correctly
- [ ] `VisibilityChanged` event fires when state changes

---

#### Step 1.4: Integrate VisibilitySystem into CombatState

**File**: `src/sim/combat/CombatState.cs`

**Changes**:

```csharp
// Add property
public VisibilitySystem Visibility { get; private set; }

// In constructor, after MapState is created:
Visibility = new VisibilitySystem(MapState);

// In ProcessTick(), after actor movement:
Visibility.UpdateVisibility(Actors);
```

**Detailed Changes**:

1. Add property declaration near other systems:
```csharp
public VisibilitySystem Visibility { get; private set; }
```

2. Initialize in constructor after MapState:
```csharp
public CombatState(int seed)
{
    Rng = new CombatRng(seed);
    MissionConfig = null;
    Actors = new List<Actor>();
    MapState = new MapState();
    TimeSystem = new TimeSystem();
    Visibility = new VisibilitySystem(MapState);  // Add this
    // ... rest of constructor
}
```

3. Update visibility each tick in `ProcessTick()`:
```csharp
private void ProcessTick()
{
    var tickDuration = TimeSystem.TickDuration;
    
    aiController.Tick();
    AbilitySystem.Tick();
    ProcessAttacks();
    ResolveMovementCollisions();
    
    foreach (var actor in Actors)
    {
        actor.Tick(tickDuration);
    }
    
    // Update visibility after movement
    Visibility.UpdateVisibility(Actors);
    
    CheckMissionEnd();
}
```

**Acceptance Criteria**:
- [ ] `CombatState.Visibility` property exists
- [ ] Visibility is updated each tick
- [ ] Visibility updates after actor movement (so new positions are considered)

---

#### Step 1.5: Fix CombatResolver.HasLineOfSight

**File**: `src/sim/combat/CombatResolver.cs`

**Current Code**:
```csharp
if (!map.IsWalkable(points[i]))
{
    return false;
}
```

**Fixed Code**:
```csharp
if (map.BlocksLOS(points[i]))
{
    return false;
}
```

**Acceptance Criteria**:
- [ ] `HasLineOfSight()` uses `BlocksLOS()` instead of `IsWalkable()`
- [ ] Existing combat still works correctly

---

### Phase 2: View Layer Integration (Priority: High)

#### Step 2.1: Create Fog Overlay System

**File**: `src/scenes/mission/MissionView.cs`

**New Members**:
```csharp
// Fog of war overlay
private Node2D fogLayer;
private Dictionary<Vector2I, ColorRect> fogTiles = new();
private bool fogDirty = true;
```

**New Methods**:

```csharp
private void CreateFogLayer()
{
    fogLayer = new Node2D();
    fogLayer.ZIndex = 5; // Above grid, below actors
    fogLayer.Name = "FogLayer";
    AddChild(fogLayer);
    
    // Subscribe to visibility changes
    CombatState.Visibility.VisibilityChanged += OnVisibilityChanged;
    
    // Initial fog creation
    CreateFogTiles();
}

private void CreateFogTiles()
{
    var gridSize = CombatState.MapState.GridSize;
    
    for (int y = 0; y < gridSize.Y; y++)
    {
        for (int x = 0; x < gridSize.X; x++)
        {
            var pos = new Vector2I(x, y);
            var fogTile = new ColorRect();
            fogTile.Size = new Vector2(TileSize, TileSize);
            fogTile.Position = new Vector2(x * TileSize, y * TileSize);
            fogTile.MouseFilter = Control.MouseFilterEnum.Ignore;
            fogLayer.AddChild(fogTile);
            fogTiles[pos] = fogTile;
        }
    }
    
    UpdateFogVisuals();
}

private void OnVisibilityChanged()
{
    fogDirty = true;
}

private void UpdateFogVisuals()
{
    if (!fogDirty)
    {
        return;
    }
    fogDirty = false;
    
    foreach (var kvp in fogTiles)
    {
        var pos = kvp.Key;
        var tile = kvp.Value;
        var visibility = CombatState.Visibility.GetVisibility(pos);
        
        switch (visibility)
        {
            case VisibilityState.Unknown:
                tile.Color = new Color(0.0f, 0.0f, 0.0f, 0.95f); // Nearly opaque black
                tile.Visible = true;
                break;
            case VisibilityState.Revealed:
                tile.Color = new Color(0.0f, 0.0f, 0.0f, 0.5f); // Semi-transparent
                tile.Visible = true;
                break;
            case VisibilityState.Visible:
                tile.Visible = false; // Fully visible, no fog
                break;
        }
    }
}
```

**Integration in _Ready()**:
```csharp
public override void _Ready()
{
    // ... existing code ...
    
    DrawGrid();
    CreateFogLayer();  // Add after DrawGrid
    SpawnActorViews();
}
```

**Integration in _Process()**:
```csharp
public override void _Process(double delta)
{
    CombatState.Update((float)delta);
    
    UpdateFogVisuals();  // Add this
    UpdateMoveTargetMarker();
    CleanupCompletedMoveTargets();
    // ... rest
}
```

**Acceptance Criteria**:
- [ ] Fog layer renders above grid, below actors
- [ ] Unknown tiles are nearly opaque black
- [ ] Revealed tiles are semi-transparent
- [ ] Visible tiles have no fog overlay
- [ ] Fog updates when visibility changes

---

#### Step 2.2: Hide Enemies in Fog

**File**: `src/scenes/mission/ActorView.cs`

Actors should be hidden when not in visible tiles.

**Option A: MissionView controls visibility**
```csharp
// In MissionView._Process(), after UpdateFogVisuals():
private void UpdateActorFogVisibility()
{
    foreach (var kvp in actorViews)
    {
        var actor = CombatState.GetActorById(kvp.Key);
        var view = kvp.Value;
        
        if (actor == null)
        {
            continue;
        }
        
        // Crew are always visible to player
        if (actor.Type == "crew")
        {
            view.Visible = true;
            continue;
        }
        
        // Enemies only visible if their tile is visible
        var isVisible = CombatState.Visibility.IsVisible(actor.GridPosition);
        view.Visible = isVisible;
    }
}
```

**Acceptance Criteria**:
- [ ] Crew units are always visible
- [ ] Enemy units are hidden when in fog
- [ ] Enemy units appear when entering visible tiles
- [ ] Enemy units disappear when leaving visible tiles

---

#### Step 2.3: Prevent Targeting Through Fog

**File**: `src/scenes/mission/MissionView.cs`

Update `HandleRightClick` to prevent attacking enemies in fog:

```csharp
private void HandleRightClick(Vector2I gridPos)
{
    // ... existing code to find targetActor ...
    
    if (targetActor != null && targetActor.State == ActorState.Alive)
    {
        // Can only target visible enemies
        if (!CombatState.Visibility.IsVisible(targetActor.GridPosition))
        {
            // Target not visible, treat as move order
            IssueGroupMoveOrder(gridPos);
            return;
        }
        
        // ... rest of attack logic ...
    }
}
```

**Acceptance Criteria**:
- [ ] Cannot issue attack orders to enemies in fog
- [ ] Right-clicking fog tile issues move order instead
- [ ] Attack orders still work for visible enemies

---

### Phase 3: AI Awareness (Priority: Medium)

#### Step 3.1: AI Uses Visibility for Targeting

**File**: `src/sim/combat/AIController.cs`

The AI already uses `HasLineOfSight()` for finding targets. For M2, this is sufficient.

**Future (M6)**: AI will need its own visibility tracking for stealth gameplay.

**Current Behavior** (no changes needed for M2):
- AI checks LOS to find targets
- AI doesn't know about fog-of-war (it's omniscient)
- This is acceptable for M2 since we're not implementing stealth yet

**Acceptance Criteria**:
- [ ] AI still functions correctly with visibility system
- [ ] No regressions in AI behavior

---

### Phase 4: Polish & Edge Cases (Priority: Low)

#### Step 4.1: Initial Visibility on Mission Start

**File**: `src/sim/combat/CombatState.cs` or `MissionFactory.cs`

Ensure visibility is calculated before the first frame:

```csharp
// In MissionFactory.BuildSandbox() or similar, after spawning actors:
combat.Visibility.UpdateVisibility(combat.Actors);
```

**Acceptance Criteria**:
- [ ] Entry zone is visible at mission start
- [ ] No "flash" of full fog on first frame

---

#### Step 4.2: Fog Transition Animation (Optional)

**File**: `src/scenes/mission/MissionView.cs`

Smooth transitions when fog state changes:

```csharp
// Instead of instant visibility changes, lerp alpha over time
private Dictionary<Vector2I, float> fogTargetAlpha = new();
private const float FogFadeSpeed = 5.0f;

private void UpdateFogVisuals()
{
    foreach (var kvp in fogTiles)
    {
        var pos = kvp.Key;
        var tile = kvp.Value;
        var visibility = CombatState.Visibility.GetVisibility(pos);
        
        float targetAlpha = visibility switch
        {
            VisibilityState.Unknown => 0.95f,
            VisibilityState.Revealed => 0.5f,
            VisibilityState.Visible => 0.0f,
            _ => 0.0f
        };
        
        // Lerp current alpha toward target
        var currentAlpha = tile.Color.A;
        var newAlpha = Mathf.MoveToward(currentAlpha, targetAlpha, FogFadeSpeed * (float)GetProcessDeltaTime());
        
        tile.Color = new Color(0.0f, 0.0f, 0.0f, newAlpha);
        tile.Visible = newAlpha > 0.01f;
    }
}
```

**Acceptance Criteria**:
- [ ] Fog fades smoothly when revealed
- [ ] Fog appears smoothly when leaving vision
- [ ] Performance is acceptable

---

#### Step 4.3: Debug Overlay for Visibility

**File**: `src/core/DevTools.cs` or new debug overlay

Add a debug toggle to visualize visibility states:

```csharp
// Toggle with F3 or similar
private void DrawVisibilityDebug()
{
    // Draw colored dots on each tile showing visibility state
    // Green = Visible, Yellow = Revealed, Red = Unknown
}
```

**Acceptance Criteria**:
- [ ] Debug overlay can be toggled
- [ ] Shows visibility state per tile
- [ ] Useful for testing LOS edge cases

---

## Testing Checklist

### Test Mission Setup

A dedicated M2 test mission is available from the main menu: **"M2 Test (Visibility & Fog)"**

The test map features:
- **Multiple rooms** separated by walls to test LOS blocking
- **Corridors** with corners to test vision around obstacles
- **4 crew members** in the entry zone (top-left)
- **3 enemies** placed in hidden areas:
  - One in the far room (top-right) - should be hidden initially
  - One behind a wall (center) - tests LOS blocking
  - One in the bottom-right room - requires exploration to find

### Manual Testing

1. **Basic Fog**
   - [ ] Mission starts with fog covering unexplored areas
   - [ ] Entry zone is visible at start
   - [ ] Moving reveals new tiles
   - [ ] Previously seen tiles remain revealed (lighter fog)

2. **Line of Sight**
   - [ ] Walls block vision
   - [ ] Can see up to and including wall tiles
   - [ ] Cannot see behind walls
   - [ ] Vision is circular (radius-based)

3. **Enemy Visibility**
   - [ ] Enemies in fog are hidden
   - [ ] Enemies become visible when in LOS
   - [ ] Enemies disappear when leaving LOS
   - [ ] Cannot target hidden enemies

4. **Multi-Unit Vision**
   - [ ] Multiple crew members contribute to visibility
   - [ ] Visibility is union of all crew vision
   - [ ] Losing a crew member reduces visible area

5. **Edge Cases**
   - [ ] Vision at map edges works correctly
   - [ ] Diagonal LOS through corners (decide on behavior)
   - [ ] Very long LOS (beyond vision radius)

### Automated Tests

Create `tests/sim/combat/VisibilitySystemTests.cs`:

```csharp
[TestClass]
public class VisibilitySystemTests
{
    [TestMethod]
    public void NewMap_AllTilesUnknown()
    {
        var map = MapBuilder.BuildTestMap(new Vector2I(10, 10));
        var visibility = new VisibilitySystem(map);
        
        Assert.AreEqual(VisibilityState.Unknown, visibility.GetVisibility(new Vector2I(5, 5)));
    }
    
    [TestMethod]
    public void CrewAtPosition_NearbyTilesVisible()
    {
        var map = MapBuilder.BuildTestMap(new Vector2I(10, 10));
        var visibility = new VisibilitySystem(map);
        var actor = new Actor(0, "crew") { GridPosition = new Vector2I(5, 5), VisionRadius = 3 };
        
        visibility.UpdateVisibility(new[] { actor });
        
        Assert.AreEqual(VisibilityState.Visible, visibility.GetVisibility(new Vector2I(5, 5)));
        Assert.AreEqual(VisibilityState.Visible, visibility.GetVisibility(new Vector2I(6, 5)));
        Assert.AreEqual(VisibilityState.Unknown, visibility.GetVisibility(new Vector2I(9, 9)));
    }
    
    [TestMethod]
    public void WallBlocksVision()
    {
        var map = MapBuilder.BuildFromTemplate(new string[]
        {
            "......",
            "..#...",
            "......",
        });
        var visibility = new VisibilitySystem(map);
        var actor = new Actor(0, "crew") { GridPosition = new Vector2I(0, 1), VisionRadius = 10 };
        
        visibility.UpdateVisibility(new[] { actor });
        
        // Can see up to wall
        Assert.AreEqual(VisibilityState.Visible, visibility.GetVisibility(new Vector2I(1, 1)));
        Assert.AreEqual(VisibilityState.Visible, visibility.GetVisibility(new Vector2I(2, 1))); // The wall itself
        
        // Cannot see behind wall
        Assert.AreEqual(VisibilityState.Unknown, visibility.GetVisibility(new Vector2I(3, 1)));
    }
    
    [TestMethod]
    public void MovingAway_TilesBecomRevealed()
    {
        var map = MapBuilder.BuildTestMap(new Vector2I(20, 20));
        var visibility = new VisibilitySystem(map);
        var actor = new Actor(0, "crew") { GridPosition = new Vector2I(5, 5), VisionRadius = 3 };
        
        // First update - tiles become visible
        visibility.UpdateVisibility(new[] { actor });
        Assert.AreEqual(VisibilityState.Visible, visibility.GetVisibility(new Vector2I(5, 5)));
        
        // Move actor away
        actor.GridPosition = new Vector2I(15, 15);
        visibility.UpdateVisibility(new[] { actor });
        
        // Old position should be revealed, not visible
        Assert.AreEqual(VisibilityState.Revealed, visibility.GetVisibility(new Vector2I(5, 5)));
        // New position should be visible
        Assert.AreEqual(VisibilityState.Visible, visibility.GetVisibility(new Vector2I(15, 15)));
    }
    
    [TestMethod]
    public void VisionRadius_RespectedCircular()
    {
        var map = MapBuilder.BuildTestMap(new Vector2I(20, 20));
        var visibility = new VisibilitySystem(map);
        var actor = new Actor(0, "crew") { GridPosition = new Vector2I(10, 10), VisionRadius = 3 };
        
        visibility.UpdateVisibility(new[] { actor });
        
        // Within radius
        Assert.AreEqual(VisibilityState.Visible, visibility.GetVisibility(new Vector2I(10, 13))); // 3 tiles away
        
        // Outside radius (diagonal would be sqrt(18) ≈ 4.2)
        Assert.AreEqual(VisibilityState.Unknown, visibility.GetVisibility(new Vector2I(13, 13))); // 3,3 diagonal
    }
    
    [TestMethod]
    public void EnemyActor_DoesNotContributeToVisibility()
    {
        var map = MapBuilder.BuildTestMap(new Vector2I(10, 10));
        var visibility = new VisibilitySystem(map);
        var enemy = new Actor(0, "enemy") { GridPosition = new Vector2I(5, 5), VisionRadius = 5 };
        
        visibility.UpdateVisibility(new[] { enemy });
        
        // Enemy vision doesn't reveal tiles for player
        Assert.AreEqual(VisibilityState.Unknown, visibility.GetVisibility(new Vector2I(5, 5)));
    }
}
```

---

## Implementation Order

1. **Day 1: Sim Foundation**
   - Step 1.1: Add VisionRadius to Actor
   - Step 1.2: Create VisibilityState enum
   - Step 1.3: Create VisibilitySystem
   - Step 1.4: Integrate into CombatState

2. **Day 2: View Integration**
   - Step 1.5: Fix CombatResolver LOS
   - Step 2.1: Create fog overlay
   - Step 2.2: Hide enemies in fog

3. **Day 3: Polish**
   - Step 2.3: Prevent targeting through fog
   - Step 4.1: Initial visibility on start
   - Testing and bug fixes

4. **Optional (if time permits)**
   - Step 4.2: Fog transition animation
   - Step 4.3: Debug overlay

---

## Success Criteria for M2

When M2 is complete, you should be able to:

1. ✅ See fog covering unexplored areas at mission start
2. ✅ Reveal tiles by moving crew into range
3. ✅ See walls block line of sight
4. ✅ See previously explored areas as "revealed" (lighter fog)
5. ✅ Enemies hidden in fog, visible when in LOS
6. ✅ Cannot target enemies in fog
7. ✅ Multiple crew contribute to combined visibility

**Natural Pause Point**: After M2, moving a squad around a fogged interior map is already an interesting exploratory "sandbox" to test feel and scale.

---

## Notes for Future Milestones

### M3 Dependencies (Basic Combat)
- Combat already uses LOS (now fixed to use `BlocksLOS()`)
- Enemies should only be targetable when visible
- Consider: should attacks reveal the attacker's position?

### M5 Dependencies (Interactables)
- Doors will need to affect LOS when closed
- `MapState.BlocksLOS()` may need to check door state
- Hacked cameras could reveal areas (use `RevealTile()`)

### M6 Dependencies (Stealth)
- Enemies will need their own visibility tracking
- Detection based on "is player in enemy's vision"
- May need per-actor visibility, not just player aggregate

---

## Open Questions

1. **Diagonal LOS through corners**: Should a diagonal line through a wall corner be blocked?
   - *Recommendation*: Yes, block it. Feels more intuitive for tactical gameplay.

2. **Vision through glass/windows**: Should some tiles block movement but not LOS?
   - *Recommendation*: Not for M2. Add `TileType.Window` later if needed.

3. **Revealed enemy positions**: Should we show "last known position" markers for enemies that moved out of sight?
   - *Recommendation*: Not for M2. This is a [PLUS] feature.

4. **Performance**: Is per-tile fog overlay efficient enough?
   - *Recommendation*: Should be fine for maps up to 50x50. Consider shader-based fog for larger maps.

5. **Fog color/style**: Pure black or tinted?
   - *Recommendation*: Start with black. Can add blue tint or texture later for style.

---

## Files to Create/Modify

### New Files
- `src/sim/combat/VisibilityState.cs` - Enum
- `src/sim/combat/VisibilitySystem.cs` - Core visibility logic

### Modified Files
- `src/sim/combat/Actor.cs` - Add VisionRadius
- `src/sim/combat/CombatState.cs` - Add VisibilitySystem, update each tick
- `src/sim/combat/CombatResolver.cs` - Fix HasLineOfSight to use BlocksLOS
- `src/scenes/mission/MissionView.cs` - Add fog overlay, hide enemies in fog
- `src/sim/combat/agents.md` - Document new files

### Test Files
- `tests/sim/combat/VisibilitySystemTests.cs` - Unit tests
