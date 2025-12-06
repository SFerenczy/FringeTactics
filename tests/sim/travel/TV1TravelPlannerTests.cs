using GdUnit4;
using static GdUnit4.Assertions;

namespace FringeTactics.Tests;

[TestSuite]
public class TV1TravelPlannerTests
{
    private WorldState world;
    private TravelPlanner planner;

    [Before]
    public void Setup()
    {
        world = WorldState.CreateTestSector();
        planner = new TravelPlanner(world);
    }

    [TestCase]
    public void PlanRoute_DirectConnection_ReturnsSingleSegment()
    {
        // Haven (0) → Waypoint (1) is direct
        var plan = planner.PlanRoute(0, 1);

        AssertBool(plan.IsValid).IsTrue();
        AssertInt(plan.Segments.Count).IsEqual(1);
        AssertInt(plan.Segments[0].FromSystemId).IsEqual(0);
        AssertInt(plan.Segments[0].ToSystemId).IsEqual(1);
    }

    [TestCase]
    public void PlanRoute_MultiHop_ReturnsCorrectPath()
    {
        // Haven (0) → Rockfall (2) requires going through Waypoint (1)
        var plan = planner.PlanRoute(0, 2);

        AssertBool(plan.IsValid).IsTrue();
        AssertInt(plan.Segments.Count).IsEqual(2);

        var path = plan.GetPath();
        AssertInt(path[0]).IsEqual(0);
        AssertInt(path[1]).IsEqual(1);
        AssertInt(path[2]).IsEqual(2);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void PlanRoute_NoPath_ReturnsInvalid()
    {
        // Create isolated system
        var isolated = new StarSystem(99, "Isolated", SystemType.Outpost, new Godot.Vector2(1000, 1000));
        world.AddSystem(isolated);

        var plan = planner.PlanRoute(0, 99);

        AssertBool(plan.IsValid).IsFalse();
        AssertInt((int)plan.InvalidReason).IsEqual((int)TravelPlanInvalidReason.NoRoute);
    }

    [TestCase]
    public void PlanRoute_SameSystem_ReturnsInvalid()
    {
        var plan = planner.PlanRoute(0, 0);

        AssertBool(plan.IsValid).IsFalse();
        AssertInt((int)plan.InvalidReason).IsEqual((int)TravelPlanInvalidReason.SameSystem);
    }

    [TestCase]
    public void PlanRoute_InvalidOrigin_ReturnsInvalid()
    {
        var plan = planner.PlanRoute(999, 1);

        AssertBool(plan.IsValid).IsFalse();
        AssertInt((int)plan.InvalidReason).IsEqual((int)TravelPlanInvalidReason.InvalidSystem);
    }

    [TestCase]
    public void PlanRoute_InvalidDestination_ReturnsInvalid()
    {
        var plan = planner.PlanRoute(0, 999);

        AssertBool(plan.IsValid).IsFalse();
        AssertInt((int)plan.InvalidReason).IsEqual((int)TravelPlanInvalidReason.InvalidSystem);
    }

    [TestCase]
    public void PlanRoute_CalculatesFuelCost()
    {
        var plan = planner.PlanRoute(0, 1);

        AssertBool(plan.IsValid).IsTrue();
        AssertInt(plan.TotalFuelCost).IsGreater(0);
    }

    [TestCase]
    public void PlanRoute_CalculatesTimeCost()
    {
        var plan = planner.PlanRoute(0, 1);

        AssertBool(plan.IsValid).IsTrue();
        AssertInt(plan.TotalTimeDays).IsGreater(0);
    }

    [TestCase]
    public void PlanRoute_CalculatesDistance()
    {
        var plan = planner.PlanRoute(0, 1);

        AssertBool(plan.IsValid).IsTrue();
        AssertFloat(plan.TotalDistance).IsGreater(0f);
    }

    [TestCase]
    public void PlanRoute_CalculatesHazard()
    {
        // Haven → Waypoint has hazard 1
        var plan = planner.PlanRoute(0, 1);

        AssertBool(plan.IsValid).IsTrue();
        AssertInt(plan.TotalHazard).IsEqual(1);
    }

    [TestCase]
    public void PlanRoute_MultiHop_AggregatesFuelCost()
    {
        var plan = planner.PlanRoute(0, 2);

        AssertBool(plan.IsValid).IsTrue();

        int expectedFuel = 0;
        foreach (var segment in plan.Segments)
        {
            expectedFuel += segment.FuelCost;
        }

        AssertInt(plan.TotalFuelCost).IsEqual(expectedFuel);
    }

    [TestCase]
    public void PlanRoute_MultiHop_AggregatesTimeDays()
    {
        var plan = planner.PlanRoute(0, 2);

        AssertBool(plan.IsValid).IsTrue();

        int expectedDays = 0;
        foreach (var segment in plan.Segments)
        {
            expectedDays += segment.TimeDays;
        }

        AssertInt(plan.TotalTimeDays).IsEqual(expectedDays);
    }

    [TestCase]
    public void PlanRoute_MultiHop_AggregatesHazard()
    {
        var plan = planner.PlanRoute(0, 2);

        AssertBool(plan.IsValid).IsTrue();

        int expectedHazard = 0;
        foreach (var segment in plan.Segments)
        {
            expectedHazard += segment.HazardLevel;
        }

        AssertInt(plan.TotalHazard).IsEqual(expectedHazard);
    }

    [TestCase]
    public void PlanRoute_CalculatesAverageEncounterChance()
    {
        var plan = planner.PlanRoute(0, 2);

        AssertBool(plan.IsValid).IsTrue();
        AssertFloat(plan.AverageEncounterChance).IsGreaterEqual(0f);
        AssertFloat(plan.AverageEncounterChance).IsLessEqual(1f);
    }

    [TestCase]
    public void PlanRoute_SystemCount_IncludesOriginAndDestination()
    {
        var plan = planner.PlanRoute(0, 2);

        AssertBool(plan.IsValid).IsTrue();
        // Path: 0 → 1 → 2 = 3 systems
        AssertInt(plan.SystemCount).IsEqual(3);
    }

    [TestCase]
    public void PlanRoute_GetPath_ReturnsCorrectSystemIds()
    {
        var plan = planner.PlanRoute(0, 2);

        var path = plan.GetPath();

        AssertInt(path.Count).IsEqual(3);
        AssertInt(path[0]).IsEqual(0);
        AssertInt(path[2]).IsEqual(2);
    }

    [TestCase]
    public void ValidatePlan_SufficientFuel_ReturnsTrue()
    {
        var plan = planner.PlanRoute(0, 1);

        bool valid = planner.ValidatePlan(plan, 100);

        AssertBool(valid).IsTrue();
    }

    [TestCase]
    public void ValidatePlan_InsufficientFuel_ReturnsFalse()
    {
        var plan = planner.PlanRoute(0, 2);

        bool valid = planner.ValidatePlan(plan, 1);

        AssertBool(valid).IsFalse();
    }

    [TestCase]
    public void ValidatePlan_ExactFuel_ReturnsTrue()
    {
        var plan = planner.PlanRoute(0, 1);

        bool valid = planner.ValidatePlan(plan, plan.TotalFuelCost);

        AssertBool(valid).IsTrue();
    }

    [TestCase]
    public void ValidatePlan_InvalidPlan_ReturnsFalse()
    {
        var plan = TravelPlan.Invalid(0, 99, TravelPlanInvalidReason.NoRoute);

        bool valid = planner.ValidatePlan(plan, 100);

        AssertBool(valid).IsFalse();
    }

    [TestCase]
    public void ValidatePlan_NullPlan_ReturnsFalse()
    {
        bool valid = planner.ValidatePlan(null, 100);

        AssertBool(valid).IsFalse();
    }

    [TestCase]
    public void GetValidationFailure_ValidPlan_ReturnsNone()
    {
        var plan = planner.PlanRoute(0, 1);

        var failure = planner.GetValidationFailure(plan, 100);

        AssertInt((int)failure).IsEqual((int)TravelPlanInvalidReason.None);
    }

    [TestCase]
    public void GetValidationFailure_InsufficientFuel_ReturnsReason()
    {
        var plan = planner.PlanRoute(0, 2);

        var failure = planner.GetValidationFailure(plan, 1);

        AssertInt((int)failure).IsEqual((int)TravelPlanInvalidReason.InsufficientFuel);
    }

    [TestCase]
    public void GetValidationFailure_InvalidPlan_ReturnsInvalidReason()
    {
        var plan = TravelPlan.Invalid(0, 99, TravelPlanInvalidReason.NoRoute);

        var failure = planner.GetValidationFailure(plan, 100);

        AssertInt((int)failure).IsEqual((int)TravelPlanInvalidReason.NoRoute);
    }

    [TestCase]
    public void GetValidationFailure_NullPlan_ReturnsNullPlan()
    {
        var failure = planner.GetValidationFailure(null, 100);

        AssertInt((int)failure).IsEqual((int)TravelPlanInvalidReason.NullPlan);
    }

    [TestCase]
    public void PlanRoute_AppliesSystemMetricsToEncounterChance()
    {
        // Haven (0) has high security (4), low crime (1)
        // Waypoint (1) has lower security (2), higher crime (3)
        var plan = planner.PlanRoute(0, 1);

        AssertBool(plan.IsValid).IsTrue();
        // Encounter chance should be affected by metrics
        AssertFloat(plan.Segments[0].EncounterChance).IsGreaterEqual(0f);
    }

    [TestCase]
    public void PlanRoute_LongPath_FindsRoute()
    {
        // Haven (0) → Wreck (7) requires: 0 → 1 → 2 → 7
        var plan = planner.PlanRoute(0, 7);

        AssertBool(plan.IsValid).IsTrue();
        AssertInt(plan.Segments.Count).IsEqual(3);
    }

    [TestCase]
    public void PlanRoute_WithCustomShipStats_AffectsCosts()
    {
        var fastStats = new TravelShipStats { Speed = 200f, Efficiency = 2.0f, SafetyWeight = 1.0f };
        var slowStats = new TravelShipStats { Speed = 50f, Efficiency = 0.5f, SafetyWeight = 1.0f };

        var fastPlan = planner.PlanRoute(0, 1, fastStats);
        var slowPlan = planner.PlanRoute(0, 1, slowStats);

        // Fast ship: less time, less fuel
        AssertInt(fastPlan.TotalTimeDays).IsLess(slowPlan.TotalTimeDays);
        AssertInt(fastPlan.TotalFuelCost).IsLess(slowPlan.TotalFuelCost);
    }

    [TestCase]
    public void TravelPlan_CanAfford_SufficientFuel_ReturnsTrue()
    {
        var plan = planner.PlanRoute(0, 1);

        AssertBool(plan.CanAfford(100)).IsTrue();
    }

    [TestCase]
    public void TravelPlan_CanAfford_InsufficientFuel_ReturnsFalse()
    {
        var plan = planner.PlanRoute(0, 2);

        AssertBool(plan.CanAfford(1)).IsFalse();
    }

    [TestCase]
    public void TravelPlan_CanAfford_InvalidPlan_ReturnsFalse()
    {
        var plan = TravelPlan.Invalid(0, 99, TravelPlanInvalidReason.NoRoute);

        AssertBool(plan.CanAfford(100)).IsFalse();
    }

    [TestCase]
    public void TravelPlan_FromSegments_CalculatesAggregates()
    {
        var route = new Route(0, 1, 150f) { HazardLevel = 2 };
        var segment = TravelSegment.FromRoute(route);
        var segments = new System.Collections.Generic.List<TravelSegment> { segment };

        var plan = TravelPlan.FromSegments(0, 1, segments);

        AssertBool(plan.IsValid).IsTrue();
        AssertInt(plan.TotalFuelCost).IsEqual(segment.FuelCost);
        AssertInt(plan.TotalTimeDays).IsEqual(segment.TimeDays);
        AssertInt(plan.TotalHazard).IsEqual(segment.HazardLevel);
    }

    [TestCase]
    public void TravelPlan_Invalid_SetsProperties()
    {
        var plan = TravelPlan.Invalid(0, 5, TravelPlanInvalidReason.NoRoute);

        AssertBool(plan.IsValid).IsFalse();
        AssertInt(plan.OriginSystemId).IsEqual(0);
        AssertInt(plan.DestinationSystemId).IsEqual(5);
        AssertInt((int)plan.InvalidReason).IsEqual((int)TravelPlanInvalidReason.NoRoute);
    }
}
