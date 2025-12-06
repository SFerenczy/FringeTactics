using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// Outcome of selecting an encounter option or succeeding/failing a skill check.
/// Contains effects to apply and optional node transition.
/// </summary>
public class EncounterOutcome
{
    /// <summary>
    /// Effects to accumulate when this outcome is triggered.
    /// </summary>
    public List<EncounterEffect> Effects { get; set; } = new();

    /// <summary>
    /// Node to transition to after this outcome. Null means stay at current node.
    /// </summary>
    public string NextNodeId { get; set; }

    /// <summary>
    /// If true, the encounter ends after this outcome.
    /// </summary>
    public bool IsEndEncounter { get; set; }

    // === Factory Methods ===

    /// <summary>
    /// Create an outcome that ends the encounter.
    /// </summary>
    public static EncounterOutcome End() => new()
    {
        IsEndEncounter = true
    };

    /// <summary>
    /// Create an outcome that ends the encounter with effects.
    /// </summary>
    public static EncounterOutcome EndWith(params EncounterEffect[] effects) => new()
    {
        Effects = new List<EncounterEffect>(effects),
        IsEndEncounter = true
    };

    /// <summary>
    /// Create an outcome that transitions to another node.
    /// </summary>
    public static EncounterOutcome Goto(string nodeId) => new()
    {
        NextNodeId = nodeId
    };

    /// <summary>
    /// Create an outcome that transitions to another node with effects.
    /// </summary>
    public static EncounterOutcome GotoWith(string nodeId, params EncounterEffect[] effects) => new()
    {
        NextNodeId = nodeId,
        Effects = new List<EncounterEffect>(effects)
    };
}
