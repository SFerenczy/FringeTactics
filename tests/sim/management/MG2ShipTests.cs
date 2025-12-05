using GdUnit4;
using static GdUnit4.Assertions;

namespace FringeTactics.Tests;

[TestSuite]
public class MG2ShipTests
{
    // === CreateStarter Tests ===

    [TestCase]
    public void Ship_CreateStarter_HasCorrectChassis()
    {
        var ship = Ship.CreateStarter();

        AssertThat(ship.ChassisId).IsEqual("scout");
        AssertThat(ship.Name).IsEqual("The Vagrant");
    }

    [TestCase]
    public void Ship_CreateStarter_HasCorrectHull()
    {
        var ship = Ship.CreateStarter();

        AssertThat(ship.Hull).IsEqual(50);
        AssertThat(ship.MaxHull).IsEqual(50);
    }

    [TestCase]
    public void Ship_CreateStarter_HasCorrectSlots()
    {
        var ship = Ship.CreateStarter();

        AssertThat(ship.EngineSlots).IsEqual(1);
        AssertThat(ship.WeaponSlots).IsEqual(1);
        AssertThat(ship.CargoSlots).IsEqual(1);
        AssertThat(ship.UtilitySlots).IsEqual(1);
    }

    [TestCase]
    public void Ship_CreateStarter_HasBasicModules()
    {
        var ship = Ship.CreateStarter();

        AssertThat(ship.Modules.Count).IsEqual(2);
        AssertThat(ship.CountModules(ShipSlotType.Engine)).IsEqual(1);
        AssertThat(ship.CountModules(ShipSlotType.Cargo)).IsEqual(1);
    }

    // === Cargo Capacity Tests ===

    [TestCase]
    public void Ship_GetCargoCapacity_IncludesBaseCapacity()
    {
        var ship = new Ship();
        ship.Modules.Clear();

        AssertThat(ship.GetCargoCapacity()).IsEqual(Ship.BASE_CARGO_CAPACITY);
    }

    [TestCase]
    public void Ship_GetCargoCapacity_IncludesCargoModules()
    {
        var ship = Ship.CreateStarter();
        // Base 20 + small cargo 20 = 40
        AssertThat(ship.GetCargoCapacity()).IsEqual(40);
    }

    [TestCase]
    public void Ship_GetCargoCapacity_SumsMultipleCargoModules()
    {
        var ship = new Ship { CargoSlots = 3 };
        ship.InstallModule(new ShipModule { Id = "c1", DefId = "small_cargo", SlotType = ShipSlotType.Cargo });
        ship.InstallModule(new ShipModule { Id = "c2", DefId = "large_cargo", SlotType = ShipSlotType.Cargo });

        // Base 20 + small(20) + large(50) = 90
        AssertThat(ship.GetCargoCapacity()).IsEqual(90);
    }

    // === Damage Tests ===

    [TestCase]
    public void Ship_TakeDamage_ReducesHull()
    {
        var ship = Ship.CreateStarter();

        ship.TakeDamage(20);

        AssertThat(ship.Hull).IsEqual(30);
    }

    [TestCase]
    public void Ship_TakeDamage_ClampsToZero()
    {
        var ship = Ship.CreateStarter();

        ship.TakeDamage(100);

        AssertThat(ship.Hull).IsEqual(0);
    }

    [TestCase]
    public void Ship_TakeDamage_ZeroOrNegativeDoesNothing()
    {
        var ship = Ship.CreateStarter();

        ship.TakeDamage(0);
        AssertThat(ship.Hull).IsEqual(50);

        ship.TakeDamage(-10);
        AssertThat(ship.Hull).IsEqual(50);
    }

    [TestCase]
    public void Ship_IsDestroyed_TrueAtZeroHull()
    {
        var ship = Ship.CreateStarter();
        ship.TakeDamage(50);

        AssertThat(ship.IsDestroyed()).IsTrue();
    }

    [TestCase]
    public void Ship_IsDestroyed_FalseAboveZero()
    {
        var ship = Ship.CreateStarter();
        ship.TakeDamage(49);

        AssertThat(ship.IsDestroyed()).IsFalse();
    }

    // === Repair Tests ===

    [TestCase]
    public void Ship_Repair_IncreasesHull()
    {
        var ship = Ship.CreateStarter();
        ship.TakeDamage(30);

        ship.Repair(15);

        AssertThat(ship.Hull).IsEqual(35);
    }

    [TestCase]
    public void Ship_Repair_ClampsToMax()
    {
        var ship = Ship.CreateStarter();
        ship.TakeDamage(20);

        ship.Repair(100);

        AssertThat(ship.Hull).IsEqual(50);
    }

    [TestCase]
    public void Ship_Repair_ZeroOrNegativeDoesNothing()
    {
        var ship = Ship.CreateStarter();
        ship.TakeDamage(20);

        ship.Repair(0);
        AssertThat(ship.Hull).IsEqual(30);

        ship.Repair(-10);
        AssertThat(ship.Hull).IsEqual(30);
    }

    // === Critical State Tests ===

    [TestCase]
    public void Ship_IsCritical_TrueAt25PercentOrBelow()
    {
        var ship = Ship.CreateStarter(); // MaxHull = 50, 25% = 12.5

        ship.Hull = 12; // At 24%
        AssertThat(ship.IsCritical()).IsTrue();

        ship.Hull = 13; // At 26%
        AssertThat(ship.IsCritical()).IsFalse();
    }

    [TestCase]
    public void Ship_IsCritical_TrueAtExactly25Percent()
    {
        var ship = new Ship { Hull = 25, MaxHull = 100 };

        AssertThat(ship.IsCritical()).IsTrue();
    }

    [TestCase]
    public void Ship_GetHullPercent_Correct()
    {
        var ship = new Ship { Hull = 75, MaxHull = 100 };
        AssertThat(ship.GetHullPercent()).IsEqual(75);

        ship.Hull = 25;
        AssertThat(ship.GetHullPercent()).IsEqual(25);
    }

    // === Module Installation Tests ===

    [TestCase]
    public void Ship_InstallModule_Success()
    {
        var ship = new Ship { WeaponSlots = 1 };
        var module = new ShipModule { Id = "w1", SlotType = ShipSlotType.Weapon };

        var result = ship.InstallModule(module);

        AssertThat(result).IsTrue();
        AssertThat(ship.Modules.Count).IsEqual(1);
    }

    [TestCase]
    public void Ship_InstallModule_FailsWhenSlotsFull()
    {
        var ship = Ship.CreateStarter(); // 1 cargo slot, already has cargo module
        var extraCargo = new ShipModule
        {
            Id = "extra",
            DefId = "small_cargo",
            SlotType = ShipSlotType.Cargo
        };

        AssertThat(ship.CanInstallModule(extraCargo)).IsFalse();
        AssertThat(ship.InstallModule(extraCargo)).IsFalse();
        AssertThat(ship.CountModules(ShipSlotType.Cargo)).IsEqual(1);
    }

    [TestCase]
    public void Ship_InstallModule_RespectsSlotType()
    {
        var ship = new Ship { EngineSlots = 2, WeaponSlots = 1 };

        ship.InstallModule(new ShipModule { Id = "e1", SlotType = ShipSlotType.Engine });
        ship.InstallModule(new ShipModule { Id = "e2", SlotType = ShipSlotType.Engine });
        ship.InstallModule(new ShipModule { Id = "w1", SlotType = ShipSlotType.Weapon });

        // Third engine should fail
        var result = ship.InstallModule(new ShipModule { Id = "e3", SlotType = ShipSlotType.Engine });
        AssertThat(result).IsFalse();

        // Second weapon should fail
        result = ship.InstallModule(new ShipModule { Id = "w2", SlotType = ShipSlotType.Weapon });
        AssertThat(result).IsFalse();
    }

    [TestCase]
    public void Ship_CanInstallModule_ReturnsFalseForNull()
    {
        var ship = Ship.CreateStarter();

        AssertThat(ship.CanInstallModule(null)).IsFalse();
    }

    // === Module Removal Tests ===

    [TestCase]
    public void Ship_RemoveModule_Success()
    {
        var ship = Ship.CreateStarter();
        var moduleId = ship.Modules[0].Id;

        var result = ship.RemoveModule(moduleId);

        AssertThat(result).IsTrue();
        AssertThat(ship.Modules.Count).IsEqual(1);
    }

    [TestCase]
    public void Ship_RemoveModule_FailsForUnknownId()
    {
        var ship = Ship.CreateStarter();

        var result = ship.RemoveModule("nonexistent");

        AssertThat(result).IsFalse();
        AssertThat(ship.Modules.Count).IsEqual(2);
    }

    [TestCase]
    public void Ship_FindModule_ReturnsModule()
    {
        var ship = Ship.CreateStarter();
        var moduleId = ship.Modules[0].Id;

        var module = ship.FindModule(moduleId);

        AssertThat(module).IsNotNull();
        AssertThat(module.Id).IsEqual(moduleId);
    }

    [TestCase]
    public void Ship_FindModule_ReturnsNullForUnknown()
    {
        var ship = Ship.CreateStarter();

        var module = ship.FindModule("nonexistent");

        AssertThat(module).IsNull();
    }

    // === Chassis Factory Tests ===

    [TestCase]
    public void Ship_CreateFromChassis_Scout()
    {
        var ship = Ship.CreateFromChassis("scout", "My Scout");

        AssertThat(ship.ChassisId).IsEqual("scout");
        AssertThat(ship.Name).IsEqual("My Scout");
        AssertThat(ship.MaxHull).IsEqual(50);
        AssertThat(ship.CargoSlots).IsEqual(1);
    }

    [TestCase]
    public void Ship_CreateFromChassis_Freighter()
    {
        var ship = Ship.CreateFromChassis("freighter");

        AssertThat(ship.ChassisId).IsEqual("freighter");
        AssertThat(ship.MaxHull).IsEqual(80);
        AssertThat(ship.CargoSlots).IsEqual(3);
    }

    [TestCase]
    public void Ship_CreateFromChassis_Corvette()
    {
        var ship = Ship.CreateFromChassis("corvette");

        AssertThat(ship.ChassisId).IsEqual("corvette");
        AssertThat(ship.MaxHull).IsEqual(100);
        AssertThat(ship.WeaponSlots).IsEqual(2);
        AssertThat(ship.CargoSlots).IsEqual(2);
    }

    [TestCase]
    public void Ship_CreateFromChassis_Gunship()
    {
        var ship = Ship.CreateFromChassis("gunship");

        AssertThat(ship.ChassisId).IsEqual("gunship");
        AssertThat(ship.MaxHull).IsEqual(120);
        AssertThat(ship.WeaponSlots).IsEqual(3);
        AssertThat(ship.CargoSlots).IsEqual(1);
    }

    [TestCase]
    public void Ship_CreateFromChassis_UnknownDefaultsToStarter()
    {
        var ship = Ship.CreateFromChassis("unknown");

        AssertThat(ship.ChassisId).IsEqual("scout");
    }

    // === Serialization Tests ===

    [TestCase]
    public void Ship_GetState_PreservesAllFields()
    {
        var ship = Ship.CreateStarter();
        ship.Name = "Test Ship";
        ship.Hull = 30;

        var data = ship.GetState();

        AssertThat(data.ChassisId).IsEqual("scout");
        AssertThat(data.Name).IsEqual("Test Ship");
        AssertThat(data.Hull).IsEqual(30);
        AssertThat(data.MaxHull).IsEqual(50);
        AssertThat(data.Modules.Count).IsEqual(2);
    }

    [TestCase]
    public void Ship_FromState_RestoresAllFields()
    {
        var original = Ship.CreateStarter();
        original.Name = "Restored Ship";
        original.Hull = 25;

        var data = original.GetState();
        var restored = Ship.FromState(data);

        AssertThat(restored.ChassisId).IsEqual(original.ChassisId);
        AssertThat(restored.Name).IsEqual(original.Name);
        AssertThat(restored.Hull).IsEqual(original.Hull);
        AssertThat(restored.MaxHull).IsEqual(original.MaxHull);
        AssertThat(restored.Modules.Count).IsEqual(original.Modules.Count);
    }

    [TestCase]
    public void Ship_FromState_RestoresModules()
    {
        var original = Ship.CreateStarter();
        var data = original.GetState();

        var restored = Ship.FromState(data);

        AssertThat(restored.CountModules(ShipSlotType.Engine)).IsEqual(1);
        AssertThat(restored.CountModules(ShipSlotType.Cargo)).IsEqual(1);
        AssertThat(restored.GetCargoCapacity()).IsEqual(original.GetCargoCapacity());
    }

    [TestCase]
    public void Ship_FromState_NullDataCreatesStarter()
    {
        var ship = Ship.FromState(null);

        AssertThat(ship).IsNotNull();
        AssertThat(ship.ChassisId).IsEqual("scout");
    }

    // === CampaignState Integration Tests ===

    [TestCase]
    public void CampaignState_CreateNew_HasShip()
    {
        var campaign = CampaignState.CreateNew();

        AssertThat(campaign.Ship).IsNotNull();
        AssertThat(campaign.Ship.ChassisId).IsEqual("scout");
        AssertThat(campaign.Ship.Name).IsEqual("The Vagrant");
    }

    [TestCase]
    public void CampaignState_RoundTrip_PreservesShip()
    {
        var campaign = CampaignState.CreateNew();
        campaign.Ship.Name = "Modified Ship";
        campaign.Ship.TakeDamage(15);

        var data = campaign.GetState();
        var restored = CampaignState.FromState(data);

        AssertThat(restored.Ship).IsNotNull();
        AssertThat(restored.Ship.Name).IsEqual("Modified Ship");
        AssertThat(restored.Ship.Hull).IsEqual(35);
    }

    [TestCase]
    public void CampaignState_RoundTrip_PreservesShipModules()
    {
        var campaign = CampaignState.CreateNew();

        var data = campaign.GetState();
        var restored = CampaignState.FromState(data);

        AssertThat(restored.Ship.Modules.Count).IsEqual(campaign.Ship.Modules.Count);
        AssertThat(restored.Ship.GetCargoCapacity()).IsEqual(campaign.Ship.GetCargoCapacity());
    }
}
