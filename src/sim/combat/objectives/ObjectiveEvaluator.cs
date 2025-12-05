using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// Evaluates mission objectives and determines overall mission outcome.
/// </summary>
public class ObjectiveEvaluator
{
    private readonly List<IObjective> objectives = new();
    
    public IReadOnlyList<IObjective> Objectives => objectives;
    
    /// <summary>
    /// Add an objective to track.
    /// </summary>
    public void AddObjective(IObjective objective)
    {
        objectives.Add(objective);
    }
    
    /// <summary>
    /// Evaluate all objectives and return the mission outcome.
    /// Returns null if mission should continue.
    /// </summary>
    public MissionOutcome? Evaluate(CombatState state)
    {
        bool anyPrimaryFailed = false;
        bool allVictoryObjectivesCompleted = true;
        bool hasVictoryObjectives = false;
        
        foreach (var objective in objectives)
        {
            objective.Evaluate(state);
            
            if (objective.IsPrimary)
            {
                if (objective.Status == ObjectiveStatus.Failed)
                {
                    anyPrimaryFailed = true;
                }
                
                // Only victory objectives (non-failure conditions) count toward victory
                if (!objective.IsFailureCondition)
                {
                    hasVictoryObjectives = true;
                    if (objective.Status != ObjectiveStatus.Complete)
                    {
                        allVictoryObjectivesCompleted = false;
                    }
                }
            }
        }
        
        // Check failure first
        if (anyPrimaryFailed)
        {
            return MissionOutcome.Defeat;
        }
        
        // Check victory - only victory objectives (not failure conditions) determine this
        if (hasVictoryObjectives && allVictoryObjectivesCompleted)
        {
            return MissionOutcome.Victory;
        }
        
        // Mission continues
        return null;
    }
    
    /// <summary>
    /// Get all completed objectives.
    /// </summary>
    public List<IObjective> GetCompletedObjectives()
    {
        var result = new List<IObjective>();
        foreach (var obj in objectives)
        {
            if (obj.Status == ObjectiveStatus.Complete)
            {
                result.Add(obj);
            }
        }
        return result;
    }
    
    /// <summary>
    /// Get all failed objectives.
    /// </summary>
    public List<IObjective> GetFailedObjectives()
    {
        var result = new List<IObjective>();
        foreach (var obj in objectives)
        {
            if (obj.Status == ObjectiveStatus.Failed)
            {
                result.Add(obj);
            }
        }
        return result;
    }
    
    /// <summary>
    /// Create default objectives for a standard elimination mission.
    /// </summary>
    public static ObjectiveEvaluator CreateEliminationMission()
    {
        var evaluator = new ObjectiveEvaluator();
        evaluator.AddObjective(new SurviveObjective());
        evaluator.AddObjective(new EliminateAllObjective());
        return evaluator;
    }
    
    /// <summary>
    /// Create objectives for an extraction mission.
    /// </summary>
    public static ObjectiveEvaluator CreateExtractionMission(List<Godot.Vector2I> extractionZone)
    {
        var evaluator = new ObjectiveEvaluator();
        evaluator.AddObjective(new SurviveObjective());
        evaluator.AddObjective(new ReachZoneObjective("extract", extractionZone));
        return evaluator;
    }
}
