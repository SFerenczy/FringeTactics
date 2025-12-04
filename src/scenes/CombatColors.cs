using Godot;

namespace FringeTactics;

/// <summary>
/// Shared color constants for combat UI elements.
/// Centralizes colors to prevent divergence across widgets.
/// </summary>
public static class CombatColors
{
    // Detection indicator colors
    public static readonly Color DetectionIdle = new(0.5f, 0.5f, 0.5f, 0.8f);
    public static readonly Color DetectionAlerted = new(1.0f, 0.2f, 0.2f, 0.9f);
    
    // Alarm state colors
    public static readonly Color AlarmQuietText = new(0.5f, 1.0f, 0.5f);
    public static readonly Color AlarmQuietBackground = new(0.0f, 0.2f, 0.0f, 0.8f);
    public static readonly Color AlarmAlertedText = new(1.0f, 0.3f, 0.3f);
    public static readonly Color AlarmAlertedBackground = new(0.3f, 0.0f, 0.0f, 0.8f);
    
    // Entry zone highlight
    public static readonly Color EntryZoneHighlight = new(0.2f, 0.9f, 0.3f, 0.35f);
}
