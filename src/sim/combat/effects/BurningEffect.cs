namespace FringeTactics;

/// <summary>
/// Burning effect - deals damage over time.
/// Applied by incendiary grenades, fire hazards, etc.
/// </summary>
public class BurningEffect : EffectBase
{
    public const string EffectId = "burning";
    
    public override string Id => EffectId;
    public override string Name => "Burning";
    public override bool CanStack => false;
    
    private readonly int damagePerTick;
    private int tickCounter = 0;
    private const int TicksPerDamage = 10;
    
    public BurningEffect(int durationTicks, int damagePerTick = 5) : base(durationTicks)
    {
        this.damagePerTick = damagePerTick;
    }
    
    public override void OnTick(Actor target)
    {
        base.OnTick(target);
        
        tickCounter++;
        if (tickCounter >= TicksPerDamage)
        {
            tickCounter = 0;
            target.TakeDamage(damagePerTick);
            SimLog.Log($"[Effect] {target.Type}#{target.Id} takes {damagePerTick} burn damage");
        }
    }
    
    public override IEffect Clone(int durationTicks) => new BurningEffect(durationTicks, damagePerTick);
}
