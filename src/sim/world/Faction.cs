using System;
using Godot;

namespace FringeTactics;

/// <summary>
/// Types of factions in the game world.
/// </summary>
public enum FactionType
{
    Corporate,
    Government,
    Criminal,
    Independent,
    Neutral
}

/// <summary>
/// A faction in the game world.
/// </summary>
public class Faction
{
    public string Id { get; set; }
    public string Name { get; set; }
    public FactionType Type { get; set; }
    public Color Color { get; set; } = Colors.Gray;
    public int HostilityDefault { get; set; } = 50;
    public FactionMetrics Metrics { get; set; } = new();

    public Faction(string id, string name, FactionType type)
    {
        Id = id;
        Name = name;
        Type = type;
    }

    public FactionData GetState()
    {
        return new FactionData
        {
            Id = Id,
            Name = Name,
            Type = Type.ToString(),
            ColorR = Color.R,
            ColorG = Color.G,
            ColorB = Color.B,
            HostilityDefault = HostilityDefault,
            Metrics = Metrics.GetState()
        };
    }

    public static Faction FromState(FactionData data)
    {
        var faction = new Faction(
            data.Id,
            data.Name,
            Enum.TryParse<FactionType>(data.Type, out var type) ? type : FactionType.Neutral
        )
        {
            Color = new Color(data.ColorR, data.ColorG, data.ColorB),
            HostilityDefault = data.HostilityDefault,
            Metrics = FactionMetrics.FromState(data.Metrics)
        };
        return faction;
    }
}

/// <summary>
/// Faction-level metrics per WD0 design.
/// </summary>
public class FactionMetrics
{
    public int MilitaryStrength { get; set; } = 3;
    public int EconomicPower { get; set; } = 3;
    public int Influence { get; set; } = 3;
    public int Desperation { get; set; } = 1;
    public int Corruption { get; set; } = 2;

    public FactionMetricsData GetState()
    {
        return new FactionMetricsData
        {
            MilitaryStrength = MilitaryStrength,
            EconomicPower = EconomicPower,
            Influence = Influence,
            Desperation = Desperation,
            Corruption = Corruption
        };
    }

    public static FactionMetrics FromState(FactionMetricsData data)
    {
        if (data == null) return new FactionMetrics();

        return new FactionMetrics
        {
            MilitaryStrength = data.MilitaryStrength,
            EconomicPower = data.EconomicPower,
            Influence = data.Influence,
            Desperation = data.Desperation,
            Corruption = data.Corruption
        };
    }
}

/// <summary>
/// Serializable data for Faction.
/// </summary>
public class FactionData
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Type { get; set; }
    public float ColorR { get; set; }
    public float ColorG { get; set; }
    public float ColorB { get; set; }
    public int HostilityDefault { get; set; }
    public FactionMetricsData Metrics { get; set; }
}

/// <summary>
/// Serializable data for FactionMetrics.
/// </summary>
public class FactionMetricsData
{
    public int MilitaryStrength { get; set; }
    public int EconomicPower { get; set; }
    public int Influence { get; set; }
    public int Desperation { get; set; }
    public int Corruption { get; set; }
}
