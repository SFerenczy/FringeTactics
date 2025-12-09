using Godot;
using System;

namespace FringeTactics;

/// <summary>
/// Handles suppressive fire execution and suppression application.
/// </summary>
public class SuppressionSystem
{
    private readonly CombatState combatState;
    
    // Balance constants
    public const float DamageMultiplier = 0.5f;
    public const int AmmoConsumption = 5;
    public const float HitSuppressionChance = 1.0f;
    public const float NearMissSuppressionChance = 0.7f;
    public const float FarMissSuppressionChance = 0.3f;
    public const float NearMissRadius = 2f;
    public const float AccuracyPenalty = 0.8f;
    public const int AreaAmmoMultiplier = 2;
    
    public event Action<Actor, Actor, SuppressionResult> SuppressionApplied;
    public event Action<Actor, Vector2I> AreaSuppressionFired;
    
    public SuppressionSystem(CombatState combatState)
    {
        this.combatState = combatState;
    }
    
    /// <summary>
    /// Execute suppressive fire against a target actor.
    /// </summary>
    public SuppressionResult ExecuteSuppressiveFire(Actor attacker, Actor target)
    {
        if (!CombatResolver.CanAttack(attacker, target, attacker.EquippedWeapon, combatState.MapState))
        {
            return new SuppressionResult { AttackerId = attacker.Id, TargetId = target.Id, Success = false };
        }
        
        var result = new SuppressionResult
        {
            AttackerId = attacker.Id,
            TargetId = target.Id,
            Success = true
        };
        
        // Consume extra ammo
        var ammoToConsume = Math.Min(AmmoConsumption, attacker.CurrentMagazine);
        for (int i = 0; i < ammoToConsume; i++)
        {
            attacker.ConsumeAmmo();
        }
        
        // Roll for hit with reduced accuracy
        var hitChance = CombatResolver.CalculateHitChance(
            attacker, target, attacker.EquippedWeapon, combatState.MapState) * AccuracyPenalty;
        var roll = combatState.Rng.NextFloat();
        result.Hit = roll < hitChance;
        result.HitChance = hitChance;
        
        if (result.Hit)
        {
            var baseDamage = attacker.EquippedWeapon.Damage;
            var damage = (int)(baseDamage * DamageMultiplier);
            damage = CombatResolver.CalculateDamage(damage, target.Armor);
            
            target.TakeDamage(damage);
            result.Damage = damage;
            
            ApplySuppression(target);
            result.Suppressed = true;
        }
        else
        {
            var distance = CombatResolver.GetDistance(attacker.GridPosition, target.GridPosition);
            var suppressionChance = distance <= NearMissRadius
                ? NearMissSuppressionChance
                : FarMissSuppressionChance;
            
            if (combatState.Rng.NextFloat() < suppressionChance)
            {
                ApplySuppression(target);
                result.Suppressed = true;
            }
        }
        
        attacker.RecordShot(result.Hit, result.Damage);
        attacker.StartCooldown();
        
        SimLog.Log($"[Suppression] {attacker.Type}#{attacker.Id} suppressive fire on {target.Type}#{target.Id}: " +
                   $"Hit={result.Hit}, Damage={result.Damage}, Suppressed={result.Suppressed}");
        
        SuppressionApplied?.Invoke(attacker, target, result);
        return result;
    }
    
    /// <summary>
    /// Execute area suppression on a tile.
    /// </summary>
    public void ExecuteAreaSuppression(Actor attacker, Vector2I targetTile, int radius = 2)
    {
        SimLog.Log($"[Suppression] {attacker.Type}#{attacker.Id} area suppression at {targetTile}");
        
        // Consume ammo
        var ammoToConsume = Math.Min(AmmoConsumption * AreaAmmoMultiplier, attacker.CurrentMagazine);
        for (int i = 0; i < ammoToConsume; i++)
        {
            attacker.ConsumeAmmo();
        }
        
        attacker.StartCooldown();
        
        foreach (var actor in combatState.Actors)
        {
            if (actor.Id == attacker.Id) continue;
            if (actor.State != ActorState.Alive) continue;
            if (actor.Type == attacker.Type) continue;
            
            var distance = CombatResolver.GetDistance(targetTile, actor.GridPosition);
            if (distance > radius) continue;
            
            if (!CombatResolver.HasLineOfSight(attacker.GridPosition, actor.GridPosition, combatState.MapState))
                continue;
            
            // Apply suppression with distance falloff
            var suppressionChance = 0.8f - (distance * 0.15f);
            if (combatState.Rng.NextFloat() < suppressionChance)
            {
                ApplySuppression(actor);
                SimLog.Log($"[Suppression] {actor.Type}#{actor.Id} suppressed by area fire");
            }
        }
        
        AreaSuppressionFired?.Invoke(attacker, targetTile);
    }
    
    /// <summary>
    /// Apply suppression effect to a target.
    /// </summary>
    public void ApplySuppression(Actor target)
    {
        target.Effects.Apply(SuppressedEffect.EffectId, SuppressedEffect.DefaultDuration);
    }
    
    /// <summary>
    /// Check if an actor can use suppressive fire.
    /// </summary>
    public bool CanSuppressiveFire(Actor actor)
    {
        if (actor.State != ActorState.Alive) return false;
        if (actor.CurrentMagazine < AmmoConsumption) return false;
        if (actor.IsReloading) return false;
        if (actor.IsChanneling) return false;
        if (actor.AttackCooldown > 0) return false;
        return true;
    }
    
    /// <summary>
    /// Check if an actor can use area suppression.
    /// </summary>
    public bool CanAreaSuppression(Actor actor)
    {
        if (actor.State != ActorState.Alive) return false;
        if (actor.CurrentMagazine < AmmoConsumption * AreaAmmoMultiplier) return false;
        if (actor.IsReloading) return false;
        if (actor.IsChanneling) return false;
        if (actor.AttackCooldown > 0) return false;
        return true;
    }
}

/// <summary>
/// Result of a suppressive fire action.
/// </summary>
public struct SuppressionResult
{
    public int AttackerId;
    public int TargetId;
    public bool Success;
    public bool Hit;
    public int Damage;
    public float HitChance;
    public bool Suppressed;
}
