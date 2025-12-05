using System;
using System.Collections.Generic;

namespace FringeTactics;

public class CampaignState
{
    // Time tracking
    public CampaignTime Time { get; private set; } = new();

    // RNG service for deterministic generation
    public RngService Rng { get; private set; }
    
    /// <summary>
    /// Event bus for cross-domain communication (optional, set by GameState).
    /// </summary>
    public EventBus EventBus { get; set; }

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
    private int nextJobId = 0;

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
    public const int MISSION_TIME_DAYS = 1;

    // Rest configuration
    public const int REST_TIME_DAYS = 3;
    public const int REST_HEAL_AMOUNT = 1;

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
    /// Generate a unique job ID for this campaign.
    /// </summary>
    public string GenerateJobId()
    {
        return $"job_{nextJobId++}";
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
            Ammo = 50,
            Time = new CampaignTime(),
            Rng = new RngService(sectorSeed)
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
        campaign.RefreshJobsAtCurrentNode();

        return campaign;
    }

    /// <summary>
    /// Refresh available jobs at current node using campaign RNG.
    /// </summary>
    public void RefreshJobsAtCurrentNode()
    {
        var rng = CreateSeededRandom();
        AvailableJobs = JobSystem.GenerateJobsForNode(this, CurrentNodeId, rng);
        SimLog.Log($"[Campaign] Generated {AvailableJobs.Count} jobs at {GetCurrentNode()?.Name}");
    }

    /// <summary>
    /// Create a seeded Random from campaign RNG for deterministic generation.
    /// </summary>
    private Random CreateSeededRandom()
    {
        return new Random(Rng?.Campaign?.NextInt(int.MaxValue) ?? Environment.TickCount);
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

        // Set absolute deadline from relative days
        if (job.DeadlineDays > 0)
        {
            job.DeadlineDay = Time.CurrentDay + job.DeadlineDays;
            SimLog.Log($"[Campaign] Job deadline: Day {job.DeadlineDay} ({job.DeadlineDays} days from now)");
        }

        // Generate mission config for the job using campaign RNG
        // Store the seed so we can regenerate identically on load
        CurrentJob.MissionConfigSeed = Rng?.Campaign?.NextInt(int.MaxValue) ?? Environment.TickCount;
        CurrentJob.MissionConfig = JobSystem.GenerateMissionConfig(job, new Random(CurrentJob.MissionConfigSeed));

        SimLog.Log($"[Campaign] Accepted job: {job.Title} at {Sector.GetNode(job.TargetNodeId)?.Name}");
        
        EventBus?.Publish(new JobAcceptedEvent(
            JobId: job.Id,
            JobTitle: job.Title,
            TargetNodeId: job.TargetNodeId,
            DeadlineDay: job.DeadlineDay
        ));
        
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

        int oldRep = FactionRep.GetValueOrDefault(factionId, 50);
        if (!FactionRep.ContainsKey(factionId))
        {
            FactionRep[factionId] = 50;
        }

        FactionRep[factionId] = Math.Clamp(FactionRep[factionId] + delta, 0, 100);
        int newRep = FactionRep[factionId];
        var factionName = Sector.Factions.GetValueOrDefault(factionId, factionId);
        SimLog.Log($"[Campaign] {factionName} rep: {newRep} ({(delta >= 0 ? "+" : "")}{delta})");
        
        EventBus?.Publish(new FactionRepChangedEvent(
            FactionId: factionId,
            FactionName: factionName,
            OldRep: oldRep,
            NewRep: newRep,
            Delta: delta
        ));
    }

    /// <summary>
    /// Get the current sector node.
    /// </summary>
    public SectorNode GetCurrentNode()
    {
        return Sector?.GetNode(CurrentNodeId);
    }

    /// <summary>
    /// Add a crew member without cost (for initial setup, testing).
    /// </summary>
    public CrewMember AddCrew(string name, CrewRole role = CrewRole.Soldier)
    {
        return CreateAndAddCrew(name, role);
    }

    /// <summary>
    /// Hire a new crew member. Costs credits.
    /// </summary>
    /// <param name="name">Crew member name</param>
    /// <param name="role">Crew role</param>
    /// <param name="cost">Hiring cost in credits</param>
    /// <returns>The hired crew member, or null if can't afford</returns>
    public CrewMember HireCrew(string name, CrewRole role, int cost)
    {
        if (Money < cost)
        {
            SimLog.Log($"[Campaign] Cannot hire {name}: insufficient funds ({Money}/{cost})");
            return null;
        }

        int oldMoney = Money;
        Money -= cost;

        var crew = CreateAndAddCrew(name, role);

        SimLog.Log($"[Campaign] Hired {name} ({role}) for {cost} credits");

        EventBus?.Publish(new ResourceChangedEvent(
            ResourceTypes.Money, oldMoney, Money, -cost, "hire_crew"));
        EventBus?.Publish(new CrewHiredEvent(crew.Id, crew.Name, role, cost));

        return crew;
    }

    /// <summary>
    /// Internal: creates crew and adds to roster.
    /// </summary>
    private CrewMember CreateAndAddCrew(string name, CrewRole role)
    {
        var crew = CrewMember.CreateWithRole(nextCrewId, name, role);
        nextCrewId++;
        Crew.Add(crew);
        return crew;
    }

    /// <summary>
    /// Fire a crew member. They are removed from the roster.
    /// </summary>
    /// <param name="crewId">ID of crew to fire</param>
    /// <returns>True if fired, false if not found, dead, or last crew</returns>
    public bool FireCrew(int crewId)
    {
        var crew = GetCrewById(crewId);
        if (crew == null)
        {
            SimLog.Log($"[Campaign] Cannot fire crew {crewId}: not found");
            return false;
        }

        if (crew.IsDead)
        {
            SimLog.Log($"[Campaign] Cannot fire {crew.Name}: already dead");
            return false;
        }

        if (GetAliveCrew().Count <= 1)
        {
            SimLog.Log($"[Campaign] Cannot fire {crew.Name}: last crew member");
            return false;
        }

        string name = crew.Name;
        Crew.Remove(crew);
        SimLog.Log($"[Campaign] Fired {name}");

        EventBus?.Publish(new CrewFiredEvent(crewId, name));

        return true;
    }

    /// <summary>
    /// Assign a trait to a crew member.
    /// </summary>
    public bool AssignTrait(int crewId, string traitId)
    {
        var crew = GetCrewById(crewId);
        if (crew == null || crew.IsDead) return false;

        if (!crew.AddTrait(traitId)) return false;

        var trait = TraitRegistry.Get(traitId);
        SimLog.Log($"[Campaign] {crew.Name} gained trait: {trait?.Name ?? traitId}");

        EventBus?.Publish(new CrewTraitChangedEvent(
            crewId, crew.Name, traitId, trait?.Name ?? traitId, true));
        return true;
    }

    /// <summary>
    /// Remove a trait from a crew member.
    /// </summary>
    public bool RemoveTrait(int crewId, string traitId)
    {
        var crew = GetCrewById(crewId);
        if (crew == null) return false;

        if (!crew.RemoveTrait(traitId)) return false;

        var trait = TraitRegistry.Get(traitId);
        SimLog.Log($"[Campaign] {crew.Name} lost trait: {trait?.Name ?? traitId}");

        EventBus?.Publish(new CrewTraitChangedEvent(
            crewId, crew.Name, traitId, trait?.Name ?? traitId, false));
        return true;
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
        int oldAmmo = Ammo;
        int oldFuel = Fuel;
        
        Ammo -= MISSION_AMMO_COST;
        Fuel -= MISSION_FUEL_COST;
        Time.AdvanceDays(MISSION_TIME_DAYS);
        SimLog.Log($"[Campaign] Mission started. Cost: {MISSION_AMMO_COST} ammo, {MISSION_FUEL_COST} fuel, {MISSION_TIME_DAYS} day(s).");
        
        EventBus?.Publish(new ResourceChangedEvent(
            ResourceType: ResourceTypes.Ammo,
            OldValue: oldAmmo,
            NewValue: Ammo,
            Delta: -MISSION_AMMO_COST,
            Reason: "mission_cost"
        ));
        
        EventBus?.Publish(new ResourceChangedEvent(
            ResourceType: ResourceTypes.Fuel,
            OldValue: oldFuel,
            NewValue: Fuel,
            Delta: -MISSION_FUEL_COST,
            Reason: "mission_cost"
        ));
    }

    /// <summary>
    /// Rest at current location. Heals injuries, advances time.
    /// </summary>
    /// <returns>Number of injuries healed.</returns>
    public int Rest()
    {
        int healed = 0;

        foreach (var crew in GetAliveCrew())
        {
            if (healed >= REST_HEAL_AMOUNT) break;
            if (crew.Injuries.Count > 0)
            {
                var injury = crew.Injuries[0];
                crew.HealInjury(injury);
                healed++;
                SimLog.Log($"[Campaign] {crew.Name}'s {injury} healed during rest.");
            }
        }

        Time.AdvanceDays(REST_TIME_DAYS);
        SimLog.Log($"[Campaign] Rested for {REST_TIME_DAYS} days. Healed {healed} injury(ies).");

        return healed;
    }

    /// <summary>
    /// Check if rest would be beneficial (any injuries to heal).
    /// </summary>
    public bool ShouldRest()
    {
        foreach (var crew in GetAliveCrew())
        {
            if (crew.Injuries.Count > 0) return true;
        }
        return false;
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
    /// Apply job reward to campaign resources.
    /// </summary>
    private void ApplyJobReward(JobReward reward)
    {
        int oldMoney = Money;
        int oldParts = Parts;
        int oldFuel = Fuel;
        int oldAmmo = Ammo;
        
        Money += reward.Money;
        TotalMoneyEarned += reward.Money;
        Parts += reward.Parts;
        Fuel += reward.Fuel;
        Ammo += reward.Ammo;
        SimLog.Log($"[Campaign] Reward: {reward}");
        
        if (reward.Money > 0)
        {
            EventBus?.Publish(new ResourceChangedEvent(ResourceTypes.Money, oldMoney, Money, reward.Money, "job_reward"));
        }
        if (reward.Parts > 0)
        {
            EventBus?.Publish(new ResourceChangedEvent(ResourceTypes.Parts, oldParts, Parts, reward.Parts, "job_reward"));
        }
        if (reward.Fuel > 0)
        {
            EventBus?.Publish(new ResourceChangedEvent(ResourceTypes.Fuel, oldFuel, Fuel, reward.Fuel, "job_reward"));
        }
        if (reward.Ammo > 0)
        {
            EventBus?.Publish(new ResourceChangedEvent(ResourceTypes.Ammo, oldAmmo, Ammo, reward.Ammo, "job_reward"));
        }
    }

    /// <summary>
    /// Check if the campaign is over (all crew dead).
    /// </summary>
    public bool IsCampaignOver()
    {
        return GetAliveCrew().Count == 0;
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

    /// <summary>
    /// Get state for serialization.
    /// </summary>
    public CampaignStateData GetState()
    {
        var data = new CampaignStateData
        {
            Time = Time.GetState(),
            Rng = Rng?.GetState(),
            CurrentNodeId = CurrentNodeId,
            NextCrewId = nextCrewId,
            NextJobId = nextJobId,
            FactionRep = new Dictionary<string, int>(FactionRep),
            Resources = new ResourcesData
            {
                Money = Money,
                Fuel = Fuel,
                Parts = Parts,
                Meds = Meds,
                Ammo = Ammo
            },
            Stats = new CampaignStatsData
            {
                MissionsCompleted = MissionsCompleted,
                MissionsFailed = MissionsFailed,
                TotalMoneyEarned = TotalMoneyEarned,
                TotalCrewDeaths = TotalCrewDeaths
            },
            Sector = Sector?.GetState()
        };

        // Serialize crew
        foreach (var crew in Crew)
        {
            data.Crew.Add(crew.GetState());
        }

        // Serialize available jobs
        foreach (var job in AvailableJobs)
        {
            data.AvailableJobs.Add(job.GetState());
        }

        // Serialize current job
        data.CurrentJob = CurrentJob?.GetState();

        return data;
    }

    /// <summary>
    /// Restore campaign from saved state.
    /// </summary>
    public static CampaignState FromState(CampaignStateData data)
    {
        var campaign = new CampaignState();

        // Restore time
        campaign.Time = new CampaignTime();
        if (data.Time != null)
        {
            campaign.Time.RestoreState(data.Time);
        }

        // Restore RNG
        if (data.Rng != null)
        {
            campaign.Rng = new RngService(data.Rng.MasterSeed);
            campaign.Rng.RestoreState(data.Rng);
        }
        else
        {
            campaign.Rng = new RngService();
        }

        // Restore resources
        if (data.Resources != null)
        {
            campaign.Money = data.Resources.Money;
            campaign.Fuel = data.Resources.Fuel;
            campaign.Parts = data.Resources.Parts;
            campaign.Meds = data.Resources.Meds;
            campaign.Ammo = data.Resources.Ammo;
        }

        // Restore location
        campaign.CurrentNodeId = data.CurrentNodeId;

        // Restore crew
        campaign.Crew.Clear();
        foreach (var crewData in data.Crew ?? new List<CrewMemberData>())
        {
            campaign.Crew.Add(CrewMember.FromState(crewData));
        }
        campaign.nextCrewId = data.NextCrewId;
        campaign.nextJobId = data.NextJobId;

        // Restore sector
        if (data.Sector != null)
        {
            campaign.Sector = Sector.FromState(data.Sector);
        }

        // Restore jobs
        campaign.AvailableJobs.Clear();
        foreach (var jobData in data.AvailableJobs ?? new List<JobData>())
        {
            campaign.AvailableJobs.Add(Job.FromState(jobData));
        }

        if (data.CurrentJob != null)
        {
            campaign.CurrentJob = Job.FromState(data.CurrentJob);
            // Regenerate MissionConfig using the stored seed for deterministic results
            if (campaign.CurrentJob != null && campaign.CurrentJob.MissionConfigSeed != 0)
            {
                var rng = new Random(campaign.CurrentJob.MissionConfigSeed);
                campaign.CurrentJob.MissionConfig = JobSystem.GenerateMissionConfig(campaign.CurrentJob, rng);
            }
        }

        // Restore faction rep
        campaign.FactionRep = new Dictionary<string, int>(data.FactionRep ?? new Dictionary<string, int>());

        // Restore stats
        if (data.Stats != null)
        {
            campaign.MissionsCompleted = data.Stats.MissionsCompleted;
            campaign.MissionsFailed = data.Stats.MissionsFailed;
            campaign.TotalMoneyEarned = data.Stats.TotalMoneyEarned;
            campaign.TotalCrewDeaths = data.Stats.TotalCrewDeaths;
        }

        return campaign;
    }
}
