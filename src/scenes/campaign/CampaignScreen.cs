using Godot;
using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// Campaign HQ screen showing resources, crew roster, and navigation.
/// UI layout is defined in CampaignScreen.tscn.
/// </summary>
public partial class CampaignScreen : Control
{
    // Scene node references (using %UniqueNames)
    private Label resourcesLabel;
    private Label statsLabel;
    private Label missionCostLabel;
    private VBoxContainer crewContainer;
    private Button backToSectorButton;
    private Button abandonButton;

    private List<Button> healButtons = new();

    public override void _Ready()
    {
        GetNodeReferences();
        ConnectSignals();
        UpdateDisplay();
    }

    private void GetNodeReferences()
    {
        resourcesLabel = GetNode<Label>("%ResourcesLabel");
        statsLabel = GetNode<Label>("%StatsLabel");
        missionCostLabel = GetNode<Label>("%MissionCostLabel");
        crewContainer = GetNode<VBoxContainer>("%CrewContainer");
        backToSectorButton = GetNode<Button>("%BackToSectorButton");
        abandonButton = GetNode<Button>("%AbandonButton");
    }

    private void ConnectSignals()
    {
        backToSectorButton.Pressed += OnBackToSectorPressed;
        abandonButton.Pressed += OnMainMenuPressed;
    }

    public override void _Process(double delta)
    {
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

    private void UpdateDisplay()
    {
        var campaign = GameState.Instance?.Campaign;
        if (campaign == null)
        {
            resourcesLabel.Text = "No active campaign!";
            return;
        }

        // Resources
        resourcesLabel.Text = $"Money: ${campaign.Money}\n" +
                              $"Fuel: {campaign.Fuel}\n" +
                              $"Ammo: {campaign.Ammo}\n" +
                              $"Parts: {campaign.Parts}\n" +
                              $"Meds: {campaign.Meds}";

        // Mission cost
        var missionConfig = CampaignConfig.Instance.Mission;
        missionCostLabel.Text = $"Mission cost: {missionConfig.FuelCost} fuel";

        // Stats
        statsLabel.Text = $"Missions: {campaign.MissionsCompleted} won, {campaign.MissionsFailed} lost";

        // Update crew roster
        UpdateCrewRoster(campaign);
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
            var xpText = $"({crew.Xp}/{CampaignConfig.Instance.Crew.XpPerLevel} XP)";
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
