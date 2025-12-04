# M7 – Session I/O & Retreat Integration: Implementation Plan

This document breaks down **Milestone 7** from `ROADMAP.md` into concrete implementation steps.

**Goal**: Make the tactical layer cleanly pluggable into the wider game with well-defined input/output contracts and retreat mechanics.

## ✅ MILESTONE COMPLETE

All phases implemented and tested:
- **Phase 1**: Input/Output contracts (`MissionInput`, `MissionOutput`, `CrewDeployment`, etc.)
- **Phase 2**: Retreat mechanism (`CombatState.InitiateRetreat()`, entry zone detection, UI)
- **Phase 3**: Output generation (`MissionOutputBuilder`, actor combat statistics)
- **Phase 4**: Integration (`GameState.EndMission()` refactored, `MissionFactory.BuildFromInput()`)
- **Phase 5**: Testing (M7 test mission, 10 automated tests passing)

---

## Current State Assessment

### What We Have (From M0–M6)

| Component | Status | Notes |
|-----------|--------|-------|
| `MissionResult` | ⚠️ Partial | Basic struct with Victory, DeadCrewIds, InjuredCrewIds, XP |
| `MissionConfig` | ⚠️ Partial | Has MapTemplate, CrewSpawnPositions, EnemySpawns |
| `MissionFactory` | ✅ Complete | Builds CombatState from config, has `BuildFromCampaign()` |
| `GameState.EndMission()` | ⚠️ Partial | Builds MissionResult from CombatState, applies to campaign |
| `CampaignState.ApplyMissionResult()` | ⚠️ Partial | Handles deaths, injuries, XP, job rewards |
| `MapState.EntryZone` | ✅ Complete | List of entry zone positions exists |
| `CombatState.MissionEnded` | ✅ Complete | Event fires on victory/defeat |
| `PerceptionSystem.AlarmState` | ✅ Complete | Tracks Quiet/Alerted |

### What M7 Requires vs What We Have

| M7 Requirement | Current Status | Gap |
|----------------|----------------|-----|
| Session input contract | ✅ Complete | `MissionInput`, `CrewDeployment`, `MissionObjective`, `MissionContext` |
| Session output contract | ✅ Complete | `MissionOutput`, `CrewOutcome`, `MissionOutcome`, `ObjectiveStatus` |
| Map specification | ⚠️ Partial | Template-based, no formal map data contract |
| Entry zone handling | ✅ Exists | `MapState.EntryZone` populated by MapBuilder |
| Player unit input (stats, equipment) | ✅ Complete | `MissionFactory.BuildFromInput()` uses `CrewDeployment` |
| Enemy/objective metadata | ✅ Complete | `MissionInput.Enemies`, `MissionInput.Objectives` |
| Surviving unit output | ✅ Complete | `MissionOutputBuilder.Build()` creates `CrewOutcome` per actor |
| Ammo/consumable tracking | ✅ Complete | `Actor.AmmoUsed`, `CrewOutcome.AmmoRemaining/AmmoUsed` |
| Objective status flags | ✅ Complete | `MissionOutput.ObjectiveResults` dictionary |
| Alarm outcome in result | ✅ Complete | `MissionOutput.AlarmTriggered` from PerceptionSystem |
| Retreat mechanism | ✅ Complete | `CombatState.InitiateRetreat()`, `AreAllCrewInEntryZone()` |
| Retreat outcome handling | ✅ Complete | `MissionOutcome.Retreat`, UI shows "RETREATED" |

---

## Architecture Decisions

### Mission I/O Contract Philosophy

Per CAMPAIGN_FOUNDATIONS.md section 7:
> - Keep the I/O boundary **narrow and explicit**.
> - Mission code should not directly mutate global campaign state; it should emit a **delta** that the campaign layer applies.
> - Mid-mission branching is allowed: Multiple exits (escape early, side-exit, complete everything).

**Decision**: Create formal `MissionInput` and `MissionOutput` structs that encapsulate all data crossing the tactical/campaign boundary.

### MissionInput Structure

```csharp
public class MissionInput
{
    // Map specification
    public string[] MapTemplate { get; set; }
    public Vector2I GridSize { get; set; }
    
    // Crew to deploy (from campaign)
    public List<CrewDeployment> Crew { get; set; }
    
    // Enemy configuration
    public List<EnemySpawn> Enemies { get; set; }
    
    // Interactables (doors, terminals, hazards)
    public List<InteractableSpawn> Interactables { get; set; }
    
    // Objectives
    public List<MissionObjective> Objectives { get; set; }
    
    // Context
    public string MissionId { get; set; }
    public string MissionName { get; set; }
    public int Seed { get; set; }
    public MissionContext Context { get; set; } // faction, location tags, etc.
}
```

### MissionOutput Structure

Per CAMPAIGN_FOUNDATIONS.md section 7.2, outputs include:
- Crew changes (XP, injuries, deaths, trait changes)
- Inventory/cargo updates
- Contract state changes
- World & faction deltas
- Follow-up hooks

```csharp
public class MissionOutput
{
    // Outcome type
    public MissionOutcome Outcome { get; set; } // Victory, Defeat, Retreat, Abort
    
    // Per-crew results
    public List<CrewOutcome> CrewOutcomes { get; set; }
    
    // Objective completion
    public Dictionary<string, ObjectiveStatus> ObjectiveResults { get; set; }
    
    // Mission statistics
    public int EnemiesKilled { get; set; }
    public int EnemiesRemaining { get; set; }
    public bool AlarmTriggered { get; set; }
    public int TicksElapsed { get; set; }
    
    // Loot/cargo (future)
    public List<LootItem> Loot { get; set; }
}
```

### Retreat Mechanics

Per DESIGN.md section 10.2:
> - Player can always choose to **abort**: Gather survivors, reach the entry zone, mission ends as a retreat/failure outcome.

**Decision**: Implement retreat as follows:

1. **Retreat Command**: Player can issue a "Retreat" order via UI button or hotkey
2. **Retreat Detection**: When all surviving crew are in the entry zone AND retreat is active, mission ends
3. **Retreat Outcome**: `MissionOutcome.Retreat` - distinct from Victory or Defeat
4. **Partial Success**: Retreat preserves crew but may forfeit objectives/rewards

### Outcome Types

```csharp
public enum MissionOutcome
{
    Victory,    // Primary objectives complete
    Defeat,     // All crew eliminated
    Retreat,    // Player voluntarily extracted (partial success)
    Abort       // Mission cancelled before meaningful progress
}
```

---

## Implementation Steps

### Phase 1: Mission Input Contract (Priority: Critical)

#### Step 1.1: Create MissionInput and Supporting Types

**New File**: `src/sim/combat/MissionInput.cs`

```csharp
using Godot;
using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// Complete specification for starting a tactical mission.
/// This is the formal contract between campaign and tactical layers.
/// </summary>
public class MissionInput
{
    // === Map Specification ===
    public string[] MapTemplate { get; set; }
    public Vector2I? GridSize { get; set; } // Optional override
    
    // === Crew Deployment ===
    public List<CrewDeployment> Crew { get; set; } = new();
    
    // === Enemy Configuration ===
    public List<EnemySpawn> Enemies { get; set; } = new();
    
    // === Interactables ===
    public List<InteractableSpawn> Interactables { get; set; } = new();
    
    // === Objectives ===
    public List<MissionObjective> Objectives { get; set; } = new();
    
    // === Context ===
    public string MissionId { get; set; } = "unknown";
    public string MissionName { get; set; } = "Unknown Mission";
    public int Seed { get; set; } = 0;
    public MissionContext Context { get; set; } = new();
}

/// <summary>
/// A crew member to deploy in the mission.
/// </summary>
public class CrewDeployment
{
    public int CampaignCrewId { get; set; }
    public string Name { get; set; }
    
    // Stats (copied from campaign crew)
    public int MaxHp { get; set; } = 100;
    public int CurrentHp { get; set; } = 100;
    public float MoveSpeed { get; set; } = 2.0f;
    public float Accuracy { get; set; } = 0.7f;
    
    // Equipment
    public string WeaponId { get; set; } = "rifle";
    public int AmmoInMagazine { get; set; } = 30;
    public int ReserveAmmo { get; set; } = 90;
    
    // Spawn position (optional - uses entry zone if not specified)
    public Vector2I? SpawnPosition { get; set; }
}

/// <summary>
/// Mission context from the campaign layer.
/// </summary>
public class MissionContext
{
    public string LocationId { get; set; }
    public string LocationName { get; set; }
    public string FactionId { get; set; }
    public List<string> Tags { get; set; } = new(); // "high_security", "lawless", etc.
    public string ContractId { get; set; } // If mission is contract-driven
}

/// <summary>
/// An objective to track during the mission.
/// </summary>
public class MissionObjective
{
    public string Id { get; set; }
    public string Description { get; set; }
    public ObjectiveType Type { get; set; }
    public bool IsPrimary { get; set; } = true;
    
    // Type-specific data
    public Vector2I? TargetPosition { get; set; } // For "reach" objectives
    public int? TargetInteractableId { get; set; } // For "hack terminal" objectives
    public string TargetActorType { get; set; } // For "eliminate" objectives
}

public enum ObjectiveType
{
    EliminateAll,       // Kill all enemies
    EliminateTarget,    // Kill specific target
    ReachZone,          // Get crew to a location
    HackTerminal,       // Complete a hack
    Survive,            // Keep crew alive for duration
    Retrieve,           // Pick up an item
    Escort              // Protect a VIP
}
```

**Acceptance Criteria**:
- [ ] `MissionInput` struct captures all data needed to start a mission
- [ ] `CrewDeployment` includes stats, equipment, and optional spawn position
- [ ] `MissionObjective` supports multiple objective types
- [ ] `MissionContext` provides campaign context to tactical layer

---

#### Step 1.2: Create MissionOutput and Supporting Types

**New File**: `src/sim/combat/MissionOutput.cs`

```csharp
using Godot;
using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// Mission outcome type.
/// </summary>
public enum MissionOutcome
{
    Victory,    // Primary objectives complete
    Defeat,     // All crew eliminated
    Retreat,    // Player voluntarily extracted
    Abort       // Mission cancelled (no meaningful progress)
}

/// <summary>
/// Status of an individual objective.
/// </summary>
public enum ObjectiveStatus
{
    Pending,    // Not started
    InProgress, // Being worked on
    Complete,   // Successfully completed
    Failed      // Cannot be completed
}

/// <summary>
/// Complete results from a tactical mission.
/// This is the formal contract for what tactical returns to campaign.
/// </summary>
public class MissionOutput
{
    // === Outcome ===
    public MissionOutcome Outcome { get; set; }
    public string MissionId { get; set; }
    
    // === Per-Crew Results ===
    public List<CrewOutcome> CrewOutcomes { get; set; } = new();
    
    // === Objective Results ===
    public Dictionary<string, ObjectiveStatus> ObjectiveResults { get; set; } = new();
    
    // === Statistics ===
    public int EnemiesKilled { get; set; }
    public int EnemiesRemaining { get; set; }
    public bool AlarmTriggered { get; set; }
    public int TicksElapsed { get; set; }
    public float MissionDurationSeconds { get; set; }
    
    // === Loot (future) ===
    public List<LootItem> Loot { get; set; } = new();
    
    // === World Deltas (future) ===
    public List<WorldDelta> WorldDeltas { get; set; } = new();
}

/// <summary>
/// Outcome for a single crew member.
/// </summary>
public class CrewOutcome
{
    public int CampaignCrewId { get; set; }
    public string Name { get; set; }
    
    // Status
    public CrewFinalStatus Status { get; set; }
    
    // Health
    public int FinalHp { get; set; }
    public int MaxHp { get; set; }
    public int DamageTaken { get; set; }
    
    // Ammo
    public int AmmoRemaining { get; set; }
    public int AmmoUsed { get; set; }
    
    // Combat stats
    public int Kills { get; set; }
    public int ShotsFired { get; set; }
    public int ShotsHit { get; set; }
    
    // XP (calculated by campaign layer, but tactical can suggest)
    public int SuggestedXp { get; set; }
    
    // Injuries (if any)
    public List<string> NewInjuries { get; set; } = new();
}

public enum CrewFinalStatus
{
    Alive,      // Survived, healthy
    Wounded,    // Survived, took significant damage
    Critical,   // Survived, near death
    Dead,       // Killed in action
    MIA         // Left behind (didn't reach extraction)
}

/// <summary>
/// Loot item acquired during mission.
/// </summary>
public class LootItem
{
    public string ItemId { get; set; }
    public string Name { get; set; }
    public int Quantity { get; set; } = 1;
    public List<string> Tags { get; set; } = new();
}

/// <summary>
/// A change to world state caused by mission outcome.
/// </summary>
public class WorldDelta
{
    public string Type { get; set; } // "reputation", "security", "faction_relation"
    public string TargetId { get; set; } // faction ID, location ID, etc.
    public int Delta { get; set; }
    public string Reason { get; set; }
}
```

**Acceptance Criteria**:
- [ ] `MissionOutput` captures all data needed by campaign layer
- [ ] `CrewOutcome` tracks per-crew status, health, ammo, and combat stats
- [ ] `MissionOutcome` enum includes Retreat as distinct from Victory/Defeat
- [ ] `ObjectiveStatus` tracks completion of each objective

---

### Phase 2: Retreat System (Priority: Critical)

#### Step 2.1: Add Retreat State to CombatState

**File**: `src/sim/combat/CombatState.cs`

**Add retreat tracking**:
```csharp
// Retreat state
public bool IsRetreating { get; private set; } = false;
public event Action RetreatInitiated;
public event Action<MissionOutcome> MissionCompleted; // More detailed than MissionEnded

/// <summary>
/// Initiate retreat. Crew must reach entry zone to complete.
/// </summary>
public void InitiateRetreat()
{
    if (IsRetreating || IsComplete)
    {
        return;
    }
    
    IsRetreating = true;
    RetreatInitiated?.Invoke();
    SimLog.Log("[CombatState] Retreat initiated! Get all crew to the entry zone.");
}

/// <summary>
/// Cancel retreat (if player changes mind before extraction).
/// </summary>
public void CancelRetreat()
{
    if (!IsRetreating || IsComplete)
    {
        return;
    }
    
    IsRetreating = false;
    SimLog.Log("[CombatState] Retreat cancelled.");
}

/// <summary>
/// Check if all surviving crew are in the entry zone.
/// </summary>
public bool AreAllCrewInEntryZone()
{
    foreach (var actor in Actors)
    {
        if (actor.Type != ActorTypes.Crew || actor.State != ActorState.Alive)
        {
            continue;
        }
        
        if (!MapState.IsInEntryZone(actor.GridPosition))
        {
            return false;
        }
    }
    
    return true;
}

/// <summary>
/// Get count of crew in entry zone vs total alive.
/// </summary>
public (int inZone, int total) GetCrewExtractionStatus()
{
    int inZone = 0;
    int total = 0;
    
    foreach (var actor in Actors)
    {
        if (actor.Type != ActorTypes.Crew || actor.State != ActorState.Alive)
        {
            continue;
        }
        
        total++;
        if (MapState.IsInEntryZone(actor.GridPosition))
        {
            inZone++;
        }
    }
    
    return (inZone, total);
}
```

**Update `CheckMissionEnd()` to handle retreat**:
```csharp
private void CheckMissionEnd()
{
    if (IsComplete || Phase == MissionPhase.Complete)
    {
        return;
    }
    
    // Check retreat completion
    if (IsRetreating && AreAllCrewInEntryZone())
    {
        EndMission(MissionOutcome.Retreat);
        SimLog.Log("[Combat] RETREAT COMPLETE! All crew extracted.");
        return;
    }
    
    // Existing victory/defeat checks...
    var aliveCrewCount = 0;
    var aliveEnemyCount = 0;
    
    foreach (var actor in Actors)
    {
        if (actor.State != ActorState.Alive) continue;
        
        if (actor.Type == ActorTypes.Crew)
            aliveCrewCount++;
        else if (actor.Type == ActorTypes.Enemy)
            aliveEnemyCount++;
    }
    
    if (hasEnemyObjective && aliveEnemyCount == 0)
    {
        EndMission(MissionOutcome.Victory);
        SimLog.Log("[Combat] VICTORY! All enemies eliminated.");
    }
    else if (aliveCrewCount == 0)
    {
        EndMission(MissionOutcome.Defeat);
        SimLog.Log("[Combat] DEFEAT! All crew eliminated.");
    }
}

private void EndMission(MissionOutcome outcome)
{
    Victory = (outcome == MissionOutcome.Victory);
    Phase = MissionPhase.Complete;
    TimeSystem.Pause();
    PhaseChanged?.Invoke(Phase);
    MissionEnded?.Invoke(Victory); // Keep for backward compatibility
    MissionCompleted?.Invoke(outcome);
}
```

**Acceptance Criteria**:
- [ ] `InitiateRetreat()` sets retreat state
- [ ] `AreAllCrewInEntryZone()` correctly checks crew positions
- [ ] Mission ends with `MissionOutcome.Retreat` when all crew extract
- [ ] `GetCrewExtractionStatus()` returns extraction progress

---

#### Step 2.2: Add Retreat UI to MissionView

**File**: `src/scenes/mission/MissionView.cs`

**Add retreat button and status**:
```csharp
private Button retreatButton;
private Label extractionStatusLabel;

// In SetupUI():
private void SetupRetreatUI()
{
    retreatButton = new Button();
    retreatButton.Text = "Retreat";
    retreatButton.Position = new Vector2(10, 120);
    retreatButton.Size = new Vector2(100, 30);
    retreatButton.Pressed += OnRetreatPressed;
    uiLayer.AddChild(retreatButton);
    
    extractionStatusLabel = new Label();
    extractionStatusLabel.Position = new Vector2(10, 155);
    extractionStatusLabel.Visible = false;
    uiLayer.AddChild(extractionStatusLabel);
}

private void OnRetreatPressed()
{
    if (CombatState.IsRetreating)
    {
        CombatState.CancelRetreat();
        retreatButton.Text = "Retreat";
        extractionStatusLabel.Visible = false;
    }
    else
    {
        CombatState.InitiateRetreat();
        retreatButton.Text = "Cancel Retreat";
        extractionStatusLabel.Visible = true;
    }
}

// In _Process():
private void UpdateExtractionStatus()
{
    if (!CombatState.IsRetreating)
    {
        return;
    }
    
    var (inZone, total) = CombatState.GetCrewExtractionStatus();
    extractionStatusLabel.Text = $"Extraction: {inZone}/{total} in zone";
    
    if (inZone == total && total > 0)
    {
        extractionStatusLabel.AddThemeColorOverride("font_color", Colors.Green);
    }
    else
    {
        extractionStatusLabel.AddThemeColorOverride("font_color", Colors.Yellow);
    }
}
```

**Add entry zone visualization**:
```csharp
private void DrawEntryZone()
{
    // Highlight entry zone tiles when retreating
    if (!CombatState.IsRetreating)
    {
        return;
    }
    
    foreach (var pos in CombatState.MapState.EntryZone)
    {
        var worldPos = GridToWorld(pos);
        // Draw green highlight for extraction zone
        DrawRect(new Rect2(worldPos, new Vector2(GridConstants.TileSize, GridConstants.TileSize)), 
                 new Color(0, 1, 0, 0.3f));
    }
}
```

**Acceptance Criteria**:
- [ ] Retreat button toggles retreat state
- [ ] Extraction status shows crew in zone / total
- [ ] Entry zone is highlighted during retreat
- [ ] UI updates in real-time as crew move

---

### Phase 3: Output Generation (Priority: Critical)

#### Step 3.1: Create MissionOutputBuilder

**New File**: `src/sim/combat/MissionOutputBuilder.cs`

```csharp
using System.Collections.Generic;
using System.Linq;

namespace FringeTactics;

/// <summary>
/// Builds a MissionOutput from the final CombatState.
/// </summary>
public static class MissionOutputBuilder
{
    /// <summary>
    /// Build complete mission output from combat state.
    /// </summary>
    public static MissionOutput Build(
        CombatState combat, 
        MissionOutcome outcome,
        Dictionary<int, int> actorToCrewMap)
    {
        var output = new MissionOutput
        {
            Outcome = outcome,
            MissionId = combat.MissionConfig?.Id ?? "unknown",
            TicksElapsed = combat.TimeSystem.CurrentTick,
            MissionDurationSeconds = combat.TimeSystem.CurrentTick * combat.TimeSystem.TickDuration,
            AlarmTriggered = combat.Perception.AlarmState == AlarmState.Alerted
        };
        
        // Count enemies
        foreach (var actor in combat.Actors)
        {
            if (actor.Type != ActorTypes.Enemy) continue;
            
            if (actor.State == ActorState.Dead)
                output.EnemiesKilled++;
            else
                output.EnemiesRemaining++;
        }
        
        // Build crew outcomes
        foreach (var actor in combat.Actors)
        {
            if (actor.Type != ActorTypes.Crew) continue;
            
            var crewId = actorToCrewMap.TryGetValue(actor.Id, out var id) ? id : -1;
            var crewOutcome = BuildCrewOutcome(actor, crewId, combat, outcome);
            output.CrewOutcomes.Add(crewOutcome);
        }
        
        // Build objective results (placeholder - expand with objective system)
        if (combat.MissionConfig != null)
        {
            // Default: if victory, primary objective complete
            output.ObjectiveResults["primary"] = outcome == MissionOutcome.Victory 
                ? ObjectiveStatus.Complete 
                : ObjectiveStatus.Failed;
        }
        
        return output;
    }
    
    private static CrewOutcome BuildCrewOutcome(
        Actor actor, 
        int campaignCrewId,
        CombatState combat,
        MissionOutcome missionOutcome)
    {
        var outcome = new CrewOutcome
        {
            CampaignCrewId = campaignCrewId,
            Name = actor.Name,
            FinalHp = actor.Hp,
            MaxHp = actor.MaxHp,
            DamageTaken = actor.MaxHp - actor.Hp,
            Kills = actor.Kills,
            ShotsFired = actor.ShotsFired,
            ShotsHit = actor.ShotsHit
        };
        
        // Determine status
        if (actor.State == ActorState.Dead)
        {
            outcome.Status = CrewFinalStatus.Dead;
        }
        else if (missionOutcome == MissionOutcome.Retreat && 
                 !combat.MapState.IsInEntryZone(actor.GridPosition))
        {
            outcome.Status = CrewFinalStatus.MIA;
        }
        else if (actor.Hp <= actor.MaxHp * 0.25f)
        {
            outcome.Status = CrewFinalStatus.Critical;
        }
        else if (actor.Hp <= actor.MaxHp * 0.5f)
        {
            outcome.Status = CrewFinalStatus.Wounded;
        }
        else
        {
            outcome.Status = CrewFinalStatus.Alive;
        }
        
        // Calculate ammo usage
        if (actor.Weapon != null)
        {
            outcome.AmmoRemaining = actor.Weapon.CurrentAmmo + actor.Weapon.ReserveAmmo;
            // AmmoUsed would need tracking in Actor
        }
        
        // Suggest XP based on performance
        outcome.SuggestedXp = CalculateSuggestedXp(outcome, missionOutcome);
        
        // Add injuries based on damage taken
        if (outcome.Status == CrewFinalStatus.Wounded)
        {
            outcome.NewInjuries.Add("wounded");
        }
        else if (outcome.Status == CrewFinalStatus.Critical)
        {
            outcome.NewInjuries.Add("critical_wound");
        }
        
        return outcome;
    }
    
    private static int CalculateSuggestedXp(CrewOutcome crew, MissionOutcome outcome)
    {
        int xp = 0;
        
        // Base participation XP
        if (crew.Status != CrewFinalStatus.Dead && crew.Status != CrewFinalStatus.MIA)
        {
            xp += 10; // CampaignState.XP_PARTICIPATION
        }
        
        // Kill XP
        xp += crew.Kills * 25; // CampaignState.XP_PER_KILL
        
        // Victory bonus
        if (outcome == MissionOutcome.Victory)
        {
            xp += 20;
        }
        else if (outcome == MissionOutcome.Retreat)
        {
            xp += 5; // Partial credit for surviving
        }
        
        return xp;
    }
}
```

**Acceptance Criteria**:
- [ ] `MissionOutputBuilder.Build()` creates complete output
- [ ] Crew outcomes include status, health, kills, ammo
- [ ] MIA status assigned to crew left behind during retreat
- [ ] XP suggestions based on performance

---

#### Step 3.2: Add Combat Statistics Tracking to Actor

**File**: `src/sim/combat/Actor.cs`

**Add tracking fields**:
```csharp
// Combat statistics
public int Kills { get; set; } = 0;
public int ShotsFired { get; set; } = 0;
public int ShotsHit { get; set; } = 0;
public int DamageDealt { get; set; } = 0;
public int DamageTaken { get; set; } = 0;

/// <summary>
/// Record a kill by this actor.
/// </summary>
public void RecordKill()
{
    Kills++;
}

/// <summary>
/// Record a shot fired.
/// </summary>
public void RecordShot(bool hit, int damage = 0)
{
    ShotsFired++;
    if (hit)
    {
        ShotsHit++;
        DamageDealt += damage;
    }
}
```

**Update AttackSystem to record stats**:
```csharp
// In ProcessAttack():
attacker.RecordShot(result.Hit, result.Damage);
if (result.TargetKilled)
{
    attacker.RecordKill();
}
target.DamageTaken += result.Damage;
```

**Acceptance Criteria**:
- [ ] Actor tracks kills, shots fired, shots hit
- [ ] AttackSystem updates stats on each attack
- [ ] Stats available for MissionOutput generation

---

### Phase 4: Integration with GameState (Priority: High)

#### Step 4.1: Update GameState.EndMission() to Use New Contracts

**File**: `src/core/GameState.cs`

**Refactor EndMission to use MissionOutput**:
```csharp
public void EndMission(MissionOutcome outcome, CombatState combatState)
{
    if (Campaign == null)
    {
        Mode = "menu";
        GoToMainMenu();
        return;
    }
    
    // Build formal mission output
    var output = MissionOutputBuilder.Build(combatState, outcome, actorToCrewMap);
    
    // Convert to legacy MissionResult for now (gradual migration)
    var result = ConvertToLegacyResult(output);
    
    Campaign.ApplyMissionResult(result);
    CurrentCombat = null;
    
    // Log mission summary
    LogMissionSummary(output);
    
    // Check for campaign over
    if (Campaign.IsCampaignOver())
    {
        GD.Print("[GameState] Campaign over - all crew lost!");
        Mode = "gameover";
        GetTree().ChangeSceneToFile(CampaignOverScene);
        return;
    }
    
    Mode = "sector";
    GoToSectorView();
}

private MissionResult ConvertToLegacyResult(MissionOutput output)
{
    var result = new MissionResult
    {
        Victory = output.Outcome == MissionOutcome.Victory,
        EnemiesKilled = output.EnemiesKilled
    };
    
    foreach (var crew in output.CrewOutcomes)
    {
        if (crew.Status == CrewFinalStatus.Dead || crew.Status == CrewFinalStatus.MIA)
        {
            result.DeadCrewIds.Add(crew.CampaignCrewId);
        }
        else
        {
            result.CrewXpGains[crew.CampaignCrewId] = crew.SuggestedXp;
            
            if (crew.Status == CrewFinalStatus.Wounded || 
                crew.Status == CrewFinalStatus.Critical)
            {
                result.InjuredCrewIds.Add(crew.CampaignCrewId);
            }
        }
    }
    
    return result;
}

private void LogMissionSummary(MissionOutput output)
{
    GD.Print($"[Mission Complete] Outcome: {output.Outcome}");
    GD.Print($"  Enemies: {output.EnemiesKilled} killed, {output.EnemiesRemaining} remaining");
    GD.Print($"  Alarm: {(output.AlarmTriggered ? "Triggered" : "Quiet")}");
    GD.Print($"  Duration: {output.MissionDurationSeconds:F1}s ({output.TicksElapsed} ticks)");
    
    foreach (var crew in output.CrewOutcomes)
    {
        GD.Print($"  {crew.Name}: {crew.Status}, {crew.Kills} kills, {crew.FinalHp}/{crew.MaxHp} HP");
    }
}
```

**Acceptance Criteria**:
- [ ] `EndMission()` uses `MissionOutputBuilder`
- [ ] Legacy `MissionResult` still works for campaign
- [ ] Mission summary logged on completion

---

### Phase 5: MissionFactory Updates (Priority: High)

#### Step 5.1: Add BuildFromInput Method

**File**: `src/sim/combat/MissionFactory.cs`

**Add new build method using MissionInput**:
```csharp
/// <summary>
/// Build a CombatState from a formal MissionInput.
/// This is the preferred method for campaign-driven missions.
/// </summary>
public static MissionBuildResult BuildFromInput(MissionInput input)
{
    var combat = new CombatState(input.Seed);
    var actorToCrewMap = new Dictionary<int, int>();
    
    // Build map from template
    if (input.MapTemplate != null)
    {
        combat.MapState = MapBuilder.BuildFromTemplate(input.MapTemplate, combat.Interactions);
    }
    else if (input.GridSize.HasValue)
    {
        combat.MapState = new MapState(input.GridSize.Value);
    }
    
    combat.InitializeVisibility();
    
    // Spawn crew from deployment specs
    foreach (var crew in input.Crew)
    {
        var spawnPos = crew.SpawnPosition ?? GetNextEntryZonePosition(combat.MapState);
        var actor = combat.AddActor(ActorTypes.Crew, spawnPos);
        
        // Apply crew stats
        actor.Name = crew.Name;
        actor.MaxHp = crew.MaxHp;
        actor.Hp = crew.CurrentHp;
        actor.MoveSpeed = crew.MoveSpeed;
        actor.BaseAccuracy = crew.Accuracy;
        
        // Apply weapon
        if (!string.IsNullOrEmpty(crew.WeaponId))
        {
            var weaponDef = Definitions.GetWeapon(crew.WeaponId);
            if (weaponDef != null)
            {
                actor.Weapon = new Weapon(weaponDef);
                actor.Weapon.CurrentAmmo = crew.AmmoInMagazine;
                actor.Weapon.ReserveAmmo = crew.ReserveAmmo;
            }
        }
        
        actorToCrewMap[actor.Id] = crew.CampaignCrewId;
    }
    
    // Spawn enemies
    foreach (var enemySpawn in input.Enemies)
    {
        var actor = combat.AddActor(ActorTypes.Enemy, enemySpawn.Position);
        ApplyEnemyTemplate(actor, enemySpawn.TemplateId);
    }
    
    // Spawn interactables
    foreach (var interactable in input.Interactables)
    {
        combat.Interactions.AddInteractable(
            interactable.Type,
            interactable.Position,
            interactable.InitialState,
            interactable.Properties
        );
    }
    
    // Set up objectives (placeholder for objective system)
    combat.MissionConfig = new MissionConfig
    {
        Id = input.MissionId,
        Name = input.MissionName
    };
    
    // Initialize perception after all actors spawned
    combat.SetHasEnemyObjective(input.Enemies.Count > 0);
    combat.InitializePerception();
    
    return new MissionBuildResult
    {
        CombatState = combat,
        ActorToCrewMap = actorToCrewMap
    };
}

private static Vector2I GetNextEntryZonePosition(MapState map)
{
    // Simple: return first available entry zone position
    // Future: track used positions to avoid stacking
    if (map.EntryZone.Count > 0)
    {
        return map.EntryZone[0];
    }
    return new Vector2I(1, 1); // Fallback
}
```

**Acceptance Criteria**:
- [ ] `BuildFromInput()` creates CombatState from MissionInput
- [ ] Crew stats and equipment applied from deployment specs
- [ ] Entry zone positions used for spawning
- [ ] Actor-to-crew mapping returned for output generation

---

### Phase 6: Test Mission & Validation (Priority: High)

#### Step 6.1: Create M7 Test Mission

**File**: `src/sim/data/MissionConfig.cs`

```csharp
/// <summary>
/// M7 test mission - session I/O and retreat testing.
/// Features entry zone, enemies, and retreat scenarios.
/// </summary>
public static MissionConfig CreateM7TestMission()
{
    return new MissionConfig
    {
        Id = "m7_test",
        Name = "M7 Test - Session I/O & Retreat",
        MapTemplate = new string[]
        {
            "####################",
            "#EE................#",
            "#EE................#",
            "#..................#",
            "#....###....###....#",
            "#....#.#....#.#....#",
            "#....#D#....#D#....#",
            "#....###....###....#",
            "#..................#",
            "#..................#",
            "#........E.........#",
            "#..................#",
            "#....###....###....#",
            "#....#T#....#T#....#",
            "#....###....###....#",
            "#..................#",
            "#.........E........#",
            "#..................#",
            "#..................#",
            "####################"
        },
        CrewSpawnPositions = new List<Vector2I>
        {
            new Vector2I(1, 1),
            new Vector2I(2, 1),
            new Vector2I(1, 2),
            new Vector2I(2, 2)
        },
        EnemySpawns = new List<EnemySpawn>
        {
            new EnemySpawn("grunt", new Vector2I(9, 10)),
            new EnemySpawn("grunt", new Vector2I(10, 16)),
        }
    };
}
```

**Map design rationale**:
- Entry zone (EE) at top-left for clear extraction point
- Enemies positioned to create combat scenarios
- Terminals (T) for objective testing
- Doors (D) for tactical options
- Open layout allows retreat path visibility

---

## Testing Checklist

### Manual Test Setup

Launch **"M7 Test (Session I/O & Retreat)"** from main menu.

### Manual Testing

1. **Mission Start (Input Contract)**
   - [ ] Crew spawns in entry zone
   - [ ] Crew has correct HP, weapons, ammo
   - [ ] Enemies spawn at configured positions
   - [ ] Entry zone tiles are identifiable

2. **Retreat Initiation**
   - [ ] Retreat button visible in UI
   - [ ] Clicking Retreat shows extraction status
   - [ ] Entry zone highlighted when retreating
   - [ ] Can cancel retreat

3. **Retreat Completion**
   - [ ] Move all crew to entry zone
   - [ ] Mission ends with "Retreat" outcome
   - [ ] Extraction status shows progress (e.g., "2/4 in zone")

4. **Crew Left Behind (MIA)**
   - [ ] Start retreat with crew spread out
   - [ ] Extract some crew, leave others
   - [ ] Left-behind crew marked as MIA in results

5. **Victory Path**
   - [ ] Kill all enemies
   - [ ] Mission ends with Victory
   - [ ] Results show kills, damage, etc.

6. **Defeat Path**
   - [ ] Let all crew die
   - [ ] Mission ends with Defeat
   - [ ] Results show all crew as Dead

7. **Mission Output Verification**
   - [ ] Check console for mission summary log
   - [ ] Verify enemy kill count
   - [ ] Verify alarm state captured
   - [ ] Verify per-crew statistics

8. **Campaign Integration**
   - [ ] Start campaign, take job, complete mission
   - [ ] Verify crew XP applied
   - [ ] Verify injuries applied
   - [ ] Verify dead crew removed

### Automated Tests

Create `tests/sim/combat/M7Tests.cs`:

```csharp
using Godot;
using GdUnit4;
using static GdUnit4.Assertions;

namespace FringeTactics.Tests;

/// <summary>
/// Tests for M7: Session I/O & Retreat Integration
/// </summary>
[TestSuite]
public class M7Tests
{
    // === Retreat State Tests ===
    
    [TestCase]
    public void CombatState_InitiateRetreat_SetsFlag()
    {
        var combat = CreateTestCombat();
        
        combat.InitiateRetreat();
        
        AssertThat(combat.IsRetreating).IsTrue();
    }
    
    [TestCase]
    public void CombatState_CancelRetreat_ClearsFlag()
    {
        var combat = CreateTestCombat();
        combat.InitiateRetreat();
        
        combat.CancelRetreat();
        
        AssertThat(combat.IsRetreating).IsFalse();
    }
    
    [TestCase]
    [RequireGodotRuntime]
    public void CombatState_AreAllCrewInEntryZone_TrueWhenAllInZone()
    {
        var combat = CreateTestCombatWithEntryZone();
        var crew1 = combat.AddActor(ActorTypes.Crew, new Vector2I(1, 1)); // In entry zone
        var crew2 = combat.AddActor(ActorTypes.Crew, new Vector2I(2, 1)); // In entry zone
        
        AssertThat(combat.AreAllCrewInEntryZone()).IsTrue();
    }
    
    [TestCase]
    [RequireGodotRuntime]
    public void CombatState_AreAllCrewInEntryZone_FalseWhenSomeOutside()
    {
        var combat = CreateTestCombatWithEntryZone();
        var crew1 = combat.AddActor(ActorTypes.Crew, new Vector2I(1, 1)); // In entry zone
        var crew2 = combat.AddActor(ActorTypes.Crew, new Vector2I(5, 5)); // Outside
        
        AssertThat(combat.AreAllCrewInEntryZone()).IsFalse();
    }
    
    [TestCase]
    [RequireGodotRuntime]
    public void CombatState_RetreatCompletes_WhenAllCrewExtract()
    {
        var combat = CreateTestCombatWithEntryZone();
        var crew = combat.AddActor(ActorTypes.Crew, new Vector2I(1, 1));
        combat.InitializePerception();
        
        MissionOutcome? outcome = null;
        combat.MissionCompleted += o => outcome = o;
        
        combat.InitiateRetreat();
        combat.Update(0.1f); // Process tick
        
        AssertThat(outcome).IsEqual(MissionOutcome.Retreat);
    }
    
    // === Mission Output Tests ===
    
    [TestCase]
    [RequireGodotRuntime]
    public void MissionOutputBuilder_BuildsCorrectOutcome()
    {
        var combat = CreateTestCombat();
        var crew = combat.AddActor(ActorTypes.Crew, new Vector2I(1, 1));
        var actorToCrewMap = new Dictionary<int, int> { { crew.Id, 100 } };
        
        var output = MissionOutputBuilder.Build(combat, MissionOutcome.Victory, actorToCrewMap);
        
        AssertThat(output.Outcome).IsEqual(MissionOutcome.Victory);
        AssertThat(output.CrewOutcomes.Count).IsEqual(1);
        AssertThat(output.CrewOutcomes[0].CampaignCrewId).IsEqual(100);
    }
    
    [TestCase]
    [RequireGodotRuntime]
    public void MissionOutputBuilder_TracksEnemyKills()
    {
        var combat = CreateTestCombat();
        var enemy = combat.AddActor(ActorTypes.Enemy, new Vector2I(5, 5));
        enemy.TakeDamage(1000); // Kill enemy
        
        var output = MissionOutputBuilder.Build(combat, MissionOutcome.Victory, new());
        
        AssertThat(output.EnemiesKilled).IsEqual(1);
        AssertThat(output.EnemiesRemaining).IsEqual(0);
    }
    
    [TestCase]
    [RequireGodotRuntime]
    public void MissionOutputBuilder_TracksAlarmState()
    {
        var combat = CreateTestCombat();
        combat.InitializePerception();
        combat.Perception.AlertAllEnemies();
        
        var output = MissionOutputBuilder.Build(combat, MissionOutcome.Victory, new());
        
        AssertThat(output.AlarmTriggered).IsTrue();
    }
    
    [TestCase]
    [RequireGodotRuntime]
    public void CrewOutcome_MIA_WhenLeftBehindDuringRetreat()
    {
        var combat = CreateTestCombatWithEntryZone();
        var crew1 = combat.AddActor(ActorTypes.Crew, new Vector2I(1, 1)); // In zone
        var crew2 = combat.AddActor(ActorTypes.Crew, new Vector2I(10, 10)); // Outside
        var actorToCrewMap = new Dictionary<int, int> 
        { 
            { crew1.Id, 1 }, 
            { crew2.Id, 2 } 
        };
        
        var output = MissionOutputBuilder.Build(combat, MissionOutcome.Retreat, actorToCrewMap);
        
        var crew1Outcome = output.CrewOutcomes.First(c => c.CampaignCrewId == 1);
        var crew2Outcome = output.CrewOutcomes.First(c => c.CampaignCrewId == 2);
        
        AssertThat(crew1Outcome.Status).IsEqual(CrewFinalStatus.Alive);
        AssertThat(crew2Outcome.Status).IsEqual(CrewFinalStatus.MIA);
    }
    
    // === Mission Input Tests ===
    
    [TestCase]
    [RequireGodotRuntime]
    public void MissionFactory_BuildFromInput_SpawnsCrewWithStats()
    {
        var input = new MissionInput
        {
            MapTemplate = new[] { "###", "#E#", "###" },
            Seed = 12345,
            Crew = new List<CrewDeployment>
            {
                new CrewDeployment
                {
                    CampaignCrewId = 1,
                    Name = "Test Crew",
                    MaxHp = 150,
                    CurrentHp = 100,
                    SpawnPosition = new Vector2I(1, 1)
                }
            }
        };
        
        var result = MissionFactory.BuildFromInput(input);
        
        var crew = result.CombatState.Actors.First(a => a.Type == ActorTypes.Crew);
        AssertThat(crew.Name).IsEqual("Test Crew");
        AssertThat(crew.MaxHp).IsEqual(150);
        AssertThat(crew.Hp).IsEqual(100);
    }
    
    // === Actor Statistics Tests ===
    
    [TestCase]
    public void Actor_RecordKill_IncrementsKills()
    {
        var actor = new Actor(1, ActorTypes.Crew, new Vector2I(0, 0));
        
        actor.RecordKill();
        actor.RecordKill();
        
        AssertThat(actor.Kills).IsEqual(2);
    }
    
    [TestCase]
    public void Actor_RecordShot_TracksHitsAndMisses()
    {
        var actor = new Actor(1, ActorTypes.Crew, new Vector2I(0, 0));
        
        actor.RecordShot(hit: true, damage: 25);
        actor.RecordShot(hit: false);
        actor.RecordShot(hit: true, damage: 30);
        
        AssertThat(actor.ShotsFired).IsEqual(3);
        AssertThat(actor.ShotsHit).IsEqual(2);
        AssertThat(actor.DamageDealt).IsEqual(55);
    }
    
    // === Helper Methods ===
    
    private CombatState CreateTestCombat()
    {
        var combat = new CombatState(12345);
        combat.MapState = MapBuilder.BuildFromTemplate(new[] 
        {
            "########",
            "#......#",
            "#......#",
            "#......#",
            "########"
        });
        combat.InitializeVisibility();
        return combat;
    }
    
    private CombatState CreateTestCombatWithEntryZone()
    {
        var combat = new CombatState(12345);
        combat.MapState = MapBuilder.BuildFromTemplate(new[] 
        {
            "########",
            "#EE....#",
            "#EE....#",
            "#......#",
            "########"
        });
        combat.InitializeVisibility();
        return combat;
    }
}
```

---

## Implementation Order

1. **Day 1: Core Data Structures**
   - Step 1.1: Create MissionInput and supporting types
   - Step 1.2: Create MissionOutput and supporting types

2. **Day 2: Retreat System**
   - Step 2.1: Add retreat state to CombatState
   - Step 2.2: Add retreat UI to MissionView

3. **Day 3: Output Generation**
   - Step 3.1: Create MissionOutputBuilder
   - Step 3.2: Add combat statistics tracking to Actor

4. **Day 4: Integration**
   - Step 4.1: Update GameState.EndMission()
   - Step 5.1: Add MissionFactory.BuildFromInput()

5. **Day 5: Testing & Polish**
   - Step 6.1: Create M7 test mission
   - Write automated tests
   - Manual testing and bug fixes

---

## Success Criteria for M7

When M7 is complete, you should be able to:

1. ✅ Start a mission with formal input contract (crew stats, equipment, objectives)
2. ✅ Initiate retreat via UI button
3. ✅ See extraction progress (X/Y crew in zone)
4. ✅ Complete retreat when all crew reach entry zone
5. ✅ See crew marked as MIA if left behind during retreat
6. ✅ Get detailed mission output (kills, damage, alarm state, per-crew stats)
7. ✅ Have campaign correctly apply mission results
8. ✅ All automated tests pass

**Natural Pause Point**: After M7, the tactical layer has a clean, well-defined interface with the campaign layer. You can confidently build campaign features knowing exactly what data flows in and out of missions.

---

## Notes for Future Milestones

### M8 Dependencies (UX & Feel Pass)
- Retreat UI can be polished with better visual feedback
- Entry zone visualization can be enhanced
- Mission summary screen can display detailed output

### Post-M8 Enhancements
- **Objective System**: Full objective tracking with multiple types
- **Loot System**: Items acquired during mission
- **World Deltas**: Mission outcomes affecting world state
- **Partial Success**: Graduated outcomes between victory and defeat

---

## Files to Create/Modify

### New Files
- `src/sim/combat/MissionInput.cs`
- `src/sim/combat/MissionOutput.cs`
- `src/sim/combat/MissionOutputBuilder.cs`
- `tests/sim/combat/M7Tests.cs`

### Modified Files
- `src/sim/combat/CombatState.cs` - Add retreat state and methods
- `src/sim/combat/Actor.cs` - Add combat statistics tracking
- `src/sim/combat/AttackSystem.cs` - Record stats on attacks
- `src/sim/combat/MissionFactory.cs` - Add BuildFromInput()
- `src/core/GameState.cs` - Update EndMission() to use new contracts
- `src/scenes/mission/MissionView.cs` - Add retreat UI
- `src/sim/data/MissionConfig.cs` - Add M7 test mission
- `src/scenes/menu/MainMenu.cs` - Add M7 test button

---

## Open Questions

1. **MIA Crew Fate**: What happens to MIA crew in campaign?
   - *Decision for M7*: Treated as dead for simplicity.
   - *Future*: Could become rescue mission opportunities.

2. **Retreat Penalty**: Should retreat have resource/reputation cost?
   - *Decision for M7*: No penalty beyond lost objectives.
   - *Future*: Could add reputation hit or partial contract failure.

3. **Partial Extraction**: Can some crew extract while others fight?
   - *Decision for M7*: No, retreat is all-or-nothing.
   - *Future*: Could allow individual extraction.

4. **Objective Completion During Retreat**: Do completed objectives count?
   - *Decision for M7*: Yes, objectives completed before retreat are preserved.

5. **Alarm State Impact**: Does alarm affect retreat difficulty?
   - *Decision for M7*: No mechanical impact.
   - *Future*: Could spawn reinforcements or block exits.
