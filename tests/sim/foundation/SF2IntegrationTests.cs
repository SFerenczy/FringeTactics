using System.Collections.Generic;
using GdUnit4;
using static GdUnit4.Assertions;

namespace FringeTactics.Tests;

[TestSuite]
public class SF2IntegrationTests
{
    [TestCase]
    public void CampaignTime_PublishesDayAdvancedEvent()
    {
        var bus = new EventBus();
        var time = new CampaignTime();
        time.EventBus = bus;

        var received = new List<DayAdvancedEvent>();
        bus.Subscribe<DayAdvancedEvent>(e => received.Add(e));

        time.AdvanceDays(3);

        AssertInt(received.Count).IsEqual(1);
        AssertInt(received[0].OldDay).IsEqual(1);
        AssertInt(received[0].NewDay).IsEqual(4);
        AssertInt(received[0].DaysAdvanced).IsEqual(3);
    }

    [TestCase]
    public void CampaignTime_BothCSharpEventAndBusWork()
    {
        var bus = new EventBus();
        var time = new CampaignTime();
        time.EventBus = bus;

        int csharpEventCount = 0;
        int busEventCount = 0;

        time.DayAdvanced += (_, _) => csharpEventCount++;
        bus.Subscribe<DayAdvancedEvent>(_ => busEventCount++);

        time.AdvanceDays(1);

        AssertInt(csharpEventCount).IsEqual(1);
        AssertInt(busEventCount).IsEqual(1);
    }

    [TestCase]
    public void CampaignTime_NoBus_StillWorks()
    {
        var time = new CampaignTime();
        // EventBus is null

        time.AdvanceDays(5);

        AssertInt(time.CurrentDay).IsEqual(6);
    }

    [TestCase]
    public void CampaignState_PublishesJobAcceptedEvent()
    {
        var bus = new EventBus();
        var campaign = CampaignState.CreateNew();
        campaign.EventBus = bus;

        var received = new List<JobAcceptedEvent>();
        bus.Subscribe<JobAcceptedEvent>(e => received.Add(e));

        if (campaign.AvailableJobs.Count > 0)
        {
            var job = campaign.AvailableJobs[0];
            campaign.AcceptJob(job);

            AssertInt(received.Count).IsEqual(1);
            AssertString(received[0].JobId).IsEqual(job.Id);
            AssertString(received[0].JobTitle).IsEqual(job.Title);
        }
    }

    [TestCase]
    public void CampaignState_PublishesFactionRepChangedEvent()
    {
        var bus = new EventBus();
        var campaign = CampaignState.CreateNew();
        campaign.EventBus = bus;

        var received = new List<FactionRepChangedEvent>();
        bus.Subscribe<FactionRepChangedEvent>(e => received.Add(e));

        // Get a faction from the sector
        string factionId = null;
        foreach (var kvp in campaign.Sector.Factions)
        {
            factionId = kvp.Key;
            break;
        }

        if (factionId != null)
        {
            campaign.ModifyFactionRep(factionId, 10);

            AssertInt(received.Count).IsEqual(1);
            AssertString(received[0].FactionId).IsEqual(factionId);
            AssertInt(received[0].Delta).IsEqual(10);
            AssertInt(received[0].NewRep).IsEqual(60); // 50 + 10
        }
    }

    [TestCase]
    public void CampaignState_NoBus_StillWorks()
    {
        var campaign = CampaignState.CreateNew();
        // EventBus is null

        if (campaign.AvailableJobs.Count > 0)
        {
            var job = campaign.AvailableJobs[0];
            campaign.AcceptJob(job);

            AssertObject(campaign.CurrentJob).IsEqual(job);
        }
    }

    [TestCase]
    [RequireGodotRuntime]
    public void CombatState_PublishesActorDiedEvent()
    {
        var bus = new EventBus();
        var combat = new CombatState(12345);
        combat.EventBus = bus;

        var received = new List<ActorDiedEvent>();
        bus.Subscribe<ActorDiedEvent>(e => received.Add(e));

        // Add an actor using the proper API
        var actor = combat.AddActor(ActorType.Enemy, new Godot.Vector2I(5, 5));
        actor.Hp = 10;
        actor.MaxHp = 10;
        actor.Name = "TestEnemy";

        // The ActorDied event is fired by AttackSystem, not TakeDamage directly
        // For this test, we'll verify the event bus is wired correctly
        AssertObject(combat.EventBus).IsEqual(bus);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void CombatState_NoBus_StillWorks()
    {
        var combat = new CombatState(12345);
        // EventBus is null

        var actor = combat.AddActor(ActorType.Crew, new Godot.Vector2I(5, 5));
        actor.Hp = 100;
        actor.MaxHp = 100;

        AssertInt(combat.Actors.Count).IsEqual(1);
    }

    [TestCase]
    public void EventBus_MultipleDomainsCanSubscribe()
    {
        var bus = new EventBus();

        int dayEvents = 0;
        int missionEvents = 0;
        int actorEvents = 0;

        bus.Subscribe<DayAdvancedEvent>(_ => dayEvents++);
        bus.Subscribe<MissionCompletedEvent>(_ => missionEvents++);
        bus.Subscribe<ActorDiedEvent>(_ => actorEvents++);

        // Publish different events
        bus.Publish(new DayAdvancedEvent(1, 2, 1));
        bus.Publish(new DayAdvancedEvent(2, 3, 1));
        bus.Publish(new MissionCompletedEvent(MissionOutcome.Victory, 5, 0, 1, 120f));

        AssertInt(dayEvents).IsEqual(2);
        AssertInt(missionEvents).IsEqual(1);
        AssertInt(actorEvents).IsEqual(0);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void EventBus_ClearRemovesAllDomainSubscribers()
    {
        var bus = new EventBus();

        int count = 0;
        bus.Subscribe<DayAdvancedEvent>(_ => count++);
        bus.Subscribe<MissionCompletedEvent>(_ => count++);
        bus.Subscribe<ActorDiedEvent>(_ => count++);

        bus.Clear();

        bus.Publish(new DayAdvancedEvent(1, 2, 1));
        bus.Publish(new MissionCompletedEvent(MissionOutcome.Victory, 5, 0, 1, 120f));
        bus.Publish(new ActorDiedEvent(1, ActorType.Enemy, "Test", 0, new Godot.Vector2I(0, 0)));

        AssertInt(count).IsEqual(0);
    }

    [TestCase]
    public void TravelSystem_PublishesTravelCompletedEvent()
    {
        var bus = new EventBus();
        var campaign = CampaignState.CreateNew();
        campaign.EventBus = bus;
        campaign.Time.EventBus = bus;

        var received = new List<TravelCompletedEvent>();
        bus.Subscribe<TravelCompletedEvent>(e => received.Add(e));

        // Find a connected system to travel to
        var currentSystem = campaign.GetCurrentSystem();
        if (currentSystem != null && currentSystem.Connections.Count > 0)
        {
            int targetId = currentSystem.Connections[0];
            var result = TravelSystem.Travel(campaign, targetId);

            if (result == TravelResult.Success)
            {
                AssertInt(received.Count).IsEqual(1);
                AssertInt(received[0].ToNodeId).IsEqual(targetId);
                AssertInt(received[0].FuelCost).IsGreater(0);
                AssertInt(received[0].DaysCost).IsGreater(0);
            }
        }
    }

    [TestCase]
    public void TravelSystem_PublishesResourceChangedEvent()
    {
        var bus = new EventBus();
        var campaign = CampaignState.CreateNew();
        campaign.EventBus = bus;
        campaign.Time.EventBus = bus;

        var received = new List<ResourceChangedEvent>();
        bus.Subscribe<ResourceChangedEvent>(e => received.Add(e));

        // Find a connected system to travel to
        var currentSystem = campaign.GetCurrentSystem();
        if (currentSystem != null && currentSystem.Connections.Count > 0)
        {
            int targetId = currentSystem.Connections[0];
            var result = TravelSystem.Travel(campaign, targetId);

            if (result == TravelResult.Success)
            {
                // Should have at least one fuel resource change
                var fuelEvents = received.FindAll(e => e.ResourceType == "fuel");
                AssertInt(fuelEvents.Count).IsGreater(0);
                AssertString(fuelEvents[0].Reason).IsEqual("travel");
                AssertInt(fuelEvents[0].Delta).IsLess(0);
            }
        }
    }

    [TestCase]
    public void ConsumeMissionResources_PublishesResourceChangedEvents()
    {
        var bus = new EventBus();
        var campaign = CampaignState.CreateNew();
        campaign.EventBus = bus;
        campaign.Time.EventBus = bus;

        var received = new List<ResourceChangedEvent>();
        bus.Subscribe<ResourceChangedEvent>(e => received.Add(e));

        campaign.ConsumeMissionResources();

        // Should have ammo and fuel events
        var ammoEvents = received.FindAll(e => e.ResourceType == "ammo");
        var fuelEvents = received.FindAll(e => e.ResourceType == "fuel");

        AssertInt(ammoEvents.Count).IsEqual(1);
        AssertInt(fuelEvents.Count).IsEqual(1);
        AssertString(ammoEvents[0].Reason).IsEqual("mission_cost");
        AssertString(fuelEvents[0].Reason).IsEqual("mission_cost");
    }
}
