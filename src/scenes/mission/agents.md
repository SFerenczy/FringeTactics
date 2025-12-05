# src/scenes/mission/ - Tactical Combat View

Visual representation of tactical combat. Renders CombatState and handles player orders.

## Files

### Scenes
- **MissionView.tscn** - Main mission scene with grid, actors container, and UI layer
- **ActorView.tscn** - Individual actor visual: sprite, selection indicator, HP bar
- **TimeStateWidget.tscn** - Pause/time display widget

### Scripts

#### Core Orchestration
- **MissionView.cs** - Main orchestrator: initializes sub-components, wires events, runs update loop. Delegates rendering and management to specialized components.
- **MissionInputController.cs** - Handles all input: selection, movement orders, attack orders, ability targeting, box selection. Owns SelectionManager.
- **SelectionManager.cs** - Manages selection state and control groups. Pure C# class (no Node).

#### Grid & Visibility
- **GridRenderer.cs** - Renders tactical grid with tile type visualization and map border. One-time setup.
- **FogOfWarLayer.cs** - Manages fog of war overlay. Creates fog tiles, subscribes to visibility changes, updates fog colors.
- **VisibilityDebugOverlay.cs** - Debug overlay showing visibility state per tile (F3 toggle). Green=Visible, Yellow=Revealed, Red=Unknown.

#### Entity Views
- **ActorViewManager.cs** - Spawns and tracks ActorView instances. Handles fog visibility for enemies and detection state updates.
- **ActorView.cs** - Actor visual: position sync, HP bar, hit flash, death state, detection state indicator for enemies (M6)
- **InteractableViewManager.cs** - Spawns and tracks InteractableView instances. Subscribes to InteractionSystem events, updates channel progress.
- **InteractableView.cs** - Interactable visual: color-coded by type/state, channel progress bar (M5)

#### UI Components
- **TimeStateWidget.cs** - Displays pause state and current time
- **TacticalCamera.cs** - Camera controller: pan (WASD/edge), zoom (scroll), follow selected unit
- **CoverIndicator.cs** - Displays directional cover indicators for selected units
- **MoveTargetMarker.cs** - Displays movement target markers for selected actors
- **MissionEndPanel.cs** - Displays mission end results (Victory/Defeat/Retreat) with summary statistics
- **AlarmStateWidget.cs** - UI widget showing current alarm state (Quiet/Alerted) (M6)
- **RetreatUIController.cs** - Manages retreat button, extraction status, and entry zone highlights (M7)

## Architecture

The mission view follows a component-based architecture with clear separation of concerns:

```
MissionView (orchestrator)
├── MissionInputController (input handling)
│   └── SelectionManager (selection state)
├── GridRenderer (static grid rendering)
├── FogOfWarLayer (fog of war)
├── VisibilityDebugOverlay (debug)
├── ActorViewManager (actor visuals)
├── InteractableViewManager (interactable visuals)
├── MoveTargetMarker (movement visualization)
├── CoverIndicator (cover visualization)
├── MissionEndPanel (end screen UI)
├── RetreatUIController (retreat UI)
└── AlarmStateWidget (alarm UI)
```

## Responsibilities

### MissionView (Orchestrator)
- Initialize and wire up all sub-components
- Subscribe to input controller events and forward to CombatState
- Run the main update loop, delegating to sub-components
- Handle alarm notifications and auto-pause

### MissionInputController
- Handle all keyboard and mouse input
- Manage selection state via SelectionManager
- Emit events for: selection changes, move/attack orders, ability targeting
- Handle box selection drag state
- Handle control groups (Ctrl+1-3 save, 1-3 recall)

### View Managers
- **ActorViewManager** - Spawn actor views, track dictionary, update fog visibility and detection states
- **InteractableViewManager** - Spawn interactable views, update channel progress

### Rendering Components
- **GridRenderer** - One-time grid rendering at mission start
- **FogOfWarLayer** - Per-frame fog updates based on visibility
- **CoverIndicator** - Show cover for selected units
- **MoveTargetMarker** - Show movement destinations

## Dependencies

- **Imports from**: `src/sim/combat/` (CombatState, Actor, TimeSystem, ActorState, VisibilitySystem, VisibilityState)
- **Imported by**: Nothing (leaf node)

## Input Flow

1. Player clicks/keys → MissionInputController._Input()
2. MissionInputController interprets intent, updates SelectionManager
3. MissionInputController emits events (MoveOrderIssued, AttackOrderIssued, etc.)
4. MissionView receives events, calls CombatState.IssueMovementOrder() etc.
5. CombatState processes orders in simulation ticks
6. Actor events fire → ActorView updates visuals

## Fog of War Flow

1. MissionView.CreateFogLayer() creates fog tiles for entire grid
2. MissionView subscribes to CombatState.Visibility.VisibilityChanged
3. Each frame: UpdateFogVisuals() sets fog tile colors based on visibility state
4. Each frame: UpdateActorFogVisibility() hides/shows enemy actors based on visibility
5. HandleRightClick() checks visibility before allowing attack orders
