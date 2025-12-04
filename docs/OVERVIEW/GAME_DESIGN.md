# GAME_DESIGN.md – Fringe Tactics (working title)

Firefly-flavored, real-time-with-pause crew tactics on a procedural sector map.  
You fly a barely-legal ship, scrape by on shady jobs, and try to keep a fragile crew alive.

---

## 1. High-Level Vision

**One-sentence pitch**

> Fly around with a misfit crew in a procedural sector, upgrade ship and crew while running on fumes, and try to survive increasingly risky jobs in gritty, real-time-with-pause on-foot combat.

**Tone**

- Gritty space-western survival.
- Banter and personality between and during missions.
- No epic “save the galaxy” narrative; it’s about keeping the lights on.

**Campaign structure**

- Repeatable, procedural, **longer** campaigns (not short roguelite runs).
- Each run: one ship + one crew, evolving over many missions.
- Death is permanent; failure is likely on higher difficulties.

**Hard constraints**

- Single-player only.
- Combat: **real-time with pause** (RTwP).
- Combat focus v0.1: **on-foot missions** (ship interiors or planetary outposts).
- Graphics: **2D top-down**, simple pixel sprites.

---

## 2. Core Design Pillars

1. **Running on fumes**
   - Money, fuel, parts, ammo, meds are always tight.
   - “Can we afford to take this risk?” is a constant question.

2. **Gritty crew survival**
   - 5–15 named crew; 4–5 active in missions.
   - Traits, skills, and injuries shape outcomes.
   - Crew death is permanent and emotionally/operationally costly.

3. **Systems over scripts**
   - Sector, factions, jobs, and encounters are procedurally generated.
   - Interlocking systems (economy, faction relations, crew traits) drive stories.
   - No pre-authored main story for v0.1.

4. **Tool-heavy, RTwP combat**
   - RTwP, FTL/Barotrauma-inspired: many ways to influence an encounter.
   - Emphasis on abilities, positioning, and environment interaction.
   - Readable, not twitch-based; success comes from planning and pausing.

5. **Long-form procedural campaign**
   - Sector runs are meant to last many missions.
   - Progression via crew leveling and ship upgrades.
   - The sector is hostile; “win” is surviving deep into the campaign.

---

## 3. Target Player Experience

In a typical 30–60 minute session, the player:

1. Checks jobs at current node, weighing risk vs payoff.
2. Accepts a contract that fits current crew/ship condition.
3. Travels through the sector (events, resource drains).
4. Plays one or more RTwP on-foot missions.
5. Deals with aftermath: injuries, deaths, rewards, repairs, upgrades.
6. Feels slightly more powerful, but still precarious.

Emotional goals:

- Anxiety about resources and crew safety.
- Satisfaction when a risky plan works or a desperate mission is salvaged.
- Attachment to specific crew (favorite pilot, medic, engineer).
- Occasional “we really shouldn’t have survived that” highs.

---

## 4. Core Loop

**Core gameplay loop (v0.1):**

1. **Dock / idle at node**
   - View jobs, buy/sell, manage crew and ship.

2. **Accept a job**
   - Job defined by: employer faction, destination node, mission type, difficulty, pay, risks.

3. **Travel to destination**
   - Spend fuel, time (for now, time is flavor only, no deadlines).
   - Roll for travel events (ambush, distress call, inspection, etc.).

4. **On-foot mission**
   - Real-time-with-pause combat in a 2D top-down environment.
   - Control 4–5 crew; objectives vary by job type.

5. **Resolve outcome**
   - Reward: money, parts, faction reputation changes, loot.
   - Cost: injuries, death, ammo/med usage, hull/ship damage (if ship involved).
   - Possible follow-up events based on what happened.

6. **Maintenance and upgrades**
   - Pay for repairs, restocks, and upgrades.
   - Level up crew, assign new perks/skills.
   - Choose the next job → back to step 2.

---

## 5. Strategic Layer – Sector, Factions, Jobs

### 5.1 Sector structure

- Procedurally generated **sector map** with ~40–50 nodes.
- Nodes represent stations, outposts, settlements, or waypoints.
- Edges are travel routes with:
  - Fuel cost.
  - Risk modifiers (pirate activity, patrols, etc.).
- No time pressure or sector-wide clock for v0.1.
  - Jobs may be static for now; no expiry timers.

### 5.2 Factions

For v0.1:

- **3 proceduralized faction archetypes**, e.g.:
  - **Authority** (Gov/Alliance analogue): lawful, controlling, decent pay, strict conditions.
  - **Corporate**: profit-driven, better gear access, morally indifferent.
  - **Fringe/Criminal**: shady jobs, high risk, often best payouts.

Each faction:

- Has generated name, flavor, and territory preferences on the sector map.
- Offers specific job mixes and difficulty profiles.
- Tracks simple **reputation**:
  - Higher rep: better jobs, better prices, maybe favors.
  - Low rep: fewer jobs, increased hostility in some events.

### 5.3 Jobs & contracts (v0.1)

Jobs are templates with parameters. For v0.1, target:

- ~5–6 job types, for example:
  - **Cargo Smuggle**: move illegal goods past authority-controlled routes.
  - **Raid/Hit**: attack an outpost/convoy for a faction.
  - **Escort/Protection**: defend a target during an attack.
  - **Extraction/Rescue**: infiltrate, grab a person/item, and escape.
  - **Security Sweep**: clear out hostiles in a location.
  - **Covert Operation** (light): plant device, hack terminal, avoid full-scale fight if possible.

Each job:

- Specifies:
  - Employer faction.
  - Target node.
  - Base pay, bonuses, penalties.
  - Expected enemy types.
  - Mission objectives & optional objectives (later).

---

## 6. Tactical Layer – RTwP On-Foot Combat

**Goal:** RTwP system inspired by FTL/Barotrauma/Desperados, but scoped tightly for v0.1.

### 6.1 Basic structure (v0.1)

- **View:** 2D top-down.
- **Control:** Player controls 4–5 active crew members.
- **Flow:**
  - Real-time simulation.
  - Player can **pause anytime** to issue orders.
  - Queue/stack of orders per character (move, shoot, use ability, interact).
- **Maps:**
  - Limited-size layouts (ship interiors or small outposts).
  - For v0.1: mix of handcrafted “rooms-and-corridors” patterns and light procedural variation (room order, loot, enemy placement).

### 6.2 Core mechanics (v0.1)

- **Movement:**
  - Click-to-move with pathfinding.
  - Simple cover/line-of-sight rules (start with “blocked / not blocked”).

- **Combat:**
  - Firearms and basic weapons.
  - Shooting resolves via:
    - Accuracy (crew stat + weapon).
    - Distance and cover modifiers.
  - Friendly fire is possible or at least plausible at higher difficulties.

- **Pause & commands:**
  - Player pauses to:
    - Set up synchronized actions (breach a door, focus fire).
    - Reprioritize targets.
    - Trigger abilities.

- **Abilities / tools:**
  - For v0.1, a small set per broad role:
    - Soldier (suppression, overwatch-like cone, stun grenade).
    - Medic (heal, stabilize bleedout).
    - Engineer (turret deployment, door lockdown, quick repair).
    - Hacker/Tech (disable turret, open locked doors, hack terminals).
  - Long-term intent: “more than enough tools” to manipulate outcomes; v0.1 is a subset.

### 6.3 FTL / Barotrauma style concepts (design intent)

Not all for v0.1, but guiding direction:

- **Critical systems** in maps:
  - Power, doors, life support, comms, security systems.
- **Environmental hazards:**
  - Fires, decompression, toxic leaks, malfunctioning bots.
- **Chain reactions:**
  - Shooting a fuel line causes fire, which spreads, forcing repositioning or retreat.
- **Systemic outcomes:**
  - You can win an encounter by manipulating systems rather than outgunning enemies.

For now, `v0.1` focuses on a **simpler subset**:
- Limited interactive props (e.g., doors, terminals, maybe a couple of environmental hazards).
- One or two “systemic moments” per mission, not full Barotrauma complexity.

---

## 7. Crew, Traits, and Progression

### 7.1 Crew size and roles

- **Total ship crew:** 5–15 named characters.
- **Active mission squad:** 4–5 at a time.
- Crew have broad roles (can overlap):
  - Pilot / Navigator
  - Mechanic / Engineer
  - Soldier / Security
  - Medic
  - Hacker / Tech

### 7.2 Stats

Each crew member has stats used across systems, e.g.:

- **On-foot combat stats:**
  - Aim
  - Toughness (HP/defense proxy)
  - Reflexes (evasion, recovery speed, etc.)
- **Ship & non-combat stats:**
  - Piloting (evasion in space encounters, event checks).
  - Mechanics (repair speed, event checks).
  - Social / Negotiation (event outcomes, prices).
  - Tech / Hacking (system events, mission objectives).

Stats are used in:

- Combat calculations.
- Event checks in travel or post-mission.
- Job suitability (some jobs strongly prefer certain skills).

### 7.3 Traits & personalities

- Each crew member gets:
  - 1–3 **traits** (mechanical and/or flavor).
    - Examples:
      - “Steady Under Fire” – less accuracy penalty when suppressed.
      - “Reckless” – more likely to push into danger, small bonus to aggressive checks.
      - “Grease Monkey” – better repair outcomes.
  - Personality tags for banter/event flavor (v0.1: mostly used in text and small modifiers).

### 7.4 Progression

For v0.1:

- **Leveling:**
  - Crew gain XP from missions and certain events.
  - Level-ups provide:
    - Stat increases.
    - Choice of perks (small, focused bonuses or abilities).

- **Gear:**
  - Weapons, armor, gadgets as a secondary progression vector.
  - Simple rarity tiers and clear tradeoffs (damage vs accuracy, protection vs speed).

- **Injuries & death:**
  - Permadeath is always on.
  - v0.1: basic injury model:
    - Downed → bleedout timer → death if not stabilized.
    - Survivors may gain a temporary or permanent debuff (e.g., “Fractured Arm” → worse accuracy).
  - Later: richer injury system; for v0.1 keep it understandable and manageable.

---

## 8. Economy & Resources

Tracked resources (v0.1):

- **Money (credits)** – wages, fuel, repairs, buying gear.
- **Fuel** – required to travel between nodes.
- **Parts** – used to repair ship and certain equipment.
- **Meds** – used for healing and treating injuries.
- **Ammo** – per-mission or pooled resource, depending on final UX.

Economy behavior:

- Jobs rarely cover *all* costs; profit margins are thin.
- Certain jobs or factions may pay in kind (parts, meds) instead of pure cash.
- Prices vary slightly by node and faction alignment.

---

## 9. Difficulty, Pressure, and Failure

### 9.1 Difficulty and pressure profile

- On higher difficulties:
  - You’re **barely scraping by** most of the time.
  - A single disastrous mission can collapse the run.
- On lower difficulties:
  - More economic slack but still not a power fantasy.

Pressure sources:

- Resource drain per mission and per travel.
- Crew injuries and deaths reducing operational capacity.
- Faction relationships influencing job quality and sector safety.

### 9.2 Fail states (v0.1)

- **Primary fail state:** All crew dead (in missions or via events).
- Secondary fail states (for later/not v0.1):
  - Bankruptcy / insurmountable debt.
  - Ship destroyed.
  - Locked-out situation where no viable jobs remain.

For v0.1, design assumes “everyone dies” as the clean end of a run.

---

## 10. Content Scope – v0.1 Targets

Concrete, initial scope (subject to change, but used for planning):

- **Sector:**
  - 1 sector generated per run.
  - ~40–50 nodes, but player may only visit a subset in a typical run.

- **Factions:**
  - 3 faction archetypes with procedural details.

- **Jobs:**
  - 5–6 job templates with parameterized variants.

- **Enemy archetypes:**
  - ~5–6 enemy types, e.g.:
    - Basic gunner
    - Heavy/bruiser
    - Sniper
    - Tech/Engineer (uses turrets, repairs, hacks)
    - Drone/bot
    - Elite/security officer (harder version of basic)

- **Maps / missions:**
  - ~4–6 base layout archetypes:
    - Small station interior.
    - Cargo dock.
    - Research outpost.
    - Pirate hideout.
    - Generic ship boarding layout.
  - Each archetype supports some procedural variation:
    - Different locked doors, enemy placements, side rooms, loot spots.

- **Crew content:**
  - Enough traits, backgrounds, and names to avoid obvious repetition for a few runs.
  - ~8–10 perks/abilities per role to pick from across levels.

---

## 11. UX & Production Constraints

- **Controls:**
  - Mouse-driven primary controls.
  - Keyboard shortcuts for pause, camera, and common commands.
  - No controller support planned for v0.1.

- **UI complexity:**
  - Acceptable to have:
    - A dense crew/ship management screen.
    - A job board screen.
    - A sector map screen.
  - Prioritized:
    - Clarity and information density over flashy art.

- **Art style:**
  - 2D top-down, simple **pixel sprites** for:
    - Crew.
    - Enemies.
    - Props (doors, terminals, crates).
  - Clean, readable UI with a “tactical ship computer” feel.
  - No detailed animations required initially; minimal poses are acceptable.

---

## 12. Non-Goals for v0.1

- No overarching scripted main story.
- No branching dialogue trees / visual novel-style scenes.
- No space combat minigame (focus on on-foot missions).
- No complex ship interior simulation (pressure, full power grid, etc.) beyond simple hooks.
- No multiplayer or co-op.

---

## 13. Future Directions (Post v0.1 – Intent Only)

Not part of v0.1 scope, but inform current decisions:

- Richer Barotrauma-like ship systems:
  - Power routing, atmosphere, hull breaches, system rooms.
- Stronger systemic map hazards and events:
  - Sector-wide crises, patrol sweeps, escalating faction wars.
- Deeper relationships:
  - Crew bonds, rivalries, personal arcs with mechanical effects.
- More expressive RTwP tools:
  - Advanced gadgets, deployables, hacking minigames, chain reactions.

For now, v0.1 aims to deliver a **coherent, replayable slice** of the above:  
A long-form, procedural campaign with real-time-with-pause on-foot missions, harsh survival arithmetic, and enough personality to make you care when your mechanic bleeds out in a corridor on some nameless rock.
