# GN1 – Contract Generation: Implementation Plan

**Status**: ✅ Complete  
**Depends on**: GN0 ✅, MG1 ✅, WD1 🔄  
**Phase**: G1

**Goal**: Generate mission offers (contracts) for a single hub using player state, hub context, and templates.

---

## Current State Assessment

### What We Have

| Component | Status | Location |
|-----------|--------|----------|
| `Job` | ✅ Exists | `src/sim/campaign/Job.cs` |
| `JobSystem` | ✅ Exists | `src/sim/campaign/JobSystem.cs` |
| `JobType` enum | ⚠️ Limited | Only Assault, Defense, Extraction |
| `CrewMember` | ✅ Exists | Full stats, traits (MG1) |
| `WorldState` | 🔄 Partial | Single hub (WD1) |
| `RngService` | ✅ Exists | Campaign RNG stream |

### Gaps

| GN0 Design | Current | Gap |
|------------|---------|-----|
| 6 Contract archetypes | 3 JobTypes | Need expansion |
| `GenerationContext` | ❌ Missing | No bundled context |
| `ContractGenerator` | ❌ Missing | Logic in JobSystem |
| `Objective` class | ❌ Missing | Implicit objectives |
| Player power scaling | ❌ Missing | Static difficulty |

---

## GN1 Deliverables Checklist

### Phase 1: Contract Type Expansion ✅
- [x] **1.1** Create `ContractType` enum (6 archetypes)
- [x] **1.2** Migration path from `JobType`
- [x] **1.3** Title/description templates per type

### Phase 2: GenerationContext ✅
- [x] **2.1** Create `GenerationContext` class
- [x] **2.2** `CalculateCrewPower()` helper
- [x] **2.3** `FromCampaign()` factory
- [x] **2.4** Hub metrics integration

### Phase 3: Objective System ✅
- [x] **3.1** Extend `ObjectiveType` enum (in MissionInput.cs)
- [x] **3.2** Create `Objective` class
- [x] **3.3** Add objectives to `Job`

### Phase 4: ContractGenerator ✅
- [x] **4.1** Create `ContractGenerator` class
- [x] **4.2** Archetype selection logic
- [x] **4.3** Difficulty scaling (player power)
- [x] **4.4** Reward calculation formulas
- [x] **4.5** Migrate `JobSystem`

### Phase 5: Serialization ✅
- [x] **5.1** Update `JobData` (ContractType, PrimaryObjective, SecondaryObjectives)
- [x] **5.2** Save version increment (v3 → v4)

### Phase 6: Testing ✅
- [x] **6.1** GenerationContext tests (20 tests)
- [x] **6.2** ContractGenerator tests (18 tests)
- [x] **6.3** Determinism tests (5 tests)
- [x] **6.4** Serialization tests (15 tests)

---

## Phase 1: Contract Type Expansion

### Step 1.1: ContractType Enum

**New File**: `src/sim/generation/ContractType.cs`

```csharp
namespace FringeTactics;

public enum ContractType
{
    Assault,    // Eliminate all hostiles
    Delivery,   // Transport cargo to extraction
    Escort,     // Keep VIP alive to extraction
    Raid,       // Destroy/steal target object
    Heist,      // Acquire target without alarm
    Extraction  // Locate and extract person(s)
}

public static class ContractTypeExtensions
{
    public static string GetDisplayName(this ContractType type) => type switch
    {
        ContractType.Assault => "Assault",
        ContractType.Delivery => "Delivery",
        ContractType.Escort => "Escort",
        ContractType.Raid => "Raid",
        ContractType.Heist => "Heist",
        ContractType.Extraction => "Extraction",
        _ => "Unknown"
    };
    
    public static float GetRewardMultiplier(this ContractType type) => type switch
    {
        ContractType.Assault => 1.0f,
        ContractType.Delivery => 1.1f,
        ContractType.Escort => 1.2f,
        ContractType.Raid => 1.3f,
        ContractType.Heist => 1.4f,
        ContractType.Extraction => 1.2f,
        _ => 1.0f
    };
    
    public static bool IsImplemented(this ContractType type) => type switch
    {
        ContractType.Assault => true,
        ContractType.Extraction => true,
        _ => false
    };
}
```

### Step 1.2: Migration from JobType

**File**: `src/sim/campaign/Job.cs`

Add `ContractType` property, deprecate `JobType`:

```csharp
public ContractType ContractType { get; set; } = ContractType.Assault;

[Obsolete("Use ContractType instead")]
public JobType Type 
{ 
    get => ContractTypeToJobType(ContractType);
    set => ContractType = JobTypeToContractType(value);
}
```

### Step 1.3: Title/Description Templates

**New File**: `src/sim/generation/ContractTemplates.cs`

- 5+ titles per contract type
- Description format strings with placeholders
- `GetRandomTitle(type, rng)` and `GetDescription(type, target, faction, reward)`

---

## Phase 2: GenerationContext

### Step 2.1: GenerationContext Class

**New File**: `src/sim/generation/GenerationContext.cs`

```csharp
public class GenerationContext
{
    // Player state
    public int CrewCount { get; set; }
    public int CrewPower { get; set; }
    public List<CrewRole> CrewRoles { get; set; } = new();
    public int CurrentNodeId { get; set; }
    public int CompletedContracts { get; set; }
    public Dictionary<string, int> FactionRep { get; set; } = new();
    
    // Resources
    public int Money { get; set; }
    public int Fuel { get; set; }
    
    // World state
    public SectorNode HubNode { get; set; }
    public List<SectorNode> NearbyNodes { get; set; } = new();
    public Dictionary<string, string> Factions { get; set; } = new();
    
    // Hub metrics (from WD1)
    public int HubSecurityLevel { get; set; } = 3;
    public int HubCriminalActivity { get; set; } = 2;
    public int HubEconomicActivity { get; set; } = 3;
    
    // RNG
    public RngStream Rng { get; set; }
    
    // Derived
    public PowerTier PlayerTier => CrewPower switch
    {
        <= 30 => PowerTier.Rookie,
        <= 60 => PowerTier.Competent,
        <= 100 => PowerTier.Veteran,
        _ => PowerTier.Elite
    };
}

public enum PowerTier { Rookie, Competent, Veteran, Elite }
```

### Step 2.2: CalculateCrewPower

```csharp
public static int CalculateCrewPower(List<CrewMember> crew, int completedMissions)
{
    int power = 0;
    foreach (var member in crew)
    {
        if (!member.CanDeploy()) continue;
        power += member.Level;
        power += member.GetEffectiveStat(CrewStatType.Aim);
        power += member.GetEffectiveStat(CrewStatType.Grit);
        power += member.GetEffectiveStat(CrewStatType.Reflexes);
    }
    return power + (completedMissions * 2);
}
```

### Step 2.3: FromCampaign Factory

```csharp
public static GenerationContext FromCampaign(CampaignState campaign)
{
    var deployable = campaign.GetDeployableCrew();
    return new GenerationContext
    {
        CrewCount = deployable.Count,
        CrewPower = CalculateCrewPower(campaign.Crew, campaign.MissionsCompleted),
        CrewRoles = deployable.Select(c => c.Role).Distinct().ToList(),
        CurrentNodeId = campaign.CurrentNodeId,
        CompletedContracts = campaign.MissionsCompleted,
        FactionRep = new(campaign.FactionRep),
        Money = campaign.Money,
        Fuel = campaign.Fuel,
        HubNode = campaign.GetCurrentNode(),
        NearbyNodes = GetNearbyNodes(campaign.Sector, campaign.CurrentNodeId),
        Factions = campaign.Sector?.Factions ?? new(),
        Rng = campaign.Rng?.Campaign
    };
}
```

---

## Phase 3: Objective System

### Step 3.1: ObjectiveType Enum

**New File**: `src/sim/generation/Objective.cs`

```csharp
public enum ObjectiveType
{
    EliminateAll, EliminateTarget, ReachZone, ProtectUnit,
    DestroyObject, HackTerminal, RetrieveItem, SurviveTurns,
    NoAlarm, NoCasualties, TimeLimit, NoInjuries
}
```

### Step 3.2: Objective Class

```csharp
public class Objective
{
    public string Id { get; set; }
    public ObjectiveType Type { get; set; }
    public string Description { get; set; }
    public bool IsRequired { get; set; }
    public int BonusRewardPercent { get; set; } = 0;
    public Dictionary<string, object> Parameters { get; set; } = new();
    
    // Factory methods
    public static Objective EliminateAll() => new() { Id = "eliminate_all", Type = ObjectiveType.EliminateAll, Description = "Eliminate all hostiles", IsRequired = true };
    public static Objective NoCasualties() => new() { Id = "no_casualties", Type = ObjectiveType.NoCasualties, Description = "No crew deaths", IsRequired = false, BonusRewardPercent = 20 };
    public static Objective TimeBonus(int turns) => new() { Id = "time_bonus", Type = ObjectiveType.TimeLimit, Description = $"Complete within {turns} turns", IsRequired = false, BonusRewardPercent = 15 };
}
```

### Step 3.3: Add to Job

```csharp
// In Job.cs
public Objective PrimaryObjective { get; set; }
public List<Objective> SecondaryObjectives { get; set; } = new();
```

---

## Phase 4: ContractGenerator

### Step 4.1: ContractGenerator Class

**New File**: `src/sim/generation/ContractGenerator.cs`

```csharp
public class ContractGenerator
{
    private readonly GenerationContext context;
    private readonly Random rng;
    
    public ContractGenerator(GenerationContext context)
    {
        this.context = context;
        this.rng = context.Rng != null 
            ? new Random(context.Rng.NextInt(int.MaxValue))
            : new Random();
    }
    
    public List<Job> GenerateContracts(int count = 3)
    {
        var contracts = new List<Job>();
        for (int i = 0; i < count; i++)
        {
            var contract = GenerateSingleContract();
            if (contract != null) contracts.Add(contract);
        }
        return contracts;
    }
}
```

### Step 4.2: Contract Type Selection

```csharp
private ContractType SelectContractType()
{
    var weights = new Dictionary<ContractType, int>
    {
        [ContractType.Assault] = 30 + (context.HubCriminalActivity * 5),
        [ContractType.Extraction] = 20 + (context.HubCriminalActivity * 2)
    };
    // Add other types when implemented
    return WeightedSelect(weights);
}
```

### Step 4.3: Difficulty Scaling

```csharp
private JobDifficulty DetermineDifficulty(SectorNode target)
{
    var baseDiff = target.Type switch
    {
        NodeType.Contested => JobDifficulty.Hard,
        NodeType.Derelict => JobDifficulty.Medium,
        _ => JobDifficulty.Easy
    };
    
    // Adjust for player power
    if (context.PlayerTier >= PowerTier.Veteran)
        baseDiff = ShiftDifficulty(baseDiff, +1);
    if (context.PlayerTier == PowerTier.Rookie && context.CompletedContracts < 3)
        baseDiff = ShiftDifficulty(baseDiff, -1);
    
    return baseDiff;
}
```

### Step 4.4: Reward Calculation

```csharp
private JobReward CalculateReward(JobDifficulty diff, ContractType type, string factionId)
{
    int baseReward = diff switch { Easy => 100, Medium => 200, Hard => 400, _ => 100 };
    float typeMult = type.GetRewardMultiplier();
    float factionMult = context.IsFriendlyWith(factionId) ? 1.15f : 1.0f;
    
    return new JobReward { Money = (int)(baseReward * typeMult * factionMult) };
}
```

---

## Phase 5: Serialization

### Step 5.1: Update SaveData

```csharp
public class JobData
{
    // Existing fields...
    public string ContractType { get; set; }
    public ObjectiveData PrimaryObjective { get; set; }
    public List<ObjectiveData> SecondaryObjectives { get; set; } = new();
}

public class ObjectiveData
{
    public string Id { get; set; }
    public string Type { get; set; }
    public string Description { get; set; }
    public bool IsRequired { get; set; }
    public int BonusRewardPercent { get; set; }
}
```

### Step 5.2: Save Version

```csharp
public static class SaveVersion
{
    public const int Current = 3;
    // 1 - SF3: Initial
    // 2 - MG1: Crew stats, traits
    // 3 - GN1: Contract types, objectives
}
```

---

## Phase 6: Testing

### Test Files

```
tests/sim/generation/
├── GN1GenerationContextTests.cs
├── GN1ContractGeneratorTests.cs
├── GN1DeterminismTests.cs
└── GN1SerializationTests.cs
```

### Key Test Cases

**GenerationContext**:
- `CalculateCrewPower_EmptyCrew_ReturnsZero`
- `CalculateCrewPower_SingleSoldier_CalculatesCorrectly`
- `CalculateCrewPower_ExcludesDeadCrew`
- `PlayerTier_CorrectForPowerRanges`
- `FromCampaign_BuildsContextCorrectly`

**ContractGenerator**:
- `GenerateContracts_ReturnsRequestedCount`
- `GenerateContracts_AllHaveValidContractType`
- `GenerateContracts_AllHavePrimaryObjective`
- `GenerateContracts_RewardsScaleWithDifficulty`
- `GenerateContracts_NoNearbyNodes_ReturnsEmpty`

**Determinism**:
- `SameSeed_ProducesSameContracts`
- `DifferentSeeds_ProducesDifferentContracts`

**Serialization**:
- `Job_RoundTrip_PreservesContractType`
- `Job_RoundTrip_PreservesObjectives`
- `Job_LegacyJobType_MigratesCorrectly`

---

## Manual Test Setup

### Scenario 1: Basic Generation

1. Start new campaign (seed 12345)
2. Open job board at Haven Station
3. **Verify**: 3 contracts displayed with titles, rewards, difficulty
4. **Verify**: Rewards scale with difficulty (Easy ~100, Hard ~400+)

### Scenario 2: Determinism

1. Start campaign with seed 12345
2. Note first 3 contract titles
3. Restart with same seed
4. **Verify**: Same 3 contracts appear

### Scenario 3: Player Power Scaling

1. Start new campaign, complete 5 missions
2. Return to hub, refresh jobs
3. **Verify**: More Medium/Hard contracts appear
4. **Verify**: Rewards are higher than initial contracts

---

## Files to Create

| File | Purpose |
|------|---------|
| `src/sim/generation/ContractType.cs` | 6 contract archetypes |
| `src/sim/generation/ContractTemplates.cs` | Title/description templates |
| `src/sim/generation/GenerationContext.cs` | Context bundling |
| `src/sim/generation/Objective.cs` | Objective system |
| `src/sim/generation/ContractGenerator.cs` | Main generator |
| `tests/sim/generation/GN1*.cs` | Test files |

## Files to Modify

| File | Changes |
|------|---------|
| `src/sim/campaign/Job.cs` | Add ContractType, objectives |
| `src/sim/campaign/JobSystem.cs` | Delegate to ContractGenerator |
| `src/sim/data/SaveData.cs` | Add ObjectiveData, update JobData |

---

## Implementation Order

1. **ContractType.cs** - Foundation enum
2. **Objective.cs** - Objective system
3. **GenerationContext.cs** - Context bundling
4. **ContractTemplates.cs** - Text templates
5. **ContractGenerator.cs** - Main logic
6. **Update Job.cs** - Add new fields
7. **Update JobSystem.cs** - Wire up generator
8. **Update SaveData.cs** - Serialization
9. **Tests** - All test files
10. **Manual verification**

---

## Success Criteria

- [ ] 6 contract types defined (2 implemented in tactical)
- [ ] Contracts generated from player/world context
- [ ] Difficulty scales with player power
- [ ] Rewards follow GN0 formulas
- [ ] Primary + secondary objectives on contracts
- [ ] Deterministic generation (same seed = same contracts)
- [ ] Save/load preserves new fields
- [ ] All tests passing
