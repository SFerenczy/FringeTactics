using System;
using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// Types of stats that can be modified.
/// </summary>
public enum StatType
{
    MoveSpeed,
    Accuracy,
    Damage,
    VisionRadius,
    FireRate
}

/// <summary>
/// A single modifier applied to a stat.
/// </summary>
public class StatModifier
{
    public string SourceId { get; }
    public StatType Stat { get; }
    public float Multiplier { get; }
    public float FlatBonus { get; }
    public int ExpiresAtTick { get; }

    /// <summary>
    /// Create a stat modifier.
    /// </summary>
    /// <param name="sourceId">Identifier for the source (e.g., "suppressed", "stunned")</param>
    /// <param name="stat">Which stat to modify</param>
    /// <param name="multiplier">Multiplier applied to base value (1.0 = no change, 0.5 = halved)</param>
    /// <param name="flatBonus">Flat value added after multiplier</param>
    /// <param name="expiresAtTick">Tick when this modifier expires (-1 = permanent until removed)</param>
    public StatModifier(string sourceId, StatType stat, float multiplier, float flatBonus, int expiresAtTick)
    {
        SourceId = sourceId;
        Stat = stat;
        Multiplier = multiplier;
        FlatBonus = flatBonus;
        ExpiresAtTick = expiresAtTick;
    }

    /// <summary>
    /// Create a multiplicative modifier (e.g., 0.5 = halve the stat).
    /// </summary>
    public static StatModifier Multiplicative(string sourceId, StatType stat, float multiplier, int expiresAtTick)
    {
        return new StatModifier(sourceId, stat, multiplier, 0f, expiresAtTick);
    }

    /// <summary>
    /// Create an additive modifier (e.g., +10 to stat).
    /// </summary>
    public static StatModifier Additive(string sourceId, StatType stat, float bonus, int expiresAtTick)
    {
        return new StatModifier(sourceId, stat, 1f, bonus, expiresAtTick);
    }

    /// <summary>
    /// Create a permanent modifier (until manually removed).
    /// </summary>
    public static StatModifier Permanent(string sourceId, StatType stat, float multiplier, float flatBonus)
    {
        return new StatModifier(sourceId, stat, multiplier, flatBonus, -1);
    }

    public bool IsExpired(int currentTick)
    {
        return ExpiresAtTick >= 0 && currentTick >= ExpiresAtTick;
    }
}

/// <summary>
/// Collection of stat modifiers with calculation methods.
/// </summary>
public class ModifierCollection
{
    private readonly List<StatModifier> modifiers = new();

    public event Action ModifiersChanged;

    public IReadOnlyList<StatModifier> All => modifiers;

    public void Add(StatModifier modifier)
    {
        modifiers.Add(modifier);
        ModifiersChanged?.Invoke();
    }

    public void RemoveBySource(string sourceId)
    {
        int removed = modifiers.RemoveAll(m => m.SourceId == sourceId);
        if (removed > 0)
        {
            ModifiersChanged?.Invoke();
        }
    }

    public void RemoveExpired(int currentTick)
    {
        int removed = modifiers.RemoveAll(m => m.IsExpired(currentTick));
        if (removed > 0)
        {
            ModifiersChanged?.Invoke();
        }
    }

    public void Clear()
    {
        if (modifiers.Count > 0)
        {
            modifiers.Clear();
            ModifiersChanged?.Invoke();
        }
    }

    public bool HasModifier(string sourceId)
    {
        foreach (var mod in modifiers)
        {
            if (mod.SourceId == sourceId)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Calculate the final value for a stat.
    /// Multipliers are combined multiplicatively, then flat bonuses are added.
    /// </summary>
    public float Calculate(StatType stat, float baseValue)
    {
        float totalMultiplier = 1f;
        float totalFlat = 0f;

        foreach (var mod in modifiers)
        {
            if (mod.Stat == stat)
            {
                totalMultiplier *= mod.Multiplier;
                totalFlat += mod.FlatBonus;
            }
        }

        return baseValue * totalMultiplier + totalFlat;
    }

    /// <summary>
    /// Get all active modifiers for a specific stat.
    /// </summary>
    public List<StatModifier> GetModifiersFor(StatType stat)
    {
        var result = new List<StatModifier>();
        foreach (var mod in modifiers)
        {
            if (mod.Stat == stat)
            {
                result.Add(mod);
            }
        }
        return result;
    }

    /// <summary>
    /// Get all unique source IDs currently affecting this collection.
    /// </summary>
    public HashSet<string> GetActiveSources()
    {
        var sources = new HashSet<string>();
        foreach (var mod in modifiers)
        {
            sources.Add(mod.SourceId);
        }
        return sources;
    }
}
