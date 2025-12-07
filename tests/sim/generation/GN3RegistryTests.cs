using GdUnit4;
using static GdUnit4.Assertions;
using System.Collections.Generic;
using System.Linq;

namespace FringeTactics.Tests;

[TestSuite]
public class GN3RegistryTests
{
    // ========================================================================
    // Registration Tests
    // ========================================================================

    [TestCase]
    public void Register_ValidTemplate_AddsToRegistry()
    {
        var registry = new EncounterTemplateRegistry();
        var template = CreateTestTemplate("test_1", "travel");

        registry.Register(template);

        AssertInt(registry.Count).IsEqual(1);
        AssertBool(registry.Contains("test_1")).IsTrue();
    }

    [TestCase]
    public void Register_NullTemplate_DoesNothing()
    {
        var registry = new EncounterTemplateRegistry();

        registry.Register(null);

        AssertInt(registry.Count).IsEqual(0);
    }

    [TestCase]
    public void Register_TemplateWithNullId_DoesNothing()
    {
        var registry = new EncounterTemplateRegistry();
        var template = new EncounterTemplate { Id = null, Name = "Test" };

        registry.Register(template);

        AssertInt(registry.Count).IsEqual(0);
    }

    [TestCase]
    public void Register_TemplateWithEmptyId_DoesNothing()
    {
        var registry = new EncounterTemplateRegistry();
        var template = new EncounterTemplate { Id = "", Name = "Test" };

        registry.Register(template);

        AssertInt(registry.Count).IsEqual(0);
    }

    [TestCase]
    public void Register_DuplicateId_OverwritesPrevious()
    {
        var registry = new EncounterTemplateRegistry();
        var template1 = CreateTestTemplate("test_1", "travel");
        template1.Name = "First";
        var template2 = CreateTestTemplate("test_1", "travel");
        template2.Name = "Second";

        registry.Register(template1);
        registry.Register(template2);

        AssertInt(registry.Count).IsEqual(1);
        AssertString(registry.Get("test_1").Name).IsEqual("Second");
    }

    [TestCase]
    public void RegisterAll_MultipleTemplates_AddsAll()
    {
        var registry = new EncounterTemplateRegistry();
        var templates = new List<EncounterTemplate>
        {
            CreateTestTemplate("test_1", "travel"),
            CreateTestTemplate("test_2", "travel"),
            CreateTestTemplate("test_3", "travel")
        };

        registry.RegisterAll(templates);

        AssertInt(registry.Count).IsEqual(3);
    }

    [TestCase]
    public void RegisterAll_NullList_DoesNothing()
    {
        var registry = new EncounterTemplateRegistry();

        registry.RegisterAll(null);

        AssertInt(registry.Count).IsEqual(0);
    }

    // ========================================================================
    // Retrieval Tests
    // ========================================================================

    [TestCase]
    public void Get_ExistingId_ReturnsTemplate()
    {
        var registry = new EncounterTemplateRegistry();
        var template = CreateTestTemplate("test_1", "travel");
        registry.Register(template);

        var result = registry.Get("test_1");

        AssertObject(result).IsNotNull();
        AssertString(result.Id).IsEqual("test_1");
    }

    [TestCase]
    public void Get_NonExistingId_ReturnsNull()
    {
        var registry = new EncounterTemplateRegistry();

        var result = registry.Get("nonexistent");

        AssertObject(result).IsNull();
    }

    [TestCase]
    public void Get_NullId_ReturnsNull()
    {
        var registry = new EncounterTemplateRegistry();

        var result = registry.Get(null);

        AssertObject(result).IsNull();
    }

    [TestCase]
    public void Get_EmptyId_ReturnsNull()
    {
        var registry = new EncounterTemplateRegistry();

        var result = registry.Get("");

        AssertObject(result).IsNull();
    }

    [TestCase]
    public void Contains_ExistingId_ReturnsTrue()
    {
        var registry = new EncounterTemplateRegistry();
        registry.Register(CreateTestTemplate("test_1", "travel"));

        AssertBool(registry.Contains("test_1")).IsTrue();
    }

    [TestCase]
    public void Contains_NonExistingId_ReturnsFalse()
    {
        var registry = new EncounterTemplateRegistry();

        AssertBool(registry.Contains("nonexistent")).IsFalse();
    }

    // ========================================================================
    // Tag Filtering Tests
    // ========================================================================

    [TestCase]
    public void GetByTag_MatchingTag_ReturnsTemplates()
    {
        var registry = new EncounterTemplateRegistry();
        registry.Register(CreateTestTemplate("pirate_1", "travel", "pirate"));
        registry.Register(CreateTestTemplate("pirate_2", "travel", "pirate"));
        registry.Register(CreateTestTemplate("patrol_1", "travel", "patrol"));

        var results = registry.GetByTag("pirate").ToList();

        AssertInt(results.Count).IsEqual(2);
        AssertBool(results.All(t => t.HasTag("pirate"))).IsTrue();
    }

    [TestCase]
    public void GetByTag_NoMatchingTag_ReturnsEmpty()
    {
        var registry = new EncounterTemplateRegistry();
        registry.Register(CreateTestTemplate("test_1", "travel"));

        var results = registry.GetByTag("nonexistent").ToList();

        AssertInt(results.Count).IsEqual(0);
    }

    [TestCase]
    public void GetByTag_NullTag_ReturnsEmpty()
    {
        var registry = new EncounterTemplateRegistry();
        registry.Register(CreateTestTemplate("test_1", "travel"));

        var results = registry.GetByTag(null).ToList();

        AssertInt(results.Count).IsEqual(0);
    }

    [TestCase]
    public void GetByAnyTag_MatchesAny_ReturnsTemplates()
    {
        var registry = new EncounterTemplateRegistry();
        registry.Register(CreateTestTemplate("pirate_1", "travel", "pirate"));
        registry.Register(CreateTestTemplate("patrol_1", "travel", "patrol"));
        registry.Register(CreateTestTemplate("trader_1", "travel", "trader"));

        var results = registry.GetByAnyTag(new[] { "pirate", "patrol" }).ToList();

        AssertInt(results.Count).IsEqual(2);
    }

    [TestCase]
    public void GetByAllTags_MatchesAll_ReturnsTemplates()
    {
        var registry = new EncounterTemplateRegistry();
        registry.Register(CreateTestTemplate("combat_pirate", "travel", "pirate", "combat"));
        registry.Register(CreateTestTemplate("social_pirate", "travel", "pirate", "social"));
        registry.Register(CreateTestTemplate("combat_patrol", "travel", "patrol", "combat"));

        var results = registry.GetByAllTags(new[] { "pirate", "combat" }).ToList();

        AssertInt(results.Count).IsEqual(1);
        AssertString(results[0].Id).IsEqual("combat_pirate");
    }

    // ========================================================================
    // Eligibility Tests
    // ========================================================================

    [TestCase]
    public void GetEligible_TravelTag_ReturnsOnlyTravelEncounters()
    {
        var registry = new EncounterTemplateRegistry();
        registry.Register(CreateTestTemplate("travel_1", "travel"));
        registry.Register(CreateTestTemplate("station_1", "station"));

        var context = new TravelContext();
        var results = registry.GetEligible(context).ToList();

        AssertInt(results.Count).IsEqual(1);
        AssertString(results[0].Id).IsEqual("travel_1");
    }

    [TestCase]
    public void GetEligible_SuggestedType_FiltersToMatchingOrGeneric()
    {
        var registry = new EncounterTemplateRegistry();
        registry.Register(CreateTestTemplate("pirate_1", "travel", "pirate"));
        registry.Register(CreateTestTemplate("patrol_1", "travel", "patrol"));
        registry.Register(CreateTestTemplate("generic_1", "travel", "generic"));

        var context = new TravelContext { SuggestedEncounterType = EncounterTypes.Pirate };
        var results = registry.GetEligible(context).ToList();

        AssertInt(results.Count).IsEqual(2);
        AssertBool(results.Any(t => t.Id == "pirate_1")).IsTrue();
        AssertBool(results.Any(t => t.Id == "generic_1")).IsTrue();
    }

    [TestCase]
    public void GetEligible_RandomType_ReturnsAllTravelEncounters()
    {
        var registry = new EncounterTemplateRegistry();
        registry.Register(CreateTestTemplate("pirate_1", "travel", "pirate"));
        registry.Register(CreateTestTemplate("patrol_1", "travel", "patrol"));
        registry.Register(CreateTestTemplate("trader_1", "travel", "trader"));

        var context = new TravelContext { SuggestedEncounterType = EncounterTypes.Random };
        var results = registry.GetEligible(context).ToList();

        AssertInt(results.Count).IsEqual(3);
    }

    [TestCase]
    public void GetEligible_NullContext_ReturnsEmpty()
    {
        var registry = new EncounterTemplateRegistry();
        registry.Register(CreateTestTemplate("test_1", "travel"));

        var results = registry.GetEligible(null).ToList();

        AssertInt(results.Count).IsEqual(0);
    }

    [TestCase]
    public void GetEligible_RequiredSystemTag_FiltersCorrectly()
    {
        var registry = new EncounterTemplateRegistry();
        var template = CreateTestTemplate("frontier_1", "travel");
        template.RequiredContextKeys = new List<string> { "system_tag:frontier" };
        registry.Register(template);
        registry.Register(CreateTestTemplate("generic_1", "travel"));

        var contextWithTag = new TravelContext
        {
            SystemTags = new HashSet<string> { "frontier" }
        };
        var contextWithoutTag = new TravelContext
        {
            SystemTags = new HashSet<string> { "core" }
        };

        var resultsWithTag = registry.GetEligible(contextWithTag).ToList();
        var resultsWithoutTag = registry.GetEligible(contextWithoutTag).ToList();

        AssertInt(resultsWithTag.Count).IsEqual(2);
        AssertInt(resultsWithoutTag.Count).IsEqual(1);
        AssertString(resultsWithoutTag[0].Id).IsEqual("generic_1");
    }

    // ========================================================================
    // Utility Tests
    // ========================================================================

    [TestCase]
    public void Remove_ExistingId_RemovesTemplate()
    {
        var registry = new EncounterTemplateRegistry();
        registry.Register(CreateTestTemplate("test_1", "travel"));

        bool removed = registry.Remove("test_1");

        AssertBool(removed).IsTrue();
        AssertInt(registry.Count).IsEqual(0);
    }

    [TestCase]
    public void Remove_NonExistingId_ReturnsFalse()
    {
        var registry = new EncounterTemplateRegistry();

        bool removed = registry.Remove("nonexistent");

        AssertBool(removed).IsFalse();
    }

    [TestCase]
    public void Clear_RemovesAllTemplates()
    {
        var registry = new EncounterTemplateRegistry();
        registry.Register(CreateTestTemplate("test_1", "travel"));
        registry.Register(CreateTestTemplate("test_2", "travel"));

        registry.Clear();

        AssertInt(registry.Count).IsEqual(0);
    }

    [TestCase]
    public void GetAllIds_ReturnsAllRegisteredIds()
    {
        var registry = new EncounterTemplateRegistry();
        registry.Register(CreateTestTemplate("test_1", "travel"));
        registry.Register(CreateTestTemplate("test_2", "travel"));

        var ids = registry.GetAllIds().ToList();

        AssertInt(ids.Count).IsEqual(2);
        AssertBool(ids.Contains("test_1")).IsTrue();
        AssertBool(ids.Contains("test_2")).IsTrue();
    }

    [TestCase]
    public void GetAll_ReturnsAllRegisteredTemplates()
    {
        var registry = new EncounterTemplateRegistry();
        registry.Register(CreateTestTemplate("test_1", "travel"));
        registry.Register(CreateTestTemplate("test_2", "travel"));

        var templates = registry.GetAll().ToList();

        AssertInt(templates.Count).IsEqual(2);
    }

    // ========================================================================
    // Factory Tests
    // ========================================================================

    [TestCase]
    public void Create_ReturnsEmptyRegistry()
    {
        var registry = EncounterTemplateRegistry.Create();

        AssertInt(registry.Count).IsEqual(0);
    }

    [TestCase]
    public void CreateForTesting_ReturnsPopulatedRegistry()
    {
        var registry = EncounterTemplateRegistry.CreateForTesting();

        AssertBool(registry.Count > 0).IsTrue();
        AssertBool(registry.Contains("pirate_ambush")).IsTrue();
    }

    // ========================================================================
    // Helper Methods
    // ========================================================================

    private static EncounterTemplate CreateTestTemplate(string id, params string[] tags)
    {
        return new EncounterTemplate
        {
            Id = id,
            Name = $"Test {id}",
            Tags = new HashSet<string>(tags),
            EntryNodeId = "start",
            Nodes = new Dictionary<string, EncounterNode>
            {
                ["start"] = new EncounterNode
                {
                    Id = "start",
                    TextKey = "test.start",
                    AutoTransition = EncounterOutcome.End()
                }
            }
        };
    }
}
