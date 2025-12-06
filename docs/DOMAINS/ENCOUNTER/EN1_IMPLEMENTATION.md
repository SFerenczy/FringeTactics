# EN1 – Runtime Core: Implementation Plan

**Status**: ✅ Complete  
**Depends on**: EN0 (Concept) – can proceed in parallel with minimal design  
**Soft dependencies**: TV2 (Travel Execution) ✅ Complete – provides encounter trigger points  
**Phase**: G2

---

## Overview

**Goal**: Implement the encounter state machine that runs encounter instances. This is the runtime engine that processes player choices through branching narrative encounters, evaluates conditions, and accumulates effects for later application.

EN1 provides:
- Data structures for encounter templates, nodes, options, conditions, and outcomes
- `EncounterRunner` state machine for stepping through encounters
- Condition evaluation system for option visibility and availability
- Effect accumulation (effects are collected, not applied – MG4 handles application)
- Integration point with `TravelContext` from TV2

---

## Current State Assessment

### What We Have (from TV2, MG1, WD3)

| Component | Status | Location | Notes |
|-----------|--------|----------|-------|
| `TravelContext` | ✅ Complete | `src/sim/travel/TravelContext.cs` | Context for encounter selection |
| `TravelEncounterTriggeredEvent` | ✅ Complete | `src/sim/Events.cs` | Event when encounter triggers |
| `CrewMember` | ✅ Complete | `src/sim/campaign/CrewMember.cs` | Stats, traits for checks |
| `CrewStatType` | ✅ Complete | `src/sim/campaign/StatType.cs` | Grit, Reflexes, Aim, Tech, Savvy, Resolve |
| `TraitRegistry` | ✅ Complete | `src/sim/campaign/TraitRegistry.cs` | Trait definitions |
| `CampaignState` | ✅ Complete | `src/sim/campaign/CampaignState.cs` | Resources, crew, faction rep |
| `RngService` | ✅ Complete | `src/sim/RngService.cs` | Deterministic RNG |
| `EventBus` | ✅ Complete | `src/sim/EventBus.cs` | Cross-domain events |
| `SystemMetrics` | ✅ Complete | `src/sim/world/SystemMetrics.cs` | World state for conditions |

### EN1 Implementation Status

| Requirement | Status | Location |
|-------------|--------|----------|
| `EncounterTemplate` class | ✅ Complete | `src/sim/encounter/EncounterTemplate.cs` |
| `EncounterNode` class | ✅ Complete | `src/sim/encounter/EncounterNode.cs` |
| `EncounterOption` class | ✅ Complete | `src/sim/encounter/EncounterOption.cs` |
| `EncounterCondition` class | ✅ Complete | `src/sim/encounter/EncounterCondition.cs` |
| `EncounterOutcome` class | ✅ Complete | `src/sim/encounter/EncounterOutcome.cs` |
| `EncounterEffect` class | ✅ Complete | `src/sim/encounter/EncounterEffect.cs` |
| `EncounterInstance` class | ✅ Complete | `src/sim/encounter/EncounterInstance.cs` |
| `EncounterContext` class | ✅ Complete | `src/sim/encounter/EncounterContext.cs` |
| `EncounterRunner` class | ✅ Complete | `src/sim/encounter/EncounterRunner.cs` |
| Encounter events | ✅ Complete | `src/sim/Events.cs` |
| Unit tests | ✅ Complete | `tests/sim/encounter/EN1*.cs` (78 tests) |

---

## Architecture Decisions

### AD1: Data-Driven Templates

**Decision**: Encounter templates are pure data structures with no behavior. All logic lives in `EncounterRunner`.

**Rationale**:
- Enables future JSON/data file loading
- Easier to test and validate templates
- Follows existing patterns (WeaponData, ItemDef, TraitDef)

### AD2: Stateless Runner with Explicit State

**Decision**: `EncounterRunner` is stateless. All encounter state lives in `EncounterInstance`.

**Rationale**:
- Follows existing patterns (`TravelExecutor`, `ContractGenerator`, `TravelPlanner`)
- Enables save/load mid-encounter
- Pure functions for testability

### AD3: Effect Accumulation, Not Application

**Decision**: EN1 accumulates effects in `EncounterInstance.PendingEffects`. MG4 applies them to `CampaignState`.

**Rationale**:
- Clear separation of concerns
- Encounter domain doesn't mutate campaign state directly
- Enables preview/confirmation UI before applying effects
- Matches architecture guidelines (sim doesn't know about Management)

### AD4: Condition Evaluation via Context

**Decision**: Conditions evaluate against `EncounterContext`, a snapshot of relevant state.

**Rationale**:
- Decouples conditions from direct CampaignState access
- Context can be constructed from different sources (travel, station, event)
- Enables testing with mock contexts

---

## Implementation Steps

### Phase 1: Core Data Structures

#### Step 1.1: Create EncounterEffect Class

**File**: `src/sim/encounter/EncounterEffect.cs`

Defines atomic effects that can result from encounter choices.

**Effect Types** (from ROADMAP.md):
- `AddResource` – Add/remove credits, fuel, etc.
- `CrewInjury` – Injure crew member
- `CrewXp` – Grant XP
- `CrewTrait` – Add/remove trait
- `ShipDamage` – Damage ship hull
- `FactionRep` – Change faction reputation
- `SetFlag` – Set campaign flag
- `TimeDelay` – Advance time
- `TriggerTactical` – Start tactical mission (EN3)
- `AddCargo` – Add cargo items
- `RemoveCargo` – Remove cargo items
- `GotoNode` – Transition to another node
- `EndEncounter` – End the encounter

**Acceptance Criteria**:
- [ ] `EffectType` enum with all effect types
- [ ] `EncounterEffect` class with type and parameters
- [ ] Factory methods for common effects

---

#### Step 1.2: Create EncounterCondition Class

**File**: `src/sim/encounter/EncounterCondition.cs`

Defines conditions for option visibility/availability.

**Condition Types** (from ROADMAP.md):
- `HasResource` – Player has minimum resource
- `HasTrait` – Any crew has trait
- `HasCargo` – Cargo matches criteria
- `FactionRep` – Reputation threshold
- `SystemTag` – Current system has tag
- `CrewStat` – Best crew stat meets threshold
- `HasFlag` – Campaign flag is set

**Acceptance Criteria**:
- [ ] `ConditionType` enum
- [ ] `EncounterCondition` class with `Evaluate(EncounterContext) -> bool`
- [ ] All condition types implemented
- [ ] Negation support (`Not` modifier)

---

#### Step 1.3: Create EncounterOutcome Class

**File**: `src/sim/encounter/EncounterOutcome.cs`

Defines what happens when an option is selected (or a check succeeds/fails).

**Acceptance Criteria**:
- [ ] `EncounterOutcome` with list of effects
- [ ] Optional `NextNodeId` for transitions
- [ ] `IsEndEncounter` flag

---

#### Step 1.4: Create EncounterOption Class

**File**: `src/sim/encounter/EncounterOption.cs`

Defines a choice the player can make at a node.

**Acceptance Criteria**:
- [ ] `Id` for identification
- [ ] `TextKey` for localization
- [ ] `Conditions` list for visibility
- [ ] `Outcome` for direct choices
- [ ] Optional `SkillCheck` (EN2 – stubbed for now)
- [ ] `SuccessOutcome` and `FailureOutcome` for checks

---

#### Step 1.5: Create EncounterNode Class

**File**: `src/sim/encounter/EncounterNode.cs`

Defines a single step in an encounter.

**Acceptance Criteria**:
- [ ] `Id` unique within template
- [ ] `TextKey` for narrative content
- [ ] `Options` list
- [ ] Optional `AutoTransition` for narrative-only nodes
- [ ] `IsEndNode` flag

---

#### Step 1.6: Create EncounterTemplate Class

**File**: `src/sim/encounter/EncounterTemplate.cs`

Defines a complete encounter structure.

**Acceptance Criteria**:
- [ ] `Id` for identification
- [ ] `Name` for display
- [ ] `Tags` for selection (pirate, patrol, travel, etc.)
- [ ] `EntryNodeId` starting point
- [ ] `Nodes` dictionary
- [ ] `RequiredContextKeys` for validation
- [ ] `GetNode(id)` helper

---

### Phase 2: Runtime Classes

#### Step 2.1: Create EncounterContext Class

**File**: `src/sim/encounter/EncounterContext.cs`

Snapshot of state for condition evaluation.

**Acceptance Criteria**:
- [ ] Player resources (money, fuel, etc.)
- [ ] Crew list with stats and traits
- [ ] Current system info (tags, metrics, owner)
- [ ] Faction reputation dictionary
- [ ] Cargo summary
- [ ] Campaign flags
- [ ] RNG stream reference
- [ ] `FromCampaign(CampaignState)` factory
- [ ] `FromTravelContext(TravelContext, CampaignState)` factory
- [ ] Query methods: `GetBestCrewForStat()`, `HasCrewWithTrait()`, `HasResource()`

---

#### Step 2.2: Create EncounterInstance Class

**File**: `src/sim/encounter/EncounterInstance.cs`

Runtime state of an active encounter.

**Acceptance Criteria**:
- [ ] Reference to `EncounterTemplate`
- [ ] `CurrentNodeId` tracking
- [ ] `VisitedNodes` history
- [ ] `PendingEffects` accumulator
- [ ] `ResolvedParameters` (NPC names, cargo types, etc.)
- [ ] `IsComplete` flag
- [ ] `IsPausedForTactical` flag (EN3)
- [ ] Serializable for save/load

---

#### Step 2.3: Create EncounterRunner Class

**File**: `src/sim/encounter/EncounterRunner.cs`

Stateless service that steps through encounters.

**Acceptance Criteria**:
- [ ] `GetCurrentNode(instance) -> EncounterNode`
- [ ] `GetAvailableOptions(instance, context) -> List<EncounterOption>`
- [ ] `SelectOption(instance, context, optionIndex) -> EncounterStepResult`
- [ ] `IsComplete(instance) -> bool`
- [ ] `GetPendingEffects(instance) -> List<EncounterEffect>`
- [ ] Condition evaluation for option filtering
- [ ] Effect accumulation on selection
- [ ] Node transition handling
- [ ] Auto-transition support

---

### Phase 3: Events

#### Step 3.1: Add Encounter Events

**File**: `src/sim/Events.cs` (add to existing)

**Events to add**:
- `EncounterStartedEvent(EncounterId, TemplateId, Tags)`
- `EncounterNodeEnteredEvent(EncounterId, NodeId)`
- `EncounterOptionSelectedEvent(EncounterId, NodeId, OptionId)`
- `EncounterCompletedEvent(EncounterId, EffectCount)`

---

### Phase 4: Test Templates

#### Step 4.1: Create Test Templates

**File**: `src/sim/encounter/TestEncounters.cs`

Static factory methods for test encounters:
- `CreateSimpleEncounter()` – 2 nodes, 2 options, no conditions
- `CreateConditionalEncounter()` – Options with resource/trait conditions
- `CreateBranchingEncounter()` – Multiple paths through nodes
- `CreatePirateAmbush()` – Full example from ROADMAP.md

---

### Phase 5: Unit Tests

#### Test Files

```
tests/sim/encounter/
├── EN1EffectTests.cs
├── EN1ConditionTests.cs
├── EN1TemplateTests.cs
├── EN1RunnerTests.cs
├── EN1DeterminismTests.cs
```

#### Key Test Cases

**EncounterEffect**:
- `AddResource_CreatesCorrectEffect`
- `ShipDamage_CreatesCorrectEffect`
- `AllEffectTypes_HaveValidParameters`

**EncounterCondition**:
- `HasResource_TrueWhenSufficient`
- `HasResource_FalseWhenInsufficient`
- `HasTrait_TrueWhenCrewHasTrait`
- `HasTrait_FalseWhenNoCrewHasTrait`
- `FactionRep_EvaluatesThresholdCorrectly`
- `SystemTag_ChecksCurrentSystem`
- `CrewStat_UsesBestCrewMember`
- `Negation_InvertsResult`

**EncounterRunner**:
- `GetCurrentNode_ReturnsEntryNodeInitially`
- `GetAvailableOptions_FiltersOnConditions`
- `SelectOption_AccumulatesEffects`
- `SelectOption_TransitionsToNextNode`
- `SelectOption_EndsEncounterWhenNoNextNode`
- `AutoTransition_AdvancesAutomatically`
- `MultipleSelections_AccumulatesAllEffects`

**Determinism**:
- `SameInputs_ProduceSameResults`
- `DifferentSeeds_ProduceDifferentResults` (for future skill checks)

---

## Files to Create

| File | Purpose |
|------|---------|
| `src/sim/encounter/EffectType.cs` | Effect type enum |
| `src/sim/encounter/EncounterEffect.cs` | Effect definition |
| `src/sim/encounter/ConditionType.cs` | Condition type enum |
| `src/sim/encounter/EncounterCondition.cs` | Condition evaluation |
| `src/sim/encounter/EncounterOutcome.cs` | Outcome with effects |
| `src/sim/encounter/EncounterOption.cs` | Player choice |
| `src/sim/encounter/EncounterNode.cs` | Single encounter step |
| `src/sim/encounter/EncounterTemplate.cs` | Complete encounter |
| `src/sim/encounter/EncounterContext.cs` | Evaluation context |
| `src/sim/encounter/EncounterInstance.cs` | Runtime state |
| `src/sim/encounter/EncounterRunner.cs` | State machine |
| `src/sim/encounter/TestEncounters.cs` | Test templates |
| `src/sim/encounter/agents.md` | Directory documentation |
| `tests/sim/encounter/EN1EffectTests.cs` | Effect tests |
| `tests/sim/encounter/EN1ConditionTests.cs` | Condition tests |
| `tests/sim/encounter/EN1TemplateTests.cs` | Template tests |
| `tests/sim/encounter/EN1RunnerTests.cs` | Runner tests |
| `tests/sim/encounter/EN1DeterminismTests.cs` | Determinism tests |

## Files to Modify

| File | Changes |
|------|---------|
| `src/sim/Events.cs` | Add encounter events |
| `src/sim/agents.md` | Add encounter subdirectory |

---

## Implementation Order

1. **EffectType.cs + EncounterEffect.cs** – Foundation
2. **ConditionType.cs + EncounterCondition.cs** – Evaluation logic
3. **EncounterOutcome.cs** – Effect containers
4. **EncounterOption.cs** – Choice structure
5. **EncounterNode.cs** – Step structure
6. **EncounterTemplate.cs** – Complete template
7. **EncounterContext.cs** – Evaluation context
8. **EncounterInstance.cs** – Runtime state
9. **EncounterRunner.cs** – State machine
10. **TestEncounters.cs** – Test data
11. **Events.cs updates** – Event types
12. **agents.md files** – Documentation
13. **Unit tests** – All test files

---

## Manual Test Setup

### Scenario 1: Simple Encounter Flow

1. Create `EncounterInstance` from `TestEncounters.CreateSimpleEncounter()`
2. Create `EncounterContext` from test campaign
3. Call `runner.GetCurrentNode(instance)` – verify entry node
4. Call `runner.GetAvailableOptions(instance, context)` – verify 2 options
5. Call `runner.SelectOption(instance, context, 0)` – verify transition
6. Verify `instance.PendingEffects` contains expected effects
7. Verify `instance.IsComplete` when reaching end

### Scenario 2: Conditional Options

1. Create `EncounterInstance` from `TestEncounters.CreateConditionalEncounter()`
2. Create context with 0 money
3. Verify money-requiring option is NOT in available options
4. Create context with 100 money
5. Verify money-requiring option IS in available options

### Scenario 3: Branching Paths

1. Create `EncounterInstance` from `TestEncounters.CreateBranchingEncounter()`
2. Select option A → verify node A reached
3. Reset, select option B → verify node B reached
4. Verify different effects accumulated for each path

### Scenario 4: Integration with Travel

1. Create `CampaignState` with test sector
2. Execute travel that triggers encounter (via `TravelExecutor`)
3. When `TravelEncounterTriggeredEvent` fires:
   - Create `EncounterContext.FromTravelContext()`
   - Create `EncounterInstance` (would be from GN3 in full system)
   - Run through encounter with `EncounterRunner`
4. Verify encounter completes and effects are accumulated

---

## Success Criteria

- [ ] All encounter data classes defined and serializable
- [ ] `EncounterRunner` steps through encounters correctly
- [ ] Conditions evaluate against context
- [ ] Effects accumulate without application
- [ ] Multiple paths through encounters work
- [ ] Deterministic given same inputs and choices
- [ ] 3-5 test templates validate the system
- [ ] All unit tests passing (~30-40 tests)
- [ ] Integration with `TravelContext` verified

---

## Future Work (EN2+)

- **EN2**: Skill checks with crew stats and trait bonuses
- **EN3**: Tactical branching (pause encounter, run mission, resume)
- **EN4**: Simulation integration (metrics affect selection/outcomes)
- **GN3**: Encounter instantiation from templates with parameters
- **MG4**: Effect application to campaign state

---

## Appendix A: Code Specifications

### A.1: EffectType Enum

```csharp
namespace FringeTactics;

public enum EffectType
{
    // Resource effects
    AddResource,
    
    // Crew effects
    CrewInjury,
    CrewXp,
    CrewTrait,
    
    // Ship effects
    ShipDamage,
    
    // World effects
    FactionRep,
    SetFlag,
    
    // Time effects
    TimeDelay,
    
    // Cargo effects
    AddCargo,
    RemoveCargo,
    
    // Flow effects
    GotoNode,
    EndEncounter,
    
    // Tactical (EN3)
    TriggerTactical
}
```

### A.2: EncounterEffect Class

```csharp
namespace FringeTactics;

public class EncounterEffect
{
    public EffectType Type { get; set; }
    
    // Common parameters (used based on Type)
    public string TargetId { get; set; }      // resource type, trait id, faction id, node id, flag id
    public int Amount { get; set; }           // quantity, damage, days, rep delta
    public string StringParam { get; set; }   // injury type, cargo item id, mission type
    public bool BoolParam { get; set; }       // add/remove for traits
    
    // Factory methods
    public static EncounterEffect AddCredits(int amount) => new()
        { Type = EffectType.AddResource, TargetId = ResourceTypes.Money, Amount = amount };
    
    public static EncounterEffect AddFuel(int amount) => new()
        { Type = EffectType.AddResource, TargetId = ResourceTypes.Fuel, Amount = amount };
    
    public static EncounterEffect LoseCredits(int amount) => new()
        { Type = EffectType.AddResource, TargetId = ResourceTypes.Money, Amount = -amount };
    
    public static EncounterEffect ShipDamage(int amount) => new()
        { Type = EffectType.ShipDamage, Amount = amount };
    
    public static EncounterEffect CrewInjury(string injuryType = InjuryTypes.Wounded) => new()
        { Type = EffectType.CrewInjury, StringParam = injuryType };
    
    public static EncounterEffect FactionRep(string factionId, int delta) => new()
        { Type = EffectType.FactionRep, TargetId = factionId, Amount = delta };
    
    public static EncounterEffect TimeDelay(int days) => new()
        { Type = EffectType.TimeDelay, Amount = days };
    
    public static EncounterEffect GotoNode(string nodeId) => new()
        { Type = EffectType.GotoNode, TargetId = nodeId };
    
    public static EncounterEffect End() => new()
        { Type = EffectType.EndEncounter };
}
```

### A.3: ConditionType Enum

```csharp
namespace FringeTactics;

public enum ConditionType
{
    HasResource,      // Player has minimum resource
    HasTrait,         // Any crew has trait
    HasCargo,         // Cargo matches criteria
    FactionRep,       // Reputation threshold
    SystemTag,        // Current system has tag
    CrewStat,         // Best crew stat meets threshold
    HasFlag,          // Campaign flag is set
    Not               // Negates child condition
}
```

### A.4: EncounterCondition Class

```csharp
namespace FringeTactics;

public class EncounterCondition
{
    public ConditionType Type { get; set; }
    public string TargetId { get; set; }      // resource type, trait id, faction id, tag, stat type, flag id
    public int Threshold { get; set; }        // minimum value for resource/rep/stat checks
    public EncounterCondition ChildCondition { get; set; }  // for Not type
    
    public bool Evaluate(EncounterContext context)
    {
        return Type switch
        {
            ConditionType.HasResource => context.GetResource(TargetId) >= Threshold,
            ConditionType.HasTrait => context.HasCrewWithTrait(TargetId),
            ConditionType.HasCargo => context.CargoValue >= Threshold,
            ConditionType.FactionRep => context.GetFactionRep(TargetId) >= Threshold,
            ConditionType.SystemTag => context.SystemTags.Contains(TargetId),
            ConditionType.CrewStat => context.GetBestCrewStat(TargetId) >= Threshold,
            ConditionType.HasFlag => context.HasFlag(TargetId),
            ConditionType.Not => ChildCondition != null && !ChildCondition.Evaluate(context),
            _ => true
        };
    }
    
    // Factory methods
    public static EncounterCondition HasCredits(int min) => new()
        { Type = ConditionType.HasResource, TargetId = ResourceTypes.Money, Threshold = min };
    
    public static EncounterCondition HasTrait(string traitId) => new()
        { Type = ConditionType.HasTrait, TargetId = traitId };
    
    public static EncounterCondition FactionRepMin(string factionId, int min) => new()
        { Type = ConditionType.FactionRep, TargetId = factionId, Threshold = min };
    
    public static EncounterCondition CrewStatMin(CrewStatType stat, int min) => new()
        { Type = ConditionType.CrewStat, TargetId = stat.ToString(), Threshold = min };
}
```

### A.5: EncounterContext Class

```csharp
namespace FringeTactics;

public class EncounterContext
{
    // Resources
    public int Money { get; set; }
    public int Fuel { get; set; }
    public int Parts { get; set; }
    public int Ammo { get; set; }
    
    // Crew snapshot
    public List<CrewSnapshot> Crew { get; set; } = new();
    
    // World state
    public int CurrentSystemId { get; set; }
    public HashSet<string> SystemTags { get; set; } = new();
    public string SystemOwnerFactionId { get; set; }
    
    // Faction rep
    public Dictionary<string, int> FactionRep { get; set; } = new();
    
    // Cargo
    public int CargoValue { get; set; }
    public bool HasIllegalCargo { get; set; }
    
    // Flags
    public HashSet<string> Flags { get; set; } = new();
    
    // RNG
    public RngStream Rng { get; set; }
    
    // Query methods
    public int GetResource(string type) => type switch
    {
        ResourceTypes.Money => Money,
        ResourceTypes.Fuel => Fuel,
        ResourceTypes.Parts => Parts,
        ResourceTypes.Ammo => Ammo,
        _ => 0
    };
    
    public bool HasCrewWithTrait(string traitId) =>
        Crew.Any(c => c.TraitIds.Contains(traitId));
    
    public int GetFactionRep(string factionId) =>
        FactionRep.TryGetValue(factionId, out var rep) ? rep : 50;
    
    public bool HasFlag(string flagId) => Flags.Contains(flagId);
    
    public int GetBestCrewStat(string statName)
    {
        if (!Enum.TryParse<CrewStatType>(statName, out var stat)) return 0;
        return Crew.Count > 0 ? Crew.Max(c => c.GetStat(stat)) : 0;
    }
    
    public CrewSnapshot GetBestCrewForStat(CrewStatType stat) =>
        Crew.OrderByDescending(c => c.GetStat(stat)).FirstOrDefault();
    
    // Factory
    public static EncounterContext FromCampaign(CampaignState campaign)
    {
        var context = new EncounterContext
        {
            Money = campaign.Money,
            Fuel = campaign.Fuel,
            Parts = campaign.Parts,
            Ammo = campaign.Ammo,
            CurrentSystemId = campaign.CurrentNodeId,
            CargoValue = campaign.Inventory?.GetTotalValue() ?? 0,
            FactionRep = new Dictionary<string, int>(campaign.FactionRep),
            Rng = campaign.Rng?.Campaign
        };
        
        var system = campaign.GetCurrentSystem();
        if (system != null)
        {
            context.SystemTags = new HashSet<string>(system.Tags ?? new HashSet<string>());
            context.SystemOwnerFactionId = system.OwningFactionId;
        }
        
        foreach (var crew in campaign.GetAliveCrew())
        {
            context.Crew.Add(CrewSnapshot.From(crew));
        }
        
        return context;
    }
    
    public static EncounterContext FromTravelContext(TravelContext travel, CampaignState campaign)
    {
        var context = FromCampaign(campaign);
        context.CurrentSystemId = travel.CurrentSystemId;
        context.SystemTags = travel.SystemTags;
        context.SystemOwnerFactionId = travel.SystemOwnerFactionId;
        context.CargoValue = travel.CargoValue;
        context.HasIllegalCargo = travel.HasIllegalCargo;
        return context;
    }
}

/// <summary>
/// Lightweight crew snapshot for encounter evaluation.
/// </summary>
public class CrewSnapshot
{
    public int Id { get; set; }
    public string Name { get; set; }
    public List<string> TraitIds { get; set; } = new();
    public int Grit { get; set; }
    public int Reflexes { get; set; }
    public int Aim { get; set; }
    public int Tech { get; set; }
    public int Savvy { get; set; }
    public int Resolve { get; set; }
    
    public int GetStat(CrewStatType stat) => stat switch
    {
        CrewStatType.Grit => Grit,
        CrewStatType.Reflexes => Reflexes,
        CrewStatType.Aim => Aim,
        CrewStatType.Tech => Tech,
        CrewStatType.Savvy => Savvy,
        CrewStatType.Resolve => Resolve,
        _ => 0
    };
    
    public static CrewSnapshot From(CrewMember crew) => new()
    {
        Id = crew.Id,
        Name = crew.Name,
        TraitIds = new List<string>(crew.TraitIds),
        Grit = crew.GetEffectiveStat(CrewStatType.Grit),
        Reflexes = crew.GetEffectiveStat(CrewStatType.Reflexes),
        Aim = crew.GetEffectiveStat(CrewStatType.Aim),
        Tech = crew.GetEffectiveStat(CrewStatType.Tech),
        Savvy = crew.GetEffectiveStat(CrewStatType.Savvy),
        Resolve = crew.GetEffectiveStat(CrewStatType.Resolve)
    };
}
```

### A.6: EncounterRunner Class (Core Logic)

```csharp
namespace FringeTactics;

public class EncounterRunner
{
    public EncounterNode GetCurrentNode(EncounterInstance instance)
    {
        if (instance.IsComplete) return null;
        return instance.Template.GetNode(instance.CurrentNodeId);
    }
    
    public List<EncounterOption> GetAvailableOptions(EncounterInstance instance, EncounterContext context)
    {
        var node = GetCurrentNode(instance);
        if (node == null) return new List<EncounterOption>();
        
        var available = new List<EncounterOption>();
        foreach (var option in node.Options)
        {
            if (EvaluateConditions(option.Conditions, context))
            {
                available.Add(option);
            }
        }
        return available;
    }
    
    public EncounterStepResult SelectOption(EncounterInstance instance, EncounterContext context, int optionIndex)
    {
        var available = GetAvailableOptions(instance, context);
        if (optionIndex < 0 || optionIndex >= available.Count)
        {
            return EncounterStepResult.Invalid("Invalid option index");
        }
        
        var option = available[optionIndex];
        var outcome = option.Outcome; // EN2 will add skill check logic here
        
        // Accumulate effects
        foreach (var effect in outcome.Effects)
        {
            if (effect.Type == EffectType.GotoNode)
            {
                instance.CurrentNodeId = effect.TargetId;
            }
            else if (effect.Type == EffectType.EndEncounter)
            {
                instance.IsComplete = true;
            }
            else
            {
                instance.PendingEffects.Add(effect);
            }
        }
        
        // Handle next node transition
        if (!instance.IsComplete && !string.IsNullOrEmpty(outcome.NextNodeId))
        {
            instance.CurrentNodeId = outcome.NextNodeId;
        }
        else if (!instance.IsComplete && string.IsNullOrEmpty(outcome.NextNodeId) && outcome.IsEndEncounter)
        {
            instance.IsComplete = true;
        }
        
        instance.VisitedNodes.Add(instance.CurrentNodeId);
        
        // Handle auto-transition
        var newNode = GetCurrentNode(instance);
        if (newNode?.AutoTransition != null && !instance.IsComplete)
        {
            foreach (var effect in newNode.AutoTransition.Effects)
            {
                instance.PendingEffects.Add(effect);
            }
            if (!string.IsNullOrEmpty(newNode.AutoTransition.NextNodeId))
            {
                instance.CurrentNodeId = newNode.AutoTransition.NextNodeId;
            }
            if (newNode.AutoTransition.IsEndEncounter)
            {
                instance.IsComplete = true;
            }
        }
        
        return EncounterStepResult.Success(instance.CurrentNodeId, instance.IsComplete);
    }
    
    private bool EvaluateConditions(List<EncounterCondition> conditions, EncounterContext context)
    {
        if (conditions == null || conditions.Count == 0) return true;
        return conditions.All(c => c.Evaluate(context));
    }
}

public class EncounterStepResult
{
    public bool IsSuccess { get; set; }
    public string CurrentNodeId { get; set; }
    public bool IsComplete { get; set; }
    public string ErrorMessage { get; set; }
    
    public static EncounterStepResult Success(string nodeId, bool complete) => new()
        { IsSuccess = true, CurrentNodeId = nodeId, IsComplete = complete };
    
    public static EncounterStepResult Invalid(string error) => new()
        { IsSuccess = false, ErrorMessage = error };
}
```

### A.7: Test Encounter Example

```csharp
namespace FringeTactics;

public static class TestEncounters
{
    public static EncounterTemplate CreatePirateAmbush()
    {
        return new EncounterTemplate
        {
            Id = "pirate_ambush",
            Name = "Pirate Ambush",
            Tags = new HashSet<string> { "pirate", "combat", "travel" },
            EntryNodeId = "intro",
            Nodes = new Dictionary<string, EncounterNode>
            {
                ["intro"] = new EncounterNode
                {
                    Id = "intro",
                    TextKey = "encounter.pirate_ambush.intro",
                    Options = new List<EncounterOption>
                    {
                        new EncounterOption
                        {
                            Id = "fight",
                            TextKey = "encounter.pirate_ambush.fight",
                            Outcome = new EncounterOutcome
                            {
                                Effects = new List<EncounterEffect>
                                {
                                    EncounterEffect.ShipDamage(10),
                                    EncounterEffect.AddCredits(50)
                                },
                                NextNodeId = "victory"
                            }
                        },
                        new EncounterOption
                        {
                            Id = "surrender",
                            TextKey = "encounter.pirate_ambush.surrender",
                            Conditions = new List<EncounterCondition>
                            {
                                EncounterCondition.HasCredits(50)
                            },
                            Outcome = new EncounterOutcome
                            {
                                Effects = new List<EncounterEffect>
                                {
                                    EncounterEffect.LoseCredits(50)
                                },
                                NextNodeId = "surrendered"
                            }
                        },
                        new EncounterOption
                        {
                            Id = "flee",
                            TextKey = "encounter.pirate_ambush.flee",
                            Outcome = new EncounterOutcome
                            {
                                Effects = new List<EncounterEffect>
                                {
                                    EncounterEffect.TimeDelay(1),
                                    EncounterEffect.ShipDamage(5)
                                },
                                NextNodeId = "escaped"
                            }
                        }
                    }
                },
                ["victory"] = new EncounterNode
                {
                    Id = "victory",
                    TextKey = "encounter.pirate_ambush.victory",
                    AutoTransition = new EncounterOutcome { IsEndEncounter = true }
                },
                ["surrendered"] = new EncounterNode
                {
                    Id = "surrendered",
                    TextKey = "encounter.pirate_ambush.surrendered",
                    AutoTransition = new EncounterOutcome { IsEndEncounter = true }
                },
                ["escaped"] = new EncounterNode
                {
                    Id = "escaped",
                    TextKey = "encounter.pirate_ambush.escaped",
                    AutoTransition = new EncounterOutcome { IsEndEncounter = true }
                }
            }
        };
    }
}
```
