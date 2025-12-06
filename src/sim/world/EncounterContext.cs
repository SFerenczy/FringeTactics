using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// Context data for route-based encounter selection.
/// Used by Encounter system to select appropriate templates.
/// </summary>
public class RouteEncounterContext
{
    public int RouteId { get; set; }
    public int FromSystemId { get; set; }
    public int ToSystemId { get; set; }
    public float Distance { get; set; }
    public int HazardLevel { get; set; }
    public int EffectiveDanger { get; set; }
    public HashSet<string> RouteTags { get; set; } = new();
    public HashSet<string> FromSystemTags { get; set; } = new();
    public HashSet<string> ToSystemTags { get; set; } = new();
    public int FromSecurityLevel { get; set; }
    public int ToSecurityLevel { get; set; }
    public int FromCriminalActivity { get; set; }
    public int ToCriminalActivity { get; set; }

    /// <summary>
    /// Check if route or either endpoint has a tag.
    /// </summary>
    public bool HasAnyTag(string tag)
    {
        return RouteTags.Contains(tag) ||
               FromSystemTags.Contains(tag) ||
               ToSystemTags.Contains(tag);
    }

    /// <summary>
    /// Get the higher criminal activity of the two endpoints.
    /// </summary>
    public int MaxCriminalActivity => System.Math.Max(FromCriminalActivity, ToCriminalActivity);

    /// <summary>
    /// Get the lower security level of the two endpoints.
    /// </summary>
    public int MinSecurityLevel => System.Math.Min(FromSecurityLevel, ToSecurityLevel);

    /// <summary>
    /// Check if this is a high-danger route (effective danger >= 3).
    /// </summary>
    public bool IsHighDanger => EffectiveDanger >= 3;

    /// <summary>
    /// Check if either endpoint is lawless.
    /// </summary>
    public bool HasLawlessEndpoint => FromSecurityLevel <= 1 || ToSecurityLevel <= 1;
}

/// <summary>
/// Context data for system-based encounter selection.
/// Used for station encounters and system events.
/// </summary>
public class SystemEncounterContext
{
    public int SystemId { get; set; }
    public string SystemName { get; set; }
    public SystemType SystemType { get; set; }
    public string OwningFactionId { get; set; }
    public HashSet<string> SystemTags { get; set; } = new();
    public HashSet<string> StationTags { get; set; } = new();
    public SystemMetrics Metrics { get; set; }
    public bool HasStation { get; set; }
    public int StationCount { get; set; }

    /// <summary>
    /// Check if system or any station has a tag.
    /// </summary>
    public bool HasAnyTag(string tag)
    {
        return SystemTags.Contains(tag) || StationTags.Contains(tag);
    }

    /// <summary>
    /// Check if this is a lawless system (security <= 1).
    /// </summary>
    public bool IsLawless => Metrics?.SecurityLevel <= 1;

    /// <summary>
    /// Check if this is a high-security system (security >= 4).
    /// </summary>
    public bool IsHighSecurity => Metrics?.SecurityLevel >= 4;

    /// <summary>
    /// Check if this system has high criminal activity (>= 4).
    /// </summary>
    public bool IsHighCrime => Metrics?.CriminalActivity >= 4;
}
