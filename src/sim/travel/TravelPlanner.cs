using System;
using System.Collections.Generic;
using Godot;

namespace FringeTactics;

/// <summary>
/// Ship stats used for travel planning.
/// </summary>
public struct TravelShipStats
{
    public float Speed;
    public float Efficiency;
    public float SafetyWeight;

    public static TravelShipStats Default => new()
    {
        Speed = TravelCosts.DefaultSpeed,
        Efficiency = TravelCosts.DefaultEfficiency,
        SafetyWeight = 1.0f
    };
}

/// <summary>
/// Stateless service for route planning.
/// Uses A* pathfinding with configurable cost weights.
/// </summary>
public class TravelPlanner
{
    private readonly WorldState world;

    public TravelPlanner(WorldState world)
    {
        this.world = world ?? throw new ArgumentNullException(nameof(world));
    }

    /// <summary>
    /// Plan a route from origin to destination with default ship stats.
    /// </summary>
    public TravelPlan PlanRoute(int originId, int destinationId)
    {
        return PlanRoute(originId, destinationId, TravelShipStats.Default);
    }

    /// <summary>
    /// Plan a route from origin to destination.
    /// Uses A* pathfinding with weighted costs.
    /// </summary>
    public TravelPlan PlanRoute(int originId, int destinationId, TravelShipStats shipStats)
    {
        if (originId == destinationId)
        {
            return TravelPlan.Invalid(originId, destinationId, TravelPlanInvalidReason.SameSystem);
        }

        var origin = world.GetSystem(originId);
        var destination = world.GetSystem(destinationId);

        if (origin == null || destination == null)
        {
            return TravelPlan.Invalid(originId, destinationId, TravelPlanInvalidReason.InvalidSystem);
        }

        float safetyWeight = shipStats.SafetyWeight > 0 ? shipStats.SafetyWeight : 1.0f;
        var path = FindPathAStar(originId, destinationId, safetyWeight);

        if (path == null || path.Count < 2)
        {
            return TravelPlan.Invalid(originId, destinationId, TravelPlanInvalidReason.NoRoute);
        }

        var segments = BuildSegments(path, shipStats);

        return TravelPlan.FromSegments(originId, destinationId, segments);
    }

    /// <summary>
    /// Validate if a plan is executable with current campaign state.
    /// </summary>
    public bool ValidatePlan(TravelPlan plan, int availableFuel)
    {
        if (plan == null || !plan.IsValid) return false;
        return plan.CanAfford(availableFuel);
    }

    /// <summary>
    /// Get validation failure reason.
    /// </summary>
    public TravelPlanInvalidReason GetValidationFailure(TravelPlan plan, int availableFuel)
    {
        if (plan == null) return TravelPlanInvalidReason.NullPlan;
        if (!plan.IsValid) return plan.InvalidReason;
        if (!plan.CanAfford(availableFuel)) return TravelPlanInvalidReason.InsufficientFuel;
        return TravelPlanInvalidReason.None;
    }

    // ========================================================================
    // Static Utility Methods (for UI)
    // ========================================================================

    /// <summary>
    /// Check if travel is possible from current location to destination.
    /// </summary>
    public static bool CanTravel(CampaignState campaign, int destinationId)
    {
        if (campaign?.World == null) return false;
        if (campaign.CurrentNodeId == destinationId) return false;

        var planner = new TravelPlanner(campaign.World);
        var plan = planner.PlanRoute(campaign.CurrentNodeId, destinationId);

        return plan.IsValid && plan.CanAfford(campaign.Fuel);
    }

    /// <summary>
    /// Get the fuel cost to travel from current location to destination.
    /// Returns -1 if no route exists.
    /// </summary>
    public static int GetFuelCost(CampaignState campaign, int destinationId)
    {
        if (campaign?.World == null) return -1;

        var planner = new TravelPlanner(campaign.World);
        var plan = planner.PlanRoute(campaign.CurrentNodeId, destinationId);

        return plan.IsValid ? plan.TotalFuelCost : -1;
    }

    /// <summary>
    /// Get reason why travel is blocked.
    /// </summary>
    public static string GetTravelBlockReason(CampaignState campaign, int destinationId)
    {
        if (campaign?.World == null) return "No world data";
        if (campaign.CurrentNodeId == destinationId) return "Already at this location";

        var planner = new TravelPlanner(campaign.World);
        var plan = planner.PlanRoute(campaign.CurrentNodeId, destinationId);

        if (!plan.IsValid)
        {
            return plan.InvalidReason switch
            {
                TravelPlanInvalidReason.NoRoute => "No route to this location",
                TravelPlanInvalidReason.InvalidSystem => "Invalid destination",
                _ => "Cannot travel"
            };
        }

        if (!plan.CanAfford(campaign.Fuel))
        {
            return $"Need {plan.TotalFuelCost} fuel (have {campaign.Fuel})";
        }

        return null;
    }

    /// <summary>
    /// A* pathfinding implementation.
    /// </summary>
    private List<int> FindPathAStar(int startId, int goalId, float safetyWeight)
    {
        var start = world.GetSystem(startId);
        var goal = world.GetSystem(goalId);

        if (start == null || goal == null) return null;

        var openSet = new PriorityQueue<int, float>();
        openSet.Enqueue(startId, 0f);

        var cameFrom = new Dictionary<int, int>();
        var gScore = new Dictionary<int, float> { [startId] = 0f };
        var fScore = new Dictionary<int, float>
        {
            [startId] = TravelCosts.CalculateHeuristic(start.Position, goal.Position)
        };

        var closedSet = new HashSet<int>();

        while (openSet.Count > 0)
        {
            int current = openSet.Dequeue();

            if (current == goalId)
            {
                return ReconstructPath(cameFrom, current);
            }

            if (closedSet.Contains(current)) continue;
            closedSet.Add(current);

            foreach (var neighborId in world.GetNeighbors(current))
            {
                if (closedSet.Contains(neighborId)) continue;

                var route = world.GetRoute(current, neighborId);
                if (route == null) continue;

                float tentativeG = gScore[current] + TravelCosts.CalculatePathfindingCost(route, safetyWeight);

                if (!gScore.ContainsKey(neighborId) || tentativeG < gScore[neighborId])
                {
                    cameFrom[neighborId] = current;
                    gScore[neighborId] = tentativeG;

                    var neighbor = world.GetSystem(neighborId);
                    float h = neighbor != null
                        ? TravelCosts.CalculateHeuristic(neighbor.Position, goal.Position)
                        : 0f;
                    fScore[neighborId] = tentativeG + h;

                    openSet.Enqueue(neighborId, fScore[neighborId]);
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Reconstruct path from A* result.
    /// </summary>
    private List<int> ReconstructPath(Dictionary<int, int> cameFrom, int current)
    {
        var path = new List<int> { current };

        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            path.Insert(0, current);
        }

        return path;
    }

    /// <summary>
    /// Build travel segments from a path.
    /// </summary>
    private List<TravelSegment> BuildSegments(List<int> path, TravelShipStats shipStats)
    {
        var segments = new List<TravelSegment>();
        float speed = shipStats.Speed > 0 ? shipStats.Speed : TravelCosts.DefaultSpeed;
        float efficiency = shipStats.Efficiency > 0 ? shipStats.Efficiency : TravelCosts.DefaultEfficiency;

        for (int i = 0; i < path.Count - 1; i++)
        {
            int fromId = path[i];
            int toId = path[i + 1];

            var route = world.GetRoute(fromId, toId);
            if (route == null) continue;

            var segment = TravelSegment.FromRoute(route, fromId, toId, speed, efficiency);
            if (segment != null)
            {
                var fromMetrics = world.GetSystemMetrics(fromId);
                var toMetrics = world.GetSystemMetrics(toId);
                segment.EncounterChance = TravelCosts.CalculateEncounterChance(route, fromMetrics, toMetrics);

                segments.Add(segment);
            }
        }

        return segments;
    }
}
