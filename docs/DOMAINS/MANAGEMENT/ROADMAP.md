# Management Domain Roadmap

This document defines the **implementation order** for the Management domain.

- G0 is concept/design only. Implementation starts in G1.
- Each milestone is a **vertical slice**.

---

## Overview of Milestones

1. **MG0 – Concept Finalization (G0)**
2. **MG1 – PlayerState & Crew Core (G1)**
3. **MG2 – Ship & Resources (G1)**
4. **MG3 – Tactical Integration (G1)**
5. **MG4 – Encounter Integration (G2)**

---

## MG0 – Concept Finalization (G0)

**Goal:**  
Finalize design decisions for crew, ship, resources, and inventory before implementation.

**Key deliverables:**

- Review and finalize CAMPAIGN_FOUNDATIONS.md sections:
  - Section 1 (Resources): Confirm credits, fuel, cargo model.
  - Section 3 (Crew): Confirm stats, traits, injury model.
- Define `PlayerState` structure (doc only):
  - What fields exist.
  - What operations are supported.
- Define crew stat ranges and starting values.
- Define ship chassis types and modules (initial set).

**Why first:**  
Avoid rework by locking down the data model before coding.

**Status:** ✅ Mostly complete (CAMPAIGN_FOUNDATIONS covers this)

---

## MG1 – PlayerState & Crew Core (G1)

**Goal:**  
Implement the core player state with crew management.

**Key capabilities:**

- `PlayerState` class:
  - Owns list of `CrewMember`.
  - Owns `Ship`.
  - Owns resource totals (credits, fuel, supplies).
- `CrewMember` class:
  - Stats: Grit, Reflexes, Aim, Tech, Savvy, Resolve.
  - Traits: List of trait IDs.
  - Status: Active, Injured, Dead.
  - Level and XP.
- Crew operations:
  - `HireCrew(CrewMember)`.
  - `FireCrew(crewId)`.
  - `ApplyInjury(crewId, injury)`.
  - `ApplyXP(crewId, amount)`.

**Deliverables:**
- `PlayerState`, `CrewMember` classes.
- Unit tests for crew operations.
- Test data for starting crew.

**Status:** ✅ Complete (see `MG1_IMPLEMENTATION.md`)

---

## MG2 – Ship & Resources (G1)

**Goal:**  
Implement ship state and resource management.

**Implementation:** See `MG2_IMPLEMENTATION.md` for detailed breakdown.

**Key capabilities:**

- `Ship` class:
  - Hull integrity.
  - Modules (weapons, engines, cargo, etc.).
  - Cargo capacity.
- Resource operations:
  - `SpendCredits(amount)`.
  - `AddCredits(amount)`.
  - `SpendFuel(amount)`.
  - `AddFuel(amount)`.
- Inventory:
  - `AddItem(item)`.
  - `RemoveItem(itemId)`.
  - `EquipItem(crewId, itemId)`.
  - Capacity enforcement.

**Deliverables:**
- `Ship`, `Inventory` classes.
- Resource validation (can't go negative).
- Unit tests for resource operations.

---

## MG3 – Tactical Integration (G1) ✅

**Goal:**  
Connect Management to Tactical via the mission I/O contract.

**Implementation:** See `MG3_IMPLEMENTATION.md` for detailed breakdown.

**Status:** Complete

**Key capabilities:**

- `MissionInputBuilder.Build(campaign, job)`:
  - Snapshot crew for tactical with full stats.
  - Include equipment and derived stats.
  - Build mission context from world state.
- `CampaignState.ApplyMissionOutput(output)`:
  - Apply injuries and deaths.
  - Apply XP gains.
  - Apply loot and rewards.
  - Track ammo consumption.
  - Update contract state.

**Deliverables:**
- `MissionInputBuilder` class.
- Enhanced `ApplyMissionOutput` method.
- Mission integration events.
- Integration tests with Tactical.

---

## MG4 – Encounter Integration (G2)

**Goal:**  
Apply encounter outcomes to player state, completing the loop between the Encounter domain and campaign state management.

**Implementation:** See `MG4_IMPLEMENTATION.md` for detailed breakdown.

**Depends on:** MG3 ✅, EN1 ✅, EN2 ✅

**Status:** ✅ Complete

**Key capabilities:**

- `ApplyEncounterOutcome(instance)`:
  - Process all accumulated `EncounterEffect` from `EncounterInstance.PendingEffects`
  - Resource deltas (credits, fuel, parts, meds, ammo)
  - Crew injuries, XP, and trait changes
  - Ship damage
  - Time advancement
  - Faction reputation changes
  - Campaign flags for encounter state tracking
  - Cargo add/remove
- Travel resource consumption:
  - `ConsumeTravelFuel(amount)` for travel segments
  - `CanAffordTravel(plan)` validation
- Campaign flags system:
  - `SetFlag(flagId, value)` for encounter state tracking
  - `GetFlag(flagId)` / `HasFlag(flagId)` for condition evaluation
- Unified outcome application:
  - Same patterns as `ApplyMissionOutput` from MG3
  - Consistent event emission for all state changes

### Implementation Phases

| Phase | Description | Priority |
|-------|-------------|----------|
| **Phase 1** | Campaign Flags System | High |
| **Phase 2** | Core Effect Application | Critical |
| **Phase 3** | Travel Integration | High |
| **Phase 4** | Serialization Updates | Medium |

### What Already Exists (Reused)

These methods already exist in `CampaignState` and will be called by effect handlers:
- `SpendResource` / `AddResource` – resource operations with events
- `ModifyFactionRep` – faction reputation with events
- `AssignTrait` / `RemoveTrait` – crew trait management with events
- `DamageShip` – ship hull damage with events
- `AddItem` / `RemoveItemByDef` – inventory operations
- `Time.AdvanceDays` – time advancement

### What Needs to Be Added

| Component | Location | Notes |
|-----------|----------|-------|
| `Flags` dictionary | `CampaignState` | Campaign flag storage |
| `SetFlag` / `GetFlag` / `HasFlag` | `CampaignState` | Flag operations |
| `ApplyEncounterOutcome` | `CampaignState` | Main entry point |
| `ApplyEncounterEffect` | `CampaignState` | Per-effect dispatcher |
| `ConsumeTravelFuel` | `CampaignState` | Travel fuel consumption |
| `CanAffordTravel` | `CampaignState` | Travel validation |
| `CampaignFlagChangedEvent` | `Events.cs` | Flag change event |
| `EncounterOutcomeAppliedEvent` | `Events.cs` | Outcome summary event |

**Deliverables:**
- `ApplyEncounterOutcome` method
- Effect handlers for all 13 `EffectType` values
- Campaign flags system
- Travel fuel consumption methods
- Events for all state changes
- ~70 unit tests across 3 test files
- Integration tests with Encounter domain

**Files to modify:**
| File | Changes |
|------|---------|
| `src/sim/campaign/CampaignState.cs` | Add flags, effect application, travel methods |
| `src/sim/Events.cs` | Add `CampaignFlagChangedEvent`, `EncounterOutcomeAppliedEvent` |
| `src/sim/data/SaveData.cs` | Add `Flags` to `CampaignStateData` |
| `tests/sim/management/MG4FlagTests.cs` | Flag system tests |
| `tests/sim/management/MG4EffectTests.cs` | Effect application tests |
| `tests/sim/management/MG4IntegrationTests.cs` | Integration tests |

---

## G0/G1/G2 Scope Summary

| Milestone | Phase | Status | Notes |
|-----------|-------|--------|-------|
| MG0 | G0 | ✅ Complete | Concept only |
| MG1 | G1 | ✅ Complete | PlayerState & Crew |
| MG2 | G1 | ✅ Complete | Ship & Resources |
| MG3 | G1 | ✅ Complete | Tactical integration |
| MG4 | G2 | ✅ Complete | Encounter integration |

---

## Dependencies

| Milestone | Depends On |
|-----------|------------|
| MG1 | MG0 |
| MG2 | MG1 |
| MG3 | MG2, Tactical M7 |
| MG4 | MG3, EN1, EN2 |

---

## Success Criteria

### MG4 ✅
- [x] All 13 encounter effect types apply correctly to campaign state
- [x] Campaign flags system works for encounter state tracking
- [x] Travel fuel consumption integrated (`ConsumeTravelFuel`, `CanAffordTravel`)
- [x] Crew targeting uses skill check participant when available
- [x] All effects emit appropriate events
- [x] Active encounter state serializes/deserializes correctly
- [x] Unit tests pass (97 tests across 3 files)
- [x] Integration tests with Encounter domain pass

---

## Backlog (G2.5 – Playtest & Polish)

### MG-UI1 – Crew roster & detail screen (G2.5)

**Goal:** Make the roster understandable at a glance.

**Status:** ⬜ Pending

- Campaign screen: show crew list with name, role, 2–3 key stats, status icon.
- Selecting a crew member shows:
  - Full stats (core attributes, level/XP).
  - Traits (names + short tags).
  - Injuries (name + simple effect summary).
- Read-only: no equipment management yet, no new sim logic.

**Implementation:** See `MG-UI1_IMPLEMENTATION.md`

---

### MG-UI2 – Fire / dismiss crew (G2.5)

**Goal:** Let the player prune the roster.

**Status:** ⬜ Pending

**Implementation:** See `MG-UI2_IMPLEMENTATION.md`

- From crew detail, add "Fire" / "Dismiss" button.
- Confirm dialog: summary of what will be lost (skills / role).
- Call existing management operation to remove crew from campaign state.
- Handle corner cases: cannot fire last operational crew member, etc.

---

### MG-SYS1 – Minimal equipment slots (G2.5)

**Goal:** Have equipment exist and matter, even if simple.

**Status:** ⬜ Pending

**Implementation:** See `MG-SYS1_IMPLEMENTATION.md`

- Each crew member has a small number of slots (e.g. main weapon, armor).
- Equipment is purely stat modifiers at this stage (no new tactical rules):
  - e.g. +Aim, +HP, +Tech.
- Management can query effective stats (base + equipment).

---

### MG-UI3 – Equip/unequip from a simple list (G2.5)

**Goal:** A basic, usable equipment UX.

**Status:** ⬜ Pending

**Implementation:** See `MG-UI3_IMPLEMENTATION.md`

**Depends on:** MG-SYS1

- From crew detail, show current equipment slots.
- Allow equipping/unequipping from global inventory list:
  - No drag-and-drop, no filters initially; keep it minimal.
- Persist changes to campaign state.

---

### MG-SHOP1 – Station shop integration (G2.5)

**Goal:** Connect station shops to real inventory.

**Status:** ⬜ Pending

- Buying an item at a station:
  - Deducts credits.
  - Adds item to campaign inventory.
- Selling an item:
  - Adds credits.
  - Removes item.
- No dynamic pricing or simulation influence yet; use static config.

---

### Future Backlog Items

| Item | Priority | Notes |
|------|----------|-------|
| Crew recruitment UI | Medium | Hire from station, see candidates |
| Ship status panel | Medium | Hull, modules, cargo capacity |
| Crew leveling UI | Medium | Spend XP, choose upgrades |
| More crew traits | Medium | Currently limited set |
| Starting crew variety | Low | Different starting loadouts |
