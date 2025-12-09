# HH6 – UX & Combat Feel: Implementation Plan

This document breaks down **HH6** from `ROADMAP.md` into concrete implementation steps.

**Goal**: Make combat state clear and satisfying without complex animations.

**Tactical Axes**: Information (all feedback is information)

---

## Current State Assessment

### What We Have (From M0–HH5)

| Component | Status | Notes |
|-----------|--------|-------|
| `ActorView` | ✅ Complete | Basic sprite, health bar, selection |
| `MissionView` | ✅ Complete | Map rendering, fog of war, UI |
| `CombatLog` | ⚠️ Basic | SimLog exists but not player-facing |
| Overwatch indicators | ⚠️ Partial | HH1 added basic indicator |
| Cover indicators | ❌ Missing | No per-unit cover display |
| Status icons | ⚠️ Partial | Suppression icon exists |
| Combat animations | ❌ Missing | No visual feedback on attacks |
| Hit/miss feedback | ❌ Missing | No impact effects |

### What HH6 Requires vs What We Have

| HH6 Requirement | Current Status | Gap |
|-----------------|----------------|-----|
| Overwatch indicators | ⚠️ Partial | Need threat area visualization |
| Cover indicators | ❌ Missing | Need per-unit cover quality display |
| Status effect icons | ⚠️ Partial | Need unified icon system |
| Basic combat animations | ❌ Missing | Need aim/fire animations |
| Hit/miss feedback | ❌ Missing | Need impact flashes, knockback |
| Combat log | ❌ Missing | Need readable player-facing log |
| Damage numbers | ❌ Missing | Need floating damage text |
| Audio feedback | ❌ Missing | Need sound effects |

---

## Architecture Decisions

### Visual Feedback Philosophy

**Decision**: Prioritize clarity over fidelity.

- Every game state should be visually readable
- Feedback should be immediate and unambiguous
- Simple effects that communicate clearly > complex animations
- Color coding should be consistent across all systems

### Color Coding Standard

| Element | Color | Meaning |
|---------|-------|---------|
| Green | `#4CAF50` | Friendly, safe, positive |
| Blue | `#2196F3` | Player units, selection |
| Red | `#F44336` | Enemy, danger, damage |
| Orange | `#FF9800` | Warning, suppression |
| Yellow | `#FFEB3B` | Caution, in-progress |
| Purple | `#9C27B0` | Abilities, special |
| Gray | `#9E9E9E` | Inactive, unavailable |

### Animation Timing

| Animation | Duration | Notes |
|-----------|----------|-------|
| Attack flash | 100ms | Muzzle flash |
| Hit impact | 150ms | Target flash |
| Damage number | 1000ms | Float up and fade |
| Status icon | Persistent | Until effect ends |
| Cover indicator | Persistent | Updates on move |

---

## Implementation Steps

### Phase 1: Cover Indicators (Priority: Critical)

#### Step 1.1: Create CoverIndicator Component

**New File**: `src/scenes/mission/CoverIndicator.cs`

```csharp
using Godot;

namespace FringeTactics;

/// <summary>
/// Shows cover quality for an actor against current threats.
/// </summary>
public partial class CoverIndicator : Node2D
{
    private Actor actor;
    private CombatState combatState;
    private CoverHeight currentCover = CoverHeight.None;
    
    // Visual elements
    private Sprite2D shieldIcon;
    private Label coverLabel;
    
    private static readonly Color NoCoverColor = new(0.8f, 0.2f, 0.2f, 0.8f);
    private static readonly Color LowCoverColor = new(0.9f, 0.6f, 0.2f, 0.8f);
    private static readonly Color HalfCoverColor = new(0.9f, 0.9f, 0.2f, 0.8f);
    private static readonly Color HighCoverColor = new(0.2f, 0.8f, 0.2f, 0.8f);
    private static readonly Color FullCoverColor = new(0.2f, 0.5f, 0.9f, 0.8f);
    
    public void Setup(Actor actor, CombatState combatState)
    {
        this.actor = actor;
        this.combatState = combatState;
        CreateVisuals();
    }
    
    private void CreateVisuals()
    {
        // Shield icon background
        var bg = new ColorRect();
        bg.Size = new Vector2(20, 20);
        bg.Position = new Vector2(GridConstants.TileSize - 22, 2);
        bg.Color = new Color(0, 0, 0, 0.5f);
        AddChild(bg);
        
        // Cover text
        coverLabel = new Label();
        coverLabel.Position = new Vector2(GridConstants.TileSize - 20, 2);
        coverLabel.AddThemeFontSizeOverride("font_size", 12);
        AddChild(coverLabel);
    }
    
    public override void _Process(double delta)
    {
        if (actor == null || combatState == null) return;
        if (actor.State != ActorState.Alive)
        {
            Visible = false;
            return;
        }
        
        UpdateCoverDisplay();
    }
    
    private void UpdateCoverDisplay()
    {
        // Find best cover against visible threats
        var bestCover = CoverHeight.None;
        var hasThreats = false;
        
        foreach (var enemy in combatState.Actors)
        {
            if (enemy.Type == actor.Type) continue;
            if (enemy.State != ActorState.Alive) continue;
            
            // Check if this enemy is a threat (has LOS)
            if (!CombatResolver.HasLineOfSight(enemy.GridPosition, actor.GridPosition, combatState.MapState))
                continue;
            
            hasThreats = true;
            var cover = combatState.MapState.GetCoverAgainst(actor.GridPosition, enemy.GridPosition);
            if ((int)cover > (int)bestCover)
            {
                bestCover = cover;
            }
        }
        
        // Only show if there are threats
        Visible = hasThreats;
        if (!hasThreats) return;
        
        currentCover = bestCover;
        
        // Update display
        coverLabel.Text = bestCover switch
        {
            CoverHeight.None => "⚠",
            CoverHeight.Low => "◐",
            CoverHeight.Half => "◑",
            CoverHeight.High => "●",
            CoverHeight.Full => "■",
            _ => ""
        };
        
        coverLabel.AddThemeColorOverride("font_color", bestCover switch
        {
            CoverHeight.None => NoCoverColor,
            CoverHeight.Low => LowCoverColor,
            CoverHeight.Half => HalfCoverColor,
            CoverHeight.High => HighCoverColor,
            CoverHeight.Full => FullCoverColor,
            _ => Colors.White
        });
    }
}
```

**Acceptance Criteria**:
- [ ] Cover indicator shows on units with threats
- [ ] Color indicates cover quality
- [ ] Updates when unit or threats move
- [ ] Hidden when no threats visible

#### Step 1.2: Integrate CoverIndicator into ActorView

**File**: `src/scenes/mission/ActorView.cs`

```csharp
private CoverIndicator coverIndicator;

public void Setup(Actor actor, CombatState combatState)
{
    // ... existing setup ...
    
    // Add cover indicator
    coverIndicator = new CoverIndicator();
    coverIndicator.Setup(actor, combatState);
    AddChild(coverIndicator);
}
```

**Acceptance Criteria**:
- [ ] All actors have cover indicators
- [ ] Indicators positioned correctly

---

### Phase 2: Status Effect Icons (Priority: Critical)

#### Step 2.1: Create StatusIconBar

**New File**: `src/scenes/mission/StatusIconBar.cs`

```csharp
using Godot;
using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// Displays active status effect icons for an actor.
/// </summary>
public partial class StatusIconBar : HBoxContainer
{
    private Actor actor;
    private readonly Dictionary<string, TextureRect> activeIcons = new();
    
    // Icon definitions
    private static readonly Dictionary<string, (string icon, Color color)> IconDefs = new()
    {
        ["suppressed"] = ("⚡", new Color(1f, 0.6f, 0f)),
        ["stunned"] = ("💫", new Color(1f, 1f, 0f)),
        ["burning"] = ("🔥", new Color(1f, 0.3f, 0f)),
        ["bleeding"] = ("💧", new Color(0.8f, 0f, 0f)),
        ["overwatch"] = ("👁", new Color(0.3f, 0.7f, 1f)),
        ["reloading"] = ("🔄", new Color(0.7f, 0.7f, 0.7f)),
        ["channeling"] = ("⏳", new Color(0.9f, 0.9f, 0.2f)),
    };
    
    public void Setup(Actor actor)
    {
        this.actor = actor;
        Position = new Vector2(0, -GridConstants.TileSize - 8);
        
        // Subscribe to effect changes
        actor.Effects.EffectApplied += OnEffectApplied;
        actor.Effects.EffectRemoved += OnEffectRemoved;
        actor.Overwatch.StateChanged += OnOverwatchChanged;
    }
    
    private void OnEffectApplied(IEffect effect)
    {
        AddIcon(effect.Id);
    }
    
    private void OnEffectRemoved(IEffect effect)
    {
        RemoveIcon(effect.Id);
    }
    
    private void OnOverwatchChanged(OverwatchState state)
    {
        if (state.IsActive)
            AddIcon("overwatch");
        else
            RemoveIcon("overwatch");
    }
    
    public override void _Process(double delta)
    {
        // Update transient states
        UpdateTransientIcons();
    }
    
    private void UpdateTransientIcons()
    {
        // Reloading
        if (actor.IsReloading && !activeIcons.ContainsKey("reloading"))
            AddIcon("reloading");
        else if (!actor.IsReloading && activeIcons.ContainsKey("reloading"))
            RemoveIcon("reloading");
        
        // Channeling
        if (actor.IsChanneling && !activeIcons.ContainsKey("channeling"))
            AddIcon("channeling");
        else if (!actor.IsChanneling && activeIcons.ContainsKey("channeling"))
            RemoveIcon("channeling");
    }
    
    private void AddIcon(string effectId)
    {
        if (activeIcons.ContainsKey(effectId)) return;
        if (!IconDefs.TryGetValue(effectId, out var def)) return;
        
        var label = new Label();
        label.Text = def.icon;
        label.AddThemeColorOverride("font_color", def.color);
        label.AddThemeFontSizeOverride("font_size", 14);
        AddChild(label);
        
        // Store as TextureRect placeholder (using Label for emoji icons)
        activeIcons[effectId] = null;
    }
    
    private void RemoveIcon(string effectId)
    {
        if (!activeIcons.ContainsKey(effectId)) return;
        
        // Find and remove the label
        foreach (var child in GetChildren())
        {
            if (child is Label label && IconDefs.TryGetValue(effectId, out var def) && label.Text == def.icon)
            {
                label.QueueFree();
                break;
            }
        }
        
        activeIcons.Remove(effectId);
    }
    
    public void Cleanup()
    {
        if (actor != null)
        {
            actor.Effects.EffectApplied -= OnEffectApplied;
            actor.Effects.EffectRemoved -= OnEffectRemoved;
        }
    }
}
```

**Acceptance Criteria**:
- [ ] Status icons appear when effects applied
- [ ] Icons disappear when effects end
- [ ] Multiple icons display in row
- [ ] Transient states (reload, channel) show

---

### Phase 3: Combat Animations (Priority: High)

#### Step 3.1: Create AttackVisualizer

**New File**: `src/scenes/mission/AttackVisualizer.cs`

```csharp
using Godot;
using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// Handles visual feedback for attacks: tracers, impacts, damage numbers.
/// </summary>
public partial class AttackVisualizer : Node2D
{
    private readonly List<Node2D> activeEffects = new();
    
    // Colors
    private static readonly Color TracerColor = new(1f, 0.9f, 0.3f);
    private static readonly Color HitColor = new(1f, 0.3f, 0.3f);
    private static readonly Color MissColor = new(0.7f, 0.7f, 0.7f);
    private static readonly Color CritColor = new(1f, 0.8f, 0f);
    
    /// <summary>
    /// Show attack visual from attacker to target.
    /// </summary>
    public void ShowAttack(Vector2 from, Vector2 to, AttackResult result)
    {
        // Muzzle flash at attacker
        ShowMuzzleFlash(from);
        
        // Tracer line
        ShowTracer(from, to, result.Hit);
        
        // Impact at target
        if (result.Hit)
        {
            ShowHitImpact(to);
            ShowDamageNumber(to, result.Damage, false);
        }
        else
        {
            ShowMissIndicator(to);
        }
    }
    
    /// <summary>
    /// Show suppressive fire visual (multiple tracers).
    /// </summary>
    public void ShowSuppressiveFire(Vector2 from, Vector2 to, bool hit)
    {
        ShowMuzzleFlash(from);
        
        // Multiple tracers with spread
        for (int i = 0; i < 5; i++)
        {
            var spread = new Vector2(
                (float)GD.RandRange(-15, 15),
                (float)GD.RandRange(-15, 15)
            );
            ShowTracer(from, to + spread, hit, 0.05f + i * 0.02f);
        }
        
        if (hit)
        {
            ShowHitImpact(to);
        }
    }
    
    /// <summary>
    /// Show overwatch reaction fire.
    /// </summary>
    public void ShowOverwatchFire(Vector2 from, Vector2 to, AttackResult result)
    {
        // Quick flash to indicate reaction
        ShowReactionIndicator(from);
        ShowAttack(from, to, result);
    }
    
    private void ShowMuzzleFlash(Vector2 position)
    {
        var flash = new ColorRect();
        flash.Size = new Vector2(12, 12);
        flash.Position = position - new Vector2(6, 6);
        flash.Color = new Color(1f, 0.9f, 0.5f, 1f);
        AddChild(flash);
        activeEffects.Add(flash);
        
        // Fade out
        var tween = CreateTween();
        tween.TweenProperty(flash, "modulate:a", 0f, 0.1f);
        tween.TweenCallback(Callable.From(() => {
            flash.QueueFree();
            activeEffects.Remove(flash);
        }));
    }
    
    private void ShowTracer(Vector2 from, Vector2 to, bool hit, float delay = 0f)
    {
        var tracer = new Line2D();
        tracer.Width = 2f;
        tracer.DefaultColor = hit ? TracerColor : MissColor;
        tracer.AddPoint(from);
        tracer.AddPoint(from); // Start at same point
        AddChild(tracer);
        activeEffects.Add(tracer);
        
        // Animate tracer
        var tween = CreateTween();
        if (delay > 0)
        {
            tween.TweenInterval(delay);
        }
        tween.TweenMethod(Callable.From<float>((t) => {
            tracer.SetPointPosition(1, from.Lerp(to, t));
        }), 0f, 1f, 0.08f);
        tween.TweenProperty(tracer, "modulate:a", 0f, 0.15f);
        tween.TweenCallback(Callable.From(() => {
            tracer.QueueFree();
            activeEffects.Remove(tracer);
        }));
    }
    
    private void ShowHitImpact(Vector2 position)
    {
        var impact = new ColorRect();
        impact.Size = new Vector2(20, 20);
        impact.Position = position - new Vector2(10, 10);
        impact.Color = HitColor;
        AddChild(impact);
        activeEffects.Add(impact);
        
        // Flash and fade
        var tween = CreateTween();
        tween.TweenProperty(impact, "scale", new Vector2(1.5f, 1.5f), 0.05f);
        tween.TweenProperty(impact, "modulate:a", 0f, 0.1f);
        tween.TweenCallback(Callable.From(() => {
            impact.QueueFree();
            activeEffects.Remove(impact);
        }));
    }
    
    private void ShowMissIndicator(Vector2 position)
    {
        var miss = new Label();
        miss.Text = "MISS";
        miss.Position = position - new Vector2(15, 10);
        miss.AddThemeFontSizeOverride("font_size", 12);
        miss.AddThemeColorOverride("font_color", MissColor);
        AddChild(miss);
        activeEffects.Add(miss);
        
        // Float up and fade
        var tween = CreateTween();
        tween.SetParallel(true);
        tween.TweenProperty(miss, "position:y", position.Y - 30, 0.5f);
        tween.TweenProperty(miss, "modulate:a", 0f, 0.5f);
        tween.SetParallel(false);
        tween.TweenCallback(Callable.From(() => {
            miss.QueueFree();
            activeEffects.Remove(miss);
        }));
    }
    
    private void ShowDamageNumber(Vector2 position, int damage, bool isCrit)
    {
        var dmgLabel = new Label();
        dmgLabel.Text = damage.ToString();
        dmgLabel.Position = position - new Vector2(10, 0);
        dmgLabel.AddThemeFontSizeOverride("font_size", isCrit ? 18 : 14);
        dmgLabel.AddThemeColorOverride("font_color", isCrit ? CritColor : HitColor);
        AddChild(dmgLabel);
        activeEffects.Add(dmgLabel);
        
        // Float up and fade
        var tween = CreateTween();
        tween.SetParallel(true);
        tween.TweenProperty(dmgLabel, "position:y", position.Y - 40, 0.8f);
        tween.TweenProperty(dmgLabel, "modulate:a", 0f, 0.8f).SetDelay(0.3f);
        tween.SetParallel(false);
        tween.TweenCallback(Callable.From(() => {
            dmgLabel.QueueFree();
            activeEffects.Remove(dmgLabel);
        }));
    }
    
    private void ShowReactionIndicator(Vector2 position)
    {
        var indicator = new Label();
        indicator.Text = "⚡ REACTION";
        indicator.Position = position - new Vector2(30, 20);
        indicator.AddThemeFontSizeOverride("font_size", 10);
        indicator.AddThemeColorOverride("font_color", new Color(1f, 0.8f, 0.2f));
        AddChild(indicator);
        activeEffects.Add(indicator);
        
        var tween = CreateTween();
        tween.TweenProperty(indicator, "modulate:a", 0f, 0.5f).SetDelay(0.3f);
        tween.TweenCallback(Callable.From(() => {
            indicator.QueueFree();
            activeEffects.Remove(indicator);
        }));
    }
}
```

**Acceptance Criteria**:
- [ ] Muzzle flash shows on attack
- [ ] Tracer lines animate from attacker to target
- [ ] Hit impacts flash red
- [ ] Miss shows "MISS" text
- [ ] Damage numbers float up

#### Step 3.2: Integrate AttackVisualizer into MissionView

**File**: `src/scenes/mission/MissionView.cs`

```csharp
private AttackVisualizer attackVisualizer;

private void SetupVisualizers()
{
    attackVisualizer = new AttackVisualizer();
    AddChild(attackVisualizer);
}

private void OnAttackResolved(Actor attacker, Actor target, AttackResult result)
{
    var attackerPos = GetActorView(attacker.Id)?.GlobalPosition ?? Vector2.Zero;
    var targetPos = GetActorView(target.Id)?.GlobalPosition ?? Vector2.Zero;
    
    attackVisualizer.ShowAttack(attackerPos, targetPos, result);
}

private void OnSuppressionApplied(Actor attacker, Actor target, bool hit)
{
    var attackerPos = GetActorView(attacker.Id)?.GlobalPosition ?? Vector2.Zero;
    var targetPos = GetActorView(target.Id)?.GlobalPosition ?? Vector2.Zero;
    
    attackVisualizer.ShowSuppressiveFire(attackerPos, targetPos, hit);
}

private void OnOverwatchFired(Actor overwatcher, Actor target, AttackResult result)
{
    var attackerPos = GetActorView(overwatcher.Id)?.GlobalPosition ?? Vector2.Zero;
    var targetPos = GetActorView(target.Id)?.GlobalPosition ?? Vector2.Zero;
    
    attackVisualizer.ShowOverwatchFire(attackerPos, targetPos, result);
}
```

**Acceptance Criteria**:
- [ ] Attack events trigger visuals
- [ ] Suppression shows burst effect
- [ ] Overwatch shows reaction indicator

---

### Phase 4: Combat Log (Priority: High)

#### Step 4.1: Create CombatLogPanel

**New File**: `src/scenes/mission/CombatLogPanel.cs`

```csharp
using Godot;
using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// Player-facing combat log showing recent events.
/// </summary>
public partial class CombatLogPanel : Control
{
    private VBoxContainer logContainer;
    private ScrollContainer scrollContainer;
    private readonly List<Label> logEntries = new();
    private const int MaxEntries = 50;
    
    // Colors for different event types
    private static readonly Color DamageColor = new(1f, 0.4f, 0.4f);
    private static readonly Color HealColor = new(0.4f, 1f, 0.4f);
    private static readonly Color InfoColor = new(0.8f, 0.8f, 0.8f);
    private static readonly Color WarningColor = new(1f, 0.8f, 0.2f);
    private static readonly Color CriticalColor = new(1f, 0.2f, 0.2f);
    
    public override void _Ready()
    {
        CreateUI();
    }
    
    private void CreateUI()
    {
        // Background
        var bg = new ColorRect();
        bg.Size = new Vector2(300, 150);
        bg.Color = new Color(0.1f, 0.1f, 0.1f, 0.85f);
        AddChild(bg);
        
        // Title
        var title = new Label();
        title.Text = "COMBAT LOG";
        title.Position = new Vector2(10, 5);
        title.AddThemeFontSizeOverride("font_size", 12);
        title.AddThemeColorOverride("font_color", Colors.Gray);
        AddChild(title);
        
        // Scroll container
        scrollContainer = new ScrollContainer();
        scrollContainer.Position = new Vector2(5, 25);
        scrollContainer.Size = new Vector2(290, 120);
        AddChild(scrollContainer);
        
        // Log container
        logContainer = new VBoxContainer();
        scrollContainer.AddChild(logContainer);
    }
    
    public void LogAttack(string attackerName, string targetName, int damage, bool hit, CoverHeight cover)
    {
        var coverText = cover != CoverHeight.None ? $" [{cover}]" : "";
        
        if (hit)
        {
            AddEntry($"{attackerName} hit {targetName} for {damage} damage{coverText}", DamageColor);
        }
        else
        {
            AddEntry($"{attackerName} missed {targetName}{coverText}", InfoColor);
        }
    }
    
    public void LogDeath(string actorName, string killerName)
    {
        AddEntry($"☠ {actorName} was killed by {killerName}", CriticalColor);
    }
    
    public void LogSuppression(string attackerName, string targetName)
    {
        AddEntry($"⚡ {attackerName} suppressed {targetName}", WarningColor);
    }
    
    public void LogOverwatch(string attackerName, string targetName, bool hit)
    {
        var result = hit ? "hit" : "missed";
        AddEntry($"👁 {attackerName} overwatch {result} {targetName}", WarningColor);
    }
    
    public void LogPhaseChange(MissionPhase phase)
    {
        AddEntry($"═══ {phase.ToString().ToUpper()} PHASE ═══", InfoColor);
    }
    
    public void LogWave(string waveName)
    {
        AddEntry($"⚠ REINFORCEMENTS: {waveName}", CriticalColor);
    }
    
    public void LogExtraction(string actorName)
    {
        AddEntry($"✓ {actorName} extracted", HealColor);
    }
    
    public void LogObjective(string description, bool completed)
    {
        var status = completed ? "COMPLETED" : "FAILED";
        var color = completed ? HealColor : CriticalColor;
        AddEntry($"★ {description}: {status}", color);
    }
    
    private void AddEntry(string text, Color color)
    {
        var entry = new Label();
        entry.Text = $"[{GetTimeStamp()}] {text}";
        entry.AddThemeFontSizeOverride("font_size", 11);
        entry.AddThemeColorOverride("font_color", color);
        entry.AutowrapMode = TextServer.AutowrapMode.Word;
        
        logContainer.AddChild(entry);
        logEntries.Add(entry);
        
        // Remove old entries
        while (logEntries.Count > MaxEntries)
        {
            var oldest = logEntries[0];
            logEntries.RemoveAt(0);
            oldest.QueueFree();
        }
        
        // Scroll to bottom
        CallDeferred(nameof(ScrollToBottom));
    }
    
    private void ScrollToBottom()
    {
        scrollContainer.ScrollVertical = (int)scrollContainer.GetVScrollBar().MaxValue;
    }
    
    private string GetTimeStamp()
    {
        // Could use in-game time or real time
        return Time.GetTicksMsec().ToString()[^4..];
    }
}
```

**Acceptance Criteria**:
- [ ] Log shows attack results
- [ ] Log shows deaths
- [ ] Log shows phase changes
- [ ] Log auto-scrolls
- [ ] Color coding by event type

#### Step 4.2: Wire CombatLogPanel to Events

**File**: `src/scenes/mission/MissionView.cs`

```csharp
private CombatLogPanel combatLog;

private void SetupUI()
{
    // ... existing UI setup ...
    
    combatLog = new CombatLogPanel();
    combatLog.Position = new Vector2(GetViewportRect().Size.X - 310, GetViewportRect().Size.Y - 160);
    uiLayer.AddChild(combatLog);
}

private void SubscribeToCombatEvents()
{
    CombatState.AttackSystem.AttackResolved += OnAttackForLog;
    CombatState.AttackSystem.ActorDied += OnDeathForLog;
    CombatState.Suppression.SuppressionApplied += OnSuppressionForLog;
    CombatState.OverwatchSystem.ReactionFired += OnOverwatchForLog;
    CombatState.Phases.PhaseChanged += OnPhaseChangeForLog;
    CombatState.Waves.WaveTriggered += OnWaveForLog;
    CombatState.Extraction.ActorExtracted += OnExtractionForLog;
    CombatState.Objectives.ObjectiveCompleted += OnObjectiveForLog;
    CombatState.Objectives.ObjectiveFailed += OnObjectiveFailedForLog;
}

private void OnAttackForLog(Actor attacker, Actor target, AttackResult result)
{
    combatLog.LogAttack(
        GetActorDisplayName(attacker),
        GetActorDisplayName(target),
        result.Damage,
        result.Hit,
        result.TargetCoverHeight
    );
}

private void OnDeathForLog(Actor victim, Actor killer)
{
    combatLog.LogDeath(GetActorDisplayName(victim), GetActorDisplayName(killer));
}

private string GetActorDisplayName(Actor actor)
{
    if (!string.IsNullOrEmpty(actor.Name))
        return actor.Name;
    return $"{actor.Type}#{actor.Id}";
}
```

**Acceptance Criteria**:
- [ ] All combat events logged
- [ ] Actor names display correctly
- [ ] Log positioned in corner

---

### Phase 5: Enhanced Overwatch Visualization (Priority: High)

#### Step 5.1: Improve OverwatchIndicator

**File**: `src/scenes/mission/OverwatchIndicator.cs`

Update to show clearer threat zones:

```csharp
public override void _Draw()
{
    if (!isVisible || actor == null || !actor.IsOnOverwatch) return;
    
    var range = actor.Overwatch.CustomRange > 0 
        ? actor.Overwatch.CustomRange 
        : actor.EquippedWeapon?.Range ?? 8;
    var pixelRange = range * GridConstants.TileSize;
    
    // Draw range circle outline
    DrawArc(Vector2.Zero, pixelRange, 0, Mathf.Tau, 64, 
            zoneColor with { A = 0.6f }, 2f, true);
    
    // Draw filled zone with lower alpha
    DrawCircle(Vector2.Zero, pixelRange, zoneColor with { A = 0.1f });
    
    // Draw "eye" icon at center
    DrawString(ThemeDB.FallbackFont, new Vector2(-6, 6), "👁", 
               HorizontalAlignment.Center, -1, 16, zoneColor with { A = 0.8f });
    
    // If cone mode, draw cone
    if (actor.Overwatch.FacingDirection != null)
    {
        DrawCone(pixelRange);
    }
}

private void DrawCone(float range)
{
    var facing = actor.Overwatch.FacingDirection.Value;
    var facingAngle = Mathf.Atan2(facing.Y, facing.X);
    var halfAngle = Mathf.DegToRad(actor.Overwatch.ConeAngle / 2f);
    
    // Draw cone edges
    var leftEdge = new Vector2(
        Mathf.Cos(facingAngle - halfAngle) * range,
        Mathf.Sin(facingAngle - halfAngle) * range
    );
    var rightEdge = new Vector2(
        Mathf.Cos(facingAngle + halfAngle) * range,
        Mathf.Sin(facingAngle + halfAngle) * range
    );
    
    DrawLine(Vector2.Zero, leftEdge, zoneColor with { A = 0.8f }, 2f);
    DrawLine(Vector2.Zero, rightEdge, zoneColor with { A = 0.8f }, 2f);
    
    // Draw arc
    DrawArc(Vector2.Zero, range, facingAngle - halfAngle, facingAngle + halfAngle, 
            32, zoneColor with { A = 0.8f }, 2f);
}
```

**Acceptance Criteria**:
- [ ] Overwatch zones clearly visible
- [ ] Cone mode shows direction
- [ ] Eye icon indicates overwatch

---

### Phase 6: Tooltip System (Priority: Medium)

#### Step 6.1: Create TooltipManager

**New File**: `src/scenes/mission/TooltipManager.cs`

```csharp
using Godot;

namespace FringeTactics;

/// <summary>
/// Manages hover tooltips for actors and interactables.
/// </summary>
public partial class TooltipManager : Control
{
    private Panel tooltipPanel;
    private VBoxContainer content;
    private Label nameLabel;
    private Label statsLabel;
    private Label statusLabel;
    
    private Node2D currentTarget;
    private float hoverTime = 0f;
    private const float HoverDelay = 0.3f;
    
    public override void _Ready()
    {
        CreateTooltip();
        Visible = false;
    }
    
    private void CreateTooltip()
    {
        tooltipPanel = new Panel();
        tooltipPanel.Size = new Vector2(180, 100);
        AddChild(tooltipPanel);
        
        content = new VBoxContainer();
        content.Position = new Vector2(8, 8);
        tooltipPanel.AddChild(content);
        
        nameLabel = new Label();
        nameLabel.AddThemeFontSizeOverride("font_size", 14);
        content.AddChild(nameLabel);
        
        statsLabel = new Label();
        statsLabel.AddThemeFontSizeOverride("font_size", 11);
        content.AddChild(statsLabel);
        
        statusLabel = new Label();
        statusLabel.AddThemeFontSizeOverride("font_size", 11);
        statusLabel.AddThemeColorOverride("font_color", Colors.Yellow);
        content.AddChild(statusLabel);
    }
    
    public void ShowActorTooltip(Actor actor, Vector2 position)
    {
        nameLabel.Text = actor.Name ?? $"{actor.Type} #{actor.Id}";
        nameLabel.AddThemeColorOverride("font_color", 
            actor.Type == ActorType.Crew ? Colors.LightBlue : Colors.Red);
        
        statsLabel.Text = $"HP: {actor.Hp}/{actor.MaxHp}\n" +
                         $"Ammo: {actor.CurrentMagazine}/{actor.EquippedWeapon.MagazineSize}\n" +
                         $"Weapon: {actor.EquippedWeapon.Name}";
        
        var statuses = new List<string>();
        if (actor.IsOnOverwatch) statuses.Add("Overwatch");
        if (actor.IsSuppressed()) statuses.Add("Suppressed");
        if (actor.IsReloading) statuses.Add("Reloading");
        if (actor.IsChanneling) statuses.Add("Channeling");
        
        statusLabel.Text = statuses.Count > 0 ? string.Join(", ", statuses) : "";
        statusLabel.Visible = statuses.Count > 0;
        
        Position = position + new Vector2(20, 20);
        Visible = true;
    }
    
    public void ShowInteractableTooltip(Interactable interactable, Vector2 position)
    {
        nameLabel.Text = interactable.Type switch
        {
            InteractableTypes.Door => "Door",
            InteractableTypes.Terminal => "Terminal",
            InteractableTypes.Loot => "Loot",
            _ => interactable.Type
        };
        nameLabel.AddThemeColorOverride("font_color", Colors.White);
        
        statsLabel.Text = $"State: {interactable.State}";
        statusLabel.Text = "";
        statusLabel.Visible = false;
        
        Position = position + new Vector2(20, 20);
        Visible = true;
    }
    
    public void Hide()
    {
        Visible = false;
        currentTarget = null;
    }
}
```

**Acceptance Criteria**:
- [ ] Hover shows actor stats
- [ ] Hover shows interactable info
- [ ] Tooltip follows mouse
- [ ] Brief delay before showing

---

### Phase 7: Audio Feedback (Priority: Medium)

#### Step 7.1: Create AudioManager

**New File**: `src/scenes/mission/AudioManager.cs`

```csharp
using Godot;
using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// Manages combat audio feedback.
/// </summary>
public partial class AudioManager : Node
{
    private readonly Dictionary<string, AudioStreamPlayer> sounds = new();
    
    // Sound effect paths (placeholder - would be actual audio files)
    private static readonly Dictionary<string, string> SoundPaths = new()
    {
        ["gunshot"] = "res://assets/audio/gunshot.wav",
        ["hit"] = "res://assets/audio/hit.wav",
        ["miss"] = "res://assets/audio/miss.wav",
        ["reload"] = "res://assets/audio/reload.wav",
        ["suppression"] = "res://assets/audio/suppression.wav",
        ["overwatch"] = "res://assets/audio/overwatch.wav",
        ["death"] = "res://assets/audio/death.wav",
        ["alert"] = "res://assets/audio/alert.wav",
        ["wave"] = "res://assets/audio/wave.wav",
        ["objective"] = "res://assets/audio/objective.wav",
    };
    
    public override void _Ready()
    {
        // Pre-load sounds (if files exist)
        foreach (var kvp in SoundPaths)
        {
            if (ResourceLoader.Exists(kvp.Value))
            {
                var player = new AudioStreamPlayer();
                player.Stream = GD.Load<AudioStream>(kvp.Value);
                AddChild(player);
                sounds[kvp.Key] = player;
            }
        }
    }
    
    public void PlaySound(string soundId, float volumeDb = 0f)
    {
        if (sounds.TryGetValue(soundId, out var player))
        {
            player.VolumeDb = volumeDb;
            player.Play();
        }
    }
    
    public void PlayAttackSound(bool hit)
    {
        PlaySound("gunshot");
        PlaySound(hit ? "hit" : "miss", -5f);
    }
    
    public void PlaySuppressionSound()
    {
        PlaySound("suppression");
    }
    
    public void PlayOverwatchSound()
    {
        PlaySound("overwatch");
    }
    
    public void PlayDeathSound()
    {
        PlaySound("death");
    }
    
    public void PlayAlertSound()
    {
        PlaySound("alert");
    }
    
    public void PlayWaveSound()
    {
        PlaySound("wave");
    }
}
```

**Acceptance Criteria**:
- [ ] Sound plays on attack
- [ ] Different sounds for hit/miss
- [ ] Alert sound on detection
- [ ] Wave announcement sound

---

## Testing Checklist

### Manual Testing

1. **Cover Indicators**
   - [ ] Cover icon shows on units with threats
   - [ ] Color reflects cover quality
   - [ ] Updates when moving
   - [ ] Hidden when no threats

2. **Status Icons**
   - [ ] Suppression icon appears
   - [ ] Overwatch icon appears
   - [ ] Reload icon appears
   - [ ] Icons disappear when effect ends

3. **Combat Animations**
   - [ ] Muzzle flash on attack
   - [ ] Tracer lines animate
   - [ ] Hit impact shows
   - [ ] Damage numbers float
   - [ ] Miss text shows

4. **Combat Log**
   - [ ] Attacks logged with damage
   - [ ] Deaths logged
   - [ ] Phase changes logged
   - [ ] Auto-scrolls to newest

5. **Overwatch Visualization**
   - [ ] Range circle visible
   - [ ] Cone mode shows direction
   - [ ] Eye icon at center

6. **Tooltips**
   - [ ] Hover shows actor info
   - [ ] Shows HP, ammo, status
   - [ ] Works on interactables

7. **Overall Readability**
   - [ ] Can tell who is suppressed
   - [ ] Can tell who is on overwatch
   - [ ] Can tell cover quality
   - [ ] Combat flow is clear

### Automated Tests

Create `tests/scenes/mission/HH6Tests.cs`:

```csharp
[TestSuite]
public class HH6Tests
{
    // === Cover Indicator ===
    [TestCase] CoverIndicator_ShowsCorrectCover_AgainstThreats()
    [TestCase] CoverIndicator_Hidden_WhenNoThreats()
    
    // === Status Icons ===
    [TestCase] StatusIconBar_ShowsIcon_WhenEffectApplied()
    [TestCase] StatusIconBar_RemovesIcon_WhenEffectEnds()
    
    // === Combat Log ===
    [TestCase] CombatLog_LogsAttack_WithCorrectInfo()
    [TestCase] CombatLog_LimitsEntries_ToMaximum()
}
```

---

## Implementation Order

1. **Day 1: Cover & Status**
   - Step 1.1-1.2: Cover indicators
   - Step 2.1: Status icon bar

2. **Day 2: Combat Animations**
   - Step 3.1: AttackVisualizer
   - Step 3.2: Integration

3. **Day 3: Combat Log**
   - Step 4.1: CombatLogPanel
   - Step 4.2: Event wiring

4. **Day 4: Overwatch & Tooltips**
   - Step 5.1: Enhanced overwatch viz
   - Step 6.1: Tooltip system

5. **Day 5: Audio & Polish**
   - Step 7.1: Audio manager
   - Integration testing
   - Bug fixes

---

## Success Criteria for HH6

When HH6 is complete:

1. ✅ Cover quality is always visible
2. ✅ Status effects have clear icons
3. ✅ Attacks have visual feedback
4. ✅ Combat log shows what happened
5. ✅ Overwatch zones are clear
6. ✅ Tooltips provide detail on hover
7. ✅ Combat is readable without pausing
8. ✅ All automated tests pass

**Natural Pause Point**: The Hangar Handover is playable and readable. Ready for external playtesting. Without good feedback, even great mechanics feel opaque - this milestone ensures the vertical slice communicates clearly.

---

## Files to Create/Modify

### New Files
- `src/scenes/mission/CoverIndicator.cs`
- `src/scenes/mission/StatusIconBar.cs`
- `src/scenes/mission/AttackVisualizer.cs`
- `src/scenes/mission/CombatLogPanel.cs`
- `src/scenes/mission/TooltipManager.cs`
- `src/scenes/mission/AudioManager.cs`
- `tests/scenes/mission/HH6Tests.cs`

### Modified Files
- `src/scenes/mission/ActorView.cs` - Add indicators
- `src/scenes/mission/MissionView.cs` - Wire up systems
- `src/scenes/mission/OverwatchIndicator.cs` - Enhanced visuals

---

## Dependencies

- **Requires**: HH1-HH5 (all systems to visualize)
- **Enables**: External playtesting, G3 content expansion

---

## Open Questions

1. **Damage number style**: Floating vs fixed position?
   - *Decision*: Floating up from target.

2. **Log verbosity**: How much detail?
   - *Decision*: Moderate - show damage, cover, but not hit chance.

3. **Audio priority**: What if many sounds at once?
   - *Decision*: Layer sounds, limit simultaneous plays.

4. **Colorblind support**: Alternative indicators?
   - *Future*: Add icon shapes in addition to colors.
