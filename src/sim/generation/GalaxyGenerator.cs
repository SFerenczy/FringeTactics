using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace FringeTactics;

/// <summary>
/// Procedurally generates a galaxy/sector for a new campaign.
/// </summary>
public class GalaxyGenerator
{
    private readonly GalaxyConfig config;
    private readonly RngStream rng;

    public GalaxyGenerator(GalaxyConfig config, RngStream rng)
    {
        this.config = config ?? throw new ArgumentNullException(nameof(config));
        this.rng = rng ?? throw new ArgumentNullException(nameof(rng));
    }

    /// <summary>
    /// Generate a complete world state.
    /// </summary>
    public WorldState Generate()
    {
        config.Validate();
        SimLog.Log($"[GalaxyGenerator] Generating sector with {config.SystemCount} systems");

        var world = new WorldState
        {
            Name = NameGenerator.GenerateSectorName(rng)
        };

        // Load factions from registry
        foreach (var faction in FactionRegistry.GetAll())
        {
            world.AddFaction(faction.Clone());
        }

        // Phase 2: Generate positions
        var positions = GeneratePositions();
        SimLog.Log($"[GalaxyGenerator] Placed {positions.Count} systems");

        // Phase 2: Create systems (placeholder content for now)
        for (int i = 0; i < positions.Count; i++)
        {
            var system = new StarSystem(i, $"System_{i}", SystemType.Outpost, positions[i]);
            world.AddSystem(system);
        }

        // Phase 3: Generate routes
        var mstEdges = BuildMST(positions);
        var extraEdges = AddExtraRoutes(positions, mstEdges);
        var allEdges = mstEdges.Concat(extraEdges).ToList();
        CreateRoutes(world, positions, allEdges);
        SimLog.Log($"[GalaxyGenerator] Created {allEdges.Count} routes ({mstEdges.Count} MST + {extraEdges.Count} extra)");

        // Phase 4: Assign factions
        var capitals = PlaceFactionCapitals(world, positions);
        AssignFactionOwnership(world, capitals);
        MarkNeutralSystems(world, capitals);
        SimLog.Log($"[GalaxyGenerator] Assigned {capitals.Count} faction capitals");

        // Phase 5: System content
        AssignSystemTypes(world, capitals);
        AssignSystemNames(world);
        InitializeSystemMetrics(world, capitals);
        AssignSystemTags(world);
        UpdateRouteHazards(world);
        SimLog.Log("[GalaxyGenerator] Assigned system content");

        // Phase 6: Station generation
        GenerateStations(world);
        SimLog.Log($"[GalaxyGenerator] Created {world.Stations.Count} stations");

        return world;
    }

    // ========================================================================
    // PHASE 2: POSITION GENERATION
    // ========================================================================

    /// <summary>
    /// Generate system positions using rejection sampling.
    /// </summary>
    public List<Vector2> GeneratePositions()
    {
        var positions = new List<Vector2>();
        int attempts = 0;
        int maxAttempts = config.SystemCount * 100;

        float minX = config.EdgeMargin;
        float maxX = config.MapWidth - config.EdgeMargin;
        float minY = config.EdgeMargin;
        float maxY = config.MapHeight - config.EdgeMargin;

        while (positions.Count < config.SystemCount && attempts < maxAttempts)
        {
            var pos = new Vector2(
                rng.NextFloat(minX, maxX),
                rng.NextFloat(minY, maxY)
            );

            if (IsValidPosition(pos, positions))
            {
                positions.Add(pos);
            }

            attempts++;
        }

        if (positions.Count < config.SystemCount)
        {
            SimLog.Log($"[GalaxyGenerator] WARNING: Only placed {positions.Count}/{config.SystemCount} systems after {maxAttempts} attempts");
        }

        return positions;
    }

    /// <summary>
    /// Check if position is valid (far enough from existing positions).
    /// </summary>
    public bool IsValidPosition(Vector2 pos, List<Vector2> existing)
    {
        float minDistSq = config.MinSystemDistance * config.MinSystemDistance;

        foreach (var other in existing)
        {
            if (pos.DistanceSquaredTo(other) < minDistSq)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Check if position is within map bounds (respecting edge margin).
    /// </summary>
    public bool IsWithinBounds(Vector2 pos)
    {
        return pos.X >= config.EdgeMargin &&
               pos.X <= config.MapWidth - config.EdgeMargin &&
               pos.Y >= config.EdgeMargin &&
               pos.Y <= config.MapHeight - config.EdgeMargin;
    }

    // ========================================================================
    // PHASE 3: ROUTE GENERATION
    // ========================================================================

    /// <summary>
    /// Build minimum spanning tree using Prim's algorithm.
    /// Guarantees all systems are connected.
    /// </summary>
    public List<(int, int)> BuildMST(List<Vector2> positions)
    {
        if (positions.Count < 2)
            return new List<(int, int)>();

        int n = positions.Count;
        var edges = new List<(int, int)>();
        var inTree = new HashSet<int> { 0 };

        while (inTree.Count < n)
        {
            float bestDist = float.MaxValue;
            int bestFrom = -1;
            int bestTo = -1;

            foreach (int from in inTree)
            {
                for (int to = 0; to < n; to++)
                {
                    if (inTree.Contains(to)) continue;

                    float dist = positions[from].DistanceTo(positions[to]);
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        bestFrom = from;
                        bestTo = to;
                    }
                }
            }

            if (bestTo >= 0)
            {
                edges.Add((bestFrom, bestTo));
                inTree.Add(bestTo);
            }
            else
            {
                break;
            }
        }

        return edges;
    }

    /// <summary>
    /// Add random extra routes beyond MST for variety.
    /// Only adds routes within MaxRouteDistance and respects MaxConnections.
    /// </summary>
    public List<(int, int)> AddExtraRoutes(List<Vector2> positions, List<(int, int)> mstEdges)
    {
        var extraEdges = new List<(int, int)>();
        var existingEdges = new HashSet<(int, int)>();

        // Track existing edges (normalized: smaller index first)
        foreach (var (a, b) in mstEdges)
        {
            existingEdges.Add((Math.Min(a, b), Math.Max(a, b)));
        }

        // Count connections per system
        var connectionCount = new int[positions.Count];
        foreach (var (a, b) in mstEdges)
        {
            connectionCount[a]++;
            connectionCount[b]++;
        }

        // Try to add extra edges
        for (int i = 0; i < positions.Count; i++)
        {
            if (connectionCount[i] >= config.MaxConnections) continue;

            for (int j = i + 1; j < positions.Count; j++)
            {
                if (connectionCount[j] >= config.MaxConnections) continue;

                var key = (i, j);
                if (existingEdges.Contains(key)) continue;

                float dist = positions[i].DistanceTo(positions[j]);
                if (dist > config.MaxRouteDistance) continue;

                // Random chance to add edge (closer = more likely)
                float chance = 1.0f - (dist / config.MaxRouteDistance);
                if (rng.NextFloat() < chance * config.ExtraRouteChance)
                {
                    extraEdges.Add(key);
                    existingEdges.Add(key);
                    connectionCount[i]++;
                    connectionCount[j]++;

                    // Stop if either system is at max connections
                    if (connectionCount[i] >= config.MaxConnections)
                        break;
                }
            }
        }

        return extraEdges;
    }

    /// <summary>
    /// Create Route objects in WorldState from edge list.
    /// </summary>
    public void CreateRoutes(WorldState world, List<Vector2> positions, List<(int, int)> edges)
    {
        foreach (var (a, b) in edges)
        {
            // Distance calculated automatically by WorldState.Connect()
            // Hazard level will be updated later based on system types
            world.Connect(a, b, 1); // Default hazard = 1
        }
    }

    /// <summary>
    /// Get total edge count (MST + extra).
    /// </summary>
    public int GetRouteCount(List<Vector2> positions)
    {
        var mst = BuildMST(positions);
        var extra = AddExtraRoutes(positions, mst);
        return mst.Count + extra.Count;
    }

    // ========================================================================
    // PHASE 4: FACTION ASSIGNMENT
    // ========================================================================

    /// <summary>
    /// Place faction capitals at well-spaced positions.
    /// Returns dictionary of factionId -> systemId.
    /// </summary>
    public Dictionary<string, int> PlaceFactionCapitals(WorldState world, List<Vector2> positions)
    {
        var capitals = new Dictionary<string, int>();
        var usedSystems = new HashSet<int>();

        // Get factions to place (limited by available systems)
        var factionIds = config.GetFactionIds();
        var factionsToPlace = factionIds
            .Where(id => world.Factions.ContainsKey(id))
            .Take(Math.Min(factionIds.Count, positions.Count))
            .ToList();

        foreach (var factionId in factionsToPlace)
        {
            int bestSystem = FindBestCapitalLocation(positions, capitals, usedSystems);

            if (bestSystem >= 0)
            {
                capitals[factionId] = bestSystem;
                usedSystems.Add(bestSystem);

                // Mark as hub
                var system = world.GetSystem(bestSystem);
                if (system != null)
                {
                    system.Tags.Add(WorldTags.Hub);
                    system.Tags.Add(WorldTags.Core);
                }
            }
        }

        return capitals;
    }

    /// <summary>
    /// Find best location for a new capital (furthest from existing capitals).
    /// </summary>
    private int FindBestCapitalLocation(
        List<Vector2> positions,
        Dictionary<string, int> existingCapitals,
        HashSet<int> usedSystems)
    {
        int bestSystem = -1;
        float bestMinDist = -1f;

        for (int i = 0; i < positions.Count; i++)
        {
            if (usedSystems.Contains(i)) continue;

            float minDist;

            if (existingCapitals.Count == 0)
            {
                // First capital: prefer systems away from center for variety
                var center = new Vector2(config.MapWidth / 2, config.MapHeight / 2);
                minDist = positions[i].DistanceTo(center);
            }
            else
            {
                // Subsequent capitals: maximize distance to nearest existing capital
                minDist = float.MaxValue;
                foreach (var capitalIdx in existingCapitals.Values)
                {
                    float dist = positions[i].DistanceTo(positions[capitalIdx]);
                    minDist = Math.Min(minDist, dist);
                }
            }

            if (minDist > bestMinDist)
            {
                bestMinDist = minDist;
                bestSystem = i;
            }
        }

        return bestSystem;
    }

    /// <summary>
    /// Assign faction ownership via flood fill from capitals.
    /// </summary>
    public void AssignFactionOwnership(WorldState world, Dictionary<string, int> capitals)
    {
        var ownership = new Dictionary<int, string>();
        var distance = new Dictionary<int, int>();

        // Initialize capitals
        foreach (var (factionId, systemId) in capitals)
        {
            ownership[systemId] = factionId;
            distance[systemId] = 0;

            var system = world.GetSystem(systemId);
            if (system != null)
            {
                system.OwningFactionId = factionId;
            }
        }

        // BFS from all capitals simultaneously
        var queue = new Queue<int>(capitals.Values);

        while (queue.Count > 0)
        {
            int current = queue.Dequeue();
            int currentDist = distance[current];
            string currentFaction = ownership[current];

            foreach (int neighbor in world.GetNeighbors(current))
            {
                if (!ownership.ContainsKey(neighbor))
                {
                    // Unowned: claim it
                    ownership[neighbor] = currentFaction;
                    distance[neighbor] = currentDist + 1;
                    queue.Enqueue(neighbor);

                    var system = world.GetSystem(neighbor);
                    if (system != null)
                    {
                        system.OwningFactionId = currentFaction;
                    }
                }
                else if (ownership[neighbor] != currentFaction &&
                         distance[neighbor] == currentDist + 1)
                {
                    // Contested: equidistant from two factions
                    var system = world.GetSystem(neighbor);
                    if (system != null && !system.HasTag(WorldTags.Contested))
                    {
                        system.Tags.Add(WorldTags.Contested);
                        system.Tags.Add(WorldTags.Border);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Mark frontier systems as neutral based on config.NeutralFraction.
    /// </summary>
    public void MarkNeutralSystems(WorldState world, Dictionary<string, int> capitals)
    {
        int neutralCount = (int)(world.Systems.Count * config.NeutralFraction);
        if (neutralCount == 0) return;

        // Find systems furthest from any capital
        var systemsByDistance = world.GetAllSystems()
            .Where(s => !capitals.ContainsValue(s.Id))
            .Select(s => new
            {
                System = s,
                MinCapitalDist = GetMinDistanceToCapitals(s, capitals, world)
            })
            .OrderByDescending(x => x.MinCapitalDist)
            .Take(neutralCount)
            .ToList();

        foreach (var item in systemsByDistance)
        {
            item.System.OwningFactionId = null;
            if (!item.System.HasTag(WorldTags.Contested))
            {
                item.System.Tags.Add(WorldTags.Frontier);
            }
        }
    }

    /// <summary>
    /// Get minimum distance from a system to any capital.
    /// </summary>
    private float GetMinDistanceToCapitals(
        StarSystem system,
        Dictionary<string, int> capitals,
        WorldState world)
    {
        float minDist = float.MaxValue;

        foreach (var capitalId in capitals.Values)
        {
            var capital = world.GetSystem(capitalId);
            if (capital != null)
            {
                float dist = system.Position.DistanceTo(capital.Position);
                minDist = Math.Min(minDist, dist);
            }
        }

        return minDist;
    }

    /// <summary>
    /// Get faction ownership statistics.
    /// </summary>
    public Dictionary<string, int> GetFactionSystemCounts(WorldState world)
    {
        var counts = new Dictionary<string, int>();

        foreach (var system in world.GetAllSystems())
        {
            string faction = system.OwningFactionId ?? "neutral";
            counts[faction] = counts.GetValueOrDefault(faction, 0) + 1;
        }

        return counts;
    }

    // ========================================================================
    // PHASE 5: SYSTEM CONTENT
    // ========================================================================

    /// <summary>
    /// Assign system types based on position and faction.
    /// </summary>
    public void AssignSystemTypes(WorldState world, Dictionary<string, int> capitals)
    {
        foreach (var system in world.GetAllSystems())
        {
            // Capitals are always stations
            if (capitals.ContainsValue(system.Id))
            {
                system.Type = SystemType.Station;
                continue;
            }

            // Contested systems
            if (system.HasTag(WorldTags.Contested))
            {
                system.Type = SystemType.Contested;
                continue;
            }

            // Random type based on weights
            system.Type = SelectSystemType();
        }
    }

    /// <summary>
    /// Select system type using weighted random.
    /// </summary>
    private SystemType SelectSystemType()
    {
        float total = config.SystemTypeWeights.Values.Sum();
        float roll = rng.NextFloat() * total;
        float cumulative = 0f;

        foreach (var (type, weight) in config.SystemTypeWeights)
        {
            cumulative += weight;
            if (roll <= cumulative)
                return type;
        }

        return SystemType.Outpost; // Fallback
    }

    /// <summary>
    /// Assign unique names to all systems.
    /// </summary>
    public void AssignSystemNames(WorldState world)
    {
        var usedNames = new HashSet<string>();

        foreach (var system in world.GetAllSystems())
        {
            string name;
            int attempts = 0;

            do
            {
                name = NameGenerator.GenerateSystemName(system.Type, rng);
                attempts++;
            } while (usedNames.Contains(name) && attempts < 20);

            // If still duplicate, add numeric suffix
            if (usedNames.Contains(name))
            {
                int suffix = 2;
                while (usedNames.Contains($"{name} {suffix}"))
                    suffix++;
                name = $"{name} {suffix}";
            }

            usedNames.Add(name);
            system.Name = name;
        }
    }

    /// <summary>
    /// Initialize system metrics based on type and context.
    /// </summary>
    public void InitializeSystemMetrics(WorldState world, Dictionary<string, int> capitals)
    {
        foreach (var system in world.GetAllSystems())
        {
            // Start with type-based defaults
            system.Metrics = SystemMetrics.ForSystemType(system.Type);

            // Capitals get boosted metrics
            if (capitals.ContainsValue(system.Id))
            {
                system.Metrics.Stability = 5;
                system.Metrics.SecurityLevel = 4;
                system.Metrics.EconomicActivity = 4;
                system.Metrics.LawEnforcementPresence = 4;
                system.Metrics.CriminalActivity = 1;
            }

            // Frontier systems are less stable
            if (system.HasTag(WorldTags.Frontier))
            {
                system.Metrics.Stability = Math.Max(1, system.Metrics.Stability - 1);
                system.Metrics.SecurityLevel = Math.Max(0, system.Metrics.SecurityLevel - 1);
            }

            // Contested systems are unstable
            if (system.HasTag(WorldTags.Contested))
            {
                system.Metrics.Stability = 1;
                system.Metrics.CriminalActivity = Math.Min(5, system.Metrics.CriminalActivity + 2);
            }

            // Add some random variance
            ApplyMetricVariance(system.Metrics);
        }
    }

    /// <summary>
    /// Apply small random variance to metrics.
    /// </summary>
    private void ApplyMetricVariance(SystemMetrics metrics)
    {
        metrics.Stability = Clamp(metrics.Stability + rng.NextInt(-1, 2), 0, 5);
        metrics.SecurityLevel = Clamp(metrics.SecurityLevel + rng.NextInt(-1, 2), 0, 5);
        metrics.CriminalActivity = Clamp(metrics.CriminalActivity + rng.NextInt(-1, 2), 0, 5);
        metrics.EconomicActivity = Clamp(metrics.EconomicActivity + rng.NextInt(-1, 2), 0, 5);
        metrics.LawEnforcementPresence = Clamp(metrics.LawEnforcementPresence + rng.NextInt(-1, 2), 0, 5);
    }

    private static int Clamp(int value, int min, int max) =>
        Math.Max(min, Math.Min(max, value));

    /// <summary>
    /// Assign tags based on system properties.
    /// </summary>
    public void AssignSystemTags(WorldState world)
    {
        foreach (var system in world.GetAllSystems())
        {
            // Type-based tags
            switch (system.Type)
            {
                case SystemType.Asteroid:
                    system.Tags.Add(WorldTags.Mining);
                    if (system.Metrics.EconomicActivity >= 4)
                        system.Tags.Add(WorldTags.Industrial);
                    break;

                case SystemType.Derelict:
                    if (!system.HasTag(WorldTags.Frontier))
                        system.Tags.Add(WorldTags.Frontier);
                    break;

                case SystemType.Nebula:
                    if (system.Metrics.CriminalActivity >= 3)
                        system.Tags.Add(WorldTags.Lawless);
                    break;
            }

            // Metric-based tags
            if (system.Metrics.SecurityLevel >= 4)
                system.Tags.Add(WorldTags.Military);

            if (system.Metrics.CriminalActivity >= 4 && system.Metrics.SecurityLevel <= 1)
                system.Tags.Add(WorldTags.Lawless);

            if (system.Metrics.CriminalActivity >= 5)
                system.Tags.Add(WorldTags.PirateHaven);
        }
    }

    /// <summary>
    /// Update route hazards based on endpoint systems.
    /// </summary>
    public void UpdateRouteHazards(WorldState world)
    {
        foreach (var route in world.GetAllRoutes())
        {
            var systemA = world.GetSystem(route.SystemA);
            var systemB = world.GetSystem(route.SystemB);

            if (systemA == null || systemB == null) continue;

            // Base hazard from system types
            int hazard = 0;

            // Dangerous system types increase hazard
            if (systemA.Type == SystemType.Contested || systemB.Type == SystemType.Contested)
                hazard += 2;
            if (systemA.Type == SystemType.Derelict || systemB.Type == SystemType.Derelict)
                hazard += 1;
            if (systemA.Type == SystemType.Nebula || systemB.Type == SystemType.Nebula)
                hazard += 1;

            // Criminal activity increases hazard
            int maxCrime = Math.Max(
                systemA.Metrics?.CriminalActivity ?? 0,
                systemB.Metrics?.CriminalActivity ?? 0);
            hazard += maxCrime / 2;

            // Security reduces hazard
            int minSecurity = Math.Min(
                systemA.Metrics?.SecurityLevel ?? 0,
                systemB.Metrics?.SecurityLevel ?? 0);
            hazard -= minSecurity / 2;

            route.HazardLevel = Math.Clamp(hazard, 0, 5);

            // Add route tags
            if (route.HazardLevel >= 3)
                route.Tags.Add(WorldTags.Dangerous);
            if (minSecurity >= 4)
                route.Tags.Add(WorldTags.Patrolled);
            if (systemA.Type == SystemType.Asteroid || systemB.Type == SystemType.Asteroid)
                route.Tags.Add(WorldTags.Asteroid);
            if (systemA.Type == SystemType.Nebula || systemB.Type == SystemType.Nebula)
                route.Tags.Add(WorldTags.Hidden);
        }
    }

    // ========================================================================
    // PHASE 6: STATION GENERATION
    // ========================================================================

    /// <summary>
    /// Generate stations for inhabited systems.
    /// </summary>
    public void GenerateStations(WorldState world)
    {
        foreach (var system in world.GetAllSystems())
        {
            if (!ShouldHaveStation(system))
                continue;

            var station = CreateStationForSystem(world, system);
            if (station != null)
            {
                world.AddStation(station);
            }
        }
    }

    /// <summary>
    /// Determine if a system should have a station.
    /// </summary>
    private bool ShouldHaveStation(StarSystem system)
    {
        // Derelicts and contested zones don't have stations
        if (system.Type == SystemType.Derelict || system.Type == SystemType.Contested)
            return false;

        // Check if type is in inhabited types config
        return config.InhabitedTypes.Contains(system.Type);
    }

    /// <summary>
    /// Create appropriate station for system type and tags.
    /// </summary>
    public Station CreateStationForSystem(WorldState world, StarSystem system)
    {
        int stationId = world.GenerateStationId();
        string stationName = NameGenerator.GenerateStationName(system.Name, rng);

        // Hub systems get hub stations
        if (system.HasTag(WorldTags.Hub))
        {
            return Station.CreateHub(stationId, stationName, system.Id, system.OwningFactionId);
        }

        // Military systems get military stations
        if (system.HasTag(WorldTags.Military))
        {
            return Station.CreateMilitary(stationId, stationName, system.Id, system.OwningFactionId);
        }

        // Lawless/pirate systems get pirate dens or black markets
        if (system.HasTag(WorldTags.PirateHaven))
        {
            return Station.CreatePirateDen(stationId, stationName, system.Id, system.OwningFactionId);
        }

        if (system.HasTag(WorldTags.Lawless))
        {
            return Station.CreateBlackMarket(stationId, stationName, system.Id, system.OwningFactionId);
        }

        // Type-based station selection
        return system.Type switch
        {
            SystemType.Station => Station.CreateOutpost(stationId, stationName, system.Id, system.OwningFactionId),
            SystemType.Outpost => Station.CreateOutpost(stationId, stationName, system.Id, system.OwningFactionId),
            SystemType.Asteroid => Station.CreateMining(stationId, stationName, system.Id, system.OwningFactionId),
            SystemType.Nebula => CreateNebulaStation(stationId, stationName, system),
            _ => Station.CreateOutpost(stationId, stationName, system.Id, system.OwningFactionId)
        };
    }

    /// <summary>
    /// Create station for nebula systems (varies based on metrics).
    /// </summary>
    private Station CreateNebulaStation(int stationId, string stationName, StarSystem system)
    {
        // High criminal activity in nebula = black market
        if (system.Metrics.CriminalActivity >= 3)
        {
            return Station.CreateBlackMarket(stationId, stationName, system.Id, system.OwningFactionId);
        }

        // Otherwise basic outpost
        return Station.CreateOutpost(stationId, stationName, system.Id, system.OwningFactionId);
    }

    /// <summary>
    /// Get count of stations by type.
    /// </summary>
    public Dictionary<string, int> GetStationTypeCounts(WorldState world)
    {
        var counts = new Dictionary<string, int>();

        foreach (var station in world.Stations.Values)
        {
            string type = GetStationType(station);
            counts[type] = counts.GetValueOrDefault(type, 0) + 1;
        }

        return counts;
    }

    /// <summary>
    /// Determine station type from tags.
    /// </summary>
    private string GetStationType(Station station)
    {
        if (station.HasTag(WorldTags.Hub)) return "Hub";
        if (station.HasTag(WorldTags.Military)) return "Military";
        if (station.HasTag(WorldTags.BlackMarket)) return "BlackMarket";
        if (station.HasTag(WorldTags.Industrial)) return "Mining";
        if (station.HasTag(WorldTags.Frontier)) return "Outpost";
        return "Unknown";
    }

}

