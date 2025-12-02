# TACTICAL_TECH.md – Tactical Layer Architecture (RTwP On-Foot)

This document describes the **technical architecture** for the tactical (on-foot) layer.

It complements `TACTICAL_DESIGN.md`:

- `TACTICAL_DESIGN.md` = what must happen from the player’s perspective.  
- `TACTICAL_TECH.md` = how we structure systems and data so that can happen, and how we keep it extendable.

As in the design doc, each point is tagged:

- **[CORE]** – Required for first usable tactical implementation.  
- **[PLUS]** – Strongly desired later, but not required for v0.1.  
- **[DREAM]** – Long-term direction, informs how we choose abstractions now.

---

## 0. Goals & Constraints

### 0.1 Goals

**[CORE]**

- Provide a **deterministic, debuggable tactical simulation** that supports:
  - Real-time with pause,
  - 5–10 controllable units,
  - High lethality, directional cover, fog of war, finite ammo,
  - Context-based interactions (doors, terminals, hazards).

- Keep a **clean separation** between:
  - Tactical simulation state (“the truth”),
  - UI/controls (commands, highlighting, overlays),
  - Campaign/mission logic.

**[PLUS]**

- Support replay and debug tooling (step-through, time scrubbing).

**[DREAM]**

- Network-ready deterministic sim (co-op / spectators).

### 0.2 Non-Goals (Technical)

**[CORE]**

- No need to support full 3D physics or multi-floor navigation.  
- No need to support large outdoor maps or hundreds of units per side.  
- No need for fully dynamic destructible geometry (structural damage simulation).

---

## 1. High-Level Architecture

### 1.1 Core Concepts

**[CORE]**

- **Tactical Session**
  - Encapsulates one mission instance of the tactical layer.
  - Owns:
    - Simulation loop control (tick, pause, auto-pause),
    - Reference to world/map data,
    - Collection of entities (units, projectiles, interactables),
    - Hooks to campaign/mission logic (entry/exit, results).

- **Simulation Loop**
  - Fixed time step simulation (e.g. fixed “ticks” per second) to keep combat, movement, and AI consistent and deterministic.
  - Input is read and converted to **commands**, which are then processed into **actions** within the simulation.

- **Systems**
  - Decompose behavior into systems that operate on subsets of state:
    - Time & scheduling system,
    - Movement/pathfinding system,
    - Visibility/fog-of-war system,
    - Combat & damage system,
    - AI system,
    - Interaction/ability system, etc.
  - Systems should be order-controlled and as stateless as possible beyond their data.

- **Entities & Components**
  - The simulation is easiest to maintain with an entity-centric view:
    - Units, doors, terminals, hazards, projectiles, etc. are entities.
    - Properties like position, health, inventory, cover, vision, etc. are components or equivalent data structures associated to entities.

**[PLUS]**

- A lightweight **event bus** or messaging layer for decoupling:
  - “Unit died”, “Door opened”, “Alarm raised”, “Objective completed”.

**[DREAM]**

- Runtime-modifiable systems (scripting, modding) plugged into the same entity/system architecture.

---

## 2. Time & Simulation Control

### 2.1 Fixed Step Simulation

**[CORE]**

- Use a **fixed tick rate** for the tactical sim.
  - Rendering can interpolate; logic uses discrete ticks.
- Simulation state only changes on ticks, not on render frames.

- Pausing:
  - Pausing stops advancing ticks.
  - Order/command queues can still be updated by UI, but actions only apply when ticking resumes.

### 2.2 Auto-Pause

**[CORE]**

- Auto-pause is implemented as:
  - A set of **conditions checked per tick** (enemy enters vision, alarm state changes).
  - When triggered, the tactical session sets itself to paused before processing further orders that depend on the new information.

**[PLUS]**

- A configurable “auto-pause profile”:
  - Player-configurable events to pause on (e.g. downed ally, low health, new objective).

---

## 3. Tactical State Model

### 3.1 State Partitioning

**[CORE]**

- **World/Map State**
  - Static and semi-static data:
    - Tiles/cells with geometry flags (walkable, block LOS, cover directions),
    - Doors, terminals, hazards,
    - Named zones (entry zone, evacuation zone, objective areas).

- **Actor/Unit State**
  - Per-unit:
    - Position, orientation,
    - Health/HP,
    - Weapon state (ammo, reload),
    - Visibility model (who they see, who sees them),
    - Current order/action,
    - AI state (for enemies).

- **Mission/Tactical Meta State**
  - Alarm level (quiet / alerted),
  - Objective status (per objective: pending, in-progress, complete, failed),
  - Retreat status (aborting, evac-in-progress, complete).

**[PLUS]**

- **Debug State**
  - Logs for recent events, LOS queries, pathfinding decisions.

**[DREAM]**

- State snapshots for replays, time-travel debugging.

---

## 4. Space, Maps, and Generation

### 4.1 Spatial Representation

**[CORE]**

- Use a **2D grid** as the primary spatial representation:
  - Each cell knows:
    - Walkable or blocked,
    - LOS-blocking or not,
    - Cover directions provided (up to 8-directional),
    - References to objects occupying it (door, hazard).
- World coordinates may be continuous, but **gameplay checks** (movement, cover, LOS) use the grid.

### 4.2 Map Assets & Authoring

**[CORE]**

- Maps are data assets containing:
  - Tile layout and properties,
  - Entities placed at positions (doors, terminals, initial enemies/units, hazards),
  - Metadata:
    - Named zones (entry, evac),
    - Logical rooms or areas (useful for AI and fog-of-war).

- This version assumes maps are **hand-authored** (using an editor or external tools), then imported into the game as data.

### 4.3 Procedural Map Generation

**[PLUS]**

- Tactical layer should not assume maps are hand-authored forever.
- Map format should be **agnostic** to origin (manual vs procedural).
- A generator would:
  - Assemble rooms/corridors based on templates and constraints,
  - Place doors, terminals, hazards according to generation rules,
  - Label zones (entry, chokepoints, objective zones).

- Technical considerations:
  - If procedural generation is added:
    - It should output maps in the same structure used by the tactical session.
    - Tactical systems must not depend on “editor-only” metadata.

**[DREAM]**

- Multi-stage generators that:
  - Integrate with campaign state (damage, previous missions, faction control),
  - Support partially persistent maps (station you revisit is modified by previous operations).

---

## 5. Pathfinding & Movement

### 5.1 Movement Graph

**[CORE]**

- Use the grid as the pathfinding graph:
  - Adjacent walkable cells define neighbor relationships.
- Doors and dynamic blockers:
  - Closed/locked doors are treated as non-walkable until opened.
  - Opening a door changes its walkability; pathfinding must respect dynamic state.

### 5.2 Pathfinding Strategy

**[CORE]**

- Use standard shortest-path algorithms (A*, variants) for pathfinding requests.
- Consider a **centralized movement/pathfinding service**:
  - Queues requests to avoid spikes,
  - Caches recent paths for small adjustments (e.g. repeated movement in same corridor).

**[PLUS]**

- Path cost weighting:
  - Avoid tiles in known enemy LOS,
  - Prefer tiles with better cover.

**[DREAM]**

- Hierarchical pathfinding or navigation meshes for very large maps.

---

## 6. Visibility & Fog of War

### 6.1 LOS & Visibility Queries

**[CORE]**

- Implement LOS on the grid:
  - Per-unit, per-tick LOS checks against LOS-blocking cells.
  - Cache results where possible:
    - Only recompute LOS when a unit moves or when geometry state changes (doors open/close).

- Each unit’s visible set contributes to:
  - Who they can target,
  - What is revealed in fog-of-war.

### 6.2 Fog-of-War Representation

**[CORE]**

- Maintain a per-cell fog state:
  - Unknown (never seen),
  - Seen but not currently visible,
  - Currently visible.

- Efficient updates:
  - On each tick, update visible cells from all player units.
  - Changes propagate to UI as events, so UI isn’t polling.

**[PLUS]**

- Additional visibility layers:
  - Sensor or camera-based visibility stored separately from unit LOS.

**[DREAM]**

- Multiple vision types (thermal, etc.) with separate maps.

---

## 7. Units, Grouping, and Selection

### 7.1 Unit Representation

**[CORE]**

- Each controlled or AI unit is an entity with:
  - Position/velocity,
  - Health,
  - Weapon/inventory,
  - Team/faction identifier,
  - Vision properties,
  - AI or control mode flag.

### 7.2 Grouping Model

**[CORE]**

- Groups live **above** the simulation state:
  - The sim doesn’t have to know about “Alpha team”; it only receives commands for specific units.
  - UI or control layer maintains selections and group shortcuts.

- When a group interaction is ordered:
  - A small selection algorithm chooses a single unit to perform the actual interaction (based on distance & capability).

**[PLUS]**

- Store group identity as metadata:
  - For convenience and potential future AI coordination.

**[DREAM]**

- Group-level behaviors and state machines.

---

## 8. Orders & Action System

### 8.1 Commands vs Actions

**[CORE]**

- Distinguish between:
  - **Commands** – high-level player or AI intent (“move there”, “attack this target”, “interact with that door”).
  - **Actions** – the internal execution steps required to fulfill a command (pathing, tracking, animation, hit resolution).

- Units have:
  - A **current action**.
  - Commands override current action immediately.

### 8.2 Action Lifecycle

**[CORE]**

- Actions have a simple lifecycle:
  - Requested → Started → Running → Completed / Cancelled / Failed.
- At each tick:
  - Action systems update running actions.
  - Check for completion conditions (reach target, time elapsed, etc.).
  - On completion:
    - Apply effects (damage, state changes),
    - Notify other systems (e.g. AI, mission logic).

**[PLUS]**

- Limited action queuing:
  - Units may hold a very short next action (e.g. “open door after reaching it”).

**[DREAM]**

- Full action queue/timeline per unit and global synchronized plans.

---

## 9. Combat & Weapon Handling

### 9.1 Shot Resolution

**[CORE]**

- Combat system handles:
  - Attack declaration (source, target),
  - Hit chance calculation:
    - Takes into account:
      - Base accuracy,
      - Distance,
      - Target cover (from cover data),
      - Movement modifiers (moving vs standing).
  - RNG resolution (hit/miss),
  - Damage application (possibly with criticals).

- High lethality:
  - Parameter tuning should reflect few hits causing incapacitation/death.

### 9.2 Ammo & Reload

**[CORE]**

- Each weapon has:
  - Magazine capacity,
  - Current ammo in magazine,
  - Reserve ammo for the mission.

- Firing decrements ammo; when empty:
  - Unit must reload (reload is an action with a time cost).

### 9.3 Extensibility

**[PLUS]**

- Shot modes:
  - Single, burst, full auto with different accuracy/ammo trade-offs.

**[DREAM]**

- Per-body-part hitboxes and injury types.

---

## 10. Interactables & Hacking

### 10.1 Interactable Interface

**[CORE]**

- Interactables share a simple interface:
  - Can they be interacted with by a given unit?
  - What interaction options exist (open, hack, trigger, etc.)?
  - What state changes occur when interaction completes?

- State should be simple but explicit:
  - E.g. doors: closed / open / locked.

### 10.2 Channeled Interactions (Hacking)

**[CORE]**

- Channeled interactions are modeled as actions with:
  - A duration,
  - A requirement to stay in range and line of interaction,
  - Clear interruption rules (movement, taking certain types of damage, new command).

- On success:
  - Apply defined state changes (unlock door, complete objective, etc.).

**[PLUS]**

- Multiple-step interactions:
  - Sequence of required actions for a complex hack.

**[DREAM]**

- Pluggable “mini-systems” for subsystems (power, security) that interactables talk to.

---

## 11. Stealth & Perception

### 11.1 Core Perception

**[CORE]**

- Enemy perception:
  - Based on LOS and a vision range.
  - If a player unit is visible, they are considered detected.

- Alarm model:
  - Simple global or area-based state:
    - Quiet vs Alerted.
  - Alarm state change triggers:
    - Auto-pause on the player side (as per design),
    - AI switching into combat behavior.

### 11.2 Extensions

**[PLUS]**

- View cones:
  - Directional vision fields for enemies.
- Hearing:
  - Noise events (gunfire, explosions) emitted at positions with a radius of effect.
  - AI hears events and can move to investigate.

**[DREAM]**

- More detailed AI knowledge model:
  - Last known positions,
  - Shared information between enemies,
  - Investigation and search patterns.

---

## 12. AI Architecture

### 12.1 Baseline AI

**[CORE]**

- A simple **state machine** per AI-controlled unit:
  - States like: Idle, Patrol (optional), Alerted, In-Cover, Fleeing.
- Each state uses:
  - Target selection rules,
  - Basic movement patterns (seek nearest cover, move toward threat, run away).

- Decision cycle:
  - On fixed ticks, evaluate current state, environment, and events.
  - Transition states and issue commands to the tactical sim (move/attack).

### 12.2 Extensibility

**[PLUS]**

- Structured behavior tree or utility AI for more complex enemy roles:
  - Specialized units (melee rushers, snipers, support).

**[DREAM]**

- Higher-level tactical AI:
  - Coordinates groups,
  - Plans flanks, retreats, and reinforcement requests.

---

## 13. Abilities & Resource Model

### 13.1 Ability Representation

**[CORE]**

- Abilities encapsulate:
  - Targeting rules (self, ally, enemy, ground, object),
  - Execution logic (creates an action or chain of actions),
  - Cost model (cooldown, charges).

- Implementation:
  - A central ability system interprets ability use requests and instantiates appropriate actions.

### 13.2 Cooldowns & Charges

**[CORE]**

- Each ability:
  - Tracks its cooldown timer.
  - Tracks charges (if applicable) per mission.

- Tactical sim:
  - Maintains these timers and decrements them per tick.

**[PLUS]**

- Shared charges between multiple abilities (e.g. “shared grenade pool”).

**[DREAM]**

- Global shared resource pools (team energy, morale) as extra cost dimensions.

---

## 14. Mission Integration & Results

### 14.1 Session Lifecycle

**[CORE]**

- Tactical session is created with:
  - Map data,
  - Initial unit loadout (positions, equipment, health from campaign),
  - Mission metadata (objectives, alarm thresholds, win/fail conditions).

- On mission end:
  - Tactical session outputs:
    - Surviving units and their status,
    - Used/remaining ammunition and consumables,
    - Which objectives completed or failed,
    - Any special events (critical injuries, deaths, loot/acquisitions).

- Campaign layer consumes this output to:
  - Update persistent characters,
  - Apply rewards/penalties,
  - Advance time.

### 14.2 Retreat Handling

**[CORE]**

- Tactical sim must support a “retreat resolved” state:
  - When declared and the conditions are met (units at entry zone), session ends and returns a retreat/failure result.

**[PLUS]**

- Additional metadata:
  - Items dropped/abandoned,
  - Units left behind (if any), etc.

---

## 15. Extensibility & Configuration

### 15.1 Data-Driven Parameters

**[CORE]**

- Core tactical parameters should be data-driven:
  - Weapon stats (damage, accuracy, ranges),
  - Cover bonuses,
  - Vision range,
  - Movement speeds,
  - Ability cooldowns.

- Allows:
  - Rapid balancing,
  - Faction differentiation via data, not code.

### 15.2 Modularity & Boundaries

**[CORE]**

- Keep clear module boundaries:
  - Tactical sim doesn’t directly own campaign/mission logic; it exposes events and results.
  - UI is an observer/command source, not mixed with simulation.

**[PLUS]**

- Plug-in style behavior:
  - New interactables, abilities, AI behaviors registered via data or configuration.

**[DREAM]**

- Full modding API for custom tactical behaviors and content.

---

## 16. Summary of Core vs Extensions

**[CORE]** (minimal viable tactical implementation must include):

- Fixed-step RTwP simulation with pause & auto-pause.  
- 2D grid-based map with:
  - Walkability, LOS blocking, 8-direction cover data.  
- 5–10 unit squad, override-only commands, auto-defend behavior.  
- Directional cover affecting hit resolution.  
- Fog-of-war and LOS-limited targeting.  
- Finite ballistic ammo and reload mechanics.  
- Basic AI with pursue-cover-shoot behavior and alarm state.  
- Context-based interactions (doors, terminals, simple hazards).  
- Channeled hacking as timed interactions.  
- Clear session lifecycle with input/output for the campaign.

Everything else (complex stealth, view cones, non-lethal, procedural maps, extended AI, injuries, deep systemic station simulation) is **[PLUS]/[DREAM]** and should be layered on top of this foundation without breaking its abstractions.
