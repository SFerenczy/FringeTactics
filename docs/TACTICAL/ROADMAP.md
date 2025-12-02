# TACTICAL_ROADMAP.md – Tactical Core Implementation Plan

This document defines the **implementation order** for the tactical (on-foot) layer.

- It only concerns **CORE** features from `TACTICAL_DESIGN` and `TACTICAL_TECH`.
- It is **not** a sprint plan or task list.
- Each milestone is a **vertical slice**: after reaching it, you have a coherent state where you can safely pause tactical work and focus on other systems if needed.

---

## Overview of Milestones

1. **M0 – Tactical Skeleton & Greybox**
2. **M1 – Multi-Unit Control & Group Movement**
3. **M2 – Visibility & Fog of War**
4. **M3 – Basic Combat Loop (No Cover)**
5. **M4 – Directional Cover & Lethality Tuning**
6. **M5 – Interactables & Channeled Hacking**
7. **M6 – Stealth & Alarm Foundations**
8. **M7 – Session I/O & Retreat Integration**
9. **M8 – UX & Feel Pass for Tactical v0**

Each milestone builds on the previous one. It’s acceptable to interleave content/UX work between them, but the order of the core systems should generally follow this sequence.

---

## M0 – Tactical Skeleton & Greybox

**Goal:**  
Have a running tactical session with a deterministic loop and a dummy map. No combat or enemies yet.

**Key capabilities:**

- Fixed-step tactical simulation loop (with pause/unpause).
- Load a simple 2D grid map:
  - Walkable vs blocked tiles.
  - Defined entry zone.
- Spawn a single controllable unit:
  - Click-to-move on the grid.
  - Camera follows or stays focused sensibly.

**Why first:**  
Everything else depends on a stable simulation loop and basic map+unit representation.

**Natural pause point:**  
You can already use this to test very early UI, camera behavior, and technical scaffolding.

---

## M1 – Multi-Unit Control & Group Movement

**Goal:**  
Control a small squad instead of a single unit. Still no combat.

**Key capabilities:**

- Multiple player-controlled units in the session.
- Selection:
  - Single selection.
  - Box/multi-selection.
- Group orders:
  - Move commands apply to all selected units (override current actions).
- Basic separation/spacing so units don’t all stack exactly on one point (even if crude).

**Dependencies:**  
Builds directly on M0 simulation + map/navigation.

**Why here:**  
This defines how the player actually interacts with a squad and sets up the group-first control philosophy.

**Natural pause point:**  
You can explore UX patterns for selection, grouping, and order feedback before combat complexity enters the picture.

---

## M2 – Visibility & Fog of War

**Goal:**  
Add the information layer (what the player knows) without yet worrying about combat.

**Key capabilities:**

- Line-of-sight (LOS) based on the grid:
  - LOS blocked by walls/closed doors.
- Per-unit vision radius.
- Fog-of-war states per cell:
  - Unknown, Seen (but not currently visible), Visible.
- Visual representation of fog and visibility.

**Dependencies:**

- Units moving on the grid (M0–M1).
- Basic map geometry.

**Why here:**  
Fog-of-war and LOS are foundational for both combat and stealth. Implementing them before shooting avoids hacks later.

**Natural pause point:**  
At this stage, simply moving a squad around a fogged interior map is already an interesting exploratory “sandbox” to test feel and scale.

---

## M3 – Basic Combat Loop (No Cover Yet)

**Goal:**  
Introduce lethal interactions: shooting, health, death, and simple enemies.

**Key capabilities:**

- Unit stats:
  - HP.
  - A basic ballistic weapon.
- Combat resolution:
  - Attack commands on visible enemies.
  - Hit chance based on distance + base accuracy.
  - Damage reduces HP; 0 HP removes unit from play.
- Ammo & reload:
  - Magazine + reserve ammo per weapon.
  - Reload action with time cost.
- Auto-defend:
  - Units can automatically return fire when attacked and able.
- Simple enemy AI:
  - Enemy units that:
    - Perceive the player using the same LOS.
    - Move toward or stay in place and shoot visible targets.

**Dependencies:**

- Visibility & fog (M2).
- Multi-unit control (M1).

**Why now:**  
You want a functioning “minimal combat loop” early so tuning and feel can start while systems are still simple.

**Natural pause point:**  
You effectively have a crude RTwP combat prototype. This is a good time to test basic pacing, lethality, and UX around targeting and feedback.

---

## M4 – Directional Cover & Lethality Tuning

**Goal:**  
Introduce the cover game so combat matches the intended “lethal but positional” fantasy.

**Key capabilities:**

- Cover data on map tiles/entities:
  - 8-direction cover values (per cell or per object).
- Combat resolution extended to consider cover:
  - Determine if the target is in cover relative to shooter direction.
  - Apply hit/damage modifiers for cover.
- Very basic cover feedback in UI:
  - Enough to understand “this tile gives cover vs that direction”.
- Balance pass:
  - Exposed units die quickly,
  - Units in strong mutual cover have long standoffs.

**Dependencies:**

- Combat loop (M3).
- Grid map representation (M0).

**Why here:**  
Cover fundamentally changes the feel of combat and unlocks deeper tactical play. It’s better to add it before interactions/stealth, so tuning is based on the final intended lethality model.

**Natural pause point:**  
At this milestone, you have a small but legitimate tactics game: move, shoot, take cover, survive. You can pause tactical work here and focus on campaign scaffolding if needed.

---

## M5 – Interactables & Channeled Hacking

**Goal:**  
Add non-combat interactions to broaden mission possibilities beyond “kill everything.”

**Key capabilities:**

- Interactable framework:
  - Doors with states: closed/open/locked.
  - Terminals: used for objectives and state changes.
  - Simple environmental hazards (e.g. objects that can be triggered or detonated).
- Context-based interaction:
  - Click object → best eligible unit in the selected group performs interaction.
- Channeled actions:
  - Hacking as an action with duration and progress.
  - Clear interruption rules (new command, movement, possibly taking damage).
- Doors integrated into:
  - Pathfinding (walkable vs not).
  - LOS (blocking vs not).

**Dependencies:**

- Stable movement (M1).
- LOS & fog (M2).
- Basic action system (M3).

**Why here:**  
Interactions and channeled actions are required for non-combat objectives and “tension without bullets”. They also set up many future mission types.

**Natural pause point:**  
You can create internal scenarios like “escort hacker to console”, “open locked path while enemies approach”, or “trigger hazard at the right time”, even without advanced stealth.

---

## M6 – Stealth & Alarm Foundations

**Goal:**  
Minimal stealth and alarm system, compatible with later complex stealth/AI work.

**Key capabilities:**

- Perception:
  - Enemies use LOS and vision radius to detect player units.
- Detection states:
  - Idle → Alerted when a player unit is visible.
- Alarm flag:
  - Global or area alarm toggled when first enemy becomes alerted.
- Auto-pause:
  - Trigger auto-pause when:
    - First enemy becomes alerted.
    - Alarm transitions from quiet to alerted.

**Dependencies:**

- Visibility (M2).
- Enemy AI (M3).
- Interaction/doors (M5) for meaningful quiet navigation.

**Why here:**  
This implements the **unified sim** for both quiet and loud missions: same rules, with an explicit “go loud” moment. It’s the baseline on which richer stealth behavior can be layered later.

**Natural pause point:**  
At this milestone, you can prototype missions that start covert and can go loud dynamically. This is a good time to let mission designers play and refine requirements for deeper stealth.

---

## M7 – Tactical Session I/O & Retreat

**Goal:**  
Make the tactical layer cleanly pluggable into the wider game.

**Key capabilities:**

- Session input contract:
  - What the tactical layer needs at start:
    - Map,
    - Entry zone,
    - Player units (positions, stats, equipment),
    - Optional initial enemies and mission metadata.
- Session output contract:
  - What the tactical layer returns on end:
    - Surviving units and their final state (HP, ammo, etc.),
    - Units that died,
    - Basic objective status flags,
    - Alarm outcome and other mission-relevant state.
- Retreat handling:
  - Defined mechanism
