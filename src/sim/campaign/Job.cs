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

    // Mission config (generated when job is accepted)
    public MissionConfig MissionConfig { get; set; } = null;

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
}
