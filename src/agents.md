# src/ - Source Code Root

All game source code lives here, organized by architectural layer.

## Subdirectories

- **core/** - Application layer: game flow, state orchestration, save/load
- **scenes/** - Adapter layer: Godot scenes, UI, input handling, rendering
- **sim/** - Domain layer: pure game logic, no engine dependencies

## Architecture Notes

Dependencies flow inward only:
- `scenes/` → `core/` → `sim/`
- Never the reverse

The `sim/` layer should be testable without running Godot.
