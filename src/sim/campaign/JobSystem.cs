using Godot; // For Vector2I only - no Node/UI types
using System;
using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// Stateless system for generating and managing jobs.
/// </summary>
public static class JobSystem
{
    private static readonly string[] AssaultTitles = new[]
    {
        "Clear the Area",
        "Hostile Elimination",
        "Strike Mission",
        "Raid Operation",
        "Combat Sweep"
    };

    private static int nextJobId = 0;

    /// <summary>
    /// Generate job offers for a given node.
    /// </summary>
    public static List<Job> GenerateJobsForNode(Sector sector, int nodeId, Random rng, int count = 3)
    {
        var jobs = new List<Job>();
        var originNode = sector.GetNode(nodeId);
        if (originNode == null) return jobs;

        // Get potential target nodes (connected or nearby)
        var potentialTargets = GetPotentialTargets(sector, nodeId);
        if (potentialTargets.Count == 0) return jobs;

        for (int i = 0; i < count; i++)
        {
            var job = GenerateSingleJob(sector, originNode, potentialTargets, rng);
            if (job != null)
            {
                jobs.Add(job);
            }
        }

        return jobs;
    }

    private static List<int> GetPotentialTargets(Sector sector, int originId)
    {
        var targets = new List<int>();
        var originNode = sector.GetNode(originId);
        if (originNode == null) return targets;

        // Include connected nodes
        foreach (var connId in originNode.Connections)
        {
            var node = sector.GetNode(connId);
            if (node != null && node.Type != NodeType.Station) // Stations are safe, not targets
            {
                targets.Add(connId);
            }
        }

        // Also include nodes connected to connected nodes (2-hop)
        foreach (var connId in originNode.Connections)
        {
            var connNode = sector.GetNode(connId);
            if (connNode == null) continue;

            foreach (var secondHop in connNode.Connections)
            {
                if (secondHop != originId && !targets.Contains(secondHop))
                {
                    var node = sector.GetNode(secondHop);
                    if (node != null && node.Type != NodeType.Station)
                    {
                        targets.Add(secondHop);
                    }
                }
            }
        }

        return targets;
    }

    private static Job GenerateSingleJob(Sector sector, SectorNode origin, List<int> potentialTargets, Random rng)
    {
        if (potentialTargets.Count == 0) return null;

        // Pick random target
        var targetId = potentialTargets[rng.Next(potentialTargets.Count)];
        var targetNode = sector.GetNode(targetId);
        if (targetNode == null) return null;

        // Determine difficulty based on target type
        var difficulty = DetermineJobDifficulty(targetNode, rng);

        // Pick employer faction (origin node's faction, or random if unclaimed)
        var employerFaction = origin.FactionId;
        if (string.IsNullOrEmpty(employerFaction))
        {
            var factionIds = new List<string>(sector.Factions.Keys);
            if (factionIds.Count > 0)
            {
                employerFaction = factionIds[rng.Next(factionIds.Count)];
            }
        }

        // Target faction is whoever controls the target, or pirates by default
        var targetFaction = targetNode.FactionId ?? "pirates";

        // Generate job
        var job = new Job($"job_{nextJobId++}")
        {
            Title = AssaultTitles[rng.Next(AssaultTitles.Length)],
            Description = $"Assault operation at {targetNode.Name}",
            Type = JobType.Assault,
            Difficulty = difficulty,
            OriginNodeId = origin.Id,
            TargetNodeId = targetId,
            EmployerFactionId = employerFaction,
            TargetFactionId = targetFaction,
            Reward = JobReward.FromDifficulty(difficulty),
            RepGain = GetRepGain(difficulty),
            RepLoss = GetRepLoss(difficulty),
            FailureRepLoss = GetFailureRepLoss(difficulty)
        };

        return job;
    }

    private static JobDifficulty DetermineJobDifficulty(SectorNode target, Random rng)
    {
        // Base difficulty on node type
        var baseDifficulty = target.Type switch
        {
            NodeType.Contested => JobDifficulty.Hard,
            NodeType.Derelict => JobDifficulty.Medium,
            NodeType.Asteroid => JobDifficulty.Easy,
            NodeType.Outpost => JobDifficulty.Medium,
            NodeType.Nebula => JobDifficulty.Medium,
            _ => JobDifficulty.Easy
        };

        // Random variance
        var roll = rng.NextDouble();
        if (roll < 0.2)
        {
            // Make easier
            return baseDifficulty switch
            {
                JobDifficulty.Hard => JobDifficulty.Medium,
                JobDifficulty.Medium => JobDifficulty.Easy,
                _ => JobDifficulty.Easy
            };
        }
        else if (roll > 0.8)
        {
            // Make harder
            return baseDifficulty switch
            {
                JobDifficulty.Easy => JobDifficulty.Medium,
                JobDifficulty.Medium => JobDifficulty.Hard,
                _ => JobDifficulty.Hard
            };
        }

        return baseDifficulty;
    }

    private static int GetRepGain(JobDifficulty difficulty)
    {
        return difficulty switch
        {
            JobDifficulty.Easy => 5,
            JobDifficulty.Medium => 10,
            JobDifficulty.Hard => 20,
            _ => 5
        };
    }

    private static int GetRepLoss(JobDifficulty difficulty)
    {
        return difficulty switch
        {
            JobDifficulty.Easy => 3,
            JobDifficulty.Medium => 5,
            JobDifficulty.Hard => 10,
            _ => 3
        };
    }

    private static int GetFailureRepLoss(JobDifficulty difficulty)
    {
        return difficulty switch
        {
            JobDifficulty.Easy => 5,
            JobDifficulty.Medium => 10,
            JobDifficulty.Hard => 15,
            _ => 5
        };
    }

    /// <summary>
    /// Generate a MissionConfig for a job based on its difficulty.
    /// </summary>
    public static MissionConfig GenerateMissionConfig(Job job, Random rng)
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
                    new EnemySpawn("grunt", new Vector2I(7, 3)),
                    new EnemySpawn("grunt", new Vector2I(8, 5))
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
                    new EnemySpawn("grunt", new Vector2I(9, 3)),
                    new EnemySpawn("gunner", new Vector2I(10, 5)),
                    new EnemySpawn("grunt", new Vector2I(8, 7))
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
                    new EnemySpawn("grunt", new Vector2I(10, 3)),
                    new EnemySpawn("gunner", new Vector2I(11, 5)),
                    new EnemySpawn("grunt", new Vector2I(9, 7)),
                    new EnemySpawn("heavy", new Vector2I(12, 6))
                };
                break;
        }

        return config;
    }
}
