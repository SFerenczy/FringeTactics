using Godot;
using System.Collections.Generic;
using System.Linq;

namespace FringeTactics;

public partial class MissionView : Node2D
{
    public const int TileSize = GridConstants.TileSize;
    private const string ActorViewScenePath = "res://src/scenes/mission/ActorView.tscn";
    private const string TimeStateWidgetScenePath = "res://src/scenes/mission/TimeStateWidget.tscn";

    private PackedScene actorViewScene;
    private PackedScene timeStateWidgetScene;

    public CombatState CombatState { get; private set; }
    private Dictionary<int, ActorView> actorViews = new();
    private List<int> crewActorIds = new();

    // Input controller (owns selection state, ability targeting, etc.)
    private MissionInputController inputController;

    // Ability targeting UI
    private Label abilityTargetingLabel;

    // Movement target marker
    private Node2D moveTargetMarker;
    private ColorRect moveTargetFill;
    private ColorRect moveTargetBorder;
    private Dictionary<int, Vector2I> actorMoveTargets = new();

    // Box selection visual
    private ColorRect selectionBox;

    // Fog of war overlay
    private Node2D fogLayer;
    private Dictionary<Vector2I, ColorRect> fogTiles = new();
    private bool fogDirty = true;

    // Debug overlay for visibility
    private Node2D visibilityDebugLayer;
    private Dictionary<Vector2I, ColorRect> debugTiles = new();
    private bool showVisibilityDebug = false;

    // Cover indicators
    private CoverIndicator coverIndicator;

    // Interactable views
    private Dictionary<int, InteractableView> interactableViews = new();
    private Node2D interactablesContainer;

    // Mission result tracking
    private bool missionVictory = false;

    // Alarm state UI
    private Label alarmNotificationLabel;
    private float alarmNotificationTimer = 0f;
    private const float AlarmNotificationDuration = 3.0f;
    private AlarmStateWidget alarmStateWidget;
    
    // Retreat UI (M7)
    private Button retreatButton;
    private Label extractionStatusLabel;
    private Node2D entryZoneHighlightLayer;

    [ExportGroup("Node Paths")]
    [Export] private Node2D gridDisplayPath;
    [Export] private Node2D actorsContainerPath;
    [Export] private CanvasLayer uiLayerPath;
    [Export] private Label instructionsLabelPath;

    private Node2D gridDisplay;
    private Node2D actorsContainer;
    private CanvasLayer uiLayer;
    private Label instructionsLabel;
    private TacticalCamera tacticalCamera;

    private TimeStateWidget timeStateWidget;
    private Panel missionEndPanel;
    private Label missionResultLabel;
    private Label missionSummaryLabel;
    private Button restartButton;

    public override void _Ready()
    {
        // Load scenes
        actorViewScene = GD.Load<PackedScene>(ActorViewScenePath);
        timeStateWidgetScene = GD.Load<PackedScene>(TimeStateWidgetScenePath);

        // Get node references
        gridDisplay = GetNode<Node2D>("GridDisplay");
        actorsContainer = GetNode<Node2D>("Actors");
        uiLayer = GetNode<CanvasLayer>("UI");
        instructionsLabel = GetNode<Label>("UI/InstructionsLabel");
        tacticalCamera = GetNode<TacticalCamera>("TacticalCamera");

        InitializeCombat();
        SetupInputController();
        SetupUI();
        SetupCamera();
        DrawGrid();
        CreateFogLayer();
        CreateInteractablesLayer();
        CreateCoverIndicator();
        SpawnActorViews();
    }

    private void SetupInputController()
    {
        inputController = new MissionInputController();
        inputController.Name = "InputController";
        AddChild(inputController);

        inputController.Initialize(
            CombatState,
            crewActorIds,
            TileSize,
            ScreenToWorld
        );

        // Subscribe to input controller events
        inputController.SelectionChanged += OnSelectionChanged;
        inputController.ActorSelected += OnActorSelected;
        inputController.MoveOrderIssued += OnMoveOrderIssued;
        inputController.AttackOrderIssued += OnAttackOrderIssued;
        inputController.InteractionOrderIssued += OnInteractionOrderIssued;
        inputController.ReloadOrderIssued += OnReloadOrderIssued;
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
        // Set camera bounds based on map size
        var gridSize = CombatState.MapState.GridSize;
        tacticalCamera.SetMapBoundsFromGrid(gridSize, TileSize);
        
        // Center camera on map
        tacticalCamera.CenterOnMap();
    }

    private void InitializeCombat()
    {
        // Get CombatState from GameState (already built by MissionFactory)
        CombatState = GameState.Instance?.CurrentCombat;

        if (CombatState == null)
        {
            // Fallback for testing in editor without going through menu
            GD.PrintErr("[MissionView] No CurrentCombat found! Creating fallback sandbox.");
            var config = MissionConfig.CreateTestMission();
            CombatState = MissionFactory.BuildSandbox(config);
        }

        // Subscribe to events
        CombatState.MissionEnded += OnMissionEnded;
        CombatState.Perception.AlarmStateChanged += OnAlarmStateChanged;
        CombatState.Perception.EnemyDetectedCrew += OnEnemyDetectedCrew;
        CombatState.RetreatInitiated += OnRetreatInitiated;
        CombatState.RetreatCancelled += OnRetreatCancelled;
    }

    private void SetupUI()
    {
        // Create and add TimeStateWidget
        timeStateWidget = timeStateWidgetScene.Instantiate<TimeStateWidget>();
        timeStateWidget.Position = new Vector2(10, 10);
        uiLayer.AddChild(timeStateWidget);
        timeStateWidget.ConnectToTimeSystem(CombatState.TimeSystem);

        // Update instructions
        instructionsLabel.Text = "Space: Pause/Resume | G: Grenade | Scroll: Zoom | WASD: Pan | F3: Debug\n1-3: Select/Recall group | Ctrl+1-3: Save group | Tab: Select all\nClick: Select | Shift+Click: Add/Remove | Drag: Box | DblClick: All\nRClick: Move/Attack | C: Center on unit";

        // Create ability targeting label
        abilityTargetingLabel = new Label();
        abilityTargetingLabel.Position = new Vector2(10, 60);
        abilityTargetingLabel.AddThemeColorOverride("font_color", Colors.Yellow);
        abilityTargetingLabel.AddThemeFontSizeOverride("font_size", 16);
        abilityTargetingLabel.Visible = false;
        uiLayer.AddChild(abilityTargetingLabel);

        // Subscribe to ability events for visual feedback
        CombatState.AbilitySystem.AbilityDetonated += OnAbilityDetonated;

        // Create movement target marker
        CreateMoveTargetMarker();

        // Create selection box for drag-select
        CreateSelectionBox();

        // Create mission end panel (hidden initially)
        CreateMissionEndPanel();
        
        // Create alarm notification label (hidden initially)
        CreateAlarmNotificationLabel();
        
        // Create left panel container for stacked widgets
        CreateLeftPanelContainer();
        
        // Create alarm state widget
        CreateAlarmStateWidget();
        
        // Create retreat UI (M7)
        CreateRetreatUI();
    }
    
    private void CreateSelectionBox()
    {
        selectionBox = new ColorRect();
        selectionBox.Color = new Color(0.3f, 0.6f, 1.0f, 0.3f);
        selectionBox.Visible = false;
        selectionBox.ZIndex = 100;
        AddChild(selectionBox);
    }

    private void CreateMoveTargetMarker()
    {
        moveTargetMarker = new Node2D();
        moveTargetMarker.Visible = false;
        moveTargetMarker.ZIndex = 1; // Above grid, below actors
        gridDisplay.AddChild(moveTargetMarker);
        
        // Border (slightly larger, behind fill)
        moveTargetBorder = new ColorRect();
        moveTargetBorder.Size = new Vector2(TileSize - 2, TileSize - 2);
        moveTargetBorder.Position = Vector2.Zero;
        moveTargetBorder.Color = new Color(0.2f, 0.9f, 0.2f, 0.8f); // Bright green border
        moveTargetMarker.AddChild(moveTargetBorder);
        
        // Inner fill (smaller, on top)
        moveTargetFill = new ColorRect();
        moveTargetFill.Size = new Vector2(TileSize - 8, TileSize - 8);
        moveTargetFill.Position = new Vector2(3, 3);
        moveTargetFill.Color = new Color(0.3f, 0.8f, 0.3f, 0.4f); // Semi-transparent green fill
        moveTargetMarker.AddChild(moveTargetFill);
    }

    private void CreateMissionEndPanel()
    {
        missionEndPanel = new Panel();
        missionEndPanel.Position = new Vector2(120, 80);
        missionEndPanel.Size = new Vector2(280, 220);
        missionEndPanel.Visible = false;
        uiLayer.AddChild(missionEndPanel);

        var vbox = new VBoxContainer();
        vbox.Position = new Vector2(20, 15);
        vbox.Size = new Vector2(240, 190);
        missionEndPanel.AddChild(vbox);

        // Result label
        missionResultLabel = new Label();
        missionResultLabel.AddThemeFontSizeOverride("font_size", 32);
        missionResultLabel.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(missionResultLabel);

        // Spacer
        var spacer = new Control();
        spacer.CustomMinimumSize = new Vector2(0, 10);
        vbox.AddChild(spacer);

        // Summary label
        missionSummaryLabel = new Label();
        missionSummaryLabel.AddThemeFontSizeOverride("font_size", 14);
        vbox.AddChild(missionSummaryLabel);

        // Spacer before button
        var spacer2 = new Control();
        spacer2.CustomMinimumSize = new Vector2(0, 15);
        vbox.AddChild(spacer2);

        // Continue/Restart button
        restartButton = new Button();
        restartButton.Pressed += OnRestartPressed;
        vbox.AddChild(restartButton);
    }

    private void OnMissionEnded(bool victory)
    {
        missionVictory = victory;

        // Set result text and color based on outcome (M7: support retreat)
        var outcome = CombatState.FinalOutcome ?? (victory ? MissionOutcome.Victory : MissionOutcome.Defeat);
        switch (outcome)
        {
            case MissionOutcome.Victory:
                missionResultLabel.Text = "VICTORY!";
                missionResultLabel.AddThemeColorOverride("font_color", Colors.Green);
                break;
            case MissionOutcome.Retreat:
                missionResultLabel.Text = "RETREATED";
                missionResultLabel.AddThemeColorOverride("font_color", Colors.Yellow);
                break;
            case MissionOutcome.Defeat:
            default:
                missionResultLabel.Text = "DEFEAT!";
                missionResultLabel.AddThemeColorOverride("font_color", Colors.Red);
                break;
        }

        // Update button text based on mode
        var hasCampaign = GameState.Instance?.HasActiveCampaign() ?? false;
        restartButton.Text = hasCampaign ? "Continue" : "Restart Test Mission";

        // Build summary
        var stats = CombatState.Stats;
        var crewAlive = 0;
        var crewDead = 0;
        foreach (var actor in CombatState.Actors)
        {
            if (actor.Type == ActorType.Crew)
            {
                if (actor.State == ActorState.Alive)
                {
                    crewAlive++;
                }
                else
                {
                    crewDead++;
                }
            }
        }

        var summary = $"--- CREW ---\n";
        summary += $"Alive: {crewAlive}  Dead: {crewDead}\n\n";
        summary += $"--- COMBAT ---\n";
        summary += $"Shots: {stats.PlayerShotsFired}  Hits: {stats.PlayerHits}  Misses: {stats.PlayerMisses}\n";
        summary += $"Accuracy: {stats.PlayerAccuracy:F0}%";

        missionSummaryLabel.Text = summary;

        // Print to console as well
        GD.Print("\n=== MISSION SUMMARY ===");
        GD.Print($"Result: {outcome}");
        GD.Print($"Crew Alive: {crewAlive}, Dead: {crewDead}");
        GD.Print($"Player Shots: {stats.PlayerShotsFired}, Hits: {stats.PlayerHits}, Misses: {stats.PlayerMisses}, Accuracy: {stats.PlayerAccuracy:F1}%");
        GD.Print($"Enemy Shots: {stats.EnemyShotsFired}, Hits: {stats.EnemyHits}, Misses: {stats.EnemyMisses}, Accuracy: {stats.EnemyAccuracy:F1}%");
        GD.Print("========================\n");

        missionEndPanel.Visible = true;
    }
    
    private void CreateAlarmNotificationLabel()
    {
        alarmNotificationLabel = new Label();
        alarmNotificationLabel.Text = "⚠️ DETECTED!";
        alarmNotificationLabel.HorizontalAlignment = HorizontalAlignment.Center;
        alarmNotificationLabel.AddThemeFontSizeOverride("font_size", 32);
        alarmNotificationLabel.AddThemeColorOverride("font_color", new Color(1.0f, 0.3f, 0.3f));
        alarmNotificationLabel.Visible = false;
        
        // Position at top center of screen
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
        
        // Initialize with current state
        alarmStateWidget.UpdateDisplay(CombatState.Perception.AlarmState);
    }
    
    private void CreateRetreatUI()
    {
        // Retreat button
        retreatButton = new Button();
        retreatButton.Text = "Retreat";
        retreatButton.Position = new Vector2(10, 75);
        retreatButton.Size = new Vector2(100, 28);
        retreatButton.Pressed += OnRetreatButtonPressed;
        uiLayer.AddChild(retreatButton);
        
        // Extraction status label (hidden until retreat initiated)
        extractionStatusLabel = new Label();
        extractionStatusLabel.Position = new Vector2(10, 108);
        extractionStatusLabel.AddThemeFontSizeOverride("font_size", 14);
        extractionStatusLabel.Visible = false;
        uiLayer.AddChild(extractionStatusLabel);
    }
    
    private void OnRetreatButtonPressed()
    {
        if (CombatState.IsRetreating)
        {
            CombatState.CancelRetreat();
        }
        else
        {
            CombatState.InitiateRetreat();
        }
    }
    
    private void OnRetreatInitiated()
    {
        retreatButton.Text = "Cancel Retreat";
        extractionStatusLabel.Visible = true;
        UpdateExtractionStatus();
        CreateEntryZoneHighlights();
    }
    
    private void OnRetreatCancelled()
    {
        retreatButton.Text = "Retreat";
        extractionStatusLabel.Visible = false;
        RemoveEntryZoneHighlights();
    }
    
    private void UpdateExtractionStatus()
    {
        if (!CombatState.IsRetreating)
        {
            return;
        }
        
        var (inZone, total) = CombatState.GetCrewExtractionStatus();
        extractionStatusLabel.Text = $"Extraction: {inZone}/{total} in zone";
        
        if (inZone == total && total > 0)
        {
            extractionStatusLabel.AddThemeColorOverride("font_color", Colors.Green);
        }
        else
        {
            extractionStatusLabel.AddThemeColorOverride("font_color", Colors.Yellow);
        }
    }
    
    private void CreateEntryZoneHighlights()
    {
        if (entryZoneHighlightLayer != null)
        {
            return;
        }
        
        entryZoneHighlightLayer = new Node2D();
        entryZoneHighlightLayer.Name = "EntryZoneHighlights";
        entryZoneHighlightLayer.ZIndex = 4; // Above grid, below fog
        gridDisplay.AddChild(entryZoneHighlightLayer);
        
        foreach (var pos in CombatState.MapState.EntryZone)
        {
            var highlight = new ColorRect();
            highlight.Size = new Vector2(TileSize - 2, TileSize - 2);
            highlight.Position = new Vector2(pos.X * TileSize + 1, pos.Y * TileSize + 1);
            highlight.Color = new Color(0.2f, 0.9f, 0.3f, 0.35f); // Green highlight
            entryZoneHighlightLayer.AddChild(highlight);
        }
    }
    
    private void RemoveEntryZoneHighlights()
    {
        if (entryZoneHighlightLayer != null)
        {
            entryZoneHighlightLayer.QueueFree();
            entryZoneHighlightLayer = null;
        }
    }
    
    private void OnAlarmStateChanged(AlarmState oldState, AlarmState newState)
    {
        // Update alarm state widget
        alarmStateWidget?.UpdateDisplay(newState);
        
        if (newState == AlarmState.Alerted && oldState == AlarmState.Quiet)
        {
            // Auto-pause on first alarm
            CombatState.TimeSystem.Pause();
            
            // Show notification
            ShowAlarmNotification();
            
            SimLog.Log("[MissionView] Auto-paused: Alarm raised!");
        }
    }
    
    private void OnEnemyDetectedCrew(Actor enemy, Actor crew)
    {
        SimLog.Log($"[MissionView] Enemy#{enemy.Id} detected Crew#{crew.Id}");
    }
    
    private void ShowAlarmNotification()
    {
        alarmNotificationLabel.Visible = true;
        alarmNotificationTimer = AlarmNotificationDuration;
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
            // Fade out in the last second
            var alpha = alarmNotificationTimer;
            var color = alarmNotificationLabel.GetThemeColor("font_color");
            alarmNotificationLabel.AddThemeColorOverride("font_color", new Color(color.R, color.G, color.B, alpha));
        }
    }

    private void OnRestartPressed()
    {
        var hasCampaign = GameState.Instance?.HasActiveCampaign() ?? false;

        if (hasCampaign)
        {
            // Return to campaign with results
            GameState.Instance.EndMission(missionVictory, CombatState);
        }
        else
        {
            // Sandbox mode - restart with same mission config
            GameState.Instance?.RestartCurrentMission();
        }
    }

    private void DrawGrid()
    {
        // Draw grid with tile type visualization
        var gridSize = CombatState.MapState.GridSize;
        var map = CombatState.MapState;
        
        // Draw map border
        DrawMapBorder(gridSize);
        
        for (int y = 0; y < gridSize.Y; y++)
        {
            for (int x = 0; x < gridSize.X; x++)
            {
                var pos = new Vector2I(x, y);
                var tile = new ColorRect();
                tile.Size = new Vector2(TileSize - 1, TileSize - 1);
                tile.Position = new Vector2(x * TileSize, y * TileSize);

                // Color based on tile type
                var tileType = map.GetTileType(pos);
                switch (tileType)
                {
                    case TileType.Wall:
                        tile.Color = GridConstants.WallColor;
                        break;
                    case TileType.Void:
                        tile.Color = GridConstants.VoidColor;
                        break;
                    case TileType.Floor:
                    default:
                        // Check for cover objects first
                        var coverHeight = map.GetTileCoverHeight(pos);
                        if (coverHeight != CoverHeight.None)
                        {
                            tile.Color = coverHeight switch
                            {
                                CoverHeight.Low => GridConstants.LowCoverTileColor,
                                CoverHeight.Half => GridConstants.HalfCoverTileColor,
                                CoverHeight.High => GridConstants.HighCoverTileColor,
                                _ => GridConstants.WallColor
                            };
                        }
                        else
                        {
                            // Checkerboard pattern for floor
                            tile.Color = (x + y) % 2 == 0 
                                ? GridConstants.FloorColorDark 
                                : GridConstants.FloorColorLight;
                        }
                        break;
                }
                
                // Subtle highlight for entry zone
                if (map.IsInEntryZone(pos))
                {
                    tile.Color = tile.Color.Lightened(0.15f);
                    // Add a slight green tint to entry zone
                    tile.Color = new Color(
                        tile.Color.R * 0.9f,
                        tile.Color.G * 1.1f,
                        tile.Color.B * 0.9f
                    );
                }

                gridDisplay.AddChild(tile);
            }
        }
    }
    
    private void DrawMapBorder(Vector2I gridSize)
    {
        var borderWidth = 2;
        var borderColor = new Color(0.4f, 0.45f, 0.5f);
        var mapWidth = gridSize.X * TileSize;
        var mapHeight = gridSize.Y * TileSize;
        
        // Top border
        var top = new ColorRect();
        top.Size = new Vector2(mapWidth + borderWidth * 2, borderWidth);
        top.Position = new Vector2(-borderWidth, -borderWidth);
        top.Color = borderColor;
        gridDisplay.AddChild(top);
        
        // Bottom border
        var bottom = new ColorRect();
        bottom.Size = new Vector2(mapWidth + borderWidth * 2, borderWidth);
        bottom.Position = new Vector2(-borderWidth, mapHeight);
        bottom.Color = borderColor;
        gridDisplay.AddChild(bottom);
        
        // Left border
        var left = new ColorRect();
        left.Size = new Vector2(borderWidth, mapHeight);
        left.Position = new Vector2(-borderWidth, 0);
        left.Color = borderColor;
        gridDisplay.AddChild(left);
        
        // Right border
        var right = new ColorRect();
        right.Size = new Vector2(borderWidth, mapHeight);
        right.Position = new Vector2(mapWidth, 0);
        right.Color = borderColor;
        gridDisplay.AddChild(right);
    }

    private void CreateFogLayer()
    {
        fogLayer = new Node2D();
        fogLayer.ZIndex = 5; // Above grid, below actors
        fogLayer.Name = "FogLayer";
        gridDisplay.AddChild(fogLayer);

        // Subscribe to visibility changes
        CombatState.Visibility.VisibilityChanged += OnVisibilityChanged;

        // Create fog tiles for entire grid
        var gridSize = CombatState.MapState.GridSize;
        for (int y = 0; y < gridSize.Y; y++)
        {
            for (int x = 0; x < gridSize.X; x++)
            {
                var pos = new Vector2I(x, y);
                var fogTile = new ColorRect();
                fogTile.Size = new Vector2(TileSize, TileSize);
                fogTile.Position = new Vector2(x * TileSize, y * TileSize);
                fogTile.MouseFilter = Control.MouseFilterEnum.Ignore;
                fogLayer.AddChild(fogTile);
                fogTiles[pos] = fogTile;
            }
        }

        // Initial fog update
        fogDirty = true;
        UpdateFogVisuals();
    }

    private void OnVisibilityChanged()
    {
        fogDirty = true;
    }

    private void CreateCoverIndicator()
    {
        coverIndicator = new CoverIndicator();
        coverIndicator.Name = "CoverIndicator";
        coverIndicator.ZIndex = 3; // Above grid, below fog and actors
        gridDisplay.AddChild(coverIndicator);
        coverIndicator.Initialize(CombatState.MapState);
    }

    private void CreateInteractablesLayer()
    {
        interactablesContainer = new Node2D();
        interactablesContainer.Name = "Interactables";
        interactablesContainer.ZIndex = 2; // Above grid, below cover indicators
        gridDisplay.AddChild(interactablesContainer);
        
        // Subscribe to interaction system events
        CombatState.Interactions.InteractableAdded += OnInteractableAdded;
        CombatState.Interactions.InteractableRemoved += OnInteractableRemoved;
        CombatState.Interactions.InteractableStateChanged += OnInteractableStateChanged;
        
        // Create views for existing interactables
        foreach (var interactable in CombatState.Interactions.GetAllInteractables())
        {
            CreateInteractableView(interactable);
        }
        
        GD.Print($"[MissionView] Created {interactableViews.Count} interactable views");
    }
    
    private void OnInteractableAdded(Interactable interactable)
    {
        CreateInteractableView(interactable);
    }
    
    private void OnInteractableRemoved(Interactable interactable)
    {
        if (interactableViews.TryGetValue(interactable.Id, out var view))
        {
            view.Cleanup();
            view.QueueFree();
            interactableViews.Remove(interactable.Id);
        }
    }
    
    private void OnInteractableStateChanged(Interactable interactable, InteractableState newState)
    {
        // Update fog/visibility when doors change
        if (interactable.IsDoor)
        {
            CombatState.Visibility.UpdateVisibility(CombatState.Actors);
            fogDirty = true;
        }
    }
    
    private void CreateInteractableView(Interactable interactable)
    {
        var view = new InteractableView();
        view.Name = $"Interactable_{interactable.Id}";
        interactablesContainer.AddChild(view);
        view.Setup(interactable);
        interactableViews[interactable.Id] = view;
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

    private void UpdateFogVisuals()
    {
        if (!fogDirty)
        {
            return;
        }
        fogDirty = false;

        foreach (var kvp in fogTiles)
        {
            var pos = kvp.Key;
            var tile = kvp.Value;
            var visibility = CombatState.Visibility.GetVisibility(pos);

            switch (visibility)
            {
                case VisibilityState.Unknown:
                    tile.Color = new Color(0.0f, 0.0f, 0.0f, 0.95f); // Nearly opaque black
                    tile.Visible = true;
                    break;
                case VisibilityState.Revealed:
                    tile.Color = new Color(0.0f, 0.0f, 0.0f, 0.5f); // Semi-transparent
                    tile.Visible = true;
                    break;
                case VisibilityState.Visible:
                    tile.Visible = false; // Fully visible, no fog
                    break;
            }
        }
    }

    private void UpdateActorFogVisibility()
    {
        foreach (var kvp in actorViews)
        {
            var actor = CombatState.GetActorById(kvp.Key);
            var view = kvp.Value;

            if (actor == null)
            {
                continue;
            }

            // Crew are always visible to player
            if (actor.Type == ActorType.Crew)
            {
                view.Visible = true;
                continue;
            }

            // Enemies only visible if their tile is visible
            var isVisible = CombatState.Visibility.IsVisible(actor.GridPosition);
            view.Visible = isVisible;
        }
    }

    private void ToggleVisibilityDebug()
    {
        showVisibilityDebug = !showVisibilityDebug;

        if (showVisibilityDebug && visibilityDebugLayer == null)
        {
            CreateVisibilityDebugLayer();
        }

        if (visibilityDebugLayer != null)
        {
            visibilityDebugLayer.Visible = showVisibilityDebug;
        }

        if (showVisibilityDebug)
        {
            UpdateVisibilityDebug();
        }

        GD.Print($"[Debug] Visibility overlay: {(showVisibilityDebug ? "ON" : "OFF")}");
    }

    private void CreateVisibilityDebugLayer()
    {
        visibilityDebugLayer = new Node2D();
        visibilityDebugLayer.ZIndex = 10; // Above fog layer
        visibilityDebugLayer.Name = "VisibilityDebugLayer";
        gridDisplay.AddChild(visibilityDebugLayer);

        var gridSize = CombatState.MapState.GridSize;
        for (int y = 0; y < gridSize.Y; y++)
        {
            for (int x = 0; x < gridSize.X; x++)
            {
                var pos = new Vector2I(x, y);
                var debugTile = new ColorRect();
                debugTile.Size = new Vector2(8, 8); // Small indicator
                debugTile.Position = new Vector2(x * TileSize + 2, y * TileSize + 2);
                debugTile.MouseFilter = Control.MouseFilterEnum.Ignore;
                visibilityDebugLayer.AddChild(debugTile);
                debugTiles[pos] = debugTile;
            }
        }
    }

    private void UpdateVisibilityDebug()
    {
        if (!showVisibilityDebug || visibilityDebugLayer == null)
        {
            return;
        }

        foreach (var kvp in debugTiles)
        {
            var pos = kvp.Key;
            var tile = kvp.Value;
            var visibility = CombatState.Visibility.GetVisibility(pos);

            // Color code: Green=Visible, Yellow=Revealed, Red=Unknown
            tile.Color = visibility switch
            {
                VisibilityState.Visible => new Color(0.0f, 1.0f, 0.0f, 0.8f),   // Green
                VisibilityState.Revealed => new Color(1.0f, 1.0f, 0.0f, 0.8f), // Yellow
                VisibilityState.Unknown => new Color(1.0f, 0.0f, 0.0f, 0.8f),  // Red
                _ => Colors.White
            };
        }
    }

    /// <summary>
    /// Create visual representations for all actors already in CombatState.
    /// MissionView no longer decides what to spawn - that's MissionFactory's job.
    /// </summary>
    private void SpawnActorViews()
    {
        var crewColors = new Color[]
        {
            new Color(0.2f, 0.6f, 1.0f), // Blue
            new Color(0.2f, 0.8f, 0.3f), // Green
            new Color(0.4f, 0.7f, 0.9f), // Light Blue
            new Color(0.6f, 0.4f, 0.9f)  // Purple
        };
        var enemyColor = new Color(0.9f, 0.2f, 0.2f); // Red

        crewActorIds.Clear();
        int crewIndex = 0;

        foreach (var actor in CombatState.Actors)
        {
            var view = actorViewScene.Instantiate<ActorView>();
            actorsContainer.AddChild(view);
            actorViews[actor.Id] = view;

            if (actor.Type == ActorType.Crew)
            {
                var color = crewColors[crewIndex % crewColors.Length];
                view.Setup(actor, color);
                crewActorIds.Add(actor.Id);
                crewIndex++;
            }
            else if (actor.Type == ActorType.Enemy)
            {
                view.Setup(actor, enemyColor);
            }
            else
            {
                view.Setup(actor, Colors.White);
            }
        }

        GD.Print($"[MissionView] Created views for {CombatState.Actors.Count} actors ({crewActorIds.Count} crew)");
    }

    private void OnActorRemoved(Actor actor)
    {
        if (actorViews.ContainsKey(actor.Id))
        {
            actorViews[actor.Id].QueueFree();
            actorViews.Remove(actor.Id);
        }
    }

    public override void _ExitTree()
    {
        // Unsubscribe from CombatState events
        if (CombatState != null)
        {
            CombatState.MissionEnded -= OnMissionEnded;
            CombatState.Perception.AlarmStateChanged -= OnAlarmStateChanged;
            CombatState.Perception.EnemyDetectedCrew -= OnEnemyDetectedCrew;
            CombatState.RetreatInitiated -= OnRetreatInitiated;
            CombatState.RetreatCancelled -= OnRetreatCancelled;
            CombatState.AbilitySystem.AbilityDetonated -= OnAbilityDetonated;
            CombatState.Visibility.VisibilityChanged -= OnVisibilityChanged;
            CombatState.Interactions.InteractableAdded -= OnInteractableAdded;
            CombatState.Interactions.InteractableRemoved -= OnInteractableRemoved;
            CombatState.Interactions.InteractableStateChanged -= OnInteractableStateChanged;
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
            inputController.AbilityTargetingStarted -= OnAbilityTargetingStarted;
            inputController.AbilityTargetingCancelled -= OnAbilityTargetingCancelled;
            inputController.AbilityOrderIssued -= OnAbilityOrderIssued;
            inputController.BoxSelectionUpdated -= OnBoxSelectionUpdated;
            inputController.ToggleVisibilityDebug -= OnToggleVisibilityDebug;
            inputController.CenterOnActor -= OnCenterOnActor;
        }
        
        // Cleanup interactable views
        foreach (var view in interactableViews.Values)
        {
            view.Cleanup();
        }
        interactableViews.Clear();
    }

    public override void _Process(double delta)
    {
        CombatState.Update((float)delta);

        // Update fog of war visuals
        UpdateFogVisuals();

        // Update enemy visibility based on fog
        UpdateActorFogVisibility();

        // Update debug overlay if enabled
        if (showVisibilityDebug)
        {
            UpdateVisibilityDebug();
        }
        
        // Update movement target marker visibility
        UpdateMoveTargetMarker();
        
        // Clean up completed movement targets
        CleanupCompletedMoveTargets();
        
        // Update cover indicators if selected units are moving
        UpdateCoverIndicatorsIfMoving();
        
        // Update interactable channel progress displays
        UpdateInteractableChannelProgress();
        
        // Update alarm notification timer
        UpdateAlarmNotification((float)delta);
        
        // Update enemy detection indicators
        UpdateDetectionIndicators();
        
        // Update retreat extraction status (M7)
        UpdateExtractionStatus();
        
        // Update box selection drag
        if (inputController != null)
        {
            var currentScreen = GetViewport().GetMousePosition();
            var currentWorld = ScreenToWorld(currentScreen);
            inputController.UpdateDragSelection(currentScreen, currentWorld);
        }
    }
    
    private void UpdateDetectionIndicators()
    {
        foreach (var kvp in actorViews)
        {
            var actorView = kvp.Value;
            var actor = actorView.GetActor();
            
            if (actor == null || actor.Type != ActorType.Enemy)
            {
                continue;
            }
            
            var detectionState = CombatState.Perception.GetDetectionState(actor.Id);
            actorView.UpdateDetectionState(detectionState);
        }
    }
    
    private void UpdateInteractableChannelProgress()
    {
        // Track which interactables are being channeled
        var channelingTargets = new HashSet<int>();
        
        foreach (var actor in CombatState.Actors)
        {
            if (actor.IsChanneling && actor.CurrentChannel != null)
            {
                var targetId = actor.CurrentChannel.TargetInteractableId;
                channelingTargets.Add(targetId);
                
                if (interactableViews.TryGetValue(targetId, out var view))
                {
                    view.ShowChannelProgress(actor.CurrentChannel.Progress);
                }
            }
        }
        
        // Hide progress for non-channeling interactables
        foreach (var kvp in interactableViews)
        {
            if (!channelingTargets.Contains(kvp.Key))
            {
                kvp.Value.HideChannelProgress();
            }
        }
    }
    
    private void CleanupCompletedMoveTargets()
    {
        var toRemove = new List<int>();
        foreach (var kvp in actorMoveTargets)
        {
            var actor = CombatState.GetActorById(kvp.Key);
            if (actor == null || !actor.IsMoving)
            {
                toRemove.Add(kvp.Key);
            }
        }
        foreach (var id in toRemove)
        {
            actorMoveTargets.Remove(id);
        }
    }

    // Track last known positions for cover update optimization
    private Dictionary<int, Vector2I> lastKnownPositions = new();

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

    // === Input Controller Event Handlers ===

    private void OnSelectionChanged(IReadOnlyList<int> selectedIds)
    {
        // Update visual selection state on all actor views
        foreach (var kvp in actorViews)
        {
            var isSelected = selectedIds.Contains(kvp.Key);
            kvp.Value.SetSelected(isSelected);
        }

        // Update cover indicators
        UpdateCoverIndicators();

        // Clear camera follow if nothing selected
        if (selectedIds.Count == 0)
        {
            tacticalCamera.ClearFollowTarget();
            coverIndicator?.Hide();
        }
    }

    private void OnActorSelected(int actorId)
    {
        if (actorViews.TryGetValue(actorId, out var view))
        {
            tacticalCamera.SetFollowTarget(view);
        }
    }

    private void OnMoveOrderIssued(int actorId, Vector2I targetPos)
    {
        CombatState.IssueMovementOrder(actorId, targetPos);
        var actor = CombatState.GetActorById(actorId);
        if (actor != null && actor.IsMoving)
        {
            actorMoveTargets[actorId] = targetPos;
        }
        UpdateMoveTargetMarker();
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
        ToggleVisibilityDebug();
    }

    private void OnCenterOnActor(int actorId)
    {
        if (actorViews.TryGetValue(actorId, out var view))
        {
            tacticalCamera.SetFollowTarget(view);
        }
    }

    private void UpdateMoveTargetMarker()
    {
        var selectedIds = inputController?.SelectedActorIds;
        if (selectedIds == null)
        {
            HideMoveTarget();
            return;
        }

        foreach (var actorId in selectedIds)
        {
            if (actorMoveTargets.TryGetValue(actorId, out var target))
            {
                var actor = CombatState.GetActorById(actorId);
                if (actor != null && actor.IsMoving)
                {
                    ShowMoveTarget(target);
                    return;
                }
            }
        }

        HideMoveTarget();
    }

    private void ShowMoveTarget(Vector2I gridPos)
    {
        moveTargetMarker.Position = new Vector2(gridPos.X * TileSize + 1, gridPos.Y * TileSize + 1);
        moveTargetMarker.Visible = true;
    }

    private void HideMoveTarget()
    {
        moveTargetMarker.Visible = false;
    }

    private void OnAbilityDetonated(AbilityData ability, Vector2I tile)
    {
        // Visual feedback for grenade explosion
        // For now, just flash the affected tiles
        GD.Print($"[Visual] {ability.Name} exploded at {tile}!");

        // Create a simple explosion visual
        var explosion = new ColorRect();
        explosion.Size = new Vector2((ability.Radius * 2 + 1) * TileSize, (ability.Radius * 2 + 1) * TileSize);
        explosion.Position = new Vector2(
            (tile.X - ability.Radius) * TileSize,
            (tile.Y - ability.Radius) * TileSize
        );
        explosion.Color = new Color(1.0f, 0.5f, 0.0f, 0.6f); // Orange flash
        AddChild(explosion);

        // Remove after a short delay using a timer
        var timer = GetTree().CreateTimer(0.3f);
        timer.Timeout += () => explosion.QueueFree();
    }

    private Vector2I ScreenToGrid(Vector2 screenPos)
    {
        // Convert screen position to world position using camera transform
        var worldPos = GetCanvasTransform().AffineInverse() * screenPos;
        return new Vector2I(
            (int)(worldPos.X) / TileSize,
            (int)(worldPos.Y) / TileSize
        );
    }
}
