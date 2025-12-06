# Encounter Domain Roadmap

This document defines the **implementation order** for the Encounter domain.

- G0/G1 have no Encounter implementation (missions only).
- Encounters come online in G2.
- Each milestone is a **vertical slice**.

---

## Overview of Milestones

1. **EN0 – Concept Finalization (G2)**
2. **EN1 – Runtime Core (G2)**
3. **EN2 – Skill Checks & Crew Integration (G2)**
4. **EN3 – Tactical Branching (G2+)**
5. **EN4 – Simulation Integration (G3)**

---

## EN0 – Concept Finalization (G2)

**Goal:**  
Finalize design decisions for encounter structure before implementation.

**Key deliverables:**

- Review DOMAIN.md and CAMPAIGN_FOUNDATIONS.md sections 2, 3, 4, 6.
- Define `EncounterTemplate` structure:
  - Nodes, options, conditions, outcomes.
  - Tags for selection (pirate, patrol, anomaly, social).
  - Required context fields.
- Define `EncounterInstance` structure:
  - Parameterized template with resolved values.
  - Current node, history, pending outcomes.
- Define condition types:
  - Resource checks (has fuel, has credits).
  - Crew checks (has trait, stat threshold).
  - World checks (faction rep, system tag).
  - Cargo checks (has illegal goods, cargo value).
- Define outcome types:
  - Resource deltas (credits, fuel, cargo).
  - Crew effects (injury, trait gain/loss, XP).
  - Ship effects (damage, module loss).
  - World effects (rep change, flag set).
  - Flow effects (goto node, end encounter, trigger tactical).
- Define initial encounter archetypes (8-12):
  - Pirate ambush, patrol inspection, distress signal.
  - Trader opportunity, smuggler contact, derelict discovery.
  - Faction agent, mysterious signal, crew event.
- Document skill check formula:
  - Base difficulty vs crew stat + trait bonuses.

**Why first:**  
Encounters are data-driven. Template structure must be stable before runtime.

**Status:** ⬜ Pending

---

## EN1 – Runtime Core (G2)

**Goal:**  
Implement the encounter state machine that runs encounter instances.

**Depends on:** EN0 (can proceed in parallel with minimal design)

**Status:** ✅ Complete

**Implementation:** See `EN1_IMPLEMENTATION.md` for detailed breakdown.

**Key capabilities:**

- `EncounterTemplate` class:
  - List of `EncounterNode`.
  - Entry node ID.
  - Tags for selection.
  - Required context keys.
- `EncounterNode` class:
  - Unique ID within template.
  - Text key (for localization/display).
  - List of `EncounterOption`.
  - Optional auto-transition (for narrative nodes).
- `EncounterOption` class:
  - Text key for option display.
  - List of `EncounterCondition` (visibility/availability).
  - Optional `SkillCheck` definition.
  - `EncounterOutcome` for success.
  - `EncounterOutcome` for failure (if skill check).
- `EncounterCondition` class:
  - Condition type enum.
  - Parameters (stat name, threshold, tag, etc.).
  - `Evaluate(EncounterContext) -> bool`.
- `EncounterOutcome` class:
  - List of `EncounterEffect`.
  - Next node ID (or null for end).
- `EncounterEffect` class:
  - Effect type enum.
  - Parameters (amount, target, etc.).
- `EncounterInstance` class:
  - Reference to template.
  - Resolved parameters (NPC names, cargo types, etc.).
  - Current node ID.
  - Accumulated effects (pending application).
  - History of visited nodes.
- `EncounterRunner` class:
  - Input: `EncounterInstance`, `EncounterContext`.
  - Methods:
    - `GetCurrentNode() -> EncounterNode`.
    - `GetAvailableOptions() -> List<EncounterOption>`.
    - `SelectOption(optionIndex) -> EncounterStepResult`.
    - `IsComplete() -> bool`.
    - `GetPendingEffects() -> List<EncounterEffect>`.
- `EncounterContext` class:
  - Player state snapshot (crew, resources, cargo).
  - World state snapshot (system, faction rep).
  - RNG stream for checks.

**Deliverables:**
- All encounter data classes.
- `EncounterRunner` state machine.
- Condition evaluation system.
- Effect accumulation (not application—that's MG4).
- Unit tests for state machine flow.
- 3-5 test templates for validation.

**Files to create:**
| File | Purpose |
|------|---------|
| `src/sim/encounter/EncounterTemplate.cs` | Template definition |
| `src/sim/encounter/EncounterNode.cs` | Single node in encounter |
| `src/sim/encounter/EncounterOption.cs` | Choice with conditions |
| `src/sim/encounter/EncounterCondition.cs` | Condition evaluation |
| `src/sim/encounter/EncounterOutcome.cs` | Outcome with effects |
| `src/sim/encounter/EncounterEffect.cs` | Single effect |
| `src/sim/encounter/EncounterInstance.cs` | Runtime instance |
| `src/sim/encounter/EncounterContext.cs` | Evaluation context |
| `src/sim/encounter/EncounterRunner.cs` | State machine |
| `tests/sim/encounter/EN1*.cs` | Test files |

---

## EN2 – Skill Checks & Crew Integration (G2)

**Goal:**  
Implement skill checks that use crew stats and traits.

**Depends on:** EN1 ✅, MG1 (Crew Stats) ✅

**Status:** ✅ Complete

**Implementation:** See `EN2_IMPLEMENTATION.md` for detailed breakdown.

**Key capabilities:**

- `SkillCheck` static class with resolution logic
- `SkillCheckResult` class capturing all check details
- Automatic best-crew selection for checks
- Trait-based bonuses (+2) and penalties (-2)
- Success chance preview for UI
- Skill check events for feedback

**Skill Check Formula:**
```
roll = rng.NextInt(1, 11)  // 1-10 inclusive
statValue = crew.GetStat(stat)
traitBonus = +2 per bonus trait, -2 per penalty trait
total = roll + statValue + traitBonus
success = total >= difficulty
margin = total - difficulty
```

**Deliverables:**
- `SkillCheck.cs` – Resolution logic
- `SkillCheckResult.cs` – Result data structure
- `SkillCheckResolvedEvent` – Event for UI
- Updated `EncounterRunner.ResolveOutcome()` – Skill check integration
- Updated `EncounterContext` – Trait query methods
- Test encounters with skill checks
- Unit tests (~25-30 tests)

**Files to create:**
| File | Purpose |
|------|---------|
| `src/sim/encounter/SkillCheck.cs` | Skill check resolution |
| `src/sim/encounter/SkillCheckResult.cs` | Result data structure |
| `tests/sim/encounter/EN2SkillCheckTests.cs` | Unit tests |
| `tests/sim/encounter/EN2RunnerIntegrationTests.cs` | Integration tests |

**Files to modify:**
| File | Changes |
|------|---------|
| `src/sim/encounter/EncounterRunner.cs` | Update `ResolveOutcome()` |
| `src/sim/encounter/EncounterContext.cs` | Add trait queries |
| `src/sim/encounter/TestEncounters.cs` | Add skill check encounters |
| `src/sim/Events.cs` | Add `SkillCheckResolvedEvent` |

---

## EN3 – Tactical Branching (G2+)

**Goal:**  
Allow encounters to branch into tactical missions.

**Depends on:** EN2 ✅, MG3 (Tactical Integration) ✅

**Key capabilities:**

- `TriggerTacticalEffect`:
  - Effect type that pauses encounter.
  - Specifies mission parameters (enemy type, objective).
  - Encounter resumes after tactical with result.
- Tactical result integration:
  - Encounter can branch based on tactical outcome.
  - Win/lose/retreat → different nodes.
- Mission context from encounter:
  - Enemy faction from encounter context.
  - Location from travel context.
  - Special conditions (ambush, reinforcements).

**Deliverables:**
- `TriggerTacticalEffect` implementation.
- Encounter pause/resume for tactical.
- Tactical result → encounter branching.
- Integration tests.

**Status:** ⬜ Pending (G2+, can be deferred)

---

## EN4 – Simulation Integration (G3)

**Goal:**  
Encounters respond to and affect simulation state.

**Key capabilities:**

- Encounter selection uses simulation metrics:
  - High `criminal_activity` → more pirate encounters.
  - High `security_level` → more patrol encounters.
  - Low `stability` → more desperate encounters.
- Encounter outcomes affect simulation:
  - Helping faction → rep increase.
  - Piracy → `criminal_activity` increase.
  - Reporting smugglers → `security_level` boost.
- Faction-specific encounter variants:
  - Same template, different flavor per faction.
  - Faction-specific options and outcomes.

**Deliverables:**
- Simulation-aware encounter selection weights.
- Simulation effect types in outcomes.
- Integration tests with Simulation.

**Status:** ⬜ Pending (G3)

---

## G2 Scope Summary

| Milestone | Phase | Notes |
|-----------|-------|-------|
| EN0 | G2 | Concept finalization |
| EN1 | G2 | Runtime core |
| EN2 | G2 | Skill checks |
| EN3 | G2+ | Tactical branching (can defer) |
| EN4 | G3 | Simulation integration |

---

## Encounter Template Format

Templates are data-driven. Example structure:

```csharp
public class EncounterTemplate
{
    public string Id { get; set; }
    public string Name { get; set; }
    public HashSet<string> Tags { get; set; } = new();
    public List<string> RequiredContextKeys { get; set; } = new();
    public string EntryNodeId { get; set; }
    public Dictionary<string, EncounterNode> Nodes { get; set; } = new();
}
```

Example encounter (Pirate Ambush):
```
Template: pirate_ambush
Tags: [pirate, combat, travel]
Entry: intro

Nodes:
  intro:
    text: "Pirates emerge from an asteroid field, weapons hot."
    options:
      - text: "Fight them off"
        outcome: { effects: [trigger_tactical: pirate_skirmish], next: combat_result }
      - text: "Try to outrun them" 
        check: { stat: Reflexes, difficulty: 6 }
        success: { effects: [time_delay: 1], next: escaped }
        failure: { effects: [ship_damage: 20], next: forced_combat }
      - text: "Surrender cargo"
        condition: { type: has_cargo, min_value: 100 }
        outcome: { effects: [lose_cargo: 50%], next: surrendered }
  
  escaped:
    text: "You outmaneuver the pirates and escape."
    auto_end: true
    
  surrendered:
    text: "The pirates take what they want and leave."
    auto_end: true
```

---

## Condition Types (EN1)

| Type | Parameters | Description |
|------|------------|-------------|
| `has_resource` | resource, min | Player has minimum resource |
| `has_trait` | traitId | Any crew has trait |
| `has_cargo` | tag, min_value | Cargo matches criteria |
| `faction_rep` | factionId, min | Reputation threshold |
| `system_tag` | tag | Current system has tag |
| `crew_stat` | stat, min | Best crew stat meets threshold |

---

## Effect Types (EN1)

| Type | Parameters | Description |
|------|------------|-------------|
| `add_resource` | resource, amount | Add/remove resources |
| `crew_injury` | severity | Injure random/specific crew |
| `crew_xp` | amount | Grant XP to crew |
| `crew_trait` | traitId, add/remove | Modify crew traits |
| `ship_damage` | amount | Damage ship hull |
| `faction_rep` | factionId, delta | Change reputation |
| `set_flag` | flagId, value | Set campaign flag |
| `time_delay` | days | Advance time |
| `trigger_tactical` | missionType, params | Start tactical mission |
| `add_cargo` | itemId, amount | Add cargo items |
| `remove_cargo` | itemId, amount | Remove cargo items |

---

## Dependencies

| Milestone | Depends On |
|-----------|------------|
| EN1 | EN0 |
| EN2 | EN1, MG1 |
| EN3 | EN2, MG3 |
| EN4 | EN2, Simulation domain |

---

## Success Criteria

### EN1
- [ ] Encounters run as state machines
- [ ] Conditions evaluate correctly
- [ ] Effects accumulate without application
- [ ] Multiple paths through encounters work
- [ ] Deterministic given same inputs and choices

### EN2
- [ ] Skill checks use crew stats
- [ ] Traits affect check bonuses
- [ ] Traits gate option visibility
- [ ] Best crew auto-selected for checks

### EN3
- [ ] Encounters can trigger tactical missions
- [ ] Tactical results branch encounter flow
- [ ] Encounter resumes after tactical

### EN4
- [ ] Encounter selection uses simulation metrics
- [ ] Encounter outcomes affect simulation
