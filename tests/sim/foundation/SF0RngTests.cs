using GdUnit4;
using static GdUnit4.Assertions;
using System.Collections.Generic;

namespace FringeTactics.Tests;

/// <summary>
/// SF0 Milestone tests - validates RNG streams and service.
/// These tests don't require Godot runtime as they test pure C# logic.
/// </summary>
[TestSuite]
public class SF0RngTests
{
    [TestCase]
    public void SameSeed_ProducesSameSequence()
    {
        var rng1 = new RngStream("test", 12345);
        var rng2 = new RngStream("test", 12345);

        for (int i = 0; i < 100; i++)
        {
            AssertThat(rng1.NextFloat()).IsEqual(rng2.NextFloat());
        }
    }

    [TestCase]
    public void DifferentSeeds_ProduceDifferentSequences()
    {
        var rng1 = new RngStream("test", 12345);
        var rng2 = new RngStream("test", 54321);

        bool foundDifference = false;
        for (int i = 0; i < 10; i++)
        {
            if (rng1.NextFloat() != rng2.NextFloat())
            {
                foundDifference = true;
                break;
            }
        }
        AssertThat(foundDifference).IsTrue();
    }

    [TestCase]
    public void RestoreState_ReproducesSequence()
    {
        var rng = new RngStream("test", 12345);

        // Consume some values
        for (int i = 0; i < 50; i++)
        {
            rng.NextFloat();
        }

        // Save state
        var state = rng.GetState();

        // Get next 10 values
        var expected = new float[10];
        for (int i = 0; i < 10; i++)
        {
            expected[i] = rng.NextFloat();
        }

        // Restore and verify
        var rng2 = new RngStream("test", 0);
        rng2.RestoreState(state.Seed, state.CallCount);

        for (int i = 0; i < 10; i++)
        {
            AssertThat(rng2.NextFloat()).IsEqual(expected[i]);
        }
    }

    [TestCase]
    public void CallCount_TracksCorrectly()
    {
        var rng = new RngStream("test", 12345);
        AssertThat(rng.CallCount).IsEqual(0);

        rng.NextFloat();
        AssertThat(rng.CallCount).IsEqual(1);

        rng.NextInt(100);
        AssertThat(rng.CallCount).IsEqual(2);

        rng.Roll(0.5f);
        AssertThat(rng.CallCount).IsEqual(3);
    }

    [TestCase]
    public void NextInt_RespectsRange()
    {
        var rng = new RngStream("test", 12345);

        for (int i = 0; i < 100; i++)
        {
            int value = rng.NextInt(5, 10);
            AssertThat(value).IsGreaterEqual(5);
            AssertThat(value).IsLess(10);
        }
    }

    [TestCase]
    public void Pick_ReturnsElementFromList()
    {
        var rng = new RngStream("test", 12345);
        var list = new List<string> { "a", "b", "c", "d", "e" };

        for (int i = 0; i < 20; i++)
        {
            var picked = rng.Pick(list);
            AssertThat(list.Contains(picked)).IsTrue();
        }
    }

    [TestCase]
    public void Pick_EmptyList_ReturnsDefault()
    {
        var rng = new RngStream("test", 12345);
        var emptyList = new List<string>();

        var result = rng.Pick(emptyList);
        AssertThat(result).IsNull();
    }

    [TestCase]
    public void Shuffle_MaintainsAllElements()
    {
        var rng = new RngStream("test", 12345);
        var list = new List<int> { 1, 2, 3, 4, 5 };
        var originalSum = 15;

        rng.Shuffle(list);

        AssertThat(list.Count).IsEqual(5);
        int sum = 0;
        foreach (var item in list)
        {
            sum += item;
        }
        AssertThat(sum).IsEqual(originalSum);
    }

    [TestCase]
    public void GetState_CapturesCurrentState()
    {
        var rng = new RngStream("mystream", 99999);

        for (int i = 0; i < 25; i++)
        {
            rng.NextFloat();
        }

        var state = rng.GetState();

        AssertThat(state.Name).IsEqual("mystream");
        AssertThat(state.Seed).IsEqual(99999);
        AssertThat(state.CallCount).IsEqual(25);
    }
}

[TestSuite]
public class RngServiceTests
{
    [TestCase]
    public void SameMasterSeed_ProducesSameStreams()
    {
        var service1 = new RngService(12345);
        var service2 = new RngService(12345);

        // Campaign streams should match
        for (int i = 0; i < 10; i++)
        {
            AssertThat(service1.Campaign.NextFloat()).IsEqual(service2.Campaign.NextFloat());
        }

        // Tactical streams should match
        for (int i = 0; i < 10; i++)
        {
            AssertThat(service1.Tactical.NextFloat()).IsEqual(service2.Tactical.NextFloat());
        }
    }

    [TestCase]
    public void Streams_AreIsolated()
    {
        var service1 = new RngService(12345);
        var service2 = new RngService(12345);

        // Consume from campaign on service1
        service1.Campaign.NextFloat();

        // Consume many from tactical on service1
        for (int i = 0; i < 1000; i++)
        {
            service1.Tactical.NextFloat();
        }

        // Campaign's second value should still match service2's second value
        service2.Campaign.NextFloat(); // Skip first
        var expected = service2.Campaign.NextFloat();
        var actual = service1.Campaign.NextFloat();

        AssertThat(actual).IsEqual(expected);
    }

    [TestCase]
    public void CampaignAndTactical_HaveDifferentSequences()
    {
        var service = new RngService(12345);

        bool foundDifference = false;
        for (int i = 0; i < 10; i++)
        {
            if (service.Campaign.NextFloat() != service.Tactical.NextFloat())
            {
                foundDifference = true;
                break;
            }
        }
        AssertThat(foundDifference).IsTrue();
    }

    [TestCase]
    public void SaveRestore_RoundTrip()
    {
        var service = new RngService(12345);

        // Consume some values
        for (int i = 0; i < 25; i++)
        {
            service.Campaign.NextFloat();
            service.Tactical.NextInt(100);
        }

        // Save state
        var state = service.GetState();

        // Get next values
        var expectedCampaign = service.Campaign.NextFloat();
        var expectedTactical = service.Tactical.NextInt(100);

        // Restore to new service
        var service2 = new RngService(0);
        service2.RestoreState(state);

        AssertThat(service2.Campaign.NextFloat()).IsEqual(expectedCampaign);
        AssertThat(service2.Tactical.NextInt(100)).IsEqual(expectedTactical);
    }

    [TestCase]
    public void ResetTacticalStream_CreatesNewSequence()
    {
        var service = new RngService(12345);

        var firstValue = service.Tactical.NextFloat();

        service.ResetTacticalStream(99999);

        var afterReset = service.Tactical.NextFloat();

        // Values should differ (different seed)
        AssertThat(afterReset).IsNotEqual(firstValue);
    }

    [TestCase]
    public void ResetTacticalStream_DoesNotAffectCampaign()
    {
        var service1 = new RngService(12345);
        var service2 = new RngService(12345);

        // Consume from campaign
        service1.Campaign.NextFloat();
        service2.Campaign.NextFloat();

        // Reset tactical on service1 only
        service1.ResetTacticalStream(99999);

        // Campaign should still match
        var expected = service2.Campaign.NextFloat();
        var actual = service1.Campaign.NextFloat();

        AssertThat(actual).IsEqual(expected);
    }

    [TestCase]
    public void GetStream_UnknownStream_Throws()
    {
        var service = new RngService(12345);

        AssertThrown(() => service.GetStream("nonexistent"))
            .IsInstanceOf<System.ArgumentException>();
    }

    [TestCase]
    public void HasStream_ReturnsCorrectly()
    {
        var service = new RngService(12345);

        AssertThat(service.HasStream(RngService.CampaignStream)).IsTrue();
        AssertThat(service.HasStream(RngService.TacticalStream)).IsTrue();
        AssertThat(service.HasStream("nonexistent")).IsFalse();
    }

    [TestCase]
    public void MasterSeed_IsPreserved()
    {
        var service = new RngService(42);
        AssertThat(service.MasterSeed).IsEqual(42);
    }

    [TestCase]
    public void GetState_IncludesAllStreams()
    {
        var service = new RngService(12345);

        // Consume some values
        service.Campaign.NextFloat();
        service.Tactical.NextFloat();
        service.Tactical.NextFloat();

        var state = service.GetState();

        AssertThat(state.MasterSeed).IsEqual(12345);
        AssertThat(state.Streams.Count).IsEqual(2);

        // Find campaign stream state
        RngStreamState campaignState = null;
        RngStreamState tacticalState = null;
        foreach (var s in state.Streams)
        {
            if (s.Name == RngService.CampaignStream) campaignState = s;
            if (s.Name == RngService.TacticalStream) tacticalState = s;
        }

        AssertThat(campaignState).IsNotNull();
        AssertThat(campaignState.CallCount).IsEqual(1);

        AssertThat(tacticalState).IsNotNull();
        AssertThat(tacticalState.CallCount).IsEqual(2);
    }
}
