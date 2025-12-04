# M1 – Multi-Unit Control & Group Movement: Implementation Plan

This document breaks down **Milestone 1** from `ROADMAP.md` into concrete implementation steps.

**Goal**: Control a small squad instead of a single unit. Still no combat.

---

## Current State Assessment

### What We Have (From M0)

| Component | Status | Notes |
|-----------|--------|-------|
| `CombatState` | ✅ Complete | Manages multiple actors, issues orders |
| `Actor` | ✅ Complete | Movement, position tracking, events |
| `MapState` | ✅ Complete | TileType, walkability, entry zones |
| `MissionView` | ✅ Partial | Has selection, but limited multi-select |
| `ActorView` | ✅ Complete | Visual representation, selection indicator |
| `TacticalCamera` | ✅ Complete | Pan, zoom, follow target |

### What M1 Requires vs What We Have

| M1 Requirement | Current Status | Gap |
|----------------|----------------|-----|
| Multiple player-controlled units | ✅ Complete | Already spawns multiple crew |
| Single selection (click) | ✅ Complete | Works |
| Box/multi-selection | ✅ Complete | Drag-select rectangle implemented |
| Shift-click multi-select | ✅ Complete | Additive selection implemented |
| Group move orders | ✅ Complete | Formation-based movement implemented |
| Basic separation/spacing | ✅ Complete | Units spread around destination |

---

## Architecture Decisions

### Selection System Location

**Decision**: Selection lives in the **adapter layer** (`MissionView`), not in the sim.

**Rationale**:
- Selection is a UI/control concern, not game state
- The sim (`CombatState`) only knows about individual actors and orders
- Group commands are translated to individual orders by the view layer
- This follows the architecture guideline: "Groups live above the simulation state"

### Group Movement Strategy

**Decision**: Use **formation-based offset** for group movement.

**Options Considered**:
1. **Same destination** (current) - All units go to clicked tile → causes stacking
2. **Formation offset** - Units maintain relative positions around destination ✅
3. **Flow-field pathfinding** - Complex, overkill for M1
4. **Smart spacing algorithm** - Find nearest free tiles around destination

**Chosen Approach**: Formation offset with fallback to smart spacing
- Calculate centroid of selected units
- Apply offset from centroid to each unit's destination
- If destination is blocked, find nearest walkable tile

---

## Implementation Steps

### Phase 1: Enhanced Selection System (Priority: Critical)

The selection system is the foundation for group control.

#### Step 1.1: Add Shift-Click Additive Selection

**File**: `src/scenes/mission/MissionView.cs`

**Current State**: 
- `SelectActor()` clears previous selection
- No modifier key handling

**Changes Needed**:

```csharp
// Add to HandleSelection():
private void HandleSelection(Vector2I gridPos, bool additive)
{
    var clickedActor = CombatState.GetActorAtPosition(gridPos);
    
    if (clickedActor != null && clickedActor.Type == "crew")
    {
        if (additive)
        {
            // Toggle selection
            if (selectedActorIds.Contains(clickedActor.Id))
            {
                DeselectActor(clickedActor.Id);
            }
            else
            {
                AddToSelection(clickedActor.Id);
            }
        }
        else
        {
            SelectActor(clickedActor.Id);
        }
    }
    else if (!additive)
    {
        ClearSelection();
    }
}

private void AddToSelection(int actorId)
{
    if (!selectedActorIds.Contains(actorId) && actorViews.ContainsKey(actorId))
    {
        selectedActorIds.Add(actorId);
        actorViews[actorId].SetSelected(true);
    }
}

private void DeselectActor(int actorId)
{
    if (selectedActorIds.Remove(actorId) && actorViews.ContainsKey(actorId))
    {
        actorViews[actorId].SetSelected(false);
    }
}
```

**Input Handling Update**:
```csharp
// In HandleMouseClick():
if (@event.ButtonIndex == MouseButton.Left)
{
    bool shiftHeld = Input.IsKeyPressed(Key.Shift);
    HandleSelection(gridPos, shiftHeld);
}
```

**Acceptance Criteria**:
- [x] Click selects single unit (clears others)
- [x] Shift+click adds to selection
- [x] Shift+click on selected unit deselects it
- [x] Click on empty space clears selection (unless shift held)

---

#### Step 1.2: Implement Box Selection (Drag Select)

**File**: `src/scenes/mission/MissionView.cs`

**New State**:
```csharp
// Box selection state
private bool isDragSelecting = false;
private Vector2 dragStartScreen;
private Vector2 dragStartWorld;
private ColorRect selectionBox;
```

**Implementation**:

```csharp
private void CreateSelectionBox()
{
    selectionBox = new ColorRect();
    selectionBox.Color = new Color(0.3f, 0.6f, 1.0f, 0.3f);
    selectionBox.Visible = false;
    selectionBox.ZIndex = 100; // Above everything
    AddChild(selectionBox);
}

private void HandleMouseClick(InputEventMouseButton @event)
{
    var gridPos = ScreenToGrid(@event.Position);
    
    if (@event.ButtonIndex == MouseButton.Left)
    {
        if (@event.Pressed)
        {
            // Start potential drag
            dragStartScreen = @event.Position;
            dragStartWorld = GetCanvasTransform().AffineInverse() * @event.Position;
        }
        else // Released
        {
            if (isDragSelecting)
            {
                FinishBoxSelection(@event.Position);
            }
            else
            {
                // Normal click selection
                bool shiftHeld = Input.IsKeyPressed(Key.Shift);
                HandleSelection(gridPos, shiftHeld);
            }
        }
    }
    // ... rest of mouse handling
}

private void UpdateBoxSelection(Vector2 currentScreen)
{
    var currentWorld = GetCanvasTransform().AffineInverse() * currentScreen;
    var distance = (currentScreen - dragStartScreen).Length();
    
    // Start drag if moved enough
    if (!isDragSelecting && distance > 5)
    {
        isDragSelecting = true;
        selectionBox.Visible = true;
    }
    
    if (isDragSelecting)
    {
        // Update box visual
        var minX = Mathf.Min(dragStartWorld.X, currentWorld.X);
        var minY = Mathf.Min(dragStartWorld.Y, currentWorld.Y);
        var maxX = Mathf.Max(dragStartWorld.X, currentWorld.X);
        var maxY = Mathf.Max(dragStartWorld.Y, currentWorld.Y);
        
        selectionBox.Position = new Vector2(minX, minY);
        selectionBox.Size = new Vector2(maxX - minX, maxY - minY);
    }
}

private void FinishBoxSelection(Vector2 endScreen)
{
    isDragSelecting = false;
    selectionBox.Visible = false;
    
    var endWorld = GetCanvasTransform().AffineInverse() * endScreen;
    var rect = new Rect2(
        Mathf.Min(dragStartWorld.X, endWorld.X),
        Mathf.Min(dragStartWorld.Y, endWorld.Y),
        Mathf.Abs(endWorld.X - dragStartWorld.X),
        Mathf.Abs(endWorld.Y - dragStartWorld.Y)
    );
    
    bool shiftHeld = Input.IsKeyPressed(Key.Shift);
    if (!shiftHeld)
    {
        ClearSelection();
    }
    
    // Select all crew in box
    foreach (var actor in CombatState.Actors)
    {
        if (actor.Type != "crew" || actor.State != ActorState.Alive)
            continue;
            
        var actorWorldPos = actor.GetVisualPosition(TileSize);
        var actorRect = new Rect2(actorWorldPos, new Vector2(TileSize, TileSize));
        
        if (rect.Intersects(actorRect))
        {
            AddToSelection(actor.Id);
        }
    }
}
```

**Process Update**:
```csharp
public override void _Process(double delta)
{
    // ... existing code ...
    
    // Update box selection if dragging
    if (Input.IsMouseButtonPressed(MouseButton.Left) && !isDragSelecting)
    {
        var mousePos = GetViewport().GetMousePosition();
        UpdateBoxSelection(mousePos);
    }
    else if (isDragSelecting)
    {
        var mousePos = GetViewport().GetMousePosition();
        UpdateBoxSelection(mousePos);
    }
}
```

**Acceptance Criteria**:
- [x] Click-drag creates visible selection rectangle
- [x] All crew units in rectangle are selected on release
- [x] Shift+drag adds to existing selection
- [x] Small movements don't trigger drag (threshold)
- [x] Selection box renders correctly with camera zoom/pan

---

### Phase 2: Group Movement (Priority: Critical)

#### Step 2.1: Create Formation Calculator

**New File**: `src/sim/combat/FormationCalculator.cs`

**Purpose**: Calculate destination positions for group movement.

```csharp
using Godot;
using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// Calculates formation positions for group movement.
/// Stateless utility - all methods are pure functions.
/// </summary>
public static class FormationCalculator
{
    /// <summary>
    /// Calculate destinations for a group of actors moving to a target position.
    /// Maintains relative formation around the group centroid.
    /// </summary>
    public static Dictionary<int, Vector2I> CalculateGroupDestinations(
        List<Actor> actors,
        Vector2I targetPos,
        MapState map)
    {
        var destinations = new Dictionary<int, Vector2I>();
        
        if (actors.Count == 0)
            return destinations;
            
        if (actors.Count == 1)
        {
            destinations[actors[0].Id] = targetPos;
            return destinations;
        }
        
        // Calculate current centroid
        var centroid = CalculateCentroid(actors);
        
        // Calculate offset for each actor from centroid
        foreach (var actor in actors)
        {
            var offset = actor.GridPosition - centroid;
            var idealDest = targetPos + offset;
            
            // Find valid destination (walkable and not occupied by destination)
            var validDest = FindNearestWalkable(idealDest, map, destinations.Values);
            destinations[actor.Id] = validDest;
        }
        
        return destinations;
    }
    
    /// <summary>
    /// Calculate destinations in a tight cluster around target.
    /// Used when formation would spread units too far.
    /// </summary>
    public static Dictionary<int, Vector2I> CalculateClusterDestinations(
        List<Actor> actors,
        Vector2I targetPos,
        MapState map)
    {
        var destinations = new Dictionary<int, Vector2I>();
        var occupied = new HashSet<Vector2I>();
        
        // Spiral outward from target
        var spiralPositions = GetSpiralPositions(targetPos, actors.Count * 2);
        
        foreach (var actor in actors)
        {
            foreach (var pos in spiralPositions)
            {
                if (map.IsWalkable(pos) && !occupied.Contains(pos))
                {
                    destinations[actor.Id] = pos;
                    occupied.Add(pos);
                    break;
                }
            }
            
            // Fallback: stay in place
            if (!destinations.ContainsKey(actor.Id))
            {
                destinations[actor.Id] = actor.GridPosition;
            }
        }
        
        return destinations;
    }
    
    private static Vector2I CalculateCentroid(List<Actor> actors)
    {
        if (actors.Count == 0)
            return Vector2I.Zero;
            
        int sumX = 0, sumY = 0;
        foreach (var actor in actors)
        {
            sumX += actor.GridPosition.X;
            sumY += actor.GridPosition.Y;
        }
        
        return new Vector2I(sumX / actors.Count, sumY / actors.Count);
    }
    
    private static Vector2I FindNearestWalkable(
        Vector2I target,
        MapState map,
        IEnumerable<Vector2I> alreadyTaken)
    {
        var taken = new HashSet<Vector2I>(alreadyTaken);
        
        if (map.IsWalkable(target) && !taken.Contains(target))
            return target;
        
        // Search in expanding rings
        for (int radius = 1; radius <= 5; radius++)
        {
            foreach (var pos in GetRingPositions(target, radius))
            {
                if (map.IsWalkable(pos) && !taken.Contains(pos))
                    return pos;
            }
        }
        
        return target; // Fallback
    }
    
    private static IEnumerable<Vector2I> GetSpiralPositions(Vector2I center, int count)
    {
        yield return center;
        
        int x = 0, y = 0;
        int dx = 0, dy = -1;
        int generated = 1;
        
        while (generated < count)
        {
            if ((-count/2 <= x && x <= count/2) && (-count/2 <= y && y <= count/2))
            {
                yield return center + new Vector2I(x, y);
                generated++;
            }
            
            if (x == y || (x < 0 && x == -y) || (x > 0 && x == 1 - y))
            {
                (dx, dy) = (-dy, dx);
            }
            x += dx;
            y += dy;
        }
    }
    
    private static IEnumerable<Vector2I> GetRingPositions(Vector2I center, int radius)
    {
        for (int x = -radius; x <= radius; x++)
        {
            yield return center + new Vector2I(x, -radius);
            yield return center + new Vector2I(x, radius);
        }
        for (int y = -radius + 1; y < radius; y++)
        {
            yield return center + new Vector2I(-radius, y);
            yield return center + new Vector2I(radius, y);
        }
    }
}
```

**Acceptance Criteria**:
- [x] Single unit goes directly to target
- [x] Multiple units maintain relative positions
- [x] Blocked destinations find nearest walkable
- [x] No two units assigned same destination

---

#### Step 2.2: Integrate Formation Movement in MissionView

**File**: `src/scenes/mission/MissionView.cs`

**Update HandleRightClick**:

```csharp
private void HandleRightClick(Vector2I gridPos)
{
    if (selectedActorIds.Count == 0)
        return;
        
    // Check if clicking on an enemy (attack order)
    var targetActor = CombatState.GetActorAtPosition(gridPos);
    if (targetActor != null && targetActor.State == ActorState.Alive)
    {
        if (IsEnemyOfSelection(targetActor))
        {
            IssueGroupAttackOrder(targetActor.Id);
            return;
        }
    }
    
    // Movement order with formation
    IssueGroupMoveOrder(gridPos);
}

private void IssueGroupMoveOrder(Vector2I targetPos)
{
    // Gather selected actors
    var selectedActors = new List<Actor>();
    foreach (var actorId in selectedActorIds)
    {
        var actor = CombatState.GetActorById(actorId);
        if (actor != null && actor.State == ActorState.Alive)
        {
            selectedActors.Add(actor);
        }
    }
    
    if (selectedActors.Count == 0)
        return;
    
    // Calculate formation destinations
    var destinations = FormationCalculator.CalculateGroupDestinations(
        selectedActors,
        targetPos,
        CombatState.MapState
    );
    
    // Issue individual orders
    foreach (var kvp in destinations)
    {
        CombatState.IssueMovementOrder(kvp.Key, kvp.Value);
        
        var actor = CombatState.GetActorById(kvp.Key);
        if (actor != null && actor.IsMoving)
        {
            actorMoveTargets[kvp.Key] = kvp.Value;
        }
    }
    
    UpdateMoveTargetMarker();
}

private bool IsEnemyOfSelection(Actor target)
{
    foreach (var actorId in selectedActorIds)
    {
        var selected = CombatState.GetActorById(actorId);
        if (selected != null && selected.Type != target.Type)
        {
            return true;
        }
    }
    return false;
}

private void IssueGroupAttackOrder(int targetId)
{
    foreach (var actorId in selectedActorIds)
    {
        CombatState.IssueAttackOrder(actorId, targetId);
    }
}
```

**Acceptance Criteria**:
- [x] Single unit moves directly to clicked position
- [x] Multiple units spread around clicked position
- [x] Units don't overlap at destination
- [x] Formation roughly maintained during movement

---

#### Step 2.3: Visual Feedback for Group Destinations

**File**: `src/scenes/mission/MissionView.cs`

**Enhancement**: Show destination markers for all selected units.

```csharp
// Replace single marker with multiple
private Dictionary<int, ColorRect> moveTargetMarkers = new();

private void CreateMoveTargetMarker(int actorId, Vector2I gridPos, Color color)
{
    if (moveTargetMarkers.ContainsKey(actorId))
    {
        UpdateMoveTargetMarkerPosition(actorId, gridPos);
        return;
    }
    
    var marker = new ColorRect();
    marker.Size = new Vector2(TileSize - 4, TileSize - 4);
    marker.Position = new Vector2(gridPos.X * TileSize + 2, gridPos.Y * TileSize + 2);
    marker.Color = new Color(color.R, color.G, color.B, 0.4f);
    marker.ZIndex = 1;
    gridDisplay.AddChild(marker);
    
    moveTargetMarkers[actorId] = marker;
}

private void UpdateMoveTargetMarkerPosition(int actorId, Vector2I gridPos)
{
    if (moveTargetMarkers.TryGetValue(actorId, out var marker))
    {
        marker.Position = new Vector2(gridPos.X * TileSize + 2, gridPos.Y * TileSize + 2);
    }
}

private void RemoveMoveTargetMarker(int actorId)
{
    if (moveTargetMarkers.TryGetValue(actorId, out var marker))
    {
        marker.QueueFree();
        moveTargetMarkers.Remove(actorId);
    }
}

private void ClearAllMoveTargetMarkers()
{
    foreach (var marker in moveTargetMarkers.Values)
    {
        marker.QueueFree();
    }
    moveTargetMarkers.Clear();
}
```

**Acceptance Criteria**:
- [ ] Each moving unit has its own destination marker
- [ ] Markers match unit colors
- [ ] Markers disappear when unit arrives
- [ ] Markers update if new order issued

---

### Phase 3: Selection UX Polish (Priority: Medium)

#### Step 3.1: Control Group Hotkeys

**File**: `src/scenes/mission/MissionView.cs`

**Feature**: Ctrl+1-3 to save groups, 1-3 to recall.

```csharp
// Control groups (saved selections)
private Dictionary<int, List<int>> controlGroups = new(); // group number -> actor IDs

private void HandleControlGroupInput(InputEvent @event)
{
    if (@event is not InputEventKey keyEvent || !keyEvent.Pressed)
        return;
        
    bool ctrlHeld = Input.IsKeyPressed(Key.Ctrl);
    
    int groupNum = keyEvent.Keycode switch
    {
        Key.Key1 => 1,
        Key.Key2 => 2,
        Key.Key3 => 3,
        _ => -1
    };
    
    if (groupNum < 0)
        return;
        
    if (ctrlHeld)
    {
        // Save current selection to group
        SaveControlGroup(groupNum);
    }
    else
    {
        // Recall group (or select single crew if no group saved)
        RecallControlGroup(groupNum);
    }
}

private void SaveControlGroup(int groupNum)
{
    if (selectedActorIds.Count == 0)
    {
        controlGroups.Remove(groupNum);
        GD.Print($"[Selection] Cleared control group {groupNum}");
        return;
    }
    
    controlGroups[groupNum] = new List<int>(selectedActorIds);
    GD.Print($"[Selection] Saved {selectedActorIds.Count} units to group {groupNum}");
}

private void RecallControlGroup(int groupNum)
{
    if (controlGroups.TryGetValue(groupNum, out var actorIds))
    {
        ClearSelection();
        foreach (var actorId in actorIds)
        {
            var actor = CombatState.GetActorById(actorId);
            if (actor != null && actor.State == ActorState.Alive)
            {
                AddToSelection(actorId);
            }
        }
        GD.Print($"[Selection] Recalled group {groupNum}: {selectedActorIds.Count} units");
    }
    else
    {
        // Fallback: select crew by index (existing behavior)
        SelectCrewByIndex(groupNum - 1);
    }
}
```

**Acceptance Criteria**:
- [x] Ctrl+1/2/3 saves current selection
- [x] 1/2/3 recalls saved group
- [x] Dead units filtered from recalled groups
- [x] Fallback to crew-by-index if no group saved

---

#### Step 3.2: Double-Click to Select All of Type

**File**: `src/scenes/mission/MissionView.cs`

**Feature**: Double-click a unit to select all visible units of same type.

```csharp
private float lastClickTime = 0f;
private int lastClickedActorId = -1;
private const float DOUBLE_CLICK_THRESHOLD = 0.3f;

private void HandleSelection(Vector2I gridPos, bool additive)
{
    var clickedActor = CombatState.GetActorAtPosition(gridPos);
    float currentTime = Time.GetTicksMsec() / 1000f;
    
    if (clickedActor != null && clickedActor.Type == "crew")
    {
        // Check for double-click
        if (clickedActor.Id == lastClickedActorId && 
            currentTime - lastClickTime < DOUBLE_CLICK_THRESHOLD)
        {
            SelectAllCrewOfType(clickedActor);
            lastClickedActorId = -1;
            return;
        }
        
        lastClickTime = currentTime;
        lastClickedActorId = clickedActor.Id;
        
        // Normal selection logic...
    }
    // ... rest of method
}

private void SelectAllCrewOfType(Actor referenceActor)
{
    ClearSelection();
    foreach (var actor in CombatState.Actors)
    {
        if (actor.Type == "crew" && actor.State == ActorState.Alive)
        {
            AddToSelection(actor.Id);
        }
    }
    GD.Print($"[Selection] Selected all {selectedActorIds.Count} crew");
}
```

**Acceptance Criteria**:
- [x] Double-click selects all crew
- [x] Works with camera zoom/pan
- [x] Threshold prevents accidental triggers (0.3s)

---

### Phase 4: Movement Polish (Priority: Low for M1)

#### Step 4.1: Collision Avoidance During Movement

**File**: `src/sim/combat/Actor.cs`

**Current Issue**: Units can overlap during movement.

**Simple Solution**: Add soft collision check in `Tick()`.

```csharp
// In Actor.Tick(), before moving to next tile:
// This requires access to other actors, so we need to pass CombatState or a collision checker

// Alternative: Handle in CombatState.ProcessTick()
private void ProcessMovementCollisions()
{
    // For each pair of actors moving to same tile, delay one
    var destinations = new Dictionary<Vector2I, List<Actor>>();
    
    foreach (var actor in Actors)
    {
        if (!actor.IsMoving)
            continue;
            
        var nextTile = actor.GridPosition + actor.MoveDirection;
        if (!destinations.ContainsKey(nextTile))
            destinations[nextTile] = new List<Actor>();
        destinations[nextTile].Add(actor);
    }
    
    foreach (var kvp in destinations)
    {
        if (kvp.Value.Count > 1)
        {
            // Multiple actors heading to same tile - let first one through
            for (int i = 1; i < kvp.Value.Count; i++)
            {
                kvp.Value[i].MoveProgress = 0; // Reset progress, try again next tick
            }
        }
    }
}
```

**Note**: This is a simple solution. More sophisticated pathfinding with reservation would be [PLUS].

**Acceptance Criteria**:
- [x] Units don't occupy same tile simultaneously
- [x] Movement feels smooth despite collision handling
- [x] No deadlocks (units pause and retry next tick)

---

## Testing Checklist

### Manual Testing

1. **Selection**
   - [ ] Click selects single unit
   - [ ] Shift+click adds/removes from selection
   - [ ] Drag creates selection box
   - [ ] Box selects all crew inside
   - [ ] Tab selects all crew
   - [ ] 1/2/3 selects individual crew
   - [ ] Ctrl+1/2/3 saves groups
   - [ ] Double-click selects all crew

2. **Group Movement**
   - [ ] Single unit moves to clicked tile
   - [ ] Multiple units spread around clicked tile
   - [ ] Units don't stack on same tile
   - [ ] Formation roughly maintained
   - [ ] Movement works with walls/obstacles

3. **Visual Feedback**
   - [ ] Selection indicators show on all selected units
   - [ ] Destination markers show for moving units
   - [ ] Markers match unit colors
   - [ ] Selection box renders correctly with zoom

4. **Edge Cases**
   - [ ] Select dead unit (should not select)
   - [ ] Move to blocked tile (should find nearest)
   - [ ] Move large group in tight space
   - [ ] Rapid click spam doesn't break selection

### Automated Tests

✅ **Created**: `tests/sim/combat/M1Tests.cs`

Tests cover:
- FormationCalculator: single unit, multiple units, no overlapping destinations, blocked tiles
- MissionConfig: M1 test mission has 6 crew, no enemies
- Collision avoidance: wall collision, unit collision, move progress reset
- Group movement integration: all units reach destinations

Example tests (actual implementation in file):

```csharp
[TestClass]
public class FormationCalculatorTests
{
    [TestMethod]
    public void SingleActor_GoesDirectlyToTarget()
    {
        var map = MapBuilder.BuildTestMap(new Vector2I(10, 10));
        var actor = new Actor(0, "crew") { GridPosition = new Vector2I(2, 2) };
        var actors = new List<Actor> { actor };
        
        var destinations = FormationCalculator.CalculateGroupDestinations(
            actors, new Vector2I(5, 5), map);
        
        Assert.AreEqual(new Vector2I(5, 5), destinations[0]);
    }
    
    [TestMethod]Better, but not gone honestly.
    public void MultipleActors_MaintainRelativePositions()
    {
        var map = MapBuilder.BuildTestMap(new Vector2I(10, 10));
        var actors = new List<Actor>
        {
            new Actor(0, "crew") { GridPosition = new Vector2I(2, 2) },
            new Actor(1, "crew") { GridPosition = new Vector2I(3, 2) },
            new Actor(2, "crew") { GridPosition = new Vector2I(2, 3) }
        };
        
        var destinations = FormationCalculator.CalculateGroupDestinations(
            actors, new Vector2I(5, 5), map);
        
        // Check relative positions maintained
        var offset01 = destinations[1] - destinations[0];
        var offset02 = destinations[2] - destinations[0];
        
        Assert.AreEqual(new Vector2I(1, 0), offset01);
        Assert.AreEqual(new Vector2I(0, 1), offset02);
    }
    
    [TestMethod]
    public void BlockedDestination_FindsNearestWalkable()
    {
        var map = MapBuilder.BuildFromTemplate(new string[]
        {
            "######",
            "#....#",
            "#.##.#",
            "#....#",
            "######"
        });
        
        var actor = new Actor(0, "crew") { GridPosition = new Vector2I(1, 1) };
        var actors = new List<Actor> { actor };
        
        // Target is a wall
        var destinations = FormationCalculator.CalculateGroupDestinations(
            actors, new Vector2I(2, 2), map);
        
        // Should find nearest walkable
        Assert.IsTrue(map.IsWalkable(destinations[0]));
    }
    
    [TestMethod]
    public void NoTwoActors_SameDestination()
    {
        var map = MapBuilder.BuildTestMap(new Vector2I(10, 10));
        var actors = new List<Actor>();
        for (int i = 0; i < 5; i++)
        {
            actors.Add(new Actor(i, "crew") { GridPosition = new Vector2I(1 + i, 1) });
        }
        
        var destinations = FormationCalculator.CalculateGroupDestinations(
            actors, new Vector2I(5, 5), map);
        
        var uniqueDestinations = new HashSet<Vector2I>(destinations.Values);
        Assert.AreEqual(actors.Count, uniqueDestinations.Count);
    }
}
```

---

## Implementation Order

1. **Day 1-2: Selection Foundation**
   - Step 1.1: Shift-click additive selection
   - Step 1.2: Box selection (drag select)

2. **Day 3-4: Group Movement**
   - Step 2.1: FormationCalculator
   - Step 2.2: Integrate in MissionView
   - Step 2.3: Visual feedback for destinations

3. **Day 5: Polish**
   - Step 3.1: Control group hotkeys
   - Step 3.2: Double-click select all
   - Testing and bug fixes

4. **Optional (if time permits)**
   - Step 4.1: Collision avoidance

---

## Success Criteria for M1

When M1 is complete, you should be able to:

1. ✅ Select multiple units via shift-click
2. ✅ Select multiple units via drag box
3. ✅ Issue move orders to groups
4. ✅ Units spread out around destination (don't stack)
5. ✅ Save/recall control groups with Ctrl+1-3
6. ✅ See destination markers for all moving units
7. ✅ Tab selects all crew

**Natural Pause Point**: After M1, you have a functional squad control system. You can explore UX patterns for selection, grouping, and order feedback before combat complexity enters the picture.

---

## Notes for Future Milestones

### M2 Dependencies (Visibility & Fog of War)
- Selection system ready for fog-hidden units
- Group movement may need to respect fog (don't move into unknown)
- Consider: should selecting a unit reveal its vision?

### M3 Dependencies (Basic Combat)
- Group attack orders already implemented
- May want "focus fire" vs "spread fire" options
- Auto-defend should work per-unit, not per-group

### M4 Dependencies (Cover)
- Formation calculator may need cover-awareness
- "Move to cover" as a group order type?

---

## Open Questions

1. **Formation Persistence**: Should groups remember their formation between moves?
   - *Recommendation*: No for M1, consider for [PLUS]

2. **Move-Attack**: Should right-click on enemy while moving queue an attack?
   - *Recommendation*: No for M1, override-only per design

3. **Selection Limit**: Should there be a max selection size?
   - *Recommendation*: No limit for M1, squad is 5-10 units anyway

4. **Pathfinding**: Should group movement use smarter pathfinding?
   - *Recommendation*: Simple A* per unit for M1, flow fields for [PLUS]

