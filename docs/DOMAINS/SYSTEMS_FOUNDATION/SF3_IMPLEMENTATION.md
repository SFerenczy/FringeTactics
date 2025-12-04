# SF3 – Save/Load (Campaign State): Implementation Plan

**Status**: ✅ **COMPLETE**

This document breaks down **Milestone SF3** from `ROADMAP.md` into concrete implementation steps.

**Goal**: Persist and restore campaign state to enable players to save progress and resume later.

---

## Current State Assessment

### What We Have (Existing Serialization Support)

| Component | Status | Notes |
|-----------|--------|-------|
| `RngServiceState` | ✅ Ready | `GetState()` / `RestoreState()` implemented in SF0 |
| `RngStreamState` | ✅ Ready | Seed + CallCount for replay-based restoration |
| `CampaignTimeState` | ✅ Ready | `GetState()` / `RestoreState()` implemented in SF1 |
| `CampaignState` | ⚠️ Partial | Has data but no serialization methods |
| `CrewMember` | ❌ Missing | No serialization support |
| `Sector` / `SectorNode` | ❌ Missing | No serialization support |
| `Job` / `JobReward` | ❌ Missing | No serialization support |
| `MissionConfig` | ❌ Missing | Needed if job has pending mission |

### SF3 Requirements vs What We Have

| SF3 Requirement | Current Status | Gap |
|-----------------|----------------|-----|
| Serialize `CampaignState` to JSON | ❌ Missing | Need `GetState()` method |
| Deserialize and restore | ❌ Missing | Need `RestoreState()` or factory |
| RNG stream states | ✅ Complete | SF0 implemented this |
| Time state | ✅ Complete | SF1 implemented this |
| Player state (crew, resources) | ❌ Missing | Need crew serialization |
| World state (sector, jobs) | ❌ Missing | Need sector/job serialization |
| Version field for migration | ❌ Missing | Need save format versioning |
| `SaveManager` class | ❌ Missing | Need file I/O wrapper |

---

## Architecture Decisions

### Save Format Design

**Decision**: Use JSON with a versioned envelope structure.

**Rationale**:
- JSON is human-readable for debugging
- System.Text.Json is available and performant
- Versioning enables forward-compatible migrations
- Matches existing config loading patterns (SF0)

**Save File Structure**:
```json
{
  "version": 1,
  "savedAt": "2024-01-15T10:30:00Z",
  "campaignName": "Outer Reach",
  "campaign": {
    "time": { ... },
    "rng": { ... },
    "resources": { ... },
    "crew": [ ... ],
    "sector": { ... },
    "jobs": { ... },
    "stats": { ... }
  }
}
```

### State Object Pattern

**Decision**: Create dedicated `*State` classes for serialization, separate from runtime objects.

**Rationale**:
- Runtime objects may have circular references, event handlers, computed properties
- State classes are pure data containers optimized for serialization
- Clear separation between "what we save" and "what we run"
- Follows pattern established by `RngStreamState`, `CampaignTimeState`

### Save Location

**Decision**: Use Godot's `user://` path for cross-platform compatibility.

**Rationale**:
- `user://saves/` maps to platform-appropriate locations
- Windows: `%APPDATA%/Godot/app_userdata/Fringe Tactics/saves/`
- Linux: `~/.local/share/godot/app_userdata/Fringe Tactics/saves/`
- macOS: `~/Library/Application Support/Godot/app_userdata/Fringe Tactics/saves/`

### Slot-Based Saves

**Decision**: Support multiple save slots (e.g., 3-5 slots) plus autosave.

**Rationale**:
- Players expect multiple save slots
- Autosave provides safety net
- Simple slot system avoids complex save management UI

### Engine-Light Principle

**Decision**: `SaveManager` core logic lives in `src/sim/`, file I/O adapter in `src/core/`.

**Rationale**:
- Serialization/deserialization logic is testable without Godot
- File I/O requires Godot's `FileAccess` (like `DataLoader`)
- Follows architecture: sim layer doesn't depend on Godot nodes

---

## Implementation Steps

### Phase 1: State Classes (Priority: Critical)

Define serializable state classes for all campaign data.

#### Step 1.1: Create Save Envelope Structure

**New File**: `src/sim/data/SaveData.cs`

**Purpose**: Top-level save file structure with versioning.

```csharp
using System;
using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// Top-level save file structure with versioning.
/// </summary>
public class SaveData
{
    /// <summary>
    /// Save format version. Increment when structure changes.
    /// </summary>
    public int Version { get; set; } = 1;
    
    /// <summary>
    /// When the save was created (UTC).
    /// </summary>
    public DateTime SavedAt { get; set; }
    
    /// <summary>
    /// Display name for the save (e.g., sector name + day).
    /// </summary>
    public string DisplayName { get; set; }
    
    /// <summary>
    /// The campaign state data.
    /// </summary>
    public CampaignStateData Campaign { get; set; }
}

/// <summary>
/// Current save format version.
/// </summary>
public static class SaveVersion
{
    public const int Current = 1;
    
    // Version history:
    // 1 - Initial save format (SF3)
}
```

**Acceptance Criteria**:
- [x] `SaveData` class with version field
- [x] `SaveVersion.Current` constant for version tracking
- [x] `SavedAt` timestamp for save metadata

---

#### Step 1.2: Create CampaignStateData

**New File**: `src/sim/data/CampaignStateData.cs`

**Purpose**: Serializable snapshot of entire campaign state.

```csharp
using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// Serializable campaign state for save/load.
/// </summary>
public class CampaignStateData
{
    // Time
    public CampaignTimeState Time { get; set; }
    
    // RNG
    public RngServiceState Rng { get; set; }
    
    // Resources
    public ResourcesData Resources { get; set; }
    
    // Location
    public int CurrentNodeId { get; set; }
    
    // Crew
    public List<CrewMemberData> Crew { get; set; } = new();
    public int NextCrewId { get; set; }
    
    // Sector (world state)
    public SectorData Sector { get; set; }
    
    // Jobs
    public List<JobData> AvailableJobs { get; set; } = new();
    public JobData CurrentJob { get; set; }
    
    // Faction reputation
    public Dictionary<string, int> FactionRep { get; set; } = new();
    
    // Statistics
    public CampaignStatsData Stats { get; set; }
}

/// <summary>
/// Campaign resources snapshot.
/// </summary>
public class ResourcesData
{
    public int Money { get; set; }
    public int Fuel { get; set; }
    public int Parts { get; set; }
    public int Meds { get; set; }
    public int Ammo { get; set; }
}

/// <summary>
/// Campaign statistics snapshot.
/// </summary>
public class CampaignStatsData
{
    public int MissionsCompleted { get; set; }
    public int MissionsFailed { get; set; }
    public int TotalMoneyEarned { get; set; }
    public int TotalCrewDeaths { get; set; }
}
```

**Acceptance Criteria**:
- [x] `CampaignStateData` captures all `CampaignState` fields
- [x] Nested data classes for resources and stats
- [x] Uses existing `CampaignTimeState` and `RngServiceState`

---

#### Step 1.3: Create CrewMemberData

**File**: `src/sim/data/CampaignStateData.cs` (add to same file)

**Purpose**: Serializable crew member state.

```csharp
/// <summary>
/// Serializable crew member state.
/// </summary>
public class CrewMemberData
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Role { get; set; }  // Enum as string for forward compatibility
    
    // Status
    public bool IsDead { get; set; }
    public List<string> Injuries { get; set; } = new();
    
    // Progression
    public int Level { get; set; }
    public int Xp { get; set; }
    
    // Stats
    public int Aim { get; set; }
    public int Toughness { get; set; }
    public int Reflexes { get; set; }
    
    // Equipment
    public string PreferredWeaponId { get; set; }
}
```

**Acceptance Criteria**:
- [x] All `CrewMember` fields captured
- [x] `Role` stored as string (not enum int) for migration safety

---

#### Step 1.4: Create SectorData and SectorNodeData

**File**: `src/sim/data/CampaignStateData.cs` (add to same file)

**Purpose**: Serializable sector/world state.

```csharp
/// <summary>
/// Serializable sector state.
/// </summary>
public class SectorData
{
    public string Name { get; set; }
    public List<SectorNodeData> Nodes { get; set; } = new();
    public Dictionary<string, string> Factions { get; set; } = new();
}

/// <summary>
/// Serializable sector node state.
/// </summary>
public class SectorNodeData
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Type { get; set; }  // Enum as string
    public string FactionId { get; set; }
    public float PositionX { get; set; }
    public float PositionY { get; set; }
    public List<int> Connections { get; set; } = new();
    public bool HasJob { get; set; }
}
```

**Acceptance Criteria**:
- [x] `SectorData` captures sector name, nodes, factions
- [x] `SectorNodeData` captures all node properties
- [x] Position stored as separate X/Y (Vector2 not directly serializable)

---

#### Step 1.5: Create JobData

**File**: `src/sim/data/CampaignStateData.cs` (add to same file)

**Purpose**: Serializable job state.

```csharp
/// <summary>
/// Serializable job state.
/// </summary>
public class JobData
{
    public string Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string Type { get; set; }  // Enum as string
    public string Difficulty { get; set; }  // Enum as string
    
    // Location
    public int OriginNodeId { get; set; }
    public int TargetNodeId { get; set; }
    
    // Faction
    public string EmployerFactionId { get; set; }
    public string TargetFactionId { get; set; }
    
    // Rewards
    public JobRewardData Reward { get; set; }
    public int RepGain { get; set; }
    public int RepLoss { get; set; }
    public int FailureRepLoss { get; set; }
    
    // Deadline
    public int DeadlineDays { get; set; }
    public int DeadlineDay { get; set; }
    
    // Note: MissionConfig is NOT saved - regenerated on load if needed
}

/// <summary>
/// Serializable job reward.
/// </summary>
public class JobRewardData
{
    public int Money { get; set; }
    public int Parts { get; set; }
    public int Fuel { get; set; }
    public int Ammo { get; set; }
}
```

**Acceptance Criteria**:
- [x] All `Job` fields captured except `MissionConfig`
- [x] `MissionConfig` excluded (regenerated deterministically from RNG)

---

### Phase 2: Serialization Methods (Priority: Critical)

Add `GetState()` and `RestoreState()` methods to runtime classes.

#### Step 2.1: Add Serialization to CrewMember

**File**: `src/sim/campaign/CrewMember.cs`

**Add methods**:

```csharp
/// <summary>
/// Get state for serialization.
/// </summary>
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
        Aim = Aim,
        Toughness = Toughness,
        Reflexes = Reflexes,
        PreferredWeaponId = PreferredWeaponId
    };
}

/// <summary>
/// Restore from saved state.
/// </summary>
public static CrewMember FromState(CrewMemberData data)
{
    var crew = new CrewMember(data.Id, data.Name)
    {
        Role = Enum.TryParse<CrewRole>(data.Role, out var role) ? role : CrewRole.Soldier,
        IsDead = data.IsDead,
        Injuries = new List<string>(data.Injuries ?? new List<string>()),
        Level = data.Level,
        Xp = data.Xp,
        Aim = data.Aim,
        Toughness = data.Toughness,
        Reflexes = data.Reflexes,
        PreferredWeaponId = data.PreferredWeaponId ?? "rifle"
    };
    return crew;
}
```

**Acceptance Criteria**:
- [x] `GetState()` captures all crew fields
- [x] `FromState()` restores crew with defaults for missing fields
- [x] Enum parsing handles unknown values gracefully

---

#### Step 2.2: Add Serialization to Sector/SectorNode

**File**: `src/sim/campaign/Sector.cs`

**Add methods**:

```csharp
// In SectorNode class:
public SectorNodeData GetState()
{
    return new SectorNodeData
    {
        Id = Id,
        Name = Name,
        Type = Type.ToString(),
        FactionId = FactionId,
        PositionX = Position.X,
        PositionY = Position.Y,
        Connections = new List<int>(Connections),
        HasJob = HasJob
    };
}

public static SectorNode FromState(SectorNodeData data)
{
    var node = new SectorNode(
        data.Id,
        data.Name,
        Enum.TryParse<NodeType>(data.Type, out var type) ? type : NodeType.Station,
        new Vector2(data.PositionX, data.PositionY)
    )
    {
        FactionId = data.FactionId,
        Connections = new List<int>(data.Connections ?? new List<int>()),
        HasJob = data.HasJob
    };
    return node;
}

// In Sector class:
public SectorData GetState()
{
    var data = new SectorData
    {
        Name = Name,
        Factions = new Dictionary<string, string>(Factions)
    };
    
    foreach (var node in Nodes)
    {
        data.Nodes.Add(node.GetState());
    }
    
    return data;
}

public static Sector FromState(SectorData data)
{
    var sector = new Sector
    {
        Name = data.Name,
        Factions = new Dictionary<string, string>(data.Factions ?? new Dictionary<string, string>())
    };
    
    foreach (var nodeData in data.Nodes ?? new List<SectorNodeData>())
    {
        sector.Nodes.Add(SectorNode.FromState(nodeData));
    }
    
    return sector;
}
```

**Acceptance Criteria**:
- [x] `SectorNode.GetState()` / `FromState()` round-trip correctly
- [x] `Sector.GetState()` / `FromState()` round-trip correctly
- [x] Vector2 position preserved via X/Y components

---

#### Step 2.3: Add Serialization to Job

**File**: `src/sim/campaign/Job.cs`

**Add methods**:

```csharp
// In JobReward class:
public JobRewardData GetState()
{
    return new JobRewardData
    {
        Money = Money,
        Parts = Parts,
        Fuel = Fuel,
        Ammo = Ammo
    };
}

public static JobReward FromState(JobRewardData data)
{
    return new JobReward
    {
        Money = data?.Money ?? 0,
        Parts = data?.Parts ?? 0,
        Fuel = data?.Fuel ?? 0,
        Ammo = data?.Ammo ?? 0
    };
}

// In Job class:
public JobData GetState()
{
    return new JobData
    {
        Id = Id,
        Title = Title,
        Description = Description,
        Type = Type.ToString(),
        Difficulty = Difficulty.ToString(),
        OriginNodeId = OriginNodeId,
        TargetNodeId = TargetNodeId,
        EmployerFactionId = EmployerFactionId,
        TargetFactionId = TargetFactionId,
        Reward = Reward?.GetState(),
        RepGain = RepGain,
        RepLoss = RepLoss,
        FailureRepLoss = FailureRepLoss,
        DeadlineDays = DeadlineDays,
        DeadlineDay = DeadlineDay
    };
}

public static Job FromState(JobData data)
{
    var job = new Job(data.Id)
    {
        Title = data.Title ?? "Unknown Job",
        Description = data.Description ?? "",
        Type = Enum.TryParse<JobType>(data.Type, out var type) ? type : JobType.Assault,
        Difficulty = Enum.TryParse<JobDifficulty>(data.Difficulty, out var diff) ? diff : JobDifficulty.Easy,
        OriginNodeId = data.OriginNodeId,
        TargetNodeId = data.TargetNodeId,
        EmployerFactionId = data.EmployerFactionId,
        TargetFactionId = data.TargetFactionId,
        Reward = JobReward.FromState(data.Reward),
        RepGain = data.RepGain,
        RepLoss = data.RepLoss,
        FailureRepLoss = data.FailureRepLoss,
        DeadlineDays = data.DeadlineDays,
        DeadlineDay = data.DeadlineDay
    };
    return job;
}
```

**Acceptance Criteria**:
- [x] `Job.GetState()` / `FromState()` round-trip correctly
- [x] `JobReward.GetState()` / `FromState()` round-trip correctly
- [x] `MissionConfig` is NOT serialized (regenerated on demand)

---

#### Step 2.4: Add Serialization to CampaignState

**File**: `src/sim/campaign/CampaignState.cs`

**Add methods**:

```csharp
/// <summary>
/// Get state for serialization.
/// </summary>
public CampaignStateData GetState()
{
    var data = new CampaignStateData
    {
        Time = Time.GetState(),
        Rng = Rng?.GetState(),
        CurrentNodeId = CurrentNodeId,
        NextCrewId = nextCrewId,
        FactionRep = new Dictionary<string, int>(FactionRep),
        Resources = new ResourcesData
        {
            Money = Money,
            Fuel = Fuel,
            Parts = Parts,
            Meds = Meds,
            Ammo = Ammo
        },
        Stats = new CampaignStatsData
        {
            MissionsCompleted = MissionsCompleted,
            MissionsFailed = MissionsFailed,
            TotalMoneyEarned = TotalMoneyEarned,
            TotalCrewDeaths = TotalCrewDeaths
        },
        Sector = Sector?.GetState()
    };
    
    // Serialize crew
    foreach (var crew in Crew)
    {
        data.Crew.Add(crew.GetState());
    }
    
    // Serialize available jobs
    foreach (var job in AvailableJobs)
    {
        data.AvailableJobs.Add(job.GetState());
    }
    
    // Serialize current job
    data.CurrentJob = CurrentJob?.GetState();
    
    return data;
}

/// <summary>
/// Restore campaign from saved state.
/// </summary>
public static CampaignState FromState(CampaignStateData data)
{
    var campaign = new CampaignState();
    
    // Restore time
    campaign.Time = new CampaignTime();
    if (data.Time != null)
    {
        campaign.Time.RestoreState(data.Time);
    }
    
    // Restore RNG
    if (data.Rng != null)
    {
        campaign.Rng = new RngService(data.Rng.MasterSeed);
        campaign.Rng.RestoreState(data.Rng);
    }
    else
    {
        campaign.Rng = new RngService();
    }
    
    // Restore resources
    if (data.Resources != null)
    {
        campaign.Money = data.Resources.Money;
        campaign.Fuel = data.Resources.Fuel;
        campaign.Parts = data.Resources.Parts;
        campaign.Meds = data.Resources.Meds;
        campaign.Ammo = data.Resources.Ammo;
    }
    
    // Restore location
    campaign.CurrentNodeId = data.CurrentNodeId;
    
    // Restore crew
    campaign.Crew.Clear();
    foreach (var crewData in data.Crew ?? new List<CrewMemberData>())
    {
        campaign.Crew.Add(CrewMember.FromState(crewData));
    }
    campaign.nextCrewId = data.NextCrewId;
    
    // Restore sector
    if (data.Sector != null)
    {
        campaign.Sector = Sector.FromState(data.Sector);
    }
    
    // Restore jobs
    campaign.AvailableJobs.Clear();
    foreach (var jobData in data.AvailableJobs ?? new List<JobData>())
    {
        campaign.AvailableJobs.Add(Job.FromState(jobData));
    }
    
    if (data.CurrentJob != null)
    {
        campaign.CurrentJob = Job.FromState(data.CurrentJob);
        // Regenerate MissionConfig if job is active
        if (campaign.CurrentJob != null)
        {
            var rng = new Random(campaign.Rng?.Campaign?.Seed ?? 0);
            campaign.CurrentJob.MissionConfig = JobSystem.GenerateMissionConfig(campaign.CurrentJob, rng);
        }
    }
    
    // Restore faction rep
    campaign.FactionRep = new Dictionary<string, int>(data.FactionRep ?? new Dictionary<string, int>());
    
    // Restore stats
    if (data.Stats != null)
    {
        campaign.MissionsCompleted = data.Stats.MissionsCompleted;
        campaign.MissionsFailed = data.Stats.MissionsFailed;
        campaign.TotalMoneyEarned = data.Stats.TotalMoneyEarned;
        campaign.TotalCrewDeaths = data.Stats.TotalCrewDeaths;
    }
    
    return campaign;
}
```

**Acceptance Criteria**:
- [x] `GetState()` captures entire campaign state
- [x] `FromState()` restores campaign with all subsystems
- [x] `MissionConfig` regenerated for active job (deterministic from RNG)
- [x] Private field `nextCrewId` properly saved/restored

---

### Phase 3: SaveManager (Priority: High)

Create the save/load orchestration layer.

#### Step 3.1: Create SaveManager Core (Sim Layer)

**New File**: `src/sim/SaveManager.cs`

**Purpose**: Serialization logic without file I/O (testable).

```csharp
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FringeTactics;

/// <summary>
/// Handles campaign state serialization/deserialization.
/// File I/O is handled by SaveFileAdapter (Godot layer).
/// </summary>
public static class SaveManager
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
    
    /// <summary>
    /// Create a SaveData envelope from campaign state.
    /// </summary>
    public static SaveData CreateSaveData(CampaignState campaign, string displayName = null)
    {
        return new SaveData
        {
            Version = SaveVersion.Current,
            SavedAt = DateTime.UtcNow,
            DisplayName = displayName ?? $"{campaign.Sector?.Name} - Day {campaign.Time.CurrentDay}",
            Campaign = campaign.GetState()
        };
    }
    
    /// <summary>
    /// Serialize SaveData to JSON string.
    /// </summary>
    public static string Serialize(SaveData data)
    {
        return JsonSerializer.Serialize(data, JsonOptions);
    }
    
    /// <summary>
    /// Deserialize JSON string to SaveData.
    /// </summary>
    public static SaveData Deserialize(string json)
    {
        return JsonSerializer.Deserialize<SaveData>(json, JsonOptions);
    }
    
    /// <summary>
    /// Restore CampaignState from SaveData.
    /// </summary>
    public static CampaignState RestoreCampaign(SaveData data)
    {
        if (data == null || data.Campaign == null)
        {
            throw new ArgumentException("Invalid save data");
        }
        
        // Version check
        if (data.Version > SaveVersion.Current)
        {
            throw new InvalidOperationException(
                $"Save file version {data.Version} is newer than supported version {SaveVersion.Current}");
        }
        
        // Future: Apply migrations for older versions
        // if (data.Version < SaveVersion.Current) { MigrateFrom(data); }
        
        return CampaignState.FromState(data.Campaign);
    }
    
    /// <summary>
    /// Validate save data integrity.
    /// </summary>
    public static ValidationResult ValidateSaveData(SaveData data)
    {
        var result = new ValidationResult();
        
        if (data == null)
        {
            result.AddError("Save data is null");
            return result;
        }
        
        if (data.Version <= 0)
        {
            result.AddError("Invalid save version");
        }
        
        if (data.Campaign == null)
        {
            result.AddError("Campaign data is missing");
            return result;
        }
        
        if (data.Campaign.Time == null)
        {
            result.AddError("Campaign time data is missing");
        }
        
        if (data.Campaign.Sector == null)
        {
            result.AddError("Sector data is missing");
        }
        
        if (data.Campaign.Crew == null || data.Campaign.Crew.Count == 0)
        {
            result.AddWarning("No crew data found");
        }
        
        return result;
    }
}
```

**Acceptance Criteria**:
- [x] `CreateSaveData()` wraps campaign in versioned envelope
- [x] `Serialize()` / `Deserialize()` round-trip correctly
- [x] `RestoreCampaign()` rebuilds campaign from data
- [x] `ValidateSaveData()` checks for required fields
- [x] Version check prevents loading newer saves

---

#### Step 3.2: Create SaveFileAdapter (Godot Layer)

**New File**: `src/core/SaveFileAdapter.cs`

**Purpose**: File I/O using Godot's FileAccess.

```csharp
using Godot;
using System;
using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// Handles save file I/O using Godot's FileAccess.
/// </summary>
public static class SaveFileAdapter
{
    private const string SaveDirectory = "user://saves/";
    private const string SaveExtension = ".json";
    private const string AutosaveName = "autosave";
    
    /// <summary>
    /// Get the full path for a save slot.
    /// </summary>
    public static string GetSavePath(int slot)
    {
        return $"{SaveDirectory}slot{slot}{SaveExtension}";
    }
    
    /// <summary>
    /// Get the autosave path.
    /// </summary>
    public static string GetAutosavePath()
    {
        return $"{SaveDirectory}{AutosaveName}{SaveExtension}";
    }
    
    /// <summary>
    /// Ensure save directory exists.
    /// </summary>
    public static void EnsureSaveDirectory()
    {
        if (!DirAccess.DirExistsAbsolute(ProjectSettings.GlobalizePath(SaveDirectory)))
        {
            DirAccess.MakeDirRecursiveAbsolute(ProjectSettings.GlobalizePath(SaveDirectory));
        }
    }
    
    /// <summary>
    /// Save campaign to a slot.
    /// </summary>
    public static bool Save(CampaignState campaign, int slot)
    {
        return SaveToPath(campaign, GetSavePath(slot));
    }
    
    /// <summary>
    /// Save campaign to autosave.
    /// </summary>
    public static bool Autosave(CampaignState campaign)
    {
        return SaveToPath(campaign, GetAutosavePath());
    }
    
    /// <summary>
    /// Save campaign to a specific path.
    /// </summary>
    private static bool SaveToPath(CampaignState campaign, string path)
    {
        try
        {
            EnsureSaveDirectory();
            
            var saveData = SaveManager.CreateSaveData(campaign);
            var json = SaveManager.Serialize(saveData);
            
            using var file = FileAccess.Open(path, FileAccess.ModeFlags.Write);
            if (file == null)
            {
                GD.PrintErr($"[SaveFileAdapter] Failed to open file for writing: {path}");
                return false;
            }
            
            file.StoreString(json);
            GD.Print($"[SaveFileAdapter] Saved to {path}");
            return true;
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[SaveFileAdapter] Save failed: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Load campaign from a slot.
    /// </summary>
    public static CampaignState Load(int slot)
    {
        return LoadFromPath(GetSavePath(slot));
    }
    
    /// <summary>
    /// Load campaign from autosave.
    /// </summary>
    public static CampaignState LoadAutosave()
    {
        return LoadFromPath(GetAutosavePath());
    }
    
    /// <summary>
    /// Load campaign from a specific path.
    /// </summary>
    private static CampaignState LoadFromPath(string path)
    {
        try
        {
            if (!FileAccess.FileExists(path))
            {
                GD.Print($"[SaveFileAdapter] Save file not found: {path}");
                return null;
            }
            
            using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
            if (file == null)
            {
                GD.PrintErr($"[SaveFileAdapter] Failed to open file for reading: {path}");
                return null;
            }
            
            var json = file.GetAsText();
            var saveData = SaveManager.Deserialize(json);
            
            // Validate
            var validation = SaveManager.ValidateSaveData(saveData);
            if (!validation.IsValid)
            {
                foreach (var error in validation.Errors)
                {
                    GD.PrintErr($"[SaveFileAdapter] Validation error: {error}");
                }
                return null;
            }
            
            var campaign = SaveManager.RestoreCampaign(saveData);
            GD.Print($"[SaveFileAdapter] Loaded from {path}");
            return campaign;
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[SaveFileAdapter] Load failed: {ex.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// Check if a save slot exists.
    /// </summary>
    public static bool SaveExists(int slot)
    {
        return FileAccess.FileExists(GetSavePath(slot));
    }
    
    /// <summary>
    /// Check if autosave exists.
    /// </summary>
    public static bool AutosaveExists()
    {
        return FileAccess.FileExists(GetAutosavePath());
    }
    
    /// <summary>
    /// Delete a save slot.
    /// </summary>
    public static bool DeleteSave(int slot)
    {
        var path = GetSavePath(slot);
        if (FileAccess.FileExists(path))
        {
            DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(path));
            GD.Print($"[SaveFileAdapter] Deleted {path}");
            return true;
        }
        return false;
    }
    
    /// <summary>
    /// Get metadata for a save slot (without loading full campaign).
    /// </summary>
    public static SaveMetadata GetSaveMetadata(int slot)
    {
        return GetMetadataFromPath(GetSavePath(slot));
    }
    
    /// <summary>
    /// Get metadata for autosave.
    /// </summary>
    public static SaveMetadata GetAutosaveMetadata()
    {
        return GetMetadataFromPath(GetAutosavePath());
    }
    
    private static SaveMetadata GetMetadataFromPath(string path)
    {
        try
        {
            if (!FileAccess.FileExists(path))
            {
                return null;
            }
            
            using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
            if (file == null) return null;
            
            var json = file.GetAsText();
            var saveData = SaveManager.Deserialize(json);
            
            return new SaveMetadata
            {
                DisplayName = saveData.DisplayName,
                SavedAt = saveData.SavedAt,
                Version = saveData.Version,
                Day = saveData.Campaign?.Time?.CurrentDay ?? 0,
                CrewCount = saveData.Campaign?.Crew?.Count ?? 0
            };
        }
        catch
        {
            return null;
        }
    }
    
    /// <summary>
    /// Get list of all available saves.
    /// </summary>
    public static List<SaveSlotInfo> GetAllSaves(int maxSlots = 5)
    {
        var saves = new List<SaveSlotInfo>();
        
        // Check autosave
        if (AutosaveExists())
        {
            saves.Add(new SaveSlotInfo
            {
                Slot = -1,
                IsAutosave = true,
                Metadata = GetAutosaveMetadata()
            });
        }
        
        // Check numbered slots
        for (int i = 1; i <= maxSlots; i++)
        {
            saves.Add(new SaveSlotInfo
            {
                Slot = i,
                IsAutosave = false,
                Exists = SaveExists(i),
                Metadata = SaveExists(i) ? GetSaveMetadata(i) : null
            });
        }
        
        return saves;
    }
}

/// <summary>
/// Lightweight save metadata for UI display.
/// </summary>
public class SaveMetadata
{
    public string DisplayName { get; set; }
    public DateTime SavedAt { get; set; }
    public int Version { get; set; }
    public int Day { get; set; }
    public int CrewCount { get; set; }
}

/// <summary>
/// Save slot information for UI.
/// </summary>
public class SaveSlotInfo
{
    public int Slot { get; set; }
    public bool IsAutosave { get; set; }
    public bool Exists { get; set; }
    public SaveMetadata Metadata { get; set; }
}
```

**Acceptance Criteria**:
- [x] `Save()` / `Load()` work with slot numbers
- [x] `Autosave()` / `LoadAutosave()` work
- [x] `SaveExists()` / `AutosaveExists()` check file presence
- [x] `GetSaveMetadata()` returns lightweight info for UI
- [x] `GetAllSaves()` returns list for save/load menu
- [x] Directory created automatically if missing

---

#### Step 3.3: Integrate with GameState

**File**: `src/core/GameState.cs`

**Add methods**:

```csharp
/// <summary>
/// Save current campaign to a slot.
/// </summary>
public bool SaveGame(int slot)
{
    if (Campaign == null)
    {
        GD.PrintErr("[GameState] No campaign to save");
        return false;
    }
    
    return SaveFileAdapter.Save(Campaign, slot);
}

/// <summary>
/// Autosave current campaign.
/// </summary>
public bool Autosave()
{
    if (Campaign == null) return false;
    return SaveFileAdapter.Autosave(Campaign);
}

/// <summary>
/// Load campaign from a slot.
/// </summary>
public bool LoadGame(int slot)
{
    var campaign = SaveFileAdapter.Load(slot);
    if (campaign == null) return false;
    
    EventBus.Clear();
    Campaign = campaign;
    WireEventBus(Campaign);
    Mode = "sector";
    
    GD.Print($"[GameState] Loaded campaign from slot {slot}");
    GoToSectorView();
    return true;
}

/// <summary>
/// Load campaign from autosave.
/// </summary>
public bool LoadAutosave()
{
    var campaign = SaveFileAdapter.LoadAutosave();
    if (campaign == null) return false;
    
    EventBus.Clear();
    Campaign = campaign;
    WireEventBus(Campaign);
    Mode = "sector";
    
    GD.Print("[GameState] Loaded campaign from autosave");
    GoToSectorView();
    return true;
}

/// <summary>
/// Check if a save slot exists.
/// </summary>
public bool HasSave(int slot) => SaveFileAdapter.SaveExists(slot);

/// <summary>
/// Check if autosave exists.
/// </summary>
public bool HasAutosave() => SaveFileAdapter.AutosaveExists();
```

**Acceptance Criteria**:
- [x] `SaveGame()` saves current campaign
- [x] `LoadGame()` loads and transitions to sector view
- [x] `Autosave()` saves without user interaction
- [x] Event bus properly wired after load
- [x] `HasSave()` / `HasAutosave()` for UI checks

---

### Phase 4: Testing (Priority: High)

#### Step 4.1: Create Serialization Unit Tests

**New File**: `tests/sim/foundation/SF3SerializationTests.cs`

```csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FringeTactics;
using System.Collections.Generic;

namespace FringeTactics.Tests;

[TestClass]
public class SF3SerializationTests
{
    [TestMethod]
    public void CrewMember_RoundTrip_PreservesAllFields()
    {
        var crew = new CrewMember(42, "Test Soldier")
        {
            Role = CrewRole.Medic,
            IsDead = false,
            Level = 3,
            Xp = 75,
            Aim = 5,
            Toughness = 2,
            Reflexes = 1,
            PreferredWeaponId = "smg"
        };
        crew.AddInjury("wounded");
        crew.AddInjury("concussed");
        
        var state = crew.GetState();
        var restored = CrewMember.FromState(state);
        
        Assert.AreEqual(42, restored.Id);
        Assert.AreEqual("Test Soldier", restored.Name);
        Assert.AreEqual(CrewRole.Medic, restored.Role);
        Assert.AreEqual(false, restored.IsDead);
        Assert.AreEqual(3, restored.Level);
        Assert.AreEqual(75, restored.Xp);
        Assert.AreEqual(5, restored.Aim);
        Assert.AreEqual(2, restored.Toughness);
        Assert.AreEqual(1, restored.Reflexes);
        Assert.AreEqual("smg", restored.PreferredWeaponId);
        Assert.AreEqual(2, restored.Injuries.Count);
        Assert.IsTrue(restored.Injuries.Contains("wounded"));
        Assert.IsTrue(restored.Injuries.Contains("concussed"));
    }
    
    [TestMethod]
    public void Job_RoundTrip_PreservesAllFields()
    {
        var job = new Job("job_123")
        {
            Title = "Test Mission",
            Description = "A test job",
            Type = JobType.Extraction,
            Difficulty = JobDifficulty.Hard,
            OriginNodeId = 1,
            TargetNodeId = 5,
            EmployerFactionId = "corp",
            TargetFactionId = "pirates",
            Reward = new JobReward { Money = 500, Parts = 25, Fuel = 10, Ammo = 15 },
            RepGain = 15,
            RepLoss = 10,
            FailureRepLoss = 20,
            DeadlineDays = 5,
            DeadlineDay = 12
        };
        
        var state = job.GetState();
        var restored = Job.FromState(state);
        
        Assert.AreEqual("job_123", restored.Id);
        Assert.AreEqual("Test Mission", restored.Title);
        Assert.AreEqual(JobType.Extraction, restored.Type);
        Assert.AreEqual(JobDifficulty.Hard, restored.Difficulty);
        Assert.AreEqual(5, restored.TargetNodeId);
        Assert.AreEqual(500, restored.Reward.Money);
        Assert.AreEqual(12, restored.DeadlineDay);
    }
    
    [TestMethod]
    public void SectorNode_RoundTrip_PreservesPosition()
    {
        var node = new SectorNode(3, "Test Station", NodeType.Contested, new Godot.Vector2(150.5f, 275.25f))
        {
            FactionId = "rebels",
            HasJob = true,
            Connections = new List<int> { 1, 2, 5 }
        };
        
        var state = node.GetState();
        var restored = SectorNode.FromState(state);
        
        Assert.AreEqual(3, restored.Id);
        Assert.AreEqual("Test Station", restored.Name);
        Assert.AreEqual(NodeType.Contested, restored.Type);
        Assert.AreEqual("rebels", restored.FactionId);
        Assert.AreEqual(150.5f, restored.Position.X, 0.01f);
        Assert.AreEqual(275.25f, restored.Position.Y, 0.01f);
        Assert.IsTrue(restored.HasJob);
        Assert.AreEqual(3, restored.Connections.Count);
    }
    
    [TestMethod]
    public void CampaignState_RoundTrip_PreservesResources()
    {
        var campaign = CampaignState.CreateNew(12345);
        campaign.Money = 999;
        campaign.Fuel = 50;
        campaign.Parts = 75;
        campaign.Meds = 3;
        campaign.Ammo = 25;
        
        var state = campaign.GetState();
        var restored = CampaignState.FromState(state);
        
        Assert.AreEqual(999, restored.Money);
        Assert.AreEqual(50, restored.Fuel);
        Assert.AreEqual(75, restored.Parts);
        Assert.AreEqual(3, restored.Meds);
        Assert.AreEqual(25, restored.Ammo);
    }
    
    [TestMethod]
    public void CampaignState_RoundTrip_PreservesCrew()
    {
        var campaign = CampaignState.CreateNew(12345);
        
        var state = campaign.GetState();
        var restored = CampaignState.FromState(state);
        
        Assert.AreEqual(campaign.Crew.Count, restored.Crew.Count);
        for (int i = 0; i < campaign.Crew.Count; i++)
        {
            Assert.AreEqual(campaign.Crew[i].Name, restored.Crew[i].Name);
            Assert.AreEqual(campaign.Crew[i].Role, restored.Crew[i].Role);
        }
    }
    
    [TestMethod]
    public void CampaignState_RoundTrip_PreservesRngState()
    {
        var campaign = CampaignState.CreateNew(12345);
        
        // Consume some RNG values
        for (int i = 0; i < 50; i++)
        {
            campaign.Rng.Campaign.NextFloat();
        }
        
        // Get next expected value
        var expectedNext = campaign.Rng.Campaign.NextFloat();
        
        // Save state BEFORE consuming that value
        campaign = CampaignState.CreateNew(12345);
        for (int i = 0; i < 50; i++)
        {
            campaign.Rng.Campaign.NextFloat();
        }
        
        var state = campaign.GetState();
        var restored = CampaignState.FromState(state);
        
        // Restored campaign should produce same next value
        var actualNext = restored.Rng.Campaign.NextFloat();
        Assert.AreEqual(expectedNext, actualNext);
    }
    
    [TestMethod]
    public void CampaignState_RoundTrip_PreservesTime()
    {
        var campaign = CampaignState.CreateNew(12345);
        campaign.Time.AdvanceDays(15);
        
        var state = campaign.GetState();
        var restored = CampaignState.FromState(state);
        
        Assert.AreEqual(16, restored.Time.CurrentDay); // Started at 1, +15 = 16
    }
}

[TestClass]
public class SF3SaveManagerTests
{
    [TestMethod]
    public void SaveData_SerializeDeserialize_RoundTrip()
    {
        var campaign = CampaignState.CreateNew(12345);
        var saveData = SaveManager.CreateSaveData(campaign, "Test Save");
        
        var json = SaveManager.Serialize(saveData);
        var restored = SaveManager.Deserialize(json);
        
        Assert.AreEqual(SaveVersion.Current, restored.Version);
        Assert.AreEqual("Test Save", restored.DisplayName);
        Assert.IsNotNull(restored.Campaign);
    }
    
    [TestMethod]
    public void SaveData_Validation_DetectsMissingCampaign()
    {
        var saveData = new SaveData { Version = 1, Campaign = null };
        
        var result = SaveManager.ValidateSaveData(saveData);
        
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Count > 0);
    }
    
    [TestMethod]
    public void SaveData_Validation_PassesValidData()
    {
        var campaign = CampaignState.CreateNew(12345);
        var saveData = SaveManager.CreateSaveData(campaign);
        
        var result = SaveManager.ValidateSaveData(saveData);
        
        Assert.IsTrue(result.IsValid);
    }
    
    [TestMethod]
    public void RestoreCampaign_FromSaveData_Works()
    {
        var original = CampaignState.CreateNew(12345);
        original.Money = 777;
        original.Time.AdvanceDays(10);
        
        var saveData = SaveManager.CreateSaveData(original);
        var json = SaveManager.Serialize(saveData);
        var loadedData = SaveManager.Deserialize(json);
        var restored = SaveManager.RestoreCampaign(loadedData);
        
        Assert.AreEqual(777, restored.Money);
        Assert.AreEqual(11, restored.Time.CurrentDay);
    }
}
```

**Acceptance Criteria**:
- [x] All serialization round-trip tests pass
- [x] RNG state preserved correctly
- [x] Time state preserved correctly
- [x] Crew, jobs, sector preserved correctly
- [x] Validation tests cover error cases

---

## Implementation Order

1. **Day 1: State Classes**
   - Step 1.1: Create SaveData envelope
   - Step 1.2: Create CampaignStateData
   - Step 1.3-1.5: Create nested data classes

2. **Day 2: Serialization Methods**
   - Step 2.1: CrewMember serialization
   - Step 2.2: Sector/SectorNode serialization
   - Step 2.3: Job serialization
   - Step 2.4: CampaignState serialization

3. **Day 3: SaveManager**
   - Step 3.1: SaveManager core (sim layer)
   - Step 3.2: SaveFileAdapter (Godot layer)
   - Step 3.3: GameState integration

4. **Day 4: Testing & Polish**
   - Step 4.1: Unit tests
   - Integration testing
   - Documentation updates

---

## Success Criteria for SF3

When SF3 is complete, you should be able to:

1. [x] Save a campaign to a numbered slot
2. [x] Load a campaign from a numbered slot
3. [x] Autosave campaign automatically
4. [x] Load from autosave
5. [x] See save metadata (day, crew count, timestamp) in UI
6. [x] Resume play with identical RNG state (deterministic)
7. [x] Resume play with correct time, resources, crew
8. [x] Resume play with correct sector and job state
9. [x] Handle missing/corrupted save files gracefully
10. [x] Reject saves from newer versions with clear error

---

## Future Extensions (Not SF3)

- **Save file migration**: Upgrade old saves when format changes
- **Cloud saves**: Sync saves across devices
- **Save compression**: Reduce file size for large campaigns
- **Save encryption**: Prevent casual tampering
- **Ironman mode**: Single autosave, no manual saves
- **Save thumbnails**: Screenshot preview in save menu

---

## Open Questions

1. **When to autosave?**
   - After completing a mission?
   - After traveling to a new node?
   - On exiting to main menu?
   - Recommendation: After mission completion and travel

2. **How many save slots?**
   - 3-5 seems reasonable for this scope
   - Plus autosave = 4-6 total

3. **Should we save mid-mission?**
   - SF3 scope: No (campaign state only)
   - Future: Could add tactical save/load

4. **JobSystem.ResetJobIdCounter() on load?**
   - Need to ensure job IDs don't collide after load
   - Track highest job ID in save and restore counter
