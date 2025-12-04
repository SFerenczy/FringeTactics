# Refactoring Plan: Addressing God Classes

Based on codebase analysis. This plan addresses architectural debt before it compounds.

---

## Current State Assessment

### What's Working Well
- **Sim/View separation is solid**: `CombatState` (sim) doesn't know about `MissionView` (scene)
- **Stateless services exist**: `CombatResolver`, `GridUtils`, `FormationCalculator` follow the right pattern
- **Event-driven communication**: C# events connect sim → view cleanly
- **Data definitions are centralized**: `Definitions.cs` provides single lookup point

### Critical Bottlenecks

#### 1. `MissionView.cs` — 1484 lines (High Risk)
**Responsibilities it currently owns:**
- Input handling (`_Input`, `HandleMouseClick`, key bindings)
- Selection state (`selectedActorIds`, `controlGroups`, box selection)
- UI state (`pendingAbility`, `abilityTargetingLabel`)
- Rendering orchestration (fog, cover indicators, actor views, interactables)
- Camera control delegation
- Mission end UI

**Why it breaks:** Every new feature (M5 interactables, M6 stealth UI) adds more `if/else` chains to `HandleMouseClick` and `_Input`.

#### 2. `CombatState.cs` — 619 lines (High Risk)
**Responsibilities it currently owns:**
- Actor list management
- Attack processing (`ProcessAttacks`, `ProcessAutoDefend`, `ExecuteAttack`)
- Movement collision resolution (`ResolveMovementCollisions`)
- Win condition checking
- Order issuing (movement, attack, ability, interaction, reload)
- System orchestration (AI, abilities, interactions, visibility)

**Why it breaks:** Adding Overwatch, suppression, or reaction fire will bloat `ProcessTick()` with condition checks.

#### 3. `Actor.cs` — 440 lines (Medium Risk)
**Current approach:** Hard properties (`Hp`, `MoveSpeed`) + string-based `StatusEffects` list with no modifier system.

**Why it breaks:** M6 stealth needs effects like "Suppressed" (halves movement, lowers aim). No clean way to compute `GetEffectiveMoveSpeed()` based on active effects.

#### 4. `Definitions.cs` — Hardcoded Data (Medium Risk)
**Current approach:** All game content locked in C# dictionaries.

**Why it breaks:** Cannot balance without recompiling. Prevents rapid iteration.

---

## Refactoring Plan

### Phase 1: Decompose MissionView (Do Now)

**Goal:** Extract input handling into a dedicated controller. MissionView becomes a pure renderer.

#### Step 1.1: Create `MissionInputController.cs`

Location: `src/scenes/mission/MissionInputController.cs`

**Extracts from MissionView:**
- `_Input()` method
- `HandleMouseClick()`, `HandleRightClick()`, `HandleSelection()`
- `StartPotentialDrag()`, `FinishLeftClick()`, `UpdateBoxSelectionVisual()`, `FinishBoxSelection()`
- `SaveControlGroup()`, `RecallControlGroup()`, `SelectAllCrew()`
- `StartAbilityTargeting()`, `ConfirmAbilityTarget()`, `CancelAbilityTargeting()`
- `ScreenToGrid()` coordinate conversion

**Emits high-level events:**
```csharp
public event Action<Vector2I> TileClicked;
public event Action<int> ActorSelected;
public event Action<List<int>> SelectionChanged;
public event Action<int, Vector2I> MoveOrderIssued;
public event Action<int, int> AttackOrderIssued;
public event Action<AbilityData, Vector2I> AbilityTargetConfirmed;
public event Action AbilityTargetCancelled;
public event Action<int> ControlGroupRecalled;
```

**Owns state:**
- `selectedActorIds`
- `controlGroups`
- `pendingAbility`
- Box selection state (`isDragSelecting`, `dragStartScreen`, etc.)
- Double-click detection state

#### Step 1.2: Create `SelectionManager.cs`

Location: `src/scenes/mission/SelectionManager.cs`

**Responsibility:** Manage selection state and visual feedback.

```csharp
public class SelectionManager
{
    public IReadOnlyList<int> SelectedActorIds { get; }
    public event Action<IReadOnlyList<int>> SelectionChanged;
    
    public void Select(int actorId);
    public void AddToSelection(int actorId);
    public void RemoveFromSelection(int actorId);
    public void ClearSelection();
    public void SelectAll(IEnumerable<int> actorIds);
    public bool IsSelected(int actorId);
}
```

#### Step 1.3: Refactor `MissionView.cs`

**After extraction, MissionView only:**
- Subscribes to `MissionInputController` events
- Manages visual layers (fog, grid, cover indicators)
- Updates `ActorView` instances
- Handles visual feedback (explosions, move target markers)

**Estimated reduction:** 1484 → ~600 lines

---

### Phase 2: System-ize CombatState (Do Now)

**Goal:** Turn `CombatState` into a coordinator that delegates to focused systems.

#### Step 2.1: Create `AttackSystem.cs`

Location: `src/sim/combat/AttackSystem.cs`

**Extracts from CombatState:**
- `ProcessAttacks()`
- `ProcessAutoDefend()`
- `ExecuteAttack()`

**Interface:**
```csharp
public class AttackSystem
{
    public event Action<Actor, Actor, AttackResult> AttackResolved;
    public event Action<Actor> ActorDied;
    
    public void ProcessTick(IReadOnlyList<Actor> actors, MapState map, CombatRng rng, CombatStats stats);
}
```

**Key change:** `ExecuteAttack` moves here. `CombatState` subscribes to `AttackResolved` and `ActorDied` events to update its own events.

#### Step 2.2: Create `MovementSystem.cs`

Location: `src/sim/combat/MovementSystem.cs`

**Extracts from CombatState:**
- `ResolveMovementCollisions()`

**Extracts from Actor.Tick:**
- Movement logic (currently in `Actor.Tick()` lines 141-210)

**Interface:**
```csharp
public static class MovementSystem
{
    public static void ResolveCollisions(IReadOnlyList<Actor> actors, MapState map);
    public static void ProcessMovement(Actor actor, float tickDuration, MapState map);
}
```

**Rationale:** Centralizes collision logic. Makes it easier to add:
- Pushing units
- Hazard tile effects (slow, damage)
- Difficult terrain

#### Step 2.3: Refactor `CombatState.ProcessTick()`

**After extraction:**
```csharp
private void ProcessTick()
{
    var tickDuration = TimeSystem.TickDuration;
    
    aiController.Tick();
    AbilitySystem.Tick();
    Interactions.Tick();
    
    attackSystem.ProcessTick(Actors, MapState, Rng, Stats);
    MovementSystem.ResolveCollisions(Actors, MapState);
    
    foreach (var actor in Actors)
    {
        MovementSystem.ProcessMovement(actor, tickDuration, MapState);
    }
    
    Visibility.UpdateVisibility(Actors);
    CheckMissionEnd();
}
```

**Estimated reduction:** 619 → ~350 lines

---

### Phase 3: Stat Modifier System (Before M6)

**Goal:** Enable dynamic stat modification for status effects.

#### Step 3.1: Create `StatModifier.cs`

Location: `src/sim/combat/StatModifier.cs`

```csharp
public enum StatType { MoveSpeed, Accuracy, Damage, VisionRadius }

public class StatModifier
{
    public string SourceId { get; }      // e.g., "suppressed", "stunned"
    public StatType Stat { get; }
    public float Multiplier { get; }     // 1.0 = no change, 0.5 = halved
    public float FlatBonus { get; }      // Added after multiplier
    public int ExpiresAtTick { get; }    // -1 = permanent until removed
}

public class ModifierCollection
{
    private List<StatModifier> modifiers = new();
    
    public void Add(StatModifier mod);
    public void RemoveBySource(string sourceId);
    public void RemoveExpired(int currentTick);
    public float Calculate(StatType stat, float baseValue);
}
```

#### Step 3.2: Refactor `Actor.cs` Stats

**Replace direct property access with computed methods:**

```csharp
// Before
public const float MoveSpeed = 4.0f;

// After
private const float BaseMoveSpeed = 4.0f;
public ModifierCollection Modifiers { get; } = new();

public float GetMoveSpeed()
{
    return Modifiers.Calculate(StatType.MoveSpeed, BaseMoveSpeed);
}

public float GetAccuracy()
{
    var baseAim = Stats.TryGetValue("aim", out var aim) ? aim : 0;
    return Modifiers.Calculate(StatType.Accuracy, 0.7f + baseAim * 0.01f);
}
```

#### Step 3.3: Integrate with Status Effects

**Replace string-based effects with modifier application:**

```csharp
// In a new StatusEffectSystem or in AbilitySystem
public void ApplyEffect(Actor actor, string effectId, int duration, int currentTick)
{
    var expiresAt = currentTick + duration;
    
    switch (effectId)
    {
        case "suppressed":
            actor.Modifiers.Add(new StatModifier("suppressed", StatType.MoveSpeed, 0.5f, 0, expiresAt));
            actor.Modifiers.Add(new StatModifier("suppressed", StatType.Accuracy, 0.7f, 0, expiresAt));
            break;
        case "stunned":
            actor.Modifiers.Add(new StatModifier("stunned", StatType.MoveSpeed, 0f, 0, expiresAt));
            actor.Modifiers.Add(new StatModifier("stunned", StatType.Accuracy, 0f, 0, expiresAt));
            break;
    }
}
```

---

### Phase 4: Externalize Data (Before Content Pass)

**Goal:** Move game data to JSON for rapid iteration.

#### Step 4.1: Create JSON Data Files

Location: `data/` folder at project root

```
data/
  weapons.json
  enemies.json
  abilities.json
```

**Example `weapons.json`:**
```json
{
  "rifle": {
    "name": "Assault Rifle",
    "damage": 25,
    "range": 8,
    "cooldownTicks": 10,
    "accuracy": 0.70,
    "magazineSize": 30,
    "reloadTicks": 40
  }
}
```

#### Step 4.2: Update `Definitions.cs` to Load JSON

```csharp
public static class Definitions
{
    private static bool isLoaded = false;
    
    public static WeaponDefinitions Weapons { get; private set; }
    public static EnemyDefinitions Enemies { get; private set; }
    public static AbilityDefinitions Abilities { get; private set; }
    
    public static void Load()
    {
        if (isLoaded) return;
        
        Weapons = LoadWeapons("res://data/weapons.json");
        Enemies = LoadEnemies("res://data/enemies.json");
        Abilities = LoadAbilities("res://data/abilities.json");
        
        isLoaded = true;
    }
    
    public static void Reload()
    {
        isLoaded = false;
        Load();
    }
}
```

#### Step 4.3: Add Dev Reload Command

In `DevTools.cs`:
```csharp
public static void ReloadDefinitions()
{
    Definitions.Reload();
    GD.Print("[DevTools] Definitions reloaded from JSON");
}
```

---

## Implementation Order

| Priority | Task | Effort | Risk if Skipped | Status |
|----------|------|--------|-----------------|--------|
| **1** | Extract `MissionInputController` | 2-3 hours | High - blocks M5/M6 | ✅ DONE |
| **2** | Extract `AttackSystem` | 1-2 hours | High - blocks Overwatch | ✅ DONE |
| **3** | Extract `MovementSystem` | 1 hour | Medium | ✅ DONE |
| **4** | Add `StatModifier` system | 2-3 hours | High - blocks M6 | ✅ DONE |
| **5** | Externalize to JSON | 2-3 hours | Medium - blocks balance pass | ✅ DONE |

---

## Validation Checklist

After each refactor:
- [ ] All existing tests pass
- [ ] Manual playtest: selection, movement, attack, abilities work
- [ ] No new Godot dependencies in `src/sim/`
- [ ] Event subscriptions properly cleaned up in `_ExitTree()`

---

## Files Changed Summary

**New files:**
- `src/scenes/mission/MissionInputController.cs`
- `src/scenes/mission/SelectionManager.cs`
- `src/sim/combat/AttackSystem.cs`
- `src/sim/combat/MovementSystem.cs`
- `src/sim/combat/StatModifier.cs`
- `data/weapons.json`
- `data/enemies.json`
- `data/abilities.json`

**Modified files:**
- `src/scenes/mission/MissionView.cs` (major reduction)
- `src/sim/combat/CombatState.cs` (major reduction)
- `src/sim/combat/Actor.cs` (stat access pattern change)
- `src/sim/data/Definitions.cs` (JSON loading)

---

## Notes

- This plan aligns with the hexagonal architecture in `ARCHITECTURE_GUIDELINES.md`
- All new sim classes remain engine-light (no Node dependencies)
- Scene-layer classes can depend on sim, not vice versa
- Consider adding integration tests for the new systems before starting M6
