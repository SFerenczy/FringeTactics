using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// A route connecting two star systems.
/// Routes are bidirectional - SystemA ↔ SystemB.
/// </summary>
public class Route
{
    public int Id { get; set; }
    public int SystemA { get; set; }
    public int SystemB { get; set; }

    /// <summary>
    /// Travel distance in arbitrary units.
    /// Used for fuel/time calculations.
    /// </summary>
    public float Distance { get; set; }

    /// <summary>
    /// Hazard level 0-5. Higher = more dangerous.
    /// Affects encounter probability and type.
    /// </summary>
    public int HazardLevel { get; set; } = 0;

    /// <summary>
    /// Route tags for encounter selection.
    /// Examples: "dangerous", "patrolled", "hidden"
    /// </summary>
    public HashSet<string> Tags { get; set; } = new();

    public Route() { }

    public Route(int systemA, int systemB, float distance = 0f)
    {
        SystemA = systemA;
        SystemB = systemB;
        Distance = distance;
        Id = GenerateId(systemA, systemB);
    }

    /// <summary>
    /// Check if this route connects to a given system.
    /// </summary>
    public bool Connects(int systemId) => SystemA == systemId || SystemB == systemId;

    /// <summary>
    /// Get the other endpoint of the route.
    /// </summary>
    public int GetOther(int systemId) => SystemA == systemId ? SystemB : SystemA;

    /// <summary>
    /// Check if route has a specific tag.
    /// </summary>
    public bool HasTag(string tag) => Tags.Contains(tag);

    /// <summary>
    /// Generate deterministic route ID from system pair.
    /// Same ID regardless of direction (A→B == B→A).
    /// </summary>
    public static int GenerateId(int systemA, int systemB)
    {
        int min = System.Math.Min(systemA, systemB);
        int max = System.Math.Max(systemA, systemB);
        return min * 1000000 + max;
    }

    public RouteData GetState()
    {
        return new RouteData
        {
            Id = Id,
            SystemA = SystemA,
            SystemB = SystemB,
            Distance = Distance,
            HazardLevel = HazardLevel,
            Tags = new List<string>(Tags)
        };
    }

    public static Route FromState(RouteData data)
    {
        return new Route
        {
            Id = data.Id,
            SystemA = data.SystemA,
            SystemB = data.SystemB,
            Distance = data.Distance,
            HazardLevel = data.HazardLevel,
            Tags = new HashSet<string>(data.Tags ?? new List<string>())
        };
    }
}

/// <summary>
/// Serializable data for Route.
/// </summary>
public class RouteData
{
    public int Id { get; set; }
    public int SystemA { get; set; }
    public int SystemB { get; set; }
    public float Distance { get; set; }
    public int HazardLevel { get; set; }
    public List<string> Tags { get; set; } = new();
}
