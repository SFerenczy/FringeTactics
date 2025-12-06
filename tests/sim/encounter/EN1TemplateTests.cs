using GdUnit4;
using static GdUnit4.Assertions;

namespace FringeTactics.Tests;

[TestSuite]
public class EN1TemplateTests
{
    [TestCase]
    public void SimpleEncounter_IsValid()
    {
        var template = TestEncounters.CreateSimpleEncounter();

        AssertBool(template.IsValid()).IsTrue();
    }

    [TestCase]
    public void SimpleEncounter_HasCorrectId()
    {
        var template = TestEncounters.CreateSimpleEncounter();

        AssertString(template.Id).IsEqual("test_simple");
    }

    [TestCase]
    public void SimpleEncounter_HasEntryNode()
    {
        var template = TestEncounters.CreateSimpleEncounter();

        AssertObject(template.GetEntryNode()).IsNotNull();
        AssertString(template.EntryNodeId).IsEqual("start");
    }

    [TestCase]
    public void SimpleEncounter_HasTwoNodes()
    {
        var template = TestEncounters.CreateSimpleEncounter();

        AssertInt(template.Nodes.Count).IsEqual(2);
    }

    [TestCase]
    public void SimpleEncounter_StartNodeHasTwoOptions()
    {
        var template = TestEncounters.CreateSimpleEncounter();
        var startNode = template.GetNode("start");

        AssertInt(startNode.Options.Count).IsEqual(2);
    }

    [TestCase]
    public void SimpleEncounter_EndNodeHasAutoTransition()
    {
        var template = TestEncounters.CreateSimpleEncounter();
        var endNode = template.GetNode("end");

        AssertBool(endNode.HasAutoTransition).IsTrue();
        AssertBool(endNode.IsEndNode).IsTrue();
    }

    [TestCase]
    public void ConditionalEncounter_IsValid()
    {
        var template = TestEncounters.CreateConditionalEncounter();

        AssertBool(template.IsValid()).IsTrue();
    }

    [TestCase]
    public void ConditionalEncounter_HasConditionalOptions()
    {
        var template = TestEncounters.CreateConditionalEncounter();
        var startNode = template.GetNode("start");

        var bribeOption = startNode.Options[0];
        AssertInt(bribeOption.Conditions.Count).IsGreater(0);
    }

    [TestCase]
    public void BranchingEncounter_IsValid()
    {
        var template = TestEncounters.CreateBranchingEncounter();

        AssertBool(template.IsValid()).IsTrue();
    }

    [TestCase]
    public void BranchingEncounter_HasSevenNodes()
    {
        var template = TestEncounters.CreateBranchingEncounter();

        AssertInt(template.Nodes.Count).IsEqual(7);
    }

    [TestCase]
    public void BranchingEncounter_HasThreePathsFromStart()
    {
        var template = TestEncounters.CreateBranchingEncounter();
        var startNode = template.GetNode("start");

        AssertInt(startNode.Options.Count).IsEqual(3);
    }

    [TestCase]
    public void PirateAmbush_IsValid()
    {
        var template = TestEncounters.CreatePirateAmbush();

        AssertBool(template.IsValid()).IsTrue();
    }

    [TestCase]
    public void PirateAmbush_HasCorrectTags()
    {
        var template = TestEncounters.CreatePirateAmbush();

        AssertBool(template.HasTag("pirate")).IsTrue();
        AssertBool(template.HasTag("combat")).IsTrue();
        AssertBool(template.HasTag("travel")).IsTrue();
    }

    [TestCase]
    public void PirateAmbush_HasFourOptions()
    {
        var template = TestEncounters.CreatePirateAmbush();
        var introNode = template.GetNode("intro");

        AssertInt(introNode.Options.Count).IsEqual(4);
    }

    [TestCase]
    public void DistressSignal_IsValid()
    {
        var template = TestEncounters.CreateDistressSignal();

        AssertBool(template.IsValid()).IsTrue();
    }

    [TestCase]
    public void GetNode_ReturnsNullForInvalidId()
    {
        var template = TestEncounters.CreateSimpleEncounter();

        AssertObject(template.GetNode("nonexistent")).IsNull();
    }

    [TestCase]
    public void GetNode_ReturnsNullForEmptyId()
    {
        var template = TestEncounters.CreateSimpleEncounter();

        AssertObject(template.GetNode("")).IsNull();
        AssertObject(template.GetNode(null)).IsNull();
    }

    [TestCase]
    public void IsValid_FalseForEmptyId()
    {
        var template = new EncounterTemplate
        {
            Id = "",
            EntryNodeId = "start",
            Nodes = { ["start"] = new EncounterNode { Id = "start" } }
        };

        AssertBool(template.IsValid()).IsFalse();
    }

    [TestCase]
    public void IsValid_FalseForMissingEntryNode()
    {
        var template = new EncounterTemplate
        {
            Id = "test",
            EntryNodeId = "missing",
            Nodes = { ["start"] = new EncounterNode { Id = "start" } }
        };

        AssertBool(template.IsValid()).IsFalse();
    }

    [TestCase]
    public void IsValid_FalseForEmptyNodes()
    {
        var template = new EncounterTemplate
        {
            Id = "test",
            EntryNodeId = "start"
        };

        AssertBool(template.IsValid()).IsFalse();
    }
}
