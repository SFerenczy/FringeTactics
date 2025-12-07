# MG-UI2 – Fire / Dismiss Crew (G2.5)

**Status:** ✅ Complete  
**Iteration:** 2025-12 – Crew Loop 01  
**Depends on:** MG-UI1 ✅ (Crew Roster & Detail Screen)

---

## Overview

Allow the player to remove crew members from the roster via the crew detail panel. This uses the existing `FireCrew` method in `CampaignState` and adds UI confirmation flow.

## Current State

- `CampaignState.FireCrew(crewId)` already exists and handles:
  - Validation (not dead, not last crew member)
  - Removal from roster
  - Event emission (`CrewFiredEvent`)
- Crew detail panel exists (MG-UI1) but has no fire action.

## Goal

From the crew detail screen, the player can:
- Fire/dismiss a crew member
- See a confirmation dialog before the action
- Understand why firing may be blocked (last crew, dead)

---

## Implementation Steps

### Step 1: Add Fire Button to Crew Detail Panel

**File:** `src/scenes/campaign/CampaignScreen.tscn`

Add to the crew detail panel:
- `FireButton` (Button) at bottom of detail panel
- `ConfirmFireDialog` (ConfirmationDialog) for confirmation

### Step 2: Wire Fire Button

**File:** `src/scenes/campaign/CampaignScreen.cs`

```csharp
private Button fireButton;
private ConfirmationDialog confirmFireDialog;

public override void _Ready()
{
    // ... existing setup ...
    fireButton = GetNode<Button>("%FireButton");
    confirmFireDialog = GetNode<ConfirmationDialog>("%ConfirmFireDialog");
    
    fireButton.Pressed += OnFireButtonPressed;
    confirmFireDialog.Confirmed += OnFireConfirmed;
}

private void OnFireButtonPressed()
{
    if (selectedCrew == null) return;
    
    // Check if can fire
    var campaign = GameState.Instance.Campaign;
    if (campaign.GetAliveCrew().Count <= 1)
    {
        ShowError("Cannot dismiss your last crew member.");
        return;
    }
    
    if (selectedCrew.IsDead)
    {
        ShowError("Cannot dismiss dead crew. Use 'Bury' instead.");
        return;
    }
    
    // Show confirmation
    confirmFireDialog.DialogText = $"Dismiss {selectedCrew.Name}?\n\n" +
        $"Role: {selectedCrew.Role}\n" +
        $"Level: {selectedCrew.Level}\n\n" +
        "This action cannot be undone.";
    confirmFireDialog.PopupCentered();
}

private void OnFireConfirmed()
{
    if (selectedCrew == null) return;
    
    var campaign = GameState.Instance.Campaign;
    if (campaign.FireCrew(selectedCrew.Id))
    {
        // Clear selection and refresh list
        selectedCrew = null;
        UpdateDetailPanel();
        PopulateCrewList();
    }
}
```

### Step 3: Update Detail Panel State

**File:** `src/scenes/campaign/CampaignScreen.cs`

Update `UpdateDetailPanel()` to manage fire button state:

```csharp
private void UpdateDetailPanel()
{
    if (selectedCrew == null)
    {
        detailPanel.Visible = false;
        return;
    }
    
    detailPanel.Visible = true;
    
    // ... existing stat/trait/injury display ...
    
    // Fire button state
    var campaign = GameState.Instance.Campaign;
    bool canFire = !selectedCrew.IsDead && campaign.GetAliveCrew().Count > 1;
    fireButton.Disabled = !canFire;
    
    if (selectedCrew.IsDead)
    {
        fireButton.Text = "Bury";
        fireButton.TooltipText = "Remove dead crew member from roster";
    }
    else if (campaign.GetAliveCrew().Count <= 1)
    {
        fireButton.Text = "Dismiss";
        fireButton.TooltipText = "Cannot dismiss last crew member";
    }
    else
    {
        fireButton.Text = "Dismiss";
        fireButton.TooltipText = "Remove crew member from roster";
    }
}
```

### Step 4: Handle Bury Dead Crew (Optional Enhancement)

If the selected crew is dead, the button becomes "Bury" instead:

```csharp
private void OnFireButtonPressed()
{
    if (selectedCrew == null) return;
    
    var campaign = GameState.Instance.Campaign;
    
    if (selectedCrew.IsDead)
    {
        // Bury dead crew (no confirmation needed)
        campaign.BuryDeadCrew(selectedCrew.Id);
        selectedCrew = null;
        UpdateDetailPanel();
        PopulateCrewList();
        return;
    }
    
    // ... existing fire confirmation flow ...
}
```

---

## Files to Modify

| File | Changes |
|------|---------|
| `src/scenes/campaign/CampaignScreen.tscn` | Add FireButton and ConfirmFireDialog |
| `src/scenes/campaign/CampaignScreen.cs` | Add fire/bury logic and confirmation flow |

## Files to Create

None required – this extends existing CampaignScreen.

---

## Acceptance Criteria

- [ ] From crew detail, I can see a "Dismiss" button
- [ ] Clicking "Dismiss" shows a confirmation dialog with crew summary
- [ ] Confirming removes the crew member and refreshes the list
- [ ] Cannot dismiss the last alive crew member (button disabled + tooltip)
- [ ] Dead crew shows "Bury" button instead (no confirmation needed)
- [ ] `CrewFiredEvent` is emitted on successful dismissal

---

## Testing

### Manual Test Scenarios

1. **Basic dismiss flow**
   - Select a crew member
   - Click Dismiss
   - Verify confirmation dialog shows name, role, level
   - Confirm and verify crew is removed from list

2. **Last crew protection**
   - Reduce roster to 1 alive crew member
   - Verify Dismiss button is disabled
   - Verify tooltip explains why

3. **Dead crew handling**
   - Kill a crew member (via debug or encounter)
   - Select dead crew member
   - Verify button shows "Bury"
   - Click and verify immediate removal (no confirmation)

4. **Event emission**
   - Subscribe to `CrewFiredEvent` in debug
   - Fire a crew member
   - Verify event is emitted with correct data

---

## Notes

- No sim changes required: `FireCrew` already exists with proper validation.
- Consider adding a "severance pay" mechanic later (return some credits).
- The confirmation dialog prevents accidental dismissals.
