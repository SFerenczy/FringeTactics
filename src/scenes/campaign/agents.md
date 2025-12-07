# src/scenes/campaign/ - Campaign Screen

Hub screen between missions showing campaign status.

## Files

- **CampaignScreen.tscn** - Scene file
- **CampaignScreen.cs** - Ship HQ interface with crew management

## Responsibilities

### Core (G1)

- Show all resources (money, fuel, ammo, parts, meds)
- Display mission cost requirements
- List crew roster with:
  - Status indicator (green=ready, orange=injured, red=dead)
  - Role, level, XP progress
  - Heal button for injured crew (costs 1 med)
- Start Mission button (disabled if can't afford or no crew)
- Show reason if mission blocked
- Abandon Campaign button (return to menu)

### G2.5 Additions

- **MG-UI1** ✅: Crew roster with clickable entries showing name, role, key stats (Aim/Grit/Tech), status icon
- **MG-UI1** ✅: Crew detail panel showing:
  - Name, role, level/XP
  - All 6 core stats with base/effective values and trait modifiers
  - Traits list with color-coding by category and tooltips
  - Injuries list with effect descriptions
- **MG-UI2**: Allow firing/dismissing a crew member via Management APIs
- **MG-UI3**: Show equipment slots and allow equip/unequip from inventory
- Provide entry points to station/shop screens where applicable

## Dependencies

- **Imports from**: `src/core/GameState`, `src/sim/campaign/CampaignState`
