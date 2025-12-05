# MG1 – PlayerState & Crew Core: Implementation Plan

**Status**: ✅ Complete  
**Depends on**: MG0 (Concept Finalization) ✅ Complete  
**Completed**: December 2024

This document breaks down **Milestone MG1** from `ROADMAP.md` into concrete implementation steps.

**Goal**: Implement the core player state with crew management, including expanded stats, trait system, and crew operations.

---

## Implementation Summary

### Test Results
- **102 MG1-specific tests** (all passing)
- **313 total tests** in full suite (all passing)

### Files Created
| File | Description |
|------|-------------|
| `src/sim/campaign/StatType.cs` | `CrewStatType` enum for primary stats |
| `src/sim/campaign/Trait.cs` | `TraitCategory`, `CrewStatModifier`, `TraitDef` |
| `src/sim/campaign/TraitRegistry.cs` | 14 default traits |
| `tests/sim/management/MG1CrewStatsTests.cs` | 25 tests |
| `tests/sim/management/MG1TraitTests.cs` | 31 tests |
| `tests/sim/management/MG1CrewOperationsTests.cs` | 25 tests |
| `tests/sim/management/MG1SerializationTests.cs` | 21 tests |

### Files Modified
| File | Changes |
|------|---------|
| `src/sim/campaign/CrewMember.cs` | 6 stats, traits, stat points, derived stats |
| `src/sim/campaign/CampaignState.cs` | HireCrew, FireCrew, AssignTrait, RemoveTrait |
| `src/sim/data/SaveData.cs` | Version 2, expanded CrewMemberData |
| `src/sim/Events.cs` | CrewHiredEvent, CrewFiredEvent, CrewTraitChangedEvent |

---

## MG1 Deliverables Checklist

### Phase 1: Crew Stats Expansion
- [x] **1.1** Rename `Toughness` → `Grit`
- [x] **1.2** Add missing stats: `Tech`, `Savvy`, `Resolve`
- [x] **1.3** Update derived stats (MaxHp formula)
- [x] **1.4** Add role-based starting stats
- [x] **1.5** Update serialization (`CrewMemberData`)

### Phase 2: Trait System Foundation
- [x] **2.1** Create `Trait` class and `TraitCategory` enum
- [x] **2.2** Create `TraitRegistry` for trait definitions
- [x] **2.3** Add `Traits` list to `CrewMember`
- [x] **2.4** Create `CrewStatModifier` struct
- [x] **2.5** Implement trait-based stat calculation
- [x] **2.6** Define initial trait set (14 traits)

### Phase 3: Crew Operations
- [x] **3.1** Implement `HireCrew(name, role, cost)`
- [x] **3.2** Implement `FireCrew(crewId)`
- [x] **3.3** Implement `AssignTrait(crewId, traitId)`
- [x] **3.4** Implement `RemoveTrait(crewId, traitId)`
- [x] **3.5** Enhance `AddXp` with stat point allocation

### Phase 4: Serialization & Integration
- [x] **4.1** Update `CrewMemberData` for new fields
- [x] **4.2** Update save/load round-trip
- [x] **4.3** Increment save version to 2
- [x] **4.4** Publish events for crew changes

### Phase 5: Testing
- [x] **5.1** Unit tests for stat calculations (25 tests)
- [x] **5.2** Unit tests for trait system (31 tests)
- [x] **5.3** Unit tests for crew operations (25 tests)
- [x] **5.4** Serialization round-trip tests (21 tests)
- [x] **5.5** Integration tests with CampaignState

---

## Phase 1: Crew Stats Expansion

### Step 1.1: Rename Toughness → Grit

**File**: `src/sim/campaign/CrewMember.cs`

**Changes**:
```csharp
// Before:
public int Toughness { get; set; } = 0;

// After:
public int Grit { get; set; } = 0;
```

**Also update**:
- `GetMaxHp()` to use `Grit`
- `GetState()` serialization
- `FromState()` deserialization

**Acceptance Criteria**:
- [ ] `Toughness` renamed to `Grit` everywhere
- [ ] Existing saves still load (backward compat in `FromState`)

---

### Step 1.2: Add Missing Stats

**File**: `src/sim/campaign/CrewMember.cs`

**Add properties**:
```csharp
// Primary stats (per CAMPAIGN_FOUNDATIONS §3.1)
public int Grit { get; set; } = 0;       // HP, injury resistance
public int Reflexes { get; set; } = 0;   // Initiative, dodge
public int Aim { get; set; } = 0;        // Ranged accuracy
public int Tech { get; set; } = 0;       // Hacking, repairs
public int Savvy { get; set; } = 0;      // Social checks
public int Resolve { get; set; } = 0;    // Stress tolerance
```

**Acceptance Criteria**:
- [ ] All 6 stats exist on `CrewMember`
- [ ] Default values are 0

---

### Step 1.3: Update Derived Stats

**File**: `src/sim/campaign/CrewMember.cs`

**Update `GetMaxHp()`**:
```csharp
public int GetMaxHp()
{
    int baseHp = 100 + (Grit * 10);
    // Apply trait modifiers (Phase 2)
    return baseHp + GetStatModifier(StatType.MaxHp);
}
```

**Add new derived stat methods**:
```csharp
/// <summary>
/// Get hit chance bonus from Aim (2% per point).
/// </summary>
public int GetHitBonus() => Aim * 2;

/// <summary>
/// Get hacking bonus from Tech (10% per point).
/// </summary>
public int GetHackBonus() => Tech * 10;

/// <summary>
/// Get social check bonus from Savvy (10% per point).
/// </summary>
public int GetTalkBonus() => Savvy * 10;

/// <summary>
/// Get stress threshold from Resolve.
/// </summary>
public int GetStressThreshold() => 50 + (Resolve * 10);
```

**Acceptance Criteria**:
- [ ] Derived stats calculate correctly
- [ ] Formulas match MG0 design

---

### Step 1.4: Role-Based Starting Stats

**File**: `src/sim/campaign/CrewMember.cs`

**Add static factory method**:
```csharp
/// <summary>
/// Create a crew member with role-appropriate starting stats.
/// </summary>
public static CrewMember CreateWithRole(int id, string name, CrewRole role)
{
    var crew = new CrewMember(id, name) { Role = role };
    ApplyRoleStats(crew, role);
    return crew;
}

private static void ApplyRoleStats(CrewMember crew, CrewRole role)
{
    switch (role)
    {
        case CrewRole.Soldier:
            crew.Grit = 3; crew.Reflexes = 2; crew.Aim = 3;
            crew.Tech = 0; crew.Savvy = 0; crew.Resolve = 2;
            break;
        case CrewRole.Medic:
            crew.Grit = 2; crew.Reflexes = 1; crew.Aim = 1;
            crew.Tech = 2; crew.Savvy = 1; crew.Resolve = 3;
            break;
        case CrewRole.Tech:
            crew.Grit = 1; crew.Reflexes = 2; crew.Aim = 1;
            crew.Tech = 3; crew.Savvy = 1; crew.Resolve = 2;
            break;
        case CrewRole.Scout:
            crew.Grit = 2; crew.Reflexes = 3; crew.Aim = 2;
            crew.Tech = 1; crew.Savvy = 1; crew.Resolve = 1;
            break;
    }
}
```

**Update `CampaignState.AddCrew()`**:
```csharp
public CrewMember AddCrew(string name, CrewRole role = CrewRole.Soldier)
{
    var crew = CrewMember.CreateWithRole(nextCrewId, name, role);
    nextCrewId++;
    Crew.Add(crew);
    return crew;
}
```

**Acceptance Criteria**:
- [ ] Each role has distinct starting stats
- [ ] Stats match MG0 design table

---

### Step 1.5: Update Serialization

**File**: `src/sim/data/SaveData.cs`

**Update `CrewMemberData`**:
```csharp
public class CrewMemberData
{
    // ... existing fields ...
    
    // Stats (expanded)
    public int Grit { get; set; }        // Renamed from Toughness
    public int Reflexes { get; set; }
    public int Aim { get; set; }
    public int Tech { get; set; }        // NEW
    public int Savvy { get; set; }       // NEW
    public int Resolve { get; set; }     // NEW
    
    // Traits (NEW)
    public List<string> TraitIds { get; set; } = new();
    
    // Stat points (NEW)
    public int UnspentStatPoints { get; set; }
}
```

**Update `CrewMember.GetState()` and `FromState()`** to handle new fields.

**Backward compatibility**: In `FromState()`, map old `Toughness` to `Grit`:
```csharp
// Handle legacy saves
Grit = data.Grit > 0 ? data.Grit : data.Toughness
```

**Acceptance Criteria**:
- [ ] New stats serialize/deserialize
- [ ] Old saves load correctly (Toughness → Grit)

---

## Phase 2: Trait System Foundation

### Step 2.1: Create Trait Infrastructure

**New File**: `src/sim/campaign/Trait.cs`

```csharp
namespace FringeTactics;

public enum TraitCategory
{
    Background,   // Ex-Military, Smuggler, Corporate
    Personality,  // Brave, Cautious, Reckless
    Acquired,     // Vengeful, Traumatized, Hardened
    Injury        // Damaged Eye, Shattered Knee
}

public enum StatType
{
    Grit, Reflexes, Aim, Tech, Savvy, Resolve,
    MaxHp, HitBonus, DodgeBonus, Initiative
}

/// <summary>
/// A stat modifier applied by a trait.
/// </summary>
public struct StatModifier
{
    public StatType Stat { get; set; }
    public int FlatBonus { get; set; }      // +/- absolute value
    public float PercentBonus { get; set; } // +/- percentage (0.1 = 10%)
    
    public StatModifier(StatType stat, int flat = 0, float percent = 0f)
    {
        Stat = stat;
        FlatBonus = flat;
        PercentBonus = percent;
    }
}

/// <summary>
/// Definition of a trait that can be assigned to crew.
/// </summary>
public class TraitDef
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public TraitCategory Category { get; set; }
    public List<StatModifier> Modifiers { get; set; } = new();
    public List<string> Tags { get; set; } = new();
    
    /// <summary>
    /// If true, this trait cannot be removed (permanent injuries).
    /// </summary>
    public bool IsPermanent { get; set; } = false;
}
```

**Acceptance Criteria**:
- [ ] `TraitCategory` enum exists
- [ ] `StatModifier` struct exists
- [ ] `TraitDef` class exists

---

### Step 2.2: Create TraitRegistry

**New File**: `src/sim/campaign/TraitRegistry.cs`

```csharp
using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// Registry of all trait definitions.
/// </summary>
public static class TraitRegistry
{
    private static Dictionary<string, TraitDef> traits = new();
    private static bool initialized = false;
    
    public static void EnsureInitialized()
    {
        if (initialized) return;
        RegisterDefaultTraits();
        initialized = true;
    }
    
    public static TraitDef Get(string id)
    {
        EnsureInitialized();
        return traits.TryGetValue(id, out var trait) ? trait : null;
    }
    
    public static bool Has(string id)
    {
        EnsureInitialized();
        return traits.ContainsKey(id);
    }
    
    public static IEnumerable<TraitDef> GetByCategory(TraitCategory category)
    {
        EnsureInitialized();
        foreach (var trait in traits.Values)
        {
            if (trait.Category == category) yield return trait;
        }
    }
    
    private static void Register(TraitDef trait)
    {
        traits[trait.Id] = trait;
    }
    
    private static void RegisterDefaultTraits()
    {
        // === Background Traits ===
        Register(new TraitDef
        {
            Id = "ex_military",
            Name = "Ex-Military",
            Category = TraitCategory.Background,
            Description = "Former military training provides combat edge.",
            Modifiers = new() { new(StatType.Aim, flat: 1) },
            Tags = new() { "military", "combat" }
        });
        
        Register(new TraitDef
        {
            Id = "smuggler",
            Name = "Smuggler",
            Category = TraitCategory.Background,
            Description = "Experience moving contraband opens doors.",
            Modifiers = new() { new(StatType.Savvy, flat: 1) },
            Tags = new() { "criminal", "trade" }
        });
        
        Register(new TraitDef
        {
            Id = "corporate",
            Name = "Corporate",
            Category = TraitCategory.Background,
            Description = "Corporate background aids negotiations.",
            Modifiers = new() { new(StatType.Savvy, flat: 1) },
            Tags = new() { "corporate", "social" }
        });
        
        Register(new TraitDef
        {
            Id = "frontier_born",
            Name = "Frontier Born",
            Category = TraitCategory.Background,
            Description = "Raised on the edge, tough and resourceful.",
            Modifiers = new() { new(StatType.Grit, flat: 1) },
            Tags = new() { "frontier", "survival" }
        });
        
        // === Personality Traits ===
        Register(new TraitDef
        {
            Id = "brave",
            Name = "Brave",
            Category = TraitCategory.Personality,
            Description = "Courage under fire.",
            Modifiers = new() { new(StatType.Resolve, flat: 1) },
            Tags = new() { "morale" }
        });
        
        Register(new TraitDef
        {
            Id = "cautious",
            Name = "Cautious",
            Category = TraitCategory.Personality,
            Description = "Careful approach, harder to hit.",
            Modifiers = new() { new(StatType.DodgeBonus, flat: 5) },
            Tags = new() { "defensive" }
        });
        
        Register(new TraitDef
        {
            Id = "reckless",
            Name = "Reckless",
            Category = TraitCategory.Personality,
            Description = "Aggressive but exposed.",
            Modifiers = new() 
            { 
                new(StatType.Aim, flat: 1),
                new(StatType.DodgeBonus, flat: -5)
            },
            Tags = new() { "aggressive" }
        });
        
        Register(new TraitDef
        {
            Id = "merciful",
            Name = "Merciful",
            Category = TraitCategory.Personality,
            Description = "Prefers non-lethal solutions.",
            Tags = new() { "social", "nonlethal" }
        });
        
        // === Acquired Traits ===
        Register(new TraitDef
        {
            Id = "vengeful",
            Name = "Vengeful",
            Category = TraitCategory.Acquired,
            Description = "Driven by revenge.",
            Tags = new() { "motivation" }
        });
        
        Register(new TraitDef
        {
            Id = "hardened",
            Name = "Hardened",
            Category = TraitCategory.Acquired,
            Description = "Seen too much to be shaken.",
            Modifiers = new() { new(StatType.Resolve, flat: 2) },
            Tags = new() { "veteran" }
        });
        
        // === Injury Traits ===
        Register(new TraitDef
        {
            Id = "damaged_eye",
            Name = "Damaged Eye",
            Category = TraitCategory.Injury,
            Description = "Permanent vision impairment.",
            Modifiers = new() { new(StatType.Aim, flat: -2) },
            IsPermanent = true,
            Tags = new() { "injury", "vision" }
        });
        
        Register(new TraitDef
        {
            Id = "shattered_knee",
            Name = "Shattered Knee",
            Category = TraitCategory.Injury,
            Description = "Permanent mobility impairment.",
            Modifiers = new() { new(StatType.Reflexes, flat: -2) },
            IsPermanent = true,
            Tags = new() { "injury", "mobility" }
        });
        
        Register(new TraitDef
        {
            Id = "nerve_damage",
            Name = "Nerve Damage",
            Category = TraitCategory.Injury,
            Description = "Reduced fine motor control.",
            Modifiers = new() { new(StatType.Tech, flat: -2) },
            IsPermanent = true,
            Tags = new() { "injury", "dexterity" }
        });
    }
}
```

**Acceptance Criteria**:
- [ ] `TraitRegistry` provides trait lookup
- [ ] Initial 12+ traits defined
- [ ] Categories properly assigned

---

### Step 2.3: Add Traits to CrewMember

**File**: `src/sim/campaign/CrewMember.cs`

**Add trait storage**:
```csharp
/// <summary>
/// Trait IDs assigned to this crew member.
/// </summary>
public List<string> TraitIds { get; set; } = new();

/// <summary>
/// Unspent stat points from leveling.
/// </summary>
public int UnspentStatPoints { get; set; } = 0;
```

**Add trait methods**:
```csharp
/// <summary>
/// Check if crew has a specific trait.
/// </summary>
public bool HasTrait(string traitId) => TraitIds.Contains(traitId);

/// <summary>
/// Add a trait. Returns false if already has it.
/// </summary>
public bool AddTrait(string traitId)
{
    if (HasTrait(traitId)) return false;
    if (!TraitRegistry.Has(traitId)) return false;
    TraitIds.Add(traitId);
    return true;
}

/// <summary>
/// Remove a trait. Returns false if didn't have it or trait is permanent.
/// </summary>
public bool RemoveTrait(string traitId)
{
    var trait = TraitRegistry.Get(traitId);
    if (trait == null || trait.IsPermanent) return false;
    return TraitIds.Remove(traitId);
}

/// <summary>
/// Get all trait definitions for this crew member.
/// </summary>
public IEnumerable<TraitDef> GetTraits()
{
    foreach (var id in TraitIds)
    {
        var trait = TraitRegistry.Get(id);
        if (trait != null) yield return trait;
    }
}
```

**Acceptance Criteria**:
- [ ] Traits can be added/removed
- [ ] Permanent traits cannot be removed
- [ ] Trait lookup works

---

### Step 2.4: Implement Stat Modifiers

**File**: `src/sim/campaign/CrewMember.cs`

**Add modifier calculation**:
```csharp
/// <summary>
/// Calculate total modifier for a stat from all traits.
/// </summary>
public int GetStatModifier(StatType stat)
{
    int total = 0;
    foreach (var trait in GetTraits())
    {
        foreach (var mod in trait.Modifiers)
        {
            if (mod.Stat == stat)
            {
                total += mod.FlatBonus;
            }
        }
    }
    return total;
}

/// <summary>
/// Get effective stat value (base + modifiers).
/// </summary>
public int GetEffectiveStat(StatType stat)
{
    int baseValue = stat switch
    {
        StatType.Grit => Grit,
        StatType.Reflexes => Reflexes,
        StatType.Aim => Aim,
        StatType.Tech => Tech,
        StatType.Savvy => Savvy,
        StatType.Resolve => Resolve,
        _ => 0
    };
    return baseValue + GetStatModifier(stat);
}
```

**Update derived stat methods to use modifiers**:
```csharp
public int GetMaxHp()
{
    int effectiveGrit = GetEffectiveStat(StatType.Grit);
    int baseHp = 100 + (effectiveGrit * 10);
    return baseHp + GetStatModifier(StatType.MaxHp);
}

public int GetHitBonus()
{
    int effectiveAim = GetEffectiveStat(StatType.Aim);
    return (effectiveAim * 2) + GetStatModifier(StatType.HitBonus);
}
```

**Acceptance Criteria**:
- [ ] Trait modifiers affect stats
- [ ] Derived stats include modifiers
- [ ] Multiple modifiers stack

---

## Phase 3: Crew Operations

### Step 3.1: Implement HireCrew

**File**: `src/sim/campaign/CampaignState.cs`

```csharp
/// <summary>
/// Hire a new crew member. Costs credits.
/// </summary>
/// <param name="name">Crew member name</param>
/// <param name="role">Crew role</param>
/// <param name="cost">Hiring cost in credits</param>
/// <returns>The hired crew member, or null if can't afford</returns>
public CrewMember HireCrew(string name, CrewRole role, int cost)
{
    if (Money < cost)
    {
        SimLog.Log($"[Campaign] Cannot hire {name}: insufficient funds ({Money}/{cost})");
        return null;
    }
    
    int oldMoney = Money;
    Money -= cost;
    
    var crew = CrewMember.CreateWithRole(nextCrewId, name, role);
    nextCrewId++;
    Crew.Add(crew);
    
    SimLog.Log($"[Campaign] Hired {name} ({role}) for {cost} credits");
    
    EventBus?.Publish(new ResourceChangedEvent(
        ResourceTypes.Money, oldMoney, Money, -cost, "hire_crew"));
    EventBus?.Publish(new CrewHiredEvent(crew.Id, crew.Name, role));
    
    return crew;
}
```

**Add event type** in `src/sim/Events.cs`:
```csharp
public record CrewHiredEvent(int CrewId, string Name, CrewRole Role);
```

**Acceptance Criteria**:
- [ ] Hiring costs credits
- [ ] Hiring fails if insufficient funds
- [ ] Event published on hire

---

### Step 3.2: Implement FireCrew

**File**: `src/sim/campaign/CampaignState.cs`

```csharp
/// <summary>
/// Fire a crew member. They are removed from the roster.
/// </summary>
/// <param name="crewId">ID of crew to fire</param>
/// <returns>True if fired, false if not found or dead</returns>
public bool FireCrew(int crewId)
{
    var crew = GetCrewById(crewId);
    if (crew == null)
    {
        SimLog.Log($"[Campaign] Cannot fire crew {crewId}: not found");
        return false;
    }
    
    if (crew.IsDead)
    {
        SimLog.Log($"[Campaign] Cannot fire {crew.Name}: already dead");
        return false;
    }
    
    // Prevent firing last crew member
    if (GetAliveCrew().Count <= 1)
    {
        SimLog.Log($"[Campaign] Cannot fire {crew.Name}: last crew member");
        return false;
    }
    
    Crew.Remove(crew);
    SimLog.Log($"[Campaign] Fired {crew.Name}");
    
    EventBus?.Publish(new CrewFiredEvent(crew.Id, crew.Name));
    
    return true;
}
```

**Add event type**:
```csharp
public record CrewFiredEvent(int CrewId, string Name);
```

**Acceptance Criteria**:
- [ ] Crew can be fired
- [ ] Cannot fire last crew member
- [ ] Cannot fire dead crew
- [ ] Event published on fire

---

### Step 3.3-3.4: Trait Assignment Operations

**File**: `src/sim/campaign/CampaignState.cs`

```csharp
/// <summary>
/// Assign a trait to a crew member.
/// </summary>
public bool AssignTrait(int crewId, string traitId)
{
    var crew = GetCrewById(crewId);
    if (crew == null || crew.IsDead) return false;
    
    if (!crew.AddTrait(traitId)) return false;
    
    var trait = TraitRegistry.Get(traitId);
    SimLog.Log($"[Campaign] {crew.Name} gained trait: {trait?.Name ?? traitId}");
    
    EventBus?.Publish(new CrewTraitChangedEvent(crewId, traitId, gained: true));
    return true;
}

/// <summary>
/// Remove a trait from a crew member.
/// </summary>
public bool RemoveTrait(int crewId, string traitId)
{
    var crew = GetCrewById(crewId);
    if (crew == null) return false;
    
    if (!crew.RemoveTrait(traitId)) return false;
    
    var trait = TraitRegistry.Get(traitId);
    SimLog.Log($"[Campaign] {crew.Name} lost trait: {trait?.Name ?? traitId}");
    
    EventBus?.Publish(new CrewTraitChangedEvent(crewId, traitId, gained: false));
    return true;
}
```

**Add event type**:
```csharp
public record CrewTraitChangedEvent(int CrewId, string TraitId, bool Gained);
```

**Acceptance Criteria**:
- [ ] Traits can be assigned via CampaignState
- [ ] Traits can be removed via CampaignState
- [ ] Events published on changes

---

### Step 3.5: Enhanced XP and Stat Point Allocation

**File**: `src/sim/campaign/CrewMember.cs`

**Update `AddXp()`**:
```csharp
/// <summary>
/// Add XP and check for level up. Awards stat point on level up.
/// </summary>
public bool AddXp(int amount)
{
    Xp += amount;
    if (Xp >= XP_PER_LEVEL)
    {
        Xp -= XP_PER_LEVEL;
        Level++;
        UnspentStatPoints++;
        return true;
    }
    return false;
}

/// <summary>
/// Spend a stat point to increase a stat.
/// </summary>
public bool SpendStatPoint(StatType stat)
{
    if (UnspentStatPoints <= 0) return false;
    
    // Check stat cap (10)
    int current = stat switch
    {
        StatType.Grit => Grit,
        StatType.Reflexes => Reflexes,
        StatType.Aim => Aim,
        StatType.Tech => Tech,
        StatType.Savvy => Savvy,
        StatType.Resolve => Resolve,
        _ => 10 // Invalid stat, will fail cap check
    };
    
    if (current >= 10) return false;
    
    UnspentStatPoints--;
    
    switch (stat)
    {
        case StatType.Grit: Grit++; break;
        case StatType.Reflexes: Reflexes++; break;
        case StatType.Aim: Aim++; break;
        case StatType.Tech: Tech++; break;
        case StatType.Savvy: Savvy++; break;
        case StatType.Resolve: Resolve++; break;
        default: return false;
    }
    
    return true;
}
```

**Acceptance Criteria**:
- [ ] Level up grants stat point
- [ ] Stat points can be spent
- [ ] Stats capped at 10

---

## Phase 4: Serialization & Integration

### Step 4.1: Update CrewMemberData

**File**: `src/sim/data/SaveData.cs`

Already covered in Step 1.5. Ensure all new fields are included.

---

### Step 4.2: Update Save/Load

**File**: `src/sim/campaign/CrewMember.cs`

**Update `GetState()`**:
```csharp
public CrewMemberData GetState()
{
    return new CrewMemberData
    {
        Id = Id,
        Name = Name,
        Role = Role.ToString(),
        IsDead = IsDead,
        Injuries = new List<string>(Injuries),
        Level = Level,
        Xp = Xp,
        Grit = Grit,
        Reflexes = Reflexes,
        Aim = Aim,
        Tech = Tech,
        Savvy = Savvy,
        Resolve = Resolve,
        TraitIds = new List<string>(TraitIds),
        UnspentStatPoints = UnspentStatPoints,
        PreferredWeaponId = PreferredWeaponId
    };
}
```

**Update `FromState()`**:
```csharp
public static CrewMember FromState(CrewMemberData data)
{
    var crew = new CrewMember(data.Id, data.Name)
    {
        Role = Enum.TryParse<CrewRole>(data.Role, out var role) ? role : CrewRole.Soldier,
        IsDead = data.IsDead,
        Injuries = new List<string>(data.Injuries ?? new()),
        Level = data.Level,
        Xp = data.Xp,
        // Handle legacy Toughness → Grit
        Grit = data.Grit > 0 ? data.Grit : data.Toughness,
        Reflexes = data.Reflexes,
        Aim = data.Aim,
        Tech = data.Tech,
        Savvy = data.Savvy,
        Resolve = data.Resolve,
        TraitIds = new List<string>(data.TraitIds ?? new()),
        UnspentStatPoints = data.UnspentStatPoints,
        PreferredWeaponId = data.PreferredWeaponId ?? "rifle"
    };
    return crew;
}
```

---

### Step 4.3: Increment Save Version

**File**: `src/sim/data/SaveData.cs`

```csharp
public static class SaveVersion
{
    public const int Current = 2;
    
    // Version history:
    // 1 - Initial save format (SF3)
    // 2 - MG1: Expanded crew stats, traits
}
```

---

## Phase 5: Testing

### Step 5.1: Crew Stats Tests

**New File**: `tests/sim/management/MG1CrewStatsTests.cs`

```csharp
using GdUnit4;
using static GdUnit4.Assertions;

namespace FringeTactics.Tests;

[TestSuite]
public class MG1CrewStatsTests
{
    [TestCase]
    public void CrewMember_HasAllSixStats()
    {
        var crew = new CrewMember(1, "Test");
        // All stats should exist and default to 0
        AssertThat(crew.Grit).IsEqual(0);
        AssertThat(crew.Reflexes).IsEqual(0);
        AssertThat(crew.Aim).IsEqual(0);
        AssertThat(crew.Tech).IsEqual(0);
        AssertThat(crew.Savvy).IsEqual(0);
        AssertThat(crew.Resolve).IsEqual(0);
    }
    
    [TestCase]
    public void CreateWithRole_Soldier_HasCorrectStats()
    {
        var crew = CrewMember.CreateWithRole(1, "Soldier", CrewRole.Soldier);
        AssertThat(crew.Grit).IsEqual(3);
        AssertThat(crew.Aim).IsEqual(3);
        AssertThat(crew.Reflexes).IsEqual(2);
        AssertThat(crew.Resolve).IsEqual(2);
        AssertThat(crew.Tech).IsEqual(0);
        AssertThat(crew.Savvy).IsEqual(0);
    }
    
    [TestCase]
    public void CreateWithRole_Tech_HasCorrectStats()
    {
        var crew = CrewMember.CreateWithRole(1, "Hacker", CrewRole.Tech);
        AssertThat(crew.Tech).IsEqual(3);
        AssertThat(crew.Reflexes).IsEqual(2);
        AssertThat(crew.Grit).IsEqual(1);
    }
    
    [TestCase]
    public void GetMaxHp_CalculatesFromGrit()
    {
        var crew = new CrewMember(1, "Test") { Grit = 3 };
        // 100 + (3 * 10) = 130
        AssertThat(crew.GetMaxHp()).IsEqual(130);
    }
    
    [TestCase]
    public void GetHitBonus_CalculatesFromAim()
    {
        var crew = new CrewMember(1, "Test") { Aim = 5 };
        // 5 * 2 = 10%
        AssertThat(crew.GetHitBonus()).IsEqual(10);
    }
    
    [TestCase]
    public void SpendStatPoint_IncreasesStatAndDecrementsPoints()
    {
        var crew = new CrewMember(1, "Test") { UnspentStatPoints = 2 };
        
        bool result = crew.SpendStatPoint(StatType.Aim);
        
        AssertThat(result).IsTrue();
        AssertThat(crew.Aim).IsEqual(1);
        AssertThat(crew.UnspentStatPoints).IsEqual(1);
    }
    
    [TestCase]
    public void SpendStatPoint_FailsWhenNoPoints()
    {
        var crew = new CrewMember(1, "Test") { UnspentStatPoints = 0 };
        
        bool result = crew.SpendStatPoint(StatType.Aim);
        
        AssertThat(result).IsFalse();
        AssertThat(crew.Aim).IsEqual(0);
    }
    
    [TestCase]
    public void SpendStatPoint_FailsAtCap()
    {
        var crew = new CrewMember(1, "Test") 
        { 
            Aim = 10, 
            UnspentStatPoints = 1 
        };
        
        bool result = crew.SpendStatPoint(StatType.Aim);
        
        AssertThat(result).IsFalse();
        AssertThat(crew.Aim).IsEqual(10);
        AssertThat(crew.UnspentStatPoints).IsEqual(1);
    }
    
    [TestCase]
    public void AddXp_LevelUp_GrantsStatPoint()
    {
        var crew = new CrewMember(1, "Test") { Xp = 90 };
        
        bool leveledUp = crew.AddXp(15);
        
        AssertThat(leveledUp).IsTrue();
        AssertThat(crew.Level).IsEqual(2);
        AssertThat(crew.UnspentStatPoints).IsEqual(1);
        AssertThat(crew.Xp).IsEqual(5); // 90 + 15 - 100 = 5
    }
}
```

---

### Step 5.2: Trait System Tests

**New File**: `tests/sim/management/MG1TraitTests.cs`

```csharp
using GdUnit4;
using static GdUnit4.Assertions;
using System.Linq;

namespace FringeTactics.Tests;

[TestSuite]
public class MG1TraitTests
{
    [TestCase]
    public void TraitRegistry_HasDefaultTraits()
    {
        TraitRegistry.EnsureInitialized();
        
        AssertThat(TraitRegistry.Has("ex_military")).IsTrue();
        AssertThat(TraitRegistry.Has("brave")).IsTrue();
        AssertThat(TraitRegistry.Has("damaged_eye")).IsTrue();
    }
    
    [TestCase]
    public void TraitRegistry_Get_ReturnsCorrectTrait()
    {
        var trait = TraitRegistry.Get("ex_military");
        
        AssertThat(trait).IsNotNull();
        AssertThat(trait.Name).IsEqual("Ex-Military");
        AssertThat(trait.Category).IsEqual(TraitCategory.Background);
    }
    
    [TestCase]
    public void TraitRegistry_GetByCategory_FiltersCorrectly()
    {
        var injuries = TraitRegistry.GetByCategory(TraitCategory.Injury).ToList();
        
        AssertThat(injuries.Count).IsGreaterEqual(3);
        foreach (var trait in injuries)
        {
            AssertThat(trait.Category).IsEqual(TraitCategory.Injury);
        }
    }
    
    [TestCase]
    public void CrewMember_AddTrait_AddsSuccessfully()
    {
        var crew = new CrewMember(1, "Test");
        
        bool result = crew.AddTrait("brave");
        
        AssertThat(result).IsTrue();
        AssertThat(crew.HasTrait("brave")).IsTrue();
    }
    
    [TestCase]
    public void CrewMember_AddTrait_FailsIfAlreadyHas()
    {
        var crew = new CrewMember(1, "Test");
        crew.AddTrait("brave");
        
        bool result = crew.AddTrait("brave");
        
        AssertThat(result).IsFalse();
    }
    
    [TestCase]
    public void CrewMember_RemoveTrait_RemovesSuccessfully()
    {
        var crew = new CrewMember(1, "Test");
        crew.AddTrait("brave");
        
        bool result = crew.RemoveTrait("brave");
        
        AssertThat(result).IsTrue();
        AssertThat(crew.HasTrait("brave")).IsFalse();
    }
    
    [TestCase]
    public void CrewMember_RemoveTrait_FailsForPermanent()
    {
        var crew = new CrewMember(1, "Test");
        crew.AddTrait("damaged_eye");
        
        bool result = crew.RemoveTrait("damaged_eye");
        
        AssertThat(result).IsFalse();
        AssertThat(crew.HasTrait("damaged_eye")).IsTrue();
    }
    
    [TestCase]
    public void CrewMember_TraitModifiers_AffectStats()
    {
        var crew = new CrewMember(1, "Test") { Aim = 3 };
        crew.AddTrait("ex_military"); // +1 Aim
        
        int effectiveAim = crew.GetEffectiveStat(StatType.Aim);
        
        AssertThat(effectiveAim).IsEqual(4);
    }
    
    [TestCase]
    public void CrewMember_MultipleTraits_StackModifiers()
    {
        var crew = new CrewMember(1, "Test") { Aim = 3 };
        crew.AddTrait("ex_military"); // +1 Aim
        crew.AddTrait("reckless");    // +1 Aim
        
        int effectiveAim = crew.GetEffectiveStat(StatType.Aim);
        
        AssertThat(effectiveAim).IsEqual(5);
    }
    
    [TestCase]
    public void CrewMember_InjuryTrait_ReducesStat()
    {
        var crew = new CrewMember(1, "Test") { Aim = 5 };
        crew.AddTrait("damaged_eye"); // -2 Aim
        
        int effectiveAim = crew.GetEffectiveStat(StatType.Aim);
        
        AssertThat(effectiveAim).IsEqual(3);
    }
    
    [TestCase]
    public void GetMaxHp_IncludesTraitModifiers()
    {
        var crew = new CrewMember(1, "Test") { Grit = 2 };
        crew.AddTrait("frontier_born"); // +1 Grit
        
        // Effective Grit = 3, MaxHp = 100 + (3 * 10) = 130
        AssertThat(crew.GetMaxHp()).IsEqual(130);
    }
}
```

---

### Step 5.3: Crew Operations Tests

**New File**: `tests/sim/management/MG1CrewOperationsTests.cs`

```csharp
using GdUnit4;
using static GdUnit4.Assertions;

namespace FringeTactics.Tests;

[TestSuite]
public class MG1CrewOperationsTests
{
    [TestCase]
    public void HireCrew_Success_DeductsCost()
    {
        var campaign = CampaignState.CreateNew();
        campaign.Money = 500;
        
        var crew = campaign.HireCrew("NewGuy", CrewRole.Soldier, 100);
        
        AssertThat(crew).IsNotNull();
        AssertThat(campaign.Money).IsEqual(400);
        AssertThat(campaign.Crew.Contains(crew)).IsTrue();
    }
    
    [TestCase]
    public void HireCrew_InsufficientFunds_ReturnsNull()
    {
        var campaign = CampaignState.CreateNew();
        campaign.Money = 50;
        
        var crew = campaign.HireCrew("NewGuy", CrewRole.Soldier, 100);
        
        AssertThat(crew).IsNull();
        AssertThat(campaign.Money).IsEqual(50);
    }
    
    [TestCase]
    public void HireCrew_HasRoleStats()
    {
        var campaign = CampaignState.CreateNew();
        campaign.Money = 500;
        
        var crew = campaign.HireCrew("Techie", CrewRole.Tech, 100);
        
        AssertThat(crew.Tech).IsEqual(3);
        AssertThat(crew.Role).IsEqual(CrewRole.Tech);
    }
    
    [TestCase]
    public void FireCrew_Success_RemovesFromRoster()
    {
        var campaign = CampaignState.CreateNew();
        int initialCount = campaign.Crew.Count;
        var crewToFire = campaign.Crew[0];
        
        bool result = campaign.FireCrew(crewToFire.Id);
        
        AssertThat(result).IsTrue();
        AssertThat(campaign.Crew.Count).IsEqual(initialCount - 1);
        AssertThat(campaign.GetCrewById(crewToFire.Id)).IsNull();
    }
    
    [TestCase]
    public void FireCrew_LastCrew_Fails()
    {
        var campaign = CampaignState.CreateNew();
        // Remove all but one
        while (campaign.Crew.Count > 1)
        {
            campaign.Crew.RemoveAt(campaign.Crew.Count - 1);
        }
        
        bool result = campaign.FireCrew(campaign.Crew[0].Id);
        
        AssertThat(result).IsFalse();
        AssertThat(campaign.Crew.Count).IsEqual(1);
    }
    
    [TestCase]
    public void FireCrew_DeadCrew_Fails()
    {
        var campaign = CampaignState.CreateNew();
        var crew = campaign.Crew[0];
        crew.IsDead = true;
        
        bool result = campaign.FireCrew(crew.Id);
        
        AssertThat(result).IsFalse();
    }
    
    [TestCase]
    public void AssignTrait_Success()
    {
        var campaign = CampaignState.CreateNew();
        var crew = campaign.Crew[0];
        
        bool result = campaign.AssignTrait(crew.Id, "brave");
        
        AssertThat(result).IsTrue();
        AssertThat(crew.HasTrait("brave")).IsTrue();
    }
    
    [TestCase]
    public void RemoveTrait_Success()
    {
        var campaign = CampaignState.CreateNew();
        var crew = campaign.Crew[0];
        crew.AddTrait("brave");
        
        bool result = campaign.RemoveTrait(crew.Id, "brave");
        
        AssertThat(result).IsTrue();
        AssertThat(crew.HasTrait("brave")).IsFalse();
    }
}
```

---

### Step 5.4: Serialization Tests

**New File**: `tests/sim/management/MG1SerializationTests.cs`

```csharp
using GdUnit4;
using static GdUnit4.Assertions;
using System.Collections.Generic;

namespace FringeTactics.Tests;

[TestSuite]
public class MG1SerializationTests
{
    [TestCase]
    public void CrewMember_RoundTrip_PreservesAllStats()
    {
        var original = new CrewMember(42, "TestCrew")
        {
            Role = CrewRole.Tech,
            Grit = 2,
            Reflexes = 3,
            Aim = 4,
            Tech = 5,
            Savvy = 1,
            Resolve = 2,
            Level = 3,
            Xp = 50,
            UnspentStatPoints = 2
        };
        original.AddTrait("ex_military");
        original.AddTrait("brave");
        
        var data = original.GetState();
        var restored = CrewMember.FromState(data);
        
        AssertThat(restored.Id).IsEqual(42);
        AssertThat(restored.Name).IsEqual("TestCrew");
        AssertThat(restored.Role).IsEqual(CrewRole.Tech);
        AssertThat(restored.Grit).IsEqual(2);
        AssertThat(restored.Reflexes).IsEqual(3);
        AssertThat(restored.Aim).IsEqual(4);
        AssertThat(restored.Tech).IsEqual(5);
        AssertThat(restored.Savvy).IsEqual(1);
        AssertThat(restored.Resolve).IsEqual(2);
        AssertThat(restored.Level).IsEqual(3);
        AssertThat(restored.Xp).IsEqual(50);
        AssertThat(restored.UnspentStatPoints).IsEqual(2);
        AssertThat(restored.HasTrait("ex_military")).IsTrue();
        AssertThat(restored.HasTrait("brave")).IsTrue();
    }
    
    [TestCase]
    public void CrewMember_LegacySave_MigratesToughnessToGrit()
    {
        // Simulate a v1 save with Toughness instead of Grit
        var legacyData = new CrewMemberData
        {
            Id = 1,
            Name = "OldCrew",
            Role = "Soldier",
            Toughness = 5, // Old field
            Grit = 0,      // New field (empty in old saves)
            Aim = 3,
            Reflexes = 2
        };
        
        var restored = CrewMember.FromState(legacyData);
        
        AssertThat(restored.Grit).IsEqual(5);
    }
    
    [TestCase]
    public void CampaignState_RoundTrip_PreservesCrewTraits()
    {
        var campaign = CampaignState.CreateNew();
        campaign.Crew[0].AddTrait("brave");
        campaign.Crew[0].AddTrait("ex_military");
        campaign.Crew[1].AddTrait("cautious");
        
        var data = campaign.GetState();
        var restored = CampaignState.FromState(data);
        
        AssertThat(restored.Crew[0].HasTrait("brave")).IsTrue();
        AssertThat(restored.Crew[0].HasTrait("ex_military")).IsTrue();
        AssertThat(restored.Crew[1].HasTrait("cautious")).IsTrue();
    }
}
```

---

## Manual Testing Setup

### Test Scenario 1: Crew Stats Verification

1. Start a new campaign
2. Verify starting crew have role-appropriate stats:
   - Soldiers: High Grit/Aim
   - Medics: High Tech/Resolve
   - Techs: High Tech
   - Scouts: High Reflexes
3. Check derived stats (HP, hit bonus) match formulas

### Test Scenario 2: Trait System

1. Start a new campaign
2. Use DevTools to assign traits:
   ```
   DevTools.AssignTrait(crewId, "brave")
   DevTools.AssignTrait(crewId, "ex_military")
   ```
3. Verify stat modifiers apply correctly
4. Try to remove a permanent injury trait (should fail)

### Test Scenario 3: Hire/Fire Operations

1. Start a new campaign with 200+ credits
2. Hire a new crew member (cost 100)
3. Verify credits deducted
4. Verify new crew has role stats
5. Fire a crew member
6. Verify roster updated
7. Try to fire last crew member (should fail)

### Test Scenario 4: Level Up and Stat Points

1. Complete a mission to gain XP
2. Verify level up grants stat point
3. Spend stat point on a stat
4. Verify stat increased
5. Try to exceed stat cap (should fail)

### Test Scenario 5: Save/Load Round-Trip

1. Create campaign with:
   - Custom crew stats
   - Assigned traits
   - Unspent stat points
2. Save game
3. Load game
4. Verify all data preserved

---

## Implementation Order

### Week 1: Stats Foundation
- Step 1.1: Rename Toughness → Grit
- Step 1.2: Add missing stats
- Step 1.3: Update derived stats
- Step 1.4: Role-based starting stats
- Step 1.5: Update serialization
- Step 5.1: Stats unit tests

### Week 2: Trait System
- Step 2.1: Create Trait infrastructure
- Step 2.2: Create TraitRegistry
- Step 2.3: Add traits to CrewMember
- Step 2.4: Implement stat modifiers
- Step 5.2: Trait unit tests

### Week 3: Operations & Integration
- Step 3.1: HireCrew
- Step 3.2: FireCrew
- Step 3.3-3.4: Trait assignment
- Step 3.5: Enhanced XP/stat points
- Step 5.3: Operations tests

### Week 4: Polish & Testing
- Step 4.1-4.4: Serialization updates
- Step 5.4: Serialization tests
- Step 5.5: Integration tests
- Manual testing
- Bug fixes

---

## Success Criteria for MG1

When MG1 is complete:

1. ✅ `CrewMember` has all 6 primary stats (Grit, Reflexes, Aim, Tech, Savvy, Resolve)
2. ✅ Each role has distinct starting stats
3. ✅ Trait system exists with 12+ initial traits
4. ✅ Traits apply stat modifiers correctly
5. ✅ `HireCrew` and `FireCrew` operations work
6. ✅ Level up grants stat points
7. ✅ Stat points can be spent (with cap enforcement)
8. ✅ All data serializes/deserializes correctly
9. ✅ Old saves migrate Toughness → Grit
10. ✅ All unit tests pass

**Natural Pause Point**: After MG1, the crew system is fully functional. MG2 adds Ship & Resources.

---

## File Summary

| File | Action | Description |
|------|--------|-------------|
| `src/sim/campaign/CrewMember.cs` | MODIFY | Expand stats, add traits |
| `src/sim/campaign/Trait.cs` | NEW | Trait classes and enums |
| `src/sim/campaign/TraitRegistry.cs` | NEW | Trait definitions |
| `src/sim/campaign/CampaignState.cs` | MODIFY | Add crew operations |
| `src/sim/data/SaveData.cs` | MODIFY | Expand CrewMemberData |
| `src/sim/Events.cs` | MODIFY | Add crew events |
| `tests/sim/management/MG1CrewStatsTests.cs` | NEW | Stats tests |
| `tests/sim/management/MG1TraitTests.cs` | NEW | Trait tests |
| `tests/sim/management/MG1CrewOperationsTests.cs` | NEW | Operations tests |
| `tests/sim/management/MG1SerializationTests.cs` | NEW | Serialization tests |

---

## Dependencies on Other Milestones

### Tactical Integration (M7 already complete)
- `MissionOutputBuilder` already handles crew outcomes
- XP calculation already exists
- Injury application already works

### Future MG2 Dependencies
- Ship system will use similar patterns
- Resource operations will follow same event patterns

---

## Open Questions

1. **Trait Generation**: Should new hires come with random traits?
   - *Recommendation*: Yes, 1 background + 0-1 personality

2. **Hiring Cost**: What should the base hiring cost be?
   - *Recommendation*: 100 credits base, +50 per level

3. **Trait Visibility**: Should trait effects be shown in UI?
   - *Recommendation*: Yes, show modifiers in tooltip

4. **Stat Point UI**: How should players allocate stat points?
   - *Recommendation*: Defer to MG3 (Tactical Integration) for UI
