# src/sim/campaign/ - Strategic Layer

Campaign/meta-game state: crew management, resources, mission tracking, jobs.

## Files

- **CampaignState.cs** - Root strategic state:
  - Resources: money, fuel, parts, meds, ammo
  - Sector and CurrentNodeId for location
  - Crew roster management
  - Jobs: AvailableJobs, CurrentJob
  - FactionRep: reputation with each faction (0-100)
  - Mission costs and rewards
  - Campaign stats: TotalMoneyEarned, TotalCrewDeaths
  - `ApplyMissionResult(MissionResult)` for XP, injuries, deaths, job rewards
  - `AcceptJob()`, `ClearCurrentJob()`, `IsAtJobTarget()`
  - `IsCampaignOver()` checks if all crew are dead
- **CrewMember.cs** - Individual crew member:
  - Identity: id, name, role (Soldier/Medic/Tech/Scout)
  - Status: IsDead, Injuries list
  - Progression: Level, Xp, stats (Aim, Toughness, Reflexes)
  - Methods: AddXp(), CanDeploy(), AddInjury(), HealInjury()
- **Sector.cs** - Sector map:
  - SectorNode: id, name, type, faction, position, connections
  - NodeType enum: Station, Outpost, Derelict, Asteroid, Nebula, Contested
  - `GenerateTestSector(seed)` creates 9-node test map
- **TravelSystem.cs** - Travel between nodes:
  - Fuel cost based on distance
  - `CanTravel()`, `Travel()` methods
  - Future: ambush encounters
- **Job.cs** - Job/contract definition:
  - JobDifficulty enum: Easy, Medium, Hard
  - JobType enum: Assault, Defense, Extraction
  - JobReward: money, parts, fuel, ammo
  - Employer/target faction, rep gains/losses
  - Links to target node and MissionConfig
- **JobSystem.cs** - Stateless job generation:
  - `GenerateJobsForNode()` creates 2-3 jobs at a location
  - `GenerateMissionConfig()` creates combat setup from job difficulty
  - Determines difficulty based on target node type
- **ShipState.cs** - Ship status (future use)

## Responsibilities

- Track persistent state between missions
- Manage crew roster, XP, levels, injuries
- Apply mission results (deaths, injuries, XP, rewards)
- Track and consume resources (ammo/fuel per mission)
- Validate mission start (enough resources, deployable crew)
- Generate and manage job offers at locations
- Track faction reputation changes

## Dependencies

- **Imports from**: `src/sim/data/` (for MissionConfig, definitions)
- **Imported by**: `src/core/GameState`, `src/sim/combat/` (for crew → actor mapping)

## Conventions

- State objects are mutable containers
- Business logic in stateless system classes (JobSystem, TravelSystem)
- Events for significant state changes
