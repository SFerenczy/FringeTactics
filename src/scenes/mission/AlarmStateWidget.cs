using Godot;

namespace FringeTactics;

/// <summary>
/// UI widget showing current alarm state.
/// </summary>
public partial class AlarmStateWidget : Control
{
    private Label stateLabel;
    private ColorRect background;
    
    public override void _Ready()
    {
        CreateUI();
    }
    
    private void CreateUI()
    {
        background = new ColorRect();
        background.Size = new Vector2(100, 24);
        background.Color = new Color(0.0f, 0.2f, 0.0f, 0.8f);
        AddChild(background);
        
        stateLabel = new Label();
        stateLabel.Position = new Vector2(8, 2);
        stateLabel.AddThemeFontSizeOverride("font_size", 14);
        AddChild(stateLabel);
        
        UpdateDisplay(AlarmState.Quiet);
    }
    
    public void UpdateDisplay(AlarmState state)
    {
        if (stateLabel == null || background == null)
        {
            return;
        }
        
        switch (state)
        {
            case AlarmState.Quiet:
                stateLabel.Text = "QUIET";
                stateLabel.AddThemeColorOverride("font_color", CombatColors.AlarmQuietText);
                background.Color = CombatColors.AlarmQuietBackground;
                break;
            case AlarmState.Alerted:
                stateLabel.Text = "ALERTED";
                stateLabel.AddThemeColorOverride("font_color", CombatColors.AlarmAlertedText);
                background.Color = CombatColors.AlarmAlertedBackground;
                break;
        }
    }
}
