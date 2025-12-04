# Systems Foundation Domain

**Dependencies**: CAMPAIGN_FOUNDATIONS.md sections 5 (Time), 6 (RNG), and 7 (Mission I/O).

## Purpose

The Systems Foundation domain provides core infrastructural services shared across all other domains: time, randomness, configuration, events, and persistence. It is the layer everything else stands on, but it should know as little as possible about game-specific logic.

## Responsibilities

- **Time system** (see CAMPAIGN_FOUNDATIONS.md 5.1):
  - Track current game time in days at campaign level.
  - Provide APIs for advancing time in discrete steps during actions.
  - Support "scaled" time (strategic vs tactical vs paused).
- **Randomness** (see CAMPAIGN_FOUNDATIONS.md 6.1):
  - Provide deterministic RNG services with seeding.
  - Offer scoped RNG streams for different systems (campaign_rng, economy_rng, tactical_rng).
  - Support player-visible campaign seed for replayability.
- **Event system**:
  - Provide an event bus / message system for domain-to-domain communication.
  - Ensure decoupling between producers and consumers of events.
- **Configuration and data loading**:
  - Load, validate, and expose configuration and content data.
- **Persistence**:
  - Save and load game state.
  - Versioning and migration for save formats.
- **Logging and diagnostics** (optional but recommended):
  - Central logging facilities.
  - Hooks for debug UI, replay tools, and simulation inspection.

## Non-Responsibilities

- Does not own game rules or domain logic.
- Does not handle UI or presentation.

## Inputs

- **Configuration files**: JSON/data files for weapons, abilities, enemies, etc.
- **Save files**: Serialized campaign state.
- **Domain events**: Events emitted by other domains for the event bus.
- **Time advancement requests**: From Travel, Tactical, Management.

## Outputs

- **Current time**: Campaign day, tactical tick.
- **RNG values**: Deterministic random numbers per stream.
- **Loaded config**: Typed data structures for game definitions.
- **Events**: Broadcast to subscribers.
- **Save data**: Serialized state for persistence.

## Key Concepts & Data

- **GameTime**:
  - `campaign_day`: Current day in the campaign.
  - `tactical_tick`: Current tick within a mission (reset per mission).
  - Time is monotonically non-decreasing.
- **RngStream / RngContext**:
  - `campaign_rng`: For world sim, contracts, encounters.
  - `tactical_rng`: For combat rolls, AI decisions.
  - Each stream stores its state for save/load.
  - RNG behavior is stable given same seeds and call order.
- **EventBus**:
  - Typed event registration and dispatch.
  - Domain isolation (domains don't directly call each other).
  - Event types are small and serializable.
- **ConfigStore / ConfigRegistry**:
  - Loaded definitions keyed by ID.
  - Validation on load with fail-fast in development.
  - Provides typed accessors and validation errors.
- **SaveGame**:
  - Serialized representation of all domain states.
  - Includes versioning metadata.

### Invariants

- Event delivery does not create infinite loops by default (enforced by design or max depth).
- RNG streams are isolated: consuming from one doesn't affect others.
- Time only advances through explicit actions, never implicitly.
- Config data is immutable after load.
- Save/load round-trip preserves state exactly.

## Interaction With Other Domains

- **All domains**:
  - Use RNG streams for randomness.
  - Use config registry for data definitions.
  - Participate in save/load.
- **Simulation**:
  - Subscribes to events via event bus.
- **Tactical**:
  - Uses tactical RNG stream.
  - Uses tactical tick time.
  - Emits events (actor died, mission completed).
- **Travel**:
  - Advances campaign time.
  - Emits events (arrived, encounter triggered).
- **Management**:
  - Emits events (resource changed, crew injured).

## Implementation Notes

- Keep Systems Foundation **engine-light**:
  - Core classes should not depend on Godot nodes.
  - Adapters can wrap for Godot integration.
- Time:
  - Central `GameClock` that advances in explicit steps.
  - Strategic systems (Simulation, Travel, Encounters) call into it rather than using real-time.
- RNG:
  - Consider exposing:
    - Seeded global RNG for campaign.
    - Substreams derived via known seeds (domain name + global seed).
  - Makes it easier to reproduce bugs and test content.
  - Methods: `NextInt`, `NextFloat`, `NextBool`, `Pick<T>`.
- Events:
  - Keep event types small and serializable.
  - Prefer explicit event structures over generic string-based messages.
  - Support both fire-and-forget and request/response patterns where needed.
  - Start simple (C# events or delegates).
  - Can evolve to more sophisticated patterns if needed.
- Config:
  - Use System.Text.Json or Godot's JSON parser.
  - Validate on load and fail fast in development builds.
  - Provide debug tools to inspect loaded content and configs.
- Persistence:
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
- Debug tools for RNG inspection.
- Async save/load for large campaigns.
