using GdUnit4;
using static GdUnit4.Assertions;

namespace FringeTactics.Tests;

[TestSuite]
public class TV2TravelResultTests
{
    [TestCase]
    public void Completed_CreatesCorrectResult()
    {
        var result = TravelResult.Completed(5, 30, 4);

        AssertThat(result.Status).IsEqual(TravelResultStatus.Completed);
        AssertInt(result.FinalSystemId).IsEqual(5);
        AssertInt(result.FuelConsumed).IsEqual(30);
        AssertInt(result.DaysElapsed).IsEqual(4);
        AssertThat(result.InterruptReason).IsEqual(TravelInterruptReason.None);
    }

    [TestCase]
    public void Completed_WithEncounters_IncludesEncounterList()
    {
        var encounters = new System.Collections.Generic.List<TravelEncounterRecord>
        {
            new TravelEncounterRecord { EncounterId = "enc_1", Outcome = "completed" },
            new TravelEncounterRecord { EncounterId = "enc_2", Outcome = "completed" }
        };

        var result = TravelResult.Completed(5, 30, 4, encounters);

        AssertInt(result.Encounters.Count).IsEqual(2);
    }

    [TestCase]
    public void Interrupted_CreatesCorrectResult()
    {
        var result = TravelResult.Interrupted(3, TravelInterruptReason.InsufficientFuel, 15, 2);

        AssertThat(result.Status).IsEqual(TravelResultStatus.Interrupted);
        AssertInt(result.FinalSystemId).IsEqual(3);
        AssertThat(result.InterruptReason).IsEqual(TravelInterruptReason.InsufficientFuel);
        AssertInt(result.FuelConsumed).IsEqual(15);
        AssertInt(result.DaysElapsed).IsEqual(2);
    }

    [TestCase]
    public void Interrupted_WithState_IncludesState()
    {
        var state = new TravelState { CurrentSystemId = 2, FuelConsumed = 10, DaysElapsed = 1 };

        var result = TravelResult.Interrupted(2, TravelInterruptReason.PlayerAbort, 10, 1, state);

        AssertObject(result.PausedState).IsNotNull();
        AssertInt(result.PausedState.CurrentSystemId).IsEqual(2);
    }

    [TestCase]
    public void Paused_IncludesState()
    {
        var state = new TravelState
        {
            CurrentSystemId = 2,
            FuelConsumed = 10,
            DaysElapsed = 1,
            EncounterHistory = new System.Collections.Generic.List<TravelEncounterRecord>
            {
                new TravelEncounterRecord { EncounterId = "enc_1" }
            }
        };

        var result = TravelResult.Paused(state, 10, 1);

        AssertThat(result.Status).IsEqual(TravelResultStatus.PausedForEncounter);
        AssertObject(result.PausedState).IsNotNull();
        AssertInt(result.PausedState.CurrentSystemId).IsEqual(2);
        AssertInt(result.Encounters.Count).IsEqual(1);
    }

    [TestCase]
    public void Paused_CopiesEncounterHistory()
    {
        var state = new TravelState
        {
            CurrentSystemId = 2,
            EncounterHistory = new System.Collections.Generic.List<TravelEncounterRecord>
            {
                new TravelEncounterRecord { EncounterId = "enc_1", Outcome = "pending" }
            }
        };

        var result = TravelResult.Paused(state, 10, 1);

        // Modify original - should not affect result
        state.EncounterHistory.Add(new TravelEncounterRecord { EncounterId = "enc_2" });

        AssertInt(result.Encounters.Count).IsEqual(1);
    }
}
