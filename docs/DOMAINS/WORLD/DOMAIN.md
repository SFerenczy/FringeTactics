# World Domain

**Dependencies**: CAMPAIGN_FOUNDATIONS.md sections 2 (Contracts), 4 (World Metrics), 5 (Time), 6 (RNG).

## Purpose

The World domain owns the canonical representation of the galaxy: its topology, locations, factions’ territorial control, and attached stateful metadata. It is the “single source of truth” for where things are and who owns what at any given time.

## Responsibilities

- Represent the **galaxy topology**:
  - Systems, routes, regions, and their connectivity.
- Represent **persistent locations**:
  - Stations, planets, outposts, points of interest.
  - Attached facilities (shops, bars, mission boards, etc.).
- Track **territorial control**:
  - Which faction owns which system/station.
  - Occupation states (contested, occupied, neutral).
- Store **world-attached metrics and tags** (see CAMPAIGN_FOUNDATIONS.md 4.1):
  - System-level: stability, security_level, law_enforcement_presence, criminal_activity, economic_activity.
  - Faction-level: military_strength, economic_power, influence, desperation, corruption.
  - Tags/flags describing characteristics: frontier, core, industrial, lawless.
- Provide **query APIs** for other domains:
  - “What stations are in this system?”
  - “What’s the security level here?”
  - “Which faction controls this region?”
- Maintain **stable identifiers**:
  - Unique IDs for systems, stations, factions, facilities, etc.
  - Stable across saves and references.

## Non-Responsibilities

- Does not simulate changes over time:
  - No economy or faction AI here.
  - No automatic updating of security, wealth, etc.
- Does not generate content:
  - No mission or map generation.
  - No encounter creation.
- Does not handle UI or player input.
- Does not resolve travel, combat, or encounters.

## Inputs

- **Initialization data**:
  - Either from Generation (procedural galaxy) or from static config (for development/testing).
- **Updates from Simulation** (see CAMPAIGN_FOUNDATIONS.md 4.1):
  - New values for world metrics (security, trade, activity, etc.).
  - Faction control changes.
- **Updates from other domains via events**:
  - Station destroyed, facility added/removed.
  - Faction spawned or eliminated.
- **Save/load system**:
  - Serialized representation of current world state.

## Outputs

- **Topological queries**:
  - Neighbours of a system.
  - Paths (or data for pathfinding) between systems.
- **Location queries**:
  - Stations in a system, their facilities, their owning faction.
- **Metric and tag queries** (see CAMPAIGN_FOUNDATIONS.md 4.1):
  - Security level, economic activity, pirate activity, unrest, tags.
- **Faction territory information**:
  - Systems per faction.
  - Regions with mixed control or contested states.
- **World snapshots**:
  - Bundled state for Simulation, Generation, Encounters, Management.

## Key Concepts & Data

- **System** (see CAMPAIGN_FOUNDATIONS.md 4.1):
  - Stable ID.
  - Position (for map layout).
  - Connections to other systems.
  - Metrics: stability, security_level, law_enforcement_presence, criminal_activity, economic_activity.
  - Tags: frontier, hub, border, etc.
- **Route**:
  - Edge between systems.
  - Attributes: distance, hazard modifiers, special properties (nebula, blockade-prone).
- **Station / Location**:
  - Belongs to a system.
  - Has facilities (shop, bar, mission board, repair yard).
  - Has an owning faction or neutral.
- **FactionTerritory**:
  - Systems and stations a faction controls.
  - Optional regional groupings.
- **WorldTags / Flags**:
  - Reusable classification bits (for Generation and Encounters).

### Invariants

- Graph topology is consistent:
  - No dangling references, bidirectional connections where required.
- IDs are unique and stable across saves.
- Metrics attached to systems/stations are always in defined ranges and types.
- Ownership relations are consistent:
  - A station’s owning faction must exist.
  - A system can’t be “owned” by a non-existent faction.

## Interaction With Other Domains

- **Simulation**:
  - Reads world topology and ownership.
  - Writes back metric changes (security, trade, activity, etc.) and territory shifts.
- **Generation**:
  - Reads world structure and metrics to generate missions, contracts, maps, and encounters.
  - May write initial galaxy at campaign start.
- **Management**:
  - Queries stations and facilities for shops, repairs, recruitment, etc.
  - Uses metrics (e.g. wealth, security) to drive prices/availability via Simulation-provided values stored in World.
- **Travel**:
  - Reads routes and system positions for pathfinding and route planning.
  - Uses tags and metrics to adjust travel difficulty/cost parameters.
- **Encounters**:
  - Queries local world context (system tags, controlling faction, security, unrest) to pick encounter types.
- **Tactical**:
  - Uses world context to parameterize mission setups (e.g. “corporate station in high-security core system”).
- **Systems Foundation**:
  - World state is part of global serialization.
  - Event bus can carry world-mutating events (e.g. “station destroyed”).

## Implementation Notes

- World can be implemented as:
  - A central **WorldState** object/service, or
  - A set of data modules with a well-defined query interface.
- Graph representation:
  - Use adjacency lists or similar; avoid hard-coding traversal logic outside this domain.
- Prefer **read-only views** for most consumers:
  - Mutation should go through well-defined APIs or event handlers.
- Testing:
  - Validate topology integrity.
  - Validate that queries behave correctly on both tiny test worlds and stress-test galaxies.

## Future Extensions

- Region concepts:
  - Named regions with shared tags and modifiers.
- Dynamic locations:
  - Moving fleets, mobile stations, temporary outposts.
- Special structures:
  - Warp gates, anomalies, unique megastructures with bespoke rules.
