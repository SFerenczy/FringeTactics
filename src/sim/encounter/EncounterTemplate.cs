using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// Complete encounter definition. Data-driven template with no behavior.
/// </summary>
public class EncounterTemplate
{
    /// <summary>
    /// Unique identifier for this template.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Display name for the encounter.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Tags for encounter selection (e.g., "pirate", "patrol", "travel", "station").
    /// </summary>
    public HashSet<string> Tags { get; set; } = new();

    /// <summary>
    /// ID of the entry node where the encounter starts.
    /// </summary>
    public string EntryNodeId { get; set; }

    /// <summary>
    /// All nodes in this encounter, keyed by node ID.
    /// </summary>
    public Dictionary<string, EncounterNode> Nodes { get; set; } = new();

    /// <summary>
    /// Context keys required for this encounter (for validation).
    /// </summary>
    public List<string> RequiredContextKeys { get; set; } = new();

    /// <summary>
    /// Get a node by ID.
    /// </summary>
    public EncounterNode GetNode(string nodeId)
    {
        if (string.IsNullOrEmpty(nodeId)) return null;
        return Nodes.TryGetValue(nodeId, out var node) ? node : null;
    }

    /// <summary>
    /// Get the entry node.
    /// </summary>
    public EncounterNode GetEntryNode() => GetNode(EntryNodeId);

    /// <summary>
    /// Check if this template has a specific tag.
    /// </summary>
    public bool HasTag(string tag) => Tags?.Contains(tag) ?? false;

    /// <summary>
    /// Validate the template structure.
    /// </summary>
    public bool IsValid()
    {
        if (string.IsNullOrEmpty(Id)) return false;
        if (string.IsNullOrEmpty(EntryNodeId)) return false;
        if (Nodes == null || Nodes.Count == 0) return false;
        if (!Nodes.ContainsKey(EntryNodeId)) return false;
        return true;
    }
}
