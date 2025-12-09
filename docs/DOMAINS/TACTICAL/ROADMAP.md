# TACTICAL_ROADMAP.md – Tactical Core Implementation Plan

This document defines the **implementation order** for the tactical (on-foot) layer.

- It is **mission-first**: features are justified by how they make the flagship mission better.
- Each milestone is a **vertical slice**: after reaching it, you have a coherent state where you can safely pause tactical work.
- The **Hangar Handover** mission is the design lab for all tactical work until G3.

See `docs/ITERATIONS/2025-12-hangar-handover.md` for the full mission concept and feature clusters.

---

## Development Philosophy

**Mission-first, feature-driven:** Instead of designing abstract systems in a vacuum, we derive all feature work from the Hangar Handover mission. This replaces the short-lived "G2.5" direction with a more grounded approach.

**Tactical Identity – Four Axes:**
- **Information** – who sees what, when
- **Position** – where you stand and what you stand behind
- **Time** – pressure and tempo
- **Value Extraction** – what you walk away with

Every tactical feature should meaningfully touch at least two of these axes.

---

## Overview of Milestones

### Foundation (Complete)
1. **M0 – Tactical Skeleton & Greybox** ✅
2. **M1 – Multi-Unit Control & Group Movement** ✅
3. **M2 – Visibility & Fog of War** ✅
4. **M3 – Basic Combat Loop (No Cover)** ✅
5. **M4 – Directional Cover & Lethality Tuning** ✅
6. **M5 – Interactables & Channeled Hacking** ✅
7. **M6 – Stealth & Alarm Foundations** ✅
8. **M7 – Session I/O & Campaign Integration** ✅

### Current Focus: Hangar Handover Slice
9. **HH1 – Overwatch & Reaction Fire** ← NEW
10. **HH2 – Suppression System** ← NEW
11. **HH3 – Wave Spawning & Mission Phases** ← NEW
12. **HH4 – Retreat & Value Extraction** ← NEW
13. **HH5 – AI Roles & Behaviour** ← NEW
14. **HH6 – UX & Combat Feel** ← NEW

### Post-Hangar (G3 Prep)
15. **M8 – Additional Mission Types**

Each milestone builds on the previous one. The HH (Hangar Handover) milestones are specifically designed to deliver the flagship mission.

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

**Status:** ✅ Complete (post-M3 cleanup done)

---

## M4 – Directional Cover & Lethality Tuning

**Goal:**  
Introduce the cover game so combat matches the intended “lethal but positional” fantasy.

**Key capabilities:**

- Cover data on map tiles/entities:
  - 8-direction cover values (per cell or per object).
  - Cover heights (low/half/high) with scaled hit reduction.
- Combat resolution extended to consider cover:
  - Determine if the target is in cover relative to shooter direction.
  - Apply hit modifiers based on cover height (15%/30%/45% reduction).
- Very basic cover feedback in UI:
  - Enough to understand "this tile gives cover vs that direction".
  - Visual distinction between cover heights (color-coded).
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

**Status:** ✅ Complete (M4.1 cover heights added, post-M4 cleanup done)

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

## HH1 – Overwatch & Reaction Fire

**Goal:**  
Make movement across open ground risky. Let defenders set up threat zones that attackers must respect.

**Tactical axes:** Information + Position + Time

**Implementation Plan:** See `HH1_IMPLEMENTATION.md` for detailed breakdown.

**Key capabilities:**

- Overwatch action:
  - Unit spends attack to enter overwatch state.
  - While in overwatch, if an enemy moves within LOS and range, a reaction shot triggers.
  - Overwatch ends after one reaction (initially).
- Threat zone visualization:
  - Show which units are on overwatch.
  - Visualize potential threat area (cone/range highlight).
- AI usage:
  - Enemy AI can use overwatch to hold positions instead of rushing.

**Dependencies:**

- Combat loop (M3).
- Visibility (M2).

**Why here:**  
Overwatch is foundational for the Hangar Handover's "contact phase" where pre-positioned enemies create threat zones the player must navigate.

**Natural pause point:**  
Combat now has a defensive layer. Players must think about movement exposure, not just cover.

---

## HH2 – Suppression System

**Goal:**  
Create non-lethal pressure tool that shapes behaviour and enables manoeuvre.

**Tactical axes:** Position + Time

**Implementation Plan:** See `HH2_IMPLEMENTATION.md` for detailed breakdown.

**Key capabilities:**

- Suppressive fire action:
  - Special action or weapon mode.
  - Less damage than normal attacks.
  - Higher chance to apply Suppressed status (even on near-miss).
- Suppressed status effects:
  - Reduced accuracy.
  - Reduced movement effectiveness.
  - Possibly disables or degrades overwatch.
- Visual feedback:
  - Clear status icon for suppressed units.

**Dependencies:**

- Combat loop (M3).
- Status effect system.

**Why here:**  
Suppression enables the "pin and flank" tactics central to the Hangar Handover's pressure phase.

**Natural pause point:**  
Combat is no longer just "who kills first" but also "who locks whom down".

---

## HH3 – Wave Spawning & Mission Phases

**Goal:**  
Create escalating pressure over time via reinforcement waves and explicit mission phases.

**Tactical axes:** Time + Value Extraction

**Implementation Plan:** See `HH3_IMPLEMENTATION.md` for detailed breakdown.

**Key capabilities:**

- Spawn closets:
  - Defined spawn points on the map (side doors, airlocks, galleries).
  - Waves triggered by time or events (e.g., boss reaches low HP).
- Mission phases:
  - Phase 0: Setup (deployment zone).
  - Phase 1: Negotiation (non-combat, narrative wrapper).
  - Phase 2: Contact (combat starts, initial enemies).
  - Phase 3: Pressure (waves arrive).
  - Phase 4: Resolution (push or retreat).
- Phase transitions:
  - Clear triggers and UI feedback when phase changes.

**Dependencies:**

- Basic enemy spawning.
- Mission state tracking.

**Why here:**  
Waves are the primary pressure source in the Hangar Handover. Without them, combat is static.

**Natural pause point:**  
Missions now have temporal structure. Staying longer = more risk.

---

## HH4 – Retreat & Value Extraction

**Goal:**  
Make retreat a designed outcome with clear tradeoffs, not a failure state.

**Tactical axes:** Time + Value Extraction

**Implementation Plan:** See `HH4_IMPLEMENTATION.md` for detailed breakdown.

**Key capabilities:**

- Retreat mechanics:
  - Evac zone (entry point or designated area).
  - Units reaching evac zone are extracted.
  - Mission can end with partial extraction.
- Mission outcomes:
  - Success: survive and extract with at least one crew member.
  - Higher success: extract with more crew and more objectives.
- Value extraction:
  - Loot pickups on enemies or in containers.
  - Optional objectives (hack terminal, secure crate).
  - Clear UI showing what's been collected vs what's at risk.
- Mission summary:
  - Post-mission screen showing outcomes and tradeoffs.

**Dependencies:**

- Mission phases (HH3).
- Objective system.

**Why here:**  
Value extraction is a core tactical axis. The Hangar Handover must demonstrate "how greedy are you?" as a recurring tension.

**Natural pause point:**  
Missions now have meaningful endings beyond "kill everyone" or "everyone dies".

---

## HH5 – AI Roles & Behaviour

**Goal:**  
Make enemy AI readable, distinct by role, and reactive to the tactical situation.

**Tactical axes:** Information + Position

**Implementation Plan:** See `HH5_IMPLEMENTATION.md` for detailed breakdown.

**Key capabilities:**

- Enemy roles:
  - **Guards/Sentries:** Hold key cover positions, use overwatch, fall back when pressured.
  - **Flankers/Hunters:** Move around edges, exploit suppression to advance.
  - **Support/Officers:** Coordinate waves, use abilities, target clusters.
- Behaviour patterns:
  - Prefer moving into cover relative to known threats.
  - Consider retreating when badly hurt or heavily suppressed.
  - Use grenades/hazards when player clumps behind cover.
  - Change behaviour between phases.

**Dependencies:**

- Overwatch (HH1).
- Suppression (HH2).
- Wave system (HH3).

**Why here:**  
The Hangar Handover needs AI that creates interesting tactical problems, not just targets to shoot.

**Natural pause point:**  
Combat feels like fighting opponents with plans, not just reaction scripts.

---

## HH6 – UX & Combat Feel

**Goal:**  
Make combat state clear and satisfying without complex animations.

**Tactical axes:** Information (all feedback is information)

**Implementation Plan:** See `HH6_IMPLEMENTATION.md` for detailed breakdown.

**Key capabilities:**

- Overwatch indicators:
  - Show which units are on overwatch.
  - Visualize threat area (cone/range).
- Cover indicators:
  - Per-unit icon showing cover quality vs main threats.
- Status effect icons:
  - Clear icons for suppression, stun, etc.
- Basic combat animations:
  - Units briefly aim/turn towards targets when firing.
  - Simple muzzle flash or recoil motion.
  - Basic hit/miss feedback (impact flashes, small knockback).
- Combat log:
  - Readable log of what happened and why.

**Dependencies:**

- All HH milestones.

**Why here:**  
Without good feedback, even great mechanics feel opaque. This is the polish pass for the vertical slice.

**Natural pause point:**  
The Hangar Handover is playable and readable. Ready for external playtesting.

---

## M7 – Session I/O & Campaign Integration

**Goal:**  
Make the tactical layer cleanly pluggable into the wider campaign.

**Key capabilities:**

- Session input contract:
  - What the tactical layer needs at start:
    - Map,
    - Entry zone,
    - Player units (positions, stats, equipment),
    - Initial enemies and mission metadata,
    - Wave definitions and spawn points.
- Session output contract:
  - What the tactical layer returns on end:
    - Surviving units and their final state (HP, ammo, etc.),
    - Units that died,
    - Objective status flags,
    - Loot collected,
    - Alarm outcome and mission-relevant state.
- Campaign integration:
  - Apply mission results to campaign state.
  - Handle crew injuries, deaths, rewards.

**Dependencies:**

- All HH milestones complete.
- Campaign state system.

**Why here:**  
The Hangar Handover proves the tactical layer works. Now we wire it into the campaign properly.

**Natural pause point:**  
Tactical missions are fully integrated. Ready to expand content.

---

## M8 – Additional Mission Types

**Goal:**  
Apply the Hangar Handover's combat foundations to additional mission types.

**Key capabilities:**

- Additional layouts:
  - Different room configurations, cover patterns, spawn points.
- Mission type variations:
  - Extraction missions (get in, grab target, get out).
  - Defense missions (hold position against waves).
  - Stealth-optional missions (quiet approach, loud fallback).
- Reusable components:
  - Wave system works across mission types.
  - Value extraction pattern applies universally.
  - AI roles adapt to different contexts.

**Dependencies:**

- Campaign integration (M7).
- Hangar Handover as reference implementation.

**Why here:**  
With one mission type proven, we can confidently expand. The patterns established in HH1-HH6 should transfer cleanly.

**Natural pause point:**  
Multiple mission types are playable. Ready for G3 scope expansion.

---

## Milestone Summary

| Milestone | Focus | Status |
|-----------|-------|--------|
| M0 | Tactical skeleton | ✅ Complete |
| M1 | Multi-unit control | ✅ Complete |
| M2 | Visibility & fog | ✅ Complete |
| M3 | Basic combat | ✅ Complete |
| M4 | Cover system | ✅ Complete |
| M5 | Interactables | In Progress |
| M6 | Stealth & alarm | Pending |
| HH1 | Overwatch | Pending |
| HH2 | Suppression | Pending |
| HH3 | Waves & phases | Pending |
| HH4 | Retreat & value | Pending |
| HH5 | AI roles | Pending |
| HH6 | UX & feel | Pending |
| M7 | Campaign integration | Pending |
| M8 | Additional missions | Pending |
