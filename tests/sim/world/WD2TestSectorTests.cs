using GdUnit4;
using static GdUnit4.Assertions;
using System.Linq;

namespace FringeTactics.Tests;

[TestSuite]
[RequireGodotRuntime]
public class WD2TestSectorTests
{
    private WorldState world;

    [Before]
    public void Setup()
    {
        world = WorldState.CreateTestSector();
    }

    // ========== Structure Tests ==========

    [TestCase]
    public void CreateTestSector_Creates8Systems()
    {
        AssertInt(world.Systems.Count).IsEqual(8);
    }

    [TestCase]
    public void CreateTestSector_Creates7Routes()
    {
        AssertInt(world.Routes.Count).IsEqual(7);
    }

    [TestCase]
    public void CreateTestSector_Creates6Stations()
    {
        AssertInt(world.Stations.Count).IsEqual(6);
    }

    [TestCase]
    public void CreateTestSector_Creates3Factions()
    {
        AssertInt(world.Factions.Count).IsEqual(3);
    }

    // ========== System Configuration Tests ==========

    [TestCase]
    public void TestSector_HavenStation_IsCorrectlyConfigured()
    {
        var haven = world.GetSystem(0);

        AssertString(haven.Name).IsEqual("Haven Station");
        AssertObject(haven.Type).IsEqual(SystemType.Station);
        AssertString(haven.OwningFactionId).IsEqual("corp");
        AssertBool(haven.HasTag(WorldTags.Core)).IsTrue();
        AssertBool(haven.HasTag(WorldTags.Hub)).IsTrue();
    }

    [TestCase]
    public void TestSector_WaypointAlpha_IsFrontierOutpost()
    {
        var waypoint = world.GetSystem(1);

        AssertString(waypoint.Name).IsEqual("Waypoint Alpha");
        AssertObject(waypoint.Type).IsEqual(SystemType.Outpost);
        AssertString(waypoint.OwningFactionId).IsEqual("corp");
        AssertBool(waypoint.HasTag(WorldTags.Frontier)).IsTrue();
    }

    [TestCase]
    public void TestSector_RockfallMining_IsMiningSystem()
    {
        var rockfall = world.GetSystem(2);

        AssertString(rockfall.Name).IsEqual("Rockfall Mining");
        AssertObject(rockfall.Type).IsEqual(SystemType.Asteroid);
        AssertBool(rockfall.HasTag(WorldTags.Mining)).IsTrue();
        AssertBool(rockfall.HasTag(WorldTags.Industrial)).IsTrue();
    }

    [TestCase]
    public void TestSector_RedClawBase_IsPirateControlled()
    {
        var redClaw = world.GetSystem(3);

        AssertString(redClaw.OwningFactionId).IsEqual("pirates");
        AssertBool(redClaw.HasTag(WorldTags.Lawless)).IsTrue();
        AssertInt(redClaw.Metrics.CriminalActivity).IsEqual(5);
        AssertInt(redClaw.Metrics.SecurityLevel).IsEqual(0);
    }

    [TestCase]
    public void TestSector_ContestedZone_IsNeutral()
    {
        var contested = world.GetSystem(4);

        AssertObject(contested.Type).IsEqual(SystemType.Contested);
        AssertObject(contested.OwningFactionId).IsNull();
        AssertBool(contested.HasTag(WorldTags.Border)).IsTrue();
    }

    [TestCase]
    public void TestSector_PatrolStation_HasHighSecurity()
    {
        var patrol = world.GetSystem(5);

        AssertBool(patrol.HasTag(WorldTags.Military)).IsTrue();
        AssertInt(patrol.Metrics.SecurityLevel).IsEqual(5);
        AssertInt(patrol.Metrics.LawEnforcementPresence).IsEqual(5);
        AssertInt(patrol.Metrics.CriminalActivity).IsEqual(0);
    }

    [TestCase]
    public void TestSector_SmugglersDen_IsLawless()
    {
        var smuggler = world.GetSystem(6);

        AssertObject(smuggler.Type).IsEqual(SystemType.Nebula);
        AssertObject(smuggler.OwningFactionId).IsNull();
        AssertBool(smuggler.HasTag(WorldTags.Lawless)).IsTrue();
    }

    [TestCase]
    public void TestSector_WreckOfIcarus_IsDerelict()
    {
        var wreck = world.GetSystem(7);

        AssertObject(wreck.Type).IsEqual(SystemType.Derelict);
        AssertObject(wreck.OwningFactionId).IsNull();
    }

    // ========== Station Tests ==========

    [TestCase]
    public void TestSector_HavenStation_HasAllHubFacilities()
    {
        var station = world.GetPrimaryStation(0);

        AssertBool(station.HasFacility(FacilityType.Shop)).IsTrue();
        AssertBool(station.HasFacility(FacilityType.MissionBoard)).IsTrue();
        AssertBool(station.HasFacility(FacilityType.RepairYard)).IsTrue();
        AssertBool(station.HasFacility(FacilityType.Bar)).IsTrue();
        AssertBool(station.HasFacility(FacilityType.Recruitment)).IsTrue();
        AssertBool(station.HasFacility(FacilityType.FuelDepot)).IsTrue();
    }

    [TestCase]
    public void TestSector_WaypointStation_HasOutpostFacilities()
    {
        var station = world.GetPrimaryStation(1);

        AssertBool(station.HasFacility(FacilityType.Shop)).IsTrue();
        AssertBool(station.HasFacility(FacilityType.MissionBoard)).IsTrue();
        AssertBool(station.HasFacility(FacilityType.FuelDepot)).IsTrue();
        AssertBool(station.HasFacility(FacilityType.Bar)).IsFalse();
    }

    [TestCase]
    public void TestSector_PirateStation_HasBlackMarket()
    {
        var station = world.GetPrimaryStation(3);

        AssertBool(station.HasFacility(FacilityType.BlackMarket)).IsTrue();
        AssertBool(station.HasFacility(FacilityType.Bar)).IsTrue();
        AssertBool(station.HasFacility(FacilityType.Shop)).IsFalse();
    }

    [TestCase]
    public void TestSector_MilitaryStation_HasMedical()
    {
        var station = world.GetPrimaryStation(5);

        AssertBool(station.HasFacility(FacilityType.Medical)).IsTrue();
        AssertBool(station.HasFacility(FacilityType.RepairYard)).IsTrue();
        AssertBool(station.HasFacility(FacilityType.MissionBoard)).IsTrue();
    }

    [TestCase]
    public void TestSector_SmugglerStation_HasHighLevelBlackMarket()
    {
        var station = world.GetPrimaryStation(6);
        var blackMarket = station.GetFacility(FacilityType.BlackMarket);

        AssertObject(blackMarket).IsNotNull();
        AssertInt(blackMarket.Level).IsEqual(3);
    }

    [TestCase]
    public void TestSector_SystemsWithoutStations()
    {
        // Contested Zone (4) and Wreck (7) have no stations
        var contestedStations = world.GetStationsInSystem(4).ToList();
        var wreckStations = world.GetStationsInSystem(7).ToList();

        AssertInt(contestedStations.Count).IsEqual(0);
        AssertInt(wreckStations.Count).IsEqual(0);
    }

    // ========== Route Tests ==========

    [TestCase]
    public void TestSector_HavenWaypointRoute_IsPatrolled()
    {
        var route = world.GetRoute(0, 1);

        AssertObject(route).IsNotNull();
        AssertBool(route.HasTag(WorldTags.Patrolled)).IsTrue();
        AssertInt(route.HazardLevel).IsEqual(1);
    }

    [TestCase]
    public void TestSector_WaypointContestedRoute_IsDangerous()
    {
        var route = world.GetRoute(1, 4);

        AssertObject(route).IsNotNull();
        AssertBool(route.HasTag(WorldTags.Dangerous)).IsTrue();
        AssertInt(route.HazardLevel).IsEqual(3);
    }

    [TestCase]
    public void TestSector_HiddenRoutes_ConnectToSmuggler()
    {
        var waypointSmuggler = world.GetRoute(1, 6);
        var pirateSmuggler = world.GetRoute(3, 6);

        AssertBool(waypointSmuggler.HasTag(WorldTags.Hidden)).IsTrue();
        AssertBool(pirateSmuggler.HasTag(WorldTags.Hidden)).IsTrue();
    }

    [TestCase]
    public void TestSector_AsteroidRoute_HasAsteroidTag()
    {
        var route = world.GetRoute(1, 2);

        AssertBool(route.HasTag(WorldTags.Asteroid)).IsTrue();
    }

    // ========== Faction Territory Tests ==========

    [TestCase]
    public void TestSector_CorpControlsFourSystems()
    {
        var corpSystems = world.GetSystemsByFaction("corp").ToList();

        // Corp: Haven (0), Waypoint (1), Rockfall (2), Patrol (5)
        AssertInt(corpSystems.Count).IsEqual(4);
    }

    [TestCase]
    public void TestSector_PiratesControlOneSystem()
    {
        var pirateSystems = world.GetSystemsByFaction("pirates").ToList();

        // Pirates: Red Claw (3)
        AssertInt(pirateSystems.Count).IsEqual(1);
    }

    [TestCase]
    public void TestSector_NeutralSystemsExist()
    {
        var neutralSystems = world.GetSystemsByFaction(null).ToList();

        // Neutral: Contested (4), Smuggler (6), Wreck (7)
        AssertInt(neutralSystems.Count).IsEqual(3);
    }

    // ========== Connectivity Tests ==========

    [TestCase]
    public void TestSector_WaypointIsHub_FourConnections()
    {
        var routes = world.GetRoutesFrom(1).ToList();

        // Waypoint connects to: Haven (0), Rockfall (2), Contested (4), Smuggler (6)
        AssertInt(routes.Count).IsEqual(4);
    }

    [TestCase]
    public void TestSector_AllSystemsReachableFromHaven()
    {
        for (int i = 1; i < 8; i++)
        {
            AssertBool(world.IsReachable(0, i)).IsTrue();
        }
    }

    [TestCase]
    public void TestSector_PathFromHavenToPirates()
    {
        var path = world.FindPath(0, 3);

        // Haven -> Waypoint -> Smuggler -> Red Claw
        AssertInt(path.Count).IsEqual(4);
        AssertInt(path[0]).IsEqual(0);
        AssertInt(path[3]).IsEqual(3);
    }

    // ========== Serialization Tests ==========

    [TestCase]
    public void TestSector_Serialization_PreservesAllData()
    {
        var data = world.GetState();
        var restored = WorldState.FromState(data);

        AssertInt(restored.Systems.Count).IsEqual(8);
        AssertInt(restored.Routes.Count).IsEqual(7);
        AssertInt(restored.Stations.Count).IsEqual(6);
        AssertInt(restored.Factions.Count).IsEqual(3);
    }

    [TestCase]
    public void TestSector_Serialization_PreservesSystemMetrics()
    {
        var data = world.GetState();
        var restored = WorldState.FromState(data);

        var patrol = restored.GetSystem(5);
        AssertInt(patrol.Metrics.SecurityLevel).IsEqual(5);
        AssertInt(patrol.Metrics.CriminalActivity).IsEqual(0);
    }

    [TestCase]
    public void TestSector_Serialization_PreservesRouteProperties()
    {
        var data = world.GetState();
        var restored = WorldState.FromState(data);

        var dangerousRoute = restored.GetRoute(1, 4);
        AssertInt(dangerousRoute.HazardLevel).IsEqual(3);
        AssertBool(dangerousRoute.HasTag(WorldTags.Dangerous)).IsTrue();
    }

    [TestCase]
    public void TestSector_Serialization_PreservesStationFacilities()
    {
        var data = world.GetState();
        var restored = WorldState.FromState(data);

        var pirateStation = restored.GetPrimaryStation(3);
        AssertBool(pirateStation.HasFacility(FacilityType.BlackMarket)).IsTrue();
    }
}

[TestSuite]
public class WD2StationFactoryTests
{
    [TestCase]
    public void CreateOutpost_HasBasicFacilities()
    {
        var station = Station.CreateOutpost(0, "Test", 0, "corp");

        AssertInt(station.Facilities.Count).IsEqual(3);
        AssertBool(station.HasFacility(FacilityType.Shop)).IsTrue();
        AssertBool(station.HasFacility(FacilityType.MissionBoard)).IsTrue();
        AssertBool(station.HasFacility(FacilityType.FuelDepot)).IsTrue();
    }

    [TestCase]
    public void CreateMining_HasRepairFocus()
    {
        var station = Station.CreateMining(0, "Test", 0, "corp");

        AssertBool(station.HasFacility(FacilityType.RepairYard)).IsTrue();
        var repairYard = station.GetFacility(FacilityType.RepairYard);
        AssertInt(repairYard.Level).IsEqual(2);
    }

    [TestCase]
    public void CreatePirateDen_HasBlackMarket()
    {
        var station = Station.CreatePirateDen(0, "Test", 0, "pirates");

        AssertBool(station.HasFacility(FacilityType.BlackMarket)).IsTrue();
        AssertBool(station.HasFacility(FacilityType.Bar)).IsTrue();
        AssertBool(station.HasFacility(FacilityType.Shop)).IsFalse();
    }

    [TestCase]
    public void CreateMilitary_HasMedical()
    {
        var station = Station.CreateMilitary(0, "Test", 0, "corp");

        AssertBool(station.HasFacility(FacilityType.Medical)).IsTrue();
        var medical = station.GetFacility(FacilityType.Medical);
        AssertInt(medical.Level).IsEqual(2);
    }

    [TestCase]
    public void CreateBlackMarket_HasHighLevelBlackMarket()
    {
        var station = Station.CreateBlackMarket(0, "Test", 0, null);

        AssertBool(station.HasFacility(FacilityType.BlackMarket)).IsTrue();
        var blackMarket = station.GetFacility(FacilityType.BlackMarket);
        AssertInt(blackMarket.Level).IsEqual(3);
    }

    [TestCase]
    public void StationFactories_SetCorrectTags()
    {
        var outpost = Station.CreateOutpost(0, "Test", 0, "corp");
        var mining = Station.CreateMining(0, "Test", 0, "corp");
        var military = Station.CreateMilitary(0, "Test", 0, "corp");

        AssertBool(outpost.Tags.Contains(WorldTags.Frontier)).IsTrue();
        AssertBool(mining.Tags.Contains(WorldTags.Industrial)).IsTrue();
        AssertBool(military.Tags.Contains(WorldTags.Military)).IsTrue();
    }
}
