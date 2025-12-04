# src/scenes/mission/ - Tactical Combat View

Visual representation of tactical combat. Renders CombatState and handles player orders.

## Files

### Scenes
- **MissionView.tscn** - Main mission scene with grid, actors container, and UI layer
- **ActorView.tscn** - Individual actor visual: sprite, selection indicator, HP bar
- **TimeStateWidget.tscn** - Pause/time display widget

### Scripts
- **MissionView.cs** - Main view: spawns actors, renders fog of war, cover indicators, interactable views. Subscribes to MissionInputController events.
- **MissionInputController.cs** - Handles all input: selection, movement orders, attack orders, ability targeting, box selection. Owns SelectionManager.
- **SelectionManager.cs** - Manages selection state and control groups. Pure C# class (no Node).
- **ActorView.cs** - Actor visual: position sync, HP bar, hit flash, death state
- **InteractableView.cs** - Interactable visual: color-coded by type/state, channel progress bar (M5)
- **TimeStateWidget.cs** - Displays pause state and current time
- **TacticalCamera.cs** - Camera controller: pan (WASD/edge), zoom (scroll), follow selected unit
- **CoverIndicator.cs** - Displays directional cover indicators for selected units

## Architecture

The mission view follows a Controller → View pattern:

- **MissionInputController** - Interprets raw input into high-level commands (events)
- **SelectionManager** - Owns selection state, control groups
- **MissionView** - Subscribes to controller events, updates visuals, issues orders to CombatState

## Responsibilities

### MissionInputController
- Handle all keyboard and mouse input
- Manage selection state via SelectionManager
- Emit events for: selection changes, move/attack orders, ability targeting
- Handle box selection drag state
- Handle control groups (Ctrl+1-3 save, 1-3 recall)

### MissionView
- Draw the tactical grid with tile type visualization
- Render fog of war overlay
- Spawn and manage actor visuals
- Show cover indicators for selected units
- Show movement target markers
- Issue orders to CombatState when receiving events from input controller

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
