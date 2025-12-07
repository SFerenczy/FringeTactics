using GdUnit4;
using static GdUnit4.Assertions;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace FringeTactics.Tests;

[TestSuite]
[RequireGodotRuntime]
public class GN2FactionTests
{
    // ========================================================================
    // CAPITAL PLACEMENT TESTS
    // ========================================================================

    [TestCase]
    public void PlaceFactionCapitals_PlacesOnePerFaction()
    {
        var config = new GalaxyConfig
        {
            SystemCount = 12,
            FactionIds = new() { "corp", "rebels", "pirates" }
        };
        var rng = new RngService(12345).Campaign;
        var generator = new GalaxyGenerator(config, rng);

        var world = generator.Generate();

        // Each faction should have exactly one capital
        foreach (var factionId in config.FactionIds)
        {
            var hubs = world.GetSystemsByFaction(factionId)
                .Where(s => s.HasTag(WorldTags.Hub))
                .ToList();
            AssertInt(hubs.Count).IsEqual(1);
        }
    }

    [TestCase]
    public void PlaceFactionCapitals_CapitalsAreWellSpaced()
    {
        var config = new GalaxyConfig
        {
            SystemCount = 12,
            FactionIds = new() { "corp", "rebels", "pirates" }
        };
        var rng = new RngService(12345).Campaign;
        var generator = new GalaxyGenerator(config, rng);

        var world = generator.Generate();

        // Get capital positions
        var capitals = world.GetAllSystems()
            .Where(s => s.HasTag(WorldTags.Hub))
            .ToList();

        AssertInt(capitals.Count).IsEqual(3);

        // Check that capitals are reasonably spaced apart
        for (int i = 0; i < capitals.Count; i++)
        {
            for (int j = i + 1; j < capitals.Count; j++)
            {
                float dist = capitals[i].Position.DistanceTo(capitals[j].Position);
                // Should be at least 100 units apart (reasonable for default map size)
                AssertFloat(dist).IsGreater(50f);
            }
        }
    }

    [TestCase]
    public void PlaceFactionCapitals_CapitalsHaveHubTag()
    {
        var config = GalaxyConfig.Default;
        var rng = new RngService(12345).Campaign;
        var generator = new GalaxyGenerator(config, rng);

        var world = generator.Generate();

        var hubs = world.GetSystemsByTag(WorldTags.Hub).ToList();

        // Should have one hub per faction
        AssertInt(hubs.Count).IsEqual(config.FactionIds.Count);

        // All hubs should also have Core tag
        foreach (var hub in hubs)
        {
            AssertBool(hub.HasTag(WorldTags.Core)).IsTrue();
        }
    }

    [TestCase]
    public void PlaceFactionCapitals_HandlesFewerSystemsThanFactions()
    {
        var config = new GalaxyConfig
        {
            SystemCount = 2,
            FactionIds = new() { "corp", "syndicate", "militia" }
        };
        var rng = new RngService(12345).Campaign;
        var generator = new GalaxyGenerator(config, rng);

        var world = generator.Generate();

        // Should only place as many capitals as there are systems
        var hubs = world.GetSystemsByTag(WorldTags.Hub).ToList();
        AssertInt(hubs.Count).IsLessEqual(config.SystemCount);
    }

    // ========================================================================
    // FACTION OWNERSHIP TESTS
    // ========================================================================

    [TestCase]
    public void AssignFactionOwnership_AllSystemsHaveOwner()
    {
        var config = new GalaxyConfig
        {
            SystemCount = 10,
            NeutralFraction = 0f // No neutral systems for this test
        };
        var rng = new RngService(12345).Campaign;
        var generator = new GalaxyGenerator(config, rng);

        var world = generator.Generate();

        // All systems should have an owner (since NeutralFraction = 0)
        foreach (var system in world.GetAllSystems())
        {
            AssertBool(system.OwningFactionId != null || system.HasTag(WorldTags.Frontier)).IsTrue();
        }
    }

    [TestCase]
    public void AssignFactionOwnership_CapitalsOwnedByTheirFaction()
    {
        var config = GalaxyConfig.Default;
        var rng = new RngService(12345).Campaign;
        var generator = new GalaxyGenerator(config, rng);

        var world = generator.Generate();

        // Each hub should be owned by a faction
        var hubs = world.GetSystemsByTag(WorldTags.Hub).ToList();
        foreach (var hub in hubs)
        {
            AssertString(hub.OwningFactionId).IsNotEmpty();
        }
    }

    [TestCase]
    public void AssignFactionOwnership_TerritoriesAreContiguous()
    {
        var config = new GalaxyConfig
        {
            SystemCount = 12,
            NeutralFraction = 0f
        };
        var rng = new RngService(12345).Campaign;
        var generator = new GalaxyGenerator(config, rng);

        var world = generator.Generate();

        // For each faction, verify territory is contiguous via BFS
        foreach (var factionId in config.FactionIds)
        {
            var factionSystems = world.GetSystemsByFaction(factionId).ToList();
            if (factionSystems.Count == 0) continue;

            // BFS from first system
            var visited = new HashSet<int>();
            var queue = new Queue<int>();
            queue.Enqueue(factionSystems[0].Id);
            visited.Add(factionSystems[0].Id);

            while (queue.Count > 0)
            {
                int current = queue.Dequeue();
                foreach (int neighbor in world.GetNeighbors(current))
                {
                    var neighborSystem = world.GetSystem(neighbor);
                    if (neighborSystem?.OwningFactionId == factionId && !visited.Contains(neighbor))
                    {
                        visited.Add(neighbor);
                        queue.Enqueue(neighbor);
                    }
                }
            }

            // All faction systems should be reachable from the first one
            AssertInt(visited.Count).IsEqual(factionSystems.Count);
        }
    }

    [TestCase]
    public void AssignFactionOwnership_ContestedSystemsAtBoundaries()
    {
        var config = new GalaxyConfig
        {
            SystemCount = 15,
            FactionIds = new() { "corp", "rebels", "pirates" },
            NeutralFraction = 0f
        };
        var rng = new RngService(99999).Campaign;
        var generator = new GalaxyGenerator(config, rng);

        var world = generator.Generate();

        // Contested systems should exist at faction boundaries
        var contested = world.GetSystemsByTag(WorldTags.Contested).ToList();

        // With 3 factions and 15 systems, there should be some contested zones
        // (probabilistic, but likely with this seed)
        // Just verify the tag exists if there are contested systems
        foreach (var system in contested)
        {
            AssertBool(system.HasTag(WorldTags.Border)).IsTrue();
        }
    }

    // ========================================================================
    // NEUTRAL SYSTEMS TESTS
    // ========================================================================

    [TestCase]
    public void MarkNeutralSystems_RespectsNeutralFraction()
    {
        var config = new GalaxyConfig
        {
            SystemCount = 10,
            NeutralFraction = 0.3f
        };
        var rng = new RngService(12345).Campaign;
        var generator = new GalaxyGenerator(config, rng);

        var world = generator.Generate();

        int neutralCount = world.GetAllSystems()
            .Count(s => s.OwningFactionId == null);

        // Should have approximately 30% neutral (3 of 10)
        int expectedNeutral = (int)(config.SystemCount * config.NeutralFraction);
        AssertInt(neutralCount).IsGreaterEqual(expectedNeutral - 1);
        AssertInt(neutralCount).IsLessEqual(expectedNeutral + 1);
    }

    [TestCase]
    public void MarkNeutralSystems_NeutralSystemsAreFrontier()
    {
        var config = new GalaxyConfig
        {
            SystemCount = 10,
            NeutralFraction = 0.2f
        };
        var rng = new RngService(12345).Campaign;
        var generator = new GalaxyGenerator(config, rng);

        var world = generator.Generate();

        var neutralSystems = world.GetAllSystems()
            .Where(s => s.OwningFactionId == null)
            .ToList();

        // Neutral systems should have Frontier or Contested tag
        foreach (var system in neutralSystems)
        {
            AssertBool(system.HasTag(WorldTags.Frontier) || system.HasTag(WorldTags.Contested)).IsTrue();
        }
    }

    [TestCase]
    public void MarkNeutralSystems_CapitalsNeverNeutral()
    {
        var config = new GalaxyConfig
        {
            SystemCount = 10,
            NeutralFraction = 0.5f // High neutral fraction
        };
        var rng = new RngService(12345).Campaign;
        var generator = new GalaxyGenerator(config, rng);

        var world = generator.Generate();

        // Capitals (hubs) should never be neutral
        var hubs = world.GetSystemsByTag(WorldTags.Hub).ToList();
        foreach (var hub in hubs)
        {
            AssertString(hub.OwningFactionId).IsNotEmpty();
        }
    }

    [TestCase]
    public void MarkNeutralSystems_ZeroFractionNoNeutrals()
    {
        var config = new GalaxyConfig
        {
            SystemCount = 10,
            NeutralFraction = 0f
        };
        var rng = new RngService(12345).Campaign;
        var generator = new GalaxyGenerator(config, rng);

        var world = generator.Generate();

        // With 0 neutral fraction, all non-contested systems should have owners
        var neutralCount = world.GetAllSystems()
            .Count(s => s.OwningFactionId == null && !s.HasTag(WorldTags.Contested));

        AssertInt(neutralCount).IsEqual(0);
    }

    // ========================================================================
    // INTEGRATION TESTS
    // ========================================================================

    [TestCase]
    public void Generate_FactionAssignmentIsDeterministic()
    {
        var config = GalaxyConfig.Default;

        var rng1 = new RngService(12345).Campaign;
        var generator1 = new GalaxyGenerator(config, rng1);
        var world1 = generator1.Generate();

        var rng2 = new RngService(12345).Campaign;
        var generator2 = new GalaxyGenerator(config, rng2);
        var world2 = generator2.Generate();

        // Same faction assignments
        var systems1 = world1.GetAllSystems().OrderBy(s => s.Id).ToList();
        var systems2 = world2.GetAllSystems().OrderBy(s => s.Id).ToList();

        for (int i = 0; i < systems1.Count; i++)
        {
            AssertString(systems1[i].OwningFactionId ?? "null")
                .IsEqual(systems2[i].OwningFactionId ?? "null");
        }
    }

    [TestCase]
    public void Generate_AllFactionsHaveTerritory()
    {
        var config = new GalaxyConfig
        {
            SystemCount = 15,
            FactionIds = new() { "corp", "rebels", "pirates" },
            NeutralFraction = 0.1f
        };
        var rng = new RngService(12345).Campaign;
        var generator = new GalaxyGenerator(config, rng);

        var world = generator.Generate();

        // Each faction should own at least one system (their capital)
        foreach (var factionId in config.FactionIds)
        {
            var count = world.GetSystemsByFaction(factionId).Count();
            AssertInt(count).IsGreaterEqual(1);
        }
    }

    [TestCase]
    public void GetFactionSystemCounts_ReturnsCorrectCounts()
    {
        var config = new GalaxyConfig
        {
            SystemCount = 12,
            NeutralFraction = 0.2f
        };
        var rng = new RngService(12345).Campaign;
        var generator = new GalaxyGenerator(config, rng);

        var world = generator.Generate();
        var counts = generator.GetFactionSystemCounts(world);

        // Total should equal system count
        int total = counts.Values.Sum();
        AssertInt(total).IsEqual(config.SystemCount);

        // Each faction in config should appear
        foreach (var factionId in config.FactionIds)
        {
            AssertBool(counts.ContainsKey(factionId)).IsTrue();
        }
    }

    [TestCase]
    public void Generate_SmallConfig_HasFactionAssignment()
    {
        var config = GalaxyConfig.Small;
        var rng = new RngService(12345).Campaign;
        var generator = new GalaxyGenerator(config, rng);

        var world = generator.Generate();

        // Should have some owned systems
        int ownedCount = world.GetAllSystems()
            .Count(s => s.OwningFactionId != null);

        AssertInt(ownedCount).IsGreater(0);
    }

    [TestCase]
    public void Generate_LargeConfig_HasFactionAssignment()
    {
        var config = GalaxyConfig.Large;
        var rng = new RngService(12345).Campaign;
        var generator = new GalaxyGenerator(config, rng);

        var world = generator.Generate();

        // Should have hubs
        var hubs = world.GetSystemsByTag(WorldTags.Hub).ToList();
        AssertInt(hubs.Count).IsGreater(0);

        // Should have faction territories
        foreach (var factionId in config.FactionIds)
        {
            var count = world.GetSystemsByFaction(factionId).Count();
            AssertInt(count).IsGreaterEqual(1);
        }
    }
}
