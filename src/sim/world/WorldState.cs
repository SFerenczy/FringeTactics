using System.Collections.Generic;
using System.Linq;
using Godot;

namespace FringeTactics;

/// <summary>
/// The canonical representation of the game world.
/// Owns systems, stations, factions, and provides query APIs.
/// </summary>
public class WorldState
{
    public string Name { get; set; } = "Unknown Sector";
    public Dictionary<int, StarSystem> Systems { get; private set; } = new();
    public Dictionary<int, Station> Stations { get; private set; } = new();
    public Dictionary<string, Faction> Factions { get; private set; } = new();

    private int nextStationId = 0;

    // ========== System Queries ==========

    public StarSystem GetSystem(int systemId)
    {
        return Systems.TryGetValue(systemId, out var system) ? system : null;
    }

    public IEnumerable<StarSystem> GetAllSystems()
    {
        return Systems.Values;
    }

    public IEnumerable<StarSystem> GetSystemsByFaction(string factionId)
    {
        return Systems.Values.Where(s => s.OwningFactionId == factionId);
    }

    public IEnumerable<StarSystem> GetSystemsByTag(string tag)
    {
        return Systems.Values.Where(s => s.HasTag(tag));
    }

    public IEnumerable<int> GetNeighbors(int systemId)
    {
        var system = GetSystem(systemId);
        return system?.Connections ?? Enumerable.Empty<int>();
    }

    public SystemMetrics GetSystemMetrics(int systemId)
    {
        return GetSystem(systemId)?.Metrics;
    }

    public int GetSecurityLevel(int systemId)
    {
        return GetSystemMetrics(systemId)?.SecurityLevel ?? 0;
    }

    public int GetCriminalActivity(int systemId)
    {
        return GetSystemMetrics(systemId)?.CriminalActivity ?? 0;
    }

    public bool HasTag(int systemId, string tag)
    {
        return GetSystem(systemId)?.HasTag(tag) ?? false;
    }

    // ========== Station Queries ==========

    public Station GetStation(int stationId)
    {
        return Stations.TryGetValue(stationId, out var station) ? station : null;
    }

    public IEnumerable<Station> GetStationsInSystem(int systemId)
    {
        var system = GetSystem(systemId);
        if (system == null) return Enumerable.Empty<Station>();

        return system.StationIds
            .Select(id => GetStation(id))
            .Where(s => s != null);
    }

    public IEnumerable<Facility> GetFacilities(int stationId)
    {
        return GetStation(stationId)?.Facilities ?? Enumerable.Empty<Facility>();
    }

    public bool HasFacility(int stationId, FacilityType type)
    {
        return GetStation(stationId)?.HasFacility(type) ?? false;
    }

    public Station GetPrimaryStation(int systemId)
    {
        return GetStationsInSystem(systemId).FirstOrDefault();
    }

    // ========== Faction Queries ==========

    public Faction GetFaction(string factionId)
    {
        return Factions.TryGetValue(factionId, out var faction) ? faction : null;
    }

    public IEnumerable<Faction> GetAllFactions()
    {
        return Factions.Values;
    }

    public string GetFactionName(string factionId)
    {
        return GetFaction(factionId)?.Name ?? factionId;
    }

    // ========== Topology Queries (for travel) ==========

    /// <summary>
    /// Check if two systems are connected.
    /// </summary>
    public bool AreConnected(int systemA, int systemB)
    {
        var system = GetSystem(systemA);
        return system?.Connections.Contains(systemB) ?? false;
    }

    /// <summary>
    /// Get travel distance between two systems.
    /// </summary>
    public float GetTravelDistance(int fromId, int toId)
    {
        var from = GetSystem(fromId);
        var to = GetSystem(toId);
        if (from == null || to == null) return float.MaxValue;
        return from.Position.DistanceTo(to.Position);
    }

    /// <summary>
    /// Connect two systems bidirectionally.
    /// </summary>
    public void Connect(int systemA, int systemB)
    {
        var a = GetSystem(systemA);
        var b = GetSystem(systemB);
        if (a != null && b != null)
        {
            if (!a.Connections.Contains(systemB))
                a.Connections.Add(systemB);
            if (!b.Connections.Contains(systemA))
                b.Connections.Add(systemA);
        }
    }

    // ========== Mutation APIs ==========

    public void AddSystem(StarSystem system)
    {
        Systems[system.Id] = system;
    }

    public void AddStation(Station station)
    {
        Stations[station.Id] = station;

        var system = GetSystem(station.SystemId);
        if (system != null && !system.StationIds.Contains(station.Id))
        {
            system.StationIds.Add(station.Id);
        }
    }

    public void AddFaction(Faction faction)
    {
        Factions[faction.Id] = faction;
    }

    public int GenerateStationId()
    {
        return nextStationId++;
    }

    /// <summary>
    /// Clone a faction to avoid sharing mutable state.
    /// </summary>
    private static Faction CloneFaction(Faction source)
    {
        return new Faction(source.Id, source.Name, source.Type)
        {
            Color = source.Color,
            HostilityDefault = source.HostilityDefault,
            Metrics = new FactionMetrics
            {
                MilitaryStrength = source.Metrics.MilitaryStrength,
                EconomicPower = source.Metrics.EconomicPower,
                Influence = source.Metrics.Influence,
                Desperation = source.Metrics.Desperation,
                Corruption = source.Metrics.Corruption
            }
        };
    }

    // ========== Factory Methods ==========

    /// <summary>
    /// Create a minimal single-hub world for G1.
    /// </summary>
    public static WorldState CreateSingleHub(string hubName = "Haven Station", string factionId = "corp")
    {
        var world = new WorldState
        {
            Name = "Outer Reach"
        };

        // Load factions from registry
        foreach (var faction in FactionRegistry.GetAll())
        {
            world.AddFaction(CloneFaction(faction));
        }

        // Create single hub system
        var hubSystem = new StarSystem(0, hubName, SystemType.Station, new Vector2(300, 250))
        {
            OwningFactionId = factionId,
            Tags = new HashSet<string> { WorldTags.Hub, WorldTags.Core }
        };
        hubSystem.Metrics = new SystemMetrics
        {
            Stability = 4,
            SecurityLevel = 4,
            CriminalActivity = 1,
            EconomicActivity = 4,
            LawEnforcementPresence = 4
        };
        world.AddSystem(hubSystem);

        // Create hub station
        var stationId = world.GenerateStationId();
        var station = Station.CreateHub(stationId, hubName, hubSystem.Id, factionId);
        world.AddStation(station);

        return world;
    }

    /// <summary>
    /// Create WorldState from existing Sector (migration helper).
    /// </summary>
    #pragma warning disable CS0618 // Sector/SectorNode are obsolete
    public static WorldState FromSector(Sector sector)
    {
        var world = new WorldState
        {
            Name = sector.Name
        };

        foreach (var kvp in sector.Factions)
        {
            world.AddFaction(new Faction(kvp.Key, kvp.Value, FactionType.Neutral));
        }

        foreach (var node in sector.Nodes)
        {
            var system = StarSystem.FromSectorNode(node);
            world.AddSystem(system);

            if (node.Type == SystemType.Station)
            {
                var stationId = world.GenerateStationId();
                var station = Station.CreateHub(stationId, node.Name, node.Id, node.FactionId);
                world.AddStation(station);
            }
        }

        return world;
    }
    #pragma warning restore CS0618

    // ========== Serialization ==========

    public WorldStateData GetState()
    {
        var data = new WorldStateData
        {
            Name = Name,
            NextStationId = nextStationId
        };

        foreach (var system in Systems.Values)
        {
            data.Systems.Add(system.GetState());
        }

        foreach (var station in Stations.Values)
        {
            data.Stations.Add(station.GetState());
        }

        foreach (var faction in Factions.Values)
        {
            data.Factions.Add(faction.GetState());
        }

        return data;
    }

    public static WorldState FromState(WorldStateData data)
    {
        var world = new WorldState
        {
            Name = data.Name ?? "Unknown Sector",
            nextStationId = data.NextStationId
        };

        foreach (var systemData in data.Systems ?? new List<StarSystemData>())
        {
            world.Systems[systemData.Id] = StarSystem.FromState(systemData);
        }

        foreach (var stationData in data.Stations ?? new List<StationData>())
        {
            world.Stations[stationData.Id] = Station.FromState(stationData);
        }

        foreach (var factionData in data.Factions ?? new List<FactionData>())
        {
            world.Factions[factionData.Id] = Faction.FromState(factionData);
        }

        return world;
    }
}

/// <summary>
/// Serializable data for WorldState.
/// </summary>
public class WorldStateData
{
    public string Name { get; set; }
    public int NextStationId { get; set; }
    public List<StarSystemData> Systems { get; set; } = new();
    public List<StationData> Stations { get; set; } = new();
    public List<FactionData> Factions { get; set; } = new();
}
