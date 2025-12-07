using GdUnit4;
using static GdUnit4.Assertions;

namespace FringeTactics.Tests;

[TestSuite]
public class TV1TravelCostsTests
{
    [TestCase]
    public void CalculateFuelCost_StandardDistance_ReturnsCorrectCost()
    {
        // 150 distance * 0.1 rate / 1.0 efficiency = 15
        int cost = TravelCosts.CalculateFuelCost(150f, 1.0f);
        AssertInt(cost).IsEqual(15);
    }

    [TestCase]
    public void CalculateFuelCost_WithHighEfficiency_ReducesCost()
    {
        // 150 * 0.1 / 1.5 = 10
        int cost = TravelCosts.CalculateFuelCost(150f, 1.5f);
        AssertInt(cost).IsEqual(10);
    }

    [TestCase]
    public void CalculateFuelCost_WithLowEfficiency_IncreasesCost()
    {
        // 150 * 0.1 / 0.5 = 30
        int cost = TravelCosts.CalculateFuelCost(150f, 0.5f);
        AssertInt(cost).IsEqual(30);
    }

    [TestCase]
    public void CalculateFuelCost_ZeroDistance_ReturnsZero()
    {
        int cost = TravelCosts.CalculateFuelCost(0f, 1.0f);
        AssertInt(cost).IsEqual(0);
    }

    [TestCase]
    public void CalculateFuelCost_NegativeDistance_ReturnsZero()
    {
        int cost = TravelCosts.CalculateFuelCost(-50f, 1.0f);
        AssertInt(cost).IsEqual(0);
    }

    [TestCase]
    public void CalculateFuelCost_ZeroEfficiency_UsesDefault()
    {
        // Should treat 0 efficiency as 1.0
        int cost = TravelCosts.CalculateFuelCost(150f, 0f);
        AssertInt(cost).IsEqual(15);
    }

    [TestCase]
    public void CalculateTimeDays_StandardDistance_ReturnsCorrectDays()
    {
        // 150 / 100 = 1.5 → ceil = 2
        int days = TravelCosts.CalculateTimeDays(150f, 100f);
        AssertInt(days).IsEqual(2);
    }

    [TestCase]
    public void CalculateTimeDays_ExactDivision_ReturnsExactDays()
    {
        // 200 / 100 = 2.0 → 2
        int days = TravelCosts.CalculateTimeDays(200f, 100f);
        AssertInt(days).IsEqual(2);
    }

    [TestCase]
    public void CalculateTimeDays_ShortDistance_ReturnsMinimumOneDay()
    {
        // 50 / 100 = 0.5 → minimum 1
        int days = TravelCosts.CalculateTimeDays(50f, 100f);
        AssertInt(days).IsEqual(1);
    }

    [TestCase]
    public void CalculateTimeDays_ZeroDistance_ReturnsZero()
    {
        int days = TravelCosts.CalculateTimeDays(0f, 100f);
        AssertInt(days).IsEqual(0);
    }

    [TestCase]
    public void CalculateTimeDays_FastShip_ReducesDays()
    {
        // 300 / 150 = 2
        int days = TravelCosts.CalculateTimeDays(300f, 150f);
        AssertInt(days).IsEqual(2);
    }

    [TestCase]
    public void CalculateTimeDays_SlowShip_IncreasesDays()
    {
        // 150 / 50 = 3
        int days = TravelCosts.CalculateTimeDays(150f, 50f);
        AssertInt(days).IsEqual(3);
    }

    [TestCase]
    public void CalculateEncounterChance_ZeroHazard_ReturnsZero()
    {
        var route = new Route(0, 1, 100f) { HazardLevel = 0 };
        float chance = TravelCosts.CalculateEncounterChance(route);
        AssertFloat(chance).IsEqual(0f);
    }

    [TestCase]
    public void CalculateEncounterChance_MaxHazard_ReturnsFiftyPercent()
    {
        var route = new Route(0, 1, 100f) { HazardLevel = 5 };
        float chance = TravelCosts.CalculateEncounterChance(route);
        AssertFloat(chance).IsEqual(0.5f);
    }

    [TestCase]
    public void CalculateEncounterChance_MidHazard_ReturnsCorrectPercent()
    {
        var route = new Route(0, 1, 100f) { HazardLevel = 3 };
        float chance = TravelCosts.CalculateEncounterChance(route);
        AssertFloat(chance).IsEqual(0.3f);
    }

    [TestCase]
    public void CalculateEncounterChance_PatrolledTag_ReducesChance()
    {
        var route = new Route(0, 1, 100f) { HazardLevel = 3 };
        route.Tags.Add(WorldTags.Patrolled);

        float chance = TravelCosts.CalculateEncounterChance(route);

        // Base 30% - 10% patrolled = 20%
        AssertFloat(chance).IsEqualApprox(0.2f, 0.001f);
    }

    [TestCase]
    public void CalculateEncounterChance_DangerousTag_IncreasesChance()
    {
        var route = new Route(0, 1, 100f) { HazardLevel = 2 };
        route.Tags.Add(WorldTags.Dangerous);

        float chance = TravelCosts.CalculateEncounterChance(route);

        // Base 20% + 10% dangerous = 30%
        AssertFloat(chance).IsEqual(0.3f);
    }

    [TestCase]
    public void CalculateEncounterChance_HiddenTag_ReducesChance()
    {
        var route = new Route(0, 1, 100f) { HazardLevel = 2 };
        route.Tags.Add(WorldTags.Hidden);

        float chance = TravelCosts.CalculateEncounterChance(route);

        // Base 20% - 5% hidden = 15%
        AssertFloat(chance).IsEqual(0.15f);
    }

    [TestCase]
    public void CalculateEncounterChance_MultipleTags_CombinesModifiers()
    {
        var route = new Route(0, 1, 100f) { HazardLevel = 3 };
        route.Tags.Add(WorldTags.Dangerous);
        route.Tags.Add(WorldTags.Asteroid);

        float chance = TravelCosts.CalculateEncounterChance(route);

        // Base 30% + 10% dangerous + 5% asteroid = 45%
        AssertFloat(chance).IsEqualApprox(0.45f, 0.001f);
    }

    [TestCase]
    public void CalculateEncounterChance_CapsAtMaximum()
    {
        var route = new Route(0, 1, 100f) { HazardLevel = 5 };
        route.Tags.Add(WorldTags.Dangerous);
        route.Tags.Add(WorldTags.Blockaded);

        float chance = TravelCosts.CalculateEncounterChance(route);

        // Would be 50% + 10% + 20% = 80%, capped at 80%
        AssertFloat(chance).IsEqual(0.8f);
    }

    [TestCase]
    public void CalculateEncounterChance_CapsAtMinimum()
    {
        var route = new Route(0, 1, 100f) { HazardLevel = 0 };
        route.Tags.Add(WorldTags.Patrolled);
        route.Tags.Add(WorldTags.Hidden);

        float chance = TravelCosts.CalculateEncounterChance(route);

        // Would be 0% - 10% - 5% = -15%, capped at 0%
        AssertFloat(chance).IsEqual(0f);
    }

    [TestCase]
    public void CalculateEncounterChance_NullRoute_ReturnsZero()
    {
        float chance = TravelCosts.CalculateEncounterChance(null);
        AssertFloat(chance).IsEqual(0f);
    }

    [TestCase]
    public void CalculateEncounterChance_WithMetrics_HighSecurity_ReducesChance()
    {
        var route = new Route(0, 1, 100f) { HazardLevel = 3 };
        var fromMetrics = new SystemMetrics { SecurityLevel = 4 };
        var toMetrics = new SystemMetrics { SecurityLevel = 5 };

        float chance = TravelCosts.CalculateEncounterChance(route, fromMetrics, toMetrics);

        // Base 30% - 10% high security = 20%
        AssertFloat(chance).IsEqualApprox(0.2f, 0.001f);
    }

    [TestCase]
    public void CalculateEncounterChance_WithMetrics_LowSecurity_IncreasesChance()
    {
        var route = new Route(0, 1, 100f) { HazardLevel = 2 };
        var fromMetrics = new SystemMetrics { SecurityLevel = 1 };
        var toMetrics = new SystemMetrics { SecurityLevel = 0 };

        float chance = TravelCosts.CalculateEncounterChance(route, fromMetrics, toMetrics);

        // Base 20% + 10% low security = 30%
        AssertFloat(chance).IsEqual(0.3f);
    }

    [TestCase]
    public void CalculateEncounterChance_WithMetrics_HighCrime_IncreasesChance()
    {
        var route = new Route(0, 1, 100f) { HazardLevel = 2 };
        var fromMetrics = new SystemMetrics { CriminalActivity = 4 };
        var toMetrics = new SystemMetrics { CriminalActivity = 5 };

        float chance = TravelCosts.CalculateEncounterChance(route, fromMetrics, toMetrics);

        // Base 20% + 15% high crime = 35%
        AssertFloat(chance).IsEqualApprox(0.35f, 0.001f);
    }

    [TestCase]
    public void CalculateEncounterChance_WithMetrics_LowCrime_ReducesChance()
    {
        var route = new Route(0, 1, 100f) { HazardLevel = 2 };
        var fromMetrics = new SystemMetrics { CriminalActivity = 0 };
        var toMetrics = new SystemMetrics { CriminalActivity = 1 };

        float chance = TravelCosts.CalculateEncounterChance(route, fromMetrics, toMetrics);

        // Base 20% - 5% low crime = 15%
        AssertFloat(chance).IsEqual(0.15f);
    }

    [TestCase]
    public void CalculatePathfindingCost_IncludesDistanceAndHazard()
    {
        var route = new Route(0, 1, 100f) { HazardLevel = 2 };

        float cost = TravelCosts.CalculatePathfindingCost(route, 1.0f);

        // 100 + (2 * 50 * 1.0) = 200
        AssertFloat(cost).IsEqual(200f);
    }

    [TestCase]
    public void CalculatePathfindingCost_HighSafetyWeight_IncreasesHazardCost()
    {
        var route = new Route(0, 1, 100f) { HazardLevel = 2 };

        float cost = TravelCosts.CalculatePathfindingCost(route, 2.0f);

        // 100 + (2 * 50 * 2.0) = 300
        AssertFloat(cost).IsEqual(300f);
    }

    [TestCase]
    public void CalculatePathfindingCost_ZeroSafetyWeight_IgnoresHazard()
    {
        var route = new Route(0, 1, 100f) { HazardLevel = 5 };

        float cost = TravelCosts.CalculatePathfindingCost(route, 0f);

        // 100 + (5 * 50 * 0) = 100
        AssertFloat(cost).IsEqual(100f);
    }

    [TestCase]
    public void CalculatePathfindingCost_NullRoute_ReturnsMaxValue()
    {
        float cost = TravelCosts.CalculatePathfindingCost(null, 1.0f);
        AssertFloat(cost).IsEqual(float.MaxValue);
    }

    [TestCase]
    public void SuggestEncounterType_HighHazard_ReturnsPirate()
    {
        var route = new Route(0, 1, 100f) { HazardLevel = 4 };
        string type = TravelCosts.SuggestEncounterType(route);
        AssertString(type).IsEqual("pirate");
    }

    [TestCase]
    public void SuggestEncounterType_Patrolled_ReturnsPatrol()
    {
        var route = new Route(0, 1, 100f) { HazardLevel = 2 };
        route.Tags.Add(WorldTags.Patrolled);

        string type = TravelCosts.SuggestEncounterType(route);
        AssertString(type).IsEqual("patrol");
    }

    [TestCase]
    public void SuggestEncounterType_Hidden_ReturnsSmuggler()
    {
        var route = new Route(0, 1, 100f) { HazardLevel = 2 };
        route.Tags.Add(WorldTags.Hidden);

        string type = TravelCosts.SuggestEncounterType(route);
        AssertString(type).IsEqual("smuggler");
    }

    [TestCase]
    public void SuggestEncounterType_Dangerous_ReturnsPirate()
    {
        var route = new Route(0, 1, 100f) { HazardLevel = 2 };
        route.Tags.Add(WorldTags.Dangerous);

        string type = TravelCosts.SuggestEncounterType(route);
        AssertString(type).IsEqual("pirate");
    }

    [TestCase]
    public void SuggestEncounterType_LowHazard_ReturnsTrader()
    {
        var route = new Route(0, 1, 100f) { HazardLevel = 1 };
        string type = TravelCosts.SuggestEncounterType(route);
        AssertString(type).IsEqual("trader");
    }

    [TestCase]
    public void SuggestEncounterType_MidHazardNoTags_ReturnsRandom()
    {
        var route = new Route(0, 1, 100f) { HazardLevel = 2 };
        string type = TravelCosts.SuggestEncounterType(route);
        AssertString(type).IsEqual("random");
    }

    [TestCase]
    public void SuggestEncounterType_NullRoute_ReturnsRandom()
    {
        string type = TravelCosts.SuggestEncounterType(null);
        AssertString(type).IsEqual("random");
    }
}
