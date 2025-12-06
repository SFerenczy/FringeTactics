using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// Well-known tags for systems, stations, and routes.
/// Tags are strings for moddability, but these constants ensure consistency.
/// </summary>
public static class WorldTags
{
    // ========== System Tags ==========

    // Region type
    public const string Core = "core";
    public const string Frontier = "frontier";
    public const string Border = "border";

    // Economic character
    public const string Industrial = "industrial";
    public const string Mining = "mining";
    public const string Agricultural = "agricultural";

    // Political/security
    public const string Lawless = "lawless";
    public const string Military = "military";
    public const string Contested = "contested";

    // Special designations
    public const string Hub = "hub";
    public const string PirateHaven = "pirate_haven";
    public const string ResearchOutpost = "research_outpost";
    public const string Quarantined = "quarantined";

    // ========== Station Tags ==========

    public const string TradeHub = "trade_hub";
    public const string BlackMarket = "black_market";
    public const string RepairYard = "repair_yard";
    public const string RecruitmentCenter = "recruitment";
    public const string MedicalFacility = "medical";
    public const string Entertainment = "entertainment";
    public const string Refinery = "refinery";
    public const string Shipyard = "shipyard";

    // ========== Route Tags ==========

    public const string Dangerous = "dangerous";
    public const string Patrolled = "patrolled";
    public const string Hidden = "hidden";
    public const string Blockaded = "blockaded";
    public const string Shortcut = "shortcut";
    public const string Asteroid = "asteroid_field";
    public const string Nebula = "nebula";
    public const string Unstable = "unstable";

    // ========== Tag Categories (for validation) ==========

    /// <summary>
    /// Tags that apply only to systems.
    /// </summary>
    public static readonly HashSet<string> SystemTags = new()
    {
        Core, Frontier, Border,
        Industrial, Mining, Agricultural,
        Lawless, Military, Contested,
        Hub, PirateHaven, ResearchOutpost, Quarantined
    };

    /// <summary>
    /// Tags that apply only to stations.
    /// </summary>
    public static readonly HashSet<string> StationTags = new()
    {
        TradeHub, BlackMarket, RepairYard, RecruitmentCenter,
        MedicalFacility, Entertainment, Refinery, Shipyard
    };

    /// <summary>
    /// Tags that apply only to routes.
    /// </summary>
    public static readonly HashSet<string> RouteTags = new()
    {
        Dangerous, Patrolled, Hidden, Blockaded,
        Shortcut, Asteroid, Nebula, Unstable
    };

    /// <summary>
    /// Tags that can apply to both systems and stations.
    /// Used when a station inherits character from its parent system.
    /// </summary>
    public static readonly HashSet<string> SharedTags = new()
    {
        Hub, Frontier, Industrial, Military, Lawless
    };

    /// <summary>
    /// Check if a tag is a known system tag (including shared tags).
    /// </summary>
    public static bool IsSystemTag(string tag) => SystemTags.Contains(tag);

    /// <summary>
    /// Check if a tag is valid for a station (station-specific or shared).
    /// </summary>
    public static bool IsStationTag(string tag) => StationTags.Contains(tag) || SharedTags.Contains(tag);

    /// <summary>
    /// Check if a tag is a known route tag.
    /// </summary>
    public static bool IsRouteTag(string tag) => RouteTags.Contains(tag);

    /// <summary>
    /// Check if a tag can be shared between systems and stations.
    /// </summary>
    public static bool IsSharedTag(string tag) => SharedTags.Contains(tag);
}
