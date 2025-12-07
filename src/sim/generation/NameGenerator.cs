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

    // === NPC Names ===

    private static readonly string[] FirstNames =
    {
        "Marcus", "Elena", "Viktor", "Zara", "Chen", "Yuki", "Omar", "Freya",
        "Dante", "Mira", "Kira", "Jax", "Nova", "Rook", "Sage", "Quinn",
        "Ash", "Blaze", "Cade", "Dex", "Echo", "Finn", "Grey", "Hawk",
        "Iris", "Jade", "Kane", "Luna", "Max", "Nyx", "Orion", "Pax",
        "Rex", "Sable", "Thorn", "Vale", "Wren", "Xander", "Yara", "Zeke"
    };

    private static readonly string[] LastNames =
    {
        "Vance", "Cross", "Stone", "Black", "Grey", "Wolf", "Hawk", "Frost",
        "Drake", "Steel", "Raven", "Storm", "Blade", "Thorn", "Vale", "Marsh",
        "Cole", "Nash", "Reed", "Shaw", "Ward", "Webb", "York", "Zane",
        "Croft", "Dunn", "Flint", "Grant", "Hayes", "Keane", "Lane", "Mercer",
        "North", "Price", "Quinn", "Reese", "Sloan", "Thorne", "Vega", "Wolfe"
    };

    private static readonly string[] Nicknames =
    {
        "the Blade", "One-Eye", "Lucky", "the Ghost", "Ironhand", "Quickshot",
        "the Rat", "Deadshot", "the Fixer", "Scarface", "the Jackal", "Whisper",
        "Red", "the Hammer", "Bones", "Patches", "the Knife", "Smokey",
        "Grim", "the Viper", "Rusty", "the Shark", "Slim", "the Crow"
    };

    // === Cargo Types ===

    private static readonly string[] LegalCargo =
    {
        "medical supplies", "food rations", "industrial parts", "electronics",
        "textiles", "construction materials", "fuel cells", "water purifiers",
        "agricultural equipment", "mining tools", "communication gear", "power converters",
        "atmospheric processors", "recycling units", "habitat modules", "solar panels"
    };

    private static readonly string[] IllegalCargo =
    {
        "weapons cache", "contraband tech", "stolen goods", "unregistered meds",
        "black market chips", "smuggled artifacts", "restricted chemicals",
        "forged documents", "unlicensed AI cores", "banned stimulants",
        "pirated data", "counterfeit credits", "stolen military hardware"
    };

    private static readonly string[] ValuableCargo =
    {
        "rare minerals", "prototype tech", "encrypted data cores", "luxury goods",
        "antique artifacts", "research samples", "corporate secrets", "exotic materials",
        "precision instruments", "bioengineered organisms", "quantum components",
        "archaeological finds", "art collection", "rare isotopes"
    };

    // === Ship Names ===

    private static readonly string[] ShipPrefixes =
    {
        "ISV", "CSV", "FTV", "MSV", "RSV", "TSV", "HSV", "PSV"
    };

    private static readonly string[] ShipNames =
    {
        "Wanderer", "Fortune", "Destiny", "Horizon", "Venture", "Pioneer",
        "Seeker", "Ranger", "Nomad", "Drifter", "Pathfinder", "Voyager",
        "Endeavor", "Prospect", "Wayfarer", "Trailblazer", "Explorer", "Surveyor",
        "Stalwart", "Resolute", "Valiant", "Intrepid", "Dauntless", "Steadfast"
    };

    private static readonly string[] PirateShipNames =
    {
        "Reaper", "Marauder", "Ravager", "Predator", "Scourge", "Raider",
        "Pillager", "Corsair", "Buccaneer", "Freebooter", "Plunderer", "Cutlass",
        "Black Tide", "Red Wake", "Dark Star", "Iron Fang", "Blood Moon", "Storm Crow"
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

    // ========================================================================
    // NPC Name Generation (GN3)
    // ========================================================================

    /// <summary>
    /// Generate a random NPC name.
    /// </summary>
    /// <param name="rng">RNG stream for deterministic generation.</param>
    /// <param name="includeNickname">If true, 20% chance to include a nickname.</param>
    public static string GenerateNpcName(RngStream rng, bool includeNickname = true)
    {
        string first = FirstNames[rng.NextInt(FirstNames.Length)];
        string last = LastNames[rng.NextInt(LastNames.Length)];

        if (includeNickname && rng.NextFloat() < 0.2f)
        {
            string nick = Nicknames[rng.NextInt(Nicknames.Length)];
            return $"{first} \"{nick}\" {last}";
        }

        return $"{first} {last}";
    }

    /// <summary>
    /// Generate a pirate/criminal name (always includes nickname).
    /// </summary>
    public static string GeneratePirateName(RngStream rng)
    {
        string first = FirstNames[rng.NextInt(FirstNames.Length)];
        string nick = Nicknames[rng.NextInt(Nicknames.Length)];
        return $"{first} {nick}";
    }

    /// <summary>
    /// Generate just a first name (for informal references).
    /// </summary>
    public static string GenerateFirstName(RngStream rng)
    {
        return FirstNames[rng.NextInt(FirstNames.Length)];
    }

    // ========================================================================
    // Cargo Type Generation (GN3)
    // ========================================================================

    /// <summary>
    /// Generate a cargo type description.
    /// </summary>
    /// <param name="rng">RNG stream for deterministic generation.</param>
    /// <param name="illegal">If true, generates illegal cargo types.</param>
    /// <param name="valuable">If true, generates valuable cargo types (ignored if illegal is true).</param>
    public static string GenerateCargoType(RngStream rng, bool illegal = false, bool valuable = false)
    {
        if (illegal)
            return IllegalCargo[rng.NextInt(IllegalCargo.Length)];
        if (valuable)
            return ValuableCargo[rng.NextInt(ValuableCargo.Length)];
        return LegalCargo[rng.NextInt(LegalCargo.Length)];
    }

    /// <summary>
    /// Generate a random cargo type (any category).
    /// </summary>
    public static string GenerateRandomCargoType(RngStream rng)
    {
        float roll = rng.NextFloat();
        if (roll < 0.6f)
            return GenerateCargoType(rng, illegal: false, valuable: false);
        if (roll < 0.85f)
            return GenerateCargoType(rng, illegal: false, valuable: true);
        return GenerateCargoType(rng, illegal: true);
    }

    // ========================================================================
    // Ship Name Generation (GN3)
    // ========================================================================

    /// <summary>
    /// Generate a ship name with prefix (e.g., "ISV Wanderer").
    /// </summary>
    public static string GenerateShipName(RngStream rng)
    {
        string prefix = ShipPrefixes[rng.NextInt(ShipPrefixes.Length)];
        string name = ShipNames[rng.NextInt(ShipNames.Length)];
        return $"{prefix} {name}";
    }

    /// <summary>
    /// Generate a ship name without prefix (e.g., "Wanderer").
    /// </summary>
    public static string GenerateShipNameSimple(RngStream rng)
    {
        return ShipNames[rng.NextInt(ShipNames.Length)];
    }

    /// <summary>
    /// Generate a pirate ship name (more menacing).
    /// </summary>
    public static string GeneratePirateShipName(RngStream rng)
    {
        return PirateShipNames[rng.NextInt(PirateShipNames.Length)];
    }
}
