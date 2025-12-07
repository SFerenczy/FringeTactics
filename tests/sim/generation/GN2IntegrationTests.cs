using GdUnit4;
using static GdUnit4.Assertions;
using System.Linq;

namespace FringeTactics.Tests;

[TestSuite]
[RequireGodotRuntime]
public class GN2IntegrationTests
{
    // ========================================================================
    // CAMPAIGN CREATION TESTS
    // ========================================================================

    [TestCase]
    public void CampaignCreateNew_UsesGalaxyGenerator()
    {
        var campaign = CampaignState.CreateNew(12345);

        // World should be generated (not null)
        AssertBool(campaign.World != null).IsTrue();

        // Should have systems
        AssertInt(campaign.World.Systems.Count).IsGreater(0);

        // Should have routes
        AssertInt(campaign.World.Routes.Count).IsGreater(0);

        // Should have stations
        AssertInt(campaign.World.Stations.Count).IsGreater(0);
    }

    [TestCase]
    public void CampaignCreateNew_StartsAtHub()
    {
        var campaign = CampaignState.CreateNew(12345);

        var startSystem = campaign.GetCurrentSystem();
        AssertBool(startSystem != null).IsTrue();
        AssertBool(startSystem.HasTag(WorldTags.Hub)).IsTrue();
    }

    [TestCase]
    public void CampaignCreateNew_HasFactionReputation()
    {
        var campaign = CampaignState.CreateNew(12345);

        // Should have rep for all factions in world
        foreach (var factionId in campaign.World.Factions.Keys)
        {
            AssertBool(campaign.FactionRep.ContainsKey(factionId)).IsTrue();
            AssertInt(campaign.FactionRep[factionId]).IsEqual(50); // Neutral
        }
    }

    [TestCase]
    public void CampaignCreateNew_HasStartingCrew()
    {
        var campaign = CampaignState.CreateNew(12345);

        AssertInt(campaign.Crew.Count).IsEqual(4);
    }

    [TestCase]
    public void CampaignCreateNew_HasStartingResources()
    {
        var campaign = CampaignState.CreateNew(12345);

        AssertInt(campaign.Money).IsGreater(0);
        AssertInt(campaign.Fuel).IsGreater(0);
    }

    [TestCase]
    public void CampaignCreateNew_GeneratesInitialJobs()
    {
        var campaign = CampaignState.CreateNew(12345);

        // Should have some available jobs
        AssertInt(campaign.AvailableJobs.Count).IsGreater(0);
    }

    // ========================================================================
    // CUSTOM CONFIG TESTS
    // ========================================================================

    [TestCase]
    public void CampaignCreateNew_WithSmallConfig()
    {
        var campaign = CampaignState.CreateNew(12345, GalaxyConfig.Small);

        AssertInt(campaign.World.Systems.Count).IsEqual(GalaxyConfig.Small.SystemCount);
    }

    [TestCase]
    public void CampaignCreateNew_WithLargeConfig()
    {
        var campaign = CampaignState.CreateNew(12345, GalaxyConfig.Large);

        AssertInt(campaign.World.Systems.Count).IsEqual(GalaxyConfig.Large.SystemCount);
    }

    [TestCase]
    public void CampaignCreateNew_WithCustomConfig()
    {
        var customConfig = new GalaxyConfig
        {
            SystemCount = 15,
            NeutralFraction = 0.3f
        };

        var campaign = CampaignState.CreateNew(12345, customConfig);

        AssertInt(campaign.World.Systems.Count).IsEqual(15);
    }

    // ========================================================================
    // DETERMINISM TESTS
    // ========================================================================

    [TestCase]
    public void CampaignCreateNew_IsDeterministic()
    {
        var campaign1 = CampaignState.CreateNew(12345);
        var campaign2 = CampaignState.CreateNew(12345);

        // Same world name
        AssertString(campaign1.World.Name).IsEqual(campaign2.World.Name);

        // Same number of systems
        AssertInt(campaign1.World.Systems.Count).IsEqual(campaign2.World.Systems.Count);

        // Same system names
        var names1 = campaign1.World.GetAllSystems().OrderBy(s => s.Id).Select(s => s.Name).ToList();
        var names2 = campaign2.World.GetAllSystems().OrderBy(s => s.Id).Select(s => s.Name).ToList();

        for (int i = 0; i < names1.Count; i++)
        {
            AssertString(names1[i]).IsEqual(names2[i]);
        }

        // Same starting location
        AssertInt(campaign1.CurrentNodeId).IsEqual(campaign2.CurrentNodeId);
    }

    [TestCase]
    public void CampaignCreateNew_DifferentSeeds_DifferentWorlds()
    {
        var campaign1 = CampaignState.CreateNew(12345);
        var campaign2 = CampaignState.CreateNew(54321);

        // Should have different world names (very likely)
        // Or at least different system names
        var names1 = campaign1.World.GetAllSystems().Select(s => s.Name).ToHashSet();
        var names2 = campaign2.World.GetAllSystems().Select(s => s.Name).ToHashSet();

        // Not all names should match
        int matching = names1.Intersect(names2).Count();
        AssertInt(matching).IsLess(names1.Count);
    }

    // ========================================================================
    // WORLD INTEGRITY TESTS
    // ========================================================================

    [TestCase]
    public void CampaignCreateNew_AllSystemsConnected()
    {
        var campaign = CampaignState.CreateNew(12345);

        // All systems should be reachable from starting system
        var startId = campaign.CurrentNodeId;
        foreach (var system in campaign.World.GetAllSystems())
        {
            AssertBool(campaign.World.IsReachable(startId, system.Id)).IsTrue();
        }
    }

    [TestCase]
    public void CampaignCreateNew_AllSystemsHaveContent()
    {
        var campaign = CampaignState.CreateNew(12345);

        foreach (var system in campaign.World.GetAllSystems())
        {
            AssertString(system.Name).IsNotEmpty();
            AssertBool(system.Metrics != null).IsTrue();
        }
    }

    [TestCase]
    public void CampaignCreateNew_StationsLinkedCorrectly()
    {
        var campaign = CampaignState.CreateNew(12345);

        foreach (var station in campaign.World.Stations.Values)
        {
            // Station's system should exist
            var system = campaign.World.GetSystem(station.SystemId);
            AssertBool(system != null).IsTrue();

            // System should reference this station
            AssertBool(system.StationIds.Contains(station.Id)).IsTrue();
        }
    }

    [TestCase]
    public void CampaignCreateNew_RoutesHaveValidEndpoints()
    {
        var campaign = CampaignState.CreateNew(12345);

        foreach (var route in campaign.World.GetAllRoutes())
        {
            var systemA = campaign.World.GetSystem(route.SystemA);
            var systemB = campaign.World.GetSystem(route.SystemB);

            AssertBool(systemA != null).IsTrue();
            AssertBool(systemB != null).IsTrue();
        }
    }

    // ========================================================================
    // SAVE/LOAD COMPATIBILITY TESTS
    // ========================================================================

    [TestCase]
    public void GeneratedWorld_CanSerialize()
    {
        var campaign = CampaignState.CreateNew(12345);

        // Get serializable state
        var worldData = campaign.World.GetState();

        AssertBool(worldData != null).IsTrue();
        AssertInt(worldData.Systems.Count).IsEqual(campaign.World.Systems.Count);
        AssertInt(worldData.Routes.Count).IsEqual(campaign.World.Routes.Count);
        AssertInt(worldData.Stations.Count).IsEqual(campaign.World.Stations.Count);
    }

    [TestCase]
    public void GeneratedWorld_CanDeserialize()
    {
        var campaign = CampaignState.CreateNew(12345);

        // Serialize
        var worldData = campaign.World.GetState();

        // Deserialize
        var restoredWorld = WorldState.FromState(worldData);

        // Should match original
        AssertString(restoredWorld.Name).IsEqual(campaign.World.Name);
        AssertInt(restoredWorld.Systems.Count).IsEqual(campaign.World.Systems.Count);
        AssertInt(restoredWorld.Routes.Count).IsEqual(campaign.World.Routes.Count);
        AssertInt(restoredWorld.Stations.Count).IsEqual(campaign.World.Stations.Count);
    }

    [TestCase]
    public void GeneratedWorld_RoundTrip_PreservesData()
    {
        var campaign = CampaignState.CreateNew(12345);

        // Serialize and deserialize
        var worldData = campaign.World.GetState();
        var restoredWorld = WorldState.FromState(worldData);

        // Check systems preserved
        foreach (var original in campaign.World.GetAllSystems())
        {
            var restored = restoredWorld.GetSystem(original.Id);
            AssertBool(restored != null).IsTrue();
            AssertString(restored.Name).IsEqual(original.Name);
            AssertThat(restored.Type).IsEqual(original.Type);
            AssertString(restored.OwningFactionId ?? "null").IsEqual(original.OwningFactionId ?? "null");
        }

        // Check stations preserved
        foreach (var original in campaign.World.Stations.Values)
        {
            var restored = restoredWorld.Stations[original.Id];
            AssertBool(restored != null).IsTrue();
            AssertString(restored.Name).IsEqual(original.Name);
            AssertInt(restored.Facilities.Count).IsEqual(original.Facilities.Count);
        }

        // Check routes preserved
        foreach (var original in campaign.World.GetAllRoutes())
        {
            var restored = restoredWorld.Routes[original.Id];
            AssertBool(restored != null).IsTrue();
            AssertInt(restored.SystemA).IsEqual(original.SystemA);
            AssertInt(restored.SystemB).IsEqual(original.SystemB);
            AssertInt(restored.HazardLevel).IsEqual(original.HazardLevel);
        }
    }

    [TestCase]
    public void GeneratedWorld_RoundTrip_PreservesConnectivity()
    {
        var campaign = CampaignState.CreateNew(12345);

        // Serialize and deserialize
        var worldData = campaign.World.GetState();
        var restoredWorld = WorldState.FromState(worldData);

        // All systems should still be connected after restore
        var startId = campaign.CurrentNodeId;
        foreach (var system in restoredWorld.GetAllSystems())
        {
            AssertBool(restoredWorld.IsReachable(startId, system.Id)).IsTrue();
        }
    }
}
