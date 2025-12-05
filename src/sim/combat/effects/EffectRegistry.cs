using System;
using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// Registry of effect types. Maps effect IDs to factory functions.
/// </summary>
public static class EffectRegistry
{
    private static readonly Dictionary<string, Func<int, IEffect>> effectFactories = new();
    private static bool initialized = false;
    
    /// <summary>
    /// Ensure registry is initialized with default effects.
    /// </summary>
    public static void EnsureInitialized()
    {
        if (initialized) return;
        
        Register(StunEffect.EffectId, duration => new StunEffect(duration));
        Register(SuppressedEffect.EffectId, duration => new SuppressedEffect(duration));
        Register(BurningEffect.EffectId, duration => new BurningEffect(duration));
        
        initialized = true;
    }
    
    /// <summary>
    /// Register an effect factory.
    /// </summary>
    public static void Register(string effectId, Func<int, IEffect> factory)
    {
        effectFactories[effectId] = factory;
    }
    
    /// <summary>
    /// Create an effect instance by ID.
    /// </summary>
    public static IEffect Create(string effectId, int durationTicks)
    {
        EnsureInitialized();
        
        if (effectFactories.TryGetValue(effectId, out var factory))
        {
            return factory(durationTicks);
        }
        
        SimLog.Log($"[EffectRegistry] Unknown effect ID: {effectId}");
        return null;
    }
    
    /// <summary>
    /// Check if an effect ID is registered.
    /// </summary>
    public static bool Has(string effectId)
    {
        EnsureInitialized();
        return effectFactories.ContainsKey(effectId);
    }
    
    /// <summary>
    /// Reset registry (for testing).
    /// </summary>
    public static void Reset()
    {
        effectFactories.Clear();
        initialized = false;
    }
}
