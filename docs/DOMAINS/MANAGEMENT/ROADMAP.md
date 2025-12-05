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

## MG3 – Tactical Integration (G1)

**Goal:**  
Connect Management to Tactical via the mission I/O contract.

**Key capabilities:**

- `CreateMissionInput(contract)`:
  - Snapshot crew for tactical.
  - Include equipment and stats.
- `ApplyMissionResult(result)`:
  - Apply injuries and deaths.
  - Apply XP gains.
  - Apply loot and rewards.
  - Update contract state.

**Deliverables:**
- Mission I/O adapter methods.
- Integration tests with Tactical.

---

## MG4 – Encounter Integration (G2)

**Goal:**  
Apply encounter outcomes to player state.

**Key capabilities:**

- `ApplyEncounterOutcome(outcome)`:
  - Resource deltas (credits, fuel, cargo).
  - Crew injuries or trait changes.
  - Ship damage.
  - Time costs.

**Deliverables:**
- Encounter outcome adapter.
- Integration tests with Encounter domain.

---

## G0/G1 Scope Summary

| Milestone | Phase | Notes |
|-----------|-------|-------|
| MG0 | G0 | Concept only, mostly done |
| MG1 | G1 | Core implementation |
| MG2 | G1 | Core implementation |
| MG3 | G1 | Tactical integration |
| MG4 | G2 | Encounter integration |
