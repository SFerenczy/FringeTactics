using System;
using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// Types of facilities available at stations.
/// </summary>
public enum FacilityType
{
    Shop,
    Bar,
    MissionBoard,
    RepairYard,
    Recruitment,
    Medical,
    BlackMarket,
    FuelDepot
}

/// <summary>
/// A facility at a station that provides services.
/// </summary>
public class Facility
{
    /// <summary>
    /// Type of facility.
    /// </summary>
    public FacilityType Type { get; set; }

    /// <summary>
    /// Quality/inventory tier (1-3).
    /// </summary>
    public int Level { get; set; } = 1;

    /// <summary>
    /// Special modifiers (e.g., "faction_exclusive").
    /// </summary>
    public HashSet<string> Tags { get; set; } = new();

    /// <summary>
    /// Can be temporarily closed.
    /// </summary>
    public bool Available { get; set; } = true;

    public Facility(FacilityType type, int level = 1)
    {
        Type = type;
        Level = level;
    }

    public FacilityData GetState()
    {
        return new FacilityData
        {
            Type = Type.ToString(),
            Level = Level,
            Tags = new List<string>(Tags),
            Available = Available
        };
    }

    public static Facility FromState(FacilityData data)
    {
        var facility = new Facility(
            Enum.TryParse<FacilityType>(data.Type, out var type) ? type : FacilityType.Shop,
            data.Level
        )
        {
            Tags = new HashSet<string>(data.Tags ?? new List<string>()),
            Available = data.Available
        };
        return facility;
    }
}

/// <summary>
/// Serializable data for Facility.
/// </summary>
public class FacilityData
{
    public string Type { get; set; }
    public int Level { get; set; }
    public List<string> Tags { get; set; }
    public bool Available { get; set; }
}
