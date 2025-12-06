using System;

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

    // ========== Typed Accessors ==========

    /// <summary>
    /// Get metric value by type.
    /// </summary>
    public int Get(SystemMetricType type)
    {
        return type switch
        {
            SystemMetricType.Stability => Stability,
            SystemMetricType.SecurityLevel => SecurityLevel,
            SystemMetricType.CriminalActivity => CriminalActivity,
            SystemMetricType.EconomicActivity => EconomicActivity,
            SystemMetricType.LawEnforcementPresence => LawEnforcementPresence,
            _ => 0
        };
    }

    /// <summary>
    /// Set metric value by type. Automatically clamps to 0-5.
    /// </summary>
    public void Set(SystemMetricType type, int value)
    {
        value = Math.Clamp(value, 0, 5);
        switch (type)
        {
            case SystemMetricType.Stability: Stability = value; break;
            case SystemMetricType.SecurityLevel: SecurityLevel = value; break;
            case SystemMetricType.CriminalActivity: CriminalActivity = value; break;
            case SystemMetricType.EconomicActivity: EconomicActivity = value; break;
            case SystemMetricType.LawEnforcementPresence: LawEnforcementPresence = value; break;
        }
    }

    /// <summary>
    /// Modify metric by delta. Automatically clamps to 0-5.
    /// </summary>
    public void Modify(SystemMetricType type, int delta)
    {
        Set(type, Get(type) + delta);
    }

    /// <summary>
    /// Clamp all metrics to valid 0-5 range.
    /// </summary>
    public void ClampAll()
    {
        Stability = Math.Clamp(Stability, 0, 5);
        SecurityLevel = Math.Clamp(SecurityLevel, 0, 5);
        CriminalActivity = Math.Clamp(CriminalActivity, 0, 5);
        EconomicActivity = Math.Clamp(EconomicActivity, 0, 5);
        LawEnforcementPresence = Math.Clamp(LawEnforcementPresence, 0, 5);
    }

    /// <summary>
    /// Create a copy of these metrics.
    /// </summary>
    public SystemMetrics Clone()
    {
        return new SystemMetrics
        {
            Stability = Stability,
            SecurityLevel = SecurityLevel,
            CriminalActivity = CriminalActivity,
            EconomicActivity = EconomicActivity,
            LawEnforcementPresence = LawEnforcementPresence
        };
    }

    // ========== Serialization ==========

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
