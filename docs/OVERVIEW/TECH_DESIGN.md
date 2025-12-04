Technical architecture and code patterns for the project.

This document describes **how** the game is structured in code and data, not what the game is about.  
For design/feature intent, see `GAME_DESIGN.md`.

---

## 1. Tech Stack & Principles

**Engine**

- Godot 4.x, 2D project.

**Language**

- C# for v0.1 (fast iteration, good editor integration).
- Rust is reserved for potential future “core sim” crate; not part of v0.1.

**Core principles**

- **Simulation-first**: game rules and state should be testable without loading scenes.
- **Separation of concerns**:
  - Core sim logic separate from presentation.
  - UI shows state and issues commands; it doesn’t implement rules.
- **Deterministic where it matters**:
  - Combat, economy, sector generation use seedable RNG.
- **Data-driven**:
  - Factions, jobs, items, enemies, map templates are data, not hardcoded.

---

## 2. High-Level Architecture

### 2.1 Layered structure

The project is organized into three conceptual layers:

1. **Core Simulation (`src/sim/`)**
   - Pure-ish logic:
     - Campaign state, sector, factions, jobs, economy.
     - Crew, items, abilities.
     - Tactical sim (RTwP, combat, AI, map/grid).
   - Depends on Godot only for basic types if necessary (Vector2, etc.), but **not** on scene tree, nodes, or UI controls.

2. **Presentation & Engine Integration (`src/scenes/`, `src/ui/`)**
   - Godot scenes and nodes:
     - Sector view, mission view, dialogs, HUDs.
   - Reads from sim state, renders it.
   - Sends commands back into sim (move unit, fire weapon, accept job).

3. **Game Flow / Glue (`src/core/`)**
   - Autoloaded singletons and flow managers:
     - `GameState` (current campaign state, current mode).
     - State machine for switching between main menu / sector / mission / debrief.
   - Save/load integration.
   - Devtools toggles.

### 2.2 Project structure (intended)

```text
src/
  core/
    GameState.gd
    FlowController.gd
    SaveManager.gd
    Config.gd
  sim/
    campaign/
      CampaignState.gd
      Sector.gd
      FactionSystem.gd
      JobSystem.gd
      EconomySystem.gd
      TravelSystem.gd
      ShipState.gd
    combat/
      CombatState.gd
      Actor.gd
      AbilitySystem.gd
      CombatResolver.gd
      AIController.gd
      TimeSystem.gd
      InteractableSystem.gd
    data/
      DataStore.gd
      CrewDefinitions.gd
      EnemyDefinitions.gd
      ItemDefinitions.gd
      FactionDefinitions.gd
      JobDefinitions.gd
      MapTemplates.gd
  scenes/
    main_menu/
    sector/
    mission/
    debrief/
  ui/
    components/
    sector/
    mission/
  assets/
    tilesets/
    sprites/
    fonts/
    audio/
````

Exact filenames can evolve, but the **layer separation** is important.

---

## 3. Core Data Model (Overview)

**Key simulation entities**:

* `CampaignState`
* `Sector` + `SectorNode`
* `Faction`
* `Job`
* `ShipState`
* `CrewMember`
* `Item`
* `Ability`
* `EnemyArchetype`
* `MissionConfig`
* `CombatState` + `Actor`
* `Resources` (money, fuel, parts, meds, ammo)

### 3.1 Data modeling style

* Use simple C# classes or Godot Resources for:

  * Templates (faction archetypes, enemy types, item types, job templates).
* Use plain C# objects (no Nodes) for **runtime state**:

  * `CampaignState`, `CombatState`, etc.

Example (simplified):

```csharp
# src/sim/campaign/CrewMember.gd
class_name CrewMember

var id: int
var name: String
var role: String
var level: int = 1
var xp: int = 0

var stats := {
    "aim": 0,
    "toughness": 0,
    "reflexes": 0,
    "piloting": 0,
    "mechanics": 0,
    "social": 0,
    "tech": 0,
}

var traits: Array[String] = []
var injuries: Array[String] = []
var perks: Array[String] = []
```

---

## 4. Campaign / Strategic Layer Systems

All of these live under `src/sim/campaign/`.

### 4.1 CampaignState

**Responsibility**

* Single aggregate object representing the **entire campaign**.

**Contains**

* Sector graph.
* Player’s current node.
* Factions & reputation.
* Ship state.
* Crew list.
* Resources (money, fuel, parts, meds, ammo).
* Active jobs (taken, available).
* Difficulty setting, campaign seed.

**Usage**

* `GameState` owns an instance of `CampaignState` and passes it into systems for mutation.
* Save/load serializes `CampaignState`.

---

### 4.2 Sector & nodes

`Sector.gd`:

* Holds:

  * Array of `SectorNode`s.
  * Adjacency representation (edges) with fuel costs and risk tags.

`SectorNode`:

* ID, name, type (`station`, `outpost`, `settlement`, etc.).
* Owning faction (optional).
* Shop inventory (optional).
* List of jobs currently offered (IDs or references).

**Generation**

* `Sector.generate(seed)`:

  * Creates ~40–50 nodes.
  * Connects them in a graph with:

    * Ensured connectivity.
    * Some “risky” routes flagged with higher event chances.

---

### 4.3 FactionSystem

`FactionSystem.gd`:

* Represents 3 faction archetypes with generated details.
* Tracks:

  * `reputation[faction_id] -> int`.
* Exposes:

  * `get_faction(faction_id)`
  * `change_reputation(faction_id, delta)`
  * `get_available_jobs_for_node(node, campaign)`

Factions influence:

* Job generation: types, difficulty, pay.
* Event outcomes (how patrols treat you, etc.).
* Prices or access to gear (later).

---

### 4.4 JobSystem

`JobSystem.gd`:

* Jobs are instances created from **job templates** (defined in data).
* Each job has:

  * ID
  * Template key
  * Employer faction
  * Origin node
  * Target node
  * Difficulty rating
  * Rewards (money, resources, rep)
  * MissionConfig seed / parameters

Responsibilities:

* Generate job lists for nodes:

  * Based on faction presence, sector state, difficulty ramp.
* Instantiate `MissionConfig` for a job when accepted:

  * Map template, enemy archetypes, objective type.

---

### 4.5 TravelSystem

`TravelSystem.gd`:

* Executes travel between two nodes:

Input:

* `CampaignState`, source node, target node.

Logic:

* Check if edge exists.
* Compute fuel cost and apply.
* Roll for travel events based on:

  * Edge risk.
  * Faction hostility.
  * Player traits/ship upgrades.

Output:

* Updated `CampaignState`.
* Possibly:

  * Triggered event(s).
  * Immediate mission (e.g., ambush).
  * Resource/injury changes.

---

### 4.6 EconomySystem

`EconomySystem.gd`:

* Holds rules for:

  * Wages (later).
  * Repairs (cost in parts and money).
  * Shop transactions (prices, factions, supply level).

Responsibilities:

* Pure-ish functions like:

  * `can_afford(campaign, cost) -> bool`
  * `apply_cost(campaign, cost)`
  * `apply_reward(campaign, reward)`

---

### 4.7 ShipState

`ShipState.gd`:

* Canonical representation of the player’s ship:

  * Hull integrity.
  * Fuel tank capacity.
  * Base evasion / travel modifiers.
  * Upgrade slots or installed modules.

Responsibilities:

* Provide modifiers to:

  * Travel events.
  * Repair speed.
  * Recovery bonuses between missions.

---

## 5. Tactical Layer Systems (On-Foot RTwP)

All under `src/sim/combat/`.

### 5.1 CombatState

Central object for a single mission instance.

Contains:

* Reference to `MissionConfig` (map, objectives, enemy composition).
* List of `Actor`s (crew + enemies + neutrals).
* Map/grid data (or reference).
* TimeSystem state (current tick, paused flag).
* Objective state (mission success/failure conditions).

Responsibilities:

* Single point of mutation for mission sim.
* Provides API:

  * `tick(dt)`
  * `issue_order(actor_id, order)`
  * `apply_ability(actor_id, ability_id, target)`
  * `get_snapshot()` for UI.
* `IsComplete` is now a computed property: `Phase == MissionPhase.Complete`

---

### 5.2 TimeSystem (RTwP)

`TimeSystem.gd`:

* Maintains a discrete tick clock:

  * e.g. `TICKS_PER_SECOND = 20`.
* Does not use `delta` directly; uses fixed timestep.

State:

* `tick: int`
* `is_paused: bool`
* (Optionally) `time_scale` for fast-forward.

Responsibilities:

* Advance tick if not paused.
* Notify combat subsystems to update per tick:

  * Actors.
  * Projectiles.
  * Effects (buffs/debuffs).

---

### 5.3 Actors

`Actor.gd`:

* Represents any unit on the mission map:

Fields (indicative):

* `id: int`
* `type: String` (e.g. `"crew"`, `"enemy"`, `"drone"`)
* `crew_id` (if it’s a player crew member, links back to `CrewMember`).
* `position: Vector2i` (grid coordinate).
* `hp: int`, `max_hp: int`
* `status_effects: Array[String]`
* `current_orders: Array[Order]`
* `weapon_id`, `armor_id`, `gadget_ids`

Methods:

* `enqueue_order(order: Order)`
* `clear_orders()`
* `update_tick(combat_state)`

Pattern:

* `Actor` is logic-only; actual sprites and animations are handled by a `MissionActorView` in the scene layer.
* Dead code `ApplyDamage()` removed - use `Actor.TakeDamage()` directly
* Event unsubscribe fixed for `ReloadCompleted` to prevent leaks

---

### 5.4 Map & Navigation

`MapState.gd`:

* Represents on-foot mission map:

Fields:

* Grid definition (tile size, walkable flags).
* Cover flags per tile (later).
* Interactable objects locations (doors, terminals, etc.).

Responsibilities:

* Pathfinding (A*):

  * `find_path(from: Vector2i, to: Vector2i) -> Array[Vector2i]`
* Queries:

  * `is_walkable(tile)`
  * `has_line_of_sight(a, b)`

Map data is derived from **map templates** + randomization.

---

### 5.5 CombatResolver

`CombatResolver.gd`:

* Pure rules for applying attacks and damage.

Inputs:

* Attacker `Actor` + defender `Actor`.
* Weapon definition.
* MapState (for LOS / cover).
* RNG object.

Outputs:

* Hit/miss.
* Damage amount.
* Status effect applied (if any).
* Log entries (for debug/UI).

Pattern: no Node logic; just functions like:

```csharp
func compute_attack_result(attacker: Actor, defender: Actor, weapon: Weapon, map: MapState, rng) -> AttackResult:
    # ...
```

---

### 5.6 AbilitySystem

`AbilitySystem.gd`:

* Central dispatcher for **abilities beyond basic attack**.

Data:

* Ability definitions loaded from data (`AbilityDefinitions.gd`):

  * Targeting mode (self, single target, area).
  * Effect list (damage, heal, move, status).
  * Cooldown, cost, requirements (weapon type, role).

Logic:

* `can_use_ability(actor, ability_id, combat_state) -> bool`
* `execute_ability(actor, ability_id, target, combat_state, rng)`

Effects are built from a small set of primitives:

* ApplyDamage
* ApplyHeal
* ApplyStatus
* MoveActor
* SpawnObject (e.g. turret, mine)
* Interact (e.g. hack door/terminal)

---

### 5.7 AIController

`AIController.gd`:

* Provides AI decision-making for enemies.

Approach:

* Simple state-machine per enemy archetype: `Idle`, `Patrol`, `Engage`, `Retreat`.
* Decision cycle at regular intervals (e.g. every N ticks):

  * Select target (closest, most exposed, etc.).
  * Choose action:

    * Move to cover.
    * Attack.
    * Use ability.
    * Fall back.

Responsibilities:

* Produce `Order`s for AI actors that the sim executes like player orders.

---

### 5.8 InteractableSystem

`InteractableSystem.gd`:

* Manages all non-actor interactables:

  * Doors (open/close/lock).
  * Terminals (hack, unlock).
  * Environmental hazards (explosive barrel, etc.).

Data:

* `Interactable` objects with type, position, state.

Logic:

* `interact(actor, interactable_id, action)`:

  * Check allowed actions (requires tech skill, etc.).
  * Trigger effects (door opens, explosion, turret disabled).

v0.1 scope is small: basic doors + 1–2 systemic toys.

---

## 6. Shared Systems

### 6.1 Constants & Shared Values

**GridConstants.cs** (`src/scenes/`):

- Central location for all grid rendering constants:
  - `TileSize` - Size of a single tile in pixels
  - Tile colors (wall, void, floor, cover heights)
  - Cover indicator colors and dimensions
- Prevents duplication across scene files
- Single source of truth for visual consistency

**CombatBalance.cs** (`src/sim/combat/`):

- All combat balance parameters:
  - Hit chance ranges, range penalties
  - Cover height reduction values (15%/30%/45%)
  - `GetCoverReduction()` helper method
- Used directly by `CombatResolver` (no duplicate aliases)

**ActorTypes.cs** (`src/sim/combat/`):

- Constants for actor type strings:
  - `Crew = "crew"`, `Enemy = "enemy"`, `Drone = "drone"`
- Prevents typos and provides compile-time checking

**GridUtils.cs** (`src/sim/combat/`):

- Utility methods for grid calculations:
  - `GetStepDirection(Vector2I from, Vector2I to)` - Returns normalized direction (-1, 0, or 1)
- Eliminates duplicate movement logic across files

### 6.2 Crew & Progression

Under `src/sim/campaign/` but used by combat.

`CrewMember.gd`:

* See earlier example.

`CrewSystem.gd`:

* Functions:

  * `apply_xp(crew_member, amount)`
  * `level_up(crew_member, choice)`
  * Derived stats for combat:

    * `get_combat_stats(crew_member, equipment)`

Combat uses **snapshots** of crew stats; upon mission completion, it writes back injuries, XP, and changed equipment.

---

### 6.3 Item & Inventory

`ItemDefinitions.gd` (data):

* Item templates:

  * Type (`weapon`, `armor`, `gadget`, `consumable`).
  * Stats (damage, ROF, armor, etc.).
  * Allowed roles (optional).

`InventorySystem.gd`:

* Manage:

  * Ship inventory.
  * Crew loadouts.
  * Loot from missions.

Simplify v0.1:

* Weapon slot + armor slot + 1–2 gadget/consumable slots per character.
* Ammo and meds as pooled resources.

---

### 6.4 DataStore / Content Loading

`DataStore.gd`:

* Single entry point to load static data from JSON or Godot Resources.

Likely structure:

```csharp
class_name DataStore

var factions: Dictionary
var jobs: Dictionary
var items: Dictionary
var enemies: Dictionary
var abilities: Dictionary
var maps: Dictionary

func load_all():
    # Load JSON/Resources from `assets/data/`
```

Simulation systems depend on `DataStore` instead of reading JSON directly.

---

### 6.5 Save / Load

`SaveManager.gd` under `src/core/`:

* Responsible for serializing/deserializing `CampaignState`.

Guidelines:

* Store data as JSON or Godot’s `FileAccess` format.
* Keep save schema close to `CampaignState` structure:

  * Sector, nodes.
  * Factions & rep.
  * Ship, crew, resources.
  * Jobs and mission history.

Mission-time saves **not required for v0.1** (only between missions).

---

## 7. Game Flow & Global State

### 7.1 GameState singleton

`GameState.gd` (autoload):

Fields:

* `campaign: CampaignState` (nullable if not started).
* `mode: String` (e.g., `"menu"`, `"sector"`, `"mission"`, `"debrief"`).
* `current_mission: CombatState` (nullable).
* `current_seed`, difficulty, settings, etc.

Methods:

* `start_new_campaign(seed, difficulty)`
* `enter_sector_view()`
* `start_mission(job_id)`
* `complete_mission(result)`
* `save_campaign(slot)`
* `load_campaign(slot)`

### 7.2 FlowController

`FlowController.gd`:

* Controls scene transitions.
* Listens to `GameState.mode` changes and loads the appropriate scene:

  * `MainMenu.tscn`
  * `SectorView.tscn`
  * `MissionView.tscn`
  * `DebriefView.tscn`

---

## 8. Presentation & UI

### 8.1 Scenes

Examples:

* `scenes/main_menu/MainMenu.tscn`
* `scenes/sector/SectorView.tscn`
* `scenes/mission/MissionView.tscn`
* `scenes/debrief/DebriefView.tscn`

Each scene has a controller script that:

* Reads data from `GameState`.
* Renders it via UI nodes.
* Calls high-level functions on `GameState` / systems when player acts.

### 8.2 Mission UI

`MissionView.tscn`:

* Subnodes:

  * Tilemap / map renderer.
  * Actor views (sprites).
  * HUD:

    * Crew list with HP bars and abilities.
    * Resource display (ammo, meds).
    * Pause/Play/Speed controls.
    * Combat log (optional v0.1).
* Script responsibilities:

  * Map sim → visual:

    * Subscribe to changes in `CombatState`.
  * Input → sim:

    * Selection, movement, ability targeting.

Pattern:

* One-way data flow as much as possible:

  * **Sim** → state snapshot → **UI renders**.
  * **UI** → commands → **sim mutates**.

---

## 9. Devtools & Testing (Integration Points)

(Strategy details live in `DEVTOOLS_TESTING.md`, this section is about architecture hooks.)

### 9.1 Deterministic RNG

* Use a small RNG wrapper:

`Rng.gd`:

* Holds a seed and provides:

  * `randf()`, `randi()`, etc.
* `CampaignState` and `CombatState` own their own RNG instances (or seeded contexts).
* For tests:

  * Pass known seeds to get reproducible behavior.

### 9.2 Test harness entry points

* Functions like:

  * `CombatState.run_test_scenario(config, seed) -> CombatSummary`
  * `CampaignState.run_auto_campaign(steps, seed) -> CampaignSummary`

These can be run:

* From simple Godot scripts.
* Or from unit tests to check difficulty/economy sanity.

### 9.3 Debug overlays

* Debug modes are driven by a global flag in `Config.gd` or similar.
* MissionView can show:

  * Paths.
  * Hit chance info.
  * AI targets.

---

## 10. v0.1 vs Later

**v0.1 must have**

* `CampaignState`, `Sector`, `FactionSystem`, `JobSystem`, `TravelSystem`, `EconomySystem`, `ShipState`.
* `CombatState`, `TimeSystem`, `MapState`, `Actor`, `CombatResolver`, minimal `AbilitySystem`, `AIController`, `InteractableSystem` (basic doors).
* `CrewMember`, `ItemDefinitions`, `InventorySystem`.
* `DataStore`.
* `GameState`, `FlowController`, `SaveManager`.
* Sector view, mission view, debrief scenes.
* Deterministic RNG wrapper and minimal test harness.

**Explicitly later**

* Deep ship systems (power, air, room-level simulation).
* Rich relationship system.
* Sector-wide escalation logic.
* Modding pipeline.
* Mid-mission saves.

---

This document should be updated when:

* You add or remove a major system or folder.
* You significantly change how the sim is separated from presentation.
* You introduce new core entities (`CampaignState`, `CombatState`, etc.) or alter their responsibilities.

Keep changes **incremental and specific** so this remains a reliable map of the codebase.

```
```
