using System;
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
            ContractType.Delivery, new[]
            {
                "Cargo Run",
                "Priority Delivery",
                "Supply Transport",
                "Secure Shipment",
                "Express Freight",
                "Hot Cargo",
                "Courier Job"
            }
        },
        {
            ContractType.Escort, new[]
            {
                "VIP Protection",
                "Executive Escort",
                "Safe Passage",
                "Personnel Transfer",
                "Witness Protection",
                "Diplomatic Guard",
                "High-Value Transport"
            }
        },
        {
            ContractType.Raid, new[]
            {
                "Sabotage Operation",
                "Asset Denial",
                "Strike and Extract",
                "Target Elimination",
                "Facility Raid",
                "Smash and Grab",
                "Demolition Run"
            }
        },
        {
            ContractType.Heist, new[]
            {
                "Data Extraction",
                "Covert Acquisition",
                "Silent Retrieval",
                "Intelligence Grab",
                "Ghost Operation",
                "Clean Sweep",
                "Shadow Job"
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
            ContractType.Delivery, new[]
            {
                "Transport sensitive cargo to {0}. {1} requires secure delivery.",
                "A shipment needs to reach {0} intact. {1} is paying well for discretion.",
                "Priority freight bound for {0}. {1} expects timely arrival."
            }
        },
        {
            ContractType.Escort, new[]
            {
                "Escort VIP safely to {0}. {1} is paying premium for their protection.",
                "A high-value individual needs transport to {0}. {1} wants them unharmed.",
                "Protect the package en route to {0}. {1} has authorized lethal force."
            }
        },
        {
            ContractType.Raid, new[]
            {
                "Infiltrate {0} and destroy the target. {1} wants this done quietly.",
                "Strike {0} and eliminate the objective. {1} needs plausible deniability.",
                "Hit {0} hard and extract. {1} is paying for results, not subtlety."
            }
        },
        {
            ContractType.Heist, new[]
            {
                "Acquire target data from {0} without raising alarms. {1} values discretion.",
                "Extract the package from {0} cleanly. {1} needs zero trace.",
                "Infiltrate {0} and retrieve the objective. {1} prefers no witnesses."
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
    /// Get a random title using System.Random (for compatibility).
    /// </summary>
    public static string GetRandomTitle(ContractType type, Random rng)
    {
        if (!Titles.TryGetValue(type, out var titles))
            titles = Titles[ContractType.Assault];

        return titles[rng.Next(titles.Length)];
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
    /// Get a random description using System.Random (for compatibility).
    /// </summary>
    public static string GetDescription(ContractType type, string targetName, string factionName, int reward, Random rng)
    {
        if (!DescriptionFormats.TryGetValue(type, out var formats))
            formats = DescriptionFormats[ContractType.Assault];

        var format = formats[rng.Next(formats.Length)];
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
