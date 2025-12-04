How we keep this thing debuggable, testable, and tunable without losing our minds.

This is about **how to test and debug**, not what the game is.  
Architecture details live in `TECH_DESIGN.md`. Design intent lives in `GAME_DESIGN.md`.

---

## 1. Goals & Principles

**Goals**

- Catch dumb regressions early (e.g. weapons suddenly doing 0 damage).
- Make balance iteration fast and targeted.
- Make “WTF just happened?” moments explainable via logs/overlays.
- Allow automated sanity checks on campaign survivability and economy.

**Principles**

- **Deterministic sim** wherever practical (combat, sector generation).
- **Pure logic tests first**, UI tests later.
- **In-game debug tools** for day-to-day iteration.
- **Small, focused tests** instead of one giant “does everything” harness.

---

## 2. Determinism & RNG

### 2.1 RNG wrapper

We use a simple RNG wrapper instead of sprinkling `randi()` calls.

- `src/core/Rng.gd`:
  - Holds an internal seed/state.
  - Exposes functions:
    - `randf()`, `randi()`, `rand_range(min, max)`, `choice(array)` etc.

Guidelines:

- `CampaignState` and `CombatState` each own a dedicated `Rng`.
- Systems that need randomness receive an `rng` instance explicitly.
- Test code passes a known seed so results are reproducible.

### 2.2 What must be deterministic

- Sector generation (given the same seed).
- Job generation from a given `CampaignState` + RNG seed.
- Combat resolution: hit/miss, damage, AI decisions from a fixed seed.

What doesn’t need strict determinism right now:

- Visual-only effects.
- UI animation timing.

---

## 3. Types of Tests

### 3.1 Unit tests (logic-level)

Scope:

- Combat math:
  - Hit chances vs stats, cover, range.
  - Damage application, armor, status effects.
- Economy:
  - Cost/reward application.
  - “Can afford?” logic.
- Sector:
  - Graph generation properties (connected, node count, risk distribution).
- Crew progression:
  - XP → level thresholds.
  - Derived stat calculations.

Implementation:

- Use Godot’s built-in test runner or a simple custom harness in `src/tests/`.
- Tests import sim classes (`CombatResolver`, `EconomySystem`, etc.) without loading scenes.

### 3.2 Scenario / simulation tests

Scope:

- “Black box” tests over bigger chunks:

Examples:

- **Combat scenarios**:
  - Given:
    - Map layout.
    - 4 crew with loadouts.
    - 5 enemies.
  - Run an auto-resolve AI-vs-AI fight N times with a fixed strategy, collect stats:
    - Win rate, average HP remaining, ammo usage.

- **Campaign survivability**:
  - Given:
    - Difficulty.
    - Starting crew/ship/resources.
  - Simulate X missions with a generic “AI player” taking safest jobs, measure:
    - Median run length.
    - Bankruptcy rate.
    - Total crew deaths.

Implementation:

- Dedicated scripts under `src/sim/tests/` or `src/tools/sim/`.
- Write results to console/log or file (CSV/JSON) for manual inspection.

### 3.3 Manual testing

Still necessary, but guided by tools:

- RTwP feel, responsiveness, UI clarity.
- Edge cases like:
  - Multiple enemies dying at once.
  - Multi-step travel events.
  - Crew death mid-objective.

Devtools are designed to shorten the feedback loop here.

---

## 4. Devtools in the Game

### 4.1 Dev mode toggle

- `Config.gd`:
  - `var DEV_MODE := true` (or environment-based).
- When `DEV_MODE`:
  - Enable debug overlays.
  - Show extra buttons/menus.
  - Allow debug hotkeys/cheats.

Never rely on dev-only tools for core gameplay.

### 4.2 Debug hotkeys

Planned hotkeys (v0.1):

**Global (anywhere)**

- `F1` – Toggle debug UI (FPS, scene name, etc.).
- `F5` – Quick reload current scene (developer convenience).

**Sector / campaign**

- `Ctrl+Shift+J` – Spawn a test job at current node.
- `Ctrl+Shift+R` – +1000 credits, +fuel/parts/meds (cheat for testing).
- `Ctrl+Shift+F` – Max out reputation for all factions.

**Mission (combat)**

- `Ctrl+Shift+K` – Kill all enemies.
- `Ctrl+Shift+H` – Heal all crew to full.
- `Ctrl+Shift+D` – Toggle pathfinding/LOS debug.
- `Ctrl+Shift+L` – Toggle “combat log details” overlay (hit chance breakdowns, etc.).

These allow you to jump to specific states quickly instead of grinding through the campaign.

---

## 5. Combat Debugging

### 5.1 Combat log

Internally, `CombatState` and `CombatResolver` should:

- Push log entries into a structured log object:
  - e.g. `CombatLogEntry` with:
    - `tick`, `type`, `actor_id`, `target_id`, `details (Dictionary)`.

In dev mode:

- Mission UI can:
  - Show a scrollable text log (“Actor A shot Actor B (72% hit, rolled 0.34)”).
  - Or show a compact overlay on hover/click.

This is the main way to answer:
- “Why did that shot miss?”
- “How did that guy die so fast?”

### 5.2 Hit chance breakdown overlay

When hovering a target (dev mode):

- Show a breakdown:
  - Base chance from stats.
  - Range modifier.
  - Cover modifier.
  - Status modifiers.
- This uses a pure helper function from `CombatResolver` so the logic is shared with tests.

### 5.3 Pathfinding and LOS debug

Dev overlay in missions:

- Optional tile highlights:
  - Path from selected actor to clicked tile.
  - LOS line + blocked tiles between shooter and target.

Implementation:

- MissionView draws simple debug lines/tiles if a `DebugDrawer` flag is on.

---

## 6. Campaign & Economy Debugging

### 6.1 Sector inspector

In dev mode on sector screen:

- Optional panel listing:
  - All nodes (name, faction, shop, job count).
  - All active jobs and their parameters.
  - Reputation values per faction.
- Buttons:
  - “Jump to node X” (teleport ship).
  - “Add test job type Y here”.

This avoids needing to “fly around” just to test job variety or event placement.

### 6.2 Resource cheats

For testing:

- Buttons or hotkeys to:
  - Add/remove resources in large chunks (money, fuel, parts, meds, ammo).
- Useful for:
  - Balancing purchases and repairs.
  - Testing UI states at high/low resource levels.

---

## 7. Test Harnesses (Concrete Hooks)

### 7.1 Combat test harness

A script such as `src/tools/combat_sandbox.gd`:

- Creates:
  - A small `MapState` (simple room/corridor).
  - A few crew + enemies.
  - A `CombatState` running in headless mode.
- Runs:
  - Simulated turns with fixed “AI scripts” (e.g., always advance and shoot nearest).
- Outputs:
  - Win/loss.
  - HP distribution.
  - Shots fired, hit/miss ratio.
  - Ammo usage.

Usage:

- Run from editor / CLI to compare configs or spot regressions in combat math.

### 7.2 Campaign/economy sim harness

`src/tools/campaign_sim.gd`:

- Creates a `CampaignState` with:
  - Default starting crew/ship/resources.
- Implements a dummy policy:
  - Always pick the safest job at the current node.
  - Always repair hull when below threshold if resources suffice.
- Runs:
  - N missions or until everyone dies.
- Outputs:
  - Run length.
  - Final resources.
  - Total deaths.

Run many times with different seeds to get rough survivability metrics.

---

## 8. Workflow & Discipline

### 8.1 When to write tests

- **Before or during** changes to:
  - Combat math (`CombatResolver`, `AbilitySystem`).
  - Economy rules (`EconomySystem`).
  - Sector generation.
- When a bug is found:
  - Add a regression test where possible.
  - Example: “Shots can no longer hit through walls” → add LOS unit test.

### 8.2 Using devtools instead of brute-force play

Examples:

- Need to tune hit chances?
  - Use combat sandbox or dev-mode overlay.
- Want to see if fuel costs are punishing?
  - Use resource cheats + sector inspector, then run a few auto campaigns.
- UI bug during mission?
  - Use mission hotkeys to quickly set up similar states (full HP, all enemies alive, etc.).

---

## 9. AI Collaboration Notes

For AI changes to tests/devtools:

- **Do**:
  - Add new unit tests under a clear path (`src/tests/` or `src/sim/tests/`).
  - Extend combat log entries and debug overlays consistently.
  - Use existing RNG wrapper instead of raw `rand*()` calls.
- **Don’t**:
  - Introduce new ad-hoc logging systems.
  - Bypass `CombatResolver` or other central systems in tests.
  - Rely on UI scenes for logic validation (logic should be testable without scenes).

If a change affects how something is tested (e.g., new damage model), this file should be updated with a short addition rather than left to drift.

---
