# src/sim/combat/ - Tactical Combat Simulation

Real-time with pause (RTwP) tactical combat logic.

## Files

### State
- **CombatState.cs** - Root combat state: actors, map, time system, MissionPhase (Setup/Active/Complete), objectives. Orchestrates systems per tick.
- **Actor.cs** - Individual combatant: position, HP, state (Alive/Down/Dead), movement, attack orders, stat modifiers
- **MapState.cs** - Grid map state: TileType (Floor/Wall/Void), cover flags, entry zones, interactables
- **MapBuilder.cs** - Factory for creating MapState from templates or configs

### Systems
- **TimeSystem.cs** - Tick-based time: pause/resume, time scale, accumulator pattern
- **AttackSystem.cs** - Processes attacks each tick: manual attacks, auto-defend. Emits AttackResolved, ActorDied events.
- **MovementSystem.cs** - Stateless movement utilities: collision resolution, pathfinding around obstacles
- **CombatResolver.cs** - Stateless attack resolution: range, LOS, hit chance, damage
- **AIController.cs** - Simple enemy AI: every N ticks, pick closest visible player, move toward range, attack if able. Respects detection state (Idle enemies don't act)
- **AbilitySystem.cs** - Ability execution: cooldowns, delayed effects, AoE damage, status effects
- **VisibilitySystem.cs** - Fog of war: tracks per-tile visibility (Unknown/Revealed/Visible), LOS from crew positions
- **InteractionSystem.cs** - Interactable management: doors, terminals, hazards; channeled actions (M5)
- **PerceptionSystem.cs** - Enemy perception and alarm state: tracks per-enemy detection (Idle/Alerted), global AlarmState (Quiet/Alerted), LOS-based detection (M6)
- **MissionFactory.cs** - Builds CombatState from MissionConfig + CampaignState. Initializes perception system after spawning actors (M6)
- **CombatSimulator.cs** - Headless battle simulator for testing/balancing
- **FormationCalculator.cs** - Stateless utility for group movement: calculates spread destinations maintaining relative formation

### Data
- **WeaponData.cs** - Weapon stats: range, damage, cooldown ticks, magazine size, reload time
- **CombatStats.cs** - Combat statistics: shots fired, hits, misses for player and enemy
- **AbilityData.cs** - Ability definitions: targeting type, range, cooldown, delay, radius, damage, effects
- **CombatRng.cs** - Seeded RNG wrapper for deterministic simulation
- **VisibilityState.cs** - Enum for tile visibility: Unknown, Revealed, Visible
- **CoverDirection.cs** - CoverHeight enum (None/Low/Half/High/Full), CoverDirection flags enum, and helper methods for direction calculations
- **CombatBalance.cs** - Centralized combat balance constants, cover height reduction values, and GetCoverReduction() helper
- **AttackResult.cs** - Struct for attack resolution results: hit/miss, damage, cover height
- **Interactable.cs** - Interactable entity: doors, terminals, hazards with state machine (M5)
- **ChanneledAction.cs** - Channeled action data: type, target, duration, progress tracking (M5)
- **StatModifier.cs** - Stat modifier system: StatType enum, StatModifier class, ModifierCollection for calculating effective stats
- **EnemyPerception.cs** - Per-enemy detection state: Idle/Alerted, last known positions of detected crew (M6)

### Utilities
- **ActorTypes.cs** - Constants for actor type strings: Crew, Enemy, Drone
- **GridUtils.cs** - Grid utility methods: GetStepDirection()

## Responsibilities

- Simulate tactical combat tick-by-tick
- Process movement and attack orders
- Resolve movement collisions (prevent units from occupying same tile)
- Resolve combat (hit/miss, damage, death)
- Emit events for state changes (ActorAdded, AttackResolved, ActorDied, MissionEnded)
- Run enemy AI decisions
- Execute abilities with delayed effects and AoE
- Track fog of war visibility per tile
- Track enemy perception and detection states (M6)
- Manage global alarm state (M6)

## Dependencies

- **Imports from**: `src/sim/data/` (MissionConfig)
- **Imported by**: `src/core/`, `src/scenes/mission/`

## Key Patterns

- **Tick-based simulation**: TimeSystem accumulates delta time, fires discrete ticks
- **Order-based control**: Actors receive orders (SetTarget, SetAttackTarget), execute over time
- **Stateless resolution**: CombatResolver takes all inputs explicitly, returns results
- **Event-driven updates**: Actors emit DamageTaken, Died; CombatState emits AttackResolved, PhaseChanged
- **Seeded RNG**: All randomness through CombatRng for reproducible battles
- **Headless simulation**: CombatSimulator can run battles without UI for testing
- **Conditional victory**: Mission only auto-wins if hasEnemyObjective is true (set by MissionFactory)

## Mission Setup Flow

1. GameState.StartMission() calls MissionFactory.BuildFromCampaign()
2. MissionFactory uses MapBuilder.BuildFromConfig() to create MapState from template
3. MissionFactory spawns crew from campaign, spawns enemies
4. GameState stores result in CurrentCombat
5. MissionView reads CurrentCombat, creates ActorViews for existing actors

## Map Building

- **MapBuilder.BuildFromTemplate()** - Parse string[] template with '#'=wall, '.'=floor, 'E'=entry, '-'=low cover, '='=half cover, '+'=high cover
- **MapBuilder.BuildFromConfig()** - Use MissionConfig.MapTemplate if present, else create basic map
- **MapBuilder.BuildTestMap()** - Create simple walled room with default entry zone

## Combat Flow

1. MissionView issues orders → CombatState.IssueAttackOrder() / IssueAbilityOrder()
2. Each tick: AIController.Tick() runs enemy decisions
3. Each tick: AbilitySystem.Tick() processes delayed abilities (grenades)
4. Each tick: CombatState.ProcessAttacks() checks actors with attack targets
5. CombatResolver.ResolveAttack() determines hit/miss
6. Actor.TakeDamage() applies damage, emits events
7. CombatState.CheckMissionEnd() detects victory/defeat
8. ActorView subscribes to events, updates visuals

## Visibility Flow

1. MissionFactory initializes VisibilitySystem after building map
2. MissionFactory calculates initial visibility from crew spawn positions
3. Each tick: CombatState.ProcessTick() calls Visibility.UpdateVisibility()
4. VisibilitySystem marks old visible tiles as Revealed, calculates new visible tiles from crew LOS
5. VisibilityChanged event fires → MissionView updates fog overlay
6. Actor.VisionRadius determines how far each crew member can see

## Ability Pipeline

1. Player presses ability key (G) → MissionView enters targeting mode
2. Player clicks target tile → MissionView.ConfirmAbilityTarget()
3. CombatState.IssueAbilityOrder() → AbilitySystem.UseAbility()
4. If delayed: ability queued in pendingAbilities, ticks down
5. On detonation: find actors in radius, apply damage/effects
6. AbilityDetonated event → MissionView shows explosion visual
