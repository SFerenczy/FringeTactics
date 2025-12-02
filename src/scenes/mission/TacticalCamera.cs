using Godot;

namespace FringeTactics;

/// <summary>
/// Camera modes for tactical view.
/// </summary>
public enum CameraMode
{
    Free,           // Player controls camera freely
    FollowSelected  // Camera follows selected unit (future: FollowAction for combat)
}

/// <summary>
/// Camera controller for tactical missions.
/// Supports panning, zooming, and centering on positions.
/// </summary>
public partial class TacticalCamera : Camera2D
{
    [Export] public float PanSpeed { get; set; } = 400f;
    [Export] public float ZoomSpeed { get; set; } = 0.1f;
    [Export] public float MinZoom { get; set; } = 0.5f;
    [Export] public float MaxZoom { get; set; } = 2.0f;
    [Export] public float EdgePanMargin { get; set; } = 20f;
    [Export] public bool EnableEdgePan { get; set; } = true;
    
    public CameraMode Mode { get; set; } = CameraMode.Free;
    
    // Map bounds for clamping (set by MissionView)
    private Vector2 mapMin = Vector2.Zero;
    private Vector2 mapMax = new Vector2(1000, 1000);
    private bool hasBounds = false;
    
    // Smooth follow
    private Node2D followTarget;
    private float followSmoothness = 5.0f;
    
    public override void _Ready()
    {
        // Start with default zoom
        Zoom = Vector2.One;
    }
    
    public override void _Process(double delta)
    {
        var dt = (float)delta;
        
        // Handle keyboard panning
        HandleKeyboardPan(dt);
        
        // Handle edge panning
        if (EnableEdgePan)
        {
            HandleEdgePan(dt);
        }
        
        // Follow target if in follow mode
        if (Mode == CameraMode.FollowSelected && followTarget != null && IsInstanceValid(followTarget))
        {
            var targetPos = followTarget.GlobalPosition;
            GlobalPosition = GlobalPosition.Lerp(targetPos, followSmoothness * dt);
        }
        
        // Clamp to bounds
        if (hasBounds)
        {
            ClampToBounds();
        }
    }
    
    public override void _UnhandledInput(InputEvent @event)
    {
        // Handle zoom with mouse wheel
        if (@event is InputEventMouseButton mouseButton)
        {
            if (mouseButton.ButtonIndex == MouseButton.WheelUp)
            {
                ZoomIn();
                GetViewport().SetInputAsHandled();
            }
            else if (mouseButton.ButtonIndex == MouseButton.WheelDown)
            {
                ZoomOut();
                GetViewport().SetInputAsHandled();
            }
        }
        
        // Center on selection with Home key or C
        if (@event is InputEventKey keyEvent && keyEvent.Pressed)
        {
            if (keyEvent.Keycode == Key.Home || keyEvent.Keycode == Key.C)
            {
                if (followTarget != null && IsInstanceValid(followTarget))
                {
                    CenterOnPosition(followTarget.GlobalPosition);
                }
            }
        }
    }
    
    private void HandleKeyboardPan(float dt)
    {
        var panDirection = Vector2.Zero;
        
        if (Input.IsKeyPressed(Key.W) || Input.IsKeyPressed(Key.Up))
        {
            panDirection.Y -= 1;
        }
        if (Input.IsKeyPressed(Key.S) || Input.IsKeyPressed(Key.Down))
        {
            panDirection.Y += 1;
        }
        if (Input.IsKeyPressed(Key.A) || Input.IsKeyPressed(Key.Left))
        {
            panDirection.X -= 1;
        }
        if (Input.IsKeyPressed(Key.D) || Input.IsKeyPressed(Key.Right))
        {
            panDirection.X += 1;
        }
        
        if (panDirection != Vector2.Zero)
        {
            // Switch to free mode when manually panning
            Mode = CameraMode.Free;
            panDirection = panDirection.Normalized();
            GlobalPosition += panDirection * PanSpeed * dt / Zoom.X;
        }
    }
    
    private void HandleEdgePan(float dt)
    {
        var viewport = GetViewport();
        if (viewport == null) return;
        
        var mousePos = viewport.GetMousePosition();
        var viewportSize = viewport.GetVisibleRect().Size;
        
        var panDirection = Vector2.Zero;
        
        if (mousePos.X < EdgePanMargin)
        {
            panDirection.X -= 1;
        }
        else if (mousePos.X > viewportSize.X - EdgePanMargin)
        {
            panDirection.X += 1;
        }
        
        if (mousePos.Y < EdgePanMargin)
        {
            panDirection.Y -= 1;
        }
        else if (mousePos.Y > viewportSize.Y - EdgePanMargin)
        {
            panDirection.Y += 1;
        }
        
        if (panDirection != Vector2.Zero)
        {
            Mode = CameraMode.Free;
            panDirection = panDirection.Normalized();
            GlobalPosition += panDirection * PanSpeed * 0.5f * dt / Zoom.X;
        }
    }
    
    private void ZoomIn()
    {
        var newZoom = Zoom.X + ZoomSpeed;
        newZoom = Mathf.Clamp(newZoom, MinZoom, MaxZoom);
        Zoom = new Vector2(newZoom, newZoom);
    }
    
    private void ZoomOut()
    {
        var newZoom = Zoom.X - ZoomSpeed;
        newZoom = Mathf.Clamp(newZoom, MinZoom, MaxZoom);
        Zoom = new Vector2(newZoom, newZoom);
    }
    
    private void ClampToBounds()
    {
        var viewportSize = GetViewportRect().Size / Zoom;
        var halfView = viewportSize / 2;
        
        // Allow some overshoot for better feel
        var overshoot = halfView * 0.2f;
        
        var clampedX = Mathf.Clamp(GlobalPosition.X, mapMin.X - overshoot.X, mapMax.X + overshoot.X);
        var clampedY = Mathf.Clamp(GlobalPosition.Y, mapMin.Y - overshoot.Y, mapMax.Y + overshoot.Y);
        
        GlobalPosition = new Vector2(clampedX, clampedY);
    }
    
    /// <summary>
    /// Set the map bounds for camera clamping.
    /// </summary>
    public void SetMapBounds(Vector2 min, Vector2 max)
    {
        mapMin = min;
        mapMax = max;
        hasBounds = true;
    }
    
    /// <summary>
    /// Set the map bounds from grid size and tile size.
    /// </summary>
    public void SetMapBoundsFromGrid(Vector2I gridSize, int tileSize)
    {
        mapMin = Vector2.Zero;
        mapMax = new Vector2(gridSize.X * tileSize, gridSize.Y * tileSize);
        hasBounds = true;
    }
    
    /// <summary>
    /// Set the target to follow when in FollowSelected mode.
    /// </summary>
    public void SetFollowTarget(Node2D target)
    {
        followTarget = target;
        if (target != null)
        {
            Mode = CameraMode.FollowSelected;
        }
    }
    
    /// <summary>
    /// Clear the follow target and switch to free mode.
    /// </summary>
    public void ClearFollowTarget()
    {
        followTarget = null;
        Mode = CameraMode.Free;
    }
    
    /// <summary>
    /// Instantly center the camera on a world position.
    /// </summary>
    public void CenterOnPosition(Vector2 worldPos)
    {
        GlobalPosition = worldPos;
    }
    
    /// <summary>
    /// Center the camera on a grid position.
    /// </summary>
    public void CenterOnGrid(Vector2I gridPos, int tileSize)
    {
        var worldPos = new Vector2(
            gridPos.X * tileSize + tileSize / 2,
            gridPos.Y * tileSize + tileSize / 2
        );
        CenterOnPosition(worldPos);
    }
    
    /// <summary>
    /// Center the camera on the map center.
    /// </summary>
    public void CenterOnMap()
    {
        if (hasBounds)
        {
            var center = (mapMin + mapMax) / 2;
            CenterOnPosition(center);
        }
    }
}
