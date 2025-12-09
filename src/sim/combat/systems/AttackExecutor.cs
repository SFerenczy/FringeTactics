using System;

namespace FringeTactics;

/// <summary>
/// Shared attack execution logic used by AttackSystem and OverwatchSystem.
/// Handles damage application, god mode checks, statistics, and death notification.
/// </summary>
public static class AttackExecutor
{
    /// <summary>
    /// Execute an attack that has already been resolved.
    /// Applies damage, records statistics, and handles death.
    /// </summary>
    /// <param name="attacker">The attacking actor</param>
    /// <param name="target">The target actor</param>
    /// <param name="result">The resolved attack result</param>
    /// <param name="onActorDied">Callback when target dies (for CombatState notification)</param>
    /// <returns>True if target died from this attack</returns>
    public static bool ApplyAttackResult(Actor attacker, Actor target, AttackResult result, Action<Actor> onActorDied = null)
    {
        attacker.StartCooldown();
        attacker.ConsumeAmmo();
        attacker.RecordShot(result.Hit, result.Hit ? result.Damage : 0);
        
        bool targetDied = false;
        
        if (result.Hit)
        {
            var isGodMode = (target.Type == ActorType.Crew && DevTools.CrewGodMode) ||
                           (target.Type == ActorType.Enemy && DevTools.EnemyGodMode);
            
            if (!isGodMode)
            {
                target.TakeDamage(result.Damage);
            }
            
            if (target.State == ActorState.Dead)
            {
                attacker.RecordKill();
                targetDied = true;
                onActorDied?.Invoke(target);
            }
        }
        
        return targetDied;
    }
    
    /// <summary>
    /// Get a formatted log message for an attack.
    /// </summary>
    public static string FormatAttackLog(Actor attacker, Actor target, AttackResult result, string attackType)
    {
        var coverTag = GetCoverTag(result.TargetCoverHeight);
        var isGodMode = (target.Type == ActorType.Crew && DevTools.CrewGodMode) ||
                       (target.Type == ActorType.Enemy && DevTools.EnemyGodMode);
        var godModeTag = isGodMode ? " [GOD MODE]" : "";
        
        if (result.Hit)
        {
            var armorTag = result.TargetArmor > 0 ? $" ({result.RawDamage} - {result.TargetArmor} armor)" : "";
            return $"[Combat] {attacker.Type}#{attacker.Id} hit {target.Type}#{target.Id} ({attackType}) with {result.WeaponName} for {result.Damage} damage{armorTag} ({result.HitChance:P0} chance){coverTag}. HP: {target.Hp}/{target.MaxHp}{godModeTag}";
        }
        else
        {
            return $"[Combat] {attacker.Type}#{attacker.Id} missed {target.Type}#{target.Id} ({attackType}) with {result.WeaponName} ({result.HitChance:P0} chance){coverTag}";
        }
    }
    
    private static string GetCoverTag(CoverHeight coverHeight)
    {
        return coverHeight switch
        {
            CoverHeight.Low => " [LOW COVER]",
            CoverHeight.Half => " [HALF COVER]",
            CoverHeight.High => " [HIGH COVER]",
            CoverHeight.Full => " [FULL COVER]",
            _ => ""
        };
    }
}
