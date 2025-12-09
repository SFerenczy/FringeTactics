# HH2 Refactoring List

Review of HH2 implementation changes. All issues have been resolved.

---

## Issues Found & Resolved

### 1. ✅ SuppressionResult converted to struct
**File**: `src/sim/combat/systems/SuppressionSystem.cs`

Converted from class to struct for consistency with `AttackResult` and to avoid heap allocations.

---

### 2. ✅ Duplicate attack resolution logic eliminated
**File**: `src/sim/combat/systems/OverwatchSystem.cs`

Added `accuracyModifier` parameter to `CombatResolver.ResolveAttack`. `OverwatchSystem.ResolveOverwatchAttack` now delegates to the shared method.

---

### 3. ✅ Unused AbilityData definitions removed
**File**: `src/sim/combat/data/AbilityData.cs`

Removed `SuppressiveFire` and `AreaSuppression` ability definitions. Suppression is handled directly by `SuppressionSystem` with its own balance constants.

---

## Summary

All refactorings completed. Tests pass (1358/1358).
