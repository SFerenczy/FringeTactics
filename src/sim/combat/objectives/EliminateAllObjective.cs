namespace FringeTactics;

/// <summary>
/// Objective: Kill all enemies on the map.
/// </summary>
public class EliminateAllObjective : ObjectiveBase
{
    public EliminateAllObjective(string id = "eliminate_all", bool isPrimary = true) 
        : base(id, "Eliminate all enemies", isPrimary)
    {
    }
    
    public override ObjectiveStatus Evaluate(CombatState state)
    {
        if (Status == ObjectiveStatus.Complete || Status == ObjectiveStatus.Failed) return Status;
        
        int aliveEnemies = 0;
        foreach (var actor in state.Actors)
        {
            if (actor.Type == ActorType.Enemy && actor.State == ActorState.Alive)
            {
                aliveEnemies++;
            }
        }
        
        if (aliveEnemies == 0)
        {
            Status = ObjectiveStatus.Complete;
        }
        
        return Status;
    }
    
    public override string GetProgressText(CombatState state)
    {
        int aliveEnemies = 0;
        int totalEnemies = 0;
        
        foreach (var actor in state.Actors)
        {
            if (actor.Type == ActorType.Enemy)
            {
                totalEnemies++;
                if (actor.State == ActorState.Alive)
                {
                    aliveEnemies++;
                }
            }
        }
        
        int killed = totalEnemies - aliveEnemies;
        return $"{killed}/{totalEnemies} enemies eliminated";
    }
}
