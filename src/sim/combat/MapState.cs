using Godot; // For Vector2I only - no Node/UI types
using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// Tile types for the tactical map.
/// </summary>
public enum TileType
{
    Floor,  // Walkable, doesn't block LOS
    Wall,   // Not walkable, blocks LOS
    Void    // Not walkable, not visible (outside map bounds)
}

/// <summary>
/// Represents the state of a tactical mission map.
/// Contains tile data, cover, interactables, and zones.
/// </summary>
public partial class MapState
{
    public Vector2I GridSize { get; private set; } = new Vector2I(12, 10);
    
    // Tile data
    private List<TileType> tiles = new();
    
    // Cover directions provided by each tile (8-directional)
    private List<CoverDirection> coverData = new();
    
    // Interactables on the map
    public Dictionary<Vector2I, string> Interactables { get; set; } = new();
    
    // Spawn points by type
    public Dictionary<string, List<Vector2I>> SpawnPoints { get; set; } = new() 
    { 
        { "crew", new List<Vector2I>() }, 
        { "enemy", new List<Vector2I>() } 
    };
    
    // Entry zone - tiles where crew can spawn and retreat to
    public List<Vector2I> EntryZone { get; set; } = new();

    /// <summary>
    /// Create an empty map state. Use MapBuilder to populate.
    /// </summary>
    public MapState()
    {
        InitializeGrid(GridSize);
    }

    /// <summary>
    /// Create a map state with specific size.
    /// </summary>
    public MapState(Vector2I gridSize)
    {
        InitializeGrid(gridSize);
    }

    /// <summary>
    /// Initialize the grid with all floor tiles.
    /// </summary>
    private void InitializeGrid(Vector2I size)
    {
        GridSize = size;
        var totalTiles = size.X * size.Y;
        tiles = new List<TileType>(totalTiles);
        coverData = new List<CoverDirection>(totalTiles);
        
        for (int i = 0; i < totalTiles; i++)
        {
            tiles.Add(TileType.Floor);
            coverData.Add(CoverDirection.None);
        }
    }

    /// <summary>
    /// Resize the map. Clears all existing tile data.
    /// </summary>
    public void Resize(Vector2I newSize)
    {
        InitializeGrid(newSize);
        EntryZone.Clear();
        Interactables.Clear();
        SpawnPoints["crew"].Clear();
        SpawnPoints["enemy"].Clear();
    }

    /// <summary>
    /// Check if a position is within map bounds.
    /// </summary>
    public bool IsInBounds(Vector2I pos)
    {
        return pos.X >= 0 && pos.X < GridSize.X && pos.Y >= 0 && pos.Y < GridSize.Y;
    }

    /// <summary>
    /// Get the tile index for a position.
    /// </summary>
    private int GetIndex(Vector2I pos)
    {
        return pos.Y * GridSize.X + pos.X;
    }

    /// <summary>
    /// Get the tile type at a position.
    /// </summary>
    public TileType GetTileType(Vector2I pos)
    {
        if (!IsInBounds(pos))
        {
            return TileType.Void;
        }
        return tiles[GetIndex(pos)];
    }

    /// <summary>
    /// Set the tile type at a position.
    /// </summary>
    public void SetTile(Vector2I pos, TileType type)
    {
        if (!IsInBounds(pos))
        {
            return;
        }
        tiles[GetIndex(pos)] = type;
    }

    /// <summary>
    /// Check if a tile is walkable (Floor tiles only).
    /// </summary>
    public bool IsWalkable(Vector2I pos)
    {
        return GetTileType(pos) == TileType.Floor;
    }

    /// <summary>
    /// Check if a tile blocks line of sight.
    /// </summary>
    public bool BlocksLOS(Vector2I pos)
    {
        var type = GetTileType(pos);
        return type == TileType.Wall || type == TileType.Void;
    }

    /// <summary>
    /// Check if a position is in the entry zone.
    /// </summary>
    public bool IsInEntryZone(Vector2I pos)
    {
        return EntryZone.Contains(pos);
    }

    /// <summary>
    /// Get cover directions provided by a tile.
    /// This is the cover the tile PROVIDES, not the cover a unit ON this tile receives.
    /// </summary>
    public CoverDirection GetTileCover(Vector2I pos)
    {
        if (!IsInBounds(pos))
        {
            return CoverDirection.None;
        }
        return coverData[GetIndex(pos)];
    }

    /// <summary>
    /// Set cover directions provided by a tile.
    /// </summary>
    public void SetTileCover(Vector2I pos, CoverDirection cover)
    {
        if (!IsInBounds(pos))
        {
            return;
        }
        coverData[GetIndex(pos)] = cover;
    }

    /// <summary>
    /// Check if a unit at targetPos has cover against an attack from attackerPos.
    /// Checks adjacent tiles for cover that blocks the attack direction.
    /// </summary>
    public bool HasCoverAgainst(Vector2I targetPos, Vector2I attackerPos)
    {
        // Direction from attacker to target
        var attackDir = CoverDirectionHelper.GetDirection(attackerPos, targetPos);
        if (attackDir == CoverDirection.None)
        {
            return false;
        }
        
        // Cover needs to block from the direction the attack is coming from
        var coverNeeded = CoverDirectionHelper.GetOpposite(attackDir);
        
        // Check adjacent tiles for cover
        foreach (var dir in CoverDirectionHelper.AllDirections)
        {
            var adjacentPos = targetPos + CoverDirectionHelper.GetOffset(dir);
            if (!IsInBounds(adjacentPos))
            {
                continue;
            }
            
            var adjacentCover = GetTileCover(adjacentPos);
            if (adjacentCover == CoverDirection.None)
            {
                continue;
            }
            
            // Adjacent tile provides cover if:
            // 1. It has cover facing toward the target (opposite of dir)
            // 2. That cover direction matches where the attack is coming from
            var coverFacing = CoverDirectionHelper.GetOpposite(dir);
            if ((adjacentCover & coverFacing) != 0 && coverFacing == coverNeeded)
            {
                return true;
            }
        }
        
        return false;
    }

    // Legacy compatibility
    public int GetCover(Vector2I pos) => (int)GetTileCover(pos);
    public void SetCover(Vector2I pos, int coverFlags) => SetTileCover(pos, (CoverDirection)coverFlags);
}
