namespace FringeTactics;

/// <summary>
/// Types of channeled actions.
/// </summary>
public static class ChannelTypes
{
    public const string Hack = "hack";
    public const string Unlock = "unlock";
    public const string DisableHazard = "disable_hazard";
}

/// <summary>
/// Represents a channeled action in progress.
/// </summary>
public class ChanneledAction
{
    public string ActionType { get; set; }
    public int TargetInteractableId { get; set; }
    public int TotalTicks { get; set; }
    public int TicksRemaining { get; set; }
    public bool CanBeInterrupted { get; set; } = true;
    
    /// <summary>
    /// Progress from 0.0 to 1.0.
    /// </summary>
    public float Progress => TotalTicks > 0 
        ? 1f - (TicksRemaining / (float)TotalTicks) 
        : 1f;
    
    /// <summary>
    /// Check if the action is complete.
    /// </summary>
    public bool IsComplete => TicksRemaining <= 0;
    
    public ChanneledAction(string actionType, int targetId, int durationTicks)
    {
        ActionType = actionType;
        TargetInteractableId = targetId;
        TotalTicks = durationTicks;
        TicksRemaining = durationTicks;
    }
    
    /// <summary>
    /// Advance the action by one tick.
    /// </summary>
    public void Tick()
    {
        if (TicksRemaining > 0)
        {
            TicksRemaining--;
        }
    }
}
