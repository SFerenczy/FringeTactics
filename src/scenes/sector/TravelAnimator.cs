using Godot;
using System;

namespace FringeTactics;

/// <summary>
/// Animates travel between star systems with a moving dot.
/// Purely visual - sim travel is executed before/after animation.
/// </summary>
public partial class TravelAnimator : Node2D
{
    /// <summary>
    /// Emitted when animation completes. Progress is 0-1 where encounter occurred (1 = no encounter).
    /// </summary>
    [Signal]
    public delegate void TravelAnimationCompletedEventHandler(float encounterProgress);

    private Vector2 startPos;
    private Vector2 endPos;
    private float duration;
    private float elapsed;
    private bool isAnimating;
    private float encounterProgress = 1f;

    private ColorRect travelDot;
    private const float DOT_SIZE = 12f;
    private const float DEFAULT_DURATION = 0.8f;

    public override void _Ready()
    {
        CreateTravelDot();
        Visible = false;
    }

    private void CreateTravelDot()
    {
        travelDot = new ColorRect();
        travelDot.Size = new Vector2(DOT_SIZE, DOT_SIZE);
        travelDot.Color = Colors.White;
        travelDot.PivotOffset = new Vector2(DOT_SIZE / 2, DOT_SIZE / 2);
        AddChild(travelDot);
    }

    /// <summary>
    /// Start travel animation between two positions.
    /// </summary>
    /// <param name="from">Start position in map coordinates</param>
    /// <param name="to">End position in map coordinates</param>
    /// <param name="encounterAt">Progress (0-1) where encounter occurs, or 1 for no encounter</param>
    /// <param name="animDuration">Animation duration in seconds</param>
    public void StartAnimation(Vector2 from, Vector2 to, float encounterAt = 1f, float animDuration = DEFAULT_DURATION)
    {
        startPos = from;
        endPos = to;
        encounterProgress = Mathf.Clamp(encounterAt, 0f, 1f);
        duration = animDuration > 0 ? animDuration : DEFAULT_DURATION;
        elapsed = 0f;
        isAnimating = true;

        EnsureTravelDot();
        travelDot.Position = startPos - new Vector2(DOT_SIZE / 2, DOT_SIZE / 2);
        Visible = true;
    }

    private void EnsureTravelDot()
    {
        if (travelDot == null || !GodotObject.IsInstanceValid(travelDot))
        {
            CreateTravelDot();
        }
    }

    public override void _Process(double delta)
    {
        if (!isAnimating) return;
        if (travelDot == null || !GodotObject.IsInstanceValid(travelDot)) return;

        elapsed += (float)delta;
        float progress = Mathf.Clamp(elapsed / duration, 0f, 1f);

        // Stop at encounter point if one occurred
        float targetProgress = Mathf.Min(progress, encounterProgress);
        Vector2 currentPos = startPos.Lerp(endPos, targetProgress);
        travelDot.Position = currentPos - new Vector2(DOT_SIZE / 2, DOT_SIZE / 2);

        // Check completion
        if (progress >= encounterProgress)
        {
            isAnimating = false;
            Visible = false;
            EmitSignal(SignalName.TravelAnimationCompleted, encounterProgress);
        }
    }

    /// <summary>
    /// Check if animation is currently playing.
    /// </summary>
    public bool IsAnimating => isAnimating;

    /// <summary>
    /// Cancel any running animation.
    /// </summary>
    public void Cancel()
    {
        isAnimating = false;
        Visible = false;
    }
}
