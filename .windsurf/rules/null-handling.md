---
trigger: always_on
---

## Godot C# – Component / Null-Handling Rules

### 1. General Principles

- Avoid scattered `if (x != null)` checks.
- Treat most nulls as **lifecycle / usage bugs**, not runtime “maybe” states.
- Make invariants explicit and enforce them early (at `_Ready` or during construction).

---

### 2. Child Nodes (Sprite, SelectionIndicator, etc.)

**These are structural dependencies and must exist if the scene is valid.**

Rules:

- Always fetch with `GetNode<T>()` in `_Ready`.
- Do **not** use `GetNodeOrNull` for required children.
- After `_Ready`, assume non-null. No null checks in other methods.
- If you want safety, assert once in `_Ready` and fail loudly if missing.

Pattern:

- One-time binding in `_Ready`.
- After that, treat child fields as non-nullable invariants.

---

### 3. External Models / Components (e.g. `Actor`)

These are injected from outside (via `Setup`, factories, etc.). Handle explicitly.

#### 3.1 Required model

If the view is meaningless without the model:

- Add an explicit initialization step (`Setup`).
- Keep a flag like `isInitialized` (or similar) that is set in `Setup`.
- In any method that relies on the model:
  - Either assume `isInitialized == true` (and assert in debug), or
  - Early-return if not initialized (for `_Process` etc.).
- Never silently “fake” values (no “`return -1` if null”); missing model is a bug.

**Goal:** no repeated `if (actor != null)`; instead: a single, clear invariant:
> “Once initialized, `actor` is never null.”

#### 3.2 Genuinely optional model

If “no model yet” is a real, intended state:

- Represent it with an explicit state flag, e.g. `hasActor`, not ad-hoc null checks.
- At the top of methods that depend on it, use **one** early-return:
  - `if (!hasActor) return;`
- Internally treat the model as non-null when the flag says it’s present.

**Goal:** null means “not set yet” only as an implementation detail; logic branches on a named state.

---

### 4. Instantiation Pattern

When possible:

- Use a factory/helper to instantiate scenes and **immediately** call `Setup` before adding them to the game world.
- That lets you rely on “constructed = initialized” and reduces the need for flags.

---

### 5. Practical Summary

1. **Child nodes**: required → bind in `_Ready` with `GetNode<T>()`, no later null checks.
2. **Injected models/components**:
   - Required → `Setup` + invariant/flag, no scattered null checks, fail fast on misuse.
   - Optional → explicit state (`hasX`), single early-return guard, not repeated `x == null` checks.
3. Prefer clear invariants and state flags over defensive “maybe null” code.