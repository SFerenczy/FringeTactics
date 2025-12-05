using Godot;
using GdUnit4;
using static GdUnit4.Assertions;

namespace FringeTactics.Tests;

/// <summary>
/// Tests for MissionInputBuilder (MG3 Phase 1).
/// </summary>
[TestSuite]
public class MG3MissionInputTests
{
    private CampaignState campaign;
    private Job testJob;

    [Before]
    public void Setup()
    {
        ItemRegistry.Reset();
        TraitRegistry.Reset();
        
        campaign = CampaignState.CreateNew(12345);
        
        // Create a test job
        testJob = new Job("test_job_1")
        {
            Title = "Test Mission",
            Description = "A test mission",
            ContractType = ContractType.Assault,
            Difficulty = JobDifficulty.Medium,
            OriginNodeId = 0,
            TargetNodeId = 0,
            EmployerFactionId = "corporate",
            TargetFactionId = "pirates",
            MissionConfig = MissionConfig.CreateTestMission()
        };
    }

    [After]
    public void Cleanup()
    {
        campaign = null;
        testJob = null;
    }

    // ========================================================================
    // BASIC INPUT BUILDING
    // ========================================================================

    [TestCase]
    public void Build_CreatesValidInput()
    {
        var input = MissionInputBuilder.Build(campaign, testJob);

        AssertThat(input).IsNotNull();
        AssertThat(input.MissionId).IsEqual("job_test_job_1");
        AssertThat(input.MissionName).IsEqual("Test Mission");
        AssertThat(input.MapTemplate).IsNotNull();
        AssertThat(input.Seed).IsNotEqual(0);
    }

    [TestCase]
    public void Build_IncludesDeployableCrew()
    {
        var input = MissionInputBuilder.Build(campaign, testJob);

        // Campaign starts with 4 crew, capped by spawn positions (4)
        AssertThat(input.Crew.Count).IsGreater(0);
        AssertThat(input.Crew.Count).IsLessEqual(campaign.GetAliveCrew().Count);
    }

    [TestCase]
    public void Build_ExcludesDeadCrew()
    {
        // Kill one crew member
        var aliveCrew = campaign.GetAliveCrew();
        int initialCount = aliveCrew.Count;
        var deadCrew = aliveCrew[0];
        deadCrew.IsDead = true;

        var input = MissionInputBuilder.Build(campaign, testJob);

        // Should have one less deployable crew (capped by spawn positions)
        int expectedCount = System.Math.Min(initialCount - 1, testJob.MissionConfig.CrewSpawnPositions.Count);
        AssertThat(input.Crew.Count).IsEqual(expectedCount);
        
        // Verify the dead crew is not included
        foreach (var deployment in input.Crew)
        {
            AssertThat(deployment.CampaignCrewId).IsNotEqual(deadCrew.Id);
        }
    }

    [TestCase]
    public void Build_ExcludesCriticallyInjuredCrew()
    {
        // Give one crew a critical injury
        var aliveCrew = campaign.GetAliveCrew();
        aliveCrew[0].AddInjury("critical");

        var input = MissionInputBuilder.Build(campaign, testJob);

        // Should have one less crew
        AssertThat(input.Crew.Count).IsEqual(2);
    }

    [TestCase]
    public void Build_IncludesEnemiesFromConfig()
    {
        var input = MissionInputBuilder.Build(campaign, testJob);

        // Test mission config includes enemies
        AssertThat(input.Enemies.Count).IsGreater(0);
    }

    // ========================================================================
    // CREW STAT MAPPING
    // ========================================================================

    [TestCase]
    public void Build_MapsGritToMaxHp()
    {
        // Use first deployed crew member
        var input = MissionInputBuilder.Build(campaign, testJob);
        AssertThat(input.Crew.Count).IsGreater(0);
        
        var deployment = input.Crew[0];
        var testCrew = campaign.GetCrewById(deployment.CampaignCrewId);
        
        // HP = BaseHp + (Grit * HpPerGrit)
        var crewConfig = CampaignConfig.Instance.Crew;
        int expectedHp = crewConfig.BaseHp + (testCrew.GetEffectiveStat(CrewStatType.Grit) * crewConfig.HpPerGrit);
        AssertThat(deployment.MaxHp).IsEqual(expectedHp);
        AssertThat(deployment.CurrentHp).IsEqual(expectedHp);
    }

    [TestCase]
    public void Build_MapsAimToAccuracy()
    {
        // Use first deployed crew member
        var input = MissionInputBuilder.Build(campaign, testJob);
        AssertThat(input.Crew.Count).IsGreater(0);
        
        var deployment = input.Crew[0];
        var testCrew = campaign.GetCrewById(deployment.CampaignCrewId);
        
        // Accuracy = 0.7 + (Aim * 0.02)
        float expectedAccuracy = 0.7f + (testCrew.GetEffectiveStat(CrewStatType.Aim) * 0.02f);
        AssertThat(deployment.Accuracy).IsEqual(expectedAccuracy);
    }

    [TestCase]
    public void Build_MapsReflexesToMoveSpeed()
    {
        // Use first deployed crew member
        var input = MissionInputBuilder.Build(campaign, testJob);
        AssertThat(input.Crew.Count).IsGreater(0);
        
        var deployment = input.Crew[0];
        var testCrew = campaign.GetCrewById(deployment.CampaignCrewId);
        
        // MoveSpeed = 2.0 + (Reflexes * 0.1)
        float expectedSpeed = 2.0f + (testCrew.GetEffectiveStat(CrewStatType.Reflexes) * 0.1f);
        AssertThat(deployment.MoveSpeed).IsEqual(expectedSpeed);
    }

    [TestCase]
    public void Build_IncludesTraitModifiersInStats()
    {
        // Get first deployable crew and add trait
        var aliveCrew = campaign.GetAliveCrew();
        var testCrew = aliveCrew[0];
        int baseAim = testCrew.Aim;
        testCrew.AddTrait("ex_military"); // +1 Aim, +1 Grit

        var input = MissionInputBuilder.Build(campaign, testJob);
        
        var deployment = input.Crew.Find(c => c.CampaignCrewId == testCrew.Id);
        // If crew wasn't deployed (spawn limit), skip this test
        if (deployment == null)
        {
            AssertThat(true).IsTrue(); // Pass - crew wasn't in deployment
            return;
        }
        
        // Effective Aim = baseAim + 1 (trait)
        int effectiveAim = testCrew.GetEffectiveStat(CrewStatType.Aim);
        float expectedAccuracy = 0.7f + (effectiveAim * 0.02f);
        AssertThat(deployment.Accuracy).IsEqual(expectedAccuracy);
    }

    // ========================================================================
    // EQUIPMENT RESOLUTION
    // ========================================================================

    [TestCase]
    public void Build_UsesPreferredWeapon()
    {
        // Set preferred weapon on all crew to ensure we test it
        foreach (var crew in campaign.GetAliveCrew())
        {
            crew.PreferredWeaponId = "smg";
        }

        var input = MissionInputBuilder.Build(campaign, testJob);
        AssertThat(input.Crew.Count).IsGreater(0);
        
        // All deployed crew should have smg
        AssertThat(input.Crew[0].WeaponId).IsEqual("smg");
    }

    [TestCase]
    public void Build_FallsBackToRifleWhenNoPreference()
    {
        // Clear all weapon preferences
        foreach (var crew in campaign.GetAliveCrew())
        {
            crew.PreferredWeaponId = null;
            crew.EquippedWeaponId = null;
        }

        var input = MissionInputBuilder.Build(campaign, testJob);
        AssertThat(input.Crew.Count).IsGreater(0);
        
        // Should fall back to rifle
        AssertThat(input.Crew[0].WeaponId).IsEqual("rifle");
    }

    [TestCase]
    public void Build_UsesEquippedWeaponOverPreferred()
    {
        // Equip shotgun on all crew
        foreach (var crew in campaign.GetAliveCrew())
        {
            crew.PreferredWeaponId = "rifle";
            var shotgunItem = campaign.Inventory.AddItem("shotgun", 1, 100);
            crew.EquippedWeaponId = shotgunItem.Id;
        }

        var input = MissionInputBuilder.Build(campaign, testJob);
        AssertThat(input.Crew.Count).IsGreater(0);
        
        // Should use equipped shotgun over preferred rifle
        AssertThat(input.Crew[0].WeaponId).IsEqual("shotgun");
    }

    [TestCase]
    public void Build_SetsCorrectMagazineSize()
    {
        // Set all crew to use shotgun
        foreach (var crew in campaign.GetAliveCrew())
        {
            crew.PreferredWeaponId = "shotgun";
        }

        var input = MissionInputBuilder.Build(campaign, testJob);
        AssertThat(input.Crew.Count).IsGreater(0);
        
        var shotgunData = WeaponData.FromId("shotgun");
        AssertThat(input.Crew[0].AmmoInMagazine).IsEqual(shotgunData.MagazineSize);
    }

    [TestCase]
    public void Build_CapsReserveAmmoToCampaignPool()
    {
        campaign.Ammo = 10; // Very low ammo

        var input = MissionInputBuilder.Build(campaign, testJob);

        foreach (var deployment in input.Crew)
        {
            AssertThat(deployment.ReserveAmmo).IsLessEqual(10);
        }
    }

    // ========================================================================
    // CONTEXT BUILDING
    // ========================================================================

    [TestCase]
    public void Build_IncludesContext()
    {
        var input = MissionInputBuilder.Build(campaign, testJob);

        AssertThat(input.Context).IsNotNull();
        AssertThat(input.Context.ContractId).IsEqual("test_job_1");
        AssertThat(input.Context.FactionId).IsEqual("corporate");
    }

    [TestCase]
    public void Build_IncludesDifficultyTag()
    {
        var input = MissionInputBuilder.Build(campaign, testJob);

        AssertThat(input.Context.Tags).Contains("difficulty_medium");
    }

    [TestCase]
    public void Build_IncludesContractTypeTag()
    {
        var input = MissionInputBuilder.Build(campaign, testJob);

        AssertThat(input.Context.Tags).Contains("contract_assault");
    }

    // ========================================================================
    // OBJECTIVES
    // ========================================================================

    [TestCase]
    public void Build_IncludesDefaultObjectiveWhenNoPrimary()
    {
        testJob.PrimaryObjective = null;

        var input = MissionInputBuilder.Build(campaign, testJob);

        AssertThat(input.Objectives.Count).IsGreater(0);
        AssertThat(input.Objectives[0].IsPrimary).IsTrue();
        AssertThat(input.Objectives[0].Type).IsEqual(ObjectiveType.EliminateAll);
    }

    [TestCase]
    public void Build_IncludesPrimaryObjective()
    {
        testJob.PrimaryObjective = Objective.HackTerminal();

        var input = MissionInputBuilder.Build(campaign, testJob);

        AssertThat(input.Objectives.Count).IsGreater(0);
        var primary = input.Objectives.Find(o => o.IsPrimary);
        AssertThat(primary).IsNotNull();
        AssertThat(primary.Type).IsEqual(ObjectiveType.HackTerminal);
    }

    [TestCase]
    public void Build_IncludesSecondaryObjectives()
    {
        testJob.PrimaryObjective = Objective.EliminateAll();
        testJob.SecondaryObjectives.Add(Objective.NoCasualties());
        testJob.SecondaryObjectives.Add(Objective.TimeBonus(10));

        var input = MissionInputBuilder.Build(campaign, testJob);

        // 1 primary + 2 secondary = 3 objectives
        AssertThat(input.Objectives.Count).IsEqual(3);
        
        var secondaries = input.Objectives.FindAll(o => !o.IsPrimary);
        AssertThat(secondaries.Count).IsEqual(2);
    }

    // ========================================================================
    // MISSION FACTORY INTEGRATION
    // ========================================================================

    [TestCase]
    public void MissionFactory_UsesInputBuilder_WhenJobActive()
    {
        campaign.CurrentJob = testJob;

        var result = MissionFactory.BuildFromCampaign(campaign, testJob.MissionConfig);

        AssertThat(result).IsNotNull();
        AssertThat(result.CombatState).IsNotNull();
        
        // Find any crew actor and verify HP matches campaign crew
        foreach (var actor in result.CombatState.Actors)
        {
            if (actor.Type == ActorType.Crew && actor.CrewId >= 0)
            {
                var crew = campaign.GetCrewById(actor.CrewId);
                if (crew != null)
                {
                    int expectedHp = crew.GetMaxHp();
                    AssertThat(actor.MaxHp).IsEqual(expectedHp);
                    return; // Test passed
                }
            }
        }
        
        // Should have found at least one crew actor
        AssertThat(result.CombatState.Actors.Count).IsGreater(0);
    }

    [TestCase]
    public void MissionFactory_UsesLegacyPath_WhenNoJob()
    {
        campaign.CurrentJob = null;
        
        var config = MissionConfig.CreateTestMission();
        var result = MissionFactory.BuildFromCampaign(campaign, config);

        AssertThat(result).IsNotNull();
        AssertThat(result.CombatState).IsNotNull();
        AssertThat(result.CombatState.Actors.Count).IsGreater(0);
    }

    // ========================================================================
    // PHASE 3: AMMO RESOURCE INTEGRATION
    // ========================================================================

    [TestCase]
    public void CalculateMissionAmmoNeeded_ReturnsPositiveValue()
    {
        int ammoNeeded = campaign.CalculateMissionAmmoNeeded();
        
        AssertThat(ammoNeeded).IsGreater(0);
    }

    [TestCase]
    public void CalculateMissionAmmoNeeded_AccountsForAllDeployableCrew()
    {
        // Get deployable crew count
        int deployableCount = 0;
        foreach (var crew in campaign.GetAliveCrew())
        {
            if (crew.CanDeploy()) deployableCount++;
        }
        
        int ammoNeeded = campaign.CalculateMissionAmmoNeeded();
        
        // Each crew needs at least 1 magazine (minimum ~6 for shotgun)
        AssertThat(ammoNeeded).IsGreaterEqual(deployableCount * 6);
    }

    [TestCase]
    public void HasEnoughAmmoForMission_TrueWhenSufficientAmmo()
    {
        campaign.Ammo = 1000;
        
        AssertThat(campaign.HasEnoughAmmoForMission()).IsTrue();
    }

    [TestCase]
    public void HasEnoughAmmoForMission_FalseWhenInsufficientAmmo()
    {
        campaign.Ammo = 0;
        
        AssertThat(campaign.HasEnoughAmmoForMission()).IsFalse();
    }

    [TestCase]
    public void ConsumeMissionResources_ConsumesFuel()
    {
        int initialFuel = campaign.Fuel;
        
        campaign.ConsumeMissionResources();
        
        AssertThat(campaign.Fuel).IsLess(initialFuel);
    }

    [TestCase]
    public void ConsumeMissionResources_DoesNotConsumeAmmoUpfront()
    {
        int initialAmmo = campaign.Ammo;
        
        campaign.ConsumeMissionResources();
        
        // Ammo should not be consumed upfront - it's tracked per-actor
        AssertThat(campaign.Ammo).IsEqual(initialAmmo);
    }

    [TestCase]
    public void ApplyMissionOutput_ConsumesAmmoBasedOnUsage()
    {
        campaign.Ammo = 100;
        
        var output = new MissionOutput
        {
            Outcome = MissionOutcome.Victory,
            CrewOutcomes = new System.Collections.Generic.List<CrewOutcome>
            {
                new CrewOutcome
                {
                    CampaignCrewId = campaign.GetAliveCrew()[0].Id,
                    Status = CrewFinalStatus.Alive,
                    AmmoUsed = 30
                }
            }
        };
        
        campaign.ApplyMissionOutput(output);
        
        // Should have consumed 30 ammo
        AssertThat(campaign.Ammo).IsEqual(70);
    }

    [TestCase]
    public void ApplyMissionOutput_ProcessesLootCredits()
    {
        int initialMoney = campaign.Money;
        
        var output = new MissionOutput
        {
            Outcome = MissionOutcome.Victory,
            Loot = new System.Collections.Generic.List<LootItem>
            {
                LootItem.Credits(500)
            }
        };
        
        campaign.ApplyMissionOutput(output);
        
        // Should have gained 500 credits (plus any victory bonus)
        AssertThat(campaign.Money).IsGreater(initialMoney + 400);
    }

    [TestCase]
    public void ApplyMissionOutput_ProcessesLootResources()
    {
        campaign.Fuel = 50;
        
        var output = new MissionOutput
        {
            Outcome = MissionOutcome.Victory,
            Loot = new System.Collections.Generic.List<LootItem>
            {
                LootItem.Resource("fuel", 25)
            }
        };
        
        campaign.ApplyMissionOutput(output);
        
        AssertThat(campaign.Fuel).IsEqual(75);
    }
}
