using GdUnit4;
using static GdUnit4.Assertions;
using System.Linq;

namespace FringeTactics.Tests;

[TestSuite]
public class GN2ConfigTests
{
    [TestCase]
    public void DefaultConfig_HasValidSystemCount()
    {
        var config = GalaxyConfig.Default;

        AssertInt(config.SystemCount).IsGreater(0);
    }

    [TestCase]
    public void DefaultConfig_HasValidSpatialConstraints()
    {
        var config = GalaxyConfig.Default;

        AssertFloat(config.MinSystemDistance).IsGreater(0f);
        AssertFloat(config.MapWidth).IsGreater(config.MinSystemDistance * 2);
        AssertFloat(config.MapHeight).IsGreater(config.MinSystemDistance * 2);
        AssertFloat(config.EdgeMargin).IsGreater(0f);
        AssertFloat(config.EdgeMargin).IsLess(config.MapWidth / 2);
    }

    [TestCase]
    public void DefaultConfig_HasValidConnectionLimits()
    {
        var config = GalaxyConfig.Default;

        AssertInt(config.MinConnections).IsGreaterEqual(1);
        AssertInt(config.MaxConnections).IsGreaterEqual(config.MinConnections);
    }

    [TestCase]
    public void DefaultConfig_HasFactions()
    {
        var config = GalaxyConfig.Default;

        AssertInt(config.FactionIds.Count).IsGreater(0);
    }

    [TestCase]
    public void DefaultConfig_HasSystemTypeWeights()
    {
        var config = GalaxyConfig.Default;

        AssertInt(config.SystemTypeWeights.Count).IsGreater(0);
    }

    [TestCase]
    public void DefaultConfig_SystemTypeWeightsSumToApproximatelyOne()
    {
        var config = GalaxyConfig.Default;
        float sum = config.SystemTypeWeights.Values.Sum();

        AssertFloat(sum).IsBetween(0.9f, 1.1f);
    }

    [TestCase]
    public void DefaultConfig_HasInhabitedTypes()
    {
        var config = GalaxyConfig.Default;

        AssertInt(config.InhabitedTypes.Count).IsGreater(0);
        AssertBool(config.InhabitedTypes.Contains(SystemType.Station)).IsTrue();
    }

    [TestCase]
    public void DefaultConfig_NeutralFractionIsValid()
    {
        var config = GalaxyConfig.Default;

        AssertFloat(config.NeutralFraction).IsGreaterEqual(0f);
        AssertFloat(config.NeutralFraction).IsLess(1f);
    }

    [TestCase]
    public void SmallConfig_HasFewerSystems()
    {
        var small = GalaxyConfig.Small;
        var def = GalaxyConfig.Default;

        AssertInt(small.SystemCount).IsLess(def.SystemCount);
    }

    [TestCase]
    public void SmallConfig_HasSmallerMap()
    {
        var small = GalaxyConfig.Small;
        var def = GalaxyConfig.Default;

        AssertFloat(small.MapWidth).IsLess(def.MapWidth);
        AssertFloat(small.MapHeight).IsLess(def.MapHeight);
    }

    [TestCase]
    public void LargeConfig_HasMoreSystems()
    {
        var large = GalaxyConfig.Large;
        var def = GalaxyConfig.Default;

        AssertInt(large.SystemCount).IsGreater(def.SystemCount);
    }

    [TestCase]
    public void LargeConfig_HasLargerMap()
    {
        var large = GalaxyConfig.Large;
        var def = GalaxyConfig.Default;

        AssertFloat(large.MapWidth).IsGreater(def.MapWidth);
        AssertFloat(large.MapHeight).IsGreater(def.MapHeight);
    }

    [TestCase]
    public void LargeConfig_HasMoreConnections()
    {
        var large = GalaxyConfig.Large;
        var def = GalaxyConfig.Default;

        AssertInt(large.MaxConnections).IsGreaterEqual(def.MaxConnections);
    }

    [TestCase]
    public void AllPresets_HaveValidConstraints()
    {
        var presets = new[] { GalaxyConfig.Default, GalaxyConfig.Small, GalaxyConfig.Large };

        foreach (var config in presets)
        {
            // Enough space for systems
            float usableWidth = config.MapWidth - (2 * config.EdgeMargin);
            float usableHeight = config.MapHeight - (2 * config.EdgeMargin);
            float usableArea = usableWidth * usableHeight;
            float minAreaPerSystem = config.MinSystemDistance * config.MinSystemDistance;

            AssertFloat(usableArea).IsGreater(minAreaPerSystem * config.SystemCount * 0.5f);
        }
    }

    [TestCase]
    public void Config_CanBeModified()
    {
        var config = new GalaxyConfig
        {
            SystemCount = 15,
            MapWidth = 900f,
            MinSystemDistance = 100f
        };

        AssertInt(config.SystemCount).IsEqual(15);
        AssertFloat(config.MapWidth).IsEqual(900f);
        AssertFloat(config.MinSystemDistance).IsEqual(100f);
    }

    [TestCase]
    public void Config_FactionIdsCanBeCustomized()
    {
        var config = new GalaxyConfig
        {
            FactionIds = new() { "faction_a", "faction_b" }
        };

        AssertInt(config.FactionIds.Count).IsEqual(2);
        AssertBool(config.FactionIds.Contains("faction_a")).IsTrue();
        AssertBool(config.FactionIds.Contains("faction_b")).IsTrue();
    }

    [TestCase]
    public void Config_GetFactionIds_ReturnsCustomWhenSet()
    {
        var config = new GalaxyConfig
        {
            FactionIds = new() { "custom_a", "custom_b" }
        };

        var ids = config.GetFactionIds();
        AssertInt(ids.Count).IsEqual(2);
        AssertBool(ids.Contains("custom_a")).IsTrue();
    }

    [TestCase]
    public void Config_GetFactionIds_FallsBackToRegistry()
    {
        var config = new GalaxyConfig(); // FactionIds = null by default

        var ids = config.GetFactionIds();
        AssertInt(ids.Count).IsGreater(0);
        // Should contain registry factions
        AssertBool(ids.Contains("corp")).IsTrue();
    }

    [TestCase]
    public void Config_SystemTypeWeightsCanBeCustomized()
    {
        var config = new GalaxyConfig
        {
            SystemTypeWeights = new()
            {
                [SystemType.Station] = 0.5f,
                [SystemType.Outpost] = 0.5f
            }
        };

        AssertInt(config.SystemTypeWeights.Count).IsEqual(2);
        AssertFloat(config.SystemTypeWeights[SystemType.Station]).IsEqual(0.5f);
    }

    // ========================================================================
    // VALIDATION TESTS
    // ========================================================================

    [TestCase]
    public void Validate_DefaultConfig_Passes()
    {
        var config = GalaxyConfig.Default;
        config.Validate(); // Should not throw
    }

    [TestCase]
    public void Validate_SmallConfig_Passes()
    {
        var config = GalaxyConfig.Small;
        config.Validate(); // Should not throw
    }

    [TestCase]
    public void Validate_LargeConfig_Passes()
    {
        var config = GalaxyConfig.Large;
        config.Validate(); // Should not throw
    }

    [TestCase]
    public void Validate_ZeroSystemCount_Throws()
    {
        var config = new GalaxyConfig { SystemCount = 0 };

        bool threw = false;
        try { config.Validate(); }
        catch (System.ArgumentException) { threw = true; }

        AssertBool(threw).IsTrue();
    }

    [TestCase]
    public void Validate_NegativeMapWidth_Throws()
    {
        var config = new GalaxyConfig { MapWidth = -100f };

        bool threw = false;
        try { config.Validate(); }
        catch (System.ArgumentException) { threw = true; }

        AssertBool(threw).IsTrue();
    }

    [TestCase]
    public void Validate_EdgeMarginTooLarge_Throws()
    {
        var config = new GalaxyConfig
        {
            MapWidth = 100f,
            MapHeight = 100f,
            EdgeMargin = 60f // Leaves no usable space
        };

        bool threw = false;
        try { config.Validate(); }
        catch (System.ArgumentException) { threw = true; }

        AssertBool(threw).IsTrue();
    }

    [TestCase]
    public void Validate_MinDistanceTooLarge_Throws()
    {
        var config = new GalaxyConfig
        {
            MapWidth = 200f,
            MapHeight = 200f,
            EdgeMargin = 50f,
            MinSystemDistance = 150f // Larger than usable area
        };

        bool threw = false;
        try { config.Validate(); }
        catch (System.ArgumentException) { threw = true; }

        AssertBool(threw).IsTrue();
    }

    [TestCase]
    public void Validate_NeutralFractionOutOfRange_Throws()
    {
        var config = new GalaxyConfig { NeutralFraction = 1.5f };

        bool threw = false;
        try { config.Validate(); }
        catch (System.ArgumentException) { threw = true; }

        AssertBool(threw).IsTrue();
    }

    [TestCase]
    public void Validate_ExtraRouteChanceOutOfRange_Throws()
    {
        var config = new GalaxyConfig { ExtraRouteChance = -0.1f };

        bool threw = false;
        try { config.Validate(); }
        catch (System.ArgumentException) { threw = true; }

        AssertBool(threw).IsTrue();
    }
}
