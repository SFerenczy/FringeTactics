using GdUnit4;
using static GdUnit4.Assertions;
using Godot;
using System.Collections.Generic;
using System.Linq;

namespace FringeTactics.Tests;

[TestSuite]
public class GN3GeneratorTests
{
    private EncounterTemplateRegistry registry;
    private EncounterGenerator generator;
    private CampaignState campaign;

    [Before]
    public void Setup()
    {
        registry = new EncounterTemplateRegistry();
        generator = new EncounterGenerator(registry);
        campaign = CampaignState.CreateForTesting(12345);
    }

    // ========================================================================
    // Basic Generation Tests
    // ========================================================================

    [TestCase]
    public void Generate_WithEligibleTemplates_ReturnsInstance()
    {
        registry.Register(CreateTravelTemplate("test_1"));
        var context = CreateBasicContext();

        var instance = generator.Generate(context, campaign);

        AssertObject(instance).IsNotNull();
        AssertString(instance.Template.Id).IsEqual("test_1");
    }

    [TestCase]
    public void Generate_NoEligibleTemplates_ReturnsNull()
    {
        // Use fresh registry with only station templates (not travel)
        var freshRegistry = new EncounterTemplateRegistry();
        freshRegistry.Register(CreateStationTemplate("station_1"));
        var freshGenerator = new EncounterGenerator(freshRegistry);
        var context = CreateBasicContext();

        var instance = freshGenerator.Generate(context, campaign);

        AssertObject(instance).IsNull();
    }

    [TestCase]
    public void Generate_EmptyRegistry_ReturnsNull()
    {
        // Use fresh empty registry
        var emptyRegistry = new EncounterTemplateRegistry();
        var emptyGenerator = new EncounterGenerator(emptyRegistry);
        var context = CreateBasicContext();

        var instance = emptyGenerator.Generate(context, campaign);

        AssertObject(instance).IsNull();
    }

    [TestCase]
    public void Generate_NullContext_ReturnsNull()
    {
        registry.Register(CreateTravelTemplate("test_1"));

        var instance = generator.Generate(null, campaign);

        AssertObject(instance).IsNull();
    }

    [TestCase]
    public void Generate_NullCampaign_ReturnsNull()
    {
        registry.Register(CreateTravelTemplate("test_1"));
        var context = CreateBasicContext();

        var instance = generator.Generate(context, null);

        AssertObject(instance).IsNull();
    }

    // ========================================================================
    // Determinism Tests
    // ========================================================================

    [TestCase]
    public void Generate_SameSeed_SameTemplate()
    {
        registry.Register(CreateTravelTemplate("template_a", "pirate"));
        registry.Register(CreateTravelTemplate("template_b", "patrol"));
        registry.Register(CreateTravelTemplate("template_c", "trader"));

        var context = CreateBasicContext();

        var campaign1 = CampaignState.CreateForTesting(99999);
        var campaign2 = CampaignState.CreateForTesting(99999);

        var instance1 = generator.Generate(context, campaign1);
        var instance2 = generator.Generate(context, campaign2);

        AssertString(instance1.Template.Id).IsEqual(instance2.Template.Id);
    }

    [TestCase]
    public void Generate_DifferentSeeds_MaySelectDifferentTemplates()
    {
        registry.Register(CreateTravelTemplate("template_a", "pirate"));
        registry.Register(CreateTravelTemplate("template_b", "patrol"));
        registry.Register(CreateTravelTemplate("template_c", "trader"));

        var context = CreateBasicContext();
        var selectedTemplates = new HashSet<string>();

        // Try many different seeds
        for (int seed = 1; seed <= 50; seed++)
        {
            var testCampaign = CampaignState.CreateForTesting(seed);
            var instance = generator.Generate(context, testCampaign);
            if (instance != null)
                selectedTemplates.Add(instance.Template.Id);
        }

        // Should have selected at least 2 different templates
        AssertBool(selectedTemplates.Count >= 2).IsTrue();
    }

    // ========================================================================
    // Weighting Tests
    // ========================================================================

    [TestCase]
    public void CalculateWeights_PirateTemplate_HigherWithCriminalActivity()
    {
        var pirateTemplate = CreateTravelTemplate("pirate_1", "pirate");

        var lowCrimeContext = CreateBasicContext();
        lowCrimeContext.SystemMetrics = new SystemMetrics { CriminalActivity = 1 };

        var highCrimeContext = CreateBasicContext();
        highCrimeContext.SystemMetrics = new SystemMetrics { CriminalActivity = 5 };

        float lowWeight = generator.GetTemplateWeight(pirateTemplate, lowCrimeContext);
        float highWeight = generator.GetTemplateWeight(pirateTemplate, highCrimeContext);

        AssertBool(highWeight > lowWeight).IsTrue();
    }

    [TestCase]
    public void CalculateWeights_PatrolTemplate_HigherWithSecurity()
    {
        var patrolTemplate = CreateTravelTemplate("patrol_1", "patrol");

        var lowSecContext = CreateBasicContext();
        lowSecContext.SystemMetrics = new SystemMetrics { SecurityLevel = 1 };

        var highSecContext = CreateBasicContext();
        highSecContext.SystemMetrics = new SystemMetrics { SecurityLevel = 5 };

        float lowWeight = generator.GetTemplateWeight(patrolTemplate, lowSecContext);
        float highWeight = generator.GetTemplateWeight(patrolTemplate, highSecContext);

        AssertBool(highWeight > lowWeight).IsTrue();
    }

    [TestCase]
    public void CalculateWeights_TraderTemplate_HigherWithEconomicActivity()
    {
        var traderTemplate = CreateTravelTemplate("trader_1", "trader");

        var lowEconContext = CreateBasicContext();
        lowEconContext.SystemMetrics = new SystemMetrics { EconomicActivity = 1 };

        var highEconContext = CreateBasicContext();
        highEconContext.SystemMetrics = new SystemMetrics { EconomicActivity = 5 };

        float lowWeight = generator.GetTemplateWeight(traderTemplate, lowEconContext);
        float highWeight = generator.GetTemplateWeight(traderTemplate, highEconContext);

        AssertBool(highWeight > lowWeight).IsTrue();
    }

    [TestCase]
    public void CalculateWeights_CombatTemplate_HigherWithRouteHazard()
    {
        var combatTemplate = CreateTravelTemplate("combat_1", "combat");

        var safeContext = CreateBasicContext();
        safeContext.RouteHazard = 0;

        var dangerousContext = CreateBasicContext();
        dangerousContext.RouteHazard = 5;

        float safeWeight = generator.GetTemplateWeight(combatTemplate, safeContext);
        float dangerousWeight = generator.GetTemplateWeight(combatTemplate, dangerousContext);

        AssertBool(dangerousWeight > safeWeight).IsTrue();
    }

    [TestCase]
    public void CalculateWeights_RareTemplate_LowerWeight()
    {
        var normalTemplate = CreateTravelTemplate("normal_1");
        var rareTemplate = CreateTravelTemplate("rare_1", "rare");

        var context = CreateBasicContext();

        float normalWeight = generator.GetTemplateWeight(normalTemplate, context);
        float rareWeight = generator.GetTemplateWeight(rareTemplate, context);

        AssertBool(rareWeight < normalWeight).IsTrue();
    }

    [TestCase]
    public void CalculateWeights_SuggestedType_GetsBoost()
    {
        var pirateTemplate = CreateTravelTemplate("pirate_1", "pirate");
        var patrolTemplate = CreateTravelTemplate("patrol_1", "patrol");

        var context = CreateBasicContext();
        context.SuggestedEncounterType = EncounterTypes.Pirate;

        float pirateWeight = generator.GetTemplateWeight(pirateTemplate, context);
        float patrolWeight = generator.GetTemplateWeight(patrolTemplate, context);

        // Pirate should have higher weight due to suggested type boost
        AssertBool(pirateWeight > patrolWeight).IsTrue();
    }

    [TestCase]
    public void CalculateWeights_CargoTemplate_HigherWithValuableCargo()
    {
        var cargoTemplate = CreateTravelTemplate("cargo_1", "cargo");

        var noCargoContext = CreateBasicContext();
        noCargoContext.CargoValue = 0;

        var valuableCargoContext = CreateBasicContext();
        valuableCargoContext.CargoValue = 500;

        float noCargoWeight = generator.GetTemplateWeight(cargoTemplate, noCargoContext);
        float valuableWeight = generator.GetTemplateWeight(cargoTemplate, valuableCargoContext);

        AssertBool(valuableWeight > noCargoWeight).IsTrue();
    }

    // ========================================================================
    // Parameter Resolution Tests
    // ========================================================================

    [TestCase]
    public void Generate_ResolvesNpcNames()
    {
        registry.Register(CreateTravelTemplate("test_1"));
        var context = CreateBasicContext();

        var instance = generator.Generate(context, campaign);

        AssertString(instance.GetParameter("npc_name")).IsNotEmpty();
        AssertString(instance.GetParameter("pirate_name")).IsNotEmpty();
        AssertString(instance.GetParameter("captain_name")).IsNotEmpty();
    }

    [TestCase]
    public void Generate_ResolvesShipNames()
    {
        registry.Register(CreateTravelTemplate("test_1"));
        var context = CreateBasicContext();

        var instance = generator.Generate(context, campaign);

        AssertString(instance.GetParameter("ship_name")).IsNotEmpty();
        AssertString(instance.GetParameter("pirate_ship")).IsNotEmpty();
    }

    [TestCase]
    public void Generate_ResolvesCargoTypes()
    {
        registry.Register(CreateTravelTemplate("test_1"));
        var context = CreateBasicContext();

        var instance = generator.Generate(context, campaign);

        AssertString(instance.GetParameter("cargo_type")).IsNotEmpty();
        AssertString(instance.GetParameter("valuable_cargo")).IsNotEmpty();
        AssertString(instance.GetParameter("illegal_cargo")).IsNotEmpty();
    }

    [TestCase]
    public void Generate_ResolvesCreditsValues()
    {
        registry.Register(CreateTravelTemplate("test_1"));
        var context = CreateBasicContext();
        context.RouteHazard = 3;

        var instance = generator.Generate(context, campaign);

        // Base = 50 + (3 * 20) = 110
        AssertString(instance.GetParameter("small_credits")).IsEqual("55");
        AssertString(instance.GetParameter("medium_credits")).IsEqual("110");
        AssertString(instance.GetParameter("large_credits")).IsEqual("220");
    }

    [TestCase]
    public void Generate_ResolvesContextFlags()
    {
        registry.Register(CreateTravelTemplate("test_1"));
        var context = CreateBasicContext();
        context.CargoValue = 100;
        context.HasIllegalCargo = true;
        context.CrewCount = 4;

        var instance = generator.Generate(context, campaign);

        AssertString(instance.GetParameter("has_cargo")).IsEqual("true");
        AssertString(instance.GetParameter("has_illegal")).IsEqual("true");
        AssertString(instance.GetParameter("crew_count")).IsEqual("4");
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Generate_ResolvesSystemInfo()
    {
        registry.Register(CreateTravelTemplate("test_1"));
        var context = CreateBasicContext();
        context.CurrentSystem = new StarSystem(1, "Test Haven", SystemType.Station, Vector2.Zero);
        context.SystemMetrics = new SystemMetrics
        {
            SecurityLevel = 4,
            CriminalActivity = 2,
            EconomicActivity = 3
        };

        var instance = generator.Generate(context, campaign);

        AssertString(instance.GetParameter("system_name")).IsEqual("Test Haven");
        AssertString(instance.GetParameter("security_level")).IsEqual("4");
        AssertString(instance.GetParameter("criminal_activity")).IsEqual("2");
    }

    [TestCase]
    public void Generate_ParametersAreDeterministic()
    {
        registry.Register(CreateTravelTemplate("test_1"));
        var context = CreateBasicContext();

        var campaign1 = CampaignState.CreateForTesting(88888);
        var campaign2 = CampaignState.CreateForTesting(88888);

        var instance1 = generator.Generate(context, campaign1);
        var instance2 = generator.Generate(context, campaign2);

        AssertString(instance1.GetParameter("npc_name")).IsEqual(instance2.GetParameter("npc_name"));
        AssertString(instance1.GetParameter("ship_name")).IsEqual(instance2.GetParameter("ship_name"));
        AssertString(instance1.GetParameter("cargo_type")).IsEqual(instance2.GetParameter("cargo_type"));
    }

    // ========================================================================
    // Integration with TestEncounters
    // ========================================================================

    [TestCase]
    public void Generate_WithTestEncounters_Works()
    {
        var testRegistry = EncounterTemplateRegistry.CreateForTesting();
        var testGenerator = new EncounterGenerator(testRegistry);

        var context = CreateBasicContext();
        context.SuggestedEncounterType = EncounterTypes.Pirate;

        var instance = testGenerator.Generate(context, campaign);

        // Should select pirate_ambush (has travel and pirate tags)
        AssertObject(instance).IsNotNull();
        AssertString(instance.Template.Id).IsEqual("pirate_ambush");
    }

    // ========================================================================
    // Helper Methods
    // ========================================================================

    private static EncounterTemplate CreateTravelTemplate(string id, params string[] extraTags)
    {
        var tags = new HashSet<string> { EncounterTags.Travel };
        foreach (var tag in extraTags)
            tags.Add(tag);

        return new EncounterTemplate
        {
            Id = id,
            Name = $"Test {id}",
            Tags = tags,
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

    private static EncounterTemplate CreateStationTemplate(string id)
    {
        return new EncounterTemplate
        {
            Id = id,
            Name = $"Station {id}",
            Tags = new HashSet<string> { EncounterTags.Station },
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

    private static TravelContext CreateBasicContext()
    {
        return new TravelContext
        {
            CurrentSystemId = 1,
            DestinationSystemId = 2,
            RouteHazard = 2,
            SystemMetrics = new SystemMetrics
            {
                SecurityLevel = 3,
                CriminalActivity = 2,
                EconomicActivity = 3
            }
        };
    }
}
