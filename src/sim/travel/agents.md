# Travel Domain (`src/sim/travel/`)

This directory contains the Travel domain simulation layer.

## Purpose

Handle route planning and travel execution across the sector map.

## Files

| File | Purpose |
|------|---------|
| `TravelCosts.cs` | Cost calculation utilities (fuel, time, encounter chance) |
| `TravelSegment.cs` | Single route step with costs |
| `TravelPlan.cs` | Complete route with aggregates |
| `TravelPlanner.cs` | A* pathfinding and plan creation |
| `TravelState.cs` | In-progress travel state (TV2) |
| `TravelResult.cs` | Travel execution outcome - `TravelResult` class (TV2) |
| `TravelContext.cs` | Context for encounter generation (TV2) |
| `TravelExecutor.cs` | Main execution logic - day-by-day travel with encounters (TV2) |
| `TravelEncounterRecord.cs` | Record of encounter during travel (TV2) |

## Dependencies

- **Imports from**: `src/sim/world/` (WorldState, Route, SystemMetrics), `src/sim/campaign/` (CampaignState)
- **Imported by**: `src/core/` (GameState)

## Key Patterns

- Stateless services with explicit parameters
- Pure functions for cost calculations
- No Godot Node dependencies
- Day-by-day execution with encounter rolls (TV2)
