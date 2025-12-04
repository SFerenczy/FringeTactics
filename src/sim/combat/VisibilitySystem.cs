using Godot;
using System;
using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// Tracks fog-of-war visibility state for the player.
/// Updated each tick based on crew positions and LOS.
/// </summary>
public class VisibilitySystem
{
    private readonly MapState map;
    private readonly VisibilityState[] tileStates;
    private readonly HashSet<Vector2I> currentlyVisible = new();

    public event Action VisibilityChanged;

    public VisibilitySystem(MapState map)
    {
        this.map = map;

        var totalTiles = map.GridSize.X * map.GridSize.Y;
        tileStates = new VisibilityState[totalTiles];

        for (int i = 0; i < totalTiles; i++)
        {
            tileStates[i] = VisibilityState.Unknown;
        }
    }

    /// <summary>
    /// Get the visibility state of a tile.
    /// </summary>
    public VisibilityState GetVisibility(Vector2I pos)
    {
        if (!map.IsInBounds(pos))
        {
            return VisibilityState.Unknown;
        }
        return tileStates[GetIndex(pos)];
    }

    /// <summary>
    /// Check if a tile is currently visible (in LOS of any crew).
    /// </summary>
    public bool IsVisible(Vector2I pos)
    {
        return GetVisibility(pos) == VisibilityState.Visible;
    }

    /// <summary>
    /// Check if a tile has ever been seen (Revealed or Visible).
    /// </summary>
    public bool IsRevealed(Vector2I pos)
    {
        var state = GetVisibility(pos);
        return state == VisibilityState.Visible || state == VisibilityState.Revealed;
    }

    /// <summary>
    /// Update visibility based on current crew positions.
    /// Called each tick by CombatState.
    /// </summary>
    public void UpdateVisibility(IEnumerable<Actor> actors)
    {
        // Mark all currently visible tiles as revealed
        foreach (var pos in currentlyVisible)
        {
            if (map.IsInBounds(pos))
            {
                tileStates[GetIndex(pos)] = VisibilityState.Revealed;
            }
        }
        currentlyVisible.Clear();

        // Calculate new visibility from all crew actors
        foreach (var actor in actors)
        {
            if (actor.Type != "crew" || actor.State != ActorState.Alive)
            {
                continue;
            }

            CalculateActorVisibility(actor);
        }

        // Mark newly visible tiles
        foreach (var pos in currentlyVisible)
        {
            tileStates[GetIndex(pos)] = VisibilityState.Visible;
        }

        VisibilityChanged?.Invoke();
    }

    private void CalculateActorVisibility(Actor actor)
    {
        var origin = actor.GridPosition;
        var radius = actor.GetVisionRadius();

        for (int dy = -radius; dy <= radius; dy++)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                var target = origin + new Vector2I(dx, dy);

                if (!map.IsInBounds(target))
                {
                    continue;
                }

                // Circular radius check
                if (dx * dx + dy * dy > radius * radius)
                {
                    continue;
                }

                if (HasLineOfSight(origin, target))
                {
                    currentlyVisible.Add(target);
                }
            }
        }
    }

    /// <summary>
    /// Check if there's clear line of sight between two positions.
    /// Uses Bresenham's line algorithm.
    /// </summary>
    public bool HasLineOfSight(Vector2I from, Vector2I to)
    {
        if (from == to)
        {
            return true;
        }

        var points = GetLinePoints(from, to);

        // Check intermediate points - can see up to and including blocking tile
        for (int i = 1; i < points.Length; i++)
        {
            var point = points[i];

            if (map.BlocksLOS(point))
            {
                // Can see the blocking tile itself, but not beyond
                return i == points.Length - 1;
            }
        }

        return true;
    }

    private Vector2I[] GetLinePoints(Vector2I from, Vector2I to)
    {
        var points = new List<Vector2I>();

        int x0 = from.X, y0 = from.Y;
        int x1 = to.X, y1 = to.Y;

        int dx = Math.Abs(x1 - x0);
        int dy = Math.Abs(y1 - y0);
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

    private int GetIndex(Vector2I pos)
    {
        return pos.Y * map.GridSize.X + pos.X;
    }

    /// <summary>
    /// Reveal a specific tile (e.g., from hacked camera).
    /// </summary>
    public void RevealTile(Vector2I pos)
    {
        if (!map.IsInBounds(pos))
        {
            return;
        }

        var index = GetIndex(pos);
        if (tileStates[index] == VisibilityState.Unknown)
        {
            tileStates[index] = VisibilityState.Revealed;
            VisibilityChanged?.Invoke();
        }
    }

    /// <summary>
    /// Reveal all tiles (debug/cheat).
    /// </summary>
    public void RevealAll()
    {
        for (int i = 0; i < tileStates.Length; i++)
        {
            if (tileStates[i] == VisibilityState.Unknown)
            {
                tileStates[i] = VisibilityState.Revealed;
            }
        }
        VisibilityChanged?.Invoke();
    }

    /// <summary>
    /// Get all currently visible tiles (for efficient rendering).
    /// </summary>
    public IReadOnlyCollection<Vector2I> GetVisibleTiles()
    {
        return currentlyVisible;
    }
}
