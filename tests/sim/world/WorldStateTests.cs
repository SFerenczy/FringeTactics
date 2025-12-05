using GdUnit4;
using static GdUnit4.Assertions;

namespace FringeTactics.Tests;

[TestSuite]
[RequireGodotRuntime]
public class WorldStateTests
{
    [TestCase]
    public void CreateSingleHub_CreatesValidWorld()
    {
        var world = WorldState.CreateSingleHub("Test Hub", "corp");

        AssertString(world.Name).IsEqual("Outer Reach");
        AssertInt(world.Systems.Count).IsEqual(1);
        AssertInt(world.Stations.Count).IsEqual(1);
        AssertInt(world.Factions.Count).IsEqual(3);
    }

    [TestCase]
    public void CreateSingleHub_SystemHasCorrectProperties()
    {
        var world = WorldState.CreateSingleHub("Test Hub", "corp");
        var system = world.GetSystem(0);

        AssertObject(system).IsNotNull();
        AssertString(system.Name).IsEqual("Test Hub");
        AssertObject(system.Type).IsEqual(SystemType.Station);
        AssertString(system.OwningFactionId).IsEqual("corp");
        AssertBool(system.HasTag(WorldTags.Hub)).IsTrue();
        AssertBool(system.HasTag(WorldTags.Core)).IsTrue();
    }

    [TestCase]
    public void CreateSingleHub_StationHasFacilities()
    {
        var world = WorldState.CreateSingleHub("Test Hub", "corp");
        var station = world.GetPrimaryStation(0);

        AssertObject(station).IsNotNull();
        AssertBool(station.HasFacility(FacilityType.Shop)).IsTrue();
        AssertBool(station.HasFacility(FacilityType.MissionBoard)).IsTrue();
        AssertBool(station.HasFacility(FacilityType.RepairYard)).IsTrue();
        AssertBool(station.HasFacility(FacilityType.Bar)).IsTrue();
        AssertBool(station.HasFacility(FacilityType.Recruitment)).IsTrue();
        AssertBool(station.HasFacility(FacilityType.FuelDepot)).IsTrue();
    }

    [TestCase]
    public void GetSystemMetrics_ReturnsCorrectValues()
    {
        var world = WorldState.CreateSingleHub();
        var metrics = world.GetSystemMetrics(0);

        AssertObject(metrics).IsNotNull();
        AssertInt(metrics.SecurityLevel).IsEqual(4);
        AssertInt(metrics.CriminalActivity).IsEqual(1);
        AssertInt(metrics.EconomicActivity).IsEqual(4);
    }

    [TestCase]
    public void GetSecurityLevel_ReturnsCorrectValue()
    {
        var world = WorldState.CreateSingleHub();

        AssertInt(world.GetSecurityLevel(0)).IsEqual(4);
    }

    [TestCase]
    public void GetFaction_ReturnsCorrectFaction()
    {
        var world = WorldState.CreateSingleHub();
        var faction = world.GetFaction("corp");

        AssertObject(faction).IsNotNull();
        AssertString(faction.Name).IsEqual("Helix Corp");
        AssertObject(faction.Type).IsEqual(FactionType.Corporate);
    }

    [TestCase]
    public void HasTag_ReturnsTrueForExistingTag()
    {
        var world = WorldState.CreateSingleHub();

        AssertBool(world.HasTag(0, WorldTags.Hub)).IsTrue();
        AssertBool(world.HasTag(0, WorldTags.Lawless)).IsFalse();
    }

    [TestCase]
    public void GetStationsInSystem_ReturnsStations()
    {
        var world = WorldState.CreateSingleHub();
        var stations = new System.Collections.Generic.List<Station>(world.GetStationsInSystem(0));

        AssertInt(stations.Count).IsEqual(1);
        AssertString(stations[0].Name).IsEqual("Haven Station");
    }

    [TestCase]
    public void HasFacility_ReturnsTrueForExistingFacility()
    {
        var world = WorldState.CreateSingleHub();
        var station = world.GetPrimaryStation(0);

        AssertBool(world.HasFacility(station.Id, FacilityType.Shop)).IsTrue();
        AssertBool(world.HasFacility(station.Id, FacilityType.BlackMarket)).IsFalse();
    }

    [TestCase]
    public void Serialization_RoundTrip_PreservesData()
    {
        var world = WorldState.CreateSingleHub("Test Hub", "corp");

        var data = world.GetState();
        var restored = WorldState.FromState(data);

        AssertString(restored.Name).IsEqual(world.Name);
        AssertInt(restored.Systems.Count).IsEqual(world.Systems.Count);
        AssertInt(restored.Stations.Count).IsEqual(world.Stations.Count);
        AssertInt(restored.Factions.Count).IsEqual(world.Factions.Count);

        var system = restored.GetSystem(0);
        AssertString(system.Name).IsEqual("Test Hub");
        AssertBool(system.HasTag(WorldTags.Hub)).IsTrue();

        var station = restored.GetPrimaryStation(0);
        AssertBool(station.HasFacility(FacilityType.Shop)).IsTrue();
    }
}

[TestSuite]
[RequireGodotRuntime]
public class SystemMetricsTests
{
    [TestCase]
    public void ForSystemType_Station_ReturnsHighSecurity()
    {
        var metrics = SystemMetrics.ForSystemType(SystemType.Station);

        AssertInt(metrics.SecurityLevel).IsEqual(4);
        AssertInt(metrics.CriminalActivity).IsEqual(1);
    }

    [TestCase]
    public void ForSystemType_Contested_ReturnsHighCrime()
    {
        var metrics = SystemMetrics.ForSystemType(SystemType.Contested);

        AssertInt(metrics.SecurityLevel).IsEqual(1);
        AssertInt(metrics.CriminalActivity).IsEqual(4);
    }

    [TestCase]
    public void Serialization_RoundTrip()
    {
        var metrics = new SystemMetrics
        {
            Stability = 5,
            SecurityLevel = 3,
            CriminalActivity = 2,
            EconomicActivity = 4,
            LawEnforcementPresence = 1
        };

        var data = metrics.GetState();
        var restored = SystemMetrics.FromState(data);

        AssertInt(restored.Stability).IsEqual(5);
        AssertInt(restored.SecurityLevel).IsEqual(3);
        AssertInt(restored.CriminalActivity).IsEqual(2);
        AssertInt(restored.EconomicActivity).IsEqual(4);
        AssertInt(restored.LawEnforcementPresence).IsEqual(1);
    }
}

[TestSuite]
[RequireGodotRuntime]
public class StationTests
{
    [TestCase]
    public void CreateHub_HasAllStandardFacilities()
    {
        var station = Station.CreateHub(0, "Test", 0, "corp");

        AssertInt(station.Facilities.Count).IsEqual(6);
        AssertBool(station.HasFacility(FacilityType.Shop)).IsTrue();
        AssertBool(station.HasFacility(FacilityType.MissionBoard)).IsTrue();
        AssertBool(station.HasFacility(FacilityType.RepairYard)).IsTrue();
    }

    [TestCase]
    public void GetFacility_ReturnsCorrectFacility()
    {
        var station = Station.CreateHub(0, "Test", 0, "corp");
        var shop = station.GetFacility(FacilityType.Shop);

        AssertObject(shop).IsNotNull();
        AssertObject(shop.Type).IsEqual(FacilityType.Shop);
        AssertInt(shop.Level).IsEqual(2);
    }

    [TestCase]
    public void HasFacility_ReturnsFalseWhenUnavailable()
    {
        var station = Station.CreateHub(0, "Test", 0, "corp");
        var shop = station.GetFacility(FacilityType.Shop);
        shop.Available = false;

        AssertBool(station.HasFacility(FacilityType.Shop)).IsFalse();
    }

    [TestCase]
    public void Serialization_RoundTrip()
    {
        var station = Station.CreateHub(0, "Test Station", 0, "corp");

        var data = station.GetState();
        var restored = Station.FromState(data);

        AssertInt(restored.Id).IsEqual(0);
        AssertString(restored.Name).IsEqual("Test Station");
        AssertInt(restored.Facilities.Count).IsEqual(6);
        AssertBool(restored.HasFacility(FacilityType.Shop)).IsTrue();
    }
}

[TestSuite]
[RequireGodotRuntime]
public class FactionTests
{
    [TestCase]
    public void Faction_Serialization_RoundTrip()
    {
        var faction = new Faction("test", "Test Faction", FactionType.Corporate)
        {
            Color = new Godot.Color(0.5f, 0.5f, 0.5f),
            HostilityDefault = 30,
            Metrics = new FactionMetrics
            {
                MilitaryStrength = 4,
                EconomicPower = 5
            }
        };

        var data = faction.GetState();
        var restored = Faction.FromState(data);

        AssertString(restored.Id).IsEqual(faction.Id);
        AssertString(restored.Name).IsEqual(faction.Name);
        AssertObject(restored.Type).IsEqual(faction.Type);
        AssertInt(restored.HostilityDefault).IsEqual(faction.HostilityDefault);
        AssertInt(restored.Metrics.MilitaryStrength).IsEqual(faction.Metrics.MilitaryStrength);
    }
}

[TestSuite]
[RequireGodotRuntime]
public class StarSystemTests
{
    [TestCase]
    public void HasTag_ReturnsTrueForExistingTag()
    {
        var system = new StarSystem(0, "Test", SystemType.Station, new Godot.Vector2(0, 0));
        system.Tags.Add(WorldTags.Hub);

        AssertBool(system.HasTag(WorldTags.Hub)).IsTrue();
        AssertBool(system.HasTag(WorldTags.Lawless)).IsFalse();
    }

    [TestCase]
    public void Serialization_RoundTrip()
    {
        var system = new StarSystem(0, "Test System", SystemType.Outpost, new Godot.Vector2(50, 75))
        {
            OwningFactionId = "rebels",
            Tags = new System.Collections.Generic.HashSet<string> { WorldTags.Frontier }
        };
        system.Connections.Add(1);
        system.StationIds.Add(0);

        var data = system.GetState();
        var restored = StarSystem.FromState(data);

        AssertInt(restored.Id).IsEqual(0);
        AssertString(restored.Name).IsEqual("Test System");
        AssertObject(restored.Type).IsEqual(SystemType.Outpost);
        AssertString(restored.OwningFactionId).IsEqual("rebels");
        AssertBool(restored.HasTag(WorldTags.Frontier)).IsTrue();
        AssertInt(restored.Connections.Count).IsEqual(1);
        AssertInt(restored.StationIds.Count).IsEqual(1);
    }
}

[TestSuite]
[RequireGodotRuntime]
public class CampaignStateWorldIntegrationTests
{
    [TestCase]
    public void CreateNew_InitializesWorldState()
    {
        var campaign = CampaignState.CreateNew(12345);

        AssertObject(campaign.World).IsNotNull();
        AssertInt(campaign.World.Systems.Count).IsEqual(1);
        AssertInt(campaign.World.Stations.Count).IsEqual(1);
    }

    [TestCase]
    public void CampaignState_Serialization_PreservesWorld()
    {
        var campaign = CampaignState.CreateNew(12345);

        var data = campaign.GetState();
        var restored = CampaignState.FromState(data);

        AssertObject(restored.World).IsNotNull();
        AssertInt(restored.World.Systems.Count).IsEqual(campaign.World.Systems.Count);
        AssertInt(restored.World.Stations.Count).IsEqual(campaign.World.Stations.Count);

        var station = restored.World.GetPrimaryStation(0);
        AssertBool(station.HasFacility(FacilityType.Shop)).IsTrue();
    }
}
