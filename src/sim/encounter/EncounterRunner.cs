using System.Collections.Generic;
using System.Linq;

namespace FringeTactics;

/// <summary>
/// Stateless service that steps through encounters.
/// All state is held in EncounterInstance.
/// </summary>
public class EncounterRunner
{
    private readonly EventBus eventBus;

    public EncounterRunner() : this(null) { }

    public EncounterRunner(EventBus eventBus)
    {
        this.eventBus = eventBus;
    }

    /// <summary>
    /// Get the current node for an encounter instance.
    /// </summary>
    public EncounterNode GetCurrentNode(EncounterInstance instance)
    {
        if (instance == null || instance.IsComplete) return null;
        return instance.GetCurrentNode();
    }

    /// <summary>
    /// Get available options at the current node, filtered by conditions.
    /// </summary>
    public List<EncounterOption> GetAvailableOptions(EncounterInstance instance, EncounterContext context)
    {
        var node = GetCurrentNode(instance);
        if (node == null) return new List<EncounterOption>();

        var available = new List<EncounterOption>();
        foreach (var option in node.Options ?? new List<EncounterOption>())
        {
            if (EvaluateConditions(option.Conditions, context))
            {
                available.Add(option);
            }
        }
        return available;
    }

    /// <summary>
    /// Select an option and advance the encounter.
    /// </summary>
    public EncounterStepResult SelectOption(EncounterInstance instance, EncounterContext context, int optionIndex)
    {
        if (instance == null || instance.IsComplete)
        {
            return EncounterStepResult.Invalid("Encounter is null or complete");
        }

        var available = GetAvailableOptions(instance, context);
        if (optionIndex < 0 || optionIndex >= available.Count)
        {
            return EncounterStepResult.Invalid($"Invalid option index: {optionIndex}");
        }

        var option = available[optionIndex];
        var currentNodeId = instance.CurrentNodeId;

        // Emit option selected event
        eventBus?.Publish(new EncounterOptionSelectedEvent(
            instance.InstanceId,
            currentNodeId,
            option.Id,
            optionIndex
        ));

        var outcome = ResolveOutcome(option, context);

        if (outcome == null)
        {
            return EncounterStepResult.Invalid("Option has no outcome");
        }

        // Process the outcome
        ProcessOutcome(instance, outcome);

        // Emit node entered event if we transitioned
        if (!instance.IsComplete && instance.CurrentNodeId != currentNodeId)
        {
            var newNode = instance.GetCurrentNode();
            if (newNode != null)
            {
                eventBus?.Publish(new EncounterNodeEnteredEvent(
                    instance.InstanceId,
                    newNode.Id,
                    newNode.RequiresInput
                ));
            }
        }

        // Handle auto-transitions
        ProcessAutoTransitions(instance);

        // Emit completed event if done
        if (instance.IsComplete)
        {
            eventBus?.Publish(new EncounterCompletedEvent(
                instance.InstanceId,
                instance.Template?.Id,
                instance.PendingEffectCount,
                instance.VisitedNodes?.Count ?? 0
            ));
        }

        return EncounterStepResult.Success(instance.CurrentNodeId, instance.IsComplete);
    }

    /// <summary>
    /// Process auto-transitions until we reach a node that requires input or encounter ends.
    /// </summary>
    public void ProcessAutoTransitions(EncounterInstance instance)
    {
        if (instance == null || instance.IsComplete) return;

        var node = instance.GetCurrentNode();
        while (node != null && node.HasAutoTransition && !instance.IsComplete)
        {
            ProcessOutcome(instance, node.AutoTransition);
            node = instance.GetCurrentNode();
        }
    }

    /// <summary>
    /// Check if the encounter is complete.
    /// </summary>
    public bool IsComplete(EncounterInstance instance)
    {
        return instance?.IsComplete ?? true;
    }

    /// <summary>
    /// Get all pending effects from the encounter.
    /// </summary>
    public List<EncounterEffect> GetPendingEffects(EncounterInstance instance)
    {
        return instance?.PendingEffects ?? new List<EncounterEffect>();
    }

    /// <summary>
    /// Start an encounter and process any initial auto-transitions.
    /// </summary>
    public EncounterStepResult Start(EncounterInstance instance)
    {
        if (instance == null)
        {
            return EncounterStepResult.Invalid("Instance is null");
        }

        // Emit start event
        eventBus?.Publish(new EncounterStartedEvent(
            instance.InstanceId,
            instance.Template?.Id,
            instance.Template?.Name
        ));

        // Emit initial node event
        var node = instance.GetCurrentNode();
        if (node != null)
        {
            eventBus?.Publish(new EncounterNodeEnteredEvent(
                instance.InstanceId,
                node.Id,
                node.RequiresInput
            ));
        }

        // Process any auto-transitions from the entry node
        ProcessAutoTransitions(instance);

        // Check if completed immediately
        if (instance.IsComplete)
        {
            eventBus?.Publish(new EncounterCompletedEvent(
                instance.InstanceId,
                instance.Template?.Id,
                instance.PendingEffectCount,
                instance.VisitedNodes?.Count ?? 0
            ));
        }

        return EncounterStepResult.Success(instance.CurrentNodeId, instance.IsComplete);
    }

    // === Private Methods ===

    private EncounterOutcome ResolveOutcome(EncounterOption option, EncounterContext context)
    {
        if (option == null) return null;

        // EN2 will add skill check resolution here
        // For now, just return the direct outcome
        if (option.HasSkillCheck)
        {
            // Stub: always succeed for EN1
            return option.SuccessOutcome ?? option.Outcome;
        }

        return option.Outcome;
    }

    private void ProcessOutcome(EncounterInstance instance, EncounterOutcome outcome)
    {
        if (instance == null || outcome == null) return;

        // Accumulate effects (except flow effects which are handled specially)
        foreach (var effect in outcome.Effects ?? new List<EncounterEffect>())
        {
            switch (effect.Type)
            {
                case EffectType.GotoNode:
                    // Transition to the specified node
                    TransitionToNode(instance, effect.TargetId);
                    break;

                case EffectType.EndEncounter:
                    // End the encounter
                    instance.IsComplete = true;
                    break;

                case EffectType.TriggerTactical:
                    // Pause for tactical (EN3)
                    instance.IsPausedForTactical = true;
                    instance.PendingTacticalMissionId = effect.StringParam;
                    instance.PendingEffects.Add(effect);
                    break;

                default:
                    // Accumulate other effects for later application
                    instance.PendingEffects.Add(effect);
                    break;
            }
        }

        // Handle next node transition
        if (!instance.IsComplete && !string.IsNullOrEmpty(outcome.NextNodeId))
        {
            TransitionToNode(instance, outcome.NextNodeId);
        }

        // Handle end encounter flag
        if (outcome.IsEndEncounter)
        {
            instance.IsComplete = true;
        }
    }

    private void TransitionToNode(EncounterInstance instance, string nodeId)
    {
        if (instance == null || string.IsNullOrEmpty(nodeId)) return;

        // Verify node exists
        var node = instance.Template?.GetNode(nodeId);
        if (node == null)
        {
            SimLog.Log($"[Encounter] Warning: Node '{nodeId}' not found in template");
            return;
        }

        instance.CurrentNodeId = nodeId;
        instance.VisitedNodes.Add(nodeId);
    }

    private bool EvaluateConditions(List<EncounterCondition> conditions, EncounterContext context)
    {
        if (conditions == null || conditions.Count == 0) return true;
        return conditions.All(c => c.Evaluate(context));
    }
}
