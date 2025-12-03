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
    private Label ammoLabel;
    private ColorRect reloadIndicator;

    private float hitFlashTimer = 0f;
    private bool isFlashing = false;
    private int lastDisplayedMagazine = -1; // Track to avoid per-frame updates

    public override void _Ready()
    {
        sprite = GetNode<ColorRect>("Sprite");
        selectionIndicator = GetNode<ColorRect>("SelectionIndicator");
        hpBarBackground = GetNode<ColorRect>("HpBarBackground");
        hpBarFill = GetNode<ColorRect>("HpBarFill");

        selectionIndicator.Visible = false;

        sprite.Color = baseColor;

        CreateAmmoLabel();
        CreateReloadIndicator();

        if (actor != null)
        {
            Position = actor.GetVisualPosition(TileSize);
            UpdateHpBar();
            UpdateAmmoDisplay();
            SubscribeToActorEvents();
        }
    }

    private void CreateAmmoLabel()
    {
        ammoLabel = new Label();
        ammoLabel.Position = new Vector2(0, TileSize - 4);
        ammoLabel.AddThemeColorOverride("font_color", new Color(0.9f, 0.9f, 0.9f));
        ammoLabel.AddThemeColorOverride("font_shadow_color", new Color(0, 0, 0));
        ammoLabel.AddThemeFontSizeOverride("font_size", 10);
        ammoLabel.AddThemeConstantOverride("shadow_offset_x", 1);
        ammoLabel.AddThemeConstantOverride("shadow_offset_y", 1);
        AddChild(ammoLabel);
    }

    private void CreateReloadIndicator()
    {
        reloadIndicator = new ColorRect();
        reloadIndicator.Size = new Vector2(TileSize - 4, 3);
        reloadIndicator.Position = new Vector2(2, TileSize - 8);
        reloadIndicator.Color = new Color(0.3f, 0.6f, 1.0f); // Blue for reload
        reloadIndicator.Visible = false;
        AddChild(reloadIndicator);
    }

    public void Setup(Actor actorData, Color color)
    {
        // Unsubscribe from old actor if any
        if (actor != null)
        {
            actor.DamageTaken -= OnDamageTaken;
            actor.Died -= OnDied;
            actor.ReloadCompleted -= OnReloadCompleted;
        }

        actor = actorData;
        baseColor = color;
        lastDisplayedMagazine = -1; // Force update on next frame

        if (sprite != null)
        {
            sprite.Color = color;
            Position = actor.GetVisualPosition(TileSize);
            UpdateHpBar();
            UpdateAmmoDisplay();
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
        actor.ReloadCompleted += OnReloadCompleted;
    }

    private void OnReloadCompleted(Actor a)
    {
        UpdateAmmoDisplay();
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
        ammoLabel.Visible = false;
        reloadIndicator.Visible = false;
    }

    private void UpdateAmmoDisplay()
    {
        if (actor == null || ammoLabel == null)
        {
            return;
        }

        // Only show ammo for player units
        if (actor.Type != ActorTypes.Crew)
        {
            ammoLabel.Visible = false;
            return;
        }

        // Skip update if ammo hasn't changed
        if (actor.CurrentMagazine == lastDisplayedMagazine)
        {
            return;
        }
        lastDisplayedMagazine = actor.CurrentMagazine;

        ammoLabel.Visible = true;
        ammoLabel.Text = actor.CurrentMagazine.ToString();

        // Color based on ammo state
        if (actor.CurrentMagazine == 0)
        {
            ammoLabel.AddThemeColorOverride("font_color", new Color(1.0f, 0.3f, 0.3f)); // Red - empty
        }
        else if (actor.CurrentMagazine <= actor.EquippedWeapon.MagazineSize / 3)
        {
            ammoLabel.AddThemeColorOverride("font_color", new Color(1.0f, 0.8f, 0.2f)); // Yellow - low
        }
        else
        {
            ammoLabel.AddThemeColorOverride("font_color", new Color(0.9f, 0.9f, 0.9f)); // White - ok
        }
    }

    private void UpdateReloadIndicator()
    {
        if (actor == null || reloadIndicator == null)
        {
            return;
        }

        if (actor.IsReloading && actor.Type == ActorTypes.Crew)
        {
            reloadIndicator.Visible = true;
            
            // Show progress bar
            var progress = 1f - (actor.ReloadProgress / (float)actor.EquippedWeapon.ReloadTicks);
            reloadIndicator.Size = new Vector2((TileSize - 4) * progress, 3);
        }
        else
        {
            reloadIndicator.Visible = false;
        }
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

        // Update ammo and reload display
        UpdateAmmoDisplay();
        UpdateReloadIndicator();

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
            actor.ReloadCompleted -= OnReloadCompleted;
        }
    }
}
