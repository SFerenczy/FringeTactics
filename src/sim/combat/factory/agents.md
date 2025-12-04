# src/sim/combat/factory/ - Mission Setup and Contracts

Factories for building combat state and formal input/output contracts for mission flow.

## Files

### Factories
- **MissionFactory.cs** - Builds CombatState from MissionConfig + CampaignState. Has BuildFromInput() for formal MissionInput.
- **MapBuilder.cs** - Factory for creating MapState from templates or configs
- **FormationCalculator.cs** - Stateless utility for group movement: calculates spread destinations maintaining relative formation
- **CombatSimulator.cs** - Headless battle simulator for testing/balancing

### Contracts
- **MissionInput.cs** - Formal input contract: CrewDeployment, MissionObjective, MissionContext. All data needed to start a mission.
- **MissionOutput.cs** - Formal output contract: MissionOutcome, CrewOutcome, ObjectiveStatus. All data returned after mission ends.
- **MissionOutputBuilder.cs** - Stateless utility to build MissionOutput from completed CombatState.

## Responsibilities

- Build CombatState from campaign data or test configs
- Define formal contracts between campaign and combat layers
- Enable headless simulation for testing

## Dependencies

- **Imports from**: `state/`, `systems/`, `data/`, `src/sim/data/` (MissionConfig)
- **Imported by**: `src/core/GameState.cs`, tests

## Key Patterns

- **Factory pattern**: MissionFactory.Build*() methods create fully initialized CombatState
- **Contract-based flow**: MissionInput → CombatState → MissionOutput
- **Template parsing**: MapBuilder parses string[] templates for map layout
