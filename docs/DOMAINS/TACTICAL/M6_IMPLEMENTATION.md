# M6 – Stealth & Alarm Foundations: Implementation Plan

This document breaks down **Milestone 6** from `ROADMAP.md` into concrete implementation steps.

**Goal**: Minimal stealth and alarm system, compatible with later complex stealth/AI work.

---

## Current State Assessment

### What We Have (From M0–M5)

| Component | Status | Notes |
|-----------|--------|-------|
| `VisibilitySystem` | ✅ Complete | Player-side fog of war, LOS checks via Bresenham |
| `AIController` | ✅ Complete | Enemy AI: finds targets via LOS, moves toward, attacks |
| `Actor` | ✅ Complete | Has `GetVisionRadius()`, position, state |
| `CombatState` | ✅ Complete | Tick-based simulation, events for actor changes |
| `InteractionSystem` | ✅ Complete | Doors affect LOS/movement |
| `TimeSystem` | ✅ Complete | Pause/unpause, tick-based updates |
| `MissionView` | ✅ Complete | UI, fog rendering, actor views |

### What M6 Requires vs What We Have

| M6 Requirement | Current Status | Gap |
|----------------|----------------|-----|
| Enemy perception (LOS + vision radius) | ⚠️ Partial | AI uses LOS for targeting, but no formal "detection" |
| Detection states (Idle → Alerted) | ❌ Missing | Enemies are always "aware" and aggressive |
| Global/area alarm flag | ❌ Missing | No alarm state tracking |
| Auto-pause on detection | ❌ Missing | Auto-pause only on mission end |
| Enemy vision distinct from player | ⚠️ Partial | Same LOS code, but no enemy-specific tracking |

---

## Architecture Decisions

### Detection State Model

**Decision**: Simple two-state model for M6 (Idle → Alerted).

Per DESIGN.md section 7.2 [CORE]:
> - Idle → Alerted/Engaged.
> - Alerted enemies know the last seen position of player units and will attempt to attack.

**States**:
```csharp
public enum DetectionState
{
    Idle,       // Unaware of player presence
    Alerted     // Aware and hostile
}
```

**Rationale**:
- Matches DESIGN.md [CORE] requirements exactly
- Simple to implement and test
- [PLUS] features (Suspicious state, view cones) can be added later
- Compatible with existing AI behavior (Alerted = current behavior)

### Alarm System Model

**Decision**: Global alarm flag with event-based transitions.

**Structure**:
```csharp
public enum AlarmState
{
    Quiet,      // No enemies alerted
    Alerted     // At least one enemy has detected player
}
```

**Transitions**:
- `Quiet → Alerted`: When first enemy transitions to `DetectionState.Alerted`
- No automatic de-escalation in M6 (once alerted, stays alerted)

**Rationale**:
- Simple binary state matches M6 scope
- Event-driven allows UI and auto-pause hooks
- Future: can add area-based alarms, escalation levels

### Enemy Perception Model

**Decision**: Enemies use same LOS/vision system as player, but track detection per-enemy.

**Key behaviors**:
1. Each enemy has a `DetectionState`
2. Each tick, enemies check LOS to all crew within vision radius
3. If crew visible and enemy is `Idle` → transition to `Alerted`
4. Alerted enemies remember `LastKnownPosition` of detected crew

**Rationale**:
- Reuses existing `VisibilitySystem.HasLineOfSight()`
- Per-enemy state allows partial detection scenarios
- `LastKnownPosition` enables future investigation behavior

### Auto-Pause Triggers

**Decision**: Auto-pause on first detection and alarm state change.

Per DESIGN.md section 4.1:
> - **Auto-pause triggers**:
>   - First enemy sighting.
>   - Alarm state raised (e.g. station goes to red alert).

**Implementation**:
- `CombatState` fires event when alarm changes
- `MissionView` subscribes and triggers pause
- Player can immediately assess and respond

---

## Implementation Steps

### Phase 1: Detection State Model (Priority: Critical)

#### Step 1.1: Create DetectionState Enum and EnemyPerception Class

**New File**: `src/sim/combat/EnemyPerception.cs`

```csharp
using Godot;
using System;
using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// Detection state for an individual enemy.
/// </summary>
public enum DetectionState
{
    Idle,       // Unaware of player presence
    Alerted     // Aware and hostile
}

/// <summary>
/// Tracks what an enemy knows about player units.
/// </summary>
public class EnemyPerception
{
    public int EnemyId { get; }
    public DetectionState State { get; private set; } = DetectionState.Idle;
    
    // Last known positions of detected crew (actorId → position)
    public Dictionary<int, Vector2I> LastKnownPositions { get; } = new();
    
    // Tick when state last changed
    public int StateChangedTick { get; private set; } = 0;
    
    // Events
    public event Action<EnemyPerception, DetectionState, DetectionState> StateChanged;
    
    public EnemyPerception(int enemyId)
    {
        EnemyId = enemyId;
    }
    
    /// <summary>
    /// Transition to a new detection state.
    /// </summary>
    public void SetState(DetectionState newState, int currentTick)
    {
        if (State == newState)
        {
            return;
        }
        
        var oldState = State;
        State = newState;
        StateChangedTick = currentTick;
        StateChanged?.Invoke(this, oldState, newState);
        
        SimLog.Log($"[Perception] Enemy#{EnemyId} state: {oldState} → {newState}");
    }
    
    /// <summary>
    /// Update last known position for a detected crew member.
    /// </summary>
    public void UpdateLastKnown(int crewId, Vector2I position)
    {
        LastKnownPositions[crewId] = position;
    }
    
    /// <summary>
    /// Clear last known position when crew is confirmed gone.
    /// </summary>
    public void ClearLastKnown(int crewId)
    {
        LastKnownPositions.Remove(crewId);
    }
    
    /// <summary>
    /// Check if this enemy has any last known positions to investigate.
    /// </summary>
    public bool HasLastKnownPositions => LastKnownPositions.Count > 0;
}
```

**Acceptance Criteria**:
- [ ] `DetectionState` enum exists with Idle and Alerted
- [ ] `EnemyPerception` tracks state per enemy
- [ ] `LastKnownPositions` dictionary tracks crew positions
- [ ] `StateChanged` event fires on transitions

---

#### Step 1.2: Create PerceptionSystem

**New File**: `src/sim/combat/PerceptionSystem.cs`

```csharp
using Godot;
using System;
using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// Global alarm state for the mission.
/// </summary>
public enum AlarmState
{
    Quiet,      // No enemies alerted
    Alerted     // At least one enemy detected player
}

/// <summary>
/// Manages enemy perception and the global alarm state.
/// </summary>
public class PerceptionSystem
{
    private readonly CombatState combatState;
    private readonly Dictionary<int, EnemyPerception> perceptions = new();
    
    public AlarmState AlarmState { get; private set; } = AlarmState.Quiet;
    
    // Events
    public event Action<AlarmState, AlarmState> AlarmStateChanged;
    public event Action<Actor, Actor> EnemyDetectedCrew; // enemy, crew
    public event Action<Actor> EnemyBecameAlerted;
    
    public PerceptionSystem(CombatState combatState)
    {
        this.combatState = combatState;
    }
    
    /// <summary>
    /// Get or create perception state for an enemy.
    /// </summary>
    public EnemyPerception GetPerception(int enemyId)
    {
        if (!perceptions.TryGetValue(enemyId, out var perception))
        {
            perception = new EnemyPerception(enemyId);
            perception.StateChanged += OnEnemyStateChanged;
            perceptions[enemyId] = perception;
        }
        return perception;
    }
    
    /// <summary>
    /// Initialize perception for all existing enemies.
    /// Called after actors are spawned.
    /// </summary>
    public void Initialize()
    {
        foreach (var actor in combatState.Actors)
        {
            if (actor.Type == ActorTypes.Enemy && actor.State == ActorState.Alive)
            {
                GetPerception(actor.Id);
            }
        }
        SimLog.Log($"[PerceptionSystem] Initialized with {perceptions.Count} enemies");
    }
    
    /// <summary>
    /// Process perception checks for all enemies.
    /// Called each tick by CombatState.
    /// </summary>
    public void Tick()
    {
        var currentTick = combatState.TimeSystem.CurrentTick;
        
        foreach (var actor in combatState.Actors)
        {
            if (actor.Type != ActorTypes.Enemy || actor.State != ActorState.Alive)
            {
                continue;
            }
            
            var perception = GetPerception(actor.Id);
            CheckPerception(actor, perception, currentTick);
        }
    }
    
    private void CheckPerception(Actor enemy, EnemyPerception perception, int currentTick)
    {
        var visionRadius = enemy.GetVisionRadius();
        var enemyPos = enemy.GridPosition;
        
        foreach (var crew in combatState.Actors)
        {
            if (crew.Type != ActorTypes.Crew || crew.State != ActorState.Alive)
            {
                continue;
            }
            
            var crewPos = crew.GridPosition;
            var distance = CombatResolver.GetDistance(enemyPos, crewPos);
            
            // Outside vision radius
            if (distance > visionRadius)
            {
                continue;
            }
            
            // Check line of sight
            if (!CombatResolver.HasLineOfSight(enemyPos, crewPos, combatState.MapState))
            {
                continue;
            }
            
            // Crew is visible to this enemy
            perception.UpdateLastKnown(crew.Id, crewPos);
            
            // If enemy was idle, they become alerted
            if (perception.State == DetectionState.Idle)
            {
                perception.SetState(DetectionState.Alerted, currentTick);
                EnemyDetectedCrew?.Invoke(enemy, crew);
                EnemyBecameAlerted?.Invoke(enemy);
                
                SimLog.Log($"[Perception] Enemy#{enemy.Id} detected Crew#{crew.Id} at {crewPos}");
            }
        }
    }
    
    private void OnEnemyStateChanged(EnemyPerception perception, DetectionState oldState, DetectionState newState)
    {
        // Check if this triggers a global alarm change
        if (newState == DetectionState.Alerted && AlarmState == AlarmState.Quiet)
        {
            SetAlarmState(AlarmState.Alerted);
        }
    }
    
    private void SetAlarmState(AlarmState newState)
    {
        if (AlarmState == newState)
        {
            return;
        }
        
        var oldState = AlarmState;
        AlarmState = newState;
        AlarmStateChanged?.Invoke(oldState, newState);
        
        SimLog.Log($"[PerceptionSystem] ALARM: {oldState} → {newState}");
    }
    
    /// <summary>
    /// Check if a specific enemy is alerted.
    /// </summary>
    public bool IsEnemyAlerted(int enemyId)
    {
        return perceptions.TryGetValue(enemyId, out var p) && p.State == DetectionState.Alerted;
    }
    
    /// <summary>
    /// Get detection state for an enemy.
    /// </summary>
    public DetectionState GetDetectionState(int enemyId)
    {
        return perceptions.TryGetValue(enemyId, out var p) ? p.State : DetectionState.Idle;
    }
    
    /// <summary>
    /// Manually alert an enemy (e.g., from hearing gunfire).
    /// </summary>
    public void AlertEnemy(int enemyId, Vector2I? investigatePosition = null)
    {
        var perception = GetPerception(enemyId);
        var currentTick = combatState.TimeSystem.CurrentTick;
        
        if (perception.State == DetectionState.Idle)
        {
            perception.SetState(DetectionState.Alerted, currentTick);
            
            var enemy = combatState.GetActorById(enemyId);
            if (enemy != null)
            {
                EnemyBecameAlerted?.Invoke(enemy);
            }
        }
    }
    
    /// <summary>
    /// Alert all enemies (e.g., alarm triggered).
    /// </summary>
    public void AlertAllEnemies()
    {
        var currentTick = combatState.TimeSystem.CurrentTick;
        
        foreach (var actor in combatState.Actors)
        {
            if (actor.Type == ActorTypes.Enemy && actor.State == ActorState.Alive)
            {
                AlertEnemy(actor.Id);
            }
        }
    }
    
    /// <summary>
    /// Remove perception tracking for a dead enemy.
    /// </summary>
    public void RemoveEnemy(int enemyId)
    {
        if (perceptions.TryGetValue(enemyId, out var perception))
        {
            perception.StateChanged -= OnEnemyStateChanged;
            perceptions.Remove(enemyId);
        }
    }
}
```

**Acceptance Criteria**:
- [ ] `PerceptionSystem` tracks all enemy perceptions
- [ ] `Tick()` checks LOS from each enemy to each crew
- [ ] Detection triggers state change and events
- [ ] Global `AlarmState` changes when first enemy is alerted
- [ ] `AlertEnemy()` and `AlertAllEnemies()` work for manual triggers

---

### Phase 2: CombatState Integration (Priority: Critical)

#### Step 2.1: Add PerceptionSystem to CombatState

**File**: `src/sim/combat/CombatState.cs`

**Add member**:
```csharp
// Perception system (enemy detection, alarm state)
public PerceptionSystem Perception { get; private set; }
```

**Update constructor**:
```csharp
public CombatState(int seed)
{
    // ... existing initialization ...
    
    Perception = new PerceptionSystem(this);
}
```

**Update `InitializeVisibility()`** (or create new `InitializePerception()`):
```csharp
public void InitializePerception()
{
    Perception.Initialize();
    SimLog.Log("[CombatState] Perception system initialized");
}
```

**Update `ProcessTick()`**:
```csharp
private void ProcessTick()
{
    // ... existing code ...
    
    // Process enemy perception (before AI so AI can use detection state)
    Perception.Tick();
    
    // Run AI decisions
    aiController.Tick();
    
    // ... rest of tick processing ...
}
```

**Add events for alarm state**:
```csharp
// Events
public event Action<AlarmState, AlarmState> AlarmStateChanged;
public event Action<Actor, Actor> EnemyDetectedCrew;

// In constructor, wire up perception events:
Perception.AlarmStateChanged += (oldState, newState) => AlarmStateChanged?.Invoke(oldState, newState);
Perception.EnemyDetectedCrew += (enemy, crew) => EnemyDetectedCrew?.Invoke(enemy, crew);
```

**Acceptance Criteria**:
- [ ] `CombatState.Perception` property exists
- [ ] Perception initialized after actors spawned
- [ ] `ProcessTick()` calls `Perception.Tick()`
- [ ] Alarm events bubble up to CombatState level

---

#### Step 2.2: Update AIController to Use Detection State

**File**: `src/sim/combat/AIController.cs`

**Update `Think()` to respect detection state**:
```csharp
private void Think(Actor enemy)
{
    var perception = combatState.Perception.GetPerception(enemy.Id);
    
    // Idle enemies don't actively hunt - they patrol or stand guard
    if (perception.State == DetectionState.Idle)
    {
        // Future: patrol behavior
        // For M6: just stand still
        return;
    }
    
    // Alerted enemies use existing aggressive behavior
    // ... existing targeting and movement code ...
}
```

**Rationale**:
- Idle enemies don't attack or chase
- Once alerted, they behave as before (aggressive)
- This creates the "stealth until detected" feel

**Acceptance Criteria**:
- [ ] Idle enemies don't attack or move toward player
- [ ] Alerted enemies behave as before
- [ ] Detection triggers transition to aggressive behavior

---

### Phase 3: Auto-Pause Integration (Priority: High)

#### Step 3.1: Add Auto-Pause to MissionView

**File**: `src/scenes/mission/MissionView.cs`

**Subscribe to alarm events in `InitializeCombat()`**:
```csharp
private void InitializeCombat()
{
    // ... existing code ...
    
    // Subscribe to alarm state changes for auto-pause
    CombatState.AlarmStateChanged += OnAlarmStateChanged;
    CombatState.EnemyDetectedCrew += OnEnemyDetectedCrew;
}

private void OnAlarmStateChanged(AlarmState oldState, AlarmState newState)
{
    if (newState == AlarmState.Alerted && oldState == AlarmState.Quiet)
    {
        // Auto-pause on first alarm
        CombatState.TimeSystem.Pause();
        ShowAlarmNotification();
        SimLog.Log("[MissionView] Auto-paused: Alarm raised!");
    }
}

private void OnEnemyDetectedCrew(Actor enemy, Actor crew)
{
    // Could show detection indicator on enemy
    // For M6: just log
    SimLog.Log($"[MissionView] Enemy#{enemy.Id} detected Crew#{crew.Id}");
}

private void ShowAlarmNotification()
{
    // Simple notification - can be enhanced later
    // For now, update instructions or show temporary label
    instructionsLabel.Text = "⚠️ DETECTED! " + instructionsLabel.Text;
}
```

**Acceptance Criteria**:
- [ ] Game pauses when alarm state changes to Alerted
- [ ] Player sees notification of detection
- [ ] Detection event available for UI feedback

---

### Phase 4: Visual Feedback (Priority: High)

#### Step 4.1: Add Detection State to ActorView

**File**: `src/scenes/mission/ActorView.cs`

**Add visual indicator for detection state**:
```csharp
private ColorRect detectionIndicator;
private DetectionState currentDetectionState = DetectionState.Idle;

// In initialization:
private void CreateDetectionIndicator()
{
    detectionIndicator = new ColorRect();
    detectionIndicator.Size = new Vector2(8, 8);
    detectionIndicator.Position = new Vector2(-4, -GridConstants.TileSize - 4);
    detectionIndicator.Visible = false;
    AddChild(detectionIndicator);
}

public void UpdateDetectionState(DetectionState state)
{
    currentDetectionState = state;
    
    if (actor.Type != ActorTypes.Enemy)
    {
        detectionIndicator.Visible = false;
        return;
    }
    
    switch (state)
    {
        case DetectionState.Idle:
            detectionIndicator.Color = new Color(0.5f, 0.5f, 0.5f, 0.7f); // Gray
            detectionIndicator.Visible = true;
            break;
        case DetectionState.Alerted:
            detectionIndicator.Color = new Color(1.0f, 0.2f, 0.2f, 0.9f); // Red
            detectionIndicator.Visible = true;
            break;
    }
}
```

**Acceptance Criteria**:
- [ ] Enemies show gray indicator when Idle
- [ ] Enemies show red indicator when Alerted
- [ ] Indicator updates when state changes

---

#### Step 4.2: Add Alarm State UI Widget

**File**: `src/scenes/mission/AlarmStateWidget.cs` (New)

```csharp
using Godot;

namespace FringeTactics;

/// <summary>
/// UI widget showing current alarm state.
/// </summary>
public partial class AlarmStateWidget : Control
{
    private Label stateLabel;
    private ColorRect background;
    
    public override void _Ready()
    {
        CreateUI();
    }
    
    private void CreateUI()
    {
        background = new ColorRect();
        background.Size = new Vector2(120, 30);
        background.Color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
        AddChild(background);
        
        stateLabel = new Label();
        stateLabel.Position = new Vector2(10, 5);
        stateLabel.AddThemeFontSizeOverride("font_size", 14);
        AddChild(stateLabel);
        
        UpdateDisplay(AlarmState.Quiet);
    }
    
    public void UpdateDisplay(AlarmState state)
    {
        switch (state)
        {
            case AlarmState.Quiet:
                stateLabel.Text = "🔇 QUIET";
                stateLabel.AddThemeColorOverride("font_color", new Color(0.5f, 1.0f, 0.5f));
                background.Color = new Color(0.0f, 0.2f, 0.0f, 0.8f);
                break;
            case AlarmState.Alerted:
                stateLabel.Text = "🚨 ALERTED";
                stateLabel.AddThemeColorOverride("font_color", new Color(1.0f, 0.3f, 0.3f));
                background.Color = new Color(0.3f, 0.0f, 0.0f, 0.8f);
                break;
        }
    }
}
```

**Integrate into MissionView**:
```csharp
private AlarmStateWidget alarmStateWidget;

// In SetupUI():
alarmStateWidget = new AlarmStateWidget();
alarmStateWidget.Position = new Vector2(10, 50); // Below time widget
uiLayer.AddChild(alarmStateWidget);

// In OnAlarmStateChanged():
alarmStateWidget.UpdateDisplay(newState);
```

**Acceptance Criteria**:
- [ ] Alarm widget shows "QUIET" in green when undetected
- [ ] Alarm widget shows "ALERTED" in red when detected
- [ ] Widget updates in real-time

---

### Phase 5: MissionFactory Integration (Priority: High)

#### Step 5.1: Initialize Perception in MissionFactory

**File**: `src/sim/combat/MissionFactory.cs`

**Update `BuildSandbox()` or equivalent**:
```csharp
public static CombatState BuildSandbox(MissionConfig config)
{
    // ... existing map and actor setup ...
    
    // Initialize perception system after actors are spawned
    combat.InitializePerception();
    
    return combat;
}
```

**Acceptance Criteria**:
- [ ] Perception system initialized for all missions
- [ ] Enemies start in Idle state
- [ ] Alarm starts in Quiet state

---

### Phase 6: Test Mission & Validation (Priority: High)

#### Step 6.1: Create M6 Test Mission

**File**: `src/sim/data/MissionConfig.cs`

```csharp
/// <summary>
/// M6 test mission - stealth and alarm testing.
/// Features patrol routes and detection scenarios.
/// </summary>
public static MissionConfig CreateM6TestMission()
{
    return new MissionConfig
    {
        Id = "m6_test",
        Name = "M6 Test - Stealth & Alarm",
        MapTemplate = new string[]
        {
            "######################",
            "#EE..................#",
            "#EE..###....###......#",
            "#....#.#....#.#......#",
            "#....#D#....#D#......#",
            "#....###....###......#",
            "#....................#",
            "#....................#",
            "#....###....###......#",
            "#....#.#....#.#...E..#",
            "#....#D#....#D#......#",
            "#....###....###......#",
            "#....................#",
            "#..........E.........#",
            "#....................#",
            "#....................#",
            "#.............###....#",
            "#.............#T#....#",
            "#.............###....#",
            "#....................#",
            "######################"
        },
        CrewSpawnPositions = new List<Vector2I>
        {
            new Vector2I(2, 1),
            new Vector2I(3, 1),
            new Vector2I(2, 2),
            new Vector2I(3, 2)
        },
        EnemySpawns = new List<EnemySpawn>
        {
            // Guards in rooms - should be avoidable via doors
            new EnemySpawn("grunt", new Vector2I(18, 9)),
            new EnemySpawn("grunt", new Vector2I(11, 13)),
        }
    };
}
```

**Map design rationale**:
- Entry zone at top-left (EE)
- Multiple rooms with doors for stealth navigation
- Enemies placed to create detection risk
- Terminal (T) at bottom as objective
- Open areas where detection is likely

---

## Testing Checklist

### Manual Test Setup

Launch **"M6 Test (Stealth & Alarm)"** from main menu.

The test map features:
- **Crew spawn area** (top-left)
- **Multiple rooms** with doors for quiet navigation
- **Patrolling enemies** (future: for now, stationary)
- **Open areas** where detection is likely
- **Terminal objective** at bottom

### Manual Testing

1. **Initial State**
   - [ ] All enemies show gray "Idle" indicator
   - [ ] Alarm widget shows "QUIET" in green
   - [ ] Game is unpaused at start

2. **Detection Trigger**
   - [ ] Move crew into enemy LOS
   - [ ] Enemy indicator turns red
   - [ ] Alarm widget turns red "ALERTED"
   - [ ] Game auto-pauses
   - [ ] Notification appears

3. **Idle Enemy Behavior**
   - [ ] Idle enemies don't move toward crew
   - [ ] Idle enemies don't attack
   - [ ] Idle enemies stay in place

4. **Alerted Enemy Behavior**
   - [ ] Alerted enemies move toward crew
   - [ ] Alerted enemies attack when in range
   - [ ] Behavior matches pre-M6 AI

5. **Stealth Navigation**
   - [ ] Can move behind walls without detection
   - [ ] Closed doors block enemy LOS
   - [ ] Opening doors can expose crew to enemies
   - [ ] Can reach objective without triggering alarm

6. **Edge Cases**
   - [ ] Enemy dies while Idle → no alarm
   - [ ] All enemies killed → mission victory
   - [ ] Detection at map edge works correctly
   - [ ] Multiple enemies detect simultaneously

### Automated Tests

Create `tests/sim/combat/M6Tests.cs`:

```csharp
using Godot;
using GdUnit4;
using static GdUnit4.Assertions;

namespace FringeTactics.Tests;

/// <summary>
/// Tests for M6: Stealth & Alarm Foundations
/// </summary>
[TestSuite]
public class M6Tests
{
    // === Detection State Tests ===
    
    [TestCase]
    public void EnemyPerception_StartsIdle()
    {
        var perception = new EnemyPerception(1);
        
        AssertThat(perception.State).IsEqual(DetectionState.Idle);
        AssertThat(perception.HasLastKnownPositions).IsFalse();
    }
    
    [TestCase]
    public void EnemyPerception_TransitionsToAlerted()
    {
        var perception = new EnemyPerception(1);
        var stateChanged = false;
        perception.StateChanged += (p, old, newState) => stateChanged = true;
        
        perception.SetState(DetectionState.Alerted, 100);
        
        AssertThat(perception.State).IsEqual(DetectionState.Alerted);
        AssertThat(stateChanged).IsTrue();
        AssertThat(perception.StateChangedTick).IsEqual(100);
    }
    
    [TestCase]
    public void EnemyPerception_TracksLastKnownPosition()
    {
        var perception = new EnemyPerception(1);
        var crewPos = new Vector2I(5, 5);
        
        perception.UpdateLastKnown(10, crewPos);
        
        AssertThat(perception.HasLastKnownPositions).IsTrue();
        AssertThat(perception.LastKnownPositions[10]).IsEqual(crewPos);
    }
    
    // === Perception System Tests ===
    
    [TestCase]
    [RequireGodotRuntime]
    public void PerceptionSystem_DetectsCrewInLOS()
    {
        var combat = CreateTestCombat();
        var enemy = combat.AddActor(ActorTypes.Enemy, new Vector2I(5, 5));
        var crew = combat.AddActor(ActorTypes.Crew, new Vector2I(5, 3)); // In LOS
        combat.InitializePerception();
        
        // Process one tick
        combat.Perception.Tick();
        
        var perception = combat.Perception.GetPerception(enemy.Id);
        AssertThat(perception.State).IsEqual(DetectionState.Alerted);
    }
    
    [TestCase]
    [RequireGodotRuntime]
    public void PerceptionSystem_DoesNotDetectCrewBehindWall()
    {
        var template = new string[]
        {
            "#######",
            "#.....#",
            "#..#..#",  // Wall at (3,2)
            "#.....#",
            "#######"
        };
        var map = MapBuilder.BuildFromTemplate(template);
        var combat = new CombatState(12345);
        combat.MapState = map;
        combat.InitializeVisibility();
        
        var enemy = combat.AddActor(ActorTypes.Enemy, new Vector2I(2, 2));
        var crew = combat.AddActor(ActorTypes.Crew, new Vector2I(4, 2)); // Behind wall
        combat.InitializePerception();
        
        combat.Perception.Tick();
        
        var perception = combat.Perception.GetPerception(enemy.Id);
        AssertThat(perception.State).IsEqual(DetectionState.Idle);
    }
    
    [TestCase]
    [RequireGodotRuntime]
    public void PerceptionSystem_DoesNotDetectCrewOutOfRange()
    {
        var combat = CreateTestCombat();
        var enemy = combat.AddActor(ActorTypes.Enemy, new Vector2I(5, 5));
        var crew = combat.AddActor(ActorTypes.Crew, new Vector2I(5, 20)); // Far away
        combat.InitializePerception();
        
        combat.Perception.Tick();
        
        var perception = combat.Perception.GetPerception(enemy.Id);
        AssertThat(perception.State).IsEqual(DetectionState.Idle);
    }
    
    // === Alarm State Tests ===
    
    [TestCase]
    [RequireGodotRuntime]
    public void AlarmState_StartsQuiet()
    {
        var combat = CreateTestCombat();
        combat.InitializePerception();
        
        AssertThat(combat.Perception.AlarmState).IsEqual(AlarmState.Quiet);
    }
    
    [TestCase]
    [RequireGodotRuntime]
    public void AlarmState_BecomesAlertedOnDetection()
    {
        var combat = CreateTestCombat();
        var enemy = combat.AddActor(ActorTypes.Enemy, new Vector2I(5, 5));
        var crew = combat.AddActor(ActorTypes.Crew, new Vector2I(5, 3));
        combat.InitializePerception();
        
        AlarmState? newAlarmState = null;
        combat.Perception.AlarmStateChanged += (old, newState) => newAlarmState = newState;
        
        combat.Perception.Tick();
        
        AssertThat(combat.Perception.AlarmState).IsEqual(AlarmState.Alerted);
        AssertThat(newAlarmState).IsEqual(AlarmState.Alerted);
    }
    
    [TestCase]
    [RequireGodotRuntime]
    public void AlarmState_OnlyFiresOnceForMultipleDetections()
    {
        var combat = CreateTestCombat();
        var enemy1 = combat.AddActor(ActorTypes.Enemy, new Vector2I(5, 5));
        var enemy2 = combat.AddActor(ActorTypes.Enemy, new Vector2I(7, 5));
        var crew = combat.AddActor(ActorTypes.Crew, new Vector2I(6, 5)); // Visible to both
        combat.InitializePerception();
        
        int alarmChanges = 0;
        combat.Perception.AlarmStateChanged += (old, newState) => alarmChanges++;
        
        combat.Perception.Tick();
        
        AssertThat(alarmChanges).IsEqual(1);
    }
    
    // === AI Behavior Tests ===
    
    [TestCase]
    [RequireGodotRuntime]
    public void IdleEnemy_DoesNotAttack()
    {
        var combat = CreateTestCombat();
        var enemy = combat.AddActor(ActorTypes.Enemy, new Vector2I(5, 5));
        var crew = combat.AddActor(ActorTypes.Crew, new Vector2I(15, 15)); // Out of LOS
        combat.InitializePerception();
        
        // Verify enemy is idle
        AssertThat(combat.Perception.GetDetectionState(enemy.Id)).IsEqual(DetectionState.Idle);
        
        // Enemy should not have attack target
        AssertThat(enemy.AttackTargetId).IsNull();
    }
    
    [TestCase]
    [RequireGodotRuntime]
    public void AlertedEnemy_AttacksVisibleCrew()
    {
        var combat = CreateTestCombat();
        var enemy = combat.AddActor(ActorTypes.Enemy, new Vector2I(5, 5));
        var crew = combat.AddActor(ActorTypes.Crew, new Vector2I(5, 3));
        combat.InitializePerception();
        
        // Run several ticks to allow detection and AI response
        for (int i = 0; i < 20; i++)
        {
            combat.Update(0.05f); // 1 tick at 20 ticks/sec
        }
        
        // Enemy should be alerted and attacking
        AssertThat(combat.Perception.GetDetectionState(enemy.Id)).IsEqual(DetectionState.Alerted);
        AssertThat(enemy.AttackTargetId).IsNotNull();
    }
    
    // === Door/LOS Integration Tests ===
    
    [TestCase]
    [RequireGodotRuntime]
    public void ClosedDoor_BlocksDetection()
    {
        var template = new string[]
        {
            "#######",
            "#.....#",
            "#..D..#",  // Door at (3,2)
            "#.....#",
            "#######"
        };
        var map = MapBuilder.BuildFromTemplate(template);
        var combat = new CombatState(12345);
        combat.MapState = map;
        map.SetInteractionSystem(combat.Interactions);
        combat.InitializeVisibility();
        
        // Door is closed by default
        var door = combat.Interactions.GetInteractableAt(new Vector2I(3, 2));
        AssertThat(door).IsNotNull();
        AssertThat(door.State).IsEqual(InteractableState.DoorClosed);
        
        var enemy = combat.AddActor(ActorTypes.Enemy, new Vector2I(2, 2));
        var crew = combat.AddActor(ActorTypes.Crew, new Vector2I(4, 2));
        combat.InitializePerception();
        
        combat.Perception.Tick();
        
        // Should not detect through closed door
        AssertThat(combat.Perception.GetDetectionState(enemy.Id)).IsEqual(DetectionState.Idle);
    }
    
    [TestCase]
    [RequireGodotRuntime]
    public void OpenDoor_AllowsDetection()
    {
        var template = new string[]
        {
            "#######",
            "#.....#",
            "#..D..#",
            "#.....#",
            "#######"
        };
        var map = MapBuilder.BuildFromTemplate(template);
        var combat = new CombatState(12345);
        combat.MapState = map;
        map.SetInteractionSystem(combat.Interactions);
        combat.InitializeVisibility();
        
        // Open the door
        var door = combat.Interactions.GetInteractableAt(new Vector2I(3, 2));
        door.SetState(InteractableState.DoorOpen);
        
        var enemy = combat.AddActor(ActorTypes.Enemy, new Vector2I(2, 2));
        var crew = combat.AddActor(ActorTypes.Crew, new Vector2I(4, 2));
        combat.InitializePerception();
        
        combat.Perception.Tick();
        
        // Should detect through open door
        AssertThat(combat.Perception.GetDetectionState(enemy.Id)).IsEqual(DetectionState.Alerted);
    }
    
    // === Manual Alert Tests ===
    
    [TestCase]
    [RequireGodotRuntime]
    public void AlertEnemy_ManuallyAlertsSpecificEnemy()
    {
        var combat = CreateTestCombat();
        var enemy = combat.AddActor(ActorTypes.Enemy, new Vector2I(5, 5));
        combat.InitializePerception();
        
        AssertThat(combat.Perception.GetDetectionState(enemy.Id)).IsEqual(DetectionState.Idle);
        
        combat.Perception.AlertEnemy(enemy.Id);
        
        AssertThat(combat.Perception.GetDetectionState(enemy.Id)).IsEqual(DetectionState.Alerted);
    }
    
    [TestCase]
    [RequireGodotRuntime]
    public void AlertAllEnemies_AlertsEveryEnemy()
    {
        var combat = CreateTestCombat();
        var enemy1 = combat.AddActor(ActorTypes.Enemy, new Vector2I(5, 5));
        var enemy2 = combat.AddActor(ActorTypes.Enemy, new Vector2I(10, 10));
        combat.InitializePerception();
        
        combat.Perception.AlertAllEnemies();
        
        AssertThat(combat.Perception.GetDetectionState(enemy1.Id)).IsEqual(DetectionState.Alerted);
        AssertThat(combat.Perception.GetDetectionState(enemy2.Id)).IsEqual(DetectionState.Alerted);
    }
    
    // === Helper Methods ===
    
    private CombatState CreateTestCombat()
    {
        var combat = new CombatState(12345);
        var map = MapBuilder.BuildTestMap(new Vector2I(20, 20));
        combat.MapState = map;
        combat.InitializeVisibility();
        return combat;
    }
}
```

**Acceptance Criteria**:
- [ ] All detection state tests pass
- [ ] All alarm state tests pass
- [ ] All AI behavior tests pass
- [ ] All door/LOS integration tests pass
- [ ] All manual alert tests pass

---

## Implementation Order

1. **Day 1: Core Data Model**
   - Step 1.1: Create EnemyPerception class
   - Step 1.2: Create PerceptionSystem
   - Write basic unit tests

2. **Day 2: CombatState Integration**
   - Step 2.1: Add PerceptionSystem to CombatState
   - Step 2.2: Update AIController for detection states
   - Run integration tests

3. **Day 3: Auto-Pause & Events**
   - Step 3.1: Add auto-pause to MissionView
   - Wire up alarm state events
   - Test auto-pause behavior

4. **Day 4: Visual Feedback**
   - Step 4.1: Add detection indicators to ActorView
   - Step 4.2: Create AlarmStateWidget
   - Integrate into MissionView

5. **Day 5: Testing & Polish**
   - Step 5.1: Initialize perception in MissionFactory
   - Step 6.1: Create M6 test mission
   - Manual testing and bug fixes
   - Run full test suite

---

## Success Criteria for M6

When M6 is complete, you should be able to:

1. ✅ Start a mission with enemies in Idle state
2. ✅ See alarm status widget showing "QUIET"
3. ✅ Move crew without alerting enemies (using cover/walls)
4. ✅ Trigger detection by entering enemy LOS
5. ✅ See auto-pause when first enemy is alerted
6. ✅ See alarm status change to "ALERTED"
7. ✅ See enemy detection indicators change from gray to red
8. ✅ Experience Idle enemies standing still
9. ✅ Experience Alerted enemies attacking as before
10. ✅ All automated tests pass

**Natural Pause Point**: After M6, you can prototype missions that start covert and can go loud dynamically. This is a good time to let mission designers play and refine requirements for deeper stealth.

---

## Notes for Future Milestones

### M7 Dependencies (Session I/O)
- Alarm state should be part of mission output
- Detection events may affect mission scoring
- "Clean" vs "loud" completion tracking

### Future Enhancements (Post-M6)
- **Suspicious state**: Idle → Suspicious → Alerted (investigation behavior)
- **View cones**: Directional vision instead of radial
- **Hearing**: Gunfire alerts nearby enemies
- **Patrol routes**: Enemies move along predefined paths when Idle
- **Last known position investigation**: Alerted enemies investigate where they last saw crew

---

## Files to Create/Modify

### New Files
- `src/sim/combat/EnemyPerception.cs` - Detection state per enemy
- `src/sim/combat/PerceptionSystem.cs` - Global perception management
- `src/scenes/mission/AlarmStateWidget.cs` - Alarm UI widget
- `tests/sim/combat/M6Tests.cs` - Automated tests

### Modified Files
- `src/sim/combat/CombatState.cs` - Add PerceptionSystem
- `src/sim/combat/AIController.cs` - Respect detection state
- `src/sim/combat/MissionFactory.cs` - Initialize perception
- `src/sim/data/MissionConfig.cs` - M6 test mission
- `src/scenes/mission/MissionView.cs` - Auto-pause, alarm widget
- `src/scenes/mission/ActorView.cs` - Detection state indicator
- `src/core/GameState.cs` - Add StartM6TestMission()
- `src/scenes/menu/MainMenu.cs` - Add M6 test button

---

## Open Questions

1. **Should detection persist after LOS is broken?**
   - *Decision for M6*: Yes, once alerted, stays alerted. No de-escalation.
   - *Future*: Could add "lost contact" state after N ticks without LOS.

2. **Should killing an Idle enemy trigger alarm?**
   - *Decision for M6*: No, silent kills don't alert others.
   - *Future*: Could add "body discovered" mechanic.

3. **Should enemies share detection information?**
   - *Decision for M6*: No, each enemy detects independently.
   - *Future*: Could add "radio alert" when one enemy detects.

4. **Should cover affect detection?**
   - *Decision for M6*: No, only LOS matters.
   - *Future*: Could add detection reduction when behind cover.

5. **Should there be a detection "grace period"?**
   - *Decision for M6*: No, detection is instant when in LOS.
   - *Future*: Could add brief delay before full alert.
