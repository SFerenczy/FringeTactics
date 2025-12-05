namespace FringeTactics;

/// <summary>
/// Base class for effects providing common functionality.
/// </summary>
public abstract class EffectBase : IEffect
{
    public abstract string Id { get; }
    public abstract string Name { get; }
    public int RemainingTicks { get; protected set; }
    public bool IsExpired => RemainingTicks == 0;
    public virtual bool CanStack => false;
    
    protected EffectBase(int durationTicks)
    {
        RemainingTicks = durationTicks;
    }
    
    public virtual void OnApply(Actor target)
    {
        SimLog.Log($"[Effect] {Name} applied to {target.Type}#{target.Id} for {RemainingTicks} ticks");
    }
    
    public virtual void OnTick(Actor target)
    {
        if (RemainingTicks > 0)
        {
            RemainingTicks--;
        }
    }
    
    public virtual void OnRemove(Actor target)
    {
        SimLog.Log($"[Effect] {Name} removed from {target.Type}#{target.Id}");
    }
    
    public abstract IEffect Clone(int durationTicks);
}
