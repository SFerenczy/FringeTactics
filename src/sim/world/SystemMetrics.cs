namespace FringeTactics;

/// <summary>
/// System-level metrics that affect gameplay.
/// All values are 0-5 tiers per CAMPAIGN_FOUNDATIONS.
/// </summary>
public class SystemMetrics
{
    /// <summary>
    /// Political/social stability. 0 = chaos, 5 = rock solid.
    /// </summary>
    public int Stability { get; set; } = 3;

    /// <summary>
    /// Law enforcement presence. 0 = lawless, 5 = heavily patrolled.
    /// </summary>
    public int SecurityLevel { get; set; } = 3;

    /// <summary>
    /// Piracy, smuggling, black market. 0 = clean, 5 = rampant.
    /// </summary>
    public int CriminalActivity { get; set; } = 2;

    /// <summary>
    /// Trade volume, wealth. 0 = dead, 5 = booming.
    /// </summary>
    public int EconomicActivity { get; set; } = 3;

    /// <summary>
    /// Patrol frequency. 0 = none, 5 = constant.
    /// </summary>
    public int LawEnforcementPresence { get; set; } = 3;

    /// <summary>
    /// Create metrics with default values for a system type.
    /// </summary>
    public static SystemMetrics ForSystemType(SystemType type)
    {
        return type switch
        {
            SystemType.Station => new SystemMetrics
            {
                Stability = 4,
                SecurityLevel = 4,
                CriminalActivity = 1,
                EconomicActivity = 4,
                LawEnforcementPresence = 4
            },
            SystemType.Outpost => new SystemMetrics
            {
                Stability = 3,
                SecurityLevel = 2,
                CriminalActivity = 2,
                EconomicActivity = 2,
                LawEnforcementPresence = 2
            },
            SystemType.Derelict => new SystemMetrics
            {
                Stability = 1,
                SecurityLevel = 0,
                CriminalActivity = 3,
                EconomicActivity = 0,
                LawEnforcementPresence = 0
            },
            SystemType.Asteroid => new SystemMetrics
            {
                Stability = 2,
                SecurityLevel = 1,
                CriminalActivity = 2,
                EconomicActivity = 3,
                LawEnforcementPresence = 1
            },
            SystemType.Nebula => new SystemMetrics
            {
                Stability = 2,
                SecurityLevel = 0,
                CriminalActivity = 3,
                EconomicActivity = 1,
                LawEnforcementPresence = 0
            },
            SystemType.Contested => new SystemMetrics
            {
                Stability = 1,
                SecurityLevel = 1,
                CriminalActivity = 4,
                EconomicActivity = 2,
                LawEnforcementPresence = 1
            },
            _ => new SystemMetrics()
        };
    }

    public SystemMetricsData GetState()
    {
        return new SystemMetricsData
        {
            Stability = Stability,
            SecurityLevel = SecurityLevel,
            CriminalActivity = CriminalActivity,
            EconomicActivity = EconomicActivity,
            LawEnforcementPresence = LawEnforcementPresence
        };
    }

    public static SystemMetrics FromState(SystemMetricsData data)
    {
        if (data == null) return new SystemMetrics();

        return new SystemMetrics
        {
            Stability = data.Stability,
            SecurityLevel = data.SecurityLevel,
            CriminalActivity = data.CriminalActivity,
            EconomicActivity = data.EconomicActivity,
            LawEnforcementPresence = data.LawEnforcementPresence
        };
    }
}

/// <summary>
/// Serializable data for SystemMetrics.
/// </summary>
public class SystemMetricsData
{
    public int Stability { get; set; }
    public int SecurityLevel { get; set; }
    public int CriminalActivity { get; set; }
    public int EconomicActivity { get; set; }
    public int LawEnforcementPresence { get; set; }
}
