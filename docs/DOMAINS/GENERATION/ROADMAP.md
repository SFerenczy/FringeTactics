# Generation Domain Roadmap

This document defines the **implementation order** for the Generation domain.

- G0 is concept/design only. Implementation starts in G1.
- Each milestone is a **vertical slice**.

---

## Overview of Milestones

1. **GN0 – Concept Finalization (G0)**
2. **GN1 – Contract Generation (G1)**
3. **GN2 – Galaxy Generation (G2)**
4. **GN3 – Encounter Instantiation (G2)**
5. **GN4 – Simulation-Aware Generation (G3)**

---

## GN0 – Concept Finalization (G0)

**Goal:**  
Finalize design decisions for contracts, archetypes, and generation context.

**Key deliverables:**

- Review and finalize CAMPAIGN_FOUNDATIONS.md section 2 (Contracts).
- Define initial contract archetypes:
  - delivery, escort, raid, heist, extraction, patrol.
- Define contract template structure:
  - Required fields, optional fields, reward formulas.
- Define `GenerationContext` structure:
  - What inputs generation needs (player state, world metrics, location).
- Document difficulty/risk model (how player power maps to content difficulty).

**Why first:**  
Generation is data-driven. Getting templates right avoids rework.

**Status:** Done (see `GN0_IMPLEMENTATION.md` for detailed breakdown)

---

## GN1 – Contract Generation (G1)

**Goal:**  
Generate mission offers for a single hub.

**Status:** ✅ Complete (see `GN1_IMPLEMENTATION.md` for detailed breakdown)

**Key capabilities:**

- `ContractGenerator` class:
  - Input: `GenerationContext` (player state, hub metrics, RNG).
  - Output: List of `Contract` offers.
- `Contract` class:
  - `issuer_faction`, `location_target`.
  - `contract_type` (from archetypes).
  - `primary_objective`, `secondary_objectives`.
  - `base_reward`, `deadline_days`.
- Template-based generation:
  - Select archetype based on hub metrics.
  - Parameterize with player power and local context.
- Difficulty scaling:
  - Enemy count, enemy types based on player crew strength.

**Deliverables:**
- `ContractGenerator`, `Contract` classes.
- Initial contract templates (3-5 archetypes).
- Unit tests for generation determinism.
- Integration with Management (player power query).

---

## GN2 – Galaxy Generation (G2)

**Goal:**  
Generate the initial sector at campaign start.

**Depends on:** GN1 ✅, WD2 ✅

**Status:** ⬜ Pending

**Key capabilities:**

- `GalaxyGenerator` class:
  - Input: Campaign seed, `GalaxyConfig`.
  - Output: `WorldState` with systems, stations, routes, factions.
- System generation:
  - Positions using spatial distribution algorithm.
  - Names from name pools.
  - Initial metrics based on system type and location.
  - Faction ownership based on territory rules.
- Route generation:
  - Connectivity graph ensuring all systems reachable.
  - Distance computed from positions.
  - Hazard values based on system metrics and location.
- Station generation:
  - Facilities per station based on system type.
  - Initial metrics inherited from system.

### Phase 2.1: GalaxyConfig

**New file:** `src/sim/generation/GalaxyConfig.cs`

```csharp
public class GalaxyConfig
{
    public int SystemCount { get; set; } = 10;
    public int MinConnections { get; set; } = 2;
    public int MaxConnections { get; set; } = 4;
    public float MapWidth { get; set; } = 800;
    public float MapHeight { get; set; } = 600;
    public float MinSystemDistance { get; set; } = 80;
    
    // Faction distribution
    public Dictionary<string, float> FactionWeights { get; set; } = new()
    {
        ["corp"] = 0.3f,
        ["syndicate"] = 0.2f,
        ["militia"] = 0.2f,
        ["neutral"] = 0.3f
    };
    
    // System type distribution
    public Dictionary<SystemType, float> SystemTypeWeights { get; set; } = new()
    {
        [SystemType.Station] = 0.3f,
        [SystemType.Outpost] = 0.3f,
        [SystemType.Derelict] = 0.1f,
        [SystemType.Contested] = 0.1f,
        [SystemType.Uninhabited] = 0.2f
    };
}
```

### Phase 2.2: GalaxyGenerator

**New file:** `src/sim/generation/GalaxyGenerator.cs`

```csharp
public class GalaxyGenerator
{
    private readonly GalaxyConfig config;
    private readonly RngStream rng;
    
    public GalaxyGenerator(GalaxyConfig config, RngStream rng)
    {
        this.config = config;
        this.rng = rng;
    }
    
    public WorldState Generate()
    {
        var world = new WorldState();
        
        // 1. Generate system positions
        var positions = GeneratePositions();
        
        // 2. Create systems with types and names
        var systems = CreateSystems(positions);
        
        // 3. Generate connectivity graph
        var routes = GenerateRoutes(systems);
        
        // 4. Assign faction ownership
        AssignFactions(systems);
        
        // 5. Generate stations for inhabited systems
        GenerateStations(world, systems);
        
        // 6. Set initial metrics
        InitializeMetrics(systems);
        
        return world;
    }
}
```

### Phase 2.3: Position Generation

Use Poisson disk sampling or simple rejection sampling:

```csharp
private List<Vector2> GeneratePositions()
{
    var positions = new List<Vector2>();
    int attempts = 0;
    int maxAttempts = config.SystemCount * 100;
    
    while (positions.Count < config.SystemCount && attempts < maxAttempts)
    {
        var pos = new Vector2(
            rng.NextFloat() * config.MapWidth,
            rng.NextFloat() * config.MapHeight
        );
        
        if (IsValidPosition(pos, positions))
            positions.Add(pos);
        
        attempts++;
    }
    
    return positions;
}
```

### Phase 2.4: Route Generation

Ensure connectivity using minimum spanning tree + random extra edges:

```csharp
private List<Route> GenerateRoutes(List<StarSystem> systems)
{
    // 1. Build MST for guaranteed connectivity
    var mstRoutes = BuildMinimumSpanningTree(systems);
    
    // 2. Add random extra routes for variety
    var extraRoutes = AddRandomRoutes(systems, mstRoutes);
    
    return mstRoutes.Concat(extraRoutes).ToList();
}
```

### Phase 2.5: Name Generation

**New file:** `src/sim/generation/NameGenerator.cs`

```csharp
public static class NameGenerator
{
    private static readonly string[] Prefixes = { "New", "Port", "Fort", "Station", "Outpost" };
    private static readonly string[] Names = { "Haven", "Reach", "Frontier", "Prospect", "Terminus" };
    private static readonly string[] Suffixes = { "Prime", "Alpha", "VII", "Station", "Hub" };
    
    public static string GenerateSystemName(RngStream rng);
    public static string GenerateStationName(RngStream rng, string systemName);
}
```

**Deliverables:**
- `GalaxyConfig` class.
- `GalaxyGenerator` class.
- `NameGenerator` helper.
- Position generation algorithm.
- Route generation with MST.
- Faction assignment logic.
- Station generation for inhabited systems.
- Determinism tests (same seed = same galaxy).
- Visual test output (optional: dump to JSON for inspection).

**Files to create:**
| File | Purpose |
|------|---------|
| `src/sim/generation/GalaxyConfig.cs` | Configuration |
| `src/sim/generation/GalaxyGenerator.cs` | Main generator |
| `src/sim/generation/NameGenerator.cs` | Name pools |
| `tests/sim/generation/GN2*.cs` | Test files |

---

## GN3 – Encounter Instantiation (G2)

**Goal:**  
Generate encounter instances for Travel and exploration.

**Depends on:** GN2 ✅, EN1 ✅, TV2 🔄

**Status:** ⬜ Pending

**Key capabilities:**

- `EncounterGenerator` class:
  - Input: `TravelContext` (route, system tags, player state).
  - Output: `EncounterInstance` for Encounter domain to run.
- Template selection:
  - Based on route hazards, system tags, player cargo.
  - Weighted random selection from eligible templates.
- Parameterization:
  - Fill in NPC names, cargo types, faction references.
  - Resolve text placeholders.
- Template registry:
  - Load templates from data files.
  - Tag-based filtering.

### Phase 3.1: EncounterTemplateRegistry

**New file:** `src/sim/generation/EncounterTemplateRegistry.cs`

```csharp
public class EncounterTemplateRegistry
{
    private Dictionary<string, EncounterTemplate> templates = new();
    
    public void Register(EncounterTemplate template);
    public EncounterTemplate Get(string id);
    public IEnumerable<EncounterTemplate> GetByTag(string tag);
    public IEnumerable<EncounterTemplate> GetEligible(TravelContext context);
    
    public static EncounterTemplateRegistry CreateDefault();
}
```

### Phase 3.2: EncounterGenerator

**New file:** `src/sim/generation/EncounterGenerator.cs`

```csharp
public class EncounterGenerator
{
    private readonly EncounterTemplateRegistry registry;
    private readonly RngStream rng;
    
    public EncounterGenerator(EncounterTemplateRegistry registry, RngStream rng)
    {
        this.registry = registry;
        this.rng = rng;
    }
    
    public EncounterInstance Generate(TravelContext context)
    {
        // 1. Get eligible templates
        var eligible = registry.GetEligible(context).ToList();
        if (eligible.Count == 0) return null;
        
        // 2. Weight by context
        var weights = CalculateWeights(eligible, context);
        
        // 3. Select template
        var template = WeightedSelect(eligible, weights);
        
        // 4. Instantiate with parameters
        return Instantiate(template, context);
    }
    
    private EncounterInstance Instantiate(EncounterTemplate template, TravelContext context)
    {
        var instance = new EncounterInstance(template);
        
        // Resolve parameters
        instance.Parameters["faction"] = context.CurrentSystem.OwningFactionId;
        instance.Parameters["npc_name"] = NameGenerator.GenerateNpcName(rng);
        instance.Parameters["cargo_type"] = SelectCargoType(context, rng);
        
        return instance;
    }
}
```

### Phase 3.3: Template Selection Weights

Weight templates based on context:

```csharp
private Dictionary<EncounterTemplate, float> CalculateWeights(
    List<EncounterTemplate> templates, 
    TravelContext context)
{
    var weights = new Dictionary<EncounterTemplate, float>();
    
    foreach (var template in templates)
    {
        float weight = 1.0f;
        
        // Pirate encounters more likely in high criminal activity
        if (template.Tags.Contains("pirate"))
            weight *= 1 + (context.CriminalActivity * 0.2f);
        
        // Patrol encounters more likely in high security
        if (template.Tags.Contains("patrol"))
            weight *= 1 + (context.SecurityLevel * 0.2f);
        
        // Cargo-related encounters if player has valuable cargo
        if (template.Tags.Contains("cargo") && context.CargoValue > 100)
            weight *= 1.5f;
        
        weights[template] = weight;
    }
    
    return weights;
}
```

### Phase 3.4: Initial Templates

Create 8-12 encounter templates:

| Template | Tags | Trigger Context |
|----------|------|-----------------|
| `pirate_ambush` | pirate, combat | High criminal activity |
| `patrol_inspection` | patrol, social | High security |
| `distress_signal` | exploration, choice | Any route |
| `trader_opportunity` | trade, social | Trade routes |
| `smuggler_contact` | criminal, social | Low security, has cargo |
| `derelict_discovery` | exploration, loot | Near derelicts |
| `faction_agent` | faction, social | Faction territory |
| `mysterious_signal` | exploration, mystery | Frontier systems |
| `crew_conflict` | crew, social | Long travel |
| `mechanical_failure` | ship, resource | Any route |

**Deliverables:**
- `EncounterTemplateRegistry` class.
- `EncounterGenerator` class.
- Template selection with weights.
- Parameter instantiation.
- 8-12 initial templates.
- Unit tests for selection logic.
- Integration tests with Travel.

**Files to create:**
| File | Purpose |
|------|---------|
| `src/sim/generation/EncounterTemplateRegistry.cs` | Template storage |
| `src/sim/generation/EncounterGenerator.cs` | Instantiation logic |
| `data/encounters/*.json` | Template definitions |
| `tests/sim/generation/GN3*.cs` | Test files |

---

## GN4 – Simulation-Aware Generation (G3)

**Goal:**  
Generation uses live simulation metrics to bias content.

**Key capabilities:**

- Contract generation responds to:
  - High piracy → more anti-pirate contracts.
  - Low security → more smuggling opportunities.
  - Faction desperation → higher rewards, riskier jobs.
- Encounter generation responds to:
  - Local unrest → more hostile encounters.
  - Trade volume → more merchant encounters.
- Statistical validation:
  - Distributions match design intentions.

**Deliverables:**
- Metric-aware generation logic.
- Statistical tests for distribution validation.
- Integration tests with Simulation.

---

## G0/G1/G2 Scope Summary

| Milestone | Phase | Notes |
|-----------|-------|-------|
| GN0 | G0 | Concept only |
| GN1 | G1 | Contract generation |
| GN2 | G2 | Galaxy generation |
| GN3 | G2 | Encounter instantiation |
| GN4 | G3 | Simulation integration |
