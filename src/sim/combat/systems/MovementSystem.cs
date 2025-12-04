using Godot;
using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// Handles movement collision resolution and movement processing.
/// Stateless utility methods.
/// </summary>
public static class MovementSystem
{
    /// <summary>
    /// Resolve movement collisions before actors move.
    /// Pauses actors that would collide with each other or stationary units.
    /// </summary>
    public static void ResolveCollisions(IReadOnlyList<Actor> actors, MapState map)
    {
        var destinations = new Dictionary<Vector2I, List<Actor>>();

        foreach (var actor in actors)
        {
            if (actor.State != ActorState.Alive || !actor.IsMoving)
            {
                continue;
            }

            var moveDir = GridUtils.GetStepDirection(actor.GridPosition, actor.TargetPosition);
            var nextTile = actor.GridPosition + moveDir;

            if (!destinations.ContainsKey(nextTile))
            {
                destinations[nextTile] = new List<Actor>();
            }
            destinations[nextTile].Add(actor);
        }

        foreach (var kvp in destinations)
        {
            var tile = kvp.Key;
            var movers = kvp.Value;

            // Check if tile is occupied by stationary unit
            Actor occupant = null;
            foreach (var actor in actors)
            {
                if (actor.GridPosition == tile && actor.State == ActorState.Alive && !actor.IsMoving)
                {
                    occupant = actor;
                    break;
                }
            }

            bool tileOccupied = occupant != null;

            if (movers.Count > 1 || tileOccupied)
            {
                // Sort by distance to target (closest gets priority)
                movers.Sort((a, b) =>
                {
                    var distA = (a.TargetPosition - a.GridPosition).LengthSquared();
                    var distB = (b.TargetPosition - b.GridPosition).LengthSquared();
                    return distA.CompareTo(distB);
                });

                // If tile is occupied by stationary unit, pause all movers
                int startIndex = tileOccupied ? 0 : 1;

                for (int i = startIndex; i < movers.Count; i++)
                {
                    movers[i].PauseMovement();
                }
            }
        }
    }

    /// <summary>
    /// Process movement for a single actor.
    /// Handles pathfinding around obstacles.
    /// </summary>
    public static void ProcessMovement(Actor actor, float tickDuration)
    {
        if (actor.State == ActorState.Dead)
        {
            return;
        }

        // Skip if reloading or channeling
        if (actor.IsReloading || actor.IsChanneling)
        {
            return;
        }

        if (!actor.IsMoving)
        {
            return;
        }

        if (actor.GridPosition == actor.TargetPosition)
        {
            actor.CompleteMovement();
            return;
        }

        var map = actor.Map;
        var moveDir = GridUtils.GetStepDirection(actor.GridPosition, actor.TargetPosition);
        var nextTile = actor.GridPosition + moveDir;

        // Check if next tile is walkable
        if (map != null && !map.IsWalkable(nextTile))
        {
            // Try cardinal directions if diagonal is blocked
            if (moveDir.X != 0 && moveDir.Y != 0)
            {
                var horizontalTile = actor.GridPosition + new Vector2I(moveDir.X, 0);
                var verticalTile = actor.GridPosition + new Vector2I(0, moveDir.Y);

                if (map.IsWalkable(horizontalTile))
                {
                    moveDir = new Vector2I(moveDir.X, 0);
                }
                else if (map.IsWalkable(verticalTile))
                {
                    moveDir = new Vector2I(0, moveDir.Y);
                }
                else
                {
                    actor.StopMovement();
                    return;
                }
            }
            else
            {
                actor.StopMovement();
                return;
            }
        }

        actor.SetMoveDirection(moveDir);
        actor.AdvanceMovement(tickDuration);
    }
}
