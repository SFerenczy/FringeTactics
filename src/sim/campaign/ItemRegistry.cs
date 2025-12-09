using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace FringeTactics;

/// <summary>
/// Registry of all item definitions.
/// Loads exclusively from data/items.json.
/// </summary>
public static class ItemRegistry
{
    private static Dictionary<string, ItemDef> items = new();
    private static bool initialized = false;
    private static readonly string DataPath = "res://data/items.json";

    public static void EnsureInitialized()
    {
        if (initialized) return;
        
        LoadFromJson();
        initialized = true;
    }

    private static void LoadFromJson()
    {
        try
        {
            string jsonPath = Godot.ProjectSettings.GlobalizePath(DataPath);
            if (!File.Exists(jsonPath))
            {
                SimLog.Log($"[ItemRegistry] ERROR: {DataPath} not found!");
                return;
            }

            string json = File.ReadAllText(jsonPath);
            var data = JsonSerializer.Deserialize<ItemsJsonData>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (data?.Items == null)
            {
                SimLog.Log($"[ItemRegistry] ERROR: {DataPath} has no items!");
                return;
            }

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
        }
        catch (Exception ex)
        {
            SimLog.Log($"[ItemRegistry] ERROR loading JSON: {ex.Message}");
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
