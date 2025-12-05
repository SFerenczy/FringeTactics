namespace FringeTactics;

/// <summary>
/// Suppression effect - reduces accuracy and movement speed.
/// Applied by sustained fire, suppressive abilities, etc.
/// </summary>
public class SuppressedEffect : EffectBase
{
    public const string EffectId = "suppressed";
    private const float AccuracyPenalty = 0.7f;
    private const float SpeedPenalty = 0.5f;
    
    public override string Id => EffectId;
    public override string Name => "Suppressed";
    public override bool CanStack => false;
    
    public SuppressedEffect(int durationTicks) : base(durationTicks)
    {
    }
    
    public override void OnApply(Actor target)
    {
        base.OnApply(target);
        
        var accuracyModifier = StatModifier.Multiplicative(EffectId, StatType.Accuracy, AccuracyPenalty, -1);
        var speedModifier = StatModifier.Multiplicative(EffectId, StatType.MoveSpeed, SpeedPenalty, -1);
        
        target.Modifiers.Add(accuracyModifier);
        target.Modifiers.Add(speedModifier);
    }
    
    public override void OnRemove(Actor target)
    {
        base.OnRemove(target);
        target.Modifiers.RemoveBySource(EffectId);
    }
    
    public override IEffect Clone(int durationTicks) => new SuppressedEffect(durationTicks);
}
