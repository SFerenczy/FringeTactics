using Godot;

namespace FringeTactics;

/// <summary>
/// Utility methods for grid-based calculations.
/// </summary>
public static class GridUtils
{
    /// <summary>
    /// Get a single-step direction vector from one position toward another.
    /// Each component is clamped to -1, 0, or 1.
    /// </summary>
    public static Vector2I GetStepDirection(Vector2I from, Vector2I to)
    {
        var diff = to - from;
        return new Vector2I(
            Mathf.Clamp(diff.X, -1, 1),
            Mathf.Clamp(diff.Y, -1, 1)
        );
    }
}
