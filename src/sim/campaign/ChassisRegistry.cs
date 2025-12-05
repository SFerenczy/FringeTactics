using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// Definition of a ship chassis type.
/// </summary>
public class ChassisDef
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public int MaxHull { get; set; }
    public int EngineSlots { get; set; }
    public int WeaponSlots { get; set; }
    public int CargoSlots { get; set; }
    public int UtilitySlots { get; set; }
    public int BaseValue { get; set; }
    public List<string> Tags { get; set; } = new();
}

/// <summary>
/// Registry of all chassis definitions.
/// </summary>
public static class ChassisRegistry
{
    private static Dictionary<string, ChassisDef> chassis = new();
    private static bool initialized = false;

    public static void EnsureInitialized()
    {
        if (initialized) return;
        RegisterDefaultChassis();
        initialized = true;
    }

    public static ChassisDef Get(string id)
    {
        EnsureInitialized();
        return chassis.TryGetValue(id, out var def) ? def : null;
    }

    public static bool Has(string id)
    {
        EnsureInitialized();
        return chassis.ContainsKey(id);
    }

    public static IEnumerable<ChassisDef> GetAll()
    {
        EnsureInitialized();
        return chassis.Values;
    }

    public static void Register(ChassisDef def)
    {
        EnsureInitialized();
        chassis[def.Id] = def;
    }

    public static void Reset()
    {
        chassis.Clear();
        initialized = false;
    }

    private static void RegisterDefaultChassis()
    {
        chassis["scout"] = new ChassisDef
        {
            Id = "scout",
            Name = "Scout",
            Description = "Light and fast. Good for exploration and quick jobs.",
            MaxHull = 50,
            EngineSlots = 1,
            WeaponSlots = 1,
            CargoSlots = 1,
            UtilitySlots = 1,
            BaseValue = 1000,
            Tags = new() { "light", "fast" }
        };

        chassis["freighter"] = new ChassisDef
        {
            Id = "freighter",
            Name = "Freighter",
            Description = "Built for hauling cargo. Slow but spacious.",
            MaxHull = 80,
            EngineSlots = 1,
            WeaponSlots = 1,
            CargoSlots = 3,
            UtilitySlots = 1,
            BaseValue = 2000,
            Tags = new() { "heavy", "cargo" }
        };

        chassis["corvette"] = new ChassisDef
        {
            Id = "corvette",
            Name = "Corvette",
            Description = "Balanced multi-role vessel. Jack of all trades.",
            MaxHull = 100,
            EngineSlots = 2,
            WeaponSlots = 2,
            CargoSlots = 2,
            UtilitySlots = 2,
            BaseValue = 5000,
            Tags = new() { "medium", "versatile" }
        };

        chassis["gunship"] = new ChassisDef
        {
            Id = "gunship",
            Name = "Gunship",
            Description = "Heavy combat vessel. Maximum firepower.",
            MaxHull = 120,
            EngineSlots = 2,
            WeaponSlots = 3,
            CargoSlots = 1,
            UtilitySlots = 2,
            BaseValue = 8000,
            Tags = new() { "heavy", "combat" }
        };
    }
}
