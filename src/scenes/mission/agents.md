# src/scenes/mission/ - Tactical Combat View

Visual representation of tactical combat. Renders CombatState and handles player orders.

## Files

### Scenes
- **MissionView.tscn** - Main mission scene with grid, actors container, and UI layer
- **ActorView.tscn** - Individual actor visual: sprite, selection indicator, HP bar
- **TimeStateWidget.tscn** - Pause/time display widget

### Scripts
- **MissionView.cs** - Main controller: spawns actors, handles input, issues orders to CombatState, renders fog of war
- **ActorView.cs** - Actor visual: position sync, HP bar, hit flash, death state
- **TimeStateWidget.cs** - Displays pause state and current time
- **TacticalCamera.cs** - Camera controller: pan (WASD/edge), zoom (scroll), follow selected unit

### Unused
- **MissionView.gd** - Empty placeholder (using C# instead)

## Responsibilities

- Draw the tactical grid with tile type visualization (floor/wall/entry zone)
- Render fog of war overlay (Unknown=black, Revealed=semi-transparent, Visible=clear)
- Hide enemies in fog, show when visible
- Prevent targeting enemies through fog
- Spawn and position actor visuals
- Handle selection:
  - Single click to select one unit
  - Shift+click to add/remove from selection
  - Drag box to select multiple units
  - Double-click to select all crew
  - Number keys (1-3) to recall control groups (or select crew by index if no group saved)
  - Ctrl+1-3 to save current selection as control group
  - Tab to select all crew
- Translate right-click to move or attack orders
- Show movement target marker when units are moving
- Show HP bars, hit feedback, death states
- Camera control: pan, zoom, follow selected unit

## Dependencies

- **Imports from**: `src/sim/combat/` (CombatState, Actor, TimeSystem, ActorState, VisibilitySystem, VisibilityState)
- **Imported by**: Nothing (leaf node)

## Input Flow

1. Player clicks/keys → MissionView._Input()
2. MissionView calls CombatState.IssueMovementOrder() or IssueAttackOrder()
3. CombatState processes orders in simulation ticks
4. Actor events fire → ActorView updates visuals

## Fog of War Flow

1. MissionView.CreateFogLayer() creates fog tiles for entire grid
2. MissionView subscribes to CombatState.Visibility.VisibilityChanged
3. Each frame: UpdateFogVisuals() sets fog tile colors based on visibility state
4. Each frame: UpdateActorFogVisibility() hides/shows enemy actors based on visibility
5. HandleRightClick() checks visibility before allowing attack orders
