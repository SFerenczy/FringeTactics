using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace FringeTactics;

/// <summary>
/// Registry of all item definitions.
/// </summary>
public static class ItemRegistry
{
    private static Dictionary<string, ItemDef> items = new();
    private static bool initialized = false;
    private static readonly string DataPath = "res://data/items.json";

    public static void EnsureInitialized()
    {
        if (initialized) return;
        
        if (!TryLoadFromJson())
        {
            RegisterDefaultItems();
        }
        initialized = true;
    }

    private static bool TryLoadFromJson()
    {
        try
        {
            string jsonPath = Godot.ProjectSettings.GlobalizePath(DataPath);
            if (!File.Exists(jsonPath)) return false;

            string json = File.ReadAllText(jsonPath);
            var data = JsonSerializer.Deserialize<ItemsJsonData>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (data?.Items == null) return false;

            foreach (var itemData in data.Items)
            {
                var def = new ItemDef
                {
                    Id = itemData.Id,
                    Name = itemData.Name,
                    Description = itemData.Description,
                    Category = Enum.TryParse<ItemCategory>(itemData.Category, true, out var cat) 
                        ? cat : ItemCategory.Cargo,
                    EquipSlot = Enum.TryParse<EquipSlot>(itemData.EquipSlot, true, out var slot) 
                        ? slot : EquipSlot.None,
                    Volume = itemData.Volume,
                    BaseValue = itemData.BaseValue,
                    Tags = itemData.Tags ?? new List<string>(),
                    Stats = itemData.Stats ?? new Dictionary<string, int>(),
                    UseEffect = itemData.UseEffect,
                    UseAmount = itemData.UseAmount,
                    ModuleSlotType = itemData.ModuleSlotType,
                    CargoBonus = itemData.CargoBonus,
                    FuelEfficiency = itemData.FuelEfficiency
                };
                items[def.Id] = def;
            }

            SimLog.Log($"[ItemRegistry] Loaded {items.Count} items from JSON");
            return true;
        }
        catch (Exception ex)
        {
            SimLog.Log($"[ItemRegistry] Failed to load JSON: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Get an item definition by ID.
    /// </summary>
    public static ItemDef Get(string id)
    {
        EnsureInitialized();
        return items.TryGetValue(id, out var item) ? item : null;
    }

    /// <summary>
    /// Check if an item definition exists.
    /// </summary>
    public static bool Has(string id)
    {
        EnsureInitialized();
        return items.ContainsKey(id);
    }

    /// <summary>
    /// Get all item definitions.
    /// </summary>
    public static IEnumerable<ItemDef> GetAll()
    {
        EnsureInitialized();
        return items.Values;
    }

    /// <summary>
    /// Get all items of a specific category.
    /// </summary>
    public static IEnumerable<ItemDef> GetByCategory(ItemCategory category)
    {
        EnsureInitialized();
        foreach (var item in items.Values)
        {
            if (item.Category == category) yield return item;
        }
    }

    /// <summary>
    /// Get all items with a specific tag.
    /// </summary>
    public static IEnumerable<ItemDef> GetByTag(string tag)
    {
        EnsureInitialized();
        foreach (var item in items.Values)
        {
            if (item.HasTag(tag)) yield return item;
        }
    }

    /// <summary>
    /// Register a custom item definition.
    /// </summary>
    public static void Register(ItemDef item)
    {
        EnsureInitialized();
        items[item.Id] = item;
    }

    /// <summary>
    /// Reset registry (for testing).
    /// </summary>
    public static void Reset()
    {
        items.Clear();
        initialized = false;
    }

    private static void RegisterDefaultItems()
    {
        // === Equipment: Weapons ===
        items["rifle"] = new ItemDef
        {
            Id = "rifle",
            Name = "Assault Rifle",
            Description = "Standard-issue automatic rifle. Reliable and versatile.",
            Category = ItemCategory.Equipment,
            EquipSlot = EquipSlot.Weapon,
            Volume = 0,
            BaseValue = 100,
            Stats = new() { { "damage", 25 }, { "range", 8 }, { "accuracy", 70 } },
            Tags = new() { "weapon", "ballistic" }
        };

        items["pistol"] = new ItemDef
        {
            Id = "pistol",
            Name = "Sidearm",
            Description = "Compact backup weapon. Quick to draw.",
            Category = ItemCategory.Equipment,
            EquipSlot = EquipSlot.Weapon,
            Volume = 0,
            BaseValue = 50,
            Stats = new() { { "damage", 15 }, { "range", 5 }, { "accuracy", 80 } },
            Tags = new() { "weapon", "ballistic", "sidearm" }
        };

        items["shotgun"] = new ItemDef
        {
            Id = "shotgun",
            Name = "Combat Shotgun",
            Description = "Devastating at close range. Spread pattern.",
            Category = ItemCategory.Equipment,
            EquipSlot = EquipSlot.Weapon,
            Volume = 0,
            BaseValue = 120,
            Stats = new() { { "damage", 40 }, { "range", 4 }, { "accuracy", 60 } },
            Tags = new() { "weapon", "ballistic", "spread" }
        };

        items["sniper"] = new ItemDef
        {
            Id = "sniper",
            Name = "Sniper Rifle",
            Description = "Long-range precision weapon. One shot, one kill.",
            Category = ItemCategory.Equipment,
            EquipSlot = EquipSlot.Weapon,
            Volume = 0,
            BaseValue = 200,
            Stats = new() { { "damage", 50 }, { "range", 12 }, { "accuracy", 85 } },
            Tags = new() { "weapon", "ballistic", "precision" }
        };

        items["smg"] = new ItemDef
        {
            Id = "smg",
            Name = "Submachine Gun",
            Description = "High rate of fire, low damage per shot.",
            Category = ItemCategory.Equipment,
            EquipSlot = EquipSlot.Weapon,
            Volume = 0,
            BaseValue = 80,
            Stats = new() { { "damage", 18 }, { "range", 6 }, { "accuracy", 65 } },
            Tags = new() { "weapon", "ballistic", "automatic" }
        };

        // === Equipment: Armor ===
        items["light_armor"] = new ItemDef
        {
            Id = "light_armor",
            Name = "Light Armor",
            Description = "Basic protection without mobility penalty.",
            Category = ItemCategory.Equipment,
            EquipSlot = EquipSlot.Armor,
            Volume = 0,
            BaseValue = 80,
            Stats = new() { { "armor", 10 } },
            Tags = new() { "armor", "light" }
        };

        items["medium_armor"] = new ItemDef
        {
            Id = "medium_armor",
            Name = "Medium Armor",
            Description = "Balanced protection and mobility.",
            Category = ItemCategory.Equipment,
            EquipSlot = EquipSlot.Armor,
            Volume = 0,
            BaseValue = 120,
            Stats = new() { { "armor", 18 } },
            Tags = new() { "armor", "medium" }
        };

        items["heavy_armor"] = new ItemDef
        {
            Id = "heavy_armor",
            Name = "Heavy Armor",
            Description = "Maximum protection at the cost of speed.",
            Category = ItemCategory.Equipment,
            EquipSlot = EquipSlot.Armor,
            Volume = 0,
            BaseValue = 180,
            Stats = new() { { "armor", 25 }, { "speed_penalty", -1 } },
            Tags = new() { "armor", "heavy" }
        };

        // === Equipment: Gadgets ===
        items["medkit_gadget"] = new ItemDef
        {
            Id = "medkit_gadget",
            Name = "Field Medkit",
            Description = "Allows healing allies in combat.",
            Category = ItemCategory.Equipment,
            EquipSlot = EquipSlot.Gadget,
            Volume = 0,
            BaseValue = 60,
            Stats = new() { { "heal_amount", 30 } },
            Tags = new() { "gadget", "medical" }
        };

        items["scanner"] = new ItemDef
        {
            Id = "scanner",
            Name = "Tactical Scanner",
            Description = "Reveals enemy positions and weaknesses.",
            Category = ItemCategory.Equipment,
            EquipSlot = EquipSlot.Gadget,
            Volume = 0,
            BaseValue = 100,
            Stats = new() { { "scan_range", 10 } },
            Tags = new() { "gadget", "tech" }
        };

        // === Consumables ===
        items["medkit"] = new ItemDef
        {
            Id = "medkit",
            Name = "Medical Kit",
            Description = "Heals one injury when used at base.",
            Category = ItemCategory.Consumable,
            Volume = 1,
            BaseValue = 25,
            UseEffect = "heal_injury",
            UseAmount = 1,
            Tags = new() { "medical", "consumable" }
        };

        items["repair_kit"] = new ItemDef
        {
            Id = "repair_kit",
            Name = "Repair Kit",
            Description = "Repairs ship hull damage.",
            Category = ItemCategory.Consumable,
            Volume = 2,
            BaseValue = 40,
            UseEffect = "repair_hull",
            UseAmount = 20,
            Tags = new() { "mechanical", "consumable" }
        };

        items["stim_pack"] = new ItemDef
        {
            Id = "stim_pack",
            Name = "Stim Pack",
            Description = "Temporary combat enhancement.",
            Category = ItemCategory.Consumable,
            Volume = 1,
            BaseValue = 35,
            UseEffect = "combat_boost",
            UseAmount = 1,
            Tags = new() { "medical", "consumable", "combat" }
        };

        // === Cargo: Trade Goods ===
        items["medical_supplies"] = new ItemDef
        {
            Id = "medical_supplies",
            Name = "Medical Supplies",
            Description = "Bulk medical equipment and pharmaceuticals.",
            Category = ItemCategory.Cargo,
            Volume = 5,
            BaseValue = 100,
            Tags = new() { "medical", "legal", "trade" }
        };

        items["luxury_goods"] = new ItemDef
        {
            Id = "luxury_goods",
            Name = "Luxury Goods",
            Description = "High-end consumer products.",
            Category = ItemCategory.Cargo,
            Volume = 3,
            BaseValue = 200,
            Tags = new() { "luxury", "legal", "trade" }
        };

        items["contraband"] = new ItemDef
        {
            Id = "contraband",
            Name = "Contraband",
            Description = "Illegal goods. High profit, high risk.",
            Category = ItemCategory.Cargo,
            Volume = 2,
            BaseValue = 300,
            Tags = new() { "illegal", "trade" }
        };

        items["weapons_cache"] = new ItemDef
        {
            Id = "weapons_cache",
            Name = "Weapons Cache",
            Description = "Military-grade weapons shipment.",
            Category = ItemCategory.Cargo,
            Volume = 10,
            BaseValue = 500,
            Tags = new() { "restricted", "military", "trade" }
        };

        items["fuel_cells"] = new ItemDef
        {
            Id = "fuel_cells",
            Name = "Fuel Cells",
            Description = "Portable fuel storage units.",
            Category = ItemCategory.Cargo,
            Volume = 4,
            BaseValue = 80,
            Tags = new() { "fuel", "legal", "trade" }
        };

        items["electronics"] = new ItemDef
        {
            Id = "electronics",
            Name = "Electronics",
            Description = "Computer components and circuits.",
            Category = ItemCategory.Cargo,
            Volume = 2,
            BaseValue = 150,
            Tags = new() { "tech", "legal", "trade" }
        };

        items["raw_ore"] = new ItemDef
        {
            Id = "raw_ore",
            Name = "Raw Ore",
            Description = "Unprocessed mineral ore. Heavy but valuable.",
            Category = ItemCategory.Cargo,
            Volume = 8,
            BaseValue = 60,
            Tags = new() { "mining", "legal", "trade", "heavy" }
        };

        // === Ship Modules ===
        items["basic_engine"] = new ItemDef
        {
            Id = "basic_engine",
            Name = "Basic Engine",
            Description = "Standard propulsion system.",
            Category = ItemCategory.Module,
            ModuleSlotType = "Engine",
            Volume = 0,
            BaseValue = 100,
            Tags = new() { "module", "engine" }
        };

        items["efficient_engine"] = new ItemDef
        {
            Id = "efficient_engine",
            Name = "Efficient Engine",
            Description = "Improved fuel economy.",
            Category = ItemCategory.Module,
            ModuleSlotType = "Engine",
            FuelEfficiency = 20,
            Volume = 0,
            BaseValue = 300,
            Tags = new() { "module", "engine" }
        };

        items["fast_engine"] = new ItemDef
        {
            Id = "fast_engine",
            Name = "Fast Engine",
            Description = "Increased travel speed.",
            Category = ItemCategory.Module,
            ModuleSlotType = "Engine",
            Volume = 0,
            BaseValue = 400,
            Stats = new() { { "speed_bonus", 25 } },
            Tags = new() { "module", "engine" }
        };

        items["small_cargo"] = new ItemDef
        {
            Id = "small_cargo",
            Name = "Small Cargo Pod",
            Description = "Adds 20 cargo capacity.",
            Category = ItemCategory.Module,
            ModuleSlotType = "Cargo",
            CargoBonus = 20,
            Volume = 0,
            BaseValue = 150,
            Tags = new() { "module", "cargo" }
        };

        items["large_cargo"] = new ItemDef
        {
            Id = "large_cargo",
            Name = "Large Cargo Pod",
            Description = "Adds 50 cargo capacity.",
            Category = ItemCategory.Module,
            ModuleSlotType = "Cargo",
            CargoBonus = 50,
            Volume = 0,
            BaseValue = 400,
            Tags = new() { "module", "cargo" }
        };

        items["point_defense"] = new ItemDef
        {
            Id = "point_defense",
            Name = "Point Defense",
            Description = "Automated defense against missiles and fighters.",
            Category = ItemCategory.Module,
            ModuleSlotType = "Weapon",
            Volume = 0,
            BaseValue = 250,
            Stats = new() { { "defense_rating", 15 } },
            Tags = new() { "module", "weapon", "defensive" }
        };

        items["railgun"] = new ItemDef
        {
            Id = "railgun",
            Name = "Railgun",
            Description = "High-velocity kinetic weapon.",
            Category = ItemCategory.Module,
            ModuleSlotType = "Weapon",
            Volume = 0,
            BaseValue = 500,
            Stats = new() { { "damage", 40 }, { "range", 15 } },
            Tags = new() { "module", "weapon", "offensive" }
        };

        items["shield_generator"] = new ItemDef
        {
            Id = "shield_generator",
            Name = "Shield Generator",
            Description = "Provides energy shielding.",
            Category = ItemCategory.Module,
            ModuleSlotType = "Utility",
            Volume = 0,
            BaseValue = 350,
            Stats = new() { { "shield_hp", 30 } },
            Tags = new() { "module", "utility", "defensive" }
        };

        items["sensor_array"] = new ItemDef
        {
            Id = "sensor_array",
            Name = "Sensor Array",
            Description = "Enhanced detection and scanning.",
            Category = ItemCategory.Module,
            ModuleSlotType = "Utility",
            Volume = 0,
            BaseValue = 200,
            Stats = new() { { "sensor_range", 50 } },
            Tags = new() { "module", "utility" }
        };
    }
}

/// <summary>
/// JSON data structure for items file.
/// </summary>
internal class ItemsJsonData
{
    public List<ItemJsonEntry> Items { get; set; }
}

/// <summary>
/// JSON data structure for a single item.
/// </summary>
internal class ItemJsonEntry
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Category { get; set; }
    public string EquipSlot { get; set; }
    public int Volume { get; set; }
    public int BaseValue { get; set; }
    public List<string> Tags { get; set; }
    public Dictionary<string, int> Stats { get; set; }
    public string UseEffect { get; set; }
    public int UseAmount { get; set; }
    public string ModuleSlotType { get; set; }
    public int CargoBonus { get; set; }
    public int FuelEfficiency { get; set; }
}
