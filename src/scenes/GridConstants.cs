using Godot;

namespace FringeTactics;

/// <summary>
/// Shared constants for grid rendering across scene files.
/// </summary>
public static class GridConstants
{
    // === Grid Dimensions ===
    
    /// <summary>Size of a single tile in pixels.</summary>
    public const int TileSize = 32;
    
    // === Tile Colors ===
    
    /// <summary>Wall tile color.</summary>
    public static readonly Color WallColor = new(0.35f, 0.35f, 0.4f);
    
    /// <summary>Void tile color (outside map).</summary>
    public static readonly Color VoidColor = new(0.05f, 0.05f, 0.08f);
    
    /// <summary>Floor tile color (light squares in checkerboard).</summary>
    public static readonly Color FloorColorLight = new(0.2f, 0.2f, 0.25f);
    
    /// <summary>Floor tile color (dark squares in checkerboard).</summary>
    public static readonly Color FloorColorDark = new(0.15f, 0.15f, 0.2f);
    
    // === Cover Object Colors (tile rendering) ===
    
    /// <summary>Low cover tile color (debris, low walls).</summary>
    public static readonly Color LowCoverTileColor = new(0.3f, 0.5f, 0.4f);
    
    /// <summary>Half cover tile color (crates, waist-high walls).</summary>
    public static readonly Color HalfCoverTileColor = new(0.4f, 0.45f, 0.35f);
    
    /// <summary>High cover tile color (tall barriers).</summary>
    public static readonly Color HighCoverTileColor = new(0.5f, 0.4f, 0.35f);
    
    // === Cover Indicator Colors (UI overlay) ===
    
    /// <summary>Low cover indicator color.</summary>
    public static readonly Color LowCoverIndicatorColor = new(0.4f, 0.8f, 1.0f, 0.6f);
    
    /// <summary>Half cover indicator color.</summary>
    public static readonly Color HalfCoverIndicatorColor = new(0.2f, 0.6f, 1.0f, 0.7f);
    
    /// <summary>High cover indicator color.</summary>
    public static readonly Color HighCoverIndicatorColor = new(0.1f, 0.3f, 0.8f, 0.8f);
    
    /// <summary>Full cover indicator color (walls).</summary>
    public static readonly Color FullCoverIndicatorColor = new(0.5f, 0.5f, 0.5f, 0.7f);
    
    // === Cover Indicator Dimensions ===
    
    /// <summary>Thickness of cover indicator bars.</summary>
    public const float IndicatorThickness = 4f;
    
    /// <summary>Inset from tile edge for indicators.</summary>
    public const float IndicatorInset = 2f;
    
    /// <summary>Scale multiplier for diagonal corner indicators.</summary>
    public const float DiagonalIndicatorScale = 1.5f;
}
