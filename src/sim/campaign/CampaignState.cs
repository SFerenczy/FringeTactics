using System;
using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// Result data from a completed mission, used to update campaign.
/// </summary>
public class MissionResult
{
    public bool Victory { get; set; }
    public List<int> DeadCrewIds { get; set; } = new();
    public List<int> InjuredCrewIds { get; set; } = new();
    public Dictionary<int, int> CrewXpGains { get; set; } = new(); // crewId -> xp
    public int EnemiesKilled { get; set; }
}

public class CampaignState
{
    // Resources
    public int Money { get; set; } = 0;
    public int Fuel { get; set; } = 100;
    public int Parts { get; set; } = 0;
    public int Meds { get; set; } = 5;
    public int Ammo { get; set; } = 50;

    // Sector and location
    public Sector Sector { get; set; }
    public int CurrentNodeId { get; set; } = 0;

    // Crew roster
    public List<CrewMember> Crew { get; set; } = new();
    private int nextCrewId = 0;

    // Jobs
    public List<Job> AvailableJobs { get; set; } = new();
    public Job CurrentJob { get; set; } = null;

    // Faction reputation (factionId -> rep, 0-100, 50 = neutral)
    public Dictionary<string, int> FactionRep { get; set; } = new();

    // Mission tracking
    public int MissionsCompleted { get; set; } = 0;
    public int MissionsFailed { get; set; } = 0;

    // Campaign statistics (for end screen)
    public int TotalMoneyEarned { get; set; } = 0;
    public int TotalCrewDeaths { get; set; } = 0;

    // Mission costs (consumed on mission start)
    public const int MISSION_AMMO_COST = 10;
    public const int MISSION_FUEL_COST = 5;

    // Rewards
    public const int VICTORY_MONEY = 150;
    public const int VICTORY_PARTS = 20;
    public const int XP_PER_KILL = 25;
    public const int XP_PARTICIPATION = 10;
    public const int XP_VICTORY_BONUS = 20;
    public const int XP_RETREAT_BONUS = 5;

    public CampaignState()
    {
    }

    /// <summary>
    /// Create a new campaign with starting crew and resources.
    /// </summary>
    public static CampaignState CreateNew(int sectorSeed = 12345)
    {
        var campaign = new CampaignState
        {
            Money = 200,
            Fuel = 100,
            Parts = 50,
            Meds = 5,
            Ammo = 50
        };

        // Generate sector
        campaign.Sector = Sector.GenerateTestSector(sectorSeed);
        campaign.CurrentNodeId = 0; // Start at Haven Station

        // Initialize faction reputation (50 = neutral)
        foreach (var factionId in campaign.Sector.Factions.Keys)
        {
            campaign.FactionRep[factionId] = 50;
        }

        // Add 4 starting crew members with varied roles
        campaign.AddCrew("Alex", CrewRole.Soldier);
        campaign.AddCrew("Jordan", CrewRole.Soldier);
        campaign.AddCrew("Morgan", CrewRole.Medic);
        campaign.AddCrew("Casey", CrewRole.Tech);

        // Generate initial jobs at starting location
        campaign.RefreshJobsAtCurrentNode(new Random(sectorSeed));

        return campaign;
    }

    /// <summary>
    /// Refresh available jobs at current node.
    /// </summary>
    public void RefreshJobsAtCurrentNode(Random rng = null)
    {
        rng ??= new Random();
        AvailableJobs = JobSystem.GenerateJobsForNode(Sector, CurrentNodeId, rng);
        SimLog.Log($"[Campaign] Generated {AvailableJobs.Count} jobs at {GetCurrentNode()?.Name}");
    }

    /// <summary>
    /// Accept a job and set it as current.
    /// </summary>
    public bool AcceptJob(Job job)
    {
        if (CurrentJob != null)
        {
            SimLog.Log("[Campaign] Cannot accept job - already have an active job");
            return false;
        }

        if (!AvailableJobs.Contains(job))
        {
            SimLog.Log("[Campaign] Cannot accept job - not in available jobs");
            return false;
        }

        CurrentJob = job;
        AvailableJobs.Remove(job);

        // Generate mission config for the job
        CurrentJob.MissionConfig = JobSystem.GenerateMissionConfig(job, new Random());

        SimLog.Log($"[Campaign] Accepted job: {job.Title} at {Sector.GetNode(job.TargetNodeId)?.Name}");
        return true;
    }

    /// <summary>
    /// Clear current job (on completion or abandonment).
    /// </summary>
    public void ClearCurrentJob()
    {
        CurrentJob = null;
    }

    /// <summary>
    /// Check if player is at the target node for current job.
    /// </summary>
    public bool IsAtJobTarget()
    {
        return CurrentJob != null && CurrentNodeId == CurrentJob.TargetNodeId;
    }

    /// <summary>
    /// Get faction reputation (0-100, 50 = neutral).
    /// </summary>
    public int GetFactionRep(string factionId)
    {
        return FactionRep.TryGetValue(factionId, out var rep) ? rep : 50;
    }

    /// <summary>
    /// Modify faction reputation.
    /// </summary>
    public void ModifyFactionRep(string factionId, int delta)
    {
        if (string.IsNullOrEmpty(factionId)) return;

        if (!FactionRep.ContainsKey(factionId))
        {
            FactionRep[factionId] = 50;
        }

        FactionRep[factionId] = Math.Clamp(FactionRep[factionId] + delta, 0, 100);
        var factionName = Sector.Factions.GetValueOrDefault(factionId, factionId);
        SimLog.Log($"[Campaign] {factionName} rep: {FactionRep[factionId]} ({(delta >= 0 ? "+" : "")}{delta})");
    }

    /// <summary>
    /// Get the current sector node.
    /// </summary>
    public SectorNode GetCurrentNode()
    {
        return Sector?.GetNode(CurrentNodeId);
    }

    public CrewMember AddCrew(string name, CrewRole role = CrewRole.Soldier)
    {
        var crew = new CrewMember(nextCrewId, name)
        {
            Role = role
        };
        nextCrewId++;
        Crew.Add(crew);
        return crew;
    }

    public List<CrewMember> GetAliveCrew()
    {
        var alive = new List<CrewMember>();
        foreach (var crew in Crew)
        {
            if (!crew.IsDead)
            {
                alive.Add(crew);
            }
        }
        return alive;
    }

    public List<CrewMember> GetDeployableCrew()
    {
        var deployable = new List<CrewMember>();
        foreach (var crew in Crew)
        {
            if (crew.CanDeploy())
            {
                deployable.Add(crew);
            }
        }
        return deployable;
    }

    /// <summary>
    /// Check if we can afford to start a mission.
    /// </summary>
    public bool CanStartMission()
    {
        if (GetDeployableCrew().Count == 0) return false;
        if (Ammo < MISSION_AMMO_COST) return false;
        if (Fuel < MISSION_FUEL_COST) return false;
        return true;
    }

    /// <summary>
    /// Get reason why mission can't start.
    /// </summary>
    public string GetMissionBlockReason()
    {
        if (GetDeployableCrew().Count == 0) return "No deployable crew!";
        if (Ammo < MISSION_AMMO_COST) return $"Need {MISSION_AMMO_COST} ammo (have {Ammo})";
        if (Fuel < MISSION_FUEL_COST) return $"Need {MISSION_FUEL_COST} fuel (have {Fuel})";
        return null;
    }

    /// <summary>
    /// Consume resources when starting a mission.
    /// </summary>
    public void ConsumeMissionResources()
    {
        Ammo -= MISSION_AMMO_COST;
        Fuel -= MISSION_FUEL_COST;
        SimLog.Log($"[Campaign] Mission started. Consumed {MISSION_AMMO_COST} ammo, {MISSION_FUEL_COST} fuel.");
    }

    /// <summary>
    /// Apply mission output to campaign state.
    /// This is the primary method for processing mission results.
    /// </summary>
    public void ApplyMissionOutput(MissionOutput output)
    {
        // Process each crew outcome
        foreach (var crewOutcome in output.CrewOutcomes)
        {
            var crew = GetCrewById(crewOutcome.CampaignCrewId);
            if (crew == null) continue;

            // Handle death
            if (crewOutcome.Status == CrewFinalStatus.Dead)
            {
                crew.IsDead = true;
                TotalCrewDeaths++;
                SimLog.Log($"[Campaign] {crew.Name} KIA.");
                continue;
            }

            // Handle MIA (treated as dead for now)
            if (crewOutcome.Status == CrewFinalStatus.MIA)
            {
                crew.IsDead = true;
                TotalCrewDeaths++;
                SimLog.Log($"[Campaign] {crew.Name} MIA - presumed dead.");
                continue;
            }

            // Apply injuries
            foreach (var injury in crewOutcome.NewInjuries)
            {
                crew.AddInjury(injury);
                SimLog.Log($"[Campaign] {crew.Name} received injury: {injury}");
            }

            // Apply XP
            if (crewOutcome.SuggestedXp > 0)
            {
                bool leveledUp = crew.AddXp(crewOutcome.SuggestedXp);
                if (leveledUp)
                {
                    SimLog.Log($"[Campaign] {crew.Name} leveled up to {crew.Level}!");
                }
            }
        }

        // Apply victory/defeat/retreat rewards
        bool isVictory = output.Outcome == MissionOutcome.Victory;
        bool isRetreat = output.Outcome == MissionOutcome.Retreat;

        if (isVictory)
        {
            MissionsCompleted++;

            if (CurrentJob != null)
            {
                ApplyJobReward(CurrentJob.Reward);
                ModifyFactionRep(CurrentJob.EmployerFactionId, CurrentJob.RepGain);
                ModifyFactionRep(CurrentJob.TargetFactionId, -CurrentJob.RepLoss);
                SimLog.Log($"[Campaign] Job completed: {CurrentJob.Title}");
                ClearCurrentJob();
            }
            else
            {
                Money += VICTORY_MONEY;
                Parts += VICTORY_PARTS;
                SimLog.Log($"[Campaign] Victory! +${VICTORY_MONEY}, +{VICTORY_PARTS} parts.");
            }
        }
        else if (isRetreat)
        {
            // Retreat: partial failure, no rewards but reduced penalty
            MissionsFailed++;
            
            if (CurrentJob != null)
            {
                // Half the reputation loss for retreat vs full failure
                ModifyFactionRep(CurrentJob.EmployerFactionId, -CurrentJob.FailureRepLoss / 2);
                SimLog.Log($"[Campaign] Job abandoned (retreat): {CurrentJob.Title}");
                ClearCurrentJob();
            }
            else
            {
                SimLog.Log("[Campaign] Mission retreat. No rewards.");
            }
        }
        else
        {
            // Defeat or Abort
            MissionsFailed++;

            if (CurrentJob != null)
            {
                ModifyFactionRep(CurrentJob.EmployerFactionId, -CurrentJob.FailureRepLoss);
                SimLog.Log($"[Campaign] Job failed: {CurrentJob.Title}");
                ClearCurrentJob();
            }
            else
            {
                SimLog.Log("[Campaign] Mission failed. No rewards.");
            }
        }
    }

    /// <summary>
    /// Apply full mission result to campaign state.
    /// DEPRECATED: Use ApplyMissionOutput instead.
    /// </summary>
    [System.Obsolete("Use ApplyMissionOutput(MissionOutput) instead")]
    public void ApplyMissionResult(MissionResult result)
    {
        // Mark dead crew
        foreach (var crewId in result.DeadCrewIds)
        {
            var crew = GetCrewById(crewId);
            if (crew != null)
            {
                crew.IsDead = true;
                TotalCrewDeaths++;
                SimLog.Log($"[Campaign] {crew.Name} KIA.");
            }
        }

        // Apply injuries to survivors
        foreach (var crewId in result.InjuredCrewIds)
        {
            var crew = GetCrewById(crewId);
            if (crew != null && !crew.IsDead)
            {
                crew.AddInjury(InjuryTypes.Wounded);
                SimLog.Log($"[Campaign] {crew.Name} was wounded.");
            }
        }

        // Award XP
        foreach (var kvp in result.CrewXpGains)
        {
            var crew = GetCrewById(kvp.Key);
            if (crew != null && !crew.IsDead)
            {
                bool leveledUp = crew.AddXp(kvp.Value);
                if (leveledUp)
                {
                    SimLog.Log($"[Campaign] {crew.Name} leveled up to {crew.Level}!");
                }
            }
        }

        // Apply victory/defeat rewards
        if (result.Victory)
        {
            MissionsCompleted++;

            // Apply job rewards if we have an active job
            if (CurrentJob != null)
            {
                ApplyJobReward(CurrentJob.Reward);
                ModifyFactionRep(CurrentJob.EmployerFactionId, CurrentJob.RepGain);
                ModifyFactionRep(CurrentJob.TargetFactionId, -CurrentJob.RepLoss);
                SimLog.Log($"[Campaign] Job completed: {CurrentJob.Title}");
                ClearCurrentJob();
            }
            else
            {
                // Fallback rewards for non-job missions
                Money += VICTORY_MONEY;
                Parts += VICTORY_PARTS;
                SimLog.Log($"[Campaign] Victory! +${VICTORY_MONEY}, +{VICTORY_PARTS} parts.");
            }
        }
        else
        {
            MissionsFailed++;

            // Apply job failure penalty
            if (CurrentJob != null)
            {
                ModifyFactionRep(CurrentJob.EmployerFactionId, -CurrentJob.FailureRepLoss);
                SimLog.Log($"[Campaign] Job failed: {CurrentJob.Title}");
                ClearCurrentJob();
            }
            else
            {
                SimLog.Log("[Campaign] Mission failed. No rewards.");
            }
        }
    }

    /// <summary>
    /// Apply job reward to campaign resources.
    /// </summary>
    private void ApplyJobReward(JobReward reward)
    {
        Money += reward.Money;
        TotalMoneyEarned += reward.Money;
        Parts += reward.Parts;
        Fuel += reward.Fuel;
        Ammo += reward.Ammo;
        SimLog.Log($"[Campaign] Reward: {reward}");
    }

    /// <summary>
    /// Check if the campaign is over (all crew dead).
    /// </summary>
    public bool IsCampaignOver()
    {
        return GetAliveCrew().Count == 0;
    }

    /// <summary>
    /// Legacy method for compatibility - converts to MissionResult.
    /// DEPRECATED: Use ApplyMissionOutput instead.
    /// </summary>
    [System.Obsolete("Use ApplyMissionOutput(MissionOutput) instead")]
    public void ApplyMissionResult(bool victory, List<int> deadCrewIds)
    {
        #pragma warning disable CS0618 // Suppress obsolete warning for internal call
        ApplyMissionResult(new MissionResult
        {
            Victory = victory,
            DeadCrewIds = deadCrewIds
        });
        #pragma warning restore CS0618
    }

    /// <summary>
    /// Use meds to heal an injured crew member.
    /// </summary>
    public bool HealCrewMember(int crewId)
    {
        if (Meds <= 0) return false;

        var crew = GetCrewById(crewId);
        if (crew == null || crew.IsDead || crew.Injuries.Count == 0) return false;

        // Heal one injury
        if (crew.Injuries.Count > 0)
        {
            var injury = crew.Injuries[0];
            crew.HealInjury(injury);
            Meds--;
            SimLog.Log($"[Campaign] Healed {crew.Name}'s {injury}. Meds remaining: {Meds}");
            return true;
        }

        return false;
    }

    public CrewMember GetCrewById(int id)
    {
        foreach (var crew in Crew)
        {
            if (crew.Id == id)
            {
                return crew;
            }
        }
        return null;
    }
}
