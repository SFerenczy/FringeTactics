using GdUnit4;
using static GdUnit4.Assertions;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace FringeTactics.Tests;

[TestSuite]
[RequireGodotRuntime]
public class WD2TopologyTests
{
    private WorldState world;

    [Before]
    public void Setup()
    {
        world = CreateTestTopology();
    }

    /// <summary>
    /// Create a simple test topology:
    /// 0 -- 1 -- 2
    /// |    |
    /// 3    4
    /// </summary>
    private WorldState CreateTestTopology()
    {
        var w = new WorldState { Name = "Test Sector" };

        w.AddSystem(new StarSystem(0, "Hub", SystemType.Station, new Vector2(0, 0)));
        w.AddSystem(new StarSystem(1, "Waypoint", SystemType.Outpost, new Vector2(100, 0)));
        w.AddSystem(new StarSystem(2, "Mining", SystemType.Asteroid, new Vector2(200, 0)));
        w.AddSystem(new StarSystem(3, "Outpost", SystemType.Outpost, new Vector2(0, 100)));
        w.AddSystem(new StarSystem(4, "Derelict", SystemType.Derelict, new Vector2(100, 100)));

        w.Connect(0, 1, 1, WorldTags.Patrolled);
        w.Connect(1, 2, 2, WorldTags.Asteroid);
        w.Connect(0, 3, 0);
        w.Connect(1, 4, 3, WorldTags.Dangerous);

        return w;
    }

    // ========== Route Query Tests ==========

    [TestCase]
    public void GetRoute_ReturnsCorrectRoute()
    {
        var route = world.GetRoute(0, 1);

        AssertObject(route).IsNotNull();
        AssertBool(route.Connects(0)).IsTrue();
        AssertBool(route.Connects(1)).IsTrue();
    }

    [TestCase]
    public void GetRoute_ReturnsSameForBothDirections()
    {
        var route01 = world.GetRoute(0, 1);
        var route10 = world.GetRoute(1, 0);

        AssertObject(route01).IsNotNull();
        AssertObject(route10).IsNotNull();
        AssertInt(route01.Id).IsEqual(route10.Id);
    }

    [TestCase]
    public void GetRoute_ReturnsNullForUnconnected()
    {
        var route = world.GetRoute(0, 2);

        AssertObject(route).IsNull();
    }

    [TestCase]
    public void GetRoutesFrom_ReturnsAllConnectedRoutes()
    {
        var routes = world.GetRoutesFrom(1).ToList();

        // System 1 connects to: 0, 2, 4
        AssertInt(routes.Count).IsEqual(3);
    }

    [TestCase]
    public void GetRoutesFrom_ReturnsEmptyForIsolatedSystem()
    {
        world.AddSystem(new StarSystem(5, "Isolated", SystemType.Nebula, new Vector2(300, 300)));
        var routes = world.GetRoutesFrom(5).ToList();

        AssertInt(routes.Count).IsEqual(0);
    }

    [TestCase]
    public void HasRoute_ReturnsTrueForConnected()
    {
        AssertBool(world.HasRoute(0, 1)).IsTrue();
        AssertBool(world.HasRoute(1, 0)).IsTrue();
    }

    [TestCase]
    public void HasRoute_ReturnsFalseForUnconnected()
    {
        AssertBool(world.HasRoute(0, 2)).IsFalse();
        AssertBool(world.HasRoute(3, 4)).IsFalse();
    }

    [TestCase]
    public void GetAllRoutes_ReturnsAllRoutes()
    {
        var routes = world.GetAllRoutes().ToList();

        AssertInt(routes.Count).IsEqual(4);
    }

    [TestCase]
    public void GetRoutesByTag_FiltersCorrectly()
    {
        var patrolled = world.GetRoutesByTag(WorldTags.Patrolled).ToList();
        var dangerous = world.GetRoutesByTag(WorldTags.Dangerous).ToList();

        AssertInt(patrolled.Count).IsEqual(1);
        AssertInt(dangerous.Count).IsEqual(1);
    }

    [TestCase]
    public void GetRouteHazard_ReturnsCorrectValue()
    {
        AssertInt(world.GetRouteHazard(0, 1)).IsEqual(1);
        AssertInt(world.GetRouteHazard(1, 4)).IsEqual(3);
        AssertInt(world.GetRouteHazard(0, 3)).IsEqual(0);
    }

    [TestCase]
    public void GetRouteHazard_ReturnsZeroForNoRoute()
    {
        AssertInt(world.GetRouteHazard(0, 2)).IsEqual(0);
    }

    // ========== Connect Tests ==========

    [TestCase]
    public void Connect_CreatesRouteWithDistance()
    {
        var route = world.GetRoute(0, 1);

        AssertObject(route).IsNotNull();
        AssertFloat(route.Distance).IsGreater(0f);
    }

    [TestCase]
    public void Connect_ReturnsExistingRouteIfAlreadyConnected()
    {
        var route1 = world.Connect(0, 1);
        var route2 = world.Connect(0, 1, 5, WorldTags.Dangerous);

        AssertInt(route1.Id).IsEqual(route2.Id);
        AssertInt(route1.HazardLevel).IsEqual(1); // Original hazard preserved
    }

    [TestCase]
    public void Connect_UpdatesSystemConnections()
    {
        var system0 = world.GetSystem(0);
        var system1 = world.GetSystem(1);

        AssertBool(system0.Connections.Contains(1)).IsTrue();
        AssertBool(system1.Connections.Contains(0)).IsTrue();
    }

    [TestCase]
    public void Connect_ReturnsNullForInvalidSystems()
    {
        var route = world.Connect(0, 99);

        AssertObject(route).IsNull();
    }

    [TestCase]
    public void AddRoute_SyncsSystemConnections()
    {
        var newWorld = new WorldState();
        newWorld.AddSystem(new StarSystem(0, "A", SystemType.Station, new Vector2(0, 0)));
        newWorld.AddSystem(new StarSystem(1, "B", SystemType.Outpost, new Vector2(100, 0)));

        var route = new Route(0, 1, 100f) { HazardLevel = 2 };
        newWorld.AddRoute(route);

        AssertBool(newWorld.GetSystem(0).Connections.Contains(1)).IsTrue();
        AssertBool(newWorld.GetSystem(1).Connections.Contains(0)).IsTrue();
        AssertObject(newWorld.GetRoute(0, 1)).IsNotNull();
    }

    // ========== Topology Query Tests ==========

    [TestCase]
    public void AreConnected_UsesRoutes()
    {
        AssertBool(world.AreConnected(0, 1)).IsTrue();
        AssertBool(world.AreConnected(0, 2)).IsFalse();
    }

    [TestCase]
    public void GetTravelDistance_UsesRouteDistance()
    {
        var route = world.GetRoute(0, 1);
        float distance = world.GetTravelDistance(0, 1);

        AssertFloat(distance).IsEqual(route.Distance);
    }

    [TestCase]
    public void GetTravelDistance_ComputesForNoRoute()
    {
        float distance = world.GetTravelDistance(0, 2);

        // Should compute from positions: (0,0) to (200,0) = 200
        AssertFloat(distance).IsEqual(200f);
    }

    // ========== Pathfinding Tests ==========

    [TestCase]
    public void FindPath_ReturnsShortestPath()
    {
        var path = world.FindPath(0, 2);

        AssertInt(path.Count).IsEqual(3);
        AssertInt(path[0]).IsEqual(0);
        AssertInt(path[1]).IsEqual(1);
        AssertInt(path[2]).IsEqual(2);
    }

    [TestCase]
    public void FindPath_ReturnsSingleNodeForSameSystem()
    {
        var path = world.FindPath(0, 0);

        AssertInt(path.Count).IsEqual(1);
        AssertInt(path[0]).IsEqual(0);
    }

    [TestCase]
    public void FindPath_ReturnsEmptyForUnreachable()
    {
        world.AddSystem(new StarSystem(5, "Isolated", SystemType.Nebula, new Vector2(300, 300)));
        var path = world.FindPath(0, 5);

        AssertInt(path.Count).IsEqual(0);
    }

    [TestCase]
    public void FindPath_ReturnsEmptyForInvalidSystems()
    {
        var path = world.FindPath(0, 99);

        AssertInt(path.Count).IsEqual(0);
    }

    [TestCase]
    public void GetPathDistance_SumsRouteDistances()
    {
        var path = world.FindPath(0, 2);
        float distance = world.GetPathDistance(path);

        var route01 = world.GetRoute(0, 1);
        var route12 = world.GetRoute(1, 2);
        float expected = route01.Distance + route12.Distance;

        AssertFloat(distance).IsEqual(expected);
    }

    [TestCase]
    public void GetPathDistance_ReturnsZeroForShortPath()
    {
        var path = new List<int> { 0 };
        float distance = world.GetPathDistance(path);

        AssertFloat(distance).IsEqual(0f);
    }

    [TestCase]
    public void GetPathDistance_ReturnsZeroForNull()
    {
        float distance = world.GetPathDistance(null);

        AssertFloat(distance).IsEqual(0f);
    }

    [TestCase]
    public void GetPathHazard_SumsRouteHazards()
    {
        var path = world.FindPath(0, 2);
        int hazard = world.GetPathHazard(path);

        // Route 0-1 hazard 1, Route 1-2 hazard 2
        AssertInt(hazard).IsEqual(3);
    }

    [TestCase]
    public void GetPathRoutes_ReturnsRoutesAlongPath()
    {
        var path = world.FindPath(0, 2);
        var routes = world.GetPathRoutes(path);

        AssertInt(routes.Count).IsEqual(2);
    }

    [TestCase]
    public void IsReachable_ReturnsTrueForConnectedSystems()
    {
        AssertBool(world.IsReachable(0, 2)).IsTrue();
        AssertBool(world.IsReachable(0, 4)).IsTrue();
        AssertBool(world.IsReachable(3, 4)).IsTrue();
    }

    [TestCase]
    public void IsReachable_ReturnsFalseForIsolatedSystems()
    {
        world.AddSystem(new StarSystem(5, "Isolated", SystemType.Nebula, new Vector2(300, 300)));

        AssertBool(world.IsReachable(0, 5)).IsFalse();
    }

    // ========== Convenience Query Tests ==========

    [TestCase]
    public void GetDangerousRoutes_FiltersCorrectly()
    {
        var dangerous = world.GetDangerousRoutes(3).ToList();

        // Only route 1-4 has hazard 3
        AssertInt(dangerous.Count).IsEqual(1);
        AssertBool(dangerous[0].Connects(1)).IsTrue();
        AssertBool(dangerous[0].Connects(4)).IsTrue();
    }

    [TestCase]
    public void GetSafeRoutes_FiltersCorrectly()
    {
        var safe = world.GetSafeRoutes(1).ToList();

        // Routes with hazard <= 1: 0-1 (1), 0-3 (0)
        AssertInt(safe.Count).IsEqual(2);
    }

    [TestCase]
    public void GetNearbyStationSystems_FindsSystemsWithStations()
    {
        // Add stations to some systems
        world.AddStation(new Station(0, "Station A", 1));
        world.AddStation(new Station(1, "Station B", 2));

        var nearby = world.GetNearbyStationSystems(0, 2);

        // Systems 1 and 2 have stations and are within 2 hops of 0
        AssertInt(nearby.Count).IsEqual(2);
    }

    [TestCase]
    public void GetNearbyStationSystems_ExcludesOrigin()
    {
        world.AddStation(new Station(0, "Origin Station", 0));
        world.AddStation(new Station(1, "Nearby Station", 1));

        var nearby = world.GetNearbyStationSystems(0, 2);

        // Should not include origin system 0
        AssertBool(nearby.Any(s => s.Id == 0)).IsFalse();
        AssertBool(nearby.Any(s => s.Id == 1)).IsTrue();
    }

    [TestCase]
    public void GetNearbyStationSystems_RespectsHopLimit()
    {
        world.AddStation(new Station(0, "Station", 2));

        // System 2 is 2 hops from 0, should be found with maxHops=2
        var nearby2 = world.GetNearbyStationSystems(0, 2);
        AssertBool(nearby2.Any(s => s.Id == 2)).IsTrue();

        // System 2 is 2 hops from 0, should NOT be found with maxHops=1
        var nearby1 = world.GetNearbyStationSystems(0, 1);
        AssertBool(nearby1.Any(s => s.Id == 2)).IsFalse();
    }

    [TestCase]
    public void GetNearbyStationSystems_ReturnsEmptyWhenNoStations()
    {
        // Create fresh world without stations
        var freshWorld = CreateTestTopology();
        var nearby = freshWorld.GetNearbyStationSystems(0, 2);

        AssertInt(nearby.Count).IsEqual(0);
    }

    // ========== Serialization Tests ==========

    [TestCase]
    public void WorldState_Serialization_PreservesRoutes()
    {
        var data = world.GetState();
        var restored = WorldState.FromState(data);

        AssertInt(restored.Routes.Count).IsEqual(world.Routes.Count);

        var route = restored.GetRoute(0, 1);
        AssertObject(route).IsNotNull();
        AssertInt(route.HazardLevel).IsEqual(1);
        AssertBool(route.HasTag(WorldTags.Patrolled)).IsTrue();
    }

    [TestCase]
    public void WorldState_Serialization_PreservesAllRouteProperties()
    {
        var data = world.GetState();
        var restored = WorldState.FromState(data);

        foreach (var originalRoute in world.Routes.Values)
        {
            var restoredRoute = restored.Routes[originalRoute.Id];
            AssertInt(restoredRoute.SystemA).IsEqual(originalRoute.SystemA);
            AssertInt(restoredRoute.SystemB).IsEqual(originalRoute.SystemB);
            AssertFloat(restoredRoute.Distance).IsEqual(originalRoute.Distance);
            AssertInt(restoredRoute.HazardLevel).IsEqual(originalRoute.HazardLevel);
        }
    }

    [TestCase]
    public void WorldState_Serialization_HandlesEmptyRoutes()
    {
        var emptyWorld = new WorldState();
        emptyWorld.AddSystem(new StarSystem(0, "Alone", SystemType.Station, new Vector2(0, 0)));

        var data = emptyWorld.GetState();
        var restored = WorldState.FromState(data);

        AssertInt(restored.Routes.Count).IsEqual(0);
    }

    [TestCase]
    public void WorldState_Serialization_RestoresSystemConnections()
    {
        var data = world.GetState();
        var restored = WorldState.FromState(data);

        // Verify connections are rebuilt from routes
        var system0 = restored.GetSystem(0);
        var system1 = restored.GetSystem(1);

        AssertBool(system0.Connections.Contains(1)).IsTrue();
        AssertBool(system1.Connections.Contains(0)).IsTrue();
        AssertBool(system1.Connections.Contains(2)).IsTrue();

        // Verify pathfinding works after restore
        var path = restored.FindPath(0, 2);
        AssertInt(path.Count).IsEqual(3);
    }
}
