# MG4 – Encounter Integration: Implementation Plan

**Status**: ✅ Complete  
**Depends on**: MG3 (Tactical Integration) ✅, EN1 (Runtime Core) ✅, EN2 (Skill Checks) ✅  
**Phase**: G2

---

## Overview

**Goal**: Apply encounter outcomes to player state, completing the loop between the Encounter domain and campaign state management. This bridges the gap where EN1/EN2 accumulate effects during encounters but don't apply them.

MG4 provides:
- `ApplyEncounterOutcome()` method to process accumulated effects
- Effect handlers for all `EffectType` values
- Travel fuel consumption integration
- Ship damage from encounters
- Faction reputation changes from encounters
- Time advancement from encounters
- Cargo add/remove operations
- Campaign flag system for encounter state tracking
- Events for all state changes

---

## Current State Assessment

### What We Have (from EN1, EN2, MG3)

| Component | Status | Location | Notes |
|-----------|--------|----------|-------|
| `EncounterInstance` | ✅ Complete | `src/sim/encounter/EncounterInstance.cs` | Has `PendingEffects` list |
| `EncounterEffect` | ✅ Complete | `src/sim/encounter/EncounterEffect.cs` | 13 effect types with factory methods |
| `EffectType` enum | ✅ Complete | `src/sim/encounter/EffectType.cs` | All types defined |
| `EncounterRunner` | ✅ Complete | `src/sim/encounter/EncounterRunner.cs` | Accumulates effects, doesn't apply |
| `CampaignState` resources | ✅ Complete | `src/sim/campaign/CampaignState.cs` | `SpendResource`, `AddResource` |
| `CampaignState` faction rep | ✅ Complete | `src/sim/campaign/CampaignState.cs` | `ModifyFactionRep` |
| `CampaignState` crew traits | ✅ Complete | `src/sim/campaign/CampaignState.cs` | `AssignTrait`, `RemoveTrait` |
| `Ship` hull | ✅ Complete | `src/sim/campaign/Ship.cs` | `TakeDamage`, `Repair` |
| `CampaignState.DamageShip` | ✅ Complete | `src/sim/campaign/CampaignState.cs` | Emits `ShipHullChangedEvent` |
| `CampaignTime` | ✅ Complete | `src/sim/CampaignTime.cs` | `AdvanceDays` |
| `Inventory` | ✅ Complete | `src/sim/campaign/Inventory.cs` | `AddItem`, `RemoveByDefId` |
| `CrewMember.AddInjury` | ✅ Complete | `src/sim/campaign/CrewMember.cs` | Injury system |

### What MG4 Requires vs What We Have

| MG4 Requirement | Current Status | Gap |
|-----------------|----------------|-----|
| `ApplyEncounterOutcome(instance)` | ❌ Missing | Main integration method |
| `ApplyEncounterEffect(effect)` | ❌ Missing | Per-effect dispatcher |
| Campaign flags system | ❌ Missing | `SetFlag` effect needs storage |
| `EncounterOutcomeAppliedEvent` | ❌ Missing | Event for UI feedback |
| Travel fuel consumption | ⚠️ Partial | Exists in `TravelExecutor`, needs campaign integration |
| Crew injury from encounters | ⚠️ Partial | `AddInjury` exists, need crew selection logic |
| Crew XP from encounters | ⚠️ Partial | `AddXp` exists, need crew selection logic |

---

## Architecture Decisions

### AD1: Effect Application Location

**Decision**: Add `ApplyEncounterOutcome()` to `CampaignState` as the single entry point.

**Rationale**:
- Follows MG3 pattern (`ApplyMissionOutput`)
- `CampaignState` already owns all state being modified
- Keeps encounter domain pure (accumulates effects, doesn't apply)
- Single point for event emission and logging

### AD2: Crew Selection for Effects

**Decision**: For effects targeting "a crew member" (injury, XP, trait), use the crew member who performed the skill check if available, otherwise select randomly from alive crew.

**Rationale**:
- Skill check results already track which crew member rolled
- Makes narrative sense (the person who tried gets hurt/rewarded)
- Random fallback for non-skill-check effects
- Stored in `EncounterInstance.ResolvedParameters["last_check_crew_id"]`

### AD3: Campaign Flags Storage

**Decision**: Add `Dictionary<string, bool> Flags` to `CampaignState` for encounter state tracking.

**Rationale**:
- Simple key-value storage for encounter flags
- Enables multi-encounter story arcs
- Serializable with existing save system
- Matches `SetFlag` effect type

### AD4: Effect Application Order

**Decision**: Apply effects in the order they were accumulated (FIFO).

**Rationale**:
- Predictable behavior
- Matches player expectation from encounter flow
- Resource checks happen at application time, not accumulation

### AD5: Failure Handling

**Decision**: Log and skip effects that can't be applied (e.g., remove cargo player doesn't have), don't fail the entire outcome.

**Rationale**:
- Graceful degradation
- Encounter templates may have conditional effects that don't always apply
- Better player experience than hard failures

---

## Implementation Steps

### Phase 1: Campaign Flags System (Priority: High)

#### Step 1.1: Add Flags to CampaignState

**File**: `src/sim/campaign/CampaignState.cs`

Add flag storage and methods:

```csharp
// Add to class fields
/// <summary>
/// Campaign flags for encounter state tracking and story arcs.
/// </summary>
public Dictionary<string, bool> Flags { get; set; } = new();

// Add methods
/// <summary>
/// Set a campaign flag.
/// </summary>
public void SetFlag(string flagId, bool value = true)
{
    if (string.IsNullOrEmpty(flagId)) return;
    
    bool oldValue = Flags.GetValueOrDefault(flagId, false);
    Flags[flagId] = value;
    
    if (oldValue != value)
    {
        SimLog.Log($"[Campaign] Flag '{flagId}' set to {value}");
        EventBus?.Publish(new CampaignFlagChangedEvent(flagId, oldValue, value));
    }
}

/// <summary>
/// Get a campaign flag value.
/// </summary>
public bool GetFlag(string flagId)
{
    return Flags.GetValueOrDefault(flagId, false);
}

/// <summary>
/// Check if a flag is set (true).
/// </summary>
public bool HasFlag(string flagId)
{
    return Flags.TryGetValue(flagId, out var value) && value;
}
```

**Acceptance Criteria**:
- [ ] `Flags` dictionary added to `CampaignState`
- [ ] `SetFlag`, `GetFlag`, `HasFlag` methods work
- [ ] `CampaignFlagChangedEvent` published on change
- [ ] Flags included in serialization

---

#### Step 1.2: Add Flag Event

**File**: `src/sim/Events.cs`

```csharp
/// <summary>
/// Published when a campaign flag changes.
/// </summary>
public readonly record struct CampaignFlagChangedEvent(
    string FlagId,
    bool OldValue,
    bool NewValue
);
```

**Acceptance Criteria**:
- [ ] Event type defined
- [ ] Follows existing event patterns

---

#### Step 1.3: Update Serialization for Flags

**File**: `src/sim/data/SaveData.cs`

Add to `CampaignStateData`:

```csharp
public Dictionary<string, bool> Flags { get; set; }
```

**File**: `src/sim/campaign/CampaignState.cs`

Update `GetState()`:
```csharp
Flags = new Dictionary<string, bool>(Flags)
```

Update `FromState()`:
```csharp
campaign.Flags = new Dictionary<string, bool>(data.Flags ?? new Dictionary<string, bool>());
```

**Acceptance Criteria**:
- [ ] Flags serialized in save data
- [ ] Flags restored on load
- [ ] Null-safe handling

---

### Phase 2: Core Effect Application (Priority: Critical)

#### Step 2.1: Add ApplyEncounterOutcome Method

**File**: `src/sim/campaign/CampaignState.cs`

```csharp
/// <summary>
/// Apply all pending effects from a completed encounter.
/// </summary>
/// <param name="instance">The completed encounter instance with pending effects.</param>
/// <returns>Number of effects successfully applied.</returns>
public int ApplyEncounterOutcome(EncounterInstance instance)
{
    if (instance == null || instance.PendingEffects == null)
    {
        SimLog.Log("[Campaign] ApplyEncounterOutcome: No instance or effects");
        return 0;
    }
    
    int applied = 0;
    
    foreach (var effect in instance.PendingEffects)
    {
        if (ApplyEncounterEffect(effect, instance))
        {
            applied++;
        }
    }
    
    SimLog.Log($"[Campaign] Applied {applied}/{instance.PendingEffects.Count} encounter effects");
    
    EventBus?.Publish(new EncounterOutcomeAppliedEvent(
        EncounterId: instance.InstanceId,
        TemplateId: instance.Template?.Id,
        EffectsApplied: applied,
        EffectsTotal: instance.PendingEffects.Count
    ));
    
    // Clear active encounter reference
    if (ActiveEncounter == instance)
    {
        ActiveEncounter = null;
    }
    
    return applied;
}
```

**Acceptance Criteria**:
- [ ] Iterates through all pending effects
- [ ] Tracks successful applications
- [ ] Publishes summary event
- [ ] Clears active encounter reference

---

#### Step 2.2: Add ApplyEncounterEffect Dispatcher

**File**: `src/sim/campaign/CampaignState.cs`

```csharp
/// <summary>
/// Apply a single encounter effect to campaign state.
/// </summary>
/// <param name="effect">The effect to apply.</param>
/// <param name="instance">The encounter instance (for context like crew selection).</param>
/// <returns>True if effect was applied successfully.</returns>
private bool ApplyEncounterEffect(EncounterEffect effect, EncounterInstance instance)
{
    try
    {
        switch (effect.Type)
        {
            case EffectType.AddResource:
                return ApplyResourceEffect(effect);
                
            case EffectType.CrewInjury:
                return ApplyCrewInjuryEffect(effect, instance);
                
            case EffectType.CrewXp:
                return ApplyCrewXpEffect(effect, instance);
                
            case EffectType.CrewTrait:
                return ApplyCrewTraitEffect(effect, instance);
                
            case EffectType.ShipDamage:
                return ApplyShipDamageEffect(effect);
                
            case EffectType.FactionRep:
                return ApplyFactionRepEffect(effect);
                
            case EffectType.SetFlag:
                return ApplySetFlagEffect(effect);
                
            case EffectType.TimeDelay:
                return ApplyTimeDelayEffect(effect);
                
            case EffectType.AddCargo:
                return ApplyAddCargoEffect(effect);
                
            case EffectType.RemoveCargo:
                return ApplyRemoveCargoEffect(effect);
                
            case EffectType.GotoNode:
            case EffectType.EndEncounter:
                // Flow effects are handled by EncounterRunner, not campaign
                return true;
                
            case EffectType.TriggerTactical:
                // EN3 - tactical trigger handled separately
                SimLog.Log($"[Campaign] TriggerTactical effect deferred (EN3)");
                return true;
                
            default:
                SimLog.Log($"[Campaign] Unknown effect type: {effect.Type}");
                return false;
        }
    }
    catch (Exception ex)
    {
        SimLog.Log($"[Campaign] Error applying effect {effect.Type}: {ex.Message}");
        return false;
    }
}
```

**Acceptance Criteria**:
- [ ] Dispatches to correct handler for each effect type
- [ ] Handles flow effects gracefully
- [ ] Catches and logs exceptions
- [ ] Returns success/failure status

---

#### Step 2.3: Add Resource Effect Handler

**File**: `src/sim/campaign/CampaignState.cs`

```csharp
/// <summary>
/// Apply a resource add/remove effect.
/// </summary>
private bool ApplyResourceEffect(EncounterEffect effect)
{
    string resourceType = effect.TargetId;
    int amount = effect.Amount;
    
    if (string.IsNullOrEmpty(resourceType))
    {
        SimLog.Log("[Campaign] Resource effect missing TargetId");
        return false;
    }
    
    if (amount > 0)
    {
        AddResource(resourceType, amount, "encounter");
        return true;
    }
    else if (amount < 0)
    {
        int absAmount = Math.Abs(amount);
        if (GetResource(resourceType) >= absAmount)
        {
            SpendResource(resourceType, absAmount, "encounter");
            return true;
        }
        else
        {
            // Not enough resource - apply what we can (drain to 0)
            int available = GetResource(resourceType);
            if (available > 0)
            {
                SpendResource(resourceType, available, "encounter_partial");
            }
            SimLog.Log($"[Campaign] Insufficient {resourceType} for encounter effect ({available}/{absAmount})");
            return true; // Partial success is still success
        }
    }
    
    return true; // amount == 0 is a no-op
}
```

**Acceptance Criteria**:
- [ ] Handles positive amounts (add)
- [ ] Handles negative amounts (spend)
- [ ] Graceful handling of insufficient resources
- [ ] Uses existing resource methods for events

---

#### Step 2.4: Add Crew Effect Handlers

**File**: `src/sim/campaign/CampaignState.cs`

```csharp
/// <summary>
/// Get the crew member to target for an encounter effect.
/// Uses the crew who performed the last skill check, or random alive crew.
/// </summary>
private CrewMember GetTargetCrewForEffect(EncounterInstance instance)
{
    // Check if a specific crew was involved in the last skill check
    if (instance?.ResolvedParameters != null &&
        instance.ResolvedParameters.TryGetValue("last_check_crew_id", out var crewIdStr) &&
        int.TryParse(crewIdStr, out var crewId))
    {
        var crew = GetCrewById(crewId);
        if (crew != null && !crew.IsDead)
        {
            return crew;
        }
    }
    
    // Fall back to random alive crew
    var aliveCrew = GetAliveCrew();
    if (aliveCrew.Count == 0) return null;
    
    int index = Rng?.Campaign?.NextInt(aliveCrew.Count) ?? 0;
    return aliveCrew[index];
}

/// <summary>
/// Apply a crew injury effect.
/// </summary>
private bool ApplyCrewInjuryEffect(EncounterEffect effect, EncounterInstance instance)
{
    var crew = GetTargetCrewForEffect(instance);
    if (crew == null)
    {
        SimLog.Log("[Campaign] No crew available for injury effect");
        return false;
    }
    
    string injuryType = effect.StringParam ?? InjuryTypes.Wounded;
    crew.AddInjury(injuryType);
    
    SimLog.Log($"[Campaign] {crew.Name} injured ({injuryType}) from encounter");
    EventBus?.Publish(new CrewInjuredEvent(crew.Id, crew.Name, injuryType));
    
    return true;
}

/// <summary>
/// Apply a crew XP effect.
/// </summary>
private bool ApplyCrewXpEffect(EncounterEffect effect, EncounterInstance instance)
{
    var crew = GetTargetCrewForEffect(instance);
    if (crew == null)
    {
        SimLog.Log("[Campaign] No crew available for XP effect");
        return false;
    }
    
    int xpAmount = effect.Amount;
    if (xpAmount <= 0) return true;
    
    int oldLevel = crew.Level;
    bool leveledUp = crew.AddXp(xpAmount);
    
    SimLog.Log($"[Campaign] {crew.Name} gained {xpAmount} XP from encounter");
    
    if (leveledUp)
    {
        SimLog.Log($"[Campaign] {crew.Name} leveled up to {crew.Level}!");
        EventBus?.Publish(new CrewLeveledUpEvent(crew.Id, crew.Name, oldLevel, crew.Level));
    }
    
    return true;
}

/// <summary>
/// Apply a crew trait add/remove effect.
/// </summary>
private bool ApplyCrewTraitEffect(EncounterEffect effect, EncounterInstance instance)
{
    var crew = GetTargetCrewForEffect(instance);
    if (crew == null)
    {
        SimLog.Log("[Campaign] No crew available for trait effect");
        return false;
    }
    
    string traitId = effect.TargetId;
    bool addTrait = effect.BoolParam;
    
    if (string.IsNullOrEmpty(traitId))
    {
        SimLog.Log("[Campaign] Trait effect missing TargetId");
        return false;
    }
    
    if (addTrait)
    {
        return AssignTrait(crew.Id, traitId);
    }
    else
    {
        return RemoveTrait(crew.Id, traitId);
    }
}
```

**Acceptance Criteria**:
- [ ] Crew selection uses skill check participant when available
- [ ] Falls back to random alive crew
- [ ] Injury effect applies correct injury type
- [ ] XP effect handles level-up
- [ ] Trait effect handles add/remove

---

#### Step 2.5: Add Ship and World Effect Handlers

**File**: `src/sim/campaign/CampaignState.cs`

```csharp
/// <summary>
/// Apply a ship damage effect.
/// </summary>
private bool ApplyShipDamageEffect(EncounterEffect effect)
{
    int damage = effect.Amount;
    if (damage <= 0) return true;
    
    DamageShip(damage, "encounter");
    return true;
}

/// <summary>
/// Apply a faction reputation effect.
/// </summary>
private bool ApplyFactionRepEffect(EncounterEffect effect)
{
    string factionId = effect.TargetId;
    int delta = effect.Amount;
    
    if (string.IsNullOrEmpty(factionId))
    {
        SimLog.Log("[Campaign] FactionRep effect missing TargetId");
        return false;
    }
    
    ModifyFactionRep(factionId, delta);
    return true;
}

/// <summary>
/// Apply a set flag effect.
/// </summary>
private bool ApplySetFlagEffect(EncounterEffect effect)
{
    string flagId = effect.TargetId;
    bool value = effect.BoolParam;
    
    if (string.IsNullOrEmpty(flagId))
    {
        SimLog.Log("[Campaign] SetFlag effect missing TargetId");
        return false;
    }
    
    SetFlag(flagId, value);
    return true;
}

/// <summary>
/// Apply a time delay effect.
/// </summary>
private bool ApplyTimeDelayEffect(EncounterEffect effect)
{
    int days = effect.Amount;
    if (days <= 0) return true;
    
    Time.AdvanceDays(days);
    SimLog.Log($"[Campaign] Time advanced {days} day(s) from encounter");
    return true;
}
```

**Acceptance Criteria**:
- [ ] Ship damage uses existing `DamageShip` method
- [ ] Faction rep uses existing `ModifyFactionRep` method
- [ ] Flag effect uses new `SetFlag` method
- [ ] Time delay uses existing `AdvanceDays` method

---

#### Step 2.6: Add Cargo Effect Handlers

**File**: `src/sim/campaign/CampaignState.cs`

```csharp
/// <summary>
/// Apply an add cargo effect.
/// </summary>
private bool ApplyAddCargoEffect(EncounterEffect effect)
{
    string itemDefId = effect.TargetId;
    int quantity = effect.Amount > 0 ? effect.Amount : 1;
    
    if (string.IsNullOrEmpty(itemDefId))
    {
        SimLog.Log("[Campaign] AddCargo effect missing TargetId");
        return false;
    }
    
    var item = AddItem(itemDefId, quantity);
    if (item == null)
    {
        SimLog.Log($"[Campaign] Could not add cargo {itemDefId} (no space?)");
        return false;
    }
    
    return true;
}

/// <summary>
/// Apply a remove cargo effect.
/// </summary>
private bool ApplyRemoveCargoEffect(EncounterEffect effect)
{
    string itemDefId = effect.TargetId;
    int quantity = effect.Amount > 0 ? effect.Amount : 1;
    
    if (string.IsNullOrEmpty(itemDefId))
    {
        SimLog.Log("[Campaign] RemoveCargo effect missing TargetId");
        return false;
    }
    
    if (!HasItem(itemDefId, quantity))
    {
        SimLog.Log($"[Campaign] Cannot remove {quantity}x {itemDefId} (not enough)");
        return false;
    }
    
    return RemoveItemByDef(itemDefId, quantity);
}
```

**Acceptance Criteria**:
- [ ] Add cargo uses existing `AddItem` method
- [ ] Remove cargo validates availability first
- [ ] Handles quantity correctly

---

#### Step 2.7: Add Outcome Applied Event

**File**: `src/sim/Events.cs`

```csharp
/// <summary>
/// Published when encounter outcome effects are applied to campaign state.
/// </summary>
public readonly record struct EncounterOutcomeAppliedEvent(
    string EncounterId,
    string TemplateId,
    int EffectsApplied,
    int EffectsTotal
);
```

**Acceptance Criteria**:
- [ ] Event type defined
- [ ] Includes summary statistics

---

### Phase 3: Travel Integration (Priority: High)

#### Step 3.1: Add Travel Fuel Consumption Method

**File**: `src/sim/campaign/CampaignState.cs`

```csharp
/// <summary>
/// Consume fuel for a travel segment.
/// </summary>
/// <param name="amount">Fuel to consume.</param>
/// <returns>True if fuel was consumed, false if insufficient.</returns>
public bool ConsumeTravelFuel(int amount)
{
    if (amount <= 0) return true;
    
    if (Fuel < amount)
    {
        SimLog.Log($"[Campaign] Insufficient fuel for travel: {Fuel}/{amount}");
        return false;
    }
    
    return SpendFuel(amount, "travel");
}

/// <summary>
/// Check if player can afford a travel plan.
/// </summary>
public bool CanAffordTravel(int fuelCost)
{
    return Fuel >= fuelCost;
}

/// <summary>
/// Check if player can afford a travel plan.
/// </summary>
public bool CanAffordTravel(TravelPlan plan)
{
    return plan != null && Fuel >= plan.TotalFuelCost;
}
```

**Acceptance Criteria**:
- [ ] `ConsumeTravelFuel` validates and spends fuel
- [ ] `CanAffordTravel` checks fuel availability
- [ ] Uses existing `SpendFuel` for events

---

### Phase 4: Serialization Updates (Priority: Medium)

#### Step 4.1: Update Save Data Structure

**File**: `src/sim/data/SaveData.cs`

Ensure `CampaignStateData` includes:
```csharp
public Dictionary<string, bool> Flags { get; set; }
public EncounterInstanceData ActiveEncounter { get; set; }
```

**File**: `src/sim/campaign/CampaignState.cs`

Update `GetState()` to include:
```csharp
Flags = new Dictionary<string, bool>(Flags),
ActiveEncounter = ActiveEncounter?.GetState()
```

Update `FromState()` to restore:
```csharp
campaign.Flags = new Dictionary<string, bool>(data.Flags ?? new Dictionary<string, bool>());

// Restore active encounter if present
if (data.ActiveEncounter != null && campaign.EncounterRegistry != null)
{
    var template = campaign.EncounterRegistry.Get(data.ActiveEncounter.TemplateId);
    if (template != null)
    {
        campaign.ActiveEncounter = EncounterInstance.FromState(data.ActiveEncounter, template);
    }
}
```

**Acceptance Criteria**:
- [ ] Flags serialized and restored
- [ ] Active encounter serialized and restored
- [ ] Null-safe handling

---

## MG4 Deliverables Checklist

### Phase 1: Campaign Flags System ✅
- [x] **1.1** Add `Flags` dictionary to `CampaignState`
- [x] **1.2** Add `CampaignFlagChangedEvent`
- [x] **1.3** Update serialization for flags
- [x] **1.4** Unit tests (25 tests in `MG4FlagTests.cs`)

### Phase 2: Core Effect Application ✅
- [x] **2.1** Add `ApplyEncounterOutcome` method
- [x] **2.2** Add `ApplyEncounterEffect` dispatcher
- [x] **2.3** Add resource effect handler
- [x] **2.4** Add crew effect handlers (injury, XP, trait)
- [x] **2.5** Add ship and world effect handlers
- [x] **2.6** Add cargo effect handlers
- [x] **2.7** Add `EncounterOutcomeAppliedEvent`
- [x] **2.8** Unit tests (47 tests in `MG4EffectTests.cs`)
- [x] **2.9** Bug fix: Added `Meds` to `GetResource`/`SetResource`

### Phase 3: Travel Integration ✅
- [x] **3.1** Add `ConsumeTravelFuel` method
- [x] **3.2** Add `CanAffordTravel(int)` method
- [x] **3.3** Add `CanAffordTravel(TravelPlan)` method
- [x] **3.4** Add `GetTravelBlockReason` method
- [x] **3.5** Unit tests (21 tests in `MG4IntegrationTests.cs`)

### Phase 4: Serialization Updates ✅
- [x] **4.1** Add `ActiveEncounter` to `CampaignStateData`
- [x] **4.2** Update `GetState()` to serialize active encounter
- [x] **4.3** Update `FromState()` to restore active encounter
- [x] **4.4** Serialization tests (4 tests added to `MG4IntegrationTests.cs`)

---

## Testing

### Test Files to Create

| File | Tests | Description |
|------|-------|-------------|
| `tests/sim/management/MG4FlagTests.cs` | ~15 tests | Flag system |
| `tests/sim/management/MG4EffectTests.cs` | ~40 tests | Effect application |
| `tests/sim/management/MG4IntegrationTests.cs` | ~15 tests | Full encounter flow |

### Unit Tests: Flag System (MG4FlagTests.cs)

```csharp
// Flag basic operations
[TestCase] SetFlag_SetsValue()
[TestCase] SetFlag_UpdatesExistingValue()
[TestCase] GetFlag_ReturnsFalseForUnset()
[TestCase] GetFlag_ReturnsTrueForSet()
[TestCase] HasFlag_ReturnsFalseForUnset()
[TestCase] HasFlag_ReturnsTrueForSet()
[TestCase] SetFlag_EmitsCampaignFlagChangedEvent()
[TestCase] SetFlag_NoEventIfValueUnchanged()

// Serialization
[TestCase] Flags_SerializedInGetState()
[TestCase] Flags_RestoredInFromState()
[TestCase] Flags_NullSafeOnLoad()
```

### Unit Tests: Effect Application (MG4EffectTests.cs)

```csharp
// Resource effects
[TestCase] ApplyResourceEffect_AddsCredits()
[TestCase] ApplyResourceEffect_AddsFuel()
[TestCase] ApplyResourceEffect_AddsParts()
[TestCase] ApplyResourceEffect_AddsMeds()
[TestCase] ApplyResourceEffect_RemovesCredits()
[TestCase] ApplyResourceEffect_PartialRemoveIfInsufficient()
[TestCase] ApplyResourceEffect_EmitsResourceChangedEvent()

// Crew effects
[TestCase] ApplyCrewInjuryEffect_InjuresTargetCrew()
[TestCase] ApplyCrewInjuryEffect_UsesSkillCheckCrew()
[TestCase] ApplyCrewInjuryEffect_FallsBackToRandomCrew()
[TestCase] ApplyCrewInjuryEffect_EmitsCrewInjuredEvent()
[TestCase] ApplyCrewXpEffect_GrantsXp()
[TestCase] ApplyCrewXpEffect_HandlesLevelUp()
[TestCase] ApplyCrewTraitEffect_AddsTrait()
[TestCase] ApplyCrewTraitEffect_RemovesTrait()

// Ship effects
[TestCase] ApplyShipDamageEffect_DamagesHull()
[TestCase] ApplyShipDamageEffect_EmitsShipHullChangedEvent()

// World effects
[TestCase] ApplyFactionRepEffect_ModifiesRep()
[TestCase] ApplyFactionRepEffect_EmitsFactionRepChangedEvent()
[TestCase] ApplySetFlagEffect_SetsFlag()
[TestCase] ApplyTimeDelayEffect_AdvancesTime()

// Cargo effects
[TestCase] ApplyAddCargoEffect_AddsItem()
[TestCase] ApplyAddCargoEffect_FailsIfNoSpace()
[TestCase] ApplyRemoveCargoEffect_RemovesItem()
[TestCase] ApplyRemoveCargoEffect_FailsIfNotEnough()

// Flow effects (no-op in campaign)
[TestCase] ApplyGotoNodeEffect_ReturnsTrue()
[TestCase] ApplyEndEncounterEffect_ReturnsTrue()
```

### Integration Tests (MG4IntegrationTests.cs)

```csharp
// Full encounter flow
[TestCase] ApplyEncounterOutcome_AppliesAllEffects()
[TestCase] ApplyEncounterOutcome_EmitsOutcomeAppliedEvent()
[TestCase] ApplyEncounterOutcome_ClearsActiveEncounter()
[TestCase] ApplyEncounterOutcome_CountsSuccessfulEffects()
[TestCase] ApplyEncounterOutcome_HandlesNullInstance()
[TestCase] ApplyEncounterOutcome_HandlesEmptyEffects()

// Travel integration
[TestCase] ConsumeTravelFuel_SpendsFuel()
[TestCase] ConsumeTravelFuel_FailsIfInsufficient()
[TestCase] CanAffordTravel_ChecksFuel()

// Serialization round-trip
[TestCase] EncounterState_PreservedAcrossSaveLoad()
[TestCase] Flags_PreservedAcrossSaveLoad()
```

---

## Manual Test Setup

### Test Scenario 1: Basic Encounter Effect Flow

**Prerequisites**:
- Campaign with 3+ crew members
- At least 100 credits, 50 fuel
- Ship with some hull damage capacity

**Steps**:

1. **Trigger a test encounter**
   - Use DevTools or travel to trigger an encounter
   - Select options that accumulate various effects

2. **Verify effect accumulation**
   - Check `ActiveEncounter.PendingEffects` count increases
   - Effects should not be applied yet

3. **Complete the encounter**
   - Reach an end node
   - Verify `ApplyEncounterOutcome` is called

4. **Verify effects applied**
   - Check resources changed correctly
   - Check crew injuries/XP/traits if applicable
   - Check ship hull if damage effect
   - Check faction rep if rep effect
   - Check flags if flag effect

5. **Verify events**
   - `EncounterOutcomeAppliedEvent` published
   - Individual effect events published (ResourceChangedEvent, etc.)

### Test Scenario 2: Skill Check Crew Targeting

**Prerequisites**:
- Campaign with 3+ crew members with different stats
- Encounter with skill check that can fail

**Steps**:

1. **Trigger encounter with skill check**
   - Note which crew member performs the check

2. **Fail the skill check (or succeed)**
   - Verify injury/XP goes to the crew who rolled

3. **Verify crew targeting**
   - The crew member who performed the check should receive the effect
   - Not a random crew member

### Test Scenario 3: Travel Fuel Integration

**Prerequisites**:
- Campaign with 50+ fuel
- Multi-hop travel plan

**Steps**:

1. **Plan travel**
   - Verify `CanAffordTravel` returns true

2. **Execute travel**
   - Verify fuel consumed per segment
   - Verify `ResourceChangedEvent` with reason "travel"

3. **Test insufficient fuel**
   - Reduce fuel below travel cost
   - Verify `CanAffordTravel` returns false
   - Verify travel blocked

---

## Files to Modify

| File | Changes |
|------|---------|
| `src/sim/campaign/CampaignState.cs` | Add flags, effect application methods |
| `src/sim/Events.cs` | Add `CampaignFlagChangedEvent`, `EncounterOutcomeAppliedEvent` |
| `src/sim/data/SaveData.cs` | Add `Flags` to `CampaignStateData` |
| `tests/sim/management/MG4FlagTests.cs` | New test file |
| `tests/sim/management/MG4EffectTests.cs` | New test file |
| `tests/sim/management/MG4IntegrationTests.cs` | New test file |
| `tests/sim/management/agents.md` | Update with MG4 test descriptions |

---

## Success Criteria

### MG4 Complete When:
- [ ] All encounter effect types apply correctly to campaign state
- [ ] Campaign flags system works for encounter state tracking
- [ ] Travel fuel consumption integrated
- [ ] All effects emit appropriate events
- [ ] Active encounter state serializes/deserializes
- [ ] Unit tests pass (70+ tests)
- [ ] Integration tests pass
- [ ] Manual test scenarios verified

---

## Dependencies on Other Milestones

| Milestone | Dependency Type | Notes |
|-----------|-----------------|-------|
| MG3 | Hard | Uses same patterns for effect application |
| EN1 | Hard | Provides `EncounterInstance`, `EncounterEffect` |
| EN2 | Soft | Skill check crew tracking for targeting |
| TV2 | Soft | Travel execution triggers encounters |
| GN3 | Soft | Encounter instantiation provides templates |

---

## Future Considerations (Not in MG4)

- **EN3 – Tactical Encounters**: `TriggerTactical` effect will need special handling
- **Encounter Chaining**: Multiple encounters in sequence
- **Effect Conditions**: Conditional effect application based on state
- **Effect Previews**: UI showing effects before confirmation
