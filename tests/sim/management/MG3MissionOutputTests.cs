using GdUnit4;
using System.Collections.Generic;
using static GdUnit4.Assertions;

namespace FringeTactics.Tests;

/// <summary>
/// Tests for MG3 Mission Output processing.
/// Covers ApplyMissionOutput, events, and loot handling.
/// </summary>
[TestSuite]
public class MG3MissionOutputTests
{
    private CampaignState campaign;
    private EventBus eventBus;

    [BeforeTest]
    public void Setup()
    {
        eventBus = new EventBus();
        campaign = CampaignState.CreateNew();
        campaign.EventBus = eventBus;
    }

    // ========================================================================
    // CREW DEATH HANDLING
    // ========================================================================

    [TestCase]
    public void ApplyMissionOutput_AppliesDeaths()
    {
        var crew = campaign.GetAliveCrew()[0];
        int initialDeaths = campaign.TotalCrewDeaths;
        
        var output = new MissionOutput
        {
            Outcome = MissionOutcome.Victory,
            CrewOutcomes = new List<CrewOutcome>
            {
                new CrewOutcome
                {
                    CampaignCrewId = crew.Id,
                    Name = crew.Name,
                    Status = CrewFinalStatus.Dead
                }
            }
        };
        
        campaign.ApplyMissionOutput(output);
        
        AssertThat(crew.IsDead).IsTrue();
        AssertThat(campaign.TotalCrewDeaths).IsEqual(initialDeaths + 1);
    }

    [TestCase]
    public void ApplyMissionOutput_AppliesMIA()
    {
        var crew = campaign.GetAliveCrew()[0];
        
        var output = new MissionOutput
        {
            Outcome = MissionOutcome.Retreat,
            CrewOutcomes = new List<CrewOutcome>
            {
                new CrewOutcome
                {
                    CampaignCrewId = crew.Id,
                    Name = crew.Name,
                    Status = CrewFinalStatus.MIA
                }
            }
        };
        
        campaign.ApplyMissionOutput(output);
        
        // MIA is treated as dead
        AssertThat(crew.IsDead).IsTrue();
    }

    [TestCase]
    public void ApplyMissionOutput_PublishesCrewDiedEvent()
    {
        var crew = campaign.GetAliveCrew()[0];
        CrewDiedEvent? receivedEvent = null;
        eventBus.Subscribe<CrewDiedEvent>(e => receivedEvent = e);
        
        var output = new MissionOutput
        {
            Outcome = MissionOutcome.Victory,
            CrewOutcomes = new List<CrewOutcome>
            {
                new CrewOutcome
                {
                    CampaignCrewId = crew.Id,
                    Name = crew.Name,
                    Status = CrewFinalStatus.Dead
                }
            }
        };
        
        campaign.ApplyMissionOutput(output);
        
        AssertThat(receivedEvent).IsNotNull();
        AssertThat(receivedEvent.Value.CrewId).IsEqual(crew.Id);
        AssertThat(receivedEvent.Value.CrewName).IsEqual(crew.Name);
        AssertThat(receivedEvent.Value.Cause).IsEqual("killed_in_action");
    }

    // ========================================================================
    // INJURY HANDLING
    // ========================================================================

    [TestCase]
    public void ApplyMissionOutput_AppliesInjuries()
    {
        var crew = campaign.GetAliveCrew()[0];
        int initialInjuries = crew.Injuries.Count;
        
        var output = new MissionOutput
        {
            Outcome = MissionOutcome.Victory,
            CrewOutcomes = new List<CrewOutcome>
            {
                new CrewOutcome
                {
                    CampaignCrewId = crew.Id,
                    Name = crew.Name,
                    Status = CrewFinalStatus.Wounded,
                    NewInjuries = new List<string> { "broken_arm" }
                }
            }
        };
        
        campaign.ApplyMissionOutput(output);
        
        AssertThat(crew.Injuries.Count).IsEqual(initialInjuries + 1);
        AssertThat(crew.Injuries).Contains("broken_arm");
    }

    [TestCase]
    public void ApplyMissionOutput_PublishesCrewInjuredEvent()
    {
        var crew = campaign.GetAliveCrew()[0];
        CrewInjuredEvent? receivedEvent = null;
        eventBus.Subscribe<CrewInjuredEvent>(e => receivedEvent = e);
        
        var output = new MissionOutput
        {
            Outcome = MissionOutcome.Victory,
            CrewOutcomes = new List<CrewOutcome>
            {
                new CrewOutcome
                {
                    CampaignCrewId = crew.Id,
                    Name = crew.Name,
                    Status = CrewFinalStatus.Wounded,
                    NewInjuries = new List<string> { "concussion" }
                }
            }
        };
        
        campaign.ApplyMissionOutput(output);
        
        AssertThat(receivedEvent).IsNotNull();
        AssertThat(receivedEvent.Value.CrewId).IsEqual(crew.Id);
        AssertThat(receivedEvent.Value.InjuryType).IsEqual("concussion");
    }

    // ========================================================================
    // XP HANDLING
    // ========================================================================

    [TestCase]
    public void ApplyMissionOutput_AppliesXp()
    {
        var crew = campaign.GetAliveCrew()[0];
        int initialXp = crew.Xp;
        
        var output = new MissionOutput
        {
            Outcome = MissionOutcome.Victory,
            CrewOutcomes = new List<CrewOutcome>
            {
                new CrewOutcome
                {
                    CampaignCrewId = crew.Id,
                    Name = crew.Name,
                    Status = CrewFinalStatus.Alive,
                    SuggestedXp = 50
                }
            }
        };
        
        campaign.ApplyMissionOutput(output);
        
        AssertThat(crew.Xp).IsEqual(initialXp + 50);
    }

    [TestCase]
    public void ApplyMissionOutput_PublishesCrewLeveledUpEvent()
    {
        var crew = campaign.GetAliveCrew()[0];
        crew.Xp = 90; // Close to level up (100 XP per level)
        int oldLevel = crew.Level;
        
        CrewLeveledUpEvent? receivedEvent = null;
        eventBus.Subscribe<CrewLeveledUpEvent>(e => receivedEvent = e);
        
        var output = new MissionOutput
        {
            Outcome = MissionOutcome.Victory,
            CrewOutcomes = new List<CrewOutcome>
            {
                new CrewOutcome
                {
                    CampaignCrewId = crew.Id,
                    Name = crew.Name,
                    Status = CrewFinalStatus.Alive,
                    SuggestedXp = 20 // Should trigger level up
                }
            }
        };
        
        campaign.ApplyMissionOutput(output);
        
        AssertThat(receivedEvent).IsNotNull();
        AssertThat(receivedEvent.Value.OldLevel).IsEqual(oldLevel);
        AssertThat(receivedEvent.Value.NewLevel).IsEqual(oldLevel + 1);
    }

    // ========================================================================
    // AMMO CONSUMPTION
    // ========================================================================

    [TestCase]
    public void ApplyMissionOutput_ConsumesAmmoFromMultipleCrew()
    {
        campaign.Ammo = 200;
        var aliveCrew = campaign.GetAliveCrew();
        
        // Ensure we have at least 2 crew
        AssertThat(aliveCrew.Count).IsGreaterEqual(2);
        
        var output = new MissionOutput
        {
            Outcome = MissionOutcome.Victory,
            CrewOutcomes = new List<CrewOutcome>
            {
                new CrewOutcome
                {
                    CampaignCrewId = aliveCrew[0].Id,
                    Status = CrewFinalStatus.Alive,
                    AmmoUsed = 45
                },
                new CrewOutcome
                {
                    CampaignCrewId = aliveCrew[1].Id,
                    Status = CrewFinalStatus.Alive,
                    AmmoUsed = 30
                }
            }
        };
        
        campaign.ApplyMissionOutput(output);
        
        // Should have consumed 75 total ammo
        AssertThat(campaign.Ammo).IsEqual(125);
    }

    [TestCase]
    public void ApplyMissionOutput_PublishesAmmoResourceChangedEvent()
    {
        campaign.Ammo = 100;
        ResourceChangedEvent? receivedEvent = null;
        eventBus.Subscribe<ResourceChangedEvent>(e => 
        {
            if (e.ResourceType == ResourceTypes.Ammo && e.Reason == "mission_consumption")
                receivedEvent = e;
        });
        
        var output = new MissionOutput
        {
            Outcome = MissionOutcome.Victory,
            CrewOutcomes = new List<CrewOutcome>
            {
                new CrewOutcome
                {
                    CampaignCrewId = campaign.GetAliveCrew()[0].Id,
                    Status = CrewFinalStatus.Alive,
                    AmmoUsed = 25
                }
            }
        };
        
        campaign.ApplyMissionOutput(output);
        
        AssertThat(receivedEvent).IsNotNull();
        AssertThat(receivedEvent.Value.Delta).IsEqual(-25);
    }

    // ========================================================================
    // LOOT PROCESSING
    // ========================================================================

    [TestCase]
    public void ApplyMissionOutput_ProcessesMultipleLootItems()
    {
        campaign.Money = 100;
        campaign.Fuel = 50;
        campaign.Parts = 10;
        
        var output = new MissionOutput
        {
            Outcome = MissionOutcome.Victory,
            Loot = new List<LootItem>
            {
                LootItem.Credits(200),
                LootItem.Resource("fuel", 30),
                LootItem.Resource("parts", 15)
            }
        };
        
        campaign.ApplyMissionOutput(output);
        
        // Credits: 100 + 200 + 150 (victory bonus) = 450
        AssertThat(campaign.Money).IsGreaterEqual(400);
        AssertThat(campaign.Fuel).IsEqual(80);
        AssertThat(campaign.Parts).IsGreaterEqual(25); // 10 + 15 + victory bonus
    }

    [TestCase]
    public void ApplyMissionOutput_PublishesLootAcquiredEvent()
    {
        LootAcquiredEvent? receivedEvent = null;
        eventBus.Subscribe<LootAcquiredEvent>(e => receivedEvent = e);
        
        // Need inventory for item loot
        campaign.Inventory = new Inventory();
        var _ = ItemRegistry.Get("rifle"); // Force load
        
        var output = new MissionOutput
        {
            Outcome = MissionOutcome.Victory,
            Loot = new List<LootItem>
            {
                LootItem.Item("rifle", 1)
            }
        };
        
        campaign.ApplyMissionOutput(output);
        
        AssertThat(receivedEvent).IsNotNull();
        AssertThat(receivedEvent.Value.ItemDefId).IsEqual("rifle");
        AssertThat(receivedEvent.Value.Quantity).IsEqual(1);
    }

    // ========================================================================
    // JOB COMPLETION
    // ========================================================================

    [TestCase]
    public void ApplyMissionOutput_PublishesJobCompletedEvent_OnVictory()
    {
        // Setup job
        var job = new Job("test_job") { Title = "Test Job", Reward = new JobReward { Money = 100 } };
        campaign.CurrentJob = job;
        
        JobCompletedEvent? receivedEvent = null;
        eventBus.Subscribe<JobCompletedEvent>(e => receivedEvent = e);
        
        var output = new MissionOutput
        {
            Outcome = MissionOutcome.Victory
        };
        
        campaign.ApplyMissionOutput(output);
        
        AssertThat(receivedEvent).IsNotNull();
        AssertThat(receivedEvent.Value.Success).IsTrue();
        AssertThat(receivedEvent.Value.JobId).IsEqual(job.Id);
    }

    [TestCase]
    public void ApplyMissionOutput_PublishesJobCompletedEvent_OnDefeat()
    {
        // Setup job
        var job = new Job("test_job") { Title = "Test Job", Reward = new JobReward { Money = 100 } };
        campaign.CurrentJob = job;
        
        JobCompletedEvent? receivedEvent = null;
        eventBus.Subscribe<JobCompletedEvent>(e => receivedEvent = e);
        
        var output = new MissionOutput
        {
            Outcome = MissionOutcome.Defeat
        };
        
        campaign.ApplyMissionOutput(output);
        
        AssertThat(receivedEvent).IsNotNull();
        AssertThat(receivedEvent.Value.Success).IsFalse();
    }

    [TestCase]
    public void ApplyMissionOutput_ClearsCurrentJob_OnVictory()
    {
        var job = new Job("test_job") { Title = "Test Job", Reward = new JobReward { Money = 100 } };
        campaign.CurrentJob = job;
        AssertThat(campaign.CurrentJob).IsNotNull();
        
        var output = new MissionOutput
        {
            Outcome = MissionOutcome.Victory
        };
        
        campaign.ApplyMissionOutput(output);
        
        AssertThat(campaign.CurrentJob).IsNull();
    }

    // ========================================================================
    // MISSION OUTCOME STATS
    // ========================================================================

    [TestCase]
    public void ApplyMissionOutput_IncrementsMissionsCompleted_OnVictory()
    {
        int initial = campaign.MissionsCompleted;
        
        var output = new MissionOutput
        {
            Outcome = MissionOutcome.Victory
        };
        
        campaign.ApplyMissionOutput(output);
        
        AssertThat(campaign.MissionsCompleted).IsEqual(initial + 1);
    }

    [TestCase]
    public void ApplyMissionOutput_IncrementsMissionsFailed_OnDefeat()
    {
        int initial = campaign.MissionsFailed;
        
        var output = new MissionOutput
        {
            Outcome = MissionOutcome.Defeat
        };
        
        campaign.ApplyMissionOutput(output);
        
        AssertThat(campaign.MissionsFailed).IsEqual(initial + 1);
    }

    [TestCase]
    public void ApplyMissionOutput_IncrementsMissionsFailed_OnRetreat()
    {
        int initial = campaign.MissionsFailed;
        
        var output = new MissionOutput
        {
            Outcome = MissionOutcome.Retreat
        };
        
        campaign.ApplyMissionOutput(output);
        
        AssertThat(campaign.MissionsFailed).IsEqual(initial + 1);
    }
}
