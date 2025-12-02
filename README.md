# Fringe Tactics

Firefly-inspired crew management meets Battle Brothers–style lethal, turn-based tactics.  
You captain a small, scrappy ship on the fringe, taking risky jobs for dubious factions to keep your misfit crew alive and paid.

> Status: Early design & prototyping. Expect everything to change.

---

## Core Fantasy & Pillars

**Fantasy:**  
Run a barely-legal ship on the edge of civilized space. Take shady contracts, juggle debts and loyalties, and survive brutal skirmishes where every injury and death actually matters.

**Design pillars:**

- **Small crew, big consequences** – A handful of named characters, high attachment, no disposable fodder.
- **Jobs are morally gray** – Factions are self-interested; “good choices” still hurt someone.
- **Lethal but readable tactics** – Simple, transparent rules; bad decisions kill, not opaque mechanics.
- **Running on fumes** – Money, fuel, and hull are always tight. Plenty of “we’re almost out of X” moments.
- **Systems over scripts** – Replay comes from interacting systems, not fixed storylines.

The detailed game design lives in [`docs/GAME_DESIGN.md`](docs/GAME_DESIGN.md).

---

## Tech Stack

- **Engine:** Godot 4 (2D focus)
- **Language:** GDScript (for fast iteration; may add Rust later for core sim)
- **Target platforms:** PC (Windows / Linux / macOS), with a view to web exports later
- **Data:** JSON / Godot Resources for content (jobs, factions, crew archetypes)

High-level technical architecture and patterns are in [`docs/TECH_DESIGN.md`](docs/TECH_DESIGN.md).

---

## Repo Structure

Planned layout (will evolve, but filenames are intended to be stable):

```text
.
├─ README.md              # You are here – high-level overview
├─ AI_GUIDE.md            # How to collaborate with AI on this repo
├─ docs/
│  ├─ GAME_DESIGN.md      # Vision, pillars, systems, content scope
│  ├─ TECH_DESIGN.md      # Architecture, data model, code patterns
│  └─ DEVTOOLS_TESTING.md # Testing strategy, debug tools, dev workflows
├─ src/                   # Game code (Godot project)
│  ├─ scenes/
│  ├─ scripts/
│  └─ assets/
└─ notes/                 # Scratch / experiments (not canonical)
````

Canonical design/tech decisions **must** live in `docs/`.  
Scratch ideas, experiments, and throwaway notes go into `notes/` (or similar) so they can be ignored by tooling.

---

## Getting Started (Dev)

1. **Install Godot 4.x**
    
    - Use the latest stable 4.x release.
        
2. **Clone this repo**
    
    ```bash
    git clone <your-repo-url>
    cd <your-repo-folder>
    ```
    
3. **Open in Godot**
    
    - Launch Godot 4, open the project at the repo root (or `src/` if the project file lives there).
        
4. **Run**
    
    - Use Godot’s “Play” button to run the current main scene (defined in the project settings).
        

Details about scenes, singletons, and sim structure are maintained in `docs/TECH_DESIGN.md`.

---

## Testing, Debugging & Dev Tools

This project aims to be **highly testable and debuggable**, especially around combat and campaign simulation.

Key ideas (fleshed out in `docs/DEVTOOLS_TESTING.md`):

- Deterministic combat and RNG with seeds for reproducible tests.
    
- Unit tests for core rules (hit chance, damage, economy).
    
- Simple simulation runs to sanity-check difficulty and campaign survivability.
    
- In-game debug features:
    
    - Start directly in a test combat.
        
    - Spawn specific jobs/events.
        
    - Toggle “god mode” / reveal debug overlays.
        

---

## Working With AI on This Repo

The repo is designed to be AI-friendly:

- **Single sources of truth:**
    
    - Game design → `docs/GAME_DESIGN.md`
        
    - Architecture & patterns → `docs/TECH_DESIGN.md`
        
    - Testing/devtools → `docs/DEVTOOLS_TESTING.md`
        
- **No doc duplication:**  
    Update existing sections instead of creating new `*_v2.md` files.
    
- **Short, focused docs:**  
    Prefer bullet-point answers under clear headings over long essays.
    

For details and conventions, see `AI_GUIDE.md`.  
When bundling the repo with tools like `repomix`, include:

- `README.md`
    
- `AI_GUIDE.md`
    
- `docs/`
    
- Relevant code under `src/`
    

…and exclude scratch/temporary content (`notes/`, old exports, build artifacts).

---

## Roadmap (High Level, v0.1)

The first playable milestone is intentionally small:

- One ship, small starmap with a handful of nodes.
    
- A few factions with basic reputation.
    
- 3–4 contract templates (cargo, raid, escort, covert).
    
- Simple but lethal turn-based ground combat on a grid.
    
- Basic crew system with traits, injuries, permadeath.
    
- A minimal campaign loop with clear fail states (crew wiped or broke).
    

More detailed scope, constraints, and non-goals are always tracked in `docs/GAME_DESIGN.md`.