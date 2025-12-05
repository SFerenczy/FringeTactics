# src/sim/campaign/ - Strategic Layer

Campaign/meta-game state: crew management, resources, mission tracking, jobs.

## Files

- **CampaignState.cs** - Root strategic state:
  - Time: CampaignTime for day tracking
  - Rng: RngService for deterministic generation
  - EventBus: for cross-domain event publishing (SF2)
  - Resources: money, fuel, parts, meds, ammo
  - World (WorldState) and CurrentNodeId for location
  - Crew roster management
  - Jobs: AvailableJobs, CurrentJob
  - FactionRep: reputation with each faction (0-100)
  - Mission costs and rewards (including time cost)
  - Campaign stats: TotalMoneyEarned, TotalCrewDeaths
  - `ApplyMissionOutput(MissionOutput)` for XP, injuries, deaths, job rewards, ammo, loot (MG3)
  - `SpendAmmo()`, `AddAmmo()` - ammo resource management (MG3)
  - `CalculateMissionAmmoNeeded()` - estimate ammo for mission (MG3)
  - `HasEnoughAmmoForMission()` - pre-mission validation (MG3)
  - `ConsumeMissionResources()` - fuel upfront, ammo tracked per-actor (MG3)
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
- **MissionInputBuilder.cs** - Builds MissionInput from campaign state (MG3):
  - `Build(campaign, job)` - creates complete MissionInput
  - Maps crew stats (Grit→HP, Aim→Accuracy, Reflexes→MoveSpeed)
  - Resolves equipped weapons or falls back to preferred/default
  - Includes mission context (location, faction, tags)
  - Converts job objectives to mission objectives
- **CrewMember.cs** - Individual crew member:
  - Identity: id, name, role (Soldier/Medic/Tech/Scout)
  - Status: IsDead, Injuries list, TraitIds list
  - Progression: Level, Xp, UnspentStatPoints
  - Primary stats (MG1): Grit, Reflexes, Aim, Tech, Savvy, Resolve
  - Derived stats: GetMaxHp(), GetHitBonus(), GetHackBonus(), GetTalkBonus(), GetStressThreshold()
  - Trait methods: HasTrait(), AddTrait(), RemoveTrait(), GetTraits(), GetTraitModifier(), GetEffectiveStat()
  - Equipment (MG2): EquippedWeaponId, EquippedArmorId, EquippedGadgetId
  - Equipment methods: GetEquipped(), SetEquipped(), HasEquipped(), ClearEquipped(), GetAllEquippedIds()
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
- **TravelSystem.cs** - Travel between nodes:
  - Fuel cost based on distance
  - Time cost based on distance (minimum 1 day)
  - `CanTravel()`, `Travel()` methods
  - `CalculateTravelDays()`, `GetTravelCostSummary()`
  - Publishes TravelCompletedEvent and ResourceChangedEvent (SF2)
  - Future: ambush encounters
- **Job.cs** - Job/contract definition:
  - JobDifficulty enum: Easy, Medium, Hard
  - JobReward: money, parts, fuel, ammo
  - ContractType (from generation domain)
  - Employer/target faction, rep gains/losses
  - DeadlineDays (relative), DeadlineDay (absolute), HasDeadline
  - PrimaryObjective, SecondaryObjectives (GN1)
  - Links to target node and MissionConfig
- **JobSystem.cs** - Stable API for job operations:
  - `GenerateJobsForNode()` delegates to ContractGenerator (GN1)
  - `GenerateMissionConfig()` creates combat setup from job difficulty
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
  - Equipment: weapons (rifle, pistol, shotgun, sniper, smg), armor (light, medium, heavy), gadgets
  - Consumables: medkit, repair_kit, stim_pack
  - Cargo: medical_supplies, luxury_goods, contraband, weapons_cache, fuel_cells, electronics, raw_ore
  - Modules: engines, cargo pods, weapons, utility
- **Inventory.cs** - Inventory management (MG2):
  - Items list with capacity tracking
  - `AddItem()`, `RemoveItem()`, `RemoveByDefId()`
  - `GetUsedVolume()`, `CanAdd()`, `HasItem()`
  - Stacking for consumables and cargo
  - Serialization: `GetState()`, `FromState()`

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
