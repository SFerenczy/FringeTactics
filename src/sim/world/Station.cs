using System.Collections.Generic;
using System.Linq;

namespace FringeTactics;

/// <summary>
/// A station within a star system.
/// Stations have facilities that provide services.
/// </summary>
public class Station
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int SystemId { get; set; }
    public string OwningFactionId { get; set; }
    public List<Facility> Facilities { get; set; } = new();
    public HashSet<string> Tags { get; set; } = new();

    public Station(int id, string name, int systemId)
    {
        Id = id;
        Name = name;
        SystemId = systemId;
    }

    public bool HasFacility(FacilityType type)
    {
        return Facilities.Any(f => f.Type == type && f.Available);
    }

    public Facility GetFacility(FacilityType type)
    {
        return Facilities.FirstOrDefault(f => f.Type == type);
    }

    public IEnumerable<Facility> GetAvailableFacilities()
    {
        return Facilities.Where(f => f.Available);
    }

    public void AddFacility(FacilityType type, int level = 1)
    {
        if (!Facilities.Any(f => f.Type == type))
        {
            Facilities.Add(new Facility(type, level));
        }
    }

    public bool HasTag(string tag)
    {
        return Tags.Contains(tag);
    }

    /// <summary>
    /// Create a standard hub station with common facilities.
    /// </summary>
    public static Station CreateHub(int id, string name, int systemId, string factionId)
    {
        var station = new Station(id, name, systemId)
        {
            OwningFactionId = factionId,
            Tags = new HashSet<string> { WorldTags.Hub, WorldTags.TradeHub }
        };

        station.AddFacility(FacilityType.Shop, 2);
        station.AddFacility(FacilityType.MissionBoard, 2);
        station.AddFacility(FacilityType.RepairYard, 1);
        station.AddFacility(FacilityType.Bar, 1);
        station.AddFacility(FacilityType.Recruitment, 1);
        station.AddFacility(FacilityType.FuelDepot, 1);

        return station;
    }

    /// <summary>
    /// Create a minor outpost with basic facilities.
    /// </summary>
    public static Station CreateOutpost(int id, string name, int systemId, string factionId)
    {
        var station = new Station(id, name, systemId)
        {
            OwningFactionId = factionId,
            Tags = new HashSet<string> { WorldTags.Frontier }
        };

        station.AddFacility(FacilityType.Shop, 1);
        station.AddFacility(FacilityType.MissionBoard, 1);
        station.AddFacility(FacilityType.FuelDepot, 1);

        return station;
    }

    /// <summary>
    /// Create a mining station with repair focus.
    /// </summary>
    public static Station CreateMining(int id, string name, int systemId, string factionId)
    {
        var station = new Station(id, name, systemId)
        {
            OwningFactionId = factionId,
            Tags = new HashSet<string> { WorldTags.Industrial }
        };

        station.AddFacility(FacilityType.Shop, 1);
        station.AddFacility(FacilityType.MissionBoard, 1);
        station.AddFacility(FacilityType.RepairYard, 2);
        station.AddFacility(FacilityType.FuelDepot, 2);

        return station;
    }

    /// <summary>
    /// Create a pirate den with black market.
    /// </summary>
    public static Station CreatePirateDen(int id, string name, int systemId, string factionId)
    {
        var station = new Station(id, name, systemId)
        {
            OwningFactionId = factionId,
            Tags = new HashSet<string> { WorldTags.BlackMarket }
        };

        station.AddFacility(FacilityType.Bar, 2);
        station.AddFacility(FacilityType.BlackMarket, 2);
        station.AddFacility(FacilityType.Recruitment, 1);
        station.AddFacility(FacilityType.RepairYard, 1);

        return station;
    }

    /// <summary>
    /// Create a military station with medical focus.
    /// </summary>
    public static Station CreateMilitary(int id, string name, int systemId, string factionId)
    {
        var station = new Station(id, name, systemId)
        {
            OwningFactionId = factionId,
            Tags = new HashSet<string> { WorldTags.Military }
        };

        station.AddFacility(FacilityType.MissionBoard, 2);
        station.AddFacility(FacilityType.RepairYard, 2);
        station.AddFacility(FacilityType.Medical, 2);
        station.AddFacility(FacilityType.FuelDepot, 2);

        return station;
    }

    /// <summary>
    /// Create a black market station.
    /// </summary>
    public static Station CreateBlackMarket(int id, string name, int systemId, string factionId)
    {
        var station = new Station(id, name, systemId)
        {
            OwningFactionId = factionId,
            Tags = new HashSet<string> { WorldTags.BlackMarket }
        };

        station.AddFacility(FacilityType.Bar, 1);
        station.AddFacility(FacilityType.BlackMarket, 3);
        station.AddFacility(FacilityType.Recruitment, 1);

        return station;
    }

    public StationData GetState()
    {
        var data = new StationData
        {
            Id = Id,
            Name = Name,
            SystemId = SystemId,
            OwningFactionId = OwningFactionId,
            Tags = new List<string>(Tags)
        };

        foreach (var facility in Facilities)
        {
            data.Facilities.Add(facility.GetState());
        }

        return data;
    }

    public static Station FromState(StationData data)
    {
        var station = new Station(data.Id, data.Name, data.SystemId)
        {
            OwningFactionId = data.OwningFactionId,
            Tags = new HashSet<string>(data.Tags ?? new List<string>())
        };

        foreach (var facilityData in data.Facilities ?? new List<FacilityData>())
        {
            station.Facilities.Add(Facility.FromState(facilityData));
        }

        return station;
    }
}

/// <summary>
/// Serializable data for Station.
/// </summary>
public class StationData
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int SystemId { get; set; }
    public string OwningFactionId { get; set; }
    public List<string> Tags { get; set; } = new();
    public List<FacilityData> Facilities { get; set; } = new();
}
