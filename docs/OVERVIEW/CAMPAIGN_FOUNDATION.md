# CAMPAIGN_FOUNDATIONS.md

Status: Draft v0  
Scope: Cross-cutting assumptions for campaign-layer systems  
Covers: Resources, Contracts, Crew, World Metrics, Time, RNG, Mission I/O

The goal of this document is to be the **single source of truth** for campaign-level fundamentals.  
Other domain docs (WORLD, TRAVEL, MANAGEMENT, ENCOUNTER, TACTICAL, SYSTEMS_FOUNDATION) should *reference* this instead of restating it.

---

## 1. Resource Model

### 1.1 Decisions (v0)

**Ammo**

- Tactical ammo is effectively **infinite** for now.
- Mechanical constraints around ammo (reload, weapon choice, etc.) are handled **inside the tactical layer**, not as a strategic resource.
- Later, ammo can become **equipment** (weapon mods, special ammo types), not a generic consumable.

**Cargo / inventory**

- No inventory Tetris.
- **Volume/Mass is tracked only for trade goods and bulk cargo**, not for every tiny item.
- Small items (weapons, armor, gadgets) are abstracted as “equipped” or “stored” without spatial puzzles.
- Cargo items can carry **tags** (e.g. `illegal`, `perishable`, `fragile`, `volatile`, `medical`, `luxury`, `restricted_faction_X`) to drive:
  - Encounter hooks (inspection, pirates, black market).
  - Price modifiers and contract availability.
  - Faction/legal interactions.

**Crew cost / economy pressure**

- There is **no constant per-day salary tax** that pushes toward “battles per minute”.
- Baseline: crews are funded **primarily via contract/job payouts** and possibly:
  - Occasional “maintenance / upkeep” events at specific beats (dock refits, license renewals, etc.), not ticking every day.
  - Optional crew “shares” (profit split) encoded in event text and outcomes instead of detailed bookkeeping.

### 1.2 Implementation Notes

- Campaign economy should treat **cargo capacity** as a key axis of growth/progression:
  - Upgrades: more cargo, special cargo types (e.g. refrigerated, secure).
  - Risk: more cargo → more attractive target, more inspection risk.
- Keep resource vocabulary small:
  - `credits` (or equivalent) as the main currency.
  - `cargo_capacity` (volume/mass).
  - Optional: `fuel` as a soft gate for long-distance travel (can be added later).
- Ensure the tactical layer doesn’t secretly depend on strategic ammo counts.

### 1.3 Open Questions / Later

- Do we want any **ongoing “ship upkeep”** equivalent (soft, not BB-style tax)?
- Do we want a hard **fuel** mechanic or keep travel cost purely time/credits-based?
- How granular should cargo tags be (handful of high-impact tags vs many flavor tags)?

---

## 2. Contract / Job Model

### 2.1 Decisions (v0)

- Contracts are **not the only way to play**, but:
  - In the first iteration they will be the **main structured loop** for low-agency players.
- **Deadlines exist but are soft**:
  - Expressed in **days**, but failure is about missed opportunities and rep hits, not instant game-over.
- **Difficulty is NOT explicitly shown**:
  - Players infer risk through contextual cues (faction, location, pay, rumors).
  - System design should support **“small win and bail out”**:
    - Optional objectives.
    - Partial payouts.
    - Escape routes instead of all-or-nothing fail states.
- Contracts may have **encoded outcomes**, especially for chains:
  - Completing or failing a contract can:
    - Move a contract chain to the next step.
    - Change localized world state (e.g. security level, patrol density).
    - Adjust faction reputation.

### 2.2 Contract Shape (v0)

At minimum, a contract has:

- `issuer_faction`
- `location_origin`, `location_target`
- `contract_type` (delivery, escort, raid, heist, extraction, etc.)
- `primary_objective` and optional `secondary_objectives`
- `base_reward` (+ optional bonuses for secondary objectives)
- `deadline_days` (soft)
- Optional: `chain_id` + `stage_index`

### 2.3 Implementation Notes

- Avoid making contracts the only glue by:
  - Allowing **free exploration** to trigger encounters and opportunities.
  - Letting world simulations generate **unsolicited offers** or emergent missions.
- Chains should be representable as simple **state machines**:
  - `chain_id`, `current_stage`, `stage_conditions`, `next_stage_on_success`, `next_stage_on_fail`.
- Partial success / bail-out should be normal:
  - Contracts define **minimal success** vs. **stretch goals**.
  - “You bit off more than you can chew but got a small win and survived” is a core fantasy.

### 2.4 Open Questions / Later

- How many **contract archetypes** do we need for v0?
- How often should **chain contracts** appear vs one-offs?
- Do we want **“hidden clauses” / secret consequences** in the first iteration or keep everything explicit?

---

## 3. Crew Model & Traits

### 3.1 Core Stats (v0)

Make this **moddable**, but start with a concise, legible set.

**Primary attributes**

- **Grit** – physical robustness, HP, resistance to injury.
- **Reflexes** – initiative, dodge, reaction checks.
- **Aim** – accuracy with ranged weapons.
- **Tech** – hacking, repairs, interacting with machinery.
- **Savvy** – social checks, negotiation, reading people.
- **Resolve** – stress tolerance, panic resistance, morale checks.

**Derived examples (non-exhaustive)**

- `max_hp` (from Grit + level)
- `initiative` (from Reflexes)
- `ranged_accuracy` (Aim)
- `tool_effectiveness` (Tech)
- `talk_checks` (Savvy)
- `stress_threshold`, `panic_resistance` (Resolve)

### 3.2 Traits

- Traits are **binary flags** with mechanical impact.
  - e.g. `Brave`, `Cautious`, `Reckless`, `Merciful`, `Vengeful`, `Smuggler`, `Ex-Military`.
- Traits **can change**:
  - Through events, mission outcomes, injuries, or long-term arcs.
- There are **permanent injuries** as a separate system:
  - e.g. “Damaged Eye” → -Aim, “Shattered Knee” → -Reflexes, movement penalties.
- Design assumption: crew are **more precious than Battle Brothers**:
  - Losing a veteran is a significant emotional event.
  - There will later be equipment or tech that lets you **save a character after apparent death** (cloning, cybernetics, emergency stasis, etc.).

### 3.3 Implementation Notes

- Separate **personality traits** vs **injury traits** in code/data.
- Traits should be **referenced by ID** so modders can add/remove easily.
- Build hooks now for:
  - Trait-gated events and dialogue.
  - Trait-based modifiers to contract offers (e.g. smugglers get smuggling jobs).
- Relationships between crew are **not required for v0**, but:
  - Document a placeholder extension point (e.g. `relationship_tags`) so it doesn’t clash later.

### 3.4 Open Questions / Later

- Exact starting stat ranges and growth curves.
- How many traits should a typical crew member have at start vs acquired over time.
- How “loud” should traits be in gameplay vs mostly flavor.

---

## 4. World Metrics & Faction State

### 4.1 Decisions (v0)

- There is **one reputation score per faction** for now.
- Systems and factions also have **simulation stats** (examples):
  - System-level: `stability`, `security_level`, `law_enforcement_presence`, `criminal_activity`, `economic_activity`.
  - Faction-level: `military_strength`, `economic_power`, `influence`, `desperation`, `corruption`.
- These metrics should be **visible enough** that players can form mental models:
  - At least rough descriptions (“High Security”, “Lawless”, “Booming Economy”).

- Metrics should **not heavily affect prices yet**:
  - Maybe small base modifiers (e.g. lawless + piracy → higher prices for weapons; safe trade hub → cheaper fuel/repairs).
  - Stronger ties to prices can be added later.

### 4.2 Implementation Notes

- Reputations and metrics are a **shared foundation** for:
  - Contract generation.
  - Encounter likelihoods and types.
  - Patrols / piracy risk.
  - Event triggers.
- Make metrics **coarse but impactful**:
  - Integers with small ranges (e.g. 0–5 tiers) instead of continuous 0–100 if possible.
- UI:
  - At minimum, show **reputation** and some **system tags** in the travel/world UI: “High Security”, “Smuggler Hotspot”, etc.

### 4.3 Open Questions / Later

- How quickly should reputations change?
- Do metrics interact globally (e.g. galactic war) or mostly locally?
- How much should player be able to **intentionally manipulate** these stats vs just react to them?

---

## 5. Time Model

### 5.1 Decisions (v0)

- Time is modeled in **days** at campaign level.
- Time should feel **non-harsh**:
  - Deadlines exist but allow for **taking multiple jobs and then choosing what to drop**.
- **Time only passes during actions**:
  - Travel between locations.
  - Rest/downtime actions.
  - Performing missions.
  - Possibly some management actions (refit, repairs).

No time passes while:

- Browsing UI, inspecting characters, reading tooltips, thinking.

Rest/downtime is an **explicit action**:

- Benefits: healing injuries, reducing stress, repairing ship, triggering low-intensity events.
- Cost: time passes; deadlines advance; some opportunities may expire.

### 5.2 Implementation Notes

- Contracts have deadlines in **absolute days from acceptance**, but:
  - Failing mostly affects **rep and future opportunities**, not immediate catastrophic losses.
- Avoid designing the economy around “profit per day” optimization:
  - Rewards and costs should be tuned such that **interesting decisions**, not grind, drive progress.

### 5.3 Open Questions / Later

- Do we need finer granularity than days (e.g. hours) or is day-level enough for campaign?
- Do certain actions need **minimum durations** to avoid exploits (e.g. 0-day micro-rests)?

---

## 6. RNG & Determinism

### 6.1 Decisions (v0)

- Campaign layer aims for **somewhat strict determinism**:
  - Given a campaign seed and the same decisions in the same order, you should get (mostly) the same outcome.
- Use **separate RNG streams**:
  - `campaign_rng` for world sim, contracts, encounters.
  - `economy_rng` for prices and trade events (optional).
  - `tactical_rng` for combat rolls, AI decisions.
- Player-facing **campaign seed**:
  - Seed is visible and can be reused to replay a galaxy.
- Tactical layer can be **less deterministic**:
  - We accept more run-to-run variation here; campaign determinism focuses on macro-level structure.

### 6.2 Implementation Notes

- Ensure that **UI-only actions** don’t consume RNG.
- Make sure mission generation is tied to **explicit triggers** (travel, dock, rest, day tick), not arbitrary background noise.
- Store the campaign RNG state with the savegame.

### 6.3 Open Questions / Later

- Do we want any **fixed daily rolls** for background events, or keep everything tied to explicit actions?
- Do we want to expose more debug information (e.g. seed + “show me why this event happened”) for power users/modders?

---

## 7. Mission / Session I/O Boundary

### 7.1 Inputs to a Mission

A mission instance is created with:

- Snapshot of **crew state** (stats, traits, injuries, loadout).
- Snapshot of **ship state** (relevant to the mission, if any).
- **Contract context**, if mission is contract-driven:
  - Contract ID, objectives, issuer, rewards, deadlines.
- **Location context**:
  - System/zone identifiers.
  - Environmental tags (law_level, security_level, etc.).
- Any **pre-applied campaign events** that modify the mission (e.g. “Ambush on arrival”).

### 7.2 Outputs from a Mission

After mission resolution, the mission returns:

- **Crew changes**:
  - XP gains.
  - Injuries (including permanent ones).
  - Deaths.
  - Trait changes (if any).
- **Inventory / cargo updates**:
  - New loot/trade goods (with tags).
  - Consumed items (if you later implement consumables).
- **Contract state changes**:
  - Success, partial success, explicit failure, or abort.
  - Which objectives were completed.
  - Resulting payout, if any.
- **World & faction deltas**:
  - Reputation changes (by faction).
  - Local metrics adjustments (security, criminal activity, etc.).
- **Follow-up hooks**:
  - IDs for triggered events or contract chain stages.

### 7.3 Implementation Notes

- Keep the I/O boundary **narrow and explicit**:
  - Mission code should not directly mutate global campaign state; it should emit a **delta** that the campaign layer applies.
- Mid-mission branching is allowed:
  - Multiple exits (escape early, side-exit, complete everything).
  - Each exit path produces its own distinct output delta.
- Irreversible, campaign-scale events are **not required for v0**, but the I/O structure should allow for them later:
  - e.g. “Station destroyed” as a high-level world delta.

### 7.4 Open Questions / Later

- Which exact **world deltas** are supported in v0 (e.g. can a mission remove/replace a location)?
- How much of this is exposed to modders vs internal only?

---

## 8. How Other Docs Should Use This

To avoid context rot and duplication:

- At the top of each domain doc, add a small **Dependencies** block, for example:

  - `WORLD_DOMAIN.md`:
    - “Depends on: CAMPAIGN_FOUNDATIONS.md sections 2 (Contracts), 4 (World Metrics), 5 (Time), 6 (RNG).”
  - `TRAVEL_DOMAIN.md`:
    - “Depends on: CAMPAIGN_FOUNDATIONS.md sections 1 (Resources, cargo), 4 (World Metrics), 5 (Time).”
  - `MANAGEMENT_DOMAIN.md`:
    - “Depends on: CAMPAIGN_FOUNDATIONS.md sections 1 (Resources), 3 (Crew), 5 (Time).”
  - `ENCOUNTER_DOMAIN.md`:
    - “Depends on: sections 2 (Contracts), 3 (Traits), 4 (World Metrics), 6 (RNG).”
  - `TACTICAL_DOMAIN.md`:
    - “Depends on: sections 3 (Crew stats/traits), 6 (RNG), 7 (Mission I/O).”
  - `SYSTEMS_FOUNDATION.md`:
    - Can largely treat this doc as **imported** and only talk about cross-system patterns.

- When a domain needs to restate something from here, it should:
  - Reference the section (“see CAMPAIGN_FOUNDATIONS 3.1”) and only explain what’s *domain-specific* about it.

---

## 9. Roadmap Integration

- You can keep your existing **global roadmap** as-is.
- This doc doesn’t invalidate it; it just **clarifies the target** those roadmap items are aiming at.
- Practical suggestion:
  - Add one roadmap item early: “Implement Campaign Foundations v0 (as per CAMPAIGN_FOUNDATIONS.md) to the extent needed by [WORLD, TRAVEL, MANAGEMENT v0].”
  - After that, roadmap items can simply say:
    - “Extend crew system (see CAMPAIGN_FOUNDATIONS 3.x).”
    - “Deepen contract chains (see 2.x).”

---
