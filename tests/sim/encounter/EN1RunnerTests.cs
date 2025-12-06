using System.Collections.Generic;
using GdUnit4;
using static GdUnit4.Assertions;

namespace FringeTactics.Tests;

[TestSuite]
public class EN1RunnerTests
{
    private EncounterRunner runner;

    [Before]
    public void Setup()
    {
        runner = new EncounterRunner();
    }

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
                    TraitIds = new List<string> { "veteran" },
                    Grit = 5,
                    Reflexes = 6,
                    Aim = 6,
                    Tech = 7,
                    Savvy = 8,
                    Resolve = 5
                }
            },
            FactionRep = new Dictionary<string, int>
            {
                ["pirates"] = 30,
                ["traders"] = 70
            },
            CargoValue = 500
        };
    }

    [TestCase]
    public void GetCurrentNode_ReturnsEntryNodeInitially()
    {
        var template = TestEncounters.CreateSimpleEncounter();
        var instance = EncounterInstance.Create(template, "test_001");
        
        var node = runner.GetCurrentNode(instance);

        AssertObject(node).IsNotNull();
        AssertString(node.Id).IsEqual("start");
    }

    [TestCase]
    public void GetCurrentNode_ReturnsNullWhenComplete()
    {
        var template = TestEncounters.CreateSimpleEncounter();
        var instance = EncounterInstance.Create(template, "test_002");
        instance.IsComplete = true;

        var node = runner.GetCurrentNode(instance);

        AssertObject(node).IsNull();
    }

    [TestCase]
    public void GetAvailableOptions_ReturnsAllOptionsWithNoConditions()
    {
        var template = TestEncounters.CreateSimpleEncounter();
        var instance = EncounterInstance.Create(template, "test_003");
        var context = CreateTestContext();

        var options = runner.GetAvailableOptions(instance, context);

        AssertInt(options.Count).IsEqual(2);
    }

    [TestCase]
    public void GetAvailableOptions_FiltersOnConditions()
    {
        var template = TestEncounters.CreateConditionalEncounter();
        var instance = EncounterInstance.Create(template, "test_004");
        var context = CreateTestContext(money: 0);

        var options = runner.GetAvailableOptions(instance, context);

        // Should have hack (tech 7 >= 5) and fight (no conditions), but not bribe (needs 50 credits)
        AssertInt(options.Count).IsEqual(2);
    }

    [TestCase]
    public void GetAvailableOptions_IncludesOptionWhenConditionMet()
    {
        var template = TestEncounters.CreateConditionalEncounter();
        var instance = EncounterInstance.Create(template, "test_005");
        var context = CreateTestContext(money: 100);

        var options = runner.GetAvailableOptions(instance, context);

        // Should have bribe, hack, and fight
        AssertInt(options.Count).IsEqual(3);
    }

    [TestCase]
    public void SelectOption_TransitionsToNextNode()
    {
        var template = TestEncounters.CreateSimpleEncounter();
        var instance = EncounterInstance.Create(template, "test_006");
        var context = CreateTestContext();

        var result = runner.SelectOption(instance, context, 0);

        AssertBool(result.IsSuccess).IsTrue();
        AssertString(instance.CurrentNodeId).IsEqual("end");
    }

    [TestCase]
    public void SelectOption_AccumulatesEffects()
    {
        var template = TestEncounters.CreateSimpleEncounter();
        var instance = EncounterInstance.Create(template, "test_007");
        var context = CreateTestContext();

        runner.SelectOption(instance, context, 0);

        AssertInt(instance.PendingEffects.Count).IsGreater(0);
    }

    [TestCase]
    public void SelectOption_ProcessesAutoTransition()
    {
        var template = TestEncounters.CreateSimpleEncounter();
        var instance = EncounterInstance.Create(template, "test_008");
        var context = CreateTestContext();

        runner.SelectOption(instance, context, 0);

        // After selecting option, should auto-transition through "end" node and complete
        AssertBool(instance.IsComplete).IsTrue();
    }

    [TestCase]
    public void SelectOption_InvalidIndex_ReturnsError()
    {
        var template = TestEncounters.CreateSimpleEncounter();
        var instance = EncounterInstance.Create(template, "test_009");
        var context = CreateTestContext();

        var result = runner.SelectOption(instance, context, 99);

        AssertBool(result.IsSuccess).IsFalse();
        AssertString(result.ErrorMessage).IsNotEmpty();
    }

    [TestCase]
    public void SelectOption_NegativeIndex_ReturnsError()
    {
        var template = TestEncounters.CreateSimpleEncounter();
        var instance = EncounterInstance.Create(template, "test_010");
        var context = CreateTestContext();

        var result = runner.SelectOption(instance, context, -1);

        AssertBool(result.IsSuccess).IsFalse();
    }

    [TestCase]
    public void IsComplete_FalseInitially()
    {
        var template = TestEncounters.CreateSimpleEncounter();
        var instance = EncounterInstance.Create(template, "test_011");

        AssertBool(runner.IsComplete(instance)).IsFalse();
    }

    [TestCase]
    public void IsComplete_TrueAfterEndNode()
    {
        var template = TestEncounters.CreateSimpleEncounter();
        var instance = EncounterInstance.Create(template, "test_012");
        var context = CreateTestContext();

        runner.SelectOption(instance, context, 0);

        AssertBool(runner.IsComplete(instance)).IsTrue();
    }

    [TestCase]
    public void GetPendingEffects_ReturnsAccumulatedEffects()
    {
        var template = TestEncounters.CreateSimpleEncounter();
        var instance = EncounterInstance.Create(template, "test_013");
        var context = CreateTestContext();

        runner.SelectOption(instance, context, 0);

        var effects = runner.GetPendingEffects(instance);
        AssertInt(effects.Count).IsGreater(0);
    }

    [TestCase]
    public void BranchingEncounter_PathA_ReachesCorrectEnd()
    {
        var template = TestEncounters.CreateBranchingEncounter();
        var instance = EncounterInstance.Create(template, "test_014");
        var context = CreateTestContext();

        // Select path A
        runner.SelectOption(instance, context, 0);
        AssertString(instance.CurrentNodeId).IsEqual("branch_a");

        // Continue from branch A
        runner.SelectOption(instance, context, 0);
        AssertBool(instance.IsComplete).IsTrue();
    }

    [TestCase]
    public void BranchingEncounter_PathC_AutoTransitionsToEnd()
    {
        var template = TestEncounters.CreateBranchingEncounter();
        var instance = EncounterInstance.Create(template, "test_015");
        var context = CreateTestContext();

        // Select path C (auto-transitions through branch_c to end_bad)
        runner.SelectOption(instance, context, 2);

        AssertBool(instance.IsComplete).IsTrue();
    }

    [TestCase]
    public void VisitedNodes_TracksHistory()
    {
        var template = TestEncounters.CreateBranchingEncounter();
        var instance = EncounterInstance.Create(template, "test_016");
        var context = CreateTestContext();

        // Start has entry node
        AssertInt(instance.VisitedNodes.Count).IsEqual(1);

        // Select path A
        runner.SelectOption(instance, context, 0);
        AssertInt(instance.VisitedNodes.Count).IsGreater(1);
    }

    [TestCase]
    public void Start_ProcessesInitialAutoTransitions()
    {
        // Create a template that starts with an auto-transition node
        var template = new EncounterTemplate
        {
            Id = "auto_start",
            Name = "Auto Start Test",
            EntryNodeId = "auto",
            Nodes = new Dictionary<string, EncounterNode>
            {
                ["auto"] = new EncounterNode
                {
                    Id = "auto",
                    TextKey = "test.auto",
                    AutoTransition = EncounterOutcome.Goto("real_start")
                },
                ["real_start"] = new EncounterNode
                {
                    Id = "real_start",
                    TextKey = "test.real_start",
                    Options = new List<EncounterOption>
                    {
                        new EncounterOption
                        {
                            Id = "end",
                            TextKey = "test.end",
                            Outcome = EncounterOutcome.End()
                        }
                    }
                }
            }
        };

        var instance = EncounterInstance.Create(template, "test_017");
        runner.Start(instance);

        AssertString(instance.CurrentNodeId).IsEqual("real_start");
    }

    [TestCase]
    public void PirateAmbush_FleeRequiresReflexes()
    {
        var template = TestEncounters.CreatePirateAmbush();
        var instance = EncounterInstance.Create(template, "test_018");

        // Context with low reflexes
        var lowReflexContext = new EncounterContext
        {
            Money = 200,
            Crew = new List<CrewSnapshot>
            {
                new CrewSnapshot { Reflexes = 2, Savvy = 2 }
            }
        };

        var options = runner.GetAvailableOptions(instance, lowReflexContext);

        // Should have fight and surrender, but not flee (needs reflexes 4) or bluff (needs savvy 6)
        AssertInt(options.Count).IsEqual(2);
    }

    [TestCase]
    public void PirateAmbush_AllOptionsAvailableWithHighStats()
    {
        var template = TestEncounters.CreatePirateAmbush();
        var instance = EncounterInstance.Create(template, "test_019");

        var highStatContext = new EncounterContext
        {
            Money = 200,
            Crew = new List<CrewSnapshot>
            {
                new CrewSnapshot { Reflexes = 6, Savvy = 8 }
            }
        };

        var options = runner.GetAvailableOptions(instance, highStatContext);

        // Should have all 4 options
        AssertInt(options.Count).IsEqual(4);
    }

    [TestCase]
    public void NullInstance_ReturnsNullNode()
    {
        var node = runner.GetCurrentNode(null);

        AssertObject(node).IsNull();
    }

    [TestCase]
    public void NullInstance_IsComplete()
    {
        AssertBool(runner.IsComplete(null)).IsTrue();
    }
}
