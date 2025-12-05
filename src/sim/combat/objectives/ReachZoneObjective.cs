using Godot;
using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// Objective: Get crew to a specific zone.
/// Used for extraction, reaching objectives, etc.
/// </summary>
public class ReachZoneObjective : ObjectiveBase
{
    private readonly List<Vector2I> zoneTiles;
    private readonly int requiredCount;
    
    /// <summary>
    /// Create a reach zone objective.
    /// </summary>
    /// <param name="id">Objective ID</param>
    /// <param name="zoneTiles">Tiles that count as the zone</param>
    /// <param name="requiredCount">How many crew must reach the zone (0 = all alive)</param>
    /// <param name="isPrimary">Whether this is a primary objective</param>
    public ReachZoneObjective(
        string id, 
        List<Vector2I> zoneTiles, 
        int requiredCount = 0,
        bool isPrimary = true) 
        : base(id, "Reach extraction zone", isPrimary)
    {
        this.zoneTiles = zoneTiles;
        this.requiredCount = requiredCount;
    }
    
    public override ObjectiveStatus Evaluate(CombatState state)
    {
        if (Status == ObjectiveStatus.Complete || Status == ObjectiveStatus.Failed) return Status;
        
        int inZone = 0;
        int aliveCrew = 0;
        
        foreach (var actor in state.Actors)
        {
            if (actor.Type != ActorType.Crew || actor.State != ActorState.Alive)
            {
                continue;
            }
            
            aliveCrew++;
            if (IsInZone(actor.GridPosition))
            {
                inZone++;
            }
        }
        
        int required = requiredCount > 0 ? requiredCount : aliveCrew;
        
        if (inZone >= required && aliveCrew > 0)
        {
            Status = ObjectiveStatus.Complete;
        }
        
        return Status;
    }
    
    private bool IsInZone(Vector2I pos)
    {
        foreach (var tile in zoneTiles)
        {
            if (tile == pos) return true;
        }
        return false;
    }
    
    public override string GetProgressText(CombatState state)
    {
        int inZone = 0;
        int aliveCrew = 0;
        
        foreach (var actor in state.Actors)
        {
            if (actor.Type != ActorType.Crew || actor.State != ActorState.Alive)
            {
                continue;
            }
            
            aliveCrew++;
            if (IsInZone(actor.GridPosition))
            {
                inZone++;
            }
        }
        
        int required = requiredCount > 0 ? requiredCount : aliveCrew;
        return $"{inZone}/{required} crew in zone";
    }
}
