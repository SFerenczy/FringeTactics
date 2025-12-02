using Godot;

namespace FringeTactics;

public partial class MainMenu : Control
{
    private Button startCampaignButton;
    private Button startTestMissionButton;
    private Button startM0TestButton;
    private Button quitButton;
    private Label titleLabel;

    public override void _Ready()
    {
        CreateUI();
    }

    private void CreateUI()
    {
        // Center container
        var vbox = new VBoxContainer();
        vbox.SetAnchorsPreset(Control.LayoutPreset.Center);
        vbox.GrowHorizontal = GrowDirection.Both;
        vbox.GrowVertical = GrowDirection.Both;
        AddChild(vbox);

        // Title
        titleLabel = new Label();
        titleLabel.Text = "FRINGE TACTICS";
        titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
        titleLabel.AddThemeFontSizeOverride("font_size", 48);
        vbox.AddChild(titleLabel);

        // Spacer
        var spacer = new Control();
        spacer.CustomMinimumSize = new Vector2(0, 40);
        vbox.AddChild(spacer);

        // Start Campaign button
        startCampaignButton = new Button();
        startCampaignButton.Text = "Start New Campaign";
        startCampaignButton.CustomMinimumSize = new Vector2(250, 50);
        startCampaignButton.Pressed += OnStartCampaignPressed;
        vbox.AddChild(startCampaignButton);

        // Spacer
        var spacer2 = new Control();
        spacer2.CustomMinimumSize = new Vector2(0, 10);
        vbox.AddChild(spacer2);

        // Start Test Mission button (no campaign, just combat sandbox)
        startTestMissionButton = new Button();
        startTestMissionButton.Text = "Start Test Mission (Sandbox)";
        startTestMissionButton.CustomMinimumSize = new Vector2(250, 50);
        startTestMissionButton.Pressed += OnStartTestMissionPressed;
        vbox.AddChild(startTestMissionButton);

        // Spacer
        var spacer3 = new Control();
        spacer3.CustomMinimumSize = new Vector2(0, 10);
        vbox.AddChild(spacer3);
        
        // M0 Test Mission button (single unit, no enemies)
        startM0TestButton = new Button();
        startM0TestButton.Text = "M0 Test (Single Unit)";
        startM0TestButton.CustomMinimumSize = new Vector2(250, 50);
        startM0TestButton.Pressed += OnStartM0TestPressed;
        vbox.AddChild(startM0TestButton);

        // Spacer
        var spacer4 = new Control();
        spacer4.CustomMinimumSize = new Vector2(0, 10);
        vbox.AddChild(spacer4);

        // Quit button
        quitButton = new Button();
        quitButton.Text = "Quit";
        quitButton.CustomMinimumSize = new Vector2(250, 50);
        quitButton.Pressed += OnQuitPressed;
        vbox.AddChild(quitButton);
    }

    private void OnStartCampaignPressed()
    {
        GD.Print("[MainMenu] Starting new campaign...");
        GameState.Instance.StartNewCampaign();
    }

    private void OnStartTestMissionPressed()
    {
        GD.Print("[MainMenu] Starting test mission (sandbox mode)...");
        GameState.Instance.StartSandboxMission();
    }
    
    private void OnStartM0TestPressed()
    {
        GD.Print("[MainMenu] Starting M0 test mission (single unit, no enemies)...");
        GameState.Instance.StartM0TestMission();
    }

    private void OnQuitPressed()
    {
        GetTree().Quit();
    }
}
