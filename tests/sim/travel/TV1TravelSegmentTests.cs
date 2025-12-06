using GdUnit4;
using static GdUnit4.Assertions;

namespace FringeTactics.Tests;

[TestSuite]
public class TV1TravelSegmentTests
{
    [TestCase]
    public void FromRoute_CreatesValidSegment()
    {
        var route = new Route(0, 1, 150f) { HazardLevel = 2 };

        var segment = TravelSegment.FromRoute(route);

        AssertInt(segment.FromSystemId).IsEqual(0);
        AssertInt(segment.ToSystemId).IsEqual(1);
        AssertFloat(segment.Distance).IsEqual(150f);
        AssertInt(segment.HazardLevel).IsEqual(2);
    }

    [TestCase]
    public void FromRoute_CalculatesFuelCost()
    {
        var route = new Route(0, 1, 150f);

        var segment = TravelSegment.FromRoute(route);

        // 150 * 0.1 / 1.0 = 15
        AssertInt(segment.FuelCost).IsEqual(15);
    }

    [TestCase]
    public void FromRoute_CalculatesTimeDays()
    {
        var route = new Route(0, 1, 150f);

        var segment = TravelSegment.FromRoute(route);

        // 150 / 100 = 1.5 → ceil = 2
        AssertInt(segment.TimeDays).IsEqual(2);
    }

    [TestCase]
    public void FromRoute_CalculatesEncounterChance()
    {
        var route = new Route(0, 1, 150f) { HazardLevel = 3 };

        var segment = TravelSegment.FromRoute(route);

        // 3 * 0.1 = 30%
        AssertFloat(segment.EncounterChance).IsEqual(0.3f);
    }

    [TestCase]
    public void FromRoute_SuggestsEncounterType()
    {
        var route = new Route(0, 1, 150f) { HazardLevel = 1 };

        var segment = TravelSegment.FromRoute(route);

        AssertString(segment.SuggestedEncounterType).IsEqual("trader");
    }

    [TestCase]
    public void FromRoute_WithCustomSpeed_AffectsTime()
    {
        var route = new Route(0, 1, 150f);

        var segment = TravelSegment.FromRoute(route, shipSpeed: 50f);

        // 150 / 50 = 3
        AssertInt(segment.TimeDays).IsEqual(3);
    }

    [TestCase]
    public void FromRoute_WithCustomEfficiency_AffectsFuel()
    {
        var route = new Route(0, 1, 150f);

        var segment = TravelSegment.FromRoute(route, shipEfficiency: 1.5f);

        // 150 * 0.1 / 1.5 = 10
        AssertInt(segment.FuelCost).IsEqual(10);
    }

    [TestCase]
    public void FromRoute_WithDirection_SetsCorrectFromTo()
    {
        var route = new Route(0, 1, 150f);

        // Travel from 1 to 0 (reverse direction)
        var segment = TravelSegment.FromRoute(route, 1, 0);

        AssertInt(segment.FromSystemId).IsEqual(1);
        AssertInt(segment.ToSystemId).IsEqual(0);
    }

    [TestCase]
    public void FromRoute_NullRoute_ReturnsNull()
    {
        var segment = TravelSegment.FromRoute(null);
        AssertObject(segment).IsNull();
    }

    [TestCase]
    public void FromRoute_WithTags_ExposesRouteTags()
    {
        var route = new Route(0, 1, 150f);
        route.Tags.Add(WorldTags.Patrolled);
        route.Tags.Add(WorldTags.Asteroid);

        var segment = TravelSegment.FromRoute(route);

        AssertBool(segment.RouteTags.Contains(WorldTags.Patrolled)).IsTrue();
        AssertBool(segment.RouteTags.Contains(WorldTags.Asteroid)).IsTrue();
    }

    [TestCase]
    public void HazardLevel_ReturnsRouteHazard()
    {
        var route = new Route(0, 1, 150f) { HazardLevel = 4 };

        var segment = TravelSegment.FromRoute(route);

        AssertInt(segment.HazardLevel).IsEqual(4);
    }

    [TestCase]
    public void HazardLevel_NullRoute_ReturnsZero()
    {
        var segment = new TravelSegment { Route = null };
        AssertInt(segment.HazardLevel).IsEqual(0);
    }

    [TestCase]
    public void RouteTags_NullRoute_ReturnsEmptySet()
    {
        var segment = new TravelSegment { Route = null };
        AssertInt(segment.RouteTags.Count).IsEqual(0);
    }
}
