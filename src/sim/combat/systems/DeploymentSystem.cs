using Godot;
using System;
using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// Manages pre-combat unit deployment.
/// </summary>
public class DeploymentSystem
{
    private readonly CombatState combatState;
    private readonly HashSet<Vector2I> deploymentZone = new();
    
    public bool IsDeploying => combatState.Phases.CurrentPhase == TacticalPhase.Setup;
    public IReadOnlyCollection<Vector2I> DeploymentZone => deploymentZone;
    
    public event Action<Actor, Vector2I> UnitDeployed;
    public event Action DeploymentConfirmed;
    
    public DeploymentSystem(CombatState combatState)
    {
        this.combatState = combatState;
    }
    
    public void SetDeploymentZone(IEnumerable<Vector2I> tiles)
    {
        deploymentZone.Clear();
        foreach (var tile in tiles)
        {
            deploymentZone.Add(tile);
        }
        SimLog.Log($"[Deployment] Zone set with {deploymentZone.Count} tiles");
    }
    
    public bool IsInDeploymentZone(Vector2I position)
    {
        return deploymentZone.Contains(position);
    }
    
    public bool CanDeployAt(Vector2I position)
    {
        if (!IsDeploying) return false;
        if (!IsInDeploymentZone(position)) return false;
        if (!combatState.MapState.IsWalkable(position)) return false;
        if (combatState.GetActorAtPosition(position) != null) return false;
        return true;
    }
    
    public bool DeployUnit(Actor actor, Vector2I position)
    {
        if (!CanDeployAt(position)) return false;
        
        actor.GridPosition = position;
        actor.VisualPosition = new Vector2(
            position.X * GridConstants.TileSize,
            position.Y * GridConstants.TileSize);
        actor.SetTarget(position);
        
        UnitDeployed?.Invoke(actor, position);
        SimLog.Log($"[Deployment] {actor.Type}#{actor.Id} deployed to {position}");
        return true;
    }
    
    /// <summary>
    /// Swap positions of two actors during deployment.
    /// </summary>
    public bool SwapUnits(Actor actor1, Actor actor2)
    {
        if (!IsDeploying) return false;
        if (actor1 == null || actor2 == null) return false;
        
        var pos1 = actor1.GridPosition;
        var pos2 = actor2.GridPosition;
        
        actor1.GridPosition = pos2;
        actor1.VisualPosition = new Vector2(pos2.X * GridConstants.TileSize, pos2.Y * GridConstants.TileSize);
        actor1.SetTarget(pos2);
        
        actor2.GridPosition = pos1;
        actor2.VisualPosition = new Vector2(pos1.X * GridConstants.TileSize, pos1.Y * GridConstants.TileSize);
        actor2.SetTarget(pos1);
        
        SimLog.Log($"[Deployment] Swapped {actor1.Type}#{actor1.Id} and {actor2.Type}#{actor2.Id}");
        return true;
    }
    
    public void ConfirmDeployment()
    {
        if (!IsDeploying) return;
        
        DeploymentConfirmed?.Invoke();
        combatState.Phases.StartMission(combatState.MissionConfig?.HasNegotiationPhase ?? false);
    }
    
    /// <summary>
    /// Check if all crew are deployed within the deployment zone.
    /// </summary>
    public bool AllCrewDeployed()
    {
        foreach (var actor in combatState.Actors)
        {
            if (actor.Type != ActorType.Crew) continue;
            if (!IsInDeploymentZone(actor.GridPosition)) return false;
        }
        return true;
    }
}
