# src/scenes/menu/ - Main Menu

Entry point UI for the game.

## Files

- **MainMenu.tscn** - Scene file
- **MainMenu.cs** - Menu UI with buttons for campaign/sandbox/quit
- **CampaignOverScreen.tscn** - Campaign over scene file
- **CampaignOverScreen.cs** - Game over screen showing final stats (missions, money, deaths) with restart options

## Responsibilities

- Display game title
- Provide entry points: Start Campaign, Test Mission (sandbox), Quit
- Display campaign over screen when all crew are lost
- Show final campaign statistics
- Call GameState methods for navigation

## Dependencies

- **Imports from**: `src/core/GameState`
