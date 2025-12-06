using System.Collections.Generic;
using System.Linq;

namespace FringeTactics;

/// <summary>
/// Resolves skill checks against crew stats.
/// Stateless service - all inputs explicit.
/// </summary>
public static class SkillCheck
{
    /// <summary>
    /// Bonus granted per matching trait.
    /// </summary>
    public const int TraitBonusAmount = 2;

    /// <summary>
    /// Penalty per matching penalty trait.
    /// </summary>
    public const int TraitPenaltyAmount = 2;

    /// <summary>
    /// Resolve a skill check using the best available crew member.
    /// </summary>
    public static SkillCheckResult Resolve(
        SkillCheckDef check,
        EncounterContext context,
        RngStream rng)
    {
        if (check == null || context == null || rng == null)
        {
            return new SkillCheckResult { Success = false };
        }

        var crew = SelectBestCrew(check, context);
        if (crew == null)
        {
            return new SkillCheckResult
            {
                Success = false,
                Stat = check.Stat,
                Difficulty = check.Difficulty
            };
        }

        return ResolveWithCrew(check, crew, rng);
    }

    /// <summary>
    /// Resolve a skill check with a specific crew member.
    /// </summary>
    public static SkillCheckResult ResolveWithCrew(
        SkillCheckDef check,
        CrewSnapshot crew,
        RngStream rng)
    {
        if (check == null || crew == null || rng == null)
        {
            return new SkillCheckResult { Success = false };
        }

        int roll = rng.NextInt(1, 11);
        int statValue = crew.GetStat(check.Stat);
        var (traitBonus, appliedBonus, appliedPenalty) = CalculateTraitBonus(crew, check);

        int total = roll + statValue + traitBonus;
        bool success = total >= check.Difficulty;

        return new SkillCheckResult
        {
            Success = success,
            Crew = crew,
            Stat = check.Stat,
            Difficulty = check.Difficulty,
            Roll = roll,
            StatValue = statValue,
            TraitBonus = traitBonus,
            AppliedBonusTraits = appliedBonus,
            AppliedPenaltyTraits = appliedPenalty
        };
    }

    /// <summary>
    /// Select the best crew member for a skill check.
    /// Considers base stat + potential trait bonuses.
    /// </summary>
    public static CrewSnapshot SelectBestCrew(SkillCheckDef check, EncounterContext context)
    {
        if (context?.Crew == null || context.Crew.Count == 0)
            return null;

        return context.Crew
            .OrderByDescending(c => GetEffectiveCheckValue(c, check))
            .FirstOrDefault();
    }

    /// <summary>
    /// Calculate effective value for crew selection (stat + trait bonus).
    /// </summary>
    public static int GetEffectiveCheckValue(CrewSnapshot crew, SkillCheckDef check)
    {
        if (crew == null || check == null) return 0;

        int statValue = crew.GetStat(check.Stat);
        var (traitBonus, _, _) = CalculateTraitBonus(crew, check);
        return statValue + traitBonus;
    }

    /// <summary>
    /// Calculate trait bonus/penalty for a check.
    /// Returns (netBonus, appliedBonusTraits, appliedPenaltyTraits).
    /// </summary>
    public static (int bonus, List<string> bonusTraits, List<string> penaltyTraits)
        CalculateTraitBonus(CrewSnapshot crew, SkillCheckDef check)
    {
        var appliedBonus = new List<string>();
        var appliedPenalty = new List<string>();
        int bonus = 0;

        if (crew?.TraitIds == null || check == null)
            return (0, appliedBonus, appliedPenalty);

        foreach (var traitId in check.BonusTraits ?? new List<string>())
        {
            if (crew.TraitIds.Contains(traitId))
            {
                bonus += TraitBonusAmount;
                appliedBonus.Add(traitId);
            }
        }

        foreach (var traitId in check.PenaltyTraits ?? new List<string>())
        {
            if (crew.TraitIds.Contains(traitId))
            {
                bonus -= TraitPenaltyAmount;
                appliedPenalty.Add(traitId);
            }
        }

        return (bonus, appliedBonus, appliedPenalty);
    }

    /// <summary>
    /// Preview the success chance for a skill check (for UI).
    /// Returns percentage (0-100).
    /// </summary>
    public static int GetSuccessChance(SkillCheckDef check, EncounterContext context)
    {
        var crew = SelectBestCrew(check, context);
        if (crew == null) return 0;

        return GetSuccessChanceWithCrew(check, crew);
    }

    /// <summary>
    /// Calculate success chance for a specific crew member.
    /// </summary>
    public static int GetSuccessChanceWithCrew(SkillCheckDef check, CrewSnapshot crew)
    {
        if (check == null || crew == null) return 0;

        int statValue = crew.GetStat(check.Stat);
        var (traitBonus, _, _) = CalculateTraitBonus(crew, check);
        int baseValue = statValue + traitBonus;

        int neededRoll = check.Difficulty - baseValue;

        if (neededRoll <= 1) return 100;
        if (neededRoll > 10) return 0;

        return (11 - neededRoll) * 10;
    }
}
