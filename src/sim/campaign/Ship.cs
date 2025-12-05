using System;
using System.Collections.Generic;

namespace FringeTactics;

public enum ShipSlotType
{
    Engine,
    Weapon,
    Cargo,
    Utility
}

/// <summary>
/// Represents the player's ship with hull, modules, and cargo capacity.
/// </summary>
public class Ship
{
    // === Constants ===
    public const int BASE_CARGO_CAPACITY = 20;

    // === Identity ===
    public string ChassisId { get; set; } = "scout";
    public string Name { get; set; } = "Unnamed Ship";

    // === Hull ===
    public int Hull { get; set; } = 50;
    public int MaxHull { get; set; } = 50;

    // === Modules ===
    public List<ShipModule> Modules { get; set; } = new();

    // === Slot Limits (from chassis) ===
    public int EngineSlots { get; set; } = 1;
    public int WeaponSlots { get; set; } = 1;
    public int CargoSlots { get; set; } = 1;
    public int UtilitySlots { get; set; } = 1;

    /// <summary>
    /// Calculate total cargo capacity from base + cargo modules.
    /// </summary>
    public int GetCargoCapacity()
    {
        int capacity = BASE_CARGO_CAPACITY;
        foreach (var module in Modules)
        {
            if (module.SlotType == ShipSlotType.Cargo)
            {
                capacity += module.CargoBonus;
            }
        }
        return capacity;
    }

    /// <summary>
    /// Check if hull is critically damaged (at or below 25%).
    /// </summary>
    public bool IsCritical() => MaxHull > 0 && Hull <= MaxHull / 4;

    /// <summary>
    /// Check if ship is destroyed.
    /// </summary>
    public bool IsDestroyed() => Hull <= 0;

    /// <summary>
    /// Apply damage to hull.
    /// </summary>
    public void TakeDamage(int amount)
    {
        if (amount <= 0) return;
        Hull = Math.Max(0, Hull - amount);
    }

    /// <summary>
    /// Repair hull up to max.
    /// </summary>
    public void Repair(int amount)
    {
        if (amount <= 0) return;
        Hull = Math.Min(MaxHull, Hull + amount);
    }

    /// <summary>
    /// Count modules of a specific slot type.
    /// </summary>
    public int CountModules(ShipSlotType slotType)
    {
        int count = 0;
        foreach (var module in Modules)
        {
            if (module.SlotType == slotType) count++;
        }
        return count;
    }

    /// <summary>
    /// Get max slots for a slot type.
    /// </summary>
    public int GetMaxSlots(ShipSlotType slotType) => slotType switch
    {
        ShipSlotType.Engine => EngineSlots,
        ShipSlotType.Weapon => WeaponSlots,
        ShipSlotType.Cargo => CargoSlots,
        ShipSlotType.Utility => UtilitySlots,
        _ => 0
    };

    /// <summary>
    /// Check if a module can be installed.
    /// </summary>
    public bool CanInstallModule(ShipModule module)
    {
        if (module == null) return false;
        return CountModules(module.SlotType) < GetMaxSlots(module.SlotType);
    }

    /// <summary>
    /// Install a module. Returns false if no slot available.
    /// </summary>
    public bool InstallModule(ShipModule module)
    {
        if (!CanInstallModule(module)) return false;
        Modules.Add(module);
        return true;
    }

    /// <summary>
    /// Remove a module by ID.
    /// </summary>
    public bool RemoveModule(string moduleId)
    {
        for (int i = 0; i < Modules.Count; i++)
        {
            if (Modules[i].Id == moduleId)
            {
                Modules.RemoveAt(i);
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Find a module by ID.
    /// </summary>
    public ShipModule FindModule(string moduleId)
    {
        foreach (var module in Modules)
        {
            if (module.Id == moduleId) return module;
        }
        return null;
    }

    /// <summary>
    /// Get hull as a percentage (0-100).
    /// </summary>
    public int GetHullPercent() => MaxHull > 0 ? (Hull * 100) / MaxHull : 0;

    // === Serialization ===

    public ShipData GetState()
    {
        var data = new ShipData
        {
            ChassisId = ChassisId,
            Name = Name,
            Hull = Hull,
            MaxHull = MaxHull,
            EngineSlots = EngineSlots,
            WeaponSlots = WeaponSlots,
            CargoSlots = CargoSlots,
            UtilitySlots = UtilitySlots
        };

        foreach (var module in Modules)
        {
            data.Modules.Add(new ShipModuleData
            {
                Id = module.Id,
                DefId = module.DefId,
                SlotType = module.SlotType.ToString()
            });
        }

        return data;
    }

    public static Ship FromState(ShipData data)
    {
        if (data == null) return CreateStarter();

        var ship = new Ship
        {
            ChassisId = data.ChassisId ?? "scout",
            Name = data.Name ?? "Unnamed Ship",
            Hull = data.Hull,
            MaxHull = data.MaxHull,
            EngineSlots = data.EngineSlots,
            WeaponSlots = data.WeaponSlots,
            CargoSlots = data.CargoSlots,
            UtilitySlots = data.UtilitySlots
        };

        foreach (var moduleData in data.Modules ?? new List<ShipModuleData>())
        {
            ship.Modules.Add(new ShipModule
            {
                Id = moduleData.Id,
                DefId = moduleData.DefId,
                SlotType = Enum.TryParse<ShipSlotType>(moduleData.SlotType, out var st)
                    ? st : ShipSlotType.Utility
            });
        }

        return ship;
    }

    // === Factory Methods ===

    /// <summary>
    /// Create a starter ship (Scout class with basic modules).
    /// </summary>
    public static Ship CreateStarter()
    {
        var ship = new Ship
        {
            ChassisId = "scout",
            Name = "The Vagrant",
            Hull = 50,
            MaxHull = 50,
            EngineSlots = 1,
            WeaponSlots = 1,
            CargoSlots = 1,
            UtilitySlots = 1
        };

        // Install basic engine
        ship.InstallModule(new ShipModule
        {
            Id = "module_engine_1",
            DefId = "basic_engine",
            SlotType = ShipSlotType.Engine
        });

        // Install small cargo pod
        ship.InstallModule(new ShipModule
        {
            Id = "module_cargo_1",
            DefId = "small_cargo",
            SlotType = ShipSlotType.Cargo
        });

        return ship;
    }

    /// <summary>
    /// Create a ship from a chassis definition.
    /// </summary>
    public static Ship CreateFromChassis(string chassisId, string name = null)
    {
        var def = ChassisRegistry.Get(chassisId);
        if (def == null) return CreateStarter();

        return new Ship
        {
            ChassisId = def.Id,
            Name = name ?? def.Name,
            Hull = def.MaxHull,
            MaxHull = def.MaxHull,
            EngineSlots = def.EngineSlots,
            WeaponSlots = def.WeaponSlots,
            CargoSlots = def.CargoSlots,
            UtilitySlots = def.UtilitySlots
        };
    }
}

/// <summary>
/// An installed ship module. References ItemDef for properties.
/// </summary>
public class ShipModule
{
    /// <summary>
    /// Unique instance ID for this installed module.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Definition ID for lookup (e.g., "basic_engine", "small_cargo").
    /// </summary>
    public string DefId { get; set; }

    /// <summary>
    /// Which slot type this module occupies.
    /// </summary>
    public ShipSlotType SlotType { get; set; }

    // Properties derived from ItemDef - no duplication
    public string Name => GetDef()?.Name ?? DefId;
    public int CargoBonus => GetDef()?.CargoBonus ?? 0;
    public int FuelEfficiency => GetDef()?.FuelEfficiency ?? 0;

    /// <summary>
    /// Get the item definition for this module.
    /// </summary>
    public ItemDef GetDef() => ItemRegistry.Get(DefId);
}
