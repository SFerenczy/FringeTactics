# src/scenes/encounter/ - Encounter UI

Displays narrative encounters to the player during travel.

## Files

- **EncounterScreen.tscn** - Scene file for encounter display
- **EncounterScreen.cs** - Controller that displays narrative, options, skill checks, and effects

## Responsibilities

- Display encounter narrative text from `EncounterNode.TextKey`
- Show available options with skill check success chances
- Handle option selection via `EncounterRunner.SelectOption()`
- Display skill check results (roll breakdown, success/failure)
- Show accumulated effects before completion
- Trigger `GameState.ResolveEncounter()` on completion

## Key Flow

1. `GameState.HandleTravelResult()` transitions here when `TravelResultStatus.PausedForEncounter`
2. `EncounterScreen._Ready()` reads `Campaign.ActiveEncounter`
3. Player selects options, sees results
4. On completion, calls `GameState.ResolveEncounter()` which applies effects and resumes travel

## Dependencies

- **Imports from**: `src/sim/encounter/` (EncounterRunner, EncounterInstance, EncounterContext, SkillCheck)
- **Imports from**: `src/core/` (GameState)
- **Imported by**: None (scene loaded by GameState)

## Conventions

- UI built programmatically in `CreateUI()`
- Subscribes to `SkillCheckResolvedEvent` for detailed roll display
- Uses `ResolveText()` for parameter substitution in narrative
