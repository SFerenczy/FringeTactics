using Godot;

namespace FringeTactics;

public partial class ActorView : Node2D
{
    public const int TileSize = 32;
    private const float HIT_FLASH_DURATION = 0.15f;
    private const float DEAD_ALPHA = 0.4f;

    private Actor actor;
    private bool isSelected = false;
    private Color baseColor = Colors.White;

    private ColorRect sprite;
    private ColorRect selectionIndicator;
    private ColorRect hpBarBackground;
    private ColorRect hpBarFill;

    private float hitFlashTimer = 0f;
    private bool isFlashing = false;

    public override void _Ready()
    {
        sprite = GetNode<ColorRect>("Sprite");
        selectionIndicator = GetNode<ColorRect>("SelectionIndicator");
        hpBarBackground = GetNode<ColorRect>("HpBarBackground");
        hpBarFill = GetNode<ColorRect>("HpBarFill");

        selectionIndicator.Visible = false;

        sprite.Color = baseColor;

        if (actor != null)
        {
            Position = actor.GetVisualPosition(TileSize);
            UpdateHpBar();
            SubscribeToActorEvents();
        }
    }

    public void Setup(Actor actorData, Color color)
    {
        // Unsubscribe from old actor if any
        if (actor != null)
        {
            actor.DamageTaken -= OnDamageTaken;
            actor.Died -= OnDied;
        }

        actor = actorData;
        baseColor = color;

        if (sprite != null)
        {
            sprite.Color = color;
            Position = actor.GetVisualPosition(TileSize);
            UpdateHpBar();
            SubscribeToActorEvents();
        }
    }

    private void SubscribeToActorEvents()
    {
        if (actor == null)
        {
            return;
        }

        actor.DamageTaken += OnDamageTaken;
        actor.Died += OnDied;
    }

    private void OnDamageTaken(Actor a, int damage)
    {
        StartHitFlash();
        UpdateHpBar();
    }

    private void OnDied(Actor a)
    {
        UpdateHpBar();
        ShowDeadState();
    }

    private void StartHitFlash()
    {
        hitFlashTimer = HIT_FLASH_DURATION;
        isFlashing = true;
        sprite.Color = Colors.White; // Flash white
    }

    private void ShowDeadState()
    {
        // Dim the sprite and hide selection
        sprite.Color = new Color(baseColor.R * 0.5f, baseColor.G * 0.5f, baseColor.B * 0.5f, DEAD_ALPHA);
        selectionIndicator.Visible = false;
        hpBarBackground.Visible = false;
        hpBarFill.Visible = false;
    }

    private void UpdateHpBar()
    {
        if (actor == null || hpBarFill == null)
        {
            return;
        }

        var hpPercent = (float)actor.Hp / actor.MaxHp;
        var maxWidth = hpBarBackground.Size.X - 2; // Account for border
        hpBarFill.Size = new Vector2(maxWidth * hpPercent, hpBarFill.Size.Y);

        // Color based on HP
        if (hpPercent > 0.6f)
        {
            hpBarFill.Color = new Color(0.2f, 0.8f, 0.2f); // Green
        }
        else if (hpPercent > 0.3f)
        {
            hpBarFill.Color = new Color(0.9f, 0.7f, 0.1f); // Yellow
        }
        else
        {
            hpBarFill.Color = new Color(0.9f, 0.2f, 0.2f); // Red
        }
    }

    public override void _Process(double delta)
    {
        if (actor == null)
        {
            return;
        }

        Position = actor.GetVisualPosition(TileSize);

        // Handle hit flash
        if (isFlashing)
        {
            hitFlashTimer -= (float)delta;
            if (hitFlashTimer <= 0)
            {
                isFlashing = false;
                if (actor.State == ActorState.Dead)
                {
                    ShowDeadState();
                }
                else
                {
                    sprite.Color = baseColor;
                }
            }
        }
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        // Don't show selection on dead actors
        selectionIndicator.Visible = selected && actor?.State == ActorState.Alive;
    }

    public int GetActorId()
    {
        return actor?.Id ?? -1;
    }

    public Actor GetActor()
    {
        return actor;
    }

    public override void _ExitTree()
    {
        // Cleanup event subscriptions
        if (actor != null)
        {
            actor.DamageTaken -= OnDamageTaken;
            actor.Died -= OnDied;
        }
    }
}
