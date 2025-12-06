using Godot;
using System.Collections.Generic;

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
    private Panel jobBoardPanel;
    private VBoxContainer jobListContainer;
    private Label currentJobLabel;
    private Button closeJobBoardButton;

    // Station Services panel
    private Button stationButton;
    private Panel stationPanel;
    private Label stationFeedbackLabel;

    private Dictionary<int, Button> nodeButtons = new();
    private int? selectedNodeId = null;

    // Visual settings
    private const float NODE_RADIUS = 20f;
    private readonly Color CurrentNodeColor = Colors.Cyan;
    private readonly Color SelectedNodeColor = Colors.Yellow;
    private readonly Color StationColor = Colors.Green;
    private readonly Color OutpostColor = Colors.LightBlue;
    private readonly Color DerelictColor = Colors.Gray;
    private readonly Color AsteroidColor = Colors.Orange;
    private readonly Color NebulaColor = Colors.Purple;
    private readonly Color ContestedColor = Colors.Red;
    private readonly Color ConnectionColor = new Color(0.3f, 0.3f, 0.4f);

    public override void _Ready()
    {
        CreateUI();
        CreateJobBoardPanel();
        CreateStationPanel();
        DrawSector();
        UpdateDisplay();
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

        resourcesLabel.Text = $"Money: ${campaign.Money}\n" +
                              $"Fuel: {campaign.Fuel}  |  Ammo: {campaign.Ammo}\n" +
                              $"Parts: {campaign.Parts}  |  Meds: {campaign.Meds}";
    }

    private void CreateUI()
    {
        // Map container (left side)
        mapContainer = new Node2D();
        mapContainer.Position = new Vector2(50, 50);
        AddChild(mapContainer);

        // UI Panel (right side)
        uiPanel = new Control();
        uiPanel.SetAnchorsPreset(LayoutPreset.RightWide);
        uiPanel.OffsetLeft = -300;
        AddChild(uiPanel);

        var vbox = new VBoxContainer();
        vbox.Position = new Vector2(20, 20);
        vbox.CustomMinimumSize = new Vector2(260, 0);
        uiPanel.AddChild(vbox);

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

        nodeInfoLabel = new Label();
        nodeInfoLabel.AddThemeFontSizeOverride("font_size", 14);
        nodeInfoLabel.CustomMinimumSize = new Vector2(0, 80);
        vbox.AddChild(nodeInfoLabel);

        AddSpacer(vbox, 20);

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

        AddSpacer(vbox, 30);

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
        // Create overlay panel for job board
        jobBoardPanel = new Panel();
        jobBoardPanel.SetAnchorsPreset(LayoutPreset.Center);
        jobBoardPanel.CustomMinimumSize = new Vector2(400, 350);
        jobBoardPanel.Position = new Vector2(-200, -175);
        jobBoardPanel.Visible = false;
        AddChild(jobBoardPanel);

        var panelStyle = new StyleBoxFlat();
        panelStyle.BgColor = new Color(0.1f, 0.1f, 0.15f, 0.95f);
        panelStyle.BorderWidthTop = 2;
        panelStyle.BorderWidthBottom = 2;
        panelStyle.BorderWidthLeft = 2;
        panelStyle.BorderWidthRight = 2;
        panelStyle.BorderColor = Colors.Cyan;
        panelStyle.CornerRadiusTopLeft = 8;
        panelStyle.CornerRadiusTopRight = 8;
        panelStyle.CornerRadiusBottomLeft = 8;
        panelStyle.CornerRadiusBottomRight = 8;
        jobBoardPanel.AddThemeStyleboxOverride("panel", panelStyle);

        var vbox = new VBoxContainer();
        vbox.SetAnchorsPreset(LayoutPreset.FullRect);
        vbox.OffsetLeft = 15;
        vbox.OffsetRight = -15;
        vbox.OffsetTop = 15;
        vbox.OffsetBottom = -15;
        jobBoardPanel.AddChild(vbox);

        // Title
        var title = new Label();
        title.Text = "JOB BOARD";
        title.AddThemeFontSizeOverride("font_size", 20);
        title.AddThemeColorOverride("font_color", Colors.Cyan);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(title);

        AddSpacer(vbox, 10);

        // Scrollable job list
        var scroll = new ScrollContainer();
        scroll.CustomMinimumSize = new Vector2(0, 220);
        scroll.SizeFlagsVertical = SizeFlags.ExpandFill;
        vbox.AddChild(scroll);

        jobListContainer = new VBoxContainer();
        jobListContainer.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        scroll.AddChild(jobListContainer);

        AddSpacer(vbox, 10);

        // Close button
        closeJobBoardButton = new Button();
        closeJobBoardButton.Text = "Close";
        closeJobBoardButton.CustomMinimumSize = new Vector2(100, 35);
        closeJobBoardButton.Pressed += () => jobBoardPanel.Visible = false;
        vbox.AddChild(closeJobBoardButton);
    }

    private void CreateStationPanel()
    {
        stationPanel = new Panel();
        stationPanel.SetAnchorsPreset(LayoutPreset.Center);
        stationPanel.CustomMinimumSize = new Vector2(350, 400);
        stationPanel.Position = new Vector2(-175, -200);
        stationPanel.Visible = false;
        AddChild(stationPanel);

        var panelStyle = new StyleBoxFlat();
        panelStyle.BgColor = new Color(0.1f, 0.12f, 0.1f, 0.95f);
        panelStyle.BorderWidthTop = 2;
        panelStyle.BorderWidthBottom = 2;
        panelStyle.BorderWidthLeft = 2;
        panelStyle.BorderWidthRight = 2;
        panelStyle.BorderColor = Colors.Green;
        panelStyle.CornerRadiusTopLeft = 8;
        panelStyle.CornerRadiusTopRight = 8;
        panelStyle.CornerRadiusBottomLeft = 8;
        panelStyle.CornerRadiusBottomRight = 8;
        stationPanel.AddThemeStyleboxOverride("panel", panelStyle);

        var vbox = new VBoxContainer();
        vbox.SetAnchorsPreset(LayoutPreset.FullRect);
        vbox.OffsetLeft = 15;
        vbox.OffsetRight = -15;
        vbox.OffsetTop = 15;
        vbox.OffsetBottom = -15;
        stationPanel.AddChild(vbox);

        // Title
        var title = new Label();
        title.Text = "STATION SERVICES";
        title.AddThemeFontSizeOverride("font_size", 20);
        title.AddThemeColorOverride("font_color", Colors.Green);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(title);

        AddSpacer(vbox, 15);

        // Shop section
        var shopLabel = new Label();
        shopLabel.Text = "SUPPLY SHOP";
        shopLabel.AddThemeFontSizeOverride("font_size", 14);
        shopLabel.AddThemeColorOverride("font_color", Colors.Yellow);
        vbox.AddChild(shopLabel);

        var shopHbox = new HBoxContainer();
        vbox.AddChild(shopHbox);

        var buyAmmoBtn = new Button();
        buyAmmoBtn.Text = "Buy Ammo ($20)";
        buyAmmoBtn.CustomMinimumSize = new Vector2(140, 35);
        buyAmmoBtn.Pressed += () => BuyResource("ammo", 20, 10);
        shopHbox.AddChild(buyAmmoBtn);

        var buyMedsBtn = new Button();
        buyMedsBtn.Text = "Buy Meds ($30)";
        buyMedsBtn.CustomMinimumSize = new Vector2(140, 35);
        buyMedsBtn.Pressed += () => BuyResource("meds", 30, 2);
        shopHbox.AddChild(buyMedsBtn);

        AddSpacer(vbox, 10);

        // Fuel Depot
        var fuelLabel = new Label();
        fuelLabel.Text = "FUEL DEPOT";
        fuelLabel.AddThemeFontSizeOverride("font_size", 14);
        fuelLabel.AddThemeColorOverride("font_color", Colors.Yellow);
        vbox.AddChild(fuelLabel);

        var buyFuelBtn = new Button();
        buyFuelBtn.Text = "Buy Fuel ($15 for 20)";
        buyFuelBtn.CustomMinimumSize = new Vector2(200, 35);
        buyFuelBtn.Pressed += () => BuyResource("fuel", 15, 20);
        vbox.AddChild(buyFuelBtn);

        AddSpacer(vbox, 10);

        // Repair Yard
        var repairLabel = new Label();
        repairLabel.Text = "REPAIR YARD";
        repairLabel.AddThemeFontSizeOverride("font_size", 14);
        repairLabel.AddThemeColorOverride("font_color", Colors.Yellow);
        vbox.AddChild(repairLabel);

        var repairBtn = new Button();
        repairBtn.Text = "Repair Hull (10 Parts)";
        repairBtn.CustomMinimumSize = new Vector2(200, 35);
        repairBtn.Pressed += OnRepairShip;
        vbox.AddChild(repairBtn);

        AddSpacer(vbox, 10);

        // Recruitment
        var recruitLabel = new Label();
        recruitLabel.Text = "RECRUITMENT";
        recruitLabel.AddThemeFontSizeOverride("font_size", 14);
        recruitLabel.AddThemeColorOverride("font_color", Colors.Yellow);
        vbox.AddChild(recruitLabel);

        var recruitHbox = new HBoxContainer();
        vbox.AddChild(recruitHbox);

        var hireSoldierBtn = new Button();
        hireSoldierBtn.Text = "Soldier ($50)";
        hireSoldierBtn.CustomMinimumSize = new Vector2(100, 35);
        hireSoldierBtn.Pressed += () => HireCrew(CrewRole.Soldier, 50);
        recruitHbox.AddChild(hireSoldierBtn);

        var hireMedicBtn = new Button();
        hireMedicBtn.Text = "Medic ($60)";
        hireMedicBtn.CustomMinimumSize = new Vector2(100, 35);
        hireMedicBtn.Pressed += () => HireCrew(CrewRole.Medic, 60);
        recruitHbox.AddChild(hireMedicBtn);

        var hireTechBtn = new Button();
        hireTechBtn.Text = "Tech ($60)";
        hireTechBtn.CustomMinimumSize = new Vector2(100, 35);
        hireTechBtn.Pressed += () => HireCrew(CrewRole.Tech, 60);
        recruitHbox.AddChild(hireTechBtn);

        AddSpacer(vbox, 10);

        // Feedback label
        stationFeedbackLabel = new Label();
        stationFeedbackLabel.AddThemeFontSizeOverride("font_size", 12);
        stationFeedbackLabel.CustomMinimumSize = new Vector2(0, 40);
        vbox.AddChild(stationFeedbackLabel);

        AddSpacer(vbox, 10);

        // Close button
        var closeBtn = new Button();
        closeBtn.Text = "Close";
        closeBtn.CustomMinimumSize = new Vector2(100, 35);
        closeBtn.Pressed += () => stationPanel.Visible = false;
        vbox.AddChild(closeBtn);
    }

    private void BuyResource(string type, int cost, int amount)
    {
        var campaign = GameState.Instance?.Campaign;
        if (campaign == null) return;

        if (campaign.Money < cost)
        {
            stationFeedbackLabel.Text = "Not enough money!";
            stationFeedbackLabel.AddThemeColorOverride("font_color", Colors.Red);
            return;
        }

        campaign.Money -= cost;
        switch (type)
        {
            case "ammo":
                campaign.Ammo += amount;
                stationFeedbackLabel.Text = $"Bought {amount} ammo.";
                break;
            case "meds":
                campaign.Meds += amount;
                stationFeedbackLabel.Text = $"Bought {amount} meds.";
                break;
            case "fuel":
                campaign.Fuel += amount;
                stationFeedbackLabel.Text = $"Bought {amount} fuel.";
                break;
        }
        stationFeedbackLabel.AddThemeColorOverride("font_color", Colors.Green);
    }

    private void OnRepairShip()
    {
        var campaign = GameState.Instance?.Campaign;
        if (campaign == null) return;

        if (campaign.Ship == null)
        {
            stationFeedbackLabel.Text = "No ship to repair!";
            stationFeedbackLabel.AddThemeColorOverride("font_color", Colors.Red);
            return;
        }

        if (campaign.Parts < 10)
        {
            stationFeedbackLabel.Text = "Not enough parts! (Need 10)";
            stationFeedbackLabel.AddThemeColorOverride("font_color", Colors.Red);
            return;
        }

        if (campaign.Ship.Hull >= campaign.Ship.MaxHull)
        {
            stationFeedbackLabel.Text = "Hull already at maximum!";
            stationFeedbackLabel.AddThemeColorOverride("font_color", Colors.Yellow);
            return;
        }

        campaign.Parts -= 10;
        int repaired = System.Math.Min(20, campaign.Ship.MaxHull - campaign.Ship.Hull);
        campaign.Ship.Hull += repaired;
        stationFeedbackLabel.Text = $"Repaired {repaired} hull damage.";
        stationFeedbackLabel.AddThemeColorOverride("font_color", Colors.Green);
    }

    private void OnStationPressed()
    {
        stationFeedbackLabel.Text = "";
        stationPanel.Visible = true;
    }

    private static readonly string[] RecruitNames = {
        "Riley", "Quinn", "Avery", "Blake", "Cameron", "Dakota", "Ellis", "Finley",
        "Harper", "Jade", "Kai", "Logan", "Mason", "Nova", "Parker", "Reese",
        "Sage", "Taylor", "Val", "Winter"
    };

    private void HireCrew(CrewRole role, int cost)
    {
        var campaign = GameState.Instance?.Campaign;
        if (campaign == null) return;

        if (campaign.Money < cost)
        {
            stationFeedbackLabel.Text = "Not enough money!";
            stationFeedbackLabel.AddThemeColorOverride("font_color", Colors.Red);
            return;
        }

        // Check max crew (6 alive)
        if (campaign.GetAliveCrew().Count >= 6)
        {
            stationFeedbackLabel.Text = "Crew roster full! (Max 6)";
            stationFeedbackLabel.AddThemeColorOverride("font_color", Colors.Red);
            return;
        }

        // Pick a random name
        var name = RecruitNames[GD.RandRange(0, RecruitNames.Length - 1)];
        var hired = campaign.HireCrew(name, role, cost);
        
        if (hired != null)
        {
            stationFeedbackLabel.Text = $"Hired {name} ({role})!";
            stationFeedbackLabel.AddThemeColorOverride("font_color", Colors.Green);
        }
    }

    private void PopulateJobBoard()
    {
        // Clear existing job entries
        foreach (var child in jobListContainer.GetChildren())
        {
            child.QueueFree();
        }

        var campaign = GameState.Instance?.Campaign;
        if (campaign == null) return;

        if (campaign.AvailableJobs.Count == 0)
        {
            var noJobsLabel = new Label();
            noJobsLabel.Text = "No jobs available at this location.";
            noJobsLabel.AddThemeFontSizeOverride("font_size", 14);
            noJobsLabel.AddThemeColorOverride("font_color", Colors.Gray);
            jobListContainer.AddChild(noJobsLabel);
            return;
        }

        foreach (var job in campaign.AvailableJobs)
        {
            CreateJobEntry(job, campaign);
        }
    }

    private void CreateJobEntry(Job job, CampaignState campaign)
    {
        var container = new PanelContainer();
        container.CustomMinimumSize = new Vector2(0, 70);

        var style = new StyleBoxFlat();
        style.BgColor = new Color(0.15f, 0.15f, 0.2f);
        style.CornerRadiusTopLeft = 4;
        style.CornerRadiusTopRight = 4;
        style.CornerRadiusBottomLeft = 4;
        style.CornerRadiusBottomRight = 4;
        container.AddThemeStyleboxOverride("panel", style);

        var hbox = new HBoxContainer();
        hbox.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        container.AddChild(hbox);

        // Job info
        var infoVbox = new VBoxContainer();
        infoVbox.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        hbox.AddChild(infoVbox);

        var titleLabel = new Label();
        titleLabel.Text = $"{job.Title} [{job.GetDifficultyDisplay()}]";
        titleLabel.AddThemeFontSizeOverride("font_size", 14);
        var diffColor = job.Difficulty switch
        {
            JobDifficulty.Easy => Colors.Green,
            JobDifficulty.Medium => Colors.Yellow,
            JobDifficulty.Hard => Colors.Red,
            _ => Colors.White
        };
        titleLabel.AddThemeColorOverride("font_color", diffColor);
        infoVbox.AddChild(titleLabel);

        var targetSystem = campaign.World?.GetSystem(job.TargetNodeId);
        var targetLabel = new Label();
        targetLabel.Text = $"Target: {targetSystem?.Name ?? "Unknown"}";
        targetLabel.AddThemeFontSizeOverride("font_size", 12);
        infoVbox.AddChild(targetLabel);

        var rewardLabel = new Label();
        rewardLabel.Text = $"Reward: {job.Reward}";
        rewardLabel.AddThemeFontSizeOverride("font_size", 12);
        rewardLabel.AddThemeColorOverride("font_color", Colors.Gold);
        infoVbox.AddChild(rewardLabel);

        // Accept button
        var acceptBtn = new Button();
        acceptBtn.Text = "Accept";
        acceptBtn.CustomMinimumSize = new Vector2(70, 50);
        acceptBtn.Pressed += () => OnAcceptJob(job);
        hbox.AddChild(acceptBtn);

        jobListContainer.AddChild(container);

        // Add small spacer between entries
        var spacer = new Control();
        spacer.CustomMinimumSize = new Vector2(0, 5);
        jobListContainer.AddChild(spacer);
    }

    private void OnAcceptJob(Job job)
    {
        var campaign = GameState.Instance?.Campaign;
        if (campaign == null) return;

        if (campaign.AcceptJob(job))
        {
            // Close job board and update display
            jobBoardPanel.Visible = false;
            UpdateDisplay();
        }
    }

    private void OnJobBoardPressed()
    {
        PopulateJobBoard();
        jobBoardPanel.Visible = true;
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
        btn.CustomMinimumSize = new Vector2(NODE_RADIUS * 2, NODE_RADIUS * 2);
        btn.Position = system.Position - new Vector2(NODE_RADIUS, NODE_RADIUS);
        btn.Text = ""; // We'll draw custom visuals

        // Style based on type
        var color = GetSystemColor(system.Type);
        if (isCurrent)
        {
            color = CurrentNodeColor;
        }

        // Create a simple colored panel as button content
        var panel = new Panel();
        panel.SetAnchorsPreset(LayoutPreset.FullRect);
        panel.MouseFilter = MouseFilterEnum.Ignore; // Don't block button clicks

        var style = new StyleBoxFlat();
        style.BgColor = color;
        style.CornerRadiusTopLeft = (int)NODE_RADIUS;
        style.CornerRadiusTopRight = (int)NODE_RADIUS;
        style.CornerRadiusBottomLeft = (int)NODE_RADIUS;
        style.CornerRadiusBottomRight = (int)NODE_RADIUS;

        if (isCurrent)
        {
            style.BorderWidthTop = 3;
            style.BorderWidthBottom = 3;
            style.BorderWidthLeft = 3;
            style.BorderWidthRight = 3;
            style.BorderColor = Colors.White;
        }

        panel.AddThemeStyleboxOverride("panel", style);
        btn.AddChild(panel);

        // System name label
        var nameLabel = new Label();
        nameLabel.Text = system.Name;
        nameLabel.Position = new Vector2(-30, NODE_RADIUS * 2 + 5);
        nameLabel.AddThemeFontSizeOverride("font_size", 10);
        nameLabel.HorizontalAlignment = HorizontalAlignment.Center;
        nameLabel.CustomMinimumSize = new Vector2(100, 0);
        nameLabel.MouseFilter = MouseFilterEnum.Ignore;
        btn.AddChild(nameLabel);

        // Job indicator (check if any station in system has jobs - for now just show if current job targets this)
        var campaign = GameState.Instance?.Campaign;
        if (campaign?.CurrentJob != null && campaign.CurrentJob.TargetNodeId == system.Id)
        {
            var jobIndicator = new Label();
            jobIndicator.Text = "!";
            jobIndicator.Position = new Vector2(NODE_RADIUS * 2 - 5, -5);
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
            SystemType.Station => StationColor,
            SystemType.Outpost => OutpostColor,
            SystemType.Derelict => DerelictColor,
            SystemType.Asteroid => AsteroidColor,
            SystemType.Nebula => NebulaColor,
            SystemType.Contested => ContestedColor,
            _ => Colors.White
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

        // Location
        locationLabel.Text = $"@ {currentSystem?.Name ?? "Unknown"}";

        // Resources
        resourcesLabel.Text = $"Money: ${campaign.Money}\n" +
                              $"Fuel: {campaign.Fuel}  |  Ammo: {campaign.Ammo}\n" +
                              $"Parts: {campaign.Parts}  |  Meds: {campaign.Meds}";

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
        
        foreach (var kvp in nodeButtons)
        {
            var systemId = kvp.Key;
            var btn = kvp.Value;
            var panel = btn.GetChild<Panel>(0);
            if (panel == null) continue;

            var style = panel.GetThemeStylebox("panel") as StyleBoxFlat;
            if (style == null) continue;

            var system = campaign.World.GetSystem(systemId);
            if (system == null) continue;

            // Reset border
            style.BorderWidthTop = 0;
            style.BorderWidthBottom = 0;
            style.BorderWidthLeft = 0;
            style.BorderWidthRight = 0;

            if (systemId == campaign.CurrentNodeId)
            {
                style.BgColor = CurrentNodeColor;
                style.BorderWidthTop = 3;
                style.BorderWidthBottom = 3;
                style.BorderWidthLeft = 3;
                style.BorderWidthRight = 3;
                style.BorderColor = Colors.White;
            }
            else if (systemId == selectedNodeId)
            {
                style.BgColor = SelectedNodeColor;
                style.BorderWidthTop = 2;
                style.BorderWidthBottom = 2;
                style.BorderWidthLeft = 2;
                style.BorderWidthRight = 2;
                style.BorderColor = Colors.Yellow;
            }
            else
            {
                style.BgColor = GetSystemColor(system.Type);
            }
        }
    }

    private void OnTravelPressed()
    {
        if (!selectedNodeId.HasValue) return;

        if (GameState.Instance.TravelTo(selectedNodeId.Value))
        {
            // Refresh the view
            RefreshSector();
        }
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
}
