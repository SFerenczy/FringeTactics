# src/scenes/sector/ - Sector Map View

Visual representation of the sector map for navigation.

## Files

- **SectorView.tscn** - Scene file
- **SectorView.cs** - Sector map interface
- **TravelAnimator.cs** - Visual travel animation (dot moving along route)
- **JobBoardPanel.cs** - Job board overlay panel
- **StationServicesPanel.cs** - Station services overlay panel

## Responsibilities

- Draw sector nodes as colored circles based on type
- Draw connections between nodes as lines
- Highlight current location (cyan with white border)
- Highlight selected node (yellow)
- Show node info: name, type, faction, travel cost
- Travel button (consumes fuel, moves player)
- Mission button (if at current location and can start)
- Ship & Crew button (goes to CampaignScreen)
- Show "!" indicator on nodes with jobs

## Node Colors

- Station: Green (safe)
- Outpost: Light Blue
- Derelict: Gray (risky)
- Asteroid: Orange (mining)
- Nebula: Purple (hiding)
- Contested: Red (dangerous)

## Dependencies

- **Imports from**: `src/core/GameState`, `src/sim/campaign/`
