# TACTICAL_DOMAIN.md – Tactical Domain

**Dependencies**: CAMPAIGN_FOUNDATIONS.md sections 3 (Crew stats/traits), 6 (RNG), 7 (Mission I/O).

## Purpose

The Tactical domain owns the **on-foot, real-time-with-pause (RTwP) simulation** for missions.

It is responsible for running a single tactical mission instance end-to-end:

- Evolving the **tactical state** (units, map, visibility, alarms, objectives) in fixed time steps.
- Applying **player commands** and AI decisions.
- Resolving **movement, combat, stealth, interactions, and hazards**.
- Producing a **mission result** that the campaign layer can consume.

It does **not** decide which missions exist, why they happen, or what their campaign consequences are. It executes a mission specification and reports what happened.

This document describes the Tactical domain’s role in the overall architecture and how it interacts with other domains. Detailed player-facing design and technical architecture live in:

- `docs/TACTICAL/DESIGN.md`
- `docs/TACTICAL/TECH.md`
- `docs/TACTICAL/ROADMAP.md` and `docs/TACTICAL/M*_IMPLEMENTATION.md`

---

## Responsibilities

The Tactical domain is responsible for:

- **Tactical session lifecycle**
  - Create, run, pause, and terminate a single mission instance.
  - Own the tactical simulation loop (fixed-step RTwP).
  - Maintain mission state, including objectives and alarm/stealth status.

- **Simulation loop**
  - Advance the simulation in **fixed ticks**.
  - Apply queued commands and AI decisions to the tactical state.
  - Keep outcomes deterministic given the same inputs and RNG seed.

- **Map & spatial model**
  - Represent a mission map as a 2D grid:
    - Walkable vs blocked tiles.
    - LOS blockers (walls, closed doors).
    - Cover data (directional, with heights).
    - Interactable objects (doors, terminals, hazards).
  - Provide queries for movement, line-of-sight, and cover evaluation.

- **Units, squad, and abilities**
  - Represent all tactical entities:
    - Player-controlled units, enemies, civilians, interactables, projectiles, hazards.
  - Apply movement, actions, and abilities according to rules defined in data/config.
  - Enforce group-first control semantics (group move, group interact, attack orders).

- **Information & visibility**
  - Calculate line-of-sight and **fog of war**.
  - Decide what is visible/known to the player’s side per tick.
  - Trigger auto-pause when new critical information appears (first contact, alarms, etc.).

- **Combat resolution**
  - Resolve attacks:
    - Hit chance, cover modifiers, range falloff, accuracy.
    - Damage, death, and incapacitation.
  - Track ammo usage, reloads, and basic resource consumption during combat.

- **Stealth, alarms, and AI**
  - Maintain stealth/alarm states at the tactical level.
  - Run tactical AI:
    - Perception (LOS, maybe hearing).
    - State machines (idle/suspicious/alerted).
    - Basic decision-making: move to cover, attack, investigate, raise alarm.

- **Interactions & channeled actions**
  - Execute context-based interactions:
    - Doors (open/close/lock).
    - Terminals (hacking, unlocking).
    - Hazards (traps, environmental threats).
  - Support **channeled actions** (e.g. long hacks) with progress and interruption rules.

- **Objective evaluation & mission results**
  - Track progress toward mission-specific objectives (reach X, hack Y, destroy Z, etc.).
  - Determine win/lose/retreat states when conditions are met.
  - Produce an output summary describing:
    - Surviving units and their states.
    - Used/remaining ammo and consumables at tactical granularity.
    - Objectives completed/failed.
    - Special events (deaths, critical injuries, alarms triggered, loot gained).

---

## Non-Responsibilities

The Tactical domain **explicitly does not**:

- Generate missions or maps:
  - It consumes mission/map data produced by the **Generation** domain or test configs.
- Own persistent campaign state:
  - It does not permanently store crew stats, injuries, ship upgrades, or resources.
- Decide campaign consequences:
  - It does not change faction relations, economy, or galaxy metrics directly.
  - It only emits mission results and tactical events.
- Own UI:
  - It does not manage controls, displays, or input bindings.
  - It exposes commands and state that UI can observe/control.
- Simulate galaxy/world or travel:
  - No economy, trade, sector-level security, or travel paths.
- Handle non-tactical encounters:
  - Dialogue-heavy or text-only events live in the **Encounter** domain, though an encounter may choose to start a tactical session.

These boundaries are important: Tactical is **“local physics”** for on-foot missions, not the strategic game.

---

## Inputs

The Tactical domain consumes:

- **Mission specification** (from Generation / Campaign):
  - Map layout and config:
    - Grid size, tile types, walls, cover, interactables.
  - Initial unit placements:
    - Player squad positions, enemy spawns, civilians.
  - Tactical parameters:
    - Difficulty modifiers, alarm thresholds, timers (if any).
  - Objective definitions:
    - Primary/secondary objectives, win/fail conditions.

- **Snapshot of persistent state** (from Management / Campaign) (see CAMPAIGN_FOUNDATIONS.md 3.1, 7.1):
  - Player squad composition:
    - Which crew members are present in this mission.
  - Tactical-relevant stats:
    - Health, skills, traits that affect on-foot behaviour (per campaign foundations).
  - Equipment and consumables:
    - Weapon loadouts, armor, gadgets, medkits, ammo counts.

- **Context from other domains** (read-only at runtime):
  - World / Faction flavour tags:
    - E.g. “corporate core station”, “raider outpost”, “military ship”.
  - Global difficulty / scaling settings (from Simulation / Progression).
  - Optional narrative flags that may alter mission scripting.

- **Runtime commands and control input**:
  - Player-issued commands:
    - Move, attack, interact, ability use, retreat.
  - Debug/test commands (dev tools).
  - Time control: pause/unpause, maybe step-through in dev mode.

- **Systems Foundation services**:
  - Time system for tick progression.
  - RNG streams for combat rolls, perception, etc.
  - Event bus for publishing tactical events.

---

## Outputs

The Tactical domain produces:

- **Mission result** (for Campaign/Management) (see CAMPAIGN_FOUNDATIONS.md 7.2):
  - Per-unit outcomes:
    - Alive/dead, health, status effects, immediate injuries.
  - Per-item outcomes:
    - Consumed ammo/consumables, items dropped or acquired (loot).
  - Objective outcomes:
    - Which objectives completed/failed/ignored.
  - Alarm/stealth summary:
    - Whether/when alarms were triggered, how loud the mission was.
  - Optional per-mission flags:
    - "Notorious incident", "silent success", "hostages killed", etc.

- **Tactical events** (via event bus):
  - “UnitDied”, “UnitDowned”, “AlarmRaised”, “DoorOpened”, “TerminalHacked”, “ObjectiveCompleted”, etc.
  - These can be observed by:
    - Campaign logic.
    - Debug tools.
    - Future systems that want to react (e.g. achievements).

- **Runtime tactical state for UI**:
  - Read-only views of:
    - Map grid, cover, LOS/FoW.
    - Unit positions, orders, and statuses.
    - Current objectives and their progress.
    - Pause state and auto-pause reasons.

- **Debug/diagnostic signals** (in dev builds):
  - Logs for hit chance components, LOS checks, cover decisions.
  - Simulation snapshots for replay tools.

---

## Key Concepts & Data

At the domain level, key abstractions are:

- **TacticalSession**
  - Encapsulates one mission instance.
  - Owns:
    - Simulation loop control.
    - Mission configuration and state.
    - Collections of units, interactables, projectiles, and tactical systems.
  - Interface:
    - `Step(dt)`, `IssueCommand(cmd)`, `GetView()`, `GetResultIfFinished()`.

- **MapState**
  - Grid-based representation of the environment:
    - Tile walkability.
    - LOS blockers.
    - Cover directions and heights.
    - Interactable placements.
  - Provides queries for movement, LOS, and cover evaluation.

- **Actor / Unit**
  - Represents any active entity:
    - Player-controlled crew, enemies, civilians, drones, etc.
  - Holds:
    - Tactical stats (HP, speed, accuracy, etc.).
    - State (idle, moving, attacking, channeling, downed).
    - Links to identity in campaign (e.g. crew ID) without owning that campaign state.

- **Systems (in the ECS-ish sense)**
  - Movement/pathfinding system.
  - Visibility/FoW system.
  - Attack/Combat system.
  - AI system.
  - Interaction/Ability system.
  - Alarm/stealth system.
  - Each operates over shared state each tick in a defined order.

- **Objective & Mission State**
  - Encodes tactical-level win/fail conditions and progress.
  - Computes when the session should end and with what result.

### Invariants

- Simulation is deterministic given:
  - Same initial tactical state.
  - Same mission specification.
  - Same sequence of commands and RNG seed.
- Tactical state changes **only** on ticks (fixed-step), not render frames.
- No tactical entity references non-existent map tiles or units.
- Mission result is self-consistent:
  - Every unit and item referenced in the result existed in the session.
  - Objective states sum to a coherent outcome.

---

## Interaction With Other Domains

### World

- Reads:
  - Context tags for mission location (“station type”, “ownership”, etc.) to parameterize flavour and some tactical options.
- Writes:
  - Nothing directly; any persistent environmental changes (e.g. “station sabotage”) should be encoded as mission result flags that the campaign/simulation interpret.

### Simulation (Galaxy/Economy/Factions)

- Reads (optionally):
  - Difficulty or global modifiers that influence tactical parameters (e.g. elite enemy loadouts).
- Writes:
  - Tactical emits mission result and events.
  - Simulation interprets those via the campaign layer to adjust macro metrics (security, raider activity, etc.).

### Generation

- Inputs from Generation:
  - Mission specifications, including:
    - Map config.
    - Enemy composition.
    - Objectives.
    - Special constraints.
- Outputs to Generation:
  - Minimal; typically just mission result and possibly hooks to spawn follow-up missions (handled at campaign level, not directly in Tactical).

### Management (Crew/Ship/Resources)

- Reads:
  - Crew stats, traits, and equipment at mission start.
- Writes:
  - Per-crew outcome: health changes, deaths/critical injuries.
  - Equipment usage: ammo/consumables spent, items lost/gained.
  - These are passed as structured mission results for Management to apply to persistent state.

### Travel

- Occasionally:
  - Travel may trigger a tactical mission (e.g. boarding during transit).
  - Travel consumes the mission result to decide how the travel event resolved (escaped, captured, damaged).

### Encounter

- Encounters may:
  - Initiate a tactical mission as one branch of a narrative event.
  - Use tactical result to branch future narrative (e.g. “you botched the boarding” vs “clean success”).

### Systems Foundation

- Uses:
  - Time system for fixed-step simulation.
  - RNG streams for combat, perception, and AI decisions.
  - Event bus for publishing tactical events.
  - Persistence for saving/loading in-progress sessions (if you ever support mid-mission saves).

---

## Code Mapping (Current Repository)

This is intentionally approximate and should be kept in sync when the code evolves:

- Core tactical simulation & data:
  - `src/sim/combat/*` (Actor, MapState, CombatState, systems, balance, etc.)
- Tactical presentation & input glue:
  - `src/scenes/mission/*` (MissionView, ActorView, TacticalCamera, input controller, etc.)
- Tactical test harnesses:
  - `tests/sim/combat/*`
- Tactical design & tech docs:
  - `docs/TACTICAL/DESIGN.md`
  - `docs/TACTICAL/TECH.md`
  - `docs/TACTICAL/ROADMAP.md`
  - `docs/TACTICAL/M*_IMPLEMENTATION.md`

Over time, if you introduce a `docs/DOMAINS/TACTICAL/` folder, this file should live there and link back to these more detailed documents.

---

## Implementation Notes

- Keep the **simulation core independent** of UI:
  - All UI interactions should translate into commands issued to the TacticalSession.
  - TacticalSession returns a view/state snapshot, not UI elements.

- Define a stable **mission I/O contract**:
  - A single struct or set of structs that:
    - Describe required inputs for starting a session.
    - Describe outputs on completion.
  - This contract is the main boundary between Tactical and the campaign layer.

- Be strict about **domain boundaries**:
  - No direct reads from campaign state inside combat systems.
  - No implicit singletons; simulate everything via explicit session state.

- Make parameters **data-driven**:
  - Weapon stats, cover bonuses, vision ranges, movement speeds, and ability parameters should come from definitions outside code to support balancing.

---

## Future Extensions

- Deeper integration with injuries and long-term trauma (feeding more detail into Management).
- More advanced stealth (view cones, sound propagation, AI search patterns) without changing the basic Tactical contract.
- Support for non-standard missions:
  - Defensive holds, escort missions, extraction-focused missions.
- Optional network-ready deterministic mode for future co-op or replays.

The core contract should remain stable: Tactical takes a mission specification and a snapshot of the crew, runs a deterministic RTwP simulation, and returns a structured mission result plus events for the rest of the game to interpret.
