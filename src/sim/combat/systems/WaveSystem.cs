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
    public bool AllWavesSpawned => spawnedWaves.Count >= waves.Count;
    
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
        
        // Check required phase (Setup means "any phase")
        if (trigger.RequiredPhase != TacticalPhase.Setup &&
            combatState.Phases.CurrentPhase != trigger.RequiredPhase)
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
            if (actor.Tag != trigger.TargetActorTag) continue;
            
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
        var door = combatState.Interactions.GetInteractableByStringId(doorId);
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
            actor.Name = enemySpawn.EnemyId;
            actor.Tag = enemySpawn.Tag;
            ApplyEnemyTemplate(actor, enemySpawn.EnemyId);
            
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
    
    private void ApplyEnemyTemplate(Actor actor, string enemyId)
    {
        // Try to get enemy definition from registry
        var enemyDef = Definitions.Enemies.Get(enemyId);
        if (enemyDef != null)
        {
            actor.MaxHp = enemyDef.Hp;
            actor.Hp = enemyDef.Hp;
            actor.Armor = enemyDef.Armor;
            actor.EquippedWeapon = WeaponData.FromId(enemyDef.WeaponId);
            return;
        }
        
        // Fallback for unknown enemy types
        switch (enemyId.ToLower())
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
    /// Get a spawn point by ID.
    /// </summary>
    public SpawnPoint GetSpawnPoint(string id)
    {
        return spawnPoints.TryGetValue(id, out var point) ? point : null;
    }
    
    /// <summary>
    /// Get all registered waves.
    /// </summary>
    public IReadOnlyList<WaveDefinition> GetWaves() => waves;
}
