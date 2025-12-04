using System;
using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// Manages selection state for tactical combat.
/// Owns the list of selected actor IDs and control groups.
/// </summary>
public class SelectionManager
{
    private readonly List<int> selectedActorIds = new();
    private readonly Dictionary<int, List<int>> controlGroups = new();
    private readonly Func<int, bool> isValidSelection;

    public IReadOnlyList<int> SelectedActorIds => selectedActorIds;

    public event Action<IReadOnlyList<int>> SelectionChanged;

    /// <summary>
    /// Create a SelectionManager with a validation function.
    /// </summary>
    /// <param name="isValidSelection">Returns true if an actor ID can be selected (alive crew member).</param>
    public SelectionManager(Func<int, bool> isValidSelection)
    {
        this.isValidSelection = isValidSelection;
    }

    public void Select(int actorId)
    {
        if (!isValidSelection(actorId))
        {
            return;
        }

        selectedActorIds.Clear();
        selectedActorIds.Add(actorId);
        SelectionChanged?.Invoke(selectedActorIds);
    }

    public void AddToSelection(int actorId)
    {
        if (!isValidSelection(actorId))
        {
            return;
        }

        if (selectedActorIds.Contains(actorId))
        {
            return;
        }

        selectedActorIds.Add(actorId);
        SelectionChanged?.Invoke(selectedActorIds);
    }

    public void RemoveFromSelection(int actorId)
    {
        if (selectedActorIds.Remove(actorId))
        {
            SelectionChanged?.Invoke(selectedActorIds);
        }
    }

    public void ToggleSelection(int actorId)
    {
        if (selectedActorIds.Contains(actorId))
        {
            RemoveFromSelection(actorId);
        }
        else
        {
            AddToSelection(actorId);
        }
    }

    public void ClearSelection()
    {
        if (selectedActorIds.Count == 0)
        {
            return;
        }

        selectedActorIds.Clear();
        SelectionChanged?.Invoke(selectedActorIds);
    }

    public void SelectMultiple(IEnumerable<int> actorIds, bool additive)
    {
        if (!additive)
        {
            selectedActorIds.Clear();
        }

        foreach (var actorId in actorIds)
        {
            if (isValidSelection(actorId) && !selectedActorIds.Contains(actorId))
            {
                selectedActorIds.Add(actorId);
            }
        }

        SelectionChanged?.Invoke(selectedActorIds);
    }

    public bool IsSelected(int actorId)
    {
        return selectedActorIds.Contains(actorId);
    }

    public void SaveControlGroup(int groupNumber)
    {
        if (selectedActorIds.Count == 0)
        {
            return;
        }

        controlGroups[groupNumber] = new List<int>(selectedActorIds);
    }

    public bool RecallControlGroup(int groupNumber)
    {
        if (!controlGroups.TryGetValue(groupNumber, out var actorIds) || actorIds.Count == 0)
        {
            return false;
        }

        selectedActorIds.Clear();
        foreach (var actorId in actorIds)
        {
            if (isValidSelection(actorId))
            {
                selectedActorIds.Add(actorId);
            }
        }

        SelectionChanged?.Invoke(selectedActorIds);
        return selectedActorIds.Count > 0;
    }

    public bool HasControlGroup(int groupNumber)
    {
        return controlGroups.TryGetValue(groupNumber, out var ids) && ids.Count > 0;
    }
}
