namespace FringeTactics;

/// <summary>
/// Generates names for systems, stations, and NPCs.
/// </summary>
public static class NameGenerator
{
    private static readonly string[] SystemPrefixes =
    {
        "New", "Port", "Fort", "Station", "Outpost", "Camp", "Point", "Base"
    };

    private static readonly string[] SystemNames =
    {
        "Haven", "Reach", "Frontier", "Prospect", "Terminus", "Horizon",
        "Vanguard", "Sentinel", "Bastion", "Refuge", "Waypoint", "Crossroads",
        "Anchor", "Beacon", "Gateway", "Threshold", "Meridian", "Apex",
        "Nexus", "Vertex", "Zenith", "Nadir", "Eclipse", "Corona",
        "Solace", "Vigil", "Bulwark", "Rampart", "Citadel", "Spire",
        "Pinnacle", "Summit", "Crest", "Ridge", "Vale", "Dell",
        "Hollow", "Glen", "Forge", "Anvil", "Crucible", "Hearth",
        "Ember", "Spark", "Flare", "Nova", "Pulsar", "Quasar",
        "Drift", "Wake", "Tide", "Current", "Eddy", "Shoal",
        "Reef", "Atoll", "Cove", "Harbor", "Berth", "Mooring"
    };

    private static readonly string[] SystemSuffixes =
    {
        "Prime", "Alpha", "Beta", "Gamma", "Delta", "VII", "IX", "XII",
        "Station", "Hub", "Post", "Colony", "Depot", "Base"
    };

    private static readonly string[] DerelictPrefixes =
    {
        "Wreck of", "Ruins of", "Remains of", "Hulk of", "Ghost of"
    };

    private static readonly string[] AsteroidNames =
    {
        "Rockfall", "Ironvein", "Dustcloud", "Shatter", "Gravel", "Ore",
        "Cinder", "Boulder", "Shard", "Fragment", "Rubble", "Drift",
        "Crater", "Pebble", "Scree", "Talus", "Cobble", "Flint",
        "Granite", "Basalt", "Obsidian", "Pumice", "Slag", "Clinker"
    };

    private static readonly string[] NebulaNames =
    {
        "Shroud", "Veil", "Mist", "Haze", "Cloud", "Shadow",
        "Murk", "Fog", "Gloom", "Dusk", "Twilight", "Shade",
        "Wisp", "Plume", "Billow", "Pall", "Mantle", "Cloak",
        "Curtain", "Screen", "Blanket", "Cover", "Canopy", "Awning"
    };

    private static readonly string[] SectorPrefixes =
    {
        "Outer", "Inner", "Far", "Near", "Deep", "High", "Low", "Mid"
    };

    private static readonly string[] SectorNames =
    {
        "Reach", "Frontier", "Expanse", "Sector", "Rim", "Cluster",
        "Territories", "Marches", "Bounds", "Fringe", "Edge", "Verge"
    };

    /// <summary>
    /// Generate a system name based on type.
    /// </summary>
    public static string GenerateSystemName(SystemType type, RngStream rng)
    {
        return type switch
        {
            SystemType.Derelict => GenerateDerelictName(rng),
            SystemType.Asteroid => GenerateAsteroidName(rng),
            SystemType.Nebula => GenerateNebulaName(rng),
            _ => GenerateStandardName(rng)
        };
    }

    private static string GenerateStandardName(RngStream rng)
    {
        bool usePrefix = rng.NextFloat() < 0.3f;
        bool useSuffix = rng.NextFloat() < 0.4f;

        string name = SystemNames[rng.NextInt(SystemNames.Length)];

        if (usePrefix)
            name = $"{SystemPrefixes[rng.NextInt(SystemPrefixes.Length)]} {name}";

        if (useSuffix)
            name = $"{name} {SystemSuffixes[rng.NextInt(SystemSuffixes.Length)]}";

        return name;
    }

    private static string GenerateDerelictName(RngStream rng)
    {
        string prefix = DerelictPrefixes[rng.NextInt(DerelictPrefixes.Length)];
        string name = SystemNames[rng.NextInt(SystemNames.Length)];
        return $"{prefix} {name}";
    }

    private static string GenerateAsteroidName(RngStream rng)
    {
        string name = AsteroidNames[rng.NextInt(AsteroidNames.Length)];
        string suffix = SystemSuffixes[rng.NextInt(SystemSuffixes.Length)];
        return $"{name} {suffix}";
    }

    private static string GenerateNebulaName(RngStream rng)
    {
        string name = NebulaNames[rng.NextInt(NebulaNames.Length)];
        string baseName = SystemNames[rng.NextInt(SystemNames.Length)];
        return $"{baseName} {name}";
    }

    /// <summary>
    /// Generate station name (usually matches system name).
    /// </summary>
    public static string GenerateStationName(string systemName, RngStream rng)
    {
        // 70% chance to match system name
        if (rng.NextFloat() < 0.7f)
            return systemName;

        // Otherwise generate variant
        string[] suffixes = { "Station", "Dock", "Port", "Hub" };
        string suffix = suffixes[rng.NextInt(suffixes.Length)];

        // Remove existing suffix if present to avoid "Haven Station Station"
        foreach (var existingSuffix in SystemSuffixes)
        {
            if (systemName.EndsWith(existingSuffix))
            {
                systemName = systemName[..^existingSuffix.Length].TrimEnd();
                break;
            }
        }

        return $"{systemName} {suffix}";
    }

    /// <summary>
    /// Generate a sector name.
    /// </summary>
    public static string GenerateSectorName(RngStream rng)
    {
        string prefix = SectorPrefixes[rng.NextInt(SectorPrefixes.Length)];
        string name = SectorNames[rng.NextInt(SectorNames.Length)];
        return $"{prefix} {name}";
    }
}
