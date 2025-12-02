using Godot;
using System.Collections.Generic;

namespace FringeTactics;

public partial class MissionView : Node2D
{
    public const int TileSize = 32;
    private const string ActorViewScenePath = "res://src/scenes/mission/ActorView.tscn";
    private const string TimeStateWidgetScenePath = "res://src/scenes/mission/TimeStateWidget.tscn";

    private PackedScene actorViewScene;
    private PackedScene timeStateWidgetScene;

    public CombatState CombatState { get; private set; }
    private Dictionary<int, ActorView> actorViews = new(); // actorId -> ActorView
    private List<int> crewActorIds = new(); // ordered list of crew actor IDs for number key selection
    private List<int> selectedActorIds = new(); // supports multi-selection

    // Ability targeting
    private AbilityData pendingAbility = null; // ability waiting for target selection
    private Label abilityTargetingLabel;

    // Movement target marker
    private Node2D moveTargetMarker;
    private ColorRect moveTargetFill;
    private ColorRect moveTargetBorder;
    private Dictionary<int, Vector2I> actorMoveTargets = new(); // actorId -> target position

    // Box selection state
    private bool isDragSelecting = false;
    private Vector2 dragStartScreen;
    private Vector2 dragStartWorld;
    private ColorRect selectionBox;
    private const float DragThreshold = 5f;

    // Control groups (Ctrl+1-3 to save, 1-3 to recall)
    private Dictionary<int, List<int>> controlGroups = new(); // group number -> actor IDs

    // Fog of war overlay
    private Node2D fogLayer;
    private Dictionary<Vector2I, ColorRect> fogTiles = new();
    private bool fogDirty = true;

    // Debug overlay for visibility
    private Node2D visibilityDebugLayer;
    private Dictionary<Vector2I, ColorRect> debugTiles = new();
    private bool showVisibilityDebug = false;

    // Double-click detection
    private float lastClickTime = 0f;
    private int lastClickedActorId = -1;
    private const float DoubleClickThreshold = 0.3f;

    // Mission result tracking
    private bool missionVictory = false;

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
        SetupUI();
        SetupCamera();
        DrawGrid();
        CreateFogLayer();
        SpawnActorViews();
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

        // Set result text and color
        missionResultLabel.Text = victory ? "VICTORY!" : "DEFEAT!";
        missionResultLabel.AddThemeColorOverride("font_color", victory ? Colors.Green : Colors.Red);

        // Update button text based on mode
        var hasCampaign = GameState.Instance?.HasActiveCampaign() ?? false;
        restartButton.Text = hasCampaign ? "Continue" : "Restart Test Mission";

        // Build summary
        var stats = CombatState.Stats;
        var crewAlive = 0;
        var crewDead = 0;
        foreach (var actor in CombatState.Actors)
        {
            if (actor.Type == "crew")
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
        GD.Print($"Result: {(victory ? "VICTORY" : "DEFEAT")}");
        GD.Print($"Crew Alive: {crewAlive}, Dead: {crewDead}");
        GD.Print($"Player Shots: {stats.PlayerShotsFired}, Hits: {stats.PlayerHits}, Misses: {stats.PlayerMisses}, Accuracy: {stats.PlayerAccuracy:F1}%");
        GD.Print($"Enemy Shots: {stats.EnemyShotsFired}, Hits: {stats.EnemyHits}, Misses: {stats.EnemyMisses}, Accuracy: {stats.EnemyAccuracy:F1}%");
        GD.Print("========================\n");

        missionEndPanel.Visible = true;
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
            // Sandbox mode - create fresh combat state and reload
            GameState.Instance?.StartSandboxMission();
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
                        tile.Color = new Color(0.35f, 0.35f, 0.4f);
                        break;
                    case TileType.Void:
                        tile.Color = new Color(0.05f, 0.05f, 0.08f);
                        break;
                    case TileType.Floor:
                    default:
                        // Checkerboard pattern for floor
                        if ((x + y) % 2 == 0)
                        {
                            tile.Color = new Color(0.15f, 0.15f, 0.2f);
                        }
                        else
                        {
                            tile.Color = new Color(0.2f, 0.2f, 0.25f);
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
            if (actor.Type == "crew")
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

            if (actor.Type == "crew")
            {
                var color = crewColors[crewIndex % crewColors.Length];
                view.Setup(actor, color);
                crewActorIds.Add(actor.Id);
                crewIndex++;
            }
            else if (actor.Type == "enemy")
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
        // Unsubscribe from events to prevent disposed object access on scene reload
        if (CombatState != null)
        {
            CombatState.MissionEnded -= OnMissionEnded;
            CombatState.AbilitySystem.AbilityDetonated -= OnAbilityDetonated;
            CombatState.Visibility.VisibilityChanged -= OnVisibilityChanged;
        }
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
        
        // Update box selection if mouse is held (check for drag start or update existing drag)
        if (Input.IsMouseButtonPressed(MouseButton.Left) && pendingAbility == null)
        {
            UpdateBoxSelectionVisual();
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

    public override void _Input(InputEvent @event)
    {
        // Toggle pause with Space
        if (@event.IsActionPressed("pause_toggle"))
        {
            GD.Print("Pause toggle pressed!");
            CombatState.TimeSystem.TogglePause();
            return;
        }

        // Number key selection / control groups
        if (@event is InputEventKey keyEvent && keyEvent.Pressed)
        {
            bool ctrlHeld = Input.IsKeyPressed(Key.Ctrl);
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
                    SaveControlGroup(groupNum);
                }
                else
                {
                    RecallControlGroup(groupNum);
                }
                return;
            }

            if (keyEvent.Keycode == Key.Tab)
            {
                SelectAllCrew();
                return;
            }
        }

        // Toggle visibility debug overlay (F3)
        if (@event is InputEventKey f3Event && f3Event.Pressed && f3Event.Keycode == Key.F3)
        {
            ToggleVisibilityDebug();
            return;
        }

        // Grenade ability (G key)
        if (@event is InputEventKey gEvent && gEvent.Pressed && gEvent.Keycode == Key.G)
        {
            var grenade = Definitions.Abilities.Get("frag_grenade")?.ToAbilityData();
            if (grenade != null)
            {
                StartAbilityTargeting(grenade);
            }
            return;
        }

        // Cancel targeting with Escape
        if (@event is InputEventKey escEvent && escEvent.Pressed && escEvent.Keycode == Key.Escape)
        {
            CancelAbilityTargeting();
            return;
        }

        // Handle mouse input
        if (@event is InputEventMouseButton mouseEvent)
        {
            HandleMouseClick(mouseEvent);
        }
    }

    private void SaveControlGroup(int groupNum)
    {
        if (selectedActorIds.Count == 0)
        {
            GD.Print($"[ControlGroup] Cannot save empty selection to group {groupNum}");
            return;
        }

        controlGroups[groupNum] = new List<int>(selectedActorIds);
        GD.Print($"[ControlGroup] Saved {selectedActorIds.Count} units to group {groupNum}");
    }

    private void RecallControlGroup(int groupNum)
    {
        if (!controlGroups.TryGetValue(groupNum, out var actorIds) || actorIds.Count == 0)
        {
            // Fallback: select crew by index if no control group saved
            if (groupNum - 1 < crewActorIds.Count)
            {
                SelectActor(crewActorIds[groupNum - 1]);
            }
            return;
        }

        ClearSelection();
        foreach (var actorId in actorIds)
        {
            var actor = CombatState.GetActorById(actorId);
            if (actor != null && actor.State == ActorState.Alive)
            {
                AddToSelection(actorId);
            }
        }
        GD.Print($"[ControlGroup] Recalled group {groupNum}: {selectedActorIds.Count} units");
    }

    private void SelectAllCrew()
    {
        ClearSelection();
        foreach (var actorId in crewActorIds)
        {
            var actor = CombatState.GetActorById(actorId);
            if (actor != null && actor.State == ActorState.Alive && actorViews.ContainsKey(actorId))
            {
                selectedActorIds.Add(actorId);
                actorViews[actorId].SetSelected(true);
            }
        }
        GD.Print($"[Selection] Selected all {selectedActorIds.Count} crew");
    }

    private void AddToSelection(int actorId)
    {
        var actor = CombatState.GetActorById(actorId);
        if (actor == null || actor.State != ActorState.Alive)
            return;
        if (actor.Type != "crew")
            return;
        if (selectedActorIds.Contains(actorId))
            return;
        if (!actorViews.ContainsKey(actorId))
            return;

        selectedActorIds.Add(actorId);
        actorViews[actorId].SetSelected(true);
    }

    private void RemoveFromSelection(int actorId)
    {
        if (selectedActorIds.Remove(actorId) && actorViews.ContainsKey(actorId))
        {
            actorViews[actorId].SetSelected(false);
        }
    }

    private void SelectActor(int actorId)
    {
        // Select a single actor by ID (clears previous selection).
        GD.Print($"SelectActor: actorId={actorId}");
        ClearSelection();
        if (actorViews.ContainsKey(actorId))
        {
            selectedActorIds.Add(actorId);
            actorViews[actorId].SetSelected(true);
            
            // Set camera to follow this actor
            tacticalCamera.SetFollowTarget(actorViews[actorId]);
            
            GD.Print($"Actor {actorId} selected, selectedActorIds.Count={selectedActorIds.Count}");
        }
    }

    private void ClearSelection()
    {
        // Deselect all currently selected actors.
        foreach (var actorId in selectedActorIds)
        {
            if (actorViews.ContainsKey(actorId))
            {
                actorViews[actorId].SetSelected(false);
            }
        }
        selectedActorIds.Clear();
        
        // Clear camera follow target
        tacticalCamera.ClearFollowTarget();
    }

    private void HandleMouseClick(InputEventMouseButton @event)
    {
        var gridPos = ScreenToGrid(@event.Position);

        // If targeting an ability, left click confirms, right click cancels
        if (pendingAbility != null)
        {
            if (@event.Pressed)
            {
                if (@event.ButtonIndex == MouseButton.Left)
                {
                    ConfirmAbilityTarget(gridPos);
                }
                else if (@event.ButtonIndex == MouseButton.Right)
                {
                    CancelAbilityTargeting();
                }
            }
            return;
        }

        if (@event.ButtonIndex == MouseButton.Left)
        {
            if (@event.Pressed)
            {
                StartPotentialDrag(@event.Position);
            }
            else
            {
                FinishLeftClick(@event.Position, gridPos);
            }
        }
        else if (@event.ButtonIndex == MouseButton.Right && @event.Pressed)
        {
            HandleRightClick(gridPos);
        }
    }

    private void StartPotentialDrag(Vector2 screenPos)
    {
        dragStartScreen = screenPos;
        dragStartWorld = GetCanvasTransform().AffineInverse() * screenPos;
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

    private void UpdateBoxSelectionVisual()
    {
        if (!Input.IsMouseButtonPressed(MouseButton.Left))
        {
            return;
        }

        var currentScreen = GetViewport().GetMousePosition();
        var distance = (currentScreen - dragStartScreen).Length();

        if (!isDragSelecting && distance > DragThreshold)
        {
            isDragSelecting = true;
            selectionBox.Visible = true;
        }

        if (isDragSelecting)
        {
            var currentWorld = GetCanvasTransform().AffineInverse() * currentScreen;
            var minX = Mathf.Min(dragStartWorld.X, currentWorld.X);
            var minY = Mathf.Min(dragStartWorld.Y, currentWorld.Y);
            var maxX = Mathf.Max(dragStartWorld.X, currentWorld.X);
            var maxY = Mathf.Max(dragStartWorld.Y, currentWorld.Y);

            selectionBox.Position = new Vector2(minX, minY);
            selectionBox.Size = new Vector2(maxX - minX, maxY - minY);
        }
    }

    private void FinishBoxSelection(Vector2 endScreen)
    {
        isDragSelecting = false;
        selectionBox.Visible = false;

        var endWorld = GetCanvasTransform().AffineInverse() * endScreen;
        var rect = new Rect2(
            Mathf.Min(dragStartWorld.X, endWorld.X),
            Mathf.Min(dragStartWorld.Y, endWorld.Y),
            Mathf.Abs(endWorld.X - dragStartWorld.X),
            Mathf.Abs(endWorld.Y - dragStartWorld.Y)
        );

        bool shiftHeld = Input.IsKeyPressed(Key.Shift);
        if (!shiftHeld)
        {
            ClearSelection();
        }

        int addedCount = 0;
        foreach (var actor in CombatState.Actors)
        {
            if (actor.Type != "crew" || actor.State != ActorState.Alive)
                continue;

            var actorWorldPos = actor.GetVisualPosition(TileSize);
            var actorCenter = actorWorldPos + new Vector2(TileSize / 2f, TileSize / 2f);

            if (rect.HasPoint(actorCenter))
            {
                AddToSelection(actor.Id);
                addedCount++;
            }
        }

        if (addedCount > 0)
        {
            GD.Print($"[Selection] Box selected {addedCount} units, total: {selectedActorIds.Count}");
        }
    }

    private void HandleSelection(Vector2I gridPos, bool additive)
    {
        var clickedActor = CombatState.GetActorAtPosition(gridPos);
        float currentTime = Time.GetTicksMsec() / 1000f;

        if (clickedActor != null && clickedActor.Type == "crew" && clickedActor.State == ActorState.Alive)
        {
            // Check for double-click to select all crew
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
                if (selectedActorIds.Contains(clickedActor.Id))
                {
                    RemoveFromSelection(clickedActor.Id);
                    GD.Print($"[Selection] Removed actor {clickedActor.Id}, total: {selectedActorIds.Count}");
                }
                else
                {
                    AddToSelection(clickedActor.Id);
                    GD.Print($"[Selection] Added actor {clickedActor.Id}, total: {selectedActorIds.Count}");
                }
            }
            else
            {
                SelectActor(clickedActor.Id);
            }
        }
        else if (!additive)
        {
            ClearSelection();
            lastClickedActorId = -1;
        }
    }

    private void HandleRightClick(Vector2I gridPos)
    {
        GD.Print($"HandleRightClick: gridPos={gridPos}, selectedCount={selectedActorIds.Count}");

        // Check if clicking on an enemy actor
        var targetActor = CombatState.GetActorAtPosition(gridPos);
        if (targetActor != null && targetActor.State == ActorState.Alive)
        {
            // Can only target visible enemies
            if (!CombatState.Visibility.IsVisible(targetActor.GridPosition))
            {
                // Target not visible, treat as move order
                IssueGroupMoveOrder(gridPos);
                return;
            }

            // Check if target is an enemy (different type from selected)
            var isEnemy = false;
            foreach (var actorId in selectedActorIds)
            {
                var selected = CombatState.GetActorById(actorId);
                if (selected != null && selected.Type != targetActor.Type)
                {
                    isEnemy = true;
                    break;
                }
            }

            if (isEnemy)
            {
                // Issue attack order
                foreach (var actorId in selectedActorIds)
                {
                    CombatState.IssueAttackOrder(actorId, targetActor.Id);
                }
                return;
            }
        }

        // Otherwise, issue move order with formation
        IssueGroupMoveOrder(gridPos);
    }

    private void IssueGroupMoveOrder(Vector2I targetPos)
    {
        // Gather selected actors
        var selectedActors = new List<Actor>();
        foreach (var actorId in selectedActorIds)
        {
            var actor = CombatState.GetActorById(actorId);
            if (actor != null && actor.State == ActorState.Alive)
            {
                selectedActors.Add(actor);
            }
        }

        if (selectedActors.Count == 0)
            return;

        // Calculate formation destinations
        var destinations = FormationCalculator.CalculateGroupDestinations(
            selectedActors,
            targetPos,
            CombatState.MapState
        );

        // Issue individual orders
        foreach (var kvp in destinations)
        {
            CombatState.IssueMovementOrder(kvp.Key, kvp.Value);

            var actor = CombatState.GetActorById(kvp.Key);
            if (actor != null && actor.IsMoving)
            {
                actorMoveTargets[kvp.Key] = kvp.Value;
            }
        }

        GD.Print($"[Movement] Group move: {selectedActors.Count} units to formation around {targetPos}");
        UpdateMoveTargetMarker();
    }
    
    private void UpdateMoveTargetMarker()
    {
        // Show marker at the target of the first selected actor that is moving
        foreach (var actorId in selectedActorIds)
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
        
        // No selected actor is moving, hide marker
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

    // === Ability Targeting ===

    private void StartAbilityTargeting(AbilityData ability)
    {
        // Need exactly one actor selected
        if (selectedActorIds.Count != 1)
        {
            GD.Print("[Ability] Select exactly one crew member to use ability");
            return;
        }

        var actorId = selectedActorIds[0];
        var actor = CombatState.GetActorById(actorId);
        if (actor == null || actor.Type != "crew")
        {
            GD.Print("[Ability] Only crew can use abilities");
            return;
        }

        // Check cooldown
        if (CombatState.AbilitySystem.IsOnCooldown(actorId, ability.Id))
        {
            var remaining = CombatState.AbilitySystem.GetCooldownRemaining(actorId, ability.Id);
            GD.Print($"[Ability] {ability.Name} on cooldown: {remaining} ticks remaining");
            return;
        }

        pendingAbility = ability;
        abilityTargetingLabel.Text = $"Targeting: {ability.Name} (Range: {ability.Range})\nLeft Click to confirm, Right Click to cancel";
        abilityTargetingLabel.Visible = true;
        GD.Print($"[Ability] Targeting {ability.Name} - click to select target tile");
    }

    private void ConfirmAbilityTarget(Vector2I targetTile)
    {
        if (pendingAbility == null || selectedActorIds.Count == 0)
        {
            CancelAbilityTargeting();
            return;
        }

        var actorId = selectedActorIds[0];
        var success = CombatState.IssueAbilityOrder(actorId, pendingAbility, targetTile);

        if (success)
        {
            GD.Print($"[Ability] {pendingAbility.Name} launched at {targetTile}!");
        }
        else
        {
            GD.Print($"[Ability] Cannot use {pendingAbility.Name} at {targetTile} (out of range?)");
        }

        CancelAbilityTargeting();
    }

    private void CancelAbilityTargeting()
    {
        pendingAbility = null;
        abilityTargetingLabel.Visible = false;
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
