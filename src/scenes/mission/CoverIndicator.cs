using Godot;
using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// Displays cover indicators on the tactical map.
/// Shows which directions provide cover for selected units.
/// </summary>
public partial class CoverIndicator : Node2D
{
    private MapState map;
    private List<ColorRect> indicators = new();
    
    public void Initialize(MapState mapState)
    {
        map = mapState;
    }
    
    /// <summary>
    /// Show cover indicators for a specific position (e.g., selected unit).
    /// </summary>
    public void ShowCoverFor(Vector2I unitPos)
    {
        ClearIndicators();
        
        if (map == null)
        {
            return;
        }
        
        // Check each direction for cover
        foreach (var dir in CoverDirectionHelper.AllDirections)
        {
            var adjacentPos = unitPos + CoverDirectionHelper.GetOffset(dir);
            if (!map.IsInBounds(adjacentPos))
            {
                continue;
            }
            
            // Check for wall cover (full cover)
            if (map.GetTileType(adjacentPos) == TileType.Wall)
            {
                CreateCoverIndicator(unitPos, dir, CoverHeight.Full);
                continue;
            }
            
            // Check for partial cover objects
            var coverHeight = map.GetTileCoverHeight(adjacentPos);
            if (coverHeight != CoverHeight.None)
            {
                CreateCoverIndicator(unitPos, dir, coverHeight);
            }
        }
    }
    
    /// <summary>
    /// Show cover indicators for multiple positions.
    /// </summary>
    public void ShowCoverForMultiple(IEnumerable<Vector2I> positions)
    {
        ClearIndicators();
        
        if (map == null)
        {
            return;
        }
        
        var processedEdges = new HashSet<(Vector2I, CoverDirection)>();
        
        foreach (var unitPos in positions)
        {
            foreach (var dir in CoverDirectionHelper.AllDirections)
            {
                var edgeKey = (unitPos, dir);
                if (processedEdges.Contains(edgeKey))
                {
                    continue;
                }
                
                var adjacentPos = unitPos + CoverDirectionHelper.GetOffset(dir);
                if (!map.IsInBounds(adjacentPos))
                {
                    continue;
                }
                
                // Check for wall cover (full cover)
                if (map.GetTileType(adjacentPos) == TileType.Wall)
                {
                    CreateCoverIndicator(unitPos, dir, CoverHeight.Full);
                    processedEdges.Add(edgeKey);
                    continue;
                }
                
                // Check for partial cover objects
                var coverHeight = map.GetTileCoverHeight(adjacentPos);
                if (coverHeight != CoverHeight.None)
                {
                    CreateCoverIndicator(unitPos, dir, coverHeight);
                    processedEdges.Add(edgeKey);
                }
            }
        }
    }
    
    private static Color GetColorForHeight(CoverHeight height)
    {
        return height switch
        {
            CoverHeight.Low => GridConstants.LowCoverIndicatorColor,
            CoverHeight.Half => GridConstants.HalfCoverIndicatorColor,
            CoverHeight.High => GridConstants.HighCoverIndicatorColor,
            CoverHeight.Full => GridConstants.FullCoverIndicatorColor,
            _ => GridConstants.HalfCoverIndicatorColor
        };
    }
    
    private void CreateCoverIndicator(Vector2I tilePos, CoverDirection dir, CoverHeight height)
    {
        var indicator = new ColorRect();
        indicator.Color = GetColorForHeight(height);
        indicator.MouseFilter = Control.MouseFilterEnum.Ignore;
        
        var offset = CoverDirectionHelper.GetOffset(dir);
        var tileOrigin = new Vector2(tilePos.X * GridConstants.TileSize, tilePos.Y * GridConstants.TileSize);
        
        var thickness = GridConstants.IndicatorThickness;
        var inset = GridConstants.IndicatorInset;
        var tileSize = GridConstants.TileSize;
        var diagonalSize = thickness * GridConstants.DiagonalIndicatorScale;
        
        // Position indicator bar on the edge of the tile facing the cover source
        if (offset.X != 0 && offset.Y == 0)
        {
            // East or West - vertical bar on side
            indicator.Size = new Vector2(thickness, tileSize - inset * 2);
            if (offset.X > 0)
            {
                // Cover from East - bar on right edge
                indicator.Position = new Vector2(
                    tileOrigin.X + tileSize - thickness - inset,
                    tileOrigin.Y + inset
                );
            }
            else
            {
                // Cover from West - bar on left edge
                indicator.Position = new Vector2(
                    tileOrigin.X + inset,
                    tileOrigin.Y + inset
                );
            }
        }
        else if (offset.Y != 0 && offset.X == 0)
        {
            // North or South - horizontal bar on top/bottom
            indicator.Size = new Vector2(tileSize - inset * 2, thickness);
            if (offset.Y > 0)
            {
                // Cover from South - bar on bottom edge
                indicator.Position = new Vector2(
                    tileOrigin.X + inset,
                    tileOrigin.Y + tileSize - thickness - inset
                );
            }
            else
            {
                // Cover from North - bar on top edge
                indicator.Position = new Vector2(
                    tileOrigin.X + inset,
                    tileOrigin.Y + inset
                );
            }
        }
        else
        {
            // Diagonal - small corner indicator
            indicator.Size = new Vector2(diagonalSize, diagonalSize);
            indicator.Position = new Vector2(
                tileOrigin.X + (offset.X > 0 ? tileSize - diagonalSize - inset : inset),
                tileOrigin.Y + (offset.Y > 0 ? tileSize - diagonalSize - inset : inset)
            );
        }
        
        AddChild(indicator);
        indicators.Add(indicator);
    }
    
    public void ClearIndicators()
    {
        foreach (var indicator in indicators)
        {
            indicator.QueueFree();
        }
        indicators.Clear();
    }
    
    public new void Hide()
    {
        ClearIndicators();
    }
}
