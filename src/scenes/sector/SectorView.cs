using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FringeTactics;

/// <summary>
/// Sector map view - shows nodes and allows travel.
/// </summary>
public partial class SectorView : Control
{
    private Node2D mapContainer;
    private Control uiPanel;
    private Label locationLabel;
    private Label resourcesLabel;
    private Label nodeInfoLabel;
    private Button travelButton;
    private Button shipButton;
    private Button missionButton;
    private Button jobBoardButton;
    private Button menuButton;

    // Job Board panel
    private JobBoardPanel jobBoardPanel;
    private Label currentJobLabel;

    // Station Services panel
    private Button stationButton;
    private StationServicesPanel stationPanel;

    // Travel Log (Phase 5)
    private Label travelLogLabel;
    private List<string> travelLog = new();

    // Travel Animation
    private TravelAnimator travelAnimator;
    private TravelResult pendingTravelResult;
    private TravelPlan pendingTravelPlan;
    private int travelFromSystemId;

    private Dictionary<int, Button> nodeButtons = new();
    private int? selectedNodeId = null;

    // Visual settings
    private const float NODE_RADIUS = 20f;

    // Planet rendering assets
    private static Shader PlanetShader;
    private static Texture2D BaseSphere;
    private static List<Texture2D> NoiseTextures = new();
    private static List<Texture2D> LightTextures = new();
    private readonly Color ConnectionColor = new Color(0.3f, 0.3f, 0.4f);

    // Background system
    private static Texture2D BackgroundTexture;
    private TextureRect backgroundRect;

    public override void _Ready()
    {
        // Ensure SectorView fills the screen
        SetAnchorsPreset(LayoutPreset.FullRect);
        
        LoadPlanetAssets();
        LoadBackgroundAssets();
        CreateBackground();
        CreateUI();
        CreateJobBoardPanel();
        CreateStationPanel();
        DrawSector();
        UpdateDisplay();
        
        // Check for resumed travel animation (after encounter)
        CheckForResumedTravel();
    }

    private void LoadPlanetAssets()
    {
        if (PlanetShader != null) return;

        PlanetShader = GD.Load<Shader>("res://assets/shaders/planet.gdshader");
        BaseSphere = GD.Load<Texture2D>("res://assets/planets/sphere0.png");

        for (int i = 0; i <= 27; i++)
        {
            var path = $"res://assets/planets/noises/noise{i:D2}.png";
            if (FileAccess.FileExists(path)) NoiseTextures.Add(GD.Load<Texture2D>(path));
        }

        for (int i = 0; i <= 10; i++)
        {
            var path = $"res://assets/planets/lights/light{i}.png";
            if (FileAccess.FileExists(path)) LightTextures.Add(GD.Load<Texture2D>(path));
        }
    }

    private void LoadBackgroundAssets()
    {
        if (BackgroundTexture != null) return;

        var path = "res://assets/star_system/star_system_background3.jpeg";
        if (FileAccess.FileExists(path)) BackgroundTexture = GD.Load<Texture2D>(path);
    }

    private void CreateBackground()
    {
        backgroundRect = new TextureRect();
        backgroundRect.SetAnchorsPreset(LayoutPreset.FullRect);
        backgroundRect.ExpandMode = TextureRect.ExpandModeEnum.FitWidth;
        backgroundRect.StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered;
        backgroundRect.MouseFilter = MouseFilterEnum.Ignore;
        backgroundRect.ZIndex = -100; // Put it behind everything
        
        backgroundRect.Texture = BackgroundTexture;
        
        AddChild(backgroundRect);
    }


    private void CheckForResumedTravel()
    {
        if (!GameState.Instance.HasPendingResumedTravel) return;
        
        var (result, plan, fromSegment) = GameState.Instance.TakePendingResumedTravel();
        if (result == null || plan == null) return;
        
        var campaign = GameState.Instance?.Campaign;
        if (campaign?.World == null) return;
        
        // Build remaining path from the segment where encounter occurred
        var pathPositions = new List<Vector2>();
        var fullPath = plan.GetPath();
        
        for (int i = fromSegment; i < fullPath.Count; i++)
        {
            var system = campaign.World.GetSystem(fullPath[i]);
            if (system != null)
            {
                pathPositions.Add(system.Position);
            }
        }
        
        if (pathPositions.Count < 2)
        {
            // Already at destination, just refresh
            RefreshSector();
            return;
        }
        
        // Store for animation completion handler
        pendingTravelResult = result;
        pendingTravelPlan = plan;
        travelFromSystemId = fullPath[fromSegment];
        
        // Calculate encounter info if another encounter triggered
        int encounterSegment = -1;
        float encounterProgress = 1f;
        if (result.Status == TravelResultStatus.PausedForEncounter && result.PausedState != null)
        {
            encounterSegment = result.PausedState.CurrentSegmentIndex - fromSegment;
            encounterProgress = 0.2f + (float)GD.Randf() * 0.6f;
        }
        
        // Disable travel button during animation
        travelButton.Disabled = true;
        travelButton.Text = "Traveling...";
        
        // Start animation for remaining path
        travelAnimator.StartAnimation(pathPositions, encounterSegment, encounterProgress);
    }

    public override void _Process(double delta)
    {
        // Refresh resource display every frame (cheap, handles devtools changes)
        UpdateResourceDisplay();
    }

    private void UpdateResourceDisplay()
    {
        var campaign = GameState.Instance?.Campaign;
        if (campaign == null) return;

        // Phase 4: Add ship hull to resources
        var hullText = campaign.Ship != null
            ? $"\nHull: {campaign.Ship.Hull}/{campaign.Ship.MaxHull}"
            : "";

        resourcesLabel.Text = $"Money: ${campaign.Money}\n" +
                              $"Fuel: {campaign.Fuel}  |  Ammo: {campaign.Ammo}\n" +
                              $"Parts: {campaign.Parts}  |  Meds: {campaign.Meds}" +
                              hullText;
    }

    private void CreateUI()
    {
        // Map container (left side)
        mapContainer = new Node2D();
        mapContainer.Position = new Vector2(50, 50);
        AddChild(mapContainer);

        // Travel animator (child of map container for correct coordinates)
        travelAnimator = new TravelAnimator();
        travelAnimator.TravelAnimationCompleted += OnTravelAnimationCompleted;
        mapContainer.AddChild(travelAnimator);

        // UI Panel (right side) - anchored to right edge, full height
        uiPanel = new Control();
        uiPanel.AnchorLeft = 1.0f;
        uiPanel.AnchorRight = 1.0f;
        uiPanel.AnchorTop = 0.0f;
        uiPanel.AnchorBottom = 1.0f;
        uiPanel.OffsetLeft = -300;
        uiPanel.OffsetRight = 0;
        uiPanel.OffsetTop = 0;
        uiPanel.OffsetBottom = 0;
        AddChild(uiPanel);

        // Scroll container to handle overflow
        var scroll = new ScrollContainer();
        scroll.SetAnchorsPreset(LayoutPreset.FullRect);
        scroll.OffsetLeft = 10;
        scroll.OffsetRight = -10;
        scroll.OffsetTop = 10;
        scroll.OffsetBottom = -10;
        scroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
        uiPanel.AddChild(scroll);

        var vbox = new VBoxContainer();
        vbox.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        scroll.AddChild(vbox);

        // Location header
        locationLabel = new Label();
        locationLabel.AddThemeFontSizeOverride("font_size", 24);
        locationLabel.AddThemeColorOverride("font_color", Colors.Cyan);
        vbox.AddChild(locationLabel);

        AddSpacer(vbox, 15);

        // Resources
        resourcesLabel = new Label();
        resourcesLabel.AddThemeFontSizeOverride("font_size", 14);
        vbox.AddChild(resourcesLabel);

        AddSpacer(vbox, 20);

        // Selected node info
        var infoTitle = new Label();
        infoTitle.Text = "SELECTED";
        infoTitle.AddThemeFontSizeOverride("font_size", 16);
        infoTitle.AddThemeColorOverride("font_color", Colors.Yellow);
        vbox.AddChild(infoTitle);

        var nodeInfoScroll = new ScrollContainer();
        nodeInfoScroll.CustomMinimumSize = new Vector2(0, 200);
        nodeInfoScroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
        vbox.AddChild(nodeInfoScroll);

        nodeInfoLabel = new Label();
        nodeInfoLabel.AddThemeFontSizeOverride("font_size", 14);
        nodeInfoLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        nodeInfoScroll.AddChild(nodeInfoLabel);

        AddSpacer(vbox, 10);

        // Travel button
        travelButton = new Button();
        travelButton.Text = "Travel";
        travelButton.CustomMinimumSize = new Vector2(200, 40);
        travelButton.Pressed += OnTravelPressed;
        travelButton.Disabled = true;
        vbox.AddChild(travelButton);

        AddSpacer(vbox, 10);

        // Current job indicator
        currentJobLabel = new Label();
        currentJobLabel.AddThemeFontSizeOverride("font_size", 12);
        currentJobLabel.AddThemeColorOverride("font_color", Colors.Orange);
        currentJobLabel.CustomMinimumSize = new Vector2(0, 40);
        vbox.AddChild(currentJobLabel);

        AddSpacer(vbox, 10);

        // Job Board button
        jobBoardButton = new Button();
        jobBoardButton.Text = "Job Board";
        jobBoardButton.CustomMinimumSize = new Vector2(200, 40);
        jobBoardButton.Pressed += OnJobBoardPressed;
        vbox.AddChild(jobBoardButton);

        AddSpacer(vbox, 10);

        // Mission button (if at job target)
        missionButton = new Button();
        missionButton.Text = "Start Mission";
        missionButton.CustomMinimumSize = new Vector2(200, 40);
        missionButton.Pressed += OnMissionPressed;
        vbox.AddChild(missionButton);

        AddSpacer(vbox, 10);

        // Ship/Crew button
        shipButton = new Button();
        shipButton.Text = "Ship & Crew";
        shipButton.CustomMinimumSize = new Vector2(200, 40);
        shipButton.Pressed += OnShipPressed;
        vbox.AddChild(shipButton);

        AddSpacer(vbox, 10);

        // Station Services button
        stationButton = new Button();
        stationButton.Text = "Station Services";
        stationButton.CustomMinimumSize = new Vector2(200, 40);
        stationButton.Pressed += OnStationPressed;
        vbox.AddChild(stationButton);

        AddSpacer(vbox, 15);

        // Travel Log (Phase 5)
        var logTitle = new Label();
        logTitle.Text = "TRAVEL LOG";
        logTitle.AddThemeFontSizeOverride("font_size", 12);
        logTitle.AddThemeColorOverride("font_color", Colors.Gray);
        vbox.AddChild(logTitle);

        travelLogLabel = new Label();
        travelLogLabel.AddThemeFontSizeOverride("font_size", 11);
        travelLogLabel.CustomMinimumSize = new Vector2(0, 70);
        vbox.AddChild(travelLogLabel);

        AddSpacer(vbox, 15);

        // Menu button
        menuButton = new Button();
        menuButton.Text = "Abandon Campaign";
        menuButton.CustomMinimumSize = new Vector2(200, 30);
        menuButton.Pressed += OnMenuPressed;
        vbox.AddChild(menuButton);
    }

    private void AddSpacer(VBoxContainer parent, int height)
    {
        var spacer = new Control();
        spacer.CustomMinimumSize = new Vector2(0, height);
        parent.AddChild(spacer);
    }

    private void CreateJobBoardPanel()
    {
        jobBoardPanel = new JobBoardPanel();
        jobBoardPanel.OnJobAccepted += UpdateDisplay;
        AddChild(jobBoardPanel);
    }

    private void CreateStationPanel()
    {
        stationPanel = new StationServicesPanel();
        AddChild(stationPanel);
    }

    private void OnStationPressed()
    {
        stationPanel.Show();
    }

    private void OnJobBoardPressed()
    {
        jobBoardPanel.Show();
    }

    private void DrawSector()
    {
        var campaign = GameState.Instance?.Campaign;
        if (campaign?.World == null) return;

        var world = campaign.World;

        // Draw connections first (behind nodes)
        foreach (var system in world.GetAllSystems())
        {
            foreach (var connId in system.Connections)
            {
                if (connId > system.Id) // Draw each connection once
                {
                    var other = world.GetSystem(connId);
                    if (other != null)
                    {
                        DrawConnection(system.Position, other.Position);
                    }
                }
            }
        }

        // Draw systems
        foreach (var system in world.GetAllSystems())
        {
            CreateSystemButton(system, campaign.CurrentNodeId == system.Id);
        }
    }

    private void DrawConnection(Vector2 from, Vector2 to)
    {
        var line = new Line2D();
        line.AddPoint(from);
        line.AddPoint(to);
        line.Width = 2;
        line.DefaultColor = ConnectionColor;
        mapContainer.AddChild(line);
    }

    private void CreateSystemButton(StarSystem system, bool isCurrent)
    {
        var btn = new Button();
        float buttonSize = NODE_RADIUS * 2.5f;
        btn.CustomMinimumSize = new Vector2(buttonSize, buttonSize);
        btn.Position = system.Position - new Vector2(buttonSize / 2, buttonSize / 2);
        btn.Flat = true;
        
        // Remove all default button styling
        var emptyStyle = new StyleBoxEmpty();
        btn.AddThemeStyleboxOverride("normal", emptyStyle);
        btn.AddThemeStyleboxOverride("hover", emptyStyle);
        btn.AddThemeStyleboxOverride("pressed", emptyStyle);
        btn.AddThemeStyleboxOverride("focus", emptyStyle);
        btn.AddThemeStyleboxOverride("disabled", emptyStyle);

        // Create the planet visual
        var planetVisual = new TextureRect();
        planetVisual.SetAnchorsPreset(LayoutPreset.FullRect);
        planetVisual.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
        planetVisual.MouseFilter = MouseFilterEnum.Ignore;

        var material = new ShaderMaterial();
        material.Shader = PlanetShader;

        var sysRng = new Random(system.Id);

        var noiseTex = NoiseTextures.Count > 0 ? NoiseTextures[sysRng.Next(NoiseTextures.Count)] : null;
        var lightTex = LightTextures.Count > 0 ? LightTextures[sysRng.Next(LightTextures.Count)] : null;

        Color baseColor = GetSystemColor(system.Type);

        material.SetShaderParameter("sphere_texture", BaseSphere);
        material.SetShaderParameter("noise_texture", noiseTex);
        material.SetShaderParameter("light_texture", lightTex);
        material.SetShaderParameter("planet_color", baseColor);
        material.SetShaderParameter("rotation_speed", 0.02f + (float)sysRng.NextDouble() * 0.04f);
        material.SetShaderParameter("time_offset", (float)sysRng.NextDouble() * 10.0f);

        planetVisual.Texture = BaseSphere;
        planetVisual.Material = material;

        btn.AddChild(planetVisual);

        // System name label
        var nameLabel = new Label();
        nameLabel.Text = system.Name;
        nameLabel.Position = new Vector2(-20, buttonSize + 2);
        nameLabel.AddThemeFontSizeOverride("font_size", 10);
        nameLabel.HorizontalAlignment = HorizontalAlignment.Center;
        nameLabel.CustomMinimumSize = new Vector2(buttonSize + 40, 0);
        nameLabel.MouseFilter = MouseFilterEnum.Ignore;
        btn.AddChild(nameLabel);

        // Job indicator
        var campaign = GameState.Instance?.Campaign;
        if (campaign?.CurrentJob != null && campaign.CurrentJob.TargetNodeId == system.Id)
        {
            var jobIndicator = new Label();
            jobIndicator.Text = "!";
            jobIndicator.Position = new Vector2(buttonSize - 10, -5);
            jobIndicator.AddThemeFontSizeOverride("font_size", 16);
            jobIndicator.AddThemeColorOverride("font_color", Colors.Yellow);
            jobIndicator.MouseFilter = MouseFilterEnum.Ignore;
            btn.AddChild(jobIndicator);
        }

        var systemId = system.Id;
        btn.Pressed += () => OnNodeClicked(systemId);

        mapContainer.AddChild(btn);
        nodeButtons[system.Id] = btn;
    }

    private Color GetSystemColor(SystemType type)
    {
        return type switch
        {
            SystemType.Station => new Color(0.3f, 0.8f, 0.4f),      // Vibrant teal-green
            SystemType.Outpost => new Color(0.4f, 0.6f, 0.9f),       // Bright cobalt blue
            SystemType.Derelict => new Color(0.7f, 0.5f, 0.3f),      // Rusty orange-brown
            SystemType.Asteroid => new Color(0.8f, 0.6f, 0.3f),       // Golden sand
            SystemType.Nebula => new Color(0.6f, 0.4f, 0.8f),         // Deep purple
            SystemType.Contested => new Color(0.9f, 0.4f, 0.3f),      // Coral red
            _ => new Color(0.7f, 0.7f, 0.8f)                         // Soft blue-white
        };
    }

    private void OnNodeClicked(int nodeId)
    {
        selectedNodeId = nodeId;
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        var campaign = GameState.Instance?.Campaign;
        if (campaign == null) return;

        var currentSystem = campaign.GetCurrentSystem();

        // Location + Campaign Day (Phase 3)
        var dayText = campaign.Time?.FormatCurrentDay() ?? "Day 1";
        locationLabel.Text = $"@ {currentSystem?.Name ?? "Unknown"} | {dayText}";

        // Selected system info
        if (selectedNodeId.HasValue && campaign.World != null)
        {
            var system = campaign.World.GetSystem(selectedNodeId.Value);
            if (system != null)
            {
                var factionName = system.OwningFactionId != null
                    ? campaign.World.GetFactionName(system.OwningFactionId)
                    : "Unclaimed";

                var fuelCost = TravelPlanner.GetFuelCost(campaign, system.Id);
                var canTravel = TravelPlanner.CanTravel(campaign, system.Id);

                nodeInfoLabel.Text = $"{system.Name}\n" +
                                     $"Type: {system.Type}\n" +
                                     $"Faction: {factionName}\n" +
                                     $"Travel cost: {fuelCost} fuel";

                // Phase 1.1: Add metrics display
                if (system.Metrics != null)
                {
                    var metrics = system.Metrics;
                    nodeInfoLabel.Text += $"\n\n--- Metrics ---\n" +
                                          $"Security: {MetricBar(metrics.SecurityLevel)}\n" +
                                          $"Crime: {MetricBar(metrics.CriminalActivity)}\n" +
                                          $"Stability: {MetricBar(metrics.Stability)}\n" +
                                          $"Economy: {MetricBar(metrics.EconomicActivity)}\n" +
                                          $"Law: {MetricBar(metrics.LawEnforcementPresence)}";
                }

                // Phase 1.2: Add tags display
                if (system.Tags.Count > 0)
                {
                    var tagList = string.Join(", ", system.Tags.Take(5));
                    nodeInfoLabel.Text += $"\nTags: {tagList}";
                }

                // Phase 1.3: Add faction reputation
                if (!string.IsNullOrEmpty(system.OwningFactionId))
                {
                    var rep = campaign.GetFactionRep(system.OwningFactionId);
                    var repText = rep >= 50 ? "Friendly" : rep >= 0 ? "Neutral" : "Hostile";
                    nodeInfoLabel.Text += $"\nYour Rep: {rep} ({repText})";
                }

                // Phase 2: Route info (only for destinations, not current location)
                if (system.Id != campaign.CurrentNodeId)
                {
                    var route = campaign.World.GetRoute(campaign.CurrentNodeId, system.Id);
                    if (route != null)
                    {
                        // Phase 2.1: Route hazard
                        var hazardText = route.HazardLevel switch
                        {
                            0 => "Safe",
                            1 => "Low Risk",
                            2 => "Moderate",
                            3 => "Dangerous",
                            4 => "Very Dangerous",
                            5 => "Extreme",
                            _ => "Unknown"
                        };
                        nodeInfoLabel.Text += $"\n\n--- Route ---\nHazard: {hazardText} ({route.HazardLevel}/5)";

                        // Route tags
                        if (route.Tags.Count > 0)
                        {
                            nodeInfoLabel.Text += $"\nRoute: {string.Join(", ", route.Tags.Take(3))}";
                        }

                        // Encounter chance (uses full formula with system metrics)
                        var fromMetrics = campaign.World.GetSystemMetrics(campaign.CurrentNodeId);
                        var toMetrics = campaign.World.GetSystemMetrics(system.Id);
                        float encounterChance = TravelCosts.CalculateEncounterChance(route, fromMetrics, toMetrics);
                        nodeInfoLabel.Text += $"\nEncounter Chance: {encounterChance * 100:F0}%";
                    }
                }

                // Update travel button
                if (system.Id == campaign.CurrentNodeId)
                {
                    travelButton.Text = "You are here";
                    travelButton.Disabled = true;
                }
                else if (canTravel)
                {
                    travelButton.Text = $"Travel ({fuelCost} fuel)";
                    travelButton.Disabled = false;
                }
                else
                {
                    var reason = TravelPlanner.GetTravelBlockReason(campaign, system.Id);
                    travelButton.Text = reason ?? "Cannot travel";
                    travelButton.Disabled = true;
                }
            }
        }
        else
        {
            nodeInfoLabel.Text = "Click a node to select";
            travelButton.Text = "Travel";
            travelButton.Disabled = true;
        }

        // Current job display
        if (campaign.CurrentJob != null)
        {
            var targetSystem = campaign.World?.GetSystem(campaign.CurrentJob.TargetNodeId);
            currentJobLabel.Text = $"Active Job: {campaign.CurrentJob.Title}\n" +
                                   $"Target: {targetSystem?.Name ?? "Unknown"}";
            currentJobLabel.Visible = true;
            jobBoardButton.Disabled = true;
            jobBoardButton.Text = "Job Active";
        }
        else
        {
            currentJobLabel.Text = "";
            currentJobLabel.Visible = false;
            jobBoardButton.Disabled = false;
            jobBoardButton.Text = $"Job Board ({campaign.AvailableJobs.Count})";
        }

        // Mission button - show only if we have an active job and are at target
        if (campaign.CurrentJob != null && campaign.IsAtJobTarget())
        {
            missionButton.Visible = true;
            if (!campaign.CanStartMission())
            {
                missionButton.Text = campaign.GetMissionBlockReason();
                missionButton.Disabled = true;
            }
            else
            {
                missionButton.Text = $"Start Mission: {campaign.CurrentJob.Title}";
                missionButton.Disabled = false;
            }
        }
        else if (campaign.CurrentJob != null)
        {
            // Have job but not at target
            missionButton.Visible = true;
            var targetSystem = campaign.World?.GetSystem(campaign.CurrentJob.TargetNodeId);
            missionButton.Text = $"Travel to {targetSystem?.Name ?? "target"}";
            missionButton.Disabled = true;
        }
        else
        {
            // No job
            missionButton.Visible = false;
        }

        // Highlight selected node
        UpdateNodeHighlights(campaign);
    }

    private void UpdateNodeHighlights(CampaignState campaign)
    {
        if (campaign.World == null) return;

        float buttonSize = NODE_RADIUS * 2.5f;

        foreach (var kvp in nodeButtons)
        {
            var systemId = kvp.Key;
            var btn = kvp.Value;

            var system = campaign.World.GetSystem(systemId);
            if (system == null) continue;

            bool isCurrent = systemId == campaign.CurrentNodeId;
            bool isSelected = systemId == selectedNodeId;

            // Remove ALL existing ring panels from this button
            var panelsToRemove = new List<Panel>();
            foreach (var child in btn.GetChildren())
            {
                if (child is Panel p)
                {
                    panelsToRemove.Add(p);
                }
            }
            
            foreach (var panel in panelsToRemove)
            {
                panel.QueueFree();
            }

            // Only create ring for current or selected nodes
            if (isCurrent || isSelected)
            {
                var ringStyle = new StyleBoxFlat();
                ringStyle.BgColor = Colors.Transparent;
                ringStyle.CornerRadiusTopLeft = (int)buttonSize;
                ringStyle.CornerRadiusTopRight = (int)buttonSize;
                ringStyle.CornerRadiusBottomLeft = (int)buttonSize;
                ringStyle.CornerRadiusBottomRight = (int)buttonSize;

                if (isCurrent)
                {
                    ringStyle.BorderColor = Colors.White;
                    ringStyle.BorderWidthBottom = 3;
                    ringStyle.BorderWidthTop = 3;
                    ringStyle.BorderWidthLeft = 3;
                    ringStyle.BorderWidthRight = 3;
                }
                else if (isSelected)
                {
                    ringStyle.BorderColor = Colors.Yellow;
                    ringStyle.BorderWidthBottom = 2;
                    ringStyle.BorderWidthTop = 2;
                    ringStyle.BorderWidthLeft = 2;
                    ringStyle.BorderWidthRight = 2;
                }

                var ringPanel = new Panel();
                ringPanel.AddThemeStyleboxOverride("panel", ringStyle);
                ringPanel.SetAnchorsPreset(LayoutPreset.FullRect);
                ringPanel.MouseFilter = MouseFilterEnum.Ignore;
                btn.AddChild(ringPanel);
                btn.MoveChild(ringPanel, 1);
            }
        }
    }

    private void OnTravelPressed()
    {
        if (!selectedNodeId.HasValue) return;
        if (!GodotObject.IsInstanceValid(mapContainer)) return;
        if (!GodotObject.IsInstanceValid(travelAnimator))
        {
            travelAnimator = new TravelAnimator();
            travelAnimator.TravelAnimationCompleted += OnTravelAnimationCompleted;
            mapContainer.AddChild(travelAnimator);
        }
        if (travelAnimator.IsAnimating) return;

        var campaign = GameState.Instance?.Campaign;
        if (campaign?.World == null) return;

        var fromSystem = campaign.World.GetSystem(campaign.CurrentNodeId);
        var toSystem = campaign.World.GetSystem(selectedNodeId.Value);
        if (fromSystem == null || toSystem == null) return;

        // Store origin for animation
        travelFromSystemId = campaign.CurrentNodeId;

        // Execute travel in sim layer first
        var planner = new TravelPlanner(campaign.World);
        var plan = planner.PlanRoute(campaign.CurrentNodeId, selectedNodeId.Value);

        if (!plan.IsValid)
        {
            GD.Print($"[SectorView] Travel failed: {plan.InvalidReason}");
            return;
        }

        var executor = new TravelExecutor(campaign.Rng);
        var result = executor.Execute(plan, campaign);

        // Store result for after animation
        pendingTravelResult = result;
        pendingTravelPlan = plan;

        // Build path of waypoints from plan
        var pathPositions = new List<Vector2>();
        foreach (var systemId in plan.GetPath())
        {
            var system = campaign.World.GetSystem(systemId);
            if (system != null)
            {
                pathPositions.Add(system.Position);
            }
        }

        // Calculate encounter segment and progress
        int encounterSegment = -1;
        float encounterProgress = 1f;
        if (result.Status == TravelResultStatus.PausedForEncounter && result.PausedState != null)
        {
            encounterSegment = result.PausedState.CurrentSegmentIndex;
            encounterProgress = 0.2f + (float)GD.Randf() * 0.6f;
        }

        // Disable travel button during animation
        travelButton.Disabled = true;
        travelButton.Text = "Traveling...";

        // Start animation with full path
        travelAnimator.StartAnimation(pathPositions, encounterSegment, encounterProgress);
    }

    private void OnTravelAnimationCompleted(float encounterProgress)
    {
        var campaign = GameState.Instance?.Campaign;
        if (campaign == null || pendingTravelResult == null) return;

        var fromSystem = campaign.World?.GetSystem(travelFromSystemId);
        var toSystem = campaign.World?.GetSystem(pendingTravelPlan?.DestinationSystemId ?? 0);

        // Handle result based on status
        switch (pendingTravelResult.Status)
        {
            case TravelResultStatus.Completed:
                AddToTravelLog($"Traveled: {fromSystem?.Name} → {toSystem?.Name}");
                if (campaign.CurrentJob == null)
                {
                    campaign.RefreshJobsAtCurrentNode();
                }
                RefreshSector();
                break;

            case TravelResultStatus.PausedForEncounter:
                // Store state in GameState and transition to encounter
                GameState.Instance.SetPausedTravel(pendingTravelResult.PausedState, pendingTravelPlan);
                GameState.Instance.GoToEncounter();
                break;

            case TravelResultStatus.Interrupted:
                GD.Print($"[SectorView] Travel interrupted: {pendingTravelResult.InterruptReason}");
                RefreshSector();
                break;
        }

        // Clear pending state
        pendingTravelResult = null;
        pendingTravelPlan = null;
    }

    private void AddToTravelLog(string message)
    {
        travelLog.Insert(0, $"[Day {GameState.Instance.GetCampaignDay()}] {message}");
        if (travelLog.Count > 5) travelLog.RemoveAt(5);
        UpdateTravelLog();
    }

    private void UpdateTravelLog()
    {
        travelLogLabel.Text = string.Join("\n", travelLog);
    }

    private void RefreshSector()
    {
        // Clear and redraw
        foreach (var child in mapContainer.GetChildren())
        {
            child.QueueFree();
        }
        nodeButtons.Clear();
        selectedNodeId = null;

        // Redraw after a frame to let nodes be freed
        CallDeferred(nameof(DrawSector));
        CallDeferred(nameof(UpdateDisplay));
    }

    private void OnMissionPressed()
    {
        GameState.Instance.StartMission();
    }

    private void OnShipPressed()
    {
        GameState.Instance.GoToCampaignScreen();
    }

    private void OnMenuPressed()
    {
        GameState.Instance.GoToMainMenu();
    }

    /// <summary>
    /// Generate a visual bar representation for a 0-5 metric value.
    /// </summary>
    private static string MetricBar(int value)
    {
        string filled = new string('■', value);
        string empty = new string('□', 5 - value);
        return $"{filled}{empty} ({value})";
    }
}
