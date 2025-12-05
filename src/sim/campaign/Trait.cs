using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// Categories for crew traits.
/// </summary>
public enum TraitCategory
{
    Background,   // Ex-Military, Smuggler, Corporate - origin/history
    Personality,  // Brave, Cautious, Reckless - behavioral tendencies
    Acquired,     // Vengeful, Hardened - gained through experience
    Injury        // Damaged Eye, Shattered Knee - permanent injuries
}

/// <summary>
/// A stat modifier applied by a trait.
/// </summary>
public struct CrewStatModifier
{
    public CrewStatType Stat { get; set; }
    public int FlatBonus { get; set; }

    public CrewStatModifier(CrewStatType stat, int flat)
    {
        Stat = stat;
        FlatBonus = flat;
    }
}

/// <summary>
/// Definition of a trait that can be assigned to crew.
/// </summary>
public class TraitDef
{
    private string id;
    private string name;

    public string Id
    {
        get => id;
        set => id = value ?? throw new System.ArgumentNullException(nameof(Id));
    }

    public string Name
    {
        get => name;
        set => name = value ?? throw new System.ArgumentNullException(nameof(Name));
    }

    public string Description { get; set; } = "";
    public TraitCategory Category { get; set; }
    public List<CrewStatModifier> Modifiers { get; set; } = new();
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// If true, this trait cannot be removed (permanent injuries).
    /// </summary>
    public bool IsPermanent { get; set; } = false;

    /// <summary>
    /// Check if this trait definition is valid.
    /// </summary>
    public bool IsValid => !string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(name);

    /// <summary>
    /// Get total modifier for a specific stat from this trait.
    /// </summary>
    public int GetModifierFor(CrewStatType stat)
    {
        int total = 0;
        foreach (var mod in Modifiers)
        {
            if (mod.Stat == stat)
            {
                total += mod.FlatBonus;
            }
        }
        return total;
    }
}
