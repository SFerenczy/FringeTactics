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
Apply encounter outcomes to player state.

**Depends on:** MG3 ✅, EN1 ✅

**Status:** ⬜ Pending

**Key capabilities:**

- `ApplyEncounterOutcome(outcome)`:
  - Resource deltas (credits, fuel, cargo).
  - Crew injuries or trait changes.
  - Ship damage.
  - Time costs.
  - Faction reputation changes.
- Travel resource consumption:
  - Fuel consumption per travel segment.
  - Supplies consumption (if implemented).
- Unified outcome application:
  - Same patterns as `ApplyMissionOutput` from MG3.
  - Consistent event emission.

### Phase 4.1: EncounterOutcome Adapter

**Add to `CampaignState`:**

```csharp
/// <summary>
/// Apply the accumulated effects from an encounter.
/// </summary>
public void ApplyEncounterOutcome(EncounterOutcome outcome)
{
    foreach (var effect in outcome.Effects)
    {
        ApplyEncounterEffect(effect);
    }
    
    EventBus?.Publish(new EncounterCompletedEvent(outcome.EncounterId, outcome.NodePath));
}

private void ApplyEncounterEffect(EncounterEffect effect)
{
    switch (effect.Type)
    {
        case EncounterEffectType.AddResource:
            ApplyResourceDelta(effect.ResourceType, effect.Amount);
            break;
        case EncounterEffectType.CrewInjury:
            ApplyCrewInjury(effect.CrewId, effect.Severity);
            break;
        case EncounterEffectType.CrewTrait:
            if (effect.Add)
                AssignTrait(effect.CrewId, effect.TraitId);
            else
                RemoveTrait(effect.CrewId, effect.TraitId);
            break;
        case EncounterEffectType.ShipDamage:
            ApplyShipDamage(effect.Amount);
            break;
        case EncounterEffectType.FactionRep:
            ModifyFactionRep(effect.FactionId, effect.Amount);
            break;
        case EncounterEffectType.TimeDelay:
            AdvanceTime(effect.Days);
            break;
        case EncounterEffectType.AddCargo:
            AddCargo(effect.ItemId, effect.Amount);
            break;
        case EncounterEffectType.RemoveCargo:
            RemoveCargo(effect.ItemId, effect.Amount);
            break;
    }
}
```

### Phase 4.2: Travel Resource Consumption

**Add to `CampaignState`:**

```csharp
/// <summary>
/// Consume fuel for a travel segment.
/// </summary>
public bool ConsumeTravelFuel(int amount)
{
    if (Fuel < amount)
    {
        SimLog.Log($"[Campaign] Insufficient fuel: {Fuel}/{amount}");
        return false;
    }
    
    int oldFuel = Fuel;
    Fuel -= amount;
    
    EventBus?.Publish(new ResourceChangedEvent(
        ResourceTypes.Fuel, oldFuel, Fuel, -amount, "travel"));
    
    return true;
}

/// <summary>
/// Check if player can afford travel.
/// </summary>
public bool CanAffordTravel(TravelPlan plan)
{
    return Fuel >= plan.TotalFuelCost;
}
```

### Phase 4.3: Ship Damage System

**Add to `Ship` class:**

```csharp
public int MaxHull { get; set; } = 100;
public int CurrentHull { get; set; } = 100;

public bool IsDamaged => CurrentHull < MaxHull;
public bool IsDisabled => CurrentHull <= 0;
public float HullPercent => (float)CurrentHull / MaxHull;

public void TakeDamage(int amount)
{
    CurrentHull = Math.Max(0, CurrentHull - amount);
}

public void Repair(int amount)
{
    CurrentHull = Math.Min(MaxHull, CurrentHull + amount);
}
```

**Add to `CampaignState`:**

```csharp
public void ApplyShipDamage(int amount)
{
    int oldHull = Ship.CurrentHull;
    Ship.TakeDamage(amount);
    
    SimLog.Log($"[Campaign] Ship took {amount} damage: {oldHull} -> {Ship.CurrentHull}");
    
    EventBus?.Publish(new ShipDamagedEvent(amount, Ship.CurrentHull, Ship.MaxHull));
}
```

### Phase 4.4: Faction Reputation

**Add to `CampaignState`:**

```csharp
public void ModifyFactionRep(string factionId, int delta)
{
    if (!FactionRep.ContainsKey(factionId))
        FactionRep[factionId] = 0;
    
    int oldRep = FactionRep[factionId];
    FactionRep[factionId] = Math.Clamp(oldRep + delta, -100, 100);
    
    EventBus?.Publish(new FactionRepChangedEvent(
        factionId, oldRep, FactionRep[factionId], delta));
}
```

### Phase 4.5: Events

**Add to `Events.cs`:**

```csharp
public record EncounterCompletedEvent(string EncounterId, List<string> NodePath);
public record ShipDamagedEvent(int Damage, int CurrentHull, int MaxHull);
public record FactionRepChangedEvent(string FactionId, int OldRep, int NewRep, int Delta);
public record TravelFuelConsumedEvent(int Amount, int Remaining);
```

**Deliverables:**
- `ApplyEncounterOutcome` method.
- Effect application for all effect types.
- Travel fuel consumption.
- Ship damage system.
- Faction reputation modification.
- Events for all state changes.
- Unit tests for each effect type.
- Integration tests with Encounter domain.

**Files to modify:**
| File | Changes |
|------|---------|
| `src/sim/campaign/CampaignState.cs` | Add encounter/travel methods |
| `src/sim/campaign/Ship.cs` | Add hull damage system |
| `src/sim/Events.cs` | Add new events |
| `tests/sim/management/MG4*.cs` | Test files |

---

## G0/G1/G2 Scope Summary

| Milestone | Phase | Status | Notes |
|-----------|-------|--------|-------|
| MG0 | G0 | ✅ Complete | Concept only |
| MG1 | G1 | ✅ Complete | PlayerState & Crew |
| MG2 | G1 | ✅ Complete | Ship & Resources |
| MG3 | G1 | ✅ Complete | Tactical integration |
| MG4 | G2 | ⬜ Pending | Encounter integration |

---

## Dependencies

| Milestone | Depends On |
|-----------|------------|
| MG1 | MG0 |
| MG2 | MG1 |
| MG3 | MG2, Tactical M7 |
| MG4 | MG3, EN1 |

---

## Success Criteria

### MG4
- [ ] Encounter effects apply to player state
- [ ] Travel fuel consumption works
- [ ] Ship damage tracked
- [ ] Faction reputation changes
- [ ] All effects emit appropriate events
- [ ] Integration tests with Encounter domain pass
