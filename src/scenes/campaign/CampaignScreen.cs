using Godot;
using System.Collections.Generic;

namespace FringeTactics;

public partial class CampaignScreen : Control
{
    private Label titleLabel;
    private Label resourcesLabel;
    private VBoxContainer crewContainer;
    private Label statsLabel;
    private Label missionCostLabel;
    private Button startMissionButton;
    private Button mainMenuButton;

    private List<Button> healButtons = new();

    public override void _Ready()
    {
        CreateUI();
        UpdateDisplay();
    }

    public override void _Process(double delta)
    {
        // Refresh resource display for devtools
        UpdateResourceDisplay();
    }

    private void UpdateResourceDisplay()
    {
        var campaign = GameState.Instance?.Campaign;
        if (campaign == null) return;

        resourcesLabel.Text = $"Money: ${campaign.Money}\n" +
                              $"Fuel: {campaign.Fuel}\n" +
                              $"Ammo: {campaign.Ammo}\n" +
                              $"Parts: {campaign.Parts}\n" +
                              $"Meds: {campaign.Meds}";
    }

    private void CreateUI()
    {
        // Main horizontal layout
        var hbox = new HBoxContainer();
        hbox.SetAnchorsPreset(LayoutPreset.FullRect);
        hbox.AddThemeConstantOverride("separation", 40);
        AddChild(hbox);

        // Left panel - Resources & Actions
        var leftPanel = new VBoxContainer();
        leftPanel.CustomMinimumSize = new Vector2(300, 0);
        hbox.AddChild(leftPanel);

        // Title
        titleLabel = new Label();
        titleLabel.Text = "SHIP HQ";
        titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
        titleLabel.AddThemeFontSizeOverride("font_size", 32);
        leftPanel.AddChild(titleLabel);

        AddSpacer(leftPanel, 20);

        // Resources section
        var resourcesTitle = new Label();
        resourcesTitle.Text = "RESOURCES";
        resourcesTitle.AddThemeFontSizeOverride("font_size", 18);
        resourcesTitle.AddThemeColorOverride("font_color", Colors.Yellow);
        leftPanel.AddChild(resourcesTitle);

        resourcesLabel = new Label();
        resourcesLabel.AddThemeFontSizeOverride("font_size", 14);
        leftPanel.AddChild(resourcesLabel);

        AddSpacer(leftPanel, 20);

        // Mission cost info
        missionCostLabel = new Label();
        missionCostLabel.AddThemeFontSizeOverride("font_size", 12);
        missionCostLabel.AddThemeColorOverride("font_color", Colors.Gray);
        leftPanel.AddChild(missionCostLabel);

        AddSpacer(leftPanel, 10);

        // Stats
        statsLabel = new Label();
        statsLabel.AddThemeFontSizeOverride("font_size", 12);
        statsLabel.AddThemeColorOverride("font_color", Colors.Gray);
        leftPanel.AddChild(statsLabel);

        AddSpacer(leftPanel, 30);

        // Start Mission button (deprecated - use sector view instead)
        startMissionButton = new Button();
        startMissionButton.Text = "Start Mission";
        startMissionButton.CustomMinimumSize = new Vector2(200, 50);
        startMissionButton.Pressed += OnStartMissionPressed;
        startMissionButton.Visible = false; // Hidden - use sector view for missions
        leftPanel.AddChild(startMissionButton);

        AddSpacer(leftPanel, 10);

        // Back to Sector button
        var backButton = new Button();
        backButton.Text = "Back to Sector";
        backButton.CustomMinimumSize = new Vector2(200, 50);
        backButton.Pressed += OnBackToSectorPressed;
        leftPanel.AddChild(backButton);

        AddSpacer(leftPanel, 10);

        // Main Menu button
        mainMenuButton = new Button();
        mainMenuButton.Text = "Abandon Campaign";
        mainMenuButton.CustomMinimumSize = new Vector2(200, 40);
        mainMenuButton.Pressed += OnMainMenuPressed;
        leftPanel.AddChild(mainMenuButton);

        // Right panel - Crew roster
        var rightPanel = new VBoxContainer();
        rightPanel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        hbox.AddChild(rightPanel);

        var crewTitle = new Label();
        crewTitle.Text = "CREW ROSTER";
        crewTitle.AddThemeFontSizeOverride("font_size", 24);
        crewTitle.AddThemeColorOverride("font_color", Colors.Cyan);
        rightPanel.AddChild(crewTitle);

        AddSpacer(rightPanel, 10);

        // Scrollable crew list
        var scroll = new ScrollContainer();
        scroll.SizeFlagsVertical = SizeFlags.ExpandFill;
        rightPanel.AddChild(scroll);

        crewContainer = new VBoxContainer();
        crewContainer.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        scroll.AddChild(crewContainer);
    }

    private void AddSpacer(Control parent, int height)
    {
        var spacer = new Control();
        spacer.CustomMinimumSize = new Vector2(0, height);
        if (parent is VBoxContainer vbox)
            vbox.AddChild(spacer);
    }

    private void UpdateDisplay()
    {
        var campaign = GameState.Instance?.Campaign;
        if (campaign == null)
        {
            resourcesLabel.Text = "No active campaign!";
            startMissionButton.Disabled = true;
            return;
        }

        // Resources
        resourcesLabel.Text = $"Money: ${campaign.Money}\n" +
                              $"Fuel: {campaign.Fuel}\n" +
                              $"Ammo: {campaign.Ammo}\n" +
                              $"Parts: {campaign.Parts}\n" +
                              $"Meds: {campaign.Meds}";

        // Mission cost
        missionCostLabel.Text = $"Mission cost: {CampaignState.MISSION_AMMO_COST} ammo, {CampaignState.MISSION_FUEL_COST} fuel";

        // Stats
        statsLabel.Text = $"Missions: {campaign.MissionsCompleted} won, {campaign.MissionsFailed} lost";

        // Update crew roster
        UpdateCrewRoster(campaign);

        // Update mission button
        if (!campaign.CanStartMission())
        {
            startMissionButton.Disabled = true;
            startMissionButton.Text = campaign.GetMissionBlockReason();
        }
        else
        {
            startMissionButton.Disabled = false;
            startMissionButton.Text = "Start Mission";
        }
    }

    private void UpdateCrewRoster(CampaignState campaign)
    {
        // Clear existing
        foreach (var child in crewContainer.GetChildren())
        {
            child.QueueFree();
        }
        healButtons.Clear();

        foreach (var crew in campaign.Crew)
        {
            var crewRow = new HBoxContainer();
            crewRow.AddThemeConstantOverride("separation", 10);
            crewContainer.AddChild(crewRow);

            // Status indicator
            var statusColor = crew.IsDead ? Colors.Red :
                              crew.Injuries.Count > 0 ? Colors.Orange :
                              Colors.Green;

            var statusDot = new ColorRect();
            statusDot.CustomMinimumSize = new Vector2(12, 12);
            statusDot.Color = statusColor;
            crewRow.AddChild(statusDot);

            // Name and info
            var infoLabel = new Label();
            var roleText = crew.Role.ToString();
            var levelText = $"Lv.{crew.Level}";
            var xpText = $"({crew.Xp}/{CrewMember.XP_PER_LEVEL} XP)";
            var statusText = crew.GetStatusText();

            infoLabel.Text = $"{crew.Name} - {roleText} {levelText} {xpText} [{statusText}]";
            infoLabel.AddThemeFontSizeOverride("font_size", 14);

            if (crew.IsDead)
            {
                infoLabel.AddThemeColorOverride("font_color", Colors.Gray);
            }

            crewRow.AddChild(infoLabel);

            // Heal button if injured and have meds
            if (!crew.IsDead && crew.Injuries.Count > 0 && campaign.Meds > 0)
            {
                var healBtn = new Button();
                healBtn.Text = "Heal (1 Med)";
                healBtn.CustomMinimumSize = new Vector2(100, 30);
                var crewId = crew.Id; // Capture for lambda
                healBtn.Pressed += () => OnHealPressed(crewId);
                crewRow.AddChild(healBtn);
                healButtons.Add(healBtn);
            }
        }
    }

    private void OnHealPressed(int crewId)
    {
        var campaign = GameState.Instance?.Campaign;
        if (campaign != null && campaign.HealCrewMember(crewId))
        {
            UpdateDisplay();
        }
    }

    private void OnStartMissionPressed()
    {
        GD.Print("[CampaignScreen] Starting mission...");
        GameState.Instance.StartMission();
    }

    private void OnBackToSectorPressed()
    {
        GD.Print("[CampaignScreen] Returning to sector view...");
        GameState.Instance.GoToSectorView();
    }

    private void OnMainMenuPressed()
    {
        GD.Print("[CampaignScreen] Returning to main menu...");
        GameState.Instance.GoToMainMenu();
    }
}
