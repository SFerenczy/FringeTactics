using Godot; // For Vector2I only - no Node/UI types
using System;
using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// Factory for creating MapState from various sources.
/// Separates map construction from MapState itself.
/// </summary>
public static class MapBuilder
{
    /// <summary>
    /// Build a simple test map with walls around the edges.
    /// </summary>
    public static MapState BuildTestMap(Vector2I size)
    {
        var map = new MapState(size);
        
        // Add walls around the perimeter
        for (int x = 0; x < size.X; x++)
        {
            map.SetTile(new Vector2I(x, 0), TileType.Wall);
            map.SetTile(new Vector2I(x, size.Y - 1), TileType.Wall);
        }
        for (int y = 0; y < size.Y; y++)
        {
            map.SetTile(new Vector2I(0, y), TileType.Wall);
            map.SetTile(new Vector2I(size.X - 1, y), TileType.Wall);
        }
        
        // Default entry zone in bottom-left corner
        map.EntryZone.Add(new Vector2I(1, 1));
        map.EntryZone.Add(new Vector2I(2, 1));
        map.EntryZone.Add(new Vector2I(1, 2));
        map.EntryZone.Add(new Vector2I(2, 2));
        
        GenerateCoverFromWalls(map);
        return map;
    }

    /// <summary>
    /// Build a map from a string template.
    /// 
    /// Template characters:
    /// - '.' = Floor
    /// - '#' = Wall
    /// - 'E' = Entry zone (floor tile marked as entry)
    /// - ' ' = Void (outside map)
    /// - '-' = Low cover (0.25 height, 15% hit reduction)
    /// - '=' = Half cover (0.50 height, 30% hit reduction)
    /// - '+' = High cover (0.75 height, 45% hit reduction)
    /// - 'D' = Door (closed)
    /// - 'L' = Locked door
    /// - 'T' = Terminal
    /// - 'X' = Explosive hazard
    /// 
    /// Example:
    /// var template = new string[] {
    ///     "##########",
    ///     "#........#",
    ///     "#..EE.-..#",  // Low cover
    ///     "#..EE.=..#",  // Half cover
    ///     "#.....+..#",  // High cover
    ///     "##########"
    /// };
    /// </summary>
    public static MapState BuildFromTemplate(string[] rows, InteractionSystem interactions = null)
    {
        if (rows == null || rows.Length == 0)
        {
            throw new ArgumentException("Template cannot be null or empty");
        }

        // Determine grid size from template
        int height = rows.Length;
        int width = 0;
        foreach (var row in rows)
        {
            if (row.Length > width)
            {
                width = row.Length;
            }
        }

        var map = new MapState(new Vector2I(width, height));

        // Parse template
        for (int y = 0; y < height; y++)
        {
            var row = rows[y];
            for (int x = 0; x < width; x++)
            {
                var pos = new Vector2I(x, y);
                char c = x < row.Length ? row[x] : ' ';

                switch (c)
                {
                    case '.':
                        map.SetTile(pos, TileType.Floor);
                        break;
                    case '#':
                        map.SetTile(pos, TileType.Wall);
                        break;
                    case 'E':
                        map.SetTile(pos, TileType.Floor);
                        map.EntryZone.Add(pos);
                        break;
                    case '-':
                        map.SetTile(pos, TileType.Floor);
                        map.SetTileCoverHeight(pos, CoverHeight.Low);
                        break;
                    case '=':
                        map.SetTile(pos, TileType.Floor);
                        map.SetTileCoverHeight(pos, CoverHeight.Half);
                        break;
                    case '+':
                        map.SetTile(pos, TileType.Floor);
                        map.SetTileCoverHeight(pos, CoverHeight.High);
                        break;
                    case 'D':
                        // Door (closed) - floor tile with interactable
                        map.SetTile(pos, TileType.Floor);
                        if (interactions != null)
                        {
                            interactions.AddInteractable(InteractableTypes.Door, pos);
                        }
                        break;
                    case 'L':
                        // Locked door - floor tile with locked door interactable
                        map.SetTile(pos, TileType.Floor);
                        if (interactions != null)
                        {
                            var lockedDoor = interactions.AddInteractable(InteractableTypes.Door, pos,
                                new Dictionary<string, object> { { "hackDifficulty", 40 } });
                            lockedDoor.SetState(InteractableState.DoorLocked);
                        }
                        break;
                    case 'T':
                        // Terminal - floor tile with terminal interactable
                        map.SetTile(pos, TileType.Floor);
                        if (interactions != null)
                        {
                            interactions.AddInteractable(InteractableTypes.Terminal, pos,
                                new Dictionary<string, object> { { "hackDifficulty", 60 } });
                        }
                        break;
                    case 'X':
                        // Explosive hazard - floor tile with hazard interactable
                        map.SetTile(pos, TileType.Floor);
                        if (interactions != null)
                        {
                            interactions.AddInteractable(InteractableTypes.Hazard, pos,
                                new Dictionary<string, object>
                                {
                                    { "hazardType", "explosive" },
                                    { "damage", 50 },
                                    { "radius", 2 },
                                    { "disableDifficulty", 30 }
                                });
                        }
                        break;
                    case ' ':
                    default:
                        map.SetTile(pos, TileType.Void);
                        break;
                }
            }
        }

        GenerateCoverFromWalls(map);
        return map;
    }

    /// <summary>
    /// Build a map from a MissionConfig.
    /// Uses template if provided, otherwise creates a basic test map.
    /// </summary>
    public static MapState BuildFromConfig(MissionConfig config, InteractionSystem interactions = null)
    {
        MapState map;

        if (config.MapTemplate != null && config.MapTemplate.Length > 0)
        {
            map = BuildFromTemplate(config.MapTemplate, interactions);
        }
        else
        {
            map = BuildTestMap(config.GridSize);
        }

        // Override entry zone if explicitly specified in config
        if (config.EntryZone != null && config.EntryZone.Count > 0)
        {
            map.EntryZone.Clear();
            foreach (var pos in config.EntryZone)
            {
                map.EntryZone.Add(pos);
            }
        }

        return map;
    }

    /// <summary>
    /// Create an empty room with walls on all sides.
    /// Useful for quick test scenarios.
    /// </summary>
    public static MapState BuildEmptyRoom(int width, int height)
    {
        return BuildTestMap(new Vector2I(width, height));
    }

    /// <summary>
    /// Add a rectangular wall section to an existing map.
    /// </summary>
    public static void AddWallRect(MapState map, Vector2I topLeft, Vector2I size)
    {
        for (int y = topLeft.Y; y < topLeft.Y + size.Y; y++)
        {
            for (int x = topLeft.X; x < topLeft.X + size.X; x++)
            {
                map.SetTile(new Vector2I(x, y), TileType.Wall);
            }
        }
    }

    /// <summary>
    /// Add a horizontal wall line to an existing map.
    /// </summary>
    public static void AddHorizontalWall(MapState map, int y, int startX, int endX)
    {
        for (int x = startX; x <= endX; x++)
        {
            map.SetTile(new Vector2I(x, y), TileType.Wall);
        }
    }

    /// <summary>
    /// Add a vertical wall line to an existing map.
    /// </summary>
    public static void AddVerticalWall(MapState map, int x, int startY, int endY)
    {
        for (int y = startY; y <= endY; y++)
        {
            map.SetTile(new Vector2I(x, y), TileType.Wall);
        }
    }

    /// <summary>
    /// Create a doorway (floor tile) in a wall.
    /// </summary>
    public static void AddDoorway(MapState map, Vector2I pos)
    {
        map.SetTile(pos, TileType.Floor);
    }

    /// <summary>
    /// Generate cover data based on wall placement.
    /// Walls provide cover to adjacent floor tiles.
    /// </summary>
    public static void GenerateCoverFromWalls(MapState map)
    {
        for (int y = 0; y < map.GridSize.Y; y++)
        {
            for (int x = 0; x < map.GridSize.X; x++)
            {
                var pos = new Vector2I(x, y);
                
                if (map.GetTileType(pos) != TileType.Wall)
                {
                    continue;
                }
                
                // Wall provides cover facing each adjacent floor tile
                var coverDirs = CoverDirection.None;
                
                foreach (var dir in CoverDirectionHelper.AllDirections)
                {
                    var adjacentPos = pos + CoverDirectionHelper.GetOffset(dir);
                    if (map.IsInBounds(adjacentPos) && map.GetTileType(adjacentPos) == TileType.Floor)
                    {
                        coverDirs |= dir;
                    }
                }
                
                map.SetTileCover(pos, coverDirs);
            }
        }
    }
}
