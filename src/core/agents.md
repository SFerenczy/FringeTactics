# src/core/ - Application Layer

Orchestrates game flow and state transitions. Bridges sim logic with scene presentation.

## Files

- **GameState.cs** - Autoload singleton managing campaign, scene transitions, mission results

## Responsibilities

- Game flow control (menu → campaign → mission → back to campaign)
- Scene transitions via GetTree().ChangeSceneToFile()
- Track actor-to-crew mapping for mission results
- Apply mission outcomes to campaign state

## Key Methods

- `StartNewCampaign()` - Create campaign with 4 crew, go to campaign screen
- `StartMission()` - Build CombatState via MissionFactory, publish `MissionStartedEvent`, load scene (MG3)
- `StartSandboxMission()` - Build sandbox CombatState, load scene (no campaign)
- `EndMission(outcome, combatState)` - Build output, publish `MissionEndedEvent`, apply to campaign (MG3)
- `GoToMainMenu()` - Clear campaign and CurrentCombat, return to menu

## Key Properties

- `Campaign` - Current CampaignState (null if no campaign)
- `CurrentCombat` - Current CombatState for active mission (null between missions)

## Time Query Accessors

- `GetCampaignDay()` - Current campaign day (0 if no campaign)
- `GetTacticalTick()` - Current tactical tick (0 if no mission)
- `GetCampaignDayFormatted()` - "Day N" string
- `GetTacticalTimeFormatted()` - "M:SS" string

## Dependencies

- **Imports from**: `src/sim/` (CampaignState, CombatState, ActorState)
- **Imported by**: `src/scenes/` (views query and command through GameState.Instance)

## Conventions

- Singleton pattern via static Instance property
- Scene paths defined as constants
- Wires SimLog to GD.Print on _Ready()
