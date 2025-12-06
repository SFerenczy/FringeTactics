using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// Skill check definition for encounter options.
/// Used by EN2 for crew-based checks.
/// </summary>
public class SkillCheckDef
{
    /// <summary>
    /// Stat type to check against.
    /// </summary>
    public CrewStatType Stat { get; set; }

    /// <summary>
    /// Base difficulty (1-10 scale).
    /// </summary>
    public int Difficulty { get; set; }

    /// <summary>
    /// Traits that provide bonuses to this check.
    /// </summary>
    public List<string> BonusTraits { get; set; } = new();

    /// <summary>
    /// Traits that provide penalties to this check.
    /// </summary>
    public List<string> PenaltyTraits { get; set; } = new();
}
