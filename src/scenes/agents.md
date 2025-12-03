# src/scenes/ - Adapter Layer (Godot Scenes)

Godot scenes and their scripts. This is the "outside" of the hexagonal architecture.

## Files

- **GridConstants.cs** - Shared constants for grid rendering (TileSize, colors for tiles and cover indicators)

## Subdirectories

- **menu/** - Main menu (game entry point)
- **sector/** - Sector map view (travel between nodes)
- **campaign/** - Ship/crew management screen
- **mission/** - Tactical combat view and related UI

## Responsibilities

- Render game state visually
- Handle player input and translate to commands
- Play animations, sounds, visual effects
- Never define game rules—only reflect state from sim

## Dependencies

- **Imports from**: `src/core/`, `src/sim/`
- **Imported by**: Nothing (outermost layer)

## Conventions

- Each scene has a `.tscn` file and a corresponding `.cs` script
- Scripts subscribe to sim events and update visuals accordingly
- Input flows: Player → Scene → Core/Sim command
- Use Godot signals for intra-scene communication only
