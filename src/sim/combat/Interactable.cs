using Godot;
using System;
using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// States for interactable objects.
/// </summary>
public enum InteractableState
{
    // Generic
    Idle,
    
    // Door states
    DoorClosed,
    DoorOpen,
    DoorLocked,
    
    // Terminal states
    TerminalIdle,
    TerminalHacking,
    TerminalHacked,
    TerminalDisabled,
    
    // Hazard states
    HazardArmed,
    HazardTriggered,
    HazardDisabled
}

/// <summary>
/// Types of interactable objects.
/// </summary>
public static class InteractableTypes
{
    public const string Door = "door";
    public const string Terminal = "terminal";
    public const string Hazard = "hazard";
}

/// <summary>
/// Represents an interactable object in the tactical map.
/// </summary>
public class Interactable
{
    public int Id { get; set; }
    public string Type { get; set; }
    public Vector2I Position { get; set; }
    public InteractableState State { get; private set; }
    
    /// <summary>
    /// Type-specific properties.
    /// Door: "locked" (bool), "hackDifficulty" (int ticks)
    /// Terminal: "hackDifficulty" (int ticks), "objectiveId" (string)
    /// Hazard: "hazardType" (string), "damage" (int), "radius" (int), "disableDifficulty" (int)
    /// </summary>
    public Dictionary<string, object> Properties { get; set; } = new();
    
    public event Action<Interactable, InteractableState> StateChanged;
    
    public Interactable(int id, string type, Vector2I position)
    {
        Id = id;
        Type = type;
        Position = position;
        State = GetDefaultState(type);
    }
    
    private static InteractableState GetDefaultState(string type)
    {
        return type switch
        {
            InteractableTypes.Door => InteractableState.DoorClosed,
            InteractableTypes.Terminal => InteractableState.TerminalIdle,
            InteractableTypes.Hazard => InteractableState.HazardArmed,
            _ => InteractableState.Idle
        };
    }
    
    public void SetState(InteractableState newState)
    {
        if (State == newState)
        {
            return;
        }
        
        var oldState = State;
        State = newState;
        StateChanged?.Invoke(this, newState);
        SimLog.Log($"[Interactable] {Type}#{Id} state: {oldState} -> {newState}");
    }
    
    // === Property Helpers ===
    
    public T GetProperty<T>(string key, T defaultValue = default)
    {
        if (Properties.TryGetValue(key, out var value))
        {
            if (value is T typedValue)
            {
                return typedValue;
            }
            
            // Handle numeric conversions (JSON may deserialize as different numeric types)
            if (typeof(T) == typeof(int) && value is IConvertible)
            {
                return (T)(object)Convert.ToInt32(value);
            }
        }
        return defaultValue;
    }
    
    public void SetProperty<T>(string key, T value)
    {
        Properties[key] = value;
    }
    
    // === Door Helpers ===
    
    public bool IsDoor => Type == InteractableTypes.Door;
    public bool IsDoorOpen => State == InteractableState.DoorOpen;
    public bool IsDoorLocked => State == InteractableState.DoorLocked;
    public bool IsDoorClosed => State == InteractableState.DoorClosed;
    
    public bool BlocksMovement()
    {
        if (!IsDoor)
        {
            return false;
        }
        return State == InteractableState.DoorClosed || State == InteractableState.DoorLocked;
    }
    
    public bool BlocksLOS()
    {
        if (!IsDoor)
        {
            return false;
        }
        return State == InteractableState.DoorClosed || State == InteractableState.DoorLocked;
    }
    
    // === Terminal Helpers ===
    
    public bool IsTerminal => Type == InteractableTypes.Terminal;
    public int HackDifficulty => GetProperty("hackDifficulty", 60); // Default 3 seconds at 20 ticks/sec
    public string ObjectiveId => GetProperty<string>("objectiveId", null);
    
    // === Hazard Helpers ===
    
    public bool IsHazard => Type == InteractableTypes.Hazard;
    public string HazardType => GetProperty("hazardType", "explosive");
    public int HazardDamage => GetProperty("damage", 50);
    public int HazardRadius => GetProperty("radius", 2);
    public int DisableDifficulty => GetProperty("disableDifficulty", 30);
}
