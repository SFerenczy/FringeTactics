using Godot; // For Vector2I, Mathf only - no Node/UI types

namespace FringeTactics;

/// <summary>
/// Stateless combat resolution service.
/// Handles hit chance, damage, and line-of-sight checks.
/// </summary>
public static class CombatResolver
{

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
    /// Calculate hit chance based on distance, weapon accuracy, attacker stats, and cover.
    /// </summary>
    public static float CalculateHitChance(Actor attacker, Actor target, WeaponData weapon, MapState map)
    {
        var distance = GetDistance(attacker.GridPosition, target.GridPosition);
        
        // Base accuracy from weapon
        var baseAccuracy = weapon.Accuracy;
        
        // Distance penalty: increases as you approach max range
        var rangeFraction = distance / weapon.Range;
        var distancePenalty = rangeFraction * CombatBalance.RangePenaltyFactor;
        
        // Apply attacker's aim stat bonus (+1% per point)
        var aimBonus = (attacker.Stats.TryGetValue("aim", out var aim) ? aim : 0) * 0.01f;
        
        // Base hit chance before cover
        var hitChance = baseAccuracy * (1f - distancePenalty) + aimBonus;
        
        // Apply cover penalty based on cover height
        if (map != null)
        {
            var coverHeight = map.GetCoverAgainst(target.GridPosition, attacker.GridPosition);
            if (coverHeight != CoverHeight.None && coverHeight != CoverHeight.Full)
            {
                var reduction = CombatBalance.GetCoverReduction(coverHeight);
                hitChance *= (1f - reduction);
            }
        }
        
        // Clamp to valid range
        return Mathf.Clamp(hitChance, CombatBalance.MinHitChance, CombatBalance.MaxHitChance);
    }

    /// <summary>
    /// Calculate hit chance without map (legacy, for tests without cover).
    /// </summary>
    public static float CalculateHitChance(Actor attacker, Actor target, WeaponData weapon)
    {
        return CalculateHitChance(attacker, target, weapon, null);
    }

    /// <summary>
    /// Calculate final damage after armor reduction.
    /// Formula: max(1, rawDamage - armor)
    /// </summary>
    public static int CalculateDamage(int rawDamage, int armor)
    {
        if (rawDamage <= 0) return 0;
        return System.Math.Max(1, rawDamage - armor);
    }

    /// <summary>
    /// Resolve an attack. Returns the result with damage dealt.
    /// Uses distance-based hit chance calculation.
    /// </summary>
    public static AttackResult ResolveAttack(Actor attacker, Actor target, WeaponData weapon, MapState map, RngStream rng)
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
            result.HitChance = 0f;
            return result;
        }

        // Check target's cover height
        var coverHeight = map != null ? map.GetCoverAgainst(target.GridPosition, attacker.GridPosition) : CoverHeight.None;
        result.TargetCoverHeight = coverHeight;
        
        // Calculate hit chance based on distance, accuracy, and cover
        var hitChance = CalculateHitChance(attacker, target, weapon, map);
        result.HitChance = hitChance;
        
        var roll = rng.NextFloat();
        result.Hit = roll < hitChance;

        if (result.Hit)
        {
            result.RawDamage = weapon.Damage;
            result.TargetArmor = target.Armor;
            result.Damage = CalculateDamage(weapon.Damage, target.Armor);
        }
        else
        {
            result.Damage = 0;
            result.RawDamage = 0;
            result.TargetArmor = target.Armor;
        }

        return result;
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
    /// Line-of-sight check using Bresenham's line algorithm.
    /// Checks if path is clear of LOS-blocking tiles (walls, void).
    /// </summary>
    public static bool HasLineOfSight(Vector2I from, Vector2I to, MapState map)
    {
        var points = GetLinePoints(from, to);

        // Skip first (attacker) and last (target) points
        for (int i = 1; i < points.Length - 1; i++)
        {
            if (map.BlocksLOS(points[i]))
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
