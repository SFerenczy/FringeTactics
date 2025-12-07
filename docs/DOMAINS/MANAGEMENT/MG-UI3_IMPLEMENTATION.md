# MG-UI3 – Equip/Unequip from Simple List (G2.5)

**Status:** ✅ Complete  
**Iteration:** 2025-12 – Crew Loop 01  
**Depends on:** MG-UI1 ✅, MG-SYS1 (Minimal Equipment Slots)

---

## Overview

Add equipment management to the crew detail panel. The player can see what a crew member has equipped and change equipment from the global inventory list.

## Current State

- Crew detail panel exists (MG-UI1) showing stats, traits, injuries.
- Equipment slots exist on `CrewMember` (MG2).
- `CampaignState.EquipItem()` and `UnequipItem()` exist with events.
- Equipment stat modifiers will be calculated (MG-SYS1).
- **Missing:** UI to view and change equipment.

## Goal

From the crew detail screen, the player can:
- See current equipment in each slot (weapon, armor, gadget)
- Click a slot to open equipment selection
- Select from available items in inventory
- See stat changes before confirming
- Unequip items back to inventory

---

## Implementation Steps

### Step 1: Add Equipment Section to Detail Panel

**File:** `src/scenes/campaign/CampaignScreen.tscn`

Add to crew detail panel:
```
EquipmentSection (VBoxContainer)
├── EquipmentHeader (Label) "Equipment"
├── WeaponSlot (HBoxContainer)
│   ├── SlotLabel (Label) "Weapon:"
│   ├── ItemLabel (Label) "[Empty]"
│   └── ChangeButton (Button) "Change"
├── ArmorSlot (HBoxContainer)
│   ├── SlotLabel (Label) "Armor:"
│   ├── ItemLabel (Label) "[Empty]"
│   └── ChangeButton (Button) "Change"
├── GadgetSlot (HBoxContainer)
│   ├── SlotLabel (Label) "Gadget:"
│   ├── ItemLabel (Label) "[Empty]"
│   └── ChangeButton (Button) "Change"
└── StatBonusLabel (Label) "Equipment bonuses: +10 Armor"
```

### Step 2: Add Equipment Selection Popup

**File:** `src/scenes/campaign/CampaignScreen.tscn`

Add popup for item selection:
```
EquipmentPopup (PopupPanel)
├── VBoxContainer
│   ├── PopupTitle (Label) "Select Weapon"
│   ├── ItemList (ItemList)
│   ├── StatPreview (Label) "Stats: +25 Damage, +70 Accuracy"
│   └── ButtonRow (HBoxContainer)
│       ├── EquipButton (Button) "Equip"
│       ├── UnequipButton (Button) "Unequip"
│       └── CancelButton (Button) "Cancel"
```

### Step 3: Create Equipment UI Controller

**File:** `src/scenes/campaign/CampaignScreen.cs`

Add equipment management fields and methods:

```csharp
// Equipment UI references
private VBoxContainer equipmentSection;
private Label weaponItemLabel;
private Label armorItemLabel;
private Label gadgetItemLabel;
private Label statBonusLabel;
private PopupPanel equipmentPopup;
private ItemList equipmentItemList;
private Label statPreviewLabel;
private Button equipButton;
private Button unequipButton;

// Equipment selection state
private EquipSlot selectedSlot;
private string selectedItemId;

public override void _Ready()
{
    // ... existing setup ...
    
    // Equipment section
    equipmentSection = GetNode<VBoxContainer>("%EquipmentSection");
    weaponItemLabel = GetNode<Label>("%WeaponItemLabel");
    armorItemLabel = GetNode<Label>("%ArmorItemLabel");
    gadgetItemLabel = GetNode<Label>("%GadgetItemLabel");
    statBonusLabel = GetNode<Label>("%StatBonusLabel");
    
    // Equipment popup
    equipmentPopup = GetNode<PopupPanel>("%EquipmentPopup");
    equipmentItemList = GetNode<ItemList>("%EquipmentItemList");
    statPreviewLabel = GetNode<Label>("%StatPreviewLabel");
    equipButton = GetNode<Button>("%EquipButton");
    unequipButton = GetNode<Button>("%UnequipButton");
    
    // Connect signals
    GetNode<Button>("%WeaponChangeButton").Pressed += () => OpenEquipmentPopup(EquipSlot.Weapon);
    GetNode<Button>("%ArmorChangeButton").Pressed += () => OpenEquipmentPopup(EquipSlot.Armor);
    GetNode<Button>("%GadgetChangeButton").Pressed += () => OpenEquipmentPopup(EquipSlot.Gadget);
    
    equipmentItemList.ItemSelected += OnEquipmentItemSelected;
    equipButton.Pressed += OnEquipPressed;
    unequipButton.Pressed += OnUnequipPressed;
    GetNode<Button>("%EquipCancelButton").Pressed += () => equipmentPopup.Hide();
}
```

### Step 4: Implement Equipment Display

**File:** `src/scenes/campaign/CampaignScreen.cs`

```csharp
private void UpdateEquipmentDisplay()
{
    if (selectedCrew == null) return;
    
    var campaign = GameState.Instance.Campaign;
    var inventory = campaign.Inventory;
    
    // Update slot labels
    weaponItemLabel.Text = GetEquippedItemName(EquipSlot.Weapon, inventory);
    armorItemLabel.Text = GetEquippedItemName(EquipSlot.Armor, inventory);
    gadgetItemLabel.Text = GetEquippedItemName(EquipSlot.Gadget, inventory);
    
    // Update stat bonus summary
    var bonuses = selectedCrew.GetEquipmentStatSummary(inventory);
    if (bonuses.Count > 0)
    {
        var parts = new List<string>();
        foreach (var kvp in bonuses)
        {
            string sign = kvp.Value >= 0 ? "+" : "";
            parts.Add($"{sign}{kvp.Value} {FormatStatName(kvp.Key)}");
        }
        statBonusLabel.Text = string.Join(", ", parts);
        statBonusLabel.Visible = true;
    }
    else
    {
        statBonusLabel.Text = "No equipment bonuses";
        statBonusLabel.Visible = true;
    }
}

private string GetEquippedItemName(EquipSlot slot, Inventory inventory)
{
    var itemId = selectedCrew.GetEquipped(slot);
    if (string.IsNullOrEmpty(itemId)) return "[Empty]";
    
    var item = inventory.FindById(itemId);
    return item?.GetName() ?? "[Missing]";
}

private string FormatStatName(string key)
{
    return key switch
    {
        "armor" => "Armor",
        "damage" => "Damage",
        "accuracy" => "Accuracy",
        "aim" => "Aim",
        "grit" => "Grit",
        "max_hp" => "HP",
        _ => key
    };
}
```

### Step 5: Implement Equipment Selection Popup

**File:** `src/scenes/campaign/CampaignScreen.cs`

```csharp
private void OpenEquipmentPopup(EquipSlot slot)
{
    if (selectedCrew == null) return;
    
    selectedSlot = slot;
    selectedItemId = null;
    
    var campaign = GameState.Instance.Campaign;
    var inventory = campaign.Inventory;
    
    // Set popup title
    var title = GetNode<Label>("%EquipmentPopupTitle");
    title.Text = $"Select {slot}";
    
    // Populate item list with available items for this slot
    equipmentItemList.Clear();
    
    var equipment = inventory.GetByCategory(ItemCategory.Equipment);
    foreach (var item in equipment)
    {
        var def = item.GetDef();
        if (def == null || def.EquipSlot != slot) continue;
        
        // Skip items equipped by other crew
        if (IsEquippedByOther(item.Id)) continue;
        
        equipmentItemList.AddItem(def.Name);
        equipmentItemList.SetItemMetadata(equipmentItemList.ItemCount - 1, item.Id);
    }
    
    // Update button states
    var currentEquipped = selectedCrew.GetEquipped(slot);
    unequipButton.Disabled = string.IsNullOrEmpty(currentEquipped);
    equipButton.Disabled = true; // Enable when item selected
    
    // Clear preview
    statPreviewLabel.Text = "Select an item to see stats";
    
    equipmentPopup.PopupCentered();
}

private bool IsEquippedByOther(string itemId)
{
    var campaign = GameState.Instance.Campaign;
    foreach (var crew in campaign.Crew)
    {
        if (crew.Id == selectedCrew.Id) continue;
        if (crew.GetAllEquippedIds().Contains(itemId)) return true;
    }
    return false;
}

private void OnEquipmentItemSelected(long index)
{
    selectedItemId = equipmentItemList.GetItemMetadata((int)index).AsString();
    equipButton.Disabled = string.IsNullOrEmpty(selectedItemId);
    
    // Show stat preview
    if (!string.IsNullOrEmpty(selectedItemId))
    {
        var campaign = GameState.Instance.Campaign;
        var item = campaign.Inventory.FindById(selectedItemId);
        var def = item?.GetDef();
        
        if (def?.Stats != null && def.Stats.Count > 0)
        {
            var parts = new List<string>();
            foreach (var kvp in def.Stats)
            {
                string sign = kvp.Value >= 0 ? "+" : "";
                parts.Add($"{sign}{kvp.Value} {FormatStatName(kvp.Key)}");
            }
            statPreviewLabel.Text = string.Join(", ", parts);
        }
        else
        {
            statPreviewLabel.Text = "No stat bonuses";
        }
    }
}
```

### Step 6: Implement Equip/Unequip Actions

**File:** `src/scenes/campaign/CampaignScreen.cs`

```csharp
private void OnEquipPressed()
{
    if (selectedCrew == null || string.IsNullOrEmpty(selectedItemId)) return;
    
    var campaign = GameState.Instance.Campaign;
    if (campaign.EquipItem(selectedCrew.Id, selectedItemId))
    {
        equipmentPopup.Hide();
        UpdateEquipmentDisplay();
        UpdateStatsDisplay(selectedCrew); // Refresh stats to show new effective values
    }
}

private void OnUnequipPressed()
{
    if (selectedCrew == null) return;
    
    var campaign = GameState.Instance.Campaign;
    if (campaign.UnequipItem(selectedCrew.Id, selectedSlot))
    {
        equipmentPopup.Hide();
        UpdateEquipmentDisplay();
        UpdateStatsDisplay(selectedCrew);
    }
}
```

### Step 7: Update Detail Panel to Include Equipment

**File:** `src/scenes/campaign/CampaignScreen.cs`

Update `UpdateDetailPanel()` to call equipment display:

```csharp
private void UpdateDetailPanel()
{
    if (selectedCrew == null)
    {
        detailPanel.Visible = false;
        return;
    }
    
    detailPanel.Visible = true;
    
    // ... existing header, stats, traits, injuries ...
    
    // Equipment (MG-UI3)
    UpdateEquipmentDisplay();
}
```

---

## Files to Modify

| File | Changes |
|------|---------|
| `src/scenes/campaign/CampaignScreen.tscn` | Add equipment section and selection popup |
| `src/scenes/campaign/CampaignScreen.cs` | Add equipment display and selection logic |

## Files to Create

None required – this extends existing CampaignScreen.

---

## Acceptance Criteria

- [ ] Crew detail shows current equipment in each slot
- [ ] Empty slots show "[Empty]"
- [ ] Clicking "Change" opens equipment selection popup
- [ ] Popup lists only items matching the slot type
- [ ] Items equipped by other crew are not shown
- [ ] Selecting an item shows stat preview
- [ ] "Equip" button equips the selected item
- [ ] "Unequip" button removes current equipment
- [ ] Equipment stat bonuses are displayed as summary
- [ ] Stats display updates after equip/unequip
- [ ] `ItemEquippedEvent` / `ItemUnequippedEvent` are emitted

---

## Testing

### Manual Test Scenarios

1. **View equipment**
   - Select crew member
   - Verify equipment section shows current items or "[Empty]"

2. **Equip item**
   - Add weapon to inventory (via debug)
   - Open weapon slot selection
   - Select weapon and click Equip
   - Verify weapon appears in slot
   - Verify stat bonuses update

3. **Unequip item**
   - With equipped item, open slot selection
   - Click Unequip
   - Verify slot shows "[Empty]"
   - Verify item is still in inventory

4. **Item exclusivity**
   - Equip item on Crew A
   - Select Crew B
   - Open same slot selection
   - Verify item is not in list (equipped by other)

5. **Stat preview**
   - Open equipment selection
   - Select different items
   - Verify stat preview updates correctly

---

## Notes

- No drag-and-drop: keep it simple with list selection.
- No filtering: show all items for the slot type.
- Items remain in inventory when equipped (they're referenced by ID).
- Consider adding item comparison (current vs selected) in future iteration.
- The popup is modal to prevent accidental clicks elsewhere.
