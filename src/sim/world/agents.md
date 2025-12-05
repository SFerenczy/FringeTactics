# World Domain (`src/sim/world/`)

This directory contains the World domain implementation - the canonical representation of the game world.

## Purpose

Owns galaxy topology, locations, factions, and world-attached metrics. Provides query APIs for other domains.

## Files

| File | Purpose |
|------|---------|
| `WorldState.cs` | Main world container with query APIs |
| `StarSystem.cs` | Star system with metrics and tags |
| `Station.cs` | Station with facilities |
| `Facility.cs` | Station facility (shop, mission_board, etc.) |
| `Faction.cs` | Faction with type and metrics |
| `FactionRegistry.cs` | Loads factions from data/factions.json |
| `SystemMetrics.cs` | System-level metrics (security, economy, etc.) |
| `SystemType.cs` | System type enum (canonical, replaces NodeType) |
| `WorldTags.cs` | Well-known tag constants |

## Key Patterns

- **WorldState as primary**: All systems use WorldState, not Sector
- **Query-first design**: Rich query APIs, limited mutation
- **Data-driven factions**: Loaded from JSON via FactionRegistry
- **Serialization support**: All classes have GetState/FromState

## Dependencies

- **Imports from**: `Godot` (Vector2, Color only)
- **Imported by**: `CampaignState`, `JobSystem`, `TravelSystem`, `SectorView`

## Non-Responsibilities

- Does not simulate metric changes (that's Simulation domain)
- Does not generate content (that's Generation domain)
- Does not handle UI or player input
