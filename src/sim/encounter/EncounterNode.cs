using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// A single step in an encounter. Contains narrative text and player options.
/// </summary>
public class EncounterNode
{
    /// <summary>
    /// Unique identifier within the template.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Localization key for the narrative text.
    /// </summary>
    public string TextKey { get; set; }

    /// <summary>
    /// Available options at this node.
    /// </summary>
    public List<EncounterOption> Options { get; set; } = new();

    /// <summary>
    /// Automatic transition for narrative-only nodes (no player choice).
    /// If set, the node auto-advances after displaying text.
    /// </summary>
    public EncounterOutcome AutoTransition { get; set; }

    /// <summary>
    /// Whether this node has an automatic transition.
    /// </summary>
    public bool HasAutoTransition => AutoTransition != null;

    /// <summary>
    /// Whether this is an end node (auto-transition that ends encounter).
    /// </summary>
    public bool IsEndNode => AutoTransition?.IsEndEncounter ?? false;

    /// <summary>
    /// Whether this node requires player input.
    /// </summary>
    public bool RequiresInput => !HasAutoTransition && Options.Count > 0;
}
