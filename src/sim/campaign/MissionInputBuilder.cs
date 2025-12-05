using Godot;
using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// Builds MissionInput from campaign state and job configuration.
/// Stateless utility for converting campaign data to tactical input (MG3).
/// </summary>
public static class MissionInputBuilder
{
    /// <summary>
    /// Build a complete MissionInput from campaign state and active job.
    /// </summary>
    public static MissionInput Build(CampaignState campaign, Job job)
    {
        var config = job.MissionConfig ?? MissionConfig.CreateTestMission();
        
        var input = new MissionInput
        {
            MissionId = $"job_{job.Id}",
            MissionName = job.Title,
            MapTemplate = config.MapTemplate,
            GridSize = config.GridSize,
            Seed = campaign.Rng?.Tactical.NextInt(int.MaxValue) ?? System.Environment.TickCount,
            Context = BuildContext(campaign, job)
        };
        
        // Add crew deployments
        var aliveCrew = campaign.GetAliveCrew();
        var deployableCrew = GetDeployableCrew(aliveCrew);
        
        for (int i = 0; i < deployableCrew.Count && i < config.CrewSpawnPositions.Count; i++)
        {
            var crew = deployableCrew[i];
            var deployment = BuildCrewDeployment(crew, campaign, config.CrewSpawnPositions[i]);
            input.Crew.Add(deployment);
        }
        
        // Add enemies from config
        foreach (var spawn in config.EnemySpawns)
        {
            input.Enemies.Add(spawn);
        }
        
        // Add interactables from config
        foreach (var spawn in config.InteractableSpawns)
        {
            input.Interactables.Add(spawn);
        }
        
        // Add objectives from job
        AddObjectivesFromJob(input, job);
        
        SimLog.Log($"[MissionInputBuilder] Built input for '{job.Title}' with {input.Crew.Count} crew, {input.Enemies.Count} enemies");
        
        return input;
    }
    
    /// <summary>
    /// Filter crew to only those who can deploy.
    /// </summary>
    private static List<CrewMember> GetDeployableCrew(List<CrewMember> crew)
    {
        var result = new List<CrewMember>();
        foreach (var c in crew)
        {
            if (c.CanDeploy())
            {
                result.Add(c);
            }
        }
        return result;
    }
    
    /// <summary>
    /// Build deployment data for a single crew member.
    /// </summary>
    private static CrewDeployment BuildCrewDeployment(
        CrewMember crew, 
        CampaignState campaign,
        Vector2I spawnPosition)
    {
        var deployment = new CrewDeployment
        {
            CampaignCrewId = crew.Id,
            Name = crew.Name,
            SpawnPosition = spawnPosition
        };
        
        // Apply stats from crew (using effective stats with trait modifiers)
        ApplyCrewStats(deployment, crew);
        
        // Apply equipment
        ApplyCrewEquipment(deployment, crew, campaign);
        
        return deployment;
    }
    
    /// <summary>
    /// Apply crew stats to deployment using derived stat formulas.
    /// </summary>
    private static void ApplyCrewStats(CrewDeployment deployment, CrewMember crew)
    {
        // HP from Grit (using CrewMember's formula)
        deployment.MaxHp = crew.GetMaxHp();
        deployment.CurrentHp = deployment.MaxHp;
        
        // Accuracy and move speed from StatFormulas
        deployment.Accuracy = StatFormulas.CalculateAccuracy(crew.GetEffectiveStat(CrewStatType.Aim));
        deployment.MoveSpeed = StatFormulas.CalculateMoveSpeed(crew.GetEffectiveStat(CrewStatType.Reflexes));
    }
    
    /// <summary>
    /// Apply crew equipment to deployment.
    /// </summary>
    private static void ApplyCrewEquipment(
        CrewDeployment deployment, 
        CrewMember crew, 
        CampaignState campaign)
    {
        // Resolve weapon using centralized method
        string weaponDefId = crew.GetEffectiveWeaponId(campaign.Inventory);
        deployment.WeaponId = weaponDefId;
        
        // Get weapon data for magazine size
        var weaponData = WeaponData.FromId(weaponDefId);
        deployment.AmmoInMagazine = weaponData.MagazineSize;
        
        // Reserve ammo from campaign pool using StatFormulas
        deployment.ReserveAmmo = StatFormulas.CalculateReserveAmmo(weaponData.MagazineSize, campaign.Ammo);
    }
    
    /// <summary>
    /// Build mission context from campaign and job.
    /// </summary>
    private static MissionContext BuildContext(CampaignState campaign, Job job)
    {
        var context = new MissionContext
        {
            ContractId = job.Id,
            FactionId = job.EmployerFactionId
        };
        
        // Get location info from WorldState (WD1)
        if (campaign.World != null)
        {
            var system = campaign.World.GetSystem(job.TargetNodeId);
            if (system != null)
            {
                context.LocationId = system.Id.ToString();
                context.LocationName = system.Name;
                
                // Add system type as tag
                context.Tags.Add(system.Type.ToString().ToLower());
                
                // Add faction as tag if present
                if (!string.IsNullOrEmpty(system.OwningFactionId))
                {
                    context.Tags.Add($"faction_{system.OwningFactionId}");
                }
                
                // Add world tags
                foreach (var tag in system.Tags)
                {
                    context.Tags.Add(tag);
                }
            }
        }
        
        // Add job-related tags
        context.Tags.Add($"difficulty_{job.Difficulty.ToString().ToLower()}");
        context.Tags.Add($"contract_{job.ContractType.ToString().ToLower()}");
        
        return context;
    }
    
    /// <summary>
    /// Add objectives from job to mission input.
    /// </summary>
    private static void AddObjectivesFromJob(MissionInput input, Job job)
    {
        // Add primary objective
        if (job.PrimaryObjective != null)
        {
            input.Objectives.Add(ConvertObjective(job.PrimaryObjective, true));
        }
        else
        {
            // Default to eliminate all if no primary objective defined
            input.Objectives.Add(new MissionObjective
            {
                Id = "primary",
                Description = job.Description,
                Type = ObjectiveType.EliminateAll,
                IsPrimary = true
            });
        }
        
        // Add secondary objectives
        if (job.SecondaryObjectives != null)
        {
            foreach (var secondary in job.SecondaryObjectives)
            {
                input.Objectives.Add(ConvertObjective(secondary, false));
            }
        }
    }
    
    /// <summary>
    /// Convert a Job Objective to a MissionObjective.
    /// </summary>
    private static MissionObjective ConvertObjective(Objective objective, bool isPrimary)
    {
        var missionObj = new MissionObjective
        {
            Id = objective.Id,
            Description = objective.Description,
            Type = objective.Type,
            IsPrimary = isPrimary
        };
        
        // Copy type-specific parameters
        if (objective.Parameters != null)
        {
            if (objective.Parameters.TryGetValue("target_name", out var targetName))
            {
                missionObj.TargetActorType = targetName?.ToString();
            }
            if (objective.Parameters.TryGetValue("turns", out var turns) && turns is int turnCount)
            {
                // Store turn limit in TargetInteractableId as a workaround until proper field exists
            }
        }
        
        return missionObj;
    }
}
