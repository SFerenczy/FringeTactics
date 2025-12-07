using GdUnit4;
using static GdUnit4.Assertions;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace FringeTactics.Tests;

[TestSuite]
[RequireGodotRuntime]
public class GN2StationTests
{
    // ========================================================================
    // STATION GENERATION TESTS
    // ========================================================================

    [TestCase]
    public void GenerateStations_CreatesStations()
    {
        var config = GalaxyConfig.Default;
        var rng = new RngService(12345).Campaign;
        var generator = new GalaxyGenerator(config, rng);

        var world = generator.Generate();

        AssertInt(world.Stations.Count).IsGreater(0);
    }

    [TestCase]
    public void GenerateStations_InhabitedSystemsHaveStations()
    {
        var config = GalaxyConfig.Default;
        var rng = new RngService(12345).Campaign;
        var generator = new GalaxyGenerator(config, rng);

        var world = generator.Generate();

        foreach (var system in world.GetAllSystems())
        {
            if (config.InhabitedTypes.Contains(system.Type) &&
                system.Type != SystemType.Derelict &&
                system.Type != SystemType.Contested)
            {
                var stations = world.GetStationsInSystem(system.Id).ToList();
                AssertInt(stations.Count).IsGreaterEqual(1);
            }
        }
    }

    [TestCase]
    public void GenerateStations_DerelictsHaveNoStations()
    {
        var config = GalaxyConfig.Large; // More systems for variety
        var rng = new RngService(12345).Campaign;
        var generator = new GalaxyGenerator(config, rng);

        var world = generator.Generate();

        var derelicts = world.GetAllSystems()
            .Where(s => s.Type == SystemType.Derelict)
            .ToList();

        foreach (var system in derelicts)
        {
            var stations = world.GetStationsInSystem(system.Id).ToList();
            AssertInt(stations.Count).IsEqual(0);
        }
    }

    [TestCase]
    public void GenerateStations_ContestedHaveNoStations()
    {
        var config = new GalaxyConfig
        {
            SystemCount = 15,
            NeutralFraction = 0f
        };
        var rng = new RngService(99999).Campaign;
        var generator = new GalaxyGenerator(config, rng);

        var world = generator.Generate();

        var contested = world.GetAllSystems()
            .Where(s => s.Type == SystemType.Contested)
            .ToList();

        foreach (var system in contested)
        {
            var stations = world.GetStationsInSystem(system.Id).ToList();
            AssertInt(stations.Count).IsEqual(0);
        }
    }

    // ========================================================================
    // STATION TYPE TESTS
    // ========================================================================

    [TestCase]
    public void GenerateStations_HubSystemsGetHubStations()
    {
        var config = GalaxyConfig.Default;
        var rng = new RngService(12345).Campaign;
        var generator = new GalaxyGenerator(config, rng);

        var world = generator.Generate();

        var hubs = world.GetSystemsByTag(WorldTags.Hub).ToList();
        foreach (var hub in hubs)
        {
            var stations = world.GetStationsInSystem(hub.Id).ToList();
            AssertInt(stations.Count).IsGreaterEqual(1);

            // Hub station should have Hub tag
            var hubStation = stations.FirstOrDefault(s => s.HasTag(WorldTags.Hub));
            AssertBool(hubStation != null).IsTrue();
        }
    }

    [TestCase]
    public void GenerateStations_AsteroidSystemsGetMiningStations()
    {
        var config = GalaxyConfig.Large;
        var rng = new RngService(12345).Campaign;
        var generator = new GalaxyGenerator(config, rng);

        var world = generator.Generate();

        var asteroids = world.GetAllSystems()
            .Where(s => s.Type == SystemType.Asteroid)
            .Where(s => !s.HasTag(WorldTags.Hub) && !s.HasTag(WorldTags.Military))
            .ToList();

        foreach (var system in asteroids)
        {
            var stations = world.GetStationsInSystem(system.Id).ToList();
            if (stations.Count > 0)
            {
                // Mining stations should have Industrial tag
                var miningStation = stations.FirstOrDefault(s => s.HasTag(WorldTags.Industrial));
                AssertBool(miningStation != null).IsTrue();
            }
        }
    }

    [TestCase]
    public void GenerateStations_StationsHaveNames()
    {
        var config = GalaxyConfig.Default;
        var rng = new RngService(12345).Campaign;
        var generator = new GalaxyGenerator(config, rng);

        var world = generator.Generate();

        foreach (var station in world.Stations.Values)
        {
            AssertString(station.Name).IsNotEmpty();
        }
    }

    [TestCase]
    public void GenerateStations_StationsHaveFacilities()
    {
        var config = GalaxyConfig.Default;
        var rng = new RngService(12345).Campaign;
        var generator = new GalaxyGenerator(config, rng);

        var world = generator.Generate();

        foreach (var station in world.Stations.Values)
        {
            AssertInt(station.Facilities.Count).IsGreater(0);
        }
    }

    [TestCase]
    public void GenerateStations_HubStationsHaveAllFacilities()
    {
        var config = GalaxyConfig.Default;
        var rng = new RngService(12345).Campaign;
        var generator = new GalaxyGenerator(config, rng);

        var world = generator.Generate();

        var hubStations = world.Stations.Values
            .Where(s => s.HasTag(WorldTags.Hub))
            .ToList();

        foreach (var station in hubStations)
        {
            // Hub stations should have shop, mission board, repair, bar, recruitment, fuel
            AssertBool(station.HasFacility(FacilityType.Shop)).IsTrue();
            AssertBool(station.HasFacility(FacilityType.MissionBoard)).IsTrue();
            AssertBool(station.HasFacility(FacilityType.RepairYard)).IsTrue();
            AssertBool(station.HasFacility(FacilityType.Bar)).IsTrue();
            AssertBool(station.HasFacility(FacilityType.Recruitment)).IsTrue();
            AssertBool(station.HasFacility(FacilityType.FuelDepot)).IsTrue();
        }
    }

    // ========================================================================
    // STATION OWNERSHIP TESTS
    // ========================================================================

    [TestCase]
    public void GenerateStations_StationsInheritSystemOwnership()
    {
        var config = GalaxyConfig.Default;
        var rng = new RngService(12345).Campaign;
        var generator = new GalaxyGenerator(config, rng);

        var world = generator.Generate();

        foreach (var station in world.Stations.Values)
        {
            var system = world.GetSystem(station.SystemId);
            if (system != null)
            {
                // Station should have same owner as system (or null if system is neutral)
                AssertString(station.OwningFactionId ?? "null")
                    .IsEqual(system.OwningFactionId ?? "null");
            }
        }
    }

    [TestCase]
    public void GenerateStations_StationsLinkedToSystems()
    {
        var config = GalaxyConfig.Default;
        var rng = new RngService(12345).Campaign;
        var generator = new GalaxyGenerator(config, rng);

        var world = generator.Generate();

        foreach (var station in world.Stations.Values)
        {
            // Station's system should exist
            var system = world.GetSystem(station.SystemId);
            AssertBool(system != null).IsTrue();

            // System should reference this station
            AssertBool(system.StationIds.Contains(station.Id)).IsTrue();
        }
    }

    // ========================================================================
    // DETERMINISM TESTS
    // ========================================================================

    [TestCase]
    public void GenerateStations_IsDeterministic()
    {
        var config = GalaxyConfig.Default;

        var rng1 = new RngService(12345).Campaign;
        var generator1 = new GalaxyGenerator(config, rng1);
        var world1 = generator1.Generate();

        var rng2 = new RngService(12345).Campaign;
        var generator2 = new GalaxyGenerator(config, rng2);
        var world2 = generator2.Generate();

        // Same number of stations
        AssertInt(world1.Stations.Count).IsEqual(world2.Stations.Count);

        // Same station names
        var names1 = world1.Stations.Values.OrderBy(s => s.Id).Select(s => s.Name).ToList();
        var names2 = world2.Stations.Values.OrderBy(s => s.Id).Select(s => s.Name).ToList();

        for (int i = 0; i < names1.Count; i++)
        {
            AssertString(names1[i]).IsEqual(names2[i]);
        }
    }

    // ========================================================================
    // INTEGRATION TESTS
    // ========================================================================

    [TestCase]
    public void Generate_SmallConfig_HasStations()
    {
        var config = GalaxyConfig.Small;
        var rng = new RngService(12345).Campaign;
        var generator = new GalaxyGenerator(config, rng);

        var world = generator.Generate();

        AssertInt(world.Stations.Count).IsGreater(0);
    }

    [TestCase]
    public void Generate_LargeConfig_HasStations()
    {
        var config = GalaxyConfig.Large;
        var rng = new RngService(12345).Campaign;
        var generator = new GalaxyGenerator(config, rng);

        var world = generator.Generate();

        AssertInt(world.Stations.Count).IsGreater(0);
    }

    [TestCase]
    public void Generate_StationCountMatchesInhabitedSystems()
    {
        var config = GalaxyConfig.Default;
        var rng = new RngService(12345).Campaign;
        var generator = new GalaxyGenerator(config, rng);

        var world = generator.Generate();

        // Count inhabited systems (excluding derelicts and contested)
        int inhabitedCount = world.GetAllSystems()
            .Count(s => config.InhabitedTypes.Contains(s.Type) &&
                       s.Type != SystemType.Derelict &&
                       s.Type != SystemType.Contested);

        // Should have one station per inhabited system
        AssertInt(world.Stations.Count).IsEqual(inhabitedCount);
    }

    [TestCase]
    public void GetStationTypeCounts_ReturnsCorrectCounts()
    {
        var config = GalaxyConfig.Default;
        var rng = new RngService(12345).Campaign;
        var generator = new GalaxyGenerator(config, rng);

        var world = generator.Generate();
        var counts = generator.GetStationTypeCounts(world);

        // Total should equal station count
        int total = counts.Values.Sum();
        AssertInt(total).IsEqual(world.Stations.Count);

        // Should have at least hub stations (one per faction capital)
        AssertBool(counts.ContainsKey("Hub")).IsTrue();
        AssertInt(counts["Hub"]).IsGreaterEqual(1);
    }

    [TestCase]
    public void Generate_FullWorld_IsComplete()
    {
        var config = GalaxyConfig.Default;
        var rng = new RngService(12345).Campaign;
        var generator = new GalaxyGenerator(config, rng);

        var world = generator.Generate();

        // World should have all components
        AssertInt(world.Systems.Count).IsEqual(config.SystemCount);
        AssertInt(world.Routes.Count).IsGreater(0);
        AssertInt(world.Factions.Count).IsGreater(0);
        AssertInt(world.Stations.Count).IsGreater(0);

        // All systems should have names and metrics
        foreach (var system in world.GetAllSystems())
        {
            AssertString(system.Name).IsNotEmpty();
            AssertBool(system.Metrics != null).IsTrue();
        }

        // All stations should have facilities
        foreach (var station in world.Stations.Values)
        {
            AssertInt(station.Facilities.Count).IsGreater(0);
        }
    }
}
