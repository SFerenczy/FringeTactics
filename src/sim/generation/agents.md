# Generation Domain (`src/sim/generation/`)

This directory contains the contract/mission generation and galaxy generation systems.

## Purpose

Generate mission offers (contracts) and procedural galaxies based on player state, world context, and templates. Pure simulation logic with no Godot dependencies.

## Files

| File | Purpose |
|------|---------|
| `ContractType.cs` | Contract archetypes enum (currently: Assault, Extraction). Future types commented out until tactical layer supports them. |
| `ContractTemplates.cs` | Title and description templates for implemented contract types |
| `GenerationContext.cs` | Bundled context for generation (player state, hub metrics, RNG) with `FromCampaign()` factory |
| `GenerationConfig.cs` | Data-driven configuration for weights and rewards (enables balancing without recompile) |
| `Objective.cs` | Mission objective class with factory methods for primary/secondary objectives |
| `ContractGenerator.cs` | Main generator: type selection, difficulty scaling, reward calculation, objective generation |
| `GalaxyConfig.cs` | Configuration for procedural galaxy generation (system count, spatial constraints, faction/type weights) |
| `GalaxyGenerator.cs` | Procedural galaxy generator: position generation, route creation, faction assignment, station generation |
| `NameGenerator.cs` | Name generation utilities for systems, stations, and sectors |
| `EncounterTemplateRegistry.cs` | Central registry for encounter templates with tag filtering and eligibility checking (GN3) |
| `EncounterGenerator.cs` | Generates encounter instances from templates with weighted selection and parameter resolution (GN3) |
| `ProductionEncounters.cs` | 11 gameplay encounter templates for travel (pirate, patrol, distress, trader, smuggler, derelict, faction, mystery, mechanical, refugee, trial_by_fire) (GN3) |
| `EncounterWeightConfig.cs` | Configuration for encounter selection weights (balancing without code changes) (GN3) |
| `EncounterValueConfig.cs` | Configuration for encounter effect values (rewards, costs, difficulties) (GN3) |

## Dependencies

- **Imports from**: `src/sim/campaign/` (Job, CrewMember), `src/sim/` (RngStream), `src/sim/data/` (ObjectiveData), `src/sim/world/` (WorldState for reachable systems)
- **Imported by**: `src/sim/campaign/JobSystem.cs`, `src/sim/campaign/CampaignState.cs`

## Key Patterns

- **Deterministic**: All generation uses `RngStream` exclusively (no `System.Random`)
- **Template-based**: Titles/descriptions selected from predefined pools
- **Data-driven config**: Weight and reward values in `GenerationConfig` for easy balancing
- **Graph logic in WorldState**: Reachable system queries delegated to `WorldState.GetReachableSystems()`
