# src/scenes/menu/ - Main Menu

Entry point UI for the game.

## Files

- **MainMenu.tscn** - Scene file
- **MainMenu.cs** - Menu UI with buttons for campaign, test missions, quit
- **TestMissionSelect.tscn** - Test mission selection scene
- **TestMissionSelect.cs** - Grid of test mission buttons (M0-M5, sandbox)
- **CampaignOverScreen.tscn** - Campaign over scene file
- **CampaignOverScreen.cs** - Game over screen showing final stats (missions, money, deaths) with restart options

## Responsibilities

- Display game title
- Provide entry points: Start Campaign, Test Missions, Quit
- Test mission selection with descriptions for each milestone
- Display campaign over screen when all crew are lost
- Show final campaign statistics
- Call GameState methods for navigation

## Dependencies

- **Imports from**: `src/core/GameState`
