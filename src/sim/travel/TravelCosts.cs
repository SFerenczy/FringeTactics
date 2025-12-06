using System;
using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// Suggested encounter types for travel segments.
/// </summary>
public static class EncounterTypes
{
    public const string Pirate = "pirate";
    public const string Patrol = "patrol";
    public const string Smuggler = "smuggler";
    public const string Trader = "trader";
    public const string Random = "random";
}

/// <summary>
/// Encounter outcome constants for travel resume.
/// </summary>
public static class EncounterOutcomes
{
    public const string Completed = "completed";
    public const string Defeat = "defeat";
    public const string Captured = "captured";
    public const string Fled = "fled";
    public const string Pending = "pending";
}

/// <summary>
/// Stateless utility for travel cost calculations.
/// Formulas defined in TV0_IMPLEMENTATION.md.
/// </summary>
public static class TravelCosts
{
    // === Constants (from TV0) ===

    /// <summary>
    /// Base fuel consumption per distance unit.
    /// </summary>
    public const float FuelRate = 0.1f;

    /// <summary>
    /// Default ship speed (distance units per day).
    /// </summary>
    public const float DefaultSpeed = 100f;

    /// <summary>
    /// Default ship fuel efficiency multiplier.
    /// </summary>
    public const float DefaultEfficiency = 1.0f;

    /// <summary>
    /// Maximum encounter chance (cap at 80%).
    /// </summary>
    public const float MaxEncounterChance = 0.8f;

    /// <summary>
    /// Safety weight for A* pathfinding cost.
    /// </summary>
    public const float SafetyWeight = 50f;

    // === Tag Modifiers (from TV0) ===

    private static readonly Dictionary<string, float> TagModifiers = new()
    {
        { WorldTags.Patrolled, -0.10f },
        { WorldTags.Dangerous, +0.10f },
        { WorldTags.Hidden, -0.05f },
        { WorldTags.Blockaded, +0.20f },
        { WorldTags.Asteroid, +0.05f },
        { WorldTags.Nebula, +0.05f }
    };

    // === Fuel Cost ===

    /// <summary>
    /// Calculate fuel cost for a distance.
    /// Formula: ceil(distance * FuelRate / efficiency)
    /// </summary>
    public static int CalculateFuelCost(float distance, float efficiency = 1.0f)
    {
        if (distance <= 0) return 0;
        if (efficiency <= 0) efficiency = 1.0f;

        return (int)Math.Ceiling(distance * FuelRate / efficiency);
    }

    // === Time Cost ===

    /// <summary>
    /// Calculate travel time in days.
    /// Formula: ceil(distance / speed)
    /// Minimum 1 day.
    /// </summary>
    public static int CalculateTimeDays(float distance, float speed = 100f)
    {
        if (distance <= 0) return 0;
        if (speed <= 0) speed = DefaultSpeed;

        return Math.Max(1, (int)Math.Ceiling(distance / speed));
    }

    // === Encounter Chance ===

    /// <summary>
    /// Calculate base encounter chance per day for a route.
    /// Formula: clamp(hazard * 0.1 + tagModifiers, 0, 0.8)
    /// </summary>
    public static float CalculateEncounterChance(Route route)
    {
        if (route == null) return 0f;

        float baseChance = route.HazardLevel * 0.1f;

        float tagModifier = 0f;
        foreach (var tag in route.Tags)
        {
            if (TagModifiers.TryGetValue(tag, out float mod))
            {
                tagModifier += mod;
            }
        }

        return Math.Clamp(baseChance + tagModifier, 0f, MaxEncounterChance);
    }

    /// <summary>
    /// Calculate encounter chance with system metric modifiers.
    /// </summary>
    public static float CalculateEncounterChance(Route route, SystemMetrics fromMetrics, SystemMetrics toMetrics)
    {
        float baseChance = CalculateEncounterChance(route);

        float metricModifier = 0f;

        int minSecurity = Math.Min(
            fromMetrics?.SecurityLevel ?? 0,
            toMetrics?.SecurityLevel ?? 0
        );
        if (minSecurity >= 4) metricModifier -= 0.10f;
        else if (minSecurity <= 1) metricModifier += 0.10f;

        int maxCrime = Math.Max(
            fromMetrics?.CriminalActivity ?? 0,
            toMetrics?.CriminalActivity ?? 0
        );
        if (maxCrime >= 4) metricModifier += 0.15f;
        else if (maxCrime <= 1) metricModifier -= 0.05f;

        return Math.Clamp(baseChance + metricModifier, 0f, MaxEncounterChance);
    }

    // === Encounter Type Suggestion ===

    /// <summary>
    /// Suggest encounter type based on route characteristics.
    /// </summary>
    public static string SuggestEncounterType(Route route)
    {
        if (route == null) return EncounterTypes.Random;

        if (route.HazardLevel >= 4) return EncounterTypes.Pirate;
        if (route.HasTag(WorldTags.Patrolled)) return EncounterTypes.Patrol;
        if (route.HasTag(WorldTags.Hidden)) return EncounterTypes.Smuggler;
        if (route.HasTag(WorldTags.Dangerous)) return EncounterTypes.Pirate;
        if (route.HazardLevel <= 1) return EncounterTypes.Trader;

        return EncounterTypes.Random;
    }

    // === A* Cost Function ===

    /// <summary>
    /// Calculate A* pathfinding cost for a route.
    /// Formula: distance + (hazard * safetyWeight)
    /// </summary>
    public static float CalculatePathfindingCost(Route route, float safetyWeight = 1.0f)
    {
        if (route == null) return float.MaxValue;

        return route.Distance + (route.HazardLevel * SafetyWeight * safetyWeight);
    }

    /// <summary>
    /// Calculate A* heuristic (straight-line distance estimate).
    /// </summary>
    public static float CalculateHeuristic(Godot.Vector2 from, Godot.Vector2 to)
    {
        return from.DistanceTo(to);
    }
}
