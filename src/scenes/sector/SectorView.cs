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

        resourcesLabel.Text = $"Fuel: {campaign.Fuel}\n" +
                              $"Money: ${campaign.Money}\n" +
                              $"Ammo: {campaign.Ammo}";
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

        var targetNode = campaign.Sector.GetNode(job.TargetNodeId);
        var targetLabel = new Label();
        targetLabel.Text = $"Target: {targetNode?.Name ?? "Unknown"}";
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
        if (campaign?.Sector == null) return;

        var sector = campaign.Sector;

        // Draw connections first (behind nodes)
        foreach (var node in sector.Nodes)
        {
            foreach (var connId in node.Connections)
            {
                if (connId > node.Id) // Draw each connection once
                {
                    var other = sector.GetNode(connId);
                    if (other != null)
                    {
                        DrawConnection(node.Position, other.Position);
                    }
                }
            }
        }

        // Draw nodes
        foreach (var node in sector.Nodes)
        {
            CreateNodeButton(node, campaign.CurrentNodeId == node.Id);
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

    private void CreateNodeButton(SectorNode node, bool isCurrent)
    {
        var btn = new Button();
        btn.CustomMinimumSize = new Vector2(NODE_RADIUS * 2, NODE_RADIUS * 2);
        btn.Position = node.Position - new Vector2(NODE_RADIUS, NODE_RADIUS);
        btn.Text = ""; // We'll draw custom visuals

        // Style based on type
        var color = GetNodeColor(node.Type);
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

        // Node name label
        var nameLabel = new Label();
        nameLabel.Text = node.Name;
        nameLabel.Position = new Vector2(-30, NODE_RADIUS * 2 + 5);
        nameLabel.AddThemeFontSizeOverride("font_size", 10);
        nameLabel.HorizontalAlignment = HorizontalAlignment.Center;
        nameLabel.CustomMinimumSize = new Vector2(100, 0);
        nameLabel.MouseFilter = MouseFilterEnum.Ignore;
        btn.AddChild(nameLabel);

        // Job indicator
        if (node.HasJob)
        {
            var jobIndicator = new Label();
            jobIndicator.Text = "!";
            jobIndicator.Position = new Vector2(NODE_RADIUS * 2 - 5, -5);
            jobIndicator.AddThemeFontSizeOverride("font_size", 16);
            jobIndicator.AddThemeColorOverride("font_color", Colors.Yellow);
            jobIndicator.MouseFilter = MouseFilterEnum.Ignore;
            btn.AddChild(jobIndicator);
        }

        var nodeId = node.Id;
        btn.Pressed += () => OnNodeClicked(nodeId);

        mapContainer.AddChild(btn);
        nodeButtons[node.Id] = btn;
    }

    private Color GetNodeColor(NodeType type)
    {
        return type switch
        {
            NodeType.Station => StationColor,
            NodeType.Outpost => OutpostColor,
            NodeType.Derelict => DerelictColor,
            NodeType.Asteroid => AsteroidColor,
            NodeType.Nebula => NebulaColor,
            NodeType.Contested => ContestedColor,
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

        var currentNode = campaign.GetCurrentNode();

        // Location
        locationLabel.Text = $"@ {currentNode?.Name ?? "Unknown"}";

        // Resources
        resourcesLabel.Text = $"Fuel: {campaign.Fuel}\n" +
                              $"Money: ${campaign.Money}\n" +
                              $"Ammo: {campaign.Ammo}";

        // Selected node info
        if (selectedNodeId.HasValue)
        {
            var node = campaign.Sector.GetNode(selectedNodeId.Value);
            if (node != null)
            {
                var factionName = node.FactionId != null
                    ? campaign.Sector.Factions.GetValueOrDefault(node.FactionId, "Unknown")
                    : "Unclaimed";

                var fuelCost = TravelSystem.CalculateFuelCost(campaign.Sector, campaign.CurrentNodeId, node.Id);
                var canTravel = TravelSystem.CanTravel(campaign, campaign.Sector, node.Id);

                nodeInfoLabel.Text = $"{node.Name}\n" +
                                     $"Type: {node.Type}\n" +
                                     $"Faction: {factionName}\n" +
                                     $"Travel cost: {fuelCost} fuel";

                // Update travel button
                if (node.Id == campaign.CurrentNodeId)
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
                    var reason = TravelSystem.GetTravelBlockReason(campaign, campaign.Sector, node.Id);
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
            var targetNode = campaign.Sector.GetNode(campaign.CurrentJob.TargetNodeId);
            currentJobLabel.Text = $"Active Job: {campaign.CurrentJob.Title}\n" +
                                   $"Target: {targetNode?.Name ?? "Unknown"}";
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
            var targetNode = campaign.Sector.GetNode(campaign.CurrentJob.TargetNodeId);
            missionButton.Text = $"Travel to {targetNode?.Name ?? "target"}";
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
        foreach (var kvp in nodeButtons)
        {
            var nodeId = kvp.Key;
            var btn = kvp.Value;
            var panel = btn.GetChild<Panel>(0);
            if (panel == null) continue;

            var style = panel.GetThemeStylebox("panel") as StyleBoxFlat;
            if (style == null) continue;

            var node = campaign.Sector.GetNode(nodeId);
            if (node == null) continue;

            // Reset border
            style.BorderWidthTop = 0;
            style.BorderWidthBottom = 0;
            style.BorderWidthLeft = 0;
            style.BorderWidthRight = 0;

            if (nodeId == campaign.CurrentNodeId)
            {
                style.BgColor = CurrentNodeColor;
                style.BorderWidthTop = 3;
                style.BorderWidthBottom = 3;
                style.BorderWidthLeft = 3;
                style.BorderWidthRight = 3;
                style.BorderColor = Colors.White;
            }
            else if (nodeId == selectedNodeId)
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
                style.BgColor = GetNodeColor(node.Type);
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
