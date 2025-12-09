using System;
using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// Manages overwatch triggers and reaction fire.
/// Checks for movement into overwatch zones and executes reaction shots.
/// </summary>
public class OverwatchSystem
{
    private readonly CombatState combatState;
    
    public event Action<Actor, Actor, AttackResult> ReactionFired;
    
    public OverwatchSystem(CombatState combatState)
    {
        this.combatState = combatState;
    }
    
    /// <summary>
    /// Check if any overwatching units should react to this movement.
    /// Called before an actor commits to a new position.
    /// </summary>
    public void CheckMovement(Actor movingActor, Godot.Vector2I newPosition)
    {
        if (movingActor.State != ActorState.Alive) return;
        
        if (movingActor.Type == ActorType.Crew)
        {
            CheckEnemyOverwatch(movingActor, newPosition);
        }
        else if (movingActor.Type == ActorType.Enemy)
        {
            CheckCrewOverwatch(movingActor, newPosition);
        }
    }
    
    private void CheckEnemyOverwatch(Actor crew, Godot.Vector2I newPosition)
    {
        foreach (var enemy in combatState.Actors)
        {
            if (enemy.Type != ActorType.Enemy) continue;
            if (enemy.State != ActorState.Alive) continue;
            if (!enemy.IsOnOverwatch) continue;
            
            TryTriggerOverwatch(enemy, crew, newPosition);
        }
    }
    
    private void CheckCrewOverwatch(Actor enemy, Godot.Vector2I newPosition)
    {
        foreach (var crew in combatState.Actors)
        {
            if (crew.Type != ActorType.Crew) continue;
            if (crew.State != ActorState.Alive) continue;
            if (!crew.IsOnOverwatch) continue;
            
            TryTriggerOverwatch(crew, enemy, newPosition);
        }
    }
    
    private void TryTriggerOverwatch(Actor overwatcher, Actor target, Godot.Vector2I targetNewPos)
    {
        if (!overwatcher.CanFire()) return;
        
        // Check range
        var distance = CombatResolver.GetDistance(overwatcher.GridPosition, targetNewPos);
        var range = overwatcher.Overwatch.GetEffectiveRange(overwatcher.EquippedWeapon.Range);
        if (distance > range) return;
        
        // Check LOS
        if (!CombatResolver.HasLineOfSight(overwatcher.GridPosition, targetNewPos, combatState.MapState))
            return;
        
        // Check cone (if directional)
        if (!overwatcher.Overwatch.IsInCone(overwatcher.GridPosition, targetNewPos))
            return;
        
        ExecuteReactionFire(overwatcher, target);
    }
    
    private void ExecuteReactionFire(Actor overwatcher, Actor target)
    {
        SimLog.Log($"[Overwatch] {overwatcher.Type}#{overwatcher.Id} triggers on {target.Type}#{target.Id}!");
        
        var result = ResolveOverwatchAttack(overwatcher, target);
        
        var targetDied = AttackExecutor.ApplyAttackResult(overwatcher, target, result,
            victim => combatState.NotifyActorDied(victim));
        
        if (result.Hit)
        {
            var isGodMode = (target.Type == ActorType.Crew && DevTools.CrewGodMode) ||
                           (target.Type == ActorType.Enemy && DevTools.EnemyGodMode);
            var godModeTag = isGodMode ? " [GOD MODE]" : "";
            SimLog.Log($"[Overwatch] HIT! {result.Damage} damage. Target HP: {target.Hp}/{target.MaxHp}{godModeTag}");
        }
        else
        {
            SimLog.Log($"[Overwatch] MISS! ({result.HitChance:P0} chance)");
        }
        
        // Consume overwatch shot
        overwatcher.Overwatch.ConsumeShot();
        
        // Fire events
        overwatcher.NotifyOverwatchTriggered(target);
        ReactionFired?.Invoke(overwatcher, target, result);
    }
    
    /// <summary>
    /// Get all actors currently on overwatch.
    /// </summary>
    public List<Actor> GetOverwatchingActors()
    {
        var result = new List<Actor>();
        foreach (var actor in combatState.Actors)
        {
            if (actor.State == ActorState.Alive && actor.IsOnOverwatch)
            {
                result.Add(actor);
            }
        }
        return result;
    }
    
    /// <summary>
    /// Get all enemy actors on overwatch (for player threat visualization).
    /// </summary>
    public List<Actor> GetEnemyOverwatchers()
    {
        var result = new List<Actor>();
        foreach (var actor in combatState.Actors)
        {
            if (actor.Type == ActorType.Enemy && actor.State == ActorState.Alive && actor.IsOnOverwatch)
            {
                result.Add(actor);
            }
        }
        return result;
    }
    
    /// <summary>
    /// Get all crew actors on overwatch.
    /// </summary>
    public List<Actor> GetCrewOverwatchers()
    {
        var result = new List<Actor>();
        foreach (var actor in combatState.Actors)
        {
            if (actor.Type == ActorType.Crew && actor.State == ActorState.Alive && actor.IsOnOverwatch)
            {
                result.Add(actor);
            }
        }
        return result;
    }
    
    /// <summary>
    /// Resolve an overwatch attack, applying overwatch accuracy modifiers (e.g., from suppression).
    /// </summary>
    private AttackResult ResolveOverwatchAttack(Actor overwatcher, Actor target)
    {
        var overwatchAccuracyMod = overwatcher.Modifiers.Calculate(StatType.OverwatchAccuracy, 1.0f);
        
        return CombatResolver.ResolveAttack(
            overwatcher, target, overwatcher.EquippedWeapon,
            combatState.MapState, combatState.Rng, overwatchAccuracyMod);
    }
}
