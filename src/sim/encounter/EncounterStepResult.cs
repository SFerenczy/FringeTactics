namespace FringeTactics;

/// <summary>
/// Result of an encounter step.
/// </summary>
public class EncounterStepResult
{
    /// <summary>
    /// Whether the step was successful.
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Current node ID after the step.
    /// </summary>
    public string CurrentNodeId { get; set; }

    /// <summary>
    /// Whether the encounter is complete after this step.
    /// </summary>
    public bool IsComplete { get; set; }

    /// <summary>
    /// Error message if step failed.
    /// </summary>
    public string ErrorMessage { get; set; }

    /// <summary>
    /// Create a successful result.
    /// </summary>
    public static EncounterStepResult Success(string nodeId, bool complete) => new()
    {
        IsSuccess = true,
        CurrentNodeId = nodeId,
        IsComplete = complete
    };

    /// <summary>
    /// Create an invalid/error result.
    /// </summary>
    public static EncounterStepResult Invalid(string error) => new()
    {
        IsSuccess = false,
        ErrorMessage = error
    };
}
