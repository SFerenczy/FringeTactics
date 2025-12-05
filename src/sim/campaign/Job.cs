using System;
using System.Collections.Generic;
using System.Linq;

namespace FringeTactics;

/// <summary>
/// Difficulty levels for jobs.
/// </summary>
public enum JobDifficulty
{
    Easy,
    Medium,
    Hard
}

/// <summary>
/// Reward structure for completing a job.
/// </summary>
public class JobReward
{
    public int Money { get; set; } = 0;
    public int Parts { get; set; } = 0;
    public int Fuel { get; set; } = 0;
    public int Ammo { get; set; } = 0;

    public override string ToString()
    {
        var parts = new List<string>();
        if (Money > 0) parts.Add($"${Money}");
        if (Parts > 0) parts.Add($"{Parts} parts");
        if (Fuel > 0) parts.Add($"{Fuel} fuel");
        if (Ammo > 0) parts.Add($"{Ammo} ammo");
        return string.Join(", ", parts);
    }

    /// <summary>
    /// Get state for serialization.
    /// </summary>
    public JobRewardData GetState()
    {
        return new JobRewardData
        {
            Money = Money,
            Parts = Parts,
            Fuel = Fuel,
            Ammo = Ammo
        };
    }

    /// <summary>
    /// Restore from saved state.
    /// </summary>
    public static JobReward FromState(JobRewardData data)
    {
        if (data == null) return new JobReward();
        return new JobReward
        {
            Money = data.Money,
            Parts = data.Parts,
            Fuel = data.Fuel,
            Ammo = data.Ammo
        };
    }
}

/// <summary>
/// A job offer available at a sector node.
/// </summary>
public class Job
{
    public string Id { get; set; }
    public string Title { get; set; } = "Unknown Job";
    public string Description { get; set; } = "No description";

    /// <summary>
    /// Contract archetype defining mission structure.
    /// </summary>
    public ContractType ContractType { get; set; } = ContractType.Assault;

    public JobDifficulty Difficulty { get; set; } = JobDifficulty.Easy;

    // Location
    public int OriginNodeId { get; set; }   // Where job was posted
    public int TargetNodeId { get; set; }   // Where mission takes place

    // Faction
    public string EmployerFactionId { get; set; }  // Who's paying
    public string TargetFactionId { get; set; }    // Who you're fighting (for rep)

    // Rewards
    public JobReward Reward { get; set; } = new();
    public int RepGain { get; set; } = 10;   // Rep with employer on success
    public int RepLoss { get; set; } = 5;    // Rep with target on success (negative)
    public int FailureRepLoss { get; set; } = 10; // Rep with employer on failure

    // Deadline tracking
    /// <summary>
    /// Days from acceptance until deadline (used during generation).
    /// </summary>
    public int DeadlineDays { get; set; } = 0;

    /// <summary>
    /// Absolute day by which the job must be completed.
    /// Set when job is accepted. 0 means no deadline.
    /// </summary>
    public int DeadlineDay { get; set; } = 0;

    /// <summary>
    /// Check if this job has a deadline.
    /// </summary>
    public bool HasDeadline => DeadlineDay > 0;

    // Mission config (generated when job is accepted)
    public MissionConfig MissionConfig { get; set; } = null;

    /// <summary>
    /// Seed used to generate MissionConfig. Stored for deterministic regeneration on load.
    /// </summary>
    public int MissionConfigSeed { get; set; } = 0;

    // Objectives (GN1)
    /// <summary>
    /// Primary objective that must be completed for mission success.
    /// </summary>
    public Objective PrimaryObjective { get; set; }

    /// <summary>
    /// Optional secondary objectives for bonus rewards.
    /// </summary>
    public List<Objective> SecondaryObjectives { get; set; } = new();

    public Job(string jobId)
    {
        Id = jobId;
    }

    /// <summary>
    /// Get a display-friendly difficulty string.
    /// </summary>
    public string GetDifficultyDisplay()
    {
        return Difficulty switch
        {
            JobDifficulty.Easy => "Easy",
            JobDifficulty.Medium => "Medium",
            JobDifficulty.Hard => "Hard",
            _ => "Unknown"
        };
    }

    /// <summary>
    /// Calculate total potential reward including all bonus objectives.
    /// </summary>
    public int GetMaxPotentialReward()
    {
        int baseReward = Reward?.Money ?? 0;
        int bonusPercent = 0;

        if (SecondaryObjectives != null)
        {
            foreach (var obj in SecondaryObjectives)
            {
                bonusPercent += obj.BonusRewardPercent;
            }
        }

        return baseReward + (baseReward * bonusPercent / 100);
    }

    /// <summary>
    /// Get state for serialization.
    /// </summary>
    public JobData GetState()
    {
        return new JobData
        {
            Id = Id,
            Title = Title,
            Description = Description,
            ContractType = ContractType.ToString(),
            Difficulty = Difficulty.ToString(),
            OriginNodeId = OriginNodeId,
            TargetNodeId = TargetNodeId,
            EmployerFactionId = EmployerFactionId,
            TargetFactionId = TargetFactionId,
            Reward = Reward?.GetState(),
            RepGain = RepGain,
            RepLoss = RepLoss,
            FailureRepLoss = FailureRepLoss,
            DeadlineDays = DeadlineDays,
            DeadlineDay = DeadlineDay,
            MissionConfigSeed = MissionConfigSeed,
            PrimaryObjective = PrimaryObjective?.GetState(),
            SecondaryObjectives = SecondaryObjectives?.Select(o => o.GetState()).ToList()
        };
    }

    /// <summary>
    /// Restore from saved state.
    /// </summary>
    public static Job FromState(JobData data)
    {
        var job = new Job(data.Id)
        {
            Title = data.Title ?? "Unknown Job",
            Description = data.Description ?? "",
            ContractType = ParseContractType(data),
            Difficulty = Enum.TryParse<JobDifficulty>(data.Difficulty, out var diff) ? diff : JobDifficulty.Easy,
            OriginNodeId = data.OriginNodeId,
            TargetNodeId = data.TargetNodeId,
            EmployerFactionId = data.EmployerFactionId,
            TargetFactionId = data.TargetFactionId,
            Reward = JobReward.FromState(data.Reward),
            RepGain = data.RepGain,
            RepLoss = data.RepLoss,
            FailureRepLoss = data.FailureRepLoss,
            DeadlineDays = data.DeadlineDays,
            DeadlineDay = data.DeadlineDay,
            MissionConfigSeed = data.MissionConfigSeed,
            PrimaryObjective = data.PrimaryObjective != null ? Objective.FromState(data.PrimaryObjective) : null,
            SecondaryObjectives = data.SecondaryObjectives?.Select(Objective.FromState).Where(o => o != null).ToList() ?? new List<Objective>()
        };
        return job;
    }

    /// <summary>
    /// Parse ContractType from save data, with fallback to legacy Type field.
    /// </summary>
    private static ContractType ParseContractType(JobData data)
    {
        // Try new ContractType field first
        if (!string.IsNullOrEmpty(data.ContractType) &&
            Enum.TryParse<ContractType>(data.ContractType, out var contractType))
        {
            return contractType;
        }

        // Fall back to legacy Type field for old saves (pre-GN1)
        return data.Type switch
        {
            "Assault" => ContractType.Assault,
            "Defense" => ContractType.Assault,
            "Extraction" => ContractType.Extraction,
            _ => ContractType.Assault
        };
    }
}
