using System.Linq;
using GdUnit4;
using static GdUnit4.Assertions;

namespace FringeTactics.Tests;

[TestSuite]
public class WD3QueryTests
{
    // ========== GetEffectiveRouteDanger Tests ==========

    [TestCase]
    public void WorldState_GetEffectiveRouteDanger_ReturnsZeroForInvalidRoute()
    {
        var world = WorldState.CreateTestSector();

        int danger = world.GetEffectiveRouteDanger(0, 999);

        AssertInt(danger).IsEqual(0);
    }

    [TestCase]
    public void WorldState_GetEffectiveRouteDanger_CombinesFactors()
    {
        var world = WorldState.CreateTestSector();

        // Route from Haven (safe) to Waypoint (moderate)
        int danger01 = world.GetEffectiveRouteDanger(0, 1);

        // Route from Waypoint to Contested (dangerous)
        int danger14 = world.GetEffectiveRouteDanger(1, 4);

        // Dangerous route should have higher effective danger
        AssertInt(danger14).IsGreater(danger01);
    }

    [TestCase]
    public void WorldState_GetEffectiveRouteDanger_IncludesBaseHazard()
    {
        var world = WorldState.CreateTestSector();

        // Route 1-4 has hazard level 3
        int danger = world.GetEffectiveRouteDanger(1, 4);

        AssertInt(danger).IsGreaterEqual(3);
    }

    [TestCase]
    public void WorldState_GetEffectiveRouteDanger_ClampedTo0To5()
    {
        var world = WorldState.CreateTestSector();

        // Check all routes are within valid range
        foreach (var route in world.GetAllRoutes())
        {
            int danger = world.GetEffectiveRouteDanger(route.SystemA, route.SystemB);
            AssertInt(danger).IsGreaterEqual(0);
            AssertInt(danger).IsLessEqual(5);
        }
    }

    // ========== GetSmugglingRoutes Tests ==========

    [TestCase]
    public void WorldState_GetSmugglingRoutes_FindsHiddenRoutes()
    {
        var world = WorldState.CreateTestSector();

        var smugglingRoutes = world.GetSmugglingRoutes().ToList();

        // Should include hidden routes
        AssertBool(smugglingRoutes.Any(r => r.HasTag(WorldTags.Hidden))).IsTrue();
    }

    [TestCase]
    public void WorldState_GetSmugglingRoutes_ExcludesPatrolledRoutes()
    {
        var world = WorldState.CreateTestSector();

        var smugglingRoutes = world.GetSmugglingRoutes().ToList();

        // Should not include patrolled routes
        AssertBool(smugglingRoutes.All(r => !r.HasTag(WorldTags.Patrolled))).IsTrue();
    }

    [TestCase]
    public void WorldState_GetSmugglingRoutes_IncludesRoutesToLawlessSystems()
    {
        var world = WorldState.CreateTestSector();

        var smugglingRoutes = world.GetSmugglingRoutes().ToList();

        // Should include routes connecting to lawless systems
        AssertInt(smugglingRoutes.Count).IsGreaterEqual(2);
    }

    // ========== GetHideoutSystems Tests ==========

    [TestCase]
    public void WorldState_GetHideoutSystems_FindsLawlessSystems()
    {
        var world = WorldState.CreateTestSector();

        var hideouts = world.GetHideoutSystems().ToList();

        // Should include systems with lawless tag or low security
        AssertInt(hideouts.Count).IsGreaterEqual(2);
    }

    [TestCase]
    public void WorldState_GetHideoutSystems_ExcludesContestedSystems()
    {
        var world = WorldState.CreateTestSector();

        var hideouts = world.GetHideoutSystems().ToList();

        // Should not include contested systems
        AssertBool(hideouts.All(s => !s.HasTag(WorldTags.Contested))).IsTrue();
    }

    [TestCase]
    public void WorldState_GetHideoutSystems_IncludesLowSecuritySystems()
    {
        var world = WorldState.CreateTestSector();

        var hideouts = world.GetHideoutSystems().ToList();

        // All hideouts should have security <= 1 or lawless tag
        AssertBool(hideouts.All(s =>
            s.Metrics.SecurityLevel <= 1 || s.HasTag(WorldTags.Lawless))).IsTrue();
    }

    // ========== GetRouteEncounterContext Tests ==========

    [TestCase]
    public void WorldState_GetRouteEncounterContext_ReturnsNullForInvalidRoute()
    {
        var world = WorldState.CreateTestSector();

        var context = world.GetRouteEncounterContext(0, 999);

        AssertObject(context).IsNull();
    }

    [TestCase]
    public void WorldState_GetRouteEncounterContext_ReturnsCompleteContext()
    {
        var world = WorldState.CreateTestSector();

        var context = world.GetRouteEncounterContext(0, 1);

        AssertObject(context).IsNotNull();
        AssertInt(context.FromSystemId).IsEqual(0);
        AssertInt(context.ToSystemId).IsEqual(1);
        AssertFloat(context.Distance).IsGreater(0f);
        AssertBool(context.RouteTags.Contains(WorldTags.Patrolled)).IsTrue();
    }

    [TestCase]
    public void WorldState_GetRouteEncounterContext_IncludesSystemTags()
    {
        var world = WorldState.CreateTestSector();

        var context = world.GetRouteEncounterContext(0, 1);

        // Haven has Core and Hub tags
        AssertBool(context.FromSystemTags.Contains(WorldTags.Core)).IsTrue();
        AssertBool(context.FromSystemTags.Contains(WorldTags.Hub)).IsTrue();
        // Waypoint has Frontier tag
        AssertBool(context.ToSystemTags.Contains(WorldTags.Frontier)).IsTrue();
    }

    [TestCase]
    public void WorldState_GetRouteEncounterContext_IncludesMetrics()
    {
        var world = WorldState.CreateTestSector();

        var context = world.GetRouteEncounterContext(0, 1);

        // Haven should have high security
        AssertInt(context.FromSecurityLevel).IsGreaterEqual(4);
        // Waypoint should have lower security
        AssertInt(context.ToSecurityLevel).IsLess(context.FromSecurityLevel);
    }

    [TestCase]
    public void WorldState_GetRouteEncounterContext_CalculatesEffectiveDanger()
    {
        var world = WorldState.CreateTestSector();

        var context = world.GetRouteEncounterContext(1, 4);

        // Should match GetEffectiveRouteDanger
        int expected = world.GetEffectiveRouteDanger(1, 4);
        AssertInt(context.EffectiveDanger).IsEqual(expected);
    }

    // ========== GetSystemEncounterContext Tests ==========

    [TestCase]
    public void WorldState_GetSystemEncounterContext_ReturnsNullForInvalidSystem()
    {
        var world = WorldState.CreateTestSector();

        var context = world.GetSystemEncounterContext(999);

        AssertObject(context).IsNull();
    }

    [TestCase]
    public void WorldState_GetSystemEncounterContext_ReturnsCompleteContext()
    {
        var world = WorldState.CreateTestSector();

        var context = world.GetSystemEncounterContext(0);

        AssertObject(context).IsNotNull();
        AssertInt(context.SystemId).IsEqual(0);
        AssertString(context.SystemName).IsEqual("Haven Station");
        AssertBool(context.HasStation).IsTrue();
        AssertInt(context.StationCount).IsGreaterEqual(1);
    }

    [TestCase]
    public void WorldState_GetSystemEncounterContext_IncludesSystemTags()
    {
        var world = WorldState.CreateTestSector();

        var context = world.GetSystemEncounterContext(0);

        AssertBool(context.SystemTags.Contains(WorldTags.Hub)).IsTrue();
        AssertBool(context.SystemTags.Contains(WorldTags.Core)).IsTrue();
    }

    [TestCase]
    public void WorldState_GetSystemEncounterContext_IncludesStationTags()
    {
        var world = WorldState.CreateTestSector();

        var context = world.GetSystemEncounterContext(0);

        // Haven Station has TradeHub tag
        AssertBool(context.StationTags.Contains(WorldTags.TradeHub)).IsTrue();
    }

    [TestCase]
    public void WorldState_GetSystemEncounterContext_IncludesMetrics()
    {
        var world = WorldState.CreateTestSector();

        var context = world.GetSystemEncounterContext(0);

        AssertObject(context.Metrics).IsNotNull();
        AssertInt(context.Metrics.SecurityLevel).IsGreaterEqual(4);
    }

    [TestCase]
    public void WorldState_GetSystemEncounterContext_MetricsAreCloned()
    {
        var world = WorldState.CreateTestSector();

        var context = world.GetSystemEncounterContext(0);
        int originalSecurity = world.GetSystemMetric(0, SystemMetricType.SecurityLevel);

        // Modify context metrics
        context.Metrics.SecurityLevel = 0;

        // Original should be unchanged
        int afterSecurity = world.GetSystemMetric(0, SystemMetricType.SecurityLevel);
        AssertInt(afterSecurity).IsEqual(originalSecurity);
    }

    // ========== RouteEncounterContext Helper Tests ==========

    [TestCase]
    public void RouteEncounterContext_HasAnyTag_ChecksAllTagSets()
    {
        var context = new RouteEncounterContext
        {
            RouteTags = new System.Collections.Generic.HashSet<string> { WorldTags.Dangerous },
            FromSystemTags = new System.Collections.Generic.HashSet<string> { WorldTags.Core },
            ToSystemTags = new System.Collections.Generic.HashSet<string> { WorldTags.Frontier }
        };

        AssertBool(context.HasAnyTag(WorldTags.Dangerous)).IsTrue();
        AssertBool(context.HasAnyTag(WorldTags.Core)).IsTrue();
        AssertBool(context.HasAnyTag(WorldTags.Frontier)).IsTrue();
        AssertBool(context.HasAnyTag(WorldTags.Lawless)).IsFalse();
    }

    [TestCase]
    public void RouteEncounterContext_MaxCriminalActivity_ReturnsHigher()
    {
        var context = new RouteEncounterContext
        {
            FromCriminalActivity = 2,
            ToCriminalActivity = 4
        };

        AssertInt(context.MaxCriminalActivity).IsEqual(4);
    }

    [TestCase]
    public void RouteEncounterContext_MinSecurityLevel_ReturnsLower()
    {
        var context = new RouteEncounterContext
        {
            FromSecurityLevel = 4,
            ToSecurityLevel = 1
        };

        AssertInt(context.MinSecurityLevel).IsEqual(1);
    }

    [TestCase]
    public void RouteEncounterContext_IsHighDanger_TrueWhenDangerIs3OrMore()
    {
        var lowDanger = new RouteEncounterContext { EffectiveDanger = 2 };
        var highDanger = new RouteEncounterContext { EffectiveDanger = 3 };

        AssertBool(lowDanger.IsHighDanger).IsFalse();
        AssertBool(highDanger.IsHighDanger).IsTrue();
    }

    [TestCase]
    public void RouteEncounterContext_HasLawlessEndpoint_TrueWhenSecurityLow()
    {
        var safe = new RouteEncounterContext { FromSecurityLevel = 4, ToSecurityLevel = 3 };
        var lawless = new RouteEncounterContext { FromSecurityLevel = 4, ToSecurityLevel = 1 };

        AssertBool(safe.HasLawlessEndpoint).IsFalse();
        AssertBool(lawless.HasLawlessEndpoint).IsTrue();
    }

    // ========== SystemEncounterContext Helper Tests ==========

    [TestCase]
    public void SystemEncounterContext_HasAnyTag_ChecksBothTagSets()
    {
        var context = new SystemEncounterContext
        {
            SystemTags = new System.Collections.Generic.HashSet<string> { WorldTags.Core },
            StationTags = new System.Collections.Generic.HashSet<string> { WorldTags.TradeHub }
        };

        AssertBool(context.HasAnyTag(WorldTags.Core)).IsTrue();
        AssertBool(context.HasAnyTag(WorldTags.TradeHub)).IsTrue();
        AssertBool(context.HasAnyTag(WorldTags.Lawless)).IsFalse();
    }

    [TestCase]
    public void SystemEncounterContext_IsLawless_TrueWhenSecurityLow()
    {
        var safe = new SystemEncounterContext { Metrics = new SystemMetrics { SecurityLevel = 4 } };
        var lawless = new SystemEncounterContext { Metrics = new SystemMetrics { SecurityLevel = 1 } };

        AssertBool(safe.IsLawless).IsFalse();
        AssertBool(lawless.IsLawless).IsTrue();
    }

    [TestCase]
    public void SystemEncounterContext_IsHighSecurity_TrueWhenSecurityHigh()
    {
        var low = new SystemEncounterContext { Metrics = new SystemMetrics { SecurityLevel = 2 } };
        var high = new SystemEncounterContext { Metrics = new SystemMetrics { SecurityLevel = 4 } };

        AssertBool(low.IsHighSecurity).IsFalse();
        AssertBool(high.IsHighSecurity).IsTrue();
    }

    [TestCase]
    public void SystemEncounterContext_IsHighCrime_TrueWhenCrimeHigh()
    {
        var low = new SystemEncounterContext { Metrics = new SystemMetrics { CriminalActivity = 2 } };
        var high = new SystemEncounterContext { Metrics = new SystemMetrics { CriminalActivity = 4 } };

        AssertBool(low.IsHighCrime).IsFalse();
        AssertBool(high.IsHighCrime).IsTrue();
    }
}
