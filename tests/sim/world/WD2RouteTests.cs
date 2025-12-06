using GdUnit4;
using static GdUnit4.Assertions;

namespace FringeTactics.Tests;

[TestSuite]
public class WD2RouteTests
{
    [TestCase]
    public void Route_Constructor_SetsProperties()
    {
        var route = new Route(0, 1, 100f);

        AssertInt(route.SystemA).IsEqual(0);
        AssertInt(route.SystemB).IsEqual(1);
        AssertFloat(route.Distance).IsEqual(100f);
        AssertInt(route.HazardLevel).IsEqual(0);
    }

    [TestCase]
    public void Route_GenerateId_IsDeterministic()
    {
        int id1 = Route.GenerateId(0, 1);
        int id2 = Route.GenerateId(1, 0);

        AssertInt(id1).IsEqual(id2);
    }

    [TestCase]
    public void Route_GenerateId_IsUnique()
    {
        int id01 = Route.GenerateId(0, 1);
        int id02 = Route.GenerateId(0, 2);
        int id12 = Route.GenerateId(1, 2);

        AssertInt(id01).IsNotEqual(id02);
        AssertInt(id01).IsNotEqual(id12);
        AssertInt(id02).IsNotEqual(id12);
    }

    [TestCase]
    public void Route_Constructor_SetsIdFromSystems()
    {
        var route = new Route(3, 7, 50f);
        int expectedId = Route.GenerateId(3, 7);

        AssertInt(route.Id).IsEqual(expectedId);
    }

    [TestCase]
    public void Route_Connects_ReturnsTrueForEndpoints()
    {
        var route = new Route(0, 1, 100f);

        AssertBool(route.Connects(0)).IsTrue();
        AssertBool(route.Connects(1)).IsTrue();
        AssertBool(route.Connects(2)).IsFalse();
    }

    [TestCase]
    public void Route_GetOther_ReturnsOtherEndpoint()
    {
        var route = new Route(0, 1, 100f);

        AssertInt(route.GetOther(0)).IsEqual(1);
        AssertInt(route.GetOther(1)).IsEqual(0);
    }

    [TestCase]
    public void Route_HasTag_WorksCorrectly()
    {
        var route = new Route(0, 1, 100f);
        route.Tags.Add(WorldTags.Dangerous);

        AssertBool(route.HasTag(WorldTags.Dangerous)).IsTrue();
        AssertBool(route.HasTag(WorldTags.Patrolled)).IsFalse();
    }

    [TestCase]
    public void Route_MultipleTags_AllDetected()
    {
        var route = new Route(0, 1, 100f);
        route.Tags.Add(WorldTags.Dangerous);
        route.Tags.Add(WorldTags.Asteroid);

        AssertBool(route.HasTag(WorldTags.Dangerous)).IsTrue();
        AssertBool(route.HasTag(WorldTags.Asteroid)).IsTrue();
        AssertBool(route.HasTag(WorldTags.Hidden)).IsFalse();
    }

    [TestCase]
    public void Route_DefaultConstructor_HasEmptyTags()
    {
        var route = new Route();

        AssertInt(route.Tags.Count).IsEqual(0);
        AssertInt(route.HazardLevel).IsEqual(0);
    }

    [TestCase]
    public void Route_Serialization_RoundTrip()
    {
        var route = new Route(0, 1, 150f)
        {
            HazardLevel = 3,
            Tags = new System.Collections.Generic.HashSet<string> { WorldTags.Dangerous, WorldTags.Asteroid }
        };

        var data = route.GetState();
        var restored = Route.FromState(data);

        AssertInt(restored.Id).IsEqual(route.Id);
        AssertInt(restored.SystemA).IsEqual(route.SystemA);
        AssertInt(restored.SystemB).IsEqual(route.SystemB);
        AssertFloat(restored.Distance).IsEqual(route.Distance);
        AssertInt(restored.HazardLevel).IsEqual(route.HazardLevel);
        AssertBool(restored.HasTag(WorldTags.Dangerous)).IsTrue();
        AssertBool(restored.HasTag(WorldTags.Asteroid)).IsTrue();
    }

    [TestCase]
    public void Route_Serialization_HandlesNullTags()
    {
        var data = new RouteData
        {
            Id = 1,
            SystemA = 0,
            SystemB = 1,
            Distance = 100f,
            HazardLevel = 2,
            Tags = null
        };

        var restored = Route.FromState(data);

        AssertInt(restored.Tags.Count).IsEqual(0);
    }

    [TestCase]
    public void Route_Serialization_PreservesAllFields()
    {
        var route = new Route(5, 10, 250.5f)
        {
            HazardLevel = 5
        };
        route.Tags.Add(WorldTags.Patrolled);

        var data = route.GetState();

        AssertInt(data.Id).IsEqual(route.Id);
        AssertInt(data.SystemA).IsEqual(5);
        AssertInt(data.SystemB).IsEqual(10);
        AssertFloat(data.Distance).IsEqual(250.5f);
        AssertInt(data.HazardLevel).IsEqual(5);
        AssertInt(data.Tags.Count).IsEqual(1);
    }
}
