# Fringe Tactics - Agent Guidelines

This file provides guidance for AI agents working on this codebase.

## Project Overview

Fringe Tactics is a tactical combat game built with Godot 4 and C#. It follows a hexagonal/ports-and-adapters architecture adapted for game development.

## Directory Structure

```
src/
├── core/       # Application layer - orchestration, game flow
├── scenes/     # Godot scenes and view scripts (adapters)
└── sim/        # Domain/simulation core - pure game logic
    ├── campaign/   # Strategic layer state
    ├── combat/     # Tactical combat simulation
    └── data/       # Configuration and data structures
```

## Architecture Rules

1. **Dependency direction**: Inner layers (sim) must not depend on outer layers (scenes)
2. **Sim is engine-light**: No Node, SceneTree, or UI references in `src/sim/`
3. **Scenes are adapters**: They reflect state, they don't define game rules
4. **Stateless services**: Combat resolution, economy, etc. take explicit parameters

## Maintaining agents.md Files

Each subdirectory contains its own `agents.md` explaining that directory's purpose and contents.

### Update Rules

When modifying code in a directory:

1. **Add new files**: Update the directory's `agents.md` to list and describe the new file
2. **Remove files**: Remove the file from the `agents.md` listing
3. **Change file purpose**: Update the description to reflect new responsibilities
4. **Add new directories**: Create a new `agents.md` in that directory

### agents.md Format

Each `agents.md` should contain:
- Brief description of the directory's role
- List of files with one-line descriptions
- Key patterns or conventions specific to that directory
- Dependencies (what it imports from, what imports it)

### When NOT to Update

- Minor implementation changes within a file
- Bug fixes that don't change file purpose
- Refactoring that preserves behavior

## Key Conventions

- **Naming**: PascalCase for classes, files, nodes; camelCase for private fields
- **Events**: Use C# events in sim layer, Godot signals only in scene layer
- **State**: Single source of truth for CampaignState and CombatState
