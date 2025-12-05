using GdUnit4;
using static GdUnit4.Assertions;
using System.Collections.Generic;
using System.Linq;

namespace FringeTactics.Tests;

[TestSuite]
public class GN1SerializationTests
{
    // ========================================================================
    // Job ContractType Serialization
    // ========================================================================

    [TestCase]
    public void Job_RoundTrip_PreservesContractType()
    {
        var job = new Job("test_1")
        {
            ContractType = ContractType.Raid,
            Title = "Test Raid"
        };

        var data = job.GetState();
        var restored = Job.FromState(data);

        AssertThat(restored.ContractType).IsEqual(ContractType.Raid);
    }

    [TestCase]
    public void Job_RoundTrip_PreservesAllContractTypes()
    {
        var types = new[] 
        { 
            ContractType.Assault, 
            ContractType.Delivery, 
            ContractType.Escort,
            ContractType.Raid,
            ContractType.Heist,
            ContractType.Extraction
        };

        foreach (var type in types)
        {
            var job = new Job($"test_{type}") { ContractType = type };
            var data = job.GetState();
            var restored = Job.FromState(data);

            AssertThat(restored.ContractType).IsEqual(type);
        }
    }

    // ========================================================================
    // Objective Serialization
    // ========================================================================

    [TestCase]
    public void Job_RoundTrip_PreservesPrimaryObjective()
    {
        var job = new Job("test_1")
        {
            PrimaryObjective = Objective.EliminateAll()
        };

        var data = job.GetState();
        var restored = Job.FromState(data);

        AssertThat(restored.PrimaryObjective).IsNotNull();
        AssertThat(restored.PrimaryObjective.Type).IsEqual(ObjectiveType.EliminateAll);
        AssertBool(restored.PrimaryObjective.IsRequired).IsTrue();
    }

    [TestCase]
    public void Job_RoundTrip_PreservesSecondaryObjectives()
    {
        var job = new Job("test_1")
        {
            SecondaryObjectives = new List<Objective>
            {
                Objective.NoCasualties(),
                Objective.TimeBonus(20)
            }
        };

        var data = job.GetState();
        var restored = Job.FromState(data);

        AssertInt(restored.SecondaryObjectives.Count).IsEqual(2);
        
        var noCasualties = restored.SecondaryObjectives.FirstOrDefault(o => o.Type == ObjectiveType.NoCasualties);
        AssertThat(noCasualties).IsNotNull();
        AssertInt(noCasualties.BonusRewardPercent).IsEqual(20);

        var timeBonus = restored.SecondaryObjectives.FirstOrDefault(o => o.Type == ObjectiveType.TimeLimit);
        AssertThat(timeBonus).IsNotNull();
        AssertInt(timeBonus.BonusRewardPercent).IsEqual(15);
    }

    [TestCase]
    public void Job_RoundTrip_PreservesObjectiveParameters()
    {
        var job = new Job("test_1")
        {
            PrimaryObjective = Objective.SurviveTurns(10)
        };

        var data = job.GetState();
        var restored = Job.FromState(data);

        AssertThat(restored.PrimaryObjective.Parameters).IsNotNull();
        AssertBool(restored.PrimaryObjective.Parameters.ContainsKey("turns")).IsTrue();
    }

    [TestCase]
    public void Job_RoundTrip_HandlesNullPrimaryObjective()
    {
        var job = new Job("test_1")
        {
            PrimaryObjective = null
        };

        var data = job.GetState();
        var restored = Job.FromState(data);

        AssertThat(restored.PrimaryObjective).IsNull();
    }

    [TestCase]
    public void Job_RoundTrip_HandlesEmptySecondaryObjectives()
    {
        var job = new Job("test_1")
        {
            SecondaryObjectives = new List<Objective>()
        };

        var data = job.GetState();
        var restored = Job.FromState(data);

        AssertThat(restored.SecondaryObjectives).IsNotNull();
        AssertInt(restored.SecondaryObjectives.Count).IsEqual(0);
    }

    // ========================================================================
    // Legacy Migration Tests
    // ========================================================================

    [TestCase]
    public void Job_LegacyJobType_MigratesCorrectly()
    {
        // Simulate old save with JobType instead of ContractType
        var data = new JobData
        {
            Id = "old_job",
            Type = "Assault",
            ContractType = null  // Old save won't have this
        };

        var restored = Job.FromState(data);

        AssertThat(restored.ContractType).IsEqual(ContractType.Assault);
    }

    [TestCase]
    public void Job_LegacyExtraction_MigratesCorrectly()
    {
        var data = new JobData
        {
            Id = "old_job",
            Type = "Extraction",
            ContractType = null
        };

        var restored = Job.FromState(data);

        AssertThat(restored.ContractType).IsEqual(ContractType.Extraction);
    }

    [TestCase]
    public void Job_LegacyDefense_MapsToAssault()
    {
        // Defense was never fully implemented, should map to Assault
        var data = new JobData
        {
            Id = "old_job",
            Type = "Defense",
            ContractType = null
        };

        var restored = Job.FromState(data);

        AssertThat(restored.ContractType).IsEqual(ContractType.Assault);
    }

    [TestCase]
    public void Job_ContractTypeTakesPrecedence()
    {
        // If both are present, ContractType wins
        var data = new JobData
        {
            Id = "job",
            Type = "Assault",
            ContractType = "Heist"
        };

        var restored = Job.FromState(data);

        AssertThat(restored.ContractType).IsEqual(ContractType.Heist);
    }

    [TestCase]
    public void Job_LegacySave_NoObjectives_HandledGracefully()
    {
        var data = new JobData
        {
            Id = "old_job",
            Type = "Assault",
            PrimaryObjective = null,
            SecondaryObjectives = null
        };

        var restored = Job.FromState(data);

        AssertThat(restored.PrimaryObjective).IsNull();
        AssertThat(restored.SecondaryObjectives).IsNotNull();
        AssertInt(restored.SecondaryObjectives.Count).IsEqual(0);
    }

    // ========================================================================
    // Objective Standalone Serialization
    // ========================================================================

    [TestCase]
    public void Objective_RoundTrip_PreservesAllFields()
    {
        var objective = new Objective
        {
            Id = "test_obj",
            Type = ObjectiveType.TimeLimit,
            Description = "Complete in 15 turns",
            IsRequired = false,
            BonusRewardPercent = 25,
            Parameters = new Dictionary<string, object> { { "turns", 15 } }
        };

        var data = objective.GetState();
        var restored = Objective.FromState(data);

        AssertString(restored.Id).IsEqual("test_obj");
        AssertThat(restored.Type).IsEqual(ObjectiveType.TimeLimit);
        AssertString(restored.Description).IsEqual("Complete in 15 turns");
        AssertBool(restored.IsRequired).IsFalse();
        AssertInt(restored.BonusRewardPercent).IsEqual(25);
        AssertBool(restored.Parameters.ContainsKey("turns")).IsTrue();
    }

    [TestCase]
    public void Objective_FromState_HandlesNull()
    {
        var restored = Objective.FromState(null);
        AssertThat(restored).IsNull();
    }

    [TestCase]
    public void Objective_RoundTrip_HandlesEmptyParameters()
    {
        var objective = Objective.EliminateAll();
        objective.Parameters = new Dictionary<string, object>();

        var data = objective.GetState();
        var restored = Objective.FromState(data);

        AssertThat(restored.Parameters).IsNotNull();
    }

    // ========================================================================
    // Full Campaign Round-Trip
    // ========================================================================

    [TestCase]
    public void Campaign_WithGeneratedContracts_RoundTrips()
    {
        var campaign = CampaignState.CreateNew(12345);
        
        // Generate contracts
        var context = GenerationContext.FromCampaign(campaign);
        var generator = new ContractGenerator(context);
        campaign.AvailableJobs = generator.GenerateContracts(3);

        // Serialize
        var saveData = campaign.GetState();

        // Deserialize
        var restored = CampaignState.FromState(saveData);

        // Verify jobs restored
        AssertInt(restored.AvailableJobs.Count).IsEqual(3);

        for (int i = 0; i < campaign.AvailableJobs.Count; i++)
        {
            var original = campaign.AvailableJobs[i];
            var restoredJob = restored.AvailableJobs[i];

            AssertString(restoredJob.Id).IsEqual(original.Id);
            AssertThat(restoredJob.ContractType).IsEqual(original.ContractType);
            AssertThat(restoredJob.Difficulty).IsEqual(original.Difficulty);
            
            if (original.PrimaryObjective != null)
            {
                AssertThat(restoredJob.PrimaryObjective).IsNotNull();
                AssertThat(restoredJob.PrimaryObjective.Type).IsEqual(original.PrimaryObjective.Type);
            }
        }
    }
}
