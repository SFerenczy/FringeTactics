using System;
using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// Summary of a simulated battle.
/// </summary>
public class BattleSummary
{
    public int Seed { get; set; }
    public bool Victory { get; set; }
    public int TicksElapsed { get; set; }
    public int CrewAlive { get; set; }
    public int CrewDead { get; set; }
    public int EnemiesKilled { get; set; }
    public CombatStats Stats { get; set; }
}

/// <summary>
/// Aggregate results from running multiple battles.
/// </summary>
public class SimulationResults
{
    public int TotalBattles { get; set; }
    public int Victories { get; set; }
    public int Defeats { get; set; }
    public float WinRate => TotalBattles > 0 ? (float)Victories / TotalBattles : 0;
    public float AverageCrewSurvivors { get; set; }
    public float AverageTicksToComplete { get; set; }
    public float AveragePlayerAccuracy { get; set; }
    public List<BattleSummary> Battles { get; set; } = new();
}

/// <summary>
/// Headless combat simulator for testing and balancing.
/// Runs battles without any UI/Node dependencies.
/// </summary>
public static class CombatSimulator
{
    private const float TICK_DURATION = 1.0f / 20.0f; // 20 ticks per second
    private const int MAX_TICKS = 20 * 60 * 5; // 5 minutes max

    /// <summary>
    /// Run a single battle to completion and return summary.
    /// </summary>
    public static BattleSummary RunBattle(MissionConfig config, int seed)
    {
        var combat = MissionFactory.BuildSandbox(config, seed);

        // Start the battle
        combat.TimeSystem.Resume();

        int ticks = 0;
        while (!combat.IsComplete && ticks < MAX_TICKS)
        {
            // Simulate one tick
            combat.Update(TICK_DURATION);
            ticks++;
        }

        // Build summary
        var summary = new BattleSummary
        {
            Seed = seed,
            Victory = combat.Victory,
            TicksElapsed = ticks,
            Stats = combat.Stats
        };

        // Count survivors
        foreach (var actor in combat.Actors)
        {
            if (actor.Type == ActorType.Crew)
            {
                if (actor.State == ActorState.Alive)
                {
                    summary.CrewAlive++;
                }
                else
                {
                    summary.CrewDead++;
                }
            }
            else if (actor.Type == ActorType.Enemy && actor.State == ActorState.Dead)
            {
                summary.EnemiesKilled++;
            }
        }

        return summary;
    }

    /// <summary>
    /// Run a battle with campaign crew.
    /// </summary>
    public static BattleSummary RunBattleWithCampaign(CampaignState campaign, MissionConfig config, int seed)
    {
        var buildResult = MissionFactory.BuildFromCampaign(campaign, config, seed);
        var combat = buildResult.CombatState;

        // Start the battle
        combat.TimeSystem.Resume();

        int ticks = 0;
        while (!combat.IsComplete && ticks < MAX_TICKS)
        {
            combat.Update(TICK_DURATION);
            ticks++;
        }

        var summary = new BattleSummary
        {
            Seed = seed,
            Victory = combat.Victory,
            TicksElapsed = ticks,
            Stats = combat.Stats
        };

        foreach (var actor in combat.Actors)
        {
            if (actor.Type == ActorType.Crew)
            {
                if (actor.State == ActorState.Alive)
                {
                    summary.CrewAlive++;
                }
                else
                {
                    summary.CrewDead++;
                }
            }
            else if (actor.Type == ActorType.Enemy && actor.State == ActorState.Dead)
            {
                summary.EnemiesKilled++;
            }
        }

        return summary;
    }

    /// <summary>
    /// Run multiple battles and aggregate results.
    /// </summary>
    public static SimulationResults RunSimulation(MissionConfig config, int battleCount, int? baseSeed = null)
    {
        var results = new SimulationResults
        {
            TotalBattles = battleCount
        };

        int startSeed = baseSeed ?? Environment.TickCount;
        int totalCrewSurvivors = 0;
        int totalTicks = 0;
        float totalAccuracy = 0;

        for (int i = 0; i < battleCount; i++)
        {
            var seed = startSeed + i;
            var summary = RunBattle(config, seed);
            results.Battles.Add(summary);

            if (summary.Victory)
            {
                results.Victories++;
            }
            else
            {
                results.Defeats++;
            }

            totalCrewSurvivors += summary.CrewAlive;
            totalTicks += summary.TicksElapsed;
            totalAccuracy += summary.Stats.PlayerAccuracy;
        }

        results.AverageCrewSurvivors = (float)totalCrewSurvivors / battleCount;
        results.AverageTicksToComplete = (float)totalTicks / battleCount;
        results.AveragePlayerAccuracy = totalAccuracy / battleCount;

        return results;
    }

    /// <summary>
    /// Print simulation results to SimLog.
    /// </summary>
    public static void PrintResults(SimulationResults results)
    {
        SimLog.Log("=== SIMULATION RESULTS ===");
        SimLog.Log($"Battles: {results.TotalBattles}");
        SimLog.Log($"Wins: {results.Victories} ({results.WinRate * 100:F1}%)");
        SimLog.Log($"Losses: {results.Defeats}");
        SimLog.Log($"Avg Crew Survivors: {results.AverageCrewSurvivors:F2}");
        SimLog.Log($"Avg Ticks to Complete: {results.AverageTicksToComplete:F0} ({results.AverageTicksToComplete / 20:F1}s)");
        SimLog.Log($"Avg Player Accuracy: {results.AveragePlayerAccuracy:F1}%");
        SimLog.Log("==========================");
    }
}
