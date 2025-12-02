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
            if (actor.Type != "enemy" || actor.State != ActorState.Alive)
            {
                continue;
            }

            Think(actor);
        }
    }

    private void Think(Actor enemy)
    {
        // If already attacking a valid target in range, keep attacking
        if (enemy.AttackTargetId.HasValue)
        {
            var currentTarget = combatState.GetActorById(enemy.AttackTargetId.Value);
            if (currentTarget != null && currentTarget.State == ActorState.Alive)
            {
                if (CombatResolver.CanAttack(enemy, currentTarget, enemy.EquippedWeapon, combatState.MapState))
                {
                    // Still valid, keep attacking
                    return;
                }
            }
            // Target invalid or out of range, clear it
            enemy.SetAttackTarget(null);
        }

        // Find closest visible player actor
        var target = FindClosestPlayerActor(enemy);
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

    private Actor FindClosestPlayerActor(Actor enemy)
    {
        Actor closest = null;
        float closestDist = float.MaxValue;

        foreach (var actor in combatState.Actors)
        {
            if (actor.Type == "enemy" || actor.State != ActorState.Alive)
            {
                continue;
            }

            // Check line of sight
            if (!CombatResolver.HasLineOfSight(enemy.GridPosition, actor.GridPosition, combatState.MapState))
            {
                continue;
            }

            var dist = CombatResolver.GetDistance(enemy.GridPosition, actor.GridPosition);
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = actor;
            }
        }

        return closest;
    }

    private Vector2I GetMoveTowardTarget(Actor enemy, Actor target)
    {
        // Simple: move one step toward target
        var diff = target.GridPosition - enemy.GridPosition;
        var step = new Vector2I(
            Mathf.Clamp(diff.X, -1, 1),
            Mathf.Clamp(diff.Y, -1, 1)
        );

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
