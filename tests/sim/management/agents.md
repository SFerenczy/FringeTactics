# tests/sim/management/ - Management Domain Tests

Unit tests for the Management domain (MG1, MG2).

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

### MG2 Tests (Planned)

- **MG2ShipTests.cs** - Ship tests:
  - Ship creation, starter ship defaults
  - Hull damage and repair
  - Cargo capacity calculation
  - Module installation/removal with slot limits
  - Critical/destroyed state checks

- **MG2InventoryTests.cs** - Inventory tests:
  - Add/remove items
  - Capacity enforcement
  - Stacking for consumables/cargo
  - Volume calculation
  - Find by ID/DefId

- **MG2ResourceTests.cs** - Resource operation tests:
  - SpendCredits/AddCredits validation
  - SpendFuel/AddFuel validation
  - SpendParts/AddParts validation
  - CanAfford checks
  - Event emission

- **MG2EquipmentTests.cs** - Equipment tests:
  - Equip/unequip items
  - Slot validation
  - Auto-unequip previous item

- **MG2SerializationTests.cs** - Serialization tests:
  - Ship round-trip
  - Inventory round-trip
  - Equipment round-trip
  - Save version 3

## Test Patterns

- Use `CampaignState.CreateNew()` for integration tests
- Use `new CrewMember(id, name)` for isolated unit tests
- Use `CrewMember.CreateWithRole()` for role-specific tests
- Subscribe to EventBus for event verification tests

## Dependencies

- **Imports from**: `src/sim/campaign/`, `src/sim/data/`, `src/sim/Events.cs`
- **Test framework**: GdUnit4
