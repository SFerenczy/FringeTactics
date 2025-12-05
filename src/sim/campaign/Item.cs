using System.Collections.Generic;

namespace FringeTactics;

public enum ItemCategory
{
    Equipment,   // Weapons, armor, gadgets (equippable by crew)
    Consumable,  // Meds, repair kits (used from inventory)
    Cargo,       // Trade goods (bulk items with volume)
    Module       // Ship modules (installed on ship)
}

public enum EquipSlot
{
    None,
    Weapon,
    Armor,
    Gadget
}

/// <summary>
/// Definition of an item type (data-driven).
/// </summary>
public class ItemDef
{
    /// <summary>
    /// Unique identifier for this item type.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Display name.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Description text.
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Item category determines behavior and stacking.
    /// </summary>
    public ItemCategory Category { get; set; }

    /// <summary>
    /// Cargo space required per unit. 0 for equipped items.
    /// </summary>
    public int Volume { get; set; } = 1;

    /// <summary>
    /// Base price in credits.
    /// </summary>
    public int BaseValue { get; set; } = 0;

    /// <summary>
    /// Tags for filtering and special behavior (e.g., "illegal", "medical", "perishable").
    /// </summary>
    public List<string> Tags { get; set; } = new();

    // === Equipment Properties ===

    /// <summary>
    /// Equipment slot for equippable items. None for non-equipment.
    /// </summary>
    public EquipSlot EquipSlot { get; set; } = EquipSlot.None;

    /// <summary>
    /// Stat modifiers for equipment (e.g., "damage" -> 25, "armor" -> 10).
    /// </summary>
    public Dictionary<string, int> Stats { get; set; } = new();

    // === Consumable Properties ===

    /// <summary>
    /// Effect when used: "heal_injury", "repair_hull", etc.
    /// </summary>
    public string UseEffect { get; set; }

    /// <summary>
    /// Amount for the use effect.
    /// </summary>
    public int UseAmount { get; set; } = 0;

    // === Module Properties ===

    /// <summary>
    /// Ship slot type for modules: "Engine", "Weapon", "Cargo", "Utility".
    /// </summary>
    public string ModuleSlotType { get; set; }

    /// <summary>
    /// Cargo capacity bonus for cargo modules.
    /// </summary>
    public int CargoBonus { get; set; } = 0;

    /// <summary>
    /// Fuel efficiency bonus percentage for engine modules.
    /// </summary>
    public int FuelEfficiency { get; set; } = 0;

    /// <summary>
    /// Check if this item has a specific tag.
    /// </summary>
    public bool HasTag(string tag) => Tags?.Contains(tag) ?? false;

    /// <summary>
    /// Check if this item is stackable (consumables and cargo stack).
    /// </summary>
    public bool IsStackable => Category == ItemCategory.Consumable || Category == ItemCategory.Cargo;

    /// <summary>
    /// Get a stat value, or default if not present.
    /// </summary>
    public int GetStat(string statName, int defaultValue = 0)
    {
        return Stats != null && Stats.TryGetValue(statName, out var value) ? value : defaultValue;
    }
}

/// <summary>
/// An instance of an item in inventory.
/// </summary>
public class Item
{
    /// <summary>
    /// Unique instance ID for this item stack.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Definition ID for lookup.
    /// </summary>
    public string DefId { get; set; }

    /// <summary>
    /// Stack count (for stackable items).
    /// </summary>
    public int Quantity { get; set; } = 1;

    /// <summary>
    /// Get the item definition from registry.
    /// </summary>
    public ItemDef GetDef() => ItemRegistry.Get(DefId);

    /// <summary>
    /// Get total volume of this item stack.
    /// </summary>
    public int GetTotalVolume()
    {
        var def = GetDef();
        return def != null ? def.Volume * Quantity : 0;
    }

    /// <summary>
    /// Get total value of this item stack.
    /// </summary>
    public int GetTotalValue()
    {
        var def = GetDef();
        return def != null ? def.BaseValue * Quantity : 0;
    }

    /// <summary>
    /// Get display name from definition.
    /// </summary>
    public string GetName() => GetDef()?.Name ?? DefId;
}
