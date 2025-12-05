using System;
using System.Collections.Generic;
using Godot;

namespace FringeTactics;

/// <summary>
/// A star system in the sector.
/// Enhanced representation with metrics and tags.
/// </summary>
public class StarSystem
{
    public int Id { get; set; }
    public string Name { get; set; }
    public SystemType Type { get; set; }
    public Vector2 Position { get; set; }
    public List<int> Connections { get; set; } = new();
    public string OwningFactionId { get; set; }
    public SystemMetrics Metrics { get; set; }
    public HashSet<string> Tags { get; set; } = new();
    public List<int> StationIds { get; set; } = new();

    public StarSystem(int id, string name, SystemType type, Vector2 position)
    {
        Id = id;
        Name = name;
        Type = type;
        Position = position;
        Metrics = SystemMetrics.ForSystemType(type);
    }

    public bool HasTag(string tag)
    {
        return Tags.Contains(tag);
    }

    public StarSystemData GetState()
    {
        return new StarSystemData
        {
            Id = Id,
            Name = Name,
            Type = Type.ToString(),
            PositionX = Position.X,
            PositionY = Position.Y,
            Connections = new List<int>(Connections),
            OwningFactionId = OwningFactionId,
            Metrics = Metrics?.GetState(),
            Tags = new List<string>(Tags),
            StationIds = new List<int>(StationIds)
        };
    }

    public static StarSystem FromState(StarSystemData data)
    {
        var system = new StarSystem(
            data.Id,
            data.Name,
            Enum.TryParse<SystemType>(data.Type, out var type) ? type : SystemType.Station,
            new Vector2(data.PositionX, data.PositionY)
        )
        {
            Connections = new List<int>(data.Connections ?? new List<int>()),
            OwningFactionId = data.OwningFactionId,
            Metrics = SystemMetrics.FromState(data.Metrics),
            Tags = new HashSet<string>(data.Tags ?? new List<string>()),
            StationIds = new List<int>(data.StationIds ?? new List<int>())
        };
        return system;
    }
}

/// <summary>
/// Serializable data for StarSystem.
/// </summary>
public class StarSystemData
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Type { get; set; }
    public float PositionX { get; set; }
    public float PositionY { get; set; }
    public List<int> Connections { get; set; }
    public string OwningFactionId { get; set; }
    public SystemMetricsData Metrics { get; set; }
    public List<string> Tags { get; set; }
    public List<int> StationIds { get; set; }
}
