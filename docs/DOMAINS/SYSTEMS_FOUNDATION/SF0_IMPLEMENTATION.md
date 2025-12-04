# SF0 – RNG Streams & Config Loading: Implementation Plan

This document breaks down **Milestone SF0** from `ROADMAP.md` into concrete implementation steps.

**Goal**: Establish deterministic randomness with separate streams and formalize data-driven configuration loading with validation.

---

## Current State Assessment

### What We Have (Existing Code)

| Component | Status | Notes |
|-----------|--------|-------|
| `CombatRng` | ⚠️ Partial | Single-stream RNG for tactical, no campaign stream |
| `DataLoader` | ✅ Exists | Generic JSON loading with Godot FileAccess |
| `Definitions` | ✅ Exists | Static registry for weapons/enemies/abilities |
| `WeaponDef` / `EnemyDef` / `AbilityDef` | ✅ Exists | Typed definition structs |

### What SF0 Requires vs What We Have

| SF0 Requirement | Current Status | Gap |
|-----------------|----------------|-----|
| Separate RNG streams (campaign, tactical) | ❌ Missing | Only `CombatRng` exists, no campaign stream |
| Seed storage and restoration | ⚠️ Partial | `CombatRng.Seed` stored, but no state serialization |
| Player-visible campaign seed | ❌ Missing | No campaign-level seed concept |
| Config loading from JSON | ✅ Complete | `DataLoader` works |
| Typed registry access | ✅ Complete | `Definitions.Weapons.Get(id)` pattern |
| Validation on load | ❌ Missing | No required field checks, silent fallbacks |
| Clear error messages | ⚠️ Partial | `GD.PrintErr` on parse failure, but no field validation |

---

## Architecture Decisions

### RNG Stream Design

**Decision**: Create an `RngService` that manages multiple named streams, each with independent state.

**Rationale**:
- Campaign layer needs its own RNG for world sim, contracts, encounters (per CAMPAIGN_FOUNDATIONS 6.1)
- Tactical layer already uses `CombatRng` but should integrate with the service
- Streams must be isolated: consuming from one doesn't affect others
- State must be serializable for save/load

**Streams for SF0**:
- `campaign` – World sim, contract generation, encounter rolls
- `tactical` – Combat rolls, AI decisions (replaces current `CombatRng`)

**Future streams** (not SF0):
- `economy` – Price fluctuations, trade events (optional per CAMPAIGN_FOUNDATIONS 6.1)

### Config Registry Design

**Decision**: Enhance `Definitions` with validation and clearer error handling.

**Rationale**:
- Current system works but silently falls back to hardcoded defaults
- Development builds should fail fast on malformed data
- Production builds can warn but continue with fallbacks
- Validation should check required fields and value ranges

### Engine-Light Principle

**Decision**: `RngService` and `ConfigRegistry` live in `src/sim/` with no Godot dependencies.

**Rationale**:
- Per architecture guidelines: "Inside doesn't know about Godot nodes"
- `DataLoader` already uses Godot's `FileAccess` but that's acceptable for file I/O
- RNG service is pure C# with `System.Random`

---

## Implementation Steps

### Phase 1: RNG Service Foundation (Priority: Critical)

The RNG service is the foundation for deterministic simulation.

#### Step 1.1: Create RngStream Class

**New File**: `src/sim/RngStream.cs`

**Purpose**: A single RNG stream with serializable state.

```csharp
using System;
using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// A single seeded RNG stream with serializable state.
/// Wraps System.Random with additional utility methods.
/// </summary>
public class RngStream
{
    private Random random;
    
    /// <summary>
    /// The original seed used to create this stream.
    /// </summary>
    public int Seed { get; private set; }
    
    /// <summary>
    /// Number of values consumed from this stream.
    /// Used for state restoration.
    /// </summary>
    public int CallCount { get; private set; }
    
    /// <summary>
    /// Name of this stream for debugging.
    /// </summary>
    public string Name { get; }
    
    public RngStream(string name, int seed)
    {
        Name = name;
        Seed = seed;
        CallCount = 0;
        random = new Random(seed);
    }
    
    /// <summary>
    /// Restore stream to a specific state by replaying calls.
    /// </summary>
    public void RestoreState(int seed, int callCount)
    {
        Seed = seed;
        random = new Random(seed);
        CallCount = 0;
        
        // Fast-forward to the saved state
        for (int i = 0; i < callCount; i++)
        {
            random.Next();
            CallCount++;
        }
    }
    
    /// <summary>
    /// Get state for serialization.
    /// </summary>
    public RngStreamState GetState() => new()
    {
        Name = Name,
        Seed = Seed,
        CallCount = CallCount
    };
    
    // --- RNG Methods ---
    
    public float NextFloat()
    {
        CallCount++;
        return (float)random.NextDouble();
    }
    
    public int NextInt(int max)
    {
        CallCount++;
        return random.Next(max);
    }
    
    public int NextInt(int min, int max)
    {
        CallCount++;
        return random.Next(min, max);
    }
    
    public bool Roll(float probability)
    {
        return NextFloat() < probability;
    }
    
    public T Pick<T>(IList<T> list)
    {
        if (list == null || list.Count == 0)
            return default;
        return list[NextInt(list.Count)];
    }
    
    public void Shuffle<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = NextInt(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}

/// <summary>
/// Serializable state for an RNG stream.
/// </summary>
public class RngStreamState
{
    public string Name { get; set; }
    public int Seed { get; set; }
    public int CallCount { get; set; }
}
```

**Acceptance Criteria**:
- [ ] `RngStream` class with seed, call count tracking
- [ ] `RestoreState()` correctly replays to saved position
- [ ] `GetState()` returns serializable state
- [ ] All RNG methods increment `CallCount`

---

#### Step 1.2: Create RngService

**New File**: `src/sim/RngService.cs`

**Purpose**: Manages multiple named RNG streams.

```csharp
using System;
using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// Central service for deterministic RNG with multiple isolated streams.
/// Each stream is independent: consuming from one doesn't affect others.
/// </summary>
public class RngService
{
    private readonly Dictionary<string, RngStream> streams = new();
    
    /// <summary>
    /// Master seed used to derive stream seeds.
    /// This is the player-visible campaign seed.
    /// </summary>
    public int MasterSeed { get; private set; }
    
    // Well-known stream names
    public const string CampaignStream = "campaign";
    public const string TacticalStream = "tactical";
    
    /// <summary>
    /// Initialize with a master seed. Derives stream seeds deterministically.
    /// </summary>
    public RngService(int masterSeed)
    {
        MasterSeed = masterSeed;
        InitializeStreams();
    }
    
    /// <summary>
    /// Initialize with a random master seed.
    /// </summary>
    public RngService() : this(Environment.TickCount)
    {
    }
    
    private void InitializeStreams()
    {
        // Derive stream seeds from master seed + stream name hash
        CreateStream(CampaignStream, DeriveStreamSeed(CampaignStream));
        CreateStream(TacticalStream, DeriveStreamSeed(TacticalStream));
    }
    
    private int DeriveStreamSeed(string streamName)
    {
        // Combine master seed with stream name hash for deterministic derivation
        return MasterSeed ^ streamName.GetHashCode();
    }
    
    private void CreateStream(string name, int seed)
    {
        streams[name] = new RngStream(name, seed);
    }
    
    /// <summary>
    /// Get a stream by name. Throws if stream doesn't exist.
    /// </summary>
    public RngStream GetStream(string name)
    {
        if (!streams.TryGetValue(name, out var stream))
        {
            throw new ArgumentException($"Unknown RNG stream: {name}");
        }
        return stream;
    }
    
    /// <summary>
    /// Shortcut for campaign stream.
    /// </summary>
    public RngStream Campaign => GetStream(CampaignStream);
    
    /// <summary>
    /// Shortcut for tactical stream.
    /// </summary>
    public RngStream Tactical => GetStream(TacticalStream);
    
    /// <summary>
    /// Reset the tactical stream with a new seed.
    /// Called at mission start for per-mission determinism.
    /// </summary>
    public void ResetTacticalStream(int missionSeed)
    {
        streams[TacticalStream] = new RngStream(TacticalStream, missionSeed);
    }
    
    /// <summary>
    /// Get state of all streams for serialization.
    /// </summary>
    public RngServiceState GetState()
    {
        var state = new RngServiceState
        {
            MasterSeed = MasterSeed,
            Streams = new List<RngStreamState>()
        };
        
        foreach (var stream in streams.Values)
        {
            state.Streams.Add(stream.GetState());
        }
        
        return state;
    }
    
    /// <summary>
    /// Restore all streams from saved state.
    /// </summary>
    public void RestoreState(RngServiceState state)
    {
        MasterSeed = state.MasterSeed;
        
        foreach (var streamState in state.Streams)
        {
            if (streams.TryGetValue(streamState.Name, out var stream))
            {
                stream.RestoreState(streamState.Seed, streamState.CallCount);
            }
            else
            {
                // Stream didn't exist, create it
                var newStream = new RngStream(streamState.Name, streamState.Seed);
                newStream.RestoreState(streamState.Seed, streamState.CallCount);
                streams[streamState.Name] = newStream;
            }
        }
    }
}

/// <summary>
/// Serializable state for the entire RNG service.
/// </summary>
public class RngServiceState
{
    public int MasterSeed { get; set; }
    public List<RngStreamState> Streams { get; set; } = new();
}
```

**Acceptance Criteria**:
- [ ] `RngService` manages multiple named streams
- [ ] `MasterSeed` is player-visible and deterministic
- [ ] Stream seeds derived deterministically from master seed
- [ ] `GetState()` / `RestoreState()` round-trip correctly
- [ ] `ResetTacticalStream()` allows per-mission seeds

---

#### Step 1.3: Migrate CombatState to Use RngService

**File**: `src/sim/combat/state/CombatState.cs`

**Changes**:
- Replace `CombatRng` field with `RngStream` from `RngService`
- Accept `RngStream` in constructor or via `RngService` reference

```csharp
// Before:
public CombatRng Rng { get; }

// After:
public RngStream Rng { get; }

// Constructor change:
public CombatState(MissionConfig config, RngStream tacticalRng)
{
    Rng = tacticalRng;
    // ... rest of initialization
}
```

**Migration Notes**:
- `CombatRng` API matches `RngStream` API, so call sites don't change
- `MissionFactory` needs to accept `RngService` or `RngStream`
- Tests need to create `RngStream` instead of `CombatRng`

**Acceptance Criteria**:
- [ ] `CombatState` uses `RngStream` instead of `CombatRng`
- [ ] All combat RNG calls go through the tactical stream
- [ ] Existing tests still pass (with updated RNG creation)

---

#### Step 1.4: Deprecate CombatRng

**File**: `src/sim/combat/data/CombatRng.cs`

**Action**: Mark as `[Obsolete]` with migration message, or delete if no external dependencies.

```csharp
[Obsolete("Use RngService.Tactical instead. Will be removed in next milestone.")]
public class CombatRng { ... }
```

**Acceptance Criteria**:
- [ ] `CombatRng` marked obsolete or removed
- [ ] No direct usages of `CombatRng` remain

---

### Phase 2: Config Validation (Priority: High)

Formalize config loading with validation and clear error reporting.

#### Step 2.1: Create ValidationResult Type

**New File**: `src/sim/data/ValidationResult.cs`

```csharp
using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// Result of validating a data definition.
/// </summary>
public class ValidationResult
{
    public bool IsValid => Errors.Count == 0;
    public List<string> Errors { get; } = new();
    public List<string> Warnings { get; } = new();
    
    public void AddError(string message) => Errors.Add(message);
    public void AddWarning(string message) => Warnings.Add(message);
    
    public static ValidationResult Success() => new();
    
    public static ValidationResult Error(string message)
    {
        var result = new ValidationResult();
        result.AddError(message);
        return result;
    }
}
```

**Acceptance Criteria**:
- [ ] `ValidationResult` type exists
- [ ] Can accumulate multiple errors/warnings
- [ ] `IsValid` returns true only when no errors

---

#### Step 2.2: Add Validation to Definition Types

**File**: `src/sim/data/Definitions.cs`

**Add validation methods to each definition type**:

```csharp
public class WeaponDef
{
    // ... existing properties ...
    
    public ValidationResult Validate()
    {
        var result = new ValidationResult();
        
        if (string.IsNullOrEmpty(Id))
            result.AddError("WeaponDef: Id is required");
        if (string.IsNullOrEmpty(Name))
            result.AddError($"WeaponDef[{Id}]: Name is required");
        if (Damage <= 0)
            result.AddError($"WeaponDef[{Id}]: Damage must be positive");
        if (Range <= 0)
            result.AddError($"WeaponDef[{Id}]: Range must be positive");
        if (CooldownTicks < 0)
            result.AddError($"WeaponDef[{Id}]: CooldownTicks cannot be negative");
        if (Accuracy < 0 || Accuracy > 1)
            result.AddError($"WeaponDef[{Id}]: Accuracy must be between 0 and 1");
        if (MagazineSize <= 0)
            result.AddError($"WeaponDef[{Id}]: MagazineSize must be positive");
        if (ReloadTicks < 0)
            result.AddError($"WeaponDef[{Id}]: ReloadTicks cannot be negative");
            
        return result;
    }
}

public class EnemyDef
{
    // ... existing properties ...
    
    public ValidationResult Validate()
    {
        var result = new ValidationResult();
        
        if (string.IsNullOrEmpty(Id))
            result.AddError("EnemyDef: Id is required");
        if (string.IsNullOrEmpty(Name))
            result.AddError($"EnemyDef[{Id}]: Name is required");
        if (Hp <= 0)
            result.AddError($"EnemyDef[{Id}]: Hp must be positive");
        if (string.IsNullOrEmpty(WeaponId))
            result.AddError($"EnemyDef[{Id}]: WeaponId is required");
            
        return result;
    }
}

public class AbilityDef
{
    // ... existing properties ...
    
    public ValidationResult Validate()
    {
        var result = new ValidationResult();
        
        if (string.IsNullOrEmpty(Id))
            result.AddError("AbilityDef: Id is required");
        if (string.IsNullOrEmpty(Name))
            result.AddError($"AbilityDef[{Id}]: Name is required");
        if (Range < 0)
            result.AddError($"AbilityDef[{Id}]: Range cannot be negative");
        if (Cooldown < 0)
            result.AddError($"AbilityDef[{Id}]: Cooldown cannot be negative");
        if (Delay < 0)
            result.AddError($"AbilityDef[{Id}]: Delay cannot be negative");
        if (Radius < 0)
            result.AddError($"AbilityDef[{Id}]: Radius cannot be negative");
            
        return result;
    }
}
```

**Acceptance Criteria**:
- [ ] Each definition type has a `Validate()` method
- [ ] Required fields are checked
- [ ] Value ranges are validated
- [ ] Error messages include the definition ID for context

---

#### Step 2.3: Create ConfigRegistry with Validation

**New File**: `src/sim/data/ConfigRegistry.cs`

**Purpose**: Enhanced registry that validates on load and provides clear error reporting.

```csharp
using System;
using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// Configuration loading result with validation status.
/// </summary>
public class ConfigLoadResult
{
    public bool Success { get; set; }
    public List<string> Errors { get; } = new();
    public List<string> Warnings { get; } = new();
    public int ItemsLoaded { get; set; }
    
    public void Merge(ValidationResult validation)
    {
        Errors.AddRange(validation.Errors);
        Warnings.AddRange(validation.Warnings);
    }
}

/// <summary>
/// Central registry for game configuration data.
/// Loads from JSON with validation and clear error reporting.
/// </summary>
public class ConfigRegistry
{
    private const string WeaponsPath = "res://data/weapons.json";
    private const string EnemiesPath = "res://data/enemies.json";
    private const string AbilitiesPath = "res://data/abilities.json";
    
    public WeaponDefinitions Weapons { get; private set; } = new();
    public EnemyDefinitions Enemies { get; private set; } = new();
    public AbilityDefinitions Abilities { get; private set; } = new();
    
    public bool IsLoaded { get; private set; }
    public ConfigLoadResult LastLoadResult { get; private set; }
    
    /// <summary>
    /// If true, throw on validation errors. Set to true for development.
    /// </summary>
    public bool FailFastOnErrors { get; set; } = false;
    
    /// <summary>
    /// Load all configuration files with validation.
    /// </summary>
    public ConfigLoadResult Load()
    {
        var result = new ConfigLoadResult { Success = true };
        
        // Load weapons
        var weaponResult = LoadWeapons();
        result.Merge(weaponResult);
        result.ItemsLoaded += Weapons.Count;
        
        // Load enemies
        var enemyResult = LoadEnemies();
        result.Merge(enemyResult);
        result.ItemsLoaded += Enemies.Count;
        
        // Load abilities
        var abilityResult = LoadAbilities();
        result.Merge(abilityResult);
        result.ItemsLoaded += Abilities.Count;
        
        result.Success = result.Errors.Count == 0;
        LastLoadResult = result;
        IsLoaded = true;
        
        // Log results
        LogLoadResult(result);
        
        // Fail fast in development if configured
        if (FailFastOnErrors && !result.Success)
        {
            throw new InvalidOperationException(
                $"Config validation failed with {result.Errors.Count} errors:\n" +
                string.Join("\n", result.Errors));
        }
        
        return result;
    }
    
    private ConfigLoadResult LoadWeapons()
    {
        var result = new ConfigLoadResult();
        
        if (!DataLoader.FileExists(WeaponsPath))
        {
            result.Warnings.Add($"Weapons file not found: {WeaponsPath}, using defaults");
            Weapons = new WeaponDefinitions();
            return result;
        }
        
        var data = DataLoader.LoadDictionary<WeaponDef>(WeaponsPath);
        if (data.Count == 0)
        {
            result.Warnings.Add("No weapons loaded, using defaults");
            Weapons = new WeaponDefinitions();
            return result;
        }
        
        // Validate each weapon
        foreach (var weapon in data.Values)
        {
            var validation = weapon.Validate();
            result.Merge(validation);
        }
        
        Weapons = new WeaponDefinitions(data);
        result.Success = result.Errors.Count == 0;
        return result;
    }
    
    private ConfigLoadResult LoadEnemies()
    {
        var result = new ConfigLoadResult();
        
        if (!DataLoader.FileExists(EnemiesPath))
        {
            result.Warnings.Add($"Enemies file not found: {EnemiesPath}, using defaults");
            Enemies = new EnemyDefinitions();
            return result;
        }
        
        var data = DataLoader.LoadDictionary<EnemyDef>(EnemiesPath);
        if (data.Count == 0)
        {
            result.Warnings.Add("No enemies loaded, using defaults");
            Enemies = new EnemyDefinitions();
            return result;
        }
        
        // Validate each enemy
        foreach (var enemy in data.Values)
        {
            var validation = enemy.Validate();
            result.Merge(validation);
            
            // Cross-reference validation: check weapon exists
            if (!string.IsNullOrEmpty(enemy.WeaponId) && !Weapons.Has(enemy.WeaponId))
            {
                result.AddWarning($"EnemyDef[{enemy.Id}]: WeaponId '{enemy.WeaponId}' not found in weapons");
            }
        }
        
        Enemies = new EnemyDefinitions(data);
        result.Success = result.Errors.Count == 0;
        return result;
    }
    
    private ConfigLoadResult LoadAbilities()
    {
        var result = new ConfigLoadResult();
        
        if (!DataLoader.FileExists(AbilitiesPath))
        {
            result.Warnings.Add($"Abilities file not found: {AbilitiesPath}, using defaults");
            Abilities = new AbilityDefinitions();
            return result;
        }
        
        var data = DataLoader.LoadDictionary<AbilityDef>(AbilitiesPath);
        if (data.Count == 0)
        {
            result.Warnings.Add("No abilities loaded, using defaults");
            Abilities = new AbilityDefinitions();
            return result;
        }
        
        // Validate each ability
        foreach (var ability in data.Values)
        {
            var validation = ability.Validate();
            result.Merge(validation);
        }
        
        Abilities = new AbilityDefinitions(data);
        result.Success = result.Errors.Count == 0;
        return result;
    }
    
    private void LogLoadResult(ConfigLoadResult result)
    {
        SimLog.Log($"[ConfigRegistry] Loaded {result.ItemsLoaded} items");
        
        foreach (var warning in result.Warnings)
        {
            SimLog.Log($"[ConfigRegistry] WARNING: {warning}");
        }
        
        foreach (var error in result.Errors)
        {
            SimLog.Log($"[ConfigRegistry] ERROR: {error}");
        }
    }
}
```

**Acceptance Criteria**:
- [ ] `ConfigRegistry` loads all config files
- [ ] Validation runs on each loaded item
- [ ] Cross-reference validation (e.g., enemy weapon exists)
- [ ] `FailFastOnErrors` throws in development mode
- [ ] `LastLoadResult` available for inspection

---

#### Step 2.4: Add Count Property to Definition Collections

**File**: `src/sim/data/Definitions.cs`

**Add `Count` property to each definitions class**:

```csharp
public class WeaponDefinitions
{
    // ... existing code ...
    public int Count => weapons.Count;
}

public class EnemyDefinitions
{
    // ... existing code ...
    public int Count => enemies.Count;
}

public class AbilityDefinitions
{
    // ... existing code ...
    public int Count => abilities.Count;
}
```

**Acceptance Criteria**:
- [ ] Each definitions class has a `Count` property

---

#### Step 2.5: Migrate from Static Definitions to ConfigRegistry

**File**: `src/sim/data/Definitions.cs`

**Option A**: Keep `Definitions` as a static facade that delegates to a `ConfigRegistry` instance.

```csharp
public static class Definitions
{
    private static ConfigRegistry registry;
    
    public static WeaponDefinitions Weapons => EnsureLoaded().Weapons;
    public static EnemyDefinitions Enemies => EnsureLoaded().Enemies;
    public static AbilityDefinitions Abilities => EnsureLoaded().Abilities;
    
    public static ConfigRegistry EnsureLoaded()
    {
        if (registry == null)
        {
            registry = new ConfigRegistry();
            registry.Load();
        }
        return registry;
    }
    
    public static void Reload()
    {
        registry = new ConfigRegistry();
        registry.Load();
    }
    
    public static ConfigLoadResult GetLastLoadResult()
    {
        return registry?.LastLoadResult;
    }
}
```

**Option B**: Deprecate `Definitions` and use `ConfigRegistry` directly.

**Recommendation**: Option A for backward compatibility during migration.

**Acceptance Criteria**:
- [ ] Existing code using `Definitions.Weapons` still works
- [ ] Validation runs on first access
- [ ] `Reload()` forces re-validation

---

### Phase 3: Integration & Testing (Priority: High)

#### Step 3.1: Create RNG Unit Tests

**New File**: `tests/sim/RngTests.cs`

```csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FringeTactics;

[TestClass]
public class RngStreamTests
{
    [TestMethod]
    public void SameSeed_ProducesSameSequence()
    {
        var rng1 = new RngStream("test", 12345);
        var rng2 = new RngStream("test", 12345);
        
        for (int i = 0; i < 100; i++)
        {
            Assert.AreEqual(rng1.NextFloat(), rng2.NextFloat());
        }
    }
    
    [TestMethod]
    public void DifferentSeeds_ProduceDifferentSequences()
    {
        var rng1 = new RngStream("test", 12345);
        var rng2 = new RngStream("test", 54321);
        
        // At least one of the first 10 values should differ
        bool foundDifference = false;
        for (int i = 0; i < 10; i++)
        {
            if (rng1.NextFloat() != rng2.NextFloat())
            {
                foundDifference = true;
                break;
            }
        }
        Assert.IsTrue(foundDifference);
    }
    
    [TestMethod]
    public void RestoreState_ReproducesSequence()
    {
        var rng = new RngStream("test", 12345);
        
        // Consume some values
        for (int i = 0; i < 50; i++)
        {
            rng.NextFloat();
        }
        
        // Save state
        var state = rng.GetState();
        
        // Get next 10 values
        var expected = new float[10];
        for (int i = 0; i < 10; i++)
        {
            expected[i] = rng.NextFloat();
        }
        
        // Restore and verify
        var rng2 = new RngStream("test", 0);
        rng2.RestoreState(state.Seed, state.CallCount);
        
        for (int i = 0; i < 10; i++)
        {
            Assert.AreEqual(expected[i], rng2.NextFloat());
        }
    }
    
    [TestMethod]
    public void CallCount_TracksCorrectly()
    {
        var rng = new RngStream("test", 12345);
        Assert.AreEqual(0, rng.CallCount);
        
        rng.NextFloat();
        Assert.AreEqual(1, rng.CallCount);
        
        rng.NextInt(100);
        Assert.AreEqual(2, rng.CallCount);
        
        rng.Roll(0.5f);
        Assert.AreEqual(3, rng.CallCount);
    }
}

[TestClass]
public class RngServiceTests
{
    [TestMethod]
    public void SameMasterSeed_ProducesSameStreams()
    {
        var service1 = new RngService(12345);
        var service2 = new RngService(12345);
        
        // Campaign streams should match
        for (int i = 0; i < 10; i++)
        {
            Assert.AreEqual(
                service1.Campaign.NextFloat(),
                service2.Campaign.NextFloat());
        }
        
        // Tactical streams should match
        for (int i = 0; i < 10; i++)
        {
            Assert.AreEqual(
                service1.Tactical.NextFloat(),
                service2.Tactical.NextFloat());
        }
    }
    
    [TestMethod]
    public void Streams_AreIsolated()
    {
        var service = new RngService(12345);
        
        // Get first value from campaign
        var campaignFirst = service.Campaign.NextFloat();
        
        // Consume many values from tactical
        for (int i = 0; i < 1000; i++)
        {
            service.Tactical.NextFloat();
        }
        
        // Campaign's second value should be deterministic
        var service2 = new RngService(12345);
        service2.Campaign.NextFloat(); // Skip first
        var campaignSecond = service.Campaign.NextFloat();
        var expectedSecond = service2.Campaign.NextFloat();
        
        Assert.AreEqual(expectedSecond, campaignSecond);
    }
    
    [TestMethod]
    public void SaveRestore_RoundTrip()
    {
        var service = new RngService(12345);
        
        // Consume some values
        for (int i = 0; i < 25; i++)
        {
            service.Campaign.NextFloat();
            service.Tactical.NextInt(100);
        }
        
        // Save state
        var state = service.GetState();
        
        // Get next values
        var expectedCampaign = service.Campaign.NextFloat();
        var expectedTactical = service.Tactical.NextInt(100);
        
        // Restore to new service
        var service2 = new RngService(0);
        service2.RestoreState(state);
        
        Assert.AreEqual(expectedCampaign, service2.Campaign.NextFloat());
        Assert.AreEqual(expectedTactical, service2.Tactical.NextInt(100));
    }
    
    [TestMethod]
    public void ResetTacticalStream_CreatesNewSequence()
    {
        var service = new RngService(12345);
        
        var firstValue = service.Tactical.NextFloat();
        
        service.ResetTacticalStream(99999);
        
        var afterReset = service.Tactical.NextFloat();
        
        // Values should differ (different seed)
        Assert.AreNotEqual(firstValue, afterReset);
    }
}
```

**Acceptance Criteria**:
- [ ] Tests verify determinism with same seed
- [ ] Tests verify stream isolation
- [ ] Tests verify save/restore round-trip
- [ ] All tests pass

---

#### Step 3.2: Create Config Validation Tests

**New File**: `tests/sim/ConfigValidationTests.cs`

```csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FringeTactics;

[TestClass]
public class ConfigValidationTests
{
    [TestMethod]
    public void WeaponDef_Valid_PassesValidation()
    {
        var weapon = new WeaponDef
        {
            Id = "test_weapon",
            Name = "Test Weapon",
            Damage = 25,
            Range = 8,
            CooldownTicks = 10,
            Accuracy = 0.7f,
            MagazineSize = 30,
            ReloadTicks = 40
        };
        
        var result = weapon.Validate();
        Assert.IsTrue(result.IsValid);
    }
    
    [TestMethod]
    public void WeaponDef_MissingId_FailsValidation()
    {
        var weapon = new WeaponDef
        {
            Name = "Test Weapon",
            Damage = 25,
            Range = 8
        };
        
        var result = weapon.Validate();
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors[0].Contains("Id"));
    }
    
    [TestMethod]
    public void WeaponDef_NegativeDamage_FailsValidation()
    {
        var weapon = new WeaponDef
        {
            Id = "test",
            Name = "Test",
            Damage = -5,
            Range = 8
        };
        
        var result = weapon.Validate();
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Exists(e => e.Contains("Damage")));
    }
    
    [TestMethod]
    public void WeaponDef_AccuracyOutOfRange_FailsValidation()
    {
        var weapon = new WeaponDef
        {
            Id = "test",
            Name = "Test",
            Damage = 25,
            Range = 8,
            Accuracy = 1.5f // Invalid: > 1
        };
        
        var result = weapon.Validate();
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Exists(e => e.Contains("Accuracy")));
    }
    
    [TestMethod]
    public void EnemyDef_MissingWeaponId_FailsValidation()
    {
        var enemy = new EnemyDef
        {
            Id = "test_enemy",
            Name = "Test Enemy",
            Hp = 100
            // WeaponId missing
        };
        
        var result = enemy.Validate();
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Exists(e => e.Contains("WeaponId")));
    }
    
    [TestMethod]
    public void ConfigRegistry_LoadsExistingData()
    {
        var registry = new ConfigRegistry();
        var result = registry.Load();
        
        // Should load without errors (assuming valid data files)
        Assert.IsTrue(result.Success || result.Warnings.Count > 0);
        Assert.IsTrue(registry.IsLoaded);
    }
}
```

**Acceptance Criteria**:
- [ ] Tests verify valid definitions pass
- [ ] Tests verify invalid definitions fail with specific errors
- [ ] Tests verify `ConfigRegistry.Load()` works

---

#### Step 3.3: Update agents.md Files

**File**: `src/sim/agents.md`

Add entries for new files:
- `RngStream.cs` - Single seeded RNG stream with serializable state
- `RngService.cs` - Multi-stream RNG manager with campaign/tactical streams

**File**: `src/sim/data/agents.md`

Add entries for new files:
- `ValidationResult.cs` - Validation result accumulator
- `ConfigRegistry.cs` - Enhanced config loading with validation

**Acceptance Criteria**:
- [ ] `agents.md` files updated with new file descriptions

---

## Testing Checklist

### Manual Testing

1. **RNG Determinism**
   - [ ] Same master seed produces same campaign events
   - [ ] Same tactical seed produces same combat outcomes
   - [ ] Consuming campaign RNG doesn't affect tactical RNG

2. **Config Loading**
   - [ ] Game starts with valid JSON files
   - [ ] Game warns but continues with missing files
   - [ ] Game fails fast (in dev mode) with invalid data

3. **Save/Load (Preparation)**
   - [ ] RNG state can be serialized to JSON
   - [ ] RNG state can be restored from JSON

### Automated Tests

See Step 3.1 and 3.2 for test implementations.

---

## Implementation Order

1. **Day 1: RNG Foundation**
   - Step 1.1: Create `RngStream`
   - Step 1.2: Create `RngService`
   - Step 3.1: RNG unit tests

2. **Day 2: RNG Integration**
   - Step 1.3: Migrate `CombatState` to use `RngService`
   - Step 1.4: Deprecate `CombatRng`
   - Fix any broken tests

3. **Day 3: Config Validation**
   - Step 2.1: Create `ValidationResult`
   - Step 2.2: Add validation to definition types
   - Step 2.3: Create `ConfigRegistry`

4. **Day 4: Config Integration**
   - Step 2.4: Add `Count` properties
   - Step 2.5: Migrate `Definitions` to use `ConfigRegistry`
   - Step 3.2: Config validation tests

5. **Day 5: Polish & Documentation**
   - Step 3.3: Update `agents.md` files
   - Final testing and bug fixes
   - Update ROADMAP.md status

---

## Success Criteria for SF0

When SF0 is complete, you should be able to:

1. ✅ Create an `RngService` with a master seed
2. ✅ Get deterministic sequences from `Campaign` and `Tactical` streams
3. ✅ Serialize and restore RNG state
4. ✅ Load config files with validation
5. ✅ See clear error messages for invalid config data
6. ✅ Fail fast in development mode on validation errors
7. ✅ All existing tactical tests still pass

**Natural Pause Point**: After SF0, you have the foundation for deterministic simulation and validated data loading. This enables replay debugging, save/load, and confident content authoring.

---

## Notes for Future Milestones

### SF1 Dependencies (Time System)
- Time system may need to interact with RNG for time-based events
- Campaign time advancement could trigger RNG-based world sim

### SF2 Dependencies (Event Bus)
- Events may carry RNG state for replay
- Config changes could emit events for hot-reload

### SF3 Dependencies (Save/Load)
- `RngServiceState` is the serialization format for RNG
- `ConfigRegistry` doesn't need saving (loaded from files)

### G1+ Dependencies
- Campaign layer will use `RngService.Campaign` for world sim
- Mission generation will derive seeds from campaign RNG

---

## Open Questions

1. **RNG State Serialization**: Should we use call-count replay or try to serialize `System.Random` internal state?
   - *Recommendation*: Call-count replay is simpler and portable

2. **Config Hot-Reload**: Should `ConfigRegistry.Reload()` be callable at runtime?
   - *Recommendation*: Yes, for development. Add an event for listeners.

3. **Validation Strictness**: Should warnings also fail in dev mode, or only errors?
   - *Recommendation*: Only errors fail; warnings are logged but allowed

4. **Additional Streams**: Do we need `economy_rng` now or defer to later?
   - *Recommendation*: Defer. Easy to add when needed.

