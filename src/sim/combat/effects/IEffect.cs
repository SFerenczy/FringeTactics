namespace FringeTactics;

/// <summary>
/// Interface for status effects that can be applied to actors.
/// Effects are tick-based and can modify actor stats or behavior.
/// </summary>
public interface IEffect
{
    /// <summary>
    /// Unique identifier for this effect type.
    /// </summary>
    string Id { get; }
    
    /// <summary>
    /// Display name for UI.
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// Remaining ticks until effect expires. -1 for permanent effects.
    /// </summary>
    int RemainingTicks { get; }
    
    /// <summary>
    /// Whether this effect has expired.
    /// </summary>
    bool IsExpired { get; }
    
    /// <summary>
    /// Whether multiple instances of this effect can stack on the same target.
    /// </summary>
    bool CanStack { get; }
    
    /// <summary>
    /// Called when effect is first applied to an actor.
    /// </summary>
    void OnApply(Actor target);
    
    /// <summary>
    /// Called each simulation tick while effect is active.
    /// </summary>
    void OnTick(Actor target);
    
    /// <summary>
    /// Called when effect is removed (expired or cleansed).
    /// </summary>
    void OnRemove(Actor target);
    
    /// <summary>
    /// Create a copy of this effect with the specified duration.
    /// </summary>
    IEffect Clone(int durationTicks);
}
