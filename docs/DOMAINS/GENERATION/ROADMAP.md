# Generation Domain Roadmap

This document defines the **implementation order** for the Generation domain.

- G0 is concept/design only. Implementation starts in G1.
- Each milestone is a **vertical slice**.

---

## Overview of Milestones

1. **GN0 – Concept Finalization (G0)**
2. **GN1 – Contract Generation (G1)**
3. **GN2 – Galaxy Generation (G2)**
4. **GN3 – Encounter Instantiation (G2)**
5. **GN4 – Simulation-Aware Generation (G3)**

---

## GN0 – Concept Finalization (G0)

**Goal:**  
Finalize design decisions for contracts, archetypes, and generation context.

**Key deliverables:**

- Review and finalize CAMPAIGN_FOUNDATIONS.md section 2 (Contracts).
- Define initial contract archetypes:
  - delivery, escort, raid, heist, extraction, patrol.
- Define contract template structure:
  - Required fields, optional fields, reward formulas.
- Define `GenerationContext` structure:
  - What inputs generation needs (player state, world metrics, location).
- Document difficulty/risk model (how player power maps to content difficulty).

**Why first:**  
Generation is data-driven. Getting templates right avoids rework.

**Status:** Done (see `GN0_IMPLEMENTATION.md` for detailed breakdown)

---

## GN1 – Contract Generation (G1)

**Goal:**  
Generate mission offers for a single hub.

**Status:** ⏳ Pending (see `GN1_IMPLEMENTATION.md` for detailed breakdown)

**Key capabilities:**

- `ContractGenerator` class:
  - Input: `GenerationContext` (player state, hub metrics, RNG).
  - Output: List of `Contract` offers.
- `Contract` class:
  - `issuer_faction`, `location_target`.
  - `contract_type` (from archetypes).
  - `primary_objective`, `secondary_objectives`.
  - `base_reward`, `deadline_days`.
- Template-based generation:
  - Select archetype based on hub metrics.
  - Parameterize with player power and local context.
- Difficulty scaling:
  - Enemy count, enemy types based on player crew strength.

**Deliverables:**
- `ContractGenerator`, `Contract` classes.
- Initial contract templates (3-5 archetypes).
- Unit tests for generation determinism.
- Integration with Management (player power query).

---

## GN2 – Galaxy Generation (G2)

**Goal:**  
Generate the initial sector at campaign start.

**Key capabilities:**

- `GalaxyGenerator` class:
  - Input: Campaign seed, galaxy config.
  - Output: `WorldState` with systems, stations, routes, factions.
- System generation:
  - Positions, names, initial metrics.
  - Faction ownership.
- Route generation:
  - Connectivity graph.
  - Distance and hazard values.
- Station generation:
  - Facilities per station.
  - Initial inventory/prices (if applicable).

**Deliverables:**
- `GalaxyGenerator` class.
- Galaxy config format.
- Snapshot tests for deterministic generation.

---

## GN3 – Encounter Instantiation (G2)

**Goal:**  
Generate encounter instances for Travel and exploration.

**Key capabilities:**

- `EncounterGenerator` class:
  - Input: `TravelContext` (route, system tags, player state).
  - Output: `EncounterInstance` for Encounter domain to run.
- Template selection:
  - Based on route hazards, system tags, player cargo.
- Parameterization:
  - Fill in NPC names, cargo types, faction references.

**Deliverables:**
- `EncounterGenerator` class.
- Initial encounter templates (5-10).
- Integration with Travel domain.

---

## GN4 – Simulation-Aware Generation (G3)

**Goal:**  
Generation uses live simulation metrics to bias content.

**Key capabilities:**

- Contract generation responds to:
  - High piracy → more anti-pirate contracts.
  - Low security → more smuggling opportunities.
  - Faction desperation → higher rewards, riskier jobs.
- Encounter generation responds to:
  - Local unrest → more hostile encounters.
  - Trade volume → more merchant encounters.
- Statistical validation:
  - Distributions match design intentions.

**Deliverables:**
- Metric-aware generation logic.
- Statistical tests for distribution validation.
- Integration tests with Simulation.

---

## G0/G1/G2 Scope Summary

| Milestone | Phase | Notes |
|-----------|-------|-------|
| GN0 | G0 | Concept only |
| GN1 | G1 | Contract generation |
| GN2 | G2 | Galaxy generation |
| GN3 | G2 | Encounter instantiation |
| GN4 | G3 | Simulation integration |
