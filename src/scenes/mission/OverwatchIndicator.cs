using Godot;

namespace FringeTactics;

/// <summary>
/// Visual indicator for overwatch zones.
/// Renders a circle or cone showing the overwatch coverage area.
/// </summary>
public partial class OverwatchIndicator : Node2D
{
    private Actor actor;
    private Color zoneColor;
    private bool isInitialized = false;
    
    public void Setup(Actor actor, bool isEnemy)
    {
        this.actor = actor;
        zoneColor = isEnemy 
            ? new Color(1f, 0.2f, 0.2f, 0.15f)
            : new Color(0.2f, 0.5f, 1f, 0.15f);
        
        actor.OverwatchActivated += OnOverwatchActivated;
        actor.OverwatchDeactivated += OnOverwatchDeactivated;
        actor.Overwatch.StateChanged += OnStateChanged;
        
        isInitialized = true;
        UpdateVisibility();
    }
    
    private void OnOverwatchActivated(Actor a) => UpdateVisibility();
    private void OnOverwatchDeactivated(Actor a) => UpdateVisibility();
    private void OnStateChanged(OverwatchState state) => QueueRedraw();
    
    private void UpdateVisibility()
    {
        Visible = actor != null && actor.IsOnOverwatch;
        QueueRedraw();
    }
    
    public override void _Process(double delta)
    {
        if (!isInitialized || actor == null) return;
        
        // Follow actor position
        Position = actor.GetVisualPosition(GridConstants.TileSize) + new Vector2(GridConstants.TileSize / 2f, GridConstants.TileSize / 2f);
    }
    
    public override void _Draw()
    {
        if (!isInitialized || actor == null || !actor.IsOnOverwatch) return;
        
        var range = actor.Overwatch.GetEffectiveRange(actor.EquippedWeapon.Range);
        var pixelRange = range * GridConstants.TileSize;
        
        if (actor.Overwatch.FacingDirection == null)
        {
            // 360° circle
            DrawCircle(Vector2.Zero, pixelRange, zoneColor);
            DrawArc(Vector2.Zero, pixelRange, 0, Mathf.Tau, 64, 
                    zoneColor with { A = 0.4f }, 2f);
        }
        else
        {
            // Cone
            var facing = actor.Overwatch.FacingDirection.Value;
            var facingAngle = Mathf.Atan2(facing.Y, facing.X);
            var halfAngle = Mathf.DegToRad(actor.Overwatch.ConeAngle / 2f);
            
            var points = new Vector2[32];
            points[0] = Vector2.Zero;
            for (int i = 0; i < 31; i++)
            {
                var angle = facingAngle - halfAngle + (halfAngle * 2 * i / 30f);
                points[i + 1] = new Vector2(
                    Mathf.Cos(angle) * pixelRange,
                    Mathf.Sin(angle) * pixelRange
                );
            }
            
            DrawColoredPolygon(points, zoneColor);
            
            // Draw outline
            for (int i = 0; i < points.Length - 1; i++)
            {
                DrawLine(points[i], points[i + 1], zoneColor with { A = 0.4f }, 2f);
            }
            DrawLine(points[^1], points[0], zoneColor with { A = 0.4f }, 2f);
        }
    }
    
    public void Cleanup()
    {
        if (actor != null)
        {
            actor.OverwatchActivated -= OnOverwatchActivated;
            actor.OverwatchDeactivated -= OnOverwatchDeactivated;
            actor.Overwatch.StateChanged -= OnStateChanged;
        }
    }
    
    public override void _ExitTree()
    {
        Cleanup();
    }
}
