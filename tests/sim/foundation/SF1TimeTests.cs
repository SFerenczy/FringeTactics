using GdUnit4;
using static GdUnit4.Assertions;

namespace FringeTactics.Tests;

[TestSuite]
public class SF1TimeTests
{
    [TestCase]
    public void NewCampaignTime_StartsAtDayOne()
    {
        var time = new CampaignTime();
        AssertInt(time.CurrentDay).IsEqual(1);
        AssertInt(time.DaysElapsed).IsEqual(0);
    }

    [TestCase]
    public void Constructor_WithStartDay_SetsCurrentDay()
    {
        var time = new CampaignTime(10);
        AssertInt(time.CurrentDay).IsEqual(10);
        AssertInt(time.DaysElapsed).IsEqual(9);
    }

    [TestCase]
    public void Constructor_WithNegativeStartDay_ClampsToOne()
    {
        var time = new CampaignTime(-5);
        AssertInt(time.CurrentDay).IsEqual(1);
    }

    [TestCase]
    public void AdvanceDays_IncrementsCurrentDay()
    {
        var time = new CampaignTime();
        time.AdvanceDays(3);
        AssertInt(time.CurrentDay).IsEqual(4);
        AssertInt(time.DaysElapsed).IsEqual(3);
    }

    [TestCase]
    public void AdvanceDays_ReturnsNewDay()
    {
        var time = new CampaignTime();
        int result = time.AdvanceDays(5);
        AssertInt(result).IsEqual(6);
    }

    [TestCase]
    public void AdvanceDays_EmitsEvent()
    {
        var time = new CampaignTime();
        int eventOldDay = 0;
        int eventNewDay = 0;
        time.DayAdvanced += (old, @new) => { eventOldDay = old; eventNewDay = @new; };

        time.AdvanceDays(5);

        AssertInt(eventOldDay).IsEqual(1);
        AssertInt(eventNewDay).IsEqual(6);
    }

    [TestCase]
    public void AdvanceDays_RejectsNegative()
    {
        var time = new CampaignTime();
        time.AdvanceDays(-5);
        AssertInt(time.CurrentDay).IsEqual(1);
    }

    [TestCase]
    public void AdvanceDays_RejectsZero()
    {
        var time = new CampaignTime();
        time.AdvanceDays(0);
        AssertInt(time.CurrentDay).IsEqual(1);
    }

    [TestCase]
    public void HasDeadlinePassed_ReturnsTrueWhenPast()
    {
        var time = new CampaignTime();
        time.AdvanceDays(10); // Now day 11

        AssertBool(time.HasDeadlinePassed(5)).IsTrue();
        AssertBool(time.HasDeadlinePassed(10)).IsTrue();
        AssertBool(time.HasDeadlinePassed(11)).IsFalse();
        AssertBool(time.HasDeadlinePassed(15)).IsFalse();
    }

    [TestCase]
    public void DaysUntilDeadline_ReturnsCorrectValue()
    {
        var time = new CampaignTime();
        time.AdvanceDays(4); // Now day 5

        AssertInt(time.DaysUntilDeadline(3)).IsEqual(0);  // Past
        AssertInt(time.DaysUntilDeadline(5)).IsEqual(0);  // Today
        AssertInt(time.DaysUntilDeadline(10)).IsEqual(5); // Future
    }

    [TestCase]
    public void FormatCurrentDay_ReturnsCorrectString()
    {
        var time = new CampaignTime();
        AssertString(time.FormatCurrentDay()).IsEqual("Day 1");

        time.AdvanceDays(14);
        AssertString(time.FormatCurrentDay()).IsEqual("Day 15");
    }

    [TestCase]
    public void FormatDuration_HandlesSingularAndPlural()
    {
        AssertString(CampaignTime.FormatDuration(1)).IsEqual("1 day");
        AssertString(CampaignTime.FormatDuration(5)).IsEqual("5 days");
        AssertString(CampaignTime.FormatDuration(0)).IsEqual("0 days");
    }

    [TestCase]
    public void FormatDeadlineStatus_ShowsCorrectStatus()
    {
        var time = new CampaignTime();
        time.AdvanceDays(4); // Now day 5

        AssertString(time.FormatDeadlineStatus(3)).IsEqual("OVERDUE");
        AssertString(time.FormatDeadlineStatus(5)).IsEqual("Due today");
        AssertString(time.FormatDeadlineStatus(6)).IsEqual("1 day left");
        AssertString(time.FormatDeadlineStatus(10)).IsEqual("5 days left");
    }

    [TestCase]
    public void SaveRestore_RoundTrip()
    {
        var time = new CampaignTime();
        time.AdvanceDays(15);

        var state = time.GetState();

        var time2 = new CampaignTime();
        time2.RestoreState(state);

        AssertInt(time2.CurrentDay).IsEqual(16);
    }
}

[TestSuite]
public class CampaignStateTimeIntegrationTests
{
    [TestCase]
    public void CampaignState_HasTimeProperty()
    {
        var campaign = CampaignState.CreateNew();
        AssertObject(campaign.Time).IsNotNull();
        AssertInt(campaign.Time.CurrentDay).IsEqual(1);
    }

    [TestCase]
    public void NewCampaignState_StartsAtDayOne()
    {
        var campaign = new CampaignState();
        AssertObject(campaign.Time).IsNotNull();
        AssertInt(campaign.Time.CurrentDay).IsEqual(1);
    }

    [TestCase]
    public void CampaignTime_CanBeAdvancedThroughCampaignState()
    {
        var campaign = CampaignState.CreateNew();
        campaign.Time.AdvanceDays(5);
        AssertInt(campaign.Time.CurrentDay).IsEqual(6);
    }

    [TestCase]
    public void ConsumeMissionResources_AdvancesTime()
    {
        var campaign = CampaignState.CreateNew();
        int startDay = campaign.Time.CurrentDay;

        campaign.ConsumeMissionResources();

        AssertInt(campaign.Time.CurrentDay).IsEqual(startDay + CampaignState.MISSION_TIME_DAYS);
    }

    [TestCase]
    public void Rest_AdvancesTime()
    {
        var campaign = CampaignState.CreateNew();
        int startDay = campaign.Time.CurrentDay;

        campaign.Rest();

        AssertInt(campaign.Time.CurrentDay).IsEqual(startDay + CampaignState.REST_TIME_DAYS);
    }

    [TestCase]
    public void Rest_HealsInjuredCrew()
    {
        var campaign = CampaignState.CreateNew();
        var crew = campaign.Crew[0];
        crew.AddInjury("Wounded");
        AssertInt(crew.Injuries.Count).IsEqual(1);

        int healed = campaign.Rest();

        AssertInt(healed).IsEqual(1);
        AssertInt(crew.Injuries.Count).IsEqual(0);
    }

    [TestCase]
    public void Rest_HealsUpToLimit()
    {
        var campaign = CampaignState.CreateNew();
        // Injure multiple crew
        campaign.Crew[0].AddInjury("Wounded");
        campaign.Crew[1].AddInjury("Wounded");
        campaign.Crew[2].AddInjury("Wounded");

        int healed = campaign.Rest();

        // Should only heal REST_HEAL_AMOUNT (1) per rest
        AssertInt(healed).IsEqual(CampaignState.REST_HEAL_AMOUNT);
    }

    [TestCase]
    public void ShouldRest_TrueWhenCrewInjured()
    {
        var campaign = CampaignState.CreateNew();
        AssertBool(campaign.ShouldRest()).IsFalse();

        campaign.Crew[0].AddInjury("Wounded");
        AssertBool(campaign.ShouldRest()).IsTrue();
    }
}

[TestSuite]
public class JobDeadlineTests
{
    [TestCase]
    public void Job_HasDeadlineFields()
    {
        var job = new Job("test_job");
        AssertInt(job.DeadlineDays).IsEqual(0);
        AssertInt(job.DeadlineDay).IsEqual(0);
        AssertBool(job.HasDeadline).IsFalse();
    }

    [TestCase]
    public void Job_HasDeadline_TrueWhenSet()
    {
        var job = new Job("test_job") { DeadlineDay = 10 };
        AssertBool(job.HasDeadline).IsTrue();
    }

    [TestCase]
    public void JobSystem_GeneratesJobsWithDeadlines()
    {
        var campaign = CampaignState.CreateNew(12345);
        var rng = new System.Random(12345);
        var jobs = JobSystem.GenerateJobsForNode(campaign, 0, rng);

        AssertBool(jobs.Count > 0).IsTrue();
        foreach (var job in jobs)
        {
            AssertBool(job.DeadlineDays > 0).IsTrue();
        }
    }

    [TestCase]
    public void AcceptJob_SetsAbsoluteDeadline()
    {
        var campaign = CampaignState.CreateNew();
        campaign.Time.AdvanceDays(5); // Now day 6

        if (campaign.AvailableJobs.Count > 0)
        {
            var job = campaign.AvailableJobs[0];
            int relativeDays = job.DeadlineDays;

            campaign.AcceptJob(job);

            AssertInt(job.DeadlineDay).IsEqual(6 + relativeDays);
            AssertBool(job.HasDeadline).IsTrue();
        }
    }

    [TestCase]
    public void DeadlineDay_CanBeCheckedWithCampaignTime()
    {
        var campaign = CampaignState.CreateNew();

        if (campaign.AvailableJobs.Count > 0)
        {
            var job = campaign.AvailableJobs[0];
            campaign.AcceptJob(job);

            // Initially deadline should not have passed
            AssertBool(campaign.Time.HasDeadlinePassed(job.DeadlineDay)).IsFalse();

            // Advance past deadline
            campaign.Time.AdvanceDays(job.DeadlineDays + 1);
            AssertBool(campaign.Time.HasDeadlinePassed(job.DeadlineDay)).IsTrue();
        }
    }
}

[TestSuite]
public class TravelTimeTests
{
    [TestCase]
    public void TravelSystem_CalculateTravelDays_ReturnsMinimumOne()
    {
        var world = WorldState.CreateSingleHub();
        // Even for very close nodes, minimum is 1 day
        int days = TravelSystem.CalculateTravelDays(world, 0, 0);
        AssertBool(days >= TravelSystem.MIN_TRAVEL_DAYS).IsTrue();
    }

    [TestCase]
    public void TravelSystem_Travel_AdvancesTime()
    {
        var campaign = CampaignState.CreateNew();
        int startDay = campaign.Time.CurrentDay;

        // Find a connected system
        var currentSystem = campaign.GetCurrentSystem();
        if (currentSystem != null && currentSystem.Connections.Count > 0)
        {
            int targetId = currentSystem.Connections[0];
            int expectedDays = TravelSystem.CalculateTravelDays(campaign.World, campaign.CurrentNodeId, targetId);

            var result = TravelSystem.Travel(campaign, targetId);

            AssertObject(result).IsEqual(TravelResult.Success);
            AssertInt(campaign.Time.CurrentDay).IsEqual(startDay + expectedDays);
        }
    }

    [TestCase]
    public void TravelSystem_GetTravelCostSummary_IncludesTimeAndFuel()
    {
        var campaign = CampaignState.CreateNew();

        var currentSystem = campaign.GetCurrentSystem();
        if (currentSystem != null && currentSystem.Connections.Count > 0)
        {
            int targetId = currentSystem.Connections[0];
            string summary = TravelSystem.GetTravelCostSummary(campaign, targetId);

            // Should contain both fuel and day information
            AssertBool(summary.Contains("fuel")).IsTrue();
            AssertBool(summary.Contains("day")).IsTrue();
        }
    }

    [TestCase]
    public void TravelSystem_MultipleTravels_AccumulateTime()
    {
        var campaign = CampaignState.CreateNew();
        int startDay = campaign.Time.CurrentDay;
        int totalDays = 0;

        var currentSystem = campaign.GetCurrentSystem();
        if (currentSystem != null && currentSystem.Connections.Count > 0)
        {
            // Travel to first connected system
            int firstTarget = currentSystem.Connections[0];
            totalDays += TravelSystem.CalculateTravelDays(campaign.World, campaign.CurrentNodeId, firstTarget);
            TravelSystem.Travel(campaign, firstTarget);

            // Travel back
            totalDays += TravelSystem.CalculateTravelDays(campaign.World, campaign.CurrentNodeId, 0);
            TravelSystem.Travel(campaign, 0);

            AssertInt(campaign.Time.CurrentDay).IsEqual(startDay + totalDays);
        }
    }
}
