using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// Registry of all trait definitions.
/// </summary>
public static class TraitRegistry
{
    private static readonly Dictionary<string, TraitDef> traits = new();
    private static bool initialized = false;

    public static void EnsureInitialized()
    {
        if (initialized) return;
        RegisterDefaultTraits();
        initialized = true;
    }

    /// <summary>
    /// Reset the registry to uninitialized state. For testing only.
    /// </summary>
    public static void Reset()
    {
        traits.Clear();
        initialized = false;
    }

    /// <summary>
    /// Register a custom trait. For data-driven traits or testing.
    /// </summary>
    public static void Register(TraitDef trait)
    {
        EnsureInitialized();
        if (trait?.Id != null)
        {
            traits[trait.Id] = trait;
        }
    }

    public static TraitDef Get(string id)
    {
        EnsureInitialized();
        return traits.TryGetValue(id, out var trait) ? trait : null;
    }

    public static bool Has(string id)
    {
        EnsureInitialized();
        return traits.ContainsKey(id);
    }

    public static IEnumerable<TraitDef> GetAll()
    {
        EnsureInitialized();
        return traits.Values;
    }

    public static IEnumerable<TraitDef> GetByCategory(TraitCategory category)
    {
        EnsureInitialized();
        foreach (var trait in traits.Values)
        {
            if (trait.Category == category)
            {
                yield return trait;
            }
        }
    }

    public static IEnumerable<TraitDef> GetByTag(string tag)
    {
        EnsureInitialized();
        foreach (var trait in traits.Values)
        {
            if (trait.Tags.Contains(tag))
            {
                yield return trait;
            }
        }
    }

    private static void RegisterInternal(TraitDef trait)
    {
        traits[trait.Id] = trait;
    }

    private static void RegisterDefaultTraits()
    {
        // === Background Traits ===
        RegisterInternal(new TraitDef
        {
            Id = "ex_military",
            Name = "Ex-Military",
            Category = TraitCategory.Background,
            Description = "Former military training provides combat edge.",
            Modifiers = new() { new(CrewStatType.Aim, 1) },
            Tags = new() { "military", "combat" }
        });

        RegisterInternal(new TraitDef
        {
            Id = "smuggler",
            Name = "Smuggler",
            Category = TraitCategory.Background,
            Description = "Experience moving contraband opens doors.",
            Modifiers = new() { new(CrewStatType.Savvy, 1) },
            Tags = new() { "criminal", "trade" }
        });

        RegisterInternal(new TraitDef
        {
            Id = "corporate",
            Name = "Corporate",
            Category = TraitCategory.Background,
            Description = "Corporate background aids negotiations.",
            Modifiers = new() { new(CrewStatType.Savvy, 1), new(CrewStatType.Tech, 1) },
            Tags = new() { "corporate", "social" }
        });

        RegisterInternal(new TraitDef
        {
            Id = "frontier_born",
            Name = "Frontier Born",
            Category = TraitCategory.Background,
            Description = "Raised on the edge, tough and resourceful.",
            Modifiers = new() { new(CrewStatType.Grit, 1) },
            Tags = new() { "frontier", "survival" }
        });

        RegisterInternal(new TraitDef
        {
            Id = "spacer",
            Name = "Spacer",
            Category = TraitCategory.Background,
            Description = "Born and raised in space, comfortable in zero-G.",
            Modifiers = new() { new(CrewStatType.Reflexes, 1) },
            Tags = new() { "space", "pilot" }
        });

        // === Personality Traits ===
        RegisterInternal(new TraitDef
        {
            Id = "brave",
            Name = "Brave",
            Category = TraitCategory.Personality,
            Description = "Courage under fire.",
            Modifiers = new() { new(CrewStatType.Resolve, 2) },
            Tags = new() { "morale" }
        });

        RegisterInternal(new TraitDef
        {
            Id = "cautious",
            Name = "Cautious",
            Category = TraitCategory.Personality,
            Description = "Careful approach, harder to hit.",
            Modifiers = new() { new(CrewStatType.Reflexes, 1) },
            Tags = new() { "defensive" }
        });

        RegisterInternal(new TraitDef
        {
            Id = "reckless",
            Name = "Reckless",
            Category = TraitCategory.Personality,
            Description = "Aggressive but exposed.",
            Modifiers = new()
            {
                new(CrewStatType.Aim, 1),
                new(CrewStatType.Grit, -1)
            },
            Tags = new() { "aggressive" }
        });

        RegisterInternal(new TraitDef
        {
            Id = "cold_blooded",
            Name = "Cold Blooded",
            Category = TraitCategory.Personality,
            Description = "Unshakeable under pressure.",
            Modifiers = new() { new(CrewStatType.Resolve, 1), new(CrewStatType.Aim, 1) },
            Tags = new() { "combat", "morale" }
        });

        RegisterInternal(new TraitDef
        {
            Id = "empathetic",
            Name = "Empathetic",
            Category = TraitCategory.Personality,
            Description = "Reads people well, good at negotiations.",
            Modifiers = new() { new(CrewStatType.Savvy, 2) },
            Tags = new() { "social" }
        });

        // === Acquired Traits ===
        RegisterInternal(new TraitDef
        {
            Id = "vengeful",
            Name = "Vengeful",
            Category = TraitCategory.Acquired,
            Description = "Driven by revenge, fights harder against old enemies.",
            Tags = new() { "motivation" }
        });

        RegisterInternal(new TraitDef
        {
            Id = "hardened",
            Name = "Hardened",
            Category = TraitCategory.Acquired,
            Description = "Seen too much to be shaken.",
            Modifiers = new() { new(CrewStatType.Resolve, 2), new(CrewStatType.Grit, 1) },
            Tags = new() { "veteran" }
        });

        RegisterInternal(new TraitDef
        {
            Id = "scarred",
            Name = "Scarred",
            Category = TraitCategory.Acquired,
            Description = "Battle scars tell stories, intimidating presence.",
            Modifiers = new() { new(CrewStatType.Savvy, 1) },
            Tags = new() { "veteran", "social" }
        });

        // === Injury Traits (Permanent) ===
        RegisterInternal(new TraitDef
        {
            Id = "damaged_eye",
            Name = "Damaged Eye",
            Category = TraitCategory.Injury,
            Description = "Permanent vision impairment.",
            Modifiers = new() { new(CrewStatType.Aim, -2) },
            IsPermanent = true,
            Tags = new() { "injury", "vision" }
        });

        RegisterInternal(new TraitDef
        {
            Id = "shattered_knee",
            Name = "Shattered Knee",
            Category = TraitCategory.Injury,
            Description = "Permanent mobility impairment.",
            Modifiers = new() { new(CrewStatType.Reflexes, -2) },
            IsPermanent = true,
            Tags = new() { "injury", "mobility" }
        });

        RegisterInternal(new TraitDef
        {
            Id = "nerve_damage",
            Name = "Nerve Damage",
            Category = TraitCategory.Injury,
            Description = "Reduced fine motor control.",
            Modifiers = new() { new(CrewStatType.Tech, -2) },
            IsPermanent = true,
            Tags = new() { "injury", "dexterity" }
        });

        RegisterInternal(new TraitDef
        {
            Id = "head_trauma",
            Name = "Head Trauma",
            Category = TraitCategory.Injury,
            Description = "Lingering effects of severe head injury.",
            Modifiers = new() { new(CrewStatType.Resolve, -2) },
            IsPermanent = true,
            Tags = new() { "injury", "mental" }
        });

        RegisterInternal(new TraitDef
        {
            Id = "chronic_pain",
            Name = "Chronic Pain",
            Category = TraitCategory.Injury,
            Description = "Constant pain affects focus and endurance.",
            Modifiers = new() { new(CrewStatType.Grit, -1), new(CrewStatType.Resolve, -1) },
            IsPermanent = true,
            Tags = new() { "injury" }
        });
    }
}
