namespace FringeTactics;

/// <summary>
/// Stun effect - prevents the target from acting.
/// Used by flashbangs, EMP grenades, melee stuns, etc.
/// </summary>
public class StunEffect : EffectBase
{
    public const string EffectId = "stunned";
    
    public override string Id => EffectId;
    public override string Name => "Stunned";
    
    public StunEffect(int durationTicks) : base(durationTicks)
    {
    }
    
    public override void OnApply(Actor target)
    {
        base.OnApply(target);
        
        // Zero multiplier = cannot move
        var stunModifier = StatModifier.Multiplicative(EffectId, StatType.MoveSpeed, 0f, -1);
        target.Modifiers.Add(stunModifier);
        
        target.ClearOrders();
        target.CancelChannel();
    }
    
    public override void OnRemove(Actor target)
    {
        base.OnRemove(target);
        target.Modifiers.RemoveBySource(EffectId);
    }
    
    public override IEffect Clone(int durationTicks) => new StunEffect(durationTicks);
}
