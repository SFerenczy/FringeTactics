using GdUnit4;
using static GdUnit4.Assertions;
using System.Linq;

namespace FringeTactics.Tests;

[TestSuite]
public class GN1DeterminismTests
{
    [TestCase]
    public void SameSeed_ProducesSameContracts()
    {
        // Generate contracts twice with same seed
        var campaign1 = CampaignState.CreateNew(12345);
        var context1 = GenerationContext.FromCampaign(campaign1);
        var generator1 = new ContractGenerator(context1);
        var contracts1 = generator1.GenerateContracts(5);

        var campaign2 = CampaignState.CreateNew(12345);
        var context2 = GenerationContext.FromCampaign(campaign2);
        var generator2 = new ContractGenerator(context2);
        var contracts2 = generator2.GenerateContracts(5);

        // Should produce identical contracts
        AssertInt(contracts1.Count).IsEqual(contracts2.Count);

        for (int i = 0; i < contracts1.Count; i++)
        {
            AssertString(contracts1[i].Title).IsEqual(contracts2[i].Title);
            AssertThat(contracts1[i].ContractType).IsEqual(contracts2[i].ContractType);
            AssertThat(contracts1[i].Difficulty).IsEqual(contracts2[i].Difficulty);
            AssertInt(contracts1[i].TargetNodeId).IsEqual(contracts2[i].TargetNodeId);
            AssertInt(contracts1[i].Reward.Money).IsEqual(contracts2[i].Reward.Money);
        }
    }

    [TestCase]
    public void DifferentSeeds_ProducesDifferentContracts()
    {
        var campaign1 = CampaignState.CreateNew(12345);
        var context1 = GenerationContext.FromCampaign(campaign1);
        var generator1 = new ContractGenerator(context1);
        var contracts1 = generator1.GenerateContracts(5);

        var campaign2 = CampaignState.CreateNew(54321);
        var context2 = GenerationContext.FromCampaign(campaign2);
        var generator2 = new ContractGenerator(context2);
        var contracts2 = generator2.GenerateContracts(5);

        // Should produce different contracts (at least some difference)
        bool anyDifferent = false;
        int minCount = System.Math.Min(contracts1.Count, contracts2.Count);
        
        for (int i = 0; i < minCount; i++)
        {
            if (contracts1[i].Title != contracts2[i].Title ||
                contracts1[i].TargetNodeId != contracts2[i].TargetNodeId ||
                contracts1[i].Reward.Money != contracts2[i].Reward.Money)
            {
                anyDifferent = true;
                break;
            }
        }

        AssertBool(anyDifferent).IsTrue();
    }

    [TestCase]
    public void SameSeed_SameObjectives()
    {
        var campaign1 = CampaignState.CreateNew(99999);
        var context1 = GenerationContext.FromCampaign(campaign1);
        var generator1 = new ContractGenerator(context1);
        var contracts1 = generator1.GenerateContracts(3);

        var campaign2 = CampaignState.CreateNew(99999);
        var context2 = GenerationContext.FromCampaign(campaign2);
        var generator2 = new ContractGenerator(context2);
        var contracts2 = generator2.GenerateContracts(3);

        for (int i = 0; i < contracts1.Count; i++)
        {
            // Primary objectives should match
            AssertThat(contracts1[i].PrimaryObjective.Type)
                .IsEqual(contracts2[i].PrimaryObjective.Type);

            // Secondary objective count should match
            AssertInt(contracts1[i].SecondaryObjectives.Count)
                .IsEqual(contracts2[i].SecondaryObjectives.Count);
        }
    }

    [TestCase]
    public void MultipleGenerations_SameCampaign_ProducesDifferentResults()
    {
        // Each generation batch should be different (RNG advances)
        var campaign = CampaignState.CreateNew(12345);
        
        var context1 = GenerationContext.FromCampaign(campaign);
        var generator1 = new ContractGenerator(context1);
        var contracts1 = generator1.GenerateContracts(3);

        // Generate again from same campaign (RNG has advanced)
        var context2 = GenerationContext.FromCampaign(campaign);
        var generator2 = new ContractGenerator(context2);
        var contracts2 = generator2.GenerateContracts(3);

        // Should be different because RNG advanced
        bool anyDifferent = false;
        for (int i = 0; i < contracts1.Count; i++)
        {
            if (contracts1[i].Title != contracts2[i].Title ||
                contracts1[i].TargetNodeId != contracts2[i].TargetNodeId)
            {
                anyDifferent = true;
                break;
            }
        }

        AssertBool(anyDifferent).IsTrue();
    }

    [TestCase]
    public void ContractType_DeterministicForSameSeed()
    {
        // Generate many contracts and verify type distribution is identical
        var campaign1 = CampaignState.CreateNew(77777);
        var context1 = GenerationContext.FromCampaign(campaign1);
        var generator1 = new ContractGenerator(context1);
        var contracts1 = generator1.GenerateContracts(10);

        var campaign2 = CampaignState.CreateNew(77777);
        var context2 = GenerationContext.FromCampaign(campaign2);
        var generator2 = new ContractGenerator(context2);
        var contracts2 = generator2.GenerateContracts(10);

        var types1 = contracts1.Select(c => c.ContractType).ToList();
        var types2 = contracts2.Select(c => c.ContractType).ToList();

        for (int i = 0; i < types1.Count; i++)
        {
            AssertThat(types1[i]).IsEqual(types2[i]);
        }
    }
}
