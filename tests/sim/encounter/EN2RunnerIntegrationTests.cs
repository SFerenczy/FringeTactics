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

    private EncounterContext CreateTestContext(int tech = 7, int savvy = 8, int reflexes = 6, List<string> traits = null, int seed = 12345)
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
                    Reflexes = reflexes,
                    Aim = 6,
                    Tech = tech,
                    Savvy = savvy,
                    Resolve = 5
                }
            },
            Rng = new RngStream("test", seed)
        };
    }

    // ========================================================================
    // BASIC SKILL CHECK INTEGRATION
    // ========================================================================

    [TestCase]
    public void SelectOption_WithSkillCheck_ResolvesCheck()
    {
        var template = TestEncounters.CreateSkillCheckEncounter();
        var instance = EncounterInstance.Create(template, "test_001");
        var context = CreateTestContext(tech: 10);

        var result = runner.SelectOption(instance, context, 0);

        AssertBool(result.IsSuccess).IsTrue();
    }

    [TestCase]
    public void SelectOption_EasyCheck_SucceedsWithModerateStats()
    {
        var template = TestEncounters.CreateEasySkillCheckEncounter();
        var instance = EncounterInstance.Create(template, "test_002");
        var context = CreateTestContext(tech: 5);

        runner.SelectOption(instance, context, 0);

        // Easy check (difficulty 5) with tech 5 should almost always succeed
        // Check for success outcome effect (50 credits)
        var hasCredits = instance.PendingEffects.Exists(e =>
            e.Type == EffectType.AddResource &&
            e.TargetId == ResourceTypes.Money &&
            e.Amount == 50);
        AssertBool(hasCredits).IsTrue();
    }

    [TestCase]
    public void SelectOption_HardCheck_FailsWithLowStats()
    {
        var template = TestEncounters.CreateHardSkillCheckEncounter();
        var instance = EncounterInstance.Create(template, "test_003");
        var context = CreateTestContext(tech: 1);

        runner.SelectOption(instance, context, 0);

        // Hard check (difficulty 25) with tech 1 should always fail
        // Check for failure outcome effect (10 ship damage)
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

    // ========================================================================
    // TRAIT BONUS INTEGRATION
    // ========================================================================

    [TestCase]
    public void SelectOption_WithBonusTrait_ImproveChances()
    {
        var template = TestEncounters.CreateTraitBonusEncounter();
        var instance = EncounterInstance.Create(template, "test_005");
        // Savvy 6 + smuggler (+2) = 8, difficulty 14, need roll >= 6 (50%)
        var context = CreateTestContext(savvy: 6, traits: new List<string> { "smuggler" });

        // Run multiple times to verify trait affects outcome
        int successes = 0;
        for (int i = 0; i < 20; i++)
        {
            var testInstance = EncounterInstance.Create(template, $"test_trait_{i}");
            var testContext = CreateTestContext(savvy: 6, traits: new List<string> { "smuggler" }, seed: i);
            runner.SelectOption(testInstance, testContext, 0);

            if (testInstance.PendingEffects.Exists(e => e.Type == EffectType.AddResource && e.Amount == 300))
            {
                successes++;
            }
        }

        // With 50% chance over 20 trials, should have some successes
        AssertInt(successes).IsGreater(0);
        AssertInt(successes).IsLess(20);
    }

    [TestCase]
    public void SelectOption_WithPenaltyTrait_ReducesChances()
    {
        var template = TestEncounters.CreatePenaltyTraitEncounter();

        // Without penalty trait: Reflexes 8, difficulty 12, need roll >= 4 (70%)
        int successesWithout = 0;
        for (int i = 0; i < 20; i++)
        {
            var instance = EncounterInstance.Create(template, $"test_no_penalty_{i}");
            var context = CreateTestContext(reflexes: 8, traits: new List<string>(), seed: i);
            runner.SelectOption(instance, context, 0);
            if (instance.PendingEffects.Exists(e => e.Type == EffectType.AddResource && e.Amount == 150))
            {
                successesWithout++;
            }
        }

        // With penalty trait: Reflexes 8 - 2 = 6, difficulty 12, need roll >= 6 (50%)
        int successesWith = 0;
        for (int i = 0; i < 20; i++)
        {
            var instance = EncounterInstance.Create(template, $"test_with_penalty_{i}");
            var context = CreateTestContext(reflexes: 8, traits: new List<string> { "reckless" }, seed: i);
            runner.SelectOption(instance, context, 0);
            if (instance.PendingEffects.Exists(e => e.Type == EffectType.AddResource && e.Amount == 150))
            {
                successesWith++;
            }
        }

        // Penalty trait should reduce success rate
        AssertInt(successesWith).IsLessEqual(successesWithout);
    }

    // ========================================================================
    // OUTCOME BRANCHING
    // ========================================================================

    [TestCase]
    public void SelectOption_SkillCheckSuccess_UsesSuccessOutcome()
    {
        var template = TestEncounters.CreateSkillCheckEncounter();
        var instance = EncounterInstance.Create(template, "test_success_outcome");
        // Very high tech ensures success
        var context = CreateTestContext(tech: 15);

        runner.SelectOption(instance, context, 0); // hack_terminal option

        // Success outcome goes to "success" node and adds 200 credits
        var hasCredits = instance.PendingEffects.Exists(e =>
            e.Type == EffectType.AddResource &&
            e.Amount == 200);
        AssertBool(hasCredits).IsTrue();
    }

    [TestCase]
    public void SelectOption_SkillCheckFailure_UsesFailureOutcome()
    {
        var template = TestEncounters.CreateSkillCheckEncounter();
        var instance = EncounterInstance.Create(template, "test_failure_outcome");
        // Very low tech ensures failure (difficulty 12)
        var context = CreateTestContext(tech: 0);

        runner.SelectOption(instance, context, 0); // hack_terminal option

        // Failure outcome goes to "failure" node and adds time delay
        var hasDelay = instance.PendingEffects.Exists(e =>
            e.Type == EffectType.TimeDelay &&
            e.Amount == 1);
        AssertBool(hasDelay).IsTrue();
    }

    // ========================================================================
    // ENCOUNTER CONTEXT INTEGRATION
    // ========================================================================

    [TestCase]
    public void EncounterContext_GetBestCrewForCheck_Works()
    {
        var check = new SkillCheckDef
        {
            Stat = CrewStatType.Tech,
            Difficulty = 10,
            BonusTraits = new List<string> { "corporate" }
        };

        var context = new EncounterContext
        {
            Crew = new List<CrewSnapshot>
            {
                new CrewSnapshot { Id = 1, Name = "Low Tech", Tech = 3, TraitIds = new List<string>() },
                new CrewSnapshot { Id = 2, Name = "High Tech", Tech = 7, TraitIds = new List<string>() },
                new CrewSnapshot { Id = 3, Name = "Med Tech + Trait", Tech = 5, TraitIds = new List<string> { "corporate" } }
            }
        };

        var best = context.GetBestCrewForCheck(check);

        // Crew 2: 7, Crew 3: 5+2=7, tie goes to first (Crew 2)
        AssertInt(best.Id).IsEqual(2);
    }

    [TestCase]
    public void EncounterContext_GetSuccessChance_Works()
    {
        var check = new SkillCheckDef
        {
            Stat = CrewStatType.Tech,
            Difficulty = 11
        };

        var context = new EncounterContext
        {
            Crew = new List<CrewSnapshot>
            {
                new CrewSnapshot { Id = 1, Name = "Test", Tech = 5, TraitIds = new List<string>() }
            }
        };

        var chance = context.GetSuccessChance(check);

        // Tech 5, difficulty 11: need roll >= 6 = 50%
        AssertInt(chance).IsEqual(50);
    }

    // ========================================================================
    // TEMPLATE VALIDATION
    // ========================================================================

    [TestCase]
    public void SkillCheckEncounter_IsValid()
    {
        var template = TestEncounters.CreateSkillCheckEncounter();

        AssertBool(template.IsValid()).IsTrue();
        AssertString(template.Id).IsEqual("test_skillcheck");
    }

    [TestCase]
    public void SkillCheckEncounter_HasSkillCheckOptions()
    {
        var template = TestEncounters.CreateSkillCheckEncounter();
        var startNode = template.GetNode("start");

        var hackOption = startNode.Options.Find(o => o.Id == "hack_terminal");
        var talkOption = startNode.Options.Find(o => o.Id == "talk_guard");
        var forceOption = startNode.Options.Find(o => o.Id == "force_entry");

        AssertBool(hackOption.HasSkillCheck).IsTrue();
        AssertBool(talkOption.HasSkillCheck).IsTrue();
        AssertBool(forceOption.HasSkillCheck).IsFalse();
    }

    [TestCase]
    public void EasySkillCheckEncounter_IsValid()
    {
        var template = TestEncounters.CreateEasySkillCheckEncounter();

        AssertBool(template.IsValid()).IsTrue();
    }

    [TestCase]
    public void HardSkillCheckEncounter_IsValid()
    {
        var template = TestEncounters.CreateHardSkillCheckEncounter();

        AssertBool(template.IsValid()).IsTrue();
    }

    [TestCase]
    public void TraitBonusEncounter_IsValid()
    {
        var template = TestEncounters.CreateTraitBonusEncounter();

        AssertBool(template.IsValid()).IsTrue();
    }

    [TestCase]
    public void PenaltyTraitEncounter_IsValid()
    {
        var template = TestEncounters.CreatePenaltyTraitEncounter();

        AssertBool(template.IsValid()).IsTrue();
    }
}
