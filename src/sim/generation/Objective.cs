using System;
using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// A mission objective (primary or secondary).
/// Uses ObjectiveType enum from MissionInput.cs.
/// </summary>
public class Objective
{
    /// <summary>
    /// Unique identifier for this objective.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Type of objective.
    /// </summary>
    public ObjectiveType Type { get; set; }

    /// <summary>
    /// Human-readable description.
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// True if this is required for mission success.
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// Bonus reward percentage for completing this objective (if secondary).
    /// </summary>
    public int BonusRewardPercent { get; set; } = 0;

    /// <summary>
    /// Type-specific parameters (e.g., turn count for TimeLimit).
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();

    // ========================================================================
    // FACTORY METHODS - Primary Objectives
    // ========================================================================

    /// <summary>
    /// Eliminate all hostiles on the map.
    /// </summary>
    public static Objective EliminateAll() => new()
    {
        Id = "eliminate_all",
        Type = ObjectiveType.EliminateAll,
        Description = "Eliminate all hostiles",
        IsRequired = true
    };

    /// <summary>
    /// Eliminate a specific target.
    /// </summary>
    public static Objective EliminateTarget(string targetName = "the target") => new()
    {
        Id = "eliminate_target",
        Type = ObjectiveType.EliminateTarget,
        Description = $"Eliminate {targetName}",
        IsRequired = true,
        Parameters = new() { { "target_name", targetName } }
    };

    /// <summary>
    /// Reach the extraction zone.
    /// </summary>
    public static Objective ReachExtraction() => new()
    {
        Id = "reach_extraction",
        Type = ObjectiveType.ReachZone,
        Description = "Reach the extraction zone",
        IsRequired = true
    };

    /// <summary>
    /// Protect a VIP unit.
    /// </summary>
    public static Objective ProtectVIP(string vipName = "the VIP") => new()
    {
        Id = "protect_vip",
        Type = ObjectiveType.ProtectUnit,
        Description = $"Keep {vipName} alive",
        IsRequired = true,
        Parameters = new() { { "unit_name", vipName } }
    };

    /// <summary>
    /// Destroy a target object.
    /// </summary>
    public static Objective DestroyTarget(string targetName = "the target") => new()
    {
        Id = "destroy_target",
        Type = ObjectiveType.DestroyObject,
        Description = $"Destroy {targetName}",
        IsRequired = true,
        Parameters = new() { { "target_name", targetName } }
    };

    /// <summary>
    /// Hack a terminal.
    /// </summary>
    public static Objective HackTerminal() => new()
    {
        Id = "hack_terminal",
        Type = ObjectiveType.HackTerminal,
        Description = "Hack the terminal",
        IsRequired = true
    };

    /// <summary>
    /// Retrieve an item and extract.
    /// </summary>
    public static Objective RetrieveItem(string itemName = "the package") => new()
    {
        Id = "retrieve_item",
        Type = ObjectiveType.RetrieveItem,
        Description = $"Retrieve {itemName}",
        IsRequired = true,
        Parameters = new() { { "item_name", itemName } }
    };

    /// <summary>
    /// Survive for a number of turns.
    /// </summary>
    public static Objective SurviveTurns(int turns) => new()
    {
        Id = "survive_turns",
        Type = ObjectiveType.SurviveTurns,
        Description = $"Survive for {turns} turns",
        IsRequired = true,
        Parameters = new() { { "turns", turns } }
    };

    // ========================================================================
    // FACTORY METHODS - Secondary/Bonus Objectives
    // ========================================================================

    /// <summary>
    /// Complete within a turn limit for bonus.
    /// </summary>
    public static Objective TimeBonus(int turns) => new()
    {
        Id = "time_bonus",
        Type = ObjectiveType.TimeLimit,
        Description = $"Complete within {turns} turns",
        IsRequired = false,
        BonusRewardPercent = 15,
        Parameters = new() { { "turns", turns } }
    };

    /// <summary>
    /// No crew deaths for bonus.
    /// </summary>
    public static Objective NoCasualties() => new()
    {
        Id = "no_casualties",
        Type = ObjectiveType.NoCasualties,
        Description = "No crew deaths",
        IsRequired = false,
        BonusRewardPercent = 20
    };

    /// <summary>
    /// No crew injuries for bonus.
    /// </summary>
    public static Objective NoInjuries() => new()
    {
        Id = "no_injuries",
        Type = ObjectiveType.NoInjuries,
        Description = "No crew injuries",
        IsRequired = false,
        BonusRewardPercent = 10
    };

    /// <summary>
    /// Complete without triggering alarm (ghost bonus).
    /// </summary>
    public static Objective GhostBonus() => new()
    {
        Id = "ghost",
        Type = ObjectiveType.NoAlarm,
        Description = "Complete without triggering alarm",
        IsRequired = false,
        BonusRewardPercent = 25
    };

    /// <summary>
    /// Eliminate all enemies (as bonus when not primary).
    /// </summary>
    public static Objective EliminateAllBonus() => new()
    {
        Id = "eliminate_all_bonus",
        Type = ObjectiveType.EliminateAll,
        Description = "Eliminate all hostiles",
        IsRequired = false,
        BonusRewardPercent = 15
    };

    // ========================================================================
    // SERIALIZATION
    // ========================================================================

    /// <summary>
    /// Get state for serialization.
    /// </summary>
    public ObjectiveData GetState()
    {
        return new ObjectiveData
        {
            Id = Id,
            Type = Type.ToString(),
            Description = Description,
            IsRequired = IsRequired,
            BonusRewardPercent = BonusRewardPercent,
            Parameters = Parameters != null && Parameters.Count > 0
                ? new Dictionary<string, object>(Parameters)
                : null
        };
    }

    /// <summary>
    /// Restore from saved state.
    /// </summary>
    public static Objective FromState(ObjectiveData data)
    {
        if (data == null) return null;

        return new Objective
        {
            Id = data.Id,
            Type = Enum.TryParse<ObjectiveType>(data.Type, out var type) ? type : ObjectiveType.EliminateAll,
            Description = data.Description ?? "",
            IsRequired = data.IsRequired,
            BonusRewardPercent = data.BonusRewardPercent,
            Parameters = data.Parameters != null
                ? new Dictionary<string, object>(data.Parameters)
                : new Dictionary<string, object>()
        };
    }
}
