# MG-UI1 – Crew Roster & Detail Screen (G2.5)

**Status:** ⬜ Pending  
**Iteration:** 2025-12 – Crew Loop 01  
**Depends on:** MG1 ✅ (PlayerState & Crew)

---

## Overview

Expose existing crew data (stats, traits, injuries) on the campaign layer so the player can understand their roster without adding new sim logic.

## Current State

- Management domain already tracks crew, stats, traits, injuries (see MG1/MG4).
- Campaign screen shows a simple crew list but lacks detail, traits, and injury visibility.
- No dedicated crew detail UI yet.

## Goal

From the campaign screen, the player can:
- See all crew at a glance with key info.
- Select a crew member to see full details.
- Understand who is injured and what that means.

---

## Implementation Steps

### Step 1: Extend CampaignScreen Scene

**File:** `src/scenes/campaign/CampaignScreen.tscn`

Add UI elements:
- `CrewListPanel` (left side or expandable)
  - `VBoxContainer` for crew entries
- `CrewDetailPanel` (right side or modal)
  - Header: name, role
  - Stats section: all 6 core stats
  - Traits section: list of trait names with tags
  - Injuries section: list of active injuries with effects
  - Level/XP display

### Step 2: Create Crew List Entry

Each crew entry in the list shows:
- Name
- Role (if applicable)
- 2-3 key stats (e.g., Aim, Grit, Tech)
- Status icon: green (ready), orange (injured), red (dead)

### Step 3: Bind Crew List

**File:** `src/scenes/campaign/CampaignScreen.cs`

```csharp
private void PopulateCrewList()
{
    var crew = GameState.Instance.Campaign.Player.Crew;
    foreach (var member in crew)
    {
        var entry = CreateCrewListEntry(member);
        crewListContainer.AddChild(entry);
    }
}

private Control CreateCrewListEntry(CrewMember member)
{
    // Create entry with name, key stats, status icon
    // Connect click to SelectCrew(member.Id)
}
```

### Step 4: Implement Detail View

**File:** `src/scenes/campaign/CampaignScreen.cs`

```csharp
private CrewMember selectedCrew = null;

private void SelectCrew(string crewId)
{
    selectedCrew = GameState.Instance.Campaign.Player.GetCrew(crewId);
    UpdateDetailPanel();
}

private void UpdateDetailPanel()
{
    if (selectedCrew == null)
    {
        detailPanel.Visible = false;
        return;
    }
    
    detailPanel.Visible = true;
    
    // Header
    nameLabel.Text = selectedCrew.Name;
    roleLabel.Text = selectedCrew.Role ?? "Crew";
    
    // Stats
    UpdateStatsDisplay(selectedCrew);
    
    // Traits
    UpdateTraitsDisplay(selectedCrew);
    
    // Injuries
    UpdateInjuriesDisplay(selectedCrew);
    
    // Level/XP
    levelLabel.Text = $"Level {selectedCrew.Level}";
    xpLabel.Text = $"XP: {selectedCrew.Xp}";
}
```

### Step 5: Stats Display

Show all 6 core stats:
- Grit, Reflexes, Aim, Tech, Savvy, Resolve

Format: `Stat Name: Value` or visual bars.

### Step 6: Traits Display

For each trait:
- Show trait name
- Show short tag or description (from trait definition)
- Color-code: green for positive, red for negative, neutral for others

```csharp
private void UpdateTraitsDisplay(CrewMember member)
{
    traitsContainer.ClearChildren();
    
    foreach (var traitId in member.Traits)
    {
        var traitDef = ConfigRegistry.GetTrait(traitId);
        var label = new Label();
        label.Text = traitDef?.Name ?? traitId;
        // Add tooltip with description
        traitsContainer.AddChild(label);
    }
}
```

### Step 7: Injuries Display

For each injury:
- Show injury name
- Show effect summary (e.g., "-2 Aim", "Cannot run")
- Show healing status if applicable

```csharp
private void UpdateInjuriesDisplay(CrewMember member)
{
    injuriesContainer.ClearChildren();
    
    foreach (var injury in member.Injuries)
    {
        var label = new Label();
        label.Text = $"{injury.Name}: {injury.EffectSummary}";
        injuriesContainer.AddChild(label);
    }
    
    if (member.Injuries.Count == 0)
    {
        var label = new Label();
        label.Text = "No injuries";
        label.Modulate = Colors.Gray;
        injuriesContainer.AddChild(label);
    }
}
```

---

## Files to Modify

| File | Changes |
|------|---------|
| `src/scenes/campaign/CampaignScreen.tscn` | Add crew list and detail panels |
| `src/scenes/campaign/CampaignScreen.cs` | Add crew selection and detail display |
| `src/scenes/campaign/agents.md` | Document new responsibilities |

## Files to Create

None required – this extends existing CampaignScreen.

---

## Acceptance Criteria

- [ ] From the campaign screen, I can see all crew members listed
- [ ] Each crew entry shows name, key stats, and status icon
- [ ] Clicking a crew member shows their detail panel
- [ ] Detail panel shows all 6 core stats
- [ ] Detail panel shows all traits with names
- [ ] Detail panel shows all injuries with effect summaries
- [ ] Detail panel shows level and XP
- [ ] No new sim logic: the screen only reads existing Management state

---

## Testing

### Manual Test Scenarios

1. **Basic crew list**
   - Start campaign
   - Verify all crew members appear in list
   - Verify status icons are correct

2. **Crew selection**
   - Click on a crew member
   - Verify detail panel appears
   - Verify all stats are displayed correctly

3. **Traits display**
   - Ensure crew with traits show them
   - Verify trait names are readable

4. **Injuries display**
   - Injure a crew member (via encounter or debug)
   - Verify injury appears in detail panel
   - Verify effect summary is shown

---

## Notes

- This is read-only: no equipment, no firing, no stat modification.
- Equipment display comes in MG-UI3 after MG-SYS1 (equipment system).
- Firing comes in MG-UI2 as a separate slice.
