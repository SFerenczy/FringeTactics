# src/scenes/campaign/ - Campaign Screen

Hub screen between missions showing campaign status.

## Files

- **CampaignScreen.tscn** - Scene file
- **CampaignScreen.cs** - Ship HQ interface with crew management

## Responsibilities

- Show all resources (money, fuel, ammo, parts, meds)
- Display mission cost requirements
- List crew roster with:
  - Status indicator (green=ready, orange=injured, red=dead)
  - Role, level, XP progress
  - Heal button for injured crew (costs 1 med)
- Start Mission button (disabled if can't afford or no crew)
- Show reason if mission blocked
- Abandon Campaign button (return to menu)

## Dependencies

- **Imports from**: `src/core/GameState`, `src/sim/campaign/CampaignState`
