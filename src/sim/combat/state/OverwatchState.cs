using Godot;
using System;

namespace FringeTactics;

/// <summary>
/// Tracks overwatch state for an actor.
/// </summary>
public class OverwatchState
{
    public bool IsActive { get; private set; } = false;
    public Vector2I? FacingDirection { get; private set; } = null;
    public float ConeAngle { get; private set; } = 90f;
    public int CustomRange { get; private set; } = 0;
    public int ShotsRemaining { get; private set; } = 1;
    public int ActivatedTick { get; private set; } = 0;
    
    public event Action<OverwatchState> StateChanged;
    
    public void Activate(int currentTick, Vector2I? facingDirection = null, 
                         float coneAngle = 90f, int customRange = 0, int shots = 1)
    {
        IsActive = true;
        FacingDirection = facingDirection;
        ConeAngle = coneAngle;
        CustomRange = customRange;
        ShotsRemaining = shots;
        ActivatedTick = currentTick;
        StateChanged?.Invoke(this);
    }
    
    public void Deactivate()
    {
        if (!IsActive) return;
        IsActive = false;
        FacingDirection = null;
        StateChanged?.Invoke(this);
    }
    
    public void ConsumeShot()
    {
        if (ShotsRemaining > 0)
        {
            ShotsRemaining--;
            if (ShotsRemaining == 0)
            {
                Deactivate();
            }
            else
            {
                StateChanged?.Invoke(this);
            }
        }
    }
    
    public bool IsInCone(Vector2I fromPos, Vector2I targetPos)
    {
        if (!IsActive) return false;
        if (FacingDirection == null) return true;
        
        var toTarget = targetPos - fromPos;
        if (toTarget == Vector2I.Zero) return false;
        
        var targetAngle = Mathf.RadToDeg(Mathf.Atan2(toTarget.Y, toTarget.X));
        var facingAngle = Mathf.RadToDeg(Mathf.Atan2(FacingDirection.Value.Y, FacingDirection.Value.X));
        
        var angleDiff = Mathf.Abs(Mathf.Wrap(targetAngle - facingAngle, -180f, 180f));
        return angleDiff <= ConeAngle / 2f;
    }
    
    /// <summary>
    /// Get the effective range for this overwatch (custom or weapon range).
    /// </summary>
    public int GetEffectiveRange(int weaponRange)
    {
        return CustomRange > 0 ? CustomRange : weaponRange;
    }
}
