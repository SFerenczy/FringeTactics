# HH4 – Retreat & Value Extraction: Implementation Plan

This document breaks down **HH4** from `ROADMAP.md` into concrete implementation steps.

**Goal**: Make retreat a designed outcome with clear tradeoffs, not a failure state.

**Tactical Axes**: Time + Value Extraction

---

## Current State Assessment

### What We Have (From M0–HH3)

| Component | Status | Notes |
|-----------|--------|-------|
| `CombatState` | ✅ Complete | Mission state, actor tracking |
| `PhaseSystem` | ✅ HH3 | Mission phases including Resolution |
| `MissionConfig` | ✅ HH3 | Mission setup with zones |
| `MissionView` | ✅ Complete | UI, actor control |
| `InteractionSystem` | ✅ M5 | Object interactions |

### What HH4 Requires vs What We Have

| HH4 Requirement | Current Status | Gap |
|-----------------|----------------|-----|
| Evac zone | ❌ Missing | Need extraction zone definition |
| Unit extraction | ❌ Missing | Need extraction mechanic |
| Partial extraction | ❌ Missing | Need per-unit extraction state |
| Loot pickups | ❌ Missing | Need loot interactables |
| Optional objectives | ❌ Missing | Need objective system |
| Mission outcomes | ⚠️ Partial | Win/lose exists, need gradations |
| Mission summary | ❌ Missing | Need detailed results screen |

---

## Architecture Decisions

### Extraction Model

**Decision**: Zone-based extraction with per-unit tracking.

```csharp
public class ExtractionState
{
    public HashSet<Vector2I> EvacZone { get; set; }
    public HashSet<int> ExtractedActorIds { get; set; }
    public bool EvacAvailable { get; set; } = true;
    public int EvacDelayTicks { get; set; } = 0;  // Countdown before evac opens
}
```

**Extraction Rules**:
- Units in evac zone can extract (action or automatic)
- Extracted units are removed from combat but survive
- Mission can end when all surviving crew are extracted
- Partial extraction is valid (some crew left behind)

### Loot System Model

**Decision**: Loot as interactables with value and risk.

```csharp
public class LootData
{
    public string Id { get; set; }
    public string Name { get; set; }
    public int Value { get; set; }           // Credits or abstract value
    public LootType Type { get; set; }       // Credits, Item, Intel, etc.
    public bool RequiresCarry { get; set; }  // Must be carried to evac
    public int CarrySlowdown { get; set; }   // Movement penalty when carrying
}

public enum LootType
{
    Credits,
    Item,
    Intel,
    Objective  // Required for mission success
}
```

### Objective System Model

**Decision**: Explicit primary and secondary objectives.

```csharp
public class MissionObjective
{
    public string Id { get; set; }
    public string Description { get; set; }
    public ObjectiveType Type { get; set; }
    public ObjectiveState State { get; set; } = ObjectiveState.Pending;
    public bool IsPrimary { get; set; }
    public int RewardValue { get; set; }
}

public enum ObjectiveType
{
    Survive,           // At least one crew extracts
    ExtractAll,        // All crew extract
    KillTarget,        // Kill specific enemy
    RetrieveLoot,      // Get specific loot to evac
    HackTerminal,      // Complete terminal hack
    ReachZone,         // Get unit to specific area
    TimeSurvive        // Survive for X time
}

public enum ObjectiveState
{
    Pending,
    InProgress,
    Completed,
    Failed
}
```

### Mission Outcome Model

**Decision**: Graduated outcomes based on objectives and extraction.

```csharp
public class MissionOutcome
{
    public MissionResult Result { get; set; }
    public List<int> ExtractedCrewIds { get; set; }
    public List<int> DeadCrewIds { get; set; }
    public List<int> LeftBehindCrewIds { get; set; }
    public List<MissionObjective> CompletedObjectives { get; set; }
    public List<MissionObjective> FailedObjectives { get; set; }
    public List<LootData> CollectedLoot { get; set; }
    public int TotalValue { get; set; }
    public CombatStats Stats { get; set; }
}

public enum MissionResult
{
    TotalVictory,    // All objectives, all crew
    Victory,         // Primary objectives, most crew
    PartialSuccess,  // Primary objectives, some losses
    Retreat,         // Survived but failed primary
    Defeat           // All crew dead
}
```

---

## Implementation Steps

### Phase 1: Extraction System (Priority: Critical)

#### Step 1.1: Create ExtractionSystem

**New File**: `src/sim/combat/systems/ExtractionSystem.cs`

```csharp
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FringeTactics;

/// <summary>
/// Manages unit extraction and evac zones.
/// </summary>
public class ExtractionSystem
{
    private readonly CombatState combatState;
    private readonly HashSet<Vector2I> evacZone = new();
    private readonly HashSet<int> extractedActorIds = new();
    private readonly HashSet<int> leftBehindActorIds = new();
    
    public bool EvacAvailable { get; private set; } = false;
    public int EvacCountdown { get; private set; } = 0;
    public IReadOnlySet<Vector2I> EvacZone => evacZone;
    public IReadOnlySet<int> ExtractedActorIds => extractedActorIds;
    
    public event Action EvacOpened;
    public event Action<Actor> ActorExtracted;
    public event Action<Actor> ActorLeftBehind;
    public event Action AllCrewExtracted;
    
    public ExtractionSystem(CombatState combatState)
    {
        this.combatState = combatState;
    }
    
    public void SetEvacZone(IEnumerable<Vector2I> tiles)
    {
        evacZone.Clear();
        foreach (var tile in tiles)
        {
            evacZone.Add(tile);
        }
    }
    
    public void SetEvacDelay(int delayTicks)
    {
        EvacCountdown = delayTicks;
        EvacAvailable = delayTicks <= 0;
    }
    
    public void OpenEvacImmediately()
    {
        EvacCountdown = 0;
        EvacAvailable = true;
        EvacOpened?.Invoke();
        SimLog.Log("[Extraction] Evac zone is now available!");
    }
    
    public void Tick()
    {
        // Countdown to evac
        if (!EvacAvailable && EvacCountdown > 0)
        {
            EvacCountdown--;
            if (EvacCountdown <= 0)
            {
                EvacAvailable = true;
                EvacOpened?.Invoke();
                SimLog.Log("[Extraction] Evac zone is now available!");
            }
        }
        
        // Check for units in evac zone
        if (EvacAvailable)
        {
            CheckAutoExtraction();
        }
    }
    
    private void CheckAutoExtraction()
    {
        foreach (var actor in combatState.Actors.ToList())
        {
            if (actor.Type != ActorType.Crew) continue;
            if (actor.State != ActorState.Alive) continue;
            if (extractedActorIds.Contains(actor.Id)) continue;
            
            if (IsInEvacZone(actor.GridPosition))
            {
                // Auto-extract after brief delay in zone
                // For now, require manual extraction
            }
        }
    }
    
    public bool IsInEvacZone(Vector2I position)
    {
        return evacZone.Contains(position);
    }
    
    public bool CanExtract(Actor actor)
    {
        if (!EvacAvailable) return false;
        if (actor.Type != ActorType.Crew) return false;
        if (actor.State != ActorState.Alive) return false;
        if (extractedActorIds.Contains(actor.Id)) return false;
        if (!IsInEvacZone(actor.GridPosition)) return false;
        return true;
    }
    
    public bool ExtractActor(Actor actor)
    {
        if (!CanExtract(actor)) return false;
        
        extractedActorIds.Add(actor.Id);
        actor.State = ActorState.Down; // Remove from combat but not dead
        
        SimLog.Log($"[Extraction] {actor.Name ?? $"Crew#{actor.Id}"} extracted!");
        ActorExtracted?.Invoke(actor);
        
        // Check if all crew extracted
        CheckAllExtracted();
        
        return true;
    }
    
    public void ExtractAllInZone()
    {
        foreach (var actor in combatState.Actors.ToList())
        {
            if (CanExtract(actor))
            {
                ExtractActor(actor);
            }
        }
    }
    
    private void CheckAllExtracted()
    {
        var remainingCrew = combatState.Actors
            .Where(a => a.Type == ActorType.Crew && 
                       a.State == ActorState.Alive && 
                       !extractedActorIds.Contains(a.Id))
            .ToList();
        
        if (remainingCrew.Count == 0)
        {
            AllCrewExtracted?.Invoke();
        }
    }
    
    /// <summary>
    /// Mark actors as left behind (for mission end without extraction).
    /// </summary>
    public void MarkLeftBehind()
    {
        foreach (var actor in combatState.Actors)
        {
            if (actor.Type != ActorType.Crew) continue;
            if (actor.State != ActorState.Alive) continue;
            if (extractedActorIds.Contains(actor.Id)) continue;
            
            leftBehindActorIds.Add(actor.Id);
            ActorLeftBehind?.Invoke(actor);
            SimLog.Log($"[Extraction] {actor.Name ?? $"Crew#{actor.Id}"} left behind!");
        }
    }
    
    public int ExtractedCount => extractedActorIds.Count;
    public int LeftBehindCount => leftBehindActorIds.Count;
    
    public bool HasExtractedAny => extractedActorIds.Count > 0;
}
```

**Acceptance Criteria**:
- [ ] Evac zone can be defined
- [ ] Units can extract when in zone
- [ ] Extraction removes unit from combat
- [ ] Events fire for UI feedback

#### Step 1.2: Integrate ExtractionSystem into CombatState

**File**: `src/sim/combat/state/CombatState.cs`

```csharp
public ExtractionSystem Extraction { get; private set; }

public CombatState(int seed)
{
    // ... existing code ...
    Extraction = new ExtractionSystem(this);
}

private void ProcessTick()
{
    // ... existing code ...
    Extraction.Tick();
}
```

---

### Phase 2: Loot System (Priority: High)

#### Step 2.1: Create Loot Interactable Type

**File**: `src/sim/combat/Interactable.cs`

Add loot type:
```csharp
public static class InteractableTypes
{
    public const string Door = "door";
    public const string Terminal = "terminal";
    public const string Hazard = "hazard";
    public const string Loot = "loot";  // NEW
}

public static class InteractableStates
{
    // ... existing states ...
    
    // Loot states
    public const string LootAvailable = "loot_available";
    public const string LootCollected = "loot_collected";
}
```

#### Step 2.2: Create LootSystem

**New File**: `src/sim/combat/systems/LootSystem.cs`

```csharp
using Godot;
using System;
using System.Collections.Generic;

namespace FringeTactics;

public enum LootType
{
    Credits,
    Item,
    Intel,
    Objective
}

public class LootData
{
    public string Id { get; set; }
    public string Name { get; set; }
    public int Value { get; set; }
    public LootType Type { get; set; }
    public bool RequiresCarry { get; set; } = false;
    public float CarrySpeedPenalty { get; set; } = 0.7f;
}

/// <summary>
/// Manages loot collection and carrying.
/// </summary>
public class LootSystem
{
    private readonly CombatState combatState;
    private readonly List<LootData> collectedLoot = new();
    private readonly Dictionary<int, LootData> carriedLoot = new(); // actorId → loot
    
    public IReadOnlyList<LootData> CollectedLoot => collectedLoot;
    public int TotalValue => collectedLoot.Sum(l => l.Value);
    
    public event Action<Actor, LootData> LootPickedUp;
    public event Action<Actor, LootData> LootDropped;
    public event Action<LootData> LootSecured;
    
    public LootSystem(CombatState combatState)
    {
        this.combatState = combatState;
        
        // Subscribe to extraction to secure carried loot
        combatState.Extraction.ActorExtracted += OnActorExtracted;
    }
    
    public bool PickUpLoot(Actor actor, Interactable lootInteractable)
    {
        if (lootInteractable.Type != InteractableTypes.Loot) return false;
        if (lootInteractable.State != InteractableStates.LootAvailable) return false;
        
        var lootData = GetLootData(lootInteractable);
        if (lootData == null) return false;
        
        // Mark as collected
        lootInteractable.SetState(InteractableStates.LootCollected);
        
        if (lootData.RequiresCarry)
        {
            // Actor must carry to evac
            carriedLoot[actor.Id] = lootData;
            ApplyCarryPenalty(actor, lootData);
            SimLog.Log($"[Loot] {actor.Type}#{actor.Id} picked up {lootData.Name} (carrying)");
        }
        else
        {
            // Instant collection
            collectedLoot.Add(lootData);
            LootSecured?.Invoke(lootData);
            SimLog.Log($"[Loot] {actor.Type}#{actor.Id} collected {lootData.Name} ({lootData.Value} value)");
        }
        
        LootPickedUp?.Invoke(actor, lootData);
        return true;
    }
    
    public void DropLoot(Actor actor)
    {
        if (!carriedLoot.TryGetValue(actor.Id, out var loot)) return;
        
        carriedLoot.Remove(actor.Id);
        RemoveCarryPenalty(actor);
        
        // Create dropped loot interactable at actor position
        var droppedLoot = new Interactable
        {
            Id = combatState.Interactions.GetNextId(),
            Type = InteractableTypes.Loot,
            Position = actor.GridPosition,
            State = InteractableStates.LootAvailable
        };
        droppedLoot.Properties["loot_data"] = loot;
        combatState.Interactions.AddInteractable(droppedLoot);
        
        LootDropped?.Invoke(actor, loot);
        SimLog.Log($"[Loot] {actor.Type}#{actor.Id} dropped {loot.Name}");
    }
    
    private void OnActorExtracted(Actor actor)
    {
        // Secure carried loot when actor extracts
        if (carriedLoot.TryGetValue(actor.Id, out var loot))
        {
            carriedLoot.Remove(actor.Id);
            collectedLoot.Add(loot);
            LootSecured?.Invoke(loot);
            SimLog.Log($"[Loot] {loot.Name} secured via extraction!");
        }
    }
    
    private void ApplyCarryPenalty(Actor actor, LootData loot)
    {
        var modifier = StatModifier.Multiplicative(
            $"carry_{loot.Id}", StatType.MoveSpeed, loot.CarrySpeedPenalty, -1);
        actor.Modifiers.Add(modifier);
    }
    
    private void RemoveCarryPenalty(Actor actor)
    {
        actor.Modifiers.RemoveBySource("carry_");
    }
    
    private LootData GetLootData(Interactable interactable)
    {
        if (interactable.Properties.TryGetValue("loot_data", out var data))
        {
            return data as LootData;
        }
        return null;
    }
    
    public bool IsCarryingLoot(Actor actor)
    {
        return carriedLoot.ContainsKey(actor.Id);
    }
    
    public LootData GetCarriedLoot(Actor actor)
    {
        return carriedLoot.TryGetValue(actor.Id, out var loot) ? loot : null;
    }
}
```

**Acceptance Criteria**:
- [ ] Loot can be picked up
- [ ] Carry loot applies movement penalty
- [ ] Extraction secures carried loot
- [ ] Dropped loot can be re-picked

---

### Phase 3: Objective System (Priority: High)

#### Step 3.1: Create ObjectiveSystem

**New File**: `src/sim/combat/systems/ObjectiveSystem.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Linq;

namespace FringeTactics;

public enum ObjectiveType
{
    Survive,
    ExtractAll,
    KillTarget,
    RetrieveLoot,
    HackTerminal,
    ReachZone,
    EliminateAll
}

public enum ObjectiveState
{
    Pending,
    InProgress,
    Completed,
    Failed
}

public class MissionObjective
{
    public string Id { get; set; }
    public string Description { get; set; }
    public ObjectiveType Type { get; set; }
    public ObjectiveState State { get; set; } = ObjectiveState.Pending;
    public bool IsPrimary { get; set; }
    public int RewardValue { get; set; }
    
    // Type-specific data
    public string TargetId { get; set; }      // For KillTarget, HackTerminal
    public string LootId { get; set; }        // For RetrieveLoot
    public string ZoneId { get; set; }        // For ReachZone
}

/// <summary>
/// Tracks mission objectives and their completion.
/// </summary>
public class ObjectiveSystem
{
    private readonly CombatState combatState;
    private readonly List<MissionObjective> objectives = new();
    
    public IReadOnlyList<MissionObjective> Objectives => objectives;
    public IEnumerable<MissionObjective> PrimaryObjectives => 
        objectives.Where(o => o.IsPrimary);
    public IEnumerable<MissionObjective> SecondaryObjectives => 
        objectives.Where(o => !o.IsPrimary);
    
    public event Action<MissionObjective> ObjectiveUpdated;
    public event Action<MissionObjective> ObjectiveCompleted;
    public event Action<MissionObjective> ObjectiveFailed;
    public event Action AllPrimaryCompleted;
    public event Action AnyPrimaryFailed;
    
    public ObjectiveSystem(CombatState combatState)
    {
        this.combatState = combatState;
        
        // Subscribe to relevant events
        combatState.Extraction.ActorExtracted += OnActorExtracted;
        combatState.Extraction.AllCrewExtracted += OnAllCrewExtracted;
    }
    
    public void AddObjective(MissionObjective objective)
    {
        objectives.Add(objective);
        SimLog.Log($"[Objective] Added: {objective.Description} (Primary={objective.IsPrimary})");
    }
    
    public void Tick()
    {
        foreach (var objective in objectives)
        {
            if (objective.State == ObjectiveState.Pending || 
                objective.State == ObjectiveState.InProgress)
            {
                CheckObjective(objective);
            }
        }
    }
    
    private void CheckObjective(MissionObjective objective)
    {
        var previousState = objective.State;
        
        switch (objective.Type)
        {
            case ObjectiveType.Survive:
                CheckSurviveObjective(objective);
                break;
            case ObjectiveType.ExtractAll:
                CheckExtractAllObjective(objective);
                break;
            case ObjectiveType.KillTarget:
                CheckKillTargetObjective(objective);
                break;
            case ObjectiveType.EliminateAll:
                CheckEliminateAllObjective(objective);
                break;
            case ObjectiveType.RetrieveLoot:
                CheckRetrieveLootObjective(objective);
                break;
            case ObjectiveType.HackTerminal:
                CheckHackTerminalObjective(objective);
                break;
        }
        
        if (objective.State != previousState)
        {
            ObjectiveUpdated?.Invoke(objective);
            
            if (objective.State == ObjectiveState.Completed)
            {
                ObjectiveCompleted?.Invoke(objective);
                SimLog.Log($"[Objective] COMPLETED: {objective.Description}");
                CheckAllPrimaryCompleted();
            }
            else if (objective.State == ObjectiveState.Failed)
            {
                ObjectiveFailed?.Invoke(objective);
                SimLog.Log($"[Objective] FAILED: {objective.Description}");
                if (objective.IsPrimary)
                {
                    AnyPrimaryFailed?.Invoke();
                }
            }
        }
    }
    
    private void CheckSurviveObjective(MissionObjective objective)
    {
        var aliveCrew = combatState.Actors
            .Count(a => a.Type == ActorType.Crew && a.State == ActorState.Alive);
        var extractedCrew = combatState.Extraction.ExtractedCount;
        
        if (aliveCrew + extractedCrew > 0)
        {
            objective.State = ObjectiveState.InProgress;
        }
        else
        {
            objective.State = ObjectiveState.Failed;
        }
    }
    
    private void CheckExtractAllObjective(MissionObjective objective)
    {
        var totalCrew = combatState.Actors.Count(a => a.Type == ActorType.Crew);
        var extractedCrew = combatState.Extraction.ExtractedCount;
        var deadCrew = combatState.Actors
            .Count(a => a.Type == ActorType.Crew && a.State == ActorState.Dead);
        
        if (deadCrew > 0)
        {
            objective.State = ObjectiveState.Failed;
        }
        else if (extractedCrew == totalCrew)
        {
            objective.State = ObjectiveState.Completed;
        }
        else
        {
            objective.State = ObjectiveState.InProgress;
        }
    }
    
    private void CheckKillTargetObjective(MissionObjective objective)
    {
        var target = combatState.Actors.FirstOrDefault(a => 
            a.Name == objective.TargetId || a.Id.ToString() == objective.TargetId);
        
        if (target == null || target.State == ActorState.Dead)
        {
            objective.State = ObjectiveState.Completed;
        }
        else
        {
            objective.State = ObjectiveState.InProgress;
        }
    }
    
    private void CheckEliminateAllObjective(MissionObjective objective)
    {
        var aliveEnemies = combatState.Actors
            .Count(a => a.Type == ActorType.Enemy && a.State == ActorState.Alive);
        
        if (aliveEnemies == 0)
        {
            objective.State = ObjectiveState.Completed;
        }
        else
        {
            objective.State = ObjectiveState.InProgress;
        }
    }
    
    private void CheckRetrieveLootObjective(MissionObjective objective)
    {
        var secured = combatState.Loot.CollectedLoot
            .Any(l => l.Id == objective.LootId);
        
        if (secured)
        {
            objective.State = ObjectiveState.Completed;
        }
        else
        {
            objective.State = ObjectiveState.InProgress;
        }
    }
    
    private void CheckHackTerminalObjective(MissionObjective objective)
    {
        var terminal = combatState.Interactions.GetInteractableById(objective.TargetId);
        if (terminal != null && terminal.State == InteractableStates.TerminalHacked)
        {
            objective.State = ObjectiveState.Completed;
        }
        else
        {
            objective.State = ObjectiveState.InProgress;
        }
    }
    
    private void OnActorExtracted(Actor actor)
    {
        // Re-check objectives on extraction
        Tick();
    }
    
    private void OnAllCrewExtracted()
    {
        // Mark survive objectives as complete
        foreach (var obj in objectives.Where(o => o.Type == ObjectiveType.Survive))
        {
            if (obj.State == ObjectiveState.InProgress)
            {
                obj.State = ObjectiveState.Completed;
                ObjectiveCompleted?.Invoke(obj);
            }
        }
        CheckAllPrimaryCompleted();
    }
    
    private void CheckAllPrimaryCompleted()
    {
        var allPrimaryComplete = PrimaryObjectives
            .All(o => o.State == ObjectiveState.Completed);
        
        if (allPrimaryComplete)
        {
            AllPrimaryCompleted?.Invoke();
        }
    }
    
    public bool ArePrimaryObjectivesComplete()
    {
        return PrimaryObjectives.All(o => o.State == ObjectiveState.Completed);
    }
    
    public bool AnyPrimaryObjectiveFailed()
    {
        return PrimaryObjectives.Any(o => o.State == ObjectiveState.Failed);
    }
}
```

**Acceptance Criteria**:
- [ ] Objectives can be defined
- [ ] Objectives track completion
- [ ] Primary vs secondary distinction
- [ ] Events fire on state changes

---

### Phase 4: Mission Outcome (Priority: High)

#### Step 4.1: Create MissionOutcome

**New File**: `src/sim/combat/data/MissionOutcome.cs`

```csharp
using System.Collections.Generic;

namespace FringeTactics;

public enum MissionResult
{
    TotalVictory,    // All objectives, all crew extracted
    Victory,         // Primary objectives, most crew
    PartialSuccess,  // Primary objectives, significant losses
    Retreat,         // Survived but failed primary
    Defeat           // All crew dead
}

public class MissionOutcome
{
    public MissionResult Result { get; set; }
    public string ResultDescription { get; set; }
    
    // Crew status
    public List<int> ExtractedCrewIds { get; set; } = new();
    public List<int> DeadCrewIds { get; set; } = new();
    public List<int> LeftBehindCrewIds { get; set; } = new();
    
    // Objectives
    public List<MissionObjective> CompletedObjectives { get; set; } = new();
    public List<MissionObjective> FailedObjectives { get; set; } = new();
    
    // Loot
    public List<LootData> CollectedLoot { get; set; } = new();
    public int TotalLootValue { get; set; }
    
    // Combat stats
    public CombatStats Stats { get; set; }
    
    // Timing
    public int TotalTicks { get; set; }
    public int WavesDefeated { get; set; }
    
    public static MissionOutcome Calculate(CombatState combat)
    {
        var outcome = new MissionOutcome();
        
        // Gather crew status
        foreach (var actor in combat.Actors)
        {
            if (actor.Type != ActorType.Crew) continue;
            
            if (combat.Extraction.ExtractedActorIds.Contains(actor.Id))
            {
                outcome.ExtractedCrewIds.Add(actor.Id);
            }
            else if (actor.State == ActorState.Dead)
            {
                outcome.DeadCrewIds.Add(actor.Id);
            }
            else
            {
                outcome.LeftBehindCrewIds.Add(actor.Id);
            }
        }
        
        // Gather objectives
        foreach (var obj in combat.Objectives.Objectives)
        {
            if (obj.State == ObjectiveState.Completed)
            {
                outcome.CompletedObjectives.Add(obj);
            }
            else if (obj.State == ObjectiveState.Failed)
            {
                outcome.FailedObjectives.Add(obj);
            }
        }
        
        // Gather loot
        outcome.CollectedLoot.AddRange(combat.Loot.CollectedLoot);
        outcome.TotalLootValue = combat.Loot.TotalValue;
        
        // Stats
        outcome.Stats = combat.Stats;
        outcome.TotalTicks = combat.TimeSystem.CurrentTick;
        outcome.WavesDefeated = combat.Waves.WavesSpawned;
        
        // Calculate result
        outcome.Result = DetermineResult(outcome, combat);
        outcome.ResultDescription = GetResultDescription(outcome.Result);
        
        return outcome;
    }
    
    private static MissionResult DetermineResult(MissionOutcome outcome, CombatState combat)
    {
        var primaryComplete = combat.Objectives.ArePrimaryObjectivesComplete();
        var anyPrimaryFailed = combat.Objectives.AnyPrimaryObjectiveFailed();
        var allCrewDead = outcome.ExtractedCrewIds.Count == 0 && 
                         outcome.LeftBehindCrewIds.Count == 0;
        var allCrewExtracted = outcome.DeadCrewIds.Count == 0 && 
                              outcome.LeftBehindCrewIds.Count == 0;
        var allSecondaryComplete = combat.Objectives.SecondaryObjectives
            .All(o => o.State == ObjectiveState.Completed);
        
        if (allCrewDead)
        {
            return MissionResult.Defeat;
        }
        
        if (!primaryComplete || anyPrimaryFailed)
        {
            return MissionResult.Retreat;
        }
        
        if (allCrewExtracted && allSecondaryComplete)
        {
            return MissionResult.TotalVictory;
        }
        
        if (outcome.DeadCrewIds.Count == 0)
        {
            return MissionResult.Victory;
        }
        
        return MissionResult.PartialSuccess;
    }
    
    private static string GetResultDescription(MissionResult result)
    {
        return result switch
        {
            MissionResult.TotalVictory => "Total Victory - All objectives complete, no casualties",
            MissionResult.Victory => "Victory - Mission successful",
            MissionResult.PartialSuccess => "Partial Success - Objectives complete with losses",
            MissionResult.Retreat => "Retreat - Survived but mission failed",
            MissionResult.Defeat => "Defeat - All crew lost",
            _ => "Unknown"
        };
    }
}
```

**Acceptance Criteria**:
- [ ] Outcome captures all mission data
- [ ] Result calculation is accurate
- [ ] Descriptions are clear

---

### Phase 5: UI Integration (Priority: High)

#### Step 5.1: Evac Zone Visualization

**File**: `src/scenes/mission/MissionView.cs`

```csharp
private void DrawEvacZone()
{
    if (!CombatState.Extraction.EvacAvailable) return;
    
    foreach (var tile in CombatState.Extraction.EvacZone)
    {
        var rect = new Rect2(
            tile.X * GridConstants.TileSize,
            tile.Y * GridConstants.TileSize,
            GridConstants.TileSize,
            GridConstants.TileSize);
        
        // Pulsing green for evac zone
        var alpha = 0.3f + 0.1f * Mathf.Sin(Time.GetTicksMsec() * 0.005f);
        DrawRect(rect, new Color(0.2f, 0.8f, 0.2f, alpha), true);
        DrawRect(rect, new Color(0.3f, 1f, 0.3f, 0.7f), false, 2f);
    }
}
```

#### Step 5.2: Objective Panel

**New File**: `src/scenes/mission/ObjectivePanel.cs`

```csharp
using Godot;
using System.Collections.Generic;

namespace FringeTactics;

public partial class ObjectivePanel : Control
{
    private VBoxContainer objectiveList;
    private Dictionary<string, Label> objectiveLabels = new();
    
    public override void _Ready()
    {
        CreateUI();
    }
    
    private void CreateUI()
    {
        var background = new ColorRect();
        background.Size = new Vector2(250, 200);
        background.Color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
        AddChild(background);
        
        var title = new Label();
        title.Text = "OBJECTIVES";
        title.Position = new Vector2(10, 5);
        title.AddThemeFontSizeOverride("font_size", 14);
        AddChild(title);
        
        objectiveList = new VBoxContainer();
        objectiveList.Position = new Vector2(10, 30);
        AddChild(objectiveList);
    }
    
    public void SetObjectives(IEnumerable<MissionObjective> objectives)
    {
        foreach (var child in objectiveList.GetChildren())
        {
            child.QueueFree();
        }
        objectiveLabels.Clear();
        
        foreach (var obj in objectives)
        {
            var label = new Label();
            label.AddThemeFontSizeOverride("font_size", 12);
            UpdateObjectiveLabel(label, obj);
            objectiveList.AddChild(label);
            objectiveLabels[obj.Id] = label;
        }
    }
    
    public void UpdateObjective(MissionObjective objective)
    {
        if (objectiveLabels.TryGetValue(objective.Id, out var label))
        {
            UpdateObjectiveLabel(label, objective);
        }
    }
    
    private void UpdateObjectiveLabel(Label label, MissionObjective objective)
    {
        var prefix = objective.IsPrimary ? "★" : "○";
        var status = objective.State switch
        {
            ObjectiveState.Completed => "✓",
            ObjectiveState.Failed => "✗",
            ObjectiveState.InProgress => "→",
            _ => " "
        };
        
        label.Text = $"{prefix} {status} {objective.Description}";
        
        label.AddThemeColorOverride("font_color", objective.State switch
        {
            ObjectiveState.Completed => Colors.Green,
            ObjectiveState.Failed => Colors.Red,
            ObjectiveState.InProgress => Colors.Yellow,
            _ => Colors.White
        });
    }
}
```

#### Step 5.3: Mission Summary Screen

**New File**: `src/scenes/mission/MissionSummary.cs`

```csharp
using Godot;

namespace FringeTactics;

public partial class MissionSummary : Control
{
    private MissionOutcome outcome;
    
    public void ShowOutcome(MissionOutcome outcome)
    {
        this.outcome = outcome;
        CreateUI();
        Visible = true;
    }
    
    private void CreateUI()
    {
        // Clear existing
        foreach (var child in GetChildren())
        {
            child.QueueFree();
        }
        
        // Background
        var bg = new ColorRect();
        bg.Size = GetViewportRect().Size;
        bg.Color = new Color(0, 0, 0, 0.8f);
        AddChild(bg);
        
        // Result header
        var header = new Label();
        header.Text = outcome.Result.ToString().ToUpper();
        header.Position = new Vector2(GetViewportRect().Size.X / 2 - 100, 50);
        header.AddThemeFontSizeOverride("font_size", 32);
        header.AddThemeColorOverride("font_color", GetResultColor(outcome.Result));
        AddChild(header);
        
        // Description
        var desc = new Label();
        desc.Text = outcome.ResultDescription;
        desc.Position = new Vector2(GetViewportRect().Size.X / 2 - 150, 100);
        AddChild(desc);
        
        // Crew status
        var crewY = 150;
        AddSection("CREW STATUS", crewY);
        AddLine($"Extracted: {outcome.ExtractedCrewIds.Count}", crewY + 25);
        AddLine($"KIA: {outcome.DeadCrewIds.Count}", crewY + 45);
        AddLine($"Left Behind: {outcome.LeftBehindCrewIds.Count}", crewY + 65);
        
        // Objectives
        var objY = 250;
        AddSection("OBJECTIVES", objY);
        AddLine($"Completed: {outcome.CompletedObjectives.Count}", objY + 25);
        AddLine($"Failed: {outcome.FailedObjectives.Count}", objY + 45);
        
        // Loot
        var lootY = 330;
        AddSection("LOOT", lootY);
        AddLine($"Items: {outcome.CollectedLoot.Count}", lootY + 25);
        AddLine($"Total Value: {outcome.TotalLootValue}", lootY + 45);
        
        // Continue button
        var continueBtn = new Button();
        continueBtn.Text = "CONTINUE";
        continueBtn.Position = new Vector2(GetViewportRect().Size.X / 2 - 50, 
                                          GetViewportRect().Size.Y - 100);
        continueBtn.Pressed += OnContinuePressed;
        AddChild(continueBtn);
    }
    
    private void AddSection(string title, float y)
    {
        var label = new Label();
        label.Text = title;
        label.Position = new Vector2(100, y);
        label.AddThemeFontSizeOverride("font_size", 16);
        AddChild(label);
    }
    
    private void AddLine(string text, float y)
    {
        var label = new Label();
        label.Text = text;
        label.Position = new Vector2(120, y);
        AddChild(label);
    }
    
    private Color GetResultColor(MissionResult result)
    {
        return result switch
        {
            MissionResult.TotalVictory => Colors.Gold,
            MissionResult.Victory => Colors.Green,
            MissionResult.PartialSuccess => Colors.Yellow,
            MissionResult.Retreat => Colors.Orange,
            MissionResult.Defeat => Colors.Red,
            _ => Colors.White
        };
    }
    
    private void OnContinuePressed()
    {
        // Return to campaign or menu
        GetTree().ChangeSceneToFile("res://scenes/main_menu/MainMenu.tscn");
    }
}
```

**Acceptance Criteria**:
- [ ] Evac zone is clearly visible
- [ ] Objectives panel shows status
- [ ] Mission summary shows all outcomes

---

### Phase 6: Retreat Command (Priority: High)

#### Step 6.1: Add Retreat Command

**File**: `src/scenes/mission/MissionView.cs`

```csharp
private void HandleRetreatCommand()
{
    // Order all crew to evac zone
    var evacCenter = GetEvacCenter();
    
    foreach (var actor in CombatState.Actors)
    {
        if (actor.Type != ActorType.Crew) continue;
        if (actor.State != ActorState.Alive) continue;
        if (CombatState.Extraction.ExtractedActorIds.Contains(actor.Id)) continue;
        
        // Find nearest evac tile
        var nearestEvac = FindNearestEvacTile(actor.GridPosition);
        actor.SetTarget(nearestEvac);
    }
    
    ShowFeedback("RETREAT! All units moving to extraction!");
}

private Vector2I FindNearestEvacTile(Vector2I from)
{
    var nearest = from;
    var nearestDist = float.MaxValue;
    
    foreach (var tile in CombatState.Extraction.EvacZone)
    {
        var dist = CombatResolver.GetDistance(from, tile);
        if (dist < nearestDist)
        {
            nearestDist = dist;
            nearest = tile;
        }
    }
    
    return nearest;
}

private void HandleExtractCommand()
{
    // Extract all selected units in evac zone
    foreach (var actor in selectedActors)
    {
        if (CombatState.Extraction.CanExtract(actor))
        {
            CombatState.Extraction.ExtractActor(actor);
        }
    }
}
```

**Acceptance Criteria**:
- [ ] Retreat command moves all crew to evac
- [ ] Extract command extracts units in zone
- [ ] Feedback is clear

---

## Testing Checklist

### Manual Testing

1. **Extraction**
   - [ ] Evac zone is visible when available
   - [ ] Units can extract in zone
   - [ ] Extracted units removed from combat
   - [ ] Partial extraction works

2. **Loot**
   - [ ] Can pick up loot
   - [ ] Carry loot slows movement
   - [ ] Extraction secures carried loot
   - [ ] Dropped loot can be re-picked

3. **Objectives**
   - [ ] Objectives display correctly
   - [ ] Objectives update on completion
   - [ ] Primary vs secondary distinction clear

4. **Mission Outcome**
   - [ ] Summary shows after mission
   - [ ] Result calculation is accurate
   - [ ] All stats displayed

5. **Retreat**
   - [ ] Retreat command works
   - [ ] All crew move to evac
   - [ ] Can extract after retreat

### Automated Tests

Create `tests/sim/combat/HH4Tests.cs`:

```csharp
[TestSuite]
public class HH4Tests
{
    // === Extraction ===
    [TestCase] ExtractionSystem_CanExtract_WhenInZone()
    [TestCase] ExtractionSystem_CannotExtract_WhenOutsideZone()
    [TestCase] ExtractionSystem_TracksExtractedActors()
    [TestCase] ExtractionSystem_EvacDelay_Works()
    
    // === Loot ===
    [TestCase] LootSystem_PickUp_CollectsLoot()
    [TestCase] LootSystem_Carry_AppliesSpeedPenalty()
    [TestCase] LootSystem_Extraction_SecuresCarriedLoot()
    [TestCase] LootSystem_Drop_CreatesInteractable()
    
    // === Objectives ===
    [TestCase] ObjectiveSystem_Survive_CompletesOnExtraction()
    [TestCase] ObjectiveSystem_KillTarget_CompletesOnDeath()
    [TestCase] ObjectiveSystem_RetrieveLoot_CompletesOnSecure()
    
    // === Outcome ===
    [TestCase] MissionOutcome_TotalVictory_AllObjectivesAllCrew()
    [TestCase] MissionOutcome_Defeat_AllCrewDead()
    [TestCase] MissionOutcome_Retreat_SurvivedButFailed()
}
```

---

## Implementation Order

1. **Day 1: Extraction System**
   - Step 1.1: Create ExtractionSystem
   - Step 1.2: Integrate into CombatState

2. **Day 2: Loot System**
   - Step 2.1: Add loot interactable type
   - Step 2.2: Create LootSystem

3. **Day 3: Objective System**
   - Step 3.1: Create ObjectiveSystem
   - Integrate into CombatState

4. **Day 4: Outcome & UI**
   - Step 4.1: Create MissionOutcome
   - Step 5.1-5.3: UI components

5. **Day 5: Commands & Testing**
   - Step 6.1: Retreat command
   - Write tests, manual testing

---

## Success Criteria for HH4

When HH4 is complete:

1. ✅ Units can extract from evac zone
2. ✅ Loot can be collected and carried
3. ✅ Objectives track completion
4. ✅ Mission outcomes are graduated
5. ✅ Summary screen shows tradeoffs
6. ✅ Retreat is a viable option
7. ✅ All automated tests pass

**Natural Pause Point**: Missions now have meaningful endings beyond "kill everyone" or "everyone dies". The "how greedy are you?" tension is now explicit.

---

## Files to Create/Modify

### New Files
- `src/sim/combat/systems/ExtractionSystem.cs`
- `src/sim/combat/systems/LootSystem.cs`
- `src/sim/combat/systems/ObjectiveSystem.cs`
- `src/sim/combat/data/MissionOutcome.cs`
- `src/scenes/mission/ObjectivePanel.cs`
- `src/scenes/mission/MissionSummary.cs`
- `tests/sim/combat/HH4Tests.cs`

### Modified Files
- `src/sim/combat/state/CombatState.cs` - Add systems
- `src/sim/combat/Interactable.cs` - Add loot type
- `src/sim/data/MissionConfig.cs` - Add evac/objective config
- `src/scenes/mission/MissionView.cs` - UI integration

---

## Dependencies

- **Requires**: HH3 (phases for evac timing)
- **Enables**: M7 (campaign integration uses outcomes)
