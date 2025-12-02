---
trigger: always_on
---

# ARCHITECTURE_GUIDELINES.md

How we structure code in this project so it stays understandable, testable, and extendable.

This is the “how we think about architecture” doc.  
Concrete classes and files are in `TECH_DESIGN.md`.

---

## 1. Overall Style: Hexagonal-ish, Game Edition

We’ll follow a **ports-and-adapters / hexagonal** mindset adapted to Godot and a sim-heavy game:

- **Domain/Sim Core (Inside)**  
  Pure-ish game logic and state:
  - Campaign, sector, jobs, factions, economy.
  - Crew, items, abilities.
  - RTwP combat, actors, combat rules, AI.
  - RNG wrapper.

- **Application Layer (Middle)**  
  High-level orchestration:
  - `GameState` and flow.
  - Save/load.
  - Running scenarios/simulations.
  - Calling domain services in the right order.

- **Adapters (Outside)**  
  Godot-specific integration:
  - Scenes, nodes, UI, input.
  - Rendering, animations, sound.
  - Debug overlays.

**Rule of thumb:**  
Inside doesn’t know about Godot nodes. Outside is free to know about everything inside.

---

## 2. Dependency Direction

Think “onion”:

- **Inner layers don’t depend on outer layers.**
- Outer layers depend on inner, never the other way around.

Concretely:

- `src/sim/**` must not depend on:
  - `Node`, `SceneTree`, `Control`, UI elements.
  - Scene-specific scripts (`MissionView.gd`, etc.).

- `src/core/**` (GameState, FlowController, SaveManager):
  - Can depend on `sim`.
  - Can know about high-level scene names, but not detailed UI logic.

- `src/scenes/**` and `src/ui/**`:
  - Can depend on everything:
    - `sim` (to read/update state).
    - `core` (to drive flow/state).

If you ever see “MissionView imports CombatState” that’s fine.  
If you see “CombatState imports MissionView” that’s a bug.

---

## 3. Domain / Sim Core Guidelines (`src/sim/**`)

This is the heart of the game. It should be:

- **Engine-light**:  
  Pure GDScript classes, no Node tree assumptions.

- **Deterministic** when seeded:  
  All randomness goes through the RNG wrapper.

- **Testable** without scenes.

### 3.1 Data over objects, objects over inheritance

- Prefer simple **data structs** + functions to deep inheritance chains.
- Example:
  - `CrewMember` is a simple container with stats/traits.
  - Combat rules live in `CombatResolver`.
  - Items/abilities are data, not subclasses.

Avoid:
- `class AssaultRifle extends Weapon extends Item extends Node` inside the sim.

Prefer:
- `Item` struct with `type = "weapon"`, then data-driven behavior.

### 3.2 Stateless services where possible

Systems like `CombatResolver`, `EconomySystem`, `JobSystem` should:

- Be stateless or nearly stateless.
- Take explicit parameters:
  - `func resolve_attack(attacker, defender, weapon, map_state, rng) -> AttackResult`.

This makes them:
- Easier to test.
- Easier to reason about.
- Less likely to develop hidden dependencies.

### 3.3 Single “truth” objects for big states

There should be exactly one canonical instance for:

- `CampaignState` – the entire strategic game.
- `CombatState` – a single mission’s tactical game.

Avoid duplicating or “partially mirroring” these in multiple places.  
All mutation of campaign/mission state should go through them.

---

## 4. Application Layer Guidelines (`src/core/**`)

This layer orchestrates the domain/sim, but doesn’t draw anything.

### 4.1 GameState as the main orchestrator

`GameState`:

- Owns the current `CampaignState` and (if active) `CombatState`.
- Knows the current mode (`"menu" | "sector" | "mission" | "debrief"`).
- Provides use-case-like methods:

Examples:

- `start_new_campaign(seed, difficulty)`
- `start_mission(job_id)`
- `resolve_mission(result)`
- `save_campaign(slot)`
- `load_campaign(slot)`

Think of them as **use cases** / application services, not dumb getters.

### 4.2 FlowController for scene transitions

`FlowController`:

- Listens to changes in GameState’s mode.
- Loads/unloads scenes accordingly.
- Never implements rules (no combat or economy logic).  
  It just says “now show MissionView” or “now show SectorView”.

---

## 5. Adapter Layer Guidelines (Godot Scenes & UI)

Godot scenes are **adapters**, not the owners of game rules.

### 5.1 Scenes reflect state; they don’t define it

MissionView, SectorView, DebriefView:

- Pull state from `GameState` / `CombatState` / `CampaignState`.
- Render it.
- Send commands back into core/application.

They should not:

- Directly manipulate core state fields arbitrarily.
- Reimplement combat rules, economy rules, or job generation logic.

### 5.2 One direction for input

Pattern:

- Player input → UI/controller → Command → Core.

Example, mission:

1. Click on ground → MissionView figures out target tile.
2. MissionView calls: `GameState.current_mission.issue_move_order(actor_id, tile)`.
3. `CombatState` validates and enqueues the order.

MissionView doesn’t push actors around; it asks the sim to do it.

### 5.3 Signals vs direct calls

Use **signals**:

- To decouple scenes/components (UI widgets, overlays).
- For events that multiple listeners care about (e.g. “mission_completed”).

Use **direct calls**:

- For clear “controller → service” interactions within the same conceptual layer.
  - e.g., MissionView calling methods on `CombatState`.

Rule of thumb:

- Core/sim never emits or listens to Godot signals.  
- Signals are a UI/adapter concern.

---

## 6. Commands, Events, and State

To avoid spaghetti:

### 6.1 Commands into the sim

“Commands” are intention:

- Move this actor.
- Fire weapon at target.
- Use ability.
- Accept job.
- Travel to node.

They go **from outside to inside**:

- UI/Scene → `GameState` / `CombatState` methods → domain logic.

Commands should be:

- Validated in the sim.
- Either applied or rejected with a clear return value / error.

### 6.2 Events from the sim

Events are **facts** that have happened:

- Actor A died.
- Mission succeeded.
- Crew got injured.
- Faction rep changed.

Sim can represent events as:

- Log entries (for combat log).
- Result objects (e.g. mission result struct).
- Simple callbacks that adapters can use if needed (but keep them decoupled).

The sim should not **emit Godot signals**; instead, outer layers can poll or subscribe through clean interfaces.

---

## 7. Godot-specific Boundaries

### 7.1 Where Node APIs are allowed

Allowed:

- `src/scenes/**`
- `src/ui/**`
- `src/core/FlowController.gd` (for scene loading)

Not allowed (or strongly discouraged):

- `src/sim/**` (combat, campaign logic).

If you need a Godot type like `Vector2` for convenience in sim:

- That’s acceptable (it’s a plain math type).
- But don’t ever call `get_tree()`, `add_child()`, etc., from sim code.

### 7.2 Scenes own visuals, not logic

Example: MissionView

- Owns:
  - TileMap.
  - Actor sprite nodes.
  - Health bar controls.

- Logic it should **not** own:
  - Hit chance math.
  - Damage application.
  - AI decisions.
  - Mission victory conditions.

---

## 8. Error Handling & Constraints

### 8.1 Fail fast in sim

In core/sim:

- Assert invariants where they matter:
  - An actor must be in `CombatState.actors` before issuing an order.
  - A job’s target node must exist in the sector.
- Use clear `assert` or error returns rather than silently ignoring problems.

Better to crash in dev than silently corrupt state.

### 8.2 Avoid hidden state

Avoid:

- Globals in sim.
- Singletons that mix UI and sim concerns.

Preferred patterns:

- `GameState` owns a `CampaignState` and passes it into systems.
- Systems take `CampaignState` and return modified versions (or mutate in well-known places).

---

## 9. Testing Implications

Architecture is chosen to make testing straightforward:

- **Unit tests** target:
  - `CombatResolver`, `EconomySystem`, `JobSystem`, `TravelSystem`.
- **Scenario tests**:
  - Use `CombatState` and `CampaignState` directly.
- **No scenes required** for logic tests.

If a new piece of logic is hard to test without scenes, check if architecture was violated (logic leaking into adapters).

---

## 10. Practical Patterns & Anti-patterns

### 10.1 Good patterns

- “Dumb” data objects + dedicated systems:
  - `CrewMember` + `CrewSystem`.
  - `Actor` + `CombatResolver`.
- Pure functions for gameplay rules:
  - `compute_hit_chance`, `compute_travel_cost`.
- Scene controllers that:
  - Just bind `state ↔ view` and forward input.

### 10.2 Anti-patterns (avoid)

- Godot Node scripts that:
  - Own authoritative game state **and** rendering **and** rules.
- Deep inheritance hierarchies for units/settings.
- Random `randi()` calls scattered everywhere.
- UI scripts that:
  - Reach directly into sim fields and mutate them without going through methods.

---

## 11. When to Update This Document

Update this file when:

- You introduce a new architectural concept (e.g., a shared event bus).
- You change how sim → UI communication happens.
- You relax/tighten a boundary (e.g., allow some Node usage in a sim area for a good reason).

Keep edits targeted and explicit so this remains a trustworthy reference.

---
