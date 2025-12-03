# Post-M3 Cleanup Spec

**Status: ✅ COMPLETE** — All items addressed.

Refactoring items identified after M3 implementation. Each item has a relevance score:
- **High**: Will cause bugs or block future milestones
- **Medium**: Technical debt that compounds over time
- **Low**: Nice-to-have improvements

---

## High Relevance

### 1. `CombatResolver.ApplyDamage()` is dead code
**File**: `src/sim/combat/CombatResolver.cs` (lines 112-129)

`ApplyDamage()` duplicates logic that's already in `Actor.TakeDamage()`. It's never called anywhere. The `ExecuteAttack()` method in `CombatState` calls `target.TakeDamage()` directly.

**Problem**: Confusing API surface. Future developers might call the wrong method and bypass events (`DamageTaken`, `Died`).

**Fix**: Delete `CombatResolver.ApplyDamage()`.

---

### 2. `ActorView.Setup()` doesn't unsubscribe from `ReloadCompleted`
**File**: `src/scenes/mission/ActorView.cs` (lines 70-89)

When `Setup()` is called with a new actor, it unsubscribes from `DamageTaken` and `Died` but not `ReloadCompleted`. This causes event handler leaks if an ActorView is reused.

**Problem**: Memory leak and potential double-firing of `OnReloadCompleted`.

**Fix**: Add `actor.ReloadCompleted -= OnReloadCompleted;` in the unsubscribe block.

---

## Medium Relevance

### 3. `Actor` class is getting large (351 lines)
**File**: `src/sim/combat/Actor.cs`

Actor now handles: identity, stats, movement, combat state, ammo/reload, events, visual position. This is approaching the point where it should be split.

**Problem**: Hard to test individual concerns. Changes to movement might break reload logic.

**Fix**: Consider extracting:
- `ActorMovement` component (movement state, `Tick` movement logic)
- `ActorCombat` component (weapon, ammo, reload, cooldown)

**Note**: Only do this if Actor continues to grow in M4+. Current size is manageable.

---

### 4. Magic strings for actor types
**Files**: Multiple (`CombatState.cs`, `AIController.cs`, `ActorView.cs`)

Actor types are hardcoded strings: `"crew"`, `"enemy"`, `"drone"`. Used in ~15 places.

**Problem**: Typo in one place causes silent bugs. No compile-time checking.

**Fix**: Create `ActorType` enum or constants class:
```csharp
public static class ActorTypes
{
    public const string Crew = "crew";
    public const string Enemy = "enemy";
    public const string Drone = "drone";
}
```

---

### 5. `UpdateAmmoDisplay()` called every frame
**File**: `src/scenes/mission/ActorView.cs` (line 227)

`_Process()` calls `UpdateAmmoDisplay()` and `UpdateReloadIndicator()` every frame, even when nothing changed.

**Problem**: Unnecessary work. Creates garbage from string formatting (`$"{actor.CurrentMagazine}"`).

**Fix**: Only update when ammo actually changes. Track `lastDisplayedMagazine` and compare, or use event-driven updates.

---

## Low Relevance

### 6. Duplicate movement direction calculation
**Files**: `Actor.cs` (lines 129-133), `CombatState.cs` (lines 137-140), `AIController.cs` (lines 185-188)

The same `Mathf.Clamp(diff.X, -1, 1)` pattern appears in three places.

**Problem**: Minor duplication. If movement logic changes, must update multiple places.

**Fix**: Extract to utility method like `GridUtils.GetStepDirection(Vector2I from, Vector2I to)`.

---

### 7. `CombatState` has two ways to check mission completion
**File**: `src/sim/combat/CombatState.cs`

Both `IsComplete` property and `Phase == MissionPhase.Complete` indicate the same thing.

**Problem**: Redundant state. Could get out of sync (though currently they're set together).

**Fix**: Remove `IsComplete` and use only `Phase`. Or make `IsComplete` a computed property: `public bool IsComplete => Phase == MissionPhase.Complete;`

---

## Summary

| # | Issue | Relevance | Status |
|---|-------|-----------|--------|
| 1 | Dead `ApplyDamage()` code | High | ✅ Deleted |
| 2 | Missing event unsubscribe | High | ✅ Fixed |
| 3 | Actor class size | Medium | ⏸️ Deferred (manageable at 346 lines) |
| 4 | Magic actor type strings | Medium | ✅ Created `ActorTypes` constants |
| 5 | Per-frame UI updates | Medium | ✅ Added `lastDisplayedMagazine` tracking |
| 6 | Duplicate direction calc | Low | ✅ Created `GridUtils.GetStepDirection()` |
| 7 | Redundant completion state | Low | ✅ `IsComplete` now computed property |

**New files created**:
- `src/sim/combat/ActorTypes.cs` — Constants for actor type strings
- `src/sim/combat/GridUtils.cs` — Grid utility methods
