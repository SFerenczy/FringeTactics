using Godot;

namespace FringeTactics;

/// <summary>
/// Renders the tactical grid with tile type visualization and map border.
/// One-time setup at mission start.
/// </summary>
public partial class GridRenderer : Node2D
{
    private int tileSize;

    public void Initialize(MapState mapState, int tileSize)
    {
        this.tileSize = tileSize;
        DrawGrid(mapState);
    }

    private void DrawGrid(MapState map)
    {
        var gridSize = map.GridSize;
        
        DrawMapBorder(gridSize);
        
        for (int y = 0; y < gridSize.Y; y++)
        {
            for (int x = 0; x < gridSize.X; x++)
            {
                var pos = new Vector2I(x, y);
                var tile = new ColorRect();
                tile.Size = new Vector2(tileSize - 1, tileSize - 1);
                tile.Position = new Vector2(x * tileSize, y * tileSize);

                tile.Color = GetTileColor(map, pos, x, y);
                
                if (map.IsInEntryZone(pos))
                {
                    tile.Color = ApplyEntryZoneTint(tile.Color);
                }

                AddChild(tile);
            }
        }
    }

    private Color GetTileColor(MapState map, Vector2I pos, int x, int y)
    {
        var tileType = map.GetTileType(pos);
        
        switch (tileType)
        {
            case TileType.Wall:
                return GridConstants.WallColor;
            case TileType.Void:
                return GridConstants.VoidColor;
            case TileType.Floor:
            default:
                var coverHeight = map.GetTileCoverHeight(pos);
                if (coverHeight != CoverHeight.None)
                {
                    return coverHeight switch
                    {
                        CoverHeight.Low => GridConstants.LowCoverTileColor,
                        CoverHeight.Half => GridConstants.HalfCoverTileColor,
                        CoverHeight.High => GridConstants.HighCoverTileColor,
                        _ => GridConstants.WallColor
                    };
                }
                
                return (x + y) % 2 == 0 
                    ? GridConstants.FloorColorDark 
                    : GridConstants.FloorColorLight;
        }
    }

    private Color ApplyEntryZoneTint(Color baseColor)
    {
        var lightened = baseColor.Lightened(0.15f);
        return new Color(
            lightened.R * 0.9f,
            lightened.G * 1.1f,
            lightened.B * 0.9f
        );
    }

    private void DrawMapBorder(Vector2I gridSize)
    {
        var borderWidth = 2;
        var borderColor = new Color(0.4f, 0.45f, 0.5f);
        var mapWidth = gridSize.X * tileSize;
        var mapHeight = gridSize.Y * tileSize;
        
        var top = new ColorRect();
        top.Size = new Vector2(mapWidth + borderWidth * 2, borderWidth);
        top.Position = new Vector2(-borderWidth, -borderWidth);
        top.Color = borderColor;
        AddChild(top);
        
        var bottom = new ColorRect();
        bottom.Size = new Vector2(mapWidth + borderWidth * 2, borderWidth);
        bottom.Position = new Vector2(-borderWidth, mapHeight);
        bottom.Color = borderColor;
        AddChild(bottom);
        
        var left = new ColorRect();
        left.Size = new Vector2(borderWidth, mapHeight);
        left.Position = new Vector2(-borderWidth, 0);
        left.Color = borderColor;
        AddChild(left);
        
        var right = new ColorRect();
        right.Size = new Vector2(borderWidth, mapHeight);
        right.Position = new Vector2(mapWidth, 0);
        right.Color = borderColor;
        AddChild(right);
    }
}
