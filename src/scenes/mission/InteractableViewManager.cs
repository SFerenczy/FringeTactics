using Godot;
using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// Manages spawning, tracking, and updating of InteractableView instances.
/// Subscribes to InteractionSystem events for dynamic updates.
/// </summary>
public partial class InteractableViewManager : Node2D
{
    private Dictionary<int, InteractableView> interactableViews = new();
    private CombatState combatState;

    public void Initialize(CombatState combatState)
    {
        this.combatState = combatState;
        
        Name = "Interactables";
        ZIndex = 2;
        
        combatState.Interactions.InteractableAdded += OnInteractableAdded;
        combatState.Interactions.InteractableRemoved += OnInteractableRemoved;
        combatState.Interactions.InteractableStateChanged += OnInteractableStateChanged;
        
        foreach (var interactable in combatState.Interactions.GetAllInteractables())
        {
            CreateInteractableView(interactable);
        }
        
        GD.Print($"[InteractableViewManager] Created {interactableViews.Count} interactable views");
    }

    private void OnInteractableAdded(Interactable interactable)
    {
        CreateInteractableView(interactable);
    }

    private void OnInteractableRemoved(Interactable interactable)
    {
        if (interactableViews.TryGetValue(interactable.Id, out var view))
        {
            view.Cleanup();
            view.QueueFree();
            interactableViews.Remove(interactable.Id);
        }
    }

    private void OnInteractableStateChanged(Interactable interactable, InteractableState newState)
    {
        if (interactable.IsDoor)
        {
            combatState.Visibility.UpdateVisibility(combatState.Actors);
        }
    }


    private void CreateInteractableView(Interactable interactable)
    {
        var view = new InteractableView();
        view.Name = $"Interactable_{interactable.Id}";
        AddChild(view);
        view.Setup(interactable);
        interactableViews[interactable.Id] = view;
    }

    public void UpdateChannelProgress()
    {
        var channelingTargets = new HashSet<int>();
        
        foreach (var actor in combatState.Actors)
        {
            if (actor.IsChanneling && actor.CurrentChannel != null)
            {
                var targetId = actor.CurrentChannel.TargetInteractableId;
                channelingTargets.Add(targetId);
                
                if (interactableViews.TryGetValue(targetId, out var view))
                {
                    view.ShowChannelProgress(actor.CurrentChannel.Progress);
                }
            }
        }
        
        foreach (var kvp in interactableViews)
        {
            if (!channelingTargets.Contains(kvp.Key))
            {
                kvp.Value.HideChannelProgress();
            }
        }
    }

    public void Cleanup()
    {
        if (combatState != null)
        {
            combatState.Interactions.InteractableAdded -= OnInteractableAdded;
            combatState.Interactions.InteractableRemoved -= OnInteractableRemoved;
            combatState.Interactions.InteractableStateChanged -= OnInteractableStateChanged;
        }
        
        foreach (var view in interactableViews.Values)
        {
            view.Cleanup();
        }
        interactableViews.Clear();
    }
}
