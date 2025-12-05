using Godot;
using GdUnit4;
using static GdUnit4.Assertions;
using System.Collections.Generic;

namespace FringeTactics.Tests;

[TestSuite]
public class SF3CrewMemberSerializationTests
{
    [TestCase]
    public void CrewMember_RoundTrip_PreservesAllFields()
    {
        var crew = new CrewMember(42, "Test Soldier")
        {
            Role = CrewRole.Medic,
            IsDead = false,
            Level = 3,
            Xp = 75,
            Aim = 5,
            Grit = 2,
            Reflexes = 1,
            Tech = 3,
            Savvy = 1,
            Resolve = 2,
            PreferredWeaponId = "smg"
        };
        crew.AddInjury("wounded");
        crew.AddInjury("concussed");

        var state = crew.GetState();
        var restored = CrewMember.FromState(state);

        AssertInt(restored.Id).IsEqual(42);
        AssertString(restored.Name).IsEqual("Test Soldier");
        AssertThat(restored.Role).IsEqual(CrewRole.Medic);
        AssertBool(restored.IsDead).IsFalse();
        AssertInt(restored.Level).IsEqual(3);
        AssertInt(restored.Xp).IsEqual(75);
        AssertInt(restored.Aim).IsEqual(5);
        AssertInt(restored.Grit).IsEqual(2);
        AssertInt(restored.Reflexes).IsEqual(1);
        AssertInt(restored.Tech).IsEqual(3);
        AssertInt(restored.Savvy).IsEqual(1);
        AssertInt(restored.Resolve).IsEqual(2);
        AssertString(restored.PreferredWeaponId).IsEqual("smg");
        AssertInt(restored.Injuries.Count).IsEqual(2);
        AssertBool(restored.Injuries.Contains("wounded")).IsTrue();
        AssertBool(restored.Injuries.Contains("concussed")).IsTrue();
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

        var restored = CrewMember.FromState(data);

        AssertThat(restored.Injuries).IsNotNull();
        AssertInt(restored.Injuries.Count).IsEqual(0);
    }

    [TestCase]
    public void CrewMember_FromState_HandlesUnknownRole()
    {
        var data = new CrewMemberData
        {
            Id = 1,
            Name = "Test",
            Role = "UnknownRole"
        };

        var restored = CrewMember.FromState(data);

        AssertThat(restored.Role).IsEqual(CrewRole.Soldier);
    }

    [TestCase]
    public void CrewMember_FromState_HandlesNullWeaponId()
    {
        var data = new CrewMemberData
        {
            Id = 1,
            Name = "Test",
            PreferredWeaponId = null
        };

        var restored = CrewMember.FromState(data);

        AssertString(restored.PreferredWeaponId).IsEqual("rifle");
    }
}

[TestSuite]
public class SF3JobSerializationTests
{
    [TestCase]
    public void JobReward_RoundTrip_PreservesAllFields()
    {
        var reward = new JobReward
        {
            Money = 500,
            Parts = 25,
            Fuel = 10,
            Ammo = 15
        };

        var state = reward.GetState();
        var restored = JobReward.FromState(state);

        AssertInt(restored.Money).IsEqual(500);
        AssertInt(restored.Parts).IsEqual(25);
        AssertInt(restored.Fuel).IsEqual(10);
        AssertInt(restored.Ammo).IsEqual(15);
    }

    [TestCase]
    public void JobReward_FromState_HandlesNull()
    {
        var restored = JobReward.FromState(null);

        AssertInt(restored.Money).IsEqual(0);
        AssertInt(restored.Parts).IsEqual(0);
    }

    [TestCase]
    public void Job_RoundTrip_PreservesAllFields()
    {
        var job = new Job("job_123")
        {
            Title = "Test Mission",
            Description = "A test job",
            Type = JobType.Extraction,
            Difficulty = JobDifficulty.Hard,
            OriginNodeId = 1,
            TargetNodeId = 5,
            EmployerFactionId = "corp",
            TargetFactionId = "pirates",
            Reward = new JobReward { Money = 500, Parts = 25, Fuel = 10, Ammo = 15 },
            RepGain = 15,
            RepLoss = 10,
            FailureRepLoss = 20,
            DeadlineDays = 5,
            DeadlineDay = 12
        };

        var state = job.GetState();
        var restored = Job.FromState(state);

        AssertString(restored.Id).IsEqual("job_123");
        AssertString(restored.Title).IsEqual("Test Mission");
        AssertString(restored.Description).IsEqual("A test job");
        AssertThat(restored.Type).IsEqual(JobType.Extraction);
        AssertThat(restored.Difficulty).IsEqual(JobDifficulty.Hard);
        AssertInt(restored.OriginNodeId).IsEqual(1);
        AssertInt(restored.TargetNodeId).IsEqual(5);
        AssertString(restored.EmployerFactionId).IsEqual("corp");
        AssertString(restored.TargetFactionId).IsEqual("pirates");
        AssertInt(restored.Reward.Money).IsEqual(500);
        AssertInt(restored.RepGain).IsEqual(15);
        AssertInt(restored.RepLoss).IsEqual(10);
        AssertInt(restored.FailureRepLoss).IsEqual(20);
        AssertInt(restored.DeadlineDays).IsEqual(5);
        AssertInt(restored.DeadlineDay).IsEqual(12);
    }

    [TestCase]
    public void Job_FromState_HandlesUnknownEnums()
    {
        var data = new JobData
        {
            Id = "test",
            Type = "UnknownType",
            Difficulty = "UnknownDifficulty"
        };

        var restored = Job.FromState(data);

        AssertThat(restored.Type).IsEqual(JobType.Assault);
        AssertThat(restored.Difficulty).IsEqual(JobDifficulty.Easy);
    }
}

[TestSuite]
public class SF3SectorSerializationTests
{
    [TestCase]
    [RequireGodotRuntime]
    public void SectorNode_RoundTrip_PreservesPosition()
    {
        var node = new SectorNode(3, "Test Station", NodeType.Contested, new Vector2(150.5f, 275.25f))
        {
            FactionId = "rebels",
            HasJob = true,
            Connections = new List<int> { 1, 2, 5 }
        };

        var state = node.GetState();
        var restored = SectorNode.FromState(state);

        AssertInt(restored.Id).IsEqual(3);
        AssertString(restored.Name).IsEqual("Test Station");
        AssertThat(restored.Type).IsEqual(NodeType.Contested);
        AssertString(restored.FactionId).IsEqual("rebels");
        AssertFloat(restored.Position.X).IsEqualApprox(150.5f, 0.01f);
        AssertFloat(restored.Position.Y).IsEqualApprox(275.25f, 0.01f);
        AssertBool(restored.HasJob).IsTrue();
        AssertInt(restored.Connections.Count).IsEqual(3);
        AssertBool(restored.Connections.Contains(1)).IsTrue();
        AssertBool(restored.Connections.Contains(2)).IsTrue();
        AssertBool(restored.Connections.Contains(5)).IsTrue();
    }

    [TestCase]
    public void SectorNode_FromState_HandlesUnknownType()
    {
        var data = new SectorNodeData
        {
            Id = 1,
            Name = "Test",
            Type = "UnknownType"
        };

        var restored = SectorNode.FromState(data);

        AssertThat(restored.Type).IsEqual(NodeType.Station);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Sector_RoundTrip_PreservesNodesAndFactions()
    {
        var sector = new Sector
        {
            Name = "Test Sector",
            Factions = new Dictionary<string, string>
            {
                { "corp", "Helix Corp" },
                { "rebels", "Free Colonies" }
            }
        };
        sector.Nodes.Add(new SectorNode(0, "Node A", NodeType.Station, new Vector2(100, 100)));
        sector.Nodes.Add(new SectorNode(1, "Node B", NodeType.Outpost, new Vector2(200, 200)));

        var state = sector.GetState();
        var restored = Sector.FromState(state);

        AssertString(restored.Name).IsEqual("Test Sector");
        AssertInt(restored.Factions.Count).IsEqual(2);
        AssertString(restored.Factions["corp"]).IsEqual("Helix Corp");
        AssertInt(restored.Nodes.Count).IsEqual(2);
        AssertString(restored.Nodes[0].Name).IsEqual("Node A");
        AssertString(restored.Nodes[1].Name).IsEqual("Node B");
    }
}

[TestSuite]
public class SF3CampaignStateSerializationTests
{
    [TestCase]
    public void CampaignState_RoundTrip_PreservesResources()
    {
        var campaign = CampaignState.CreateNew(12345);
        campaign.Money = 999;
        campaign.Fuel = 50;
        campaign.Parts = 75;
        campaign.Meds = 3;
        campaign.Ammo = 25;

        var state = campaign.GetState();
        var restored = CampaignState.FromState(state);

        AssertInt(restored.Money).IsEqual(999);
        AssertInt(restored.Fuel).IsEqual(50);
        AssertInt(restored.Parts).IsEqual(75);
        AssertInt(restored.Meds).IsEqual(3);
        AssertInt(restored.Ammo).IsEqual(25);
    }

    [TestCase]
    public void CampaignState_RoundTrip_PreservesCrew()
    {
        var campaign = CampaignState.CreateNew(12345);
        var originalCrewCount = campaign.Crew.Count;
        var originalFirstName = campaign.Crew[0].Name;

        var state = campaign.GetState();
        var restored = CampaignState.FromState(state);

        AssertInt(restored.Crew.Count).IsEqual(originalCrewCount);
        AssertString(restored.Crew[0].Name).IsEqual(originalFirstName);
    }

    [TestCase]
    public void CampaignState_RoundTrip_PreservesTime()
    {
        var campaign = CampaignState.CreateNew(12345);
        campaign.Time.AdvanceDays(15);

        var state = campaign.GetState();
        var restored = CampaignState.FromState(state);

        AssertInt(restored.Time.CurrentDay).IsEqual(16);
    }

    [TestCase]
    public void CampaignState_RoundTrip_PreservesStats()
    {
        var campaign = CampaignState.CreateNew(12345);
        campaign.MissionsCompleted = 5;
        campaign.MissionsFailed = 2;
        campaign.TotalMoneyEarned = 1500;
        campaign.TotalCrewDeaths = 1;

        var state = campaign.GetState();
        var restored = CampaignState.FromState(state);

        AssertInt(restored.MissionsCompleted).IsEqual(5);
        AssertInt(restored.MissionsFailed).IsEqual(2);
        AssertInt(restored.TotalMoneyEarned).IsEqual(1500);
        AssertInt(restored.TotalCrewDeaths).IsEqual(1);
    }

    [TestCase]
    public void CampaignState_RoundTrip_PreservesFactionRep()
    {
        var campaign = CampaignState.CreateNew(12345);
        campaign.ModifyFactionRep("corp", 10);
        campaign.ModifyFactionRep("rebels", -5);

        var state = campaign.GetState();
        var restored = CampaignState.FromState(state);

        AssertInt(restored.GetFactionRep("corp")).IsEqual(60);
        AssertInt(restored.GetFactionRep("rebels")).IsEqual(45);
    }

    [TestCase]
    public void CampaignState_RoundTrip_PreservesLocation()
    {
        var campaign = CampaignState.CreateNew(12345);
        campaign.CurrentNodeId = 3;

        var state = campaign.GetState();
        var restored = CampaignState.FromState(state);

        AssertInt(restored.CurrentNodeId).IsEqual(3);
    }

    [TestCase]
    public void CampaignState_RoundTrip_PreservesSector()
    {
        var campaign = CampaignState.CreateNew(12345);
        var originalSectorName = campaign.Sector.Name;
        var originalNodeCount = campaign.Sector.Nodes.Count;

        var state = campaign.GetState();
        var restored = CampaignState.FromState(state);

        AssertString(restored.Sector.Name).IsEqual(originalSectorName);
        AssertInt(restored.Sector.Nodes.Count).IsEqual(originalNodeCount);
    }

    [TestCase]
    public void CampaignState_RoundTrip_PreservesRngState()
    {
        var campaign = CampaignState.CreateNew(12345);

        // Consume some RNG values
        for (int i = 0; i < 50; i++)
        {
            campaign.Rng.Campaign.NextFloat();
        }

        var state = campaign.GetState();
        var restored = CampaignState.FromState(state);

        // Both should produce the same next value
        var originalNext = campaign.Rng.Campaign.NextFloat();
        var restoredNext = restored.Rng.Campaign.NextFloat();

        AssertFloat(restoredNext).IsEqualApprox(originalNext, 0.0001f);
    }

    [TestCase]
    public void CampaignState_RoundTrip_PreservesAvailableJobs()
    {
        var campaign = CampaignState.CreateNew(12345);
        var originalJobCount = campaign.AvailableJobs.Count;

        var state = campaign.GetState();
        var restored = CampaignState.FromState(state);

        AssertInt(restored.AvailableJobs.Count).IsEqual(originalJobCount);
    }
}

[TestSuite]
public class SF3SaveManagerTests
{
    [TestCase]
    public void SaveData_SerializeDeserialize_RoundTrip()
    {
        var campaign = CampaignState.CreateNew(12345);
        var saveData = SaveManager.CreateSaveData(campaign, "Test Save");

        var json = SaveManager.Serialize(saveData);
        var restored = SaveManager.Deserialize(json);

        AssertInt(restored.Version).IsEqual(SaveVersion.Current);
        AssertString(restored.DisplayName).IsEqual("Test Save");
        AssertThat(restored.Campaign).IsNotNull();
    }

    [TestCase]
    public void SaveData_CreateSaveData_SetsVersion()
    {
        var campaign = CampaignState.CreateNew(12345);
        var saveData = SaveManager.CreateSaveData(campaign);

        AssertInt(saveData.Version).IsEqual(SaveVersion.Current);
    }

    [TestCase]
    public void SaveData_CreateSaveData_SetsTimestamp()
    {
        var campaign = CampaignState.CreateNew(12345);
        var before = System.DateTime.UtcNow;
        var saveData = SaveManager.CreateSaveData(campaign);
        var after = System.DateTime.UtcNow;

        AssertBool(saveData.SavedAt >= before).IsTrue();
        AssertBool(saveData.SavedAt <= after).IsTrue();
    }

    [TestCase]
    public void SaveData_CreateSaveData_GeneratesDisplayName()
    {
        var campaign = CampaignState.CreateNew(12345);
        campaign.Time.AdvanceDays(5);
        var saveData = SaveManager.CreateSaveData(campaign);

        AssertBool(saveData.DisplayName.Contains("Day 6")).IsTrue();
    }

    [TestCase]
    public void SaveData_Validation_DetectsMissingCampaign()
    {
        var saveData = new SaveData { Version = 1, Campaign = null };

        var result = SaveManager.ValidateSaveData(saveData);

        AssertBool(result.IsValid).IsFalse();
        AssertBool(result.Errors.Count > 0).IsTrue();
    }

    [TestCase]
    public void SaveData_Validation_DetectsInvalidVersion()
    {
        var saveData = new SaveData { Version = 0, Campaign = new CampaignStateData() };

        var result = SaveManager.ValidateSaveData(saveData);

        AssertBool(result.IsValid).IsFalse();
    }

    [TestCase]
    public void SaveData_Validation_DetectsMissingTime()
    {
        var saveData = new SaveData
        {
            Version = 1,
            Campaign = new CampaignStateData { Time = null, Sector = new SectorData() }
        };

        var result = SaveManager.ValidateSaveData(saveData);

        AssertBool(result.IsValid).IsFalse();
    }

    [TestCase]
    public void SaveData_Validation_DetectsMissingSector()
    {
        var saveData = new SaveData
        {
            Version = 1,
            Campaign = new CampaignStateData { Time = new CampaignTimeState(), Sector = null }
        };

        var result = SaveManager.ValidateSaveData(saveData);

        AssertBool(result.IsValid).IsFalse();
    }

    [TestCase]
    public void SaveData_Validation_WarnsOnEmptyCrew()
    {
        var saveData = new SaveData
        {
            Version = 1,
            Campaign = new CampaignStateData
            {
                Time = new CampaignTimeState(),
                Sector = new SectorData(),
                Crew = new List<CrewMemberData>()
            }
        };

        var result = SaveManager.ValidateSaveData(saveData);

        AssertBool(result.Warnings.Count > 0).IsTrue();
    }

    [TestCase]
    public void SaveData_Validation_PassesValidData()
    {
        var campaign = CampaignState.CreateNew(12345);
        var saveData = SaveManager.CreateSaveData(campaign);

        var result = SaveManager.ValidateSaveData(saveData);

        AssertBool(result.IsValid).IsTrue();
    }

    [TestCase]
    public void RestoreCampaign_FromSaveData_Works()
    {
        var original = CampaignState.CreateNew(12345);
        original.Money = 777;
        original.Time.AdvanceDays(10);

        var saveData = SaveManager.CreateSaveData(original);
        var json = SaveManager.Serialize(saveData);
        var loadedData = SaveManager.Deserialize(json);
        var restored = SaveManager.RestoreCampaign(loadedData);

        AssertInt(restored.Money).IsEqual(777);
        AssertInt(restored.Time.CurrentDay).IsEqual(11);
    }

    [TestCase]
    public void RestoreCampaign_RejectsNewerVersion()
    {
        var saveData = new SaveData
        {
            Version = SaveVersion.Current + 1,
            Campaign = new CampaignStateData()
        };

        AssertThrown(() => SaveManager.RestoreCampaign(saveData))
            .IsInstanceOf<System.InvalidOperationException>();
    }

    [TestCase]
    public void RestoreCampaign_RejectsNullData()
    {
        AssertThrown(() => SaveManager.RestoreCampaign(null))
            .IsInstanceOf<System.ArgumentException>();
    }
}

[TestSuite]
public class SF3FullRoundTripTests
{
    [TestCase]
    public void FullCampaign_JsonRoundTrip_PreservesEverything()
    {
        // Create a campaign with various state
        var original = CampaignState.CreateNew(99999);
        original.Money = 1234;
        original.Fuel = 77;
        original.Parts = 88;
        original.Meds = 9;
        original.Ammo = 33;
        original.Time.AdvanceDays(20);
        original.MissionsCompleted = 7;
        original.MissionsFailed = 3;
        original.TotalMoneyEarned = 5000;
        original.CurrentNodeId = 2;
        original.ModifyFactionRep("corp", 15);

        // Injure a crew member
        if (original.Crew.Count > 0)
        {
            original.Crew[0].AddInjury("wounded");
            original.Crew[0].AddXp(50);
        }

        // Consume some RNG
        for (int i = 0; i < 100; i++)
        {
            original.Rng.Campaign.NextFloat();
            original.Rng.Tactical.NextInt(100);
        }

        // Full round-trip through JSON
        var saveData = SaveManager.CreateSaveData(original, "Full Test");
        var json = SaveManager.Serialize(saveData);
        var loadedData = SaveManager.Deserialize(json);
        var restored = SaveManager.RestoreCampaign(loadedData);

        // Verify everything
        AssertInt(restored.Money).IsEqual(1234);
        AssertInt(restored.Fuel).IsEqual(77);
        AssertInt(restored.Parts).IsEqual(88);
        AssertInt(restored.Meds).IsEqual(9);
        AssertInt(restored.Ammo).IsEqual(33);
        AssertInt(restored.Time.CurrentDay).IsEqual(21);
        AssertInt(restored.MissionsCompleted).IsEqual(7);
        AssertInt(restored.MissionsFailed).IsEqual(3);
        AssertInt(restored.TotalMoneyEarned).IsEqual(5000);
        AssertInt(restored.CurrentNodeId).IsEqual(2);
        AssertInt(restored.GetFactionRep("corp")).IsEqual(65);

        // Verify crew
        AssertInt(restored.Crew.Count).IsEqual(original.Crew.Count);
        if (restored.Crew.Count > 0)
        {
            AssertBool(restored.Crew[0].Injuries.Contains("wounded")).IsTrue();
            AssertInt(restored.Crew[0].Xp).IsEqual(50);
        }

        // Verify RNG state preserved
        var originalNext = original.Rng.Campaign.NextFloat();
        var restoredNext = restored.Rng.Campaign.NextFloat();
        AssertFloat(restoredNext).IsEqualApprox(originalNext, 0.0001f);
    }
}
