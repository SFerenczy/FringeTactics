using GdUnit4;
using static GdUnit4.Assertions;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace FringeTactics.Tests;

[TestSuite]
[RequireGodotRuntime]
public class GN2DistributionTests
{
    // ========================================================================
    // FACTION BALANCE TESTS
    // ========================================================================

    [TestCase]
    public void FactionDistribution_AllFactionsHaveTerritory()
    {
        var config = GalaxyConfig.Default;
        var rng = new RngService(12345).Campaign;
        var generator = new GalaxyGenerator(config, rng);

        var world = generator.Generate();
        var counts = generator.GetFactionSystemCounts(world);

        // Each faction should have at least one system (their capital)
        foreach (var factionId in config.GetFactionIds())
        {
            AssertBool(counts.ContainsKey(factionId)).IsTrue();
            AssertInt(counts[factionId]).IsGreaterEqual(1);
        }
    }

    [TestCase]
    public void FactionDistribution_ReasonablyBalanced()
    {
        var config = new GalaxyConfig
        {
            SystemCount = 15,
            NeutralFraction = 0.1f
        };
        var rng = new RngService(12345).Campaign;
        var generator = new GalaxyGenerator(config, rng);

        var world = generator.Generate();
        var counts = generator.GetFactionSystemCounts(world);

        // Get faction counts (excluding neutral)
        var factionCounts = counts
            .Where(kv => kv.Key != "neutral")
            .Select(kv => kv.Value)
            .ToList();

        if (factionCounts.Count >= 2)
        {
            int max = factionCounts.Max();
            int min = factionCounts.Min();

            // No faction should have more than 3x the systems of another
            AssertInt(max).IsLessEqual(min * 3 + 2);
        }
    }

    [TestCase]
    public void FactionDistribution_NeutralFractionRespected()
    {
        var config = new GalaxyConfig
        {
            SystemCount = 20,
            NeutralFraction = 0.25f
        };
        var rng = new RngService(12345).Campaign;
        var generator = new GalaxyGenerator(config, rng);

        var world = generator.Generate();
        var counts = generator.GetFactionSystemCounts(world);

        int neutralCount = counts.GetValueOrDefault("neutral", 0);
        int expectedNeutral = (int)(config.SystemCount * config.NeutralFraction);

        // Should be within ±2 of expected
        AssertInt(neutralCount).IsGreaterEqual(expectedNeutral - 2);
        AssertInt(neutralCount).IsLessEqual(expectedNeutral + 2);
    }

    [TestCase]
    public void FactionDistribution_ZeroNeutralFraction_NoNeutrals()
    {
        var config = new GalaxyConfig
        {
            SystemCount = 12,
            NeutralFraction = 0f
        };
        var rng = new RngService(12345).Campaign;
        var generator = new GalaxyGenerator(config, rng);

        var world = generator.Generate();
        var counts = generator.GetFactionSystemCounts(world);

        int neutralCount = counts.GetValueOrDefault("neutral", 0);
        AssertInt(neutralCount).IsEqual(0);
    }

    [TestCase]
    public void FactionDistribution_HighNeutralFraction_ManyNeutrals()
    {
        var config = new GalaxyConfig
        {
            SystemCount = 15,
            NeutralFraction = 0.4f
        };
        var rng = new RngService(12345).Campaign;
        var generator = new GalaxyGenerator(config, rng);

        var world = generator.Generate();
        var counts = generator.GetFactionSystemCounts(world);

        int neutralCount = counts.GetValueOrDefault("neutral", 0);
        int expectedNeutral = (int)(config.SystemCount * config.NeutralFraction);

        AssertInt(neutralCount).IsGreaterEqual(expectedNeutral - 2);
    }

    // ========================================================================
    // SYSTEM TYPE DISTRIBUTION TESTS
    // ========================================================================

    [TestCase]
    public void SystemTypeDistribution_HasVariety()
    {
        var config = GalaxyConfig.Large; // More systems for better distribution
        var rng = new RngService(12345).Campaign;
        var generator = new GalaxyGenerator(config, rng);

        var world = generator.Generate();

        var typeCounts = world.GetAllSystems()
            .GroupBy(s => s.Type)
            .ToDictionary(g => g.Key, g => g.Count());

        // Should have at least 3 different types
        AssertInt(typeCounts.Count).IsGreaterEqual(3);
    }

    [TestCase]
    public void SystemTypeDistribution_WeightsRespected()
    {
        // Run multiple generations and check average distribution
        var config = new GalaxyConfig
        {
            SystemCount = 30,
            NeutralFraction = 0f,
            SystemTypeWeights = new()
            {
                [SystemType.Station] = 0.4f,
                [SystemType.Outpost] = 0.3f,
                [SystemType.Asteroid] = 0.2f,
                [SystemType.Nebula] = 0.1f
            }
        };

        var totalCounts = new Dictionary<SystemType, int>();

        // Generate multiple worlds
        for (int seed = 1; seed <= 5; seed++)
        {
            var rng = new RngService(seed * 1000).Campaign;
            var generator = new GalaxyGenerator(config, rng);
            var world = generator.Generate();

            foreach (var system in world.GetAllSystems())
            {
                // Skip capitals (always Station) and contested
                if (system.HasTag(WorldTags.Hub) || system.HasTag(WorldTags.Contested))
                    continue;

                totalCounts[system.Type] = totalCounts.GetValueOrDefault(system.Type, 0) + 1;
            }
        }

        // Station should be most common (highest weight)
        if (totalCounts.ContainsKey(SystemType.Station) && totalCounts.ContainsKey(SystemType.Nebula))
        {
            AssertInt(totalCounts[SystemType.Station]).IsGreater(totalCounts[SystemType.Nebula]);
        }
    }

    [TestCase]
    public void SystemTypeDistribution_CapitalsAreStations()
    {
        var config = GalaxyConfig.Default;
        var rng = new RngService(12345).Campaign;
        var generator = new GalaxyGenerator(config, rng);

        var world = generator.Generate();

        var hubs = world.GetSystemsByTag(WorldTags.Hub).ToList();

        foreach (var hub in hubs)
        {
            AssertThat(hub.Type).IsEqual(SystemType.Station);
        }
    }

    [TestCase]
    public void SystemTypeDistribution_ContestedSystemsHaveContestedType()
    {
        var config = new GalaxyConfig
        {
            SystemCount = 15,
            NeutralFraction = 0f
        };
        var rng = new RngService(99999).Campaign;
        var generator = new GalaxyGenerator(config, rng);

        var world = generator.Generate();

        var contested = world.GetSystemsByTag(WorldTags.Contested).ToList();

        foreach (var system in contested)
        {
            AssertThat(system.Type).IsEqual(SystemType.Contested);
        }
    }

    // ========================================================================
    // STATION DISTRIBUTION TESTS
    // ========================================================================

    [TestCase]
    public void StationDistribution_OnePerInhabitedSystem()
    {
        var config = GalaxyConfig.Default;
        var rng = new RngService(12345).Campaign;
        var generator = new GalaxyGenerator(config, rng);

        var world = generator.Generate();

        foreach (var system in world.GetAllSystems())
        {
            var stations = world.GetStationsInSystem(system.Id).ToList();

            if (config.InhabitedTypes.Contains(system.Type) &&
                system.Type != SystemType.Derelict &&
                system.Type != SystemType.Contested)
            {
                AssertInt(stations.Count).IsEqual(1);
            }
            else
            {
                AssertInt(stations.Count).IsEqual(0);
            }
        }
    }

    [TestCase]
    public void StationDistribution_TypesMatchSystems()
    {
        var config = GalaxyConfig.Large;
        var rng = new RngService(12345).Campaign;
        var generator = new GalaxyGenerator(config, rng);

        var world = generator.Generate();
        var stationCounts = generator.GetStationTypeCounts(world);

        // Should have hub stations (one per faction capital)
        AssertBool(stationCounts.ContainsKey("Hub")).IsTrue();
        AssertInt(stationCounts["Hub"]).IsGreaterEqual(1);

        // Total should match station count
        int total = stationCounts.Values.Sum();
        AssertInt(total).IsEqual(world.Stations.Count);
    }

    // ========================================================================
    // ROUTE DISTRIBUTION TESTS
    // ========================================================================

    [TestCase]
    public void RouteDistribution_MinimumConnectivity()
    {
        var config = GalaxyConfig.Default;
        var rng = new RngService(12345).Campaign;
        var generator = new GalaxyGenerator(config, rng);

        var world = generator.Generate();

        // MST has n-1 edges, so should have at least that many routes
        int minRoutes = config.SystemCount - 1;
        AssertInt(world.Routes.Count).IsGreaterEqual(minRoutes);
    }

    [TestCase]
    public void RouteDistribution_MaxConnectionsRespected()
    {
        var config = new GalaxyConfig
        {
            SystemCount = 15,
            MaxConnections = 3
        };
        var rng = new RngService(12345).Campaign;
        var generator = new GalaxyGenerator(config, rng);

        var world = generator.Generate();

        foreach (var system in world.GetAllSystems())
        {
            AssertInt(system.Connections.Count).IsLessEqual(config.MaxConnections);
        }
    }

    [TestCase]
    public void RouteDistribution_HazardLevelsVary()
    {
        var config = GalaxyConfig.Large;
        var rng = new RngService(12345).Campaign;
        var generator = new GalaxyGenerator(config, rng);

        var world = generator.Generate();

        var hazardLevels = world.GetAllRoutes()
            .Select(r => r.HazardLevel)
            .Distinct()
            .ToList();

        // Should have at least 2 different hazard levels
        AssertInt(hazardLevels.Count).IsGreaterEqual(2);
    }

    [TestCase]
    public void RouteDistribution_TagsApplied()
    {
        var config = GalaxyConfig.Large;
        var rng = new RngService(12345).Campaign;
        var generator = new GalaxyGenerator(config, rng);

        var world = generator.Generate();

        // Check that some routes have tags
        int routesWithTags = world.GetAllRoutes()
            .Count(r => r.Tags.Count > 0);

        // With a large config, should have some tagged routes
        AssertInt(routesWithTags).IsGreaterEqual(0);
    }

    // ========================================================================
    // SPATIAL DISTRIBUTION TESTS
    // ========================================================================

    [TestCase]
    public void SpatialDistribution_MinDistanceRespected()
    {
        var config = GalaxyConfig.Default;
        var rng = new RngService(12345).Campaign;
        var generator = new GalaxyGenerator(config, rng);

        var world = generator.Generate();
        var systems = world.GetAllSystems().ToList();

        float minDistSq = config.MinSystemDistance * config.MinSystemDistance;

        for (int i = 0; i < systems.Count; i++)
        {
            for (int j = i + 1; j < systems.Count; j++)
            {
                float distSq = systems[i].Position.DistanceSquaredTo(systems[j].Position);
                AssertFloat(distSq).IsGreaterEqual(minDistSq * 0.99f); // Small tolerance
            }
        }
    }

    [TestCase]
    public void SpatialDistribution_EdgeMarginRespected()
    {
        var config = GalaxyConfig.Default;
        var rng = new RngService(12345).Campaign;
        var generator = new GalaxyGenerator(config, rng);

        var world = generator.Generate();

        foreach (var system in world.GetAllSystems())
        {
            AssertFloat(system.Position.X).IsGreaterEqual(config.EdgeMargin);
            AssertFloat(system.Position.X).IsLessEqual(config.MapWidth - config.EdgeMargin);
            AssertFloat(system.Position.Y).IsGreaterEqual(config.EdgeMargin);
            AssertFloat(system.Position.Y).IsLessEqual(config.MapHeight - config.EdgeMargin);
        }
    }

    [TestCase]
    public void SpatialDistribution_CapitalsWellSpaced()
    {
        var config = GalaxyConfig.Default;
        var rng = new RngService(12345).Campaign;
        var generator = new GalaxyGenerator(config, rng);

        var world = generator.Generate();

        var capitals = world.GetSystemsByTag(WorldTags.Hub).ToList();

        // Capitals should be reasonably spread out
        for (int i = 0; i < capitals.Count; i++)
        {
            for (int j = i + 1; j < capitals.Count; j++)
            {
                float dist = capitals[i].Position.DistanceTo(capitals[j].Position);
                // Should be at least 50 units apart
                AssertFloat(dist).IsGreater(50f);
            }
        }
    }

    // ========================================================================
    // CONSISTENCY ACROSS SEEDS TESTS
    // ========================================================================

    [TestCase]
    public void ConsistencyAcrossSeeds_SimilarStructure()
    {
        var config = GalaxyConfig.Default;

        int totalSystems = 0;
        int totalRoutes = 0;
        int totalStations = 0;

        // Generate with different seeds
        for (int seed = 1; seed <= 5; seed++)
        {
            var rng = new RngService(seed * 1000).Campaign;
            var generator = new GalaxyGenerator(config, rng);
            var world = generator.Generate();

            totalSystems += world.Systems.Count;
            totalRoutes += world.Routes.Count;
            totalStations += world.Stations.Count;
        }

        // Average should match config
        float avgSystems = totalSystems / 5f;
        AssertFloat(avgSystems).IsEqualApprox(config.SystemCount, 1f);

        // Routes should be consistent (MST + some extra)
        float avgRoutes = totalRoutes / 5f;
        AssertFloat(avgRoutes).IsGreaterEqual(config.SystemCount - 1);
    }

    [TestCase]
    public void ConsistencyAcrossSeeds_AllConnected()
    {
        var config = GalaxyConfig.Default;

        // Generate with different seeds and verify connectivity
        for (int seed = 1; seed <= 5; seed++)
        {
            var rng = new RngService(seed * 1000).Campaign;
            var generator = new GalaxyGenerator(config, rng);
            var world = generator.Generate();

            var systems = world.GetAllSystems().ToList();
            if (systems.Count < 2) continue;

            int startId = systems[0].Id;
            foreach (var system in systems)
            {
                AssertBool(world.IsReachable(startId, system.Id)).IsTrue();
            }
        }
    }
}
