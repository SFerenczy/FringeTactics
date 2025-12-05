using Godot; // For Vector2I only - no Node/UI types
using System;
using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// Stable API for job/contract operations.
/// Contract generation delegates to ContractGenerator (GN1).
/// Mission config generation will move to MissionFactory in future.
/// </summary>
public static class JobSystem
{
    /// <summary>
    /// Generate job offers for a given system.
    /// Uses ContractGenerator internally with campaign's RNG for determinism.
    /// </summary>
    public static List<Job> GenerateJobsForNode(CampaignState campaign, int systemId, int count = 3)
    {
        var context = GenerationContext.FromCampaign(campaign);

        if (systemId != campaign.CurrentNodeId)
        {
            context.CurrentNodeId = systemId;
            context.HubSystem = campaign.World?.GetSystem(systemId);
        }

        var generator = new ContractGenerator(context);
        return generator.GenerateContracts(count, $"contract_{campaign.GenerateJobId()}");
    }

    /// <summary>
    /// Generate a MissionConfig for a job based on its difficulty.
    /// TODO: Move to MissionFactory when tactical layer is refactored.
    /// </summary>
    public static MissionConfig GenerateMissionConfig(Job job)
    {
        var config = new MissionConfig
        {
            Id = $"mission_{job.Id}",
            Name = job.Title
        };

        // Set grid size and spawns based on difficulty
        switch (job.Difficulty)
        {
            case JobDifficulty.Easy:
                config.GridSize = new Vector2I(10, 8);
                config.CrewSpawnPositions = new List<Vector2I>
                {
                    new Vector2I(2, 2),
                    new Vector2I(3, 3),
                    new Vector2I(2, 4),
                    new Vector2I(3, 5)
                };
                config.EnemySpawns = new List<EnemySpawn>
                {
                    new EnemySpawn(EnemyIds.Grunt, new Vector2I(7, 3)),
                    new EnemySpawn(EnemyIds.Grunt, new Vector2I(8, 5))
                };
                break;

            case JobDifficulty.Medium:
                config.GridSize = new Vector2I(12, 10);
                config.CrewSpawnPositions = new List<Vector2I>
                {
                    new Vector2I(2, 2),
                    new Vector2I(4, 2),
                    new Vector2I(3, 4),
                    new Vector2I(5, 4)
                };
                config.EnemySpawns = new List<EnemySpawn>
                {
                    new EnemySpawn(EnemyIds.Grunt, new Vector2I(9, 3)),
                    new EnemySpawn(EnemyIds.Gunner, new Vector2I(10, 5)),
                    new EnemySpawn(EnemyIds.Grunt, new Vector2I(8, 7))
                };
                break;

            case JobDifficulty.Hard:
                config.GridSize = new Vector2I(14, 12);
                config.CrewSpawnPositions = new List<Vector2I>
                {
                    new Vector2I(2, 2),
                    new Vector2I(4, 2),
                    new Vector2I(3, 4),
                    new Vector2I(5, 4)
                };
                config.EnemySpawns = new List<EnemySpawn>
                {
                    new EnemySpawn(EnemyIds.Grunt, new Vector2I(10, 3)),
                    new EnemySpawn(EnemyIds.Gunner, new Vector2I(11, 5)),
                    new EnemySpawn(EnemyIds.Grunt, new Vector2I(9, 7)),
                    new EnemySpawn(EnemyIds.Heavy, new Vector2I(12, 6))
                };
                break;
        }

        return config;
    }
}
