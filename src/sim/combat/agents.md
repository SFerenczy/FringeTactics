# src/sim/combat/ - Tactical Combat Simulation

Real-time with pause (RTwP) tactical combat logic.

## Files

### State
- **CombatState.cs** - Root combat state: actors, map, time system, objectives. Processes ticks and attacks.
- **Actor.cs** - Individual combatant: position, HP, state (Alive/Down/Dead), movement, attack orders
- **MapState.cs** - Grid definition: walkable tiles, cover flags, spawn points

### Systems
- **TimeSystem.cs** - Tick-based time: pause/resume, time scale, accumulator pattern
- **CombatResolver.cs** - Stateless attack resolution: range, LOS, hit chance, damage
- **AIController.cs** - Simple enemy AI: every N ticks, pick closest visible player, move toward range, attack if able
- **AbilitySystem.cs** - Ability execution: cooldowns, delayed effects, AoE damage, status effects
- **MissionFactory.cs** - Builds CombatState from MissionConfig + CampaignState
- **CombatSimulator.cs** - Headless battle simulator for testing/balancing

### Data
- **WeaponData.cs** - Weapon stats: range, damage, cooldown ticks
- **CombatStats.cs** - Combat statistics: shots fired, hits, misses for player and enemy
- **AbilityData.cs** - Ability definitions: targeting type, range, cooldown, delay, radius, damage, effects
- **CombatRng.cs** - Seeded RNG wrapper for deterministic simulation

## Responsibilities

- Simulate tactical combat tick-by-tick
- Process movement and attack orders
- Resolve combat (hit/miss, damage, death)
- Emit events for state changes (ActorAdded, AttackResolved, ActorDied, MissionEnded)
- Run enemy AI decisions
- Execute abilities with delayed effects and AoE

## Dependencies

- **Imports from**: `src/sim/data/` (MissionConfig)
- **Imported by**: `src/core/`, `src/scenes/mission/`

## Key Patterns

- **Tick-based simulation**: TimeSystem accumulates delta time, fires discrete ticks
- **Order-based control**: Actors receive orders (SetTarget, SetAttackTarget), execute over time
- **Stateless resolution**: CombatResolver takes all inputs explicitly, returns results
- **Event-driven updates**: Actors emit DamageTaken, Died; CombatState emits AttackResolved
- **Seeded RNG**: All randomness through CombatRng for reproducible battles
- **Headless simulation**: CombatSimulator can run battles without UI for testing

## Mission Setup Flow

1. GameState.StartMission() calls MissionFactory.BuildFromCampaign()
2. MissionFactory creates CombatState, spawns crew from campaign, spawns enemies
3. GameState stores result in CurrentCombat
4. MissionView reads CurrentCombat, creates ActorViews for existing actors

## Combat Flow

1. MissionView issues orders → CombatState.IssueAttackOrder() / IssueAbilityOrder()
2. Each tick: AIController.Tick() runs enemy decisions
3. Each tick: AbilitySystem.Tick() processes delayed abilities (grenades)
4. Each tick: CombatState.ProcessAttacks() checks actors with attack targets
5. CombatResolver.ResolveAttack() determines hit/miss
6. Actor.TakeDamage() applies damage, emits events
7. CombatState.CheckMissionEnd() detects victory/defeat
8. ActorView subscribes to events, updates visuals

## Ability Pipeline

1. Player presses ability key (G) → MissionView enters targeting mode
2. Player clicks target tile → MissionView.ConfirmAbilityTarget()
3. CombatState.IssueAbilityOrder() → AbilitySystem.UseAbility()
4. If delayed: ability queued in pendingAbilities, ticks down
5. On detonation: find actors in radius, apply damage/effects
6. AbilityDetonated event → MissionView shows explosion visual
