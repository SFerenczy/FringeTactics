using System;

namespace FringeTactics;

/// <summary>
/// Executes travel plans, consuming resources and triggering encounters.
/// Stateless service - all state is passed in/out via TravelState.
/// </summary>
public class TravelExecutor
{
    private readonly RngService rng;

    public TravelExecutor(RngService rng)
    {
        this.rng = rng ?? throw new ArgumentNullException(nameof(rng));
    }

    /// <summary>
    /// Execute a travel plan from start to finish (or until interrupted).
    /// </summary>
    public TravelResult Execute(TravelPlan plan, CampaignState campaign)
    {
        if (plan == null || !plan.IsValid)
        {
            return TravelResult.Interrupted(
                campaign.CurrentNodeId,
                TravelInterruptReason.RouteBlocked,
                0, 0);
        }

        // Check fuel before starting
        if (campaign.Fuel < plan.TotalFuelCost)
        {
            SimLog.Log($"[Travel] Cannot start: insufficient fuel ({campaign.Fuel}/{plan.TotalFuelCost})");
            return TravelResult.Interrupted(
                campaign.CurrentNodeId,
                TravelInterruptReason.InsufficientFuel,
                0, 0);
        }

        // Create initial state
        var state = TravelState.Create(plan, campaign.CurrentNodeId);

        // Emit start event
        campaign.EventBus?.Publish(new TravelStartedEvent(
            plan.OriginSystemId,
            plan.DestinationSystemId,
            plan.TotalTimeDays,
            plan.TotalFuelCost
        ));

        SimLog.Log($"[Travel] Starting journey: {campaign.World?.GetSystem(plan.OriginSystemId)?.Name} → {campaign.World?.GetSystem(plan.DestinationSystemId)?.Name}");
        SimLog.Log($"[Travel] Estimated: {plan.TotalTimeDays} days, {plan.TotalFuelCost} fuel, {plan.Segments.Count} segment(s)");

        // Execute travel
        return ExecuteFromState(state, campaign);
    }

    /// <summary>
    /// Resume travel from a paused state (after encounter resolution).
    /// </summary>
    public TravelResult Resume(TravelState state, CampaignState campaign, string encounterOutcome = "completed")
    {
        if (state == null || state.Plan == null)
        {
            return TravelResult.Interrupted(
                campaign.CurrentNodeId,
                TravelInterruptReason.RouteBlocked,
                0, 0);
        }

        // Record encounter outcome
        if (state.IsPausedForEncounter && !string.IsNullOrEmpty(state.PendingEncounterId))
        {
            // Update the last encounter record with outcome
            if (state.EncounterHistory.Count > 0)
            {
                state.EncounterHistory[^1].Outcome = encounterOutcome;
            }

            campaign.EventBus?.Publish(new TravelEncounterResolvedEvent(
                state.PendingEncounterId,
                encounterOutcome
            ));

            // Check if encounter result should abort travel
            if (encounterOutcome == EncounterOutcomes.Defeat || encounterOutcome == EncounterOutcomes.Captured)
            {
                var reason = encounterOutcome == EncounterOutcomes.Captured
                    ? TravelInterruptReason.EncounterCapture
                    : TravelInterruptReason.EncounterDefeat;

                return TravelResult.Interrupted(
                    state.CurrentSystemId,
                    reason,
                    state.FuelConsumed,
                    state.DaysElapsed,
                    state);
            }
        }

        // Clear pause state
        state.IsPausedForEncounter = false;
        state.PendingEncounterId = null;

        // Continue execution
        return ExecuteFromState(state, campaign);
    }

    /// <summary>
    /// Core execution loop.
    /// </summary>
    private TravelResult ExecuteFromState(TravelState state, CampaignState campaign)
    {
        while (!state.IsComplete)
        {
            var segment = state.CurrentSegment;

            // Emit segment start event (only on first day of segment)
            if (state.CurrentDayInSegment == 0)
            {
                campaign.EventBus?.Publish(new TravelSegmentStartedEvent(
                    segment.FromSystemId,
                    segment.ToSystemId,
                    state.CurrentSegmentIndex,
                    segment.TimeDays
                ));

                SimLog.Log($"[Travel] Segment {state.CurrentSegmentIndex + 1}/{state.Plan.Segments.Count}: {campaign.World?.GetSystem(segment.FromSystemId)?.Name} → {campaign.World?.GetSystem(segment.ToSystemId)?.Name}");
            }

            // Process remaining days in current segment
            while (state.CurrentDayInSegment < segment.TimeDays)
            {
                // Calculate fuel for this day (proportional)
                int dailyFuel = CalculateDailyFuel(segment, state.CurrentDayInSegment);

                // Check fuel
                if (campaign.Fuel < dailyFuel)
                {
                    SimLog.Log($"[Travel] Out of fuel at day {state.DaysElapsed + 1}!");
                    campaign.EventBus?.Publish(new TravelInterruptedEvent(
                        state.CurrentSystemId,
                        "out_of_fuel"
                    ));

                    return TravelResult.Interrupted(
                        state.CurrentSystemId,
                        TravelInterruptReason.InsufficientFuel,
                        state.FuelConsumed,
                        state.DaysElapsed,
                        state);
                }

                // Consume fuel
                campaign.SpendFuel(dailyFuel, "travel");
                state.FuelConsumed += dailyFuel;

                // Advance time
                campaign.Time.AdvanceDays(1);
                state.DaysElapsed++;
                state.CurrentDayInSegment++;

                // Roll for encounter
                var encounterResult = TryTriggerEncounter(state, campaign, segment);
                if (encounterResult != null)
                {
                    return encounterResult;
                }
            }

            // Segment complete - move to destination
            int fromSystemId = state.CurrentSystemId;
            state.CurrentSystemId = segment.ToSystemId;
            campaign.CurrentNodeId = segment.ToSystemId;

            campaign.EventBus?.Publish(new TravelSegmentCompletedEvent(
                segment.FromSystemId,
                segment.ToSystemId,
                segment.FuelCost,
                segment.TimeDays
            ));

            campaign.EventBus?.Publish(new PlayerMovedEvent(
                fromSystemId,
                segment.ToSystemId,
                campaign.World?.GetSystem(segment.ToSystemId)?.Name ?? "Unknown"
            ));

            SimLog.Log($"[Travel] Arrived at {campaign.World?.GetSystem(segment.ToSystemId)?.Name}");

            // Move to next segment
            state.CurrentSegmentIndex++;
            state.CurrentDayInSegment = 0;
        }

        // Travel complete
        campaign.EventBus?.Publish(new TravelCompletedEvent(
            state.Plan.OriginSystemId,
            state.Plan.DestinationSystemId,
            state.DaysElapsed,
            state.FuelConsumed,
            state.EncounterHistory.Count
        ));

        SimLog.Log($"[Travel] Journey complete! {state.DaysElapsed} days, {state.FuelConsumed} fuel, {state.EncounterHistory.Count} encounter(s)");

        return TravelResult.Completed(
            state.Plan.DestinationSystemId,
            state.FuelConsumed,
            state.DaysElapsed,
            state.EncounterHistory);
    }

    /// <summary>
    /// Calculate fuel consumption for a specific day of a segment.
    /// Distributes fuel evenly across days, with remainder on last day.
    /// </summary>
    private int CalculateDailyFuel(TravelSegment segment, int dayIndex)
    {
        if (segment.TimeDays <= 0) return segment.FuelCost;

        int baseDailyFuel = segment.FuelCost / segment.TimeDays;
        int remainder = segment.FuelCost % segment.TimeDays;

        // Add remainder to last day
        if (dayIndex == segment.TimeDays - 1)
            return baseDailyFuel + remainder;

        return baseDailyFuel;
    }

    /// <summary>
    /// Roll for encounter and handle if triggered.
    /// Returns TravelResult if travel should pause/stop, null to continue.
    /// </summary>
    private TravelResult TryTriggerEncounter(TravelState state, CampaignState campaign, TravelSegment segment)
    {
        float encounterChance = segment.EncounterChance;
        float roll = rng.Campaign.NextFloat();

        if (roll >= encounterChance)
        {
            return null; // No encounter
        }

        // Create travel context for encounter generation
        var context = TravelContext.Create(state, campaign);
        string encounterType = segment.SuggestedEncounterType ?? EncounterTypes.Random;

        SimLog.Log($"[Travel] Encounter triggered! Type: {encounterType}, Roll: {roll:F2} < {encounterChance:F2}");

        // Generate encounter using GN3 system
        EncounterInstance encounter = null;
        if (campaign.EncounterGenerator != null)
        {
            encounter = campaign.EncounterGenerator.Generate(context, campaign);
        }

        // Fallback ID if generation fails
        string encounterId = encounter?.InstanceId 
            ?? $"enc_{state.CurrentSystemId}_{state.DaysElapsed}_{rng.Campaign.NextInt(10000)}";

        // Record encounter
        var record = new TravelEncounterRecord
        {
            SegmentIndex = state.CurrentSegmentIndex,
            DayInSegment = state.CurrentDayInSegment,
            SystemId = state.CurrentSystemId,
            EncounterType = encounter?.Template?.Id ?? encounterType,
            EncounterId = encounterId,
            Outcome = EncounterOutcomes.Pending
        };
        state.EncounterHistory.Add(record);

        campaign.EventBus?.Publish(new TravelEncounterTriggeredEvent(
            state.CurrentSystemId,
            encounter?.Template?.Id ?? encounterType,
            encounterId
        ));

        // If we have a real encounter instance, pause for it
        if (encounter != null)
        {
            campaign.ActiveEncounter = encounter;
            state.IsPausedForEncounter = true;
            state.PendingEncounterId = encounterId;

            SimLog.Log($"[Travel] Pausing for encounter: {encounter.Template.Id} ({encounterId})");
            return TravelResult.Paused(state, state.FuelConsumed, state.DaysElapsed);
        }

        // Fallback: auto-resolve if no encounter generated (shouldn't happen with production templates)
        record.Outcome = EncounterOutcomes.Completed;
        campaign.EventBus?.Publish(new TravelEncounterResolvedEvent(encounterId, EncounterOutcomes.Completed));
        SimLog.Log($"[Travel] No encounter generated, auto-resolved");

        return null;
    }
}
