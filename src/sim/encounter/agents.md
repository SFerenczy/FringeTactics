# Encounter Domain (`src/sim/encounter/`)

This directory contains the Encounter domain simulation layer.

## Purpose

Handle non-combat events and lightweight interactive scenes: text-based choices, skill checks, emergent narrative beats during travel or station life.

## Files

| File | Purpose |
|------|---------|
| `EffectType.cs` | Enum of effect types (resource, crew, ship, world, flow) |
| `EncounterEffect.cs` | Atomic effect from encounter choices |
| `ConditionType.cs` | Enum of condition types for option visibility |
| `EncounterCondition.cs` | Condition evaluation logic |
| `EncounterOutcome.cs` | Outcome with effects and node transition |
| `EncounterOption.cs` | Player choice with conditions and outcome |
| `EncounterNode.cs` | Single step in an encounter |
| `EncounterTemplate.cs` | Complete encounter definition |
| `EncounterContext.cs` | Snapshot of state for condition evaluation |
| `EncounterInstance.cs` | Runtime state + serialization (EncounterInstanceData) |
| `EncounterRunner.cs` | Stateless state machine with EventBus integration |
| `EncounterStepResult.cs` | Result of an encounter step |
| `SkillCheckDef.cs` | Skill check definition (EN2 stub) |
| `CrewSnapshot.cs` | Lightweight crew snapshot for condition evaluation |
| `TestEncounters.cs` | Factory methods for test encounter templates |

## Dependencies

- **Imports from**: `src/sim/campaign/` (CrewMember, CampaignState), `src/sim/travel/` (TravelContext)
- **Imported by**: `src/core/` (GameState), future `src/scenes/encounter/`

## Key Patterns

- Data-driven templates with no behavior
- Stateless evaluation via EncounterContext
- Effect accumulation (not application) - MG4 applies effects
- Factory methods for common conditions and effects
- No Godot Node dependencies
