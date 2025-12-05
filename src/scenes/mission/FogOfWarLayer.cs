using Godot;
using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// Manages the fog of war overlay. Creates fog tiles for the entire grid
/// and updates their visibility based on the VisibilitySystem.
/// </summary>
public partial class FogOfWarLayer : Node2D
{
    private Dictionary<Vector2I, ColorRect> fogTiles = new();
    private VisibilitySystem visibilitySystem;
    private bool isDirty = true;
    private int tileSize;

    public void Initialize(MapState mapState, VisibilitySystem visibilitySystem, int tileSize)
    {
        this.visibilitySystem = visibilitySystem;
        this.tileSize = tileSize;
        
        ZIndex = 5;
        Name = "FogLayer";
        
        visibilitySystem.VisibilityChanged += OnVisibilityChanged;
        
        CreateFogTiles(mapState.GridSize);
        UpdateVisuals();
    }

    private void CreateFogTiles(Vector2I gridSize)
    {
        for (int y = 0; y < gridSize.Y; y++)
        {
            for (int x = 0; x < gridSize.X; x++)
            {
                var pos = new Vector2I(x, y);
                var fogTile = new ColorRect();
                fogTile.Size = new Vector2(tileSize, tileSize);
                fogTile.Position = new Vector2(x * tileSize, y * tileSize);
                fogTile.MouseFilter = Control.MouseFilterEnum.Ignore;
                AddChild(fogTile);
                fogTiles[pos] = fogTile;
            }
        }
    }

    private void OnVisibilityChanged()
    {
        isDirty = true;
    }

    public void MarkDirty()
    {
        isDirty = true;
    }

    public void UpdateVisuals()
    {
        if (!isDirty)
        {
            return;
        }
        isDirty = false;

        foreach (var kvp in fogTiles)
        {
            var pos = kvp.Key;
            var tile = kvp.Value;
            var visibility = visibilitySystem.GetVisibility(pos);

            switch (visibility)
            {
                case VisibilityState.Unknown:
                    tile.Color = new Color(0.0f, 0.0f, 0.0f, 0.95f);
                    tile.Visible = true;
                    break;
                case VisibilityState.Revealed:
                    tile.Color = new Color(0.0f, 0.0f, 0.0f, 0.5f);
                    tile.Visible = true;
                    break;
                case VisibilityState.Visible:
                    tile.Visible = false;
                    break;
            }
        }
    }

    public void Cleanup()
    {
        if (visibilitySystem != null)
        {
            visibilitySystem.VisibilityChanged -= OnVisibilityChanged;
        }
    }
}
