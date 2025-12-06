using System.Collections.Generic;
using System.Linq;

namespace FringeTactics;

/// <summary>
/// Condition for encounter option visibility/availability.
/// Evaluates against EncounterContext to determine if an option is available.
/// </summary>
public class EncounterCondition
{
    public ConditionType Type { get; set; }

    /// <summary>
    /// Target identifier: resource type, trait id, faction id, tag, stat type, flag id.
    /// </summary>
    public string TargetId { get; set; }

    /// <summary>
    /// Threshold value for resource/rep/stat checks.
    /// </summary>
    public int Threshold { get; set; }

    /// <summary>
    /// Child condition for Not type.
    /// </summary>
    public EncounterCondition ChildCondition { get; set; }

    /// <summary>
    /// Child conditions for And/Or types.
    /// </summary>
    public List<EncounterCondition> ChildConditions { get; set; }

    /// <summary>
    /// Evaluate this condition against the given context.
    /// </summary>
    public bool Evaluate(EncounterContext context)
    {
        if (context == null) return false;

        return Type switch
        {
            ConditionType.HasResource => context.GetResource(TargetId) >= Threshold,
            ConditionType.HasTrait => context.HasCrewWithTrait(TargetId),
            ConditionType.HasCargo => context.CargoValue >= Threshold,
            ConditionType.FactionRep => context.GetFactionRep(TargetId) >= Threshold,
            ConditionType.SystemTag => context.SystemTags?.Contains(TargetId) ?? false,
            ConditionType.CrewStat => context.GetBestCrewStat(TargetId) >= Threshold,
            ConditionType.HasFlag => context.HasFlag(TargetId),
            ConditionType.Not => ChildCondition != null && !ChildCondition.Evaluate(context),
            ConditionType.And => ChildConditions?.All(c => c.Evaluate(context)) ?? true,
            ConditionType.Or => ChildConditions?.Any(c => c.Evaluate(context)) ?? false,
            _ => true
        };
    }

    // === Factory Methods ===

    public static EncounterCondition HasCredits(int min) => new()
    {
        Type = ConditionType.HasResource,
        TargetId = ResourceTypes.Money,
        Threshold = min
    };

    public static EncounterCondition HasFuel(int min) => new()
    {
        Type = ConditionType.HasResource,
        TargetId = ResourceTypes.Fuel,
        Threshold = min
    };

    public static EncounterCondition HasTrait(string traitId) => new()
    {
        Type = ConditionType.HasTrait,
        TargetId = traitId
    };

    public static EncounterCondition HasCargoValue(int minValue) => new()
    {
        Type = ConditionType.HasCargo,
        Threshold = minValue
    };

    public static EncounterCondition FactionRepMin(string factionId, int min) => new()
    {
        Type = ConditionType.FactionRep,
        TargetId = factionId,
        Threshold = min
    };

    public static EncounterCondition SystemHasTag(string tag) => new()
    {
        Type = ConditionType.SystemTag,
        TargetId = tag
    };

    public static EncounterCondition CrewStatMin(CrewStatType stat, int min) => new()
    {
        Type = ConditionType.CrewStat,
        TargetId = stat.ToString(),
        Threshold = min
    };

    public static EncounterCondition FlagSet(string flagId) => new()
    {
        Type = ConditionType.HasFlag,
        TargetId = flagId
    };

    public static EncounterCondition Not(EncounterCondition condition) => new()
    {
        Type = ConditionType.Not,
        ChildCondition = condition
    };

    public static EncounterCondition And(params EncounterCondition[] conditions) => new()
    {
        Type = ConditionType.And,
        ChildConditions = conditions.ToList()
    };

    public static EncounterCondition Or(params EncounterCondition[] conditions) => new()
    {
        Type = ConditionType.Or,
        ChildConditions = conditions.ToList()
    };
}
