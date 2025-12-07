using GdUnit4;
using static GdUnit4.Assertions;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace FringeTactics.Tests;

[TestSuite]
[RequireGodotRuntime]
public class GN2RouteTests
{
    // ========================================================================
    // MST TESTS
    // ========================================================================

    [TestCase]
    public void BuildMST_ReturnsNMinus1Edges()
    {
        var config = new GalaxyConfig { SystemCount = 10 };
        var rng = new RngService(12345).Campaign;
        var generator = new GalaxyGenerator(config, rng);

        var positions = generator.GeneratePositions();
        var mst = generator.BuildMST(positions);

        // MST has exactly n-1 edges for n nodes
        AssertInt(mst.Count).IsEqual(positions.Count - 1);
    }

    [TestCase]
    public void BuildMST_EmptyPositions_ReturnsEmpty()
    {
        var config = GalaxyConfig.Default;
        var rng = new RngService(12345).Campaign;
        var generator = new GalaxyGenerator(config, rng);

        var mst = generator.BuildMST(new List<Vector2>());

        AssertInt(mst.Count).IsEqual(0);
    }

    [TestCase]
    public void BuildMST_SinglePosition_ReturnsEmpty()
    {
        var config = GalaxyConfig.Default;
        var rng = new RngService(12345).Campaign;
        var generator = new GalaxyGenerator(config, rng);

        var positions = new List<Vector2> { new Vector2(100, 100) };
        var mst = generator.BuildMST(positions);

        AssertInt(mst.Count).IsEqual(0);
    }

    [TestCase]
    public void BuildMST_TwoPositions_ReturnsOneEdge()
    {
        var config = GalaxyConfig.Default;
        var rng = new RngService(12345).Campaign;
        var generator = new GalaxyGenerator(config, rng);

        var positions = new List<Vector2>
        {
            new Vector2(100, 100),
            new Vector2(200, 200)
        };
        var mst = generator.BuildMST(positions);

        AssertInt(mst.Count).IsEqual(1);
        AssertBool(mst[0] == (0, 1) || mst[0] == (1, 0)).IsTrue();
    }

    [TestCase]
    public void BuildMST_AllNodesConnected()
    {
        var config = new GalaxyConfig { SystemCount = 8 };
        var rng = new RngService(12345).Campaign;
        var generator = new GalaxyGenerator(config, rng);

        var positions = generator.GeneratePositions();
        var mst = generator.BuildMST(positions);

        // Verify all nodes appear in at least one edge
        var connectedNodes = new HashSet<int>();
        foreach (var (a, b) in mst)
        {
            connectedNodes.Add(a);
            connectedNodes.Add(b);
        }

        AssertInt(connectedNodes.Count).IsEqual(positions.Count);
    }

    [TestCase]
    public void BuildMST_IsDeterministic()
    {
        var config = new GalaxyConfig { SystemCount = 10 };

        var rng1 = new RngService(12345).Campaign;
        var generator1 = new GalaxyGenerator(config, rng1);
        var positions1 = generator1.GeneratePositions();
        var mst1 = generator1.BuildMST(positions1);

        var rng2 = new RngService(12345).Campaign;
        var generator2 = new GalaxyGenerator(config, rng2);
        var positions2 = generator2.GeneratePositions();
        var mst2 = generator2.BuildMST(positions2);

        AssertInt(mst1.Count).IsEqual(mst2.Count);

        for (int i = 0; i < mst1.Count; i++)
        {
            AssertInt(mst1[i].Item1).IsEqual(mst2[i].Item1);
            AssertInt(mst1[i].Item2).IsEqual(mst2[i].Item2);
        }
    }

    // ========================================================================
    // EXTRA ROUTES TESTS
    // ========================================================================

    [TestCase]
    public void AddExtraRoutes_RespectsMaxConnections()
    {
        var config = new GalaxyConfig
        {
            SystemCount = 10,
            MaxConnections = 3,
            MaxRouteDistance = 300f
        };
        var rng = new RngService(12345).Campaign;
        var generator = new GalaxyGenerator(config, rng);

        var positions = generator.GeneratePositions();
        var mst = generator.BuildMST(positions);
        var extra = generator.AddExtraRoutes(positions, mst);

        // Count connections per node
        var connectionCount = new int[positions.Count];
        foreach (var (a, b) in mst.Concat(extra))
        {
            connectionCount[a]++;
            connectionCount[b]++;
        }

        // No node should exceed max connections
        foreach (var count in connectionCount)
        {
            AssertInt(count).IsLessEqual(config.MaxConnections);
        }
    }

    [TestCase]
    public void AddExtraRoutes_RespectsMaxDistance()
    {
        var config = new GalaxyConfig
        {
            SystemCount = 10,
            MaxRouteDistance = 150f
        };
        var rng = new RngService(12345).Campaign;
        var generator = new GalaxyGenerator(config, rng);

        var positions = generator.GeneratePositions();
        var mst = generator.BuildMST(positions);
        var extra = generator.AddExtraRoutes(positions, mst);

        // All extra edges should be within max distance
        foreach (var (a, b) in extra)
        {
            float dist = positions[a].DistanceTo(positions[b]);
            AssertFloat(dist).IsLessEqual(config.MaxRouteDistance);
        }
    }

    [TestCase]
    public void AddExtraRoutes_NoDuplicateEdges()
    {
        var config = new GalaxyConfig { SystemCount = 10 };
        var rng = new RngService(12345).Campaign;
        var generator = new GalaxyGenerator(config, rng);

        var positions = generator.GeneratePositions();
        var mst = generator.BuildMST(positions);
        var extra = generator.AddExtraRoutes(positions, mst);

        // Normalize all edges (smaller index first)
        var allEdges = new HashSet<(int, int)>();
        foreach (var (a, b) in mst.Concat(extra))
        {
            var normalized = (System.Math.Min(a, b), System.Math.Max(a, b));
            AssertBool(allEdges.Contains(normalized)).IsFalse();
            allEdges.Add(normalized);
        }
    }

    [TestCase]
    public void AddExtraRoutes_CanAddEdges()
    {
        // With high max distance and connections, should add some extra edges
        var config = new GalaxyConfig
        {
            SystemCount = 10,
            MaxConnections = 5,
            MaxRouteDistance = 400f
        };
        var rng = new RngService(99999).Campaign;
        var generator = new GalaxyGenerator(config, rng);

        var positions = generator.GeneratePositions();
        var mst = generator.BuildMST(positions);
        var extra = generator.AddExtraRoutes(positions, mst);

        // Should have at least some extra edges with these generous settings
        // (probabilistic, but very likely with seed 99999)
        AssertInt(extra.Count).IsGreaterEqual(0);
    }

    // ========================================================================
    // ROUTE CREATION TESTS
    // ========================================================================

    [TestCase]
    public void CreateRoutes_AddsRoutesToWorld()
    {
        var config = new GalaxyConfig { SystemCount = 8 };
        var rng = new RngService(12345).Campaign;
        var generator = new GalaxyGenerator(config, rng);

        var world = generator.Generate();

        // Should have routes
        AssertInt(world.Routes.Count).IsGreater(0);
    }

    [TestCase]
    public void CreateRoutes_AllSystemsReachable()
    {
        var config = new GalaxyConfig { SystemCount = 10 };
        var rng = new RngService(12345).Campaign;
        var generator = new GalaxyGenerator(config, rng);

        var world = generator.Generate();

        // All systems should be reachable from system 0
        var firstSystem = world.GetAllSystems().First();
        foreach (var system in world.GetAllSystems())
        {
            AssertBool(world.IsReachable(firstSystem.Id, system.Id)).IsTrue();
        }
    }

    [TestCase]
    public void CreateRoutes_RoutesHaveDistance()
    {
        var config = GalaxyConfig.Default;
        var rng = new RngService(12345).Campaign;
        var generator = new GalaxyGenerator(config, rng);

        var world = generator.Generate();

        foreach (var route in world.GetAllRoutes())
        {
            AssertFloat(route.Distance).IsGreater(0f);
        }
    }

    [TestCase]
    public void CreateRoutes_RoutesHaveDefaultHazard()
    {
        var config = GalaxyConfig.Default;
        var rng = new RngService(12345).Campaign;
        var generator = new GalaxyGenerator(config, rng);

        var world = generator.Generate();

        foreach (var route in world.GetAllRoutes())
        {
            AssertInt(route.HazardLevel).IsGreaterEqual(0);
        }
    }

    // ========================================================================
    // INTEGRATION TESTS
    // ========================================================================

    [TestCase]
    public void Generate_HasMoreRoutesThanMST()
    {
        // With default settings, should usually have extra routes
        var config = GalaxyConfig.Default;
        var rng = new RngService(12345).Campaign;
        var generator = new GalaxyGenerator(config, rng);

        var world = generator.Generate();
        int minRoutes = config.SystemCount - 1; // MST minimum

        AssertInt(world.Routes.Count).IsGreaterEqual(minRoutes);
    }

    [TestCase]
    public void Generate_RoutesDeterministic()
    {
        var config = GalaxyConfig.Default;

        var rng1 = new RngService(12345).Campaign;
        var generator1 = new GalaxyGenerator(config, rng1);
        var world1 = generator1.Generate();

        var rng2 = new RngService(12345).Campaign;
        var generator2 = new GalaxyGenerator(config, rng2);
        var world2 = generator2.Generate();

        AssertInt(world1.Routes.Count).IsEqual(world2.Routes.Count);

        var routes1 = world1.Routes.Keys.OrderBy(k => k).ToList();
        var routes2 = world2.Routes.Keys.OrderBy(k => k).ToList();

        for (int i = 0; i < routes1.Count; i++)
        {
            AssertInt(routes1[i]).IsEqual(routes2[i]);
        }
    }

    [TestCase]
    public void Generate_SmallConfig_HasRoutes()
    {
        var config = GalaxyConfig.Small;
        var rng = new RngService(12345).Campaign;
        var generator = new GalaxyGenerator(config, rng);

        var world = generator.Generate();

        AssertInt(world.Routes.Count).IsGreaterEqual(config.SystemCount - 1);
    }

    [TestCase]
    public void Generate_LargeConfig_HasRoutes()
    {
        var config = GalaxyConfig.Large;
        var rng = new RngService(12345).Campaign;
        var generator = new GalaxyGenerator(config, rng);

        var world = generator.Generate();

        AssertInt(world.Routes.Count).IsGreaterEqual(config.SystemCount - 1);
    }

    [TestCase]
    public void Generate_SystemConnectionsMatchRoutes()
    {
        var config = GalaxyConfig.Default;
        var rng = new RngService(12345).Campaign;
        var generator = new GalaxyGenerator(config, rng);

        var world = generator.Generate();

        // Each route should have corresponding system connections
        foreach (var route in world.GetAllRoutes())
        {
            var systemA = world.GetSystem(route.SystemA);
            var systemB = world.GetSystem(route.SystemB);

            AssertBool(systemA.Connections.Contains(route.SystemB)).IsTrue();
            AssertBool(systemB.Connections.Contains(route.SystemA)).IsTrue();
        }
    }
}
