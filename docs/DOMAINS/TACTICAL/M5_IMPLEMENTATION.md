# M5 – Interactables & Channeled Hacking: Implementation Plan

This document breaks down **Milestone 5** from `ROADMAP.md` into concrete implementation steps.

**Goal**: Add non-combat interactions to broaden mission possibilities beyond "kill everything."

---

## Current State Assessment

### What We Have (From M0–M4)

| Component | Status | Notes |
|-----------|--------|-------|
| `MapState` | ✅ Complete | Has `Interactables` dictionary (Vector2I → string), unused |
| `MapBuilder` | ✅ Complete | `AddDoorway()` exists but only sets tile to floor |
| `CombatState` | ✅ Complete | Processes attacks, movement, abilities; no interaction system |
| `Actor` | ✅ Complete | Has `IsReloading`, `ReloadProgress` pattern for channeled actions |
| `AbilitySystem` | ✅ Complete | Handles delayed/channeled abilities with `PendingAbility` |
| `VisibilitySystem` | ✅ Complete | LOS checks via `MapState.BlocksLOS()` |
| `MissionView` | ✅ Complete | Click handling, actor selection, attack orders |

### What M5 Requires vs What We Have

| M5 Requirement | Current Status | Gap |
|----------------|----------------|-----|
| Interactable framework | ⚠️ Partial | Dictionary exists, no entity/state model |
| Doors (closed/open/locked) | ❌ Missing | Need door entity with states |
| Terminals for objectives | ❌ Missing | Need terminal entity |
| Environmental hazards | ❌ Missing | Need hazard entity |
| Context-based interaction | ❌ Missing | Need interaction system |
| Channeled hacking | ❌ Missing | Need channeled action on Actor |
| Doors affect pathfinding | ❌ Missing | `IsWalkable()` doesn't check door state |
| Doors affect LOS | ❌ Missing | `BlocksLOS()` doesn't check door state |

---

## Architecture Decisions

### Interactable Entity Model

**Decision**: Create a unified `Interactable` class with type-specific behavior via composition.

**Structure**:
```csharp
public class Interactable
{
    public int Id { get; set; }
    public string Type { get; set; }           // "door", "terminal", "hazard"
    public Vector2I Position { get; set; }
    public InteractableState State { get; set; }
    public Dictionary<string, object> Properties { get; set; }
}
```

**Rationale**:
- Keeps simulation layer clean (no deep inheritance)
- Matches existing architecture patterns (data + systems)
- Easy to extend for future interactable types

### Channeled Action Model

**Decision**: Extend `Actor` with a generic channeled action system, similar to reload.

**Structure**:
```csharp
public class ChanneledAction
{
    public string ActionType { get; set; }     // "hack", "unlock", etc.
    public int TargetInteractableId { get; set; }
    public int TotalTicks { get; set; }
    public int TicksRemaining { get; set; }
    public bool CanBeInterrupted { get; set; } = true;
}
```

**Rationale**:
- Reload already uses this pattern (`IsReloading`, `ReloadProgress`)
- Hacking is conceptually similar: duration, interruptible, progress-based
- Can reuse for future channeled actions (planting explosives, healing, etc.)

### Door Behavior

| State | Walkable | Blocks LOS | Can Interact |
|-------|----------|------------|--------------|
| Closed | No | Yes | Yes (open) |
| Open | Yes | No | Yes (close) |
| Locked | No | Yes | Yes (hack/unlock) |

### Terminal Behavior

| State | Can Interact | Action |
|-------|--------------|--------|
| Idle | Yes | Start hacking |
| Hacking | No (in progress) | Wait for completion |
| Hacked | No | Objective complete |

### Hazard Behavior

| State | Can Interact | Effect |
|-------|--------------|--------|
| Armed | Yes | Trigger explosion/effect |
| Triggered | No | Effect applied |
| Disabled | No | Safe, no effect |

---

## Implementation Steps

### Phase 1: Interactable Data Model (Priority: Critical)

#### Step 1.1: Create Interactable Class

**New File**: `src/sim/combat/Interactable.cs`

- `InteractableState` enum with door/terminal/hazard states
- `InteractableTypes` constants: "door", "terminal", "hazard"
- `Interactable` class with Id, Type, Position, State, Properties
- Helper methods: `IsDoor`, `BlocksMovement()`, `BlocksLOS()`
- `StateChanged` event for view updates

**Acceptance Criteria**:
- [ ] `Interactable` class exists with Id, Type, Position, State
- [ ] `InteractableState` enum covers door/terminal/hazard states
- [ ] Property helpers work for type-specific data
- [ ] Door helpers correctly report blocking behavior

#### Step 1.2: Create ChanneledAction Class

**New File**: `src/sim/combat/ChanneledAction.cs`

- `ChannelTypes` constants: "hack", "unlock", "disable_hazard"
- `ChanneledAction` class with ActionType, TargetId, TotalTicks, TicksRemaining
- `Progress` property (0.0 to 1.0)
- `IsComplete` property
- `Tick()` method to advance progress

**Acceptance Criteria**:
- [ ] `ChanneledAction` tracks target, duration, and progress
- [ ] `Progress` property returns 0-1 value
- [ ] `IsComplete` returns true when ticks reach 0

#### Step 1.3: Extend Actor with Channeled Action Support

**File**: `src/sim/combat/Actor.cs`

**Add**:
- `IsChanneling` and `CurrentChannel` properties
- `ChannelStarted`, `ChannelCompleted`, `ChannelInterrupted` events
- `StartChannel()`, `CancelChannel()`, `CompleteChannel()` methods
- Update `Tick()` to process channel progress
- Update `SetTarget()` to interrupt channeling
- Update `TakeDamage()` to interrupt channeling

**Acceptance Criteria**:
- [ ] `Actor.IsChanneling` and `CurrentChannel` properties exist
- [ ] `StartChannel()` initiates channeled action
- [ ] Movement orders interrupt channeling
- [ ] Taking damage interrupts channeling

---

### Phase 2: Interaction System (Priority: Critical)

#### Step 2.1: Create InteractionSystem

**New File**: `src/sim/combat/InteractionSystem.cs`

**Core Methods**:
- `AddInteractable()`, `RemoveInteractable()`, `GetInteractable()`
- `GetInteractableAt(Vector2I)` - find by position
- `CanInteract(Actor, Interactable)` - check adjacency and state
- `GetAvailableInteractions()` - list valid actions
- `ExecuteInteraction()` - perform action

**Door Interactions**:
- "open" - instant, changes state to DoorOpen
- "close" - instant, changes state to DoorClosed
- "hack" - channeled, unlocks locked door

**Terminal Interactions**:
- "hack" - channeled, completes objective when done

**Hazard Interactions**:
- "trigger" - instant, deals AoE damage
- "disable" - channeled, makes hazard safe

**Events**:
- `InteractableAdded`, `InteractableRemoved`
- `InteractableStateChanged`
- `InteractionStarted`, `InteractionCompleted`
- `HazardTriggered`

**Acceptance Criteria**:
- [ ] `InteractionSystem` manages interactables
- [ ] `CanInteract()` checks adjacency and state
- [ ] Door open/close works instantly
- [ ] Door/terminal hack is channeled
- [ ] Hazard trigger deals AoE damage

#### Step 2.2: Integrate InteractionSystem into CombatState

**File**: `src/sim/combat/CombatState.cs`

- Add `Interactions` property
- Initialize in constructor
- Call `Interactions.Tick()` in `ProcessTick()`
- Add `IssueInteractionOrder()` method

**Acceptance Criteria**:
- [ ] `CombatState.Interactions` property exists
- [ ] `ProcessTick()` calls `Interactions.Tick()`
- [ ] `IssueInteractionOrder()` works

#### Step 2.3: Update MapState for Door Integration

**File**: `src/sim/combat/MapState.cs`

- Add `SetInteractionSystem()` method
- Update `IsWalkable()` to check door blocking
- Update `BlocksLOS()` to check door blocking

**Acceptance Criteria**:
- [ ] `IsWalkable()` returns false for closed/locked doors
- [ ] `BlocksLOS()` returns true for closed/locked doors
- [ ] Opening a door makes tile walkable and visible through

---

### Phase 3: Map Building & Configuration (Priority: High)

#### Step 3.1: Update MapBuilder for Interactables

**File**: `src/sim/combat/MapBuilder.cs`

**Template characters**:
```
D = Door (closed)
L = Locked door
T = Terminal
X = Explosive hazard
```

Update `BuildFromTemplate()` to create interactables from template characters.

#### Step 3.2: Update MissionConfig

**File**: `src/sim/data/MissionConfig.cs`

- Add `InteractableSpawn` class
- Add `InteractableSpawns` list to `MissionConfig`

#### Step 3.3: Update MissionFactory

**File**: `src/sim/combat/MissionFactory.cs`

- Pass `InteractionSystem` to `MapBuilder.BuildFromTemplate()`
- Spawn additional interactables from config

---

### Phase 4: View Layer (Priority: High)

#### Step 4.1: Create InteractableView

**New File**: `src/scenes/mission/InteractableView.cs`

- Visual representation with ColorRect
- Color coding by type and state:
  - Door: Brown (closed), Green (open), Red (locked)
  - Terminal: Blue (idle), Yellow (hacking), Green (hacked)
  - Hazard: Orange (armed), Red (triggered), Gray (disabled)
- Progress bar for channeled actions
- Subscribe to `StateChanged` event

#### Step 4.2: Integrate into MissionView

**File**: `src/scenes/mission/MissionView.cs`

- Track `interactableViews` dictionary
- Subscribe to `InteractionSystem` events
- Create/remove views on add/remove
- Handle right-click on interactables
- `FindBestInteractor()` - select closest eligible actor
- Update channel progress displays in `_Process()`

---

### Phase 5: Test Mission & Validation (Priority: High)

#### Step 5.1: Create M5 Test Mission

**File**: `src/sim/data/MissionConfig.cs`

```csharp
public static MissionConfig CreateM5TestMission()
{
    return new MissionConfig
    {
        Id = "m5_test",
        Name = "M5 Test - Interactables",
        MapTemplate = new string[]
        {
            "####################",
            "#EE................#",
            "#EE.....#D#........#",
            "#.......#.#........#",
            "#.......#.#...T....#",
            "#.......###........#",
            "#..................#",
            "#....X.............#",
            "#..................#",
            "#........###L###...#",
            "#........#.....#...#",
            "#........#..T..#...#",
            "#........#.....#...#",
            "#........#######...#",
            "#..................#",
            "####################"
        },
        // Crew and enemy spawns...
    };
}
```

---

## Testing Checklist

### Manual Test Setup

Launch **"M5 Test (Interactables)"** from main menu.

### Manual Testing

1. **Door Interaction - Open/Close**
   - [ ] Right-click closed door → opens instantly
   - [ ] Unit can walk through open door
   - [ ] LOS extends through open door
   - [ ] Right-click open door → closes
   - [ ] LOS blocked by closed door

2. **Locked Door - Hacking**
   - [ ] Locked door shows red color
   - [ ] Right-click → channeled hack starts
   - [ ] Progress bar appears
   - [ ] Completion → door opens
   - [ ] Move order during hack → interrupted

3. **Terminal - Hacking**
   - [ ] Terminal shows blue color
   - [ ] Right-click → channeled hack starts
   - [ ] Terminal turns yellow during hack
   - [ ] Completion → terminal turns green
   - [ ] Taking damage → hack interrupted

4. **Hazard - Trigger**
   - [ ] Hazard shows orange color
   - [ ] Right-click → explosion triggers
   - [ ] Actors in radius take damage

5. **Pathfinding with Doors**
   - [ ] Move order through closed door → blocked
   - [ ] Open door → can path through

6. **Interruption Rules**
   - [ ] Move order interrupts channeling
   - [ ] Taking damage interrupts channeling
   - [ ] Terminal returns to idle on interrupt

7. **Best Actor Selection**
   - [ ] Multiple selected → closest eligible interacts

### Automated Tests

Create `tests/sim/combat/M5Tests.cs`:

```csharp
[TestSuite]
public class M5Tests
{
    // === Interactable Creation ===
    [TestCase] InteractionSystem_AddInteractable_CreatesWithCorrectState()
    [TestCase] InteractionSystem_AddLockedDoor_StartsLocked()
    [TestCase] InteractionSystem_GetInteractableAt_FindsByPosition()
    
    // === Door Behavior ===
    [TestCase] Door_BlocksMovement_WhenClosed()
    [TestCase] Door_AllowsMovement_WhenOpen()
    [TestCase] Door_BlocksLOS_WhenClosed()
    [TestCase] Door_AllowsLOS_WhenOpen()
    [TestCase] Door_OpenClose_InstantInteraction()
    [TestCase] LockedDoor_RequiresChanneledHack()
    
    // === Channeled Actions ===
    [TestCase] ChanneledAction_Progress_IncreasesOverTime()
    [TestCase] ChanneledAction_Interrupted_ByMovement()
    [TestCase] ChanneledAction_Interrupted_ByDamage()
    [TestCase] ChanneledAction_Completes_AfterDuration()
    
    // === Terminal ===
    [TestCase] Terminal_Hack_CompletesObjective()
    [TestCase] Terminal_Hack_Interrupted_ResetsState()
    
    // === Hazard ===
    [TestCase] Hazard_Trigger_DealsAoEDamage()
    [TestCase] Hazard_Disable_MakesSafe()
    
    // === Integration ===
    [TestCase] MapState_IsWalkable_ChecksDoorState()
    [TestCase] MapState_BlocksLOS_ChecksDoorState()
    [TestCase] Visibility_Updates_WhenDoorOpens()
}
```

---

## Implementation Order

1. **Day 1: Core Data Model**
   - Step 1.1: Create Interactable class
   - Step 1.2: Create ChanneledAction class
   - Step 1.3: Extend Actor with channeling

2. **Day 2: Interaction System**
   - Step 2.1: Create InteractionSystem
   - Step 2.2: Integrate into CombatState
   - Step 2.3: Update MapState for doors

3. **Day 3: Map Building**
   - Step 3.1: Update MapBuilder
   - Step 3.2: Update MissionConfig
   - Step 3.3: Update MissionFactory

4. **Day 4: View Layer**
   - Step 4.1: Create InteractableView
   - Step 4.2: Integrate into MissionView

5. **Day 5: Testing & Polish**
   - Step 5.1: Create M5 test mission
   - Write automated tests
   - Manual testing and bug fixes

---

## Success Criteria for M5

When M5 is complete, you should be able to:

1. ✅ Open and close doors by clicking on them
2. ✅ Hack locked doors with a channeled action
3. ✅ Hack terminals to complete objectives
4. ✅ Trigger environmental hazards for AoE damage
5. ✅ See doors block movement and LOS when closed
6. ✅ See channel progress on interactables
7. ✅ Have channeling interrupted by movement or damage
8. ✅ All automated tests pass

**Natural Pause Point**: After M5, you can create missions with non-combat objectives like "hack the terminal", "reach the locked room", or "use hazards tactically". This enables stealth-oriented mission design for M6.

---

## Notes for Future Milestones

### M6 Dependencies (Stealth)
- Doors enable quiet navigation (open silently vs breach loudly)
- Terminals may trigger alarms if hacked wrong
- Hazards can be used to distract or eliminate enemies

### M7 Dependencies (Session I/O)
- Interactable states should be part of mission output
- Objectives completed via terminals feed into results

### Future Enhancements (Post-M5)
- **Key items**: Unlock doors without hacking
- **Breach charges**: Destroy locked doors (loud)
- **Security cameras**: Interactable that affects detection
- **Power systems**: Disable lights/security via terminals

---

## Files to Create/Modify

### New Files
- `src/sim/combat/Interactable.cs`
- `src/sim/combat/ChanneledAction.cs`
- `src/sim/combat/InteractionSystem.cs`
- `src/scenes/mission/InteractableView.cs`
- `tests/sim/combat/M5Tests.cs`

### Modified Files
- `src/sim/combat/Actor.cs` - Add channeling support
- `src/sim/combat/CombatState.cs` - Add InteractionSystem
- `src/sim/combat/MapState.cs` - Door integration
- `src/sim/combat/MapBuilder.cs` - Template parsing
- `src/sim/data/MissionConfig.cs` - InteractableSpawn, M5 test mission
- `src/sim/combat/MissionFactory.cs` - Interactable creation
- `src/scenes/mission/MissionView.cs` - InteractableView integration
- `src/core/GameState.cs` - Add StartM5TestMission()
- `src/scenes/menu/MainMenu.cs` - Add M5 test button

---

## Open Questions

1. **Partial progress retention**: Should interrupted hacks retain progress?
   - *Decision for M5*: No, progress resets. Simpler to implement.
   - *Future*: [PLUS] feature to retain 50% progress.

2. **Interaction range**: Adjacent only, or allow 2-tile range?
   - *Decision*: Adjacent only (distance ≤ 1.5 for diagonals).

3. **Multiple actors hacking**: Can two actors hack the same terminal faster?
   - *Decision for M5*: No, one actor per interactable.

4. **Door auto-close**: Should doors close automatically after time?
   - *Decision for M5*: No, manual only.

5. **Hazard friendly fire**: Do triggered hazards damage crew?
   - *Decision*: Yes, hazards damage all actors in radius.
