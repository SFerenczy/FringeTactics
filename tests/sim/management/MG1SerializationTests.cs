using GdUnit4;
using static GdUnit4.Assertions;
using System.Collections.Generic;

namespace FringeTactics.Tests;

/// <summary>
/// MG1 Phase 4 tests - validates serialization of all MG1 features.
/// </summary>
[TestSuite]
public class MG1SerializationTests
{
    // === Save Version Tests ===

    [TestCase]
    public void SaveVersion_IsThree()
    {
        // MG2 incremented save version to 3
        AssertThat(SaveVersion.Current).IsEqual(3);
    }

    // === CrewMemberData Field Tests ===

    [TestCase]
    public void CrewMemberData_HasAllStatFields()
    {
        var data = new CrewMemberData();

        // All stat fields should exist and default to 0
        AssertThat(data.Grit).IsEqual(0);
        AssertThat(data.Reflexes).IsEqual(0);
        AssertThat(data.Aim).IsEqual(0);
        AssertThat(data.Tech).IsEqual(0);
        AssertThat(data.Savvy).IsEqual(0);
        AssertThat(data.Resolve).IsEqual(0);
    }

    [TestCase]
    public void CrewMemberData_HasProgressionFields()
    {
        var data = new CrewMemberData();

        AssertThat(data.Level).IsEqual(0);
        AssertThat(data.Xp).IsEqual(0);
        AssertThat(data.UnspentStatPoints).IsEqual(0);
    }

    [TestCase]
    public void CrewMemberData_HasTraitIds()
    {
        var data = new CrewMemberData();

        AssertThat(data.TraitIds).IsNotNull();
        AssertThat(data.TraitIds.Count).IsEqual(0);
    }

    [TestCase]
    public void CrewMemberData_HasLegacyToughness()
    {
        var data = new CrewMemberData();
        AssertThat(data.Toughness).IsEqual(0);
    }

    // === CrewMember GetState Tests ===

    [TestCase]
    public void CrewMember_GetState_IncludesAllStats()
    {
        var crew = new CrewMember(1, "Test")
        {
            Grit = 3,
            Reflexes = 2,
            Aim = 5,
            Tech = 1,
            Savvy = 4,
            Resolve = 2
        };

        var data = crew.GetState();

        AssertThat(data.Grit).IsEqual(3);
        AssertThat(data.Reflexes).IsEqual(2);
        AssertThat(data.Aim).IsEqual(5);
        AssertThat(data.Tech).IsEqual(1);
        AssertThat(data.Savvy).IsEqual(4);
        AssertThat(data.Resolve).IsEqual(2);
    }

    [TestCase]
    public void CrewMember_GetState_IncludesProgression()
    {
        var crew = new CrewMember(1, "Test")
        {
            Level = 5,
            Xp = 75,
            UnspentStatPoints = 3
        };

        var data = crew.GetState();

        AssertThat(data.Level).IsEqual(5);
        AssertThat(data.Xp).IsEqual(75);
        AssertThat(data.UnspentStatPoints).IsEqual(3);
    }

    [TestCase]
    public void CrewMember_GetState_IncludesTraits()
    {
        var crew = new CrewMember(1, "Test");
        crew.AddTrait("brave");
        crew.AddTrait("ex_military");
        crew.AddTrait("damaged_eye");

        var data = crew.GetState();

        AssertThat(data.TraitIds.Count).IsEqual(3);
        AssertThat(data.TraitIds.Contains("brave")).IsTrue();
        AssertThat(data.TraitIds.Contains("ex_military")).IsTrue();
        AssertThat(data.TraitIds.Contains("damaged_eye")).IsTrue();
    }

    // === CrewMember FromState Tests ===

    [TestCase]
    public void CrewMember_FromState_RestoresAllStats()
    {
        var data = new CrewMemberData
        {
            Id = 42,
            Name = "TestCrew",
            Role = "Tech",
            Grit = 2,
            Reflexes = 3,
            Aim = 4,
            Tech = 5,
            Savvy = 1,
            Resolve = 2
        };

        var crew = CrewMember.FromState(data);

        AssertThat(crew.Grit).IsEqual(2);
        AssertThat(crew.Reflexes).IsEqual(3);
        AssertThat(crew.Aim).IsEqual(4);
        AssertThat(crew.Tech).IsEqual(5);
        AssertThat(crew.Savvy).IsEqual(1);
        AssertThat(crew.Resolve).IsEqual(2);
    }

    [TestCase]
    public void CrewMember_FromState_RestoresProgression()
    {
        var data = new CrewMemberData
        {
            Id = 1,
            Name = "Test",
            Role = "Soldier",
            Level = 7,
            Xp = 50,
            UnspentStatPoints = 4
        };

        var crew = CrewMember.FromState(data);

        AssertThat(crew.Level).IsEqual(7);
        AssertThat(crew.Xp).IsEqual(50);
        AssertThat(crew.UnspentStatPoints).IsEqual(4);
    }

    [TestCase]
    public void CrewMember_FromState_RestoresTraits()
    {
        var data = new CrewMemberData
        {
            Id = 1,
            Name = "Test",
            Role = "Soldier",
            TraitIds = new List<string> { "brave", "frontier_born" }
        };

        var crew = CrewMember.FromState(data);

        AssertThat(crew.HasTrait("brave")).IsTrue();
        AssertThat(crew.HasTrait("frontier_born")).IsTrue();
        AssertThat(crew.TraitIds.Count).IsEqual(2);
    }

    // === Backward Compatibility Tests ===

    [TestCase]
    public void CrewMember_FromState_LegacySave_MigratesToughnessToGrit()
    {
        // Simulate a v1 save with Toughness but no Grit
        var legacyData = new CrewMemberData
        {
            Id = 1,
            Name = "OldCrew",
            Role = "Soldier",
            Toughness = 5,
            Grit = 0,
            Aim = 3,
            Reflexes = 2
        };

        var crew = CrewMember.FromState(legacyData);

        AssertThat(crew.Grit).IsEqual(5);
    }

    [TestCase]
    public void CrewMember_FromState_NewSave_UsesGritNotToughness()
    {
        // New save has Grit set, Toughness should be ignored
        var newData = new CrewMemberData
        {
            Id = 1,
            Name = "NewCrew",
            Role = "Soldier",
            Grit = 3,
            Toughness = 5 // Should be ignored
        };

        var crew = CrewMember.FromState(newData);

        AssertThat(crew.Grit).IsEqual(3);
    }

    [TestCase]
    public void CrewMember_FromState_HandlesNullTraitIds()
    {
        var data = new CrewMemberData
        {
            Id = 1,
            Name = "Test",
            Role = "Soldier",
            TraitIds = null
        };

        var crew = CrewMember.FromState(data);

        AssertThat(crew.TraitIds).IsNotNull();
        AssertThat(crew.TraitIds.Count).IsEqual(0);
    }

    [TestCase]
    public void CrewMember_FromState_HandlesNullInjuries()
    {
        var data = new CrewMemberData
        {
            Id = 1,
            Name = "Test",
            Role = "Soldier",
            Injuries = null
        };

        var crew = CrewMember.FromState(data);

        AssertThat(crew.Injuries).IsNotNull();
        AssertThat(crew.Injuries.Count).IsEqual(0);
    }

    // === Full Round-Trip Tests ===

    [TestCase]
    public void CrewMember_RoundTrip_PreservesAllData()
    {
        var original = new CrewMember(99, "FullTest")
        {
            Role = CrewRole.Scout,
            IsDead = false,
            Level = 4,
            Xp = 65,
            UnspentStatPoints = 2,
            Grit = 3,
            Reflexes = 4,
            Aim = 2,
            Tech = 1,
            Savvy = 2,
            Resolve = 3,
            PreferredWeaponId = "smg"
        };
        original.AddInjury("wounded");
        original.AddTrait("brave");
        original.AddTrait("ex_military");

        var data = original.GetState();
        var restored = CrewMember.FromState(data);

        // Identity
        AssertThat(restored.Id).IsEqual(99);
        AssertThat(restored.Name).IsEqual("FullTest");
        AssertThat(restored.Role).IsEqual(CrewRole.Scout);

        // Status
        AssertThat(restored.IsDead).IsFalse();
        AssertThat(restored.Injuries.Count).IsEqual(1);
        AssertThat(restored.Injuries.Contains("wounded")).IsTrue();

        // Progression
        AssertThat(restored.Level).IsEqual(4);
        AssertThat(restored.Xp).IsEqual(65);
        AssertThat(restored.UnspentStatPoints).IsEqual(2);

        // Stats
        AssertThat(restored.Grit).IsEqual(3);
        AssertThat(restored.Reflexes).IsEqual(4);
        AssertThat(restored.Aim).IsEqual(2);
        AssertThat(restored.Tech).IsEqual(1);
        AssertThat(restored.Savvy).IsEqual(2);
        AssertThat(restored.Resolve).IsEqual(3);

        // Traits
        AssertThat(restored.HasTrait("brave")).IsTrue();
        AssertThat(restored.HasTrait("ex_military")).IsTrue();
        AssertThat(restored.TraitIds.Count).IsEqual(2);

        // Equipment
        AssertThat(restored.PreferredWeaponId).IsEqual("smg");
    }

    [TestCase]
    public void CrewMember_RoundTrip_PreservesEffectiveStats()
    {
        var original = CrewMember.CreateWithRole(1, "StatTest", CrewRole.Soldier);
        original.AddTrait("ex_military"); // +1 Aim
        original.AddTrait("frontier_born"); // +1 Grit

        int originalEffectiveAim = original.GetEffectiveStat(CrewStatType.Aim);
        int originalEffectiveGrit = original.GetEffectiveStat(CrewStatType.Grit);
        int originalMaxHp = original.GetMaxHp();

        var data = original.GetState();
        var restored = CrewMember.FromState(data);

        AssertThat(restored.GetEffectiveStat(CrewStatType.Aim)).IsEqual(originalEffectiveAim);
        AssertThat(restored.GetEffectiveStat(CrewStatType.Grit)).IsEqual(originalEffectiveGrit);
        AssertThat(restored.GetMaxHp()).IsEqual(originalMaxHp);
    }

    [TestCase]
    public void CrewMember_RoundTrip_WithLevelUpAndStatSpend()
    {
        var original = CrewMember.CreateWithRole(1, "LevelTest", CrewRole.Tech);
        original.AddXp(150); // Level up, get stat point
        original.SpendStatPoint(CrewStatType.Tech); // Spend it

        var data = original.GetState();
        var restored = CrewMember.FromState(data);

        AssertThat(restored.Level).IsEqual(2);
        AssertThat(restored.Xp).IsEqual(50);
        AssertThat(restored.UnspentStatPoints).IsEqual(0);
        AssertThat(restored.Tech).IsEqual(4); // 3 base + 1 spent
    }

    // === CampaignState Integration Tests ===

    [TestCase]
    public void CampaignState_RoundTrip_PreservesCrewWithTraits()
    {
        var campaign = CampaignState.CreateNew();

        // Modify crew
        var crew = campaign.Crew[0];
        crew.AddTrait("brave");
        crew.AddTrait("hardened");
        crew.AddXp(100); // Level up
        crew.SpendStatPoint(CrewStatType.Aim);

        var data = campaign.GetState();
        var restored = CampaignState.FromState(data);

        var restoredCrew = restored.GetCrewById(crew.Id);
        AssertThat(restoredCrew).IsNotNull();
        AssertThat(restoredCrew.HasTrait("brave")).IsTrue();
        AssertThat(restoredCrew.HasTrait("hardened")).IsTrue();
        AssertThat(restoredCrew.Level).IsEqual(2);
    }

    [TestCase]
    public void CampaignState_RoundTrip_PreservesHiredCrew()
    {
        var campaign = CampaignState.CreateNew();
        campaign.Money = 500;

        var hired = campaign.HireCrew("NewHire", CrewRole.Scout, 100);
        hired.AddTrait("spacer");

        var data = campaign.GetState();
        var restored = CampaignState.FromState(data);

        var restoredHired = restored.GetCrewById(hired.Id);
        AssertThat(restoredHired).IsNotNull();
        AssertThat(restoredHired.Name).IsEqual("NewHire");
        AssertThat(restoredHired.Role).IsEqual(CrewRole.Scout);
        AssertThat(restoredHired.HasTrait("spacer")).IsTrue();
        AssertThat(restored.Money).IsEqual(400);
    }

    [TestCase]
    public void CampaignState_RoundTrip_PreservesAllCrewStats()
    {
        var campaign = CampaignState.CreateNew();

        // Verify all starting crew have proper role stats after round-trip
        var data = campaign.GetState();
        var restored = CampaignState.FromState(data);

        foreach (var originalCrew in campaign.Crew)
        {
            var restoredCrew = restored.GetCrewById(originalCrew.Id);
            AssertThat(restoredCrew).IsNotNull();
            AssertThat(restoredCrew.Grit).IsEqual(originalCrew.Grit);
            AssertThat(restoredCrew.Reflexes).IsEqual(originalCrew.Reflexes);
            AssertThat(restoredCrew.Aim).IsEqual(originalCrew.Aim);
            AssertThat(restoredCrew.Tech).IsEqual(originalCrew.Tech);
            AssertThat(restoredCrew.Savvy).IsEqual(originalCrew.Savvy);
            AssertThat(restoredCrew.Resolve).IsEqual(originalCrew.Resolve);
        }
    }
}
