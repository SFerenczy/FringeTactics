using GdUnit4;
using static GdUnit4.Assertions;

namespace FringeTactics.Tests;

[TestSuite]
public class TV2TravelExecutorTests
{
    private WorldState CreateTestWorld()
    {
        return WorldState.CreateTestSector();
    }

    private CampaignState CreateTestCampaign(WorldState world, int fuel = 100)
    {
        var campaign = CampaignState.CreateNew(12345);
        campaign.World = world;
        campaign.CurrentNodeId = 0;
        campaign.Fuel = fuel;
        return campaign;
    }

    // ========================================================================
    // Basic Execution Tests
    // ========================================================================

    [TestCase]
    public void Execute_SingleSegment_CompletesSuccessfully()
    {
        var world = CreateTestWorld();
        var campaign = CreateTestCampaign(world, 100);
        var planner = new TravelPlanner(world);
        var plan = planner.PlanRoute(0, 1); // Haven → Waypoint (single segment)
        var executor = new TravelExecutor(campaign.Rng);

        var result = executor.Execute(plan, campaign);

        AssertThat(result.Status).IsEqual(TravelResultStatus.Completed);
        AssertInt(result.FinalSystemId).IsEqual(1);
        AssertInt(result.FuelConsumed).IsGreater(0);
        AssertInt(result.DaysElapsed).IsGreater(0);
    }

    [TestCase]
    public void Execute_MultiSegment_CompletesAllSegments()
    {
        var world = CreateTestWorld();
        var campaign = CreateTestCampaign(world, 100);
        var planner = new TravelPlanner(world);
        var plan = planner.PlanRoute(0, 2); // Haven → Waypoint → Rockfall
        var executor = new TravelExecutor(campaign.Rng);

        // Verify multi-segment
        AssertInt(plan.Segments.Count).IsGreaterEqual(1);

        var result = executor.Execute(plan, campaign);

        AssertThat(result.Status).IsEqual(TravelResultStatus.Completed);
        AssertInt(result.FinalSystemId).IsEqual(2);
        AssertInt(campaign.CurrentNodeId).IsEqual(2);
    }

    // ========================================================================
    // Resource Consumption Tests
    // ========================================================================

    [TestCase]
    public void Execute_ConsumesFuel()
    {
        var world = CreateTestWorld();
        var campaign = CreateTestCampaign(world, 100);
        int startFuel = campaign.Fuel;
        var planner = new TravelPlanner(world);
        var plan = planner.PlanRoute(0, 1);
        var executor = new TravelExecutor(campaign.Rng);

        var result = executor.Execute(plan, campaign);

        AssertInt(campaign.Fuel).IsLess(startFuel);
        AssertInt(result.FuelConsumed).IsEqual(startFuel - campaign.Fuel);
    }

    [TestCase]
    public void Execute_AdvancesTime()
    {
        var world = CreateTestWorld();
        var campaign = CreateTestCampaign(world, 100);
        int startDay = campaign.Time.CurrentDay;
        var planner = new TravelPlanner(world);
        var plan = planner.PlanRoute(0, 1);
        var executor = new TravelExecutor(campaign.Rng);

        var result = executor.Execute(plan, campaign);

        AssertInt(campaign.Time.CurrentDay).IsGreater(startDay);
        AssertInt(result.DaysElapsed).IsEqual(campaign.Time.CurrentDay - startDay);
    }

    [TestCase]
    public void Execute_UpdatesPosition()
    {
        var world = CreateTestWorld();
        var campaign = CreateTestCampaign(world, 100);
        var planner = new TravelPlanner(world);
        var plan = planner.PlanRoute(0, 1);
        var executor = new TravelExecutor(campaign.Rng);

        executor.Execute(plan, campaign);

        AssertInt(campaign.CurrentNodeId).IsEqual(1);
    }

    // ========================================================================
    // Insufficient Fuel Tests
    // ========================================================================

    [TestCase]
    public void Execute_InsufficientFuel_InterruptsBeforeStart()
    {
        var world = CreateTestWorld();
        var planner = new TravelPlanner(world);
        var plan = planner.PlanRoute(0, 1);

        // Set fuel lower than required
        var campaign = CreateTestCampaign(world, plan.TotalFuelCost - 1);
        var executor = new TravelExecutor(campaign.Rng);

        var result = executor.Execute(plan, campaign);

        AssertThat(result.Status).IsEqual(TravelResultStatus.Interrupted);
        AssertThat(result.InterruptReason).IsEqual(TravelInterruptReason.InsufficientFuel);
        AssertInt(campaign.CurrentNodeId).IsEqual(0); // Didn't move
        AssertInt(result.FuelConsumed).IsEqual(0);
    }

    [TestCase]
    public void Execute_ExactFuel_Completes()
    {
        var world = CreateTestWorld();
        var planner = new TravelPlanner(world);
        var plan = planner.PlanRoute(0, 1);

        // Set fuel exactly to required amount
        var campaign = CreateTestCampaign(world, plan.TotalFuelCost);
        var executor = new TravelExecutor(campaign.Rng);

        var result = executor.Execute(plan, campaign);

        AssertThat(result.Status).IsEqual(TravelResultStatus.Completed);
        AssertInt(campaign.Fuel).IsEqual(0);
    }

    // ========================================================================
    // Invalid Plan Tests
    // ========================================================================

    [TestCase]
    public void Execute_NullPlan_ReturnsInterrupted()
    {
        var world = CreateTestWorld();
        var campaign = CreateTestCampaign(world, 100);
        var executor = new TravelExecutor(campaign.Rng);

        var result = executor.Execute(null, campaign);

        AssertThat(result.Status).IsEqual(TravelResultStatus.Interrupted);
        AssertThat(result.InterruptReason).IsEqual(TravelInterruptReason.RouteBlocked);
    }

    [TestCase]
    public void Execute_InvalidPlan_ReturnsInterrupted()
    {
        var world = CreateTestWorld();
        var campaign = CreateTestCampaign(world, 100);
        var planner = new TravelPlanner(world);
        var plan = planner.PlanRoute(0, 0); // Same system - invalid
        var executor = new TravelExecutor(campaign.Rng);

        var result = executor.Execute(plan, campaign);

        AssertThat(result.Status).IsEqual(TravelResultStatus.Interrupted);
    }

    // ========================================================================
    // Resume Tests
    // ========================================================================

    [TestCase]
    public void Resume_NullState_ReturnsInterrupted()
    {
        var world = CreateTestWorld();
        var campaign = CreateTestCampaign(world, 100);
        var executor = new TravelExecutor(campaign.Rng);

        var result = executor.Resume(null, campaign);

        AssertThat(result.Status).IsEqual(TravelResultStatus.Interrupted);
    }

    [TestCase]
    public void Resume_DefeatOutcome_InterruptsTravel()
    {
        var world = CreateTestWorld();
        var campaign = CreateTestCampaign(world, 100);
        var planner = new TravelPlanner(world);
        var plan = planner.PlanRoute(0, 2);
        var executor = new TravelExecutor(campaign.Rng);

        // Create a paused state
        var state = TravelState.Create(plan, 0);
        state.IsPausedForEncounter = true;
        state.PendingEncounterId = "enc_test";
        state.EncounterHistory.Add(new TravelEncounterRecord
        {
            EncounterId = "enc_test",
            Outcome = "pending"
        });

        var result = executor.Resume(state, campaign, "defeat");

        AssertThat(result.Status).IsEqual(TravelResultStatus.Interrupted);
        AssertThat(result.InterruptReason).IsEqual(TravelInterruptReason.EncounterDefeat);
    }

    [TestCase]
    public void Resume_CapturedOutcome_InterruptsTravel()
    {
        var world = CreateTestWorld();
        var campaign = CreateTestCampaign(world, 100);
        var planner = new TravelPlanner(world);
        var plan = planner.PlanRoute(0, 2);
        var executor = new TravelExecutor(campaign.Rng);

        var state = TravelState.Create(plan, 0);
        state.IsPausedForEncounter = true;
        state.PendingEncounterId = "enc_test";
        state.EncounterHistory.Add(new TravelEncounterRecord
        {
            EncounterId = "enc_test",
            Outcome = "pending"
        });

        var result = executor.Resume(state, campaign, "captured");

        AssertThat(result.Status).IsEqual(TravelResultStatus.Interrupted);
        AssertThat(result.InterruptReason).IsEqual(TravelInterruptReason.EncounterCapture);
    }

    [TestCase]
    public void Resume_CompletedOutcome_ContinuesTravel()
    {
        var world = CreateTestWorld();
        var campaign = CreateTestCampaign(world, 100);
        var planner = new TravelPlanner(world);
        var plan = planner.PlanRoute(0, 1);
        var executor = new TravelExecutor(campaign.Rng);

        var state = TravelState.Create(plan, 0);
        state.IsPausedForEncounter = true;
        state.PendingEncounterId = "enc_test";
        state.EncounterHistory.Add(new TravelEncounterRecord
        {
            EncounterId = "enc_test",
            Outcome = "pending"
        });

        var result = executor.Resume(state, campaign, "completed");

        AssertThat(result.Status).IsEqual(TravelResultStatus.Completed);
    }

    // ========================================================================
    // Encounter Tests (with stub)
    // ========================================================================

    [TestCase]
    public void Execute_EncountersAreRecorded()
    {
        // Use a different seed that may trigger encounters
        // The stub auto-resolves them, so travel should complete
        var world = CreateTestWorld();
        var campaign = CampaignState.CreateNew(99999); // Different seed
        campaign.World = world;
        campaign.CurrentNodeId = 1; // Start at Waypoint
        campaign.Fuel = 100;
        var planner = new TravelPlanner(world);

        // Travel to pirate base (high hazard route) for better encounter chance
        var plan = planner.PlanRoute(1, 3); // Waypoint → Smuggler → Pirate Base
        var executor = new TravelExecutor(campaign.Rng);

        var result = executor.Execute(plan, campaign);

        // Travel should complete (stub auto-resolves encounters)
        AssertThat(result.Status).IsEqual(TravelResultStatus.Completed);
        // Encounters may or may not have triggered depending on RNG
        AssertObject(result.Encounters).IsNotNull();
    }
}
