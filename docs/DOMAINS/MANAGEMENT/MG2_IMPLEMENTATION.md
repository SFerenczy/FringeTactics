# MG2 – Ship & Resources: Implementation Plan

**Status**: ⏳ Pending  
**Depends on**: MG1 (PlayerState & Crew Core) ✅ Complete  
**Blocked by**: None

This document breaks down **Milestone MG2** from `ROADMAP.md` into concrete implementation steps.

**Goal**: Implement ship state and resource management, including hull integrity, modules, cargo capacity, and inventory operations.

---

## Current State Assessment

### What We Have (From MG1 + SF0-SF3)

| Component | Status | Notes |
|-----------|--------|-------|
| `CampaignState` | ✅ Complete | Owns crew, resources, sector, jobs |
| `CrewMember` | ✅ Complete | 6 stats, traits, injuries, XP/leveling |
| Resources (flat) | ✅ Complete | `Money`, `Fuel`, `Parts`, `Meds`, `Ammo` on CampaignState |
| `SaveData` | ✅ Complete | Version 2, handles crew serialization |
| `EventBus` | ✅ Complete | `ResourceChangedEvent` exists |
| `RngService` | ✅ Complete | Deterministic generation |

### What MG2 Requires vs What We Have

| MG2 Requirement | Current Status | Gap |
|-----------------|----------------|-----|
| `Ship` class with hull integrity | ⚠️ Partial | `ShipState.cs` exists with basic Hp/Fuel, needs expansion |
| Ship modules (weapons, engines, cargo) | ❌ Missing | Need module system |
| Cargo capacity | ❌ Missing | Need capacity tracking |
| `Inventory` class | ❌ Missing | Need item storage |
| `SpendCredits(amount)` | ⚠️ Partial | Direct `Money -= x` exists, no validation method |
| `AddCredits(amount)` | ⚠️ Partial | Direct `Money += x` exists, no validation method |
| `AddItem(item)` / `RemoveItem(itemId)` | ❌ Missing | Need inventory operations |
| `EquipItem(crewId, itemId)` | ❌ Missing | Need equipment system |
| Capacity enforcement | ❌ Missing | Need validation |

---

## Architecture Decisions

### Ship as a Separate Class

**Decision**: Create `Ship` as a standalone class owned by `CampaignState`.

**Rationale**:
- Ships have distinct state (hull, modules, cargo) separate from crew
- Future: multiple ships, ship upgrades, ship combat
- Matches CAMPAIGN_FOUNDATIONS §1 (cargo capacity as progression axis)

### Module System Design

**Decision**: Use a slot-based module system with typed slots.

**Structure**:
```
Ship
├── Hull (integrity, max integrity)
├── ModuleSlots[]
│   ├── Engine slot
│   ├── Weapon slots (0-2)
│   ├── Cargo slots (1-3)
│   └── Utility slots (0-2)
└── Cargo (items stored)
```

### Item System Design

**Decision**: Items are data-driven with type tags, not deep inheritance.

**Item Categories**:
- **Equipment**: Weapons, armor, gadgets (equippable by crew)
- **Consumables**: Meds, repair kits (used from inventory)
- **Cargo**: Trade goods, contraband (bulk items with volume)
- **Modules**: Ship upgrades (installed on ship)

**Rationale**: Follows architecture guidelines: "data over objects". Tags enable flexible behavior (`illegal`, `perishable`, etc.).

### Resource Operations Pattern

**Decision**: All resource changes go through validated methods that emit events.

**Rationale**: Single point of validation, consistent event emission, prevents negative resources.

---

## Data Model

### Ship Chassis Types (Initial Set)

| Chassis | Hull | Engine | Weapon | Cargo | Utility | Notes |
|---------|------|--------|--------|-------|---------|-------|
| Scout | 50 | 1 | 1 | 1 | 1 | Fast, fragile, starter |
| Freighter | 80 | 1 | 1 | 3 | 1 | Cargo focus |
| Corvette | 100 | 2 | 2 | 2 | 2 | Balanced |
| Gunship | 120 | 2 | 3 | 1 | 2 | Combat focus |

### Item Data Structure

```csharp
public class ItemDef
{
    public string Id { get; set; }
    public string Name { get; set; }
    public ItemCategory Category { get; set; }  // Equipment, Consumable, Cargo, Module
    public int Volume { get; set; }             // Space required
    public int BaseValue { get; set; }          // Base price in credits
    public List<string> Tags { get; set; }      // illegal, perishable, medical, etc.
}
```

---

## Implementation Steps

### Phase 1: Ship Foundation (Priority: Critical)

#### Step 1.1: Expand Ship Class

**File**: `src/sim/campaign/Ship.cs` (rename from `ShipState.cs`)

**Note**: `ShipState.cs` exists with basic Hp/Fuel. Rename to `Ship.cs` and expand.

- `Ship` class with hull, max hull, chassis ID, name
- `ShipModule` class for installed modules
- `ShipSlotType` enum (Engine, Weapon, Cargo, Utility)
- Slot limits per chassis type
- `GetCargoCapacity()` - base + cargo module bonuses
- `InstallModule()` / `RemoveModule()` with slot validation
- `TakeDamage()` / `Repair()` methods
- `CreateStarter()` factory method

**Acceptance Criteria**:
- [ ] `Ship` class with hull, modules, cargo capacity
- [ ] Module installation/removal with slot validation
- [ ] `CreateStarter()` creates a scout with basic modules

#### Step 1.2: Create Ship Serialization

**File**: `src/sim/data/SaveData.cs`

- Add `ShipData` class
- Add `ShipModuleData` class
- Add `Ship` field to `CampaignStateData`

#### Step 1.3: Add Ship to CampaignState

**File**: `src/sim/campaign/CampaignState.cs`

- Add `Ship` property
- Create starter ship in `CreateNew()`
- Add `GetState()` / `FromState()` for ship

---

### Phase 2: Item & Inventory System (Priority: Critical)

#### Step 2.1: Create Item Definitions

**New File**: `src/sim/campaign/Item.cs`

- `ItemCategory` enum
- `ItemDef` class for item definitions
- `Item` class for inventory instances

#### Step 2.2: Create ItemRegistry

**New File**: `src/sim/campaign/ItemRegistry.cs`

- Static registry of item definitions
- Default items: weapons, armor, consumables, cargo, modules
- Lookup by ID, category, tag

#### Step 2.3: Create Inventory Class

**New File**: `src/sim/campaign/Inventory.cs`

- `Items` list
- `GetUsedVolume()` - total volume of items
- `CanAdd()` - check capacity
- `AddItem()` - add with stacking for consumables/cargo
- `RemoveItem()` / `RemoveByDefId()`
- `FindById()` / `FindByDefId()`

#### Step 2.4: Add Inventory Serialization

**File**: `src/sim/data/SaveData.cs`

- Add `InventoryData` class
- Add `ItemData` class
- Add `Inventory` field to `CampaignStateData`

---

### Phase 3: Resource Operations (Priority: High)

#### Step 3.1: Add Resource Operation Methods

**File**: `src/sim/campaign/CampaignState.cs`

Add validated methods:
- `SpendCredits(amount, reason)` - returns false if insufficient
- `AddCredits(amount, reason)`
- `SpendFuel(amount, reason)`
- `AddFuel(amount, reason)`
- `SpendParts(amount, reason)`
- `AddParts(amount, reason)`
- `CanAfford(credits, fuel, parts)`

All methods emit `ResourceChangedEvent`.

#### Step 3.2: Add Inventory to CampaignState

**File**: `src/sim/campaign/CampaignState.cs`

- Add `Inventory` property
- `GetCargoCapacity()` - from ship
- `GetUsedCargo()` / `GetAvailableCargo()`
- `AddItem(defId, quantity)`
- `RemoveItem(itemId)` / `RemoveItemByDef(defId, quantity)`

#### Step 3.3: Add Item Events

**File**: `src/sim/Events.cs`

- `ItemAddedEvent`
- `ItemRemovedEvent`
- `ItemEquippedEvent`
- `ShipHullChangedEvent`
- `ShipModuleChangedEvent`

---

### Phase 4: Equipment System (Priority: Medium)

#### Step 4.1: Add Equipment to CrewMember

**File**: `src/sim/campaign/CrewMember.cs`

- Add `EquippedWeaponId`, `EquippedArmorId`, `EquippedGadgetId`
- `GetEquipped(slot)` / `SetEquipped(slot, itemId)`
- Update serialization

#### Step 4.2: Add Equip Operations to CampaignState

**File**: `src/sim/campaign/CampaignState.cs`

- `EquipItem(crewId, itemId)` - validates category, slot, equips
- `UnequipItem(crewId, slot)` - clears slot

---

### Phase 5: Ship Operations (Priority: Medium)

#### Step 5.1: Add Ship Operations to CampaignState

**File**: `src/sim/campaign/CampaignState.cs`

- `RepairShip(partsToUse)` - uses parts, repairs hull
- `DamageShip(amount, reason)` - reduces hull
- `InstallModule(itemId)` - from inventory to ship
- `RemoveModule(moduleId)` - from ship to inventory

---

### Phase 6: Save Version Update

#### Step 6.1: Increment Save Version

**File**: `src/sim/data/SaveData.cs`

```csharp
public const int Current = 3;
// Version history:
// 1 - Initial save format (SF3)
// 2 - MG1: Expanded crew stats, traits
// 3 - MG2: Ship, inventory, equipment
```

---

## MG2 Deliverables Checklist

### Phase 1: Ship Foundation
- [ ] **1.1** Create `Ship` class with hull, modules
- [ ] **1.2** Create ship serialization
- [ ] **1.3** Add `Ship` to `CampaignState`

### Phase 2: Item & Inventory System
- [ ] **2.1** Create `Item` and `ItemDef` classes
- [ ] **2.2** Create `ItemRegistry` with default items
- [ ] **2.3** Create `Inventory` class with capacity tracking
- [ ] **2.4** Add inventory serialization

### Phase 3: Resource Operations
- [ ] **3.1** Add validated resource methods
- [ ] **3.2** Add `Inventory` to `CampaignState`
- [ ] **3.3** Add item and ship events

### Phase 4: Equipment System
- [ ] **4.1** Add equipment slots to `CrewMember`
- [ ] **4.2** Add `EquipItem` / `UnequipItem` operations

### Phase 5: Ship Operations
- [ ] **5.1** Add `RepairShip`, `DamageShip`, `InstallModule`, `RemoveModule`

### Phase 6: Save Version
- [ ] **6.1** Increment save version to 3

---

## Testing

### Test Files to Create

| File | Tests |
|------|-------|
| `tests/sim/management/MG2ShipTests.cs` | Ship creation, damage, repair, modules |
| `tests/sim/management/MG2InventoryTests.cs` | Add/remove items, capacity, stacking |
| `tests/sim/management/MG2ResourceTests.cs` | Spend/add resources, validation |
| `tests/sim/management/MG2EquipmentTests.cs` | Equip/unequip items |
| `tests/sim/management/MG2SerializationTests.cs` | Ship, inventory round-trip |

### Key Test Cases

#### Ship Tests
- `Ship_CreateStarter_HasCorrectDefaults`
- `Ship_GetCargoCapacity_IncludesModules`
- `Ship_TakeDamage_ReducesHull`
- `Ship_TakeDamage_ClampsToZero`
- `Ship_Repair_ClampsToMax`
- `Ship_InstallModule_RespectsSlotLimit`
- `Ship_RemoveModule_Works`
- `Ship_IsCritical_At25Percent`

#### Inventory Tests
- `Inventory_AddItem_Works`
- `Inventory_AddItem_RespectsCapacity`
- `Inventory_AddItem_StacksConsumables`
- `Inventory_GetUsedVolume_Correct`
- `Inventory_RemoveByDefId_Works`
- `Inventory_RemoveByDefId_FailsIfInsufficient`

#### Resource Tests
- `SpendCredits_Works`
- `SpendCredits_FailsIfInsufficient`
- `AddCredits_Works`
- `SpendFuel_Works`
- `CanAfford_ChecksMultipleResources`

#### Equipment Tests
- `EquipItem_Works`
- `EquipItem_FailsForNonEquipment`
- `EquipItem_UnequipsPreviousItem`
- `UnequipItem_Works`

#### Serialization Tests
- `Ship_RoundTrip_PreservesState`
- `Inventory_RoundTrip_PreservesItems`
- `Campaign_RoundTrip_PreservesShipAndInventory`

---

## Manual Test Setup

### Test Scenario: Basic Ship & Inventory Flow

1. **Start new campaign**
   - Verify ship exists with starter modules
   - Verify cargo capacity is 40 (base 20 + small cargo 20)

2. **Add items to inventory**
   - Add 3x medkit (volume 3)
   - Add 2x repair_kit (volume 4)
   - Verify used cargo is 7

3. **Test capacity limit**
   - Try to add cargo that exceeds capacity
   - Verify it fails

4. **Damage and repair ship**
   - Damage ship by 20
   - Verify hull is 30
   - Repair with 10 parts
   - Verify hull is 50 (clamped to max)

5. **Equip crew**
   - Add rifle to inventory
   - Equip rifle to crew member
   - Verify crew has weapon equipped

6. **Save and load**
   - Save campaign
   - Load campaign
   - Verify ship, inventory, equipment preserved

### DevTools Commands (Suggested)

```
/ship status          - Show ship hull, modules, cargo
/ship damage <amount> - Damage ship
/ship repair <parts>  - Repair ship
/inv add <defId> [qty] - Add item to inventory
/inv remove <defId> [qty] - Remove item
/inv list             - List inventory
/equip <crewId> <itemId> - Equip item to crew
```

---

## Files Summary

### New Files
| File | Description |
|------|-------------|
| `src/sim/campaign/Ship.cs` | Ship class with hull, modules (rename/expand `ShipState.cs`) |
| `src/sim/campaign/Item.cs` | ItemDef, Item, ItemCategory |
| `src/sim/campaign/ItemRegistry.cs` | Default item definitions |
| `src/sim/campaign/Inventory.cs` | Inventory management |
| `tests/sim/management/MG2ShipTests.cs` | Ship unit tests |
| `tests/sim/management/MG2InventoryTests.cs` | Inventory unit tests |
| `tests/sim/management/MG2ResourceTests.cs` | Resource operation tests |
| `tests/sim/management/MG2EquipmentTests.cs` | Equipment tests |
| `tests/sim/management/MG2SerializationTests.cs` | Serialization tests |

### Modified Files
| File | Changes |
|------|---------|
| `src/sim/campaign/CampaignState.cs` | Ship, Inventory, resource methods |
| `src/sim/campaign/CrewMember.cs` | Equipment slots |
| `src/sim/data/SaveData.cs` | ShipData, InventoryData, version 3 |
| `src/sim/Events.cs` | Item and ship events |

---

## Implementation Order

Recommended order for implementation:

1. **Ship.cs** - Core ship class
2. **Item.cs** - Item definitions
3. **ItemRegistry.cs** - Default items
4. **Inventory.cs** - Inventory management
5. **SaveData.cs** - Add serialization classes
6. **Events.cs** - Add new events
7. **CampaignState.cs** - Integrate ship, inventory, resource methods
8. **CrewMember.cs** - Add equipment slots
9. **Tests** - All test files
10. **Save version** - Increment to 3

---

## Dependencies on Other Milestones

- **MG3 (Tactical Integration)**: Will use equipment from inventory for mission input
- **WD1 (Single Hub World)**: Station facilities may sell items, repair ship
- **GN1 (Contract Generation)**: Rewards may include items

---

## Open Questions

1. **Fuel capacity**: Should ship have max fuel based on tank module?
2. **Item durability**: Should equipment degrade over time?
3. **Module damage**: Can modules be damaged in combat?
4. **Crew loadout UI**: How to present equipment management?

These can be deferred to later milestones if needed.
