# EN2 – Skill Checks & Crew Integration: Implementation Plan

**Status**: ✅ Complete  
**Depends on**: EN1 (Runtime Core) ✅, MG1 (Crew Stats) ✅  
**Phase**: G2

---

## Overview

**Goal**: Implement skill checks that use crew stats and traits to resolve encounter options with uncertain outcomes. This transforms encounters from purely conditional choices into risk/reward decisions where crew composition matters.

EN2 provides:
- `SkillCheck` class with resolution logic
- Crew stat integration for check resolution
- Trait-based bonuses and penalties
- Automatic best-crew selection for checks
- Success/failure outcome branching
- Skill check result events for UI feedback

---

## Current State Assessment

### What We Have (from EN1, MG1)

| Component | Status | Location | Notes |
|-----------|--------|----------|-------|
| `SkillCheckDef` | ✅ Stub | `src/sim/encounter/SkillCheckDef.cs` | Has Stat, Difficulty, BonusTraits, PenaltyTraits |
| `EncounterOption` | ✅ Complete | `src/sim/encounter/EncounterOption.cs` | Has SkillCheck, SuccessOutcome, FailureOutcome |
| `EncounterRunner` | ✅ Complete | `src/sim/encounter/EncounterRunner.cs` | Stub in `ResolveOutcome()` always succeeds |
| `EncounterContext` | ✅ Complete | `src/sim/encounter/EncounterContext.cs` | Has `GetBestCrewForStat()`, `GetBestCrewStat()` |
| `CrewSnapshot` | ✅ Complete | `src/sim/encounter/CrewSnapshot.cs` | Has all stats, traits |
| `CrewMember` | ✅ Complete | `src/sim/campaign/CrewMember.cs` | `GetEffectiveStat()`, `GetTraitModifier()` |
| `TraitRegistry` | ✅ Complete | `src/sim/campaign/TraitRegistry.cs` | 15+ traits with stat modifiers |
| `CrewStatType` | ✅ Complete | `src/sim/campaign/StatType.cs` | Grit, Reflexes, Aim, Tech, Savvy, Resolve |
| `RngStream` | ✅ Complete | `src/sim/RngService.cs` | Deterministic RNG |

### EN2 Implementation Status

| Requirement | Status | Location |
|-------------|--------|----------|
| `SkillCheck` resolution class | ✅ Complete | `src/sim/encounter/SkillCheck.cs` |
| `SkillCheckResult` class | ✅ Complete | `src/sim/encounter/SkillCheckResult.cs` |
| Trait bonus calculation | ✅ Complete | In `SkillCheck.CalculateTraitBonus()` |
| `EncounterRunner` skill check integration | ✅ Complete | `ResolveOutcome()` updated |
| `EncounterContext` trait query methods | ✅ Complete | Added 6 new methods |
| Skill check events | ✅ Complete | `SkillCheckResolvedEvent` in Events.cs |
| Test encounters with skill checks | ✅ Complete | 5 new encounters in `TestEncounters.cs` |
| Unit tests | ✅ Complete | 43 tests in `EN2*.cs` files |

---

## Architecture Decisions

### AD1: Skill Check Formula

**Decision**: Use a d10 roll + stat + trait bonus vs difficulty threshold.

```
roll = rng.Next(1, 11)           // 1-10 inclusive
statValue = crew.GetStat(stat)   // Effective stat (base + trait modifiers)
traitBonus = CalculateTraitBonus(crew, check.BonusTraits, check.PenaltyTraits)
total = roll + statValue + traitBonus
success = total >= difficulty
margin = total - difficulty
```

**Rationale**:
- Simple and transparent for players
- Stat range (0-10) + roll (1-10) = 1-20 range
- Difficulty 10 = 50% chance with stat 5, no bonuses
- Trait bonuses provide meaningful differentiation (+2 to +4)
- Margin enables graduated outcomes (critical success/failure)

### AD2: Automatic Best Crew Selection

**Decision**: By default, select the crew member with the highest effective stat for the check. Player crew selection is a future enhancement.

**Rationale**:
- Simplifies initial implementation
- Matches player expectation (use best person for the job)
- Injured/dead crew already excluded via `GetAliveCrew()` in context creation
- Player choice can be added in EN2+ without breaking changes

### AD3: Trait Bonus System

**Decision**: Specific traits listed in `SkillCheckDef.BonusTraits` grant +2, traits in `PenaltyTraits` give -2.

**Rationale**:
- Explicit per-check bonuses allow narrative-appropriate modifiers
- "smuggler" trait helps with smuggling checks, not all Savvy checks
- Flat +2/-2 is significant but not overwhelming
- Keeps trait system simple and predictable

### AD4: Skill Check Result Structure

**Decision**: Return a `SkillCheckResult` with all details for UI display and logging.

**Rationale**:
- UI needs to show: who rolled, what they rolled, bonuses, outcome
- Enables "show your work" transparency
- Supports future features (critical success animations, etc.)

---

## Implementation Steps

### Phase 1: Core Skill Check Classes

#### Step 1.1: Create SkillCheckResult Class

**File**: `src/sim/encounter/SkillCheckResult.cs`

```csharp
namespace FringeTactics;

/// <summary>
/// Result of a skill check resolution.
/// Contains all information needed for UI display and logging.
/// </summary>
public class SkillCheckResult
{
    /// <summary>
    /// Whether the check succeeded.
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// The crew member who performed the check.
    /// </summary>
    public CrewSnapshot Crew { get; set; }
    
    /// <summary>
    /// The stat used for the check.
    /// </summary>
    public CrewStatType Stat { get; set; }
    
    /// <summary>
    /// Base difficulty of the check.
    /// </summary>
    public int Difficulty { get; set; }
    
    /// <summary>
    /// The d10 roll result (1-10).
    /// </summary>
    public int Roll { get; set; }
    
    /// <summary>
    /// Crew's effective stat value.
    /// </summary>
    public int StatValue { get; set; }
    
    /// <summary>
    /// Bonus from matching traits.
    /// </summary>
    public int TraitBonus { get; set; }
    
    /// <summary>
    /// Total = Roll + StatValue + TraitBonus.
    /// </summary>
    public int Total => Roll + StatValue + TraitBonus;
    
    /// <summary>
    /// Margin = Total - Difficulty. Positive = success margin, negative = failure margin.
    /// </summary>
    public int Margin => Total - Difficulty;
    
    /// <summary>
    /// Whether this was a critical success (margin >= 5).
    /// </summary>
    public bool IsCriticalSuccess => Success && Margin >= 5;
    
    /// <summary>
    /// Whether this was a critical failure (margin <= -5).
    /// </summary>
    public bool IsCriticalFailure => !Success && Margin <= -5;
    
    /// <summary>
    /// Traits that contributed to the bonus.
    /// </summary>
    public List<string> AppliedBonusTraits { get; set; } = new();
    
    /// <summary>
    /// Traits that contributed to the penalty.
    /// </summary>
    public List<string> AppliedPenaltyTraits { get; set; } = new();
}
```

**Acceptance Criteria**:
- [x] All check details captured
- [x] `Total` and `Margin` computed correctly
- [x] Critical success/failure thresholds defined
- [x] Applied traits tracked for UI

---

#### Step 1.2: Create SkillCheck Resolution Class

**File**: `src/sim/encounter/SkillCheck.cs`

```csharp
using System.Collections.Generic;
using System.Linq;

namespace FringeTactics;

/// <summary>
/// Resolves skill checks against crew stats.
/// Stateless service - all inputs explicit.
/// </summary>
public static class SkillCheck
{
    /// <summary>
    /// Bonus granted per matching trait.
    /// </summary>
    public const int TraitBonusAmount = 2;
    
    /// <summary>
    /// Penalty per matching penalty trait.
    /// </summary>
    public const int TraitPenaltyAmount = 2;
    
    /// <summary>
    /// Resolve a skill check using the best available crew member.
    /// </summary>
    public static SkillCheckResult Resolve(
        SkillCheckDef check,
        EncounterContext context,
        RngStream rng)
    {
        if (check == null || context == null || rng == null)
        {
            return new SkillCheckResult { Success = false };
        }
        
        // Find best crew for this check
        var crew = SelectBestCrew(check, context);
        if (crew == null)
        {
            return new SkillCheckResult 
            { 
                Success = false,
                Stat = check.Stat,
                Difficulty = check.Difficulty
            };
        }
        
        return ResolveWithCrew(check, crew, rng);
    }
    
    /// <summary>
    /// Resolve a skill check with a specific crew member.
    /// </summary>
    public static SkillCheckResult ResolveWithCrew(
        SkillCheckDef check,
        CrewSnapshot crew,
        RngStream rng)
    {
        if (check == null || crew == null || rng == null)
        {
            return new SkillCheckResult { Success = false };
        }
        
        // Roll d10 (1-10)
        int roll = rng.NextInt(1, 11);
        
        // Get stat value
        int statValue = crew.GetStat(check.Stat);
        
        // Calculate trait bonus
        var (traitBonus, appliedBonus, appliedPenalty) = CalculateTraitBonus(crew, check);
        
        // Calculate total
        int total = roll + statValue + traitBonus;
        bool success = total >= check.Difficulty;
        
        return new SkillCheckResult
        {
            Success = success,
            Crew = crew,
            Stat = check.Stat,
            Difficulty = check.Difficulty,
            Roll = roll,
            StatValue = statValue,
            TraitBonus = traitBonus,
            AppliedBonusTraits = appliedBonus,
            AppliedPenaltyTraits = appliedPenalty
        };
    }
    
    /// <summary>
    /// Select the best crew member for a skill check.
    /// Considers base stat + potential trait bonuses.
    /// </summary>
    public static CrewSnapshot SelectBestCrew(SkillCheckDef check, EncounterContext context)
    {
        if (context?.Crew == null || context.Crew.Count == 0)
            return null;
        
        return context.Crew
            .OrderByDescending(c => GetEffectiveCheckValue(c, check))
            .FirstOrDefault();
    }
    
    /// <summary>
    /// Calculate effective value for crew selection (stat + trait bonus).
    /// </summary>
    public static int GetEffectiveCheckValue(CrewSnapshot crew, SkillCheckDef check)
    {
        if (crew == null || check == null) return 0;
        
        int statValue = crew.GetStat(check.Stat);
        var (traitBonus, _, _) = CalculateTraitBonus(crew, check);
        return statValue + traitBonus;
    }
    
    /// <summary>
    /// Calculate trait bonus/penalty for a check.
    /// Returns (netBonus, appliedBonusTraits, appliedPenaltyTraits).
    /// </summary>
    public static (int bonus, List<string> bonusTraits, List<string> penaltyTraits) 
        CalculateTraitBonus(CrewSnapshot crew, SkillCheckDef check)
    {
        var appliedBonus = new List<string>();
        var appliedPenalty = new List<string>();
        int bonus = 0;
        
        if (crew?.TraitIds == null || check == null)
            return (0, appliedBonus, appliedPenalty);
        
        // Check bonus traits
        foreach (var traitId in check.BonusTraits ?? new List<string>())
        {
            if (crew.TraitIds.Contains(traitId))
            {
                bonus += TraitBonusAmount;
                appliedBonus.Add(traitId);
            }
        }
        
        // Check penalty traits
        foreach (var traitId in check.PenaltyTraits ?? new List<string>())
        {
            if (crew.TraitIds.Contains(traitId))
            {
                bonus -= TraitPenaltyAmount;
                appliedPenalty.Add(traitId);
            }
        }
        
        return (bonus, appliedBonus, appliedPenalty);
    }
    
    /// <summary>
    /// Preview the success chance for a skill check (for UI).
    /// Returns percentage (0-100).
    /// </summary>
    public static int GetSuccessChance(SkillCheckDef check, EncounterContext context)
    {
        var crew = SelectBestCrew(check, context);
        if (crew == null) return 0;
        
        return GetSuccessChanceWithCrew(check, crew);
    }
    
    /// <summary>
    /// Calculate success chance for a specific crew member.
    /// </summary>
    public static int GetSuccessChanceWithCrew(SkillCheckDef check, CrewSnapshot crew)
    {
        if (check == null || crew == null) return 0;
        
        int statValue = crew.GetStat(check.Stat);
        var (traitBonus, _, _) = CalculateTraitBonus(crew, check);
        int baseValue = statValue + traitBonus;
        
        // Need to roll (difficulty - baseValue) or higher on d10
        // Roll range is 1-10
        int neededRoll = check.Difficulty - baseValue;
        
        if (neededRoll <= 1) return 100;  // Auto-success
        if (neededRoll > 10) return 0;    // Auto-fail
        
        // Chance = (11 - neededRoll) / 10 * 100
        return (11 - neededRoll) * 10;
    }
}
```

**Acceptance Criteria**:
- [ ] `Resolve()` uses best crew automatically
- [ ] `ResolveWithCrew()` allows specific crew selection
- [ ] Trait bonuses calculated correctly
- [ ] Success chance preview accurate
- [ ] Deterministic with same RNG seed

---

### Phase 2: EncounterRunner Integration

#### Step 2.1: Update ResolveOutcome Method

**File**: `src/sim/encounter/EncounterRunner.cs`

Update the `ResolveOutcome` method to handle skill checks:

```csharp
private EncounterOutcome ResolveOutcome(EncounterOption option, EncounterContext context)
{
    if (option == null) return null;

    // Handle skill check options
    if (option.HasSkillCheck)
    {
        var rng = context.Rng ?? new RngStream(0);
        var result = SkillCheck.Resolve(option.SkillCheck, context, rng);
        
        // Store result for event emission
        lastSkillCheckResult = result;
        
        // Emit skill check event
        eventBus?.Publish(new SkillCheckResolvedEvent(
            result.Crew?.Name ?? "Unknown",
            result.Stat.ToString(),
            result.Difficulty,
            result.Roll,
            result.StatValue,
            result.TraitBonus,
            result.Total,
            result.Success,
            result.Margin
        ));
        
        return result.Success 
            ? option.SuccessOutcome ?? option.Outcome 
            : option.FailureOutcome ?? option.Outcome;
    }

    return option.Outcome;
}
```

**Acceptance Criteria**:
- [ ] Skill checks resolved when `HasSkillCheck` is true
- [ ] Success uses `SuccessOutcome`, failure uses `FailureOutcome`
- [ ] Falls back to `Outcome` if specific outcome is null
- [ ] Event emitted with check details

---

### Phase 3: Events

#### Step 3.1: Add Skill Check Event

**File**: `src/sim/Events.cs` (add to existing)

```csharp
// === Skill Check Events ===

/// <summary>
/// A skill check has been resolved during an encounter.
/// </summary>
public record SkillCheckResolvedEvent(
    string CrewName,
    string StatName,
    int Difficulty,
    int Roll,
    int StatValue,
    int TraitBonus,
    int Total,
    bool Success,
    int Margin
);
```

**Acceptance Criteria**:
- [ ] Event contains all display-relevant information
- [ ] Event is a record (immutable)

---

### Phase 4: EncounterContext Enhancements

#### Step 4.1: Add Trait Query Methods

**File**: `src/sim/encounter/EncounterContext.cs` (add methods)

```csharp
/// <summary>
/// Get all unique traits across all crew members.
/// </summary>
public HashSet<string> GetAllCrewTraits()
{
    var traits = new HashSet<string>();
    foreach (var crew in Crew)
    {
        foreach (var trait in crew.TraitIds ?? new List<string>())
        {
            traits.Add(trait);
        }
    }
    return traits;
}

/// <summary>
/// Check if any crew member has a specific trait.
/// </summary>
public bool AnyCrewHasTrait(string traitId)
{
    return Crew.Any(c => c.TraitIds?.Contains(traitId) ?? false);
}

/// <summary>
/// Get crew members with a specific trait.
/// </summary>
public List<CrewSnapshot> GetCrewWithTrait(string traitId)
{
    return Crew.Where(c => c.TraitIds?.Contains(traitId) ?? false).ToList();
}
```

**Acceptance Criteria**:
- [ ] `GetAllCrewTraits()` returns union of all crew traits
- [ ] `AnyCrewHasTrait()` checks across all crew
- [ ] `GetCrewWithTrait()` filters crew by trait

---

### Phase 5: Test Encounters with Skill Checks

#### Step 5.1: Add Skill Check Test Encounters

**File**: `src/sim/encounter/TestEncounters.cs` (add methods)

```csharp
/// <summary>
/// Encounter with skill check options.
/// Tests skill check resolution.
/// </summary>
public static EncounterTemplate CreateSkillCheckEncounter()
{
    return new EncounterTemplate
    {
        Id = "test_skillcheck",
        Name = "Skill Check Test Encounter",
        Tags = new HashSet<string> { "test", "skillcheck" },
        EntryNodeId = "start",
        Nodes = new Dictionary<string, EncounterNode>
        {
            ["start"] = new EncounterNode
            {
                Id = "start",
                TextKey = "test.skillcheck.start",
                Options = new List<EncounterOption>
                {
                    new EncounterOption
                    {
                        Id = "hack_terminal",
                        TextKey = "test.skillcheck.hack",
                        SkillCheck = new SkillCheckDef
                        {
                            Stat = CrewStatType.Tech,
                            Difficulty = 12,
                            BonusTraits = new List<string> { "corporate", "spacer" }
                        },
                        SuccessOutcome = EncounterOutcome.GotoWith("success",
                            EncounterEffect.AddCredits(200)),
                        FailureOutcome = EncounterOutcome.GotoWith("failure",
                            EncounterEffect.TimeDelay(1))
                    },
                    new EncounterOption
                    {
                        Id = "talk_guard",
                        TextKey = "test.skillcheck.talk",
                        SkillCheck = new SkillCheckDef
                        {
                            Stat = CrewStatType.Savvy,
                            Difficulty = 10,
                            BonusTraits = new List<string> { "smuggler", "empathetic" },
                            PenaltyTraits = new List<string> { "reckless" }
                        },
                        SuccessOutcome = EncounterOutcome.Goto("success"),
                        FailureOutcome = EncounterOutcome.GotoWith("caught",
                            EncounterEffect.FactionRep("security", -10))
                    },
                    new EncounterOption
                    {
                        Id = "force_entry",
                        TextKey = "test.skillcheck.force",
                        Outcome = EncounterOutcome.GotoWith("alarm",
                            EncounterEffect.ShipDamage(15))
                    }
                }
            },
            ["success"] = new EncounterNode
            {
                Id = "success",
                TextKey = "test.skillcheck.success",
                AutoTransition = EncounterOutcome.EndWith(
                    EncounterEffect.AddCredits(100))
            },
            ["failure"] = new EncounterNode
            {
                Id = "failure",
                TextKey = "test.skillcheck.failure",
                AutoTransition = EncounterOutcome.End()
            },
            ["caught"] = new EncounterNode
            {
                Id = "caught",
                TextKey = "test.skillcheck.caught",
                AutoTransition = EncounterOutcome.End()
            },
            ["alarm"] = new EncounterNode
            {
                Id = "alarm",
                TextKey = "test.skillcheck.alarm",
                AutoTransition = EncounterOutcome.End()
            }
        }
    };
}

/// <summary>
/// Encounter with easy skill check (for testing guaranteed success).
/// </summary>
public static EncounterTemplate CreateEasySkillCheckEncounter()
{
    return new EncounterTemplate
    {
        Id = "test_easy_skillcheck",
        Name = "Easy Skill Check Test",
        Tags = new HashSet<string> { "test" },
        EntryNodeId = "start",
        Nodes = new Dictionary<string, EncounterNode>
        {
            ["start"] = new EncounterNode
            {
                Id = "start",
                TextKey = "test.easy.start",
                Options = new List<EncounterOption>
                {
                    new EncounterOption
                    {
                        Id = "easy_check",
                        TextKey = "test.easy.check",
                        SkillCheck = new SkillCheckDef
                        {
                            Stat = CrewStatType.Tech,
                            Difficulty = 5  // Very easy
                        },
                        SuccessOutcome = EncounterOutcome.EndWith(
                            EncounterEffect.AddCredits(50)),
                        FailureOutcome = EncounterOutcome.End()
                    }
                }
            }
        }
    };
}

/// <summary>
/// Encounter with hard skill check (for testing guaranteed failure).
/// </summary>
public static EncounterTemplate CreateHardSkillCheckEncounter()
{
    return new EncounterTemplate
    {
        Id = "test_hard_skillcheck",
        Name = "Hard Skill Check Test",
        Tags = new HashSet<string> { "test" },
        EntryNodeId = "start",
        Nodes = new Dictionary<string, EncounterNode>
        {
            ["start"] = new EncounterNode
            {
                Id = "start",
                TextKey = "test.hard.start",
                Options = new List<EncounterOption>
                {
                    new EncounterOption
                    {
                        Id = "hard_check",
                        TextKey = "test.hard.check",
                        SkillCheck = new SkillCheckDef
                        {
                            Stat = CrewStatType.Tech,
                            Difficulty = 25  // Impossible without bonuses
                        },
                        SuccessOutcome = EncounterOutcome.EndWith(
                            EncounterEffect.AddCredits(1000)),
                        FailureOutcome = EncounterOutcome.EndWith(
                            EncounterEffect.ShipDamage(10))
                    }
                }
            }
        }
    };
}
```

**Acceptance Criteria**:
- [ ] `CreateSkillCheckEncounter()` has multiple skill check options
- [ ] Trait bonuses and penalties demonstrated
- [ ] Easy/hard encounters for deterministic testing

---

### Phase 6: Unit Tests

#### Test File: `tests/sim/encounter/EN2SkillCheckTests.cs`

```csharp
using System.Collections.Generic;
using GdUnit4;
using static GdUnit4.Assertions;

namespace FringeTactics.Tests;

[TestSuite]
public class EN2SkillCheckTests
{
    [Before]
    public void Setup()
    {
        TraitRegistry.EnsureInitialized();
    }
    
    private CrewSnapshot CreateTestCrew(int tech = 5, int savvy = 5, List<string> traits = null)
    {
        return new CrewSnapshot
        {
            Id = 1,
            Name = "Test Crew",
            TraitIds = traits ?? new List<string>(),
            Grit = 5,
            Reflexes = 5,
            Aim = 5,
            Tech = tech,
            Savvy = savvy,
            Resolve = 5
        };
    }
    
    private EncounterContext CreateTestContext(List<CrewSnapshot> crew = null)
    {
        return new EncounterContext
        {
            Money = 100,
            Fuel = 50,
            Crew = crew ?? new List<CrewSnapshot> { CreateTestCrew() },
            Rng = new RngStream(12345)
        };
    }

    // === Basic Resolution Tests ===
    
    [TestCase]
    public void Resolve_WithHighStat_Succeeds()
    {
        var check = new SkillCheckDef { Stat = CrewStatType.Tech, Difficulty = 10 };
        var context = CreateTestContext(new List<CrewSnapshot> 
        { 
            CreateTestCrew(tech: 10) 
        });
        
        // With stat 10 and difficulty 10, need roll >= 0, always succeeds
        var result = SkillCheck.Resolve(check, context, context.Rng);
        
        AssertBool(result.Success).IsTrue();
    }
    
    [TestCase]
    public void Resolve_WithLowStat_CanFail()
    {
        var check = new SkillCheckDef { Stat = CrewStatType.Tech, Difficulty = 20 };
        var context = CreateTestContext(new List<CrewSnapshot> 
        { 
            CreateTestCrew(tech: 1) 
        });
        
        // With stat 1 and difficulty 20, need roll >= 19, always fails
        var result = SkillCheck.Resolve(check, context, context.Rng);
        
        AssertBool(result.Success).IsFalse();
    }
    
    [TestCase]
    public void Resolve_CapturesAllDetails()
    {
        var check = new SkillCheckDef 
        { 
            Stat = CrewStatType.Tech, 
            Difficulty = 12 
        };
        var context = CreateTestContext(new List<CrewSnapshot> 
        { 
            CreateTestCrew(tech: 7) 
        });
        
        var result = SkillCheck.Resolve(check, context, context.Rng);
        
        AssertObject(result.Crew).IsNotNull();
        AssertInt(result.StatValue).IsEqual(7);
        AssertInt(result.Difficulty).IsEqual(12);
        AssertInt(result.Roll).IsBetween(1, 10);
        AssertInt(result.Total).IsEqual(result.Roll + result.StatValue + result.TraitBonus);
    }

    // === Trait Bonus Tests ===
    
    [TestCase]
    public void Resolve_WithBonusTrait_AddsBonus()
    {
        var check = new SkillCheckDef 
        { 
            Stat = CrewStatType.Savvy, 
            Difficulty = 15,
            BonusTraits = new List<string> { "smuggler" }
        };
        var context = CreateTestContext(new List<CrewSnapshot> 
        { 
            CreateTestCrew(savvy: 5, traits: new List<string> { "smuggler" }) 
        });
        
        var result = SkillCheck.Resolve(check, context, context.Rng);
        
        AssertInt(result.TraitBonus).IsEqual(SkillCheck.TraitBonusAmount);
        AssertBool(result.AppliedBonusTraits.Contains("smuggler")).IsTrue();
    }
    
    [TestCase]
    public void Resolve_WithPenaltyTrait_SubtractsBonus()
    {
        var check = new SkillCheckDef 
        { 
            Stat = CrewStatType.Savvy, 
            Difficulty = 10,
            PenaltyTraits = new List<string> { "reckless" }
        };
        var context = CreateTestContext(new List<CrewSnapshot> 
        { 
            CreateTestCrew(savvy: 5, traits: new List<string> { "reckless" }) 
        });
        
        var result = SkillCheck.Resolve(check, context, context.Rng);
        
        AssertInt(result.TraitBonus).IsEqual(-SkillCheck.TraitPenaltyAmount);
        AssertBool(result.AppliedPenaltyTraits.Contains("reckless")).IsTrue();
    }
    
    [TestCase]
    public void Resolve_WithMultipleTraits_CombinesBonuses()
    {
        var check = new SkillCheckDef 
        { 
            Stat = CrewStatType.Savvy, 
            Difficulty = 15,
            BonusTraits = new List<string> { "smuggler", "empathetic" },
            PenaltyTraits = new List<string> { "reckless" }
        };
        var context = CreateTestContext(new List<CrewSnapshot> 
        { 
            CreateTestCrew(savvy: 5, traits: new List<string> { "smuggler", "empathetic" }) 
        });
        
        var result = SkillCheck.Resolve(check, context, context.Rng);
        
        // +2 for smuggler, +2 for empathetic, no penalty (no reckless)
        AssertInt(result.TraitBonus).IsEqual(4);
    }

    // === Crew Selection Tests ===
    
    [TestCase]
    public void SelectBestCrew_ChoosesHighestStat()
    {
        var check = new SkillCheckDef { Stat = CrewStatType.Tech, Difficulty = 10 };
        var context = CreateTestContext(new List<CrewSnapshot> 
        { 
            CreateTestCrew(tech: 3),
            CreateTestCrew(tech: 8),
            CreateTestCrew(tech: 5)
        });
        context.Crew[0].Id = 1;
        context.Crew[1].Id = 2;
        context.Crew[2].Id = 3;
        
        var best = SkillCheck.SelectBestCrew(check, context);
        
        AssertInt(best.Id).IsEqual(2);
        AssertInt(best.Tech).IsEqual(8);
    }
    
    [TestCase]
    public void SelectBestCrew_ConsidersTraitBonuses()
    {
        var check = new SkillCheckDef 
        { 
            Stat = CrewStatType.Savvy, 
            Difficulty = 10,
            BonusTraits = new List<string> { "smuggler" }
        };
        var context = CreateTestContext(new List<CrewSnapshot> 
        { 
            CreateTestCrew(savvy: 6, traits: new List<string>()),
            CreateTestCrew(savvy: 4, traits: new List<string> { "smuggler" })
        });
        context.Crew[0].Id = 1;
        context.Crew[1].Id = 2;
        
        var best = SkillCheck.SelectBestCrew(check, context);
        
        // Crew 1: 6 + 0 = 6
        // Crew 2: 4 + 2 = 6 (tie, but smuggler should be considered)
        // With equal effective value, first in order wins
        AssertInt(best.Id).IsEqual(1);
    }
    
    [TestCase]
    public void SelectBestCrew_ReturnsNullWhenNoCrew()
    {
        var check = new SkillCheckDef { Stat = CrewStatType.Tech, Difficulty = 10 };
        var context = CreateTestContext(new List<CrewSnapshot>());
        
        var best = SkillCheck.SelectBestCrew(check, context);
        
        AssertObject(best).IsNull();
    }

    // === Success Chance Tests ===
    
    [TestCase]
    public void GetSuccessChance_Returns100ForAutoSuccess()
    {
        var check = new SkillCheckDef { Stat = CrewStatType.Tech, Difficulty = 5 };
        var context = CreateTestContext(new List<CrewSnapshot> 
        { 
            CreateTestCrew(tech: 10) 
        });
        
        var chance = SkillCheck.GetSuccessChance(check, context);
        
        // Stat 10, difficulty 5: need roll >= -5, always succeeds
        AssertInt(chance).IsEqual(100);
    }
    
    [TestCase]
    public void GetSuccessChance_Returns0ForAutoFail()
    {
        var check = new SkillCheckDef { Stat = CrewStatType.Tech, Difficulty = 25 };
        var context = CreateTestContext(new List<CrewSnapshot> 
        { 
            CreateTestCrew(tech: 1) 
        });
        
        var chance = SkillCheck.GetSuccessChance(check, context);
        
        // Stat 1, difficulty 25: need roll >= 24, impossible
        AssertInt(chance).IsEqual(0);
    }
    
    [TestCase]
    public void GetSuccessChance_Returns50ForEvenOdds()
    {
        var check = new SkillCheckDef { Stat = CrewStatType.Tech, Difficulty = 11 };
        var context = CreateTestContext(new List<CrewSnapshot> 
        { 
            CreateTestCrew(tech: 5) 
        });
        
        var chance = SkillCheck.GetSuccessChance(check, context);
        
        // Stat 5, difficulty 11: need roll >= 6
        // Rolls 6-10 succeed (5 outcomes out of 10) = 50%
        AssertInt(chance).IsEqual(50);
    }

    // === Determinism Tests ===
    
    [TestCase]
    public void Resolve_IsDeterministicWithSameSeed()
    {
        var check = new SkillCheckDef { Stat = CrewStatType.Tech, Difficulty = 12 };
        var crew = CreateTestCrew(tech: 5);
        
        var rng1 = new RngStream(42);
        var rng2 = new RngStream(42);
        
        var result1 = SkillCheck.ResolveWithCrew(check, crew, rng1);
        var result2 = SkillCheck.ResolveWithCrew(check, crew, rng2);
        
        AssertInt(result1.Roll).IsEqual(result2.Roll);
        AssertBool(result1.Success).IsEqual(result2.Success);
    }
    
    [TestCase]
    public void Resolve_DifferentSeedsProduceDifferentResults()
    {
        var check = new SkillCheckDef { Stat = CrewStatType.Tech, Difficulty = 12 };
        var crew = CreateTestCrew(tech: 5);
        
        var results = new List<int>();
        for (int seed = 0; seed < 100; seed++)
        {
            var rng = new RngStream(seed);
            var result = SkillCheck.ResolveWithCrew(check, crew, rng);
            results.Add(result.Roll);
        }
        
        // Should have variety in rolls
        var uniqueRolls = new HashSet<int>(results);
        AssertInt(uniqueRolls.Count).IsGreater(5);
    }
}
```

#### Test File: `tests/sim/encounter/EN2RunnerIntegrationTests.cs`

```csharp
using System.Collections.Generic;
using GdUnit4;
using static GdUnit4.Assertions;

namespace FringeTactics.Tests;

[TestSuite]
public class EN2RunnerIntegrationTests
{
    private EncounterRunner runner;

    [Before]
    public void Setup()
    {
        TraitRegistry.EnsureInitialized();
        runner = new EncounterRunner();
    }
    
    private EncounterContext CreateTestContext(int tech = 7, int savvy = 8, List<string> traits = null)
    {
        return new EncounterContext
        {
            Money = 100,
            Fuel = 50,
            Crew = new List<CrewSnapshot>
            {
                new CrewSnapshot
                {
                    Id = 1,
                    Name = "Test Crew",
                    TraitIds = traits ?? new List<string>(),
                    Grit = 5,
                    Reflexes = 6,
                    Aim = 6,
                    Tech = tech,
                    Savvy = savvy,
                    Resolve = 5
                }
            },
            Rng = new RngStream(12345)
        };
    }

    [TestCase]
    public void SelectOption_WithSkillCheck_ResolvesCheck()
    {
        var template = TestEncounters.CreateSkillCheckEncounter();
        var instance = EncounterInstance.Create(template, "test_001");
        var context = CreateTestContext(tech: 10);  // High tech for guaranteed success
        
        // Select the hack option (index 0)
        var result = runner.SelectOption(instance, context, 0);
        
        AssertBool(result.IsSuccess).IsTrue();
    }
    
    [TestCase]
    public void SelectOption_SkillCheckSuccess_UsesSuccessOutcome()
    {
        var template = TestEncounters.CreateEasySkillCheckEncounter();
        var instance = EncounterInstance.Create(template, "test_002");
        var context = CreateTestContext(tech: 10);  // Guaranteed success
        
        runner.SelectOption(instance, context, 0);
        
        // Easy check success grants 50 credits
        var hasCredits = instance.PendingEffects.Exists(e => 
            e.Type == EffectType.AddResource && 
            e.TargetId == ResourceTypes.Money && 
            e.Amount == 50);
        AssertBool(hasCredits).IsTrue();
    }
    
    [TestCase]
    public void SelectOption_SkillCheckFailure_UsesFailureOutcome()
    {
        var template = TestEncounters.CreateHardSkillCheckEncounter();
        var instance = EncounterInstance.Create(template, "test_003");
        var context = CreateTestContext(tech: 1);  // Guaranteed failure
        
        runner.SelectOption(instance, context, 0);
        
        // Hard check failure causes ship damage
        var hasDamage = instance.PendingEffects.Exists(e => 
            e.Type == EffectType.ShipDamage && 
            e.Amount == 10);
        AssertBool(hasDamage).IsTrue();
    }
    
    [TestCase]
    public void SelectOption_NonSkillCheckOption_WorksNormally()
    {
        var template = TestEncounters.CreateSkillCheckEncounter();
        var instance = EncounterInstance.Create(template, "test_004");
        var context = CreateTestContext();
        
        // Select the force entry option (index 2, no skill check)
        var result = runner.SelectOption(instance, context, 2);
        
        AssertBool(result.IsSuccess).IsTrue();
        AssertString(instance.CurrentNodeId).IsEqual("alarm");
    }
}
```

---

## Files to Create

| File | Purpose |
|------|---------|
| `src/sim/encounter/SkillCheck.cs` | Skill check resolution logic |
| `src/sim/encounter/SkillCheckResult.cs` | Result data structure |
| `tests/sim/encounter/EN2SkillCheckTests.cs` | Skill check unit tests |
| `tests/sim/encounter/EN2RunnerIntegrationTests.cs` | Runner integration tests |

## Files to Modify

| File | Changes |
|------|---------|
| `src/sim/encounter/EncounterRunner.cs` | Update `ResolveOutcome()` for skill checks |
| `src/sim/encounter/EncounterContext.cs` | Add trait query methods |
| `src/sim/encounter/TestEncounters.cs` | Add skill check test encounters |
| `src/sim/Events.cs` | Add `SkillCheckResolvedEvent` |
| `src/sim/encounter/agents.md` | Document new files |
| `tests/sim/encounter/agents.md` | Document new test files |

---

## Implementation Order

1. **SkillCheckResult.cs** – Data structure for results
2. **SkillCheck.cs** – Resolution logic
3. **Events.cs update** – Add skill check event
4. **EncounterRunner.cs update** – Integrate skill checks
5. **EncounterContext.cs update** – Add trait queries
6. **TestEncounters.cs update** – Add test encounters
7. **EN2SkillCheckTests.cs** – Unit tests
8. **EN2RunnerIntegrationTests.cs** – Integration tests
9. **agents.md updates** – Documentation

---

## Manual Test Setup

### Scenario 1: Basic Skill Check Flow

1. Create `EncounterInstance` from `TestEncounters.CreateSkillCheckEncounter()`
2. Create `EncounterContext` with crew having Tech 7, Savvy 8
3. Call `runner.GetAvailableOptions()` – verify 3 options available
4. Select hack option (index 0)
5. Verify skill check was resolved (check events or result)
6. Verify correct outcome was applied based on success/failure

### Scenario 2: Trait Bonus Verification

1. Create context with crew having "smuggler" trait
2. Create encounter with Savvy check that has "smuggler" as bonus trait
3. Resolve check and verify `TraitBonus` is +2
4. Verify `AppliedBonusTraits` contains "smuggler"

### Scenario 3: Best Crew Selection

1. Create context with 3 crew members:
   - Crew A: Tech 3
   - Crew B: Tech 8
   - Crew C: Tech 5
2. Create Tech skill check
3. Verify `SelectBestCrew()` returns Crew B
4. Resolve check and verify Crew B was used

### Scenario 4: Success Chance Preview

1. Create various difficulty checks
2. Call `GetSuccessChance()` for each
3. Verify:
   - Difficulty 5 with stat 10 → 100%
   - Difficulty 25 with stat 1 → 0%
   - Difficulty 11 with stat 5 → 50%

### Scenario 5: Determinism Verification

1. Create check with moderate difficulty
2. Resolve with RNG seed 42
3. Record result
4. Reset and resolve again with seed 42
5. Verify identical results

---

## Success Criteria

- [ ] Skill checks use crew stats correctly
- [ ] Trait bonuses/penalties apply correctly
- [ ] Best crew auto-selected for checks
- [ ] Success uses `SuccessOutcome`, failure uses `FailureOutcome`
- [ ] Success chance preview is accurate
- [ ] Deterministic with same RNG seed
- [ ] Events emitted for UI feedback
- [ ] All unit tests passing (~25-30 tests)
- [ ] Integration with existing EN1 tests maintained

---

## Future Work (EN3+)

- **EN3**: Player crew selection for checks (instead of auto-best)
- **EN3**: Critical success/failure special outcomes
- **EN3**: Tactical branching from skill check failure
- **EN4**: Simulation metrics affecting difficulty
- **MG4**: Effect application to campaign state

---

## Appendix A: Difficulty Guidelines

| Difficulty | Description | 50% Success Stat |
|------------|-------------|------------------|
| 6 | Trivial | 0 |
| 8 | Easy | 2 |
| 10 | Moderate | 4 |
| 12 | Challenging | 6 |
| 14 | Hard | 8 |
| 16 | Very Hard | 10 |
| 18+ | Near Impossible | 10+ with traits |

**Formula**: For 50% success, `stat + traitBonus = difficulty - 6`

---

## Appendix B: Trait Bonus Examples

| Check Type | Bonus Traits | Penalty Traits |
|------------|--------------|----------------|
| Hacking | corporate, spacer | - |
| Negotiation | smuggler, empathetic, corporate | reckless |
| Intimidation | ex_military, cold_blooded, scarred | empathetic |
| Piloting | spacer, frontier_born | - |
| Medical | - | reckless |
| Stealth | cautious, smuggler | reckless |

---

## Appendix C: RngStream.NextInt Specification

The `RngStream.NextInt(min, max)` method returns integers in the range `[min, max)` (inclusive min, exclusive max).

For a d10 roll (1-10 inclusive), use: `rng.NextInt(1, 11)`
