# Travel Domain

## Purpose

The Travel domain handles movement of the player’s ship across the galaxy map and the passage of time associated with that movement. It turns route choices and movement commands into time costs, resource consumption, and potential encounters.

## Responsibilities

- **Route planning and validation**:
  - Compute viable paths between systems based on world topology.
  - Respect blocked routes, range limits, and special constraints.
- **Travel execution**:
  - Turn a chosen route into a sequence of travel segments.
  - Advance time along these segments.
  - Consume travel-related resources (fuel, supplies, etc.).
- **Risk and encounter hooks**:
  - For each travel segment, compute:
    - Encounter risk based on Simulation-provided probabilities.
    - When to roll for an encounter.
  - Trigger encounter generation when thresholds are met.
- **Patrols and special activity**:
  - Use Simulation’s probability fields and world metrics to:
    - Increase chance of patrols in high-security space.
    - Increase chance of raids/pirates in low-security space.
- **Feedback into other domains via events**:
  - Emit “arrived at system”, “encounter triggered”, “ran out of fuel”, etc.

## Non-Responsibilities

- Does not simulate economy or factions.
- Does not generate encounter content:
  - It only decides *that* an encounter happens and passes context to the Encounters/Generation domains.
- Does not manage detailed inventory or crew state:
  - It only consumes/requests resource usage via Management.
- Does not own world topology:
  - It queries World for systems and routes.

## Inputs

- **World data** (via World domain):
  - Systems, routes, distances, route tags.
- **Simulation metrics**:
  - Security level, pirate activity, patrol intensity per system/route.
- **Management data**:
  - Current ship status (fuel, speed modifiers, cargo load).
  - Travel-related upgrades (better engines, stealth drives).
- **Player intent**:
  - Target system / route choice.
  - Route preferences (safe vs fast, avoid certain factions, etc.).
- **Time system**:
  - Current game time and interface for advancing time.

## Outputs

- **Travel plans**:
  - Selected route, estimated time, fuel/supply cost, risk estimate.
- **Progress updates**:
  - Current segment, percent complete, ETA.
- **Resource consumption events**:
  - Fuel and supplies decrements.
- **Encounter triggers**:
  - Requests to Encounters/Generation to instantiate an encounter.
- **Arrival events**:
  - “Arrived at System X”, including summary of what happened en route.

## Key Concepts & Data

- **TravelSegment**:
  - A single step along a route (system A → system B, or a sub-step within that).
  - Distance, base time, base fuel cost.
  - Modifiers from world tags (nebula, dangerous route) and Simulation metrics.
- **TravelPlan**:
  - Ordered list of segments.
  - Computed total time, total cost, aggregate risk.
- **RiskProfile**:
  - Encapsulates:
    - Encounter probability per unit time or segment.
    - Encounter intensity weighting (patrol vs raiders vs anomalies).
- **TravelState**:
  - Where the ship is on the current segment.
  - How much time has passed.
  - Pending or resolved encounter status.

### Invariants

- Travel always respects world topology:
  - No movement across non-existent or forbidden edges.
- Time, distance, and fuel consumption are consistent:
  - No “teleporting” unless explicitly allowed by mechanics.
- Risk calculation is reproducible:
  - Given the same world/simulation state, travel plan, and seed, the same risk profile is produced.

## Interaction With Other Domains

- **World**:
  - Reads systems and routes for pathfinding and route validity.
  - Reads route and system tags that influence base travel cost and risk.
- **Simulation**:
  - Reads security, piracy, and patrol intensity metrics to compute risk profiles.
  - Travel events (e.g. piracy by player) can generate events that Simulation consumes to update probabilities.
- **Generation**:
  - When Travel decides an encounter should happen, it passes a TravelContext for Generation to create a concrete encounter instance.
- **Encounters**:
  - Travel triggers encounters; Encounters run them and return outcomes (damage, resource loss, time delay, etc.).
- **Management**:
  - Travel requests fuel/supply consumption and applies movement-related consequences (damage to ship, resource loss from encounters).
  - Ship upgrades feed back into Travel costs and risk modifiers.
- **Tactical**:
  - In rare cases, Travel may trigger a full tactical mission (ambush at a jump gate, blockade, etc.).
- **Systems Foundation**:
  - Uses time system to advance travel.
  - Uses RNG services for encounter rolls.
  - Uses event bus for travel-related events.

## Implementation Notes

- Pathfinding:
  - Use standard algorithms (e.g. Dijkstra/A*) over the world graph.
  - Cost function incorporates distance, time, fuel, and risk weights.
- Risk calculation:
  - Ideally expressed as pure functions of:
    - Segment data.
    - Simulation metrics.
    - Ship traits (stealth, speed).
  - Expose both a “preview” (for route choice UI) and “runtime” roll.
- Travel update loop:
  - Can be:
    - Abstracted (jump instantly, advance time in chunks), or
    - Simulated incrementally (for more granular chance to interrupt, pause, or react).
- Testing:
  - Validate that routes are consistent with world definitions.
  - Check that edge cases (no routes, out-of-fuel, hostile routes) behave predictably.
  - Statistical tests on encounter frequency vs design-intended curves.

## Future Extensions

- Multiple ships or fleets travelling simultaneously.
- Background NPC traffic simulation that occasionally surfaces into player encounters.
- Special travel modes:
  - Risky shortcuts, hidden routes, wormholes with unique rules.
- Player abilities that manipulate travel:
  - Forced marches, stealth transits, emergency jumps.
