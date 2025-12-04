using Godot;
using System;
using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// Handles all input for the mission view and translates it into high-level commands.
/// Owns selection state, ability targeting state, and box selection state.
/// </summary>
public partial class MissionInputController : Node
{
    private const float DragThreshold = 5f;
    private const float DoubleClickThreshold = 0.3f;

    private CombatState combatState;
    private SelectionManager selection;
    private List<int> crewActorIds;
    private int tileSize;
    private Func<Vector2, Vector2> screenToWorld;

    // Ability targeting state
    private AbilityData pendingAbility = null;
    public bool IsTargetingAbility => pendingAbility != null;
    public AbilityData PendingAbility => pendingAbility;

    // Box selection state
    private bool isDragSelecting = false;
    private Vector2 dragStartScreen;
    private Vector2 dragStartWorld;

    // Double-click detection
    private float lastClickTime = 0f;
    private int lastClickedActorId = -1;

    // Events for MissionView to handle
    public event Action<IReadOnlyList<int>> SelectionChanged;
    public event Action<int> ActorSelected;
    public event Action<int, Vector2I> MoveOrderIssued;
    public event Action<int, int> AttackOrderIssued;
    public event Action<int, int> InteractionOrderIssued;
    public event Action<int> ReloadOrderIssued;
    public event Action<AbilityData> AbilityTargetingStarted;
    public event Action AbilityTargetingCancelled;
    public event Action<int, AbilityData, Vector2I> AbilityOrderIssued;
    public event Action<Rect2, bool> BoxSelectionUpdated; // rect in world coords, isActive
    public event Action<int> ControlGroupSaved;
    public event Action<int> ControlGroupRecalled;
    public event Action ToggleVisibilityDebug;
    public event Action<int> CenterOnActor;

    public IReadOnlyList<int> SelectedActorIds => selection.SelectedActorIds;

    public void Initialize(
        CombatState combatState,
        List<int> crewActorIds,
        int tileSize,
        Func<Vector2, Vector2> screenToWorld)
    {
        this.combatState = combatState;
        this.crewActorIds = crewActorIds;
        this.tileSize = tileSize;
        this.screenToWorld = screenToWorld;

        selection = new SelectionManager(IsValidCrewSelection);
        selection.SelectionChanged += OnSelectionChanged;
    }

    private bool IsValidCrewSelection(int actorId)
    {
        var actor = combatState.GetActorById(actorId);
        return actor != null && actor.Type == ActorType.Enemy && actor.State == ActorState.Alive;
    }

    private void OnSelectionChanged(IReadOnlyList<int> selected)
    {
        SelectionChanged?.Invoke(selected);
    }

    public override void _Input(InputEvent @event)
    {
        if (combatState == null)
        {
            return;
        }

        // Pause toggle
        if (@event.IsActionPressed("pause_toggle"))
        {
            combatState.TimeSystem.TogglePause();
            return;
        }

        // Key events
        if (@event is InputEventKey keyEvent && keyEvent.Pressed)
        {
            HandleKeyPress(keyEvent);
            return;
        }

        // Mouse events
        if (@event is InputEventMouseButton mouseEvent)
        {
            HandleMouseButton(mouseEvent);
        }
    }

    private void HandleKeyPress(InputEventKey keyEvent)
    {
        bool ctrlHeld = Input.IsKeyPressed(Key.Ctrl);

        // Number keys for control groups
        int groupNum = keyEvent.Keycode switch
        {
            Key.Key1 => 1,
            Key.Key2 => 2,
            Key.Key3 => 3,
            _ => -1
        };

        if (groupNum > 0)
        {
            if (ctrlHeld)
            {
                selection.SaveControlGroup(groupNum);
                ControlGroupSaved?.Invoke(groupNum);
                GD.Print($"[ControlGroup] Saved {selection.SelectedActorIds.Count} units to group {groupNum}");
            }
            else
            {
                if (selection.RecallControlGroup(groupNum))
                {
                    ControlGroupRecalled?.Invoke(groupNum);
                    GD.Print($"[ControlGroup] Recalled group {groupNum}: {selection.SelectedActorIds.Count} units");
                }
                else if (groupNum - 1 < crewActorIds.Count)
                {
                    // Fallback: select crew by index
                    selection.Select(crewActorIds[groupNum - 1]);
                    ActorSelected?.Invoke(crewActorIds[groupNum - 1]);
                }
            }
            return;
        }

        // Tab - select all crew
        if (keyEvent.Keycode == Key.Tab)
        {
            SelectAllCrew();
            return;
        }

        // F3 - toggle visibility debug
        if (keyEvent.Keycode == Key.F3)
        {
            ToggleVisibilityDebug?.Invoke();
            return;
        }

        // G - grenade
        if (keyEvent.Keycode == Key.G)
        {
            var grenade = Definitions.Abilities.Get("frag_grenade")?.ToAbilityData();
            if (grenade != null)
            {
                StartAbilityTargeting(grenade);
            }
            return;
        }

        // R - reload
        if (keyEvent.Keycode == Key.R)
        {
            foreach (var actorId in selection.SelectedActorIds)
            {
                ReloadOrderIssued?.Invoke(actorId);
            }
            return;
        }

        // Escape - cancel targeting
        if (keyEvent.Keycode == Key.Escape)
        {
            CancelAbilityTargeting();
            return;
        }

        // C - center on selected unit
        if (keyEvent.Keycode == Key.C && selection.SelectedActorIds.Count > 0)
        {
            CenterOnActor?.Invoke(selection.SelectedActorIds[0]);
            return;
        }
    }

    private void HandleMouseButton(InputEventMouseButton mouseEvent)
    {
        var worldPos = screenToWorld(mouseEvent.Position);
        var gridPos = WorldToGrid(worldPos);

        // Ability targeting mode
        if (pendingAbility != null)
        {
            if (mouseEvent.Pressed)
            {
                if (mouseEvent.ButtonIndex == MouseButton.Left)
                {
                    ConfirmAbilityTarget(gridPos);
                }
                else if (mouseEvent.ButtonIndex == MouseButton.Right)
                {
                    CancelAbilityTargeting();
                }
            }
            return;
        }

        // Normal input
        if (mouseEvent.ButtonIndex == MouseButton.Left)
        {
            if (mouseEvent.Pressed)
            {
                StartPotentialDrag(mouseEvent.Position, worldPos);
            }
            else
            {
                FinishLeftClick(mouseEvent.Position, gridPos);
            }
        }
        else if (mouseEvent.ButtonIndex == MouseButton.Right && mouseEvent.Pressed)
        {
            HandleRightClick(gridPos);
        }
    }

    private void StartPotentialDrag(Vector2 screenPos, Vector2 worldPos)
    {
        dragStartScreen = screenPos;
        dragStartWorld = worldPos;
        isDragSelecting = false;
    }

    public void UpdateDragSelection(Vector2 currentScreenPos, Vector2 currentWorldPos)
    {
        if (!Input.IsMouseButtonPressed(MouseButton.Left) || pendingAbility != null)
        {
            if (isDragSelecting)
            {
                isDragSelecting = false;
                BoxSelectionUpdated?.Invoke(new Rect2(), false);
            }
            return;
        }

        var distance = (currentScreenPos - dragStartScreen).Length();

        if (!isDragSelecting && distance > DragThreshold)
        {
            isDragSelecting = true;
        }

        if (isDragSelecting)
        {
            var rect = CreateRect(dragStartWorld, currentWorldPos);
            BoxSelectionUpdated?.Invoke(rect, true);
        }
    }

    private void FinishLeftClick(Vector2 screenPos, Vector2I gridPos)
    {
        if (isDragSelecting)
        {
            FinishBoxSelection(screenPos);
        }
        else
        {
            bool shiftHeld = Input.IsKeyPressed(Key.Shift);
            HandleSelection(gridPos, shiftHeld);
        }
    }

    private void FinishBoxSelection(Vector2 endScreen)
    {
        isDragSelecting = false;
        BoxSelectionUpdated?.Invoke(new Rect2(), false);

        var endWorld = screenToWorld(endScreen);
        var rect = CreateRect(dragStartWorld, endWorld);

        bool shiftHeld = Input.IsKeyPressed(Key.Shift);
        var actorsInBox = new List<int>();

        foreach (var actor in combatState.Actors)
        {
            if (actor.Type != ActorType.Enemy || actor.State != ActorState.Alive)
            {
                continue;
            }

            var actorWorldPos = actor.GetVisualPosition(tileSize);
            var actorCenter = actorWorldPos + new Vector2(tileSize / 2f, tileSize / 2f);

            if (rect.HasPoint(actorCenter))
            {
                actorsInBox.Add(actor.Id);
            }
        }

        if (actorsInBox.Count > 0)
        {
            selection.SelectMultiple(actorsInBox, shiftHeld);
            GD.Print($"[Selection] Box selected {actorsInBox.Count} units, total: {selection.SelectedActorIds.Count}");
        }
        else if (!shiftHeld)
        {
            selection.ClearSelection();
        }
    }

    private void HandleSelection(Vector2I gridPos, bool additive)
    {
        var clickedActor = combatState.GetActorAtPosition(gridPos);
        float currentTime = Time.GetTicksMsec() / 1000f;

        if (clickedActor != null && clickedActor.Type == ActorType.Enemy && clickedActor.State == ActorState.Alive)
        {
            // Double-click detection
            if (clickedActor.Id == lastClickedActorId &&
                currentTime - lastClickTime < DoubleClickThreshold)
            {
                SelectAllCrew();
                lastClickedActorId = -1;
                return;
            }

            lastClickTime = currentTime;
            lastClickedActorId = clickedActor.Id;

            if (additive)
            {
                selection.ToggleSelection(clickedActor.Id);
            }
            else
            {
                selection.Select(clickedActor.Id);
                ActorSelected?.Invoke(clickedActor.Id);
            }
        }
        else if (!additive)
        {
            selection.ClearSelection();
            lastClickedActorId = -1;
        }
    }

    private void HandleRightClick(Vector2I gridPos)
    {
        if (selection.SelectedActorIds.Count == 0)
        {
            return;
        }

        // Check for interactable
        var interactable = combatState.Interactions.GetInteractableAt(gridPos);
        if (interactable != null)
        {
            if (TryInteractWith(interactable))
            {
                return;
            }
        }

        // Check for enemy target
        var targetActor = combatState.GetActorAtPosition(gridPos);
        if (targetActor != null && targetActor.State == ActorState.Alive)
        {
            if (!combatState.Visibility.IsVisible(targetActor.GridPosition))
            {
                IssueGroupMoveOrder(gridPos);
                return;
            }

            bool isEnemy = false;
            foreach (var actorId in selection.SelectedActorIds)
            {
                var selected = combatState.GetActorById(actorId);
                if (selected != null && selected.Type != targetActor.Type)
                {
                    isEnemy = true;
                    break;
                }
            }

            if (isEnemy)
            {
                foreach (var actorId in selection.SelectedActorIds)
                {
                    AttackOrderIssued?.Invoke(actorId, targetActor.Id);
                }
                return;
            }
        }

        // Default: move order
        IssueGroupMoveOrder(gridPos);
    }

    private bool TryInteractWith(Interactable interactable)
    {
        Actor bestActor = null;
        float bestDistance = float.MaxValue;

        foreach (var actorId in selection.SelectedActorIds)
        {
            var actor = combatState.GetActorById(actorId);
            if (actor == null || actor.State != ActorState.Alive)
            {
                continue;
            }

            if (!combatState.Interactions.CanInteract(actor, interactable))
            {
                continue;
            }

            var distance = CombatResolver.GetDistance(actor.GridPosition, interactable.Position);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestActor = actor;
            }
        }

        if (bestActor == null)
        {
            return false;
        }

        InteractionOrderIssued?.Invoke(bestActor.Id, interactable.Id);
        GD.Print($"[Interaction] {bestActor.Type}#{bestActor.Id} interacting with {interactable.Type}#{interactable.Id}");
        return true;
    }

    private void IssueGroupMoveOrder(Vector2I targetPos)
    {
        var selectedActors = new List<Actor>();
        foreach (var actorId in selection.SelectedActorIds)
        {
            var actor = combatState.GetActorById(actorId);
            if (actor != null && actor.State == ActorState.Alive)
            {
                selectedActors.Add(actor);
            }
        }

        if (selectedActors.Count == 0)
        {
            return;
        }

        var destinations = FormationCalculator.CalculateGroupDestinations(
            selectedActors,
            targetPos,
            combatState.MapState
        );

        foreach (var kvp in destinations)
        {
            MoveOrderIssued?.Invoke(kvp.Key, kvp.Value);
        }

        GD.Print($"[Movement] Group move: {selectedActors.Count} units to formation around {targetPos}");
    }

    private void SelectAllCrew()
    {
        selection.SelectMultiple(crewActorIds, additive: false);
        GD.Print($"[Selection] Selected all {selection.SelectedActorIds.Count} crew");
    }

    // === Ability Targeting ===

    public void StartAbilityTargeting(AbilityData ability)
    {
        if (selection.SelectedActorIds.Count != 1)
        {
            GD.Print("[Ability] Select exactly one crew member to use ability");
            return;
        }

        var actorId = selection.SelectedActorIds[0];
        var actor = combatState.GetActorById(actorId);
        if (actor == null || actor.Type != ActorType.Enemy)
        {
            GD.Print("[Ability] Only crew can use abilities");
            return;
        }

        if (combatState.AbilitySystem.IsOnCooldown(actorId, ability.Id))
        {
            var remaining = combatState.AbilitySystem.GetCooldownRemaining(actorId, ability.Id);
            GD.Print($"[Ability] {ability.Name} on cooldown: {remaining} ticks remaining");
            return;
        }

        pendingAbility = ability;
        AbilityTargetingStarted?.Invoke(ability);
        GD.Print($"[Ability] Targeting {ability.Name} - click to select target tile");
    }

    private void ConfirmAbilityTarget(Vector2I targetTile)
    {
        if (pendingAbility == null || selection.SelectedActorIds.Count == 0)
        {
            CancelAbilityTargeting();
            return;
        }

        var actorId = selection.SelectedActorIds[0];
        AbilityOrderIssued?.Invoke(actorId, pendingAbility, targetTile);
        CancelAbilityTargeting();
    }

    public void CancelAbilityTargeting()
    {
        if (pendingAbility != null)
        {
            pendingAbility = null;
            AbilityTargetingCancelled?.Invoke();
        }
    }

    // === Helpers ===

    private Vector2I WorldToGrid(Vector2 worldPos)
    {
        return new Vector2I(
            (int)(worldPos.X) / tileSize,
            (int)(worldPos.Y) / tileSize
        );
    }

    private static Rect2 CreateRect(Vector2 a, Vector2 b)
    {
        return new Rect2(
            Mathf.Min(a.X, b.X),
            Mathf.Min(a.Y, b.Y),
            Mathf.Abs(b.X - a.X),
            Mathf.Abs(b.Y - a.Y)
        );
    }

    public void ClearSelection()
    {
        selection.ClearSelection();
    }

    public void Select(int actorId)
    {
        selection.Select(actorId);
    }
}
