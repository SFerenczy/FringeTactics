using Godot;

namespace FringeTactics;

public partial class MainMenu : Control
{
    public override void _Ready()
    {
        CreateUI();
    }

    private void CreateUI()
    {
        // Center container
        var vbox = new VBoxContainer();
        vbox.SetAnchorsPreset(LayoutPreset.Center);
        vbox.GrowHorizontal = GrowDirection.Both;
        vbox.GrowVertical = GrowDirection.Both;
        AddChild(vbox);

        // Title
        var titleLabel = new Label();
        titleLabel.Text = "FRINGE TACTICS";
        titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
        titleLabel.AddThemeFontSizeOverride("font_size", 48);
        vbox.AddChild(titleLabel);

        // Spacer
        AddSpacer(vbox, 60);

        // Start Campaign button
        AddButton(vbox, "Start New Campaign", OnStartCampaignPressed);
        AddSpacer(vbox, 15);

        // Test Missions button
        AddButton(vbox, "Test Missions", OnTestMissionsPressed);
        AddSpacer(vbox, 30);

        // Quit button
        AddButton(vbox, "Quit", OnQuitPressed);
    }
    
    private void AddButton(VBoxContainer container, string text, System.Action onPressed)
    {
        var button = new Button();
        button.Text = text;
        button.CustomMinimumSize = new Vector2(280, 50);
        button.Pressed += onPressed;
        container.AddChild(button);
    }
    
    private void AddSpacer(VBoxContainer container, float height)
    {
        var spacer = new Control();
        spacer.CustomMinimumSize = new Vector2(0, height);
        container.AddChild(spacer);
    }

    private void OnStartCampaignPressed()
    {
        GD.Print("[MainMenu] Starting new campaign...");
        GameState.Instance.StartNewCampaign();
    }

    private void OnTestMissionsPressed()
    {
        GD.Print("[MainMenu] Opening test mission selection...");
        GetTree().ChangeSceneToFile("res://src/scenes/menu/TestMissionSelect.tscn");
    }

    private void OnQuitPressed()
    {
        GetTree().Quit();
    }
}
