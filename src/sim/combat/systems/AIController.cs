using Godot; // For Vector2I, Mathf only - no Node/UI types

namespace FringeTactics;

/// <summary>
/// Simple AI controller for enemy actors.
/// Runs every N ticks: picks closest visible player, moves toward range, attacks if in range.
/// </summary>
public class AIController
{
    public const int THINK_INTERVAL_TICKS = 10; // Think every 0.5 seconds at 20 ticks/sec

    private readonly CombatState combatState;
    private int ticksSinceLastThink = 0;

    public AIController(CombatState combatState)
    {
        this.combatState = combatState;
    }

    public void Tick()
    {
        ticksSinceLastThink++;

        if (ticksSinceLastThink < THINK_INTERVAL_TICKS)
        {
            return;
        }

        ticksSinceLastThink = 0;
        ThinkAllEnemies();
    }

    private void ThinkAllEnemies()
    {
        foreach (var actor in combatState.Actors)
        {
            if (actor.Type != ActorType.Enemy || actor.State != ActorState.Alive)
            {
                continue;
            }

            Think(actor);
        }
    }

    private void Think(Actor enemy)
    {
        // Idle enemies don't actively hunt - they stand guard
        var detectionState = combatState.Perception.GetDetectionState(enemy.Id);
        if (detectionState == DetectionState.Idle)
        {
            return;
        }
        
        // If already attacking a valid target in range, consider switching to better target
        if (enemy.AttackTargetId.HasValue)
        {
            var currentTarget = combatState.GetActorById(enemy.AttackTargetId.Value);
            if (currentTarget != null && currentTarget.State == ActorState.Alive)
            {
                if (CombatResolver.CanAttack(enemy, currentTarget, enemy.EquippedWeapon, combatState.MapState))
                {
                    // Check if there's a significantly better target
                    var betterTarget = FindBestTarget(enemy);
                    if (betterTarget != null && betterTarget.Id != currentTarget.Id)
                    {
                        var currentScore = ScoreTarget(enemy, currentTarget);
                        var betterScore = ScoreTarget(enemy, betterTarget);
                        
                        // Only switch if new target is significantly better (25% threshold)
                        if (betterScore > currentScore * 1.25f)
                        {
                            enemy.SetAttackTarget(betterTarget.Id);
                            SimLog.Log($"[AI] Enemy#{enemy.Id} switching target to Player#{betterTarget.Id} (better priority)");
                        }
                    }
                    return;
                }
            }
            // Target invalid or out of range, clear it
            enemy.SetAttackTarget(null);
        }

        // Find best target using priority scoring
        var target = FindBestTarget(enemy);
        if (target == null)
        {
            return;
        }

        // Check if in range to attack
        if (CombatResolver.CanAttack(enemy, target, enemy.EquippedWeapon, combatState.MapState))
        {
            // Attack!
            enemy.SetAttackTarget(target.Id);
            SimLog.Log($"[AI] Enemy#{enemy.Id} attacking Player#{target.Id}");
        }
        else
        {
            // Move toward target
            var moveTarget = GetMoveTowardTarget(enemy, target);
            if (moveTarget != enemy.GridPosition)
            {
                enemy.SetTarget(moveTarget);
                SimLog.Log($"[AI] Enemy#{enemy.Id} moving toward Player#{target.Id} at {moveTarget}");
            }
        }
    }

    /// <summary>
    /// Find the best target using priority scoring.
    /// Considers: distance, health, threat level.
    /// </summary>
    private Actor FindBestTarget(Actor enemy)
    {
        Actor bestTarget = null;
        float bestScore = float.MinValue;

        foreach (var actor in combatState.Actors)
        {
            if (actor.Type == ActorType.Enemy || actor.State != ActorState.Alive)
            {
                continue;
            }

            // Check line of sight
            if (!CombatResolver.HasLineOfSight(enemy.GridPosition, actor.GridPosition, combatState.MapState))
            {
                continue;
            }

            var score = ScoreTarget(enemy, actor);
            if (score > bestScore)
            {
                bestScore = score;
                bestTarget = actor;
            }
        }

        return bestTarget;
    }

    /// <summary>
    /// Score a potential target for priority.
    /// Higher score = higher priority.
    /// </summary>
    private float ScoreTarget(Actor enemy, Actor target)
    {
        var distance = CombatResolver.GetDistance(enemy.GridPosition, target.GridPosition);
        
        // Distance score: closer targets are better (inverse relationship)
        // Range 0-1, where 1 = adjacent, approaches 0 at long range
        var distanceScore = 1f / (distance + 1f);
        
        // Health score: wounded targets are prioritized (finish them off)
        // Range 0-1, where 1 = nearly dead, 0 = full health
        var healthScore = 1f - (target.Hp / (float)target.MaxHp);
        
        // Threat score: prioritize targets that are attacking us
        var threatScore = 0f;
        if (target.AttackTargetId == enemy.Id)
        {
            threatScore = 0.5f; // Significant bonus for threats
        }
        else if (target.AutoDefendTargetId == enemy.Id)
        {
            threatScore = 0.3f; // Smaller bonus for auto-defend targets
        }
        
        // In-range bonus: prefer targets we can actually shoot right now
        var inRangeBonus = 0f;
        if (CombatResolver.CanAttack(enemy, target, enemy.EquippedWeapon, combatState.MapState))
        {
            inRangeBonus = 0.4f;
        }
        
        // Weighted combination
        // Distance is most important, then in-range, then threat, then health
        var totalScore = distanceScore * 1.0f 
                       + inRangeBonus 
                       + threatScore 
                       + healthScore * 0.3f;
        
        return totalScore;
    }

    private Vector2I GetMoveTowardTarget(Actor enemy, Actor target)
    {
        // Simple: move one step toward target
        var step = GridUtils.GetStepDirection(enemy.GridPosition, target.GridPosition);
        var newPos = enemy.GridPosition + step;

        // Check if walkable and not occupied
        if (combatState.MapState.IsWalkable(newPos) && combatState.GetActorAtPosition(newPos) == null)
        {
            return newPos;
        }

        // Try cardinal directions only if diagonal blocked
        var cardinalSteps = new Vector2I[]
        {
            new Vector2I(step.X, 0),
            new Vector2I(0, step.Y)
        };

        foreach (var cardinalStep in cardinalSteps)
        {
            if (cardinalStep == Vector2I.Zero)
            {
                continue;
            }

            var cardinalPos = enemy.GridPosition + cardinalStep;
            if (combatState.MapState.IsWalkable(cardinalPos) && combatState.GetActorAtPosition(cardinalPos) == null)
            {
                return cardinalPos;
            }
        }

        // Can't move
        return enemy.GridPosition;
    }
}
