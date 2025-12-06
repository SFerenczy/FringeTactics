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
    public Dictionary<int, Route> Routes { get; private set; } = new();

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

    public bool HasTag(int systemId, string tag)
    {
        return GetSystem(systemId)?.HasTag(tag) ?? false;
    }

    // ========== Metric Queries (WD3) ==========

    /// <summary>
    /// Get a specific metric value for a system.
    /// </summary>
    public int GetSystemMetric(int systemId, SystemMetricType metric)
    {
        return GetSystemMetrics(systemId)?.Get(metric) ?? 0;
    }

    /// <summary>
    /// Get systems where a metric meets a threshold.
    /// </summary>
    public IEnumerable<StarSystem> GetSystemsByMetric(
        SystemMetricType metric,
        int minValue = 0,
        int maxValue = 5)
    {
        return Systems.Values.Where(s =>
        {
            int value = s.Metrics?.Get(metric) ?? 0;
            return value >= minValue && value <= maxValue;
        });
    }

    /// <summary>
    /// Get high-security systems (SecurityLevel >= threshold).
    /// </summary>
    public IEnumerable<StarSystem> GetHighSecuritySystems(int minLevel = 4)
    {
        return GetSystemsByMetric(SystemMetricType.SecurityLevel, minLevel, 5);
    }

    /// <summary>
    /// Get lawless systems (SecurityLevel <= threshold).
    /// </summary>
    public IEnumerable<StarSystem> GetLawlessSystems(int maxSecurity = 1)
    {
        return GetSystemsByMetric(SystemMetricType.SecurityLevel, 0, maxSecurity);
    }

    /// <summary>
    /// Get systems with high criminal activity.
    /// </summary>
    public IEnumerable<StarSystem> GetHighCrimeSystems(int minCrime = 4)
    {
        return GetSystemsByMetric(SystemMetricType.CriminalActivity, minCrime, 5);
    }

    /// <summary>
    /// Get economically active systems.
    /// </summary>
    public IEnumerable<StarSystem> GetProsperousSystems(int minEconomy = 4)
    {
        return GetSystemsByMetric(SystemMetricType.EconomicActivity, minEconomy, 5);
    }

    // ========== Tag Queries (WD3) ==========

    /// <summary>
    /// Get systems that have ALL specified tags.
    /// </summary>
    public IEnumerable<StarSystem> GetSystemsWithAllTags(params string[] tags)
    {
        return Systems.Values.Where(s => tags.All(t => s.HasTag(t)));
    }

    /// <summary>
    /// Get systems that have ANY of the specified tags.
    /// </summary>
    public IEnumerable<StarSystem> GetSystemsWithAnyTag(params string[] tags)
    {
        return Systems.Values.Where(s => tags.Any(t => s.HasTag(t)));
    }

    /// <summary>
    /// Get stations by tag.
    /// </summary>
    public IEnumerable<Station> GetStationsByTag(string tag)
    {
        return Stations.Values.Where(s => s.HasTag(tag));
    }

    /// <summary>
    /// Get stations that have ALL specified tags.
    /// </summary>
    public IEnumerable<Station> GetStationsWithAllTags(params string[] tags)
    {
        return Stations.Values.Where(s => tags.All(t => s.HasTag(t)));
    }

    /// <summary>
    /// Get stations that have ANY of the specified tags.
    /// </summary>
    public IEnumerable<Station> GetStationsWithAnyTag(params string[] tags)
    {
        return Stations.Values.Where(s => tags.Any(t => s.HasTag(t)));
    }

    // ========== Composite Queries (WD3) ==========

    /// <summary>
    /// Get the effective danger level for a route.
    /// Combines route hazard with endpoint system metrics.
    /// </summary>
    public int GetEffectiveRouteDanger(int fromId, int toId)
    {
        var route = GetRoute(fromId, toId);
        if (route == null) return 0;

        int baseHazard = route.HazardLevel;

        int fromCrime = GetSystemMetric(fromId, SystemMetricType.CriminalActivity);
        int toCrime = GetSystemMetric(toId, SystemMetricType.CriminalActivity);
        int crimeFactor = System.Math.Max(fromCrime, toCrime) / 2;

        int fromSecurity = GetSystemMetric(fromId, SystemMetricType.SecurityLevel);
        int toSecurity = GetSystemMetric(toId, SystemMetricType.SecurityLevel);
        int securityFactor = System.Math.Min(fromSecurity, toSecurity) / 2;

        return System.Math.Clamp(baseHazard + crimeFactor - securityFactor, 0, 5);
    }

    /// <summary>
    /// Get routes suitable for smuggling (hidden, low patrol, connects to lawless).
    /// </summary>
    public IEnumerable<Route> GetSmugglingRoutes()
    {
        return Routes.Values.Where(r =>
        {
            if (r.HasTag(WorldTags.Patrolled)) return false;
            if (r.HasTag(WorldTags.Hidden)) return true;

            var sysA = GetSystem(r.SystemA);
            var sysB = GetSystem(r.SystemB);
            return (sysA?.HasTag(WorldTags.Lawless) ?? false) ||
                   (sysB?.HasTag(WorldTags.Lawless) ?? false);
        });
    }

    /// <summary>
    /// Get systems suitable for laying low (lawless, low security, not contested).
    /// </summary>
    public IEnumerable<StarSystem> GetHideoutSystems()
    {
        return Systems.Values.Where(s =>
        {
            if (s.HasTag(WorldTags.Contested)) return false;
            if (s.Metrics == null) return false;

            return s.Metrics.SecurityLevel <= 1 || s.HasTag(WorldTags.Lawless);
        });
    }

    /// <summary>
    /// Get encounter context for a route.
    /// Used by Encounter system to select appropriate templates.
    /// </summary>
    public RouteEncounterContext GetRouteEncounterContext(int fromId, int toId)
    {
        var route = GetRoute(fromId, toId);
        var fromSystem = GetSystem(fromId);
        var toSystem = GetSystem(toId);

        if (route == null || fromSystem == null || toSystem == null)
            return null;

        return new RouteEncounterContext
        {
            RouteId = route.Id,
            FromSystemId = fromId,
            ToSystemId = toId,
            Distance = route.Distance,
            HazardLevel = route.HazardLevel,
            EffectiveDanger = GetEffectiveRouteDanger(fromId, toId),
            RouteTags = new HashSet<string>(route.Tags),
            FromSystemTags = new HashSet<string>(fromSystem.Tags),
            ToSystemTags = new HashSet<string>(toSystem.Tags),
            FromSecurityLevel = fromSystem.Metrics?.SecurityLevel ?? 0,
            ToSecurityLevel = toSystem.Metrics?.SecurityLevel ?? 0,
            FromCriminalActivity = fromSystem.Metrics?.CriminalActivity ?? 0,
            ToCriminalActivity = toSystem.Metrics?.CriminalActivity ?? 0
        };
    }

    /// <summary>
    /// Get encounter context for a system (for station encounters).
    /// </summary>
    public SystemEncounterContext GetSystemEncounterContext(int systemId)
    {
        var system = GetSystem(systemId);
        if (system == null) return null;

        var stations = GetStationsInSystem(systemId).ToList();
        var stationTags = new HashSet<string>();
        foreach (var station in stations)
        {
            foreach (var tag in station.Tags)
                stationTags.Add(tag);
        }

        return new SystemEncounterContext
        {
            SystemId = systemId,
            SystemName = system.Name,
            SystemType = system.Type,
            OwningFactionId = system.OwningFactionId,
            SystemTags = new HashSet<string>(system.Tags),
            StationTags = stationTags,
            Metrics = system.Metrics?.Clone() ?? new SystemMetrics(),
            HasStation = stations.Count > 0,
            StationCount = stations.Count
        };
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

    // ========== Route Queries ==========

    /// <summary>
    /// Get route between two systems.
    /// Returns null if no direct route exists.
    /// </summary>
    public Route GetRoute(int fromId, int toId)
    {
        int routeId = Route.GenerateId(fromId, toId);
        return Routes.TryGetValue(routeId, out var route) ? route : null;
    }

    /// <summary>
    /// Get all routes from a system.
    /// </summary>
    public IEnumerable<Route> GetRoutesFrom(int systemId)
    {
        return Routes.Values.Where(r => r.Connects(systemId));
    }

    /// <summary>
    /// Check if a direct route exists between two systems.
    /// </summary>
    public bool HasRoute(int fromId, int toId)
    {
        return GetRoute(fromId, toId) != null;
    }

    /// <summary>
    /// Get all routes in the sector.
    /// </summary>
    public IEnumerable<Route> GetAllRoutes()
    {
        return Routes.Values;
    }

    /// <summary>
    /// Get routes by tag.
    /// </summary>
    public IEnumerable<Route> GetRoutesByTag(string tag)
    {
        return Routes.Values.Where(r => r.HasTag(tag));
    }

    /// <summary>
    /// Get hazard level for route between two systems.
    /// Returns 0 if no route exists.
    /// </summary>
    public int GetRouteHazard(int fromId, int toId)
    {
        return GetRoute(fromId, toId)?.HazardLevel ?? 0;
    }

    // ========== Topology Queries (for travel) ==========

    /// <summary>
    /// Get systems reachable within a given hop count from origin.
    /// Excludes station-type systems and the origin itself.
    /// </summary>
    public List<StarSystem> GetReachableSystems(int originId, int maxHops = 2, bool includeOriginAsFallback = true)
    {
        var result = new List<StarSystem>();
        var visited = new HashSet<int> { originId };

        var currentFrontier = new List<int> { originId };

        for (int hop = 0; hop < maxHops && currentFrontier.Count > 0; hop++)
        {
            var nextFrontier = new List<int>();

            foreach (var systemId in currentFrontier)
            {
                var system = GetSystem(systemId);
                if (system == null) continue;

                foreach (var connId in system.Connections)
                {
                    if (visited.Contains(connId)) continue;
                    visited.Add(connId);

                    var connSystem = GetSystem(connId);
                    if (connSystem != null && connSystem.Type != SystemType.Station)
                    {
                        result.Add(connSystem);
                    }

                    nextFrontier.Add(connId);
                }
            }

            currentFrontier = nextFrontier;
        }

        if (result.Count == 0 && includeOriginAsFallback)
        {
            var origin = GetSystem(originId);
            if (origin != null)
            {
                result.Add(origin);
            }
        }

        return result;
    }

    /// <summary>
    /// Check if two systems are connected (have a route).
    /// </summary>
    public bool AreConnected(int systemA, int systemB)
    {
        return HasRoute(systemA, systemB);
    }

    /// <summary>
    /// Get travel distance between two systems.
    /// Uses route distance if available, otherwise computes from positions.
    /// </summary>
    public float GetTravelDistance(int fromId, int toId)
    {
        var route = GetRoute(fromId, toId);
        if (route != null) return route.Distance;

        var from = GetSystem(fromId);
        var to = GetSystem(toId);
        if (from == null || to == null) return float.MaxValue;
        return from.Position.DistanceTo(to.Position);
    }

    /// <summary>
    /// Connect two systems with a route.
    /// Creates bidirectional connection and route entry.
    /// </summary>
    public Route Connect(int systemA, int systemB, int hazardLevel = 0, params string[] tags)
    {
        var a = GetSystem(systemA);
        var b = GetSystem(systemB);
        if (a == null || b == null) return null;

        var existing = GetRoute(systemA, systemB);
        if (existing != null) return existing;

        if (!a.Connections.Contains(systemB))
            a.Connections.Add(systemB);
        if (!b.Connections.Contains(systemA))
            b.Connections.Add(systemA);

        float distance = a.Position.DistanceTo(b.Position);

        var route = new Route(systemA, systemB, distance)
        {
            HazardLevel = hazardLevel,
            Tags = new HashSet<string>(tags)
        };

        Routes[route.Id] = route;
        return route;
    }

    /// <summary>
    /// Add a pre-configured route.
    /// Also updates system connections.
    /// </summary>
    public void AddRoute(Route route)
    {
        var a = GetSystem(route.SystemA);
        var b = GetSystem(route.SystemB);

        if (a != null && !a.Connections.Contains(route.SystemB))
            a.Connections.Add(route.SystemB);
        if (b != null && !b.Connections.Contains(route.SystemA))
            b.Connections.Add(route.SystemA);

        Routes[route.Id] = route;
    }

    /// <summary>
    /// Find shortest path between two systems using BFS.
    /// Returns list of system IDs including start and end.
    /// Returns empty list if no path exists.
    /// </summary>
    public List<int> FindPath(int fromId, int toId)
    {
        if (fromId == toId) return new List<int> { fromId };
        if (!Systems.ContainsKey(fromId) || !Systems.ContainsKey(toId))
            return new List<int>();

        var visited = new HashSet<int>();
        var queue = new Queue<List<int>>();
        queue.Enqueue(new List<int> { fromId });
        visited.Add(fromId);

        while (queue.Count > 0)
        {
            var path = queue.Dequeue();
            var current = path[path.Count - 1];

            foreach (var neighbor in GetNeighbors(current))
            {
                if (visited.Contains(neighbor)) continue;

                var newPath = new List<int>(path) { neighbor };

                if (neighbor == toId)
                    return newPath;

                visited.Add(neighbor);
                queue.Enqueue(newPath);
            }
        }

        return new List<int>();
    }

    /// <summary>
    /// Calculate total route distance for a path.
    /// </summary>
    public float GetPathDistance(List<int> path)
    {
        if (path == null || path.Count < 2) return 0f;

        float total = 0f;
        for (int i = 0; i < path.Count - 1; i++)
        {
            total += GetTravelDistance(path[i], path[i + 1]);
        }
        return total;
    }

    /// <summary>
    /// Calculate total hazard for a path (sum of route hazards).
    /// </summary>
    public int GetPathHazard(List<int> path)
    {
        if (path == null || path.Count < 2) return 0;

        int total = 0;
        for (int i = 0; i < path.Count - 1; i++)
        {
            total += GetRouteHazard(path[i], path[i + 1]);
        }
        return total;
    }

    /// <summary>
    /// Get routes along a path.
    /// </summary>
    public List<Route> GetPathRoutes(List<int> path)
    {
        var routes = new List<Route>();
        if (path == null || path.Count < 2) return routes;

        for (int i = 0; i < path.Count - 1; i++)
        {
            var route = GetRoute(path[i], path[i + 1]);
            if (route != null) routes.Add(route);
        }
        return routes;
    }

    /// <summary>
    /// Check if system is reachable from another.
    /// </summary>
    public bool IsReachable(int fromId, int toId)
    {
        return FindPath(fromId, toId).Count > 0;
    }

    /// <summary>
    /// Get dangerous routes (hazard >= threshold).
    /// </summary>
    public IEnumerable<Route> GetDangerousRoutes(int minHazard = 3)
    {
        return Routes.Values.Where(r => r.HazardLevel >= minHazard);
    }

    /// <summary>
    /// Get safe routes (hazard <= threshold).
    /// </summary>
    public IEnumerable<Route> GetSafeRoutes(int maxHazard = 1)
    {
        return Routes.Values.Where(r => r.HazardLevel <= maxHazard);
    }

    /// <summary>
    /// Get systems within N hops that have a station.
    /// </summary>
    public List<StarSystem> GetNearbyStationSystems(int originId, int maxHops = 2)
    {
        var result = new List<StarSystem>();
        var visited = new HashSet<int> { originId };
        var frontier = new List<int> { originId };

        for (int hop = 0; hop < maxHops && frontier.Count > 0; hop++)
        {
            var nextFrontier = new List<int>();
            foreach (var systemId in frontier)
            {
                foreach (var neighbor in GetNeighbors(systemId))
                {
                    if (visited.Contains(neighbor)) continue;
                    visited.Add(neighbor);
                    nextFrontier.Add(neighbor);

                    var system = GetSystem(neighbor);
                    if (system != null && system.StationIds.Count > 0)
                        result.Add(system);
                }
            }
            frontier = nextFrontier;
        }

        return result;
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

    // ========== Metric Mutations (WD3) ==========

    /// <summary>
    /// Set a specific metric value for a system.
    /// </summary>
    public bool SetSystemMetric(int systemId, SystemMetricType metric, int value)
    {
        var system = GetSystem(systemId);
        if (system?.Metrics == null) return false;

        int oldValue = system.Metrics.Get(metric);
        system.Metrics.Set(metric, value);
        int newValue = system.Metrics.Get(metric);

        if (oldValue != newValue)
        {
            SimLog.Log($"[World] System {system.Name}: {metric} {oldValue} → {newValue}");
        }

        return true;
    }

    /// <summary>
    /// Modify a metric by delta for a system.
    /// </summary>
    public bool ModifySystemMetric(int systemId, SystemMetricType metric, int delta)
    {
        var system = GetSystem(systemId);
        if (system?.Metrics == null) return false;

        int oldValue = system.Metrics.Get(metric);
        system.Metrics.Modify(metric, delta);
        int newValue = system.Metrics.Get(metric);

        if (oldValue != newValue)
        {
            SimLog.Log($"[World] System {system.Name}: {metric} {oldValue} → {newValue} (delta: {delta:+#;-#;0})");
        }

        return true;
    }

    // ========== Tag Mutations (WD3) ==========

    /// <summary>
    /// Add a tag to a system.
    /// </summary>
    public bool AddSystemTag(int systemId, string tag)
    {
        var system = GetSystem(systemId);
        if (system == null) return false;

        if (system.Tags.Add(tag))
        {
            SimLog.Log($"[World] System {system.Name}: added tag '{tag}'");
            return true;
        }
        return false;
    }

    /// <summary>
    /// Remove a tag from a system.
    /// </summary>
    public bool RemoveSystemTag(int systemId, string tag)
    {
        var system = GetSystem(systemId);
        if (system == null) return false;

        if (system.Tags.Remove(tag))
        {
            SimLog.Log($"[World] System {system.Name}: removed tag '{tag}'");
            return true;
        }
        return false;
    }

    /// <summary>
    /// Add a tag to a station.
    /// </summary>
    public bool AddStationTag(int stationId, string tag)
    {
        var station = GetStation(stationId);
        if (station == null) return false;

        if (station.Tags.Add(tag))
        {
            SimLog.Log($"[World] Station {station.Name}: added tag '{tag}'");
            return true;
        }
        return false;
    }

    /// <summary>
    /// Remove a tag from a station.
    /// </summary>
    public bool RemoveStationTag(int stationId, string tag)
    {
        var station = GetStation(stationId);
        if (station == null) return false;

        if (station.Tags.Remove(tag))
        {
            SimLog.Log($"[World] Station {station.Name}: removed tag '{tag}'");
            return true;
        }
        return false;
    }

    /// <summary>
    /// Add a tag to a route.
    /// </summary>
    public bool AddRouteTag(int fromId, int toId, string tag)
    {
        var route = GetRoute(fromId, toId);
        if (route == null) return false;

        if (route.Tags.Add(tag))
        {
            var fromName = GetSystem(fromId)?.Name ?? fromId.ToString();
            var toName = GetSystem(toId)?.Name ?? toId.ToString();
            SimLog.Log($"[World] Route {fromName}↔{toName}: added tag '{tag}'");
            return true;
        }
        return false;
    }

    /// <summary>
    /// Remove a tag from a route.
    /// </summary>
    public bool RemoveRouteTag(int fromId, int toId, string tag)
    {
        var route = GetRoute(fromId, toId);
        if (route == null) return false;

        if (route.Tags.Remove(tag))
        {
            var fromName = GetSystem(fromId)?.Name ?? fromId.ToString();
            var toName = GetSystem(toId)?.Name ?? toId.ToString();
            SimLog.Log($"[World] Route {fromName}↔{toName}: removed tag '{tag}'");
            return true;
        }
        return false;
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
    /// Create a test sector with 8 systems for G2 development.
    /// Layout:
    ///   [4] Contested Zone
    ///        |
    ///   [0] Haven -- [1] Waypoint -- [2] Mining
    ///        |            |              |
    ///   [5] Patrol    [6] Smuggler   [7] Derelict
    ///                      |
    ///                 [3] Pirate Base
    /// </summary>
    public static WorldState CreateTestSector()
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

        // ===== Systems =====

        // 0: Haven Station - prosperous core world, main hub
        var haven = new StarSystem(0, "Haven Station", SystemType.Station, new Vector2(200, 300))
        {
            OwningFactionId = "corp",
            Tags = new HashSet<string> { WorldTags.Core, WorldTags.Hub }
        };
        haven.Metrics = new SystemMetrics
        {
            Stability = 5,
            SecurityLevel = 4,
            CriminalActivity = 1,
            EconomicActivity = 5,
            LawEnforcementPresence = 4
        };
        world.AddSystem(haven);

        // 1: Waypoint Alpha - frontier outpost, some crime
        var waypoint = new StarSystem(1, "Waypoint Alpha", SystemType.Outpost, new Vector2(350, 300))
        {
            OwningFactionId = "corp",
            Tags = new HashSet<string> { WorldTags.Frontier }
        };
        waypoint.Metrics = new SystemMetrics
        {
            Stability = 3,
            SecurityLevel = 2,
            CriminalActivity = 3,
            EconomicActivity = 2,
            LawEnforcementPresence = 2
        };
        world.AddSystem(waypoint);

        // 2: Rockfall Mining - industrial, moderate security
        var rockfall = new StarSystem(2, "Rockfall Mining", SystemType.Asteroid, new Vector2(500, 300))
        {
            OwningFactionId = "corp",
            Tags = new HashSet<string> { WorldTags.Mining, WorldTags.Industrial }
        };
        rockfall.Metrics = new SystemMetrics
        {
            Stability = 3,
            SecurityLevel = 2,
            CriminalActivity = 2,
            EconomicActivity = 4,
            LawEnforcementPresence = 2
        };
        world.AddSystem(rockfall);

        // 3: Red Claw Base - pirate haven, lawless
        var redClaw = new StarSystem(3, "Red Claw Base", SystemType.Outpost, new Vector2(350, 500))
        {
            OwningFactionId = "pirates",
            Tags = new HashSet<string> { WorldTags.Lawless, WorldTags.PirateHaven }
        };
        redClaw.Metrics = new SystemMetrics
        {
            Stability = 2,
            SecurityLevel = 0,
            CriminalActivity = 5,
            EconomicActivity = 3,
            LawEnforcementPresence = 0
        };
        world.AddSystem(redClaw);

        // 4: Contested Zone - unstable, dangerous
        var contested = new StarSystem(4, "Contested Zone", SystemType.Contested, new Vector2(350, 150))
        {
            OwningFactionId = null,
            Tags = new HashSet<string> { WorldTags.Border, WorldTags.Contested }
        };
        contested.Metrics = new SystemMetrics
        {
            Stability = 1,
            SecurityLevel = 1,
            CriminalActivity = 4,
            EconomicActivity = 1,
            LawEnforcementPresence = 1
        };
        world.AddSystem(contested);

        // 5: Patrol Station - military, very secure
        var patrol = new StarSystem(5, "Patrol Station", SystemType.Station, new Vector2(100, 400))
        {
            OwningFactionId = "corp",
            Tags = new HashSet<string> { WorldTags.Military }
        };
        patrol.Metrics = new SystemMetrics
        {
            Stability = 5,
            SecurityLevel = 5,
            CriminalActivity = 0,
            EconomicActivity = 2,
            LawEnforcementPresence = 5
        };
        world.AddSystem(patrol);

        // 6: Smuggler's Den - hidden, lawless
        var smuggler = new StarSystem(6, "Smuggler's Den", SystemType.Nebula, new Vector2(350, 400))
        {
            OwningFactionId = null,
            Tags = new HashSet<string> { WorldTags.Lawless }
        };
        smuggler.Metrics = new SystemMetrics
        {
            Stability = 2,
            SecurityLevel = 0,
            CriminalActivity = 4,
            EconomicActivity = 3,
            LawEnforcementPresence = 0
        };
        world.AddSystem(smuggler);

        // 7: Wreck of Icarus - abandoned, dangerous
        var wreck = new StarSystem(7, "Wreck of Icarus", SystemType.Derelict, new Vector2(550, 400))
        {
            OwningFactionId = null,
            Tags = new HashSet<string> { WorldTags.Frontier }
        };
        wreck.Metrics = new SystemMetrics
        {
            Stability = 0,
            SecurityLevel = 0,
            CriminalActivity = 3,
            EconomicActivity = 0,
            LawEnforcementPresence = 0
        };
        world.AddSystem(wreck);

        // ===== Routes =====
        world.Connect(0, 1, 1, WorldTags.Patrolled);      // Haven - Waypoint
        world.Connect(0, 5, 0, WorldTags.Patrolled);      // Haven - Patrol
        world.Connect(1, 2, 2, WorldTags.Asteroid);       // Waypoint - Rockfall
        world.Connect(1, 4, 3, WorldTags.Dangerous);      // Waypoint - Contested
        world.Connect(1, 6, 2, WorldTags.Hidden);         // Waypoint - Smuggler
        world.Connect(2, 7, 3, WorldTags.Dangerous);      // Rockfall - Wreck
        world.Connect(3, 6, 2, WorldTags.Hidden);         // Red Claw - Smuggler

        // ===== Stations =====

        // Haven Station - major hub
        var havenStation = Station.CreateHub(world.GenerateStationId(), "Haven Station", 0, "corp");
        world.AddStation(havenStation);

        // Waypoint Alpha - minor outpost
        var waypointStation = Station.CreateOutpost(world.GenerateStationId(), "Waypoint Alpha", 1, "corp");
        world.AddStation(waypointStation);

        // Rockfall Mining - mining station
        var rockfallStation = Station.CreateMining(world.GenerateStationId(), "Rockfall Mining", 2, "corp");
        world.AddStation(rockfallStation);

        // Red Claw Base - pirate den
        var pirateStation = Station.CreatePirateDen(world.GenerateStationId(), "Red Claw Base", 3, "pirates");
        world.AddStation(pirateStation);

        // Patrol Station - military
        var patrolStation = Station.CreateMilitary(world.GenerateStationId(), "Patrol Station", 5, "corp");
        world.AddStation(patrolStation);

        // Smuggler's Den - black market
        var smugglerStation = Station.CreateBlackMarket(world.GenerateStationId(), "Smuggler's Den", 6, null);
        world.AddStation(smugglerStation);

        return world;
    }

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

        foreach (var route in Routes.Values)
        {
            data.Routes.Add(route.GetState());
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

        foreach (var routeData in data.Routes ?? new List<RouteData>())
        {
            var route = Route.FromState(routeData);
            world.Routes[route.Id] = route;

            // Rebuild system connections from routes
            var systemA = world.GetSystem(route.SystemA);
            var systemB = world.GetSystem(route.SystemB);
            if (systemA != null && !systemA.Connections.Contains(route.SystemB))
                systemA.Connections.Add(route.SystemB);
            if (systemB != null && !systemB.Connections.Contains(route.SystemA))
                systemB.Connections.Add(route.SystemA);
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
    public List<RouteData> Routes { get; set; } = new();
}
