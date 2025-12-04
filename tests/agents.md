# tests/ - Automated Tests

Unit and integration tests using GdUnit4.

## Structure

```
tests/
└── sim/
    ├── foundation/
    │   ├── SF0RngTests.cs    # SF0 RNG tests (RngStream, RngService, determinism, save/restore)
    │   ├── SF0ConfigTests.cs # SF0 config tests (validation, ConfigRegistry, Definitions)
    │   └── SF1TimeTests.cs   # SF1 time tests (CampaignTime, day advancement, deadlines)
    └── combat/
        ├── M0Tests.cs    # M0 milestone tests (map, movement, time)
        ├── M1Tests.cs    # M1 milestone tests (formation, collision, group movement)
        ├── M3Tests.cs    # M3 milestone tests (hit chance, ammo, auto-defend, combat)
        ├── M4Tests.cs    # M4 milestone tests (directional cover, cover heights, flanking, balance)
        ├── M5Tests.cs    # M5 milestone tests (interactables, channeled actions, doors, terminals, hazards)
        ├── M6Tests.cs    # M6 milestone tests (stealth, detection, alarm, perception, door/LOS integration)
        └── M7Tests.cs    # M7 milestone tests (session I/O, retreat integration)
```

## Running Tests

From Godot Editor:
- Open GdUnit4 panel (bottom dock)
- Click "Run All" or right-click specific tests

From command line:
```bash
dotnet test
```

or from project folder (adjust to your local setup)

```
.\addons\gdUnit4\runtest.cmd --godot_binary "E:\Godot\4.5\godot.exe" -a tests
```

## Test Conventions

### Attributes
- `[TestSuite]` - Marks a class as a test suite
- `[TestCase]` - Marks a method as a test
- `[RequireGodotRuntime]` - Required when using Godot types (Vector2I, etc.)

### Naming
- Test classes: `{Feature}Tests.cs`
- Test methods: `{Unit}_{Scenario}_{ExpectedResult}`

### What to Test

**Good candidates for unit tests:**
- `MapBuilder` - Template parsing, map generation
- `MapState` - Walkability, LOS blocking, entry zones
- `CombatState` - Win/lose conditions, phase transitions, collision avoidance
- `Actor` - Movement, damage, state changes, wall collision, ammo, reload
- `TimeSystem` - Pause/resume, tick accumulation
- `FormationCalculator` - Group destinations, formation maintenance
- `CombatResolver` - Hit chance calculation, attack resolution, cover modifiers
- `AIController` - Target prioritization, behavior decisions
- `CoverDirection` - Direction helpers, opposite directions
- `CoverHeight` - Height-based cover reduction values
- `MapState.GetCoverAgainst` - Directional cover detection with height
- `CombatBalance` - Balance constants and cover reduction helpers
- `Interactable` - State transitions, property helpers
- `ChanneledAction` - Progress tracking, completion
- `InteractionSystem` - Interaction execution, door/terminal/hazard logic
- `EnemyPerception` - Detection state, last known positions
- `PerceptionSystem` - LOS detection, alarm state, manual alerts
- `MissionFactory` - Perception initialization
- `RngStream` - Determinism, state serialization, call count tracking
- `RngService` - Stream isolation, save/restore, master seed derivation
- `ValidationResult` - Error/warning accumulation, merge
- `ConfigRegistry` - Config loading, validation, fail-fast mode
- `CampaignTime` - Day advancement, deadline checking, save/restore
- `WeaponDef.Validate` - Required fields, value ranges
- `EnemyDef.Validate` - Required fields, weapon reference
- `AbilityDef.Validate` - Required fields, value ranges

**Not ideal for unit tests:**
- Scene rendering (use manual testing)
- Input handling (use manual testing)
- Visual effects (use manual testing)

## Dependencies

- GdUnit4 (addon)
- gdUnit4.api NuGet package
