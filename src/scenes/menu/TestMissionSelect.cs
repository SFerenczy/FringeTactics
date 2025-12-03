using Godot;

namespace FringeTactics;

/// <summary>
/// Test mission selection screen.
/// </summary>
public partial class TestMissionSelect : Control
{
    private VBoxContainer buttonContainer;
    
    public override void _Ready()
    {
        CreateUI();
    }
    
    private void CreateUI()
    {
        // Main container with margin
        var margin = new MarginContainer();
        margin.SetAnchorsPreset(LayoutPreset.FullRect);
        margin.AddThemeConstantOverride("margin_left", 40);
        margin.AddThemeConstantOverride("margin_right", 40);
        margin.AddThemeConstantOverride("margin_top", 40);
        margin.AddThemeConstantOverride("margin_bottom", 40);
        AddChild(margin);
        
        var mainVbox = new VBoxContainer();
        margin.AddChild(mainVbox);
        
        // Title
        var titleLabel = new Label();
        titleLabel.Text = "Test Missions";
        titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
        titleLabel.AddThemeFontSizeOverride("font_size", 36);
        mainVbox.AddChild(titleLabel);
        
        // Subtitle
        var subtitleLabel = new Label();
        subtitleLabel.Text = "Select a milestone test mission";
        subtitleLabel.HorizontalAlignment = HorizontalAlignment.Center;
        subtitleLabel.AddThemeFontSizeOverride("font_size", 16);
        subtitleLabel.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.7f));
        mainVbox.AddChild(subtitleLabel);
        
        // Spacer
        var spacer = new Control();
        spacer.CustomMinimumSize = new Vector2(0, 20);
        mainVbox.AddChild(spacer);
        
        // Scroll container for mission list
        var scroll = new ScrollContainer();
        scroll.SizeFlagsVertical = SizeFlags.ExpandFill;
        scroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
        mainVbox.AddChild(scroll);
        
        // Grid container for mission buttons (2 columns)
        var grid = new GridContainer();
        grid.Columns = 2;
        grid.AddThemeConstantOverride("h_separation", 20);
        grid.AddThemeConstantOverride("v_separation", 10);
        grid.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        scroll.AddChild(grid);
        
        // Add mission buttons
        AddMissionButton(grid, "Sandbox", "General test mission with enemies", OnSandboxPressed);
        AddMissionButton(grid, "M0 - Movement", "Single unit, no enemies\nBasic movement testing", OnM0Pressed);
        AddMissionButton(grid, "M1 - Selection", "6 units, no enemies\nMulti-unit selection & groups", OnM1Pressed);
        AddMissionButton(grid, "M2 - Visibility", "Fog of war & LOS testing\nMultiple rooms", OnM2Pressed);
        AddMissionButton(grid, "M3 - Combat", "Basic combat mechanics\nHit chance, ammo, auto-defend", OnM3Pressed);
        AddMissionButton(grid, "M4 - Cover", "Directional cover testing\nWall-based cover", OnM4Pressed);
        AddMissionButton(grid, "M4.1 - Cover Heights", "Low/Half/High cover\nCover height mechanics", OnM4_1Pressed);
        AddMissionButton(grid, "M5 - Interactables", "Doors, terminals, hazards\nChanneled hacking", OnM5Pressed);
        
        // Bottom spacer
        var bottomSpacer = new Control();
        bottomSpacer.CustomMinimumSize = new Vector2(0, 20);
        mainVbox.AddChild(bottomSpacer);
        
        // Back button
        var backButton = new Button();
        backButton.Text = "← Back to Main Menu";
        backButton.CustomMinimumSize = new Vector2(200, 40);
        backButton.Pressed += OnBackPressed;
        mainVbox.AddChild(backButton);
    }
    
    private void AddMissionButton(GridContainer grid, string title, string description, System.Action onPressed)
    {
        var panel = new PanelContainer();
        panel.CustomMinimumSize = new Vector2(280, 80);
        panel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        grid.AddChild(panel);
        
        var button = new Button();
        button.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        button.SizeFlagsVertical = SizeFlags.ExpandFill;
        button.Pressed += onPressed;
        panel.AddChild(button);
        
        var vbox = new VBoxContainer();
        vbox.SetAnchorsPreset(LayoutPreset.FullRect);
        vbox.AddThemeConstantOverride("separation", 2);
        button.AddChild(vbox);
        
        // Center vertically
        var topSpacer = new Control();
        topSpacer.SizeFlagsVertical = SizeFlags.ExpandFill;
        vbox.AddChild(topSpacer);
        
        var titleLbl = new Label();
        titleLbl.Text = title;
        titleLbl.HorizontalAlignment = HorizontalAlignment.Center;
        titleLbl.AddThemeFontSizeOverride("font_size", 18);
        vbox.AddChild(titleLbl);
        
        var descLbl = new Label();
        descLbl.Text = description;
        descLbl.HorizontalAlignment = HorizontalAlignment.Center;
        descLbl.AddThemeFontSizeOverride("font_size", 12);
        descLbl.AddThemeColorOverride("font_color", new Color(0.8f, 0.8f, 0.8f));
        vbox.AddChild(descLbl);
        
        var bottomSpacer = new Control();
        bottomSpacer.SizeFlagsVertical = SizeFlags.ExpandFill;
        vbox.AddChild(bottomSpacer);
    }
    
    private void OnSandboxPressed()
    {
        GD.Print("[TestMissionSelect] Starting sandbox mission...");
        GameState.Instance.StartSandboxMission();
    }
    
    private void OnM0Pressed()
    {
        GD.Print("[TestMissionSelect] Starting M0 test mission...");
        GameState.Instance.StartM0TestMission();
    }
    
    private void OnM1Pressed()
    {
        GD.Print("[TestMissionSelect] Starting M1 test mission...");
        GameState.Instance.StartM1TestMission();
    }
    
    private void OnM2Pressed()
    {
        GD.Print("[TestMissionSelect] Starting M2 test mission...");
        GameState.Instance.StartM2TestMission();
    }
    
    private void OnM3Pressed()
    {
        GD.Print("[TestMissionSelect] Starting M3 test mission...");
        GameState.Instance.StartM3TestMission();
    }
    
    private void OnM4Pressed()
    {
        GD.Print("[TestMissionSelect] Starting M4 test mission...");
        GameState.Instance.StartM4TestMission();
    }
    
    private void OnM4_1Pressed()
    {
        GD.Print("[TestMissionSelect] Starting M4.1 test mission...");
        GameState.Instance.StartM4_1TestMission();
    }
    
    private void OnM5Pressed()
    {
        GD.Print("[TestMissionSelect] Starting M5 test mission...");
        GameState.Instance.StartM5TestMission();
    }
    
    private void OnBackPressed()
    {
        GetTree().ChangeSceneToFile("res://src/scenes/menu/MainMenu.tscn");
    }
}
