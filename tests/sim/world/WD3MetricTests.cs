using System.Linq;
using GdUnit4;
using static GdUnit4.Assertions;

namespace FringeTactics.Tests;

[TestSuite]
public class WD3MetricTests
{
    // ========== SystemMetricType Enum Tests ==========

    [TestCase]
    public void SystemMetricType_HasAllFiveMetrics()
    {
        var values = System.Enum.GetValues<SystemMetricType>();
        AssertInt(values.Length).IsEqual(5);
    }

    // ========== SystemMetrics.Get() Tests ==========

    [TestCase]
    public void SystemMetrics_Get_ReturnsStability()
    {
        var metrics = new SystemMetrics { Stability = 4 };
        AssertInt(metrics.Get(SystemMetricType.Stability)).IsEqual(4);
    }

    [TestCase]
    public void SystemMetrics_Get_ReturnsSecurityLevel()
    {
        var metrics = new SystemMetrics { SecurityLevel = 5 };
        AssertInt(metrics.Get(SystemMetricType.SecurityLevel)).IsEqual(5);
    }

    [TestCase]
    public void SystemMetrics_Get_ReturnsCriminalActivity()
    {
        var metrics = new SystemMetrics { CriminalActivity = 3 };
        AssertInt(metrics.Get(SystemMetricType.CriminalActivity)).IsEqual(3);
    }

    [TestCase]
    public void SystemMetrics_Get_ReturnsEconomicActivity()
    {
        var metrics = new SystemMetrics { EconomicActivity = 2 };
        AssertInt(metrics.Get(SystemMetricType.EconomicActivity)).IsEqual(2);
    }

    [TestCase]
    public void SystemMetrics_Get_ReturnsLawEnforcementPresence()
    {
        var metrics = new SystemMetrics { LawEnforcementPresence = 1 };
        AssertInt(metrics.Get(SystemMetricType.LawEnforcementPresence)).IsEqual(1);
    }

    // ========== SystemMetrics.Set() Tests ==========

    [TestCase]
    public void SystemMetrics_Set_UpdatesValue()
    {
        var metrics = new SystemMetrics();

        metrics.Set(SystemMetricType.SecurityLevel, 4);

        AssertInt(metrics.SecurityLevel).IsEqual(4);
    }

    [TestCase]
    public void SystemMetrics_Set_ClampsToMaximum()
    {
        var metrics = new SystemMetrics();

        metrics.Set(SystemMetricType.SecurityLevel, 10);

        AssertInt(metrics.SecurityLevel).IsEqual(5);
    }

    [TestCase]
    public void SystemMetrics_Set_ClampsToMinimum()
    {
        var metrics = new SystemMetrics();

        metrics.Set(SystemMetricType.SecurityLevel, -5);

        AssertInt(metrics.SecurityLevel).IsEqual(0);
    }

    [TestCase]
    public void SystemMetrics_Set_AllMetricTypes()
    {
        var metrics = new SystemMetrics();

        metrics.Set(SystemMetricType.Stability, 1);
        metrics.Set(SystemMetricType.SecurityLevel, 2);
        metrics.Set(SystemMetricType.CriminalActivity, 3);
        metrics.Set(SystemMetricType.EconomicActivity, 4);
        metrics.Set(SystemMetricType.LawEnforcementPresence, 5);

        AssertInt(metrics.Stability).IsEqual(1);
        AssertInt(metrics.SecurityLevel).IsEqual(2);
        AssertInt(metrics.CriminalActivity).IsEqual(3);
        AssertInt(metrics.EconomicActivity).IsEqual(4);
        AssertInt(metrics.LawEnforcementPresence).IsEqual(5);
    }

    // ========== SystemMetrics.Modify() Tests ==========

    [TestCase]
    public void SystemMetrics_Modify_AppliesPositiveDelta()
    {
        var metrics = new SystemMetrics { SecurityLevel = 2 };

        metrics.Modify(SystemMetricType.SecurityLevel, 2);

        AssertInt(metrics.SecurityLevel).IsEqual(4);
    }

    [TestCase]
    public void SystemMetrics_Modify_AppliesNegativeDelta()
    {
        var metrics = new SystemMetrics { SecurityLevel = 4 };

        metrics.Modify(SystemMetricType.SecurityLevel, -2);

        AssertInt(metrics.SecurityLevel).IsEqual(2);
    }

    [TestCase]
    public void SystemMetrics_Modify_ClampsResultToMaximum()
    {
        var metrics = new SystemMetrics { SecurityLevel = 4 };

        metrics.Modify(SystemMetricType.SecurityLevel, 5);

        AssertInt(metrics.SecurityLevel).IsEqual(5);
    }

    [TestCase]
    public void SystemMetrics_Modify_ClampsResultToMinimum()
    {
        var metrics = new SystemMetrics { SecurityLevel = 2 };

        metrics.Modify(SystemMetricType.SecurityLevel, -5);

        AssertInt(metrics.SecurityLevel).IsEqual(0);
    }

    // ========== SystemMetrics.ClampAll() Tests ==========

    [TestCase]
    public void SystemMetrics_ClampAll_ClampsAllMetrics()
    {
        var metrics = new SystemMetrics
        {
            Stability = 10,
            SecurityLevel = -5,
            CriminalActivity = 7,
            EconomicActivity = -1,
            LawEnforcementPresence = 100
        };

        metrics.ClampAll();

        AssertInt(metrics.Stability).IsEqual(5);
        AssertInt(metrics.SecurityLevel).IsEqual(0);
        AssertInt(metrics.CriminalActivity).IsEqual(5);
        AssertInt(metrics.EconomicActivity).IsEqual(0);
        AssertInt(metrics.LawEnforcementPresence).IsEqual(5);
    }

    [TestCase]
    public void SystemMetrics_ClampAll_PreservesValidValues()
    {
        var metrics = new SystemMetrics
        {
            Stability = 3,
            SecurityLevel = 2,
            CriminalActivity = 4,
            EconomicActivity = 1,
            LawEnforcementPresence = 5
        };

        metrics.ClampAll();

        AssertInt(metrics.Stability).IsEqual(3);
        AssertInt(metrics.SecurityLevel).IsEqual(2);
        AssertInt(metrics.CriminalActivity).IsEqual(4);
        AssertInt(metrics.EconomicActivity).IsEqual(1);
        AssertInt(metrics.LawEnforcementPresence).IsEqual(5);
    }

    // ========== SystemMetrics.Clone() Tests ==========

    [TestCase]
    public void SystemMetrics_Clone_CreatesIndependentCopy()
    {
        var original = new SystemMetrics
        {
            Stability = 3,
            SecurityLevel = 4,
            CriminalActivity = 2
        };

        var clone = original.Clone();
        clone.SecurityLevel = 1;

        AssertInt(original.SecurityLevel).IsEqual(4);
        AssertInt(clone.SecurityLevel).IsEqual(1);
    }

    [TestCase]
    public void SystemMetrics_Clone_CopiesAllValues()
    {
        var original = new SystemMetrics
        {
            Stability = 1,
            SecurityLevel = 2,
            CriminalActivity = 3,
            EconomicActivity = 4,
            LawEnforcementPresence = 5
        };

        var clone = original.Clone();

        AssertInt(clone.Stability).IsEqual(1);
        AssertInt(clone.SecurityLevel).IsEqual(2);
        AssertInt(clone.CriminalActivity).IsEqual(3);
        AssertInt(clone.EconomicActivity).IsEqual(4);
        AssertInt(clone.LawEnforcementPresence).IsEqual(5);
    }

    // ========== Integration with ForSystemType ==========

    [TestCase]
    public void SystemMetrics_ForSystemType_CanBeAccessedViaGet()
    {
        var metrics = SystemMetrics.ForSystemType(SystemType.Station);

        AssertInt(metrics.Get(SystemMetricType.Stability)).IsEqual(4);
        AssertInt(metrics.Get(SystemMetricType.SecurityLevel)).IsEqual(4);
        AssertInt(metrics.Get(SystemMetricType.CriminalActivity)).IsEqual(1);
        AssertInt(metrics.Get(SystemMetricType.EconomicActivity)).IsEqual(4);
        AssertInt(metrics.Get(SystemMetricType.LawEnforcementPresence)).IsEqual(4);
    }

    [TestCase]
    public void SystemMetrics_ForSystemType_CanBeModified()
    {
        var metrics = SystemMetrics.ForSystemType(SystemType.Derelict);

        metrics.Modify(SystemMetricType.CriminalActivity, 2);

        AssertInt(metrics.CriminalActivity).IsEqual(5);
    }

    // ========== WorldState.GetSystemMetric() Tests ==========

    [TestCase]
    public void WorldState_GetSystemMetric_ReturnsCorrectValue()
    {
        var world = WorldState.CreateTestSector();

        int security = world.GetSystemMetric(0, SystemMetricType.SecurityLevel);

        AssertInt(security).IsGreaterEqual(4);
    }

    [TestCase]
    public void WorldState_GetSystemMetric_ReturnsZeroForInvalidSystem()
    {
        var world = WorldState.CreateTestSector();

        int security = world.GetSystemMetric(999, SystemMetricType.SecurityLevel);

        AssertInt(security).IsEqual(0);
    }

    [TestCase]
    public void WorldState_GetSystemMetric_AllMetricTypes()
    {
        var world = WorldState.CreateTestSector();

        int stability = world.GetSystemMetric(0, SystemMetricType.Stability);
        int security = world.GetSystemMetric(0, SystemMetricType.SecurityLevel);
        int crime = world.GetSystemMetric(0, SystemMetricType.CriminalActivity);
        int economy = world.GetSystemMetric(0, SystemMetricType.EconomicActivity);
        int patrol = world.GetSystemMetric(0, SystemMetricType.LawEnforcementPresence);

        AssertInt(stability).IsGreaterEqual(0);
        AssertInt(security).IsGreaterEqual(0);
        AssertInt(crime).IsGreaterEqual(0);
        AssertInt(economy).IsGreaterEqual(0);
        AssertInt(patrol).IsGreaterEqual(0);
    }

    // ========== WorldState.GetSystemsByMetric() Tests ==========

    [TestCase]
    public void WorldState_GetSystemsByMetric_FiltersCorrectly()
    {
        var world = WorldState.CreateTestSector();

        var highSecurity = world.GetSystemsByMetric(SystemMetricType.SecurityLevel, 4, 5).ToList();

        AssertBool(highSecurity.All(s => s.Metrics.SecurityLevel >= 4)).IsTrue();
    }

    [TestCase]
    public void WorldState_GetSystemsByMetric_ReturnsEmptyForNoMatches()
    {
        var world = WorldState.CreateSingleHub();

        var lowSecurity = world.GetSystemsByMetric(SystemMetricType.SecurityLevel, 0, 0).ToList();

        AssertInt(lowSecurity.Count).IsEqual(0);
    }

    // ========== WorldState Convenience Methods Tests ==========

    [TestCase]
    public void WorldState_GetHighSecuritySystems_FiltersCorrectly()
    {
        var world = WorldState.CreateTestSector();

        var highSec = world.GetHighSecuritySystems(4).ToList();

        AssertInt(highSec.Count).IsGreaterEqual(2);
        AssertBool(highSec.All(s => s.Metrics.SecurityLevel >= 4)).IsTrue();
    }

    [TestCase]
    public void WorldState_GetLawlessSystems_FiltersCorrectly()
    {
        var world = WorldState.CreateTestSector();

        var lawless = world.GetLawlessSystems(1).ToList();

        AssertInt(lawless.Count).IsGreaterEqual(3);
        AssertBool(lawless.All(s => s.Metrics.SecurityLevel <= 1)).IsTrue();
    }

    [TestCase]
    public void WorldState_GetHighCrimeSystems_FiltersCorrectly()
    {
        var world = WorldState.CreateTestSector();

        var highCrime = world.GetHighCrimeSystems(4).ToList();

        AssertInt(highCrime.Count).IsGreaterEqual(1);
        AssertBool(highCrime.All(s => s.Metrics.CriminalActivity >= 4)).IsTrue();
    }

    [TestCase]
    public void WorldState_GetProsperousSystems_FiltersCorrectly()
    {
        var world = WorldState.CreateTestSector();

        var prosperous = world.GetProsperousSystems(4).ToList();

        AssertInt(prosperous.Count).IsGreaterEqual(1);
        AssertBool(prosperous.All(s => s.Metrics.EconomicActivity >= 4)).IsTrue();
    }

    // ========== WorldState.SetSystemMetric() Tests ==========

    [TestCase]
    public void WorldState_SetSystemMetric_UpdatesValue()
    {
        var world = WorldState.CreateTestSector();

        world.SetSystemMetric(0, SystemMetricType.CriminalActivity, 3);

        int crime = world.GetSystemMetric(0, SystemMetricType.CriminalActivity);
        AssertInt(crime).IsEqual(3);
    }

    [TestCase]
    public void WorldState_SetSystemMetric_ClampsValue()
    {
        var world = WorldState.CreateTestSector();

        world.SetSystemMetric(0, SystemMetricType.SecurityLevel, 10);

        int security = world.GetSystemMetric(0, SystemMetricType.SecurityLevel);
        AssertInt(security).IsEqual(5);
    }

    [TestCase]
    public void WorldState_SetSystemMetric_ReturnsFalseForInvalidSystem()
    {
        var world = WorldState.CreateTestSector();

        bool result = world.SetSystemMetric(999, SystemMetricType.SecurityLevel, 3);

        AssertBool(result).IsFalse();
    }

    [TestCase]
    public void WorldState_SetSystemMetric_ReturnsTrueOnSuccess()
    {
        var world = WorldState.CreateTestSector();

        bool result = world.SetSystemMetric(0, SystemMetricType.SecurityLevel, 3);

        AssertBool(result).IsTrue();
    }

    // ========== WorldState.ModifySystemMetric() Tests ==========

    [TestCase]
    public void WorldState_ModifySystemMetric_AppliesPositiveDelta()
    {
        var world = WorldState.CreateTestSector();
        int original = world.GetSystemMetric(1, SystemMetricType.SecurityLevel);

        world.ModifySystemMetric(1, SystemMetricType.SecurityLevel, 1);

        int modified = world.GetSystemMetric(1, SystemMetricType.SecurityLevel);
        AssertInt(modified).IsEqual(System.Math.Min(original + 1, 5));
    }

    [TestCase]
    public void WorldState_ModifySystemMetric_AppliesNegativeDelta()
    {
        var world = WorldState.CreateTestSector();
        int original = world.GetSystemMetric(0, SystemMetricType.SecurityLevel);

        world.ModifySystemMetric(0, SystemMetricType.SecurityLevel, -1);

        int modified = world.GetSystemMetric(0, SystemMetricType.SecurityLevel);
        AssertInt(modified).IsEqual(original - 1);
    }

    [TestCase]
    public void WorldState_ModifySystemMetric_ClampsResult()
    {
        var world = WorldState.CreateTestSector();

        world.ModifySystemMetric(0, SystemMetricType.SecurityLevel, 10);

        int security = world.GetSystemMetric(0, SystemMetricType.SecurityLevel);
        AssertInt(security).IsEqual(5);
    }

    [TestCase]
    public void WorldState_ModifySystemMetric_ReturnsFalseForInvalidSystem()
    {
        var world = WorldState.CreateTestSector();

        bool result = world.ModifySystemMetric(999, SystemMetricType.SecurityLevel, 1);

        AssertBool(result).IsFalse();
    }

    [TestCase]
    public void WorldState_ModifySystemMetric_ReturnsTrueOnSuccess()
    {
        var world = WorldState.CreateTestSector();

        bool result = world.ModifySystemMetric(0, SystemMetricType.SecurityLevel, -1);

        AssertBool(result).IsTrue();
    }
}
