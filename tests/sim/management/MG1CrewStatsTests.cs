using GdUnit4;
using static GdUnit4.Assertions;

namespace FringeTactics.Tests;

/// <summary>
/// MG1 Phase 1 tests - validates expanded crew stats system.
/// </summary>
[TestSuite]
public class MG1CrewStatsTests
{
    // === Basic Stats Tests ===

    [TestCase]
    public void CrewMember_HasAllSixStats()
    {
        var crew = new CrewMember(1, "Test");

        AssertThat(crew.Grit).IsEqual(0);
        AssertThat(crew.Reflexes).IsEqual(0);
        AssertThat(crew.Aim).IsEqual(0);
        AssertThat(crew.Tech).IsEqual(0);
        AssertThat(crew.Savvy).IsEqual(0);
        AssertThat(crew.Resolve).IsEqual(0);
    }

    [TestCase]
    public void CrewMember_StatsAreSettable()
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

        AssertThat(crew.Grit).IsEqual(3);
        AssertThat(crew.Reflexes).IsEqual(2);
        AssertThat(crew.Aim).IsEqual(5);
        AssertThat(crew.Tech).IsEqual(1);
        AssertThat(crew.Savvy).IsEqual(4);
        AssertThat(crew.Resolve).IsEqual(2);
    }

    // === Role-Based Stats Tests ===

    [TestCase]
    public void CreateWithRole_Soldier_HasCorrectStats()
    {
        var crew = CrewMember.CreateWithRole(1, "Soldier", CrewRole.Soldier);

        AssertThat(crew.Grit).IsEqual(3);
        AssertThat(crew.Reflexes).IsEqual(2);
        AssertThat(crew.Aim).IsEqual(3);
        AssertThat(crew.Tech).IsEqual(0);
        AssertThat(crew.Savvy).IsEqual(0);
        AssertThat(crew.Resolve).IsEqual(2);
        AssertThat(crew.Role).IsEqual(CrewRole.Soldier);
    }

    [TestCase]
    public void CreateWithRole_Medic_HasCorrectStats()
    {
        var crew = CrewMember.CreateWithRole(1, "Medic", CrewRole.Medic);

        AssertThat(crew.Grit).IsEqual(2);
        AssertThat(crew.Reflexes).IsEqual(1);
        AssertThat(crew.Aim).IsEqual(1);
        AssertThat(crew.Tech).IsEqual(2);
        AssertThat(crew.Savvy).IsEqual(1);
        AssertThat(crew.Resolve).IsEqual(3);
        AssertThat(crew.Role).IsEqual(CrewRole.Medic);
    }

    [TestCase]
    public void CreateWithRole_Tech_HasCorrectStats()
    {
        var crew = CrewMember.CreateWithRole(1, "Hacker", CrewRole.Tech);

        AssertThat(crew.Grit).IsEqual(1);
        AssertThat(crew.Reflexes).IsEqual(2);
        AssertThat(crew.Aim).IsEqual(1);
        AssertThat(crew.Tech).IsEqual(3);
        AssertThat(crew.Savvy).IsEqual(1);
        AssertThat(crew.Resolve).IsEqual(2);
        AssertThat(crew.Role).IsEqual(CrewRole.Tech);
    }

    [TestCase]
    public void CreateWithRole_Scout_HasCorrectStats()
    {
        var crew = CrewMember.CreateWithRole(1, "Scout", CrewRole.Scout);

        AssertThat(crew.Grit).IsEqual(2);
        AssertThat(crew.Reflexes).IsEqual(3);
        AssertThat(crew.Aim).IsEqual(2);
        AssertThat(crew.Tech).IsEqual(1);
        AssertThat(crew.Savvy).IsEqual(1);
        AssertThat(crew.Resolve).IsEqual(1);
        AssertThat(crew.Role).IsEqual(CrewRole.Scout);
    }

    // === Derived Stats Tests ===

    [TestCase]
    public void GetMaxHp_CalculatesFromGrit()
    {
        var crew = new CrewMember(1, "Test") { Grit = 0 };
        AssertThat(crew.GetMaxHp()).IsEqual(100);

        crew.Grit = 3;
        AssertThat(crew.GetMaxHp()).IsEqual(130);

        crew.Grit = 10;
        AssertThat(crew.GetMaxHp()).IsEqual(200);
    }

    [TestCase]
    public void GetHitBonus_CalculatesFromAim()
    {
        var crew = new CrewMember(1, "Test") { Aim = 0 };
        AssertThat(crew.GetHitBonus()).IsEqual(0);

        crew.Aim = 5;
        AssertThat(crew.GetHitBonus()).IsEqual(10);

        crew.Aim = 10;
        AssertThat(crew.GetHitBonus()).IsEqual(20);
    }

    [TestCase]
    public void GetHackBonus_CalculatesFromTech()
    {
        var crew = new CrewMember(1, "Test") { Tech = 0 };
        AssertThat(crew.GetHackBonus()).IsEqual(0);

        crew.Tech = 3;
        AssertThat(crew.GetHackBonus()).IsEqual(30);
    }

    [TestCase]
    public void GetTalkBonus_CalculatesFromSavvy()
    {
        var crew = new CrewMember(1, "Test") { Savvy = 0 };
        AssertThat(crew.GetTalkBonus()).IsEqual(0);

        crew.Savvy = 4;
        AssertThat(crew.GetTalkBonus()).IsEqual(40);
    }

    [TestCase]
    public void GetStressThreshold_CalculatesFromResolve()
    {
        var crew = new CrewMember(1, "Test") { Resolve = 0 };
        AssertThat(crew.GetStressThreshold()).IsEqual(50);

        crew.Resolve = 3;
        AssertThat(crew.GetStressThreshold()).IsEqual(80);
    }

    // === Stat Point Tests ===

    [TestCase]
    public void SpendStatPoint_IncreasesStatAndDecrementsPoints()
    {
        var crew = new CrewMember(1, "Test") { UnspentStatPoints = 2, Aim = 0 };

        bool result = crew.SpendStatPoint(CrewStatType.Aim);

        AssertThat(result).IsTrue();
        AssertThat(crew.Aim).IsEqual(1);
        AssertThat(crew.UnspentStatPoints).IsEqual(1);
    }

    [TestCase]
    public void SpendStatPoint_FailsWhenNoPoints()
    {
        var crew = new CrewMember(1, "Test") { UnspentStatPoints = 0, Aim = 0 };

        bool result = crew.SpendStatPoint(CrewStatType.Aim);

        AssertThat(result).IsFalse();
        AssertThat(crew.Aim).IsEqual(0);
        AssertThat(crew.UnspentStatPoints).IsEqual(0);
    }

    [TestCase]
    public void SpendStatPoint_FailsAtCap()
    {
        var crew = new CrewMember(1, "Test") { UnspentStatPoints = 1, Aim = 10 };

        bool result = crew.SpendStatPoint(CrewStatType.Aim);

        AssertThat(result).IsFalse();
        AssertThat(crew.Aim).IsEqual(10);
        AssertThat(crew.UnspentStatPoints).IsEqual(1);
    }

    [TestCase]
    public void SpendStatPoint_WorksForAllStats()
    {
        var crew = new CrewMember(1, "Test") { UnspentStatPoints = 6 };

        AssertThat(crew.SpendStatPoint(CrewStatType.Grit)).IsTrue();
        AssertThat(crew.Grit).IsEqual(1);

        AssertThat(crew.SpendStatPoint(CrewStatType.Reflexes)).IsTrue();
        AssertThat(crew.Reflexes).IsEqual(1);

        AssertThat(crew.SpendStatPoint(CrewStatType.Aim)).IsTrue();
        AssertThat(crew.Aim).IsEqual(1);

        AssertThat(crew.SpendStatPoint(CrewStatType.Tech)).IsTrue();
        AssertThat(crew.Tech).IsEqual(1);

        AssertThat(crew.SpendStatPoint(CrewStatType.Savvy)).IsTrue();
        AssertThat(crew.Savvy).IsEqual(1);

        AssertThat(crew.SpendStatPoint(CrewStatType.Resolve)).IsTrue();
        AssertThat(crew.Resolve).IsEqual(1);

        AssertThat(crew.UnspentStatPoints).IsEqual(0);
    }

    // === XP and Level Up Tests ===

    [TestCase]
    public void AddXp_LevelUp_GrantsStatPoint()
    {
        var crew = new CrewMember(1, "Test") { Xp = 90, Level = 1, UnspentStatPoints = 0 };

        bool leveledUp = crew.AddXp(15);

        AssertThat(leveledUp).IsTrue();
        AssertThat(crew.Level).IsEqual(2);
        AssertThat(crew.UnspentStatPoints).IsEqual(1);
        AssertThat(crew.Xp).IsEqual(5);
    }

    [TestCase]
    public void AddXp_NoLevelUp_NoStatPoint()
    {
        var crew = new CrewMember(1, "Test") { Xp = 50, Level = 1, UnspentStatPoints = 0 };

        bool leveledUp = crew.AddXp(30);

        AssertThat(leveledUp).IsFalse();
        AssertThat(crew.Level).IsEqual(1);
        AssertThat(crew.UnspentStatPoints).IsEqual(0);
        AssertThat(crew.Xp).IsEqual(80);
    }

    [TestCase]
    public void AddXp_MultipleLevelUps_GrantsMultiplePoints()
    {
        var crew = new CrewMember(1, "Test") { Xp = 0, Level = 1, UnspentStatPoints = 0 };

        crew.AddXp(100);
        AssertThat(crew.Level).IsEqual(2);
        AssertThat(crew.UnspentStatPoints).IsEqual(1);

        crew.AddXp(100);
        AssertThat(crew.Level).IsEqual(3);
        AssertThat(crew.UnspentStatPoints).IsEqual(2);
    }

    // === Serialization Tests ===

    [TestCase]
    public void GetState_IncludesAllStats()
    {
        var crew = new CrewMember(42, "TestCrew")
        {
            Role = CrewRole.Tech,
            Grit = 2,
            Reflexes = 3,
            Aim = 4,
            Tech = 5,
            Savvy = 1,
            Resolve = 2,
            Level = 3,
            Xp = 50,
            UnspentStatPoints = 2
        };

        var data = crew.GetState();

        AssertThat(data.Id).IsEqual(42);
        AssertThat(data.Name).IsEqual("TestCrew");
        AssertThat(data.Role).IsEqual("Tech");
        AssertThat(data.Grit).IsEqual(2);
        AssertThat(data.Reflexes).IsEqual(3);
        AssertThat(data.Aim).IsEqual(4);
        AssertThat(data.Tech).IsEqual(5);
        AssertThat(data.Savvy).IsEqual(1);
        AssertThat(data.Resolve).IsEqual(2);
        AssertThat(data.Level).IsEqual(3);
        AssertThat(data.Xp).IsEqual(50);
        AssertThat(data.UnspentStatPoints).IsEqual(2);
    }

    [TestCase]
    public void FromState_RestoresAllStats()
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
            Resolve = 2,
            Level = 3,
            Xp = 50,
            UnspentStatPoints = 2
        };

        var crew = CrewMember.FromState(data);

        AssertThat(crew.Id).IsEqual(42);
        AssertThat(crew.Name).IsEqual("TestCrew");
        AssertThat(crew.Role).IsEqual(CrewRole.Tech);
        AssertThat(crew.Grit).IsEqual(2);
        AssertThat(crew.Reflexes).IsEqual(3);
        AssertThat(crew.Aim).IsEqual(4);
        AssertThat(crew.Tech).IsEqual(5);
        AssertThat(crew.Savvy).IsEqual(1);
        AssertThat(crew.Resolve).IsEqual(2);
        AssertThat(crew.Level).IsEqual(3);
        AssertThat(crew.Xp).IsEqual(50);
        AssertThat(crew.UnspentStatPoints).IsEqual(2);
    }

    [TestCase]
    public void FromState_LegacySave_MigratesToughnessToGrit()
    {
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
    public void FromState_NewSave_UsesGritNotToughness()
    {
        var newData = new CrewMemberData
        {
            Id = 1,
            Name = "NewCrew",
            Role = "Soldier",
            Grit = 3,
            Toughness = 5
        };

        var crew = CrewMember.FromState(newData);

        AssertThat(crew.Grit).IsEqual(3);
    }

    [TestCase]
    public void RoundTrip_PreservesAllData()
    {
        var original = CrewMember.CreateWithRole(99, "RoundTrip", CrewRole.Scout);
        original.AddXp(150);
        original.SpendStatPoint(CrewStatType.Aim);

        var data = original.GetState();
        var restored = CrewMember.FromState(data);

        AssertThat(restored.Id).IsEqual(original.Id);
        AssertThat(restored.Name).IsEqual(original.Name);
        AssertThat(restored.Role).IsEqual(original.Role);
        AssertThat(restored.Level).IsEqual(original.Level);
        AssertThat(restored.Xp).IsEqual(original.Xp);
        AssertThat(restored.UnspentStatPoints).IsEqual(original.UnspentStatPoints);
        AssertThat(restored.Grit).IsEqual(original.Grit);
        AssertThat(restored.Reflexes).IsEqual(original.Reflexes);
        AssertThat(restored.Aim).IsEqual(original.Aim);
        AssertThat(restored.Tech).IsEqual(original.Tech);
        AssertThat(restored.Savvy).IsEqual(original.Savvy);
        AssertThat(restored.Resolve).IsEqual(original.Resolve);
    }

    // === CampaignState Integration Tests ===

    [TestCase]
    public void CampaignState_AddCrew_UsesRoleStats()
    {
        var campaign = CampaignState.CreateNew();
        int initialCount = campaign.Crew.Count;

        var newCrew = campaign.AddCrew("NewRecruit", CrewRole.Tech);

        AssertThat(campaign.Crew.Count).IsEqual(initialCount + 1);
        AssertThat(newCrew.Tech).IsEqual(3);
        AssertThat(newCrew.Role).IsEqual(CrewRole.Tech);
    }

    [TestCase]
    public void CampaignState_CreateNew_HasCrewWithRoleStats()
    {
        var campaign = CampaignState.CreateNew();

        var soldiers = campaign.Crew.FindAll(c => c.Role == CrewRole.Soldier);
        var medics = campaign.Crew.FindAll(c => c.Role == CrewRole.Medic);
        var techs = campaign.Crew.FindAll(c => c.Role == CrewRole.Tech);

        AssertThat(soldiers.Count).IsGreaterEqual(1);
        AssertThat(medics.Count).IsGreaterEqual(1);
        AssertThat(techs.Count).IsGreaterEqual(1);

        foreach (var soldier in soldiers)
        {
            AssertThat(soldier.Grit).IsEqual(3);
            AssertThat(soldier.Aim).IsEqual(3);
        }

        foreach (var medic in medics)
        {
            AssertThat(medic.Resolve).IsEqual(3);
        }

        foreach (var tech in techs)
        {
            AssertThat(tech.Tech).IsEqual(3);
        }
    }
}
