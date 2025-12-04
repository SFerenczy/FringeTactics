using Godot;
using System;
using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// Detection state for an individual enemy.
/// </summary>
public enum DetectionState
{
    Idle,
    Alerted
}

/// <summary>
/// Tracks what an enemy knows about player units.
/// </summary>
public class EnemyPerception
{
    public int EnemyId { get; }
    public DetectionState State { get; private set; } = DetectionState.Idle;
    
    public Dictionary<int, Vector2I> LastKnownPositions { get; } = new();
    
    public int StateChangedTick { get; private set; } = 0;
    
    /// <summary>
    /// Fired when detection state changes. Internal use by PerceptionSystem only.
    /// External systems should subscribe to PerceptionSystem.EnemyDetectedCrew instead.
    /// </summary>
    internal event Action<EnemyPerception, DetectionState, DetectionState> StateChanged;
    
    public EnemyPerception(int enemyId)
    {
        EnemyId = enemyId;
    }
    
    public void SetState(DetectionState newState, int currentTick)
    {
        if (State == newState)
        {
            return;
        }
        
        var oldState = State;
        State = newState;
        StateChangedTick = currentTick;
        StateChanged?.Invoke(this, oldState, newState);
        
        SimLog.Log($"[Perception] Enemy#{EnemyId} state: {oldState} → {newState}");
    }
    
    public void UpdateLastKnown(int crewId, Vector2I position)
    {
        LastKnownPositions[crewId] = position;
    }
    
    public void ClearLastKnown(int crewId)
    {
        LastKnownPositions.Remove(crewId);
    }
    
    public bool HasLastKnownPositions => LastKnownPositions.Count > 0;
}
