# HH1 – Overwatch & Reaction Fire: Implementation Plan

This document breaks down **HH1** from `ROADMAP.md` into concrete implementation steps.

**Goal**: Make movement across open ground risky. Let defenders set up threat zones that attackers must respect.

**Tactical Axes**: Information + Position + Time

---

## Current State Assessment

### What We Have (From M0–M6)

| Component | Status | Notes |
|-----------|--------|-------|
| `Actor` | ✅ Complete | Has attack target, cooldown, weapon, state machine |
| `AttackSystem` | ✅ Complete | Processes manual attacks and auto-defend |
| `CombatResolver` | ✅ Complete | LOS checks, hit chance, damage calculation |
| `AIController` | ✅ Complete | Enemy decision-making, target selection |
| `PerceptionSystem` | ✅ Complete | Detection states, alarm system |
| `ActorEffects` | ✅ Complete | Status effect system with modifiers |
| `MissionView` | ✅ Complete | Actor views, selection, orders |

### What HH1 Requires vs What We Have

| HH1 Requirement | Current Status | Gap |
|-----------------|----------------|-----|
| Overwatch action | ❌ Missing | Need new actor state and action |
| Reaction fire trigger | ❌ Missing | Need movement detection system |
| Threat zone visualization | ❌ Missing | Need cone/range rendering |
| AI overwatch usage | ❌ Missing | Need AI decision to enter overwatch |
| Overwatch cancellation | ❌ Missing | Need rules for when overwatch ends |

---

## Architecture Decisions

### Overwatch State Model

**Decision**: Add `IsOnOverwatch` state to `Actor` with associated data.

```csharp
public class OverwatchState
{
    public bool IsActive { get; set; } = false;
    public Vector2I? FacingDirection { get; set; } = null;  // null = 360°, else cone
    public float ConeAngle { get; set; } = 90f;             // degrees for cone mode
    public int Range { get; set; } = 0;                     // 0 = weapon range
    public int ShotsRemaining { get; set; } = 1;            // reactions before ending
}
```

**Rationale**:
- Simple boolean + data class keeps Actor clean
- Supports both 360° and cone-based overwatch
- `ShotsRemaining` allows future multi-shot overwatch abilities

### Reaction Fire Trigger

**Decision**: Check for overwatch triggers during movement processing, before position changes.

**Trigger conditions**:
1. Enemy moves within LOS of overwatching unit
2. Enemy is within overwatch range (weapon range or custom)
3. Enemy is within cone angle (if directional)
4. Overwatching unit can fire (has ammo, not stunned)

**Execution**:
- Interrupt movement temporarily
- Execute reaction shot
- Decrement `ShotsRemaining`
- If `ShotsRemaining == 0`, end overwatch

### Overwatch Cancellation Rules

| Event | Cancels Overwatch |
|-------|-------------------|
| Movement order | Yes |
| Attack order | Yes |
| Taking damage | Yes (optional, configurable) |
| Using ability | Yes |
| Reaction shot fired | Only if ShotsRemaining == 0 |
| New overwatch order | Replaces existing |

### Threat Zone Visualization

**Decision**: Render overwatch zones as semi-transparent overlays.

- **360° overwatch**: Circle around unit at weapon range
- **Cone overwatch**: Arc in facing direction
- **Color coding**: Red for enemy overwatch, blue for friendly

---

## Implementation Steps

### Phase 1: Core Overwatch State (Priority: Critical)

#### Step 1.1: Create OverwatchState Class

**New File**: `src/sim/combat/state/OverwatchState.cs`

```csharp
using Godot;

namespace FringeTactics;

/// <summary>
/// Tracks overwatch state for an actor.
/// </summary>
public class OverwatchState
{
    public bool IsActive { get; private set; } = false;
    public Vector2I? FacingDirection { get; private set; } = null;
    public float ConeAngle { get; private set; } = 90f;
    public int CustomRange { get; private set; } = 0;
    public int ShotsRemaining { get; private set; } = 1;
    public int ActivatedTick { get; private set; } = 0;
    
    public event Action<OverwatchState> StateChanged;
    
    public void Activate(int currentTick, Vector2I? facingDirection = null, 
                         float coneAngle = 90f, int customRange = 0, int shots = 1)
    {
        IsActive = true;
        FacingDirection = facingDirection;
        ConeAngle = coneAngle;
        CustomRange = customRange;
        ShotsRemaining = shots;
        ActivatedTick = currentTick;
        StateChanged?.Invoke(this);
    }
    
    public void Deactivate()
    {
        if (!IsActive) return;
        IsActive = false;
        StateChanged?.Invoke(this);
    }
    
    public void ConsumeShot()
    {
        if (ShotsRemaining > 0)
        {
            ShotsRemaining--;
            if (ShotsRemaining == 0)
            {
                Deactivate();
            }
            else
            {
                StateChanged?.Invoke(this);
            }
        }
    }
    
    public bool IsInCone(Vector2I fromPos, Vector2I targetPos)
    {
        if (!IsActive) return false;
        if (FacingDirection == null) return true; // 360° mode
        
        var toTarget = targetPos - fromPos;
        if (toTarget == Vector2I.Zero) return false;
        
        var targetAngle = Mathf.RadToDeg(Mathf.Atan2(toTarget.Y, toTarget.X));
        var facingAngle = Mathf.RadToDeg(Mathf.Atan2(FacingDirection.Value.Y, FacingDirection.Value.X));
        
        var angleDiff = Mathf.Abs(Mathf.Wrap(targetAngle - facingAngle, -180f, 180f));
        return angleDiff <= ConeAngle / 2f;
    }
}
```

**Acceptance Criteria**:
- [ ] `OverwatchState` class exists with activation/deactivation
- [ ] Cone angle calculation works correctly
- [ ] `StateChanged` event fires on state changes
- [ ] `ConsumeShot()` decrements and deactivates when empty

#### Step 1.2: Extend Actor with Overwatch

**File**: `src/sim/combat/state/Actor.cs`

**Add**:
```csharp
// Overwatch state
public OverwatchState Overwatch { get; } = new();

// Events
public event Action<Actor> OverwatchActivated;
public event Action<Actor> OverwatchDeactivated;
public event Action<Actor, Actor> OverwatchTriggered; // (overwatcher, target)
```

**Update constructor**:
```csharp
public Actor(int actorId, ActorType actorType)
{
    // ... existing code ...
    Overwatch.StateChanged += (state) => 
    {
        if (state.IsActive)
            OverwatchActivated?.Invoke(this);
        else
            OverwatchDeactivated?.Invoke(this);
    };
}
```

**Add methods**:
```csharp
public void EnterOverwatch(int currentTick, Vector2I? facingDirection = null)
{
    if (State != ActorState.Alive || !CanFire()) return;
    
    // Cancel other actions
    ClearOrders();
    CancelChannel();
    
    var range = EquippedWeapon?.Range ?? 8;
    Overwatch.Activate(currentTick, facingDirection, 90f, range, 1);
    SimLog.Log($"[Actor] {Type}#{Id} entered overwatch");
}

public void ExitOverwatch()
{
    Overwatch.Deactivate();
}

public bool IsOnOverwatch => Overwatch.IsActive;
```

**Update `SetTarget()` and `SetAttackTarget()`** to cancel overwatch:
```csharp
public void SetTarget(Vector2I target)
{
    ExitOverwatch(); // Cancel overwatch on movement
    // ... existing code ...
}

public void SetAttackTarget(int? targetId)
{
    if (targetId.HasValue)
    {
        ExitOverwatch(); // Cancel overwatch on attack order
    }
    // ... existing code ...
}
```

**Acceptance Criteria**:
- [ ] `Actor.Overwatch` property exists
- [ ] `EnterOverwatch()` activates overwatch state
- [ ] Movement and attack orders cancel overwatch
- [ ] Events fire correctly

---

### Phase 2: Reaction Fire System (Priority: Critical)

#### Step 2.1: Create OverwatchSystem

**New File**: `src/sim/combat/systems/OverwatchSystem.cs`

```csharp
using Godot;
using System;
using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// Manages overwatch triggers and reaction fire.
/// </summary>
public class OverwatchSystem
{
    private readonly CombatState combatState;
    private readonly HashSet<int> actorsMovedThisTick = new();
    
    public event Action<Actor, Actor, AttackResult> ReactionFired; // overwatcher, target, result
    
    public OverwatchSystem(CombatState combatState)
    {
        this.combatState = combatState;
    }
    
    /// <summary>
    /// Register that an actor moved this tick (called before position change).
    /// </summary>
    public void RegisterMovement(Actor movingActor, Vector2I newPosition)
    {
        if (movingActor.Type == ActorType.Crew)
        {
            CheckEnemyOverwatch(movingActor, newPosition);
        }
        else if (movingActor.Type == ActorType.Enemy)
        {
            CheckCrewOverwatch(movingActor, newPosition);
        }
    }
    
    private void CheckEnemyOverwatch(Actor crew, Vector2I newPosition)
    {
        foreach (var enemy in combatState.Actors)
        {
            if (enemy.Type != ActorType.Enemy) continue;
            if (enemy.State != ActorState.Alive) continue;
            if (!enemy.IsOnOverwatch) continue;
            
            TryTriggerOverwatch(enemy, crew, newPosition);
        }
    }
    
    private void CheckCrewOverwatch(Actor enemy, Vector2I newPosition)
    {
        foreach (var crew in combatState.Actors)
        {
            if (crew.Type != ActorType.Crew) continue;
            if (crew.State != ActorState.Alive) continue;
            if (!crew.IsOnOverwatch) continue;
            
            TryTriggerOverwatch(crew, enemy, newPosition);
        }
    }
    
    private void TryTriggerOverwatch(Actor overwatcher, Actor target, Vector2I targetNewPos)
    {
        // Check if can fire
        if (!overwatcher.CanFire()) return;
        
        // Check range
        var distance = CombatResolver.GetDistance(overwatcher.GridPosition, targetNewPos);
        var range = overwatcher.Overwatch.CustomRange > 0 
            ? overwatcher.Overwatch.CustomRange 
            : overwatcher.EquippedWeapon.Range;
        if (distance > range) return;
        
        // Check LOS
        if (!CombatResolver.HasLineOfSight(overwatcher.GridPosition, targetNewPos, combatState.MapState))
            return;
        
        // Check cone (if directional)
        if (!overwatcher.Overwatch.IsInCone(overwatcher.GridPosition, targetNewPos))
            return;
        
        // Execute reaction fire!
        ExecuteReactionFire(overwatcher, target);
    }
    
    private void ExecuteReactionFire(Actor overwatcher, Actor target)
    {
        SimLog.Log($"[Overwatch] {overwatcher.Type}#{overwatcher.Id} triggers on {target.Type}#{target.Id}!");
        
        // Fire the shot
        var result = CombatResolver.ResolveAttack(
            overwatcher, target, overwatcher.EquippedWeapon, 
            combatState.MapState, combatState.Rng);
        
        overwatcher.StartCooldown();
        overwatcher.ConsumeAmmo();
        overwatcher.RecordShot(result.Hit, result.Hit ? result.Damage : 0);
        
        if (result.Hit)
        {
            var isGodMode = (target.Type == ActorType.Crew && DevTools.CrewGodMode) ||
                           (target.Type == ActorType.Enemy && DevTools.EnemyGodMode);
            
            if (!isGodMode)
            {
                target.TakeDamage(result.Damage);
            }
            
            SimLog.Log($"[Overwatch] HIT! {result.Damage} damage. Target HP: {target.Hp}/{target.MaxHp}");
            
            if (target.State == ActorState.Dead)
            {
                overwatcher.RecordKill();
            }
        }
        else
        {
            SimLog.Log($"[Overwatch] MISS! ({result.HitChance:P0} chance)");
        }
        
        // Consume overwatch shot
        overwatcher.Overwatch.ConsumeShot();
        
        // Fire event
        overwatcher.OverwatchTriggered?.Invoke(overwatcher, target);
        ReactionFired?.Invoke(overwatcher, target, result);
    }
    
    /// <summary>
    /// Clear movement tracking at end of tick.
    /// </summary>
    public void EndTick()
    {
        actorsMovedThisTick.Clear();
    }
}
```

**Acceptance Criteria**:
- [ ] `OverwatchSystem` detects movement into overwatch zones
- [ ] Reaction fire executes with proper damage
- [ ] Overwatch consumes shots and deactivates
- [ ] Events fire for UI feedback

#### Step 2.2: Integrate OverwatchSystem into CombatState

**File**: `src/sim/combat/state/CombatState.cs`

**Add member**:
```csharp
public OverwatchSystem OverwatchSystem { get; private set; }
```

**Update constructor**:
```csharp
public CombatState(int seed)
{
    // ... existing code ...
    OverwatchSystem = new OverwatchSystem(this);
}
```

**Hook into movement**:
The `OverwatchSystem.RegisterMovement()` needs to be called when actors move. This should be integrated into the movement processing in `Actor.Tick()` or via events.

**Option A**: Subscribe to `Actor.PositionChanged` event
**Option B**: Call from `MovementSystem` before position updates

**Recommended**: Option B - modify movement processing to check overwatch before committing position change.

**Acceptance Criteria**:
- [ ] `CombatState.OverwatchSystem` property exists
- [ ] Movement triggers overwatch checks
- [ ] Reaction fire happens before movement completes

---

### Phase 3: Player Commands (Priority: High)

#### Step 3.1: Add Overwatch Order to MissionView

**File**: `src/scenes/mission/MissionView.cs`

**Add overwatch command**:
```csharp
// Keyboard shortcut for overwatch (e.g., 'O' key)
private void HandleOverwatchCommand()
{
    if (selectedActors.Count == 0) return;
    
    foreach (var actor in selectedActors)
    {
        if (actor.Type != ActorType.Crew) continue;
        if (actor.State != ActorState.Alive) continue;
        
        actor.EnterOverwatch(CombatState.TimeSystem.CurrentTick);
    }
}
```

**Add UI button** in ability bar or context menu.

**Acceptance Criteria**:
- [ ] 'O' key enters overwatch for selected units
- [ ] UI button available for overwatch
- [ ] Only living crew can enter overwatch

#### Step 3.2: Add Directional Overwatch (Optional)

**File**: `src/scenes/mission/MissionView.cs`

For directional overwatch, player clicks a direction after pressing overwatch:
```csharp
private bool awaitingOverwatchDirection = false;

private void HandleOverwatchDirectionClick(Vector2I clickedTile)
{
    if (!awaitingOverwatchDirection) return;
    
    foreach (var actor in selectedActors)
    {
        var direction = clickedTile - actor.GridPosition;
        if (direction != Vector2I.Zero)
        {
            actor.EnterOverwatch(CombatState.TimeSystem.CurrentTick, direction);
        }
    }
    
    awaitingOverwatchDirection = false;
}
```

**Acceptance Criteria**:
- [ ] Shift+O (or similar) enters directional overwatch mode
- [ ] Click sets facing direction
- [ ] Cone is visualized before confirming

---

### Phase 4: AI Overwatch Usage (Priority: High)

#### Step 4.1: Update AIController for Overwatch

**File**: `src/sim/combat/systems/AIController.cs`

**Add overwatch decision logic**:
```csharp
private void Think(Actor enemy)
{
    var detectionState = combatState.Perception.GetDetectionState(enemy.Id);
    if (detectionState == DetectionState.Idle)
    {
        return;
    }
    
    // If already on overwatch, stay on overwatch unless target is close
    if (enemy.IsOnOverwatch)
    {
        if (ShouldBreakOverwatch(enemy))
        {
            enemy.ExitOverwatch();
        }
        else
        {
            return; // Stay on overwatch
        }
    }
    
    // Consider entering overwatch if in good position
    if (ShouldEnterOverwatch(enemy))
    {
        var facingDirection = GetBestOverwatchDirection(enemy);
        enemy.EnterOverwatch(combatState.TimeSystem.CurrentTick, facingDirection);
        SimLog.Log($"[AI] Enemy#{enemy.Id} entering overwatch");
        return;
    }
    
    // ... existing attack/move logic ...
}

private bool ShouldEnterOverwatch(Actor enemy)
{
    // Enter overwatch if:
    // 1. No visible targets in range
    // 2. In cover position
    // 3. Guarding a chokepoint or objective
    
    var hasTargetInRange = false;
    foreach (var crew in combatState.Actors)
    {
        if (crew.Type != ActorType.Crew || crew.State != ActorState.Alive) continue;
        if (CombatResolver.CanAttack(enemy, crew, enemy.EquippedWeapon, combatState.MapState))
        {
            hasTargetInRange = true;
            break;
        }
    }
    
    // If no target in range but have LOS to likely approach paths, overwatch
    if (!hasTargetInRange)
    {
        // Simple heuristic: 30% chance to overwatch when no targets
        return combatState.Rng.NextFloat() < 0.3f;
    }
    
    return false;
}

private bool ShouldBreakOverwatch(Actor enemy)
{
    // Break overwatch if target is very close (melee range)
    foreach (var crew in combatState.Actors)
    {
        if (crew.Type != ActorType.Crew || crew.State != ActorState.Alive) continue;
        var distance = CombatResolver.GetDistance(enemy.GridPosition, crew.GridPosition);
        if (distance <= 2) return true;
    }
    return false;
}

private Vector2I? GetBestOverwatchDirection(Actor enemy)
{
    // Find direction with most likely enemy approach
    // For now, return null (360° overwatch)
    return null;
}
```

**Acceptance Criteria**:
- [ ] AI enemies enter overwatch when appropriate
- [ ] AI breaks overwatch when targets are close
- [ ] Overwatch creates defensive positions

---

### Phase 5: Visual Feedback (Priority: High)

#### Step 5.1: Create OverwatchIndicator

**New File**: `src/scenes/mission/OverwatchIndicator.cs`

```csharp
using Godot;

namespace FringeTactics;

/// <summary>
/// Visual indicator for overwatch zones.
/// </summary>
public partial class OverwatchIndicator : Node2D
{
    private Actor actor;
    private Color zoneColor;
    private bool isVisible = false;
    
    public void Setup(Actor actor, bool isEnemy)
    {
        this.actor = actor;
        zoneColor = isEnemy 
            ? new Color(1f, 0.2f, 0.2f, 0.15f)  // Red for enemies
            : new Color(0.2f, 0.5f, 1f, 0.15f); // Blue for crew
        
        actor.OverwatchActivated += OnOverwatchActivated;
        actor.OverwatchDeactivated += OnOverwatchDeactivated;
        actor.Overwatch.StateChanged += OnStateChanged;
    }
    
    private void OnOverwatchActivated(Actor a) => UpdateVisibility();
    private void OnOverwatchDeactivated(Actor a) => UpdateVisibility();
    private void OnStateChanged(OverwatchState state) => QueueRedraw();
    
    private void UpdateVisibility()
    {
        isVisible = actor.IsOnOverwatch;
        QueueRedraw();
    }
    
    public override void _Draw()
    {
        if (!isVisible || actor == null || !actor.IsOnOverwatch) return;
        
        var range = actor.Overwatch.CustomRange > 0 
            ? actor.Overwatch.CustomRange 
            : actor.EquippedWeapon?.Range ?? 8;
        var pixelRange = range * GridConstants.TileSize;
        
        if (actor.Overwatch.FacingDirection == null)
        {
            // 360° circle
            DrawCircle(Vector2.Zero, pixelRange, zoneColor);
            DrawArc(Vector2.Zero, pixelRange, 0, Mathf.Tau, 64, 
                    zoneColor with { A = 0.5f }, 2f);
        }
        else
        {
            // Cone
            var facing = actor.Overwatch.FacingDirection.Value;
            var facingAngle = Mathf.Atan2(facing.Y, facing.X);
            var halfAngle = Mathf.DegToRad(actor.Overwatch.ConeAngle / 2f);
            
            var points = new Vector2[32];
            points[0] = Vector2.Zero;
            for (int i = 0; i < 31; i++)
            {
                var angle = facingAngle - halfAngle + (halfAngle * 2 * i / 30f);
                points[i + 1] = new Vector2(
                    Mathf.Cos(angle) * pixelRange,
                    Mathf.Sin(angle) * pixelRange
                );
            }
            
            DrawColoredPolygon(points, zoneColor);
        }
    }
    
    public void Cleanup()
    {
        if (actor != null)
        {
            actor.OverwatchActivated -= OnOverwatchActivated;
            actor.OverwatchDeactivated -= OnOverwatchDeactivated;
        }
    }
}
```

**Acceptance Criteria**:
- [ ] Overwatch zones render as semi-transparent areas
- [ ] Enemy zones are red, crew zones are blue
- [ ] Cone mode renders correctly
- [ ] Zones update when overwatch state changes

#### Step 5.2: Add Overwatch Icon to ActorView

**File**: `src/scenes/mission/ActorView.cs`

**Add overwatch indicator**:
```csharp
private Label overwatchIcon;

private void CreateOverwatchIcon()
{
    overwatchIcon = new Label();
    overwatchIcon.Text = "👁"; // Or use a custom icon
    overwatchIcon.Position = new Vector2(GridConstants.TileSize - 12, -4);
    overwatchIcon.Visible = false;
    AddChild(overwatchIcon);
}

public void UpdateOverwatchDisplay(bool isOnOverwatch)
{
    overwatchIcon.Visible = isOnOverwatch;
}
```

**Acceptance Criteria**:
- [ ] Overwatch icon appears on units in overwatch
- [ ] Icon is clearly visible
- [ ] Icon disappears when overwatch ends

---

### Phase 6: Integration into MissionView (Priority: High)

#### Step 6.1: Wire Up Overwatch Events

**File**: `src/scenes/mission/MissionView.cs`

```csharp
private void InitializeCombat()
{
    // ... existing code ...
    
    // Subscribe to overwatch events
    CombatState.OverwatchSystem.ReactionFired += OnReactionFired;
    
    foreach (var actor in CombatState.Actors)
    {
        actor.OverwatchActivated += OnActorOverwatchChanged;
        actor.OverwatchDeactivated += OnActorOverwatchChanged;
    }
}

private void OnReactionFired(Actor overwatcher, Actor target, AttackResult result)
{
    // Show reaction fire feedback
    var overwatcherView = GetActorView(overwatcher.Id);
    var targetView = GetActorView(target.Id);
    
    // Flash or highlight
    ShowReactionFireEffect(overwatcherView, targetView, result);
    
    // Auto-pause on reaction fire (optional)
    if (autoPauseOnOverwatch)
    {
        CombatState.TimeSystem.Pause();
    }
}

private void OnActorOverwatchChanged(Actor actor)
{
    var view = GetActorView(actor.Id);
    view?.UpdateOverwatchDisplay(actor.IsOnOverwatch);
}
```

**Acceptance Criteria**:
- [ ] Reaction fire shows visual feedback
- [ ] Actor views update overwatch icons
- [ ] Optional auto-pause on reaction fire

---

## Testing Checklist

### Manual Testing

1. **Enter Overwatch**
   - [ ] Select crew, press 'O' → enters overwatch
   - [ ] Overwatch icon appears on unit
   - [ ] Overwatch zone renders around unit
   - [ ] Unit stops moving/attacking

2. **Reaction Fire Trigger**
   - [ ] Enemy moves into overwatch zone → reaction shot fires
   - [ ] Damage applies correctly
   - [ ] Overwatch ends after shot (single-shot mode)
   - [ ] Miss still consumes overwatch

3. **Overwatch Cancellation**
   - [ ] Move order cancels overwatch
   - [ ] Attack order cancels overwatch
   - [ ] Taking damage cancels overwatch (if configured)

4. **AI Overwatch**
   - [ ] Enemies enter overwatch when no targets in range
   - [ ] Enemies break overwatch when targets are close
   - [ ] AI overwatch triggers on crew movement

5. **Edge Cases**
   - [ ] Multiple overwatchers trigger in sequence
   - [ ] Overwatch doesn't trigger on own team
   - [ ] Dead units don't maintain overwatch
   - [ ] Out of ammo prevents overwatch

### Automated Tests

Create `tests/sim/combat/HH1Tests.cs`:

```csharp
[TestSuite]
public class HH1Tests
{
    // === Overwatch State ===
    [TestCase] OverwatchState_Activate_SetsIsActive()
    [TestCase] OverwatchState_ConsumeShot_DecrementsAndDeactivates()
    [TestCase] OverwatchState_IsInCone_360Mode_AlwaysTrue()
    [TestCase] OverwatchState_IsInCone_ConeMode_ChecksAngle()
    
    // === Actor Overwatch ===
    [TestCase] Actor_EnterOverwatch_ActivatesState()
    [TestCase] Actor_SetTarget_CancelsOverwatch()
    [TestCase] Actor_SetAttackTarget_CancelsOverwatch()
    [TestCase] Actor_EnterOverwatch_RequiresAmmo()
    
    // === Reaction Fire ===
    [TestCase] OverwatchSystem_EnemyMovesIntoZone_TriggersReaction()
    [TestCase] OverwatchSystem_EnemyOutOfRange_NoReaction()
    [TestCase] OverwatchSystem_EnemyBehindWall_NoReaction()
    [TestCase] OverwatchSystem_ConeMode_OnlyTriggersInCone()
    
    // === Integration ===
    [TestCase] Overwatch_ReactionFire_DealsDamage()
    [TestCase] Overwatch_ReactionFire_ConsumesAmmo()
    [TestCase] Overwatch_MultipleOverwatchers_AllTrigger()
}
```

---

## Implementation Order

1. **Day 1: Core State**
   - Step 1.1: Create OverwatchState class
   - Step 1.2: Extend Actor with overwatch

2. **Day 2: Reaction System**
   - Step 2.1: Create OverwatchSystem
   - Step 2.2: Integrate into CombatState

3. **Day 3: Player Commands**
   - Step 3.1: Add overwatch order to MissionView
   - Step 3.2: Add directional overwatch (optional)

4. **Day 4: AI & Visuals**
   - Step 4.1: Update AIController
   - Step 5.1: Create OverwatchIndicator
   - Step 5.2: Add icon to ActorView

5. **Day 5: Integration & Testing**
   - Step 6.1: Wire up events
   - Write automated tests
   - Manual testing and bug fixes

---

## Success Criteria for HH1

When HH1 is complete:

1. ✅ Player can order units into overwatch
2. ✅ Overwatch zones are clearly visualized
3. ✅ Movement through overwatch triggers reaction fire
4. ✅ Reaction fire deals damage and consumes overwatch
5. ✅ AI enemies use overwatch defensively
6. ✅ Overwatch creates meaningful tactical decisions
7. ✅ All automated tests pass

**Natural Pause Point**: Combat now has a defensive layer. Players must think about movement exposure, not just cover. This fundamentally changes how the Hangar Handover's contact phase plays out.

---

## Files to Create/Modify

### New Files
- `src/sim/combat/state/OverwatchState.cs`
- `src/sim/combat/systems/OverwatchSystem.cs`
- `src/scenes/mission/OverwatchIndicator.cs`
- `tests/sim/combat/HH1Tests.cs`

### Modified Files
- `src/sim/combat/state/Actor.cs` - Add overwatch state and methods
- `src/sim/combat/state/CombatState.cs` - Add OverwatchSystem
- `src/sim/combat/systems/AIController.cs` - Add overwatch decisions
- `src/scenes/mission/MissionView.cs` - Add overwatch commands and events
- `src/scenes/mission/ActorView.cs` - Add overwatch icon

---

## Dependencies

- **Requires**: M3 (combat loop), M4 (cover system)
- **Enables**: HH2 (suppression interacts with overwatch), HH5 (AI roles use overwatch)

---

## Open Questions

1. **Overwatch accuracy modifier**: Should overwatch shots have reduced accuracy?
   - *Decision*: No modifier for HH1. Can add as balance lever later.

2. **Multiple reactions per turn**: Should overwatch trigger multiple times per enemy?
   - *Decision*: No, one trigger per enemy movement action.

3. **Overwatch on allies**: Should friendly movement trigger overwatch?
   - *Decision*: No, overwatch only triggers on enemies.

4. **Overwatch through doors**: Should opening a door trigger overwatch?
   - *Decision*: Yes, if enemy becomes visible and moves.
