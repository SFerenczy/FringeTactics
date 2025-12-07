using GdUnit4;
using static GdUnit4.Assertions;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace FringeTactics.Tests;

[TestSuite]
[RequireGodotRuntime]
public class GN2ContentTests
{
    // ========================================================================
    // SYSTEM TYPE TESTS
    // ========================================================================

    [TestCase]
    public void AssignSystemTypes_CapitalsAreStations()
    {
        var config = GalaxyConfig.Default;
        var rng = new RngService(12345).Campaign;
        var generator = new GalaxyGenerator(config, rng);

        var world = generator.Generate();

        // All hubs should be Station type
        var hubs = world.GetSystemsByTag(WorldTags.Hub).ToList();
        foreach (var hub in hubs)
        {
            AssertThat(hub.Type).IsEqual(SystemType.Station);
        }
    }

    [TestCase]
    public void AssignSystemTypes_ContestedSystemsHaveContestedType()
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

    [TestCase]
    public void AssignSystemTypes_AllSystemsHaveValidType()
    {
        var config = GalaxyConfig.Default;
        var rng = new RngService(12345).Campaign;
        var generator = new GalaxyGenerator(config, rng);

        var world = generator.Generate();

        foreach (var system in world.GetAllSystems())
        {
            // Type should be one of the valid enum values
            AssertBool(System.Enum.IsDefined(typeof(SystemType), system.Type)).IsTrue();
        }
    }

    [TestCase]
    public void AssignSystemTypes_TypeDistributionVaries()
    {
        var config = GalaxyConfig.Large; // More systems for better distribution
        var rng = new RngService(12345).Campaign;
        var generator = new GalaxyGenerator(config, rng);

        var world = generator.Generate();

        // Count types
        var typeCounts = world.GetAllSystems()
            .GroupBy(s => s.Type)
            .ToDictionary(g => g.Key, g => g.Count());

        // Should have multiple different types
        AssertInt(typeCounts.Count).IsGreater(1);
    }

    // ========================================================================
    // SYSTEM NAME TESTS
    // ========================================================================

    [TestCase]
    public void AssignSystemNames_AllSystemsHaveNames()
    {
        var config = GalaxyConfig.Default;
        var rng = new RngService(12345).Campaign;
        var generator = new GalaxyGenerator(config, rng);

        var world = generator.Generate();

        foreach (var system in world.GetAllSystems())
        {
            AssertString(system.Name).IsNotEmpty();
            AssertBool(system.Name.StartsWith("System_")).IsFalse(); // Not placeholder
        }
    }

    [TestCase]
    public void AssignSystemNames_NamesAreUnique()
    {
        var config = GalaxyConfig.Default;
        var rng = new RngService(12345).Campaign;
        var generator = new GalaxyGenerator(config, rng);

        var world = generator.Generate();

        var names = world.GetAllSystems().Select(s => s.Name).ToList();
        var uniqueNames = names.Distinct().ToList();

        AssertInt(uniqueNames.Count).IsEqual(names.Count);
    }

    [TestCase]
    public void AssignSystemNames_NamesMatchType()
    {
        var config = GalaxyConfig.Large;
        var rng = new RngService(12345).Campaign;
        var generator = new GalaxyGenerator(config, rng);

        var world = generator.Generate();

        // Derelict systems should have derelict-style names
        var derelicts = world.GetAllSystems()
            .Where(s => s.Type == SystemType.Derelict)
            .ToList();

        foreach (var system in derelicts)
        {
            // Derelict names typically start with "Wreck of", "Ruins of", etc.
            bool hasDerelictPrefix = system.Name.StartsWith("Wreck") ||
                                     system.Name.StartsWith("Ruins") ||
                                     system.Name.StartsWith("Remains") ||
                                     system.Name.StartsWith("Hulk") ||
                                     system.Name.StartsWith("Ghost");
            AssertBool(hasDerelictPrefix).IsTrue();
        }
    }

    [TestCase]
    public void AssignSystemNames_IsDeterministic()
    {
        var config = GalaxyConfig.Default;

        var rng1 = new RngService(12345).Campaign;
        var generator1 = new GalaxyGenerator(config, rng1);
        var world1 = generator1.Generate();

        var rng2 = new RngService(12345).Campaign;
        var generator2 = new GalaxyGenerator(config, rng2);
        var world2 = generator2.Generate();

        var names1 = world1.GetAllSystems().OrderBy(s => s.Id).Select(s => s.Name).ToList();
        var names2 = world2.GetAllSystems().OrderBy(s => s.Id).Select(s => s.Name).ToList();

        for (int i = 0; i < names1.Count; i++)
        {
            AssertString(names1[i]).IsEqual(names2[i]);
        }
    }

    // ========================================================================
    // SYSTEM METRICS TESTS
    // ========================================================================

    [TestCase]
    public void InitializeSystemMetrics_AllSystemsHaveMetrics()
    {
        var config = GalaxyConfig.Default;
        var rng = new RngService(12345).Campaign;
        var generator = new GalaxyGenerator(config, rng);

        var world = generator.Generate();

        foreach (var system in world.GetAllSystems())
        {
            AssertBool(system.Metrics != null).IsTrue();
        }
    }

    [TestCase]
    public void InitializeSystemMetrics_MetricsInValidRange()
    {
        var config = GalaxyConfig.Default;
        var rng = new RngService(12345).Campaign;
        var generator = new GalaxyGenerator(config, rng);

        var world = generator.Generate();

        foreach (var system in world.GetAllSystems())
        {
            AssertInt(system.Metrics.Stability).IsBetween(0, 5);
            AssertInt(system.Metrics.SecurityLevel).IsBetween(0, 5);
            AssertInt(system.Metrics.CriminalActivity).IsBetween(0, 5);
            AssertInt(system.Metrics.EconomicActivity).IsBetween(0, 5);
            AssertInt(system.Metrics.LawEnforcementPresence).IsBetween(0, 5);
        }
    }

    [TestCase]
    public void InitializeSystemMetrics_CapitalsHaveBoostedMetrics()
    {
        var config = GalaxyConfig.Default;
        var rng = new RngService(12345).Campaign;
        var generator = new GalaxyGenerator(config, rng);

        var world = generator.Generate();

        var hubs = world.GetSystemsByTag(WorldTags.Hub).ToList();
        foreach (var hub in hubs)
        {
            // Capitals should have high stability and security (with variance)
            AssertInt(hub.Metrics.Stability).IsGreaterEqual(3);
            AssertInt(hub.Metrics.SecurityLevel).IsGreaterEqual(2);
        }
    }

    [TestCase]
    public void InitializeSystemMetrics_ContestedSystemsAreUnstable()
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
            // Contested systems should have low stability
            AssertInt(system.Metrics.Stability).IsLessEqual(2);
        }
    }

    // ========================================================================
    // SYSTEM TAGS TESTS
    // ========================================================================

    [TestCase]
    public void AssignSystemTags_AsteroidSystemsHaveMiningTag()
    {
        var config = GalaxyConfig.Large;
        var rng = new RngService(12345).Campaign;
        var generator = new GalaxyGenerator(config, rng);

        var world = generator.Generate();

        var asteroids = world.GetAllSystems()
            .Where(s => s.Type == SystemType.Asteroid)
            .ToList();

        foreach (var system in asteroids)
        {
            AssertBool(system.HasTag(WorldTags.Mining)).IsTrue();
        }
    }

    [TestCase]
    public void AssignSystemTags_HighSecuritySystemsHaveMilitaryTag()
    {
        var config = GalaxyConfig.Large;
        var rng = new RngService(12345).Campaign;
        var generator = new GalaxyGenerator(config, rng);

        var world = generator.Generate();

        var highSecurity = world.GetAllSystems()
            .Where(s => s.Metrics.SecurityLevel >= 4)
            .ToList();

        foreach (var system in highSecurity)
        {
            AssertBool(system.HasTag(WorldTags.Military)).IsTrue();
        }
    }

    [TestCase]
    public void AssignSystemTags_HighCrimeLowSecurityIsLawless()
    {
        var config = GalaxyConfig.Large;
        var rng = new RngService(12345).Campaign;
        var generator = new GalaxyGenerator(config, rng);

        var world = generator.Generate();

        var lawless = world.GetAllSystems()
            .Where(s => s.Metrics.CriminalActivity >= 4 && s.Metrics.SecurityLevel <= 1)
            .ToList();

        foreach (var system in lawless)
        {
            AssertBool(system.HasTag(WorldTags.Lawless)).IsTrue();
        }
    }

    // ========================================================================
    // ROUTE HAZARD TESTS
    // ========================================================================

    [TestCase]
    public void UpdateRouteHazards_AllRoutesHaveValidHazard()
    {
        var config = GalaxyConfig.Default;
        var rng = new RngService(12345).Campaign;
        var generator = new GalaxyGenerator(config, rng);

        var world = generator.Generate();

        foreach (var route in world.GetAllRoutes())
        {
            AssertInt(route.HazardLevel).IsBetween(0, 5);
        }
    }

    [TestCase]
    public void UpdateRouteHazards_DangerousRoutesHaveTag()
    {
        var config = GalaxyConfig.Large;
        var rng = new RngService(12345).Campaign;
        var generator = new GalaxyGenerator(config, rng);

        var world = generator.Generate();

        var dangerous = world.GetAllRoutes()
            .Where(r => r.HazardLevel >= 3)
            .ToList();

        foreach (var route in dangerous)
        {
            AssertBool(route.HasTag(WorldTags.Dangerous)).IsTrue();
        }
    }

    [TestCase]
    public void UpdateRouteHazards_ContestedRoutesAreMoreDangerous()
    {
        var config = new GalaxyConfig
        {
            SystemCount = 15,
            NeutralFraction = 0f
        };
        var rng = new RngService(99999).Campaign;
        var generator = new GalaxyGenerator(config, rng);

        var world = generator.Generate();

        // Routes touching contested systems should have higher hazard
        var contestedSystems = world.GetSystemsByTag(WorldTags.Contested)
            .Select(s => s.Id)
            .ToHashSet();

        if (contestedSystems.Count > 0)
        {
            var contestedRoutes = world.GetAllRoutes()
                .Where(r => contestedSystems.Contains(r.SystemA) || contestedSystems.Contains(r.SystemB))
                .ToList();

            var otherRoutes = world.GetAllRoutes()
                .Where(r => !contestedSystems.Contains(r.SystemA) && !contestedSystems.Contains(r.SystemB))
                .ToList();

            if (contestedRoutes.Count > 0 && otherRoutes.Count > 0)
            {
                float avgContestedHazard = (float)contestedRoutes.Average(r => r.HazardLevel);
                float avgOtherHazard = (float)otherRoutes.Average(r => r.HazardLevel);

                // Contested routes should be more dangerous on average
                AssertFloat(avgContestedHazard).IsGreaterEqual(avgOtherHazard);
            }
        }
    }

    [TestCase]
    public void UpdateRouteHazards_IsDeterministic()
    {
        var config = GalaxyConfig.Default;

        var rng1 = new RngService(12345).Campaign;
        var generator1 = new GalaxyGenerator(config, rng1);
        var world1 = generator1.Generate();

        var rng2 = new RngService(12345).Campaign;
        var generator2 = new GalaxyGenerator(config, rng2);
        var world2 = generator2.Generate();

        var routes1 = world1.GetAllRoutes().OrderBy(r => r.Id).ToList();
        var routes2 = world2.GetAllRoutes().OrderBy(r => r.Id).ToList();

        AssertInt(routes1.Count).IsEqual(routes2.Count);

        for (int i = 0; i < routes1.Count; i++)
        {
            AssertInt(routes1[i].HazardLevel).IsEqual(routes2[i].HazardLevel);
        }
    }

    // ========================================================================
    // INTEGRATION TESTS
    // ========================================================================

    [TestCase]
    public void Generate_FullContentAssignment()
    {
        var config = GalaxyConfig.Default;
        var rng = new RngService(12345).Campaign;
        var generator = new GalaxyGenerator(config, rng);

        var world = generator.Generate();

        // All systems should have complete content
        foreach (var system in world.GetAllSystems())
        {
            AssertString(system.Name).IsNotEmpty();
            AssertBool(system.Metrics != null).IsTrue();
            AssertBool(System.Enum.IsDefined(typeof(SystemType), system.Type)).IsTrue();
        }

        // All routes should have hazards
        foreach (var route in world.GetAllRoutes())
        {
            AssertInt(route.HazardLevel).IsBetween(0, 5);
        }
    }

    [TestCase]
    public void Generate_SmallConfig_HasContent()
    {
        var config = GalaxyConfig.Small;
        var rng = new RngService(12345).Campaign;
        var generator = new GalaxyGenerator(config, rng);

        var world = generator.Generate();

        foreach (var system in world.GetAllSystems())
        {
            AssertString(system.Name).IsNotEmpty();
            AssertBool(system.Metrics != null).IsTrue();
        }
    }

    [TestCase]
    public void Generate_LargeConfig_HasContent()
    {
        var config = GalaxyConfig.Large;
        var rng = new RngService(12345).Campaign;
        var generator = new GalaxyGenerator(config, rng);

        var world = generator.Generate();

        foreach (var system in world.GetAllSystems())
        {
            AssertString(system.Name).IsNotEmpty();
            AssertBool(system.Metrics != null).IsTrue();
        }
    }
}
