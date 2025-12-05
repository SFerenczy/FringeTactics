using Godot; // For Vector2I, Mathf only - no Node/UI types
using System;
using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// Pending ability execution (for delayed abilities like grenades).
/// </summary>
public class PendingAbility
{
    public int CasterId { get; set; }
    public AbilityData Ability { get; set; }
    public Vector2I TargetTile { get; set; }
    public int? TargetActorId { get; set; }
    public int TicksRemaining { get; set; }
}

/// <summary>
/// Manages ability execution, cooldowns, and delayed effects.
/// </summary>
public class AbilitySystem
{
    private readonly CombatState combatState;
    private readonly List<PendingAbility> pendingAbilities = new();
    private readonly Dictionary<int, Dictionary<string, int>> actorCooldowns = new(); // actorId -> abilityId -> ticksRemaining

    // Events
    public event Action<int, AbilityData, Vector2I> AbilityCast;        // casterId, ability, targetTile
    public event Action<AbilityData, Vector2I> AbilityDetonated;        // ability, tile (for AoE visual)
    public event Action<Actor, string, int> StatusEffectApplied;        // target, effectId, duration

    public AbilitySystem(CombatState combatState)
    {
        this.combatState = combatState;
    }

    public void Tick()
    {
        // Tick down cooldowns
        foreach (var actorCooldown in actorCooldowns.Values)
        {
            var keysToRemove = new List<string>();
            foreach (var kvp in actorCooldown)
            {
                actorCooldown[kvp.Key] = kvp.Value - 1;
                if (actorCooldown[kvp.Key] <= 0)
                {
                    keysToRemove.Add(kvp.Key);
                }
            }
            foreach (var key in keysToRemove)
            {
                actorCooldown.Remove(key);
            }
        }

        // Process pending abilities (grenades in flight)
        for (int i = pendingAbilities.Count - 1; i >= 0; i--)
        {
            var pending = pendingAbilities[i];
            pending.TicksRemaining--;

            if (pending.TicksRemaining <= 0)
            {
                ExecuteAbilityEffect(pending);
                pendingAbilities.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// Check if an actor can use an ability.
    /// </summary>
    public bool CanUseAbility(int actorId, AbilityData ability, Vector2I targetTile)
    {
        var actor = combatState.GetActorById(actorId);
        if (actor == null || actor.State != ActorState.Alive)
        {
            return false;
        }

        // Check cooldown
        if (IsOnCooldown(actorId, ability.Id))
        {
            return false;
        }

        // Check range
        var distance = CombatResolver.GetDistance(actor.GridPosition, targetTile);
        if (distance > ability.Range)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Use an ability. Returns true if successful.
    /// </summary>
    public bool UseAbility(int actorId, AbilityData ability, Vector2I targetTile, int? targetActorId = null)
    {
        if (!CanUseAbility(actorId, ability, targetTile))
        {
            return false;
        }

        var actor = combatState.GetActorById(actorId);

        // Start cooldown
        SetCooldown(actorId, ability.Id, ability.Cooldown);

        // Stop movement/attack
        actor.ClearOrders();

        SimLog.Log($"[Ability] {actor.Type}#{actor.Id} uses {ability.Name} at {targetTile}");
        AbilityCast?.Invoke(actorId, ability, targetTile);

        if (ability.Delay > 0)
        {
            // Queue for delayed execution
            pendingAbilities.Add(new PendingAbility
            {
                CasterId = actorId,
                Ability = ability,
                TargetTile = targetTile,
                TargetActorId = targetActorId,
                TicksRemaining = ability.Delay
            });
        }
        else
        {
            // Execute immediately
            ExecuteAbilityEffect(new PendingAbility
            {
                CasterId = actorId,
                Ability = ability,
                TargetTile = targetTile,
                TargetActorId = targetActorId,
                TicksRemaining = 0
            });
        }

        return true;
    }

    private void ExecuteAbilityEffect(PendingAbility pending)
    {
        var ability = pending.Ability;

        SimLog.Log($"[Ability] {ability.Name} detonates at {pending.TargetTile}!");
        AbilityDetonated?.Invoke(ability, pending.TargetTile);

        // Find affected actors
        var affectedActors = GetActorsInRadius(pending.TargetTile, ability.Radius);

        foreach (var target in affectedActors)
        {
            // Apply damage
            if (ability.Damage > 0)
            {
                target.TakeDamage(ability.Damage);
                SimLog.Log($"[Ability] {ability.Name} deals {ability.Damage} damage to {target.Type}#{target.Id}");

                if (target.State == ActorState.Dead)
                {
                    combatState.NotifyActorDied(target);
                }
            }

            // Apply status effect
            if (!string.IsNullOrEmpty(ability.EffectId) && target.State == ActorState.Alive)
            {
                ApplyStatusEffect(target, ability.EffectId, ability.EffectDuration);
            }
        }
    }

    private List<Actor> GetActorsInRadius(Vector2I center, int radius)
    {
        var result = new List<Actor>();

        foreach (var actor in combatState.Actors)
        {
            if (actor.State != ActorState.Alive)
            {
                continue;
            }

            var distance = CombatResolver.GetDistance(center, actor.GridPosition);
            if (distance <= radius)
            {
                result.Add(actor);
            }
        }

        return result;
    }

    private void ApplyStatusEffect(Actor target, string effectId, int duration)
    {
        if (!EffectRegistry.Has(effectId))
        {
            SimLog.Log($"[Ability] Unknown effect ID: {effectId} - register in EffectRegistry");
            return;
        }
        
        target.Effects.Apply(effectId, duration);
        SimLog.Log($"[Ability] {target.Type}#{target.Id} is now {effectId} for {duration} ticks");
        StatusEffectApplied?.Invoke(target, effectId, duration);
    }

    public bool IsOnCooldown(int actorId, string abilityId)
    {
        if (!actorCooldowns.ContainsKey(actorId))
        {
            return false;
        }

        return actorCooldowns[actorId].ContainsKey(abilityId);
    }

    public int GetCooldownRemaining(int actorId, string abilityId)
    {
        if (!actorCooldowns.ContainsKey(actorId))
        {
            return 0;
        }

        if (!actorCooldowns[actorId].ContainsKey(abilityId))
        {
            return 0;
        }

        return actorCooldowns[actorId][abilityId];
    }

    private void SetCooldown(int actorId, string abilityId, int ticks)
    {
        if (!actorCooldowns.ContainsKey(actorId))
        {
            actorCooldowns[actorId] = new Dictionary<string, int>();
        }

        actorCooldowns[actorId][abilityId] = ticks;
    }

    public List<PendingAbility> GetPendingAbilities()
    {
        return pendingAbilities;
    }
}
