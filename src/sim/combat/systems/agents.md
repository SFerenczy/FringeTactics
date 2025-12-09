# src/sim/combat/systems/ - Combat Systems

Tick-based systems that process and mutate combat state each simulation tick.

## Files

- **TimeSystem.cs** - Tick-based time: pause/resume, time scale, accumulator pattern
- **AttackSystem.cs** - Processes attacks each tick: manual attacks, auto-defend. Emits AttackResolved, ActorDied events.
- **MovementSystem.cs** - Stateless movement utilities: collision resolution, pathfinding around obstacles
- **CombatResolver.cs** - Stateless attack resolution: range, LOS, hit chance, damage with armor reduction (see docs/DOMAINS/TACTICAL/ARMOR_DAMAGE.md)
- **AIController.cs** - Enemy AI: pick closest visible player, move toward range, attack if able. Respects detection state.
- **AbilitySystem.cs** - Ability execution: cooldowns, delayed effects, AoE damage, status effects
- **VisibilitySystem.cs** - Fog of war: tracks per-tile visibility, LOS from crew positions
- **InteractionSystem.cs** - Interactable management: doors, terminals, hazards; channeled actions
- **PerceptionSystem.cs** - Enemy perception and alarm state: per-enemy detection, global AlarmState, LOS-based detection

## Responsibilities

- Process state changes each tick
- Resolve combat (hit/miss, damage, death)
- Run enemy AI decisions
- Execute abilities with delayed effects
- Track fog of war and enemy perception

## Dependencies

- **Imports from**: `state/`, `data/`
- **Imported by**: `state/CombatState.cs` (orchestrates systems)

## Key Patterns

- **Stateless where possible**: Systems take explicit parameters, return results
- **Tick-driven**: All systems called from CombatState.ProcessTick()
- **Seeded RNG**: All randomness through CombatRng for reproducible battles
