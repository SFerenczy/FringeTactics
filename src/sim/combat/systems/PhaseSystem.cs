using System;

namespace FringeTactics;

/// <summary>
/// Mission phases for tracking tactical session progression.
/// </summary>
public enum TacticalPhase
{
    Setup,       // Pre-combat deployment
    Negotiation, // Non-combat narrative (optional)
    Contact,     // Combat begins, initial enemies
    Pressure,    // Waves arrive
    Resolution,  // Push to win or retreat
    Complete     // Mission ended
}

/// <summary>
/// Manages mission phase transitions.
/// </summary>
public class PhaseSystem
{
    private readonly CombatState combatState;
    
    public TacticalPhase CurrentPhase { get; private set; } = TacticalPhase.Setup;
    public int PhaseStartTick { get; private set; } = 0;
    public int TicksInPhase => combatState.TimeSystem.CurrentTick - PhaseStartTick;
    
    public event Action<TacticalPhase, TacticalPhase> PhaseChanged; // old, new
    
    public PhaseSystem(CombatState combatState)
    {
        this.combatState = combatState;
    }
    
    public void TransitionTo(TacticalPhase newPhase)
    {
        if (CurrentPhase == newPhase) return;
        
        var oldPhase = CurrentPhase;
        CurrentPhase = newPhase;
        PhaseStartTick = combatState.TimeSystem.CurrentTick;
        
        SimLog.Log($"[Phase] Transition: {oldPhase} → {newPhase}");
        PhaseChanged?.Invoke(oldPhase, newPhase);
    }
    
    /// <summary>
    /// Start the mission (transition from Setup).
    /// </summary>
    public void StartMission(bool hasNegotiation = false)
    {
        if (CurrentPhase != TacticalPhase.Setup) return;
        
        if (hasNegotiation)
        {
            TransitionTo(TacticalPhase.Negotiation);
        }
        else
        {
            TransitionTo(TacticalPhase.Contact);
        }
    }
    
    /// <summary>
    /// End negotiation and start combat.
    /// </summary>
    public void EndNegotiation()
    {
        if (CurrentPhase != TacticalPhase.Negotiation) return;
        TransitionTo(TacticalPhase.Contact);
    }
    
    /// <summary>
    /// Escalate to pressure phase.
    /// </summary>
    public void Escalate()
    {
        if (CurrentPhase != TacticalPhase.Contact) return;
        TransitionTo(TacticalPhase.Pressure);
    }
    
    /// <summary>
    /// Enter resolution phase.
    /// </summary>
    public void EnterResolution()
    {
        if (CurrentPhase != TacticalPhase.Pressure) return;
        TransitionTo(TacticalPhase.Resolution);
    }
    
    /// <summary>
    /// Complete the mission.
    /// </summary>
    public void Complete()
    {
        TransitionTo(TacticalPhase.Complete);
    }
    
    /// <summary>
    /// Check if combat is active (Contact, Pressure, or Resolution phases).
    /// </summary>
    public bool IsCombatActive => CurrentPhase == TacticalPhase.Contact 
                                || CurrentPhase == TacticalPhase.Pressure 
                                || CurrentPhase == TacticalPhase.Resolution;
}
