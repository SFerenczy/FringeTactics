using Godot;
using System;
using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// Animates travel between star systems with a moving dot.
/// Supports multi-segment paths for multi-hop routes.
/// Purely visual - sim travel is executed before/after animation.
/// </summary>
public partial class TravelAnimator : Node2D
{
    /// <summary>
    /// Emitted when animation completes. Progress is 0-1 where encounter occurred (1 = no encounter).
    /// </summary>
    [Signal]
    public delegate void TravelAnimationCompletedEventHandler(float encounterProgress);

    private List<Vector2> waypoints = new();
    private int currentSegmentIndex;
    private float segmentElapsed;
    private float segmentDuration;
    private bool isAnimating;
    private float encounterProgress = 1f;
    private int encounterSegmentIndex;

    private ColorRect travelDot;
    private const float DOT_SIZE = 12f;
    private const float DURATION_PER_SEGMENT = 0.5f;

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
    /// Start travel animation along a path of waypoints.
    /// </summary>
    /// <param name="path">List of positions to travel through</param>
    /// <param name="encounterAtSegment">Segment index where encounter occurs (-1 for no encounter)</param>
    /// <param name="encounterAtProgress">Progress within that segment (0-1) where encounter occurs</param>
    public void StartAnimation(List<Vector2> path, int encounterAtSegment = -1, float encounterAtProgress = 1f)
    {
        if (path == null || path.Count < 2)
        {
            GD.PrintErr("[TravelAnimator] Path must have at least 2 waypoints");
            return;
        }

        waypoints = new List<Vector2>(path);
        currentSegmentIndex = 0;
        segmentElapsed = 0f;
        segmentDuration = DURATION_PER_SEGMENT;
        encounterSegmentIndex = encounterAtSegment >= 0 ? encounterAtSegment : path.Count;
        encounterProgress = Mathf.Clamp(encounterAtProgress, 0f, 1f);
        isAnimating = true;

        EnsureTravelDot();
        travelDot.Position = waypoints[0] - new Vector2(DOT_SIZE / 2, DOT_SIZE / 2);
        Visible = true;
    }

    /// <summary>
    /// Start travel animation between two positions (convenience overload).
    /// </summary>
    public void StartAnimation(Vector2 from, Vector2 to, float encounterAt = 1f)
    {
        int encSegment = encounterAt < 1f ? 0 : -1;
        StartAnimation(new List<Vector2> { from, to }, encSegment, encounterAt);
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
        if (waypoints.Count < 2 || currentSegmentIndex >= waypoints.Count - 1)
        {
            CompleteAnimation();
            return;
        }

        segmentElapsed += (float)delta;
        float segmentProgress = Mathf.Clamp(segmentElapsed / segmentDuration, 0f, 1f);

        // Check if we should stop for encounter in this segment
        bool isEncounterSegment = currentSegmentIndex == encounterSegmentIndex;
        float targetProgress = isEncounterSegment ? Mathf.Min(segmentProgress, encounterProgress) : segmentProgress;

        // Interpolate position within current segment
        Vector2 segmentStart = waypoints[currentSegmentIndex];
        Vector2 segmentEnd = waypoints[currentSegmentIndex + 1];
        Vector2 currentPos = segmentStart.Lerp(segmentEnd, targetProgress);
        travelDot.Position = currentPos - new Vector2(DOT_SIZE / 2, DOT_SIZE / 2);

        // Check if we hit encounter point
        if (isEncounterSegment && segmentProgress >= encounterProgress)
        {
            CompleteAnimation();
            return;
        }

        // Check if segment is complete
        if (segmentProgress >= 1f)
        {
            currentSegmentIndex++;
            segmentElapsed = 0f;

            // Check if all segments complete
            if (currentSegmentIndex >= waypoints.Count - 1)
            {
                CompleteAnimation();
            }
        }
    }

    private void CompleteAnimation()
    {
        isAnimating = false;
        Visible = false;
        EmitSignal(SignalName.TravelAnimationCompleted, encounterProgress);
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
