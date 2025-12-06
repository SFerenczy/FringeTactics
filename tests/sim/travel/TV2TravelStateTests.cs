using GdUnit4;
using static GdUnit4.Assertions;

namespace FringeTactics.Tests;

[TestSuite]
public class TV2TravelStateTests
{
    private WorldState CreateTestWorld()
    {
        return WorldState.CreateTestSector();
    }

    [TestCase]
    public void Create_InitializesCorrectly()
    {
        var world = CreateTestWorld();
        var planner = new TravelPlanner(world);
        var plan = planner.PlanRoute(0, 2); // Haven → Rockfall

        var state = TravelState.Create(plan, 0);

        AssertInt(state.CurrentSegmentIndex).IsEqual(0);
        AssertInt(state.CurrentDayInSegment).IsEqual(0);
        AssertInt(state.CurrentSystemId).IsEqual(0);
        AssertInt(state.FuelConsumed).IsEqual(0);
        AssertInt(state.DaysElapsed).IsEqual(0);
        AssertBool(state.IsComplete).IsFalse();
        AssertBool(state.IsPausedForEncounter).IsFalse();
        AssertObject(state.PendingEncounterId).IsNull();
    }

    [TestCase]
    public void IsComplete_FalseWhenSegmentsRemain()
    {
        var world = CreateTestWorld();
        var planner = new TravelPlanner(world);
        var plan = planner.PlanRoute(0, 2);

        var state = TravelState.Create(plan, 0);

        AssertBool(state.IsComplete).IsFalse();
    }

    [TestCase]
    public void IsComplete_TrueWhenAllSegmentsProcessed()
    {
        var world = CreateTestWorld();
        var planner = new TravelPlanner(world);
        var plan = planner.PlanRoute(0, 2);

        var state = TravelState.Create(plan, 0);
        state.CurrentSegmentIndex = plan.Segments.Count;

        AssertBool(state.IsComplete).IsTrue();
    }

    [TestCase]
    public void IsComplete_TrueWhenPlanIsNull()
    {
        var state = new TravelState { Plan = null };

        AssertBool(state.IsComplete).IsTrue();
    }

    [TestCase]
    public void CurrentSegment_ReturnsCorrectSegment()
    {
        var world = CreateTestWorld();
        var planner = new TravelPlanner(world);
        var plan = planner.PlanRoute(0, 2);

        var state = TravelState.Create(plan, 0);

        AssertObject(state.CurrentSegment).IsNotNull();
        AssertInt(state.CurrentSegment.FromSystemId).IsEqual(0);
    }

    [TestCase]
    public void CurrentSegment_NullWhenComplete()
    {
        var world = CreateTestWorld();
        var planner = new TravelPlanner(world);
        var plan = planner.PlanRoute(0, 2);

        var state = TravelState.Create(plan, 0);
        state.CurrentSegmentIndex = plan.Segments.Count;

        AssertObject(state.CurrentSegment).IsNull();
    }

    [TestCase]
    public void EncounterHistory_StartsEmpty()
    {
        var world = CreateTestWorld();
        var planner = new TravelPlanner(world);
        var plan = planner.PlanRoute(0, 1);

        var state = TravelState.Create(plan, 0);

        AssertInt(state.EncounterHistory.Count).IsEqual(0);
    }
}
