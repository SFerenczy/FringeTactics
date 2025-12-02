using Godot; // For Vector2I, Mathf only - no Node/UI types
using System;

namespace FringeTactics;

/// <summary>
/// Stateless combat resolution service.
/// Handles hit chance, damage, and line-of-sight checks.
/// </summary>
public static class CombatResolver
{
    public const float BASE_HIT_CHANCE = 0.70f;

    /// <summary>
    /// Check if attacker can attack target with given weapon.
    /// </summary>
    public static bool CanAttack(Actor attacker, Actor target, WeaponData weapon, MapState map)
    {
        if (attacker == null || target == null)
        {
            return false;
        }

        if (attacker.State != ActorState.Alive || target.State != ActorState.Alive)
        {
            return false;
        }

        if (!IsInRange(attacker.GridPosition, target.GridPosition, weapon.Range))
        {
            return false;
        }

        if (!HasLineOfSight(attacker.GridPosition, target.GridPosition, map))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Resolve an attack. Returns the result with damage dealt.
    /// </summary>
    public static AttackResult ResolveAttack(Actor attacker, Actor target, WeaponData weapon, MapState map, Random rng)
    {
        var result = new AttackResult
        {
            AttackerId = attacker.Id,
            TargetId = target.Id,
            WeaponName = weapon.Name
        };

        if (!CanAttack(attacker, target, weapon, map))
        {
            result.Hit = false;
            result.Damage = 0;
            return result;
        }

        // Simple flat hit chance for now
        var roll = rng.NextDouble();
        result.Hit = roll < BASE_HIT_CHANCE;

        if (result.Hit)
        {
            result.Damage = weapon.Damage;
        }
        else
        {
            result.Damage = 0;
        }

        return result;
    }

    /// <summary>
    /// Apply damage to target. Returns true if target died.
    /// </summary>
    public static bool ApplyDamage(Actor target, int damage)
    {
        if (target == null || target.State != ActorState.Alive)
        {
            return false;
        }

        target.Hp -= damage;

        if (target.Hp <= 0)
        {
            target.Hp = 0;
            target.State = ActorState.Dead;
            return true;
        }

        return false;
    }

    public static bool IsInRange(Vector2I from, Vector2I to, int range)
    {
        var distance = GetDistance(from, to);
        return distance <= range;
    }

    public static float GetDistance(Vector2I from, Vector2I to)
    {
        var diff = to - from;
        return Mathf.Sqrt(diff.X * diff.X + diff.Y * diff.Y);
    }

    /// <summary>
    /// Simple line-of-sight check using Bresenham's line algorithm.
    /// For now, just checks if path is clear of non-walkable tiles.
    /// </summary>
    public static bool HasLineOfSight(Vector2I from, Vector2I to, MapState map)
    {
        // Simple implementation: check tiles along the line
        var points = GetLinePoints(from, to);

        // Skip first (attacker) and last (target) points
        for (int i = 1; i < points.Length - 1; i++)
        {
            if (!map.IsWalkable(points[i]))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Bresenham's line algorithm to get points between two grid positions.
    /// </summary>
    private static Vector2I[] GetLinePoints(Vector2I from, Vector2I to)
    {
        var points = new System.Collections.Generic.List<Vector2I>();

        int x0 = from.X, y0 = from.Y;
        int x1 = to.X, y1 = to.Y;

        int dx = Mathf.Abs(x1 - x0);
        int dy = Mathf.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            points.Add(new Vector2I(x0, y0));

            if (x0 == x1 && y0 == y1)
            {
                break;
            }

            int e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x0 += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                y0 += sy;
            }
        }

        return points.ToArray();
    }
}

/// <summary>
/// Result of an attack resolution.
/// </summary>
public struct AttackResult
{
    public int AttackerId { get; set; }
    public int TargetId { get; set; }
    public string WeaponName { get; set; }
    public bool Hit { get; set; }
    public int Damage { get; set; }
}
