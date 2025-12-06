using System.Collections.Generic;
using GdUnit4;
using static GdUnit4.Assertions;

namespace FringeTactics.Tests;

[TestSuite]
public class EN1ConditionTests
{
    private EncounterContext CreateTestContext(int money = 100, int fuel = 50)
    {
        return new EncounterContext
        {
            Money = money,
            Fuel = fuel,
            Parts = 10,
            Ammo = 30,
            Crew = new List<CrewSnapshot>
            {
                new CrewSnapshot
                {
                    Id = 1,
                    Name = "Test Crew",
                    TraitIds = new List<string> { "veteran", "tech_expert" },
                    Grit = 5,
                    Reflexes = 4,
                    Aim = 6,
                    Tech = 7,
                    Savvy = 3,
                    Resolve = 5
                }
            },
            SystemTags = new HashSet<string> { "frontier", "lawless" },
            FactionRep = new Dictionary<string, int>
            {
                ["pirates"] = 30,
                ["traders"] = 70
            },
            CargoValue = 500,
            Flags = new HashSet<string> { "quest_active" }
        };
    }

    [TestCase]
    public void HasResource_TrueWhenSufficient()
    {
        var context = CreateTestContext(money: 100);
        var condition = EncounterCondition.HasCredits(50);

        AssertBool(condition.Evaluate(context)).IsTrue();
    }

    [TestCase]
    public void HasResource_TrueWhenExact()
    {
        var context = CreateTestContext(money: 100);
        var condition = EncounterCondition.HasCredits(100);

        AssertBool(condition.Evaluate(context)).IsTrue();
    }

    [TestCase]
    public void HasResource_FalseWhenInsufficient()
    {
        var context = CreateTestContext(money: 50);
        var condition = EncounterCondition.HasCredits(100);

        AssertBool(condition.Evaluate(context)).IsFalse();
    }

    [TestCase]
    public void HasFuel_EvaluatesCorrectly()
    {
        var context = CreateTestContext(fuel: 50);

        AssertBool(EncounterCondition.HasFuel(50).Evaluate(context)).IsTrue();
        AssertBool(EncounterCondition.HasFuel(51).Evaluate(context)).IsFalse();
    }

    [TestCase]
    public void HasTrait_TrueWhenCrewHasTrait()
    {
        var context = CreateTestContext();
        var condition = EncounterCondition.HasTrait("veteran");

        AssertBool(condition.Evaluate(context)).IsTrue();
    }

    [TestCase]
    public void HasTrait_FalseWhenNoCrewHasTrait()
    {
        var context = CreateTestContext();
        var condition = EncounterCondition.HasTrait("nonexistent_trait");

        AssertBool(condition.Evaluate(context)).IsFalse();
    }

    [TestCase]
    public void FactionRep_TrueWhenAboveThreshold()
    {
        var context = CreateTestContext();
        var condition = EncounterCondition.FactionRepMin("traders", 60);

        AssertBool(condition.Evaluate(context)).IsTrue();
    }

    [TestCase]
    public void FactionRep_FalseWhenBelowThreshold()
    {
        var context = CreateTestContext();
        var condition = EncounterCondition.FactionRepMin("pirates", 50);

        AssertBool(condition.Evaluate(context)).IsFalse();
    }

    [TestCase]
    public void FactionRep_DefaultsTo50ForUnknownFaction()
    {
        var context = CreateTestContext();
        var condition = EncounterCondition.FactionRepMin("unknown_faction", 50);

        AssertBool(condition.Evaluate(context)).IsTrue();
    }

    [TestCase]
    public void SystemTag_TrueWhenTagPresent()
    {
        var context = CreateTestContext();
        var condition = EncounterCondition.SystemHasTag("frontier");

        AssertBool(condition.Evaluate(context)).IsTrue();
    }

    [TestCase]
    public void SystemTag_FalseWhenTagMissing()
    {
        var context = CreateTestContext();
        var condition = EncounterCondition.SystemHasTag("core");

        AssertBool(condition.Evaluate(context)).IsFalse();
    }

    [TestCase]
    public void CrewStat_TrueWhenStatMeetsThreshold()
    {
        var context = CreateTestContext();
        var condition = EncounterCondition.CrewStatMin(CrewStatType.Tech, 7);

        AssertBool(condition.Evaluate(context)).IsTrue();
    }

    [TestCase]
    public void CrewStat_FalseWhenStatBelowThreshold()
    {
        var context = CreateTestContext();
        var condition = EncounterCondition.CrewStatMin(CrewStatType.Savvy, 5);

        AssertBool(condition.Evaluate(context)).IsFalse();
    }

    [TestCase]
    public void HasFlag_TrueWhenFlagSet()
    {
        var context = CreateTestContext();
        var condition = EncounterCondition.FlagSet("quest_active");

        AssertBool(condition.Evaluate(context)).IsTrue();
    }

    [TestCase]
    public void HasFlag_FalseWhenFlagNotSet()
    {
        var context = CreateTestContext();
        var condition = EncounterCondition.FlagSet("quest_complete");

        AssertBool(condition.Evaluate(context)).IsFalse();
    }

    [TestCase]
    public void Not_InvertsResult()
    {
        var context = CreateTestContext(money: 100);
        var condition = EncounterCondition.Not(EncounterCondition.HasCredits(50));

        AssertBool(condition.Evaluate(context)).IsFalse();
    }

    [TestCase]
    public void Not_InvertsFalseToTrue()
    {
        var context = CreateTestContext(money: 30);
        var condition = EncounterCondition.Not(EncounterCondition.HasCredits(50));

        AssertBool(condition.Evaluate(context)).IsTrue();
    }

    [TestCase]
    public void And_TrueWhenAllTrue()
    {
        var context = CreateTestContext(money: 100, fuel: 50);
        var condition = EncounterCondition.And(
            EncounterCondition.HasCredits(50),
            EncounterCondition.HasFuel(25)
        );

        AssertBool(condition.Evaluate(context)).IsTrue();
    }

    [TestCase]
    public void And_FalseWhenAnyFalse()
    {
        var context = CreateTestContext(money: 100, fuel: 10);
        var condition = EncounterCondition.And(
            EncounterCondition.HasCredits(50),
            EncounterCondition.HasFuel(25)
        );

        AssertBool(condition.Evaluate(context)).IsFalse();
    }

    [TestCase]
    public void Or_TrueWhenAnyTrue()
    {
        var context = CreateTestContext(money: 100, fuel: 10);
        var condition = EncounterCondition.Or(
            EncounterCondition.HasCredits(50),
            EncounterCondition.HasFuel(25)
        );

        AssertBool(condition.Evaluate(context)).IsTrue();
    }

    [TestCase]
    public void Or_FalseWhenAllFalse()
    {
        var context = CreateTestContext(money: 30, fuel: 10);
        var condition = EncounterCondition.Or(
            EncounterCondition.HasCredits(50),
            EncounterCondition.HasFuel(25)
        );

        AssertBool(condition.Evaluate(context)).IsFalse();
    }

    [TestCase]
    public void HasCargoValue_EvaluatesCorrectly()
    {
        var context = CreateTestContext();
        var condition = EncounterCondition.HasCargoValue(400);

        AssertBool(condition.Evaluate(context)).IsTrue();
    }

    [TestCase]
    public void NullContext_ReturnsFalse()
    {
        var condition = EncounterCondition.HasCredits(50);

        AssertBool(condition.Evaluate(null)).IsFalse();
    }
}
