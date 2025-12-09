using Godot;
using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// Main tactical combat view. Orchestrates sub-components for grid rendering,
/// fog of war, actor management, and UI elements.
/// </summary>
public partial class MissionView : Node2D
{
    public const int TileSize = GridConstants.TileSize;
    private const string TimeStateWidgetScenePath = "res://src/scenes/mission/TimeStateWidget.tscn";

    private PackedScene timeStateWidgetScene;

    public CombatState CombatState { get; private set; }

    // Sub-components
    private MissionInputController inputController;
    private GridRenderer gridRenderer;
    private FogOfWarLayer fogOfWarLayer;
    private VisibilityDebugOverlay visibilityDebugOverlay;
    private ActorViewManager actorViewManager;
    private InteractableViewManager interactableViewManager;
    private MoveTargetMarker moveTargetMarker;
    private CoverIndicator coverIndicator;
    private MissionEndPanel missionEndPanel;
    private RetreatUIController retreatUIController;

    // UI elements that remain in MissionView
    private Label abilityTargetingLabel;
    private ColorRect selectionBox;
    private Label alarmNotificationLabel;
    private float alarmNotificationTimer = 0f;
    private const float AlarmNotificationDuration = 3.0f;
    private VBoxContainer leftPanelContainer;
    private AlarmStateWidget alarmStateWidget;

    // Node references
    private Node2D gridDisplay;
    private CanvasLayer uiLayer;
    private Label instructionsLabel;
    private TacticalCamera tacticalCamera;

    private TimeStateWidget timeStateWidget;
    private PhaseWidget phaseWidget;
    
    // Wave announcement
    private Label waveAnnouncementLabel;
    private float waveAnnouncementTimer = 0f;
    private const float WaveAnnouncementDuration = 3.0f;

    // Cover indicator position tracking
    private Dictionary<int, Vector2I> lastKnownPositions = new();

    public override void _Ready()
    {
        timeStateWidgetScene = GD.Load<PackedScene>(TimeStateWidgetScenePath);

        gridDisplay = GetNode<Node2D>("GridDisplay");
        uiLayer = GetNode<CanvasLayer>("UI");
        instructionsLabel = GetNode<Label>("UI/InstructionsLabel");
        tacticalCamera = GetNode<TacticalCamera>("TacticalCamera");

        InitializeCombat();
        SetupSubComponents();
        SetupInputController();
        SetupUI();
        SetupCamera();
    }

    private void InitializeCombat()
    {
        CombatState = GameState.Instance?.CurrentCombat;

        if (CombatState == null)
        {
            GD.PrintErr("[MissionView] No CurrentCombat found! Creating fallback sandbox.");
            var config = MissionConfig.CreateTestMission();
            CombatState = MissionFactory.BuildSandbox(config);
        }

        CombatState.MissionEnded += OnMissionEnded;
        CombatState.Perception.AlarmStateChanged += OnAlarmStateChanged;
        CombatState.Perception.EnemyDetectedCrew += OnEnemyDetectedCrew;
        CombatState.OverwatchSystem.ReactionFired += OnReactionFired;
        CombatState.Waves.WaveTriggered += OnWaveTriggered;
        CombatState.Phases.PhaseChanged += OnPhaseChanged;
    }

    private void SetupSubComponents()
    {
        // Grid renderer
        gridRenderer = new GridRenderer();
        gridRenderer.Name = "GridRenderer";
        gridDisplay.AddChild(gridRenderer);
        gridRenderer.Initialize(CombatState.MapState, TileSize);

        // Fog of war
        fogOfWarLayer = new FogOfWarLayer();
        gridDisplay.AddChild(fogOfWarLayer);
        fogOfWarLayer.Initialize(CombatState.MapState, CombatState.Visibility, TileSize);

        // Visibility debug overlay
        visibilityDebugOverlay = new VisibilityDebugOverlay();
        gridDisplay.AddChild(visibilityDebugOverlay);
        visibilityDebugOverlay.Initialize(CombatState.MapState, CombatState.Visibility, TileSize);

        // Interactable views
        interactableViewManager = new InteractableViewManager();
        gridDisplay.AddChild(interactableViewManager);
        interactableViewManager.Initialize(CombatState);

        // Cover indicator
        coverIndicator = new CoverIndicator();
        coverIndicator.Name = "CoverIndicator";
        coverIndicator.ZIndex = 3;
        gridDisplay.AddChild(coverIndicator);
        coverIndicator.Initialize(CombatState.MapState);

        // Move target marker
        moveTargetMarker = new MoveTargetMarker();
        moveTargetMarker.Name = "MoveTargetMarker";
        gridDisplay.AddChild(moveTargetMarker);
        moveTargetMarker.Initialize(CombatState, TileSize);

        // Actor views - must be after grid so actors render on top
        actorViewManager = new ActorViewManager();
        AddChild(actorViewManager);
        actorViewManager.Initialize(CombatState);
    }

    private void SetupInputController()
    {
        inputController = new MissionInputController();
        inputController.Name = "InputController";
        AddChild(inputController);

        inputController.Initialize(
            CombatState,
            new List<int>(actorViewManager.CrewActorIds),
            TileSize,
            ScreenToWorld
        );

        inputController.SelectionChanged += OnSelectionChanged;
        inputController.ActorSelected += OnActorSelected;
        inputController.MoveOrderIssued += OnMoveOrderIssued;
        inputController.AttackOrderIssued += OnAttackOrderIssued;
        inputController.InteractionOrderIssued += OnInteractionOrderIssued;
        inputController.ReloadOrderIssued += OnReloadOrderIssued;
        inputController.OverwatchOrderIssued += OnOverwatchOrderIssued;
        inputController.AbilityTargetingStarted += OnAbilityTargetingStarted;
        inputController.AbilityTargetingCancelled += OnAbilityTargetingCancelled;
        inputController.AbilityOrderIssued += OnAbilityOrderIssued;
        inputController.BoxSelectionUpdated += OnBoxSelectionUpdated;
        inputController.ToggleVisibilityDebug += OnToggleVisibilityDebug;
        inputController.CenterOnActor += OnCenterOnActor;
    }

    private Vector2 ScreenToWorld(Vector2 screenPos)
    {
        return GetCanvasTransform().AffineInverse() * screenPos;
    }

    private void SetupCamera()
    {
        var gridSize = CombatState.MapState.GridSize;
        tacticalCamera.SetMapBoundsFromGrid(gridSize, TileSize);
        tacticalCamera.CenterOnMap();
    }

    private void SetupUI()
    {
        // Time state widget
        timeStateWidget = timeStateWidgetScene.Instantiate<TimeStateWidget>();
        timeStateWidget.Position = new Vector2(10, 10);
        uiLayer.AddChild(timeStateWidget);
        timeStateWidget.ConnectToTimeSystem(CombatState.TimeSystem);

        // Instructions
        instructionsLabel.Text = "Space: Pause/Resume | G: Grenade | O: Overwatch | Scroll: Zoom | WASD: Pan | F3: Debug\n1-3: Select/Recall group | Ctrl+1-3: Save group | Tab: Select all\nClick: Select | Shift+Click: Add/Remove | Drag: Box | DblClick: All\nRClick: Move/Attack | R: Reload | C: Center on unit";

        // Ability targeting label
        abilityTargetingLabel = new Label();
        abilityTargetingLabel.Position = new Vector2(10, 60);
        abilityTargetingLabel.AddThemeColorOverride("font_color", Colors.Yellow);
        abilityTargetingLabel.AddThemeFontSizeOverride("font_size", 16);
        abilityTargetingLabel.Visible = false;
        uiLayer.AddChild(abilityTargetingLabel);

        CombatState.AbilitySystem.AbilityDetonated += OnAbilityDetonated;

        // Selection box
        selectionBox = new ColorRect();
        selectionBox.Color = new Color(0.3f, 0.6f, 1.0f, 0.3f);
        selectionBox.Visible = false;
        selectionBox.ZIndex = 100;
        AddChild(selectionBox);

        // Mission end panel
        missionEndPanel = new MissionEndPanel();
        missionEndPanel.Name = "MissionEndPanel";
        uiLayer.AddChild(missionEndPanel);
        missionEndPanel.RestartRequested += OnRestartRequested;
        missionEndPanel.ContinueRequested += OnContinueRequested;

        // Alarm notification
        CreateAlarmNotificationLabel();
        CreateLeftPanelContainer();
        CreateAlarmStateWidget();

        // Retreat UI
        retreatUIController = new RetreatUIController();
        retreatUIController.Name = "RetreatUIController";
        AddChild(retreatUIController);
        retreatUIController.Initialize(CombatState, uiLayer, gridDisplay, TileSize);
        
        // Phase widget (HH3)
        phaseWidget = new PhaseWidget();
        phaseWidget.Position = new Vector2(10, 75);
        uiLayer.AddChild(phaseWidget);
        phaseWidget.ConnectToPhaseSystem(CombatState.Phases);
        
        // Wave announcement label (HH3)
        CreateWaveAnnouncementLabel();
    }
    
    private void CreateWaveAnnouncementLabel()
    {
        waveAnnouncementLabel = new Label();
        waveAnnouncementLabel.HorizontalAlignment = HorizontalAlignment.Center;
        waveAnnouncementLabel.AddThemeFontSizeOverride("font_size", 28);
        waveAnnouncementLabel.AddThemeColorOverride("font_color", new Color(1.0f, 0.4f, 0.2f));
        waveAnnouncementLabel.Visible = false;
        waveAnnouncementLabel.SetAnchorsPreset(Control.LayoutPreset.CenterTop);
        waveAnnouncementLabel.Position = new Vector2(-150, 120);
        waveAnnouncementLabel.Size = new Vector2(300, 50);
        uiLayer.AddChild(waveAnnouncementLabel);
    }

    private void CreateAlarmNotificationLabel()
    {
        alarmNotificationLabel = new Label();
        alarmNotificationLabel.Text = "⚠️ DETECTED!";
        alarmNotificationLabel.HorizontalAlignment = HorizontalAlignment.Center;
        alarmNotificationLabel.AddThemeFontSizeOverride("font_size", 32);
        alarmNotificationLabel.AddThemeColorOverride("font_color", new Color(1.0f, 0.3f, 0.3f));
        alarmNotificationLabel.Visible = false;
        alarmNotificationLabel.SetAnchorsPreset(Control.LayoutPreset.CenterTop);
        alarmNotificationLabel.Position = new Vector2(-100, 80);
        alarmNotificationLabel.Size = new Vector2(200, 40);
        uiLayer.AddChild(alarmNotificationLabel);
    }

    private void CreateLeftPanelContainer()
    {
        leftPanelContainer = new VBoxContainer();
        leftPanelContainer.Position = new Vector2(10, 45);
        leftPanelContainer.AddThemeConstantOverride("separation", 5);
        uiLayer.AddChild(leftPanelContainer);
    }

    private void CreateAlarmStateWidget()
    {
        alarmStateWidget = new AlarmStateWidget();
        leftPanelContainer.AddChild(alarmStateWidget);
        alarmStateWidget.UpdateDisplay(CombatState.Perception.AlarmState);
    }

    public override void _Process(double delta)
    {
        CombatState.Update((float)delta);

        // Update sub-components
        fogOfWarLayer.UpdateVisuals();
        actorViewManager.UpdateFogVisibility();
        actorViewManager.UpdateDetectionIndicators();
        interactableViewManager.UpdateChannelProgress();
        moveTargetMarker.Update(inputController?.SelectedActorIds);
        retreatUIController.UpdateExtractionStatus();

        if (visibilityDebugOverlay.IsVisible)
        {
            visibilityDebugOverlay.UpdateVisuals();
        }

        UpdateCoverIndicatorsIfMoving();
        UpdateAlarmNotification((float)delta);
        UpdateWaveAnnouncement((float)delta);

        // Update box selection drag
        if (inputController != null)
        {
            var currentScreen = GetViewport().GetMousePosition();
            var currentWorld = ScreenToWorld(currentScreen);
            inputController.UpdateDragSelection(currentScreen, currentWorld);
        }
    }

    private void UpdateCoverIndicatorsIfMoving()
    {
        var selectedIds = inputController?.SelectedActorIds;
        if (selectedIds == null || selectedIds.Count == 0)
        {
            return;
        }

        bool needsUpdate = false;
        foreach (var actorId in selectedIds)
        {
            var actor = CombatState.GetActorById(actorId);
            if (actor == null)
            {
                continue;
            }

            if (!lastKnownPositions.TryGetValue(actorId, out var lastPos) || lastPos != actor.GridPosition)
            {
                lastKnownPositions[actorId] = actor.GridPosition;
                needsUpdate = true;
            }
        }

        if (needsUpdate)
        {
            UpdateCoverIndicators();
        }
    }

    private void UpdateCoverIndicators()
    {
        if (coverIndicator == null)
        {
            return;
        }

        var selectedIds = inputController?.SelectedActorIds;
        if (selectedIds == null || selectedIds.Count == 0)
        {
            coverIndicator.Hide();
            return;
        }

        var positions = new List<Vector2I>();
        foreach (var actorId in selectedIds)
        {
            var actor = CombatState.GetActorById(actorId);
            if (actor != null && actor.State == ActorState.Alive)
            {
                positions.Add(actor.GridPosition);
            }
        }

        coverIndicator.ShowCoverForMultiple(positions);
    }

    private void UpdateAlarmNotification(float delta)
    {
        if (!alarmNotificationLabel.Visible)
        {
            return;
        }

        alarmNotificationTimer -= delta;

        if (alarmNotificationTimer <= 0)
        {
            alarmNotificationLabel.Visible = false;
        }
        else if (alarmNotificationTimer < 1.0f)
        {
            var alpha = alarmNotificationTimer;
            var color = alarmNotificationLabel.GetThemeColor("font_color");
            alarmNotificationLabel.AddThemeColorOverride("font_color", new Color(color.R, color.G, color.B, alpha));
        }
    }
    
    private void UpdateWaveAnnouncement(float delta)
    {
        if (!waveAnnouncementLabel.Visible)
        {
            return;
        }

        waveAnnouncementTimer -= delta;

        if (waveAnnouncementTimer <= 0)
        {
            waveAnnouncementLabel.Visible = false;
        }
        else if (waveAnnouncementTimer < 1.0f)
        {
            var alpha = waveAnnouncementTimer;
            waveAnnouncementLabel.AddThemeColorOverride("font_color", new Color(1.0f, 0.4f, 0.2f, alpha));
        }
    }

    // === Event Handlers ===

    private void OnMissionEnded(bool victory)
    {
        missionEndPanel.Show(CombatState, victory);
    }

    private void OnAlarmStateChanged(AlarmState oldState, AlarmState newState)
    {
        alarmStateWidget?.UpdateDisplay(newState);

        if (newState == AlarmState.Alerted && oldState == AlarmState.Quiet)
        {
            CombatState.TimeSystem.Pause();
            alarmNotificationLabel.Visible = true;
            alarmNotificationTimer = AlarmNotificationDuration;
            SimLog.Log("[MissionView] Auto-paused: Alarm raised!");
        }
    }

    private void OnEnemyDetectedCrew(Actor enemy, Actor crew)
    {
        SimLog.Log($"[MissionView] Enemy#{enemy.Id} detected Crew#{crew.Id}");
    }
    
    private void OnWaveTriggered(WaveDefinition wave)
    {
        if (wave.Announced)
        {
            ShowWaveAnnouncement(wave);
        }
        
        // Auto-pause on wave
        CombatState.TimeSystem.Pause();
        SimLog.Log($"[MissionView] Auto-paused: Wave '{wave.Name ?? wave.Id}' triggered!");
    }
    
    private void ShowWaveAnnouncement(WaveDefinition wave)
    {
        var waveName = wave.Name ?? $"Wave {CombatState.Waves.WavesSpawned}";
        waveAnnouncementLabel.Text = $"REINFORCEMENTS: {waveName}";
        waveAnnouncementLabel.Visible = true;
        waveAnnouncementTimer = WaveAnnouncementDuration;
        
        // Reset alpha
        waveAnnouncementLabel.AddThemeColorOverride("font_color", new Color(1.0f, 0.4f, 0.2f, 1.0f));
    }
    
    private void OnPhaseChanged(TacticalPhase oldPhase, TacticalPhase newPhase)
    {
        SimLog.Log($"[MissionView] Phase changed: {oldPhase} -> {newPhase}");
        
        // Auto-pause on significant phase changes
        if (newPhase == TacticalPhase.Pressure || newPhase == TacticalPhase.Resolution)
        {
            CombatState.TimeSystem.Pause();
        }
    }

    private void OnRestartRequested()
    {
        GameState.Instance?.RestartCurrentMission();
    }

    private void OnContinueRequested()
    {
        var victory = CombatState.FinalOutcome == MissionOutcome.Victory;
        GameState.Instance.EndMission(victory, CombatState);
    }

    // === Input Controller Event Handlers ===

    private void OnSelectionChanged(IReadOnlyList<int> selectedIds)
    {
        actorViewManager.UpdateSelectionState(selectedIds);
        UpdateCoverIndicators();

        if (selectedIds.Count == 0)
        {
            tacticalCamera.ClearFollowTarget();
            coverIndicator?.Hide();
        }
    }

    private void OnActorSelected(int actorId)
    {
        var view = actorViewManager.GetView(actorId);
        if (view != null)
        {
            tacticalCamera.SetFollowTarget(view);
        }
    }

    private void OnMoveOrderIssued(int actorId, Vector2I targetPos)
    {
        CombatState.IssueMovementOrder(actorId, targetPos);
        moveTargetMarker.SetTarget(actorId, targetPos);
    }

    private void OnAttackOrderIssued(int actorId, int targetId)
    {
        CombatState.IssueAttackOrder(actorId, targetId);
    }

    private void OnInteractionOrderIssued(int actorId, int interactableId)
    {
        CombatState.IssueInteractionOrder(actorId, interactableId);
    }

    private void OnReloadOrderIssued(int actorId)
    {
        CombatState.IssueReloadOrder(actorId);
    }
    
    private void OnOverwatchOrderIssued(int actorId)
    {
        CombatState.IssueOverwatchOrder(actorId);
    }
    
    private void OnReactionFired(Actor overwatcher, Actor target, AttackResult result)
    {
        // Show reaction fire visual feedback
        var overwatcherView = actorViewManager.GetView(overwatcher.Id);
        var targetView = actorViewManager.GetView(target.Id);
        
        if (overwatcherView != null && targetView != null)
        {
            ShowReactionFireEffect(overwatcherView.Position, targetView.Position, result.Hit);
        }
        
        GD.Print($"[Overwatch] {overwatcher.Type}#{overwatcher.Id} fired at {target.Type}#{target.Id} - {(result.Hit ? "HIT" : "MISS")}");
    }
    
    private void ShowReactionFireEffect(Vector2 from, Vector2 to, bool hit)
    {
        // Draw a line from overwatcher to target
        var line = new Line2D();
        line.AddPoint(from + new Vector2(TileSize / 2f, TileSize / 2f));
        line.AddPoint(to + new Vector2(TileSize / 2f, TileSize / 2f));
        line.Width = 3f;
        line.DefaultColor = hit ? new Color(1f, 0.3f, 0.3f, 0.8f) : new Color(1f, 1f, 0.3f, 0.6f);
        line.ZIndex = 50;
        AddChild(line);
        
        // Remove after short delay
        var timer = GetTree().CreateTimer(0.2f);
        timer.Timeout += () => line.QueueFree();
    }

    private void OnAbilityTargetingStarted(AbilityData ability)
    {
        abilityTargetingLabel.Text = $"Targeting: {ability.Name} (Range: {ability.Range})\nLeft Click to confirm, Right Click to cancel";
        abilityTargetingLabel.Visible = true;
    }

    private void OnAbilityTargetingCancelled()
    {
        abilityTargetingLabel.Visible = false;
    }

    private void OnAbilityOrderIssued(int actorId, AbilityData ability, Vector2I targetTile)
    {
        var success = CombatState.IssueAbilityOrder(actorId, ability, targetTile);
        if (success)
        {
            GD.Print($"[Ability] {ability.Name} launched at {targetTile}!");
        }
        else
        {
            GD.Print($"[Ability] Cannot use {ability.Name} at {targetTile} (out of range?)");
        }
    }

    private void OnBoxSelectionUpdated(Rect2 rect, bool isActive)
    {
        selectionBox.Visible = isActive;
        if (isActive)
        {
            selectionBox.Position = rect.Position;
            selectionBox.Size = rect.Size;
        }
    }

    private void OnToggleVisibilityDebug()
    {
        visibilityDebugOverlay.Toggle();
    }

    private void OnCenterOnActor(int actorId)
    {
        var view = actorViewManager.GetView(actorId);
        if (view != null)
        {
            tacticalCamera.SetFollowTarget(view);
        }
    }

    private void OnAbilityDetonated(AbilityData ability, Vector2I tile)
    {
        GD.Print($"[Visual] {ability.Name} exploded at {tile}!");

        var explosion = new ColorRect();
        explosion.Size = new Vector2((ability.Radius * 2 + 1) * TileSize, (ability.Radius * 2 + 1) * TileSize);
        explosion.Position = new Vector2(
            (tile.X - ability.Radius) * TileSize,
            (tile.Y - ability.Radius) * TileSize
        );
        explosion.Color = new Color(1.0f, 0.5f, 0.0f, 0.6f);
        AddChild(explosion);

        var timer = GetTree().CreateTimer(0.3f);
        timer.Timeout += () => explosion.QueueFree();
    }

    public override void _ExitTree()
    {
        // Unsubscribe from CombatState events
        if (CombatState != null)
        {
            CombatState.MissionEnded -= OnMissionEnded;
            CombatState.Perception.AlarmStateChanged -= OnAlarmStateChanged;
            CombatState.Perception.EnemyDetectedCrew -= OnEnemyDetectedCrew;
            CombatState.AbilitySystem.AbilityDetonated -= OnAbilityDetonated;
            CombatState.OverwatchSystem.ReactionFired -= OnReactionFired;
            CombatState.Waves.WaveTriggered -= OnWaveTriggered;
            CombatState.Phases.PhaseChanged -= OnPhaseChanged;
        }

        // Unsubscribe from input controller events
        if (inputController != null)
        {
            inputController.SelectionChanged -= OnSelectionChanged;
            inputController.ActorSelected -= OnActorSelected;
            inputController.MoveOrderIssued -= OnMoveOrderIssued;
            inputController.AttackOrderIssued -= OnAttackOrderIssued;
            inputController.InteractionOrderIssued -= OnInteractionOrderIssued;
            inputController.ReloadOrderIssued -= OnReloadOrderIssued;
            inputController.OverwatchOrderIssued -= OnOverwatchOrderIssued;
            inputController.AbilityTargetingStarted -= OnAbilityTargetingStarted;
            inputController.AbilityTargetingCancelled -= OnAbilityTargetingCancelled;
            inputController.AbilityOrderIssued -= OnAbilityOrderIssued;
            inputController.BoxSelectionUpdated -= OnBoxSelectionUpdated;
            inputController.ToggleVisibilityDebug -= OnToggleVisibilityDebug;
            inputController.CenterOnActor -= OnCenterOnActor;
        }

        // Cleanup sub-components
        fogOfWarLayer?.Cleanup();
        interactableViewManager?.Cleanup();
        retreatUIController?.Cleanup();
        
        if (missionEndPanel != null)
        {
            missionEndPanel.RestartRequested -= OnRestartRequested;
            missionEndPanel.ContinueRequested -= OnContinueRequested;
        }
    }
}
