using Godot;
using System;
using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// Global alarm state for the mission.
/// </summary>
public enum AlarmState
{
    Quiet,
    Alerted
}

/// <summary>
/// Manages enemy perception and the global alarm state.
/// </summary>
public class PerceptionSystem
{
    private readonly CombatState combatState;
    private readonly Dictionary<int, EnemyPerception> perceptions = new();
    private bool isInitialized = false;
    
    public AlarmState AlarmState { get; private set; } = AlarmState.Quiet;
    
    public event Action<AlarmState, AlarmState> AlarmStateChanged;
    public event Action<Actor, Actor> EnemyDetectedCrew;
    public event Action<Actor> EnemyBecameAlerted;
    
    public PerceptionSystem(CombatState combatState)
    {
        this.combatState = combatState;
    }
    
    /// <summary>
    /// Get perception for an enemy. Returns null if enemy not tracked.
    /// Use Initialize() to set up perception tracking for all enemies.
    /// </summary>
    public EnemyPerception GetPerception(int enemyId)
    {
        perceptions.TryGetValue(enemyId, out var perception);
        return perception;
    }
    
    /// <summary>
    /// Get or create perception for an enemy. Used internally during initialization.
    /// </summary>
    private EnemyPerception GetOrCreatePerception(int enemyId)
    {
        if (!perceptions.TryGetValue(enemyId, out var perception))
        {
            perception = new EnemyPerception(enemyId);
            perception.StateChanged += OnEnemyStateChanged;
            perceptions[enemyId] = perception;
        }
        return perception;
    }
    
    public void Initialize()
    {
        foreach (var actor in combatState.Actors)
        {
            if (actor.Type == ActorTypes.Enemy && actor.State == ActorState.Alive)
            {
                GetOrCreatePerception(actor.Id);
            }
        }
        isInitialized = true;
        SimLog.Log($"[PerceptionSystem] Initialized with {perceptions.Count} enemies");
    }
    
    public void Tick()
    {
        var currentTick = combatState.TimeSystem.CurrentTick;
        
        foreach (var actor in combatState.Actors)
        {
            if (actor.Type != ActorTypes.Enemy || actor.State != ActorState.Alive)
            {
                continue;
            }
            
            var perception = GetPerception(actor.Id);
            if (perception == null)
            {
                continue;
            }
            CheckPerception(actor, perception, currentTick);
        }
    }
    
    private void CheckPerception(Actor enemy, EnemyPerception perception, int currentTick)
    {
        var visionRadius = enemy.GetVisionRadius();
        var enemyPos = enemy.GridPosition;
        
        foreach (var crew in combatState.Actors)
        {
            if (crew.Type != ActorTypes.Crew || crew.State != ActorState.Alive)
            {
                continue;
            }
            
            var crewPos = crew.GridPosition;
            var distance = CombatResolver.GetDistance(enemyPos, crewPos);
            
            if (distance > visionRadius)
            {
                continue;
            }
            
            if (!CombatResolver.HasLineOfSight(enemyPos, crewPos, combatState.MapState))
            {
                continue;
            }
            
            perception.UpdateLastKnown(crew.Id, crewPos);
            
            if (perception.State == DetectionState.Idle)
            {
                perception.SetState(DetectionState.Alerted, currentTick);
                EnemyDetectedCrew?.Invoke(enemy, crew);
                EnemyBecameAlerted?.Invoke(enemy);
                
                SimLog.Log($"[Perception] Enemy#{enemy.Id} detected Crew#{crew.Id} at {crewPos}");
            }
        }
    }
    
    private void OnEnemyStateChanged(EnemyPerception perception, DetectionState oldState, DetectionState newState)
    {
        if (newState == DetectionState.Alerted && AlarmState == AlarmState.Quiet)
        {
            SetAlarmState(AlarmState.Alerted);
        }
    }
    
    private void SetAlarmState(AlarmState newState)
    {
        if (AlarmState == newState)
        {
            return;
        }
        
        var oldState = AlarmState;
        AlarmState = newState;
        AlarmStateChanged?.Invoke(oldState, newState);
        
        SimLog.Log($"[PerceptionSystem] ALARM: {oldState} → {newState}");
    }
    
    public bool IsEnemyAlerted(int enemyId)
    {
        return perceptions.TryGetValue(enemyId, out var p) && p.State == DetectionState.Alerted;
    }
    
    public DetectionState GetDetectionState(int enemyId)
    {
        return perceptions.TryGetValue(enemyId, out var p) ? p.State : DetectionState.Idle;
    }
    
    public void AlertEnemy(int enemyId, Vector2I? investigatePosition = null)
    {
        var perception = GetOrCreatePerception(enemyId);
        var currentTick = combatState.TimeSystem.CurrentTick;
        
        if (perception.State == DetectionState.Idle)
        {
            perception.SetState(DetectionState.Alerted, currentTick);
            
            var enemy = combatState.GetActorById(enemyId);
            if (enemy != null)
            {
                EnemyBecameAlerted?.Invoke(enemy);
            }
        }
    }
    
    public void AlertAllEnemies()
    {
        foreach (var actor in combatState.Actors)
        {
            if (actor.Type == ActorTypes.Enemy && actor.State == ActorState.Alive)
            {
                AlertEnemy(actor.Id);
            }
        }
    }
    
    public void RemoveEnemy(int enemyId)
    {
        if (perceptions.TryGetValue(enemyId, out var perception))
        {
            perception.StateChanged -= OnEnemyStateChanged;
            perceptions.Remove(enemyId);
        }
    }
}
