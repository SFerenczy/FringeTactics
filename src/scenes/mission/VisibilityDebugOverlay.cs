using Godot;
using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// Debug overlay showing visibility state per tile.
/// Green = Visible, Yellow = Revealed, Red = Unknown.
/// Toggle with F3.
/// </summary>
public partial class VisibilityDebugOverlay : Node2D
{
    private Dictionary<Vector2I, ColorRect> debugTiles = new();
    private VisibilitySystem visibilitySystem;
    private int tileSize;
    private bool isInitialized = false;

    public new bool IsVisible => Visible;

    public void Initialize(MapState mapState, VisibilitySystem visibilitySystem, int tileSize)
    {
        this.visibilitySystem = visibilitySystem;
        this.tileSize = tileSize;
        
        ZIndex = 10;
        Name = "VisibilityDebugLayer";
        Visible = false;
        
        CreateDebugTiles(mapState.GridSize);
        isInitialized = true;
    }

    private void CreateDebugTiles(Vector2I gridSize)
    {
        for (int y = 0; y < gridSize.Y; y++)
        {
            for (int x = 0; x < gridSize.X; x++)
            {
                var pos = new Vector2I(x, y);
                var debugTile = new ColorRect();
                debugTile.Size = new Vector2(8, 8);
                debugTile.Position = new Vector2(x * tileSize + 2, y * tileSize + 2);
                debugTile.MouseFilter = Control.MouseFilterEnum.Ignore;
                AddChild(debugTile);
                debugTiles[pos] = debugTile;
            }
        }
    }

    public void Toggle()
    {
        Visible = !Visible;
        
        if (Visible)
        {
            UpdateVisuals();
        }
        
        GD.Print($"[Debug] Visibility overlay: {(Visible ? "ON" : "OFF")}");
    }

    public void UpdateVisuals()
    {
        if (!Visible || !isInitialized)
        {
            return;
        }

        foreach (var kvp in debugTiles)
        {
            var pos = kvp.Key;
            var tile = kvp.Value;
            var visibility = visibilitySystem.GetVisibility(pos);

            tile.Color = visibility switch
            {
                VisibilityState.Visible => new Color(0.0f, 1.0f, 0.0f, 0.8f),
                VisibilityState.Revealed => new Color(1.0f, 1.0f, 0.0f, 0.8f),
                VisibilityState.Unknown => new Color(1.0f, 0.0f, 0.0f, 0.8f),
                _ => Colors.White
            };
        }
    }
}
