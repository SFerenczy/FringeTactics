# HH1 Refactoring List

Issues identified during HH1 implementation review that will definitely cause problems.

**Status**: All refactorings completed ✅

---

## 1. Duplicate Movement Logic in Actor.cs ✅

**Problem**: `Actor.Tick()` and `Actor.AdvanceMovement()` both contain nearly identical movement processing code with the `MovingToPosition` event. This violates DRY and will cause maintenance issues when movement logic needs to change.

**Fix Applied**: Extracted `CommitTileMovement()` private method that both `Tick()` and `AdvanceMovement()` now call.

---

## 2. OverwatchSystem Duplicates Attack Execution Logic ✅

**Problem**: `OverwatchSystem.ExecuteReactionFire()` duplicates significant logic from `AttackSystem.ExecuteAttack()`:
- God mode checks
- Damage application
- Kill recording
- Statistics tracking

**Fix Applied**: Created `AttackExecutor` static helper class with `ApplyAttackResult()` and `FormatAttackLog()` methods. Both `AttackSystem` and `OverwatchSystem` now use this shared logic.

---

## 3. Missing Event Unsubscription in ActorView.Setup() ✅

**Problem**: `ActorView.Setup()` unsubscribes from old actor events but doesn't include the new overwatch events. If `Setup()` is called multiple times with different actors, the old overwatch subscriptions will leak.

**Fix Applied**: Added `OverwatchActivated` and `OverwatchDeactivated` unsubscription in `Setup()`.

---

