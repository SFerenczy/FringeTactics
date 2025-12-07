# MG-SYS1 – Minimal Equipment Slots (G2.5)

**Status:** Pending  
**Iteration:** 2025-12 – Crew Loop 01  
**Depends on:** MG2 ✅ (Ship & Resources)

---

## Overview

Formalize equipment slots on crew members and add stat modifier calculation. The infrastructure already exists (MG2), but this milestone ensures equipment actually affects crew stats in a queryable way.

## Current State

- `CrewMember` already has equipment slots:
  - `EquippedWeaponId`, `EquippedArmorId`, `EquippedGadgetId`
  - `GetEquipped(slot)`, `SetEquipped(slot, itemId)`, `HasEquipped(slot)`
- `CampaignState` has equip/unequip operations:
  - `EquipItem(crewId, itemId)`, `UnequipItem(crewId, slot)`
- `ItemDef` has `Stats` dictionary for modifiers (e.g., `{ "armor": 10, "damage": 25 }`)
- **Missing:** Effective stat calculation that includes equipment modifiers.

## Goal

- Equipment stat modifiers are applied to crew effective stats.
- Management layer can query "effective stats" (base + traits + equipment).
- Tactical layer receives correct stats when building mission input.

---

## Implementation Steps

### Step 1: Define Equipment Stat Keys

**File:** `src/sim/campaign/CrewMember.cs`

Add constants for equipment stat keys that map to crew stats:

```csharp
/// <summary>
/// Equipment stat keys that map to crew stat modifiers.
/// </summary>
public static class EquipmentStats
{
    // Direct stat bonuses
    public const string Grit = "grit";
    public const string Reflexes = "reflexes";
    public const string Aim = "aim";
    public const string Tech = "tech";
    public const string Savvy = "savvy";
    public const string Resolve = "resolve";
    
    // Derived stat bonuses
    public const string Armor = "armor";
    public const string Damage = "damage";
    public const string MaxHp = "max_hp";
    public const string Accuracy = "accuracy";
}
```

### Step 2: Add Equipment Modifier Calculation

**File:** `src/sim/campaign/CrewMember.cs`

Add method to calculate total equipment modifiers:

```csharp
/// <summary>
/// Calculate total modifier for a stat from all equipped items.
/// </summary>
/// <param name="statKey">The stat key to look up (e.g., "aim", "armor").</param>
/// <param name="inventory">The inventory to look up item definitions.</param>
public int GetEquipmentModifier(string statKey, Inventory inventory)
{
    if (inventory == null) return 0;
    
    int total = 0;
    
    foreach (var slot in new[] { EquipSlot.Weapon, EquipSlot.Armor, EquipSlot.Gadget })
    {
        var itemId = GetEquipped(slot);
        if (string.IsNullOrEmpty(itemId)) continue;
        
        var item = inventory.FindById(itemId);
        if (item == null) continue;
        
        var def = item.GetDef();
        if (def?.Stats != null && def.Stats.TryGetValue(statKey, out var value))
        {
            total += value;
        }
    }
    
    return total;
}

/// <summary>
/// Get equipment modifier for a crew stat type.
/// </summary>
public int GetEquipmentStatModifier(CrewStatType stat, Inventory inventory)
{
    string key = stat switch
    {
        CrewStatType.Grit => EquipmentStats.Grit,
        CrewStatType.Reflexes => EquipmentStats.Reflexes,
        CrewStatType.Aim => EquipmentStats.Aim,
        CrewStatType.Tech => EquipmentStats.Tech,
        CrewStatType.Savvy => EquipmentStats.Savvy,
        CrewStatType.Resolve => EquipmentStats.Resolve,
        _ => null
    };
    
    return key != null ? GetEquipmentModifier(key, inventory) : 0;
}
```

### Step 3: Add Full Effective Stat Method

**File:** `src/sim/campaign/CrewMember.cs`

Add method that combines base + traits + equipment:

```csharp
/// <summary>
/// Get fully effective stat value (base + traits + equipment).
/// </summary>
/// <param name="stat">The stat type.</param>
/// <param name="inventory">The inventory for equipment lookup.</param>
public int GetFullEffectiveStat(CrewStatType stat, Inventory inventory)
{
    return GetBaseStat(stat) 
         + GetTraitModifier(stat) 
         + GetEquipmentStatModifier(stat, inventory);
}

/// <summary>
/// Get effective armor value from equipment.
/// </summary>
public int GetArmorValue(Inventory inventory)
{
    return GetEquipmentModifier(EquipmentStats.Armor, inventory);
}

/// <summary>
/// Get effective max HP (base + grit + equipment).
/// </summary>
public int GetFullMaxHp(Inventory inventory)
{
    int gritBonus = GetFullEffectiveStat(CrewStatType.Grit, inventory) * StatConfig.HpPerGrit;
    int equipBonus = GetEquipmentModifier(EquipmentStats.MaxHp, inventory);
    return StatConfig.BaseHp + gritBonus + equipBonus;
}
```

### Step 4: Update MissionInputBuilder

**File:** `src/sim/combat/MissionInputBuilder.cs`

Ensure tactical actors receive equipment-modified stats:

```csharp
private static ActorSnapshot BuildCrewSnapshot(CrewMember crew, CampaignState campaign)
{
    var inventory = campaign.Inventory;
    
    return new ActorSnapshot
    {
        Id = crew.Id,
        Name = crew.Name,
        Type = ActorType.Player,
        
        // Use full effective stats (base + traits + equipment)
        MaxHp = crew.GetFullMaxHp(inventory),
        Armor = crew.GetArmorValue(inventory),
        Aim = crew.GetFullEffectiveStat(CrewStatType.Aim, inventory),
        Reflexes = crew.GetFullEffectiveStat(CrewStatType.Reflexes, inventory),
        Tech = crew.GetFullEffectiveStat(CrewStatType.Tech, inventory),
        
        WeaponId = crew.GetEffectiveWeaponId(inventory),
        // ... other fields ...
    };
}
```

### Step 5: Add Equipment Summary Method

**File:** `src/sim/campaign/CrewMember.cs`

Add helper for UI to display equipment effects:

```csharp
/// <summary>
/// Get a summary of all stat modifiers from equipment.
/// </summary>
/// <param name="inventory">The inventory for equipment lookup.</param>
/// <returns>Dictionary of stat key to total modifier.</returns>
public Dictionary<string, int> GetEquipmentStatSummary(Inventory inventory)
{
    var summary = new Dictionary<string, int>();
    if (inventory == null) return summary;
    
    foreach (var slot in new[] { EquipSlot.Weapon, EquipSlot.Armor, EquipSlot.Gadget })
    {
        var itemId = GetEquipped(slot);
        if (string.IsNullOrEmpty(itemId)) continue;
        
        var item = inventory.FindById(itemId);
        var def = item?.GetDef();
        if (def?.Stats == null) continue;
        
        foreach (var kvp in def.Stats)
        {
            if (!summary.ContainsKey(kvp.Key))
                summary[kvp.Key] = 0;
            summary[kvp.Key] += kvp.Value;
        }
    }
    
    return summary;
}
```

---

## Files to Modify

| File | Changes |
|------|---------|
| `src/sim/campaign/CrewMember.cs` | Add `EquipmentStats` constants, equipment modifier methods |
| `src/sim/combat/MissionInputBuilder.cs` | Use full effective stats when building actor snapshots |

## Files to Create

None required – this extends existing classes.

---

## Acceptance Criteria

- [ ] `CrewMember.GetEquipmentModifier(statKey, inventory)` returns correct totals
- [ ] `CrewMember.GetFullEffectiveStat(stat, inventory)` combines base + traits + equipment
- [ ] `CrewMember.GetArmorValue(inventory)` returns armor from equipment
- [ ] `CrewMember.GetFullMaxHp(inventory)` includes equipment HP bonuses
- [ ] `MissionInputBuilder` uses full effective stats for tactical actors
- [ ] Unit tests verify equipment modifier calculations

---

## Testing

### Unit Tests

**File:** `tests/sim/management/MG_SYS1_EquipmentTests.cs`

```csharp
[TestCase]
public void GetEquipmentModifier_ReturnsArmorFromEquippedArmor()
{
    var campaign = CampaignState.CreateForTesting(123);
    var crew = campaign.AddCrew("Test", CrewRole.Soldier);
    
    // Add and equip light armor (armor: 10)
    var item = campaign.AddItem("light_armor");
    campaign.EquipItem(crew.Id, item.Id);
    
    var armor = crew.GetEquipmentModifier(EquipmentStats.Armor, campaign.Inventory);
    Assert.AreEqual(10, armor);
}

[TestCase]
public void GetFullEffectiveStat_CombinesBaseTraitsEquipment()
{
    var campaign = CampaignState.CreateForTesting(123);
    var crew = campaign.AddCrew("Test", CrewRole.Soldier);
    crew.Aim = 5;
    crew.AddTrait("sharpshooter"); // +2 Aim
    
    // Add equipment with +1 Aim
    ItemRegistry.Register(new ItemDef 
    { 
        Id = "test_scope", 
        Category = ItemCategory.Equipment,
        EquipSlot = EquipSlot.Gadget,
        Stats = new() { { "aim", 1 } }
    });
    var item = campaign.AddItem("test_scope");
    campaign.EquipItem(crew.Id, item.Id);
    
    var aim = crew.GetFullEffectiveStat(CrewStatType.Aim, campaign.Inventory);
    Assert.AreEqual(8, aim); // 5 base + 2 trait + 1 equipment
}

[TestCase]
public void GetArmorValue_ReturnsZeroWithNoArmor()
{
    var campaign = CampaignState.CreateForTesting(123);
    var crew = campaign.AddCrew("Test", CrewRole.Soldier);
    
    var armor = crew.GetArmorValue(campaign.Inventory);
    Assert.AreEqual(0, armor);
}
```

### Manual Test Scenarios

1. **Equipment affects tactical stats**
   - Equip armor on crew member
   - Start mission
   - Verify actor has armor value in tactical

2. **Multiple equipment stacks**
   - Equip weapon, armor, and gadget with stat bonuses
   - Verify all bonuses are summed correctly

---

## Notes

- Equipment stats use string keys for flexibility (new stats can be added via data).
- The `GetEffectiveStat` method (existing) only includes traits; `GetFullEffectiveStat` adds equipment.
- Tactical layer should always use `GetFullEffectiveStat` or `GetFullMaxHp` for combat.
- UI can use `GetEquipmentStatSummary` to show "+10 Armor, +25 Damage" style tooltips.
