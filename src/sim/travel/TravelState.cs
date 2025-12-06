using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// In-progress travel state. Tracks position within a travel plan.
/// TODO: Add GetState()/FromState() for save/load mid-travel when needed.
/// </summary>
public class TravelState
{
    /// <summary>
    /// The travel plan being executed.
    /// </summary>
    public TravelPlan Plan { get; set; }

    /// <summary>
    /// Current segment index (0-based).
    /// </summary>
    public int CurrentSegmentIndex { get; set; } = 0;

    /// <summary>
    /// Current day within the current segment (0-based).
    /// </summary>
    public int CurrentDayInSegment { get; set; } = 0;

    /// <summary>
    /// Whether travel is paused for an encounter.
    /// </summary>
    public bool IsPausedForEncounter { get; set; } = false;

    /// <summary>
    /// Pending encounter ID if paused.
    /// </summary>
    public string PendingEncounterId { get; set; }

    /// <summary>
    /// Fuel consumed so far.
    /// </summary>
    public int FuelConsumed { get; set; } = 0;

    /// <summary>
    /// Days elapsed so far.
    /// </summary>
    public int DaysElapsed { get; set; } = 0;

    /// <summary>
    /// Encounters that occurred during travel.
    /// </summary>
    public List<TravelEncounterRecord> EncounterHistory { get; set; } = new();

    /// <summary>
    /// Current system ID (updated as we complete segments).
    /// </summary>
    public int CurrentSystemId { get; set; }

    /// <summary>
    /// Whether travel is complete.
    /// </summary>
    public bool IsComplete => Plan == null || CurrentSegmentIndex >= Plan.Segments.Count;

    /// <summary>
    /// Get current segment (null if complete).
    /// </summary>
    public TravelSegment CurrentSegment =>
        IsComplete ? null : Plan?.Segments[CurrentSegmentIndex];

    /// <summary>
    /// Create initial state for a travel plan.
    /// </summary>
    public static TravelState Create(TravelPlan plan, int startSystemId)
    {
        return new TravelState
        {
            Plan = plan,
            CurrentSystemId = startSystemId,
            CurrentSegmentIndex = 0,
            CurrentDayInSegment = 0
        };
    }
}
