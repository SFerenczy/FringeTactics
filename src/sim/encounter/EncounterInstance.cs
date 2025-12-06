using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// Runtime state of an active encounter.
/// Tracks position within template, accumulated effects, and history.
/// Serializable for save/load mid-encounter.
/// </summary>
public class EncounterInstance
{
    /// <summary>
    /// Reference to the encounter template.
    /// </summary>
    public EncounterTemplate Template { get; set; }

    /// <summary>
    /// Current node ID within the template.
    /// </summary>
    public string CurrentNodeId { get; set; }

    /// <summary>
    /// History of visited node IDs.
    /// </summary>
    public List<string> VisitedNodes { get; set; } = new();

    /// <summary>
    /// Accumulated effects to be applied when encounter completes.
    /// </summary>
    public List<EncounterEffect> PendingEffects { get; set; } = new();

    /// <summary>
    /// Resolved parameters for this instance (NPC names, cargo types, etc.).
    /// </summary>
    public Dictionary<string, string> ResolvedParameters { get; set; } = new();

    /// <summary>
    /// Whether the encounter has completed.
    /// </summary>
    public bool IsComplete { get; set; }

    /// <summary>
    /// Whether the encounter is paused for tactical mission (EN3).
    /// </summary>
    public bool IsPausedForTactical { get; set; }

    /// <summary>
    /// Pending tactical mission ID if paused (EN3).
    /// </summary>
    public string PendingTacticalMissionId { get; set; }

    /// <summary>
    /// Unique instance ID for tracking.
    /// </summary>
    public string InstanceId { get; set; }

    /// <summary>
    /// Create a new encounter instance from a template.
    /// </summary>
    /// <param name="template">The encounter template.</param>
    /// <param name="instanceId">Unique instance ID. Required for determinism.</param>
    public static EncounterInstance Create(EncounterTemplate template, string instanceId)
    {
        if (template == null || !template.IsValid())
        {
            return null;
        }

        if (string.IsNullOrEmpty(instanceId))
        {
            SimLog.Log("[Encounter] Warning: Creating instance without explicit ID");
            instanceId = $"enc_{template.Id}_0";
        }

        var instance = new EncounterInstance
        {
            Template = template,
            CurrentNodeId = template.EntryNodeId,
            InstanceId = instanceId,
            IsComplete = false
        };

        instance.VisitedNodes.Add(template.EntryNodeId);

        return instance;
    }

    /// <summary>
    /// Create a new encounter instance with auto-generated ID from RNG.
    /// </summary>
    public static EncounterInstance Create(EncounterTemplate template, RngStream rng)
    {
        var id = rng != null
            ? $"enc_{template?.Id}_{rng.Next(0, 999999)}"
            : $"enc_{template?.Id}_0";
        return Create(template, id);
    }

    /// <summary>
    /// Get the current node.
    /// </summary>
    public EncounterNode GetCurrentNode()
    {
        if (IsComplete || Template == null) return null;
        return Template.GetNode(CurrentNodeId);
    }

    /// <summary>
    /// Get a resolved parameter value.
    /// </summary>
    public string GetParameter(string key, string defaultValue = null)
    {
        return ResolvedParameters.TryGetValue(key, out var value) ? value : defaultValue;
    }

    /// <summary>
    /// Set a resolved parameter value.
    /// </summary>
    public void SetParameter(string key, string value)
    {
        ResolvedParameters[key] = value;
    }

    /// <summary>
    /// Get total count of pending effects.
    /// </summary>
    public int PendingEffectCount => PendingEffects?.Count ?? 0;

    /// <summary>
    /// Check if this instance has any pending effects of a specific type.
    /// </summary>
    public bool HasPendingEffect(EffectType type)
    {
        if (PendingEffects == null) return false;
        foreach (var effect in PendingEffects)
        {
            if (effect.Type == type) return true;
        }
        return false;
    }

    // === Serialization ===

    /// <summary>
    /// Get serializable state for save/load.
    /// </summary>
    public EncounterInstanceData GetState()
    {
        var effectsData = new List<EncounterEffectData>();
        foreach (var effect in PendingEffects ?? new List<EncounterEffect>())
        {
            effectsData.Add(new EncounterEffectData
            {
                Type = effect.Type.ToString(),
                TargetId = effect.TargetId,
                Amount = effect.Amount,
                StringParam = effect.StringParam,
                BoolParam = effect.BoolParam
            });
        }

        return new EncounterInstanceData
        {
            InstanceId = InstanceId,
            TemplateId = Template?.Id,
            CurrentNodeId = CurrentNodeId,
            VisitedNodes = new List<string>(VisitedNodes ?? new List<string>()),
            PendingEffects = effectsData,
            ResolvedParameters = new Dictionary<string, string>(ResolvedParameters ?? new()),
            IsComplete = IsComplete,
            IsPausedForTactical = IsPausedForTactical,
            PendingTacticalMissionId = PendingTacticalMissionId
        };
    }

    /// <summary>
    /// Restore from saved state. Requires template registry to resolve template.
    /// </summary>
    public static EncounterInstance FromState(EncounterInstanceData data, EncounterTemplate template)
    {
        if (data == null || template == null) return null;

        var effects = new List<EncounterEffect>();
        foreach (var effectData in data.PendingEffects ?? new List<EncounterEffectData>())
        {
            if (System.Enum.TryParse<EffectType>(effectData.Type, out var effectType))
            {
                effects.Add(new EncounterEffect
                {
                    Type = effectType,
                    TargetId = effectData.TargetId,
                    Amount = effectData.Amount,
                    StringParam = effectData.StringParam,
                    BoolParam = effectData.BoolParam
                });
            }
        }

        return new EncounterInstance
        {
            InstanceId = data.InstanceId,
            Template = template,
            CurrentNodeId = data.CurrentNodeId,
            VisitedNodes = new List<string>(data.VisitedNodes ?? new List<string>()),
            PendingEffects = effects,
            ResolvedParameters = new Dictionary<string, string>(data.ResolvedParameters ?? new()),
            IsComplete = data.IsComplete,
            IsPausedForTactical = data.IsPausedForTactical,
            PendingTacticalMissionId = data.PendingTacticalMissionId
        };
    }
}

/// <summary>
/// Serializable data for EncounterInstance.
/// </summary>
public class EncounterInstanceData
{
    public string InstanceId { get; set; }
    public string TemplateId { get; set; }
    public string CurrentNodeId { get; set; }
    public List<string> VisitedNodes { get; set; }
    public List<EncounterEffectData> PendingEffects { get; set; }
    public Dictionary<string, string> ResolvedParameters { get; set; }
    public bool IsComplete { get; set; }
    public bool IsPausedForTactical { get; set; }
    public string PendingTacticalMissionId { get; set; }
}

/// <summary>
/// Serializable data for EncounterEffect.
/// </summary>
public class EncounterEffectData
{
    public string Type { get; set; }
    public string TargetId { get; set; }
    public int Amount { get; set; }
    public string StringParam { get; set; }
    public bool BoolParam { get; set; }
}
