using GdUnit4;
using static GdUnit4.Assertions;

namespace FringeTactics.Tests;

/// <summary>
/// Tests for MG4 Phase 1: Campaign Flags System.
/// </summary>
[TestSuite]
public class MG4FlagTests
{
    // ========================================================================
    // SetFlag Tests
    // ========================================================================

    [TestCase]
    public void SetFlag_SetsValueToTrue()
    {
        var campaign = CampaignState.CreateForTesting(12345);

        campaign.SetFlag("test_flag");

        AssertBool(campaign.GetFlag("test_flag")).IsTrue();
    }

    [TestCase]
    public void SetFlag_SetsValueToFalse()
    {
        var campaign = CampaignState.CreateForTesting(12345);

        campaign.SetFlag("test_flag", false);

        AssertBool(campaign.GetFlag("test_flag")).IsFalse();
    }

    [TestCase]
    public void SetFlag_UpdatesExistingValue()
    {
        var campaign = CampaignState.CreateForTesting(12345);
        campaign.SetFlag("test_flag", true);

        campaign.SetFlag("test_flag", false);

        AssertBool(campaign.GetFlag("test_flag")).IsFalse();
    }

    [TestCase]
    public void SetFlag_IgnoresNullFlagId()
    {
        var campaign = CampaignState.CreateForTesting(12345);

        campaign.SetFlag(null);

        AssertInt(campaign.Flags.Count).IsEqual(0);
    }

    [TestCase]
    public void SetFlag_IgnoresEmptyFlagId()
    {
        var campaign = CampaignState.CreateForTesting(12345);

        campaign.SetFlag("");

        AssertInt(campaign.Flags.Count).IsEqual(0);
    }

    [TestCase]
    public void SetFlag_EmitsEventOnChange()
    {
        var campaign = CampaignState.CreateForTesting(12345);
        var eventBus = new EventBus();
        campaign.EventBus = eventBus;

        CampaignFlagChangedEvent? receivedEvent = null;
        eventBus.Subscribe<CampaignFlagChangedEvent>(e => receivedEvent = e);

        campaign.SetFlag("test_flag", true);

        AssertObject(receivedEvent).IsNotNull();
        AssertString(receivedEvent.Value.FlagId).IsEqual("test_flag");
        AssertBool(receivedEvent.Value.OldValue).IsFalse();
        AssertBool(receivedEvent.Value.NewValue).IsTrue();
    }

    [TestCase]
    public void SetFlag_NoEventIfValueUnchanged()
    {
        var campaign = CampaignState.CreateForTesting(12345);
        var eventBus = new EventBus();
        campaign.EventBus = eventBus;
        campaign.SetFlag("test_flag", true);

        CampaignFlagChangedEvent? receivedEvent = null;
        eventBus.Subscribe<CampaignFlagChangedEvent>(e => receivedEvent = e);

        campaign.SetFlag("test_flag", true); // Same value

        AssertObject(receivedEvent).IsNull();
    }

    // ========================================================================
    // GetFlag Tests
    // ========================================================================

    [TestCase]
    public void GetFlag_ReturnsFalseForUnsetFlag()
    {
        var campaign = CampaignState.CreateForTesting(12345);

        var result = campaign.GetFlag("nonexistent_flag");

        AssertBool(result).IsFalse();
    }

    [TestCase]
    public void GetFlag_ReturnsTrueForSetFlag()
    {
        var campaign = CampaignState.CreateForTesting(12345);
        campaign.SetFlag("test_flag", true);

        var result = campaign.GetFlag("test_flag");

        AssertBool(result).IsTrue();
    }

    [TestCase]
    public void GetFlag_ReturnsFalseForFlagSetToFalse()
    {
        var campaign = CampaignState.CreateForTesting(12345);
        campaign.SetFlag("test_flag", false);

        var result = campaign.GetFlag("test_flag");

        AssertBool(result).IsFalse();
    }

    [TestCase]
    public void GetFlag_ReturnsFalseForNullFlagId()
    {
        var campaign = CampaignState.CreateForTesting(12345);

        var result = campaign.GetFlag(null);

        AssertBool(result).IsFalse();
    }

    [TestCase]
    public void GetFlag_ReturnsFalseForEmptyFlagId()
    {
        var campaign = CampaignState.CreateForTesting(12345);

        var result = campaign.GetFlag("");

        AssertBool(result).IsFalse();
    }

    // ========================================================================
    // HasFlag Tests
    // ========================================================================

    [TestCase]
    public void HasFlag_ReturnsFalseForUnsetFlag()
    {
        var campaign = CampaignState.CreateForTesting(12345);

        var result = campaign.HasFlag("nonexistent_flag");

        AssertBool(result).IsFalse();
    }

    [TestCase]
    public void HasFlag_ReturnsTrueForFlagSetToTrue()
    {
        var campaign = CampaignState.CreateForTesting(12345);
        campaign.SetFlag("test_flag", true);

        var result = campaign.HasFlag("test_flag");

        AssertBool(result).IsTrue();
    }

    [TestCase]
    public void HasFlag_ReturnsFalseForFlagSetToFalse()
    {
        var campaign = CampaignState.CreateForTesting(12345);
        campaign.SetFlag("test_flag", false);

        var result = campaign.HasFlag("test_flag");

        AssertBool(result).IsFalse();
    }

    [TestCase]
    public void HasFlag_ReturnsFalseForNullFlagId()
    {
        var campaign = CampaignState.CreateForTesting(12345);

        var result = campaign.HasFlag(null);

        AssertBool(result).IsFalse();
    }

    // ========================================================================
    // ClearFlag Tests
    // ========================================================================

    [TestCase]
    public void ClearFlag_RemovesExistingFlag()
    {
        var campaign = CampaignState.CreateForTesting(12345);
        campaign.SetFlag("test_flag", true);

        var result = campaign.ClearFlag("test_flag");

        AssertBool(result).IsTrue();
        AssertBool(campaign.Flags.ContainsKey("test_flag")).IsFalse();
    }

    [TestCase]
    public void ClearFlag_ReturnsFalseForNonexistentFlag()
    {
        var campaign = CampaignState.CreateForTesting(12345);

        var result = campaign.ClearFlag("nonexistent_flag");

        AssertBool(result).IsFalse();
    }

    [TestCase]
    public void ClearFlag_EmitsEvent()
    {
        var campaign = CampaignState.CreateForTesting(12345);
        var eventBus = new EventBus();
        campaign.EventBus = eventBus;
        campaign.SetFlag("test_flag", true);

        CampaignFlagChangedEvent? receivedEvent = null;
        eventBus.Subscribe<CampaignFlagChangedEvent>(e => receivedEvent = e);

        campaign.ClearFlag("test_flag");

        AssertObject(receivedEvent).IsNotNull();
        AssertString(receivedEvent.Value.FlagId).IsEqual("test_flag");
        AssertBool(receivedEvent.Value.OldValue).IsTrue();
        AssertBool(receivedEvent.Value.NewValue).IsFalse();
    }

    [TestCase]
    public void ClearFlag_ReturnsFalseForNullFlagId()
    {
        var campaign = CampaignState.CreateForTesting(12345);

        var result = campaign.ClearFlag(null);

        AssertBool(result).IsFalse();
    }

    // ========================================================================
    // Multiple Flags Tests
    // ========================================================================

    [TestCase]
    public void MultipleFlags_CanSetAndRetrieve()
    {
        var campaign = CampaignState.CreateForTesting(12345);

        campaign.SetFlag("flag_a", true);
        campaign.SetFlag("flag_b", false);
        campaign.SetFlag("flag_c", true);

        AssertBool(campaign.GetFlag("flag_a")).IsTrue();
        AssertBool(campaign.GetFlag("flag_b")).IsFalse();
        AssertBool(campaign.GetFlag("flag_c")).IsTrue();
        AssertInt(campaign.Flags.Count).IsEqual(3);
    }

    // ========================================================================
    // Serialization Tests
    // ========================================================================

    [TestCase]
    public void Flags_SerializedInGetState()
    {
        var campaign = CampaignState.CreateForTesting(12345);
        campaign.SetFlag("saved_flag", true);
        campaign.SetFlag("another_flag", false);

        var state = campaign.GetState();

        AssertObject(state.Flags).IsNotNull();
        AssertInt(state.Flags.Count).IsEqual(2);
        AssertBool(state.Flags["saved_flag"]).IsTrue();
        AssertBool(state.Flags["another_flag"]).IsFalse();
    }

    [TestCase]
    public void Flags_RestoredInFromState()
    {
        var original = CampaignState.CreateForTesting(12345);
        original.SetFlag("restored_flag", true);
        original.SetFlag("false_flag", false);

        var state = original.GetState();
        var restored = CampaignState.FromState(state);

        AssertBool(restored.GetFlag("restored_flag")).IsTrue();
        AssertBool(restored.GetFlag("false_flag")).IsFalse();
        AssertInt(restored.Flags.Count).IsEqual(2);
    }

    [TestCase]
    public void Flags_NullSafeOnLoad()
    {
        var state = new CampaignStateData
        {
            Flags = null
        };

        var campaign = CampaignState.FromState(state);

        AssertObject(campaign.Flags).IsNotNull();
        AssertInt(campaign.Flags.Count).IsEqual(0);
    }

    [TestCase]
    public void Flags_RoundTripPreservesAllFlags()
    {
        var original = CampaignState.CreateForTesting(12345);
        original.SetFlag("quest_started", true);
        original.SetFlag("npc_met", true);
        original.SetFlag("item_found", false);
        original.SetFlag("boss_defeated", true);

        var state = original.GetState();
        var restored = CampaignState.FromState(state);

        AssertInt(restored.Flags.Count).IsEqual(4);
        AssertBool(restored.HasFlag("quest_started")).IsTrue();
        AssertBool(restored.HasFlag("npc_met")).IsTrue();
        AssertBool(restored.HasFlag("item_found")).IsFalse();
        AssertBool(restored.HasFlag("boss_defeated")).IsTrue();
    }
}
