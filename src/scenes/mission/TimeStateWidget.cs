using Godot;

namespace FringeTactics;

public partial class TimeStateWidget : HBoxContainer
{
    // Displays current time state: PAUSED/PLAYING and game speed.
    // Connect to a TimeSystem to auto-update.

    private Label stateLabel;
    private Label speedLabel;
    private Label tickLabel;

    private TimeSystem timeSystem;

    public override void _Ready()
    {
        stateLabel = GetNode<Label>("StateLabel");
        speedLabel = GetNode<Label>("SpeedLabel");
        tickLabel = GetNode<Label>("TickLabel");
        UpdateDisplay();
    }

    public void ConnectToTimeSystem(TimeSystem ts)
    {
        DisconnectFromTimeSystem();
        timeSystem = ts;
        timeSystem.PauseChanged += OnPauseChanged;
        timeSystem.TickAdvanced += OnTickAdvanced;
        timeSystem.TimeScaleChanged += OnTimeScaleChanged;
        UpdateDisplay();
    }

    public override void _ExitTree()
    {
        DisconnectFromTimeSystem();
    }

    private void DisconnectFromTimeSystem()
    {
        if (timeSystem != null)
        {
            timeSystem.PauseChanged -= OnPauseChanged;
            timeSystem.TickAdvanced -= OnTickAdvanced;
            timeSystem.TimeScaleChanged -= OnTimeScaleChanged;
            timeSystem = null;
        }
    }

    private void OnPauseChanged(bool paused)
    {
        UpdateDisplay();
    }

    private void OnTickAdvanced(int tick)
    {
        UpdateTickDisplay();
    }

    private void OnTimeScaleChanged(float scale)
    {
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (timeSystem == null)
        {
            stateLabel.Text = "NO SIM";
            stateLabel.Modulate = Colors.Gray;
            speedLabel.Text = "";
            tickLabel.Text = "";
            return;
        }

        // State indicator
        if (timeSystem.IsPaused)
        {
            stateLabel.Text = "⏸ PAUSED";
            stateLabel.Modulate = Colors.Yellow;
        }
        else
        {
            stateLabel.Text = "▶ PLAYING";
            stateLabel.Modulate = Colors.Green;
        }

        // Speed indicator
        var speedText = $"{timeSystem.TimeScale:F1}x";
        speedLabel.Text = speedText;
        speedLabel.Modulate = timeSystem.TimeScale == 1.0f ? Colors.White : Colors.Cyan;

        UpdateTickDisplay();
    }

    private void UpdateTickDisplay()
    {
        if (timeSystem != null)
        {
            tickLabel.Text = $"T:{timeSystem.CurrentTick}";
        }
    }
}
