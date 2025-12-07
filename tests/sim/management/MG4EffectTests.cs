using GdUnit4;
using static GdUnit4.Assertions;
using System.Collections.Generic;

namespace FringeTactics.Tests;

/// <summary>
/// Tests for MG4 Phase 2: Encounter Effect Application.
/// </summary>
[TestSuite]
public class MG4EffectTests
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

    private EncounterInstance CreateTestInstance(List<EncounterEffect> effects = null)
    {
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

        var instance = EncounterInstance.Create(template, "test_instance_1");
        if (effects != null)
        {
            instance.PendingEffects.AddRange(effects);
        }
        return instance;
    }

    // ========================================================================
    // ApplyEncounterOutcome Tests
    // ========================================================================

    [TestCase]
    public void ApplyEncounterOutcome_ReturnsZeroForNullInstance()
    {
        var campaign = CreateTestCampaign();

        var result = campaign.ApplyEncounterOutcome(null);

        AssertInt(result).IsEqual(0);
    }

    [TestCase]
    public void ApplyEncounterOutcome_ReturnsZeroForEmptyEffects()
    {
        var campaign = CreateTestCampaign();
        var instance = CreateTestInstance();

        var result = campaign.ApplyEncounterOutcome(instance);

        AssertInt(result).IsEqual(0);
    }

    [TestCase]
    public void ApplyEncounterOutcome_AppliesAllEffects()
    {
        var campaign = CreateTestCampaign();
        var effects = new List<EncounterEffect>
        {
            EncounterEffect.AddCredits(100),
            EncounterEffect.AddFuel(10)
        };
        var instance = CreateTestInstance(effects);

        var result = campaign.ApplyEncounterOutcome(instance);

        AssertInt(result).IsEqual(2);
        AssertInt(campaign.Money).IsEqual(1100);
        AssertInt(campaign.Fuel).IsEqual(110);
    }

    [TestCase]
    public void ApplyEncounterOutcome_EmitsEvent()
    {
        var campaign = CreateTestCampaign();
        var eventBus = new EventBus();
        campaign.EventBus = eventBus;
        var effects = new List<EncounterEffect> { EncounterEffect.AddCredits(50) };
        var instance = CreateTestInstance(effects);

        EncounterOutcomeAppliedEvent? receivedEvent = null;
        eventBus.Subscribe<EncounterOutcomeAppliedEvent>(e => receivedEvent = e);

        campaign.ApplyEncounterOutcome(instance);

        AssertObject(receivedEvent).IsNotNull();
        AssertString(receivedEvent.Value.EncounterId).IsEqual("test_instance_1");
        AssertInt(receivedEvent.Value.EffectsApplied).IsEqual(1);
        AssertInt(receivedEvent.Value.EffectsTotal).IsEqual(1);
    }

    [TestCase]
    public void ApplyEncounterOutcome_ClearsActiveEncounter()
    {
        var campaign = CreateTestCampaign();
        var instance = CreateTestInstance();
        campaign.ActiveEncounter = instance;

        campaign.ApplyEncounterOutcome(instance);

        AssertObject(campaign.ActiveEncounter).IsNull();
    }

    // ========================================================================
    // Resource Effect Tests
    // ========================================================================

    [TestCase]
    public void ResourceEffect_AddsCredits()
    {
        var campaign = CreateTestCampaign();
        var effects = new List<EncounterEffect> { EncounterEffect.AddCredits(250) };
        var instance = CreateTestInstance(effects);

        campaign.ApplyEncounterOutcome(instance);

        AssertInt(campaign.Money).IsEqual(1250);
    }

    [TestCase]
    public void ResourceEffect_AddsFuel()
    {
        var campaign = CreateTestCampaign();
        var effects = new List<EncounterEffect> { EncounterEffect.AddFuel(25) };
        var instance = CreateTestInstance(effects);

        campaign.ApplyEncounterOutcome(instance);

        AssertInt(campaign.Fuel).IsEqual(125);
    }

    [TestCase]
    public void ResourceEffect_AddsParts()
    {
        var campaign = CreateTestCampaign();
        var effects = new List<EncounterEffect> { EncounterEffect.AddParts(15) };
        var instance = CreateTestInstance(effects);

        campaign.ApplyEncounterOutcome(instance);

        AssertInt(campaign.Parts).IsEqual(65);
    }

    [TestCase]
    public void ResourceEffect_AddsMeds()
    {
        var campaign = CreateTestCampaign();
        var effects = new List<EncounterEffect> { EncounterEffect.AddMeds(5) };
        var instance = CreateTestInstance(effects);

        campaign.ApplyEncounterOutcome(instance);

        AssertInt(campaign.Meds).IsEqual(15);
    }

    [TestCase]
    public void ResourceEffect_RemovesCredits()
    {
        var campaign = CreateTestCampaign();
        var effects = new List<EncounterEffect> { EncounterEffect.LoseCredits(200) };
        var instance = CreateTestInstance(effects);

        campaign.ApplyEncounterOutcome(instance);

        AssertInt(campaign.Money).IsEqual(800);
    }

    [TestCase]
    public void ResourceEffect_RemovesFuel()
    {
        var campaign = CreateTestCampaign();
        var effects = new List<EncounterEffect> { EncounterEffect.LoseFuel(30) };
        var instance = CreateTestInstance(effects);

        campaign.ApplyEncounterOutcome(instance);

        AssertInt(campaign.Fuel).IsEqual(70);
    }

    [TestCase]
    public void ResourceEffect_PartialRemoveIfInsufficient()
    {
        var campaign = CreateTestCampaign();
        campaign.Fuel = 20;
        var effects = new List<EncounterEffect> { EncounterEffect.LoseFuel(50) };
        var instance = CreateTestInstance(effects);

        var result = campaign.ApplyEncounterOutcome(instance);

        AssertInt(result).IsEqual(1); // Still counts as success
        AssertInt(campaign.Fuel).IsEqual(0); // Drained to 0
    }

    [TestCase]
    public void ResourceEffect_EmitsResourceChangedEvent()
    {
        var campaign = CreateTestCampaign();
        var eventBus = new EventBus();
        campaign.EventBus = eventBus;
        var effects = new List<EncounterEffect> { EncounterEffect.AddCredits(100) };
        var instance = CreateTestInstance(effects);

        ResourceChangedEvent? receivedEvent = null;
        eventBus.Subscribe<ResourceChangedEvent>(e => receivedEvent = e);

        campaign.ApplyEncounterOutcome(instance);

        AssertObject(receivedEvent).IsNotNull();
        AssertString(receivedEvent.Value.ResourceType).IsEqual(ResourceTypes.Money);
        AssertInt(receivedEvent.Value.Delta).IsEqual(100);
        AssertString(receivedEvent.Value.Reason).IsEqual("encounter");
    }

    [TestCase]
    public void ResourceEffect_FailsForMissingTargetId()
    {
        var campaign = CreateTestCampaign();
        var effect = new EncounterEffect { Type = EffectType.AddResource, Amount = 100 };
        var instance = CreateTestInstance(new List<EncounterEffect> { effect });

        var result = campaign.ApplyEncounterOutcome(instance);

        AssertInt(result).IsEqual(0);
    }

    // ========================================================================
    // Crew Injury Effect Tests
    // ========================================================================

    [TestCase]
    public void CrewInjuryEffect_InjuresTargetCrew()
    {
        var campaign = CreateTestCampaign();
        var effects = new List<EncounterEffect> { EncounterEffect.CrewInjury() };
        var instance = CreateTestInstance(effects);

        campaign.ApplyEncounterOutcome(instance);

        // At least one crew should be injured
        var injured = campaign.Crew.FindAll(c => c.Injuries.Count > 0);
        AssertInt(injured.Count).IsGreater(0);
    }

    [TestCase]
    public void CrewInjuryEffect_UsesSpecifiedInjuryType()
    {
        var campaign = CreateTestCampaign();
        var effects = new List<EncounterEffect> { EncounterEffect.CrewInjury(InjuryTypes.Critical) };
        var instance = CreateTestInstance(effects);

        campaign.ApplyEncounterOutcome(instance);

        var injured = campaign.Crew.Find(c => c.Injuries.Count > 0);
        AssertObject(injured).IsNotNull();
        AssertBool(injured.Injuries.Contains(InjuryTypes.Critical)).IsTrue();
    }

    [TestCase]
    public void CrewInjuryEffect_UsesSkillCheckCrew()
    {
        var campaign = CreateTestCampaign();
        var targetCrew = campaign.Crew[1]; // Second crew member
        var effects = new List<EncounterEffect> { EncounterEffect.CrewInjury() };
        var instance = CreateTestInstance(effects);
        instance.ResolvedParameters["last_check_crew_id"] = targetCrew.Id.ToString();

        campaign.ApplyEncounterOutcome(instance);

        AssertInt(targetCrew.Injuries.Count).IsGreater(0);
    }

    [TestCase]
    public void CrewInjuryEffect_EmitsCrewInjuredEvent()
    {
        var campaign = CreateTestCampaign();
        var eventBus = new EventBus();
        campaign.EventBus = eventBus;
        var effects = new List<EncounterEffect> { EncounterEffect.CrewInjury() };
        var instance = CreateTestInstance(effects);

        CrewInjuredEvent? receivedEvent = null;
        eventBus.Subscribe<CrewInjuredEvent>(e => receivedEvent = e);

        campaign.ApplyEncounterOutcome(instance);

        AssertObject(receivedEvent).IsNotNull();
        AssertString(receivedEvent.Value.InjuryType).IsEqual(InjuryTypes.Wounded);
    }

    [TestCase]
    public void CrewInjuryEffect_FailsIfNoCrewAvailable()
    {
        var campaign = CreateTestCampaign();
        // Kill all crew
        foreach (var crew in campaign.Crew)
        {
            crew.IsDead = true;
        }
        var effects = new List<EncounterEffect> { EncounterEffect.CrewInjury() };
        var instance = CreateTestInstance(effects);

        var result = campaign.ApplyEncounterOutcome(instance);

        AssertInt(result).IsEqual(0);
    }

    // ========================================================================
    // Crew XP Effect Tests
    // ========================================================================

    [TestCase]
    public void CrewXpEffect_GrantsXp()
    {
        var campaign = CreateTestCampaign();
        var targetCrew = campaign.Crew[0];
        int initialXp = targetCrew.Xp;
        var effects = new List<EncounterEffect> { EncounterEffect.CrewXp(50) };
        var instance = CreateTestInstance(effects);
        instance.ResolvedParameters["last_check_crew_id"] = targetCrew.Id.ToString();

        campaign.ApplyEncounterOutcome(instance);

        AssertInt(targetCrew.Xp).IsEqual(initialXp + 50);
    }

    [TestCase]
    public void CrewXpEffect_HandlesLevelUp()
    {
        var campaign = CreateTestCampaign();
        var eventBus = new EventBus();
        campaign.EventBus = eventBus;
        var targetCrew = campaign.Crew[0];
        targetCrew.Xp = 95; // Close to level up (100 XP needed)
        var effects = new List<EncounterEffect> { EncounterEffect.CrewXp(10) };
        var instance = CreateTestInstance(effects);
        instance.ResolvedParameters["last_check_crew_id"] = targetCrew.Id.ToString();

        CrewLeveledUpEvent? receivedEvent = null;
        eventBus.Subscribe<CrewLeveledUpEvent>(e => receivedEvent = e);

        campaign.ApplyEncounterOutcome(instance);

        AssertObject(receivedEvent).IsNotNull();
        AssertInt(targetCrew.Level).IsEqual(2);
    }

    [TestCase]
    public void CrewXpEffect_ZeroXpIsNoOp()
    {
        var campaign = CreateTestCampaign();
        var targetCrew = campaign.Crew[0];
        int initialXp = targetCrew.Xp;
        var effects = new List<EncounterEffect> { EncounterEffect.CrewXp(0) };
        var instance = CreateTestInstance(effects);

        var result = campaign.ApplyEncounterOutcome(instance);

        AssertInt(result).IsEqual(1); // Still counts as success
        AssertInt(targetCrew.Xp).IsEqual(initialXp);
    }

    // ========================================================================
    // Crew Trait Effect Tests
    // ========================================================================

    [TestCase]
    public void CrewTraitEffect_AddsTrait()
    {
        var campaign = CreateTestCampaign();
        var targetCrew = campaign.Crew[0];
        var effects = new List<EncounterEffect> { EncounterEffect.AddTrait("hardened") };
        var instance = CreateTestInstance(effects);
        instance.ResolvedParameters["last_check_crew_id"] = targetCrew.Id.ToString();

        campaign.ApplyEncounterOutcome(instance);

        AssertBool(targetCrew.HasTrait("hardened")).IsTrue();
    }

    [TestCase]
    public void CrewTraitEffect_RemovesTrait()
    {
        var campaign = CreateTestCampaign();
        var targetCrew = campaign.Crew[0];
        targetCrew.AddTrait("hardened");
        var effects = new List<EncounterEffect> { EncounterEffect.RemoveTrait("hardened") };
        var instance = CreateTestInstance(effects);
        instance.ResolvedParameters["last_check_crew_id"] = targetCrew.Id.ToString();

        campaign.ApplyEncounterOutcome(instance);

        AssertBool(targetCrew.HasTrait("hardened")).IsFalse();
    }

    [TestCase]
    public void CrewTraitEffect_FailsForMissingTraitId()
    {
        var campaign = CreateTestCampaign();
        var effect = new EncounterEffect { Type = EffectType.CrewTrait, BoolParam = true };
        var instance = CreateTestInstance(new List<EncounterEffect> { effect });

        var result = campaign.ApplyEncounterOutcome(instance);

        AssertInt(result).IsEqual(0);
    }

    // ========================================================================
    // Ship Damage Effect Tests
    // ========================================================================

    [TestCase]
    public void ShipDamageEffect_DamagesHull()
    {
        var campaign = CreateTestCampaign();
        int initialHull = campaign.Ship.Hull;
        var effects = new List<EncounterEffect> { EncounterEffect.ShipDamage(15) };
        var instance = CreateTestInstance(effects);

        campaign.ApplyEncounterOutcome(instance);

        AssertInt(campaign.Ship.Hull).IsEqual(initialHull - 15);
    }

    [TestCase]
    public void ShipDamageEffect_EmitsShipHullChangedEvent()
    {
        var campaign = CreateTestCampaign();
        var eventBus = new EventBus();
        campaign.EventBus = eventBus;
        var effects = new List<EncounterEffect> { EncounterEffect.ShipDamage(10) };
        var instance = CreateTestInstance(effects);

        ShipHullChangedEvent? receivedEvent = null;
        eventBus.Subscribe<ShipHullChangedEvent>(e => receivedEvent = e);

        campaign.ApplyEncounterOutcome(instance);

        AssertObject(receivedEvent).IsNotNull();
        AssertString(receivedEvent.Value.Reason).IsEqual("encounter");
    }

    [TestCase]
    public void ShipDamageEffect_ZeroDamageIsNoOp()
    {
        var campaign = CreateTestCampaign();
        int initialHull = campaign.Ship.Hull;
        var effects = new List<EncounterEffect> { EncounterEffect.ShipDamage(0) };
        var instance = CreateTestInstance(effects);

        var result = campaign.ApplyEncounterOutcome(instance);

        AssertInt(result).IsEqual(1);
        AssertInt(campaign.Ship.Hull).IsEqual(initialHull);
    }

    // ========================================================================
    // Faction Rep Effect Tests
    // ========================================================================

    [TestCase]
    public void FactionRepEffect_IncreasesRep()
    {
        var campaign = CreateTestCampaign();
        campaign.FactionRep["test_faction"] = 50;
        var effects = new List<EncounterEffect> { EncounterEffect.FactionRep("test_faction", 10) };
        var instance = CreateTestInstance(effects);

        campaign.ApplyEncounterOutcome(instance);

        AssertInt(campaign.GetFactionRep("test_faction")).IsEqual(60);
    }

    [TestCase]
    public void FactionRepEffect_DecreasesRep()
    {
        var campaign = CreateTestCampaign();
        campaign.FactionRep["test_faction"] = 50;
        var effects = new List<EncounterEffect> { EncounterEffect.FactionRep("test_faction", -15) };
        var instance = CreateTestInstance(effects);

        campaign.ApplyEncounterOutcome(instance);

        AssertInt(campaign.GetFactionRep("test_faction")).IsEqual(35);
    }

    [TestCase]
    public void FactionRepEffect_EmitsFactionRepChangedEvent()
    {
        var campaign = CreateTestCampaign();
        var eventBus = new EventBus();
        campaign.EventBus = eventBus;
        campaign.FactionRep["test_faction"] = 50;
        var effects = new List<EncounterEffect> { EncounterEffect.FactionRep("test_faction", 5) };
        var instance = CreateTestInstance(effects);

        FactionRepChangedEvent? receivedEvent = null;
        eventBus.Subscribe<FactionRepChangedEvent>(e => receivedEvent = e);

        campaign.ApplyEncounterOutcome(instance);

        AssertObject(receivedEvent).IsNotNull();
        AssertString(receivedEvent.Value.FactionId).IsEqual("test_faction");
        AssertInt(receivedEvent.Value.Delta).IsEqual(5);
    }

    [TestCase]
    public void FactionRepEffect_FailsForMissingFactionId()
    {
        var campaign = CreateTestCampaign();
        var effect = new EncounterEffect { Type = EffectType.FactionRep, Amount = 10 };
        var instance = CreateTestInstance(new List<EncounterEffect> { effect });

        var result = campaign.ApplyEncounterOutcome(instance);

        AssertInt(result).IsEqual(0);
    }

    // ========================================================================
    // Set Flag Effect Tests
    // ========================================================================

    [TestCase]
    public void SetFlagEffect_SetsFlag()
    {
        var campaign = CreateTestCampaign();
        var effects = new List<EncounterEffect> { EncounterEffect.SetFlag("quest_complete") };
        var instance = CreateTestInstance(effects);

        campaign.ApplyEncounterOutcome(instance);

        AssertBool(campaign.HasFlag("quest_complete")).IsTrue();
    }

    [TestCase]
    public void SetFlagEffect_SetsFlagToFalse()
    {
        var campaign = CreateTestCampaign();
        campaign.SetFlag("existing_flag", true);
        var effects = new List<EncounterEffect> { EncounterEffect.SetFlag("existing_flag", false) };
        var instance = CreateTestInstance(effects);

        campaign.ApplyEncounterOutcome(instance);

        AssertBool(campaign.HasFlag("existing_flag")).IsFalse();
    }

    [TestCase]
    public void SetFlagEffect_FailsForMissingFlagId()
    {
        var campaign = CreateTestCampaign();
        var effect = new EncounterEffect { Type = EffectType.SetFlag, BoolParam = true };
        var instance = CreateTestInstance(new List<EncounterEffect> { effect });

        var result = campaign.ApplyEncounterOutcome(instance);

        AssertInt(result).IsEqual(0);
    }

    // ========================================================================
    // Time Delay Effect Tests
    // ========================================================================

    [TestCase]
    public void TimeDelayEffect_AdvancesTime()
    {
        var campaign = CreateTestCampaign();
        int initialDay = campaign.Time.CurrentDay;
        var effects = new List<EncounterEffect> { EncounterEffect.TimeDelay(3) };
        var instance = CreateTestInstance(effects);

        campaign.ApplyEncounterOutcome(instance);

        AssertInt(campaign.Time.CurrentDay).IsEqual(initialDay + 3);
    }

    [TestCase]
    public void TimeDelayEffect_ZeroDaysIsNoOp()
    {
        var campaign = CreateTestCampaign();
        int initialDay = campaign.Time.CurrentDay;
        var effects = new List<EncounterEffect> { EncounterEffect.TimeDelay(0) };
        var instance = CreateTestInstance(effects);

        var result = campaign.ApplyEncounterOutcome(instance);

        AssertInt(result).IsEqual(1);
        AssertInt(campaign.Time.CurrentDay).IsEqual(initialDay);
    }

    // ========================================================================
    // Cargo Effect Tests
    // ========================================================================

    [TestCase]
    public void AddCargoEffect_AddsItem()
    {
        var campaign = CreateTestCampaign();
        var effects = new List<EncounterEffect> { EncounterEffect.AddCargo("medkit", 2) };
        var instance = CreateTestInstance(effects);

        campaign.ApplyEncounterOutcome(instance);

        AssertBool(campaign.HasItem("medkit", 2)).IsTrue();
    }

    [TestCase]
    public void AddCargoEffect_FailsForMissingItemId()
    {
        var campaign = CreateTestCampaign();
        var effect = new EncounterEffect { Type = EffectType.AddCargo, Amount = 1 };
        var instance = CreateTestInstance(new List<EncounterEffect> { effect });

        var result = campaign.ApplyEncounterOutcome(instance);

        AssertInt(result).IsEqual(0);
    }

    [TestCase]
    public void RemoveCargoEffect_RemovesItem()
    {
        var campaign = CreateTestCampaign();
        campaign.AddItem("medkit", 3);
        var effects = new List<EncounterEffect> { EncounterEffect.RemoveCargo("medkit", 2) };
        var instance = CreateTestInstance(effects);

        campaign.ApplyEncounterOutcome(instance);

        AssertBool(campaign.HasItem("medkit", 1)).IsTrue();
        AssertBool(campaign.HasItem("medkit", 2)).IsFalse();
    }

    [TestCase]
    public void RemoveCargoEffect_FailsIfNotEnough()
    {
        var campaign = CreateTestCampaign();
        campaign.AddItem("medkit", 1);
        var effects = new List<EncounterEffect> { EncounterEffect.RemoveCargo("medkit", 5) };
        var instance = CreateTestInstance(effects);

        var result = campaign.ApplyEncounterOutcome(instance);

        AssertInt(result).IsEqual(0);
        AssertBool(campaign.HasItem("medkit", 1)).IsTrue(); // Item not removed
    }

    [TestCase]
    public void RemoveCargoEffect_FailsForMissingItemId()
    {
        var campaign = CreateTestCampaign();
        var effect = new EncounterEffect { Type = EffectType.RemoveCargo, Amount = 1 };
        var instance = CreateTestInstance(new List<EncounterEffect> { effect });

        var result = campaign.ApplyEncounterOutcome(instance);

        AssertInt(result).IsEqual(0);
    }

    // ========================================================================
    // Flow Effect Tests (No-ops in Campaign)
    // ========================================================================

    [TestCase]
    public void GotoNodeEffect_ReturnsTrue()
    {
        var campaign = CreateTestCampaign();
        var effect = new EncounterEffect { Type = EffectType.GotoNode, TargetId = "some_node" };
        var instance = CreateTestInstance(new List<EncounterEffect> { effect });

        var result = campaign.ApplyEncounterOutcome(instance);

        AssertInt(result).IsEqual(1);
    }

    [TestCase]
    public void EndEncounterEffect_ReturnsTrue()
    {
        var campaign = CreateTestCampaign();
        var effect = new EncounterEffect { Type = EffectType.EndEncounter };
        var instance = CreateTestInstance(new List<EncounterEffect> { effect });

        var result = campaign.ApplyEncounterOutcome(instance);

        AssertInt(result).IsEqual(1);
    }

    [TestCase]
    public void TriggerTacticalEffect_ReturnsTrue()
    {
        var campaign = CreateTestCampaign();
        var effect = EncounterEffect.TriggerTactical("ambush");
        var instance = CreateTestInstance(new List<EncounterEffect> { effect });

        var result = campaign.ApplyEncounterOutcome(instance);

        AssertInt(result).IsEqual(1);
    }

    // ========================================================================
    // Multiple Effects Tests
    // ========================================================================

    [TestCase]
    public void MultipleEffects_AllApplied()
    {
        var campaign = CreateTestCampaign();
        var targetCrew = campaign.Crew[0];
        var effects = new List<EncounterEffect>
        {
            EncounterEffect.AddCredits(100),
            EncounterEffect.LoseFuel(10),
            EncounterEffect.ShipDamage(5),
            EncounterEffect.SetFlag("encounter_done")
        };
        var instance = CreateTestInstance(effects);

        var result = campaign.ApplyEncounterOutcome(instance);

        AssertInt(result).IsEqual(4);
        AssertInt(campaign.Money).IsEqual(1100);
        AssertInt(campaign.Fuel).IsEqual(90);
        AssertInt(campaign.Ship.Hull).IsEqual(45);
        AssertBool(campaign.HasFlag("encounter_done")).IsTrue();
    }

    [TestCase]
    public void MultipleEffects_PartialSuccess()
    {
        var campaign = CreateTestCampaign();
        var effects = new List<EncounterEffect>
        {
            EncounterEffect.AddCredits(100), // Should succeed
            new EncounterEffect { Type = EffectType.FactionRep, Amount = 10 }, // Should fail (no faction ID)
            EncounterEffect.AddFuel(20) // Should succeed
        };
        var instance = CreateTestInstance(effects);

        var result = campaign.ApplyEncounterOutcome(instance);

        AssertInt(result).IsEqual(2); // 2 of 3 succeeded
        AssertInt(campaign.Money).IsEqual(1100);
        AssertInt(campaign.Fuel).IsEqual(120);
    }
}
