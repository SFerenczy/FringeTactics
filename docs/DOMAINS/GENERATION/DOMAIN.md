# Generation Domain

**Dependencies**: CAMPAIGN_FOUNDATIONS.md sections 2 (Contracts), 4 (World Metrics), 6 (RNG).

## Purpose

The Generation domain creates concrete content instances (missions, encounters, maps, contracts) from systemic state and reusable templates. It is the main bridge between raw simulation/world data and the actual playable content presented to the player.

## Responsibilities

- **Galaxy and world initialization**:
  - Generate the initial galaxy layout, systems, stations, and factions based on seeds and configuration.
- **Mission generation** (see CAMPAIGN_FOUNDATIONS.md 2):
  - Create mission offers (contracts) based on:
    - World metrics (security, trade, instability).
    - Faction needs and relationships.
    - Player state (reputation, strength, location).
- **Map / level generation** for tactical missions:
  - Select map archetypes and parameterize them.
  - Place objectives, enemies, civilians, and props.
- **Encounter template selection and instantiation**:
  - For non-combat events, map simulation and travel context to encounter templates.
- **Contract generation** (see CAMPAIGN_FOUNDATIONS.md 2.2):
  - Define rewards, risks, constraints, and failure conditions.
- Remain as **pure as possible**:
  - Input: world/simulation snapshot + RNG seed.
  - Output: generated content objects.

## Non-Responsibilities

- Does not apply changes to the world or simulation state.
- Does not handle player UI or presentation of offers.
- Does not run tactical combat or encounter resolution.
- Does not simulate time or faction/economy changes.

## Inputs

- **World snapshot**:
  - Systems, factions, stations, ownership, metrics.
- **Simulation metrics**:
  - Security, piracy, trade, instability, faction wealth.
- **Player & Management state**:
  - Crew strength, ship capabilities, inventory, location, reputation.
- **Configuration / templates**:
  - Mission archetypes.
  - Map archetypes.
  - Encounter archetypes.
  - Contract templates.
- **RNG seed** and optional generation parameters:
  - Deterministic generation per campaign/mission.

## Outputs

- **Galaxy definition** at campaign start:
  - Systems, stations, routes, initial metrics.
- **Mission offers / contracts** (see CAMPAIGN_FOUNDATIONS.md 2.2):
  - Structured data describing objectives, rewards, factions involved, time limits, difficulty.
- **Tactical mission instances**:
  - Map layout data.
  - Spawn lists and behaviours for NPCs/enemies.
  - Objective placement and win/fail conditions.
- **Encounter instances**:
  - Concrete narrative or choice-based encounter definitions for Encounters domain to run.

## Key Concepts & Data

- **Archetype**:
  - A parameterized template (mission/map/encounter) with tags and constraints.
- **GenerationContext**:
  - Bundled inputs:
    - Current system.
    - Local metrics.
    - Factions present.
    - Player stats.
- **Difficulty & Risk models**:
  - Mapping from player power and world threat to generated content difficulty.

### Invariants

- Given the same seed and the same input state, generation is deterministic.
- Generated content always references valid world entities (systems, factions, stations).
- Generated missions and encounters are internally consistent (rewards vs risks vs difficulty).

## Interaction With Other Domains

- **World**:
  - Writes initial galaxy at campaign start.
  - Reads world state and metrics to generate contextually appropriate content.
- **Simulation**:
  - Reads metrics and faction states to bias mission/encounter types and frequencies.
- **Management**:
  - Reads player power and needs to ensure content is achievable but challenging.
- **Travel**:
  - Provides encounter templates for routes and regions.
- **Encounters**:
  - Supplies fully defined encounter instances for runtime execution.
- **Tactical**:
  - Supplies map/layout data and scenario setup for missions.
- **Systems Foundation**:
  - Uses RNG, config loading, and possibly serialization helpers.

## Implementation Notes

- Aim for **data-driven generation**:
  - Archetypes defined in external data (JSON/TOML/etc).
  - Logic uses tags and rules, not hard-coded IDs.
- Keep generation functions pure where possible:
  - Easier to test and debug.
  - Allows regeneration or previewing content in tools.
- Testing:
  - Snapshot tests for generated content given known seeds.
  - Statistical tests to ensure distributions (e.g. mission types) match design intentions.

## Future Extensions

- Story-aware generation:
  - Soft arcs or long-running plots expressed as constraints and biases.
- Region-specific content flavours:
  - Different mission styles and encounter tones per faction/region.
- Offline “world bake” tools:
  - Generating candidate galaxies and selecting interesting ones.
