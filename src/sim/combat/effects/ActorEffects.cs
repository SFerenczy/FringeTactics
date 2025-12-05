using System;
using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// Manages active effects on an actor.
/// </summary>
public class ActorEffects
{
    private readonly List<IEffect> activeEffects = new();
    private readonly Actor owner;
    
    public event Action<IEffect> EffectApplied;
    public event Action<IEffect> EffectRemoved;
    
    public IReadOnlyList<IEffect> Active => activeEffects;
    
    public ActorEffects(Actor owner)
    {
        this.owner = owner;
    }
    
    /// <summary>
    /// Apply an effect to the actor.
    /// </summary>
    public bool Apply(IEffect effect)
    {
        if (effect == null) return false;
        
        if (!effect.CanStack)
        {
            var existing = Find(effect.Id);
            if (existing != null)
            {
                Remove(existing);
            }
        }
        
        activeEffects.Add(effect);
        effect.OnApply(owner);
        EffectApplied?.Invoke(effect);
        
        return true;
    }
    
    /// <summary>
    /// Apply an effect by ID with duration.
    /// </summary>
    public bool Apply(string effectId, int durationTicks)
    {
        var effect = EffectRegistry.Create(effectId, durationTicks);
        return Apply(effect);
    }
    
    /// <summary>
    /// Remove a specific effect instance.
    /// </summary>
    public bool Remove(IEffect effect)
    {
        if (!activeEffects.Contains(effect)) return false;
        
        effect.OnRemove(owner);
        activeEffects.Remove(effect);
        EffectRemoved?.Invoke(effect);
        
        return true;
    }
    
    /// <summary>
    /// Remove all effects with the given ID.
    /// </summary>
    public int RemoveAll(string effectId)
    {
        int removed = 0;
        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            if (activeEffects[i].Id == effectId)
            {
                var effect = activeEffects[i];
                effect.OnRemove(owner);
                activeEffects.RemoveAt(i);
                EffectRemoved?.Invoke(effect);
                removed++;
            }
        }
        return removed;
    }
    
    /// <summary>
    /// Find an active effect by ID.
    /// </summary>
    public IEffect Find(string effectId)
    {
        foreach (var effect in activeEffects)
        {
            if (effect.Id == effectId) return effect;
        }
        return null;
    }
    
    /// <summary>
    /// Check if actor has an effect.
    /// </summary>
    public bool Has(string effectId) => Find(effectId) != null;
    
    /// <summary>
    /// Tick all active effects and remove expired ones.
    /// </summary>
    public void Tick()
    {
        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            var effect = activeEffects[i];
            effect.OnTick(owner);
            
            if (effect.IsExpired)
            {
                effect.OnRemove(owner);
                activeEffects.RemoveAt(i);
                EffectRemoved?.Invoke(effect);
            }
        }
    }
    
    /// <summary>
    /// Clear all effects.
    /// </summary>
    public void Clear()
    {
        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            var effect = activeEffects[i];
            effect.OnRemove(owner);
            EffectRemoved?.Invoke(effect);
        }
        activeEffects.Clear();
    }
}
