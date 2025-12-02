# M0 – Tactical Skeleton & Greybox: Implementation Plan

This document breaks down **Milestone 0** from `ROADMAP.md` into concrete implementation steps.

**Goal**: A running tactical session with a deterministic loop and a dummy map. No combat or enemies yet.

---

## Current State Assessment

Before implementing M0, we need to understand what already exists:

### What We Have (Existing Code)

| Component | Status | Notes |
|-----------|--------|-------|
| `TimeSystem` | ✅ Exists | Fixed-step simulation (20 ticks/sec), pause/resume, time scale |
| `CombatState` | ✅ Exists | Main orchestrator, owns actors/map/time, has Update loop |
| `Actor` | ✅ Exists | Movement, position, tick updates, events |
| `MapState` | ⚠️ Partial | Grid size, walkable tiles, but no LOS-blocking or proper initialization |
| `MissionView` | ✅ Exists | Basic grid rendering, actor views, input handling |
| `MissionFactory` | ✅ Exists | Builds CombatState from config |
| `MissionConfig` | ✅ Exists | Grid size, spawn positions |

### What M0 Requires vs What We Have

| M0 Requirement | Current Status | Gap |
|----------------|----------------|-----|
| Fixed-step tactical simulation loop | ✅ Complete | None |
| Pause/unpause | ✅ Complete | None |
| Load a simple 2D grid map | ⚠️ Partial | Need proper map data structure with blocked tiles |
| Walkable vs blocked tiles | ⚠️ Partial | `IsWalkable()` exists but no blocked tiles in test maps |
| Defined entry zone | ❌ Missing | Need entry zone concept in MapState |
| Spawn a single controllable unit | ✅ Complete | Works, but spawns multiple |
| Click-to-move on the grid | ✅ Complete | Works |
| Camera follows or stays focused | ❌ Missing | No camera system |

---

## Implementation Steps

### Phase 1: Map Foundation (Priority: Critical)

The map is the foundation everything else builds on. We need a proper map structure before anything else.

#### Step 1.1: Enhance MapState with Tile Data

**File**: `src/sim/combat/MapState.cs`

**Current State**: 
- Has `GridSize`, `WalkableTiles` list, basic `IsWalkable()` check
- No concept of tile types, LOS blocking, or zones

**Changes Needed**:

```csharp
// New tile data structure
public enum TileType
{
    Floor,      // Walkable, no LOS block
    Wall,       // Not walkable, blocks LOS
    Void        // Not walkable, not visible (outside map)
}

// MapState additions:
public List<TileType> Tiles { get; set; }  // Replace WalkableTiles
public List<Vector2I> EntryZone { get; set; } = new();

// New methods:
public TileType GetTileType(Vector2I pos);
public bool BlocksLOS(Vector2I pos);
public bool IsInEntryZone(Vector2I pos);
public void SetTile(Vector2I pos, TileType type);
```

**Implementation Notes**:
- Keep backward compatibility: `IsWalkable()` should still work
- `TileType.Floor` → walkable, doesn't block LOS
- `TileType.Wall` → not walkable, blocks LOS
- Entry zone is a list of positions where crew can spawn/retreat to

**Acceptance Criteria**:
- [ ] `MapState` has `TileType` enum and `Tiles` list
- [ ] `GetTileType()`, `SetTile()`, `BlocksLOS()` work correctly
- [ ] `IsWalkable()` derives from tile type
- [ ] Entry zone can be defined and queried

---

#### Step 1.2: Create Map Builder/Loader

**New File**: `src/sim/combat/MapBuilder.cs`

**Purpose**: Construct maps from data or procedurally. Separates map creation from MapState.

```csharp
public static class MapBuilder
{
    // Build a simple test map with walls
    public static MapState BuildTestMap(Vector2I size);
    
    // Build from a string template (for easy test map authoring)
    public static MapState BuildFromTemplate(string[] rows);
    
    // Future: Build from Godot TileMap data
    // public static MapState BuildFromTileMap(TileMapData data);
}
```

**Template Format** (for easy test maps):
```
. = floor
# = wall
E = entry zone (floor)
```

Example:
```csharp
var template = new string[] {
    "##########",
    "#........#",
    "#..EE....#",
    "#..EE....#",
    "#........#",
    "##########"
};
var map = MapBuilder.BuildFromTemplate(template);
```

**Acceptance Criteria**:
- [ ] `MapBuilder.BuildTestMap()` creates a map with walls around edges
- [ ] `MapBuilder.BuildFromTemplate()` parses string templates
- [ ] Entry zone tiles are marked correctly
- [ ] Unit tests verify map building

---

#### Step 1.3: Update MissionConfig for Map Data

**File**: `src/sim/data/MissionConfig.cs`

**Changes**:
```csharp
public class MissionConfig
{
    // Existing...
    
    // New: Map template (optional, for simple maps)
    public string[] MapTemplate { get; set; } = null;
    
    // New: Entry zone (if not using template)
    public List<Vector2I> EntryZone { get; set; } = new();
}
```

**Update `CreateTestMission()`**:
```csharp
public static MissionConfig CreateTestMission()
{
    return new MissionConfig
    {
        Id = "test_mission",
        Name = "Test Mission",
        MapTemplate = new string[] {
            "##############",
            "#............#",
            "#.EE.........#",
            "#.EE.........#",
            "#............#",
            "#............#",
            "#............#",
            "#............#",
            "#............#",
            "##############"
        },
        // CrewSpawnPositions derived from entry zone
        EnemySpawns = new List<EnemySpawn>() // Empty for M0
    };
}
```

**Acceptance Criteria**:
- [ ] `MissionConfig` supports map templates
- [ ] Test mission has a proper walled map with entry zone
- [ ] No enemies in M0 test mission

---

### Phase 2: Simulation Loop Cleanup (Priority: High)

The simulation loop exists but needs refinement for M0's "no combat" requirement.

#### Step 2.1: Add Mission Phases

**File**: `src/sim/combat/CombatState.cs`

**Current State**: 
- Has `IsComplete`, `Victory` flags
- Checks win/lose based on actor deaths

**Changes Needed**:
```csharp
public enum MissionPhase
{
    Setup,      // Before mission starts (future: loadout screen)
    Active,     // Mission in progress
    Complete    // Mission ended
}

public MissionPhase Phase { get; private set; } = MissionPhase.Active;

// Event for phase changes
public event Action<MissionPhase> PhaseChanged;
```

**Acceptance Criteria**:
- [ ] `MissionPhase` enum exists
- [ ] `CombatState.Phase` tracks current phase
- [ ] Phase changes emit events

---

#### Step 2.2: Simplify ProcessTick for M0

**File**: `src/sim/combat/CombatState.cs`

For M0, we want a clean tick that only handles movement. Combat comes in M3.

**Current `ProcessTick()`**:
```csharp
private void ProcessTick()
{
    var tickDuration = TimeSystem.TickDuration;
    aiController.Tick();           // Skip for M0
    AbilitySystem.Tick();          // Skip for M0
    ProcessAttacks();              // Skip for M0
    foreach (var actor in Actors)
    {
        actor.Tick(tickDuration);  // Keep for movement
    }
    CheckMissionEnd();             // Simplify for M0
}
```

**M0 Approach**:
- Keep the existing code but ensure it gracefully handles no enemies
- `CheckMissionEnd()` should not trigger victory just because there are no enemies
- Add a flag or check: if no enemies were ever spawned, don't auto-win

**Acceptance Criteria**:
- [ ] Mission doesn't auto-complete when no enemies exist
- [ ] Movement still works correctly
- [ ] AI controller doesn't crash with no enemies

---

### Phase 3: Single Unit Control (Priority: High)

M0 specifies "spawn a single controllable unit" but our system already supports multiple. We'll ensure single-unit works well.

#### Step 3.1: Verify Single Unit Spawning

**File**: `src/sim/combat/MissionFactory.cs`

**Changes**:
- Add a `BuildM0Test()` method that spawns exactly one crew member
- Or modify `CreateTestMission()` to have only one crew spawn position

```csharp
public static MissionConfig CreateM0TestMission()
{
    return new MissionConfig
    {
        Id = "m0_test",
        Name = "M0 Test - Single Unit",
        MapTemplate = new string[] {
            "##############",
            "#............#",
            "#.E..........#",
            "#............#",
            "#............#",
            "#............#",
            "#............#",
            "#............#",
            "#............#",
            "##############"
        },
        CrewSpawnPositions = new List<Vector2I> { new Vector2I(2, 2) },
        EnemySpawns = new List<EnemySpawn>()
    };
}
```

**Acceptance Criteria**:
- [ ] M0 test mission spawns exactly one unit
- [ ] Unit spawns in entry zone
- [ ] No enemies spawn

---

#### Step 3.2: Improve Click-to-Move Feedback

**File**: `src/scenes/mission/MissionView.cs`

**Current State**: 
- Click-to-move works
- No visual feedback for target position

**Changes Needed**:
- Add a simple marker showing the movement target
- Show path preview (optional for M0, but useful)

```csharp
// Add to MissionView:
private ColorRect moveTargetMarker;

private void ShowMoveTarget(Vector2I gridPos)
{
    moveTargetMarker.Visible = true;
    moveTargetMarker.Position = new Vector2(gridPos.X * TileSize, gridPos.Y * TileSize);
}

private void HideMoveTarget()
{
    moveTargetMarker.Visible = false;
}
```

**Acceptance Criteria**:
- [ ] Movement target is visually indicated
- [ ] Marker disappears when unit arrives
- [ ] Invalid targets (walls) show different feedback or no marker

---

### Phase 4: Camera System (Priority: Medium)

M0 requires "camera follows or stays focused sensibly."

#### Step 4.1: Create TacticalCamera

**New File**: `src/scenes/mission/TacticalCamera.cs`

```csharp
public partial class TacticalCamera : Camera2D
{
    public enum CameraMode
    {
        Free,           // Player controls camera
        FollowSelected, // Camera follows selected unit
        FollowAction    // Camera follows combat action (future)
    }
    
    public CameraMode Mode { get; set; } = CameraMode.Free;
    public float PanSpeed { get; set; } = 300f;
    public float ZoomSpeed { get; set; } = 0.1f;
    public Vector2 ZoomRange { get; set; } = new Vector2(0.5f, 2.0f);
    
    private Node2D followTarget;
    
    public void SetFollowTarget(Node2D target);
    public void CenterOnPosition(Vector2 worldPos);
    public void CenterOnGrid(Vector2I gridPos, int tileSize);
}
```

**Features for M0**:
- Edge panning (move camera when mouse near screen edge)
- WASD/arrow key panning
- Mouse wheel zoom
- Center on selected unit (press key or double-click)

**Acceptance Criteria**:
- [ ] Camera can pan with keyboard
- [ ] Camera can zoom with mouse wheel
- [ ] Camera can center on a grid position
- [ ] Camera stays within map bounds

---

#### Step 4.2: Integrate Camera with MissionView

**File**: `src/scenes/mission/MissionView.cs`

**Changes**:
- Add `TacticalCamera` as child node
- Connect camera to selected unit
- Add keyboard shortcuts for camera control

```csharp
// In _Ready():
camera = GetNode<TacticalCamera>("TacticalCamera");
camera.CenterOnGrid(CombatState.MapState.GridSize / 2, TileSize);

// In selection handling:
private void SelectActor(int actorId)
{
    // ... existing code ...
    if (actorViews.TryGetValue(actorId, out var view))
    {
        camera.CenterOnPosition(view.GlobalPosition);
    }
}
```

**Acceptance Criteria**:
- [ ] Camera exists in MissionView scene
- [ ] Selecting a unit centers camera on it
- [ ] Camera controls feel responsive

---

### Phase 5: Visual Polish (Priority: Low for M0)

Basic greybox visuals to make testing pleasant.

#### Step 5.1: Improve Grid Rendering

**File**: `src/scenes/mission/MissionView.cs`

**Current State**: Checkerboard pattern, no distinction for walls

**Changes**:
```csharp
private void DrawGrid()
{
    var gridSize = CombatState.MapState.GridSize;
    for (int y = 0; y < gridSize.Y; y++)
    {
        for (int x = 0; x < gridSize.X; x++)
        {
            var pos = new Vector2I(x, y);
            var tile = new ColorRect();
            tile.Size = new Vector2(TileSize - 1, TileSize - 1);
            tile.Position = new Vector2(x * TileSize, y * TileSize);
            
            var tileType = CombatState.MapState.GetTileType(pos);
            tile.Color = tileType switch
            {
                TileType.Wall => new Color(0.3f, 0.3f, 0.35f),
                TileType.Floor => (x + y) % 2 == 0 
                    ? new Color(0.15f, 0.15f, 0.2f) 
                    : new Color(0.2f, 0.2f, 0.25f),
                TileType.Void => new Color(0.05f, 0.05f, 0.05f),
                _ => Colors.Magenta // Debug: unexpected type
            };
            
            // Highlight entry zone
            if (CombatState.MapState.IsInEntryZone(pos))
            {
                tile.Color = tile.Color.Lightened(0.1f);
            }
            
            gridDisplay.AddChild(tile);
        }
    }
}
```

**Acceptance Criteria**:
- [ ] Walls are visually distinct from floors
- [ ] Entry zone has subtle highlight
- [ ] Grid looks clean and readable

---

#### Step 5.2: Entry Zone Visualization

**File**: `src/scenes/mission/MissionView.cs`

Add a subtle overlay or border for entry zone tiles.

**Acceptance Criteria**:
- [ ] Entry zone is visually identifiable
- [ ] Doesn't obscure other information

---

## Testing Checklist

### Manual Testing

1. **Launch M0 Test Mission**
   - [ ] Scene loads without errors
   - [ ] Single unit appears in entry zone
   - [ ] No enemies present
   - [ ] Grid displays correctly with walls

2. **Movement**
   - [ ] Click on floor tile → unit moves there
   - [ ] Click on wall tile → unit doesn't move (or moves to nearest valid tile)
   - [ ] Movement is smooth (interpolated)
   - [ ] Unit stops at destination

3. **Pause/Resume**
   - [ ] Space pauses simulation
   - [ ] Space resumes simulation
   - [ ] Movement pauses when game is paused
   - [ ] Orders can be issued while paused

4. **Camera**
   - [ ] Camera can pan
   - [ ] Camera can zoom
   - [ ] Camera centers on unit when selected

5. **No Auto-Win**
   - [ ] Mission doesn't end automatically
   - [ ] Player can move around indefinitely

### Automated Tests

Create `tests/sim/combat/M0Tests.cs`:

```csharp
[TestClass]
public class M0Tests
{
    [TestMethod]
    public void MapBuilder_BuildFromTemplate_CreatesCorrectMap()
    {
        var template = new string[] {
            "###",
            "#.#",
            "###"
        };
        var map = MapBuilder.BuildFromTemplate(template);
        
        Assert.AreEqual(new Vector2I(3, 3), map.GridSize);
        Assert.AreEqual(TileType.Wall, map.GetTileType(new Vector2I(0, 0)));
        Assert.AreEqual(TileType.Floor, map.GetTileType(new Vector2I(1, 1)));
    }
    
    [TestMethod]
    public void MapState_IsWalkable_RespectsWalls()
    {
        var map = MapBuilder.BuildTestMap(new Vector2I(5, 5));
        
        Assert.IsFalse(map.IsWalkable(new Vector2I(0, 0))); // Edge wall
        Assert.IsTrue(map.IsWalkable(new Vector2I(2, 2)));  // Center floor
    }
    
    [TestMethod]
    public void CombatState_NoEnemies_DoesNotAutoWin()
    {
        var config = MissionConfig.CreateM0TestMission();
        var combat = MissionFactory.BuildSandbox(config);
        combat.TimeSystem.Resume();
        
        // Simulate 5 seconds
        for (int i = 0; i < 100; i++)
        {
            combat.Update(0.05f);
        }
        
        Assert.IsFalse(combat.IsComplete);
    }
    
    [TestMethod]
    public void Actor_Movement_ReachesTarget()
    {
        var config = MissionConfig.CreateM0TestMission();
        var combat = MissionFactory.BuildSandbox(config);
        var actor = combat.Actors[0];
        
        var target = new Vector2I(5, 5);
        combat.IssueMovementOrder(actor.Id, target);
        combat.TimeSystem.Resume();
        
        // Simulate until arrival (max 10 seconds)
        for (int i = 0; i < 200 && actor.IsMoving; i++)
        {
            combat.Update(0.05f);
        }
        
        Assert.AreEqual(target, actor.GridPosition);
    }
}
```

---

## Implementation Order

1. **Week 1: Map Foundation**
   - Step 1.1: Enhance MapState
   - Step 1.2: Create MapBuilder
   - Step 1.3: Update MissionConfig

2. **Week 1-2: Simulation Cleanup**
   - Step 2.1: Add Mission Phases
   - Step 2.2: Fix no-enemy behavior

3. **Week 2: Unit Control**
   - Step 3.1: M0 test mission
   - Step 3.2: Movement feedback

4. **Week 2-3: Camera**
   - Step 4.1: TacticalCamera
   - Step 4.2: Integration

5. **Week 3: Polish**
   - Step 5.1: Grid rendering
   - Step 5.2: Entry zone visualization
   - Testing and bug fixes

---

## Success Criteria for M0

When M0 is complete, you should be able to:

1. ✅ Launch a tactical session with a single controllable unit
2. ✅ See a grid map with visible walls and floor tiles
3. ✅ Click to move the unit around the map
4. ✅ Pause and resume the simulation
5. ✅ Pan and zoom the camera
6. ✅ The mission does not auto-complete
7. ✅ Entry zone is visually marked

**Natural Pause Point**: After M0, you have a working tactical sandbox. You can test camera behavior, movement feel, and map authoring before adding complexity.

---

## Notes for Future Milestones

### M1 Dependencies (Multi-Unit Control)
- M0's single-unit control extends naturally to multi-unit
- Selection system already supports multiple units
- Need to add: group movement spacing, formation hints

### M2 Dependencies (Visibility & Fog of War)
- M0's `BlocksLOS()` in MapState is the foundation
- Need to add: per-tile visibility state, vision radius per actor
- Grid rendering will need fog overlay

### M3 Dependencies (Basic Combat)
- M0's simulation loop is ready for combat processing
- Need to add: enemies back, attack processing, damage
- AI controller already exists

---

## Open Questions

1. **Map Authoring**: Should we support Godot TileMap import for M0, or is string templates sufficient?
   - *Recommendation*: String templates for M0, TileMap import for M1+

2. **Camera Bounds**: Should camera be strictly bounded to map, or allow some overshoot?
   - *Recommendation*: Allow small overshoot (half screen) for better feel

3. **Entry Zone Purpose**: Is entry zone just for spawning, or also for retreat (M7)?
   - *Recommendation*: Design for both now, implement retreat in M7

