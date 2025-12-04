using System;
using System.Collections.Generic;

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
/// Types of jobs available.
/// </summary>
public enum JobType
{
    Assault,    // Attack enemies at target
    Defense,    // Defend position (future)
    Extraction  // Rescue/retrieve (future)
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

    public static JobReward FromDifficulty(JobDifficulty difficulty)
    {
        return difficulty switch
        {
            JobDifficulty.Easy => new JobReward { Money = 100, Parts = 10 },
            JobDifficulty.Medium => new JobReward { Money = 200, Parts = 25, Ammo = 10 },
            JobDifficulty.Hard => new JobReward { Money = 400, Parts = 50, Fuel = 20, Ammo = 20 },
            _ => new JobReward { Money = 100 }
        };
    }

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
    public JobType Type { get; set; } = JobType.Assault;
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
    /// Get state for serialization.
    /// </summary>
    public JobData GetState()
    {
        return new JobData
        {
            Id = Id,
            Title = Title,
            Description = Description,
            Type = Type.ToString(),
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
            MissionConfigSeed = MissionConfigSeed
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
            Type = Enum.TryParse<JobType>(data.Type, out var type) ? type : JobType.Assault,
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
            MissionConfigSeed = data.MissionConfigSeed
        };
        return job;
    }
}
