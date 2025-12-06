using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// Result of a skill check resolution.
/// Contains all information needed for UI display and logging.
/// Immutable after creation.
/// </summary>
public class SkillCheckResult
{
    /// <summary>
    /// Margin threshold for critical success/failure.
    /// </summary>
    public const int CriticalThreshold = 5;

    /// <summary>
    /// Whether the check succeeded.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// The crew member who performed the check.
    /// </summary>
    public CrewSnapshot Crew { get; init; }

    /// <summary>
    /// The stat used for the check.
    /// </summary>
    public CrewStatType Stat { get; init; }

    /// <summary>
    /// Base difficulty of the check.
    /// </summary>
    public int Difficulty { get; init; }

    /// <summary>
    /// The d10 roll result (1-10).
    /// </summary>
    public int Roll { get; init; }

    /// <summary>
    /// Crew's effective stat value.
    /// </summary>
    public int StatValue { get; init; }

    /// <summary>
    /// Bonus from matching traits.
    /// </summary>
    public int TraitBonus { get; init; }

    /// <summary>
    /// Total = Roll + StatValue + TraitBonus.
    /// </summary>
    public int Total => Roll + StatValue + TraitBonus;

    /// <summary>
    /// Margin = Total - Difficulty. Positive = success margin, negative = failure margin.
    /// </summary>
    public int Margin => Total - Difficulty;

    /// <summary>
    /// Whether this was a critical success (margin >= CriticalThreshold).
    /// </summary>
    public bool IsCriticalSuccess => Success && Margin >= CriticalThreshold;

    /// <summary>
    /// Whether this was a critical failure (margin <= -CriticalThreshold).
    /// </summary>
    public bool IsCriticalFailure => !Success && Margin <= -CriticalThreshold;

    /// <summary>
    /// Traits that contributed to the bonus.
    /// </summary>
    public List<string> AppliedBonusTraits { get; init; } = new();

    /// <summary>
    /// Traits that contributed to the penalty.
    /// </summary>
    public List<string> AppliedPenaltyTraits { get; init; } = new();
}
