# HH3 – Wave Spawning & Mission Phases: Implementation Plan

This document breaks down **HH3** from `ROADMAP.md` into concrete implementation steps.

**Goal**: Create escalating pressure over time via reinforcement waves and explicit mission phases.

**Tactical Axes**: Time + Value Extraction

---

## Current State Assessment

### What We Have (From M0–HH2)

| Component | Status | Notes |
|-----------|--------|-------|
| `CombatState` | ✅ Complete | Tick-based simulation, actor management |
| `MissionConfig` | ✅ Complete | Mission setup data |
| `MissionFactory` | ✅ Complete | Creates combat state from config |
| `TimeSystem` | ✅ Complete | Tick tracking, pause/unpause |
| `PerceptionSystem` | ✅ Complete | Detection and alarm states |
| `MissionView` | ✅ Complete | UI, actor spawning |

### What HH3 Requires vs What We Have

| HH3 Requirement | Current Status | Gap |
|-----------------|----------------|-----|
| Spawn closets | ❌ Missing | Need spawn point definitions |
| Wave triggers | ❌ Missing | Need time/event-based triggers |
| Mission phases | ❌ Missing | Need phase state machine |
| Phase transitions | ❌ Missing | Need transition logic and events |
| Wave UI feedback | ❌ Missing | Need wave announcements |
| Deployment phase | ❌ Missing | Need pre-combat unit placement |

---

## Architecture Decisions

### Mission Phase Model

**Decision**: Explicit phase state machine with clear transitions.

```csharp
public enum MissionPhase
{
    Setup,       // Pre-combat deployment
    Negotiation, // Non-combat narrative (optional)
    Contact,     // Combat begins, initial enemies
    Pressure,    // Waves arrive
    Resolution   // Push to win or retreat
}
```

**Phase Transitions**:
| From | To | Trigger |
|------|-----|---------|
| Setup | Negotiation | Player confirms deployment |
| Setup | Contact | Skip negotiation (config) |
| Negotiation | Contact | Dialogue ends / timer / player action |
| Contact | Pressure | First wave timer / boss HP threshold |
| Pressure | Resolution | All waves spawned / objective complete |

### Wave System Model

**Decision**: Data-driven wave definitions with flexible triggers.

```csharp
public class WaveDefinition
{
    public string Id { get; set; }
    public List<EnemySpawn> Enemies { get; set; }
    public string SpawnPointId { get; set; }
    public WaveTrigger Trigger { get; set; }
}

public class WaveTrigger
{
    public WaveTriggerType Type { get; set; }
    public int DelayTicks { get; set; }        // For time-based
    public string EventId { get; set; }         // For event-based
    public float HpThreshold { get; set; }      // For HP-based
    public string TargetActorTag { get; set; }  // For HP-based
}

public enum WaveTriggerType
{
    Time,           // After X ticks from phase start
    Event,          // When specific event fires
    ActorHpBelow,   // When tagged actor HP drops below threshold
    WaveComplete,   // When previous wave is eliminated
    Manual          // Triggered by script/objective
}
```

### Spawn Point Model

**Decision**: Named spawn points on the map with properties.

```csharp
public class SpawnPoint
{
    public string Id { get; set; }
    public Vector2I Position { get; set; }
    public List<Vector2I> AdditionalPositions { get; set; } // For multi-unit spawns
    public bool RequiresLOS { get; set; } = false;  // Don't spawn if player can see
    public string DoorId { get; set; }              // Optional door to open on spawn
}
```

---

## Implementation Steps

### Phase 1: Mission Phase System (Priority: Critical)

#### Step 1.1: Create MissionPhase Enum and PhaseSystem

**New File**: `src/sim/combat/systems/PhaseSystem.cs`

```csharp
using System;
using System.Collections.Generic;

namespace FringeTactics;

public enum MissionPhase
{
    Setup,
    Negotiation,
    Contact,
    Pressure,
    Resolution,
    Complete
}

/// <summary>
/// Manages mission phase transitions.
/// </summary>
public class PhaseSystem
{
    private readonly CombatState combatState;
    
    public MissionPhase CurrentPhase { get; private set; } = MissionPhase.Setup;
    public int PhaseStartTick { get; private set; } = 0;
    public int TicksInPhase => combatState.TimeSystem.CurrentTick - PhaseStartTick;
    
    public event Action<MissionPhase, MissionPhase> PhaseChanged; // old, new
    
    public PhaseSystem(CombatState combatState)
    {
        this.combatState = combatState;
    }
    
    public void TransitionTo(MissionPhase newPhase)
    {
        if (CurrentPhase == newPhase) return;
        
        var oldPhase = CurrentPhase;
        CurrentPhase = newPhase;
        PhaseStartTick = combatState.TimeSystem.CurrentTick;
        
        SimLog.Log($"[Phase] Transition: {oldPhase} → {newPhase}");
        PhaseChanged?.Invoke(oldPhase, newPhase);
    }
    
    /// <summary>
    /// Start the mission (transition from Setup).
    /// </summary>
    public void StartMission(bool hasNegotiation = false)
    {
        if (CurrentPhase != MissionPhase.Setup) return;
        
        if (hasNegotiation)
        {
            TransitionTo(MissionPhase.Negotiation);
        }
        else
        {
            TransitionTo(MissionPhase.Contact);
        }
    }
    
    /// <summary>
    /// End negotiation and start combat.
    /// </summary>
    public void EndNegotiation()
    {
        if (CurrentPhase != MissionPhase.Negotiation) return;
        TransitionTo(MissionPhase.Contact);
    }
    
    /// <summary>
    /// Escalate to pressure phase.
    /// </summary>
    public void Escalate()
    {
        if (CurrentPhase != MissionPhase.Contact) return;
        TransitionTo(MissionPhase.Pressure);
    }
    
    /// <summary>
    /// Enter resolution phase.
    /// </summary>
    public void EnterResolution()
    {
        if (CurrentPhase != MissionPhase.Pressure) return;
        TransitionTo(MissionPhase.Resolution);
    }
    
    /// <summary>
    /// Complete the mission.
    /// </summary>
    public void Complete()
    {
        TransitionTo(MissionPhase.Complete);
    }
}
```

**Acceptance Criteria**:
- [ ] `MissionPhase` enum exists with all phases
- [ ] `PhaseSystem` tracks current phase
- [ ] Phase transitions fire events
- [ ] `TicksInPhase` tracks time in current phase

#### Step 1.2: Integrate PhaseSystem into CombatState

**File**: `src/sim/combat/state/CombatState.cs`

```csharp
public PhaseSystem Phases { get; private set; }

public CombatState(int seed)
{
    // ... existing code ...
    Phases = new PhaseSystem(this);
}
```

**Update `ProcessTick()`** to check phase-based logic:
```csharp
private void ProcessTick()
{
    // ... existing code ...
    
    // Check phase-based triggers
    CheckPhaseTriggers();
}

private void CheckPhaseTriggers()
{
    // Auto-escalate from Contact to Pressure after time
    if (Phases.CurrentPhase == MissionPhase.Contact)
    {
        var escalationTicks = Config?.EscalationDelayTicks ?? 600; // 30 seconds default
        if (Phases.TicksInPhase >= escalationTicks)
        {
            Phases.Escalate();
        }
    }
}
```

**Acceptance Criteria**:
- [ ] `CombatState.Phases` property exists
- [ ] Phase triggers checked each tick
- [ ] Auto-escalation works

---

### Phase 2: Wave System (Priority: Critical)

#### Step 2.1: Create Wave Data Structures

**New File**: `src/sim/combat/data/WaveData.cs`

```csharp
using Godot;
using System.Collections.Generic;

namespace FringeTactics;

public enum WaveTriggerType
{
    Time,
    Event,
    ActorHpBelow,
    WaveComplete,
    PhaseStart,
    Manual
}

public class WaveTrigger
{
    public WaveTriggerType Type { get; set; } = WaveTriggerType.Time;
    public int DelayTicks { get; set; } = 0;
    public string EventId { get; set; }
    public float HpThreshold { get; set; } = 0.5f;
    public string TargetActorTag { get; set; }
    public string PreviousWaveId { get; set; }
    public MissionPhase RequiredPhase { get; set; } = MissionPhase.Pressure;
}

public class WaveDefinition
{
    public string Id { get; set; }
    public string Name { get; set; }
    public List<EnemySpawn> Enemies { get; set; } = new();
    public string SpawnPointId { get; set; }
    public WaveTrigger Trigger { get; set; } = new();
    public bool Announced { get; set; } = true;  // Show wave announcement
}

public class SpawnPoint
{
    public string Id { get; set; }
    public Vector2I Position { get; set; }
    public List<Vector2I> AdditionalPositions { get; set; } = new();
    public bool BlockedByLOS { get; set; } = true;  // Don't spawn if player sees
    public string DoorId { get; set; }              // Door to open on spawn
    public SpawnDirection FacingDirection { get; set; } = SpawnDirection.Toward_Center;
}

public enum SpawnDirection
{
    Toward_Center,
    Toward_Player,
    Fixed_North,
    Fixed_South,
    Fixed_East,
    Fixed_West
}
```

**Acceptance Criteria**:
- [ ] Wave data structures defined
- [ ] Trigger types cover all use cases
- [ ] Spawn points have necessary properties

#### Step 2.2: Create WaveSystem

**New File**: `src/sim/combat/systems/WaveSystem.cs`

```csharp
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FringeTactics;

/// <summary>
/// Manages wave spawning and tracking.
/// </summary>
public class WaveSystem
{
    private readonly CombatState combatState;
    private readonly List<WaveDefinition> waves = new();
    private readonly Dictionary<string, SpawnPoint> spawnPoints = new();
    private readonly HashSet<string> spawnedWaves = new();
    private readonly Dictionary<string, List<int>> waveActors = new(); // waveId → actorIds
    
    public int WavesSpawned => spawnedWaves.Count;
    public int TotalWaves => waves.Count;
    
    public event Action<WaveDefinition> WaveTriggered;
    public event Action<WaveDefinition, List<Actor>> WaveSpawned;
    public event Action<string> WaveEliminated; // waveId
    
    public WaveSystem(CombatState combatState)
    {
        this.combatState = combatState;
    }
    
    public void AddWave(WaveDefinition wave)
    {
        waves.Add(wave);
    }
    
    public void AddSpawnPoint(SpawnPoint point)
    {
        spawnPoints[point.Id] = point;
    }
    
    public void Tick()
    {
        CheckWaveTriggers();
        CheckWaveElimination();
    }
    
    private void CheckWaveTriggers()
    {
        foreach (var wave in waves)
        {
            if (spawnedWaves.Contains(wave.Id)) continue;
            if (!ShouldTrigger(wave)) continue;
            
            TriggerWave(wave);
        }
    }
    
    private bool ShouldTrigger(WaveDefinition wave)
    {
        var trigger = wave.Trigger;
        
        // Check required phase
        if (combatState.Phases.CurrentPhase != trigger.RequiredPhase &&
            trigger.RequiredPhase != MissionPhase.Setup) // Setup means "any phase"
        {
            return false;
        }
        
        switch (trigger.Type)
        {
            case WaveTriggerType.Time:
                return combatState.Phases.TicksInPhase >= trigger.DelayTicks;
                
            case WaveTriggerType.PhaseStart:
                return combatState.Phases.TicksInPhase == 0;
                
            case WaveTriggerType.WaveComplete:
                return IsWaveEliminated(trigger.PreviousWaveId);
                
            case WaveTriggerType.ActorHpBelow:
                return CheckHpTrigger(trigger);
                
            case WaveTriggerType.Manual:
                return false; // Only triggered externally
                
            default:
                return false;
        }
    }
    
    private bool CheckHpTrigger(WaveTrigger trigger)
    {
        foreach (var actor in combatState.Actors)
        {
            if (actor.State != ActorState.Alive) continue;
            if (actor.Name != trigger.TargetActorTag) continue;
            
            var hpPercent = actor.Hp / (float)actor.MaxHp;
            if (hpPercent <= trigger.HpThreshold)
            {
                return true;
            }
        }
        return false;
    }
    
    private void TriggerWave(WaveDefinition wave)
    {
        SimLog.Log($"[Wave] Triggering wave: {wave.Name ?? wave.Id}");
        spawnedWaves.Add(wave.Id);
        WaveTriggered?.Invoke(wave);
        
        // Get spawn point
        if (!spawnPoints.TryGetValue(wave.SpawnPointId, out var spawnPoint))
        {
            SimLog.Log($"[Wave] ERROR: Spawn point '{wave.SpawnPointId}' not found!");
            return;
        }
        
        // Check LOS blocking
        if (spawnPoint.BlockedByLOS && IsSpawnPointVisible(spawnPoint))
        {
            SimLog.Log($"[Wave] Spawn point '{spawnPoint.Id}' is visible - delaying spawn");
            spawnedWaves.Remove(wave.Id); // Allow retry next tick
            return;
        }
        
        // Open door if specified
        if (!string.IsNullOrEmpty(spawnPoint.DoorId))
        {
            OpenSpawnDoor(spawnPoint.DoorId);
        }
        
        // Spawn enemies
        var spawnedActors = SpawnWaveEnemies(wave, spawnPoint);
        waveActors[wave.Id] = spawnedActors.Select(a => a.Id).ToList();
        
        WaveSpawned?.Invoke(wave, spawnedActors);
    }
    
    private bool IsSpawnPointVisible(SpawnPoint point)
    {
        // Check if any crew can see the spawn point
        foreach (var actor in combatState.Actors)
        {
            if (actor.Type != ActorType.Crew || actor.State != ActorState.Alive) continue;
            
            if (CombatResolver.HasLineOfSight(actor.GridPosition, point.Position, combatState.MapState))
            {
                return true;
            }
        }
        return false;
    }
    
    private void OpenSpawnDoor(string doorId)
    {
        var door = combatState.Interactions.GetInteractableById(doorId);
        if (door != null && door.Type == InteractableTypes.Door)
        {
            combatState.Interactions.SetDoorState(door.Id, InteractableState.DoorOpen);
            SimLog.Log($"[Wave] Opened spawn door: {doorId}");
        }
    }
    
    private List<Actor> SpawnWaveEnemies(WaveDefinition wave, SpawnPoint point)
    {
        var spawned = new List<Actor>();
        var positions = new List<Vector2I> { point.Position };
        positions.AddRange(point.AdditionalPositions);
        
        int posIndex = 0;
        foreach (var enemySpawn in wave.Enemies)
        {
            var pos = positions[posIndex % positions.Count];
            
            // Find nearby empty position if occupied
            pos = FindNearbyEmptyPosition(pos);
            
            var actor = combatState.AddActor(ActorType.Enemy, pos);
            actor.Name = enemySpawn.EnemyType;
            ApplyEnemyTemplate(actor, enemySpawn.EnemyType);
            
            // Alert the spawned enemy immediately
            combatState.Perception.AlertEnemy(actor.Id);
            
            spawned.Add(actor);
            posIndex++;
        }
        
        SimLog.Log($"[Wave] Spawned {spawned.Count} enemies for wave '{wave.Id}'");
        return spawned;
    }
    
    private Vector2I FindNearbyEmptyPosition(Vector2I preferred)
    {
        if (combatState.GetActorAtPosition(preferred) == null &&
            combatState.MapState.IsWalkable(preferred))
        {
            return preferred;
        }
        
        // Search in expanding rings
        for (int radius = 1; radius <= 3; radius++)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    var pos = preferred + new Vector2I(dx, dy);
                    if (combatState.GetActorAtPosition(pos) == null &&
                        combatState.MapState.IsWalkable(pos))
                    {
                        return pos;
                    }
                }
            }
        }
        
        return preferred; // Fallback
    }
    
    private void ApplyEnemyTemplate(Actor actor, string enemyType)
    {
        // Apply stats based on enemy type
        // This should use a data-driven system in the future
        switch (enemyType.ToLower())
        {
            case "grunt":
                actor.MaxHp = 80;
                actor.Hp = 80;
                break;
            case "heavy":
                actor.MaxHp = 150;
                actor.Hp = 150;
                actor.Armor = 2;
                break;
            case "flanker":
                actor.MaxHp = 60;
                actor.Hp = 60;
                actor.Stats["reflexes"] = 10;
                break;
            default:
                // Use defaults
                break;
        }
    }
    
    private void CheckWaveElimination()
    {
        foreach (var kvp in waveActors.ToList())
        {
            var waveId = kvp.Key;
            var actorIds = kvp.Value;
            
            var allDead = actorIds.All(id =>
            {
                var actor = combatState.GetActorById(id);
                return actor == null || actor.State == ActorState.Dead;
            });
            
            if (allDead)
            {
                SimLog.Log($"[Wave] Wave '{waveId}' eliminated!");
                WaveEliminated?.Invoke(waveId);
                waveActors.Remove(waveId);
            }
        }
    }
    
    public bool IsWaveEliminated(string waveId)
    {
        if (!spawnedWaves.Contains(waveId)) return false;
        return !waveActors.ContainsKey(waveId);
    }
    
    /// <summary>
    /// Manually trigger a wave by ID.
    /// </summary>
    public void TriggerWaveManually(string waveId)
    {
        var wave = waves.FirstOrDefault(w => w.Id == waveId);
        if (wave != null && !spawnedWaves.Contains(waveId))
        {
            TriggerWave(wave);
        }
    }
    
    /// <summary>
    /// Check if all waves have been spawned.
    /// </summary>
    public bool AllWavesSpawned => spawnedWaves.Count >= waves.Count;
}
```

**Acceptance Criteria**:
- [ ] Waves trigger based on conditions
- [ ] Enemies spawn at spawn points
- [ ] LOS blocking prevents visible spawns
- [ ] Wave elimination tracking works
- [ ] Events fire for UI feedback

#### Step 2.3: Integrate WaveSystem into CombatState

**File**: `src/sim/combat/state/CombatState.cs`

```csharp
public WaveSystem Waves { get; private set; }

public CombatState(int seed)
{
    // ... existing code ...
    Waves = new WaveSystem(this);
}

private void ProcessTick()
{
    // ... existing code ...
    
    // Process waves
    Waves.Tick();
}
```

**Acceptance Criteria**:
- [ ] `CombatState.Waves` property exists
- [ ] Wave system ticks each frame

---

### Phase 3: MissionConfig Extensions (Priority: High)

#### Step 3.1: Extend MissionConfig for Phases and Waves

**File**: `src/sim/data/MissionConfig.cs`

```csharp
public class MissionConfig
{
    // ... existing properties ...
    
    // Phase configuration
    public bool HasNegotiationPhase { get; set; } = false;
    public int EscalationDelayTicks { get; set; } = 600; // 30 seconds
    public int ResolutionDelayTicks { get; set; } = 1200; // 60 seconds after last wave
    
    // Spawn points
    public List<SpawnPoint> SpawnPoints { get; set; } = new();
    
    // Waves
    public List<WaveDefinition> Waves { get; set; } = new();
    
    // Deployment zone
    public List<Vector2I> DeploymentZone { get; set; } = new();
}
```

#### Step 3.2: Update MissionFactory

**File**: `src/sim/combat/MissionFactory.cs`

```csharp
public static CombatState BuildFromConfig(MissionConfig config)
{
    var combat = new CombatState(config.Seed);
    combat.Config = config;
    
    // ... existing map and actor setup ...
    
    // Add spawn points
    foreach (var spawnPoint in config.SpawnPoints)
    {
        combat.Waves.AddSpawnPoint(spawnPoint);
    }
    
    // Add waves
    foreach (var wave in config.Waves)
    {
        combat.Waves.AddWave(wave);
    }
    
    // Initialize systems
    combat.InitializePerception();
    
    return combat;
}
```

**Acceptance Criteria**:
- [ ] MissionConfig includes phase and wave data
- [ ] MissionFactory sets up waves and spawn points

---

### Phase 4: Deployment Phase (Priority: High)

#### Step 4.1: Create DeploymentSystem

**New File**: `src/sim/combat/systems/DeploymentSystem.cs`

```csharp
using Godot;
using System;
using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// Manages pre-combat unit deployment.
/// </summary>
public class DeploymentSystem
{
    private readonly CombatState combatState;
    private readonly HashSet<Vector2I> deploymentZone = new();
    
    public bool IsDeploying => combatState.Phases.CurrentPhase == MissionPhase.Setup;
    public IReadOnlySet<Vector2I> DeploymentZone => deploymentZone;
    
    public event Action<Actor, Vector2I> UnitDeployed;
    public event Action DeploymentConfirmed;
    
    public DeploymentSystem(CombatState combatState)
    {
        this.combatState = combatState;
    }
    
    public void SetDeploymentZone(IEnumerable<Vector2I> tiles)
    {
        deploymentZone.Clear();
        foreach (var tile in tiles)
        {
            deploymentZone.Add(tile);
        }
    }
    
    public bool IsInDeploymentZone(Vector2I position)
    {
        return deploymentZone.Contains(position);
    }
    
    public bool CanDeployAt(Vector2I position)
    {
        if (!IsDeploying) return false;
        if (!IsInDeploymentZone(position)) return false;
        if (!combatState.MapState.IsWalkable(position)) return false;
        if (combatState.GetActorAtPosition(position) != null) return false;
        return true;
    }
    
    public bool DeployUnit(Actor actor, Vector2I position)
    {
        if (!CanDeployAt(position)) return false;
        
        actor.GridPosition = position;
        actor.VisualPosition = new Vector2(
            position.X * GridConstants.TileSize,
            position.Y * GridConstants.TileSize);
        
        UnitDeployed?.Invoke(actor, position);
        SimLog.Log($"[Deployment] {actor.Type}#{actor.Id} deployed to {position}");
        return true;
    }
    
    public void ConfirmDeployment()
    {
        if (!IsDeploying) return;
        
        DeploymentConfirmed?.Invoke();
        combatState.Phases.StartMission(combatState.Config?.HasNegotiationPhase ?? false);
    }
}
```

**Acceptance Criteria**:
- [ ] Deployment zone defined
- [ ] Units can be placed in zone
- [ ] Confirmation starts mission

---

### Phase 5: UI Integration (Priority: High)

#### Step 5.1: Phase UI Widget

**New File**: `src/scenes/mission/PhaseWidget.cs`

```csharp
using Godot;

namespace FringeTactics;

public partial class PhaseWidget : Control
{
    private Label phaseLabel;
    private Label timerLabel;
    private ColorRect background;
    
    public override void _Ready()
    {
        CreateUI();
    }
    
    private void CreateUI()
    {
        background = new ColorRect();
        background.Size = new Vector2(150, 50);
        background.Color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
        AddChild(background);
        
        phaseLabel = new Label();
        phaseLabel.Position = new Vector2(10, 5);
        phaseLabel.AddThemeFontSizeOverride("font_size", 16);
        AddChild(phaseLabel);
        
        timerLabel = new Label();
        timerLabel.Position = new Vector2(10, 28);
        timerLabel.AddThemeFontSizeOverride("font_size", 12);
        AddChild(timerLabel);
    }
    
    public void UpdateDisplay(MissionPhase phase, int ticksInPhase)
    {
        var phaseName = phase switch
        {
            MissionPhase.Setup => "⚙️ DEPLOYMENT",
            MissionPhase.Negotiation => "🤝 NEGOTIATION",
            MissionPhase.Contact => "⚔️ CONTACT",
            MissionPhase.Pressure => "🔥 PRESSURE",
            MissionPhase.Resolution => "🏁 RESOLUTION",
            MissionPhase.Complete => "✅ COMPLETE",
            _ => phase.ToString()
        };
        
        phaseLabel.Text = phaseName;
        
        var seconds = ticksInPhase / 20; // Assuming 20 ticks/sec
        timerLabel.Text = $"Time: {seconds}s";
        
        // Color based on phase
        background.Color = phase switch
        {
            MissionPhase.Setup => new Color(0.2f, 0.2f, 0.3f, 0.8f),
            MissionPhase.Negotiation => new Color(0.2f, 0.3f, 0.2f, 0.8f),
            MissionPhase.Contact => new Color(0.3f, 0.2f, 0.1f, 0.8f),
            MissionPhase.Pressure => new Color(0.4f, 0.1f, 0.1f, 0.8f),
            MissionPhase.Resolution => new Color(0.3f, 0.3f, 0.1f, 0.8f),
            _ => new Color(0.1f, 0.1f, 0.1f, 0.8f)
        };
    }
}
```

#### Step 5.2: Wave Announcement

**File**: `src/scenes/mission/MissionView.cs`

```csharp
private void OnWaveTriggered(WaveDefinition wave)
{
    if (wave.Announced)
    {
        ShowWaveAnnouncement(wave);
    }
    
    // Auto-pause on wave
    CombatState.TimeSystem.Pause();
}

private void ShowWaveAnnouncement(WaveDefinition wave)
{
    var announcement = new Label();
    announcement.Text = $"⚠️ REINFORCEMENTS: {wave.Name ?? "Wave " + CombatState.Waves.WavesSpawned}";
    announcement.AddThemeFontSizeOverride("font_size", 24);
    announcement.AddThemeColorOverride("font_color", Colors.Red);
    announcement.Position = new Vector2(GetViewportRect().Size.X / 2 - 150, 100);
    uiLayer.AddChild(announcement);
    
    // Fade out after 3 seconds
    var tween = CreateTween();
    tween.TweenProperty(announcement, "modulate:a", 0f, 1f).SetDelay(2f);
    tween.TweenCallback(Callable.From(() => announcement.QueueFree()));
}
```

#### Step 5.3: Deployment Zone Visualization

**File**: `src/scenes/mission/MissionView.cs`

```csharp
private void DrawDeploymentZone()
{
    if (CombatState.Phases.CurrentPhase != MissionPhase.Setup) return;
    
    foreach (var tile in CombatState.Deployment.DeploymentZone)
    {
        var rect = new Rect2(
            tile.X * GridConstants.TileSize,
            tile.Y * GridConstants.TileSize,
            GridConstants.TileSize,
            GridConstants.TileSize);
        
        DrawRect(rect, new Color(0.2f, 0.5f, 0.2f, 0.3f), true);
        DrawRect(rect, new Color(0.3f, 0.7f, 0.3f, 0.5f), false, 2f);
    }
}
```

**Acceptance Criteria**:
- [ ] Phase widget shows current phase
- [ ] Wave announcements appear
- [ ] Deployment zone is visualized

---

### Phase 6: Hangar Handover Test Mission (Priority: High)

#### Step 6.1: Create Hangar Handover Config

**File**: `src/sim/data/MissionConfig.cs`

```csharp
public static MissionConfig CreateHangarHandoverMission()
{
    return new MissionConfig
    {
        Id = "hangar_handover",
        Name = "Hangar Handover",
        MapTemplate = new string[]
        {
            "########################",
            "#EE....................#",
            "#EE....................#",
            "#....####....####......#",
            "#....#..#....#..#......#",
            "#....#..D....D..#......#",
            "#....####....####......#",
            "#......................#",
            "#.........XX...........#",
            "#.........XX...........#",
            "#......................#",
            "#....####....####......#",
            "#....#..D....D..#...S1.#",
            "#....#..#....#..#......#",
            "#....####....####......#",
            "#......................#",
            "#......................#",
            "#..S2..................#",
            "#......................#",
            "#......................#",
            "#.........BB...........#",
            "#.........BB...........#",
            "#......................#",
            "########################"
        },
        
        // Deployment zone (top-left area)
        DeploymentZone = GenerateDeploymentZone(1, 1, 4, 4),
        
        // Spawn points
        SpawnPoints = new List<SpawnPoint>
        {
            new SpawnPoint
            {
                Id = "spawn_east",
                Position = new Vector2I(20, 12),
                AdditionalPositions = new List<Vector2I>
                {
                    new Vector2I(20, 13),
                    new Vector2I(20, 14)
                },
                BlockedByLOS = true
            },
            new SpawnPoint
            {
                Id = "spawn_south",
                Position = new Vector2I(3, 17),
                AdditionalPositions = new List<Vector2I>
                {
                    new Vector2I(4, 17),
                    new Vector2I(5, 17)
                },
                BlockedByLOS = true
            }
        },
        
        // Initial enemies (boss and guards)
        EnemySpawns = new List<EnemySpawn>
        {
            new EnemySpawn("boss", new Vector2I(10, 20)) { Tag = "boss" },
            new EnemySpawn("guard", new Vector2I(9, 20)),
            new EnemySpawn("guard", new Vector2I(11, 20)),
            new EnemySpawn("guard", new Vector2I(10, 19))
        },
        
        // Waves
        Waves = new List<WaveDefinition>
        {
            new WaveDefinition
            {
                Id = "wave_1",
                Name = "Flankers",
                SpawnPointId = "spawn_east",
                Trigger = new WaveTrigger
                {
                    Type = WaveTriggerType.Time,
                    DelayTicks = 400, // 20 seconds into Pressure
                    RequiredPhase = MissionPhase.Pressure
                },
                Enemies = new List<EnemySpawn>
                {
                    new EnemySpawn("flanker", Vector2I.Zero),
                    new EnemySpawn("flanker", Vector2I.Zero)
                }
            },
            new WaveDefinition
            {
                Id = "wave_2",
                Name = "Reinforcements",
                SpawnPointId = "spawn_south",
                Trigger = new WaveTrigger
                {
                    Type = WaveTriggerType.ActorHpBelow,
                    HpThreshold = 0.5f,
                    TargetActorTag = "boss",
                    RequiredPhase = MissionPhase.Pressure
                },
                Enemies = new List<EnemySpawn>
                {
                    new EnemySpawn("grunt", Vector2I.Zero),
                    new EnemySpawn("grunt", Vector2I.Zero),
                    new EnemySpawn("heavy", Vector2I.Zero)
                }
            },
            new WaveDefinition
            {
                Id = "wave_3",
                Name = "Final Push",
                SpawnPointId = "spawn_east",
                Trigger = new WaveTrigger
                {
                    Type = WaveTriggerType.WaveComplete,
                    PreviousWaveId = "wave_2",
                    RequiredPhase = MissionPhase.Pressure
                },
                Enemies = new List<EnemySpawn>
                {
                    new EnemySpawn("grunt", Vector2I.Zero),
                    new EnemySpawn("grunt", Vector2I.Zero)
                }
            }
        },
        
        // Phase timing
        HasNegotiationPhase = true,
        EscalationDelayTicks = 200, // 10 seconds after contact
    };
}

private static List<Vector2I> GenerateDeploymentZone(int x, int y, int width, int height)
{
    var zone = new List<Vector2I>();
    for (int dx = 0; dx < width; dx++)
    {
        for (int dy = 0; dy < height; dy++)
        {
            zone.Add(new Vector2I(x + dx, y + dy));
        }
    }
    return zone;
}
```

**Acceptance Criteria**:
- [ ] Hangar map with cover and spawn points
- [ ] Boss and initial guards
- [ ] Three waves with different triggers
- [ ] Deployment zone defined

---

## Testing Checklist

### Manual Testing

1. **Deployment Phase**
   - [ ] Deployment zone is visible
   - [ ] Can place units in zone
   - [ ] Cannot place outside zone
   - [ ] Confirm button starts mission

2. **Phase Transitions**
   - [ ] Setup → Contact (or Negotiation)
   - [ ] Contact → Pressure (after timer)
   - [ ] Pressure → Resolution (after waves)
   - [ ] Phase widget updates correctly

3. **Wave Spawning**
   - [ ] Time-based wave triggers
   - [ ] HP-based wave triggers (boss at 50%)
   - [ ] Wave-complete triggers
   - [ ] Enemies spawn at spawn points
   - [ ] LOS blocking works

4. **Wave Feedback**
   - [ ] Wave announcement appears
   - [ ] Game pauses on wave
   - [ ] Spawned enemies are alerted

5. **Hangar Handover**
   - [ ] Full mission playable
   - [ ] All waves trigger correctly
   - [ ] Phases progress naturally

### Automated Tests

Create `tests/sim/combat/HH3Tests.cs`:

```csharp
[TestSuite]
public class HH3Tests
{
    // === Phase System ===
    [TestCase] PhaseSystem_StartsInSetup()
    [TestCase] PhaseSystem_TransitionsCorrectly()
    [TestCase] PhaseSystem_TracksTicksInPhase()
    
    // === Wave System ===
    [TestCase] WaveSystem_TimeTriggeredWave_Spawns()
    [TestCase] WaveSystem_HpTriggeredWave_Spawns()
    [TestCase] WaveSystem_WaveCompleteTriggeredWave_Spawns()
    [TestCase] WaveSystem_LOSBlocking_DelaysSpawn()
    [TestCase] WaveSystem_TracksWaveElimination()
    
    // === Deployment ===
    [TestCase] DeploymentSystem_AllowsPlacementInZone()
    [TestCase] DeploymentSystem_BlocksPlacementOutsideZone()
    [TestCase] DeploymentSystem_ConfirmStartsMission()
    
    // === Integration ===
    [TestCase] HangarHandover_AllWavesSpawn()
    [TestCase] HangarHandover_BossHpTriggersWave()
}
```

---

## Implementation Order

1. **Day 1: Phase System**
   - Step 1.1: Create PhaseSystem
   - Step 1.2: Integrate into CombatState

2. **Day 2: Wave System**
   - Step 2.1: Create wave data structures
   - Step 2.2: Create WaveSystem
   - Step 2.3: Integrate into CombatState

3. **Day 3: Config & Factory**
   - Step 3.1: Extend MissionConfig
   - Step 3.2: Update MissionFactory

4. **Day 4: Deployment & UI**
   - Step 4.1: Create DeploymentSystem
   - Step 5.1-5.3: UI widgets and visualization

5. **Day 5: Test Mission & Testing**
   - Step 6.1: Create Hangar Handover config
   - Write tests, manual testing

---

## Success Criteria for HH3

When HH3 is complete:

1. ✅ Missions have explicit phases
2. ✅ Deployment phase allows unit placement
3. ✅ Waves spawn based on triggers
4. ✅ Phase and wave UI is clear
5. ✅ Hangar Handover mission is playable
6. ✅ All automated tests pass

**Natural Pause Point**: Missions now have temporal structure. Staying longer = more risk. This is the primary pressure source in the Hangar Handover.

---

## Files to Create/Modify

### New Files
- `src/sim/combat/systems/PhaseSystem.cs`
- `src/sim/combat/systems/WaveSystem.cs`
- `src/sim/combat/systems/DeploymentSystem.cs`
- `src/sim/combat/data/WaveData.cs`
- `src/scenes/mission/PhaseWidget.cs`
- `tests/sim/combat/HH3Tests.cs`

### Modified Files
- `src/sim/combat/state/CombatState.cs` - Add systems
- `src/sim/data/MissionConfig.cs` - Add phase/wave config
- `src/sim/combat/MissionFactory.cs` - Setup waves
- `src/scenes/mission/MissionView.cs` - UI integration

---

## Dependencies

- **Requires**: M6 (perception for spawn LOS checks)
- **Enables**: HH4 (retreat timing), HH5 (AI phase-based behavior)
