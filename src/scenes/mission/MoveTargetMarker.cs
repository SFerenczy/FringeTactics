using Godot;
using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// Displays movement target markers for selected actors.
/// Shows where units are moving to.
/// </summary>
public partial class MoveTargetMarker : Node2D
{
    private Node2D markerNode;
    private ColorRect markerFill;
    private ColorRect markerBorder;
    private Dictionary<int, Vector2I> actorMoveTargets = new();
    private CombatState combatState;
    private int tileSize;

    public void Initialize(CombatState combatState, int tileSize)
    {
        this.combatState = combatState;
        this.tileSize = tileSize;
        
        CreateMarkerVisuals();
    }

    private void CreateMarkerVisuals()
    {
        markerNode = new Node2D();
        markerNode.Visible = false;
        markerNode.ZIndex = 1;
        AddChild(markerNode);
        
        markerBorder = new ColorRect();
        markerBorder.Size = new Vector2(tileSize - 2, tileSize - 2);
        markerBorder.Position = Vector2.Zero;
        markerBorder.Color = new Color(0.2f, 0.9f, 0.2f, 0.8f);
        markerNode.AddChild(markerBorder);
        
        markerFill = new ColorRect();
        markerFill.Size = new Vector2(tileSize - 8, tileSize - 8);
        markerFill.Position = new Vector2(3, 3);
        markerFill.Color = new Color(0.3f, 0.8f, 0.3f, 0.4f);
        markerNode.AddChild(markerFill);
    }

    public void SetTarget(int actorId, Vector2I targetPos)
    {
        var actor = combatState.GetActorById(actorId);
        if (actor != null && actor.IsMoving)
        {
            actorMoveTargets[actorId] = targetPos;
        }
    }

    public void ClearTarget(int actorId)
    {
        actorMoveTargets.Remove(actorId);
    }

    public void Update(IReadOnlyList<int> selectedActorIds)
    {
        CleanupCompletedTargets();
        UpdateMarkerVisibility(selectedActorIds);
    }

    private void CleanupCompletedTargets()
    {
        var toRemove = new List<int>();
        foreach (var kvp in actorMoveTargets)
        {
            var actor = combatState.GetActorById(kvp.Key);
            if (actor == null || !actor.IsMoving)
            {
                toRemove.Add(kvp.Key);
            }
        }
        foreach (var id in toRemove)
        {
            actorMoveTargets.Remove(id);
        }
    }

    private void UpdateMarkerVisibility(IReadOnlyList<int> selectedActorIds)
    {
        if (selectedActorIds == null)
        {
            Hide();
            return;
        }

        foreach (var actorId in selectedActorIds)
        {
            if (actorMoveTargets.TryGetValue(actorId, out var target))
            {
                var actor = combatState.GetActorById(actorId);
                if (actor != null && actor.IsMoving)
                {
                    ShowAt(target);
                    return;
                }
            }
        }

        Hide();
    }

    private void ShowAt(Vector2I gridPos)
    {
        markerNode.Position = new Vector2(gridPos.X * tileSize + 1, gridPos.Y * tileSize + 1);
        markerNode.Visible = true;
    }

    private new void Hide()
    {
        markerNode.Visible = false;
    }
}
