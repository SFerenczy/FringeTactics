# TACTICAL_DESIGN.md – Tactical Layer (RTwP On-Foot)

This document specifies the **tactical (on-foot) layer** for *Fringe Tactics* (working title).  
It defines what must be true in-game from the player’s perspective.  
Technical details live in `TACTICAL_TECH.md`.

This document **supersedes earlier tactical notes** from `GAME_DESIGN.md` where they conflict.

---

## 0. Scope & Terminology

The tactical layer is the **on-foot, real-time-with-pause (RTwP)** part of the game.  
It is used for:

- Hostile boarding actions / shootouts.
- Covert infiltrations.
- Non-combat problem-solving (unlocking stations, hacking, etc.).

Ship-to-ship combat, dialogue scenes, and pure strategy decisions are **outside** this document.

### 0.1 Scope Levels

To keep scope clear, every feature is tagged:

- **[CORE]** – Must exist for the first tactical release (v0.1).
- **[PLUS]** – Strongly desired, but can ship after core loop is working.
- **[DREAM]** – Long-term direction. Not required for initial implementation.

---

## 1. Tactical Layer Purpose & Pillars

### 1.1 Purpose

**[CORE]**  
The tactical layer delivers:

- **High-lethality RTwP squad combat** in cramped sci-fi interiors.
- **Unified ruleset** for:
  - Full-on firefights,
  - Stealthy infiltrations that may or may not go loud,
  - Quiet missions with no enemies that still feel tense (time pressure, resource risk).

This is not just “combat mode”; it is the **on-foot simulation** that underpins most risky jobs.

### 1.2 Design Pillars

**[CORE]**

1. **Lethal, cover-driven combat**  
   - Exposed units die quickly.  
   - Two units in strong mutual cover can stalemate for a while.

2. **Information and positioning over raw stats**  
   - Fog of war and LOS matter.  
   - Good scouting and flanking are more important than grinding levels.

3. **Group-first control to manage complexity**  
   - Player usually commands 5–10 crew.  
   - Group orders are the default; micro is optional, not mandatory.

4. **Unified sim for “loud” and “quiet” missions**  
   - Same rules handle a back-alley handover gone wrong and a quiet station unlock.

5. **Extensible, not brittle**  
   - Designed so future systems (deeper stealth, injuries, systemic station mechanics) can plug in without rewriting the basics.

### 1.3 Tactical Identity – Four Axes

Every tactical feature should meaningfully touch at least two of these axes:

| Axis | Description | Examples |
|------|-------------|----------|
| **Information** | Who sees what, when | Fog of war, vision ranges, sensors, hacked intel, "knowing" enemy positions vs guessing |
| **Position** | Where you stand and what you stand behind | Cover, flanking, chokepoints, doors, hazards, firing lanes |
| **Time** | Pressure and tempo | Timers, waves, action economy, channelled actions, retreat windows |
| **Value Extraction** | What you walk away with | Primary objectives vs optional loot, bonus objectives, risk-reward decisions |

**Feature validation examples:**
- **Overwatch:** Information + Position + Time (threat zones that shape movement over time)
- **Suppression:** Position + Time (pinning enemies so others can move)
- **Retreat / evac:** Time + Value Extraction (how long you risk staying for more rewards)

This framework ensures consistent tactical depth even as mission "vibes" differ (tight hangar firefight vs sprawling ship heist).

---

## 2. Player Loop Inside a Mission

This section describes how a typical mission feels, from the player’s perspective.

### 2.1 High-Level Flow

**[CORE]**

1. **Insertion**
   - Crew appears at an entry zone (airlock, docking port, breach point).
   - Game is unpaused; player sees immediate surroundings under fog of war.

2. **Recon & Setup**
   - Player moves the squad or sub-groups through interior spaces.
   - Doors, terminals, and hazards can be interacted with.
   - Enemies or hazards are unknown until spotted (fog-of-war + LOS).

3. **Contact / Escalation**
   - First enemy sighting or alarm → **auto-pause**.
   - Player evaluates: engage, retreat, reposition, or attempt to keep things quiet.

4. **Objective Play**
   - Player performs mission-specific tasks: killing, hacking, reaching areas, triggering systems, etc.
   - This may involve multiple contacts and quiet segments.

5. **Resolution**
   - Mission ends when:
     - Primary objectives are complete, or
     - Player voluntarily retreats with surviving crew back to the insertion point, or
     - Squad is wiped (campaign handles consequences).

**[PLUS]**

- Multiple extraction zones with different risk/reward profiles.

**[DREAM]**

- Multi-phase missions in one map (e.g. stealth ingress, then shipboard defense, then exfil).

---

## 3. Squad & Units

### 3.1 Squad Size & Identity

**[CORE]**

- The active tactical squad is typically **5–10 units**.
- All units are discrete characters (no faceless “generic soldiers” in the core design).
- Squad size can vary per mission (e.g. small 3-person insertion vs 8-person boarding).

### 3.2 Grouping and Fireteams

**[CORE]**

- Player can:
  - Box-select multiple units.
  - Multi-select via shift-click.
- Group orders:
  - **Move** to a position.
  - **Attack** a target.
  - **Interact** with an object (door, terminal, hazard).

- For interaction orders, the game selects the **“best candidate”** among the group:
  - Must be able to reach the object.
  - Prefer units with required skills/equipment (e.g. hacking rig).
  - Prefer units closest to the object.

**[PLUS]**

- Named fireteams (Alpha/Bravo/Charlie) with hotkeys.
- Persisting fireteam assignments across missions.

**[DREAM]**

- Fireteam behavior presets (stealthy, aggressive, support).
- Fireteams executing synchronized sequences.

### 3.3 Autonomy & Rules of Engagement

**[CORE]**

- Units **do not act on their own** offensively, except:
  - They **auto-defend** by returning fire if attacked and they have LOS + weapon ready.
- They do not:
  - Advance toward enemies without orders.
  - Change cover positions without orders (beyond minimal collision avoidance).

**[PLUS]**

- Simple per-unit/party Rules of Engagement:
  - Hold fire / return fire / free fire.

**[DREAM]**

- Behavior tuning per unit:
  - Risk tolerance, aggressiveness, tendency to push or hold.

---

## 4. Time & Orders

### 4.1 Time Controls

**[CORE]**

- Real-time simulation.
- Player can **pause at any time**.
- No time acceleration modes in v0.1.

- **Auto-pause triggers**:
  - First enemy sighting.
  - Alarm state raised (e.g. station goes to red alert).

### 4.2 Orders & Responsiveness

**[CORE]**

- Orders are **override-only**:
  - Issuing a new order cancels the unit’s current action (move, shoot, interact, channel).
- Order types:
  - Move to location.
  - Attack target.
  - Interact with object.
  - Use ability (if present).
  - Change stance/weapon (when such features exist).

- Design intent:
  - Player should **never fight the interface** in critical moments.
  - Responsiveness beats automation.

**[PLUS]**

- Minimal queuing:
  - Two-step orders like “move here, then interact with this object”.

**[DREAM]**

- Full action queues per unit.
- Global synchronized planning:
  - e.g. “On my mark, breach three doors at once, throw stun grenades, start hacking.”

---

## 5. Space, Movement, Cover, Visibility

### 5.1 Environment Scope

**[CORE]**

- Tactical maps are:
  - **2D top-down**.
  - **Single-floor interiors**: ships, stations, outposts, small facility maps.
- No explicit verticality, no stacked floors.

**[PLUS]**

- Outdoor areas that behave like large “rooms” with different visuals.

**[DREAM]**

- True vertical structures, multi-floor maps, and vertical LOS rules.

### 5.2 Movement

**[CORE]**

- Units move along navigable space, respecting:
  - Walls and closed doors as blockers.
  - Other units as soft blockers (collision-avoidance, no overlap).
- Design intent:
  - Movement is readable and predictable; no surprising teleports or pathfinding “magic”.

**[PLUS]**

- Pathfinding that prefers safer routes (e.g. avoiding known enemy LOS where possible).

**[DREAM]**

- Explicit wall-hugging, peeking behavior, and advanced path tactics.

### 5.3 Cover System

**[CORE]**

- Map elements can provide **directional cover**.
- Cover is evaluated in **8 directions** (N, NE, E, SE, S, SW, W, NW).
- When a unit is adjacent to cover, that cover:
  - Provides **defensive bonuses** (to-hit reduction and/or damage reduction) from attacks coming from covered directions.
- Desired combat feel:
  - Exposed unit: dies very quickly.
  - Two units in strong cover facing each other: low hit rates, long trade, stalemate until flanked or disrupted.

**[PLUS]**

- Two cover qualities:
  - “Good” vs “Bad” cover (e.g. high vs low) with different bonuses.

**[DREAM]**

- Multi-layer cover, stances (crouch/stand), partial exposure, and more fine-grained geometry interaction.

### 5.4 Visibility & Fog of War

**[CORE]**

- **Fog of War**:
  - At mission start, unexplored areas are hidden.
  - Rooms/tiles become revealed when within LOS of a squad member.
- Unit vision:
  - Each unit has a vision radius and LOS blocked by walls and closed doors.
- Targeting:
  - Enemies must be **visible** to be targetable.
  - No “perfect knowledge” markers behind walls.

**[PLUS]**

- Basic sensor/camera integration:
  - Hacked cameras or scanned areas can reveal fog temporarily.
  - Visited areas may remain revealed even if no one is currently looking.

**[DREAM]**

- Multiple vision types:
  - Thermal, motion sensors, stealth fields, and complex sensor gameplay.

---

## 6. Combat Model

### 6.1 Lethality & Health

**[CORE]**

- Combat is **very lethal**:
  - Few hits will incapacitate or kill an exposed unit.
- Units have a single health pool for v0.1.
- On reaching 0 HP, unit is considered **dead** (removed from tactical play; campaign handles consequences).

**[PLUS]**

- “Downed” state:
  - Unit becomes incapacitated instead of instantly dead.
  - Allies can reach and stabilize/extract them within a time window.

**[DREAM]**

- **Body-part injury model**:
  - E.g. leg injury → slower movement, arm injury → worse aim, head injury → incapacitation.
  - Persistent injuries feeding back into the strategic layer.

### 6.2 Friendly Fire

**[CORE]**

- **No friendly fire** in v0.1:
  - Shots do not harm allies.
  - Simplifies positioning and allows more aggressive use of group orders.

**[PLUS]**

- Optional friendly fire for higher difficulties.

**[DREAM]**

- Full ballistic simulation with penetration, ricochet, and strict friendly fire.

### 6.3 Weapons & Ammunition

**[CORE]**

- Initial focus on **ballistic weapons** only:
  - Pistols, SMGs, rifles, etc.
- Ammunition is **finite and significant**:
  - Weapons have magazines/clips.
  - Reloading takes time.
  - Running low on ammo mid-mission is a real risk.

**[PLUS]**

- Distinct weapon behaviors:
  - Burst fire, suppression, shotguns vs rifles differences.
- Attachments (scopes, silencers) as simple modifiers.

**[DREAM]**

- Non-ballistic weapons (energy, chemical, exotic) with distinct tactical consequences.

---

## 7. Stealth & Detection

### 7.1 Stealth Role & Failure Profile

**[CORE]**

- Stealth is viable but **minimal** in mechanical complexity:
  - Stay outside LOS to avoid detection.
  - Use cover and walls to move unseen.
- General rule:
  - Detection **does not automatically fail the mission**.
  - Detection escalates to combat or higher alert; player can still succeed via force.

**[PLUS]**

- Certain mission types where detection = hard failure.
- Clear “alarm state” UI with consequences (e.g. reinforcements, locked exits).

**[DREAM]**

- Missions built as deep stealth puzzles where detection is effectively failure and the mechanic set supports this in full depth.

### 7.2 Detection Model

**[CORE]**

- v0.1 detection:
  - Enemies have a vision radius and LOS.
  - If a player unit is in LOS and not fully behind cover → instantly detected.
  - State model:
    - Idle → Alerted/Engaged.
  - Alerted enemies know the last seen position of player units and will attempt to attack.

**[PLUS]**

- View cones instead of pure radial vision.
- Basic hearing:
  - Loud actions (gunfire, explosions) draw enemies within a radius.
- Intermediate state:
  - Idle → Suspicious → Alerted.
  - “Suspicious” causes investigation rather than immediate engagement.

**[DREAM]**

- Rich stealth AI:
  - Patrol routes with memory of past anomalies.
  - Coordinated search patterns, last-known-position tracking, and communication between enemy groups.

### 7.3 Non-Lethal Tools

**[CORE]**

- Not required for v0.1.

**[PLUS]**

- Limited non-lethal options (stun grenades, tasers, knockout gas).

**[DREAM]**

- Fully viable non-lethal play:
  - Getting paid more for clean, bloodless runs.
  - Hiding unconscious bodies, sophisticated takedown mechanics.

---

## 8. Interaction, Hacking, Engineering

### 8.1 Interaction Model

**[CORE]**

- Player interacts via **context actions** on objects:
  - Click object (door, terminal, hazard).
  - Game chooses the best eligible unit in range to perform the action.
- Core interactable types for v0.1:
  1. **Doors**
     - States: closed, open, locked.
     - Locked doors can be:
       - Opened with a key/authorization, or
       - Bypassed via hacking (if present).
  2. **Computer Terminals**
     - Used to:
       - Complete objectives (download data, transfer cargo).
       - Trigger changes in the map (open bulkhead, power on/off something).
  3. **Environmental Hazards**
     - Objects that can be triggered to cause tactical effects:
       - Explosions.
       - Area denial (fire, gas, etc.) if content supports it.
       - Disabling certain parts of the map.

### 8.2 Hacking & Channeled Actions

**[CORE]**

- Hacking is a **channeled action**:
  - A unit interacts with a terminal.
  - A progress bar fills over a short duration.
  - If the unit moves, is interrupted, or is ordered to do something else, hacking stops and progress is lost (or at least not completed).

- Design intent:
  - Hacking forces commitment and creates tension in both quiet and loud missions.

**[PLUS]**

- Variable hack difficulties:
  - Longer durations.
  - Multiple sequential steps (e.g. disable turrets → unlock main door).
- Partial progress retention:
  - E.g. 50% done hack resumes from 50% if re-engaged quickly.

**[DREAM]**

- Multi-step systemic hacking puzzles:
  - Complex security systems with meaningful tradeoffs (disabling one thing worsens another).
  - Layered hacking (subsystems, power rerouting, deeper security).

### 8.3 Engineering / Systemic Feel

**[CORE]**

- Even simple interactions have **systemic consequences**:
  - Opening a door changes pathing and LOS.
  - Triggering a hazard reshapes control of nearby space.

**[PLUS]**

- Interactions between subsystems:
  - Turning off a generator disables lights (beneficial for stealth) but may trigger backup measures.
  - Turning on sprinklers affects visibility or certain hazards.

**[DREAM]**

- True systemic station simulation:
  - Power, atmosphere, gravity, security, etc., all accessible and tactically relevant.

---

## 9. Abilities & Resources

### 9.1 Ability Types

**[CORE]**

- Units can have abilities that fall into:
  - Combat (e.g. aimed shot, defensive stances).
  - Utility (e.g. grenades, deployable gadgets).
  - Interaction-related (hacking, breaching) – surfaced mostly as context actions.

### 9.2 Costs & Limits

**[CORE]**

- Abilities are limited by:
  - **Cooldowns**, and/or
  - **Per-mission charges** (e.g. number of grenades, charge-based gadgets).
- No shared “team energy/mana” in v0.1.

**[PLUS]**

- Differentiation:
  - Some abilities are frequent (short cooldown, low impact).
  - Some are rare but powerful (few charges, high impact).

**[DREAM]**

- Shared global resources:
  - Team Focus, Heat, or similar governing global-level effects.

### 9.3 Execution Feel

**[CORE]**

- Combat abilities:
  - Fast, responsive, and readable.
- Hacking/engineering:
  - Channeled with visible progress and clear cancellation behavior.

**[PLUS]**

- Abilities that create temporary zones:
  - Smoke, suppression zones, sensor beacons.

**[DREAM]**

- Heavy synergy design between abilities (tagging, combo effects, chained actions).

---

## 10. AI & Difficulty Profile

### 10.1 Enemy Behavior (v0.1)

**[CORE]**

- Baseline enemy behaviors:
  - Seek cover when under fire if possible.
  - Attack visible enemies using straightforward priority (closest / most exposed / biggest threat).
  - May attempt basic retreat when heavily damaged and an escape route exists.

- No requirement for:
  - Complex flanking logic.
  - Coordinated maneuvers between many units.

**[PLUS]**

- Simple coordinated behaviors:
  - Some units attempt flanks if the player holds a position.
  - Enemies recognize obvious chokepoints and respond differently.

**[DREAM]**

- Faction-specific tactics, suppression, feints, adaptive behaviors.

### 10.2 Failure & Retreat

**[CORE]**

- Intended experience: **“harsh but recoverable”**.
  - Casual mistakes have real consequences (injuries, deaths, resource loss).
  - Full squad wipes are possible but should result from serious tactical errors, not random spikes.

- Retreat:
  - Player can always choose to **abort**:
    - Gather survivors.
    - Reach the entry zone.
    - Mission ends as a retreat/failure outcome; campaign handles penalties.

**[PLUS]**

- Dedicated retreat command:
  - Marks paths to the entry zone for all units, provides UI feedback.

**[DREAM]**

- More elaborate evac mechanics:
  - Calling shuttles, extraction timers, enemy attempts to cut off exit routes.

---

## 11. UX Principles for Micro Management

**[CORE]**

- The design intentionally **minimizes unnecessary micro**:
  - Group orders should cover most common actions.
  - Auto-pause on critical events gives breathing room.
  - Auto-selection of the best unit for interactions removes busywork.

- UI always aims to:
  - Make cover and LOS **visually legible**.
  - Make current orders clear and overridable at any time.

**[PLUS]**

- Overlays:
  - Cover quality indicators.
  - LOS previews when hovering over positions or targets.
  - Path previews showing expected exposure.

**[DREAM]**

- Planning overlays:
  - “Ghost” previews of unit paths and post-action positions.
  - Simple outcome prediction for queued/synchronized plans.

---

## 12. Constraints & Explicit Non-Features (v0.1)

The following are **explicitly out of scope for v0.1** (even if they appear in the Dream sections):

- No destructible terrain (walls, regular cover).  
  - Doors and specific objects can change state (open/closed/locked) but are not simulated destructibles.
- No friendly fire.
- No verticality or multi-floor tactical maps.
- No vehicles/mechs or mounted weapons the player directly drives.
- No time acceleration (fast-forward).
- No advanced stealth AI (patrol logic, complex search patterns) beyond what is defined as [CORE]/[PLUS].
- No body-part-specific injury model.

These constraints are important to keep the tactical layer **implementable and stable** for the first iterations.

---

## 13. Extensibility Points (Design-Level)

Future systems should plug into the tactical layer at these seams:

- **Actor model**  
  - New stats (e.g. injuries, stress) and abilities should extend existing unit properties, not redefine them.

- **Abilities**  
  - New abilities should work through the existing targeting/execution model (single-target, AOE, self, context-interaction).

- **Interactables**  
  - Any new object type (turret, camera, bulkhead, generator) should behave as:
    - An entity with states,
    - A source of context actions,
    - A state that impacts movement, LOS, or other defined tactical variables.

- **AI**  
  - New behaviors should build on the same perception (LOS, hearing), state (idle/suspicious/alerted), and goal-selection frameworks.

- **Mission Logic**  
  - Tactical missions should be composed from:
    - Objectives referencing world entities (reach X, hack Y, destroy Z),
    - State changes that the tactical sim exposes (e.g. alarm states, door states, terminal states).

This section is a design contract: deeper technical details and specific interfaces are defined in `TACTICAL_TECH.md`.

