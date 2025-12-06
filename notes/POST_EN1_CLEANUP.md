# Post-EN1 Cleanup Spec

Issues identified during EN1 implementation. **All resolved.**

---

## Issues (All Fixed)

### 1. ✅ `EncounterStepResult` in wrong file

**Status**: Fixed  
**Fix**: Moved to `src/sim/encounter/EncounterStepResult.cs`

---

### 2. ✅ `SkillCheckDef` in wrong file

**Status**: Fixed  
**Fix**: Moved to `src/sim/encounter/SkillCheckDef.cs`

---

### 3. ✅ `CrewSnapshot` in wrong file

**Status**: Fixed  
**Fix**: Moved to `src/sim/encounter/CrewSnapshot.cs`

---

### 4. ✅ Non-deterministic ID generation

**Status**: Fixed  
**Fix**: `EncounterInstance.Create()` now requires explicit ID or RNG stream. Added overload `Create(template, rng)` for deterministic ID generation.

---

### 5. ✅ Missing serialization support

**Status**: Fixed  
**Fix**: Added `GetState()`/`FromState()` methods and `EncounterInstanceData`/`EncounterEffectData` classes.

---

### 6. ✅ No event emission

**Status**: Fixed  
**Fix**: `EncounterRunner` now accepts `EventBus` in constructor and emits all 4 encounter events (`EncounterStartedEvent`, `EncounterNodeEnteredEvent`, `EncounterOptionSelectedEvent`, `EncounterCompletedEvent`).

---

## Summary

All 6 issues resolved. 78 tests passing.
