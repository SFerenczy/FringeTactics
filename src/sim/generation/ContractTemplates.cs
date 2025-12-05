using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// Title and description templates for contract generation.
/// </summary>
public static class ContractTemplates
{
    private static readonly Dictionary<ContractType, string[]> Titles = new()
    {
        {
            ContractType.Assault, new[]
            {
                "Clear the Area",
                "Hostile Elimination",
                "Strike Mission",
                "Combat Sweep",
                "Neutralize Hostiles",
                "Sector Cleanse",
                "Armed Response"
            }
        },
        {
            ContractType.Extraction, new[]
            {
                "Rescue Mission",
                "Personnel Recovery",
                "Hostage Extraction",
                "Asset Retrieval",
                "Emergency Evac",
                "Search and Rescue",
                "Recovery Op"
            }
        }
    };

    private static readonly Dictionary<ContractType, string[]> DescriptionFormats = new()
    {
        {
            ContractType.Assault, new[]
            {
                "Eliminate all hostiles at {0}. {1} is offering {2} credits for completion.",
                "Clear out the enemy presence at {0}. {1} wants the area secured.",
                "Hostile forces have occupied {0}. {1} needs them removed permanently."
            }
        },
        {
            ContractType.Extraction, new[]
            {
                "Locate and extract personnel from {0}. {1} needs them back alive.",
                "Recover our people from {0}. {1} is offering hazard pay.",
                "Search {0} for survivors and bring them home. {1} wants results."
            }
        }
    };

    /// <summary>
    /// Get a random title for the given contract type.
    /// </summary>
    public static string GetRandomTitle(ContractType type, RngStream rng)
    {
        if (!Titles.TryGetValue(type, out var titles))
            titles = Titles[ContractType.Assault];

        return titles[rng.NextInt(titles.Length)];
    }


    /// <summary>
    /// Get a random description for the given contract type.
    /// </summary>
    public static string GetDescription(ContractType type, string targetName, string factionName, int reward, RngStream rng)
    {
        if (!DescriptionFormats.TryGetValue(type, out var formats))
            formats = DescriptionFormats[ContractType.Assault];

        var format = formats[rng.NextInt(formats.Length)];
        return string.Format(format, targetName, factionName, reward);
    }


    /// <summary>
    /// Get a description using the first template (for deterministic output).
    /// </summary>
    public static string GetDescription(ContractType type, string targetName, string factionName, int reward)
    {
        if (!DescriptionFormats.TryGetValue(type, out var formats))
            formats = DescriptionFormats[ContractType.Assault];

        return string.Format(formats[0], targetName, factionName, reward);
    }
}
