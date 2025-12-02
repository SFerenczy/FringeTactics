# src/scenes/mission/ - Tactical Combat View

Visual representation of tactical combat. Renders CombatState and handles player orders.

## Files

### Scenes
- **MissionView.tscn** - Main mission scene with grid, actors container, and UI layer
- **ActorView.tscn** - Individual actor visual: sprite, selection indicator, HP bar
- **TimeStateWidget.tscn** - Pause/time display widget

### Scripts
- **MissionView.cs** - Main controller: spawns actors, handles input, issues orders to CombatState
- **ActorView.cs** - Actor visual: position sync, HP bar, hit flash, death state
- **TimeStateWidget.cs** - Displays pause state and current time

### Unused
- **MissionView.gd** - Empty placeholder (using C# instead)

## Responsibilities

- Draw the tactical grid
- Spawn and position actor visuals
- Handle selection (click, number keys)
- Translate right-click to move or attack orders
- Show HP bars, hit feedback, death states

## Dependencies

- **Imports from**: `src/sim/combat/` (CombatState, Actor, TimeSystem, ActorState)
- **Imported by**: Nothing (leaf node)

## Input Flow

1. Player clicks/keys → MissionView._Input()
2. MissionView calls CombatState.IssueMovementOrder() or IssueAttackOrder()
3. CombatState processes orders in simulation ticks
4. Actor events fire → ActorView updates visuals
