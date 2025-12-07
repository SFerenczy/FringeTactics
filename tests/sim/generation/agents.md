# Generation Tests (`tests/sim/generation/`)

Unit tests for the Generation domain (GN1+).

## Files

| File | Purpose |
|------|---------|
| `GN1GenerationContextTests.cs` | Tests for `GenerationContext`, `CalculateCrewPower()`, `PowerTier`, reputation helpers |
| `GN1ContractGeneratorTests.cs` | Tests for `ContractGenerator`, contract type selection, difficulty scaling, rewards, objectives |
| `GN1DeterminismTests.cs` | Tests verifying deterministic generation (same seed = same contracts) |
| `GN1SerializationTests.cs` | Tests for `Job`/`Objective` serialization, legacy migration |
| `GN3RegistryTests.cs` | Tests for `EncounterTemplateRegistry` registration, retrieval, tag filtering, eligibility |
| `GN3NameGeneratorTests.cs` | Tests for NPC, cargo, and ship name generation methods |
| `GN3GeneratorTests.cs` | Tests for `EncounterGenerator` selection, weighting, parameter resolution, determinism |

## Running Tests

```bash
.\addons\gdUnit4\runtest.cmd --godot_binary "E:\Godot\4.5\godot.exe" -a tests/sim/generation
```

## Key Test Categories

- **Crew Power Calculation**: Empty crew, dead crew, multiple crew, experience bonus
- **Power Tiers**: Rookie/Competent/Veteran/Elite boundaries
- **Contract Generation**: Count, types, objectives, rewards, factions
- **Difficulty Scaling**: Rookie gets easier, Veteran gets harder
- **Determinism**: Same seed produces identical contracts
- **Serialization**: Round-trip for ContractType, objectives, legacy migration
