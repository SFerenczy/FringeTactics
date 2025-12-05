using GdUnit4;
using static GdUnit4.Assertions;
using System.Collections.Generic;
using System.Linq;

namespace FringeTactics.Tests;

[TestSuite]
public class GN1ContractGeneratorTests
{
    private GenerationContext CreateTestContext(int seed = 12345)
    {
        var campaign = CampaignState.CreateNew(seed);
        return GenerationContext.FromCampaign(campaign);
    }

    // ========================================================================
    // Basic Generation Tests
    // ========================================================================

    [TestCase]
    public void GenerateContracts_ReturnsRequestedCount()
    {
        var context = CreateTestContext();
        var generator = new ContractGenerator(context);

        var contracts = generator.GenerateContracts(3);

        AssertInt(contracts.Count).IsEqual(3);
    }

    [TestCase]
    public void GenerateContracts_ReturnsEmptyForZeroCount()
    {
        var context = CreateTestContext();
        var generator = new ContractGenerator(context);

        var contracts = generator.GenerateContracts(0);

        AssertInt(contracts.Count).IsEqual(0);
    }

    [TestCase]
    public void GenerateContracts_NoNearbySystems_ReturnsEmpty()
    {
        var context = new GenerationContext
        {
            NearbySystems = new List<StarSystem>(),
            Rng = new RngStream("test", 12345)
        };
        var generator = new ContractGenerator(context);

        var contracts = generator.GenerateContracts(3);

        AssertInt(contracts.Count).IsEqual(0);
    }

    // ========================================================================
    // Contract Type Tests
    // ========================================================================

    [TestCase]
    public void GenerateContracts_AllHaveValidContractType()
    {
        var context = CreateTestContext();
        var generator = new ContractGenerator(context);

        var contracts = generator.GenerateContracts(5);

        foreach (var contract in contracts)
        {
            // Should only generate implemented types
            AssertBool(contract.ContractType.IsImplemented()).IsTrue();
        }
    }

    [TestCase]
    public void GenerateContracts_OnlyGeneratesImplementedTypes()
    {
        var context = CreateTestContext();
        var generator = new ContractGenerator(context);

        var contracts = generator.GenerateContracts(20);

        foreach (var contract in contracts)
        {
            // Currently only Assault and Extraction are implemented
            AssertBool(
                contract.ContractType == ContractType.Assault ||
                contract.ContractType == ContractType.Extraction
            ).IsTrue();
        }
    }

    // ========================================================================
    // Objective Tests
    // ========================================================================

    [TestCase]
    public void GenerateContracts_AllHavePrimaryObjective()
    {
        var context = CreateTestContext();
        var generator = new ContractGenerator(context);

        var contracts = generator.GenerateContracts(5);

        foreach (var contract in contracts)
        {
            AssertThat(contract.PrimaryObjective).IsNotNull();
            AssertBool(contract.PrimaryObjective.IsRequired).IsTrue();
        }
    }

    [TestCase]
    public void GenerateContracts_AssaultHasEliminateAllObjective()
    {
        var context = CreateTestContext();
        var generator = new ContractGenerator(context);

        var contracts = generator.GenerateContracts(20);
        var assaultContracts = contracts.Where(c => c.ContractType == ContractType.Assault).ToList();

        foreach (var contract in assaultContracts)
        {
            AssertThat(contract.PrimaryObjective.Type).IsEqual(ObjectiveType.EliminateAll);
        }
    }

    [TestCase]
    public void GenerateContracts_ExtractionHasReachZoneObjective()
    {
        var context = CreateTestContext();
        var generator = new ContractGenerator(context);

        var contracts = generator.GenerateContracts(20);
        var extractionContracts = contracts.Where(c => c.ContractType == ContractType.Extraction).ToList();

        foreach (var contract in extractionContracts)
        {
            AssertThat(contract.PrimaryObjective.Type).IsEqual(ObjectiveType.ReachZone);
        }
    }

    [TestCase]
    public void GenerateContracts_HardDifficulty_HasSecondaryObjectives()
    {
        var context = CreateTestContext();
        context.CrewPower = 100; // Force veteran tier for harder contracts
        var generator = new ContractGenerator(context);

        var contracts = generator.GenerateContracts(20);
        var hardContracts = contracts.Where(c => c.Difficulty == JobDifficulty.Hard).ToList();

        // Hard contracts should have secondary objectives
        if (hardContracts.Count > 0)
        {
            var hasSecondaries = hardContracts.Any(c => c.SecondaryObjectives.Count > 0);
            AssertBool(hasSecondaries).IsTrue();
        }
    }

    [TestCase]
    public void GenerateContracts_MediumDifficulty_HasNoCasualtiesBonus()
    {
        var context = CreateTestContext();
        var generator = new ContractGenerator(context);

        var contracts = generator.GenerateContracts(20);
        var mediumOrHarder = contracts.Where(c => c.Difficulty >= JobDifficulty.Medium).ToList();

        foreach (var contract in mediumOrHarder)
        {
            var hasNoCasualties = contract.SecondaryObjectives.Any(o => o.Type == ObjectiveType.NoCasualties);
            AssertBool(hasNoCasualties).IsTrue();
        }
    }

    // ========================================================================
    // Reward Tests
    // ========================================================================

    [TestCase]
    public void GenerateContracts_RewardsScaleWithDifficulty()
    {
        var context = CreateTestContext();
        var generator = new ContractGenerator(context);

        var contracts = generator.GenerateContracts(30);

        var easyRewards = contracts
            .Where(c => c.Difficulty == JobDifficulty.Easy)
            .Select(c => c.Reward.Money)
            .ToList();
        var hardRewards = contracts
            .Where(c => c.Difficulty == JobDifficulty.Hard)
            .Select(c => c.Reward.Money)
            .ToList();

        if (easyRewards.Count > 0 && hardRewards.Count > 0)
        {
            AssertThat(hardRewards.Average()).IsGreater(easyRewards.Average());
        }
    }

    [TestCase]
    public void GenerateContracts_EasyRewardIsAround100()
    {
        var context = CreateTestContext();
        var generator = new ContractGenerator(context);

        var contracts = generator.GenerateContracts(20);
        var easyContracts = contracts.Where(c => c.Difficulty == JobDifficulty.Easy).ToList();

        foreach (var contract in easyContracts)
        {
            // Base 100 * type multiplier (1.0-1.4) = 100-140
            AssertThat(contract.Reward.Money).IsGreaterEqual(80);
            AssertThat(contract.Reward.Money).IsLessEqual(200);
        }
    }

    [TestCase]
    public void GenerateContracts_HardRewardIsAround400()
    {
        var context = CreateTestContext();
        context.CrewPower = 100; // Force harder contracts
        var generator = new ContractGenerator(context);

        var contracts = generator.GenerateContracts(20);
        var hardContracts = contracts.Where(c => c.Difficulty == JobDifficulty.Hard).ToList();

        foreach (var contract in hardContracts)
        {
            // Base 400 * type multiplier (1.0-1.4) = 400-560
            AssertThat(contract.Reward.Money).IsGreaterEqual(300);
            AssertThat(contract.Reward.Money).IsLessEqual(700);
        }
    }

    // ========================================================================
    // Difficulty Scaling Tests
    // ========================================================================

    [TestCase]
    public void GenerateContracts_RookieGetsEasierContracts()
    {
        var context = CreateTestContext();
        context.CrewPower = 20;  // Rookie tier
        context.CompletedContracts = 0;
        var generator = new ContractGenerator(context);

        var contracts = generator.GenerateContracts(20);

        // Rookies should get mostly Easy contracts
        var easyCount = contracts.Count(c => c.Difficulty == JobDifficulty.Easy);
        var hardCount = contracts.Count(c => c.Difficulty == JobDifficulty.Hard);

        AssertThat(easyCount).IsGreater(hardCount);
    }

    [TestCase]
    public void GenerateContracts_VeteranGetsHarderContracts()
    {
        var context = CreateTestContext();
        context.CrewPower = 80;  // Veteran tier
        context.CompletedContracts = 10;
        var generator = new ContractGenerator(context);

        var contracts = generator.GenerateContracts(20);

        // Veterans should get more Medium/Hard contracts
        var mediumOrHarder = contracts.Count(c => c.Difficulty >= JobDifficulty.Medium);
        var easy = contracts.Count(c => c.Difficulty == JobDifficulty.Easy);

        AssertThat(mediumOrHarder).IsGreaterEqual(easy);
    }

    // ========================================================================
    // Contract Fields Tests
    // ========================================================================

    [TestCase]
    public void GenerateContracts_AllHaveRequiredFields()
    {
        var context = CreateTestContext();
        var generator = new ContractGenerator(context);

        var contracts = generator.GenerateContracts(5);

        foreach (var contract in contracts)
        {
            AssertString(contract.Id).IsNotEmpty();
            AssertString(contract.Title).IsNotEmpty();
            AssertString(contract.Description).IsNotEmpty();
            AssertThat(contract.Reward).IsNotNull();
            AssertThat(contract.Reward.Money).IsGreater(0);
            AssertThat(contract.DeadlineDays).IsGreater(0);
        }
    }

    [TestCase]
    public void GenerateContracts_AllHaveFactions()
    {
        var context = CreateTestContext();
        var generator = new ContractGenerator(context);

        var contracts = generator.GenerateContracts(5);

        foreach (var contract in contracts)
        {
            AssertString(contract.EmployerFactionId).IsNotEmpty();
            AssertString(contract.TargetFactionId).IsNotEmpty();
        }
    }

    [TestCase]
    public void GenerateContracts_AllHaveLocations()
    {
        var context = CreateTestContext();
        var generator = new ContractGenerator(context);

        var contracts = generator.GenerateContracts(5);

        foreach (var contract in contracts)
        {
            AssertInt(contract.OriginNodeId).IsEqual(context.CurrentNodeId);
            // Target should be different from origin (nearby system)
            AssertThat(contract.TargetNodeId).IsNotEqual(contract.OriginNodeId);
        }
    }

    // ========================================================================
    // GetMaxPotentialReward Tests
    // ========================================================================

    [TestCase]
    public void GetMaxPotentialReward_NoSecondaries_ReturnsBase()
    {
        var job = new Job("test")
        {
            Reward = new JobReward { Money = 100 },
            SecondaryObjectives = new List<Objective>()
        };

        AssertInt(job.GetMaxPotentialReward()).IsEqual(100);
    }

    [TestCase]
    public void GetMaxPotentialReward_WithSecondaries_IncludesBonus()
    {
        var job = new Job("test")
        {
            Reward = new JobReward { Money = 100 },
            SecondaryObjectives = new List<Objective>
            {
                Objective.NoCasualties(),  // 20%
                Objective.TimeBonus(20)    // 15%
            }
        };

        // 100 + (100 * 35 / 100) = 135
        AssertInt(job.GetMaxPotentialReward()).IsEqual(135);
    }
}
