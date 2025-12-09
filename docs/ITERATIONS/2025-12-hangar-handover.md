# Iteration: Hangar Handover – Mission-First Tactical Slice

**Status:** Active  
**Replaces:** G2.5 abstract systems direction  
**Goal:** Prove tactical combat is fun, readable, and deep via one flagship mission.

---

## 1. Context & Motivation

Tactical currently has many systems (time, fog of war, perception, interactables, status effects, objectives), but they are not yet orchestrated into a strong, repeatable gameplay loop.

**Problem:** We've been designing abstract systems in a vacuum instead of deriving features from concrete gameplay needs.

**Solution:** Focus on a single "prototype flagship" mission—the **Hangar Handover**—and use it as a design lab for combat mechanics, AI behaviour, level layout, and player-facing UX. Features and systems should be justified by how they make this mission better and more expressive.

---

## 2. Tactical Identity – Four Axes

Every tactical feature we introduce should meaningfully touch at least two of these axes:

| Axis | Description | Examples |
|------|-------------|----------|
| **Information** | Who sees what, when | Fog of war, vision ranges, sensors, hacked intel |
| **Position** | Where you stand and what you stand behind | Cover, flanking, chokepoints, doors, firing lanes |
| **Time** | Pressure and tempo | Timers, waves, action economy, channelled actions, retreat windows |
| **Value Extraction** | What you walk away with | Primary objectives vs optional loot, bonus objectives, risk-reward decisions |

**Feature validation:** Overwatch touches Information + Position + Time. Suppression touches Position + Time. Retreat/evac touches Time + Value Extraction.

---

## 3. The "Hangar Handover" Mission Concept

### 3.1 Theme

A negotiation or cargo handover in a ship hangar goes bad. It escalates into a firefight in a single large room with:
- Lots of mid-range cover
- Some explosive hazards
- Multiple enemy groups / waves entering over time

### 3.2 Mission Phases

| Phase | Name | Description |
|-------|------|-------------|
| **0** | Setup | Player sees hangar layout, places units in deployment zone |
| **1** | Negotiation | Brief non-combat state (narrative wrapper, future hook for player choices) |
| **2** | Contact / Ambush | Deal breaks down, combat starts. Boss + guards in favourable positions, some on overwatch |
| **3** | Pressure / Escalation | Reinforcement waves from spawn closets as time passes or events trigger |
| **4** | Resolution | Push to win or retreat with survivors. Value extraction decisions |

### 3.3 Design Goals

The Hangar Handover should clearly demonstrate:
- **Firefights with phases** (contact → pressure → resolution)
- **Meaningful positioning and cover**
- **Time-based pressure** via waves
- **Explicit, designed tradeoffs** in value extraction and retreat

---

## 4. Core Combat Mechanics for This Mission

### 4.1 Overwatch (Reaction Fire)

**Purpose:** Make movement across open ground risky. Let defenders set up threat zones.

**Design:**
- Unit spends attack to enter overwatch state
- Reaction shot triggers when enemy moves within LOS and range
- Overwatch ends after one reaction (initially)
- Strong on long fire lanes, chokepoints, flanking angles

**Interactions:** Encourages suppression, smoke, or alternative paths. Enemy AI can also use overwatch.

### 4.2 Leaning from Cover

**Purpose:** Make cover more interesting than a flat hit modifier. Reward flanking.

**Design:**
- When attacking from cover, unit "leans" and keeps strong protection against enemies in front
- More vulnerable from side angles during attack window
- Good flanks feel powerful; holding a strong angle is powerful but not invulnerable

**Interactions:** Synergizes with suppression and flanking. Makes room geometry tactically rich.

### 4.3 Suppressive Fire

**Purpose:** Create non-lethal pressure tool that shapes behaviour and enables manoeuvre.

**Design:**
- Special "suppressing fire" action or weapon mode
- Less damage than normal attacks
- Higher chance to apply Suppressed status (even on near-miss)
- Suppressed effects: reduced accuracy, reduced movement, possibly disables overwatch

**Interactions:** Pin opponents while others flank or fall back.

---

## 5. AI Behaviour Requirements

### 5.1 Enemy Roles

| Role | Behaviour |
|------|-----------|
| **Guards/Sentries** | Hold key cover positions, use overwatch on important lanes, fall back when under pressure |
| **Flankers/Hunters** | Move around edges, exploit suppression to advance on pinned players |
| **Support/Officers** | Coordinate waves, use abilities that buff allies or debuff players, target clusters with grenades |

### 5.2 Behaviour Patterns

- Prefer moving into cover relative to known threats
- Consider retreating to new positions when badly hurt or heavily suppressed
- Use grenades/hazards when player clumps behind cover
- Change behaviour between phases (contact → waves → cleanup)

---

## 6. Pressure, Relief, and Retreat

### 6.1 Pressure Sources

- **Reinforcement waves:** Spawn closets bring new enemies as time passes
- **Action economy:** Overwatch fields restrict safe movement
- **Suppression:** Punishes indecision and poor positioning
- **Optional timers:** Evac availability, bonus objective windows

### 6.2 Relief Valves

- **Retreat / Evac:** Not a failure state but a designed outcome
  - Success: survive and extract with at least one crew member
  - Higher success: extract with more crew and more bonus objectives
- **Environmental control:** Cover, hazards, doors to break LOS and funnel enemies
- **Abilities and items:** Smoke, stuns, suppression as tools to buy time or disengage

---

## 7. Value Extraction & Mission Outcomes

### 7.1 Objective Structure

| Type | Examples |
|------|----------|
| **Primary** | Survive the ambush and retreat with at least one crew member; ensure VIP/asset survives |
| **Secondary** | Loot the boss, secure cargo containers in risky positions, retrieve data from terminals |

### 7.2 Player Experience

Clear UI presentation so player feels tradeoffs:
- "We got out alive but left the big score on the table."
- "We pushed deeper, got the loot, but lost two people."

This pattern should be reusable across future mission types.

---

## 8. UX & Feedback Requirements

### 8.1 Indicators

| Element | Requirement |
|---------|-------------|
| **Overwatch** | Show which units are on overwatch, visualize threat area (cone/range) |
| **Cover** | Icon/UI element per unit showing cover quality vs main threats |
| **Status effects** | Clear icons for suppression, stun, etc. |

### 8.2 Combat Animations (Minimal)

- Units briefly aim/turn towards targets when firing
- Simple muzzle flash or recoil motion
- Basic hit/miss feedback (impact flashes, small knockback)

---

## 9. Hangar Layout Considerations

### 9.1 Spatial Design

- Single, medium-to-large room
- Varied cover islands and some open lanes
- Few "spawn closets" / side entries for reinforcement waves
- Clear "center of gravity": boss and high-value loot in central/forward area
- Player deployment behind/near entry zone

### 9.2 Design Goals

- Multiple viable lines of advance and retreat
- Good positions for overwatch
- Flanking routes that become important once suppression/overwatch are in play
- Obvious locations for spawn closets (side doors, upper walkways, blast doors)

---

## 10. Feature Clusters & Implementation Order

### Cluster A: Combat Mechanics Foundation

1. **Overwatch system** – reaction fire, threat zones, AI usage
2. **Suppression system** – suppressive fire action, Suppressed status, effects
3. **Cover refinement** – leaning/exposure mechanics, flanking bonuses

### Cluster B: AI & Mission Flow

4. **Enemy roles** – distinct behaviours for guards, flankers, support
5. **Wave/spawn system** – spawn closets, timed or event-triggered waves
6. **Phase transitions** – setup → negotiation → contact → pressure → resolution

### Cluster C: Value Extraction & Retreat

7. **Retreat mechanics** – evac zones, retreat command, mission outcome states
8. **Objective system** – primary vs secondary, loot pickups, data retrieval
9. **Mission summary** – clear presentation of outcomes and tradeoffs

### Cluster D: UX & Feel

10. **Overwatch indicators** – visual threat zones
11. **Cover indicators** – per-unit cover quality display
12. **Status effect icons** – suppression, stun visibility
13. **Basic combat animations** – aim, fire, hit feedback

---

## 11. Roadmap Alignment

This iteration replaces the short-lived "G2.5" direction. The global roadmap should be realigned:

1. **Priority:** Deliver combat-focused vertical slice via Hangar Handover
2. **Then:** Apply same combat foundations to additional mission types
3. **Then:** Gradually integrate stealth and "heist" mechanics once combat is solid
4. **Finally:** Scale toward G3 (larger maps, multi-room, campaign integration)

---

## 12. Success Criteria

The Hangar Handover is complete when:

- [ ] Firefights have clear phases (contact, pressure, resolution)
- [ ] Overwatch creates meaningful threat zones that shape player movement
- [ ] Suppression enables tactical manoeuvre (pin and flank)
- [ ] Cover and positioning feel consequential (flanking matters)
- [ ] Waves create escalating pressure over time
- [ ] Retreat is a viable, designed option with clear tradeoffs
- [ ] Value extraction decisions feel meaningful (risk vs reward)
- [ ] AI behaviour is readable and role-distinct
- [ ] UX makes combat state clear without complex animations

---

## 13. Findings (Post-Implementation)

_(Fill in after playtesting)_

- 
- 
- 

## 14. Decisions

_(Turn findings into concrete follow-ups)_

- 
- 
- 
