using Godot;

namespace FringeTactics;

/// <summary>
/// UI widget displaying current mission phase and time in phase.
/// </summary>
public partial class PhaseWidget : Control
{
    private Label phaseLabel;
    private Label timerLabel;
    private ColorRect background;
    
    private PhaseSystem phaseSystem;
    
    public override void _Ready()
    {
        CreateUI();
    }
    
    private void CreateUI()
    {
        CustomMinimumSize = new Vector2(160, 55);
        
        background = new ColorRect();
        background.SetAnchorsPreset(LayoutPreset.FullRect);
        background.Color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
        AddChild(background);
        
        var vbox = new VBoxContainer();
        vbox.SetAnchorsPreset(LayoutPreset.FullRect);
        vbox.AddThemeConstantOverride("separation", 2);
        AddChild(vbox);
        
        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 8);
        margin.AddThemeConstantOverride("margin_right", 8);
        margin.AddThemeConstantOverride("margin_top", 5);
        margin.AddThemeConstantOverride("margin_bottom", 5);
        vbox.AddChild(margin);
        
        var innerVbox = new VBoxContainer();
        innerVbox.AddThemeConstantOverride("separation", 2);
        margin.AddChild(innerVbox);
        
        phaseLabel = new Label();
        phaseLabel.AddThemeFontSizeOverride("font_size", 16);
        phaseLabel.Text = "SETUP";
        innerVbox.AddChild(phaseLabel);
        
        timerLabel = new Label();
        timerLabel.AddThemeFontSizeOverride("font_size", 12);
        timerLabel.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.7f));
        timerLabel.Text = "Time: 0s";
        innerVbox.AddChild(timerLabel);
    }
    
    public void ConnectToPhaseSystem(PhaseSystem system)
    {
        phaseSystem = system;
        phaseSystem.PhaseChanged += OnPhaseChanged;
        UpdateDisplay(phaseSystem.CurrentPhase, phaseSystem.TicksInPhase);
    }
    
    private void OnPhaseChanged(TacticalPhase oldPhase, TacticalPhase newPhase)
    {
        UpdateDisplay(newPhase, 0);
    }
    
    public override void _Process(double delta)
    {
        if (phaseSystem != null)
        {
            UpdateDisplay(phaseSystem.CurrentPhase, phaseSystem.TicksInPhase);
        }
    }
    
    public void UpdateDisplay(TacticalPhase phase, int ticksInPhase)
    {
        var phaseName = phase switch
        {
            TacticalPhase.Setup => "DEPLOYMENT",
            TacticalPhase.Negotiation => "NEGOTIATION",
            TacticalPhase.Contact => "CONTACT",
            TacticalPhase.Pressure => "PRESSURE",
            TacticalPhase.Resolution => "RESOLUTION",
            TacticalPhase.Complete => "COMPLETE",
            _ => phase.ToString().ToUpper()
        };
        
        phaseLabel.Text = phaseName;
        
        var seconds = ticksInPhase / 20; // Assuming 20 ticks/sec
        timerLabel.Text = $"Time: {seconds}s";
        
        // Color based on phase
        background.Color = phase switch
        {
            TacticalPhase.Setup => new Color(0.2f, 0.2f, 0.3f, 0.85f),
            TacticalPhase.Negotiation => new Color(0.2f, 0.3f, 0.2f, 0.85f),
            TacticalPhase.Contact => new Color(0.3f, 0.25f, 0.15f, 0.85f),
            TacticalPhase.Pressure => new Color(0.4f, 0.15f, 0.15f, 0.85f),
            TacticalPhase.Resolution => new Color(0.35f, 0.35f, 0.15f, 0.85f),
            TacticalPhase.Complete => new Color(0.15f, 0.3f, 0.15f, 0.85f),
            _ => new Color(0.1f, 0.1f, 0.1f, 0.85f)
        };
    }
    
    public override void _ExitTree()
    {
        if (phaseSystem != null)
        {
            phaseSystem.PhaseChanged -= OnPhaseChanged;
        }
    }
}
