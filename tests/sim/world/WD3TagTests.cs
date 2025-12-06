using System.Linq;
using GdUnit4;
using static GdUnit4.Assertions;

namespace FringeTactics.Tests;

[TestSuite]
public class WD3TagTests
{
    // ========== WorldTags Category Tests ==========

    [TestCase]
    public void WorldTags_SystemTags_Has13Tags()
    {
        AssertInt(WorldTags.SystemTags.Count).IsEqual(13);
    }

    [TestCase]
    public void WorldTags_StationTags_Has8Tags()
    {
        AssertInt(WorldTags.StationTags.Count).IsEqual(8);
    }

    [TestCase]
    public void WorldTags_RouteTags_Has8Tags()
    {
        AssertInt(WorldTags.RouteTags.Count).IsEqual(8);
    }

    [TestCase]
    public void WorldTags_IsSystemTag_RecognizesSystemTags()
    {
        AssertBool(WorldTags.IsSystemTag(WorldTags.Core)).IsTrue();
        AssertBool(WorldTags.IsSystemTag(WorldTags.Frontier)).IsTrue();
        AssertBool(WorldTags.IsSystemTag(WorldTags.Lawless)).IsTrue();
        AssertBool(WorldTags.IsSystemTag(WorldTags.Hub)).IsTrue();
        AssertBool(WorldTags.IsSystemTag(WorldTags.PirateHaven)).IsTrue();
    }

    [TestCase]
    public void WorldTags_IsSystemTag_RejectsNonSystemTags()
    {
        AssertBool(WorldTags.IsSystemTag(WorldTags.Dangerous)).IsFalse();
        AssertBool(WorldTags.IsSystemTag(WorldTags.BlackMarket)).IsFalse();
        AssertBool(WorldTags.IsSystemTag(WorldTags.Patrolled)).IsFalse();
    }

    [TestCase]
    public void WorldTags_IsStationTag_RecognizesStationTags()
    {
        AssertBool(WorldTags.IsStationTag(WorldTags.TradeHub)).IsTrue();
        AssertBool(WorldTags.IsStationTag(WorldTags.BlackMarket)).IsTrue();
        AssertBool(WorldTags.IsStationTag(WorldTags.Shipyard)).IsTrue();
    }

    [TestCase]
    public void WorldTags_IsStationTag_RejectsNonStationTags()
    {
        AssertBool(WorldTags.IsStationTag(WorldTags.Core)).IsFalse();
        AssertBool(WorldTags.IsStationTag(WorldTags.Dangerous)).IsFalse();
        AssertBool(WorldTags.IsStationTag(WorldTags.Border)).IsFalse();
    }

    [TestCase]
    public void WorldTags_IsStationTag_AcceptsSharedTags()
    {
        // Shared tags are valid for both systems and stations
        AssertBool(WorldTags.IsStationTag(WorldTags.Hub)).IsTrue();
        AssertBool(WorldTags.IsStationTag(WorldTags.Frontier)).IsTrue();
        AssertBool(WorldTags.IsStationTag(WorldTags.Industrial)).IsTrue();
        AssertBool(WorldTags.IsStationTag(WorldTags.Military)).IsTrue();
        AssertBool(WorldTags.IsStationTag(WorldTags.Lawless)).IsTrue();
    }

    [TestCase]
    public void WorldTags_SharedTags_Has5Tags()
    {
        AssertInt(WorldTags.SharedTags.Count).IsEqual(5);
    }

    [TestCase]
    public void WorldTags_IsSharedTag_RecognizesSharedTags()
    {
        AssertBool(WorldTags.IsSharedTag(WorldTags.Hub)).IsTrue();
        AssertBool(WorldTags.IsSharedTag(WorldTags.Frontier)).IsTrue();
        AssertBool(WorldTags.IsSharedTag(WorldTags.Core)).IsFalse();
        AssertBool(WorldTags.IsSharedTag(WorldTags.TradeHub)).IsFalse();
    }

    [TestCase]
    public void WorldTags_IsRouteTag_RecognizesRouteTags()
    {
        AssertBool(WorldTags.IsRouteTag(WorldTags.Dangerous)).IsTrue();
        AssertBool(WorldTags.IsRouteTag(WorldTags.Patrolled)).IsTrue();
        AssertBool(WorldTags.IsRouteTag(WorldTags.Hidden)).IsTrue();
        AssertBool(WorldTags.IsRouteTag(WorldTags.Unstable)).IsTrue();
    }

    [TestCase]
    public void WorldTags_IsRouteTag_RejectsNonRouteTags()
    {
        AssertBool(WorldTags.IsRouteTag(WorldTags.Core)).IsFalse();
        AssertBool(WorldTags.IsRouteTag(WorldTags.BlackMarket)).IsFalse();
    }

    // ========== WorldState Tag Query Tests ==========

    [TestCase]
    public void WorldState_GetSystemsWithAllTags_RequiresAllTags()
    {
        var world = WorldState.CreateTestSector();

        var coreHubs = world.GetSystemsWithAllTags(WorldTags.Core, WorldTags.Hub).ToList();

        AssertInt(coreHubs.Count).IsEqual(1);
        AssertString(coreHubs[0].Name).IsEqual("Haven Station");
    }

    [TestCase]
    public void WorldState_GetSystemsWithAllTags_ReturnsEmptyIfNoMatch()
    {
        var world = WorldState.CreateTestSector();

        var impossible = world.GetSystemsWithAllTags(WorldTags.Core, WorldTags.Lawless).ToList();

        AssertInt(impossible.Count).IsEqual(0);
    }

    [TestCase]
    public void WorldState_GetSystemsWithAnyTag_MatchesAnyTag()
    {
        var world = WorldState.CreateTestSector();

        var lawlessOrMilitary = world.GetSystemsWithAnyTag(
            WorldTags.Lawless, WorldTags.Military).ToList();

        AssertInt(lawlessOrMilitary.Count).IsGreaterEqual(3);
    }

    [TestCase]
    public void WorldState_GetSystemsWithAnyTag_ReturnsEmptyIfNoMatch()
    {
        var world = WorldState.CreateTestSector();

        var quarantined = world.GetSystemsWithAnyTag(WorldTags.Quarantined).ToList();

        AssertInt(quarantined.Count).IsEqual(0);
    }

    [TestCase]
    public void WorldState_GetStationsByTag_FiltersCorrectly()
    {
        var world = WorldState.CreateTestSector();

        var blackMarkets = world.GetStationsByTag(WorldTags.BlackMarket).ToList();

        AssertInt(blackMarkets.Count).IsGreaterEqual(1);
        AssertBool(blackMarkets.All(s => s.HasTag(WorldTags.BlackMarket))).IsTrue();
    }

    [TestCase]
    public void WorldState_GetStationsWithAllTags_RequiresAllTags()
    {
        var world = WorldState.CreateTestSector();

        // Haven Station has both trade_hub and recruitment
        var result = world.GetStationsWithAllTags(WorldTags.TradeHub).ToList();

        AssertInt(result.Count).IsGreaterEqual(1);
    }

    [TestCase]
    public void WorldState_GetStationsWithAnyTag_MatchesAnyTag()
    {
        var world = WorldState.CreateTestSector();

        var result = world.GetStationsWithAnyTag(
            WorldTags.BlackMarket, WorldTags.TradeHub).ToList();

        AssertInt(result.Count).IsGreaterEqual(2);
    }

    // ========== WorldState Tag Mutation Tests ==========

    [TestCase]
    public void WorldState_AddSystemTag_AddsTag()
    {
        var world = WorldState.CreateTestSector();

        bool added = world.AddSystemTag(1, WorldTags.Quarantined);

        AssertBool(added).IsTrue();
        AssertBool(world.HasTag(1, WorldTags.Quarantined)).IsTrue();
    }

    [TestCase]
    public void WorldState_AddSystemTag_ReturnsFalseIfAlreadyHasTag()
    {
        var world = WorldState.CreateTestSector();

        bool added = world.AddSystemTag(0, WorldTags.Core);

        AssertBool(added).IsFalse();
    }

    [TestCase]
    public void WorldState_AddSystemTag_ReturnsFalseForInvalidSystem()
    {
        var world = WorldState.CreateTestSector();

        bool added = world.AddSystemTag(999, WorldTags.Quarantined);

        AssertBool(added).IsFalse();
    }

    [TestCase]
    public void WorldState_RemoveSystemTag_RemovesTag()
    {
        var world = WorldState.CreateTestSector();

        bool removed = world.RemoveSystemTag(0, WorldTags.Hub);

        AssertBool(removed).IsTrue();
        AssertBool(world.HasTag(0, WorldTags.Hub)).IsFalse();
    }

    [TestCase]
    public void WorldState_RemoveSystemTag_ReturnsFalseIfTagNotPresent()
    {
        var world = WorldState.CreateTestSector();

        bool removed = world.RemoveSystemTag(0, WorldTags.Lawless);

        AssertBool(removed).IsFalse();
    }

    [TestCase]
    public void WorldState_RemoveSystemTag_ReturnsFalseForInvalidSystem()
    {
        var world = WorldState.CreateTestSector();

        bool removed = world.RemoveSystemTag(999, WorldTags.Core);

        AssertBool(removed).IsFalse();
    }

    [TestCase]
    public void WorldState_AddStationTag_AddsTag()
    {
        var world = WorldState.CreateTestSector();

        bool added = world.AddStationTag(0, WorldTags.Shipyard);

        AssertBool(added).IsTrue();
        AssertBool(world.GetStation(0).HasTag(WorldTags.Shipyard)).IsTrue();
    }

    [TestCase]
    public void WorldState_AddStationTag_ReturnsFalseForInvalidStation()
    {
        var world = WorldState.CreateTestSector();

        bool added = world.AddStationTag(999, WorldTags.Shipyard);

        AssertBool(added).IsFalse();
    }

    [TestCase]
    public void WorldState_RemoveStationTag_RemovesTag()
    {
        var world = WorldState.CreateTestSector();
        var station = world.GetStation(0);
        string existingTag = station.Tags.First();

        bool removed = world.RemoveStationTag(0, existingTag);

        AssertBool(removed).IsTrue();
        AssertBool(station.HasTag(existingTag)).IsFalse();
    }

    [TestCase]
    public void WorldState_AddRouteTag_AddsTag()
    {
        var world = WorldState.CreateTestSector();

        bool added = world.AddRouteTag(0, 1, WorldTags.Blockaded);

        AssertBool(added).IsTrue();
        var route = world.GetRoute(0, 1);
        AssertBool(route.HasTag(WorldTags.Blockaded)).IsTrue();
    }

    [TestCase]
    public void WorldState_AddRouteTag_ReturnsFalseIfAlreadyHasTag()
    {
        var world = WorldState.CreateTestSector();

        bool added = world.AddRouteTag(0, 1, WorldTags.Patrolled);

        AssertBool(added).IsFalse();
    }

    [TestCase]
    public void WorldState_AddRouteTag_ReturnsFalseForInvalidRoute()
    {
        var world = WorldState.CreateTestSector();

        bool added = world.AddRouteTag(0, 999, WorldTags.Blockaded);

        AssertBool(added).IsFalse();
    }

    [TestCase]
    public void WorldState_RemoveRouteTag_RemovesTag()
    {
        var world = WorldState.CreateTestSector();

        bool removed = world.RemoveRouteTag(0, 1, WorldTags.Patrolled);

        AssertBool(removed).IsTrue();
        var route = world.GetRoute(0, 1);
        AssertBool(route.HasTag(WorldTags.Patrolled)).IsFalse();
    }

    [TestCase]
    public void WorldState_RemoveRouteTag_ReturnsFalseIfTagNotPresent()
    {
        var world = WorldState.CreateTestSector();

        bool removed = world.RemoveRouteTag(0, 1, WorldTags.Blockaded);

        AssertBool(removed).IsFalse();
    }

    [TestCase]
    public void WorldState_RemoveRouteTag_ReturnsFalseForInvalidRoute()
    {
        var world = WorldState.CreateTestSector();

        bool removed = world.RemoveRouteTag(0, 999, WorldTags.Patrolled);

        AssertBool(removed).IsFalse();
    }
}
