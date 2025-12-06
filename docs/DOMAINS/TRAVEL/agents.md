# Travel Domain Documentation

This directory contains design and implementation documentation for the Travel domain.

## Purpose

The Travel domain handles movement of the player's ship across the galaxy map and the passage of time associated with that movement.

## Files

| File | Purpose |
|------|---------|
| `DOMAIN.md` | Domain responsibilities, inputs/outputs, interactions |
| `ROADMAP.md` | Implementation milestones and order |
| `TV0_IMPLEMENTATION.md` | Concept finalization (formulas, data structures) |
| `TV1_IMPLEMENTATION.md` | Route planning implementation spec |
| `TV2_IMPLEMENTATION.md` | Travel execution implementation spec |

## Milestone Status

| Milestone | Status | Description |
|-----------|--------|-------------|
| TV0 | ✅ Complete | Concept finalization |
| TV1 | ✅ Complete | Route planning (A* pathfinding, travel plans) |
| TV2 | ✅ Complete | Travel execution (fuel, time, encounters) |
| TV3 | ⬜ Pending | Simulation integration (G3) |

## Key Concepts

### TV1 (Implemented)
- **TravelSegment**: Single route step with costs
- **TravelPlan**: Complete route with aggregated costs
- **TravelPlanner**: A* pathfinding service
- **TravelCosts**: Cost calculation utilities

### TV2 (Implemented)
- **TravelExecutor**: Executes travel plans day-by-day
- **TravelState**: In-progress travel state (position, fuel consumed, encounters)
- **TravelExecutionResult**: Outcome of travel (completed, interrupted, paused)
- **TravelContext**: Context passed to Encounter domain when triggered

## Dependencies

- **Imports from**: World domain (WorldState, Route, SystemMetrics)
- **Imported by**: Campaign (CampaignState), Core (GameState)
- **Triggers**: Encounter domain (via TravelContext)
