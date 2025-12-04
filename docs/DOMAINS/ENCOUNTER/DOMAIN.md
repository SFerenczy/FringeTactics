# Encounter Domain

## Purpose

The Encounter domain handles non-combat events and lightweight interactive scenes: text-based choices, skill checks, emergent narrative beats during travel or station life. It’s the layer that turns systemic state into specific narrative moments, without owning the simulation or the world itself.

## Responsibilities

- **Run encounters**:
  - Present an encounter’s structure (internally, not UI).
  - Process player choices and resolve outcomes.
- **Apply systemic context to encounters**:
  - Use world metrics (security, unrest, faction control).
  - Use simulation-derived probabilities.
  - Use crew traits and ship stats from Management.
- **Perform skill/trait checks**:
  - Determine which options are available or succeed.
  - Factor in crew stats, ship systems, and personality traits.
- **Produce outcome payloads**:
  - Resource changes, injuries, damage.
  - Reputation changes.
  - Time delays, future flags/hooks.
- **Drive emergent behaviour**:
  - Encounters should feel tied to the current system, faction, and crew, not generic.

## Non-Responsibilities

- Does not generate encounter templates/archetypes:
  - Generation domain picks templates and instantiates parameters.
- Does not own world or simulation state:
  - It reads context; it doesn’t run global updates.
- Does not render UI or text:
  - It gives a structured representation for the UI to display.
- Does not run tactical combat:
  - It may trigger Tactical missions as a choice or consequence but does not handle them.

## Inputs

- **Encounter instance** from Generation:
  - Template ID, text keys, choices, tags.
  - Embedded conditions and outcome definitions.
- **Context**:
  - Current system, station, route (from World/Travel).
  - Local metrics (security, pirate activity, unrest) from Simulation.
  - Crew, ship, and resource state from Management.
  - Player reputation and faction relationships.
- **Player choices**:
  - Which option the player selects at each step.

## Outputs

- **Encounter state progression**:
  - Current node, available options, success/failure results.
- **Outcome payload**:
  - Structured consequences:
    - Resource deltas (credits, fuel, supplies).
    - Crew changes (injuries, deaths, traits gained/lost).
    - Ship damage or repairs.
    - Reputation and relationship changes.
    - Time passed.
- **Events for other domains**:
  - “Trigger tactical mission X.”
  - “Flag storyline Y as active.”
  - “Apply world/faction modifiers via Simulation.”

## Key Concepts & Data

- **EncounterInstance**:
  - Concrete version of an encounter with parameters resolved from Generation.
  - Contains nodes, options, tests, and outcomes.
- **EncounterNode**:
  - One “step” of the encounter.
  - Has descriptive content (via keys), choice options, and optional automatic transitions.
- **EncounterOption**:
  - A possible choice with:
    - Conditions (traits, stats, resources, world state).
    - Optional skill checks or RNG tests.
    - Defined outcomes (success/failure branches).
- **EncounterOutcome**:
  - Atomic effects:
    - Add/remove resources/items.
    - Modify crew/ship.
    - Emit events.
    - End encounter or transition to another node.

### Invariants

- Encounters are finite:
  - No infinite loops unless explicitly designed that way.
- All referenced entities (crew, items, factions, systems) must exist.
- Conditions and outcomes are side-effect free until applied:
  - Easy to preview and debug.

## Interaction With Other Domains

- **World**:
  - Provides contextual tags and ownership for flavour and conditions.
- **Simulation**:
  - Supplies local conditions (security, unrest, activity) for encounter selection and weighting.
  - Receives events if an encounter has macro consequences (e.g., sabotage, political actions).
- **Generation**:
  - Supplies fully instantiated EncounterInstances.
  - May receive feedback for chained or follow-up encounters.
- **Management**:
  - Provides crew, ship, and resources for conditions and tests.
  - Applies outcome payloads to persistent state.
- **Travel**:
  - Triggers encounters during transit and incorporates time delays or diversions.
- **Tactical**:
  - Encounters may branch into a tactical mission and then continue based on combat results.
- **Systems Foundation**:
  - Uses RNG for checks and branching.
  - Uses event bus for communicating outcomes to other systems.

## Implementation Notes

- Encounter runtime should be a small **state machine**:
  - Current node + context → available options + resulting node.
- Represent conditions and outcomes as **data**:
  - Predicates and effects that operate over a shared context interface.
  - Enables tools and easier testing.
- Skill/trait checks:
  - Use clear formulas and expose them for tuning.
  - e.g., base difficulty vs crew stat, plus bonuses from traits/gear.
- Testing:
  - Unit tests for individual encounter templates.
  - Simulated runs over many contexts to ensure variety and avoid dead branches.

## Future Extensions

- Long-running or multi-stage encounters:
  - Chains of related encounters forming micro-stories.
- Persistent flags:
  - NPCs or factions remembering past choices.
- Crew relationship hooks:
  - Encounter outcomes affecting interpersonal dynamics, not just stats.
