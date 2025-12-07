using Godot;
using System.Collections.Generic;
using System.Linq;

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

    // Detail panel references
    private PanelContainer crewDetailPanel;
    private Label detailNameLabel;
    private Label detailRoleLabel;
    private Label detailLevelLabel;
    private GridContainer statsContainer;
    private VBoxContainer traitsContainer;
    private VBoxContainer injuriesContainer;
    private Button closeDetailButton;

    // State
    private int? selectedCrewId;

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

        // Detail panel
        crewDetailPanel = GetNode<PanelContainer>("%CrewDetailPanel");
        detailNameLabel = GetNode<Label>("%DetailNameLabel");
        detailRoleLabel = GetNode<Label>("%DetailRoleLabel");
        detailLevelLabel = GetNode<Label>("%DetailLevelLabel");
        statsContainer = GetNode<GridContainer>("%StatsContainer");
        traitsContainer = GetNode<VBoxContainer>("%TraitsContainer");
        injuriesContainer = GetNode<VBoxContainer>("%InjuriesContainer");
        closeDetailButton = GetNode<Button>("%CloseDetailButton");
    }

    private void ConnectSignals()
    {
        backToSectorButton.Pressed += OnBackToSectorPressed;
        abandonButton.Pressed += OnMainMenuPressed;
        closeDetailButton.Pressed += OnCloseDetailPressed;
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

        UpdateResourceDisplay();

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
        ClearChildren(crewContainer);

        foreach (var crew in campaign.Crew)
        {
            var crewRow = CreateCrewListEntry(crew, campaign);
            crewContainer.AddChild(crewRow);
        }
    }

    private HBoxContainer CreateCrewListEntry(CrewMember crew, CampaignState campaign)
    {
        var crewRow = new HBoxContainer();
        crewRow.AddThemeConstantOverride("separation", 10);

        // Status indicator
        var statusColor = crew.IsDead ? Colors.Red :
                          crew.Injuries.Count > 0 ? Colors.Orange :
                          Colors.Green;

        var statusDot = new ColorRect();
        statusDot.CustomMinimumSize = new Vector2(12, 12);
        statusDot.Color = statusColor;
        crewRow.AddChild(statusDot);

        // Clickable crew button
        var crewButton = new Button();
        crewButton.Flat = true;
        crewButton.SizeFlagsHorizontal = SizeFlags.ExpandFill;

        // Show name, role, and key stats (Aim, Grit, Tech)
        var aimVal = crew.GetEffectiveStat(CrewStatType.Aim);
        var gritVal = crew.GetEffectiveStat(CrewStatType.Grit);
        var techVal = crew.GetEffectiveStat(CrewStatType.Tech);
        crewButton.Text = $"{crew.Name} - {crew.Role} | Aim:{aimVal} Grit:{gritVal} Tech:{techVal}";

        if (crew.IsDead)
        {
            crewButton.AddThemeColorOverride("font_color", Colors.Gray);
            crewButton.Disabled = true;
        }

        var crewId = crew.Id;
        crewButton.Pressed += () => SelectCrew(crewId);
        crewRow.AddChild(crewButton);

        // Heal button if injured and have meds
        if (!crew.IsDead && crew.Injuries.Count > 0 && campaign.Meds > 0)
        {
            var healBtn = new Button();
            healBtn.Text = "Heal";
            healBtn.CustomMinimumSize = new Vector2(60, 30);
            healBtn.Pressed += () => OnHealPressed(crewId);
            crewRow.AddChild(healBtn);
        }

        return crewRow;
    }

    private void SelectCrew(int crewId)
    {
        selectedCrewId = crewId;
        UpdateDetailPanel();
    }

    private CrewMember GetSelectedCrew()
    {
        if (selectedCrewId == null) return null;
        return GameState.Instance?.Campaign?.Crew.Find(c => c.Id == selectedCrewId.Value);
    }

    private void UpdateDetailPanel()
    {
        var crew = GetSelectedCrew();
        if (crew == null)
        {
            selectedCrewId = null;
            crewDetailPanel.Visible = false;
            return;
        }

        crewDetailPanel.Visible = true;

        // Header
        detailNameLabel.Text = crew.Name;
        detailRoleLabel.Text = crew.Role.ToString();

        // Level/XP
        var xpPerLevel = CampaignConfig.Instance.Crew.XpPerLevel;
        detailLevelLabel.Text = $"Level {crew.Level} ({crew.Xp}/{xpPerLevel} XP)";

        // Stats
        UpdateStatsDisplay(crew);

        // Traits
        UpdateTraitsDisplay(crew);

        // Injuries
        UpdateInjuriesDisplay(crew);
    }

    private void UpdateStatsDisplay(CrewMember crew)
    {
        ClearChildren(statsContainer);

        var stats = new[] {
            ("Grit", CrewStatType.Grit),
            ("Reflexes", CrewStatType.Reflexes),
            ("Aim", CrewStatType.Aim),
            ("Tech", CrewStatType.Tech),
            ("Savvy", CrewStatType.Savvy),
            ("Resolve", CrewStatType.Resolve)
        };

        foreach (var (name, statType) in stats)
        {
            var baseStat = crew.GetBaseStat(statType);
            var effectiveStat = crew.GetEffectiveStat(statType);
            var modifier = effectiveStat - baseStat;

            var nameLabel = new Label();
            nameLabel.Text = name;
            nameLabel.CustomMinimumSize = new Vector2(80, 0);
            statsContainer.AddChild(nameLabel);

            var valueLabel = new Label();
            if (modifier != 0)
            {
                var modSign = modifier > 0 ? "+" : "";
                valueLabel.Text = $"{effectiveStat} ({baseStat}{modSign}{modifier})";
                valueLabel.AddThemeColorOverride("font_color", modifier > 0 ? Colors.Green : Colors.Red);
            }
            else
            {
                valueLabel.Text = effectiveStat.ToString();
            }
            statsContainer.AddChild(valueLabel);
        }
    }

    private void UpdateTraitsDisplay(CrewMember crew)
    {
        ClearChildren(traitsContainer);

        var traits = crew.GetTraits().ToList();
        if (traits.Count == 0)
        {
            var noTraitsLabel = new Label();
            noTraitsLabel.Text = "No traits";
            noTraitsLabel.AddThemeColorOverride("font_color", Colors.Gray);
            traitsContainer.AddChild(noTraitsLabel);
            return;
        }

        foreach (var trait in traits)
        {
            var traitLabel = new Label();
            traitLabel.Text = trait.Name;

            // Color-code by category
            var color = trait.Category switch
            {
                TraitCategory.Injury => Colors.Red,
                TraitCategory.Personality => Colors.Cyan,
                TraitCategory.Background => Colors.Yellow,
                TraitCategory.Acquired => Colors.Green,
                _ => Colors.White
            };
            traitLabel.AddThemeColorOverride("font_color", color);
            traitLabel.TooltipText = trait.Description;
            traitsContainer.AddChild(traitLabel);
        }
    }

    private void UpdateInjuriesDisplay(CrewMember crew)
    {
        ClearChildren(injuriesContainer);

        if (crew.Injuries.Count == 0)
        {
            var noInjuriesLabel = new Label();
            noInjuriesLabel.Text = "No injuries";
            noInjuriesLabel.AddThemeColorOverride("font_color", Colors.Gray);
            injuriesContainer.AddChild(noInjuriesLabel);
            return;
        }

        foreach (var injury in crew.Injuries)
        {
            var injuryLabel = new Label();
            injuryLabel.Text = $"• {FormatInjuryName(injury)}";
            injuryLabel.AddThemeColorOverride("font_color", Colors.Orange);
            injuriesContainer.AddChild(injuryLabel);
        }
    }

    private static string FormatInjuryName(string injury)
    {
        return injury switch
        {
            InjuryTypes.Wounded => "Wounded (reduced combat effectiveness)",
            InjuryTypes.Critical => "Critical (cannot deploy)",
            InjuryTypes.Concussed => "Concussed (reduced accuracy)",
            InjuryTypes.Bleeding => "Bleeding (needs immediate care)",
            _ => injury
        };
    }

    private static void ClearChildren(Node container)
    {
        foreach (var child in container.GetChildren())
        {
            child.QueueFree();
        }
    }

    private void OnCloseDetailPressed()
    {
        selectedCrewId = null;
        crewDetailPanel.Visible = false;
    }

    private void OnHealPressed(int crewId)
    {
        var campaign = GameState.Instance?.Campaign;
        if (campaign != null && campaign.HealCrewMember(crewId))
        {
            UpdateDisplay();
            if (selectedCrewId == crewId)
            {
                UpdateDetailPanel();
            }
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
