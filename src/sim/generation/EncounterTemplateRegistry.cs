using System.Collections.Generic;
using System.Linq;

namespace FringeTactics;

/// <summary>
/// Central registry for encounter templates.
/// Provides lookup by ID, tag filtering, and eligibility checking based on TravelContext.
/// </summary>
public class EncounterTemplateRegistry
{
    private readonly Dictionary<string, EncounterTemplate> templates = new();

    /// <summary>
    /// Register a template. Overwrites if ID already exists.
    /// </summary>
    public void Register(EncounterTemplate template)
    {
        if (template == null || string.IsNullOrEmpty(template.Id)) return;
        templates[template.Id] = template;
    }

    /// <summary>
    /// Register multiple templates.
    /// </summary>
    public void RegisterAll(IEnumerable<EncounterTemplate> templateList)
    {
        if (templateList == null) return;
        foreach (var template in templateList)
        {
            Register(template);
        }
    }

    /// <summary>
    /// Get template by ID. Returns null if not found.
    /// </summary>
    public EncounterTemplate Get(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        return templates.TryGetValue(id, out var template) ? template : null;
    }

    /// <summary>
    /// Check if a template with the given ID exists.
    /// </summary>
    public bool Contains(string id)
    {
        if (string.IsNullOrEmpty(id)) return false;
        return templates.ContainsKey(id);
    }

    /// <summary>
    /// Get all templates with a specific tag.
    /// </summary>
    public IEnumerable<EncounterTemplate> GetByTag(string tag)
    {
        if (string.IsNullOrEmpty(tag)) return Enumerable.Empty<EncounterTemplate>();
        return templates.Values.Where(t => t.HasTag(tag));
    }

    /// <summary>
    /// Get all templates matching any of the given tags.
    /// </summary>
    public IEnumerable<EncounterTemplate> GetByAnyTag(IEnumerable<string> tags)
    {
        if (tags == null) return Enumerable.Empty<EncounterTemplate>();
        var tagSet = new HashSet<string>(tags);
        if (tagSet.Count == 0) return Enumerable.Empty<EncounterTemplate>();
        return templates.Values.Where(t => t.Tags != null && t.Tags.Overlaps(tagSet));
    }

    /// <summary>
    /// Get all templates matching all of the given tags.
    /// </summary>
    public IEnumerable<EncounterTemplate> GetByAllTags(IEnumerable<string> tags)
    {
        if (tags == null) return Enumerable.Empty<EncounterTemplate>();
        var tagSet = new HashSet<string>(tags);
        if (tagSet.Count == 0) return templates.Values;
        return templates.Values.Where(t => t.Tags != null && tagSet.IsSubsetOf(t.Tags));
    }

    /// <summary>
    /// Get templates eligible for the given travel context.
    /// Filters by travel tag and suggested encounter type.
    /// </summary>
    public IEnumerable<EncounterTemplate> GetEligible(TravelContext context)
    {
        if (context == null) return Enumerable.Empty<EncounterTemplate>();
        return templates.Values.Where(t => IsEligible(t, context));
    }

    /// <summary>
    /// Check if a template is eligible for the given context.
    /// </summary>
    private bool IsEligible(EncounterTemplate template, TravelContext context)
    {
        if (template == null || !template.IsValid()) return false;

        // Must have "travel" tag for travel encounters
        if (!template.HasTag(EncounterTags.Travel)) return false;

        // Check suggested type match (if specified and not random)
        if (!string.IsNullOrEmpty(context.SuggestedEncounterType) &&
            context.SuggestedEncounterType != EncounterTypes.Random)
        {
            // Template should match suggested type OR be generic
            if (!template.HasTag(context.SuggestedEncounterType) &&
                !template.HasTag(EncounterTags.Generic))
            {
                return false;
            }
        }

        // Check system tag requirements
        foreach (var requiredKey in template.RequiredContextKeys ?? Enumerable.Empty<string>())
        {
            if (requiredKey.StartsWith("system_tag:"))
            {
                var tag = requiredKey.Substring("system_tag:".Length);
                if (context.SystemTags == null || !context.SystemTags.Contains(tag))
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Get count of registered templates.
    /// </summary>
    public int Count => templates.Count;

    /// <summary>
    /// Get all template IDs.
    /// </summary>
    public IEnumerable<string> GetAllIds() => templates.Keys;

    /// <summary>
    /// Get all registered templates.
    /// </summary>
    public IEnumerable<EncounterTemplate> GetAll() => templates.Values;

    /// <summary>
    /// Remove a template by ID.
    /// </summary>
    public bool Remove(string id)
    {
        if (string.IsNullOrEmpty(id)) return false;
        return templates.Remove(id);
    }

    /// <summary>
    /// Clear all templates.
    /// </summary>
    public void Clear() => templates.Clear();

    /// <summary>
    /// Create an empty registry.
    /// </summary>
    public static EncounterTemplateRegistry Create() => new();

    /// <summary>
    /// Create registry with production templates for gameplay.
    /// </summary>
    public static EncounterTemplateRegistry CreateDefault()
    {
        var registry = new EncounterTemplateRegistry();
        registry.RegisterAll(ProductionEncounters.GetAllTemplates());
        return registry;
    }

    /// <summary>
    /// Create registry with test templates only (for unit testing).
    /// </summary>
    public static EncounterTemplateRegistry CreateForTesting()
    {
        var registry = new EncounterTemplateRegistry();
        registry.Register(TestEncounters.CreateSimpleEncounter());
        registry.Register(TestEncounters.CreateConditionalEncounter());
        registry.Register(TestEncounters.CreateBranchingEncounter());
        registry.Register(TestEncounters.CreatePirateAmbush());
        return registry;
    }
}
