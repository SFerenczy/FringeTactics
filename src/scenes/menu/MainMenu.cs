using Godot;

namespace FringeTactics;

public partial class MainMenu : Control
{
    private Button startCampaignButton;
    private Button startTestMissionButton;
    private Button startM0TestButton;
    private Button startM1TestButton;
    private Button startM2TestButton;
    private Button startM3TestButton;
    private Button startM4TestButton;
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

        // M1 Test Mission button (6 units for selection testing)
        startM1TestButton = new Button();
        startM1TestButton.Text = "M1 Test (Multi-Unit Selection)";
        startM1TestButton.CustomMinimumSize = new Vector2(250, 50);
        startM1TestButton.Pressed += OnStartM1TestPressed;
        vbox.AddChild(startM1TestButton);

        // Spacer
        var spacer5 = new Control();
        spacer5.CustomMinimumSize = new Vector2(0, 10);
        vbox.AddChild(spacer5);

        // M2 Test Mission button (visibility and fog of war)
        startM2TestButton = new Button();
        startM2TestButton.Text = "M2 Test (Visibility & Fog)";
        startM2TestButton.CustomMinimumSize = new Vector2(250, 50);
        startM2TestButton.Pressed += OnStartM2TestPressed;
        vbox.AddChild(startM2TestButton);

        // Spacer
        var spacer6 = new Control();
        spacer6.CustomMinimumSize = new Vector2(0, 10);
        vbox.AddChild(spacer6);

        // M3 Test Mission button (basic combat)
        startM3TestButton = new Button();
        startM3TestButton.Text = "M3 Test (Basic Combat)";
        startM3TestButton.CustomMinimumSize = new Vector2(250, 50);
        startM3TestButton.Pressed += OnStartM3TestPressed;
        vbox.AddChild(startM3TestButton);

        // Spacer
        var spacer7 = new Control();
        spacer7.CustomMinimumSize = new Vector2(0, 10);
        vbox.AddChild(spacer7);

        // M4 Test Mission button (cover combat)
        startM4TestButton = new Button();
        startM4TestButton.Text = "M4 Test (Cover Combat)";
        startM4TestButton.CustomMinimumSize = new Vector2(250, 50);
        startM4TestButton.Pressed += OnStartM4TestPressed;
        vbox.AddChild(startM4TestButton);

        // Spacer
        var spacer8 = new Control();
        spacer8.CustomMinimumSize = new Vector2(0, 10);
        vbox.AddChild(spacer8);

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

    private void OnStartM1TestPressed()
    {
        GD.Print("[MainMenu] Starting M1 test mission (6 units for selection testing)...");
        GameState.Instance.StartM1TestMission();
    }

    private void OnStartM2TestPressed()
    {
        GD.Print("[MainMenu] Starting M2 test mission (visibility & fog of war)...");
        GameState.Instance.StartM2TestMission();
    }

    private void OnStartM3TestPressed()
    {
        GD.Print("[MainMenu] Starting M3 test mission (basic combat)...");
        GameState.Instance.StartM3TestMission();
    }

    private void OnStartM4TestPressed()
    {
        GD.Print("[MainMenu] Starting M4 test mission (cover combat)...");
        GameState.Instance.StartM4TestMission();
    }

    private void OnQuitPressed()
    {
        GetTree().Quit();
    }
}
