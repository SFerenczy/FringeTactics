namespace FringeTactics;

/// <summary>
/// Enumeration of system-level metrics.
/// All metrics use 0-5 scale per CAMPAIGN_FOUNDATIONS.
/// </summary>
public enum SystemMetricType
{
    /// <summary>
    /// Political/social stability. 0 = chaos, 5 = rock solid.
    /// </summary>
    Stability,

    /// <summary>
    /// Law enforcement presence. 0 = lawless, 5 = heavily patrolled.
    /// </summary>
    SecurityLevel,

    /// <summary>
    /// Piracy, smuggling, black market. 0 = clean, 5 = rampant.
    /// </summary>
    CriminalActivity,

    /// <summary>
    /// Trade volume, wealth. 0 = dead, 5 = booming.
    /// </summary>
    EconomicActivity,

    /// <summary>
    /// Patrol frequency. 0 = none, 5 = constant.
    /// </summary>
    LawEnforcementPresence
}
