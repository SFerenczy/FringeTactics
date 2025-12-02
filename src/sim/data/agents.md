# src/sim/data/ - Configuration and Data Structures

Static configuration types and data definitions used across the sim layer.

## Files

- **Definitions.cs** - Central registry for all game data:
  - `Definitions.Weapons` - WeaponDef lookup by ID (rifle, pistol, smg, shotgun)
  - `Definitions.Enemies` - EnemyDef lookup by ID (grunt, gunner, sniper, heavy)
  - `Definitions.Abilities` - AbilityDef lookup by ID (frag_grenade, stun_grenade, stun_shot)
- **MissionConfig.cs** - Mission setup: grid size, map template, entry zone, crew spawns, enemy spawns. Has `CreateTestMission()`, `CreateM0TestMission()`, `CreateHardMission()`.

## Responsibilities

- Define all game content as data (weapons, enemies, abilities)
- Provide lookup by ID for spawning
- Define mission configurations
- No runtime state—these are templates/definitions

## Usage

```csharp
// Look up enemy definition
var enemyDef = Definitions.Enemies.Get("grunt");

// Create weapon from definition
var weapon = WeaponData.FromId("rifle");

// Get ability for use
var ability = Definitions.Abilities.Get("frag_grenade")?.ToAbilityData();
```

## Dependencies

- **Imports from**: Nothing
- **Imported by**: `src/sim/combat/` (MissionFactory, MissionView)

## Conventions

- All IDs are lowercase snake_case strings
- Definitions are dictionaries with string keys
- `Get(id)` returns default if ID not found (fail-safe)
- Keep data-only; no behavior beyond construction
