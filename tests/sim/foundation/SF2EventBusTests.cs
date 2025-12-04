using System;
using System.Collections.Generic;
using GdUnit4;
using static GdUnit4.Assertions;

namespace FringeTactics.Tests;

[TestSuite]
public class SF2EventBusTests
{
    [TestCase]
    public void Subscribe_ReceivesPublishedEvents()
    {
        var bus = new EventBus();
        var received = new List<DayAdvancedEvent>();

        bus.Subscribe<DayAdvancedEvent>(e => received.Add(e));
        bus.Publish(new DayAdvancedEvent(1, 2, 1));

        AssertInt(received.Count).IsEqual(1);
        AssertInt(received[0].OldDay).IsEqual(1);
        AssertInt(received[0].NewDay).IsEqual(2);
    }

    [TestCase]
    public void Subscribe_MultipleHandlers_AllReceive()
    {
        var bus = new EventBus();
        int count1 = 0, count2 = 0;

        bus.Subscribe<DayAdvancedEvent>(_ => count1++);
        bus.Subscribe<DayAdvancedEvent>(_ => count2++);
        bus.Publish(new DayAdvancedEvent(1, 2, 1));

        AssertInt(count1).IsEqual(1);
        AssertInt(count2).IsEqual(1);
    }

    [TestCase]
    public void Subscribe_DifferentEventTypes_Isolated()
    {
        var bus = new EventBus();
        int dayCount = 0, missionCount = 0;

        bus.Subscribe<DayAdvancedEvent>(_ => dayCount++);
        bus.Subscribe<MissionCompletedEvent>(_ => missionCount++);

        bus.Publish(new DayAdvancedEvent(1, 2, 1));

        AssertInt(dayCount).IsEqual(1);
        AssertInt(missionCount).IsEqual(0);
    }

    [TestCase]
    public void Unsubscribe_StopsReceivingEvents()
    {
        var bus = new EventBus();
        int count = 0;
        Action<DayAdvancedEvent> handler = _ => count++;

        bus.Subscribe(handler);
        bus.Publish(new DayAdvancedEvent(1, 2, 1));
        AssertInt(count).IsEqual(1);

        bus.Unsubscribe(handler);
        bus.Publish(new DayAdvancedEvent(2, 3, 1));
        AssertInt(count).IsEqual(1);
    }

    [TestCase]
    public void Publish_NoSubscribers_DoesNotThrow()
    {
        var bus = new EventBus();
        bus.Publish(new DayAdvancedEvent(1, 2, 1));
    }

    [TestCase]
    public void Subscribe_DuplicateHandler_OnlyCalledOnce()
    {
        var bus = new EventBus();
        int count = 0;
        Action<DayAdvancedEvent> handler = _ => count++;

        bus.Subscribe(handler);
        bus.Subscribe(handler);
        bus.Publish(new DayAdvancedEvent(1, 2, 1));

        AssertInt(count).IsEqual(1);
    }

    [TestCase]
    public void Clear_RemovesAllSubscribers()
    {
        var bus = new EventBus();
        int count = 0;

        bus.Subscribe<DayAdvancedEvent>(_ => count++);
        bus.Clear();
        bus.Publish(new DayAdvancedEvent(1, 2, 1));

        AssertInt(count).IsEqual(0);
    }

    [TestCase]
    public void GetSubscriberCount_ReturnsCorrectCount()
    {
        var bus = new EventBus();

        AssertInt(bus.GetSubscriberCount<DayAdvancedEvent>()).IsEqual(0);

        bus.Subscribe<DayAdvancedEvent>(_ => { });
        AssertInt(bus.GetSubscriberCount<DayAdvancedEvent>()).IsEqual(1);

        bus.Subscribe<DayAdvancedEvent>(_ => { });
        AssertInt(bus.GetSubscriberCount<DayAdvancedEvent>()).IsEqual(2);
    }

    [TestCase]
    public void Publish_HandlerThrows_OtherHandlersStillCalled()
    {
        var bus = new EventBus();
        int count = 0;

        bus.Subscribe<DayAdvancedEvent>(_ => throw new Exception("Test"));
        bus.Subscribe<DayAdvancedEvent>(_ => count++);

        bus.Publish(new DayAdvancedEvent(1, 2, 1));

        AssertInt(count).IsEqual(1);
    }

    [TestCase]
    public void Publish_UnsubscribeDuringHandler_Safe()
    {
        var bus = new EventBus();
        int count = 0;
        Action<DayAdvancedEvent> handler = null;
        handler = _ =>
        {
            count++;
            bus.Unsubscribe(handler);
        };

        bus.Subscribe(handler);
        bus.Publish(new DayAdvancedEvent(1, 2, 1));
        bus.Publish(new DayAdvancedEvent(2, 3, 1));

        AssertInt(count).IsEqual(1);
    }

    [TestCase]
    public void Unsubscribe_NonExistentHandler_DoesNotThrow()
    {
        var bus = new EventBus();
        Action<DayAdvancedEvent> handler = _ => { };

        bus.Unsubscribe(handler);
    }

    [TestCase]
    public void Unsubscribe_NonExistentEventType_DoesNotThrow()
    {
        var bus = new EventBus();
        Action<MissionCompletedEvent> handler = _ => { };

        bus.Unsubscribe(handler);
    }

    [TestCase]
    public void GetSubscriberCount_NonExistentEventType_ReturnsZero()
    {
        var bus = new EventBus();
        AssertInt(bus.GetSubscriberCount<MissionCompletedEvent>()).IsEqual(0);
    }
}

[TestSuite]
public class SF2EventTypesTests
{
    [TestCase]
    public void DayAdvancedEvent_ContainsCorrectData()
    {
        var evt = new DayAdvancedEvent(5, 8, 3);

        AssertInt(evt.OldDay).IsEqual(5);
        AssertInt(evt.NewDay).IsEqual(8);
        AssertInt(evt.DaysAdvanced).IsEqual(3);
    }

    [TestCase]
    public void MissionCompletedEvent_ContainsCorrectData()
    {
        var evt = new MissionCompletedEvent(
            Outcome: MissionOutcome.Victory,
            EnemiesKilled: 5,
            CrewDeaths: 1,
            CrewInjured: 2,
            DurationSeconds: 120.5f
        );

        AssertObject(evt.Outcome).IsEqual(MissionOutcome.Victory);
        AssertInt(evt.EnemiesKilled).IsEqual(5);
        AssertInt(evt.CrewDeaths).IsEqual(1);
        AssertInt(evt.CrewInjured).IsEqual(2);
        AssertFloat(evt.DurationSeconds).IsEqual(120.5f);
    }

    [TestCase]
    public void ResourceChangedEvent_ContainsCorrectData()
    {
        var evt = new ResourceChangedEvent(
            ResourceType: "fuel",
            OldValue: 100,
            NewValue: 85,
            Delta: -15,
            Reason: "travel"
        );

        AssertString(evt.ResourceType).IsEqual("fuel");
        AssertInt(evt.OldValue).IsEqual(100);
        AssertInt(evt.NewValue).IsEqual(85);
        AssertInt(evt.Delta).IsEqual(-15);
        AssertString(evt.Reason).IsEqual("travel");
    }

    [TestCase]
    [RequireGodotRuntime]
    public void ActorDiedEvent_ContainsCorrectData()
    {
        var evt = new ActorDiedEvent(
            ActorId: 42,
            ActorType: ActorType.Enemy,
            ActorName: "Grunt",
            KillerId: 1,
            Position: new Godot.Vector2I(5, 10)
        );

        AssertInt(evt.ActorId).IsEqual(42);
        AssertObject(evt.ActorType).IsEqual(ActorType.Enemy);
        AssertString(evt.ActorName).IsEqual("Grunt");
        AssertInt(evt.KillerId).IsEqual(1);
        AssertObject(evt.Position).IsEqual(new Godot.Vector2I(5, 10));
    }

    [TestCase]
    public void JobAcceptedEvent_ContainsCorrectData()
    {
        var evt = new JobAcceptedEvent(
            JobId: "job_7",
            JobTitle: "Clear the Outpost",
            TargetNodeId: 3,
            DeadlineDay: 15
        );

        AssertString(evt.JobId).IsEqual("job_7");
        AssertString(evt.JobTitle).IsEqual("Clear the Outpost");
        AssertInt(evt.TargetNodeId).IsEqual(3);
        AssertInt(evt.DeadlineDay).IsEqual(15);
    }

    [TestCase]
    public void TravelCompletedEvent_ContainsCorrectData()
    {
        var evt = new TravelCompletedEvent(
            FromNodeId: 0,
            ToNodeId: 2,
            ToNodeName: "Mining Station",
            FuelCost: 15,
            DaysCost: 2
        );

        AssertInt(evt.FromNodeId).IsEqual(0);
        AssertInt(evt.ToNodeId).IsEqual(2);
        AssertString(evt.ToNodeName).IsEqual("Mining Station");
        AssertInt(evt.FuelCost).IsEqual(15);
        AssertInt(evt.DaysCost).IsEqual(2);
    }

    [TestCase]
    public void FactionRepChangedEvent_ContainsCorrectData()
    {
        var evt = new FactionRepChangedEvent(
            FactionId: "corp_a",
            FactionName: "Stellar Corp",
            OldRep: 50,
            NewRep: 60,
            Delta: 10
        );

        AssertString(evt.FactionId).IsEqual("corp_a");
        AssertString(evt.FactionName).IsEqual("Stellar Corp");
        AssertInt(evt.OldRep).IsEqual(50);
        AssertInt(evt.NewRep).IsEqual(60);
        AssertInt(evt.Delta).IsEqual(10);
    }

    [TestCase]
    public void CrewLeveledUpEvent_ContainsCorrectData()
    {
        var evt = new CrewLeveledUpEvent(
            CrewId: 1,
            CrewName: "Alex",
            OldLevel: 2,
            NewLevel: 3
        );

        AssertInt(evt.CrewId).IsEqual(1);
        AssertString(evt.CrewName).IsEqual("Alex");
        AssertInt(evt.OldLevel).IsEqual(2);
        AssertInt(evt.NewLevel).IsEqual(3);
    }

    [TestCase]
    public void CrewInjuredEvent_ContainsCorrectData()
    {
        var evt = new CrewInjuredEvent(
            CrewId: 2,
            CrewName: "Jordan",
            InjuryType: "Wounded"
        );

        AssertInt(evt.CrewId).IsEqual(2);
        AssertString(evt.CrewName).IsEqual("Jordan");
        AssertString(evt.InjuryType).IsEqual("Wounded");
    }

    [TestCase]
    public void CrewDiedEvent_ContainsCorrectData()
    {
        var evt = new CrewDiedEvent(
            CrewId: 3,
            CrewName: "Morgan",
            Cause: "combat"
        );

        AssertInt(evt.CrewId).IsEqual(3);
        AssertString(evt.CrewName).IsEqual("Morgan");
        AssertString(evt.Cause).IsEqual("combat");
    }

    [TestCase]
    public void AlarmStateChangedEvent_ContainsCorrectData()
    {
        var evt = new AlarmStateChangedEvent(
            OldState: AlarmState.Quiet,
            NewState: AlarmState.Alerted
        );

        AssertObject(evt.OldState).IsEqual(AlarmState.Quiet);
        AssertObject(evt.NewState).IsEqual(AlarmState.Alerted);
    }

    [TestCase]
    public void MissionPhaseChangedEvent_ContainsCorrectData()
    {
        var evt = new MissionPhaseChangedEvent(
            OldPhase: MissionPhase.Setup,
            NewPhase: MissionPhase.Active
        );

        AssertObject(evt.OldPhase).IsEqual(MissionPhase.Setup);
        AssertObject(evt.NewPhase).IsEqual(MissionPhase.Active);
    }

    [TestCase]
    public void JobCompletedEvent_ContainsCorrectData()
    {
        var evt = new JobCompletedEvent(
            JobId: "job_5",
            JobTitle: "Escort Mission",
            Success: true,
            MoneyReward: 500
        );

        AssertString(evt.JobId).IsEqual("job_5");
        AssertString(evt.JobTitle).IsEqual("Escort Mission");
        AssertBool(evt.Success).IsTrue();
        AssertInt(evt.MoneyReward).IsEqual(500);
    }

    [TestCase]
    public void Events_AreValueTypes()
    {
        AssertBool(typeof(DayAdvancedEvent).IsValueType).IsTrue();
        AssertBool(typeof(MissionCompletedEvent).IsValueType).IsTrue();
        AssertBool(typeof(ResourceChangedEvent).IsValueType).IsTrue();
        AssertBool(typeof(ActorDiedEvent).IsValueType).IsTrue();
        AssertBool(typeof(JobAcceptedEvent).IsValueType).IsTrue();
        AssertBool(typeof(TravelCompletedEvent).IsValueType).IsTrue();
        AssertBool(typeof(FactionRepChangedEvent).IsValueType).IsTrue();
        AssertBool(typeof(CrewLeveledUpEvent).IsValueType).IsTrue();
        AssertBool(typeof(CrewInjuredEvent).IsValueType).IsTrue();
        AssertBool(typeof(CrewDiedEvent).IsValueType).IsTrue();
        AssertBool(typeof(AlarmStateChangedEvent).IsValueType).IsTrue();
        AssertBool(typeof(MissionPhaseChangedEvent).IsValueType).IsTrue();
        AssertBool(typeof(JobCompletedEvent).IsValueType).IsTrue();
    }

    [TestCase]
    public void Events_HaveValueEquality()
    {
        var evt1 = new DayAdvancedEvent(1, 2, 1);
        var evt2 = new DayAdvancedEvent(1, 2, 1);
        var evt3 = new DayAdvancedEvent(1, 3, 2);

        AssertObject(evt1).IsEqual(evt2);
        AssertObject(evt1).IsNotEqual(evt3);
    }
}
