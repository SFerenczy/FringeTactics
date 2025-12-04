using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FringeTactics;

/// <summary>
/// Manages interactable objects and processes interactions.
/// </summary>
public class InteractionSystem
{
    private readonly CombatState combatState;
    private readonly Dictionary<int, Interactable> interactables = new();
    private int nextInteractableId = 0;
    
    // Events
    public event Action<Interactable> InteractableAdded;
    public event Action<Interactable> InteractableRemoved;
    public event Action<Interactable, InteractableState> InteractableStateChanged;
    public event Action<Actor, Interactable> InteractionStarted;
    public event Action<Actor, Interactable> InteractionCompleted;
    public event Action<Vector2I, int> HazardTriggered; // position, damage
    
    public InteractionSystem(CombatState combatState)
    {
        this.combatState = combatState;
    }
    
    /// <summary>
    /// Process one tick of the interaction system.
    /// </summary>
    public void Tick()
    {
        // Actor channeling is handled by Actor.Tick()
        // We listen for completion via events subscribed in ExecuteInteraction
    }
    
    /// <summary>
    /// Add an interactable to the system.
    /// </summary>
    public Interactable AddInteractable(string type, Vector2I position, Dictionary<string, object> properties = null)
    {
        var interactable = new Interactable(nextInteractableId++, type, position);
        
        if (properties != null)
        {
            foreach (var kvp in properties)
            {
                interactable.Properties[kvp.Key] = kvp.Value;
            }
        }
        
        interactable.StateChanged += OnInteractableStateChanged;
        interactables[interactable.Id] = interactable;
        InteractableAdded?.Invoke(interactable);
        
        SimLog.Log($"[Interaction] Added {type}#{interactable.Id} at {position}");
        return interactable;
    }
    
    /// <summary>
    /// Remove an interactable from the system.
    /// </summary>
    public void RemoveInteractable(int id)
    {
        if (interactables.TryGetValue(id, out var interactable))
        {
            interactable.StateChanged -= OnInteractableStateChanged;
            interactables.Remove(id);
            InteractableRemoved?.Invoke(interactable);
            SimLog.Log($"[Interaction] Removed {interactable.Type}#{id}");
        }
    }
    
    /// <summary>
    /// Get an interactable by ID.
    /// </summary>
    public Interactable GetInteractable(int id)
    {
        return interactables.TryGetValue(id, out var interactable) ? interactable : null;
    }
    
    /// <summary>
    /// Get an interactable at a position.
    /// </summary>
    public Interactable GetInteractableAt(Vector2I position)
    {
        return interactables.Values.FirstOrDefault(i => i.Position == position);
    }
    
    /// <summary>
    /// Get all interactables.
    /// </summary>
    public IEnumerable<Interactable> GetAllInteractables()
    {
        return interactables.Values;
    }
    
    /// <summary>
    /// Get all doors.
    /// </summary>
    public IEnumerable<Interactable> GetDoors()
    {
        return interactables.Values.Where(i => i.IsDoor);
    }
    
    /// <summary>
    /// Check if an actor can interact with an interactable.
    /// </summary>
    public bool CanInteract(Actor actor, Interactable interactable)
    {
        if (actor == null || actor.State != ActorState.Alive)
        {
            return false;
        }
        
        if (interactable == null)
        {
            return false;
        }
        
        // Must be adjacent (including diagonals)
        var distance = CombatResolver.GetDistance(actor.GridPosition, interactable.Position);
        if (distance > 1.5f)
        {
            return false;
        }
        
        return interactable.Type switch
        {
            InteractableTypes.Door => CanInteractWithDoor(interactable),
            InteractableTypes.Terminal => CanInteractWithTerminal(interactable),
            InteractableTypes.Hazard => CanInteractWithHazard(interactable),
            _ => false
        };
    }
    
    private bool CanInteractWithDoor(Interactable door)
    {
        return door.State == InteractableState.DoorClosed 
            || door.State == InteractableState.DoorOpen 
            || door.State == InteractableState.DoorLocked;
    }
    
    private bool CanInteractWithTerminal(Interactable terminal)
    {
        return terminal.State == InteractableState.TerminalIdle;
    }
    
    private bool CanInteractWithHazard(Interactable hazard)
    {
        return hazard.State == InteractableState.HazardArmed;
    }
    
    /// <summary>
    /// Get available interactions for an actor with an interactable.
    /// </summary>
    public List<string> GetAvailableInteractions(Actor actor, Interactable interactable)
    {
        var interactions = new List<string>();
        
        if (!CanInteract(actor, interactable))
        {
            return interactions;
        }
        
        switch (interactable.Type)
        {
            case InteractableTypes.Door:
                if (interactable.State == InteractableState.DoorClosed)
                {
                    interactions.Add("open");
                }
                else if (interactable.State == InteractableState.DoorOpen)
                {
                    interactions.Add("close");
                }
                else if (interactable.State == InteractableState.DoorLocked)
                {
                    interactions.Add("hack");
                }
                break;
                
            case InteractableTypes.Terminal:
                if (interactable.State == InteractableState.TerminalIdle)
                {
                    interactions.Add("hack");
                }
                break;
                
            case InteractableTypes.Hazard:
                if (interactable.State == InteractableState.HazardArmed)
                {
                    interactions.Add("trigger");
                    interactions.Add("disable");
                }
                break;
        }
        
        return interactions;
    }
    
    /// <summary>
    /// Execute an interaction.
    /// </summary>
    public bool ExecuteInteraction(Actor actor, Interactable interactable, string action)
    {
        if (!CanInteract(actor, interactable))
        {
            return false;
        }
        
        var available = GetAvailableInteractions(actor, interactable);
        if (!available.Contains(action))
        {
            return false;
        }
        
        SimLog.Log($"[Interaction] {actor.Type}#{actor.Id} -> {action} on {interactable.Type}#{interactable.Id}");
        InteractionStarted?.Invoke(actor, interactable);
        
        return interactable.Type switch
        {
            InteractableTypes.Door => ExecuteDoorInteraction(actor, interactable, action),
            InteractableTypes.Terminal => ExecuteTerminalInteraction(actor, interactable, action),
            InteractableTypes.Hazard => ExecuteHazardInteraction(actor, interactable, action),
            _ => false
        };
    }
    
    private bool ExecuteDoorInteraction(Actor actor, Interactable door, string action)
    {
        switch (action)
        {
            case "open":
                if (door.State == InteractableState.DoorClosed)
                {
                    door.SetState(InteractableState.DoorOpen);
                    InteractionCompleted?.Invoke(actor, door);
                    return true;
                }
                break;
                
            case "close":
                if (door.State == InteractableState.DoorOpen)
                {
                    door.SetState(InteractableState.DoorClosed);
                    InteractionCompleted?.Invoke(actor, door);
                    return true;
                }
                break;
                
            case "hack":
                if (door.State == InteractableState.DoorLocked)
                {
                    var hackDuration = door.GetProperty("hackDifficulty", 40);
                    var channel = new ChanneledAction(ChannelTypes.Unlock, door.Id, hackDuration);
                    
                    if (actor.StartChannel(channel))
                    {
                        actor.ChannelCompleted += OnDoorHackCompleted;
                        actor.ChannelInterrupted += OnDoorHackInterrupted;
                        return true;
                    }
                }
                break;
        }
        
        return false;
    }
    
    private void OnDoorHackCompleted(Actor actor, ChanneledAction channel)
    {
        actor.ChannelCompleted -= OnDoorHackCompleted;
        actor.ChannelInterrupted -= OnDoorHackInterrupted;
        
        var door = GetInteractable(channel.TargetInteractableId);
        if (door != null && door.State == InteractableState.DoorLocked)
        {
            door.SetState(InteractableState.DoorOpen);
            InteractionCompleted?.Invoke(actor, door);
        }
    }
    
    private void OnDoorHackInterrupted(Actor actor, ChanneledAction channel)
    {
        actor.ChannelCompleted -= OnDoorHackCompleted;
        actor.ChannelInterrupted -= OnDoorHackInterrupted;
        SimLog.Log($"[Interaction] Door hack interrupted");
    }
    
    private bool ExecuteTerminalInteraction(Actor actor, Interactable terminal, string action)
    {
        if (action != "hack" || terminal.State != InteractableState.TerminalIdle)
        {
            return false;
        }
        
        var hackDuration = terminal.HackDifficulty;
        var channel = new ChanneledAction(ChannelTypes.Hack, terminal.Id, hackDuration);
        
        if (actor.StartChannel(channel))
        {
            terminal.SetState(InteractableState.TerminalHacking);
            actor.ChannelCompleted += OnTerminalHackCompleted;
            actor.ChannelInterrupted += OnTerminalHackInterrupted;
            return true;
        }
        
        return false;
    }
    
    private void OnTerminalHackCompleted(Actor actor, ChanneledAction channel)
    {
        actor.ChannelCompleted -= OnTerminalHackCompleted;
        actor.ChannelInterrupted -= OnTerminalHackInterrupted;
        
        var terminal = GetInteractable(channel.TargetInteractableId);
        if (terminal == null)
        {
            return;
        }
        
        terminal.SetState(InteractableState.TerminalHacked);
        InteractionCompleted?.Invoke(actor, terminal);
        
        var objectiveId = terminal.ObjectiveId;
        if (!string.IsNullOrEmpty(objectiveId))
        {
            combatState.Objectives[objectiveId] = true;
            SimLog.Log($"[Interaction] Objective '{objectiveId}' completed via terminal hack");
        }
    }
    
    private void OnTerminalHackInterrupted(Actor actor, ChanneledAction channel)
    {
        actor.ChannelCompleted -= OnTerminalHackCompleted;
        actor.ChannelInterrupted -= OnTerminalHackInterrupted;
        
        var terminal = GetInteractable(channel.TargetInteractableId);
        if (terminal != null && terminal.State == InteractableState.TerminalHacking)
        {
            terminal.SetState(InteractableState.TerminalIdle);
        }
        SimLog.Log($"[Interaction] Terminal hack interrupted");
    }
    
    private bool ExecuteHazardInteraction(Actor actor, Interactable hazard, string action)
    {
        if (hazard.State != InteractableState.HazardArmed)
        {
            return false;
        }
        
        switch (action)
        {
            case "trigger":
                TriggerHazard(hazard);
                InteractionCompleted?.Invoke(actor, hazard);
                return true;
                
            case "disable":
                var disableDuration = hazard.DisableDifficulty;
                var channel = new ChanneledAction(ChannelTypes.DisableHazard, hazard.Id, disableDuration);
                
                if (actor.StartChannel(channel))
                {
                    actor.ChannelCompleted += OnHazardDisableCompleted;
                    actor.ChannelInterrupted += OnHazardDisableInterrupted;
                    return true;
                }
                break;
        }
        
        return false;
    }
    
    private void OnHazardDisableCompleted(Actor actor, ChanneledAction channel)
    {
        actor.ChannelCompleted -= OnHazardDisableCompleted;
        actor.ChannelInterrupted -= OnHazardDisableInterrupted;
        
        var hazard = GetInteractable(channel.TargetInteractableId);
        if (hazard != null && hazard.State == InteractableState.HazardArmed)
        {
            hazard.SetState(InteractableState.HazardDisabled);
            InteractionCompleted?.Invoke(actor, hazard);
        }
    }
    
    private void OnHazardDisableInterrupted(Actor actor, ChanneledAction channel)
    {
        actor.ChannelCompleted -= OnHazardDisableCompleted;
        actor.ChannelInterrupted -= OnHazardDisableInterrupted;
        SimLog.Log($"[Interaction] Hazard disable interrupted");
    }
    
    /// <summary>
    /// Trigger a hazard's effect.
    /// </summary>
    public void TriggerHazard(Interactable hazard)
    {
        if (hazard == null || !hazard.IsHazard || hazard.State != InteractableState.HazardArmed)
        {
            return;
        }
        
        hazard.SetState(InteractableState.HazardTriggered);
        
        var damage = hazard.HazardDamage;
        var radius = hazard.HazardRadius;
        var position = hazard.Position;
        
        SimLog.Log($"[Interaction] Hazard triggered at {position}, {damage} damage, radius {radius}");
        HazardTriggered?.Invoke(position, damage);
        
        foreach (var actor in combatState.Actors)
        {
            if (actor.State != ActorState.Alive)
            {
                continue;
            }
            
            var distance = CombatResolver.GetDistance(position, actor.GridPosition);
            if (distance <= radius)
            {
                actor.TakeDamage(damage);
                SimLog.Log($"[Interaction] Hazard dealt {damage} damage to {actor.Type}#{actor.Id}");
                
                if (actor.State == ActorState.Dead)
                {
                    combatState.NotifyActorDied(actor);
                }
            }
        }
    }
    
    private void OnInteractableStateChanged(Interactable interactable, InteractableState newState)
    {
        InteractableStateChanged?.Invoke(interactable, newState);
    }
    
    /// <summary>
    /// Check if a position is blocked by a closed/locked door.
    /// </summary>
    public bool IsDoorBlocking(Vector2I position)
    {
        var interactable = GetInteractableAt(position);
        return interactable != null && interactable.IsDoor && interactable.BlocksMovement();
    }
    
    /// <summary>
    /// Check if a position has a door that blocks LOS.
    /// </summary>
    public bool IsDoorBlockingLOS(Vector2I position)
    {
        var interactable = GetInteractableAt(position);
        return interactable != null && interactable.IsDoor && interactable.BlocksLOS();
    }
}
