namespace FringeTactics;

/// <summary>
/// Suppression effect - reduces accuracy, movement speed, and overwatch effectiveness.
/// Applied by suppressive fire, sustained attacks, etc.
/// </summary>
public class SuppressedEffect : EffectBase
{
    public const string EffectId = "suppressed";
    
    private const float AccuracyPenalty = 0.7f;
    private const float SpeedPenalty = 0.5f;
    private const float OverwatchPenalty = 0.5f;
    public const int DefaultDuration = 60; // 3 seconds at 20 ticks/sec
    
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
        var overwatchModifier = StatModifier.Multiplicative(EffectId, StatType.OverwatchAccuracy, OverwatchPenalty, -1);
        
        target.Modifiers.Add(accuracyModifier);
        target.Modifiers.Add(speedModifier);
        target.Modifiers.Add(overwatchModifier);
        
        SimLog.Log($"[Effect] {target.Type}#{target.Id} is SUPPRESSED");
    }
    
    public override void OnRemove(Actor target)
    {
        base.OnRemove(target);
        target.Modifiers.RemoveBySource(EffectId);
        SimLog.Log($"[Effect] {target.Type}#{target.Id} suppression ended");
    }
    
    public override IEffect Clone(int durationTicks) => new SuppressedEffect(durationTicks);
}
