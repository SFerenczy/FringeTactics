using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// Result of travel execution.
/// </summary>
public class TravelResult
{
    /// <summary>
    /// Final status of the travel.
    /// </summary>
    public TravelResultStatus Status { get; set; }

    /// <summary>
    /// Final system ID (destination if complete, current if interrupted).
    /// </summary>
    public int FinalSystemId { get; set; }

    /// <summary>
    /// Total fuel consumed.
    /// </summary>
    public int FuelConsumed { get; set; }

    /// <summary>
    /// Total days elapsed.
    /// </summary>
    public int DaysElapsed { get; set; }

    /// <summary>
    /// Encounters that occurred.
    /// </summary>
    public List<TravelEncounterRecord> Encounters { get; set; } = new();

    /// <summary>
    /// Reason for interruption (if Status != Completed).
    /// </summary>
    public TravelInterruptReason InterruptReason { get; set; }

    /// <summary>
    /// Travel state for resumption (if paused for encounter).
    /// </summary>
    public TravelState PausedState { get; set; }

    /// <summary>
    /// Create a completed result.
    /// </summary>
    public static TravelResult Completed(int destinationId, int fuelConsumed, int daysElapsed, List<TravelEncounterRecord> encounters = null)
    {
        return new TravelResult
        {
            Status = TravelResultStatus.Completed,
            FinalSystemId = destinationId,
            FuelConsumed = fuelConsumed,
            DaysElapsed = daysElapsed,
            Encounters = encounters ?? new List<TravelEncounterRecord>()
        };
    }

    /// <summary>
    /// Create an interrupted result.
    /// </summary>
    public static TravelResult Interrupted(int currentSystemId, TravelInterruptReason reason, int fuelConsumed, int daysElapsed, TravelState state = null)
    {
        return new TravelResult
        {
            Status = TravelResultStatus.Interrupted,
            FinalSystemId = currentSystemId,
            InterruptReason = reason,
            FuelConsumed = fuelConsumed,
            DaysElapsed = daysElapsed,
            PausedState = state
        };
    }

    /// <summary>
    /// Create a paused result (for encounter).
    /// </summary>
    public static TravelResult Paused(TravelState state, int fuelConsumed, int daysElapsed)
    {
        return new TravelResult
        {
            Status = TravelResultStatus.PausedForEncounter,
            FinalSystemId = state.CurrentSystemId,
            FuelConsumed = fuelConsumed,
            DaysElapsed = daysElapsed,
            PausedState = state,
            Encounters = new List<TravelEncounterRecord>(state.EncounterHistory)
        };
    }
}

/// <summary>
/// Status of travel execution.
/// </summary>
public enum TravelResultStatus
{
    /// <summary>
    /// Arrived at destination.
    /// </summary>
    Completed,

    /// <summary>
    /// Stopped mid-travel (out of fuel, player abort, etc.).
    /// </summary>
    Interrupted,

    /// <summary>
    /// Paused for encounter resolution.
    /// </summary>
    PausedForEncounter,

    /// <summary>
    /// Cancelled before starting.
    /// </summary>
    Cancelled
}

/// <summary>
/// Reason for travel interruption.
/// </summary>
public enum TravelInterruptReason
{
    None,
    InsufficientFuel,
    PlayerAbort,
    EncounterDefeat,
    EncounterCapture,
    RouteBlocked
}
