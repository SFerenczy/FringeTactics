namespace FringeTactics;

/// <summary>
/// Types of conditions for encounter option visibility/availability.
/// </summary>
public enum ConditionType
{
    /// <summary>
    /// Player has minimum resource amount.
    /// TargetId = resource type, Threshold = minimum value.
    /// </summary>
    HasResource,

    /// <summary>
    /// Any crew member has the specified trait.
    /// TargetId = trait id.
    /// </summary>
    HasTrait,

    /// <summary>
    /// Cargo value meets threshold.
    /// Threshold = minimum cargo value.
    /// </summary>
    HasCargo,

    /// <summary>
    /// Faction reputation meets threshold.
    /// TargetId = faction id, Threshold = minimum rep.
    /// </summary>
    FactionRep,

    /// <summary>
    /// Current system has the specified tag.
    /// TargetId = tag name.
    /// </summary>
    SystemTag,

    /// <summary>
    /// Best crew stat meets threshold.
    /// TargetId = stat type name, Threshold = minimum value.
    /// </summary>
    CrewStat,

    /// <summary>
    /// Campaign flag is set.
    /// TargetId = flag id.
    /// </summary>
    HasFlag,

    /// <summary>
    /// Negates the child condition.
    /// </summary>
    Not,

    /// <summary>
    /// All child conditions must be true.
    /// </summary>
    And,

    /// <summary>
    /// At least one child condition must be true.
    /// </summary>
    Or
}
