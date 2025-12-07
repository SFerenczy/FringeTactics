# GLOBAL_ROADMAP

## 1. Global milestone map (end-to-end, domain-aware)

Think of this as the “campaign roadmap” that Tactical slots into:

1. **G0 – Foundations + Tactical Core in a Vacuum**
2. **G1 – Single-System Jobbing Loop (no real galaxy yet)**
3. **G2 – Sector Map, Travel, and Encounters (no full sim yet)**
4. **G3 – Simulation-Driven Sector (probabilities become systemic)**
5. **G4 – Depth & Personality (crew, factions, richer generation)**

Tactical’s own milestones M0–M8 are mostly inside G0–G2, culminating with M7 “Session I/O & Retreat”, which is where Tactical truly joins the campaign layer.

I’ll walk each global milestone with domain focus, what’s playable, and what you intentionally leave out.

---

## 3. G0 – Foundations + Tactical Core in a Vacuum

### Goal

Have a robust RTwP tactical sandbox running in isolation, plus core Systems Foundation, so you can iterate on combat feel and test mission specs without worrying about the campaign yet.

### Domains to focus

* **Systems Foundation** (see `DOMAINS/SYSTEMS_FOUNDATION/ROADMAP.md`)

  * Time system (support both tactical ticks and future strategic time).
  * RNG streams (separate tactical from future generation/sim).
  * Basic event bus.
  * Minimal data/config loading (weapon defs, unit archetypes, test maps). 

* **Tactical** (see `DOMAINS/TACTICAL/ROADMAP.md`)

  * Basically drive your existing Tactical roadmap from M0 through M6/M7: skeleton, multi-unit control, FoW, basic combat, cover, interactables & hacking, stealth/alarm foundations, and the mission I/O contract.

* **Concepts** (docs only, not systems yet)

  * Resources, Crew, Contracts, Factions, World Metrics: first pass of those concept docs, as discussed above.

### G0 Checklist

| Item | Status | Notes |
|------|--------|-------|
| **Tactical M0–M7** | ✅ Complete | Core tactical loop done |
| **SF0 – RNG & Config** | ✅ Complete | RngService, ConfigRegistry |
| **SF1 – Time System** | ✅ Complete | GameTime with campaign days and tactical ticks |
| **SF2 – Event Bus** | ✅ Complete | EventBus with typed events |
| **SF3 – Save/Load** | ✅ Complete | Campaign state persistence |
| **Concept: Resources** | ✅ Complete | CAMPAIGN_FOUNDATIONS.md §1 |
| **Concept: Crew** | ✅ Complete | CAMPAIGN_FOUNDATIONS.md §3 |
| **Concept: Contracts** | ✅ Complete | CAMPAIGN_FOUNDATIONS.md §2 |
| **Concept: World Metrics** | ✅ Complete | CAMPAIGN_FOUNDATIONS.md §4 |
| **Concept: Mission I/O** | ✅ Complete | CAMPAIGN_FOUNDATIONS.md §7 |

**G0 Status: ✅ COMPLETE** – Proceed to G1.

### Playable state

* You can load a mission spec by hand (or from a test harness), play through a lethal firefight or a "escort hacker to console" style mission, and get a mission result struct back.
* All tuning is local: no campaign, no money, no fuel, no galaxy.

### What you deliberately postpone

* WORLD, SIMULATION, GENERATION, TRAVEL, ENCOUNTER, MANAGEMENT as *implemented* systems.
* Campaign UI, node map, job boards.

### Why this order

* Tactical is technically dense; you already have a detailed roadmap and design. Getting it stable early de-risks the most complex simulation you have, and the mission I/O contract becomes the anchor for everything else.

---

## 4. G1 – Single-System Jobbing Loop (no real galaxy yet)

This is your first real *campaign* loop, but constrained to a single “hub”.

### Goal

Let the player:

1. Sit at a station.
2. Pick from a small job list.
3. Play a tactical mission.
4. Get paid, repair, heal, level up.
5. Repeat until they run themselves into the ground.

Basically Battle Brothers on one town, no overworld.

### Domains to bring online

* **WORLD (minimal)**

  * Exactly one system and one station, but represented through the real World domain structures: ownership, facilities, and world-attached metrics (even if most are fixed constants right now). 

* **MANAGEMENT (minimal but real)**

  * PlayerState: one ship, a handful of crew, inventory, resources.
  * Basic operations: pay costs, receive rewards, apply tactical results (injuries, deaths, ammo usage, ship damage as a simple “hull” resource).

* **GENERATION (thin)**

  * No galaxy gen yet.
  * Just: given (player state, world metrics at the hub), generate 3–5 contracts using templates, with difficulty and reward roughly matched to player power. All from a single hub. 

* **TACTICAL**

  * Used via the M7 session I/O: Management passes in the crew snapshot, Tactical runs the mission, returns mission result; Management applies it.

* **SYSTEMS FOUNDATION**

  * Now also owns save/load of CampaignState + Tactical results, not just tactical tests.

### Playable loop

From the player’s perspective (and roughly matching GAME_DESIGN core loop):

1. Docked at “Nowhere Station”.
2. Open a job board (list of generated contracts).
3. Accept a job.
4. (Strategic) Start mission → Tactical runs.
5. Tactical result applied: crew injuries/deaths, ammo consumed, resources paid out.
6. Use station facilities: basic shop (buy ammo/meds), repair ship, hire/fire crew.
7. Repeat.

### Things you *don’t* do yet

* No travel between systems.
* No Simulation: world metrics are static or hand-tweaked.
* No Encounters domain or text events.
* No dynamic sector map at all.

### Why this order

* It gives you a full vertical slice: Systems Foundation ↔ Management ↔ Tactical ↔ Generation ↔ a tiny sliver of World.
* You validate contract schema, mission I/O, and the Management consequences loop without touching the complexity of travel or sim.
* You already exercise the crew/trait model (through Tactical) and resource flows (through rewards/repairs).

### G1 Checklist

| Item | Status | Notes |
|------|--------|-------|
| **MG0 – Concept** | ✅ Complete | See `MG0_IMPLEMENTATION.md` |
| **WD0 – Concept** | ✅ Complete | See `WD0_IMPLEMENTATION.md` |
| **GN0 – Concept** | ✅ Complete | See `GN0_IMPLEMENTATION.md` |
| **MG1 – PlayerState & Crew** | ✅ Complete | See `MG1_IMPLEMENTATION.md` |
| **WD1 – Single Hub World** | ✅ Complete | See `WD1_IMPLEMENTATION.md` |
| **MG2 – Ship & Resources** | ✅ Complete | See `MG2_IMPLEMENTATION.md` |
| **GN1 – Contract Generation** | ✅ Complete | See `GN1_IMPLEMENTATION.md` |
| **MG3 – Tactical Integration** | ✅ Complete | See `MG3_IMPLEMENTATION.md` |

### Recommended Implementation Order

1. **MG1 – PlayerState & Crew Core**: Foundation that World and Generation reference
2. **WD1 – Single Hub World**: Minimal world with one station and facilities
3. **MG2 – Ship & Resources**: Complete player state with ship and inventory
4. **GN1 – Contract Generation**: Generate contracts using player power and hub metrics
5. **MG3 – Tactical Integration**: Wire mission I/O to complete the loop

---

## 5. G2 – Sector Map, Travel, and Encounters (no full sim yet)

Now you turn “one hub with jobs” into a small systemic sandbox: multiple nodes, travel, and emergent non-combat events. Still no heavy Simulation.

### Goal

Let the player roam a small sector, choose routes, consume fuel, risk encounters, and feel that jobs, prices, and events differ by place.

### Domains to focus

* **WORLD (real sector)**

  * Actual galaxy/sector graph: multiple systems, stations, routes, tags (dangerous route, core/border, pirate space, etc.).
  * World metrics per system/station present but still mostly driven by scripted rules or by direct consequences of jobs, not by a ticking Simulation yet.

* **TRAVEL (v1)**

  * Route planning over World topology.
  * Travel plans: time + fuel/supplies cost + a simple risk profile.
  * Execution: advance time, consume resources, roll for encounters along segments.

* **ENCOUNTER (v1 runtime)**

  * Implement the encounter state machine runtime: EncounterInstance, nodes, options, conditions, outcomes.
  * Skill/trait checks that actually look at crew state.
  * Output: structured outcome payloads (resource deltas, injuries, time delays, flags).

* **GENERATION (galaxy + encounter hooks)**

  * Galaxy generation at campaign start: generate the initial sector graph, systems, stations, ownership, initial metrics, using the concept of archetypes and tags.
  * Mission generation now respects region/faction/world metrics (even if those are still rule-based).
  * Encounter template selection and instantiation based on TravelContext and system tags (border, pirate, corporate, backwater).

* **MANAGEMENT**

  * Now integrated into Travel (fuel/supplies consumption) and Encounter consequences as well as Tactical.

### Playable loop

Now a typical 30–60 minute session starts looking like your intended experience: 

1. At station A, choose a contract to B or C.
2. Plan a route (fast and dangerous vs long and safer).
3. Travel: fuel ticks down, maybe an encounter fires (pirates, patrols, anomaly).
4. Encounter resolves via the Encounter domain; some may branch into Tactical, others are purely narrative/skill-check.
5. Arrive at B, run the job tactically, get paid (or limp away).
6. Manage aftermath: injuries, resources, ship condition.
7. Decide where to go next.

### Still missing / postponed

* **SIMULATION**: there is still no autonomous, ticking simulation of factions and economy. World metrics may be tweaked by simple rules (e.g., “complete security contract in system → +security, -pirate_activity”), but they don’t evolve on their own. 
* Deep faction AI, wars, or region-wide events.
* Complex economic feedback into prices everywhere.

### Why this order

* You get “space-opera jobbing around a sector” *feeling* emergent before tackling the complexity of a full sim.
* Encounters and Travel are inherently about *moment-to-moment* play; they don’t actually need the sim at first if you’re willing to author simple rules and probabilities.
* You can test whether the campaign pacing (time, fuel, job density) feels good *before* you let metrics drift systemically.

### G2 Checklist

| Item | Status | Notes |
|------|--------|-------|
| **WD2 – Sector Topology** | ✅ Complete | Multi-system graph with routes |
| **WD3 – Metrics & Tags** | ✅ Complete | Live metrics and tag system (see `WD3_IMPLEMENTATION.md`) |
| **TV0 – Concept** | ✅ Complete | Travel design finalization (see `TV0_IMPLEMENTATION.md`) |
| **TV1 – Route Planning** | ✅ Complete | Pathfinding and travel plans (see `TV1_IMPLEMENTATION.md`) |
| **TV2 – Travel Execution** | ✅ Complete | Time/fuel consumption, encounter triggers (see `TV2_IMPLEMENTATION.md`) |
| **EN0 – Concept** | ✅ Complete | Encounter design finalization | see `EN0_IMPLEMENTATION.md`
| **EN1 – Runtime Core** | ✅ Complete | State machine, conditions, outcomes | see `EN1_IMPLEMENTATION.md` |
| **EN2 – Skill Checks** | ✅ Complete | Crew-based checks and modifiers (see `EN2_IMPLEMENTATION.md`) |
| **GN2 – Galaxy Generation** | ⬜ Pending | Sector graph generation (see `GN2_IMPLEMENTATION.md`) |
| **GN3 – Encounter Instantiation** | ⬜ Pending | Template selection and parameterization (see `GN3_IMPLEMENTATION.md`) |
| **MG4 – Encounter Integration** | ⬜ Pending | Apply encounter outcomes to player state |

### Recommended Implementation Order

**Phase A: World Foundation (WD2 → WD3)**
1. **WD2 – Sector Topology**: Multi-system graph, routes, connections
2. **WD3 – Metrics & Tags**: System-level metrics and tag vocabulary

**Phase B: Travel System (TV0 → TV1 → TV2)**
3. **TV0 – Concept**: Finalize travel mechanics design
4. **TV1 – Route Planning**: Pathfinding, travel plan creation
5. **TV2 – Travel Execution**: Time/fuel consumption, encounter trigger points

**Phase C: Encounter System (EN0 → EN1 → EN2)**
6. **EN0 – Concept**: Finalize encounter structure and templates
7. **EN1 – Runtime Core**: State machine, node traversal, outcomes
8. **EN2 – Skill Checks**: Crew stat integration, trait-based options

**Phase D: Generation & Integration (GN2 → GN3 → MG4)**
9. **GN2 – Galaxy Generation**: Generate sector at campaign start
10. **GN3 – Encounter Instantiation**: Select and parameterize encounters
11. **MG4 – Encounter Integration**: Apply outcomes to player state

---

## 6. G3 – Simulation-Driven Sector

This is where you make the world truly systemic rather than “set-dressing with some rules”.

### Goal

Bring Simulation online so:

* System metrics (security, piracy, trade, unrest) evolve over time based on events and faction policies.
* Travel risk, mission offers, encounter types, and prices all shift according to that evolving state.

### Domains to focus

* **SIMULATION (v1)**

  * Core macro state representation (SystemMetrics, FactionState).
  * SimulationTick that consumes event stream + Δt and updates metrics in deterministic way.
  * Simple response curves: more piracy → more security investment with delay; success/failure of security contracts feed into those curves. 

* **SYSTEMS FOUNDATION**

  * Event bus is now properly used: Tactical, Travel, Encounter, Management emit events; Simulation subscribes.

* **WORLD**

  * Becomes the shared storage for metrics and faction ownership that Simulation reads/writes and that everyone else queries.

* **GENERATION / TRAVEL / ENCOUNTER / MANAGEMENT integration**

  * Generation: uses simulation metrics to bias mission types/frequencies (e.g. more pirate hunting contracts where piracy is high).
  * Travel: uses Simulation’s probability fields to compute risk profiles and encounter intensities.
  * Encounter: uses local unrest/security/piracy for template selection and flavor; sends back macro-relevant events (sabotage, political actions).
  * Management: prices and availability draw from Simulation/World (wealth and trade_volume), at least in a coarse way.

### Playable loop

Now, without changing the player-facing UI much, runs start to feel different:

* If you prey on traders in a region, trade_volume drops, prices spike, security ramps up, and pirate/patrol encounter probabilities shift meaningfully.
* If you accept security contracts for a faction, you gradually stabilize their space, which changes job offerings and travel risk.

You get “my actions shape the sector” without ever having a 4X AI.

### What you still postpone

* Sophisticated faction “brains” (explicit strategies, coalition politics). You can get a lot of emergent behavior using response curves and thresholds alone. 
* Big story-like region events (plagues, wars) can wait; just keep hooks for them in Simulation and World.

### Why here

* Only now do you have enough data and event streams to justify a simulation pass: Tactical, Travel, Encounter, Management, and World all produce the events Simulation needs.
* If you’d tried to do Simulation earlier, you’d be tuning in a vacuum.

---

## 7. G4 – Depth & Personality (crew, factions, richer generation)

At this point, your baseline systemic loop works. G4 is about making it *interesting for 50+ hour campaigns* rather than “solid 10-hour prototype”.

### Goal

Increase expressive richness and replayability without breaking abstractions.

### Places to deepen

* **MANAGEMENT / CREW**

  * More nuanced traits and their systemic hooks (morale, stress, interpersonal relationships as a later extension).
  * Injuries that feed back more cleanly into tactical stats and encounter options (even if Tactical stays on a simple HP model for now).

* **FACTIONS & WORLD**

  * Faction-specific mission flavors and encounter tables.
  * Soft arcs: “this region is sliding into lawlessness unless you intervene”.

* **GENERATION / ENCOUNTER**

  * Longer micro-stories as chains of encounters and missions driven by flags rather than scripts.
  * Region-specific mission archetypes and tones.

* **TACTICAL polish**

  * M8 UX & feel passes, more ability variety, better AI behaviors, etc.

### Tradeoffs

* It’s tempting to start here (crew drama, fancy encounters) but they’re multiplied by how solid your underlying loops are. By pushing this to G4, you ensure you’re building on a stable systemic bedrock.

---

## 8. Domain bring-up order (condensed)

If you want a simple checklist view:

1. **G0 – Foundations + Tactical**

   * Systems Foundation (time, RNG, events, minimal config).
   * Tactical M0–M7.
   * Concept docs for Resources, Crew, Contracts, Factions, WorldMetrics.

2. **G1 – Single-System Jobbing**

   * World (one system, one station).
   * Management (PlayerState + crew/ship/resources).
   * Generation (contracts for a single hub).
   * Tactical integrated via mission I/O.
   * Persistence for campaign.

3. **G2 – Sector + Travel + Encounters**

   * World (real sector topology).
   * Travel (planning, execution, risk).
   * Encounter runtime + initial templates.
   * Generation (galaxy init, region-aware jobs, encounter instantiation).
   * Management extended to travel/encounter costs & consequences.

4. **G3 – Simulation**

   * Simulation domain with metrics & response curves.
   * Integration: World (metrics), Travel (risk), Generation (job distributions), Encounter (context + events), Management (prices).

5. **G4 – Depth**

   * Crew depth, faction personality, micro-stories, more content, plus Tactical UX/feel and extended tools.

