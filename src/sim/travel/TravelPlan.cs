using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// A complete travel plan from origin to destination.
/// Contains ordered segments and aggregated costs.
/// </summary>
public class TravelPlan
{
    /// <summary>
    /// Origin system ID.
    /// </summary>
    public int OriginSystemId { get; set; }

    /// <summary>
    /// Destination system ID.
    /// </summary>
    public int DestinationSystemId { get; set; }

    /// <summary>
    /// Ordered list of travel segments.
    /// </summary>
    public List<TravelSegment> Segments { get; set; } = new();

    /// <summary>
    /// Total fuel cost for the entire journey.
    /// </summary>
    public int TotalFuelCost { get; set; }

    /// <summary>
    /// Total time in days for the entire journey.
    /// </summary>
    public int TotalTimeDays { get; set; }

    /// <summary>
    /// Total distance for the entire journey.
    /// </summary>
    public float TotalDistance { get; set; }

    /// <summary>
    /// Sum of hazard levels across all segments.
    /// </summary>
    public int TotalHazard { get; set; }

    /// <summary>
    /// Average encounter chance per day across the journey.
    /// </summary>
    public float AverageEncounterChance { get; set; }

    /// <summary>
    /// Whether this plan is valid and executable.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Reason for invalidity (if IsValid is false).
    /// </summary>
    public TravelPlanInvalidReason InvalidReason { get; set; }

    /// <summary>
    /// Number of systems in the path (including origin and destination).
    /// </summary>
    public int SystemCount => Segments.Count + 1;

    /// <summary>
    /// Check if player has enough fuel for this plan.
    /// </summary>
    public bool CanAfford(int availableFuel)
    {
        return IsValid && availableFuel >= TotalFuelCost;
    }

    /// <summary>
    /// Get the path as a list of system IDs.
    /// </summary>
    public List<int> GetPath()
    {
        var path = new List<int>();
        if (Segments.Count == 0)
        {
            if (OriginSystemId == DestinationSystemId)
                path.Add(OriginSystemId);
            return path;
        }

        path.Add(Segments[0].FromSystemId);
        foreach (var segment in Segments)
        {
            path.Add(segment.ToSystemId);
        }
        return path;
    }

    /// <summary>
    /// Create an invalid plan with a reason.
    /// </summary>
    public static TravelPlan Invalid(int origin, int destination, TravelPlanInvalidReason reason)
    {
        return new TravelPlan
        {
            OriginSystemId = origin,
            DestinationSystemId = destination,
            IsValid = false,
            InvalidReason = reason
        };
    }

    /// <summary>
    /// Create a valid plan from segments.
    /// Automatically calculates aggregates.
    /// </summary>
    public static TravelPlan FromSegments(int origin, int destination, List<TravelSegment> segments)
    {
        var plan = new TravelPlan
        {
            OriginSystemId = origin,
            DestinationSystemId = destination,
            Segments = segments ?? new List<TravelSegment>(),
            IsValid = true
        };

        plan.CalculateAggregates();
        return plan;
    }

    /// <summary>
    /// Recalculate aggregate values from segments.
    /// </summary>
    public void CalculateAggregates()
    {
        TotalFuelCost = 0;
        TotalTimeDays = 0;
        TotalDistance = 0f;
        TotalHazard = 0;
        float totalChance = 0f;

        foreach (var segment in Segments)
        {
            TotalFuelCost += segment.FuelCost;
            TotalTimeDays += segment.TimeDays;
            TotalDistance += segment.Distance;
            TotalHazard += segment.HazardLevel;
            totalChance += segment.EncounterChance;
        }

        AverageEncounterChance = Segments.Count > 0
            ? totalChance / Segments.Count
            : 0f;
    }
}
