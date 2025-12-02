using Godot;

namespace FringeTactics;

/// <summary>
/// Campaign over screen - shows final stats and options to restart or return to menu.
/// </summary>
public partial class CampaignOverScreen : Control
{
    private Label titleLabel;
    private Label statsLabel;
    private Button newCampaignButton;
    private Button mainMenuButton;

    // Store stats from the ended campaign
    private int missionsCompleted;
    private int totalMoneyEarned;
    private int crewDeaths;

    public override void _Ready()
    {
        // Capture stats from campaign before it's cleared
        var campaign = GameState.Instance?.Campaign;
        if (campaign != null)
        {
            missionsCompleted = campaign.MissionsCompleted;
            totalMoneyEarned = campaign.TotalMoneyEarned;
            crewDeaths = campaign.TotalCrewDeaths;
        }

        CreateUI();
    }

    private void CreateUI()
    {
        // Dark overlay background
        var bg = new ColorRect();
        bg.Color = new Color(0.05f, 0.02f, 0.02f, 1.0f);
        bg.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(bg);

        // Center container
        var vbox = new VBoxContainer();
        vbox.SetAnchorsPreset(LayoutPreset.Center);
        vbox.GrowHorizontal = GrowDirection.Both;
        vbox.GrowVertical = GrowDirection.Both;
        AddChild(vbox);

        // Title
        titleLabel = new Label();
        titleLabel.Text = "CAMPAIGN OVER";
        titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
        titleLabel.AddThemeFontSizeOverride("font_size", 48);
        titleLabel.AddThemeColorOverride("font_color", Colors.Red);
        vbox.AddChild(titleLabel);

        // Subtitle
        var subtitleLabel = new Label();
        subtitleLabel.Text = "All crew members have been lost.";
        subtitleLabel.HorizontalAlignment = HorizontalAlignment.Center;
        subtitleLabel.AddThemeFontSizeOverride("font_size", 18);
        subtitleLabel.AddThemeColorOverride("font_color", Colors.Gray);
        vbox.AddChild(subtitleLabel);

        AddSpacer(vbox, 40);

        // Stats panel
        var statsPanel = new PanelContainer();
        statsPanel.CustomMinimumSize = new Vector2(300, 0);
        var panelStyle = new StyleBoxFlat();
        panelStyle.BgColor = new Color(0.1f, 0.1f, 0.12f, 0.9f);
        panelStyle.BorderWidthTop = 2;
        panelStyle.BorderWidthBottom = 2;
        panelStyle.BorderWidthLeft = 2;
        panelStyle.BorderWidthRight = 2;
        panelStyle.BorderColor = new Color(0.3f, 0.3f, 0.35f);
        panelStyle.CornerRadiusTopLeft = 8;
        panelStyle.CornerRadiusTopRight = 8;
        panelStyle.CornerRadiusBottomLeft = 8;
        panelStyle.CornerRadiusBottomRight = 8;
        panelStyle.ContentMarginTop = 20;
        panelStyle.ContentMarginBottom = 20;
        panelStyle.ContentMarginLeft = 20;
        panelStyle.ContentMarginRight = 20;
        statsPanel.AddThemeStyleboxOverride("panel", panelStyle);
        vbox.AddChild(statsPanel);

        var statsVbox = new VBoxContainer();
        statsPanel.AddChild(statsVbox);

        // Stats header
        var statsHeader = new Label();
        statsHeader.Text = "FINAL STATISTICS";
        statsHeader.HorizontalAlignment = HorizontalAlignment.Center;
        statsHeader.AddThemeFontSizeOverride("font_size", 20);
        statsHeader.AddThemeColorOverride("font_color", Colors.Cyan);
        statsVbox.AddChild(statsHeader);

        AddSpacer(statsVbox, 15);

        // Stats content
        statsLabel = new Label();
        statsLabel.Text = $"Missions Completed: {missionsCompleted}\n" +
                          $"Total Money Earned: ${totalMoneyEarned}\n" +
                          $"Crew Deaths: {crewDeaths}";
        statsLabel.HorizontalAlignment = HorizontalAlignment.Center;
        statsLabel.AddThemeFontSizeOverride("font_size", 16);
        statsVbox.AddChild(statsLabel);

        AddSpacer(vbox, 40);

        // Buttons container
        var buttonContainer = new HBoxContainer();
        buttonContainer.Alignment = BoxContainer.AlignmentMode.Center;
        vbox.AddChild(buttonContainer);

        // New Campaign button
        newCampaignButton = new Button();
        newCampaignButton.Text = "Start New Campaign";
        newCampaignButton.CustomMinimumSize = new Vector2(200, 50);
        newCampaignButton.Pressed += OnNewCampaignPressed;
        buttonContainer.AddChild(newCampaignButton);

        // Spacer between buttons
        var btnSpacer = new Control();
        btnSpacer.CustomMinimumSize = new Vector2(20, 0);
        buttonContainer.AddChild(btnSpacer);

        // Main Menu button
        mainMenuButton = new Button();
        mainMenuButton.Text = "Main Menu";
        mainMenuButton.CustomMinimumSize = new Vector2(200, 50);
        mainMenuButton.Pressed += OnMainMenuPressed;
        buttonContainer.AddChild(mainMenuButton);
    }

    private void AddSpacer(VBoxContainer parent, int height)
    {
        var spacer = new Control();
        spacer.CustomMinimumSize = new Vector2(0, height);
        parent.AddChild(spacer);
    }

    private void OnNewCampaignPressed()
    {
        GD.Print("[CampaignOverScreen] Starting new campaign...");
        GameState.Instance.StartNewCampaign();
    }

    private void OnMainMenuPressed()
    {
        GD.Print("[CampaignOverScreen] Returning to main menu...");
        GameState.Instance.GoToMainMenu();
    }
}
