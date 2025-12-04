# Management Domain

## Purpose

The Management domain handles all player-facing strategic assets: crew, ship(s), resources, and inventory. It turns world/simulation consequences and tactical results into concrete changes to the player’s party and capabilities, without caring about presentation.

## Responsibilities

- **Crew management**:
  - Track crew members, their stats, traits, injuries, experience, and roles.
  - Apply personality traits as modifiers to stats, growth, and reactions.
  - Handle leveling, skill gains, and long-term progression.
- **Ship management**:
  - Track ship hull, subsystems, upgrades, and damage.
  - Process repairs, refits, and upgrades (subject to resources and facilities).
- **Inventory & cargo management**:
  - Track items, equipment, and cargo.
  - Enforce capacity constraints (no Tetris, but limits on totals).
  - Handle equipping, unequipping, selling, and buying.
- **Resource management**:
  - Track abstracted resources (credits, fuel, supplies, repair parts, etc.).
  - Apply costs and rewards from missions, encounters, and trade.
- **Apply consequences**:
  - Convert results from Tactical and Encounters into concrete changes:
    - Injuries, deaths, morale changes.
    - Damage to ship and equipment.
    - Gain/loss of items and resources.

## Non-Responsibilities

- Does not simulate factions or global economy.
- Does not generate missions, encounters, or tactical maps.
- Does not handle UI; only exposes state and operations.
- Does not own world topology or simulation metrics.

## Inputs

- **Tactical outcomes**:
  - Mission results (success/failure).
  - Casualty lists, wounds, damage, loot.
- **Encounter outcomes**:
  - Choices made and their direct consequences (e.g., fuel lost, crew injured, item gained).
- **World & Simulation context**:
  - Prices, availability, and constraints when trading or upgrading.
- **Player actions**:
  - Equip/unequip items.
  - Hire/fire crew.
  - Buy/sell items.
  - Repair and upgrade ship.
- **Configuration data**:
  - XP curves, injury rules, morale rules.
  - Item and equipment definitions.

## Outputs

- **Crew state**:
  - Current stats, traits, injuries, experience, roles, and availability.
- **Ship state**:
  - Hull integrity, subsystem condition, installed upgrades.
- **Inventory and cargo state**:
  - What the player is carrying and how close they are to capacity.
- **Resource totals**:
  - Credits, fuel, supplies, etc.
- **Summary events**:
  - “Crew member leveled up.”
  - “Ship upgraded.”
  - “Out of fuel.”

## Key Concepts & Data

- **CrewMember**:
  - Stats: combat ability, technical skills, social, etc.
  - Traits/personality: brave, cowardly, opportunistic, loyal, etc.
  - Status: active, injured, dead, missing.
  - Progression: level, XP, abilities.
- **Ship**:
  - Base chassis type.
  - Modules: weapons, engines, sensors, cargo, special systems.
  - Stats: hull, speed, detection, cargo capacity, fuel efficiency.
- **Inventory**:
  - Items with categories (weapon, armor, module, commodity, quest item).
  - Capacity model: simple limits (e.g., cargo units, equipment slots).
- **Resources**:
  - Currency and consumables, with clear units and sinks.

### Invariants

- Capacity constraints are always respected:
  - No exceeding cargo or equipment limits.
- Crew and ship states are internally consistent:
  - No dead crew assigned as active.
  - No non-existent items equipped.
- Progression rules are coherent:
  - XP → level → abilities follows a consistent model.

## Interaction With Other Domains

- **World**:
  - Uses station facilities to determine what management actions are possible (repair yard, shop, bar, recruitment).
- **Simulation**:
  - Consumes prices and availability influenced by simulation metrics.
  - Emits events (e.g., large purchases, selling rare goods) if needed.
- **Generation**:
  - Provides player power level and composition to guide mission difficulty and rewards.
- **Travel**:
  - Provides ship speed modifiers and fuel capacity/consumption.
  - Consumes fuel and supplies during travel.
- **Encounters**:
  - Provides crew traits and ship stats as context for encounter resolution.
  - Applies encounter consequences to crew, ship, and resources.
- **Tactical**:
  - Provides combat-ready crew, equipment, and ship stats.
  - Receives combat results and applies them as persistent changes.
- **Systems Foundation**:
  - Relies on event bus for updates and notifications.
  - Part of save/load state.

## Implementation Notes

- Represent Management state in a single **PlayerState** structure:
  - Crew, ship, inventory, resources in one place for saving/loading.
- Operations should be expressed as **commands**:
  - `ApplyMissionResult`, `ApplyEncounterOutcome`, `EquipItem`, `SpendResources`.
  - Easy to test and replay.
- Personality traits:
  - Represent as tags with clear rules for how they affect:
    - Stat modifiers.
    - Encounter options.
    - Morale and stress systems (if any).

## Future Extensions

- Deeper crew relationships:
  - Bonds, rivalries, mutiny risks.
- Multiple ships or an entire small fleet.
- More nuanced resource types:
  - Legal vs illegal goods, perishable vs durable.
