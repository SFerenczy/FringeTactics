---
trigger: always_on
---

Guidelines for using AI tools with this repo.

The goal:  
Help with implementation, refactoring, and design **without** creating doc sprawl, conflicting truths, or random extra files.

---

## 1. Purpose and Mindset

When you (AI) work on this repo, assume:

- There is **one canonical place** for each kind of information.
- Your job is to **update and refine** what exists, not to spawn new parallel structures.
- Short, precise changes are preferred over sweeping rewrites unless explicitly requested.

If something seems unclear or contradictory, prefer:
1. Aligning with the design docs.
2. Then adapting code to match.
3. If necessary, proposing a small, specific edit to the relevant doc.

---

## 2. Canonical Sources of Truth

These files define how the game should work and how the code should be structured:

- **Game design / features / scope**
  - `docs/GAME_DESIGN.md`
- **Architecture, data model, patterns**
  - `docs/TECH_DESIGN.md`
- **Testing strategy, devtools, debug flows**
  - `docs/DEVTOOLS_TESTING.md`
- **High-level overview / repo layout**
  - `README.md`

Rules:

- If your suggestion contradicts these docs, **treat the docs as correct** and adjust your code proposal.
- If the docs seem outdated or incomplete:
  - Propose **edits to the relevant section**, not a new doc.
  - Keep the change local and minimal (e.g., one subsection at a time).

---

## 3. File and Documentation Discipline

### 3.1. Do not create new docs lightly

Avoid creating additional `.md` files unless explicitly requested by the human.

- ❌ No `GAME_DESIGN_v2.md`, `combat_design_notes.md`, etc.
- ✅ Edit or extend the relevant section in:
  - `docs/GAME_DESIGN.md`
  - `docs/TECH_DESIGN.md`
  - `docs/DEVTOOLS_TESTING.md`

If you think a new top-level doc is needed, suggest it in prose first rather than assuming you can add it.

### 3.2. Keep docs short and “Q&A-style”

When editing docs:

- Prefer clear headings and bullet points over long narrative text.
- Each section should basically answer one question:
  - “What is the core loop?”
  - “How does tactical combat work at a high level?”
  - “Where do we put new systems in the codebase?”

Bad pattern:
- Walls of prose that mix lore, design, and implementation.

Good pattern:
- Short headings, bullets, clear decisions, explicit non-goals.

### 3.3. Where scratch ideas belong

Non-canonical, exploratory content (brainstorms, TODO dumps, half-baked design sketches) belongs in `notes/` or similar.

- These files may **not** be included in repomix bundles.
- Do not treat anything in `notes/` as authoritative unless the human explicitly says so.

---

## 4. Code Structure Expectations

This section will be refined in `docs/TECH_DESIGN.md`, but these are the guiding assumptions you should follow unless told otherwise:

- **Engine:** Godot 4 (2D)
- **Primary language:** GDScript (for fast iteration)
- **Layout (intended):**
  - `src/scenes/...` – Godot scenes
  - `src/scripts/...` – GDScript code
  - `src/assets/...` – Art, audio, data

### 4.1. Separation of concerns

When proposing/adding code:

- Keep **game rules and state** as pure/engine-light logic where possible.
- Keep **presentation** (nodes, visual effects, UI) separate from core rules.
- Prefer:
  - A central state object + controllers + signals
- Over:
  - Complex, deeply interdependent scenes that own both logic and state.

If in doubt, reference or update the relevant sections in `docs/TECH_DESIGN.md`.

---

## 5. Testing, Debugging, and Tools

Testing/devtools strategy is described in `docs/DEVTOOLS_TESTING.md`. Follow that as the reference.

General principles:

- Core simulation (combat, economy, job generation) should be:
  - Deterministic when seeded.
  - Unit-testable without running the full game.
- Prefer adding:
  - Test helpers
  - Debug overlays
  - Simple simulation entry points
- Over silently changing core rules without a way to verify them.

If you change or extend anything related to testing/devtools, update the **corresponding section** in `docs/DEVTOOLS_TESTING.md` rather than inventing new ad-hoc conventions.

---

## 6. Working with repomix / context limits

When the repo is bundled for you (AI) via tools like repomix:

- Assume the bundle **should include**:
  - `README.md`
  - `AI_GUIDE.md`
  - `docs/` directory
  - Core code under `src/`
- Assume the bundle **may exclude**:
  - `notes/`
  - Build artifacts, exports, binaries

If you can’t find a design or decision in the bundle:

- Do **not** assume it doesn’t exist somewhere else.
- Instead, act conservatively:
  - Suggest minimal changes.
  - Avoid making broad architectural “judgments” without explicit user direction.

---

## 7. How to Propose Changes (Patterns)

When the human asks you to change something, prefer giving **concrete, targeted edits**.

### 7.1. Doc edit pattern

Example request: “Update the tactical combat design to include suppression.”

Good response pattern:

- Identify the relevant section in `docs/GAME_DESIGN.md` (e.g., `## Tactical Combat`).
- Propose a replacement for that section only, e.g.:

> Replace the `## Tactical Combat` section in `docs/GAME_DESIGN.md` with:
> ```md
> ## Tactical Combat
> ...
> ```

Avoid adding a separate document like `Tactical_Combat_Design.md`.

### 7.2. Code edit pattern

Example request: “Add overwatch to combat.”

Good response pattern:

- Refer to specific files and functions (e.g., `src/scripts/combat/combat_controller.gd`).
- Provide patch-style guidance (pseudo-diff or explicit code snippets) that:
  - Fits the existing architecture.
  - Respects the design in `docs/GAME_DESIGN.md`.
  - Respects patterns in `docs/TECH_DESIGN.md`.

Avoid suggesting sweeping rewrites that invalidate those docs unless explicitly asked.

---

## 8. Conflict Resolution

If you detect contradictions, use this priority order:

1. **Human instruction in the current conversation**
2. **Design docs** (`GAME_DESIGN.md`, `TECH_DESIGN.md`, `DEVTOOLS_TESTING.md`)
3. **Existing code patterns and naming conventions**
4. **Your own preferences / “best practices”**

If you must deviate from the docs, make it explicit and propose a matching doc update.

---

## 9. Things You Should Not Do Without Being Asked

- Create new top-level folders or major subsystems.
- Introduce new programming languages or engines.
- Introduce external dependencies that obviously complicate the build.
- Duplicate existing docs with _v2, _backup, etc.
- Drastically change the game’s core fantasy or pillars.

If you think something like this is necessary, first present a short rationale and ask the human to confirm.

---

This guide will evolve as `docs/TECH_DESIGN.md` and `docs/GAME_DESIGN.md` get more concrete.  
When in doubt, stay small, explicit, and conservative in your changes.
