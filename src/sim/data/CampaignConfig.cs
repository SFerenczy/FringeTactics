using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FringeTactics;

/// <summary>
/// Campaign balance configuration loaded from JSON.
/// Centralizes all tunable campaign parameters.
/// </summary>
public class CampaignConfig
{
    private static CampaignConfig instance;
    private static bool initialized;
    private const string ConfigPath = "res://data/campaign.json";

    // === Mission Costs ===
    public MissionCostConfig Mission { get; set; } = new();

    // === Rest Configuration ===
    public RestConfig Rest { get; set; } = new();

    // === Reward Configuration ===
    public RewardConfig Rewards { get; set; } = new();

    // === Crew Stat Configuration ===
    public CrewStatConfig Crew { get; set; } = new();

    // === Role Starting Stats ===
    public Dictionary<string, RoleStatBlock> RoleStats { get; set; } = new();

    // === Starting Resources ===
    public StartingResourcesConfig Starting { get; set; } = new();

    /// <summary>
    /// Get the singleton config instance.
    /// </summary>
    public static CampaignConfig Instance
    {
        get
        {
            EnsureInitialized();
            return instance;
        }
    }

    public static void EnsureInitialized()
    {
        if (initialized) return;

        instance = Load();
        initialized = true;
    }

    /// <summary>
    /// Reset to allow reloading (for testing).
    /// </summary>
    public static void Reset()
    {
        instance = null;
        initialized = false;
    }

    private static CampaignConfig Load()
    {
        try
        {
            string jsonPath = Godot.ProjectSettings.GlobalizePath(ConfigPath);
            if (!File.Exists(jsonPath))
            {
                SimLog.Log($"[CampaignConfig] Config not found at {ConfigPath}, using defaults");
                return CreateDefaults();
            }

            string json = File.ReadAllText(jsonPath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var config = JsonSerializer.Deserialize<CampaignConfig>(json, options);
            if (config == null)
            {
                SimLog.Log("[CampaignConfig] Failed to parse config, using defaults");
                return CreateDefaults();
            }

            SimLog.Log("[CampaignConfig] Loaded from JSON");
            return config;
        }
        catch (Exception ex)
        {
            SimLog.Log($"[CampaignConfig] Error loading config: {ex.Message}, using defaults");
            return CreateDefaults();
        }
    }

    private static CampaignConfig CreateDefaults()
    {
        return new CampaignConfig
        {
            Mission = new MissionCostConfig(),
            Rest = new RestConfig(),
            Rewards = new RewardConfig(),
            Crew = new CrewStatConfig(),
            Starting = new StartingResourcesConfig(),
            RoleStats = new Dictionary<string, RoleStatBlock>
            {
                ["soldier"] = new() { Grit = 3, Reflexes = 2, Aim = 3, Tech = 0, Savvy = 0, Resolve = 2 },
                ["medic"] = new() { Grit = 2, Reflexes = 1, Aim = 1, Tech = 2, Savvy = 1, Resolve = 3 },
                ["tech"] = new() { Grit = 1, Reflexes = 2, Aim = 1, Tech = 3, Savvy = 1, Resolve = 2 },
                ["scout"] = new() { Grit = 2, Reflexes = 3, Aim = 2, Tech = 1, Savvy = 1, Resolve = 1 }
            }
        };
    }

    /// <summary>
    /// Get starting stats for a role.
    /// </summary>
    public RoleStatBlock GetRoleStats(CrewRole role)
    {
        string key = role.ToString().ToLowerInvariant();
        if (RoleStats.TryGetValue(key, out var stats))
        {
            return stats;
        }
        return new RoleStatBlock();
    }
}

/// <summary>
/// Mission cost configuration.
/// </summary>
public class MissionCostConfig
{
    public int FuelCost { get; set; } = 5;
    public int TimeDays { get; set; } = 1;
}

/// <summary>
/// Rest action configuration.
/// </summary>
public class RestConfig
{
    public int TimeDays { get; set; } = 3;
    public int HealAmount { get; set; } = 1;
}

/// <summary>
/// Reward configuration for missions and XP.
/// </summary>
public class RewardConfig
{
    public int VictoryMoney { get; set; } = 150;
    public int VictoryParts { get; set; } = 20;
    public int XpPerKill { get; set; } = 25;
    public int XpParticipation { get; set; } = 10;
    public int XpVictoryBonus { get; set; } = 20;
    public int XpRetreatBonus { get; set; } = 5;
}

/// <summary>
/// Crew stat formula configuration.
/// </summary>
public class CrewStatConfig
{
    public int XpPerLevel { get; set; } = 100;
    public int StatCap { get; set; } = 10;
    public int BaseHp { get; set; } = 100;
    public int HpPerGrit { get; set; } = 10;
    public int HitBonusPerAim { get; set; } = 2;
    public int HackBonusPerTech { get; set; } = 10;
    public int TalkBonusPerSavvy { get; set; } = 10;
    public int BaseStressThreshold { get; set; } = 50;
    public int StressPerResolve { get; set; } = 10;
}

/// <summary>
/// Starting stats for a crew role.
/// </summary>
public class RoleStatBlock
{
    public int Grit { get; set; }
    public int Reflexes { get; set; }
    public int Aim { get; set; }
    public int Tech { get; set; }
    public int Savvy { get; set; }
    public int Resolve { get; set; }
}

/// <summary>
/// Starting resources for a new campaign.
/// </summary>
public class StartingResourcesConfig
{
    public int Money { get; set; } = 200;
    public int Fuel { get; set; } = 100;
    public int Parts { get; set; } = 50;
    public int Meds { get; set; } = 5;
    public int Ammo { get; set; } = 50;
}
