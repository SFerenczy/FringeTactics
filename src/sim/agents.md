# src/sim/ - Domain/Simulation Layer

Pure game logic. The "inside" of the hexagonal architecture.

## Files

- **SimLog.cs** - Logging abstraction: adapter layer subscribes to receive log messages
- **RngStream.cs** - Single seeded RNG stream with serializable state for save/load
- **RngService.cs** - Multi-stream RNG manager with isolated campaign/tactical streams
- **CampaignTime.cs** - Campaign day tracking with explicit advancement API

## Subdirectories

- **campaign/** - Strategic layer: crew, ship, jobs, sector state
- **combat/** - Tactical layer: actors, combat resolution, time system
- **data/** - Configuration structs and data definitions

## Responsibilities

- Define all game rules
- Maintain authoritative game state
- Provide deterministic simulation when seeded
- Expose events for state changes (C# events, not Godot signals)

## Dependencies

- **Imports from**: Nothing outside sim (self-contained)
- **Imported by**: `src/core/`, `src/scenes/`

## Conventions

- No Node, SceneTree, Control, or UI references
- No GD.Print - use SimLog.Log() instead
- Godot math types (Vector2, Vector2I, Mathf) are allowed
- All randomness through RngService streams (seeded) for reproducible simulation
- Stateless services where possible (take state as parameter, return results)
- Single source of truth: one CampaignState, one CombatState

## Testing

- CombatSimulator.RunBattle(config, seed) runs a single headless battle
- CombatSimulator.RunSimulation(config, count, seed) runs multiple battles for balancing
- Same seed = same battle outcome (deterministic)
