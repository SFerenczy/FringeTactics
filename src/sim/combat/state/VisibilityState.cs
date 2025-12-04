namespace FringeTactics;

/// <summary>
/// Visibility state of a tile from the player's perspective.
/// </summary>
public enum VisibilityState
{
    /// <summary>
    /// Never seen by any crew member. Contents unknown.
    /// </summary>
    Unknown,

    /// <summary>
    /// Was visible at some point, but not currently in LOS.
    /// Player remembers terrain but not dynamic elements (enemies).
    /// </summary>
    Revealed,

    /// <summary>
    /// Currently in LOS of at least one crew member.
    /// All contents visible and targetable.
    /// </summary>
    Visible
}
