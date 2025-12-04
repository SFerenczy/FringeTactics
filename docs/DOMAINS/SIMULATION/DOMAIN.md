# Simulation Domain

## Purpose

The Simulation domain evolves the strategic state of the galaxy over time. It models faction behaviour, economy changes, security levels, and activity probabilities in a fully systemic way.

## Responsibilities

- Maintain and update the **macro-state** of the galaxy:
  - Faction wealth, strength, and influence.
  - System-level metrics: security, pirate activity, trade volume, unrest.
- Run discrete or continuous **simulation ticks** to evolve the world based on:
  - Current world state.
  - Player actions (missions, piracy, trade).
  - Internal faction policies and relationships.
- Provide **probability fields** used by other domains:
  - Chance of random encounters during travel.
  - Chance and intensity of patrols, raids, blockades.
- Apply **macro consequences** of player actions:
  - Destroying trade ships reduces trade, wealth, and security.
  - Completing security contracts increases security and reduces pirate activity.
- Expose a **deterministic API** that can be stepped forward in time and queried.

## Non-Responsibilities

- Does not render UI or handle input.
- Does not manage tactical combat or mission-level details.
- Does not generate missions, encounters, or maps directly.
  - It only influences the probabilities and parameters that Generation and Encounters use.
- Does not own the canonical world topology (systems, stations, etc.).
  - It reads and writes metrics attached to world entities.

## Inputs

- **World snapshot**:
  - Systems, factions, ownership, existing metrics.
- **Event stream** from other domains:
  - Mission results (success, failure, collateral damage).
  - Ship destruction events, piracy events, trade deliveries.
  - Faction relationship changes.
- **Time progression**:
  - Simulated time deltas (`Δt` per tick).
- **Configuration data**:
  - Economic models, faction personalities, response curves.

## Outputs

- Updated **World metrics** per system/station:
  - `security_level`, `pirate_activity`, `trade_volume`, `unrest`, etc.
- Updated **faction state**:
  - Wealth, fleet strength (abstract), aggression, war states.
- **Probability fields** for:
  - Encounter generation during travel.
  - Patrol and raid intensity around specific locations.
- Optional **summary events**:
  - “Faction A increased security in System X.”
  - “Trade collapsed in Region Y.”

## Key Concepts & Data

- **SimulationTick**: A function that consumes events + current state and produces a new state.
- **SystemMetrics**:
  - `security_level: float`
  - `pirate_activity: float`
  - `trade_volume: float`
  - `instability: float`
- **FactionState**:
  - `wealth`, `military_power`, `desperation`, `policy` flags.
- **Response Curves**:
  - Functions describing how metrics respond to events.
  - Example: more piracy → more security investment → less piracy, with delays.

### Invariants

- Metrics remain in defined ranges (e.g. 0.0–1.0 or constrained integers).
- Simulation is deterministic given:
  - Initial state.
  - Event sequence.
  - RNG seed.

## Interaction With Other Domains

- **World**:
  - Reads topology and ownership.
  - Writes back metrics and faction states into world-attached structures.
- **Generation**:
  - Provides system and faction metrics that influence mission/contract templates.
- **Travel**:
  - Provides probability fields used when rolling for encounters during travel.
- **Encounters**:
  - Provides background context (e.g. unrest, security) to weight encounter types.
- **Management**:
  - Feeds economic consequences into prices, availability, and opportunities.
- **Tactical**:
  - Consumes high-level metrics (e.g. “this is a high-security outpost”) as parameters, not internal logic.
- **Systems Foundation**:
  - Subscribes to event bus for mission results and world events.
  - Uses time system for ticking.

## Implementation Notes

- Good candidate for a **Rust crate** exposed via FFI:
  - Simulation state as a serialized struct.
  - API: `init_state(seed)`, `apply_events(events)`, `tick(dt)`, `snapshot()`.
- Must be **testable in isolation**:
  - Property-based tests for stability and convergence.
  - Long-run simulations to verify emergent patterns (e.g. no runaway collapse unless configured).
- Performance:
  - Designed to handle many systems and factions cheaply per tick.

## Future Extensions

- More sophisticated faction AI:
  - Explicit strategies and doctrines.
- Regional events:
  - Plagues, resource discoveries, system-scale disasters.
- Feedback into narrative systems:
  - Unlocking unique encounters or storylines based on extreme metric states.
