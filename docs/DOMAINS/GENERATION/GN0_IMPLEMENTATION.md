# GN0 – Concept Finalization: Implementation Plan

**Status**: 🔄 In Progress

This document breaks down **Milestone GN0** from `ROADMAP.md` into concrete design decisions and documentation deliverables.

**Goal**: Finalize design decisions for contracts, archetypes, generation context, and difficulty/risk models before implementation begins in GN1.

**Phase**: G0 (Concept/Design only – no code implementation)

---

## Current State Assessment

### What We Have (Existing Code)

| Component | Status | Location | Notes |
|-----------|--------|----------|-------|
| `Job` | ✅ Exists | `src/sim/campaign/Job.cs` | Basic contract with type, difficulty, rewards |
| `JobSystem` | ✅ Exists | `src/sim/campaign/JobSystem.cs` | Generates jobs for nodes |
| `JobType` enum | ⚠️ Limited | `src/sim/campaign/Job.cs` | Only Assault, Defense, Extraction |
| `JobDifficulty` enum | ✅ Exists | `src/sim/campaign/Job.cs` | Easy, Medium, Hard |
| `JobReward` | ✅ Exists | `src/sim/campaign/Job.cs` | Money, Parts, Fuel, Ammo |
| `MissionConfig` | ✅ Exists | `src/sim/data/MissionConfig.cs` | Map templates, spawns |
| `Sector` | ✅ Exists | `src/sim/campaign/Sector.cs` | Graph of nodes |
| `SectorNode` | ✅ Exists | `src/sim/campaign/Sector.cs` | Node with type, faction |

### Gap Analysis: CAMPAIGN_FOUNDATIONS vs Implementation

| CAMPAIGN_FOUNDATIONS Section | Current Status | Gap |
|------------------------------|----------------|-----|
| **§2.1 Contract decisions** | ⚠️ Partial | Deadlines exist; no partial success, no secondary objectives |
| **§2.2 Contract shape** | ⚠️ Partial | Missing: secondary_objectives, chain_id, location_origin distinction |
| **§2.3 Implementation notes** | ❌ Missing | No chain contracts, no minimal vs stretch goals |
| **§4 World Metrics** | ❌ Missing | No metrics influence on generation |

### What GN0 Requires vs What We Have

| GN0 Requirement | Current Status | Gap |
|-----------------|----------------|-----|
| Contract archetypes (6 types) | ⚠️ Partial | Only 3 JobTypes (Assault, Defense, Extraction) |
| Contract template structure | ⚠️ Partial | Missing optional fields, secondary objectives |
| `GenerationContext` structure | ❌ Missing | No bundled context object |
| Difficulty/risk model | ⚠️ Partial | Basic difficulty from NodeType, no player power scaling |
| Reward formulas | ⚠️ Partial | Static rewards per difficulty, no dynamic calculation |

---

## GN0 Deliverables Checklist

### Phase 1: Contract Archetype Finalization

- [ ] **1.1** Define all 6 contract archetypes with gameplay distinctions
- [ ] **1.2** Define archetype-specific objectives and success conditions
- [ ] **1.3** Define archetype-specific tactical implications
- [ ] **1.4** Map archetypes to world context (when each appears)

### Phase 2: Contract Template Structure

- [ ] **2.1** Finalize required fields for all contracts
- [ ] **2.2** Define optional fields and their purposes
- [ ] **2.3** Design secondary objectives system
- [ ] **2.4** Design contract chain structure (for future)

### Phase 3: GenerationContext Design

- [ ] **3.1** Define player state inputs
- [ ] **3.2** Define world/location inputs
- [ ] **3.3** Define metric inputs (for future simulation)
- [ ] **3.4** Document context bundling pattern

### Phase 4: Difficulty & Risk Model

- [ ] **4.1** Define player power calculation
- [ ] **4.2** Define content difficulty calculation
- [ ] **4.3** Design difficulty-to-content mapping
- [ ] **4.4** Define reward scaling formulas

---

## Phase 1: Contract Archetype Finalization

### 1.1 Contract Archetypes

Per CAMPAIGN_FOUNDATIONS §2.2, we need 6 core archetypes. Each archetype defines:
- **Primary objective** – What must be done for minimal success
- **Stretch goals** – Optional objectives for bonus rewards
- **Tactical flavor** – How it plays differently in combat
- **Context fit** – When/where this archetype appears

| Archetype | Primary Objective | Stretch Goals | Tactical Flavor |
|-----------|-------------------|---------------|-----------------|
| **Assault** | Eliminate all hostiles | Time bonus, no casualties | Direct combat, enemy elimination |
| **Delivery** | Reach extraction with cargo | Cargo intact, time bonus | Escort cargo unit, avoid damage |
| **Escort** | Keep VIP alive to extraction | VIP uninjured, time bonus | Protect NPC, defensive positioning |
| **Raid** | Destroy/steal target object | Secondary targets, clean escape | Objective-focused, can retreat after |
| **Heist** | Hack/retrieve without alarm | No kills, time bonus | Stealth-first, alarm = harder |
| **Extraction** | Rescue target(s) to extraction | All targets, no casualties | Find + escort, split objectives |

### 1.2 Archetype Details

#### Assault
**Current implementation**: `JobType.Assault` – already exists.

**Primary objective**: Eliminate all enemy combatants.
**Success condition**: No enemies remain alive.
**Failure condition**: All crew dead or retreated before completion.

**Stretch goals**:
- Complete within X turns (time bonus)
- No crew casualties
- No crew injuries

**Tactical implications**:
- Standard combat encounter
- No special mechanics required
- Victory = elimination

**Context fit**:
- High `criminal_activity` systems
- Contested zones
- Faction conflict areas
- Anti-pirate contracts

---

#### Delivery
**Current implementation**: Not implemented.

**Primary objective**: Move cargo unit from spawn to extraction zone.
**Success condition**: Cargo reaches extraction.
**Failure condition**: Cargo destroyed OR all crew dead/retreated.

**Stretch goals**:
- Cargo takes no damage
- Complete within time limit
- No crew casualties

**Tactical implications**:
- Cargo is a special "unit" that must be escorted
- Cargo cannot attack, has HP
- Crew must protect cargo from enemies
- May need to clear path before moving cargo

**Context fit**:
- Trade routes
- High `economic_activity` systems
- Faction supply lines
- Smuggling (illegal cargo tag)

---

#### Escort
**Current implementation**: Not implemented (listed as future in `JobType`).

**Primary objective**: Keep VIP alive until they reach extraction.
**Success condition**: VIP reaches extraction alive.
**Failure condition**: VIP dies OR all crew dead/retreated.

**Stretch goals**:
- VIP takes no damage
- Complete within time limit
- Eliminate all threats

**Tactical implications**:
- VIP is an NPC unit (AI-controlled or follow behavior)
- VIP has HP, can be injured
- Enemies may prioritize VIP
- Player must balance offense and protection

**Context fit**:
- Corporate/government contracts
- High-value personnel transport
- Witness protection scenarios
- Faction VIP movement

---

#### Raid
**Current implementation**: Not implemented.

**Primary objective**: Destroy or steal a specific target object.
**Success condition**: Target destroyed/acquired AND at least one crew extracts.
**Failure condition**: All crew dead before objective OR target becomes unreachable.

**Stretch goals**:
- Secondary targets destroyed
- Clean extraction (no pursuit)
- Intel gathered (hack terminals)

**Tactical implications**:
- Objective-focused, not elimination
- Can retreat after objective complete
- Target may be guarded, locked, or require hacking
- Enemies may reinforce after objective triggered

**Context fit**:
- Faction sabotage
- Corporate espionage
- Pirate base attacks
- Resource denial operations

---

#### Heist
**Current implementation**: Not implemented.

**Primary objective**: Acquire target data/item without triggering full alarm.
**Success condition**: Target acquired AND crew extracts.
**Failure condition**: All crew dead OR target destroyed/locked down.

**Stretch goals**:
- No alarm triggered (ghost bonus)
- No kills (pacifist bonus)
- Additional data retrieved
- Time bonus

**Tactical implications**:
- Stealth-first approach rewarded
- Alarm escalates difficulty (reinforcements, lockdowns)
- Hacking/tech skills valuable
- Multiple approach paths (stealth vs loud)

**Context fit**:
- Corporate targets
- High-security facilities
- Data theft contracts
- Faction intelligence gathering

---

#### Extraction
**Current implementation**: Listed in `JobType` but not fully implemented.

**Primary objective**: Locate and extract target person(s) to extraction zone.
**Success condition**: At least one target extracted alive.
**Failure condition**: All targets dead OR all crew dead/retreated.

**Stretch goals**:
- All targets extracted
- Targets uninjured
- Time bonus
- No crew casualties

**Tactical implications**:
- Two-phase: find targets, then escort out
- Targets may be in unknown locations (fog of war)
- Targets may be guarded, restrained, or injured
- Combines exploration with escort mechanics

**Context fit**:
- Rescue operations
- Hostage situations
- Downed crew recovery
- Faction prisoner extraction

---

### 1.3 Archetype-to-JobType Migration

**Current `JobType` enum**:
```csharp
public enum JobType
{
    Assault,
    Defense,    // Not in our 6 archetypes
    Extraction
}
```

**Proposed `ContractType` enum** (for GN1):
```csharp
public enum ContractType
{
    Assault,
    Delivery,
    Escort,
    Raid,
    Heist,
    Extraction
}
```

**Decision**: Rename `JobType` to `ContractType` and expand to 6 types. `Defense` can be a variant of `Assault` (defend position = eliminate attackers).

---

### 1.4 Archetype Context Mapping

When should each archetype appear? This maps world state to archetype probability.

| Archetype | Primary Context | Secondary Context |
|-----------|-----------------|-------------------|
| **Assault** | High `criminal_activity`, Contested zones | Anti-faction operations |
| **Delivery** | Trade routes, High `economic_activity` | Smuggling (illegal cargo) |
| **Escort** | Corporate/government employers | VIP transport, witness protection |
| **Raid** | Faction conflict, Sabotage opportunities | Resource denial |
| **Heist** | Corporate targets, High-security areas | Intelligence gathering |
| **Extraction** | Rescue scenarios, Hostage situations | Downed crew, prisoner recovery |

**Implementation note**: For GN1 (single hub), we'll use simplified selection. Full metric-based selection comes in GN4.

---

## Phase 2: Contract Template Structure

### 2.1 Required Fields

Every contract must have these fields:

| Field | Type | Description | Current Status |
|-------|------|-------------|----------------|
| `id` | string | Unique identifier | ✅ Exists |
| `title` | string | Display name | ✅ Exists |
| `description` | string | Flavor text | ✅ Exists |
| `contract_type` | ContractType | Archetype | ⚠️ As `JobType` |
| `issuer_faction_id` | string | Who's paying | ✅ As `EmployerFactionId` |
| `target_faction_id` | string | Who you're opposing | ✅ Exists |
| `origin_node_id` | int | Where contract was posted | ✅ Exists |
| `target_node_id` | int | Where mission takes place | ✅ Exists |
| `primary_objective` | Objective | Main goal | ❌ Missing (implicit) |
| `base_reward` | Reward | Guaranteed payout | ✅ As `JobReward` |
| `deadline_days` | int | Days from acceptance | ✅ Exists |

### 2.2 Optional Fields

| Field | Type | Description | When Used |
|-------|------|-------------|-----------|
| `secondary_objectives` | List<Objective> | Bonus goals | Stretch goals |
| `bonus_rewards` | List<Reward> | Per secondary objective | With secondaries |
| `chain_id` | string | Contract chain identifier | Chain contracts |
| `chain_stage` | int | Position in chain | Chain contracts |
| `special_conditions` | List<string> | Tags affecting generation | Contextual modifiers |
| `intel_level` | int (0-3) | How much player knows | Fog of war |
| `time_pressure` | TimePressure | Urgency level | Deadline flavor |

### 2.3 Objective Structure

**Proposed `Objective` class**:

```csharp
public class Objective
{
    public string Id { get; set; }              // "eliminate_all", "extract_vip"
    public ObjectiveType Type { get; set; }     // Eliminate, Reach, Protect, Hack, etc.
    public string Description { get; set; }     // "Eliminate all hostiles"
    public bool IsRequired { get; set; }        // Primary vs secondary
    public Reward BonusReward { get; set; }     // Reward for completing (if secondary)
    public Dictionary<string, object> Parameters { get; set; }  // Type-specific params
}

public enum ObjectiveType
{
    EliminateAll,       // Kill all enemies
    EliminateTarget,    // Kill specific enemy
    ReachZone,          // Get unit(s) to extraction
    ProtectUnit,        // Keep unit alive
    DestroyObject,      // Destroy interactable
    HackTerminal,       // Complete hack
    RetrieveItem,       // Pick up and extract item
    SurviveTurns,       // Hold out for X turns
    NoAlarm,            // Complete without alarm
    NoCasualties,       // No crew deaths
    TimeLimit           // Complete within X turns
}
```

### 2.4 Contract Chain Structure (Design Only)

Per CAMPAIGN_FOUNDATIONS §2.3, chains are state machines:

```csharp
public class ContractChain
{
    public string ChainId { get; set; }
    public string Name { get; set; }
    public int TotalStages { get; set; }
    public List<ChainStage> Stages { get; set; }
}

public class ChainStage
{
    public int StageIndex { get; set; }
    public string ContractTemplateId { get; set; }
    public string OnSuccessNextStage { get; set; }  // Stage index or "complete"
    public string OnFailNextStage { get; set; }     // Stage index or "failed"
    public List<string> UnlockConditions { get; set; }  // Flags required
}
```

**GN0 Decision**: Document chain structure; implementation deferred to GN2+.

---

## Phase 3: GenerationContext Design

### 3.1 Player State Inputs

What the generator needs to know about the player:

| Input | Type | Purpose | Source |
|-------|------|---------|--------|
| `crew_count` | int | Scale enemy count | `CampaignState.Crew.Count` |
| `crew_power` | int | Average crew strength | Calculated from stats |
| `crew_roles` | List<CrewRole> | Available capabilities | `CrewMember.Role` |
| `current_location` | int | Where player is | `CampaignState.CurrentNodeId` |
| `resources` | ResourceSnapshot | Economic state | Money, Fuel, etc. |
| `faction_rep` | Dict<string, int> | Relationship state | `CampaignState.FactionRep` |
| `completed_contracts` | int | Experience level | `CampaignState.MissionsCompleted` |

### 3.2 World/Location Inputs

| Input | Type | Purpose | Source |
|-------|------|---------|--------|
| `hub_node` | SectorNode | Current station | `Sector.GetNode()` |
| `hub_faction` | string | Who controls hub | `SectorNode.FactionId` |
| `nearby_nodes` | List<SectorNode> | Potential targets | Connected nodes |
| `sector_factions` | Dict<string, Faction> | All factions | `Sector.Factions` |

### 3.3 Metric Inputs (Future - GN4)

For simulation-aware generation:

| Input | Type | Purpose | Source |
|-------|------|---------|--------|
| `system_metrics` | SystemMetrics | Local conditions | WorldState (WD1) |
| `faction_metrics` | FactionMetrics | Faction state | WorldState (WD1) |
| `global_metrics` | GlobalMetrics | Sector-wide state | Simulation (future) |

### 3.4 GenerationContext Structure

**Proposed `GenerationContext` class**:

```csharp
public class GenerationContext
{
    // Player state
    public int CrewCount { get; set; }
    public int CrewPower { get; set; }
    public List<CrewRole> CrewRoles { get; set; }
    public int CurrentNodeId { get; set; }
    public int CompletedContracts { get; set; }
    public Dictionary<string, int> FactionRep { get; set; }
    
    // Resources
    public int Money { get; set; }
    public int Fuel { get; set; }
    
    // World state
    public SectorNode HubNode { get; set; }
    public List<SectorNode> NearbyNodes { get; set; }
    public Dictionary<string, string> Factions { get; set; }
    
    // Future: Metrics (null until WD1/GN4)
    public SystemMetrics HubMetrics { get; set; }
    
    // RNG
    public Random Rng { get; set; }
    
    /// <summary>
    /// Build context from campaign state.
    /// </summary>
    public static GenerationContext FromCampaign(CampaignState campaign)
    {
        var hub = campaign.GetCurrentNode();
        var nearby = GetNearbyNodes(campaign.Sector, campaign.CurrentNodeId);
        
        return new GenerationContext
        {
            CrewCount = campaign.GetDeployableCrew().Count,
            CrewPower = CalculateCrewPower(campaign.Crew),
            CrewRoles = campaign.Crew.Select(c => c.Role).ToList(),
            CurrentNodeId = campaign.CurrentNodeId,
            CompletedContracts = campaign.MissionsCompleted,
            FactionRep = new Dictionary<string, int>(campaign.FactionRep),
            Money = campaign.Money,
            Fuel = campaign.Fuel,
            HubNode = hub,
            NearbyNodes = nearby,
            Factions = campaign.Sector.Factions,
            Rng = campaign.CreateSeededRandom()
        };
    }
    
    private static int CalculateCrewPower(List<CrewMember> crew)
    {
        // Sum of crew levels + average stats
        // Placeholder formula - refine in GN1
        return crew.Sum(c => c.Level + c.Aim + c.Toughness + c.Reflexes);
    }
}
```

---

## Phase 4: Difficulty & Risk Model

### 4.1 Player Power Calculation

**Current implementation**: None (difficulty based only on target node type).

**Proposed formula**:

```
PlayerPower = CrewPower + EquipmentBonus + ExperienceBonus

Where:
- CrewPower = Sum of (Level + Aim + Grit + Reflexes) for deployable crew
- EquipmentBonus = Sum of weapon tiers (future)
- ExperienceBonus = MissionsCompleted * 2
```

**Power tiers**:

| Tier | Power Range | Description |
|------|-------------|-------------|
| Rookie | 0-30 | New crew, basic gear |
| Competent | 31-60 | Some experience, decent gear |
| Veteran | 61-100 | Experienced, good gear |
| Elite | 101+ | Highly experienced, best gear |

### 4.2 Content Difficulty Calculation

**Current implementation**: Based on `NodeType` with random variance.

**Proposed formula**:

```
ContentDifficulty = BaseDifficulty + ContextModifiers + RandomVariance

Where:
- BaseDifficulty = From target node type (current implementation)
- ContextModifiers = From world metrics (future)
- RandomVariance = ±1 tier (20% chance each direction)
```

**Difficulty tiers** (align with `JobDifficulty`):

| Tier | Enemy Count | Enemy Types | Map Complexity |
|------|-------------|-------------|----------------|
| Easy | 2-3 | Grunts only | Simple layout |
| Medium | 3-4 | Grunts + Gunner | Some cover/rooms |
| Hard | 4-5 | Grunts + Gunner + Heavy | Complex layout |
| Extreme | 5-6 | Mixed + Elites | Multi-room, hazards |

### 4.3 Difficulty-to-Content Mapping

How difficulty translates to mission content:

| Aspect | Easy | Medium | Hard | Extreme |
|--------|------|--------|------|---------|
| **Grid size** | 10x8 | 12x10 | 14x12 | 16x14 |
| **Enemy count** | 2 | 3 | 4-5 | 5-6 |
| **Enemy composition** | 100% grunt | 66% grunt, 33% gunner | 50% grunt, 25% gunner, 25% heavy | Mixed + elite |
| **Interactables** | 0-1 | 1-2 | 2-3 | 3+ |
| **Cover density** | Low | Medium | High | High + hazards |

### 4.4 Reward Scaling Formulas

**Current implementation**: Static rewards per difficulty tier.

**Proposed formula**:

```
BaseReward = DifficultyReward * ArchetypeMultiplier * FactionMultiplier

Where:
- DifficultyReward = { Easy: 100, Medium: 200, Hard: 400, Extreme: 600 }
- ArchetypeMultiplier = { Assault: 1.0, Delivery: 1.1, Escort: 1.2, Raid: 1.3, Heist: 1.4, Extraction: 1.2 }
- FactionMultiplier = Based on faction wealth/desperation (future)
```

**Bonus rewards** (for secondary objectives):

| Objective Type | Bonus % |
|----------------|---------|
| Time bonus | +15% |
| No casualties | +20% |
| No injuries | +10% |
| Ghost (no alarm) | +25% |
| All targets | +15% |

---

## Implementation Order for GN1

Based on GN0 decisions, GN1 will implement:

### Priority 1: Core Contract Generation

1. **Rename `JobType` to `ContractType`**
   - Add Delivery, Escort, Raid, Heist types
   - Update all references

2. **Create `GenerationContext` class**
   - Implement `FromCampaign()` builder
   - Add `CalculateCrewPower()` helper

3. **Create `ContractGenerator` class**
   - Replace inline generation in `JobSystem`
   - Use `GenerationContext` as input
   - Return `List<Contract>` (renamed from `Job`)

### Priority 2: Objective System

4. **Create `Objective` class**
   - Define `ObjectiveType` enum
   - Add to `Contract` (primary + secondary)

5. **Update `MissionConfig` generation**
   - Generate based on contract type
   - Include objective-specific elements

### Priority 3: Difficulty Scaling

6. **Implement player power calculation**
   - Add `CalculateCrewPower()` to context

7. **Implement difficulty matching**
   - Scale content to player power
   - Apply reward formulas

---

## Files to Create (GN1)

| File | Purpose |
|------|---------|
| `src/sim/generation/GenerationContext.cs` | Context bundling |
| `src/sim/generation/ContractGenerator.cs` | Contract generation |
| `src/sim/generation/ContractTemplate.cs` | Template definitions |
| `src/sim/campaign/Contract.cs` | Renamed from Job.cs |
| `src/sim/campaign/Objective.cs` | Objective structure |

---

## Acceptance Criteria for GN0

### Documentation Complete

- [x] Contract archetypes defined (6 types with details)
- [x] Archetype objectives and success conditions documented
- [x] Contract template structure finalized
- [x] Secondary objectives system designed
- [x] `GenerationContext` structure documented
- [x] Player power calculation defined
- [x] Difficulty-to-content mapping defined
- [x] Reward scaling formulas defined

### Design Decisions Recorded

- [x] `JobType` → `ContractType` rename planned
- [x] Objective system structure defined
- [x] Context bundling pattern established
- [x] Difficulty tiers aligned with content

### Ready for GN1

- [x] Clear list of files to create
- [x] Clear class structures
- [x] Migration path from current code
- [x] No ambiguity in generation model

---

## Open Questions for Future Milestones

### For GN1 (Contract Generation)

- How many contracts should be generated per hub? (Current: 3)
- Should contracts expire if not accepted?
- How do we handle contract rejection (rep penalty)?

### For GN2 (Galaxy Generation)

- How do we seed initial contracts across the galaxy?
- Should some contracts be "persistent" (always available)?
- How do faction relationships affect contract availability?

### For GN4 (Simulation-Aware)

- How do metrics bias archetype selection?
- How do we prevent "death spirals" (low security → more crime → lower security)?
- How do we ensure variety despite metric biases?

---

## Appendix: Alignment with CAMPAIGN_FOUNDATIONS.md

| CAMPAIGN_FOUNDATIONS Section | GN0 Coverage |
|------------------------------|--------------|
| §2.1 Contract decisions | ✅ Soft deadlines, partial success, contextual cues |
| §2.2 Contract shape | ✅ All required fields, optional fields defined |
| §2.3 Implementation notes | ✅ Chain structure designed, minimal vs stretch goals |
| §4.1 World metrics | ⚠️ Designed for future, not implemented in GN0 |
| §6 RNG & Determinism | ✅ Context includes RNG, deterministic generation |

---

## Appendix: Alignment with DOMAIN.md

| DOMAIN.md Responsibility | GN0 Coverage |
|--------------------------|--------------|
| Mission generation | ✅ Contract archetypes, templates |
| Contract generation | ✅ Full structure defined |
| GenerationContext | ✅ Structure documented |
| Difficulty & Risk models | ✅ Player power, content scaling |
| Deterministic generation | ✅ RNG in context |
| Data-driven generation | ✅ Template-based approach |

---

## Appendix: Current Code Migration

### Job.cs → Contract.cs

| Current Field | New Field | Notes |
|---------------|-----------|-------|
| `Id` | `Id` | Keep |
| `Title` | `Title` | Keep |
| `Description` | `Description` | Keep |
| `Type` (JobType) | `ContractType` | Rename + expand |
| `Difficulty` | `Difficulty` | Keep |
| `OriginNodeId` | `OriginNodeId` | Keep |
| `TargetNodeId` | `TargetNodeId` | Keep |
| `EmployerFactionId` | `IssuerFactionId` | Rename for clarity |
| `TargetFactionId` | `TargetFactionId` | Keep |
| `Reward` | `BaseReward` | Rename |
| `RepGain` | `RepGain` | Keep |
| `RepLoss` | `RepLoss` | Keep |
| `FailureRepLoss` | `FailureRepLoss` | Keep |
| `DeadlineDays` | `DeadlineDays` | Keep |
| `DeadlineDay` | `DeadlineDay` | Keep |
| `MissionConfig` | `MissionConfig` | Keep |
| — | `PrimaryObjective` | Add |
| — | `SecondaryObjectives` | Add |
| — | `BonusRewards` | Add |
| — | `ChainId` | Add (optional) |
| — | `ChainStage` | Add (optional) |

### JobSystem.cs → ContractGenerator.cs

| Current Method | New Method | Notes |
|----------------|------------|-------|
| `GenerateJobsForNode()` | `GenerateContracts()` | Takes `GenerationContext` |
| `GenerateSingleJob()` | `GenerateContract()` | Uses templates |
| `DetermineJobDifficulty()` | `DetermineDifficulty()` | Uses player power |
| `GenerateMissionConfig()` | `GenerateMissionConfig()` | Based on contract type |

---

## Success Criteria for GN0

When GN0 is complete:

1. ✅ All 6 contract archetypes are defined with gameplay distinctions
2. ✅ Contract template structure is finalized
3. ✅ Secondary objectives system is designed
4. ✅ `GenerationContext` structure is documented
5. ✅ Player power calculation is defined
6. ✅ Difficulty-to-content mapping is clear
7. ✅ Reward scaling formulas are defined
8. ✅ Migration path from current code is documented
9. ✅ GN1 implementation path is clear

**Natural Pause Point**: After GN0, the design is locked. GN1 begins implementation.
