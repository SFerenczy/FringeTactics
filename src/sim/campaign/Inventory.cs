using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// Manages item storage with capacity constraints.
/// </summary>
public class Inventory
{
    public List<Item> Items { get; set; } = new();
    private int nextItemId = 0;

    /// <summary>
    /// Calculate total volume of all items.
    /// </summary>
    public int GetUsedVolume()
    {
        int total = 0;
        foreach (var item in Items)
        {
            total += item.GetTotalVolume();
        }
        return total;
    }

    /// <summary>
    /// Check if an item can be added given capacity.
    /// </summary>
    public bool CanAdd(string defId, int quantity, int capacity)
    {
        if (quantity <= 0) return false;

        var def = ItemRegistry.Get(defId);
        if (def == null) return false;

        int volumeNeeded = def.Volume * quantity;
        return GetUsedVolume() + volumeNeeded <= capacity;
    }

    /// <summary>
    /// Add an item to inventory. Returns the item instance or null if failed.
    /// </summary>
    public Item AddItem(string defId, int quantity, int capacity)
    {
        if (quantity <= 0) return null;
        if (!CanAdd(defId, quantity, capacity)) return null;

        var def = ItemRegistry.Get(defId);
        if (def == null) return null;

        // Stackable items (cargo, consumables) merge with existing stacks
        if (def.IsStackable)
        {
            var existing = FindByDefId(defId);
            if (existing != null)
            {
                existing.Quantity += quantity;
                return existing;
            }
        }

        var item = new Item
        {
            Id = $"item_{nextItemId++}",
            DefId = defId,
            Quantity = quantity
        };
        Items.Add(item);
        return item;
    }

    /// <summary>
    /// Remove an item by instance ID.
    /// </summary>
    public bool RemoveItem(string itemId)
    {
        for (int i = 0; i < Items.Count; i++)
        {
            if (Items[i].Id == itemId)
            {
                Items.RemoveAt(i);
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Remove quantity of an item by definition ID.
    /// </summary>
    public bool RemoveByDefId(string defId, int quantity)
    {
        if (quantity <= 0) return false;

        var item = FindByDefId(defId);
        if (item == null || item.Quantity < quantity) return false;

        item.Quantity -= quantity;
        if (item.Quantity <= 0)
        {
            RemoveItem(item.Id);
        }
        return true;
    }

    /// <summary>
    /// Find an item by definition ID.
    /// </summary>
    public Item FindByDefId(string defId)
    {
        foreach (var item in Items)
        {
            if (item.DefId == defId) return item;
        }
        return null;
    }

    /// <summary>
    /// Find an item by instance ID.
    /// </summary>
    public Item FindById(string itemId)
    {
        foreach (var item in Items)
        {
            if (item.Id == itemId) return item;
        }
        return null;
    }

    /// <summary>
    /// Get all items of a category.
    /// </summary>
    public List<Item> GetByCategory(ItemCategory category)
    {
        var result = new List<Item>();
        foreach (var item in Items)
        {
            var def = item.GetDef();
            if (def != null && def.Category == category)
            {
                result.Add(item);
            }
        }
        return result;
    }

    /// <summary>
    /// Get all items with a specific tag.
    /// </summary>
    public List<Item> GetByTag(string tag)
    {
        var result = new List<Item>();
        foreach (var item in Items)
        {
            var def = item.GetDef();
            if (def != null && def.HasTag(tag))
            {
                result.Add(item);
            }
        }
        return result;
    }

    /// <summary>
    /// Count total quantity of an item by definition ID.
    /// </summary>
    public int CountByDefId(string defId)
    {
        var item = FindByDefId(defId);
        return item?.Quantity ?? 0;
    }

    /// <summary>
    /// Check if inventory contains at least the specified quantity.
    /// </summary>
    public bool HasItem(string defId, int quantity = 1)
    {
        return CountByDefId(defId) >= quantity;
    }

    /// <summary>
    /// Get total value of all items in inventory.
    /// </summary>
    public int GetTotalValue()
    {
        int total = 0;
        foreach (var item in Items)
        {
            total += item.GetTotalValue();
        }
        return total;
    }

    /// <summary>
    /// Clear all items from inventory.
    /// </summary>
    public void Clear()
    {
        Items.Clear();
    }

    // === Serialization ===

    public InventoryData GetState()
    {
        var data = new InventoryData { NextItemId = nextItemId };
        foreach (var item in Items)
        {
            data.Items.Add(new ItemData
            {
                Id = item.Id,
                DefId = item.DefId,
                Quantity = item.Quantity
            });
        }
        return data;
    }

    public static Inventory FromState(InventoryData data)
    {
        var inv = new Inventory();
        if (data == null) return inv;

        inv.nextItemId = data.NextItemId;
        foreach (var itemData in data.Items ?? new List<ItemData>())
        {
            inv.Items.Add(new Item
            {
                Id = itemData.Id,
                DefId = itemData.DefId,
                Quantity = itemData.Quantity
            });
        }
        return inv;
    }
}
