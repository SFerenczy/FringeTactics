# Systems Foundation

## Purpose

The Systems Foundation domain provides core infrastructural services shared across all other domains: time, randomness, configuration, events, and persistence. It is the layer everything else stands on, but it should know as little as possible about game-specific logic.

## Responsibilities

- **Time system**:
  - Track current game time.
  - Provide APIs for advancing time in discrete steps.
  - Support “scaled” time (strategic vs tactical vs paused).
- **Randomness**:
  - Provide deterministic RNG services with seeding.
  - Offer scoped RNG streams for different systems (simulation, generation, encounters).
- **Event system**:
  - Provide an event bus / message system for domain-to-domain communication.
  - Ensure decoupling between producers and consumers of events.
- **Configuration and data loading**:
  - Load, validate, and expose configuration and content data.
  - Provide tools for reloading data in development.
- **Persistence**:
  - Save and load game state.
  - Versioning and migration for save formats.
- **Logging and diagnostics** (optional but recommended):
  - Central logging facilities.
  - Hooks for debug UI, replay tools, and simulation inspection.

## Non-Responsibilities

- Does not own any higher-level game logic (missions, encounters, combat).
- Does not decide gameplay rules or balance.
- Does not maintain world, simulation, or player state except for persistence snapshots.

## Inputs

- **Game state** from all domains (for saving).
- **Configuration files** and content data sources.
- **Events** emitted by all domains.
- **Engine/platform callbacks**:
  - Ticks, I/O, file handling.

## Outputs

- **Time information**:
  - Current time and delta times on request.
- **RNG services**:
  - Deterministic random values for domains that request them.
- **Events**:
  - Delivery of events to subscribers.
- **Loaded config and content**:
  - Parsed and validated data structures used by Generation, Simulation, etc.
- **Serialized game state**:
  - Save files and restored state.

## Key Concepts & Data

- **GameTime**:
  - Absolute timestamp (e.g., days/hours since campaign start).
  - Relative time deltas for updates.
- **RngStream / RngContext**:
  - Named or scoped RNG sources:
    - `simulation_rng`, `generation_rng`, `encounter_rng`, etc.
  - Ensures reproducibility and reduces cross-system correlation.
- **EventBus**:
  - Central registry where:
    - Domains can publish events.
    - Domains can subscribe to certain event types or channels.
- **ConfigStore**:
  - In-memory repository of loaded configuration and content data.
  - Provides typed accessors and validation errors.
- **SaveGame**:
  - Serialized representation of all domain states.
  - Includes versioning metadata.

### Invariants

- Event delivery does not create infinite loops by default:
  - Either enforced by design or guarded by mechanisms (e.g., max depth).
- RNG behaviour is stable given the same seeds and call order.
- Time is monotonically non-decreasing.
- Save/load round-trip preserves state (within reasonable precision).

## Interaction With Other Domains

- **World**:
  - Uses event bus for structural changes (e.g., station destroyed).
  - Included in save/load.
- **Simulation**:
  - Uses RNG and time system for ticks and stochastic events.
  - Emits summary events (wars, trade booms, collapses).
- **Generation**:
  - Uses RNG and config data heavily.
  - May use events to request previews or debug info.
- **Management**:
  - Uses persistence for player state.
  - May subscribe to events affecting player assets.
- **Travel**:
  - Uses time system to advance travel.
  - Uses RNG for risk rolls and encounter chances.
- **Encounters**:
  - Uses RNG for skill checks and branching.
  - Emits events for outcomes with broader consequences.
- **Tactical**:
  - Uses RNG for combat outcomes, if needed.
  - Included in save/load if tactical state must persist.

## Implementation Notes

- **Time**:
  - Central `GameClock` that advances in explicit steps.
  - Strategic systems (Simulation, Travel, Encounters) call into it rather than using real-time.
- **RNG**:
  - Consider exposing:
    - Seeded global RNG for campaign.
    - Substreams derived via known seeds (domain name + global seed).
  - Makes it easier to reproduce bugs and test content.
- **Events**:
  - Keep event types small and serializable.
  - Prefer explicit event structures over generic string-based messages.
  - Support both fire-and-forget and request/response patterns where needed.
- **Config**:
  - Validate on load and fail fast in development builds.
  - Provide debug tools to inspect loaded content and configs.
- **Persistence**:
  - Design a stable, versioned save format:
    - Explicit version field.
    - Migration paths when structures change.
  - Ensure all domain states can be serialized/deserialized without hidden dependencies.

## Future Extensions

- Replay/debug tooling:
  - Record event streams and RNG seeds to replay sessions.
- Mod support:
  - Extend config loading to support user-generated content.
- Live-tuning tools:
  - Hot-reload selected config (encounter tables, balance numbers) in development.
