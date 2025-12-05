# MG0 – Concept Finalization: Implementation Plan

**Status**: 🔄 In Progress

This document breaks down **Milestone MG0** from `ROADMAP.md` into concrete design decisions and finalization steps.

**Goal**: Finalize design decisions for crew, ship, resources, and inventory before implementation, ensuring alignment with CAMPAIGN_FOUNDATIONS.md and existing codebase.

---

## Current State Assessment

### What We Have (Existing Code)

| Component | Status | Location | Notes |
|-----------|--------|----------|-------|
| `CampaignState` | ✅ Exists | `src/sim/campaign/CampaignState.cs` | Owns resources, crew, jobs, location |
| `CrewMember` | ✅ Exists | `src/sim/campaign/CrewMember.cs` | Basic stats, injuries, XP, level |
| `ShipState` | ⚠️ Stub | `src/sim/campaign/ShipState.cs` | Only HP/Fuel, not integrated |
| Resources | ✅ Exists | `CampaignState` | Money, Fuel, Parts, Meds, Ammo |
| `ResourceTypes` | ✅ Exists | `src/sim/Events.cs` | Constants for event publishing |
| `TravelSystem` | ✅ Exists | `src/sim/campaign/TravelSystem.cs` | Fuel/time costs |
| `JobSystem` | ✅ Exists | `src/sim/campaign/JobSystem.cs` | Job generation |
| Save/Load | ✅ Exists | `src/sim/data/SaveData.cs` | Full serialization |

### Gap Analysis: CAMPAIGN_FOUNDATIONS vs Implementation

| CAMPAIGN_FOUNDATIONS Section | Current Status | Gap |
|------------------------------|----------------|-----|
| **§1 Resources** | ⚠️ Partial | Cargo/inventory not implemented; resources exist but no capacity limits |
| **§3.1 Crew Stats** | ⚠️ Partial | Only Aim, Toughness, Reflexes; missing Grit, Tech, Savvy, Resolve |
| **§3.2 Traits** | ❌ Missing | No trait system; only injuries exist |
| **§3.3 Crew Model** | ⚠️ Partial | Role enum exists but not tied to stats/abilities |
| **§7 Mission I/O** | ✅ Complete | `MissionOutput`, `CrewOutcome` implemented |

---

## MG0 Deliverables Checklist

### Phase 1: Resource Model Finalization

- [ ] **1.1** Confirm resource vocabulary
- [ ] **1.2** Define cargo/inventory model (design only)
- [ ] **1.3** Define resource starting values and ranges
- [ ] **1.4** Document resource flow (sources and sinks)

### Phase 2: Crew Model Finalization

- [ ] **2.1** Finalize primary stats (align with CAMPAIGN_FOUNDATIONS §3.1)
- [ ] **2.2** Define stat ranges and starting values
- [ ] **2.3** Design trait system (structure, not full content)
- [ ] **2.4** Finalize injury model
- [ ] **2.5** Define crew progression (XP, leveling, stat growth)

### Phase 3: Ship Model Finalization

- [ ] **3.1** Define ship chassis concept
- [ ] **3.2** Define module system (design only)
- [ ] **3.3** Define cargo capacity model

### Phase 4: PlayerState Structure

- [ ] **4.1** Document `PlayerState` structure (what fields exist)
- [ ] **4.2** Document supported operations
- [ ] **4.3** Identify integration points with other domains

---

## Phase 1: Resource Model Finalization

### 1.1 Resource Vocabulary

**Decision**: Keep the current resource set with minor clarifications.

| Resource | Purpose | Current | Recommendation |
|----------|---------|---------|----------------|
| `Money` | Universal currency | ✅ Exists | Keep as-is, rename to `Credits` for flavor |
| `Fuel` | Travel gate | ✅ Exists | Keep as-is |
| `Ammo` | Mission gate | ✅ Exists | Keep as-is (abstracted, not per-weapon) |
| `Parts` | Repairs/upgrades | ✅ Exists | Keep as-is |
| `Meds` | Healing injuries | ✅ Exists | Keep as-is |

**Rationale**: Per CAMPAIGN_FOUNDATIONS §1.1:
- Ammo is "effectively infinite" in tactical; strategic ammo is a mission cost abstraction
- No inventory Tetris; cargo is volume-based for trade goods only
- No per-day salary; costs are event-driven

**Action Items**:
- [ ] Consider renaming `Money` → `Credits` for thematic consistency
- [ ] Add `ResourceTypes.Credits` constant if renamed
- [ ] Update UI labels to use "Credits" terminology

---

### 1.2 Cargo/Inventory Model (Design Only)

**Decision**: Defer full cargo implementation to MG2, but establish the design now.

**Design Principles** (per CAMPAIGN_FOUNDATIONS §1.1):
- **No inventory Tetris**: Items don't have spatial dimensions
- **Volume/Mass for trade goods only**: Bulk cargo has weight, personal items don't
- **Cargo tags drive gameplay**: `illegal`, `perishable`, `fragile`, `volatile`, `medical`, `luxury`, `restricted_faction_X`

**Proposed Structure** (for MG2):

```
Inventory
├── Equipment (no capacity limit)
│   ├── Weapons (equipped to crew or stored)
│   ├── Armor (equipped to crew or stored)
│   └── Gadgets (equipped to crew or stored)
└── Cargo (capacity-limited)
    ├── Trade Goods (volume, tags)
    └── Special Items (quest items, etc.)
```

**Key Design Decisions**:

| Aspect | Decision | Rationale |
|--------|----------|-----------|
| Equipment capacity | Unlimited | Per §1.1: "Small items are abstracted" |
| Cargo capacity | Ship-based | Upgradeable, creates progression |
| Item stacking | Trade goods stack | Reduces inventory noise |
| Cargo tags | 5-8 core tags | Enough for variety, not overwhelming |

**Initial Cargo Tags**:
- `illegal` – Contraband, inspection risk
- `perishable` – Time-sensitive delivery
- `fragile` – Combat damage risk
- `volatile` – Explosion risk if damaged
- `medical` – Legal but regulated
- `luxury` – High value, pirate target
- `restricted` – Faction-specific legality

**MG0 Action**: Document this design; implementation in MG2.

---

### 1.3 Resource Starting Values and Ranges

**Current Starting Values** (from `CampaignState.CreateNew()`):

| Resource | Starting | Notes |
|----------|----------|-------|
| Money | 200 | Enough for 1-2 small purchases |
| Fuel | 100 | ~5-10 travels depending on distance |
| Parts | 50 | Ship repair buffer |
| Meds | 5 | Heal 5 injuries |
| Ammo | 50 | 5 missions worth |

**Proposed Ranges** (soft caps, not hard limits):

| Resource | Typical Range | Notes |
|----------|---------------|-------|
| Money | 0 – 10,000+ | No hard cap; economy scales |
| Fuel | 0 – 200 | Ship tank capacity (upgradeable) |
| Parts | 0 – 100 | Reasonable stockpile |
| Meds | 0 – 20 | Rare, valuable |
| Ammo | 0 – 100 | Replenished at stations |

**Action Items**:
- [ ] Verify starting values feel right in playtesting
- [ ] Consider fuel tank as ship upgrade (MG2)
- [ ] Document resource economy balance targets

---

### 1.4 Resource Flow (Sources and Sinks)

**Sources** (how resources are gained):

| Resource | Sources |
|----------|---------|
| Money | Job rewards, selling cargo, encounters |
| Fuel | Station purchase, job rewards, salvage |
| Parts | Job rewards, salvage, station purchase |
| Meds | Station purchase, job rewards, encounters |
| Ammo | Station purchase, job rewards, salvage |

**Sinks** (how resources are consumed):

| Resource | Sinks |
|----------|-------|
| Money | Station purchases, repairs, hiring crew, bribes |
| Fuel | Travel, mission deployment |
| Parts | Ship repairs, upgrades (future) |
| Meds | Healing crew injuries |
| Ammo | Mission deployment |

**Current Implementation Status**:
- ✅ Mission costs: `MISSION_AMMO_COST = 10`, `MISSION_FUEL_COST = 5`
- ✅ Travel costs: `TravelSystem.CalculateFuelCost()`
- ✅ Job rewards: `JobReward` struct with Money, Parts, Fuel, Ammo
- ❌ Station purchases: Not implemented (MG2)
- ❌ Ship repairs: Not implemented (MG2)

---

## Phase 2: Crew Model Finalization

### 2.1 Primary Stats Alignment

**CAMPAIGN_FOUNDATIONS §3.1 defines**:
- **Grit** – physical robustness, HP, resistance to injury
- **Reflexes** – initiative, dodge, reaction checks
- **Aim** – accuracy with ranged weapons
- **Tech** – hacking, repairs, interacting with machinery
- **Savvy** – social checks, negotiation, reading people
- **Resolve** – stress tolerance, panic resistance, morale checks

**Current Implementation** (`CrewMember.cs`):
- `Aim` – ✅ Matches
- `Toughness` – ⚠️ Similar to Grit but named differently
- `Reflexes` – ✅ Matches

**Gap**: Missing Tech, Savvy, Resolve; Toughness should be renamed to Grit.

**Decision**: Align with CAMPAIGN_FOUNDATIONS naming.

| CAMPAIGN_FOUNDATIONS | Current | Action |
|----------------------|---------|--------|
| Grit | Toughness | Rename |
| Reflexes | Reflexes | Keep |
| Aim | Aim | Keep |
| Tech | — | Add |
| Savvy | — | Add |
| Resolve | — | Add |

**Derived Stats** (computed from primaries):

| Derived | Formula | Used By |
|---------|---------|---------|
| `MaxHp` | 100 + (Grit × 10) | Tactical |
| `Initiative` | Reflexes × 5 | Tactical (future) |
| `HitBonus` | Aim × 2% | Tactical |
| `HackBonus` | Tech × 10% | Tactical (interactables) |
| `TalkBonus` | Savvy × 10% | Encounters |
| `StressThreshold` | 50 + (Resolve × 10) | Tactical (future) |

**MG1 Implementation**: Add missing stats to `CrewMember`, update tactical integration.

---

### 2.2 Stat Ranges and Starting Values

**Proposed Stat Ranges**:

| Stat | Min | Max | Starting Range | Notes |
|------|-----|-----|----------------|-------|
| Grit | 0 | 10 | 1-3 | Affects HP significantly |
| Reflexes | 0 | 10 | 1-3 | Affects dodge/initiative |
| Aim | 0 | 10 | 1-3 | Affects hit chance |
| Tech | 0 | 10 | 0-2 | Specialists only |
| Savvy | 0 | 10 | 0-2 | Specialists only |
| Resolve | 0 | 10 | 1-3 | Affects stress (future) |

**Role-Based Starting Stats**:

| Role | Grit | Reflexes | Aim | Tech | Savvy | Resolve |
|------|------|----------|-----|------|-------|---------|
| Soldier | 3 | 2 | 3 | 0 | 0 | 2 |
| Medic | 2 | 1 | 1 | 2 | 1 | 3 |
| Tech | 1 | 2 | 1 | 3 | 1 | 2 |
| Scout | 2 | 3 | 2 | 1 | 1 | 1 |

**Stat Growth** (per level):
- +1 to one stat (player choice or random)
- Max stat cap: 10
- Typical campaign length: 10-20 levels

---

### 2.3 Trait System Design

**CAMPAIGN_FOUNDATIONS §3.2 defines**:
- Traits are **binary flags** with mechanical impact
- Traits **can change** through events, missions, injuries
- Separate **personality traits** vs **injury traits**

**Proposed Trait Categories**:

| Category | Examples | Mechanical Impact |
|----------|----------|-------------------|
| **Background** | Ex-Military, Smuggler, Corporate | Unlock dialogue, affect job availability |
| **Personality** | Brave, Cautious, Reckless, Merciful | Affect encounter options, morale |
| **Acquired** | Vengeful, Traumatized, Hardened | Gained through events |
| **Injury** | Damaged Eye, Shattered Knee | Stat penalties, permanent |

**Trait Structure** (for MG1):

```csharp
public class Trait
{
    public string Id { get; set; }           // "ex_military"
    public string Name { get; set; }         // "Ex-Military"
    public TraitCategory Category { get; set; }
    public List<StatModifier> Modifiers { get; set; }  // Optional stat effects
    public List<string> Tags { get; set; }   // For filtering/matching
}

public enum TraitCategory
{
    Background,
    Personality,
    Acquired,
    Injury
}
```

**Initial Trait Set** (for MG1, ~10-15 traits):

| Trait ID | Name | Category | Effect |
|----------|------|----------|--------|
| `ex_military` | Ex-Military | Background | +1 Aim, unlocks military dialogue |
| `smuggler` | Smuggler | Background | Unlocks smuggling jobs |
| `corporate` | Corporate | Background | +1 Savvy, faction-specific |
| `brave` | Brave | Personality | +10% morale in combat |
| `cautious` | Cautious | Personality | +5% dodge, -5% initiative |
| `reckless` | Reckless | Personality | +10% damage, -10% defense |
| `merciful` | Merciful | Personality | Unlocks non-lethal options |
| `vengeful` | Vengeful | Acquired | +20% damage vs faction that wronged |
| `damaged_eye` | Damaged Eye | Injury | -2 Aim |
| `shattered_knee` | Shattered Knee | Injury | -2 Reflexes, movement penalty |

**MG0 Action**: Document trait structure; implementation in MG1.

---

### 2.4 Injury Model Finalization

**Current Implementation**:
- Injuries stored as `List<string>` on `CrewMember`
- Standard types: `wounded`, `critical`, `concussed`, `bleeding`
- `critical` prevents deployment
- Healed via `Meds` or `Rest()`

**Proposed Injury Categories**:

| Category | Duration | Effect | Healing |
|----------|----------|--------|---------|
| **Minor** | 1 rest | Small stat penalty | Rest or Meds |
| **Moderate** | 2-3 rests | Moderate penalty | Meds required |
| **Severe** | Permanent | Major penalty | Cannot heal (becomes trait) |

**Injury Structure** (for MG1):

```csharp
public class Injury
{
    public string Id { get; set; }           // "wounded_arm"
    public string Name { get; set; }         // "Wounded Arm"
    public InjurySeverity Severity { get; set; }
    public List<StatModifier> Modifiers { get; set; }
    public int HealingRequired { get; set; } // Rests or Meds needed
    public bool PreventsDeployment { get; set; }
}

public enum InjurySeverity
{
    Minor,      // Heals with rest
    Moderate,   // Needs meds
    Severe      // Permanent (becomes injury trait)
}
```

**MG0 Decision**: Keep current string-based injuries for MG1; upgrade to structured injuries in MG2+.

---

### 2.5 Crew Progression

**Current Implementation**:
- `XP_PER_LEVEL = 100`
- XP sources: kills, participation, victory bonus
- Level affects: nothing directly (stats are separate)

**Proposed Progression**:

| Level | Total XP | Stat Points | Notes |
|-------|----------|-------------|-------|
| 1 | 0 | 0 | Starting |
| 2 | 100 | 1 | First upgrade |
| 3 | 250 | 2 | |
| 4 | 450 | 3 | |
| 5 | 700 | 4 | Veteran |
| 10 | 2500 | 9 | Elite |

**XP Sources** (per CAMPAIGN_FOUNDATIONS):

| Source | XP | Notes |
|--------|-----|-------|
| Kill | 25 | Per enemy killed |
| Participation | 10 | Just being in mission |
| Victory | 20 | Mission success |
| Retreat | 5 | Survived retreat |
| Objective | 15 | Per objective completed |

**MG0 Decision**: Current XP system is adequate; add stat point allocation in MG1.

---

## Phase 3: Ship Model Finalization

### 3.1 Ship Chassis Concept

**Current Implementation** (`ShipState.cs`):
- Only `Hp`, `MaxHp`, `Fuel`, `MaxFuel`
- Not integrated with `CampaignState`

**Proposed Ship Model**:

```csharp
public class Ship
{
    public string ChassisId { get; set; }    // "corvette", "freighter"
    public string Name { get; set; }         // Player-named
    
    // Hull
    public int Hull { get; set; }
    public int MaxHull { get; set; }
    
    // Capacity
    public int FuelCapacity { get; set; }
    public int CargoCapacity { get; set; }
    public int CrewCapacity { get; set; }
    
    // Modules (future)
    public List<ShipModule> Modules { get; set; }
}
```

**Initial Chassis Types** (for MG2):

| Chassis | Hull | Fuel | Cargo | Crew | Notes |
|---------|------|------|-------|------|-------|
| Scout | 80 | 80 | 20 | 4 | Fast, fragile |
| Corvette | 100 | 100 | 40 | 6 | Balanced starter |
| Freighter | 120 | 120 | 80 | 4 | Cargo-focused |
| Gunship | 150 | 80 | 20 | 6 | Combat-focused |

**MG0 Decision**: Document chassis concept; implementation in MG2.

---

### 3.2 Module System (Design Only)

**Proposed Module Categories**:

| Category | Examples | Effect |
|----------|----------|--------|
| **Engines** | Standard, Fast, Efficient | Speed, fuel efficiency |
| **Weapons** | Point Defense, Turrets | Ship combat (future) |
| **Cargo** | Expanded Hold, Refrigerated | Capacity, cargo types |
| **Utility** | Medbay, Workshop, Scanner | Crew healing, repairs, intel |

**Module Slots** (per chassis):
- Scout: 2 slots
- Corvette: 3 slots
- Freighter: 4 slots (2 must be cargo)
- Gunship: 4 slots (2 must be weapons)

**MG0 Decision**: Document module concept; implementation in MG3+.

---

### 3.3 Cargo Capacity Model

**Design** (per §1.2):
- Cargo capacity is ship-based
- Trade goods consume capacity
- Equipment does not consume capacity

**Capacity Units**:
- 1 unit = 1 "cargo slot"
- Trade goods: 1-10 units each
- Ship capacity: 20-100 units depending on chassis

**MG0 Decision**: Document capacity model; implementation in MG2.

---

## Phase 4: PlayerState Structure

### 4.1 PlayerState Fields

**Current**: `CampaignState` serves as the de facto PlayerState.

**Proposed Structure** (clarification, not new class):

```
CampaignState (PlayerState)
├── Time: CampaignTime
├── Rng: RngService
├── EventBus: EventBus
│
├── Resources
│   ├── Money: int
│   ├── Fuel: int
│   ├── Parts: int
│   ├── Meds: int
│   └── Ammo: int
│
├── Location
│   ├── Sector: Sector
│   └── CurrentNodeId: int
│
├── Crew: List<CrewMember>
│   └── (each has stats, traits, injuries, equipment)
│
├── Ship: Ship (MG2)
│   ├── Chassis
│   ├── Hull
│   ├── Modules
│   └── Cargo
│
├── Jobs
│   ├── AvailableJobs: List<Job>
│   └── CurrentJob: Job
│
├── Reputation
│   └── FactionRep: Dictionary<string, int>
│
└── Statistics
    ├── MissionsCompleted: int
    ├── MissionsFailed: int
    ├── TotalMoneyEarned: int
    └── TotalCrewDeaths: int
```

**MG0 Decision**: Current `CampaignState` structure is correct; add Ship in MG2.

---

### 4.2 Supported Operations

**Current Operations** (in `CampaignState`):

| Category | Operations | Status |
|----------|------------|--------|
| **Crew** | `AddCrew`, `GetAliveCrew`, `GetDeployableCrew`, `GetCrewById` | ✅ |
| **Jobs** | `AcceptJob`, `ClearCurrentJob`, `IsAtJobTarget`, `RefreshJobsAtCurrentNode` | ✅ |
| **Resources** | `ConsumeMissionResources`, `ApplyJobReward`, `HealCrewMember` | ✅ |
| **Faction** | `GetFactionRep`, `ModifyFactionRep` | ✅ |
| **Time** | `Rest` (via `CampaignTime.AdvanceDays`) | ✅ |
| **Mission** | `CanStartMission`, `GetMissionBlockReason`, `ApplyMissionOutput` | ✅ |
| **Save/Load** | `GetState`, `FromState` | ✅ |

**Operations to Add** (MG1-MG2):

| Category | Operations | Milestone |
|----------|------------|-----------|
| **Crew** | `HireCrew`, `FireCrew`, `AssignTrait`, `RemoveTrait` | MG1 |
| **Ship** | `RepairShip`, `InstallModule`, `RemoveModule` | MG2 |
| **Cargo** | `AddCargo`, `RemoveCargo`, `GetCargoSpace` | MG2 |
| **Equipment** | `EquipItem`, `UnequipItem`, `TransferItem` | MG2 |

---

### 4.3 Integration Points

**Domain Integration Map**:

| Domain | Reads From PlayerState | Writes To PlayerState |
|--------|------------------------|----------------------|
| **Tactical** | Crew stats, equipment | — (via MissionOutput) |
| **Generation** | Crew, resources, location | Jobs |
| **Travel** | Fuel, location | Fuel, location, time |
| **Encounter** | Crew traits, resources | Resources, crew status |
| **World** | Location | — |
| **Simulation** | Faction rep, resources | Faction rep (indirect) |

**Event Bus Integration** (existing):
- `ResourceChangedEvent` – Published on resource changes
- `JobAcceptedEvent` – Published on job acceptance
- `FactionRepChangedEvent` – Published on rep changes
- `DayAdvancedEvent` – Published on time advancement
- `TravelCompletedEvent` – Published on travel

---

## Implementation Order

### MG0 Completion Checklist

1. **Phase 1: Resources** ✅ Design complete
   - [x] Confirm resource vocabulary
   - [x] Define cargo model (design)
   - [x] Define starting values
   - [x] Document resource flow

2. **Phase 2: Crew** ✅ Design complete
   - [x] Finalize primary stats
   - [x] Define stat ranges
   - [x] Design trait system
   - [x] Finalize injury model
   - [x] Define progression

3. **Phase 3: Ship** ✅ Design complete
   - [x] Define chassis concept
   - [x] Design module system
   - [x] Define cargo capacity

4. **Phase 4: PlayerState** ✅ Design complete
   - [x] Document structure
   - [x] Document operations
   - [x] Identify integration points

---

## MG1 Implementation Preview

Based on MG0 decisions, MG1 will implement:

1. **Crew Stats Expansion**
   - Add `Grit`, `Tech`, `Savvy`, `Resolve` to `CrewMember`
   - Rename `Toughness` → `Grit`
   - Update tactical integration

2. **Trait System Foundation**
   - Add `Trait` class
   - Add `Traits` list to `CrewMember`
   - Implement initial 10-15 traits
   - Add trait-based modifiers

3. **Crew Operations**
   - `HireCrew(CrewMember)` – Add crew with cost
   - `FireCrew(crewId)` – Remove crew
   - `ApplyInjury(crewId, injury)` – Structured injuries
   - `ApplyXP(crewId, amount)` – With stat point allocation

4. **Unit Tests**
   - Crew stat calculations
   - Trait application
   - Injury effects
   - XP and leveling

---

## Open Questions for Review

1. **Stat Naming**: Should we rename `Money` → `Credits` for thematic consistency?
   - *Recommendation*: Yes, update in MG1

2. **Trait Complexity**: How many traits should a starting crew member have?
   - *Recommendation*: 1-2 traits (1 background, 0-1 personality)

3. **Injury Permanence**: When do injuries become permanent traits?
   - *Recommendation*: Only "Severe" injuries from specific events

4. **Ship Integration**: Should `Ship` be a separate class or embedded in `CampaignState`?
   - *Recommendation*: Separate class, owned by `CampaignState`

5. **Cargo Tags**: Should cargo tags be strings or an enum?
   - *Recommendation*: Strings for moddability, with well-known constants

---

## File Summary

| File | Action | Description |
|------|--------|-------------|
| This document | NEW | MG0 design finalization |
| `CAMPAIGN_FOUNDATIONS.md` | REFERENCE | Source of truth for concepts |
| `CampaignState.cs` | FUTURE (MG1) | Add Ship, expand crew operations |
| `CrewMember.cs` | FUTURE (MG1) | Add stats, traits |
| `Ship.cs` | FUTURE (MG2) | New class |
| `Trait.cs` | FUTURE (MG1) | New class |
| `Injury.cs` | FUTURE (MG2) | Structured injuries |

---

## Success Criteria for MG0

When MG0 is complete:

1. ✅ Resource model is documented and aligned with CAMPAIGN_FOUNDATIONS
2. ✅ Crew stat system is finalized (6 primary stats)
3. ✅ Trait system is designed (structure, initial set)
4. ✅ Injury model is clarified (categories, healing)
5. ✅ Ship/chassis concept is documented
6. ✅ PlayerState structure is clear
7. ✅ Integration points with other domains are identified
8. ✅ MG1 implementation path is clear

**Natural Pause Point**: After MG0, the design is locked. MG1 begins implementation.
