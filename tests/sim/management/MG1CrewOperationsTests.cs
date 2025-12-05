using GdUnit4;
using static GdUnit4.Assertions;
using System.Collections.Generic;

namespace FringeTactics.Tests;

/// <summary>
/// MG1 Phase 3 tests - validates crew operations (hire, fire, traits).
/// </summary>
[TestSuite]
public class MG1CrewOperationsTests
{
    // === HireCrew Tests ===

    [TestCase]
    public void HireCrew_Success_DeductsCostAndAddsCrew()
    {
        var campaign = CampaignState.CreateNew();
        campaign.Money = 500;
        int initialCrewCount = campaign.Crew.Count;

        var crew = campaign.HireCrew("NewRecruit", CrewRole.Soldier, 100);

        AssertThat(crew).IsNotNull();
        AssertThat(campaign.Money).IsEqual(400);
        AssertThat(campaign.Crew.Count).IsEqual(initialCrewCount + 1);
        AssertThat(campaign.Crew.Contains(crew)).IsTrue();
    }

    [TestCase]
    public void HireCrew_InsufficientFunds_ReturnsNull()
    {
        var campaign = CampaignState.CreateNew();
        campaign.Money = 50;

        var crew = campaign.HireCrew("NewRecruit", CrewRole.Soldier, 100);

        AssertThat(crew).IsNull();
        AssertThat(campaign.Money).IsEqual(50);
    }

    [TestCase]
    public void HireCrew_ExactFunds_Succeeds()
    {
        var campaign = CampaignState.CreateNew();
        campaign.Money = 100;

        var crew = campaign.HireCrew("NewRecruit", CrewRole.Soldier, 100);

        AssertThat(crew).IsNotNull();
        AssertThat(campaign.Money).IsEqual(0);
    }

    [TestCase]
    public void HireCrew_HasRoleStats()
    {
        var campaign = CampaignState.CreateNew();
        campaign.Money = 500;

        var crew = campaign.HireCrew("Techie", CrewRole.Tech, 100);

        AssertThat(crew.Tech).IsEqual(3);
        AssertThat(crew.Role).IsEqual(CrewRole.Tech);
    }

    [TestCase]
    public void HireCrew_AssignsUniqueId()
    {
        var campaign = CampaignState.CreateNew();
        campaign.Money = 1000;

        var crew1 = campaign.HireCrew("First", CrewRole.Soldier, 100);
        var crew2 = campaign.HireCrew("Second", CrewRole.Soldier, 100);

        AssertThat(crew1.Id).IsNotEqual(crew2.Id);
    }

    [TestCase]
    public void HireCrew_PublishesEvents()
    {
        var campaign = CampaignState.CreateNew();
        campaign.Money = 500;
        var eventBus = new EventBus();
        campaign.EventBus = eventBus;

        var resourceEvents = new List<ResourceChangedEvent>();
        var hireEvents = new List<CrewHiredEvent>();

        eventBus.Subscribe<ResourceChangedEvent>(e => resourceEvents.Add(e));
        eventBus.Subscribe<CrewHiredEvent>(e => hireEvents.Add(e));

        campaign.HireCrew("NewGuy", CrewRole.Scout, 150);

        AssertThat(resourceEvents.Count).IsEqual(1);
        AssertThat(resourceEvents[0].ResourceType).IsEqual(ResourceTypes.Money);
        AssertThat(resourceEvents[0].Delta).IsEqual(-150);

        AssertThat(hireEvents.Count).IsEqual(1);
        AssertThat(hireEvents[0].CrewName).IsEqual("NewGuy");
        AssertThat(hireEvents[0].Role).IsEqual(CrewRole.Scout);
        AssertThat(hireEvents[0].Cost).IsEqual(150);
    }

    // === FireCrew Tests ===

    [TestCase]
    public void FireCrew_Success_RemovesFromRoster()
    {
        var campaign = CampaignState.CreateNew();
        int initialCount = campaign.Crew.Count;
        var crewToFire = campaign.Crew[0];
        int crewId = crewToFire.Id;

        bool result = campaign.FireCrew(crewId);

        AssertThat(result).IsTrue();
        AssertThat(campaign.Crew.Count).IsEqual(initialCount - 1);
        AssertThat(campaign.GetCrewById(crewId)).IsNull();
    }

    [TestCase]
    public void FireCrew_NotFound_ReturnsFalse()
    {
        var campaign = CampaignState.CreateNew();

        bool result = campaign.FireCrew(99999);

        AssertThat(result).IsFalse();
    }

    [TestCase]
    public void FireCrew_DeadCrew_ReturnsFalse()
    {
        var campaign = CampaignState.CreateNew();
        var crew = campaign.Crew[0];
        crew.IsDead = true;

        bool result = campaign.FireCrew(crew.Id);

        AssertThat(result).IsFalse();
    }

    [TestCase]
    public void FireCrew_LastAliveCrew_ReturnsFalse()
    {
        var campaign = CampaignState.CreateNew();
        
        // Kill all but one
        for (int i = 1; i < campaign.Crew.Count; i++)
        {
            campaign.Crew[i].IsDead = true;
        }
        
        var lastAlive = campaign.GetAliveCrew()[0];

        bool result = campaign.FireCrew(lastAlive.Id);

        AssertThat(result).IsFalse();
        AssertThat(campaign.Crew.Contains(lastAlive)).IsTrue();
    }

    [TestCase]
    public void FireCrew_PublishesEvent()
    {
        var campaign = CampaignState.CreateNew();
        var eventBus = new EventBus();
        campaign.EventBus = eventBus;

        var fireEvents = new List<CrewFiredEvent>();
        eventBus.Subscribe<CrewFiredEvent>(e => fireEvents.Add(e));

        var crewToFire = campaign.Crew[0];
        string name = crewToFire.Name;
        int id = crewToFire.Id;

        campaign.FireCrew(id);

        AssertThat(fireEvents.Count).IsEqual(1);
        AssertThat(fireEvents[0].CrewId).IsEqual(id);
        AssertThat(fireEvents[0].CrewName).IsEqual(name);
    }

    // === AssignTrait Tests ===

    [TestCase]
    public void AssignTrait_Success()
    {
        var campaign = CampaignState.CreateNew();
        var crew = campaign.Crew[0];

        bool result = campaign.AssignTrait(crew.Id, "brave");

        AssertThat(result).IsTrue();
        AssertThat(crew.HasTrait("brave")).IsTrue();
    }

    [TestCase]
    public void AssignTrait_CrewNotFound_ReturnsFalse()
    {
        var campaign = CampaignState.CreateNew();

        bool result = campaign.AssignTrait(99999, "brave");

        AssertThat(result).IsFalse();
    }

    [TestCase]
    public void AssignTrait_DeadCrew_ReturnsFalse()
    {
        var campaign = CampaignState.CreateNew();
        var crew = campaign.Crew[0];
        crew.IsDead = true;

        bool result = campaign.AssignTrait(crew.Id, "brave");

        AssertThat(result).IsFalse();
    }

    [TestCase]
    public void AssignTrait_AlreadyHasTrait_ReturnsFalse()
    {
        var campaign = CampaignState.CreateNew();
        var crew = campaign.Crew[0];
        crew.AddTrait("brave");

        bool result = campaign.AssignTrait(crew.Id, "brave");

        AssertThat(result).IsFalse();
    }

    [TestCase]
    public void AssignTrait_InvalidTrait_ReturnsFalse()
    {
        var campaign = CampaignState.CreateNew();
        var crew = campaign.Crew[0];

        bool result = campaign.AssignTrait(crew.Id, "nonexistent_trait");

        AssertThat(result).IsFalse();
    }

    [TestCase]
    public void AssignTrait_PublishesEvent()
    {
        var campaign = CampaignState.CreateNew();
        var eventBus = new EventBus();
        campaign.EventBus = eventBus;

        var traitEvents = new List<CrewTraitChangedEvent>();
        eventBus.Subscribe<CrewTraitChangedEvent>(e => traitEvents.Add(e));

        var crew = campaign.Crew[0];
        campaign.AssignTrait(crew.Id, "ex_military");

        AssertThat(traitEvents.Count).IsEqual(1);
        AssertThat(traitEvents[0].CrewId).IsEqual(crew.Id);
        AssertThat(traitEvents[0].TraitId).IsEqual("ex_military");
        AssertThat(traitEvents[0].TraitName).IsEqual("Ex-Military");
        AssertThat(traitEvents[0].Gained).IsTrue();
    }

    // === RemoveTrait Tests ===

    [TestCase]
    public void RemoveTrait_Success()
    {
        var campaign = CampaignState.CreateNew();
        var crew = campaign.Crew[0];
        crew.AddTrait("brave");

        bool result = campaign.RemoveTrait(crew.Id, "brave");

        AssertThat(result).IsTrue();
        AssertThat(crew.HasTrait("brave")).IsFalse();
    }

    [TestCase]
    public void RemoveTrait_CrewNotFound_ReturnsFalse()
    {
        var campaign = CampaignState.CreateNew();

        bool result = campaign.RemoveTrait(99999, "brave");

        AssertThat(result).IsFalse();
    }

    [TestCase]
    public void RemoveTrait_DoesntHaveTrait_ReturnsFalse()
    {
        var campaign = CampaignState.CreateNew();
        var crew = campaign.Crew[0];

        bool result = campaign.RemoveTrait(crew.Id, "brave");

        AssertThat(result).IsFalse();
    }

    [TestCase]
    public void RemoveTrait_PermanentTrait_ReturnsFalse()
    {
        var campaign = CampaignState.CreateNew();
        var crew = campaign.Crew[0];
        crew.AddTrait("damaged_eye");

        bool result = campaign.RemoveTrait(crew.Id, "damaged_eye");

        AssertThat(result).IsFalse();
        AssertThat(crew.HasTrait("damaged_eye")).IsTrue();
    }

    [TestCase]
    public void RemoveTrait_PublishesEvent()
    {
        var campaign = CampaignState.CreateNew();
        var eventBus = new EventBus();
        campaign.EventBus = eventBus;

        var traitEvents = new List<CrewTraitChangedEvent>();
        eventBus.Subscribe<CrewTraitChangedEvent>(e => traitEvents.Add(e));

        var crew = campaign.Crew[0];
        crew.AddTrait("brave");
        campaign.RemoveTrait(crew.Id, "brave");

        AssertThat(traitEvents.Count).IsEqual(1);
        AssertThat(traitEvents[0].CrewId).IsEqual(crew.Id);
        AssertThat(traitEvents[0].TraitId).IsEqual("brave");
        AssertThat(traitEvents[0].Gained).IsFalse();
    }

    // === Integration Tests ===

    [TestCase]
    public void HireAndFire_Workflow()
    {
        var campaign = CampaignState.CreateNew();
        campaign.Money = 500;
        int initialCount = campaign.Crew.Count;

        // Hire
        var hired = campaign.HireCrew("TempWorker", CrewRole.Soldier, 100);
        AssertThat(campaign.Crew.Count).IsEqual(initialCount + 1);
        AssertThat(campaign.Money).IsEqual(400);

        // Fire
        bool fired = campaign.FireCrew(hired.Id);
        AssertThat(fired).IsTrue();
        AssertThat(campaign.Crew.Count).IsEqual(initialCount);
    }

    [TestCase]
    public void AssignAndRemoveTrait_Workflow()
    {
        var campaign = CampaignState.CreateNew();
        var crew = campaign.Crew[0];
        int baseAim = crew.Aim;

        // Assign trait
        campaign.AssignTrait(crew.Id, "ex_military");
        AssertThat(crew.GetEffectiveStat(CrewStatType.Aim)).IsEqual(baseAim + 1);

        // Remove trait
        campaign.RemoveTrait(crew.Id, "ex_military");
        AssertThat(crew.GetEffectiveStat(CrewStatType.Aim)).IsEqual(baseAim);
    }

    [TestCase]
    public void HiredCrew_CanHaveTraitsAssigned()
    {
        var campaign = CampaignState.CreateNew();
        campaign.Money = 500;

        var hired = campaign.HireCrew("NewGuy", CrewRole.Tech, 100);
        campaign.AssignTrait(hired.Id, "corporate");

        AssertThat(hired.HasTrait("corporate")).IsTrue();
        // Corporate gives +1 Tech, +1 Savvy
        // Tech role starts with Tech=3, Savvy=1
        AssertThat(hired.GetEffectiveStat(CrewStatType.Tech)).IsEqual(4);
        AssertThat(hired.GetEffectiveStat(CrewStatType.Savvy)).IsEqual(2);
    }
}
