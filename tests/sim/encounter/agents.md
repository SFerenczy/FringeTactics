# Encounter Tests (`tests/sim/encounter/`)

Unit tests for the Encounter domain.

## Files

| File | Purpose |
|------|---------|
| `EN1EffectTests.cs` | Tests for EncounterEffect factory methods |
| `EN1ConditionTests.cs` | Tests for EncounterCondition evaluation |
| `EN1TemplateTests.cs` | Tests for EncounterTemplate validation and test templates |
| `EN1RunnerTests.cs` | Tests for EncounterRunner state machine |
| `EN2SkillCheckTests.cs` | Tests for SkillCheck resolution and trait bonuses |
| `EN2RunnerIntegrationTests.cs` | Tests for EncounterRunner skill check integration |

## Test Coverage

### EN1EffectTests (~14 tests)
- Factory methods create correct effect types
- Resource effects with positive/negative amounts
- Crew, ship, world, and flow effects

### EN1ConditionTests (~22 tests)
- Resource threshold conditions
- Trait and stat conditions
- Faction rep and system tag conditions
- Composite conditions (And, Or, Not)
- Null context handling

### EN1TemplateTests (~18 tests)
- Test template validation
- Node and option structure
- Template validity checks

### EN1RunnerTests (~21 tests)
- Node navigation and transitions
- Option filtering by conditions
- Effect accumulation
- Auto-transition processing
- Branching path handling
- Error cases

### EN2SkillCheckTests (~27 tests)
- Basic skill check resolution
- Trait bonus/penalty calculation
- Crew selection for checks
- Success chance preview
- Determinism verification
- Critical success/failure thresholds

### EN2RunnerIntegrationTests (~16 tests)
- Skill check integration with EncounterRunner
- Success/failure outcome branching
- Trait bonus effects on outcomes
- Template validation for skill check encounters
- EncounterContext helper methods

## Running Tests

```bash
.\addons\gdUnit4\runtest.cmd --godot_binary "E:\Godot\4.5\godot.exe" -a tests/sim/encounter
```
