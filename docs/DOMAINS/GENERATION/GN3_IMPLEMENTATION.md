# GN3 – Encounter Instantiation: Implementation Plan

**Status**: ✅ Complete  
**Depends on**: GN2 ✅, EN1 ✅, EN2 ✅, TV2 ✅  
**Phase**: G2

**Goal**: Generate encounter instances during travel by selecting appropriate templates based on context and parameterizing them with resolved values.

---

## Overview

GN3 bridges the gap between the Encounter domain (EN1/EN2) and the Travel domain (TV2). Currently, `TravelExecutor` triggers encounters but auto-resolves them as stubs. GN3 provides:

1. **Template Registry**: Storage and retrieval of encounter templates by tags
2. **Encounter Generator**: Context-aware selection and instantiation
3. **Parameter Resolution**: Fill in NPC names, cargo types, faction references
4. **Travel Integration**: Wire generator into `TravelExecutor`

**Key outcomes**:
- Travel encounters use real encounter templates instead of stubs
- Template selection is weighted by context (route hazard, system tags, player state)
- Encounters feel contextually appropriate (pirates in lawless space, patrols in secure space)
- Deterministic: same seed + context = same encounter selection

---

## Current State Assessment

### What We Have

| Component | Status | Location | Notes |
|-----------|--------|----------|-------|
| `EncounterTemplate` | ✅ Complete | `src/sim/encounter/EncounterTemplate.cs` | Data-driven template structure |
| `EncounterInstance` | ✅ Complete | `src/sim/encounter/EncounterInstance.cs` | Runtime state with parameters |
| `EncounterRunner` | ✅ Complete | `src/sim/encounter/EncounterRunner.cs` | State machine for running encounters |
| `EncounterContext` | ✅ Complete | `src/sim/encounter/EncounterContext.cs` | Evaluation context for conditions |
| `TravelContext` | ✅ Complete | `src/sim/travel/TravelContext.cs` | Context for encounter selection |
| `TravelExecutor` | ✅ Complete | `src/sim/travel/TravelExecutor.cs` | Triggers encounters (stub resolution) |
| `TestEncounters` | ✅ Complete | `src/sim/encounter/TestEncounters.cs` | 10+ test templates |
| `NameGenerator` | ✅ Complete | `src/sim/generation/NameGenerator.cs` | System/station names |
| `RngService` | ✅ Complete | `src/sim/RngService.cs` | Deterministic RNG streams |

### What GN3 Requires

| Requirement | Current Status | Gap |
|-------------|----------------|-----|
| `EncounterTemplateRegistry` | ❌ Missing | No centralized template storage |
| `EncounterGenerator` | ❌ Missing | No selection/instantiation logic |
| Template weighting | ❌ Missing | No context-based selection weights |
| Parameter resolution | ⚠️ Partial | `EncounterInstance.ResolvedParameters` exists but no resolver |
| NPC name generation | ❌ Missing | `NameGenerator` has systems, not NPCs |
| Travel integration | ⚠️ Stub | `TravelExecutor.TryTriggerEncounter()` auto-resolves |
| Production templates | ⚠️ Test only | `TestEncounters` are for testing, not gameplay |

---

## Architecture Decisions

### AD1: Registry as Singleton-like Service

**Decision**: `EncounterTemplateRegistry` is a service that can be populated at startup and queried during generation.

**Rationale**:
- Templates are static data, loaded once
- Multiple systems need to query templates (generation, save/load)
- Follows existing patterns (`FactionRegistry`, `TraitRegistry`)

### AD2: Generator Takes TravelContext

**Decision**: `EncounterGenerator.Generate()` takes `TravelContext` directly, not a separate context class.

**Rationale**:
- `TravelContext` already contains all needed information
- Avoids creating yet another context class
- Clear ownership: Travel owns context, Generation uses it

### AD3: Weighted Selection with Eligibility Filter

**Decision**: Two-phase selection:
1. Filter templates by eligibility (required tags, conditions)
2. Weight remaining templates by context factors

**Rationale**:
- Eligibility is binary (template can/cannot fire)
- Weights are continuous (some templates more likely)
- Separation makes debugging easier

### AD4: Parameter Resolution at Instantiation

**Decision**: Parameters are resolved when `EncounterInstance` is created, not lazily.

**Rationale**:
- Deterministic: parameters fixed at creation time
- Serializable: resolved parameters save/load cleanly
- Simpler: no need to track resolution state

### AD5: Templates in Code Initially, Data Files Later

**Decision**: Initial templates are defined in code (like `TestEncounters`). Data file loading is a future enhancement.

**Rationale**:
- Faster iteration during development
- No JSON schema to maintain yet
- Can migrate to data files in G3/G4

---

## GN3 Deliverables Checklist

### Phase 1: Template Registry ✅
- [x] **1.1** Create `EncounterTemplateRegistry` class
- [x] **1.2** Implement `Register()`, `Get()`, `GetByTag()` methods
- [x] **1.3** Implement `GetEligible(TravelContext)` filtering
- [x] **1.4** Create `EncounterTags` constants class

### Phase 2: Name Generation Extension ✅
- [x] **2.1** Add NPC name generation to `NameGenerator`
- [x] **2.2** Add cargo type generation
- [x] **2.3** Add ship name generation

### Phase 3: Encounter Generator ✅
- [x] **3.1** Create `EncounterGenerator` class
- [x] **3.2** Implement template selection with weights
- [x] **3.3** Implement parameter resolution
- [x] **3.4** Implement `Generate(TravelContext, CampaignState)` method

### Phase 4: Production Templates ✅
- [x] **4.1** Create `ProductionEncounters` class
- [x] **4.2** Implement 10 gameplay templates
- [x] **4.3** Tag templates appropriately for selection

### Phase 5: Travel Integration ✅
- [x] **5.1** Update `TravelExecutor.TryTriggerEncounter()` to use generator
- [x] **5.2** Wire encounter instance to campaign state
- [x] **5.3** Handle encounter completion/resume flow

### Phase 6: Testing ✅
- [x] **6.1** Registry tests (30 tests in `GN3RegistryTests.cs`)
- [x] **6.2** Generator tests (22 tests in `GN3GeneratorTests.cs`)
- [x] **6.3** Name generation tests (22 tests in `GN3NameGeneratorTests.cs`)
- [x] **6.4** Travel integration verified (14 tests in `TV2TravelExecutorTests.cs`)

---

## Phase 1: Template Registry

### Step 1.1: EncounterTemplateRegistry Class

**New File**: `src/sim/generation/EncounterTemplateRegistry.cs`

```csharp
using System.Collections.Generic;
using System.Linq;

namespace FringeTactics;

public class EncounterTemplateRegistry
{
    private readonly Dictionary<string, EncounterTemplate> templates = new();
    
    public void Register(EncounterTemplate template)
    {
        if (template == null || string.IsNullOrEmpty(template.Id)) return;
        templates[template.Id] = template;
    }
    
    public void RegisterAll(IEnumerable<EncounterTemplate> templateList)
    {
        foreach (var template in templateList)
            Register(template);
    }
    
    public EncounterTemplate Get(string id) =>
        templates.TryGetValue(id, out var t) ? t : null;
    
    public IEnumerable<EncounterTemplate> GetByTag(string tag) =>
        templates.Values.Where(t => t.HasTag(tag));
    
    public IEnumerable<EncounterTemplate> GetEligible(TravelContext context) =>
        templates.Values.Where(t => IsEligible(t, context));
    
    private bool IsEligible(EncounterTemplate template, TravelContext context)
    {
        if (!template.HasTag(EncounterTags.Travel)) return false;
        
        if (!string.IsNullOrEmpty(context.SuggestedEncounterType) &&
            context.SuggestedEncounterType != EncounterTypes.Random &&
            !template.HasTag(context.SuggestedEncounterType) &&
            !template.HasTag(EncounterTags.Generic))
            return false;
        
        return true;
    }
    
    public int Count => templates.Count;
    
    public static EncounterTemplateRegistry CreateDefault()
    {
        var registry = new EncounterTemplateRegistry();
        registry.RegisterAll(ProductionEncounters.GetAllTemplates());
        return registry;
    }
}
```

### Step 1.2: EncounterTags Constants

**New File**: `src/sim/encounter/EncounterTags.cs`

```csharp
namespace FringeTactics;

public static class EncounterTags
{
    // Trigger Context
    public const string Travel = "travel";
    public const string Station = "station";
    public const string Exploration = "exploration";
    
    // Encounter Type
    public const string Pirate = "pirate";
    public const string Patrol = "patrol";
    public const string Trader = "trader";
    public const string Smuggler = "smuggler";
    public const string Distress = "distress";
    public const string Anomaly = "anomaly";
    public const string Crew = "crew";
    public const string Faction = "faction";
    
    // Interaction Style
    public const string Combat = "combat";
    public const string Social = "social";
    public const string Choice = "choice";
    public const string SkillCheck = "skill_check";
    
    // Special
    public const string Generic = "generic";
    public const string Rare = "rare";
}
```

---

## Phase 2: Name Generation Extension

### Step 2.1-2.3: Extend NameGenerator

**File**: `src/sim/generation/NameGenerator.cs` (add to existing)

```csharp
// === NPC Names ===
private static readonly string[] FirstNames = 
{
    "Marcus", "Elena", "Viktor", "Zara", "Chen", "Yuki", "Omar", "Freya",
    "Dante", "Mira", "Kira", "Jax", "Nova", "Rook", "Sage", "Quinn"
};

private static readonly string[] LastNames = 
{
    "Vance", "Cross", "Stone", "Black", "Grey", "Wolf", "Hawk", "Frost",
    "Drake", "Steel", "Raven", "Storm", "Blade", "Thorn", "Vale", "Marsh"
};

private static readonly string[] Nicknames = 
{
    "the Blade", "One-Eye", "Lucky", "the Ghost", "Ironhand", "Quickshot"
};

public static string GenerateNpcName(RngStream rng)
{
    string first = FirstNames[rng.NextInt(FirstNames.Length)];
    string last = LastNames[rng.NextInt(LastNames.Length)];
    if (rng.NextFloat() < 0.2f)
    {
        string nick = Nicknames[rng.NextInt(Nicknames.Length)];
        return $"{first} \"{nick}\" {last}";
    }
    return $"{first} {last}";
}

// === Cargo Types ===
private static readonly string[] LegalCargo = 
{
    "medical supplies", "food rations", "industrial parts", "electronics"
};

private static readonly string[] IllegalCargo = 
{
    "weapons cache", "contraband tech", "stolen goods", "unregistered meds"
};

public static string GenerateCargoType(RngStream rng, bool illegal = false)
{
    var pool = illegal ? IllegalCargo : LegalCargo;
    return pool[rng.NextInt(pool.Length)];
}

// === Ship Names ===
private static readonly string[] ShipPrefixes = { "ISV", "CSV", "FTV", "MSV" };
private static readonly string[] ShipNames = 
{
    "Wanderer", "Fortune", "Destiny", "Horizon", "Venture", "Pioneer"
};

public static string GenerateShipName(RngStream rng)
{
    string prefix = ShipPrefixes[rng.NextInt(ShipPrefixes.Length)];
    string name = ShipNames[rng.NextInt(ShipNames.Length)];
    return $"{prefix} {name}";
}
```

---

## Phase 3: Encounter Generator

### Step 3.1-3.4: EncounterGenerator Class

**New File**: `src/sim/generation/EncounterGenerator.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Linq;

namespace FringeTactics;

public class EncounterGenerator
{
    private readonly EncounterTemplateRegistry registry;
    
    public EncounterGenerator(EncounterTemplateRegistry registry)
    {
        this.registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }
    
    public EncounterInstance Generate(TravelContext context, CampaignState campaign)
    {
        if (context == null) return null;
        var rng = campaign?.Rng?.Campaign;
        if (rng == null) return null;
        
        var eligible = registry.GetEligible(context).ToList();
        if (eligible.Count == 0) return null;
        
        var weights = CalculateWeights(eligible, context);
        var template = WeightedSelect(eligible, weights, rng);
        if (template == null) return null;
        
        var instance = EncounterInstance.Create(template, rng);
        ResolveParameters(instance, context, campaign, rng);
        return instance;
    }
    
    private Dictionary<EncounterTemplate, float> CalculateWeights(
        List<EncounterTemplate> templates, TravelContext context)
    {
        var weights = new Dictionary<EncounterTemplate, float>();
        var metrics = context.SystemMetrics;
        
        foreach (var template in templates)
        {
            float weight = 1.0f;
            
            if (template.HasTag(EncounterTags.Pirate))
                weight *= 0.5f + ((metrics?.CriminalActivity ?? 2) * 0.3f);
            
            if (template.HasTag(EncounterTags.Patrol))
                weight *= 0.5f + ((metrics?.SecurityLevel ?? 2) * 0.3f);
            
            if (template.HasTag(EncounterTags.Combat))
                weight *= 0.8f + (context.RouteHazard * 0.15f);
            
            if (template.HasTag(EncounterTags.Rare))
                weight *= 0.3f;
            
            if (!string.IsNullOrEmpty(context.SuggestedEncounterType) &&
                template.HasTag(context.SuggestedEncounterType))
                weight *= 2.0f;
            
            weights[template] = Math.Max(0.1f, weight);
        }
        return weights;
    }
    
    private EncounterTemplate WeightedSelect(
        List<EncounterTemplate> templates,
        Dictionary<EncounterTemplate, float> weights,
        RngStream rng)
    {
        float total = weights.Values.Sum();
        float roll = rng.NextFloat() * total;
        float cumulative = 0f;
        
        foreach (var template in templates)
        {
            cumulative += weights.GetValueOrDefault(template, 0f);
            if (roll <= cumulative) return template;
        }
        return templates.LastOrDefault();
    }
    
    private void ResolveParameters(
        EncounterInstance instance, TravelContext context,
        CampaignState campaign, RngStream rng)
    {
        instance.SetParameter("system_name", context.CurrentSystem?.Name ?? "Unknown");
        instance.SetParameter("faction_name", 
            campaign?.World?.GetFaction(context.SystemOwnerFactionId)?.Name ?? "local authorities");
        instance.SetParameter("npc_name", NameGenerator.GenerateNpcName(rng));
        instance.SetParameter("ship_name", NameGenerator.GenerateShipName(rng));
        instance.SetParameter("cargo_type", NameGenerator.GenerateCargoType(rng));
    }
}
```

---

## Phase 4: Production Templates

### Template List (8-12 templates)

| Template ID | Tags | Trigger Context |
|-------------|------|-----------------|
| `pirate_ambush` | pirate, combat, choice | High criminal activity |
| `patrol_inspection` | patrol, social | High security |
| `distress_signal` | distress, choice, skill_check | Any route |
| `trader_opportunity` | trader, social | Trade routes |
| `smuggler_contact` | smuggler, social | Low security |
| `derelict_discovery` | exploration, choice | Near derelicts |
| `faction_agent` | faction, social | Faction territory |
| `mysterious_signal` | anomaly, exploration | Frontier systems |
| `crew_conflict` | crew, social | Long travel |
| `mechanical_failure` | ship, resource | Any route |
| `refugee_plea` | social, choice | Unstable regions |
| `bounty_hunter` | combat, choice | Player has bounty |

**New File**: `src/sim/generation/ProductionEncounters.cs`

Contains factory methods for each template (see detailed examples in Phase 4 section below).

---

## Phase 5: Travel Integration

### Step 5.1: Update TravelExecutor

**File**: `src/sim/travel/TravelExecutor.cs`

Replace the stub in `TryTriggerEncounter()`:

```csharp
private TravelResult TryTriggerEncounter(TravelState state, CampaignState campaign, TravelSegment segment)
{
    float encounterChance = segment.EncounterChance;
    float roll = rng.Campaign.NextFloat();
    
    if (roll >= encounterChance) return null;
    
    // Create context and generate encounter
    var context = TravelContext.Create(state, campaign);
    var generator = new EncounterGenerator(campaign.EncounterRegistry);
    var encounter = generator.Generate(context, campaign);
    
    if (encounter == null)
    {
        SimLog.Log("[Travel] No eligible encounter template found");
        return null;
    }
    
    // Record and pause for encounter
    state.IsPausedForEncounter = true;
    state.PendingEncounterId = encounter.InstanceId;
    campaign.ActiveEncounter = encounter;
    
    campaign.EventBus?.Publish(new TravelEncounterTriggeredEvent(
        state.CurrentSystemId,
        encounter.Template.Id,
        encounter.InstanceId
    ));
    
    return TravelResult.Paused(state, state.FuelConsumed, state.DaysElapsed);
}
```

### Step 5.2: CampaignState Changes

Add to `CampaignState`:
- `EncounterTemplateRegistry EncounterRegistry { get; set; }`
- `EncounterInstance ActiveEncounter { get; set; }`

---

## Phase 6: Testing

### Test Files to Create

| File | Purpose | Test Count |
|------|---------|------------|
| `tests/sim/generation/GN3RegistryTests.cs` | Registry operations | ~15 |
| `tests/sim/generation/GN3GeneratorTests.cs` | Selection, weighting | ~20 |
| `tests/sim/generation/GN3ParameterTests.cs` | Parameter resolution | ~10 |
| `tests/sim/generation/GN3IntegrationTests.cs` | Travel integration | ~10 |

### Key Test Cases

**Registry Tests**:
- Register and retrieve template by ID
- Get templates by single tag
- Get templates by multiple tags
- Eligibility filtering with TravelContext
- Empty registry returns no eligible templates

**Generator Tests**:
- Determinism: same seed + context = same template
- Weighting: pirate templates more likely with high criminal activity
- Weighting: patrol templates more likely with high security
- No eligible templates returns null
- Parameters are resolved correctly

**Integration Tests**:
- Travel triggers encounter with generator
- Encounter instance has resolved parameters
- Travel pauses correctly for encounter
- Travel resumes after encounter completion

---

## Manual Test Setup

### Test Scenario: Travel with Encounters

1. **Setup**:
   - Create campaign with `GalaxyGenerator` (GN2)
   - Populate `EncounterTemplateRegistry` with production templates
   - Position player at a hub system

2. **Execute**:
   - Plan travel to a distant system (3+ segments)
   - Execute travel with `TravelExecutor`

3. **Verify**:
   - At least one encounter triggers (adjust `EncounterChance` if needed)
   - Encounter template matches context (pirates in lawless, patrols in secure)
   - Parameters are resolved (NPC names, faction names)
   - Travel pauses and can be resumed

### DevTools Integration

Add to `DevTools.cs`:
```csharp
public static void TestEncounterGeneration()
{
    var registry = EncounterTemplateRegistry.CreateDefault();
    var generator = new EncounterGenerator(registry);
    
    // Create test context
    var context = new TravelContext
    {
        SystemMetrics = new SystemMetrics { CriminalActivity = 4, SecurityLevel = 1 },
        RouteHazard = 3,
        SuggestedEncounterType = EncounterTypes.Pirate
    };
    
    // Generate 10 encounters and log distribution
    var counts = new Dictionary<string, int>();
    for (int i = 0; i < 10; i++)
    {
        var enc = generator.Generate(context, campaign);
        var id = enc?.Template?.Id ?? "none";
        counts[id] = counts.GetValueOrDefault(id) + 1;
    }
    
    foreach (var (id, count) in counts)
        SimLog.Log($"  {id}: {count}");
}
```

---

## Files Summary

### New Files

| File | Purpose |
|------|---------|
| `src/sim/generation/EncounterTemplateRegistry.cs` | Template storage and retrieval |
| `src/sim/generation/EncounterGenerator.cs` | Selection and instantiation |
| `src/sim/generation/ProductionEncounters.cs` | Gameplay encounter templates |
| `src/sim/encounter/EncounterTags.cs` | Tag constants |
| `tests/sim/generation/GN3RegistryTests.cs` | Registry tests |
| `tests/sim/generation/GN3GeneratorTests.cs` | Generator tests |
| `tests/sim/generation/GN3ParameterTests.cs` | Parameter tests |
| `tests/sim/generation/GN3IntegrationTests.cs` | Integration tests |

### Files to Modify

| File | Changes |
|------|---------|
| `src/sim/generation/NameGenerator.cs` | Add NPC, cargo, ship name generation |
| `src/sim/travel/TravelExecutor.cs` | Use generator instead of stub |
| `src/sim/campaign/CampaignState.cs` | Add `EncounterRegistry`, `ActiveEncounter` |
| `src/sim/generation/agents.md` | Update with new files |

---

## Success Criteria

### Functional
- [ ] Travel encounters use real templates instead of stubs
- [ ] Template selection respects context (metrics, tags, hazard)
- [ ] Parameters are resolved with contextual values
- [ ] Encounters are deterministic given same seed

### Quality
- [ ] All tests pass (~55 tests)
- [ ] No regressions in existing travel tests
- [ ] Manual test scenario works end-to-end

### Integration
- [ ] `TravelExecutor` pauses for encounters
- [ ] `CampaignState` tracks active encounter
- [ ] Save/load works with active encounter (via `EncounterInstanceData`)
