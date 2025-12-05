using System.Linq;
using GdUnit4;
using static GdUnit4.Assertions;

namespace FringeTactics.Tests;

[TestSuite]
public class MG2InventoryTests
{
    [BeforeTest]
    public void Setup()
    {
        ItemRegistry.Reset();
    }

    // === ItemRegistry Tests ===

    [TestCase]
    public void ItemRegistry_Has_ReturnsTrueForExistingItem()
    {
        AssertThat(ItemRegistry.Has("rifle")).IsTrue();
        AssertThat(ItemRegistry.Has("medkit")).IsTrue();
        AssertThat(ItemRegistry.Has("small_cargo")).IsTrue();
    }

    [TestCase]
    public void ItemRegistry_Has_ReturnsFalseForUnknownItem()
    {
        AssertThat(ItemRegistry.Has("nonexistent")).IsFalse();
    }

    [TestCase]
    public void ItemRegistry_Get_ReturnsItemDef()
    {
        var rifle = ItemRegistry.Get("rifle");

        AssertThat(rifle).IsNotNull();
        AssertThat(rifle.Id).IsEqual("rifle");
        AssertThat(rifle.Name).IsEqual("Assault Rifle");
        AssertThat(rifle.Category).IsEqual(ItemCategory.Equipment);
    }

    [TestCase]
    public void ItemRegistry_Get_ReturnsNullForUnknown()
    {
        var item = ItemRegistry.Get("nonexistent");
        AssertThat(item).IsNull();
    }

    [TestCase]
    public void ItemRegistry_GetByCategory_ReturnsCorrectItems()
    {
        var equipment = ItemRegistry.GetByCategory(ItemCategory.Equipment).ToList();
        var consumables = ItemRegistry.GetByCategory(ItemCategory.Consumable).ToList();
        var cargo = ItemRegistry.GetByCategory(ItemCategory.Cargo).ToList();
        var modules = ItemRegistry.GetByCategory(ItemCategory.Module).ToList();

        AssertThat(equipment.Count).IsGreater(0);
        AssertThat(consumables.Count).IsGreater(0);
        AssertThat(cargo.Count).IsGreater(0);
        AssertThat(modules.Count).IsGreater(0);

        // Verify all items in category are correct
        foreach (var item in equipment)
        {
            AssertThat(item.Category).IsEqual(ItemCategory.Equipment);
        }
    }

    [TestCase]
    public void ItemRegistry_GetByTag_ReturnsCorrectItems()
    {
        var weapons = ItemRegistry.GetByTag("weapon").ToList();
        var medical = ItemRegistry.GetByTag("medical").ToList();
        var illegal = ItemRegistry.GetByTag("illegal").ToList();

        AssertThat(weapons.Count).IsGreater(0);
        AssertThat(medical.Count).IsGreater(0);
        AssertThat(illegal.Count).IsGreater(0);

        // Verify contraband is in illegal
        AssertThat(illegal.Any(i => i.Id == "contraband")).IsTrue();
    }

    // === ItemDef Tests ===

    [TestCase]
    public void ItemDef_HasTag_Works()
    {
        var contraband = ItemRegistry.Get("contraband");

        AssertThat(contraband.HasTag("illegal")).IsTrue();
        AssertThat(contraband.HasTag("trade")).IsTrue();
        AssertThat(contraband.HasTag("legal")).IsFalse();
    }

    [TestCase]
    public void ItemDef_IsStackable_TrueForCargoAndConsumables()
    {
        var medkit = ItemRegistry.Get("medkit");
        var cargo = ItemRegistry.Get("medical_supplies");
        var rifle = ItemRegistry.Get("rifle");
        var module = ItemRegistry.Get("basic_engine");

        AssertThat(medkit.IsStackable).IsTrue();
        AssertThat(cargo.IsStackable).IsTrue();
        AssertThat(rifle.IsStackable).IsFalse();
        AssertThat(module.IsStackable).IsFalse();
    }

    [TestCase]
    public void ItemDef_GetStat_ReturnsValue()
    {
        var rifle = ItemRegistry.Get("rifle");

        AssertThat(rifle.GetStat("damage")).IsEqual(25);
        AssertThat(rifle.GetStat("range")).IsEqual(8);
        AssertThat(rifle.GetStat("nonexistent", 99)).IsEqual(99);
    }

    [TestCase]
    public void ItemDef_EquipmentHasCorrectSlots()
    {
        AssertThat(ItemRegistry.Get("rifle").EquipSlot).IsEqual(EquipSlot.Weapon);
        AssertThat(ItemRegistry.Get("light_armor").EquipSlot).IsEqual(EquipSlot.Armor);
        AssertThat(ItemRegistry.Get("scanner").EquipSlot).IsEqual(EquipSlot.Gadget);
    }

    [TestCase]
    public void ItemDef_ModulesHaveCorrectSlotTypes()
    {
        AssertThat(ItemRegistry.Get("basic_engine").ModuleSlotType).IsEqual("Engine");
        AssertThat(ItemRegistry.Get("small_cargo").ModuleSlotType).IsEqual("Cargo");
        AssertThat(ItemRegistry.Get("point_defense").ModuleSlotType).IsEqual("Weapon");
        AssertThat(ItemRegistry.Get("shield_generator").ModuleSlotType).IsEqual("Utility");
    }

    // === Inventory Add Tests ===

    [TestCase]
    public void Inventory_AddItem_Works()
    {
        var inv = new Inventory();

        var item = inv.AddItem("medkit", 1, 100);

        AssertThat(item).IsNotNull();
        AssertThat(inv.Items.Count).IsEqual(1);
        AssertThat(item.DefId).IsEqual("medkit");
        AssertThat(item.Quantity).IsEqual(1);
    }

    [TestCase]
    public void Inventory_AddItem_RespectsCapacity()
    {
        var inv = new Inventory();
        // medkit has volume 1, try to add 10 with capacity 5
        var item = inv.AddItem("medkit", 10, 5);

        AssertThat(item).IsNull();
        AssertThat(inv.Items.Count).IsEqual(0);
    }

    [TestCase]
    public void Inventory_AddItem_StacksConsumables()
    {
        var inv = new Inventory();

        inv.AddItem("medkit", 2, 100);
        inv.AddItem("medkit", 3, 100);

        AssertThat(inv.Items.Count).IsEqual(1);
        AssertThat(inv.Items[0].Quantity).IsEqual(5);
    }

    [TestCase]
    public void Inventory_AddItem_StacksCargo()
    {
        var inv = new Inventory();

        inv.AddItem("fuel_cells", 2, 100);
        inv.AddItem("fuel_cells", 1, 100);

        AssertThat(inv.Items.Count).IsEqual(1);
        AssertThat(inv.Items[0].Quantity).IsEqual(3);
    }

    [TestCase]
    public void Inventory_AddItem_DoesNotStackEquipment()
    {
        var inv = new Inventory();

        inv.AddItem("rifle", 1, 100);
        inv.AddItem("rifle", 1, 100);

        AssertThat(inv.Items.Count).IsEqual(2);
    }

    [TestCase]
    public void Inventory_AddItem_FailsForUnknownItem()
    {
        var inv = new Inventory();

        var item = inv.AddItem("nonexistent", 1, 100);

        AssertThat(item).IsNull();
    }

    [TestCase]
    public void Inventory_AddItem_FailsForZeroQuantity()
    {
        var inv = new Inventory();

        var item = inv.AddItem("medkit", 0, 100);

        AssertThat(item).IsNull();
    }

    // === Inventory Volume Tests ===

    [TestCase]
    public void Inventory_GetUsedVolume_Correct()
    {
        var inv = new Inventory();

        inv.AddItem("medkit", 5, 100);      // 5 * 1 = 5
        inv.AddItem("repair_kit", 2, 100);  // 2 * 2 = 4

        AssertThat(inv.GetUsedVolume()).IsEqual(9);
    }

    [TestCase]
    public void Inventory_GetUsedVolume_EquipmentIsZero()
    {
        var inv = new Inventory();

        inv.AddItem("rifle", 1, 100);
        inv.AddItem("light_armor", 1, 100);

        AssertThat(inv.GetUsedVolume()).IsEqual(0);
    }

    [TestCase]
    public void Inventory_CanAdd_ChecksCapacity()
    {
        var inv = new Inventory();
        inv.AddItem("medkit", 5, 100); // Uses 5 volume

        AssertThat(inv.CanAdd("medkit", 90, 100)).IsTrue();  // 5 + 90 = 95 <= 100
        AssertThat(inv.CanAdd("medkit", 96, 100)).IsFalse(); // 5 + 96 = 101 > 100
    }

    // === Inventory Remove Tests ===

    [TestCase]
    public void Inventory_RemoveItem_Works()
    {
        var inv = new Inventory();
        var item = inv.AddItem("rifle", 1, 100);

        var result = inv.RemoveItem(item.Id);

        AssertThat(result).IsTrue();
        AssertThat(inv.Items.Count).IsEqual(0);
    }

    [TestCase]
    public void Inventory_RemoveItem_FailsForUnknownId()
    {
        var inv = new Inventory();
        inv.AddItem("rifle", 1, 100);

        var result = inv.RemoveItem("nonexistent");

        AssertThat(result).IsFalse();
        AssertThat(inv.Items.Count).IsEqual(1);
    }

    [TestCase]
    public void Inventory_RemoveByDefId_Works()
    {
        var inv = new Inventory();
        inv.AddItem("medkit", 5, 100);

        var result = inv.RemoveByDefId("medkit", 3);

        AssertThat(result).IsTrue();
        AssertThat(inv.CountByDefId("medkit")).IsEqual(2);
    }

    [TestCase]
    public void Inventory_RemoveByDefId_FailsIfInsufficient()
    {
        var inv = new Inventory();
        inv.AddItem("medkit", 2, 100);

        var result = inv.RemoveByDefId("medkit", 5);

        AssertThat(result).IsFalse();
        AssertThat(inv.CountByDefId("medkit")).IsEqual(2);
    }

    [TestCase]
    public void Inventory_RemoveByDefId_RemovesItemAtZero()
    {
        var inv = new Inventory();
        inv.AddItem("medkit", 2, 100);

        inv.RemoveByDefId("medkit", 2);

        AssertThat(inv.Items.Count).IsEqual(0);
    }

    [TestCase]
    public void Inventory_RemoveByDefId_FailsForZeroQuantity()
    {
        var inv = new Inventory();
        inv.AddItem("medkit", 5, 100);

        var result = inv.RemoveByDefId("medkit", 0);

        AssertThat(result).IsFalse();
        AssertThat(inv.CountByDefId("medkit")).IsEqual(5);
    }

    // === Inventory Find Tests ===

    [TestCase]
    public void Inventory_FindByDefId_Works()
    {
        var inv = new Inventory();
        inv.AddItem("medkit", 3, 100);

        var item = inv.FindByDefId("medkit");

        AssertThat(item).IsNotNull();
        AssertThat(item.Quantity).IsEqual(3);
    }

    [TestCase]
    public void Inventory_FindByDefId_ReturnsNullIfNotFound()
    {
        var inv = new Inventory();

        var item = inv.FindByDefId("medkit");

        AssertThat(item).IsNull();
    }

    [TestCase]
    public void Inventory_FindById_Works()
    {
        var inv = new Inventory();
        var added = inv.AddItem("rifle", 1, 100);

        var found = inv.FindById(added.Id);

        AssertThat(found).IsNotNull();
        AssertThat(found.Id).IsEqual(added.Id);
    }

    [TestCase]
    public void Inventory_HasItem_Works()
    {
        var inv = new Inventory();
        inv.AddItem("medkit", 5, 100);

        AssertThat(inv.HasItem("medkit")).IsTrue();
        AssertThat(inv.HasItem("medkit", 5)).IsTrue();
        AssertThat(inv.HasItem("medkit", 6)).IsFalse();
        AssertThat(inv.HasItem("rifle")).IsFalse();
    }

    // === Inventory Category/Tag Tests ===

    [TestCase]
    public void Inventory_GetByCategory_Works()
    {
        var inv = new Inventory();
        inv.AddItem("rifle", 1, 100);
        inv.AddItem("medkit", 3, 100);
        inv.AddItem("fuel_cells", 2, 100);

        var equipment = inv.GetByCategory(ItemCategory.Equipment);
        var consumables = inv.GetByCategory(ItemCategory.Consumable);
        var cargo = inv.GetByCategory(ItemCategory.Cargo);

        AssertThat(equipment.Count).IsEqual(1);
        AssertThat(consumables.Count).IsEqual(1);
        AssertThat(cargo.Count).IsEqual(1);
    }

    [TestCase]
    public void Inventory_GetByTag_Works()
    {
        var inv = new Inventory();
        inv.AddItem("medkit", 3, 100);
        inv.AddItem("medical_supplies", 1, 100);

        var medical = inv.GetByTag("medical");

        AssertThat(medical.Count).IsEqual(2);
    }

    // === Inventory Value Tests ===

    [TestCase]
    public void Inventory_GetTotalValue_Works()
    {
        var inv = new Inventory();
        inv.AddItem("medkit", 2, 100);  // 2 * 25 = 50
        inv.AddItem("rifle", 1, 100);   // 1 * 100 = 100

        AssertThat(inv.GetTotalValue()).IsEqual(150);
    }

    // === Item Instance Tests ===

    [TestCase]
    public void Item_GetDef_ReturnsDefinition()
    {
        var item = new Item { DefId = "rifle", Quantity = 1 };

        var def = item.GetDef();

        AssertThat(def).IsNotNull();
        AssertThat(def.Id).IsEqual("rifle");
    }

    [TestCase]
    public void Item_GetTotalVolume_CalculatesCorrectly()
    {
        var item = new Item { DefId = "medkit", Quantity = 5 };

        AssertThat(item.GetTotalVolume()).IsEqual(5); // 5 * 1
    }

    [TestCase]
    public void Item_GetTotalValue_CalculatesCorrectly()
    {
        var item = new Item { DefId = "medkit", Quantity = 4 };

        AssertThat(item.GetTotalValue()).IsEqual(100); // 4 * 25
    }

    [TestCase]
    public void Item_GetName_ReturnsDefName()
    {
        var item = new Item { DefId = "rifle", Quantity = 1 };

        AssertThat(item.GetName()).IsEqual("Assault Rifle");
    }

    // === Serialization Tests ===

    [TestCase]
    public void Inventory_GetState_PreservesItems()
    {
        var inv = new Inventory();
        inv.AddItem("medkit", 5, 100);
        inv.AddItem("rifle", 1, 100);

        var data = inv.GetState();

        AssertThat(data.Items.Count).IsEqual(2);
    }

    [TestCase]
    public void Inventory_FromState_RestoresItems()
    {
        var original = new Inventory();
        original.AddItem("medkit", 5, 100);
        original.AddItem("rifle", 1, 100);

        var data = original.GetState();
        var restored = Inventory.FromState(data);

        AssertThat(restored.Items.Count).IsEqual(2);
        AssertThat(restored.CountByDefId("medkit")).IsEqual(5);
        AssertThat(restored.CountByDefId("rifle")).IsEqual(1);
    }

    [TestCase]
    public void Inventory_FromState_NullDataReturnsEmpty()
    {
        var inv = Inventory.FromState(null);

        AssertThat(inv).IsNotNull();
        AssertThat(inv.Items.Count).IsEqual(0);
    }

    [TestCase]
    public void Inventory_RoundTrip_PreservesItemIds()
    {
        var original = new Inventory();
        var item = original.AddItem("rifle", 1, 100);
        var originalId = item.Id;

        var data = original.GetState();
        var restored = Inventory.FromState(data);

        AssertThat(restored.FindById(originalId)).IsNotNull();
    }

    // === Clear Tests ===

    [TestCase]
    public void Inventory_Clear_RemovesAllItems()
    {
        var inv = new Inventory();
        inv.AddItem("medkit", 5, 100);
        inv.AddItem("rifle", 2, 100);

        inv.Clear();

        AssertThat(inv.Items.Count).IsEqual(0);
        AssertThat(inv.GetUsedVolume()).IsEqual(0);
    }
}
