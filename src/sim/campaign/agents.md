# src/sim/campaign/ - Strategic Layer

Campaign/meta-game state: crew management, resources, mission tracking, jobs.

## Files

- **CampaignState.cs** - Root strategic state:
  - Time: CampaignTime for day tracking
  - Rng: RngService for deterministic generation
  - EventBus: for cross-domain event publishing (SF2)
  - Resources: money, fuel, parts, meds, ammo
  - Sector and CurrentNodeId for location
  - Crew roster management
  - Jobs: AvailableJobs, CurrentJob
  - FactionRep: reputation with each faction (0-100)
  - Mission costs and rewards (including time cost)
  - Campaign stats: TotalMoneyEarned, TotalCrewDeaths
  - `ApplyMissionOutput(MissionOutput)` for XP, injuries, deaths, job rewards
  - `AcceptJob()` sets absolute deadline, publishes JobAcceptedEvent
  - `ModifyFactionRep()` publishes FactionRepChangedEvent
  - `ConsumeMissionResources()` publishes ResourceChangedEvent
  - `Rest()` heals injuries and advances time
  - `ShouldRest()` checks if rest would be beneficial
  - `IsCampaignOver()` checks if all crew are dead
  - `HireCrew(name, role, cost)` - hire crew member, deducts money (MG1)
  - `FireCrew(crewId)` - remove crew from roster (MG1)
  - `AssignTrait(crewId, traitId)` - add trait to crew (MG1)
  - `RemoveTrait(crewId, traitId)` - remove non-permanent trait (MG1)
- **CrewMember.cs** - Individual crew member:
  - Identity: id, name, role (Soldier/Medic/Tech/Scout)
  - Status: IsDead, Injuries list, TraitIds list
  - Progression: Level, Xp, UnspentStatPoints
  - Primary stats (MG1): Grit, Reflexes, Aim, Tech, Savvy, Resolve
  - Derived stats: GetMaxHp(), GetHitBonus(), GetHackBonus(), GetTalkBonus(), GetStressThreshold()
  - Trait methods: HasTrait(), AddTrait(), RemoveTrait(), GetTraits(), GetTraitModifier(), GetEffectiveStat()
  - Factory: `CreateWithRole(id, name, role)` - creates crew with role-based starting stats
  - Methods: AddXp() (grants stat point on level up), SpendStatPoint(), CanDeploy(), AddInjury(), HealInjury()
- **StatType.cs** - CrewStatType enum for primary stats (MG1)
- **Trait.cs** - Trait system infrastructure (MG1):
  - TraitCategory enum: Background, Personality, Acquired, Injury
  - CrewStatModifier struct: stat type and flat bonus
  - TraitDef class: id, name, description, category, modifiers, tags, IsPermanent
- **TraitRegistry.cs** - Static registry of trait definitions (MG1):
  - 14 default traits across 4 categories
  - Background: ex_military, smuggler, corporate, frontier_born, spacer
  - Personality: brave, cautious, reckless, cold_blooded, empathetic
  - Acquired: vengeful, hardened, scarred
  - Injury (permanent): damaged_eye, shattered_knee, nerve_damage, head_trauma, chronic_pain
- **Sector.cs** - Sector map:
  - SectorNode: id, name, type, faction, position, connections
  - NodeType enum: Station, Outpost, Derelict, Asteroid, Nebula, Contested
  - `GenerateTestSector(seed)` creates 9-node test map
- **TravelSystem.cs** - Travel between nodes:
  - Fuel cost based on distance
  - Time cost based on distance (minimum 1 day)
  - `CanTravel()`, `Travel()` methods
  - `CalculateTravelDays()`, `GetTravelCostSummary()`
  - Publishes TravelCompletedEvent and ResourceChangedEvent (SF2)
  - Future: ambush encounters
- **Job.cs** - Job/contract definition:
  - JobDifficulty enum: Easy, Medium, Hard
  - JobType enum: Assault, Defense, Extraction
  - JobReward: money, parts, fuel, ammo
  - Employer/target faction, rep gains/losses
  - DeadlineDays (relative), DeadlineDay (absolute), HasDeadline
  - Links to target node and MissionConfig
- **JobSystem.cs** - Stateless job generation:
  - `GenerateJobsForNode()` creates 2-3 jobs at a location with deadlines
  - `GenerateMissionConfig()` creates combat setup from job difficulty
  - `ResetJobIdCounter()` resets job ID counter for new campaigns
  - Determines difficulty and deadline based on target node type
- **ShipState.cs** - Ship status (placeholder, to be expanded in MG2)

### MG2 Files (Planned)

- **Ship.cs** - Full ship implementation (MG2):
  - Hull integrity, max hull
  - Chassis type (Scout, Freighter, Corvette, Gunship)
  - Module slots: Engine, Weapon, Cargo, Utility
  - `GetCargoCapacity()` - base + cargo module bonuses
  - `InstallModule()`, `RemoveModule()` with slot validation
  - `TakeDamage()`, `Repair()`, `IsCritical()`, `IsDestroyed()`
  - Serialization: `GetState()`, `FromState()`
- **Item.cs** - Item system (MG2):
  - ItemCategory enum: Equipment, Consumable, Cargo, Module
  - ItemDef class: id, name, category, volume, baseValue, tags, stats
  - Item class: instance with id, defId, quantity
- **ItemRegistry.cs** - Static item definitions (MG2):
  - Equipment: weapons (rifle, pistol, shotgun, sniper), armor
  - Consumables: medkit, repair_kit
  - Cargo: medical_supplies, luxury_goods, contraband, weapons_cache
  - Modules: basic_engine, efficient_engine, small_cargo, large_cargo
- **Inventory.cs** - Inventory management (MG2):
  - Items list with capacity tracking
  - `AddItem()`, `RemoveItem()`, `RemoveByDefId()`
  - `GetUsedVolume()`, `CanAdd()`
  - Stacking for consumables and cargo

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
