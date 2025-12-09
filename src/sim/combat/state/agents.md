# src/sim/combat/state/ - Combat State Containers

Core state objects that represent the tactical combat simulation at any point in time.

## Files

- **CombatState.cs** - Root combat state: actors, map, time system, MissionPhase (Setup/Active/Complete), objectives. Orchestrates systems per tick.
- **Actor.cs** - Individual combatant: position, HP, Armor (damage reduction), state (Alive/Down/Dead), movement, attack orders, stat modifiers
- **MapState.cs** - Grid map state: TileType (Floor/Wall/Void), cover flags, entry zones, interactables
- **VisibilityState.cs** - Enum for tile visibility: Unknown, Revealed, Visible
- **EnemyPerception.cs** - Per-enemy detection state: Idle/Alerted, last known positions of detected crew
- **ChanneledAction.cs** - Channeled action data: type, target, duration, progress tracking
- **Interactable.cs** - Interactable entity: doors, terminals, hazards with state machine

## Responsibilities

- Hold authoritative combat state
- Provide read access for systems and views
- Emit events when state changes (ActorAdded, ActorDied, PhaseChanged)

## Dependencies

- **Imports from**: `data/` (WeaponData, AbilityData, CombatBalance)
- **Imported by**: `systems/`, `factory/`, `src/scenes/mission/`

## Key Patterns

- **Single source of truth**: One CombatState per active mission
- **Event-driven updates**: State objects emit C# events, views subscribe
- **Tick-based mutation**: State changes happen during ProcessTick(), not arbitrarily
