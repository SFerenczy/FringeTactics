using Godot;
using System;

namespace FringeTactics;

/// <summary>
/// 8-directional cover flags. A tile can provide cover from multiple directions.
/// </summary>
[Flags]
public enum CoverDirection : byte
{
    None = 0,
    N  = 1 << 0,  // North (up, -Y)
    NE = 1 << 1,  // Northeast
    E  = 1 << 2,  // East (right, +X)
    SE = 1 << 3,  // Southeast
    S  = 1 << 4,  // South (down, +Y)
    SW = 1 << 5,  // Southwest
    W  = 1 << 6,  // West (left, -X)
    NW = 1 << 7,  // Northwest
    All = 0xFF    // Full cover from all directions
}

/// <summary>
/// Helper methods for cover direction calculations.
/// </summary>
public static class CoverDirectionHelper
{
    private static readonly CoverDirection[] allDirections = new[]
    {
        CoverDirection.N, CoverDirection.NE, CoverDirection.E, CoverDirection.SE,
        CoverDirection.S, CoverDirection.SW, CoverDirection.W, CoverDirection.NW
    };

    /// <summary>
    /// Get all 8 cardinal and diagonal directions.
    /// </summary>
    public static CoverDirection[] AllDirections => allDirections;

    /// <summary>
    /// Get the direction from one grid position to another.
    /// Returns the closest of 8 cardinal/diagonal directions.
    /// </summary>
    public static CoverDirection GetDirection(Vector2I from, Vector2I to)
    {
        var diff = to - from;
        
        var dx = Math.Sign(diff.X);
        var dy = Math.Sign(diff.Y);
        
        return (dx, dy) switch
        {
            (0, -1)  => CoverDirection.N,
            (1, -1)  => CoverDirection.NE,
            (1, 0)   => CoverDirection.E,
            (1, 1)   => CoverDirection.SE,
            (0, 1)   => CoverDirection.S,
            (-1, 1)  => CoverDirection.SW,
            (-1, 0)  => CoverDirection.W,
            (-1, -1) => CoverDirection.NW,
            _ => CoverDirection.None
        };
    }
    
    /// <summary>
    /// Get the opposite direction (for determining what cover protects against).
    /// </summary>
    public static CoverDirection GetOpposite(CoverDirection dir)
    {
        return dir switch
        {
            CoverDirection.N  => CoverDirection.S,
            CoverDirection.NE => CoverDirection.SW,
            CoverDirection.E  => CoverDirection.W,
            CoverDirection.SE => CoverDirection.NW,
            CoverDirection.S  => CoverDirection.N,
            CoverDirection.SW => CoverDirection.NE,
            CoverDirection.W  => CoverDirection.E,
            CoverDirection.NW => CoverDirection.SE,
            _ => CoverDirection.None
        };
    }
    
    /// <summary>
    /// Get the offset vector for a direction.
    /// </summary>
    public static Vector2I GetOffset(CoverDirection dir)
    {
        return dir switch
        {
            CoverDirection.N  => new Vector2I(0, -1),
            CoverDirection.NE => new Vector2I(1, -1),
            CoverDirection.E  => new Vector2I(1, 0),
            CoverDirection.SE => new Vector2I(1, 1),
            CoverDirection.S  => new Vector2I(0, 1),
            CoverDirection.SW => new Vector2I(-1, 1),
            CoverDirection.W  => new Vector2I(-1, 0),
            CoverDirection.NW => new Vector2I(-1, -1),
            _ => Vector2I.Zero
        };
    }
}
