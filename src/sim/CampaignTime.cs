using System;

namespace FringeTactics;

/// <summary>
/// Tracks campaign time in days.
/// Time only advances through explicit actions (travel, rest, missions).
/// </summary>
public class CampaignTime
{
    /// <summary>
    /// Current campaign day (1-indexed, day 1 is campaign start).
    /// </summary>
    public int CurrentDay { get; private set; } = 1;

    /// <summary>
    /// Total days elapsed since campaign start.
    /// </summary>
    public int DaysElapsed => CurrentDay - 1;

    public event Action<int, int> DayAdvanced; // (oldDay, newDay)

    public CampaignTime()
    {
        CurrentDay = 1;
    }

    public CampaignTime(int startDay)
    {
        CurrentDay = Math.Max(1, startDay);
    }

    /// <summary>
    /// Advance time by a number of days.
    /// </summary>
    /// <param name="days">Number of days to advance (must be positive).</param>
    /// <returns>The new current day.</returns>
    public int AdvanceDays(int days)
    {
        if (days <= 0)
        {
            SimLog.Log($"[CampaignTime] Warning: Attempted to advance by {days} days (ignored)");
            return CurrentDay;
        }

        int oldDay = CurrentDay;
        CurrentDay += days;

        SimLog.Log($"[CampaignTime] Day {oldDay} -> Day {CurrentDay} (+{days} days)");
        DayAdvanced?.Invoke(oldDay, CurrentDay);

        return CurrentDay;
    }

    /// <summary>
    /// Check if a deadline (absolute day) has passed.
    /// </summary>
    public bool HasDeadlinePassed(int deadlineDay)
    {
        return CurrentDay > deadlineDay;
    }

    /// <summary>
    /// Get days remaining until a deadline.
    /// Returns 0 if deadline has passed or is today.
    /// </summary>
    public int DaysUntilDeadline(int deadlineDay)
    {
        return Math.Max(0, deadlineDay - CurrentDay);
    }

    /// <summary>
    /// Format current day for display.
    /// </summary>
    public string FormatCurrentDay()
    {
        return $"Day {CurrentDay}";
    }

    /// <summary>
    /// Format a duration in days.
    /// </summary>
    public static string FormatDuration(int days)
    {
        if (days == 1) return "1 day";
        return $"{days} days";
    }

    /// <summary>
    /// Format deadline status.
    /// </summary>
    public string FormatDeadlineStatus(int deadlineDay)
    {
        if (CurrentDay > deadlineDay) return "OVERDUE";
        if (CurrentDay == deadlineDay) return "Due today";
        int remaining = deadlineDay - CurrentDay;
        if (remaining == 1) return "1 day left";
        return $"{remaining} days left";
    }

    /// <summary>
    /// Get state for serialization.
    /// </summary>
    public CampaignTimeState GetState()
    {
        return new CampaignTimeState { CurrentDay = CurrentDay };
    }

    /// <summary>
    /// Restore from saved state.
    /// </summary>
    public void RestoreState(CampaignTimeState state)
    {
        CurrentDay = state.CurrentDay;
        SimLog.Log($"[CampaignTime] Restored to day {CurrentDay}");
    }
}

/// <summary>
/// Serializable state for campaign time.
/// </summary>
public class CampaignTimeState
{
    public int CurrentDay { get; set; } = 1;
}
