# Generation Domain (`src/sim/generation/`)

This directory contains the contract/mission generation system.

## Purpose

Generate mission offers (contracts) based on player state, world context, and templates. Pure simulation logic with no Godot dependencies.

## Files

| File | Purpose |
|------|---------|
| `ContractType.cs` | 6 contract archetypes (Assault, Delivery, Escort, Raid, Heist, Extraction) with extension methods |
| `ContractTemplates.cs` | Title and description templates for each contract type |
| `GenerationContext.cs` | Bundled context for generation (player state, hub metrics, RNG) with `FromCampaign()` factory |
| `Objective.cs` | Mission objective class with factory methods for primary/secondary objectives |
| `ContractGenerator.cs` | Main generator: type selection, difficulty scaling, reward calculation, objective generation |

## Dependencies

- **Imports from**: `src/sim/campaign/` (Job, CrewMember), `src/sim/` (RngStream), `src/sim/data/` (ObjectiveData)
- **Imported by**: `src/sim/campaign/JobSystem.cs`, `src/sim/campaign/CampaignState.cs`

## Key Patterns

- **Deterministic**: All generation uses RNG streams for reproducibility
- **Template-based**: Titles/descriptions selected from predefined pools
- **Constants**: Weight and reward values extracted to named constants in `ContractGenerator`
