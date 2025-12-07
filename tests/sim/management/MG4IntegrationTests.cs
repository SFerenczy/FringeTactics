using GdUnit4;
using static GdUnit4.Assertions;
using System.Collections.Generic;

namespace FringeTactics.Tests;

/// <summary>
/// Tests for MG4 Phase 3: Travel Integration and full encounter flow.
/// </summary>
[TestSuite]
public class MG4IntegrationTests
{
    // ========================================================================
    // Helper Methods
    // ========================================================================

    private CampaignState CreateTestCampaign()
    {
        var campaign = CampaignState.CreateForTesting(12345);
        campaign.Money = 1000;
        campaign.Fuel = 100;
        campaign.Parts = 50;
        campaign.Meds = 10;
        campaign.Ammo = 100;
        campaign.Ship = Ship.CreateStarter();
        campaign.Inventory = new Inventory();

        // Add test crew
        campaign.AddCrew("TestCrew1", CrewRole.Soldier);
        campaign.AddCrew("TestCrew2", CrewRole.Tech);

        return campaign;
    }

    private TravelPlan CreateValidTravelPlan(int fuelCost)
    {
        return new TravelPlan
        {
            OriginSystemId = 1,
            DestinationSystemId = 2,
            IsValid = true,
            TotalFuelCost = fuelCost,
            TotalTimeDays = 1,
            Segments = new List<TravelSegment>
            {
                new TravelSegment
                {
                    FromSystemId = 1,
                    ToSystemId = 2,
                    FuelCost = fuelCost,
                    TimeDays = 1
                }
            }
        };
    }

    private TravelPlan CreateInvalidTravelPlan()
    {
        return TravelPlan.Invalid(1, 99, TravelPlanInvalidReason.NoRoute);
    }

    // ========================================================================
    // ConsumeTravelFuel Tests
    // ========================================================================

    [TestCase]
    public void ConsumeTravelFuel_SpendsFuel()
    {
        var campaign = CreateTestCampaign();
        campaign.Fuel = 100;

        var result = campaign.ConsumeTravelFuel(30);

        AssertBool(result).IsTrue();
        AssertInt(campaign.Fuel).IsEqual(70);
    }

    [TestCase]
    public void ConsumeTravelFuel_FailsIfInsufficient()
    {
        var campaign = CreateTestCampaign();
        campaign.Fuel = 20;

        var result = campaign.ConsumeTravelFuel(50);

        AssertBool(result).IsFalse();
        AssertInt(campaign.Fuel).IsEqual(20); // Unchanged
    }

    [TestCase]
    public void ConsumeTravelFuel_ZeroAmountSucceeds()
    {
        var campaign = CreateTestCampaign();
        campaign.Fuel = 100;

        var result = campaign.ConsumeTravelFuel(0);

        AssertBool(result).IsTrue();
        AssertInt(campaign.Fuel).IsEqual(100); // Unchanged
    }

    [TestCase]
    public void ConsumeTravelFuel_ExactAmountSucceeds()
    {
        var campaign = CreateTestCampaign();
        campaign.Fuel = 50;

        var result = campaign.ConsumeTravelFuel(50);

        AssertBool(result).IsTrue();
        AssertInt(campaign.Fuel).IsEqual(0);
    }

    [TestCase]
    public void ConsumeTravelFuel_EmitsResourceChangedEvent()
    {
        var campaign = CreateTestCampaign();
        var eventBus = new EventBus();
        campaign.EventBus = eventBus;
        campaign.Fuel = 100;

        ResourceChangedEvent? receivedEvent = null;
        eventBus.Subscribe<ResourceChangedEvent>(e => receivedEvent = e);

        campaign.ConsumeTravelFuel(25);

        AssertObject(receivedEvent).IsNotNull();
        AssertString(receivedEvent.Value.ResourceType).IsEqual(ResourceTypes.Fuel);
        AssertInt(receivedEvent.Value.Delta).IsEqual(-25);
        AssertString(receivedEvent.Value.Reason).IsEqual("travel");
    }

    // ========================================================================
    // CanAffordTravel (int) Tests
    // ========================================================================

    [TestCase]
    public void CanAffordTravel_Int_ReturnsTrueIfEnoughFuel()
    {
        var campaign = CreateTestCampaign();
        campaign.Fuel = 100;

        var result = campaign.CanAffordTravel(50);

        AssertBool(result).IsTrue();
    }

    [TestCase]
    public void CanAffordTravel_Int_ReturnsFalseIfInsufficientFuel()
    {
        var campaign = CreateTestCampaign();
        campaign.Fuel = 30;

        var result = campaign.CanAffordTravel(50);

        AssertBool(result).IsFalse();
    }

    [TestCase]
    public void CanAffordTravel_Int_ReturnsTrueForExactFuel()
    {
        var campaign = CreateTestCampaign();
        campaign.Fuel = 50;

        var result = campaign.CanAffordTravel(50);

        AssertBool(result).IsTrue();
    }

    [TestCase]
    public void CanAffordTravel_Int_ReturnsTrueForZeroCost()
    {
        var campaign = CreateTestCampaign();
        campaign.Fuel = 0;

        var result = campaign.CanAffordTravel(0);

        AssertBool(result).IsTrue();
    }

    // ========================================================================
    // CanAffordTravel (TravelPlan) Tests
    // ========================================================================

    [TestCase]
    public void CanAffordTravel_Plan_ReturnsTrueForValidAffordablePlan()
    {
        var campaign = CreateTestCampaign();
        campaign.Fuel = 100;
        var plan = CreateValidTravelPlan(50);

        var result = campaign.CanAffordTravel(plan);

        AssertBool(result).IsTrue();
    }

    [TestCase]
    public void CanAffordTravel_Plan_ReturnsFalseForValidUnaffordablePlan()
    {
        var campaign = CreateTestCampaign();
        campaign.Fuel = 30;
        var plan = CreateValidTravelPlan(50);

        var result = campaign.CanAffordTravel(plan);

        AssertBool(result).IsFalse();
    }

    [TestCase]
    public void CanAffordTravel_Plan_ReturnsFalseForInvalidPlan()
    {
        var campaign = CreateTestCampaign();
        campaign.Fuel = 100;
        var plan = CreateInvalidTravelPlan();

        var result = campaign.CanAffordTravel(plan);

        AssertBool(result).IsFalse();
    }

    [TestCase]
    public void CanAffordTravel_Plan_ReturnsFalseForNullPlan()
    {
        var campaign = CreateTestCampaign();
        campaign.Fuel = 100;

        var result = campaign.CanAffordTravel((TravelPlan)null);

        AssertBool(result).IsFalse();
    }

    // ========================================================================
    // GetTravelBlockReason Tests
    // ========================================================================

    [TestCase]
    public void GetTravelBlockReason_ReturnsNullForAffordablePlan()
    {
        var campaign = CreateTestCampaign();
        campaign.Fuel = 100;
        var plan = CreateValidTravelPlan(50);

        var result = campaign.GetTravelBlockReason(plan);

        AssertObject(result).IsNull();
    }

    [TestCase]
    public void GetTravelBlockReason_ReturnsReasonForNullPlan()
    {
        var campaign = CreateTestCampaign();

        var result = campaign.GetTravelBlockReason(null);

        AssertString(result).IsEqual("No travel plan");
    }

    [TestCase]
    public void GetTravelBlockReason_ReturnsReasonForInvalidPlan()
    {
        var campaign = CreateTestCampaign();
        var plan = CreateInvalidTravelPlan();

        var result = campaign.GetTravelBlockReason(plan);

        AssertString(result).Contains("Invalid route");
        AssertString(result).Contains("NoRoute");
    }

    [TestCase]
    public void GetTravelBlockReason_ReturnsReasonForInsufficientFuel()
    {
        var campaign = CreateTestCampaign();
        campaign.Fuel = 30;
        var plan = CreateValidTravelPlan(50);

        var result = campaign.GetTravelBlockReason(plan);

        AssertString(result).Contains("Insufficient fuel");
        AssertString(result).Contains("30/50");
    }

    // ========================================================================
    // Full Encounter Flow Integration Tests
    // ========================================================================

    [TestCase]
    public void FullEncounterFlow_EffectsAppliedAndCleared()
    {
        var campaign = CreateTestCampaign();
        var eventBus = new EventBus();
        campaign.EventBus = eventBus;

        // Create encounter with multiple effects
        var template = new EncounterTemplate
        {
            Id = "test_encounter",
            Name = "Test Encounter",
            EntryNodeId = "start",
            Nodes = new Dictionary<string, EncounterNode>
            {
                { "start", new EncounterNode { Id = "start", TextKey = "test" } }
            }
        };
        var instance = EncounterInstance.Create(template, "test_instance");
        instance.PendingEffects.Add(EncounterEffect.AddCredits(100));
        instance.PendingEffects.Add(EncounterEffect.LoseFuel(10));
        instance.PendingEffects.Add(EncounterEffect.SetFlag("encounter_complete"));

        // Set as active encounter
        campaign.ActiveEncounter = instance;

        // Apply outcome
        var applied = campaign.ApplyEncounterOutcome(instance);

        // Verify
        AssertInt(applied).IsEqual(3);
        AssertInt(campaign.Money).IsEqual(1100);
        AssertInt(campaign.Fuel).IsEqual(90);
        AssertBool(campaign.HasFlag("encounter_complete")).IsTrue();
        AssertObject(campaign.ActiveEncounter).IsNull();
    }

    [TestCase]
    public void FullEncounterFlow_SkillCheckCrewTargeting()
    {
        var campaign = CreateTestCampaign();
        var targetCrew = campaign.Crew[1]; // Second crew member

        var template = new EncounterTemplate
        {
            Id = "test_encounter",
            Name = "Test Encounter",
            EntryNodeId = "start",
            Nodes = new Dictionary<string, EncounterNode>
            {
                { "start", new EncounterNode { Id = "start", TextKey = "test" } }
            }
        };
        var instance = EncounterInstance.Create(template, "test_instance");
        instance.PendingEffects.Add(EncounterEffect.CrewInjury());
        instance.ResolvedParameters["last_check_crew_id"] = targetCrew.Id.ToString();

        campaign.ApplyEncounterOutcome(instance);

        // The targeted crew should be injured
        AssertInt(targetCrew.Injuries.Count).IsGreater(0);
        // The other crew should not be injured
        AssertInt(campaign.Crew[0].Injuries.Count).IsEqual(0);
    }

    [TestCase]
    public void TravelThenEncounter_ResourcesTrackedCorrectly()
    {
        var campaign = CreateTestCampaign();
        campaign.Fuel = 100;
        campaign.Money = 500;

        // Simulate travel consuming fuel
        campaign.ConsumeTravelFuel(30);
        AssertInt(campaign.Fuel).IsEqual(70);

        // Then encounter adds resources
        var template = new EncounterTemplate
        {
            Id = "salvage",
            Name = "Salvage",
            EntryNodeId = "start",
            Nodes = new Dictionary<string, EncounterNode>
            {
                { "start", new EncounterNode { Id = "start", TextKey = "test" } }
            }
        };
        var instance = EncounterInstance.Create(template, "salvage_1");
        instance.PendingEffects.Add(EncounterEffect.AddCredits(200));
        instance.PendingEffects.Add(EncounterEffect.AddFuel(15));

        campaign.ApplyEncounterOutcome(instance);

        AssertInt(campaign.Money).IsEqual(700);
        AssertInt(campaign.Fuel).IsEqual(85);
    }

    [TestCase]
    public void MultipleEncounters_FlagsPreserved()
    {
        var campaign = CreateTestCampaign();

        // First encounter sets a flag
        var template1 = new EncounterTemplate
        {
            Id = "encounter_1",
            Name = "First",
            EntryNodeId = "start",
            Nodes = new Dictionary<string, EncounterNode>
            {
                { "start", new EncounterNode { Id = "start", TextKey = "test" } }
            }
        };
        var instance1 = EncounterInstance.Create(template1, "inst_1");
        instance1.PendingEffects.Add(EncounterEffect.SetFlag("met_npc"));
        campaign.ApplyEncounterOutcome(instance1);

        AssertBool(campaign.HasFlag("met_npc")).IsTrue();

        // Second encounter checks flag (simulated) and sets another
        var template2 = new EncounterTemplate
        {
            Id = "encounter_2",
            Name = "Second",
            EntryNodeId = "start",
            Nodes = new Dictionary<string, EncounterNode>
            {
                { "start", new EncounterNode { Id = "start", TextKey = "test" } }
            }
        };
        var instance2 = EncounterInstance.Create(template2, "inst_2");
        instance2.PendingEffects.Add(EncounterEffect.SetFlag("quest_complete"));
        campaign.ApplyEncounterOutcome(instance2);

        // Both flags should be set
        AssertBool(campaign.HasFlag("met_npc")).IsTrue();
        AssertBool(campaign.HasFlag("quest_complete")).IsTrue();
    }

    // ========================================================================
    // Serialization Tests (Phase 4)
    // ========================================================================

    [TestCase]
    public void Serialization_FlagsPreservedAcrossSaveLoad()
    {
        var original = CreateTestCampaign();
        original.SetFlag("quest_started", true);
        original.SetFlag("npc_met", true);
        original.SetFlag("item_found", false);

        var state = original.GetState();
        var restored = CampaignState.FromState(state);

        AssertBool(restored.HasFlag("quest_started")).IsTrue();
        AssertBool(restored.HasFlag("npc_met")).IsTrue();
        AssertBool(restored.HasFlag("item_found")).IsFalse();
        AssertInt(restored.Flags.Count).IsEqual(3);
    }

    [TestCase]
    public void Serialization_ActiveEncounterSerialized()
    {
        var original = CreateTestCampaign();

        // Create and set active encounter
        var template = new EncounterTemplate
        {
            Id = "test_encounter",
            Name = "Test",
            EntryNodeId = "start",
            Nodes = new Dictionary<string, EncounterNode>
            {
                { "start", new EncounterNode { Id = "start", TextKey = "test" } }
            }
        };
        var instance = EncounterInstance.Create(template, "active_inst");
        instance.PendingEffects.Add(EncounterEffect.AddCredits(100));
        original.ActiveEncounter = instance;

        var state = original.GetState();

        AssertObject(state.ActiveEncounter).IsNotNull();
        AssertString(state.ActiveEncounter.InstanceId).IsEqual("active_inst");
        AssertString(state.ActiveEncounter.TemplateId).IsEqual("test_encounter");
    }

    [TestCase]
    public void Serialization_NoActiveEncounterSerializesNull()
    {
        var original = CreateTestCampaign();
        original.ActiveEncounter = null;

        var state = original.GetState();

        AssertObject(state.ActiveEncounter).IsNull();
    }

    [TestCase]
    public void Serialization_EncounterEffectsAppliedAfterLoad()
    {
        // This tests that after loading, we can still apply encounter outcomes
        var original = CreateTestCampaign();
        original.Money = 500;
        original.SetFlag("before_save", true);

        var state = original.GetState();
        var restored = CampaignState.FromState(state);

        // Create a new encounter and apply it
        var template = new EncounterTemplate
        {
            Id = "post_load_encounter",
            Name = "Post Load",
            EntryNodeId = "start",
            Nodes = new Dictionary<string, EncounterNode>
            {
                { "start", new EncounterNode { Id = "start", TextKey = "test" } }
            }
        };
        var instance = EncounterInstance.Create(template, "post_load_inst");
        instance.PendingEffects.Add(EncounterEffect.AddCredits(200));
        instance.PendingEffects.Add(EncounterEffect.SetFlag("after_load"));

        var applied = restored.ApplyEncounterOutcome(instance);

        AssertInt(applied).IsEqual(2);
        AssertInt(restored.Money).IsEqual(700);
        AssertBool(restored.HasFlag("before_save")).IsTrue();
        AssertBool(restored.HasFlag("after_load")).IsTrue();
    }
}
