namespace FringeTactics;

/// <summary>
/// Interface for mission objectives that can be evaluated each tick.
/// Uses ObjectiveStatus from MissionOutput.cs.
/// </summary>
public interface IObjective
{
    /// <summary>
    /// Unique identifier for this objective instance.
    /// </summary>
    string Id { get; }
    
    /// <summary>
    /// Player-facing description.
    /// </summary>
    string Description { get; }
    
    /// <summary>
    /// Whether this is a primary (required for victory) or secondary (bonus) objective.
    /// </summary>
    bool IsPrimary { get; }
    
    /// <summary>
    /// Whether this objective can be completed (victory condition) or only failed (failure condition).
    /// Failure conditions stay InProgress while active and only transition to Failed.
    /// </summary>
    bool IsFailureCondition { get; }
    
    /// <summary>
    /// Current status of the objective.
    /// </summary>
    ObjectiveStatus Status { get; }
    
    /// <summary>
    /// Evaluate the objective against current combat state.
    /// Returns the new status.
    /// </summary>
    ObjectiveStatus Evaluate(CombatState state);
    
    /// <summary>
    /// Get progress text for UI display (e.g., "3/5 enemies killed").
    /// </summary>
    string GetProgressText(CombatState state);
}
