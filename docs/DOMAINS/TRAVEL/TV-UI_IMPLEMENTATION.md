# TV-UI – Travel Visibility: Implementation Plan

**Status**: ✅ Complete  
**Depends on**: TV2 ✅, WD3 ✅, GN2 ✅  
**Phase**: G2

---

## Overview

**Goal**: Expose the rich travel and world systems to the player through UI enhancements. This is a **display-only** milestone—no new sim code required.

The sim layer already tracks:
- System metrics (security, crime, stability, economy, law enforcement)
- System tags (Hub, Frontier, Lawless, Mining, etc.)
- Route hazards and tags
- Campaign time (days)
- Ship hull integrity
- Travel events and encounters

TV-UI makes all of this **visible** in the sector view.

---

## Current State Assessment

### What Exists (Sim Layer)

| Data | Location | Status |
|------|----------|--------|
| `SystemMetrics` | `StarSystem.Metrics` | ✅ 5 metrics (0-5 scale) |
| System tags | `StarSystem.Tags` | ✅ HashSet of tag strings |
| Route hazard | `Route.HazardLevel` | ✅ 0-5 scale |
| Route tags | `Route.Tags` | ✅ HashSet of tag strings |
| Campaign day | `CampaignState.Time.CurrentDay` | ✅ Integer day count |
| Ship hull | `CampaignState.Ship.Hull/MaxHull` | ✅ Current/max values |
| Station facilities | `Station.Facilities` | ✅ List of facility types |
| Faction reputation | `CampaignState.FactionRep` | ✅ Dictionary<string, int> |

### What's Displayed (UI Layer)

| Feature | Current | Gap |
|---------|---------|-----|
| System name | ✅ Shown | - |
| System type | ✅ Color-coded | - |
| Faction owner | ✅ Shown | Missing rep display |
| Fuel cost | ✅ Shown | - |
| System metrics | ❌ Hidden | Need info panel |
| System tags | ❌ Hidden | Need tag display |
| Route hazard | ❌ Hidden | Need route info |
| Campaign day | ❌ Hidden | Need header display |
| Ship hull | ❌ Hidden | Need resources panel |
| Travel feedback | ❌ Hidden | Need log panel |

---

## Architecture Decisions

### AD1: Display-Only Changes

**Decision**: TV-UI modifies only scene files (`src/scenes/`), not sim code.

**Rationale**:
- All data already exists in sim layer
- Follows architecture (scenes are adapters, not logic owners)
- Minimizes risk of breaking existing functionality

### AD2: Inline Info Panel

**Decision**: Expand the existing node info panel rather than creating a popup.

**Rationale**:
- Consistent with current UI pattern
- No modal interruption
- Information visible while planning

### AD3: Travel Log as Event History

**Decision**: Show last N travel events in a collapsible log panel.

**Rationale**:
- Provides feedback without blocking gameplay
- Can be expanded for details
- Prepares for encounter UI integration

---

## Implementation Phases

### Phase 1: System Info Enhancement (Priority: High)

Expand the selected node info panel to show metrics and tags.

#### Step 1.1: Add Metrics Display

**File**: `src/scenes/sector/SectorView.cs`

Update `UpdateDisplay()` to show system metrics:

```csharp
// In the selected node info section
if (system.Metrics != null)
{
    var metrics = system.Metrics;
    nodeInfoLabel.Text += $"\n\n--- Metrics ---\n" +
                          $"Security: {MetricBar(metrics.SecurityLevel)}\n" +
                          $"Crime: {MetricBar(metrics.CriminalActivity)}\n" +
                          $"Stability: {MetricBar(metrics.Stability)}\n" +
                          $"Economy: {MetricBar(metrics.EconomicActivity)}\n" +
                          $"Law: {MetricBar(metrics.LawEnforcementPresence)}";
}

// Helper method
private string MetricBar(int value)
{
    // value is 0-5, show as filled/empty squares
    string filled = new string('■', value);
    string empty = new string('□', 5 - value);
    return $"{filled}{empty} ({value})";
}
```

**Acceptance Criteria**:
- [x] All 5 metrics displayed for selected system
- [x] Visual bar representation (0-5)
- [x] Updates when selecting different nodes

#### Step 1.2: Add Tags Display

**File**: `src/scenes/sector/SectorView.cs`

Show system tags below metrics:

```csharp
if (system.Tags.Count > 0)
{
    var tagList = string.Join(", ", system.Tags.Take(5)); // Limit display
    nodeInfoLabel.Text += $"\nTags: {tagList}";
}
```

**Acceptance Criteria**:
- [x] System tags displayed
- [x] Truncated if too many tags

#### Step 1.3: Add Faction Reputation

**File**: `src/scenes/sector/SectorView.cs`

Show player's reputation with owning faction:

```csharp
if (!string.IsNullOrEmpty(system.OwningFactionId))
{
    var rep = campaign.GetFactionRep(system.OwningFactionId);
    var repText = rep >= 50 ? "Friendly" : rep >= 0 ? "Neutral" : "Hostile";
    nodeInfoLabel.Text += $"\nYour Rep: {rep} ({repText})";
}
```

**Acceptance Criteria**:
- [x] Faction reputation shown
- [x] Friendly/Neutral/Hostile label

---

### Phase 2: Route Info (Priority: High)

Show route details when selecting a destination.

#### Step 2.1: Display Route Hazard

**File**: `src/scenes/sector/SectorView.cs`

When showing travel cost, also show hazard:

```csharp
// Get route between current and selected
var route = campaign.World.GetRoute(campaign.CurrentNodeId, system.Id);
if (route != null)
{
    var hazardText = route.HazardLevel switch
    {
        0 => "Safe",
        1 => "Low Risk",
        2 => "Moderate",
        3 => "Dangerous",
        4 => "Very Dangerous",
        5 => "Extreme",
        _ => "Unknown"
    };
    
    nodeInfoLabel.Text += $"\nRoute Hazard: {hazardText} ({route.HazardLevel}/5)";
    
    if (route.Tags.Count > 0)
    {
        nodeInfoLabel.Text += $"\nRoute: {string.Join(", ", route.Tags)}";
    }
}
```

**Acceptance Criteria**:
- [x] Route hazard level shown
- [x] Hazard text label (Safe → Extreme)
- [x] Route tags shown

#### Step 2.2: Show Encounter Chance

**File**: `src/scenes/sector/SectorView.cs`

Calculate and display encounter probability:

```csharp
// Encounter chance based on hazard (from TV2 formula)
float encounterChance = route.HazardLevel * 0.1f; // 0-50%
nodeInfoLabel.Text += $"\nEncounter Chance: {encounterChance * 100:F0}%";
```

**Acceptance Criteria**:
- [x] Encounter chance displayed as percentage
- [x] Reflects route hazard

---

### Phase 3: Campaign Time Display (Priority: High)

Show current campaign day prominently.

#### Step 3.1: Add Day to Header

**File**: `src/scenes/sector/SectorView.cs`

Update location header to include day:

```csharp
// In UpdateDisplay()
var dayText = campaign.Time?.FormatCurrentDay() ?? "Day 1";
locationLabel.Text = $"@ {currentSystem?.Name ?? "Unknown"} | {dayText}";
```

**Acceptance Criteria**:
- [x] Campaign day shown in header
- [x] Updates after travel

---

### Phase 4: Ship Status (Priority: Medium)

Show ship hull in resources panel.

#### Step 4.1: Add Hull to Resources

**File**: `src/scenes/sector/SectorView.cs`

Update `UpdateResourceDisplay()`:

```csharp
private void UpdateResourceDisplay()
{
    var campaign = GameState.Instance?.Campaign;
    if (campaign == null) return;

    var hullText = campaign.Ship != null 
        ? $"Hull: {campaign.Ship.Hull}/{campaign.Ship.MaxHull}"
        : "";

    resourcesLabel.Text = $"Money: ${campaign.Money}\n" +
                          $"Fuel: {campaign.Fuel}  |  Ammo: {campaign.Ammo}\n" +
                          $"Parts: {campaign.Parts}  |  Meds: {campaign.Meds}\n" +
                          hullText;
}
```

**Acceptance Criteria**:
- [x] Ship hull shown as current/max
- [x] Updates when damaged/repaired

---

### Phase 5: Travel Feedback (Priority: Medium)

Show what happened during travel.

#### Step 5.1: Add Travel Log Panel

**File**: `src/scenes/sector/SectorView.cs`

Add a collapsible log panel that shows recent travel events:

```csharp
// New field
private Label travelLogLabel;
private List<string> travelLog = new();

// In CreateUI()
var logTitle = new Label();
logTitle.Text = "TRAVEL LOG";
logTitle.AddThemeFontSizeOverride("font_size", 12);
logTitle.AddThemeColorOverride("font_color", Colors.Gray);
vbox.AddChild(logTitle);

travelLogLabel = new Label();
travelLogLabel.AddThemeFontSizeOverride("font_size", 11);
travelLogLabel.CustomMinimumSize = new Vector2(0, 60);
vbox.AddChild(travelLogLabel);
```

#### Step 5.2: Populate Log After Travel

**File**: `src/scenes/sector/SectorView.cs`

After travel completes, show what happened:

```csharp
private void OnTravelPressed()
{
    if (!selectedNodeId.HasValue) return;

    var campaign = GameState.Instance?.Campaign;
    var fromSystem = campaign?.World?.GetSystem(campaign.CurrentNodeId);
    
    if (GameState.Instance.TravelTo(selectedNodeId.Value))
    {
        var toSystem = campaign?.World?.GetSystem(campaign.CurrentNodeId);
        
        // Add to travel log
        AddToTravelLog($"Traveled: {fromSystem?.Name} → {toSystem?.Name}");
        
        // Check for encounters that were auto-resolved
        // (This info comes from TravelResult, need to expose it)
        
        RefreshSector();
    }
}

private void AddToTravelLog(string message)
{
    travelLog.Insert(0, $"[Day {GameState.Instance.GetCampaignDay()}] {message}");
    if (travelLog.Count > 5) travelLog.RemoveAt(5);
    UpdateTravelLog();
}

private void UpdateTravelLog()
{
    travelLogLabel.Text = string.Join("\n", travelLog);
}
```

**Acceptance Criteria**:
- [x] Travel log shows recent events
- [x] Includes day number
- [x] Limited to last 5 entries

---

## TV-UI Deliverables Checklist

### Phase 1: System Info Enhancement ✅
- [x] **1.1** Add metrics display (5 metrics with bars)
- [x] **1.2** Add tags display
- [x] **1.3** Add faction reputation display

### Phase 2: Route Info ✅
- [x] **2.1** Display route hazard level
- [x] **2.2** Show encounter chance percentage

### Phase 3: Campaign Time ✅
- [x] **3.1** Add campaign day to header

### Phase 4: Ship Status ✅
- [x] **4.1** Add hull to resources panel

### Phase 5: Travel Feedback ✅
- [x] **5.1** Add travel log panel
- [x] **5.2** Populate log after travel

---

## Files to Modify

| File | Changes |
|------|---------|
| `src/scenes/sector/SectorView.cs` | All UI enhancements |
| `src/scenes/sector/SectorView.tscn` | Layout adjustments if needed |

---

## Testing

### Manual Test Scenarios

#### Scenario 1: System Info Display
1. Start new campaign
2. Click on various systems
3. Verify metrics, tags, faction rep displayed
4. Verify different system types show different metrics

#### Scenario 2: Route Info
1. Select a destination system
2. Verify hazard level shown
3. Verify encounter chance shown
4. Compare different routes (safe vs dangerous)

#### Scenario 3: Campaign Time
1. Note starting day in header
2. Travel to another system
3. Verify day advanced
4. Verify header updated

#### Scenario 4: Ship Hull
1. Check hull displayed in resources
2. Take damage (via encounter or devtools)
3. Verify hull updates
4. Repair at station
5. Verify hull updates

#### Scenario 5: Travel Log
1. Travel between systems
2. Verify log shows travel entry
3. Travel multiple times
4. Verify log limited to 5 entries
5. Verify day numbers correct

---

## Success Criteria

TV-UI is complete when:
- [ ] System metrics visible when selecting any node
- [ ] System tags visible for all systems
- [ ] Route hazard shown when planning travel
- [ ] Encounter chance displayed
- [ ] Campaign day shown in sector header
- [ ] Ship hull shown in resources
- [ ] Travel log shows recent events
- [ ] All displays update correctly after actions

---

## Future Considerations (Not in TV-UI)

- **Station facilities panel**: Show what services are available
- **Route comparison**: Side-by-side route options
- **Encounter history**: Detailed log of past encounters
- **Map overlays**: Toggle metrics/tags as map colors
