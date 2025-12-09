using System.Collections.Generic;
using GdUnit4;
using static GdUnit4.Assertions;

namespace FringeTactics.Tests;

[TestSuite]
public class MG2ResourceTests
{
    private CampaignState campaign;
    private List<ResourceChangedEvent> resourceEvents;
    private List<ItemAddedEvent> itemAddedEvents;
    private List<ItemRemovedEvent> itemRemovedEvents;
    private List<ShipHullChangedEvent> shipHullEvents;
    private List<ShipModuleInstalledEvent> moduleInstalledEvents;
    private List<ShipModuleRemovedEvent> moduleRemovedEvents;

    [BeforeTest]
    public void Setup()
    {
        ItemRegistry.Reset();
        campaign = CampaignState.CreateNew();
        
        resourceEvents = new List<ResourceChangedEvent>();
        itemAddedEvents = new List<ItemAddedEvent>();
        itemRemovedEvents = new List<ItemRemovedEvent>();
        shipHullEvents = new List<ShipHullChangedEvent>();
        moduleInstalledEvents = new List<ShipModuleInstalledEvent>();
        moduleRemovedEvents = new List<ShipModuleRemovedEvent>();
        
        campaign.EventBus = new EventBus();
        campaign.EventBus.Subscribe<ResourceChangedEvent>(e => resourceEvents.Add(e));
        campaign.EventBus.Subscribe<ItemAddedEvent>(e => itemAddedEvents.Add(e));
        campaign.EventBus.Subscribe<ItemRemovedEvent>(e => itemRemovedEvents.Add(e));
        campaign.EventBus.Subscribe<ShipHullChangedEvent>(e => shipHullEvents.Add(e));
        campaign.EventBus.Subscribe<ShipModuleInstalledEvent>(e => moduleInstalledEvents.Add(e));
        campaign.EventBus.Subscribe<ShipModuleRemovedEvent>(e => moduleRemovedEvents.Add(e));
    }

    // ========================================================================
    // SPEND CREDITS TESTS
    // ========================================================================

    [TestCase]
    public void SpendCredits_Works()
    {
        campaign.Money = 100;

        var result = campaign.SpendCredits(30, "test");

        AssertThat(result).IsTrue();
        AssertThat(campaign.Money).IsEqual(70);
    }

    [TestCase]
    public void SpendCredits_FailsIfInsufficient()
    {
        campaign.Money = 20;

        var result = campaign.SpendCredits(50, "test");

        AssertThat(result).IsFalse();
        AssertThat(campaign.Money).IsEqual(20);
    }

    [TestCase]
    public void SpendCredits_FailsForZeroAmount()
    {
        campaign.Money = 100;

        var result = campaign.SpendCredits(0, "test");

        AssertThat(result).IsFalse();
        AssertThat(campaign.Money).IsEqual(100);
    }

    [TestCase]
    public void SpendCredits_EmitsEvent()
    {
        campaign.Money = 100;

        campaign.SpendCredits(30, "test_reason");

        AssertThat(resourceEvents.Count).IsEqual(1);
        AssertThat(resourceEvents[0].ResourceType).IsEqual(ResourceTypes.Money);
        AssertThat(resourceEvents[0].OldValue).IsEqual(100);
        AssertThat(resourceEvents[0].NewValue).IsEqual(70);
        AssertThat(resourceEvents[0].Delta).IsEqual(-30);
        AssertThat(resourceEvents[0].Reason).IsEqual("test_reason");
    }

    // ========================================================================
    // ADD CREDITS TESTS
    // ========================================================================

    [TestCase]
    public void AddCredits_Works()
    {
        campaign.Money = 100;

        campaign.AddCredits(50, "test");

        AssertThat(campaign.Money).IsEqual(150);
    }

    [TestCase]
    public void AddCredits_UpdatesTotalEarned()
    {
        int initialEarned = campaign.TotalMoneyEarned;

        campaign.AddCredits(100, "test");

        AssertThat(campaign.TotalMoneyEarned).IsEqual(initialEarned + 100);
    }

    [TestCase]
    public void AddCredits_IgnoresZeroAmount()
    {
        campaign.Money = 100;

        campaign.AddCredits(0, "test");

        AssertThat(campaign.Money).IsEqual(100);
        AssertThat(resourceEvents.Count).IsEqual(0);
    }

    [TestCase]
    public void AddCredits_EmitsEvent()
    {
        campaign.Money = 100;

        campaign.AddCredits(50, "reward");

        AssertThat(resourceEvents.Count).IsEqual(1);
        AssertThat(resourceEvents[0].Delta).IsEqual(50);
        AssertThat(resourceEvents[0].Reason).IsEqual("reward");
    }

    // ========================================================================
    // SPEND/ADD FUEL TESTS
    // ========================================================================

    [TestCase]
    public void SpendFuel_Works()
    {
        campaign.Fuel = 100;

        var result = campaign.SpendFuel(20, "travel");

        AssertThat(result).IsTrue();
        AssertThat(campaign.Fuel).IsEqual(80);
    }

    [TestCase]
    public void SpendFuel_FailsIfInsufficient()
    {
        campaign.Fuel = 10;

        var result = campaign.SpendFuel(50, "travel");

        AssertThat(result).IsFalse();
        AssertThat(campaign.Fuel).IsEqual(10);
    }

    [TestCase]
    public void AddFuel_Works()
    {
        campaign.Fuel = 50;

        campaign.AddFuel(30, "refuel");

        AssertThat(campaign.Fuel).IsEqual(80);
    }

    // ========================================================================
    // SPEND/ADD PARTS TESTS
    // ========================================================================

    [TestCase]
    public void SpendParts_Works()
    {
        campaign.Parts = 50;

        var result = campaign.SpendParts(20, "repair");

        AssertThat(result).IsTrue();
        AssertThat(campaign.Parts).IsEqual(30);
    }

    [TestCase]
    public void SpendParts_FailsIfInsufficient()
    {
        campaign.Parts = 10;

        var result = campaign.SpendParts(50, "repair");

        AssertThat(result).IsFalse();
        AssertThat(campaign.Parts).IsEqual(10);
    }

    [TestCase]
    public void AddParts_Works()
    {
        campaign.Parts = 20;

        campaign.AddParts(30, "salvage");

        AssertThat(campaign.Parts).IsEqual(50);
    }

    // ========================================================================
    // CAN AFFORD TESTS
    // ========================================================================

    [TestCase]
    public void CanAfford_ChecksCredits()
    {
        campaign.Money = 100;

        AssertThat(campaign.CanAfford(credits: 50)).IsTrue();
        AssertThat(campaign.CanAfford(credits: 150)).IsFalse();
    }

    [TestCase]
    public void CanAfford_ChecksMultipleResources()
    {
        campaign.Money = 100;
        campaign.Fuel = 50;
        campaign.Parts = 20;

        AssertThat(campaign.CanAfford(credits: 50, fuel: 30, parts: 10)).IsTrue();
        AssertThat(campaign.CanAfford(credits: 150, fuel: 30, parts: 10)).IsFalse();
        AssertThat(campaign.CanAfford(credits: 50, fuel: 100, parts: 10)).IsFalse();
        AssertThat(campaign.CanAfford(credits: 50, fuel: 30, parts: 50)).IsFalse();
    }

    // ========================================================================
    // INVENTORY INTEGRATION TESTS
    // ========================================================================

    [TestCase]
    public void GetCargoCapacity_ReturnsShipCapacity()
    {
        // Starter ship has base 20 + small cargo 20 = 40
        AssertThat(campaign.GetCargoCapacity()).IsEqual(40);
    }

    [TestCase]
    public void AddItem_Works()
    {
        var item = campaign.AddItem("medkit", 3);

        AssertThat(item).IsNotNull();
        AssertThat(campaign.Inventory.CountByDefId("medkit")).IsEqual(3);
    }

    [TestCase]
    public void AddItem_EmitsEvent()
    {
        campaign.AddItem("medkit", 2);

        AssertThat(itemAddedEvents.Count).IsEqual(1);
        AssertThat(itemAddedEvents[0].DefId).IsEqual("medkit");
        AssertThat(itemAddedEvents[0].Quantity).IsEqual(2);
    }

    [TestCase]
    public void AddItem_RespectsCapacity()
    {
        // Add cargo that exceeds capacity (40)
        var item = campaign.AddItem("weapons_cache", 5); // 5 * 10 = 50 volume

        AssertThat(item).IsNull();
    }

    [TestCase]
    public void RemoveItem_Works()
    {
        var initialCount = campaign.Inventory.Items.Count;
        var item = campaign.AddItem("rifle", 1);
        itemAddedEvents.Clear();

        var result = campaign.RemoveItem(item.Id);

        AssertThat(result).IsTrue();
        AssertThat(campaign.Inventory.Items.Count).IsEqual(initialCount);
    }

    [TestCase]
    public void RemoveItem_EmitsEvent()
    {
        var item = campaign.AddItem("rifle", 1);
        itemAddedEvents.Clear();

        campaign.RemoveItem(item.Id);

        AssertThat(itemRemovedEvents.Count).IsEqual(1);
        AssertThat(itemRemovedEvents[0].DefId).IsEqual("rifle");
    }

    [TestCase]
    public void RemoveItemByDef_Works()
    {
        campaign.AddItem("medkit", 5);

        var result = campaign.RemoveItemByDef("medkit", 3);

        AssertThat(result).IsTrue();
        AssertThat(campaign.Inventory.CountByDefId("medkit")).IsEqual(2);
    }

    [TestCase]
    public void HasItem_Works()
    {
        campaign.AddItem("medkit", 5);

        AssertThat(campaign.HasItem("medkit")).IsTrue();
        AssertThat(campaign.HasItem("medkit", 5)).IsTrue();
        AssertThat(campaign.HasItem("medkit", 6)).IsFalse();
        AssertThat(campaign.HasItem("rifle")).IsFalse();
    }

    [TestCase]
    public void GetUsedCargo_Works()
    {
        campaign.AddItem("medkit", 5);  // 5 * 1 = 5
        campaign.AddItem("repair_kit", 2);  // 2 * 2 = 4

        AssertThat(campaign.GetUsedCargo()).IsEqual(9);
    }

    [TestCase]
    public void GetAvailableCargo_Works()
    {
        campaign.AddItem("medkit", 10);  // 10 volume

        AssertThat(campaign.GetAvailableCargo()).IsEqual(30);  // 40 - 10
    }

    // ========================================================================
    // SHIP OPERATIONS TESTS
    // ========================================================================

    [TestCase]
    public void RepairShip_Works()
    {
        campaign.Ship.TakeDamage(20);
        campaign.Parts = 10;

        var result = campaign.RepairShip(5);

        AssertThat(result).IsTrue();
        AssertThat(campaign.Ship.Hull).IsEqual(40);  // 30 + 5*2 = 40
        AssertThat(campaign.Parts).IsEqual(5);
    }

    [TestCase]
    public void RepairShip_FailsIfFullHull()
    {
        campaign.Parts = 10;

        var result = campaign.RepairShip(5);

        AssertThat(result).IsFalse();
        AssertThat(campaign.Parts).IsEqual(10);
    }

    [TestCase]
    public void RepairShip_FailsIfInsufficientParts()
    {
        campaign.Ship.TakeDamage(20);
        campaign.Parts = 3;

        var result = campaign.RepairShip(5);

        AssertThat(result).IsFalse();
    }

    [TestCase]
    public void RepairShip_EmitsEvent()
    {
        campaign.Ship.TakeDamage(20);
        campaign.Parts = 10;

        campaign.RepairShip(5);

        AssertThat(shipHullEvents.Count).IsEqual(1);
        AssertThat(shipHullEvents[0].OldHull).IsEqual(30);
        AssertThat(shipHullEvents[0].NewHull).IsEqual(40);
        AssertThat(shipHullEvents[0].Reason).IsEqual("repair");
    }

    [TestCase]
    public void DamageShip_Works()
    {
        campaign.DamageShip(15, "combat");

        AssertThat(campaign.Ship.Hull).IsEqual(35);
    }

    [TestCase]
    public void DamageShip_EmitsEvent()
    {
        campaign.DamageShip(15, "asteroid");

        AssertThat(shipHullEvents.Count).IsEqual(1);
        AssertThat(shipHullEvents[0].OldHull).IsEqual(50);
        AssertThat(shipHullEvents[0].NewHull).IsEqual(35);
        AssertThat(shipHullEvents[0].Reason).IsEqual("asteroid");
    }

    [TestCase]
    public void InstallModule_Works()
    {
        // Remove existing cargo module to make room
        var existingModule = campaign.Ship.Modules.Find(m => m.SlotType == ShipSlotType.Cargo);
        campaign.Ship.RemoveModule(existingModule.Id);
        
        var initialCount = campaign.Inventory.Items.Count;
        var item = campaign.AddItem("large_cargo", 1);
        itemAddedEvents.Clear();

        var result = campaign.InstallModule(item.Id);

        AssertThat(result).IsTrue();
        AssertThat(campaign.Inventory.Items.Count).IsEqual(initialCount);
        AssertThat(campaign.Ship.CountModules(ShipSlotType.Cargo)).IsEqual(1);
    }

    [TestCase]
    public void InstallModule_FailsIfSlotsFull()
    {
        // Starter ship has 1 cargo slot, already filled
        var initialCount = campaign.Inventory.Items.Count;
        var item = campaign.AddItem("large_cargo", 1);

        var result = campaign.InstallModule(item.Id);

        AssertThat(result).IsFalse();
        AssertThat(campaign.Inventory.Items.Count).IsEqual(initialCount + 1);
    }

    [TestCase]
    public void InstallModule_FailsForNonModule()
    {
        var item = campaign.AddItem("rifle", 1);

        var result = campaign.InstallModule(item.Id);

        AssertThat(result).IsFalse();
    }

    [TestCase]
    public void InstallModule_EmitsEvent()
    {
        // Remove existing cargo module to make room
        var existingModule = campaign.Ship.Modules.Find(m => m.SlotType == ShipSlotType.Cargo);
        campaign.Ship.RemoveModule(existingModule.Id);
        
        var item = campaign.AddItem("large_cargo", 1);
        itemAddedEvents.Clear();

        campaign.InstallModule(item.Id);

        AssertThat(moduleInstalledEvents.Count).IsEqual(1);
        AssertThat(moduleInstalledEvents[0].ModuleDefId).IsEqual("large_cargo");
        AssertThat(moduleInstalledEvents[0].SlotType).IsEqual("Cargo");
    }

    [TestCase]
    public void RemoveModule_Works()
    {
        var module = campaign.Ship.Modules.Find(m => m.SlotType == ShipSlotType.Cargo);
        itemAddedEvents.Clear();

        var result = campaign.RemoveModule(module.Id);

        AssertThat(result).IsTrue();
        AssertThat(campaign.Ship.CountModules(ShipSlotType.Cargo)).IsEqual(0);
        AssertThat(campaign.Inventory.CountByDefId("small_cargo")).IsEqual(1);
    }

    [TestCase]
    public void RemoveModule_EmitsEvent()
    {
        var module = campaign.Ship.Modules.Find(m => m.SlotType == ShipSlotType.Cargo);

        campaign.RemoveModule(module.Id);

        AssertThat(moduleRemovedEvents.Count).IsEqual(1);
        AssertThat(moduleRemovedEvents[0].ModuleDefId).IsEqual("small_cargo");
    }

    // ========================================================================
    // SERIALIZATION TESTS
    // ========================================================================

    [TestCase]
    public void CampaignState_RoundTrip_PreservesInventory()
    {
        var initialCount = campaign.Inventory.Items.Count;
        campaign.AddItem("medkit", 5);
        campaign.AddItem("rifle", 2);

        var data = campaign.GetState();
        var restored = CampaignState.FromState(data);

        AssertThat(restored.Inventory.Items.Count).IsEqual(initialCount + 2);
        AssertThat(restored.Inventory.CountByDefId("medkit")).IsEqual(5);
        AssertThat(restored.Inventory.CountByDefId("rifle")).IsEqual(2);
    }

    [TestCase]
    public void CampaignState_RoundTrip_PreservesResources()
    {
        campaign.Money = 500;
        campaign.Fuel = 75;
        campaign.Parts = 30;

        var data = campaign.GetState();
        var restored = CampaignState.FromState(data);

        AssertThat(restored.Money).IsEqual(500);
        AssertThat(restored.Fuel).IsEqual(75);
        AssertThat(restored.Parts).IsEqual(30);
    }
}
