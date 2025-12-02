using Godot;
using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// Calculates formation positions for group movement.
/// Stateless utility - all methods are pure functions.
/// </summary>
public static class FormationCalculator
{
    /// <summary>
    /// Calculate destinations for a group of actors moving to a target position.
    /// Maintains relative formation around the group centroid.
    /// </summary>
    public static Dictionary<int, Vector2I> CalculateGroupDestinations(
        List<Actor> actors,
        Vector2I targetPos,
        MapState map)
    {
        var destinations = new Dictionary<int, Vector2I>();

        if (actors.Count == 0)
            return destinations;

        if (actors.Count == 1)
        {
            destinations[actors[0].Id] = FindNearestWalkable(targetPos, map, destinations.Values);
            return destinations;
        }

        // Calculate current centroid
        var centroid = CalculateCentroid(actors);

        // Calculate offset for each actor from centroid
        foreach (var actor in actors)
        {
            var offset = actor.GridPosition - centroid;
            var idealDest = targetPos + offset;

            // Find valid destination (walkable and not already taken)
            var validDest = FindNearestWalkable(idealDest, map, destinations.Values);
            destinations[actor.Id] = validDest;
        }

        return destinations;
    }

    /// <summary>
    /// Calculate destinations in a tight cluster around target.
    /// Used when formation would spread units too far.
    /// </summary>
    public static Dictionary<int, Vector2I> CalculateClusterDestinations(
        List<Actor> actors,
        Vector2I targetPos,
        MapState map)
    {
        var destinations = new Dictionary<int, Vector2I>();
        var occupied = new HashSet<Vector2I>();

        // Spiral outward from target
        foreach (var actor in actors)
        {
            var pos = FindNearestWalkableSpiral(targetPos, map, occupied);
            destinations[actor.Id] = pos;
            occupied.Add(pos);
        }

        return destinations;
    }

    private static Vector2I CalculateCentroid(List<Actor> actors)
    {
        if (actors.Count == 0)
            return Vector2I.Zero;

        int sumX = 0, sumY = 0;
        foreach (var actor in actors)
        {
            sumX += actor.GridPosition.X;
            sumY += actor.GridPosition.Y;
        }

        return new Vector2I(sumX / actors.Count, sumY / actors.Count);
    }

    private static Vector2I FindNearestWalkable(
        Vector2I target,
        MapState map,
        IEnumerable<Vector2I> alreadyTaken)
    {
        var taken = new HashSet<Vector2I>(alreadyTaken);

        if (map.IsWalkable(target) && !taken.Contains(target))
            return target;

        // Search in expanding rings
        for (int radius = 1; radius <= 10; radius++)
        {
            foreach (var pos in GetRingPositions(target, radius))
            {
                if (map.IsWalkable(pos) && !taken.Contains(pos))
                    return pos;
            }
        }

        return target; // Fallback
    }

    private static Vector2I FindNearestWalkableSpiral(
        Vector2I center,
        MapState map,
        HashSet<Vector2I> occupied)
    {
        if (map.IsWalkable(center) && !occupied.Contains(center))
            return center;

        // Search in expanding rings
        for (int radius = 1; radius <= 10; radius++)
        {
            foreach (var pos in GetRingPositions(center, radius))
            {
                if (map.IsWalkable(pos) && !occupied.Contains(pos))
                    return pos;
            }
        }

        return center; // Fallback
    }

    private static IEnumerable<Vector2I> GetRingPositions(Vector2I center, int radius)
    {
        // Top and bottom edges
        for (int x = -radius; x <= radius; x++)
        {
            yield return center + new Vector2I(x, -radius);
            yield return center + new Vector2I(x, radius);
        }
        // Left and right edges (excluding corners already covered)
        for (int y = -radius + 1; y < radius; y++)
        {
            yield return center + new Vector2I(-radius, y);
            yield return center + new Vector2I(radius, y);
        }
    }
}
