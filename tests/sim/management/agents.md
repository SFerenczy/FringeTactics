# tests/sim/management/ - Management Domain Tests

Unit tests for the Management domain (MG1, MG2, MG3, MG4).

## Files

- **MG1CrewStatsTests.cs** - Phase 1 tests (25 tests):
  - Basic stats: all 6 stats exist and are settable
  - Role-based stats: CreateWithRole for Soldier, Medic, Tech, Scout
  - Derived stats: GetMaxHp, GetHitBonus, GetHackBonus, GetTalkBonus, GetStressThreshold
  - Stat points: SpendStatPoint success/failure, cap enforcement
  - XP and level up: AddXp grants stat points
  - Serialization: GetState/FromState round-trip
  - CampaignState integration: AddCrew uses role stats

- **MG1TraitTests.cs** - Phase 2 tests (31 tests):
  - TraitRegistry: Has, Get, GetByCategory, GetByTag
  - TraitDef: GetModifierFor with single/multiple modifiers
  - CrewMember trait methods: AddTrait, RemoveTrait, HasTrait, GetTraits
  - Trait modifiers: GetTraitModifier, GetEffectiveStat
  - Derived stats with traits: all derived stats include trait modifiers
  - Serialization: traits preserved in round-trip

- **MG1CrewOperationsTests.cs** - Phase 3 tests (25 tests):
  - HireCrew: success, insufficient funds, exact funds, role stats, unique IDs, events
  - FireCrew: success, not found, dead crew, last alive crew, events
  - AssignTrait: success, crew not found, dead crew, already has trait, invalid trait, events
  - RemoveTrait: success, crew not found, doesn't have trait, permanent trait, events
  - Integration: hire/fire workflow, assign/remove trait workflow

- **MG1SerializationTests.cs** - Phase 4 tests (21 tests):
  - Save version verification
  - CrewMemberData field verification
  - CrewMember GetState/FromState for all fields
  - Backward compatibility: Toughness → Grit migration
  - Null handling: TraitIds, Injuries
  - Full round-trip: all data preserved
  - CampaignState integration: crew with traits, hired crew

### MG2 Tests

- **MG2ShipTests.cs** - Ship tests (38 tests):
  - Ship creation: CreateStarter, CreateFromChassis (all 4 chassis types)
  - Hull: damage, repair, clamping, IsCritical, IsDestroyed, GetHullPercent
  - Cargo capacity: base + cargo modules
  - Modules: install, remove, slot limits, find
  - Serialization: GetState/FromState round-trip
  - CampaignState integration: CreateNew has ship, round-trip preserves ship

- **MG2InventoryTests.cs** - Inventory tests (43 tests):
  - ItemRegistry: Has, Get, GetByCategory, GetByTag
  - ItemDef: HasTag, IsStackable, GetStat, EquipSlot, ModuleSlotType
  - Inventory add: works, respects capacity, stacks consumables/cargo, no stack equipment
  - Inventory volume: GetUsedVolume, CanAdd
  - Inventory remove: RemoveItem, RemoveByDefId
  - Inventory find: FindByDefId, FindById, HasItem
  - Inventory category/tag: GetByCategory, GetByTag
  - Item instance: GetDef, GetTotalVolume, GetTotalValue, GetName
  - Serialization: GetState/FromState round-trip

- **MG2ResourceTests.cs** - Resource & integration tests (40 tests):
  - SpendCredits/AddCredits: validation, events, TotalMoneyEarned
  - SpendFuel/AddFuel: validation
  - SpendParts/AddParts: validation
  - CanAfford: single and multiple resources
  - Inventory integration: AddItem, RemoveItem, RemoveItemByDef, HasItem
  - Cargo: GetCargoCapacity, GetUsedCargo, GetAvailableCargo
  - Ship operations: RepairShip, DamageShip, InstallModule, RemoveModule
  - Serialization: round-trip preserves inventory and resources

- **MG2EquipmentTests.cs** - Equipment tests (27 tests):
  - CrewMember equipment methods: GetEquipped, SetEquipped, HasEquipped, ClearEquipped, GetAllEquippedIds
  - CampaignState.EquipItem: works, emits event, fails for invalid crew/item/non-equipment
  - Auto-unequip previous item, different slots
  - CampaignState.UnequipItem: works, emits event, fails for invalid
  - GetEquippedItem: returns item or null
  - Serialization: CrewMember and CampaignState round-trip preserves equipment

### MG3 Tests

- **MG3MissionInputTests.cs** - Mission input builder tests (31 tests)
- **MG3MissionOutputTests.cs** - Mission output processing tests (17 tests)
- **MG3IntegrationTests.cs** - Full mission flow integration tests (9 tests)

### MG4 Tests

- **MG4FlagTests.cs** - Campaign flags system tests (25 tests):
  - SetFlag: sets value, updates existing, ignores null/empty, emits event, no event if unchanged
  - GetFlag: returns false for unset, returns correct value, handles null/empty
  - HasFlag: returns false for unset/false, returns true for set
  - ClearFlag: removes flag, returns false for nonexistent, emits event
  - Multiple flags: can set and retrieve multiple
  - Serialization: GetState includes flags, FromState restores flags, null-safe, round-trip

- **MG4EffectTests.cs** - Encounter effect application tests (47 tests):
  - ApplyEncounterOutcome: null handling, empty effects, applies all, emits event, clears active
  - Resource effects: add/remove credits, fuel, parts, meds; partial remove; events
  - Crew injury: injures target, uses skill check crew, emits event, fails if no crew
  - Crew XP: grants XP, handles level up, zero is no-op
  - Crew trait: adds/removes trait, fails for missing trait ID
  - Ship damage: damages hull, emits event, zero is no-op
  - Faction rep: increases/decreases, emits event, fails for missing faction ID
  - Set flag: sets flag, sets to false, fails for missing flag ID
  - Time delay: advances time, zero is no-op
  - Cargo: add/remove items, fails for missing item ID, fails if not enough
  - Flow effects: GotoNode, EndEncounter, TriggerTactical return true (no-op in campaign)
  - Multiple effects: all applied, partial success

- **MG4IntegrationTests.cs** - Travel integration and full flow tests (25 tests):
  - ConsumeTravelFuel: spends fuel, fails if insufficient, zero succeeds, exact succeeds, emits event
  - CanAffordTravel (int): true if enough, false if insufficient, true for exact, true for zero
  - CanAffordTravel (TravelPlan): true for valid affordable, false for unaffordable, false for invalid/null
  - GetTravelBlockReason: null for affordable, reasons for null/invalid/insufficient
  - Full encounter flow: effects applied and cleared, skill check crew targeting
  - Travel then encounter: resources tracked correctly
  - Multiple encounters: flags preserved across encounters
  - Serialization: flags preserved, active encounter serialized, null handling, effects work after load

## Test Patterns

- Use `CampaignState.CreateNew()` for integration tests
- Use `new CrewMember(id, name)` for isolated unit tests
- Use `CrewMember.CreateWithRole()` for role-specific tests
- Subscribe to EventBus for event verification tests

## Dependencies

- **Imports from**: `src/sim/campaign/`, `src/sim/data/`, `src/sim/Events.cs`
- **Test framework**: GdUnit4
