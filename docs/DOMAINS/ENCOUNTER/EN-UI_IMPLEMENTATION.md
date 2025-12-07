# EN-UI – Encounter Screen: Implementation Plan

**Status**: ✅ Complete (sim layer complete, UI pending)  
**Depends on**: EN1 ✅, EN2 ✅, MG4 ✅, TV-UI (recommended)  
**Phase**: G2

---

## Overview

**Goal**: Create an interactive encounter UI that presents encounters to the player instead of auto-resolving them. This completes the G2 encounter loop by connecting the sim layer (EncounterRunner, EncounterInstance) to player interaction.

**Current state**: Encounters are generated during travel but auto-resolved because:
1. `GameState.TravelTo()` doesn't handle `TravelResult.Paused`
2. No encounter screen exists to display encounters
3. `ApplyEncounterOutcome()` is never called

**After EN-UI**: When an encounter triggers during travel:
1. Travel pauses
2. Player sees encounter screen with narrative and options
3. Player makes choices (with skill checks)
4. Effects accumulate and are displayed
5. On completion, effects are applied and travel resumes

---

## Current State Assessment

### What Exists (Sim Layer)

| Component | Location | Status |
|-----------|----------|--------|
| `EncounterTemplate` | `src/sim/encounter/` | ✅ Complete |
| `EncounterInstance` | `src/sim/encounter/` | ✅ Complete |
| `EncounterRunner` | `src/sim/encounter/` | ✅ Complete |
| `EncounterContext` | `src/sim/encounter/` | ✅ Complete |
| `SkillCheck` | `src/sim/encounter/` | ✅ Complete |
| `EncounterGenerator` | `src/sim/generation/` | ✅ Complete |
| `ProductionEncounters` | `src/sim/generation/` | ✅ 10 templates |
| `ApplyEncounterOutcome` | `src/sim/campaign/` | ✅ Complete |
| `TravelResult.Paused` | `src/sim/travel/` | ✅ Complete |

### What's Missing (UI Layer)

| Component | Gap |
|-----------|-----|
| `EncounterScreen.tscn` | No scene file |
| `EncounterScreen.cs` | No controller |
| `GameState` encounter flow | Doesn't handle paused travel |
| Travel → Encounter transition | Auto-resolves instead |

---

## Architecture Decisions

### AD1: Separate Scene for Encounters

**Decision**: Create `EncounterScreen.tscn` as a full-screen scene, not a popup overlay.

**Rationale**:
- Encounters are significant events deserving focus
- Cleaner state management (scene transition vs overlay)
- Consistent with mission screen pattern
- Easier to extend for tactical branching (EN3)

### AD2: EncounterRunner Drives Logic

**Decision**: `EncounterScreen` calls `EncounterRunner` methods; it doesn't implement encounter logic.

**Rationale**:
- Follows architecture (scenes are adapters)
- `EncounterRunner` is already tested
- Single source of truth for encounter state

### AD3: TravelState Preserved

**Decision**: When encounter triggers, store `TravelState` in `GameState` for resumption.

**Rationale**:
- Travel may have multiple segments remaining
- Encounter may not be at final destination
- Need to resume from exact point

### AD4: Effects Shown Before Application

**Decision**: Display accumulated effects to player before applying them.

**Rationale**:
- Player sees consequences of choices
- Matches narrative flow (outcome → result)
- Allows for future "undo" or confirmation

---

## Implementation Phases

### Phase 1: GameState Encounter Flow (Priority: Critical)

Wire up the encounter transition in GameState.

#### Step 1.1: Add Encounter State Fields

**File**: `src/core/GameState.cs`

```csharp
// Add fields
private TravelState pausedTravelState = null;
private TravelPlan pausedTravelPlan = null;

// Add scene path
private const string EncounterScene = "res://src/scenes/encounter/EncounterScreen.tscn";
```

#### Step 1.2: Modify TravelTo to Handle Paused

**File**: `src/core/GameState.cs`

```csharp
public bool TravelTo(int systemId)
{
    if (Campaign == null) return false;

    var planner = new TravelPlanner(Campaign.World);
    var plan = planner.PlanRoute(Campaign.CurrentNodeId, systemId);
    
    if (!plan.IsValid)
    {
        GD.Print($"[GameState] Travel failed: {plan.InvalidReason}");
        return false;
    }
    
    var executor = new TravelExecutor(Campaign.Rng);
    var result = executor.Execute(plan, Campaign);
    
    return HandleTravelResult(result, plan);
}

private bool HandleTravelResult(TravelResult result, TravelPlan plan)
{
    switch (result.Status)
    {
        case TravelResultStatus.Completed:
            GD.Print($"[GameState] Arrived at {Campaign.World?.GetSystem(Campaign.CurrentNodeId)?.Name}");
            if (Campaign.CurrentJob == null)
            {
                Campaign.RefreshJobsAtCurrentNode();
            }
            return true;
            
        case TravelResultStatus.Paused:
            // Encounter triggered - transition to encounter screen
            GD.Print($"[GameState] Travel paused for encounter");
            pausedTravelState = result.State;
            pausedTravelPlan = plan;
            Mode = "encounter";
            GetTree().ChangeSceneToFile(EncounterScene);
            return true; // Travel initiated, will complete after encounter
            
        case TravelResultStatus.Interrupted:
            if (result.InterruptReason == TravelInterruptReason.InsufficientFuel)
            {
                GD.Print("[GameState] Travel failed: insufficient fuel");
            }
            else
            {
                GD.Print($"[GameState] Travel interrupted: {result.InterruptReason}");
            }
            return false;
            
        default:
            GD.Print($"[GameState] Travel failed: {result.Status}");
            return false;
    }
}
```

#### Step 1.3: Add Encounter Resolution Method

**File**: `src/core/GameState.cs`

```csharp
/// <summary>
/// Called when encounter completes. Applies effects and resumes travel.
/// </summary>
public void ResolveEncounter(string outcome = "completed")
{
    if (Campaign?.ActiveEncounter == null)
    {
        GD.PrintErr("[GameState] No active encounter to resolve");
        GoToSectorView();
        return;
    }
    
    // Apply accumulated effects
    int effectsApplied = Campaign.ApplyEncounterOutcome(Campaign.ActiveEncounter);
    GD.Print($"[GameState] Applied {effectsApplied} encounter effects");
    
    // Resume travel if we have paused state
    if (pausedTravelState != null && pausedTravelPlan != null)
    {
        var executor = new TravelExecutor(Campaign.Rng);
        var result = executor.Resume(pausedTravelState, Campaign, outcome);
        
        pausedTravelState = null;
        pausedTravelPlan = null;
        
        // Handle resumed travel result (may pause again for another encounter)
        if (!HandleTravelResult(result, pausedTravelPlan))
        {
            // Travel failed or interrupted, go to sector
            GoToSectorView();
        }
        else if (result.Status == TravelResultStatus.Completed)
        {
            // Travel complete, go to sector
            GoToSectorView();
        }
        // If paused again, HandleTravelResult already transitioned to encounter
    }
    else
    {
        // No travel to resume, just go to sector
        GoToSectorView();
    }
}
```

**Acceptance Criteria**:
- [ ] `TravelResult.Paused` triggers encounter screen transition
- [ ] Travel state preserved for resumption
- [ ] `ResolveEncounter()` applies effects and resumes travel
- [ ] Multiple encounters in one journey handled correctly

---

### Phase 2: Encounter Screen Scene (Priority: Critical)

Create the encounter UI scene.

#### Step 2.1: Create Scene Structure

**File**: `src/scenes/encounter/EncounterScreen.tscn`

Scene tree:
```
EncounterScreen (Control)
├── Background (Panel)
├── ContentContainer (VBoxContainer)
│   ├── HeaderLabel (Label) - "ENCOUNTER"
│   ├── NarrativePanel (Panel)
│   │   └── NarrativeLabel (RichTextLabel)
│   ├── OptionsContainer (VBoxContainer) - Dynamic option buttons
│   ├── ResultPanel (Panel) - Hidden until action taken
│   │   └── ResultLabel (RichTextLabel)
│   └── EffectsPanel (Panel) - Shows accumulated effects
│       └── EffectsLabel (Label)
└── ContinueButton (Button) - "Continue" / "Complete"
```

#### Step 2.2: Create Controller Script

**File**: `src/scenes/encounter/EncounterScreen.cs`

```csharp
using Godot;
using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// Encounter screen controller. Displays encounter narrative and options,
/// handles player choices, and shows results.
/// </summary>
public partial class EncounterScreen : Control
{
    // Node references
    private Label headerLabel;
    private RichTextLabel narrativeLabel;
    private VBoxContainer optionsContainer;
    private Panel resultPanel;
    private RichTextLabel resultLabel;
    private Panel effectsPanel;
    private Label effectsLabel;
    private Button continueButton;
    
    // State
    private EncounterRunner runner;
    private EncounterInstance instance;
    private EncounterContext context;
    private bool awaitingContinue = false;
    
    public override void _Ready()
    {
        GetNodeReferences();
        ConnectSignals();
        InitializeEncounter();
    }
    
    private void GetNodeReferences()
    {
        headerLabel = GetNode<Label>("%HeaderLabel");
        narrativeLabel = GetNode<RichTextLabel>("%NarrativeLabel");
        optionsContainer = GetNode<VBoxContainer>("%OptionsContainer");
        resultPanel = GetNode<Panel>("%ResultPanel");
        resultLabel = GetNode<RichTextLabel>("%ResultLabel");
        effectsPanel = GetNode<Panel>("%EffectsPanel");
        effectsLabel = GetNode<Label>("%EffectsLabel");
        continueButton = GetNode<Button>("%ContinueButton");
    }
    
    private void ConnectSignals()
    {
        continueButton.Pressed += OnContinuePressed;
    }
    
    private void InitializeEncounter()
    {
        var campaign = GameState.Instance?.Campaign;
        if (campaign?.ActiveEncounter == null)
        {
            GD.PrintErr("[EncounterScreen] No active encounter!");
            GameState.Instance?.GoToSectorView();
            return;
        }
        
        instance = campaign.ActiveEncounter;
        runner = new EncounterRunner(GameState.Instance.EventBus);
        context = EncounterContext.FromCampaign(campaign);
        
        // Start the encounter
        runner.Start(instance);
        
        // Display initial state
        UpdateDisplay();
    }
    
    private void UpdateDisplay()
    {
        if (instance == null) return;
        
        var node = runner.GetCurrentNode(instance);
        if (node == null)
        {
            // Encounter complete
            ShowCompletion();
            return;
        }
        
        // Update header
        headerLabel.Text = instance.Template?.Name ?? "ENCOUNTER";
        
        // Update narrative
        narrativeLabel.Text = ResolveText(node.Text);
        
        // Update options
        UpdateOptions(node);
        
        // Update effects display
        UpdateEffectsDisplay();
        
        // Hide result panel until action taken
        resultPanel.Visible = false;
        
        // Continue button state
        continueButton.Visible = awaitingContinue || instance.IsComplete;
        continueButton.Text = instance.IsComplete ? "Complete" : "Continue";
    }
    
    private void UpdateOptions(EncounterNode node)
    {
        // Clear existing options
        foreach (var child in optionsContainer.GetChildren())
        {
            child.QueueFree();
        }
        
        if (awaitingContinue) return; // Don't show options while showing result
        
        var options = runner.GetAvailableOptions(instance, context);
        
        for (int i = 0; i < options.Count; i++)
        {
            var option = options[i];
            var optionIndex = i;
            
            var btn = CreateOptionButton(option, optionIndex);
            optionsContainer.AddChild(btn);
        }
    }
    
    private Button CreateOptionButton(EncounterOption option, int index)
    {
        var btn = new Button();
        btn.CustomMinimumSize = new Vector2(400, 50);
        btn.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        
        // Build button text
        string text = ResolveText(option.Text);
        
        // Add skill check info if present
        if (option.HasSkillCheck)
        {
            var check = option.SkillCheck;
            var chance = SkillCheck.GetSuccessChance(check, context);
            text += $"\n[{check.Stat}: {chance}% chance]";
        }
        
        btn.Text = text;
        btn.Pressed += () => OnOptionSelected(index);
        
        return btn;
    }
    
    private void OnOptionSelected(int index)
    {
        var result = runner.SelectOption(instance, context, index);
        
        if (!result.IsValid)
        {
            GD.PrintErr($"[EncounterScreen] Invalid option: {result.ErrorMessage}");
            return;
        }
        
        // Show result
        ShowResult(result);
    }
    
    private void ShowResult(EncounterStepResult result)
    {
        resultPanel.Visible = true;
        awaitingContinue = true;
        
        // Build result text
        var resultText = "";
        
        // Check for skill check result in recent events
        // (EncounterRunner publishes SkillCheckResolvedEvent)
        // For now, show generic result
        
        if (instance.IsComplete)
        {
            resultText = "The encounter concludes.";
        }
        else
        {
            var nextNode = runner.GetCurrentNode(instance);
            if (nextNode != null)
            {
                resultText = ResolveText(nextNode.Text);
            }
        }
        
        resultLabel.Text = resultText;
        
        // Update effects display
        UpdateEffectsDisplay();
        
        // Hide options, show continue
        UpdateOptions(null);
        continueButton.Visible = true;
        continueButton.Text = instance.IsComplete ? "Complete" : "Continue";
    }
    
    private void ShowCompletion()
    {
        headerLabel.Text = "ENCOUNTER COMPLETE";
        narrativeLabel.Text = "The encounter has concluded.";
        
        // Clear options
        foreach (var child in optionsContainer.GetChildren())
        {
            child.QueueFree();
        }
        
        // Show final effects
        UpdateEffectsDisplay();
        
        // Show complete button
        continueButton.Visible = true;
        continueButton.Text = "Complete";
    }
    
    private void UpdateEffectsDisplay()
    {
        var effects = runner.GetPendingEffects(instance);
        
        if (effects.Count == 0)
        {
            effectsPanel.Visible = false;
            return;
        }
        
        effectsPanel.Visible = true;
        
        var lines = new List<string>();
        foreach (var effect in effects)
        {
            lines.Add(FormatEffect(effect));
        }
        
        effectsLabel.Text = "Effects:\n" + string.Join("\n", lines);
    }
    
    private string FormatEffect(EncounterEffect effect)
    {
        return effect.Type switch
        {
            EffectType.AddResource => $"  {(effect.Amount >= 0 ? "+" : "")}{effect.Amount} {effect.TargetId}",
            EffectType.CrewInjury => $"  Crew injured: {effect.StringParam ?? "wounded"}",
            EffectType.CrewXp => $"  +{effect.Amount} XP",
            EffectType.ShipDamage => $"  Ship damage: {effect.Amount}",
            EffectType.FactionRep => $"  {effect.TargetId} rep: {(effect.Amount >= 0 ? "+" : "")}{effect.Amount}",
            EffectType.TimeDelay => $"  Time: +{effect.Amount} day(s)",
            EffectType.SetFlag => $"  Flag set: {effect.TargetId}",
            _ => $"  {effect.Type}"
        };
    }
    
    private void OnContinuePressed()
    {
        if (instance.IsComplete)
        {
            // Encounter done, resolve and return
            GameState.Instance.ResolveEncounter();
        }
        else
        {
            // Continue to next node
            awaitingContinue = false;
            UpdateDisplay();
        }
    }
    
    private string ResolveText(string text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        
        // Replace parameter placeholders
        if (instance?.ResolvedParameters != null)
        {
            foreach (var (key, value) in instance.ResolvedParameters)
            {
                text = text.Replace($"{{{key}}}", value);
            }
        }
        
        return text;
    }
}
```

**Acceptance Criteria**:
- [ ] Encounter screen displays narrative text
- [ ] Options shown as buttons
- [ ] Skill check chances displayed
- [ ] Results shown after selection
- [ ] Effects accumulated and displayed
- [ ] Continue/Complete button works

---

### Phase 3: Skill Check Display (Priority: High)

Enhance skill check feedback.

#### Step 3.1: Subscribe to Skill Check Events

**File**: `src/scenes/encounter/EncounterScreen.cs`

```csharp
private SkillCheckResolvedEvent? lastSkillCheck = null;

private void InitializeEncounter()
{
    // ... existing code ...
    
    // Subscribe to skill check events
    GameState.Instance.EventBus.Subscribe<SkillCheckResolvedEvent>(OnSkillCheckResolved);
}

public override void _ExitTree()
{
    GameState.Instance?.EventBus?.Unsubscribe<SkillCheckResolvedEvent>(OnSkillCheckResolved);
}

private void OnSkillCheckResolved(SkillCheckResolvedEvent evt)
{
    lastSkillCheck = evt;
}
```

#### Step 3.2: Show Skill Check Result

**File**: `src/scenes/encounter/EncounterScreen.cs`

Update `ShowResult()`:

```csharp
private void ShowResult(EncounterStepResult result)
{
    resultPanel.Visible = true;
    awaitingContinue = true;
    
    var resultText = "";
    
    // Show skill check result if we have one
    if (lastSkillCheck.HasValue)
    {
        var check = lastSkillCheck.Value;
        var successText = check.Success ? "[color=green]SUCCESS[/color]" : "[color=red]FAILURE[/color]";
        
        resultText = $"{check.CrewName} attempts {check.Stat}...\n";
        resultText += $"Roll: {check.Roll} + Stat: {check.StatValue}";
        if (check.TraitBonus != 0)
        {
            resultText += $" + Traits: {check.TraitBonus}";
        }
        resultText += $" = {check.Total} vs {check.Difficulty}\n";
        resultText += successText;
        
        if (check.IsCriticalSuccess)
            resultText += " (Critical!)";
        else if (check.IsCriticalFailure)
            resultText += " (Critical Failure!)";
        
        resultText += "\n\n";
        lastSkillCheck = null;
    }
    
    // Add narrative continuation
    if (!instance.IsComplete)
    {
        var nextNode = runner.GetCurrentNode(instance);
        if (nextNode != null)
        {
            resultText += ResolveText(nextNode.Text);
        }
    }
    else
    {
        resultText += "The encounter concludes.";
    }
    
    resultLabel.Text = resultText;
    
    // ... rest of method ...
}
```

**Acceptance Criteria**:
- [ ] Skill check roll shown (roll + stat + traits = total)
- [ ] Success/failure clearly indicated
- [ ] Critical results highlighted
- [ ] Crew name shown

---

### Phase 4: Visual Polish (Priority: Medium)

Make the encounter screen look good.

#### Step 4.1: Style the Panels

**File**: `src/scenes/encounter/EncounterScreen.cs`

Add styling in `_Ready()`:

```csharp
private void ApplyStyles()
{
    // Background
    var bgStyle = new StyleBoxFlat();
    bgStyle.BgColor = new Color(0.05f, 0.05f, 0.1f);
    GetNode<Panel>("Background").AddThemeStyleboxOverride("panel", bgStyle);
    
    // Narrative panel
    var narrativeStyle = new StyleBoxFlat();
    narrativeStyle.BgColor = new Color(0.1f, 0.1f, 0.15f);
    narrativeStyle.BorderWidthTop = 2;
    narrativeStyle.BorderWidthBottom = 2;
    narrativeStyle.BorderWidthLeft = 2;
    narrativeStyle.BorderWidthRight = 2;
    narrativeStyle.BorderColor = Colors.Cyan;
    narrativeStyle.ContentMarginLeft = 20;
    narrativeStyle.ContentMarginRight = 20;
    narrativeStyle.ContentMarginTop = 15;
    narrativeStyle.ContentMarginBottom = 15;
    GetNode<Panel>("%NarrativePanel").AddThemeStyleboxOverride("panel", narrativeStyle);
    
    // Result panel (similar but different color)
    var resultStyle = new StyleBoxFlat();
    resultStyle.BgColor = new Color(0.1f, 0.12f, 0.1f);
    resultStyle.BorderWidthTop = 2;
    resultStyle.BorderWidthBottom = 2;
    resultStyle.BorderWidthLeft = 2;
    resultStyle.BorderWidthRight = 2;
    resultStyle.BorderColor = Colors.Green;
    resultStyle.ContentMarginLeft = 20;
    resultStyle.ContentMarginRight = 20;
    resultStyle.ContentMarginTop = 15;
    resultStyle.ContentMarginBottom = 15;
    resultPanel.AddThemeStyleboxOverride("panel", resultStyle);
    
    // Effects panel
    var effectsStyle = new StyleBoxFlat();
    effectsStyle.BgColor = new Color(0.15f, 0.1f, 0.1f);
    effectsStyle.BorderColor = Colors.Orange;
    effectsStyle.BorderWidthTop = 1;
    effectsStyle.BorderWidthBottom = 1;
    effectsStyle.BorderWidthLeft = 1;
    effectsStyle.BorderWidthRight = 1;
    effectsPanel.AddThemeStyleboxOverride("panel", effectsStyle);
}
```

**Acceptance Criteria**:
- [ ] Consistent visual style with other screens
- [ ] Clear visual hierarchy
- [ ] Readable text

---

## EN-UI Deliverables Checklist

### Phase 1: GameState Encounter Flow ✅
- [x] **1.1** Add encounter state fields to GameState
- [x] **1.2** Modify TravelTo to handle Paused result
- [x] **1.3** Add ResolveEncounter method
- [x] **1.4** Handle multi-encounter journeys

### Phase 2: Encounter Screen Scene ✅
- [x] **2.1** Create EncounterScreen.tscn
- [x] **2.2** Create EncounterScreen.cs controller
- [x] **2.3** Display narrative text
- [x] **2.4** Display options as buttons
- [x] **2.5** Handle option selection
- [x] **2.6** Show results and effects
- [x] **2.7** Handle encounter completion

### Phase 3: Skill Check Display ✅
- [x] **3.1** Subscribe to skill check events
- [x] **3.2** Show detailed skill check results
- [x] **3.3** Display success chance before selection

### Phase 4: Visual Polish ✅
- [x] **4.1** Style panels consistently
- [x] **4.2** Add visual feedback for success/failure
- [x] **4.3** Polish text formatting

---

## Files to Create

| File | Purpose |
|------|---------|
| `src/scenes/encounter/EncounterScreen.tscn` | Encounter UI scene |
| `src/scenes/encounter/EncounterScreen.cs` | Encounter UI controller |
| `src/scenes/encounter/agents.md` | Directory documentation |

## Files to Modify

| File | Changes |
|------|---------|
| `src/core/GameState.cs` | Add encounter flow handling |
| `src/scenes/agents.md` | Add encounter directory |

---

## Testing

### Manual Test Scenarios

#### Scenario 1: Basic Encounter Flow
1. Start campaign
2. Travel to trigger an encounter (may need multiple trips)
3. Verify encounter screen appears
4. Select an option
5. Verify result shown
6. Complete encounter
7. Verify effects applied
8. Verify returned to sector view

#### Scenario 2: Skill Check Encounter
1. Trigger encounter with skill check option
2. Note success chance displayed
3. Select skill check option
4. Verify roll details shown
5. Verify success/failure outcome

#### Scenario 3: Multi-Encounter Journey
1. Plan long journey (multiple segments)
2. Travel (may trigger encounter)
3. Complete encounter
4. Verify travel resumes
5. If another encounter triggers, verify it works
6. Verify final arrival at destination

#### Scenario 4: Effect Application
1. Note starting resources
2. Trigger encounter with resource effects
3. Complete encounter
4. Verify resources changed correctly
5. Check crew injuries if applicable
6. Check faction rep if applicable

---

## Success Criteria

EN-UI is complete when:
- [ ] Encounters display narrative text
- [ ] Options shown with skill check chances
- [ ] Skill check results show full roll breakdown
- [ ] Effects displayed before application
- [ ] Effects applied via `ApplyEncounterOutcome()`
- [ ] Travel resumes after encounter
- [ ] Multiple encounters in one journey work
- [ ] Visual style consistent with game

---

## Future Considerations (Not in EN-UI)

- **EN3 – Tactical Branching**: `TriggerTactical` effect pauses for mission
- **Encounter animations**: Transition effects, text reveal
- **Encounter portraits**: Character art for NPCs
- **Voice/sound**: Audio feedback for encounters
- **Encounter log**: History of past encounters
