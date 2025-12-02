# tests/ - Automated Tests

Unit and integration tests using GdUnit4.

## Structure

```
tests/
└── sim/
    └── combat/
        └── M0Tests.cs    # M0 milestone tests
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
- `CombatState` - Win/lose conditions, phase transitions
- `Actor` - Movement, damage, state changes
- `TimeSystem` - Pause/resume, tick accumulation

**Not ideal for unit tests:**
- Scene rendering (use manual testing)
- Input handling (use manual testing)
- Visual effects (use manual testing)

## Dependencies

- GdUnit4 (addon)
- gdUnit4.api NuGet package
