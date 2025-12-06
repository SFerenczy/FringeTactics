using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// Reasons why a travel plan is invalid.
/// </summary>
public enum TravelPlanInvalidReason
{
    None,
    NoRoute,
    SameSystem,
    InvalidSystem,
    InsufficientFuel,
    NullPlan
}

/// <summary>
/// A single segment of a travel plan (one route between two systems).
/// Contains computed costs and risk for this segment.
/// </summary>
public class TravelSegment
{
    /// <summary>
    /// Origin system ID for this segment.
    /// </summary>
    public int FromSystemId { get; set; }

    /// <summary>
    /// Destination system ID for this segment.
    /// </summary>
    public int ToSystemId { get; set; }

    /// <summary>
    /// Reference to the world route (for tags, hazard).
    /// </summary>
    public Route Route { get; set; }

    /// <summary>
    /// Route distance in world units.
    /// </summary>
    public float Distance { get; set; }

    /// <summary>
    /// Fuel cost for this segment.
    /// Formula: ceil(distance * FuelRate / shipEfficiency)
    /// </summary>
    public int FuelCost { get; set; }

    /// <summary>
    /// Time cost in days for this segment.
    /// Formula: ceil(distance / shipSpeed)
    /// </summary>
    public int TimeDays { get; set; }

    /// <summary>
    /// Base encounter chance per day (0.0 - 1.0).
    /// Calculated from route hazard and tags.
    /// </summary>
    public float EncounterChance { get; set; }

    /// <summary>
    /// Suggested encounter type based on route/system context.
    /// Used by Generation domain for encounter selection.
    /// </summary>
    public string SuggestedEncounterType { get; set; }

    /// <summary>
    /// Route hazard level (0-5).
    /// </summary>
    public int HazardLevel => Route?.HazardLevel ?? 0;

    private static readonly HashSet<string> EmptyTags = new();

    /// <summary>
    /// Route tags for reference.
    /// </summary>
    public HashSet<string> RouteTags => Route?.Tags ?? EmptyTags;

    /// <summary>
    /// Create a segment from a route with default ship stats.
    /// </summary>
    public static TravelSegment FromRoute(Route route, float shipSpeed = 100f, float shipEfficiency = 1.0f)
    {
        if (route == null) return null;

        var segment = new TravelSegment
        {
            FromSystemId = route.SystemA,
            ToSystemId = route.SystemB,
            Route = route,
            Distance = route.Distance
        };

        segment.FuelCost = TravelCosts.CalculateFuelCost(route.Distance, shipEfficiency);
        segment.TimeDays = TravelCosts.CalculateTimeDays(route.Distance, shipSpeed);
        segment.EncounterChance = TravelCosts.CalculateEncounterChance(route);
        segment.SuggestedEncounterType = TravelCosts.SuggestEncounterType(route);

        return segment;
    }

    /// <summary>
    /// Create a segment with explicit from/to (for directional clarity).
    /// </summary>
    public static TravelSegment FromRoute(Route route, int fromId, int toId, float shipSpeed = 100f, float shipEfficiency = 1.0f)
    {
        var segment = FromRoute(route, shipSpeed, shipEfficiency);
        if (segment != null)
        {
            segment.FromSystemId = fromId;
            segment.ToSystemId = toId;
        }
        return segment;
    }
}
