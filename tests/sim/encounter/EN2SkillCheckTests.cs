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

    private CrewSnapshot CreateTestCrew(int id = 1, int tech = 5, int savvy = 5, int reflexes = 5, int aim = 5, List<string> traits = null)
    {
        return new CrewSnapshot
        {
            Id = id,
            Name = $"Test Crew {id}",
            TraitIds = traits ?? new List<string>(),
            Grit = 5,
            Reflexes = reflexes,
            Aim = aim,
            Tech = tech,
            Savvy = savvy,
            Resolve = 5
        };
    }

    private EncounterContext CreateTestContext(List<CrewSnapshot> crew = null, int seed = 12345)
    {
        return new EncounterContext
        {
            Money = 100,
            Fuel = 50,
            Crew = crew ?? new List<CrewSnapshot> { CreateTestCrew() },
            Rng = new RngStream("test", seed)
        };
    }

    // ========================================================================
    // BASIC RESOLUTION TESTS
    // ========================================================================

    [TestCase]
    public void Resolve_WithHighStat_Succeeds()
    {
        var check = new SkillCheckDef { Stat = CrewStatType.Tech, Difficulty = 10 };
        var context = CreateTestContext(new List<CrewSnapshot>
        {
            CreateTestCrew(tech: 10)
        });

        var result = SkillCheck.Resolve(check, context, context.Rng);

        // With stat 10 and difficulty 10, need roll >= 0, always succeeds
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

        var result = SkillCheck.Resolve(check, context, context.Rng);

        // With stat 1 and difficulty 20, need roll >= 19, always fails
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

    [TestCase]
    public void Resolve_WithNullCheck_ReturnsFalse()
    {
        var context = CreateTestContext();

        var result = SkillCheck.Resolve(null, context, context.Rng);

        AssertBool(result.Success).IsFalse();
    }

    [TestCase]
    public void Resolve_WithNullContext_ReturnsFalse()
    {
        var check = new SkillCheckDef { Stat = CrewStatType.Tech, Difficulty = 10 };

        var result = SkillCheck.Resolve(check, null, new RngStream("test", 0));

        AssertBool(result.Success).IsFalse();
    }

    [TestCase]
    public void Resolve_WithNoCrew_ReturnsFalse()
    {
        var check = new SkillCheckDef { Stat = CrewStatType.Tech, Difficulty = 10 };
        var context = CreateTestContext(new List<CrewSnapshot>());

        var result = SkillCheck.Resolve(check, context, context.Rng);

        AssertBool(result.Success).IsFalse();
    }

    // ========================================================================
    // TRAIT BONUS TESTS
    // ========================================================================

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
    public void Resolve_WithMultipleBonusTraits_CombinesBonuses()
    {
        var check = new SkillCheckDef
        {
            Stat = CrewStatType.Savvy,
            Difficulty = 15,
            BonusTraits = new List<string> { "smuggler", "empathetic" }
        };
        var context = CreateTestContext(new List<CrewSnapshot>
        {
            CreateTestCrew(savvy: 5, traits: new List<string> { "smuggler", "empathetic" })
        });

        var result = SkillCheck.Resolve(check, context, context.Rng);

        // +2 for smuggler, +2 for empathetic
        AssertInt(result.TraitBonus).IsEqual(4);
        AssertInt(result.AppliedBonusTraits.Count).IsEqual(2);
    }

    [TestCase]
    public void Resolve_WithMixedTraits_CalculatesNetBonus()
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
            CreateTestCrew(savvy: 5, traits: new List<string> { "smuggler", "reckless" })
        });

        var result = SkillCheck.Resolve(check, context, context.Rng);

        // +2 for smuggler, -2 for reckless = 0 net
        AssertInt(result.TraitBonus).IsEqual(0);
        AssertInt(result.AppliedBonusTraits.Count).IsEqual(1);
        AssertInt(result.AppliedPenaltyTraits.Count).IsEqual(1);
    }

    [TestCase]
    public void Resolve_WithUnmatchedTraits_NoBonus()
    {
        var check = new SkillCheckDef
        {
            Stat = CrewStatType.Savvy,
            Difficulty = 15,
            BonusTraits = new List<string> { "smuggler" }
        };
        var context = CreateTestContext(new List<CrewSnapshot>
        {
            CreateTestCrew(savvy: 5, traits: new List<string> { "brave" })
        });

        var result = SkillCheck.Resolve(check, context, context.Rng);

        AssertInt(result.TraitBonus).IsEqual(0);
        AssertInt(result.AppliedBonusTraits.Count).IsEqual(0);
    }

    // ========================================================================
    // CREW SELECTION TESTS
    // ========================================================================

    [TestCase]
    public void SelectBestCrew_ChoosesHighestStat()
    {
        var check = new SkillCheckDef { Stat = CrewStatType.Tech, Difficulty = 10 };
        var context = CreateTestContext(new List<CrewSnapshot>
        {
            CreateTestCrew(id: 1, tech: 3),
            CreateTestCrew(id: 2, tech: 8),
            CreateTestCrew(id: 3, tech: 5)
        });

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
            CreateTestCrew(id: 1, savvy: 6, traits: new List<string>()),
            CreateTestCrew(id: 2, savvy: 5, traits: new List<string> { "smuggler" })
        });

        var best = SkillCheck.SelectBestCrew(check, context);

        // Crew 1: 6 + 0 = 6, Crew 2: 5 + 2 = 7
        AssertInt(best.Id).IsEqual(2);
    }

    [TestCase]
    public void SelectBestCrew_ReturnsNullWhenNoCrew()
    {
        var check = new SkillCheckDef { Stat = CrewStatType.Tech, Difficulty = 10 };
        var context = CreateTestContext(new List<CrewSnapshot>());

        var best = SkillCheck.SelectBestCrew(check, context);

        AssertObject(best).IsNull();
    }

    [TestCase]
    public void SelectBestCrew_ReturnsNullWithNullContext()
    {
        var check = new SkillCheckDef { Stat = CrewStatType.Tech, Difficulty = 10 };

        var best = SkillCheck.SelectBestCrew(check, null);

        AssertObject(best).IsNull();
    }

    // ========================================================================
    // SUCCESS CHANCE TESTS
    // ========================================================================

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

    [TestCase]
    public void GetSuccessChance_IncludesTraitBonus()
    {
        var check = new SkillCheckDef
        {
            Stat = CrewStatType.Tech,
            Difficulty = 13,
            BonusTraits = new List<string> { "corporate" }
        };
        var context = CreateTestContext(new List<CrewSnapshot>
        {
            CreateTestCrew(tech: 5, traits: new List<string> { "corporate" })
        });

        var chance = SkillCheck.GetSuccessChance(check, context);

        // Stat 5 + trait 2 = 7, difficulty 13: need roll >= 6 = 50%
        AssertInt(chance).IsEqual(50);
    }

    [TestCase]
    public void GetSuccessChance_Returns0WithNoCrew()
    {
        var check = new SkillCheckDef { Stat = CrewStatType.Tech, Difficulty = 10 };
        var context = CreateTestContext(new List<CrewSnapshot>());

        var chance = SkillCheck.GetSuccessChance(check, context);

        AssertInt(chance).IsEqual(0);
    }

    // ========================================================================
    // DETERMINISM TESTS
    // ========================================================================

    [TestCase]
    public void Resolve_IsDeterministicWithSameSeed()
    {
        var check = new SkillCheckDef { Stat = CrewStatType.Tech, Difficulty = 12 };
        var crew = CreateTestCrew(tech: 5);

        var rng1 = new RngStream("test", 42);
        var rng2 = new RngStream("test", 42);

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
            var rng = new RngStream("test", seed);
            var result = SkillCheck.ResolveWithCrew(check, crew, rng);
            results.Add(result.Roll);
        }

        // Should have variety in rolls
        var uniqueRolls = new HashSet<int>(results);
        AssertInt(uniqueRolls.Count).IsGreater(5);
    }

    // ========================================================================
    // CRITICAL SUCCESS/FAILURE TESTS
    // ========================================================================

    [TestCase]
    public void SkillCheckResult_CriticalSuccess_WhenMarginAtLeast5()
    {
        var result = new SkillCheckResult
        {
            Success = true,
            Difficulty = 10,
            Roll = 10,
            StatValue = 8,
            TraitBonus = 0
            // Total = 18, Margin = 8
        };

        AssertBool(result.IsCriticalSuccess).IsTrue();
        AssertBool(result.IsCriticalFailure).IsFalse();
    }

    [TestCase]
    public void SkillCheckResult_CriticalFailure_WhenMarginAtMostMinus5()
    {
        var result = new SkillCheckResult
        {
            Success = false,
            Difficulty = 15,
            Roll = 1,
            StatValue = 3,
            TraitBonus = 0
            // Total = 4, Margin = -11
        };

        AssertBool(result.IsCriticalFailure).IsTrue();
        AssertBool(result.IsCriticalSuccess).IsFalse();
    }

    [TestCase]
    public void SkillCheckResult_NormalSuccess_WhenMarginLessThan5()
    {
        var result = new SkillCheckResult
        {
            Success = true,
            Difficulty = 10,
            Roll = 5,
            StatValue = 6,
            TraitBonus = 0
            // Total = 11, Margin = 1
        };

        AssertBool(result.Success).IsTrue();
        AssertBool(result.IsCriticalSuccess).IsFalse();
        AssertBool(result.IsCriticalFailure).IsFalse();
    }

    // ========================================================================
    // EFFECTIVE CHECK VALUE TESTS
    // ========================================================================

    [TestCase]
    public void GetEffectiveCheckValue_IncludesStatAndTraits()
    {
        var check = new SkillCheckDef
        {
            Stat = CrewStatType.Savvy,
            Difficulty = 10,
            BonusTraits = new List<string> { "smuggler" }
        };
        var crew = CreateTestCrew(savvy: 6, traits: new List<string> { "smuggler" });

        var value = SkillCheck.GetEffectiveCheckValue(crew, check);

        // 6 stat + 2 trait bonus = 8
        AssertInt(value).IsEqual(8);
    }

    [TestCase]
    public void GetEffectiveCheckValue_WithPenalty()
    {
        var check = new SkillCheckDef
        {
            Stat = CrewStatType.Reflexes,
            Difficulty = 10,
            PenaltyTraits = new List<string> { "reckless" }
        };
        var crew = CreateTestCrew(reflexes: 6, traits: new List<string> { "reckless" });

        var value = SkillCheck.GetEffectiveCheckValue(crew, check);

        // 6 stat - 2 penalty = 4
        AssertInt(value).IsEqual(4);
    }
}
