# Systems Foundation Roadmap

This document defines the **implementation order** for Systems Foundation.

- Each milestone is a **vertical slice**: after reaching it, you have a coherent state.
- G0 requires SF0–SF2. Later milestones support G1+.

---

## Overview of Milestones

1. **SF0 – RNG Streams & Config Loading**
2. **SF1 – Time System**
3. **SF2 – Event Bus (Minimal)**
4. **SF3 – Save/Load (Campaign State)**

---

## SF0 – RNG Streams & Config Loading

**Goal:**  
Establish deterministic randomness and data-driven configuration.

**Key capabilities:**

- RNG wrapper with separate streams:
  - `campaign_rng` for strategic layer.
  - `tactical_rng` for combat.
  - Seed storage and restoration.
- Config loading:
  - Load weapon definitions from `data/weapons.json`.
  - Load ability definitions from `data/abilities.json`.
  - Load enemy definitions from `data/enemies.json`.
  - Typed access via registry.
- Validation:
  - Required fields checked on load.
  - Clear error messages for malformed data.

**Why first:**  
Tactical already uses RNG and config data. Formalizing this ensures determinism and testability.

**Deliverables:**
- `RngService` with stream management.
- `ConfigRegistry` with typed loaders.
- Unit tests for RNG determinism.
- Unit tests for config loading.

**Status:** ✅ Complete

**Implementation:**
- `RngStream.cs` - Single seeded RNG stream with serializable state
- `RngService.cs` - Multi-stream RNG manager with campaign/tactical streams
- `ValidationResult.cs` - Validation result accumulator
- `ConfigRegistry.cs` - Config loading with validation and fail-fast mode
- `CombatRng.cs` - Marked obsolete, replaced by RngStream
- Unit tests in `SF0RngTests.cs` and `SF0ConfigTests.cs`

---

## SF1 – Time System

**Goal:**  
Unified time representation for both campaign and tactical layers.

**Key capabilities:**

- `CampaignTime` with:
  - `CurrentDay`: Current day (int, 1-indexed).
  - `AdvanceDays(int days)`: Advance campaign time.
  - `DayAdvanced` event for subscribers.
- `TimeSystem` (tactical, unchanged):
  - `CurrentTick`: Current tick within mission.
  - `Update(dt)`: Advance ticks at 20/sec.
- Time queries via `GameState`:
  - `GetCampaignDay()`, `GetCampaignDayFormatted()`.
  - `GetTacticalTick()`, `GetTacticalTimeFormatted()`.
- Time-consuming actions:
  - Travel: +1-N days based on distance.
  - Mission: +1 day on start.
  - Rest: +3 days, heals 1 injury.
- Job deadlines:
  - `DeadlineDays` (relative), `DeadlineDay` (absolute).
  - `HasDeadlinePassed()`, `DaysUntilDeadline()`.

**Why here:**  
Campaign time is needed for contracts, deadlines, and travel. Tactical already has tick-based time.

**Deliverables:**
- `CampaignTime` class with serialization support.
- Integration with `CampaignState`, `TravelSystem`, `JobSystem`.
- Time query accessors in `GameState`.
- Unit tests for time advancement.

**Status:** ✅ Complete

**Implementation:**
- `CampaignTime.cs` - Campaign day tracking with advancement API
- `CampaignState.Time` - Owns CampaignTime, Rest() action, mission time cost
- `TravelSystem` - Travel time cost based on distance
- `Job` - DeadlineDays, DeadlineDay, HasDeadline
- `JobSystem` - Generates jobs with deadlines
- `GameState` - Time query accessors
- Unit tests in `SF1TimeTests.cs` (31 tests)

---

## SF2 – Event Bus (Minimal)

**Goal:**  
Decouple domains via a simple event system.

**Key capabilities:**

- Typed event registration:
  - `Subscribe<TEvent>(Action<TEvent> handler)`.
  - `Unsubscribe<TEvent>(Action<TEvent> handler)`.
- Event dispatch:
  - `Publish<TEvent>(TEvent evt)`.
- Initial event types:
  - `MissionCompletedEvent`.
  - `ActorDiedEvent`.
  - `ResourceChangedEvent`.

**Why here:**  
Simulation (G3) needs to subscribe to events from other domains. Starting simple now avoids coupling.

**Deliverables:**
- `EventBus` class.
- Event type definitions.
- Integration points in Tactical (emit on mission end).

---

## SF3 – Save/Load (Campaign State)

**Goal:**  
Persist and restore campaign state.

**Key capabilities:**

- Serialize `CampaignState` to JSON.
- Deserialize and restore.
- Include:
  - RNG stream states.
  - Time state.
  - Player state (crew, ship, resources).
  - World state (when implemented).
- Version field for future migration.

**Why here:**  
Required for G1 when the campaign loop exists. Not needed for G0 tactical-only testing.

**Deliverables:**
- `SaveManager` with `Save()` and `Load()`.
- Save file format documentation.
- Unit tests for round-trip serialization.

---

## G0 Scope Summary

For G0, implement **SF0** and **SF1**. SF2 can be minimal (just the interface). SF3 is G1.

| Milestone | G0 Required | Notes |
|-----------|-------------|-------|
| SF0 | ✅ Yes | Formalize existing RNG/config |
| SF1 | ✅ Yes | Campaign time for future use |
| SF2 | ⚠️ Minimal | Interface only, full use in G3 |
| SF3 | ❌ No | Needed for G1 |
