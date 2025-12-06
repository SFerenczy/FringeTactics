using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// A choice the player can make at an encounter node.
/// </summary>
public class EncounterOption
{
    /// <summary>
    /// Unique identifier within the node.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Localization key for the option text.
    /// </summary>
    public string TextKey { get; set; }

    /// <summary>
    /// Conditions that must be met for this option to be available.
    /// All conditions must pass (implicit AND).
    /// </summary>
    public List<EncounterCondition> Conditions { get; set; } = new();

    /// <summary>
    /// Outcome when this option is selected (for direct choices without skill checks).
    /// </summary>
    public EncounterOutcome Outcome { get; set; }

    /// <summary>
    /// Skill check definition (EN2). Null means no check required.
    /// </summary>
    public SkillCheckDef SkillCheck { get; set; }

    /// <summary>
    /// Outcome when skill check succeeds (EN2).
    /// </summary>
    public EncounterOutcome SuccessOutcome { get; set; }

    /// <summary>
    /// Outcome when skill check fails (EN2).
    /// </summary>
    public EncounterOutcome FailureOutcome { get; set; }

    /// <summary>
    /// Whether this option requires a skill check.
    /// </summary>
    public bool HasSkillCheck => SkillCheck != null;

    /// <summary>
    /// Get the effective outcome for this option.
    /// For EN1, always returns Outcome. EN2 will handle skill check resolution.
    /// </summary>
    public EncounterOutcome GetOutcome()
    {
        return Outcome;
    }
}
