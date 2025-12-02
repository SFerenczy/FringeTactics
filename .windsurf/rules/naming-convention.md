---
trigger: always_on
---

```markdown
# Godot C# Naming Conventions (C# / Godot 4)

Opinionated but strict rules for a Godot C# codebase. No examples, only mappings.

---

## 1. Case Styles

- `PascalCase`
  - Namespaces
  - Classes, structs, interfaces, enums, delegates
  - Public and internal methods
  - Public and internal properties and events
  - Scene names (`.tscn`)
  - Node names in the scene tree
  - Autoload (singleton) names
  - Input action names (if not using `snake_case`)
  - Physics layer names
  - Group names
  - Top-level folders and logical module names

- `camelCase`
  - Local variables
  - Method parameters
  - Private and protected fields
  - Serialized fields (`[Export]`), even when effectively public
  - Private and protected methods (unless you prefer PascalCase for all methods; then use PascalCase consistently for every method)

- `SCREAMING_SNAKE_CASE`
  - `const` fields
  - `static readonly` fields that behave like constants
  - Global/static configuration values when not represented as resources

- `lower_snake_case`
  - Animation names (optional but recommended)
  - Input actions *if* you decide against PascalCase; choose one style and stick to it
  - Internal non-code identifiers that Godot treats as strings only (optional, but keep consistent)

---

## 2. Files, Folders, Scenes

- C# source files
  - File name: `PascalCase`
  - One main type per file; file name matches type name

- Scene files
  - File name: `PascalCase`
  - Name reflects the scene’s primary responsibility

- Resource files (`.tres`, `.res`)
  - File name: `PascalCase`

- Top-level folders
  - Folder names: `PascalCase`
  - Directory structure mirrors namespaces where practical

---

## 3. Nodes, Groups, Layers, Input, Animations

- Node names
  - `PascalCase`
  - Name by role or responsibility, not by concrete engine type
  - Indexed suffixes allowed but still `PascalCase`

- Groups
  - `PascalCase`
  - Use plural for collections; singular for roles when appropriate

- Physics layers and masks
  - Named in project settings using `PascalCase`
  - When mirrored in code (enums, flags, constants), maintain PascalCase and consistent bit flags

- Input actions
  - Either `PascalCase` or `lower_snake_case`
  - Pick one convention, no mixing

- Animation names
  - Prefer `lower_snake_case`
  - Treat as `StringName` constants in C# if you centralize them

---

## 4. Members, Signals, Async

- Fields
  - Private/protected: `camelCase`
  - Public fields should generally be avoided; use properties instead
  - Serialized fields: `camelCase` with `[Export]` attribute; avoid unnecessary public exposure

- Properties
  - `PascalCase` for all properties
  - Read-only or computed properties follow same rule
  - Booleans: express state positively (e.g. `is`, `has`, `can`, `should` prefixes are recommended)

- Methods
  - `PascalCase` for all methods (public, internal, private) if you choose the unified style
  - If you distinguish, see above: public/internal `PascalCase`, private/protected `camelCase`
  - Asynchronous methods that are part of a public API suffix with `Async`

- Signals
  - Signal names: `PascalCase`
  - Signal delegates: `PascalCase`
  - Signal handler methods: `PascalCase` and prefixed with `On` when wired to specific emitters (e.g. conceptual `On<Emitter><Signal>` pattern)

---

## 5. Constants, Enums, Collections, Booleans

- Constants
  - `SCREAMING_SNAKE_CASE`
  - Avoid magic numbers/strings in code; promote to constants when reused

- Enums
  - Enum type: `PascalCase`
  - Enum values: `PascalCase`
  - Flags enums: same, with bit-shift values

- Collections
  - Use clear plurals and descriptive names
  - Prefix is optional, but semantic clarity is mandatory (avoid `list`, `dict` as the only distinguishing part)

- Booleans
  - Name so that `if` conditions read naturally
  - Prefer positive meaning; avoid names that require double negation

---

## 6. Cross-Cutting Rules

- Abbreviations
  - Avoid unless widely understood; when used, follow normal casing rules (`HttpClient`, not all-caps abbreviations)
- Autoload singletons
  - Node name: `PascalCase`
  - Corresponding class: same `PascalCase` name; do not diverge
- Depth and complexity
  - Namespaces at most three or four levels deep in normal cases
  - Folder hierarchy mirrors namespace depth as closely as feasible

---
```
