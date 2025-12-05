namespace FringeTactics;

/// <summary>
/// Objective: Keep at least one crew member alive.
/// This is typically an implicit failure condition.
/// </summary>
public class SurviveObjective : ObjectiveBase
{
    public override bool IsFailureCondition => true;
    
    public SurviveObjective(string id = "survive", bool isPrimary = true) 
        : base(id, "Keep crew alive", isPrimary)
    {
    }
    
    public override ObjectiveStatus Evaluate(CombatState state)
    {
        if (Status == ObjectiveStatus.Failed) return Status;
        
        int aliveCrew = 0;
        foreach (var actor in state.Actors)
        {
            if (actor.Type == ActorType.Crew && actor.State == ActorState.Alive)
            {
                aliveCrew++;
            }
        }
        
        if (aliveCrew == 0)
        {
            Status = ObjectiveStatus.Failed;
        }
        // Survive stays InProgress while crew are alive - it's a failure condition, not a victory condition
        
        return Status;
    }
    
    public override string GetProgressText(CombatState state)
    {
        int aliveCrew = 0;
        int totalCrew = 0;
        
        foreach (var actor in state.Actors)
        {
            if (actor.Type == ActorType.Crew)
            {
                totalCrew++;
                if (actor.State == ActorState.Alive)
                {
                    aliveCrew++;
                }
            }
        }
        
        return $"{aliveCrew}/{totalCrew} crew alive";
    }
}
