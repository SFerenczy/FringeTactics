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

## Dependencies

- **Imports from**: `src/sim/campaign/` (Job, CrewMember), `src/sim/` (RngStream), `src/sim/data/` (ObjectiveData), `src/sim/world/` (WorldState for reachable systems)
- **Imported by**: `src/sim/campaign/JobSystem.cs`, `src/sim/campaign/CampaignState.cs`

## Key Patterns

- **Deterministic**: All generation uses `RngStream` exclusively (no `System.Random`)
- **Template-based**: Titles/descriptions selected from predefined pools
- **Data-driven config**: Weight and reward values in `GenerationConfig` for easy balancing
- **Graph logic in WorldState**: Reachable system queries delegated to `WorldState.GetReachableSystems()`
