# src/sim/combat/data/ - Combat Data Definitions

Data structures, constants, and utilities for tactical combat.

## Files

- **WeaponData.cs** - Weapon stats: range, damage, cooldown ticks, magazine size, reload time
- **AbilityData.cs** - Ability definitions: targeting type, range, cooldown, delay, radius, damage, effects
- **CombatStats.cs** - Combat statistics: shots fired, hits, misses for player and enemy
- **CombatRng.cs** - Seeded RNG wrapper for deterministic simulation
- **CoverDirection.cs** - CoverHeight enum (None/Low/Half/High/Full), CoverDirection flags, direction helpers
- **CombatBalance.cs** - Centralized combat balance constants, cover reduction values
- **AttackResult.cs** - Struct for attack resolution results: hit/miss, damage, cover height
- **StatModifier.cs** - Stat modifier system: StatType enum, ModifierCollection for effective stats
- **ActorTypes.cs** - Constants for actor type strings: Crew, Enemy, Drone
- **GridUtils.cs** - Grid utility methods: GetStepDirection()

## Responsibilities

- Define data structures used across combat simulation
- Provide balance constants and formulas
- Wrap RNG for determinism

## Dependencies

- **Imports from**: Nothing (leaf layer)
- **Imported by**: `state/`, `systems/`, `factory/`

## Key Patterns

- **Pure data**: No logic beyond simple calculations
- **Centralized balance**: All magic numbers in CombatBalance.cs
- **Deterministic RNG**: CombatRng wraps System.Random with explicit seeding
