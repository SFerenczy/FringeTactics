using System;
using System.Collections.Generic;
using System.Linq;

namespace FringeTactics;

/// <summary>
/// Configuration for procedural galaxy generation.
/// </summary>
public class GalaxyConfig
{
    // ===== Topology =====

    /// <summary>Number of star systems to generate.</summary>
    public int SystemCount { get; set; } = 12;

    /// <summary>Minimum connections per system (MST guarantees at least 1).</summary>
    public int MinConnections { get; set; } = 1;

    /// <summary>Maximum connections per system.</summary>
    public int MaxConnections { get; set; } = 4;

    /// <summary>Maximum distance for adding extra routes (beyond MST).</summary>
    public float MaxRouteDistance { get; set; } = 200f;

    /// <summary>Chance multiplier for adding extra routes (0-1).</summary>
    public float ExtraRouteChance { get; set; } = 0.4f;

    // ===== Spatial =====

    /// <summary>Map width in arbitrary units.</summary>
    public float MapWidth { get; set; } = 800f;

    /// <summary>Map height in arbitrary units.</summary>
    public float MapHeight { get; set; } = 600f;

    /// <summary>Minimum distance between systems.</summary>
    public float MinSystemDistance { get; set; } = 80f;

    /// <summary>Margin from map edges.</summary>
    public float EdgeMargin { get; set; } = 50f;

    // ===== Factions =====

    /// <summary>Faction IDs to include in generation. Null means use all from FactionRegistry.</summary>
    public List<string> FactionIds { get; set; } = null;

    /// <summary>Get faction IDs, falling back to registry if not specified.</summary>
    public List<string> GetFactionIds()
    {
        if (FactionIds != null && FactionIds.Count > 0)
            return FactionIds;
        return FactionRegistry.GetAll().Select(f => f.Id).ToList();
    }

    /// <summary>Fraction of systems that should be neutral/contested.</summary>
    public float NeutralFraction { get; set; } = 0.2f;

    // ===== System Types =====

    /// <summary>Weights for system type selection.</summary>
    public Dictionary<SystemType, float> SystemTypeWeights { get; set; } = new()
    {
        [SystemType.Station] = 0.25f,
        [SystemType.Outpost] = 0.30f,
        [SystemType.Asteroid] = 0.15f,
        [SystemType.Nebula] = 0.10f,
        [SystemType.Derelict] = 0.10f,
        [SystemType.Contested] = 0.10f
    };

    // ===== Stations =====

    /// <summary>System types that get stations.</summary>
    public HashSet<SystemType> InhabitedTypes { get; set; } = new()
    {
        SystemType.Station,
        SystemType.Outpost,
        SystemType.Asteroid,
        SystemType.Nebula
    };

    // ===== Presets =====

    /// <summary>Default configuration for standard campaigns.</summary>
    public static GalaxyConfig Default => new();

    /// <summary>Small sector for testing.</summary>
    public static GalaxyConfig Small => new()
    {
        SystemCount = 8,
        MapWidth = 600f,
        MapHeight = 450f
    };

    /// <summary>Large sector for extended campaigns.</summary>
    public static GalaxyConfig Large => new()
    {
        SystemCount = 20,
        MapWidth = 1000f,
        MapHeight = 800f,
        MaxConnections = 5
    };

    // ===== Validation =====

    /// <summary>
    /// Validate configuration and throw if invalid.
    /// </summary>
    public void Validate()
    {
        if (SystemCount < 1)
            throw new ArgumentException($"SystemCount must be >= 1, got {SystemCount}");

        if (MapWidth <= 0 || MapHeight <= 0)
            throw new ArgumentException($"Map dimensions must be positive, got {MapWidth}x{MapHeight}");

        if (MinSystemDistance <= 0)
            throw new ArgumentException($"MinSystemDistance must be positive, got {MinSystemDistance}");

        if (EdgeMargin < 0)
            throw new ArgumentException($"EdgeMargin cannot be negative, got {EdgeMargin}");

        float usableWidth = MapWidth - 2 * EdgeMargin;
        float usableHeight = MapHeight - 2 * EdgeMargin;
        if (usableWidth <= 0 || usableHeight <= 0)
            throw new ArgumentException($"EdgeMargin ({EdgeMargin}) is too large for map size ({MapWidth}x{MapHeight})");

        if (MinSystemDistance > Math.Min(usableWidth, usableHeight))
            throw new ArgumentException($"MinSystemDistance ({MinSystemDistance}) is too large for usable map area");

        if (MaxConnections < 1)
            throw new ArgumentException($"MaxConnections must be >= 1, got {MaxConnections}");

        if (NeutralFraction < 0 || NeutralFraction > 1)
            throw new ArgumentException($"NeutralFraction must be 0-1, got {NeutralFraction}");

        if (ExtraRouteChance < 0 || ExtraRouteChance > 1)
            throw new ArgumentException($"ExtraRouteChance must be 0-1, got {ExtraRouteChance}");
    }
}
