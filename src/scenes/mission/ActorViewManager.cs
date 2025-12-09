using Godot;
using System.Collections.Generic;
using System.Linq;

namespace FringeTactics;

/// <summary>
/// Manages spawning, tracking, and updating of ActorView instances.
/// Handles fog visibility for enemy actors and detection state updates.
/// </summary>
public partial class ActorViewManager : Node2D
{
    private const string ActorViewScenePath = "res://src/scenes/mission/ActorView.tscn";
    
    private PackedScene actorViewScene;
    private Dictionary<int, ActorView> actorViews = new();
    private Dictionary<int, OverwatchIndicator> overwatchIndicators = new();
    private List<int> crewActorIds = new();
    private CombatState combatState;

    private static readonly Color[] CrewColors = new Color[]
    {
        new Color(0.2f, 0.6f, 1.0f), // Blue
        new Color(0.2f, 0.8f, 0.3f), // Green
        new Color(0.4f, 0.7f, 0.9f), // Light Blue
        new Color(0.6f, 0.4f, 0.9f)  // Purple
    };
    private static readonly Color EnemyColor = new Color(0.9f, 0.2f, 0.2f);

    public IReadOnlyList<int> CrewActorIds => crewActorIds;
    public IReadOnlyDictionary<int, ActorView> ActorViews => actorViews;

    public void Initialize(CombatState combatState)
    {
        this.combatState = combatState;
        actorViewScene = GD.Load<PackedScene>(ActorViewScenePath);
        
        Name = "Actors";
        
        SpawnActorViews();
    }

    private void SpawnActorViews()
    {
        crewActorIds.Clear();
        int crewIndex = 0;

        foreach (var actor in combatState.Actors)
        {
            var view = actorViewScene.Instantiate<ActorView>();
            AddChild(view);
            actorViews[actor.Id] = view;
            
            // Create overwatch indicator for this actor
            var overwatchIndicator = new OverwatchIndicator();
            overwatchIndicator.ZIndex = -1; // Render behind actors
            AddChild(overwatchIndicator);
            overwatchIndicators[actor.Id] = overwatchIndicator;

            if (actor.Type == ActorType.Crew)
            {
                var color = CrewColors[crewIndex % CrewColors.Length];
                view.Setup(actor, color);
                overwatchIndicator.Setup(actor, isEnemy: false);
                crewActorIds.Add(actor.Id);
                crewIndex++;
            }
            else if (actor.Type == ActorType.Enemy)
            {
                view.Setup(actor, EnemyColor);
                overwatchIndicator.Setup(actor, isEnemy: true);
            }
            else
            {
                view.Setup(actor, Colors.White);
                overwatchIndicator.Setup(actor, isEnemy: false);
            }
        }

        GD.Print($"[ActorViewManager] Created views for {combatState.Actors.Count} actors ({crewActorIds.Count} crew)");
    }

    public ActorView GetView(int actorId)
    {
        return actorViews.GetValueOrDefault(actorId);
    }

    public void RemoveActor(Actor actor)
    {
        if (actorViews.TryGetValue(actor.Id, out var view))
        {
            view.QueueFree();
            actorViews.Remove(actor.Id);
        }
        
        if (overwatchIndicators.TryGetValue(actor.Id, out var indicator))
        {
            indicator.Cleanup();
            indicator.QueueFree();
            overwatchIndicators.Remove(actor.Id);
        }
    }

    public void UpdateFogVisibility()
    {
        foreach (var kvp in actorViews)
        {
            var actor = combatState.GetActorById(kvp.Key);
            var view = kvp.Value;

            if (actor == null)
            {
                continue;
            }

            if (actor.Type == ActorType.Crew)
            {
                view.Visible = true;
                continue;
            }

            var isVisible = combatState.Visibility.IsVisible(actor.GridPosition);
            view.Visible = isVisible;
        }
    }

    public void UpdateDetectionIndicators()
    {
        foreach (var kvp in actorViews)
        {
            var actorView = kvp.Value;
            var actor = actorView.GetActor();
            
            if (actor == null || actor.Type != ActorType.Enemy)
            {
                continue;
            }
            
            var detectionState = combatState.Perception.GetDetectionState(actor.Id);
            actorView.UpdateDetectionState(detectionState);
        }
    }

    public void SetActorSelected(int actorId, bool isSelected)
    {
        if (actorViews.TryGetValue(actorId, out var view))
        {
            view.SetSelected(isSelected);
        }
    }

    public void UpdateSelectionState(IReadOnlyList<int> selectedIds)
    {
        foreach (var kvp in actorViews)
        {
            var isSelected = selectedIds.Contains(kvp.Key);
            kvp.Value.SetSelected(isSelected);
        }
    }
}
