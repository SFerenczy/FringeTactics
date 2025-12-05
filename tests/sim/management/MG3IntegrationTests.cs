using GdUnit4;
using System.Collections.Generic;
using static GdUnit4.Assertions;

namespace FringeTactics.Tests;

/// <summary>
/// Integration tests for MG3 full mission flow.
/// Tests the complete cycle from campaign -> tactical -> campaign.
/// </summary>
[TestSuite]
public class MG3IntegrationTests
{
    private CampaignState campaign;
    private EventBus eventBus;
    private Job testJob;

    [BeforeTest]
    public void Setup()
    {
        eventBus = new EventBus();
        campaign = CampaignState.CreateNew();
        campaign.EventBus = eventBus;
        
        // Create a test job
        var config = MissionConfig.CreateTestMission();
        testJob = new Job("integration_test_job")
        {
            Title = "Integration Test Mission",
            Description = "Test the full mission flow",
            MissionConfig = config,
            Difficulty = JobDifficulty.Medium,
            ContractType = ContractType.Assault,
            TargetNodeId = campaign.CurrentNodeId,
            Reward = new JobReward { Money = 500, Parts = 20 }
        };
    }

    // ========================================================================
    // FULL FLOW: VICTORY
    // ========================================================================

    [TestCase]
    public void FullMissionFlow_Victory_AppliesRewards()
    {
        // Setup
        campaign.CurrentJob = testJob;
        int initialMoney = campaign.Money;
        int initialMissionsCompleted = campaign.MissionsCompleted;
        
        // Build mission input
        var input = MissionInputBuilder.Build(campaign, testJob);
        AssertThat(input.Crew.Count).IsGreater(0);
        
        // Simulate mission completion
        var output = new MissionOutput
        {
            Outcome = MissionOutcome.Victory,
            MissionId = input.MissionId,
            EnemiesKilled = 3,
            CrewOutcomes = new List<CrewOutcome>()
        };
        
        // Add outcomes for deployed crew
        foreach (var deployment in input.Crew)
        {
            output.CrewOutcomes.Add(new CrewOutcome
            {
                CampaignCrewId = deployment.CampaignCrewId,
                Name = deployment.Name,
                Status = CrewFinalStatus.Alive,
                SuggestedXp = 25,
                AmmoUsed = 15
            });
        }
        
        // Apply output
        campaign.ApplyMissionOutput(output);
        
        // Verify rewards applied
        AssertThat(campaign.Money).IsGreater(initialMoney);
        AssertThat(campaign.MissionsCompleted).IsEqual(initialMissionsCompleted + 1);
        AssertThat(campaign.CurrentJob).IsNull(); // Job cleared
    }

    [TestCase]
    public void FullMissionFlow_Victory_AppliesXpToAllCrew()
    {
        campaign.CurrentJob = testJob;
        
        // Build mission input
        var input = MissionInputBuilder.Build(campaign, testJob);
        
        // Track initial XP
        var initialXp = new Dictionary<int, int>();
        foreach (var deployment in input.Crew)
        {
            var crew = campaign.GetCrewById(deployment.CampaignCrewId);
            initialXp[deployment.CampaignCrewId] = crew.Xp;
        }
        
        // Simulate mission with XP gains
        var output = new MissionOutput
        {
            Outcome = MissionOutcome.Victory,
            CrewOutcomes = new List<CrewOutcome>()
        };
        
        foreach (var deployment in input.Crew)
        {
            output.CrewOutcomes.Add(new CrewOutcome
            {
                CampaignCrewId = deployment.CampaignCrewId,
                Name = deployment.Name,
                Status = CrewFinalStatus.Alive,
                SuggestedXp = 30
            });
        }
        
        campaign.ApplyMissionOutput(output);
        
        // Verify all crew gained XP
        foreach (var deployment in input.Crew)
        {
            var crew = campaign.GetCrewById(deployment.CampaignCrewId);
            AssertThat(crew.Xp).IsEqual(initialXp[deployment.CampaignCrewId] + 30);
        }
    }

    // ========================================================================
    // FULL FLOW: DEFEAT
    // ========================================================================

    [TestCase]
    public void FullMissionFlow_Defeat_AppliesPenalties()
    {
        campaign.CurrentJob = testJob;
        int initialMissionsFailed = campaign.MissionsFailed;
        
        var input = MissionInputBuilder.Build(campaign, testJob);
        
        var output = new MissionOutput
        {
            Outcome = MissionOutcome.Defeat,
            CrewOutcomes = new List<CrewOutcome>()
        };
        
        // All crew dead
        foreach (var deployment in input.Crew)
        {
            output.CrewOutcomes.Add(new CrewOutcome
            {
                CampaignCrewId = deployment.CampaignCrewId,
                Name = deployment.Name,
                Status = CrewFinalStatus.Dead
            });
        }
        
        campaign.ApplyMissionOutput(output);
        
        AssertThat(campaign.MissionsFailed).IsEqual(initialMissionsFailed + 1);
        AssertThat(campaign.CurrentJob).IsNull(); // Job cleared even on failure
    }

    // ========================================================================
    // FULL FLOW: RETREAT
    // ========================================================================

    [TestCase]
    public void FullMissionFlow_Retreat_PartialPenalty()
    {
        campaign.CurrentJob = testJob;
        int initialMissionsFailed = campaign.MissionsFailed;
        
        var input = MissionInputBuilder.Build(campaign, testJob);
        
        var output = new MissionOutput
        {
            Outcome = MissionOutcome.Retreat,
            CrewOutcomes = new List<CrewOutcome>()
        };
        
        // Some crew survived, some MIA
        bool first = true;
        foreach (var deployment in input.Crew)
        {
            output.CrewOutcomes.Add(new CrewOutcome
            {
                CampaignCrewId = deployment.CampaignCrewId,
                Name = deployment.Name,
                Status = first ? CrewFinalStatus.Alive : CrewFinalStatus.MIA
            });
            first = false;
        }
        
        campaign.ApplyMissionOutput(output);
        
        AssertThat(campaign.MissionsFailed).IsEqual(initialMissionsFailed + 1);
    }

    // ========================================================================
    // CREW STATS FLOW
    // ========================================================================

    [TestCase]
    public void FullMissionFlow_CrewStatsAffectTactical()
    {
        campaign.CurrentJob = testJob;
        
        // Modify a crew member's stats
        var crew = campaign.GetAliveCrew()[0];
        crew.Grit = 8;  // High grit = more HP
        crew.Aim = 6;   // High aim = better accuracy
        
        var input = MissionInputBuilder.Build(campaign, testJob);
        
        // Find the deployment for this crew
        CrewDeployment deployment = null;
        foreach (var d in input.Crew)
        {
            if (d.CampaignCrewId == crew.Id)
            {
                deployment = d;
                break;
            }
        }
        
        AssertThat(deployment).IsNotNull();
        
        // Verify stats were mapped
        int expectedHp = crew.GetMaxHp();
        float expectedAccuracy = 0.7f + (crew.GetEffectiveStat(CrewStatType.Aim) * 0.02f);
        
        AssertThat(deployment.MaxHp).IsEqual(expectedHp);
        AssertThat(deployment.Accuracy).IsEqual(expectedAccuracy);
    }

    [TestCase]
    public void FullMissionFlow_EquipmentAffectsTactical()
    {
        campaign.CurrentJob = testJob;
        
        // Setup inventory and equip a weapon
        campaign.Inventory = new Inventory();
        var _ = ItemRegistry.Get("rifle"); // Force load
        
        var crew = campaign.GetAliveCrew()[0];
        var item = campaign.Inventory.AddItem("smg", 1, 100);
        if (item != null)
        {
            crew.EquippedWeaponId = item.Id;
        }
        
        var input = MissionInputBuilder.Build(campaign, testJob);
        
        // Find the deployment for this crew
        CrewDeployment deployment = null;
        foreach (var d in input.Crew)
        {
            if (d.CampaignCrewId == crew.Id)
            {
                deployment = d;
                break;
            }
        }
        
        AssertThat(deployment).IsNotNull();
        
        // Should have SMG equipped
        AssertThat(deployment.WeaponId).IsEqual("smg");
    }

    // ========================================================================
    // AMMO FLOW
    // ========================================================================

    [TestCase]
    public void FullMissionFlow_AmmoTrackedThroughMission()
    {
        campaign.CurrentJob = testJob;
        campaign.Ammo = 500;
        int initialAmmo = campaign.Ammo;
        
        var input = MissionInputBuilder.Build(campaign, testJob);
        
        // Verify reserve ammo was set based on campaign pool
        foreach (var deployment in input.Crew)
        {
            AssertThat(deployment.ReserveAmmo).IsGreater(0);
        }
        
        // Simulate mission with ammo usage
        var output = new MissionOutput
        {
            Outcome = MissionOutcome.Victory,
            CrewOutcomes = new List<CrewOutcome>()
        };
        
        int totalAmmoUsed = 0;
        foreach (var deployment in input.Crew)
        {
            int ammoUsed = 20;
            totalAmmoUsed += ammoUsed;
            output.CrewOutcomes.Add(new CrewOutcome
            {
                CampaignCrewId = deployment.CampaignCrewId,
                Name = deployment.Name,
                Status = CrewFinalStatus.Alive,
                AmmoUsed = ammoUsed
            });
        }
        
        campaign.ApplyMissionOutput(output);
        
        // Verify ammo was consumed
        AssertThat(campaign.Ammo).IsEqual(initialAmmo - totalAmmoUsed);
    }

    // ========================================================================
    // LOOT FLOW
    // ========================================================================

    [TestCase]
    public void FullMissionFlow_LootAddedToInventory()
    {
        campaign.CurrentJob = testJob;
        campaign.Inventory = new Inventory();
        var _ = ItemRegistry.Get("rifle"); // Force load
        
        int initialItemCount = campaign.Inventory.Items.Count;
        
        var input = MissionInputBuilder.Build(campaign, testJob);
        
        var output = new MissionOutput
        {
            Outcome = MissionOutcome.Victory,
            Loot = new List<LootItem>
            {
                LootItem.Item("medkit", 2),
                LootItem.Credits(100)
            },
            CrewOutcomes = new List<CrewOutcome>()
        };
        
        foreach (var deployment in input.Crew)
        {
            output.CrewOutcomes.Add(new CrewOutcome
            {
                CampaignCrewId = deployment.CampaignCrewId,
                Status = CrewFinalStatus.Alive
            });
        }
        
        campaign.ApplyMissionOutput(output);
        
        // Verify item was added
        AssertThat(campaign.Inventory.Items.Count).IsGreater(initialItemCount);
    }

    // ========================================================================
    // EVENT FLOW
    // ========================================================================

    [TestCase]
    public void FullMissionFlow_PublishesAllExpectedEvents()
    {
        campaign.CurrentJob = testJob;
        
        var receivedEvents = new List<string>();
        eventBus.Subscribe<CrewDiedEvent>(e => receivedEvents.Add("CrewDied"));
        eventBus.Subscribe<CrewInjuredEvent>(e => receivedEvents.Add("CrewInjured"));
        eventBus.Subscribe<CrewLeveledUpEvent>(e => receivedEvents.Add("CrewLeveledUp"));
        eventBus.Subscribe<JobCompletedEvent>(e => receivedEvents.Add("JobCompleted"));
        eventBus.Subscribe<ResourceChangedEvent>(e => receivedEvents.Add($"ResourceChanged:{e.ResourceType}"));
        
        var input = MissionInputBuilder.Build(campaign, testJob);
        var aliveCrew = campaign.GetAliveCrew();
        
        // Set one crew close to level up
        aliveCrew[0].Xp = 95;
        
        var output = new MissionOutput
        {
            Outcome = MissionOutcome.Victory,
            CrewOutcomes = new List<CrewOutcome>
            {
                new CrewOutcome
                {
                    CampaignCrewId = aliveCrew[0].Id,
                    Name = aliveCrew[0].Name,
                    Status = CrewFinalStatus.Alive,
                    SuggestedXp = 10, // Should level up
                    AmmoUsed = 20
                },
                new CrewOutcome
                {
                    CampaignCrewId = aliveCrew[1].Id,
                    Name = aliveCrew[1].Name,
                    Status = CrewFinalStatus.Wounded,
                    NewInjuries = new List<string> { "sprain" }
                }
            }
        };
        
        campaign.ApplyMissionOutput(output);
        
        // Verify expected events were published
        AssertThat(receivedEvents).Contains("CrewLeveledUp");
        AssertThat(receivedEvents).Contains("CrewInjured");
        AssertThat(receivedEvents).Contains("JobCompleted");
        AssertThat(receivedEvents).Contains("ResourceChanged:ammo");
    }
}
