using Godot; // For Vector2I only - no Node/UI types
using System.Collections.Generic;

namespace FringeTactics;

public partial class MapState
{
    public Vector2I GridSize { get; set; } = new Vector2I(12, 10);
    public List<bool> WalkableTiles { get; set; } = new();
    public List<int> CoverFlags { get; set; } = new();
    public Dictionary<Vector2I, string> Interactables { get; set; } = new(); // position -> interactable_id
    public Dictionary<string, List<Vector2I>> SpawnPoints { get; set; } = new() { { "crew", new List<Vector2I>() }, { "enemy", new List<Vector2I>() } };

    public MapState()
    {
        InitializeGrid();
    }

    private void InitializeGrid()
    {
        var totalTiles = GridSize.X * GridSize.Y;
        WalkableTiles = new List<bool>(new bool[totalTiles]);
        CoverFlags = new List<int>(new int[totalTiles]);
        for (int i = 0; i < totalTiles; i++)
        {
            WalkableTiles[i] = true;
            CoverFlags[i] = 0;
        }
    }

    public bool IsWalkable(Vector2I pos)
    {
        var index = pos.Y * GridSize.X + pos.X;
        if (index < 0 || index >= WalkableTiles.Count)
        {
            return false;
        }
        return WalkableTiles[index];
    }

    public int GetCover(Vector2I pos)
    {
        var index = pos.Y * GridSize.X + pos.X;
        if (index < 0 || index >= CoverFlags.Count)
        {
            return 0;
        }
        return CoverFlags[index];
    }
}
