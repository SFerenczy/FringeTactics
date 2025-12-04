using System;
using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// Processes attacks and auto-defend behavior each tick.
/// Stateless service that operates on actor lists.
/// </summary>
public class AttackSystem
{
    private readonly Func<int, Actor> getActorById;

    public event Action<Actor, Actor, AttackResult> AttackResolved;
    public event Action<Actor> ActorDied;

    public AttackSystem(Func<int, Actor> getActorById)
    {
        this.getActorById = getActorById;
    }

    /// <summary>
    /// Process all attacks for this tick.
    /// </summary>
    public void ProcessTick(IReadOnlyList<Actor> actors, MapState map, CombatRng rng, CombatStats stats)
    {
        ProcessManualAttacks(actors, map, rng, stats);
        ProcessAutoDefend(actors, map, rng, stats);
    }

    private void ProcessManualAttacks(IReadOnlyList<Actor> actors, MapState map, CombatRng rng, CombatStats stats)
    {
        foreach (var attacker in actors)
        {
            if (attacker.State != ActorState.Alive)
            {
                continue;
            }

            if (!attacker.AttackTargetId.HasValue)
            {
                continue;
            }

            if (!attacker.CanFire())
            {
                if (attacker.NeedsReload())
                {
                    attacker.StartReload();
                }
                continue;
            }

            var target = getActorById(attacker.AttackTargetId.Value);
            if (target == null || target.State != ActorState.Alive)
            {
                attacker.SetAttackTarget(null);
                continue;
            }

            if (CombatResolver.CanAttack(attacker, target, attacker.EquippedWeapon, map))
            {
                ExecuteAttack(attacker, target, map, rng, stats, isAutoDefend: false);
            }
        }
    }

    private void ProcessAutoDefend(IReadOnlyList<Actor> actors, MapState map, CombatRng rng, CombatStats stats)
    {
        foreach (var defender in actors)
        {
            if (defender.State != ActorState.Alive)
            {
                continue;
            }

            // Manual orders take priority
            if (defender.AttackTargetId.HasValue)
            {
                continue;
            }

            if (!defender.AutoDefendTargetId.HasValue)
            {
                continue;
            }

            if (!defender.CanFire())
            {
                if (defender.NeedsReload())
                {
                    defender.StartReload();
                }
                continue;
            }

            var attacker = getActorById(defender.AutoDefendTargetId.Value);
            if (attacker == null || attacker.State != ActorState.Alive)
            {
                defender.ClearAutoDefendTarget();
                continue;
            }

            if (CombatResolver.CanAttack(defender, attacker, defender.EquippedWeapon, map))
            {
                ExecuteAttack(defender, attacker, map, rng, stats, isAutoDefend: true);
            }
        }
    }

    private void ExecuteAttack(Actor attacker, Actor target, MapState map, CombatRng rng, CombatStats stats, bool isAutoDefend)
    {
        var result = CombatResolver.ResolveAttack(attacker, target, attacker.EquippedWeapon, map, rng.GetRandom());
        attacker.StartCooldown();
        attacker.ConsumeAmmo();

        var attackType = isAutoDefend ? "auto-defend" : "attack";
        var coverTag = GetCoverTag(result.TargetCoverHeight);

        // Record shot statistics (M7)
        attacker.RecordShot(result.Hit, result.Hit ? result.Damage : 0);
        
        if (result.Hit)
        {
            var isGodMode = (target.Type == ActorType.Crew && DevTools.CrewGodMode) ||
                           (target.Type == ActorType.Enemy && DevTools.EnemyGodMode);

            if (!isGodMode)
            {
                target.TakeDamage(result.Damage);
            }

            var godModeTag = isGodMode ? " [GOD MODE]" : "";
            SimLog.Log($"[Combat] {attacker.Type}#{attacker.Id} hit {target.Type}#{target.Id} ({attackType}) for {result.Damage} damage ({result.HitChance:P0} chance){coverTag}. HP: {target.Hp}/{target.MaxHp}{godModeTag}");

            if (target.State == ActorState.Alive)
            {
                target.SetAutoDefendTarget(attacker.Id);
            }
            else
            {
                attacker.RecordKill(); // M7 statistics
                SimLog.Log($"[Combat] {target.Type}#{target.Id} DIED!");
                ActorDied?.Invoke(target);
            }
        }
        else
        {
            SimLog.Log($"[Combat] {attacker.Type}#{attacker.Id} missed {target.Type}#{target.Id} ({attackType}) ({result.HitChance:P0} chance){coverTag}");
            target.SetAutoDefendTarget(attacker.Id);
        }

        AttackResolved?.Invoke(attacker, target, result);
        UpdateStats(stats, attacker, result);
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

    private static void UpdateStats(CombatStats stats, Actor attacker, AttackResult result)
    {
        if (attacker.Type == ActorType.Crew)
        {
            stats.PlayerShotsFired++;
            if (result.Hit)
            {
                stats.PlayerHits++;
            }
            else
            {
                stats.PlayerMisses++;
            }
        }
        else
        {
            stats.EnemyShotsFired++;
            if (result.Hit)
            {
                stats.EnemyHits++;
            }
            else
            {
                stats.EnemyMisses++;
            }
        }
    }
}
