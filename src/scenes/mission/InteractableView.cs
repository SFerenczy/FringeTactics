using Godot;

namespace FringeTactics;

/// <summary>
/// Visual representation of an interactable object.
/// </summary>
public partial class InteractableView : Node2D
{
    private const int TileSize = GridConstants.TileSize;
    
    private Interactable interactable;
    private ColorRect sprite;
    private ColorRect iconOverlay;
    private ProgressBar channelProgress;
    private bool isInitialized = false;
    
    public int InteractableId => interactable?.Id ?? -1;
    
    public override void _Ready()
    {
        CreateVisuals();
    }
    
    private void CreateVisuals()
    {
        // Main sprite (background)
        sprite = new ColorRect();
        sprite.Size = new Vector2(TileSize - 2, TileSize - 2);
        sprite.Position = new Vector2(1, 1);
        AddChild(sprite);
        
        // Icon overlay (smaller, centered) for type indication
        iconOverlay = new ColorRect();
        iconOverlay.Size = new Vector2(TileSize - 12, TileSize - 12);
        iconOverlay.Position = new Vector2(6, 6);
        AddChild(iconOverlay);
        
        // Channel progress bar
        channelProgress = new ProgressBar();
        channelProgress.Size = new Vector2(TileSize - 4, 6);
        channelProgress.Position = new Vector2(2, TileSize - 8);
        channelProgress.ShowPercentage = false;
        channelProgress.Visible = false;
        channelProgress.MinValue = 0;
        channelProgress.MaxValue = 100;
        AddChild(channelProgress);
    }
    
    public void Setup(Interactable interactable)
    {
        this.interactable = interactable;
        interactable.StateChanged += OnStateChanged;
        isInitialized = true;
        UpdateVisuals();
    }
    
    public void Cleanup()
    {
        if (interactable != null)
        {
            interactable.StateChanged -= OnStateChanged;
            interactable = null;
        }
    }
    
    private void OnStateChanged(Interactable obj, InteractableState newState)
    {
        UpdateVisuals();
    }
    
    private void UpdateVisuals()
    {
        if (!isInitialized || interactable == null)
        {
            return;
        }
        
        // Update position
        Position = new Vector2(interactable.Position.X * TileSize, interactable.Position.Y * TileSize);
        
        // Update colors based on type and state
        var (bgColor, iconColor) = GetColors();
        sprite.Color = bgColor;
        iconOverlay.Color = iconColor;
    }
    
    private (Color bg, Color icon) GetColors()
    {
        return interactable.Type switch
        {
            InteractableTypes.Door => GetDoorColors(),
            InteractableTypes.Terminal => GetTerminalColors(),
            InteractableTypes.Hazard => GetHazardColors(),
            _ => (Colors.Magenta, Colors.White)
        };
    }
    
    private (Color bg, Color icon) GetDoorColors()
    {
        return interactable.State switch
        {
            // Open door: transparent green (walkable)
            InteractableState.DoorOpen => (
                new Color(0.3f, 0.6f, 0.3f, 0.3f),
                new Color(0.4f, 0.8f, 0.4f, 0.5f)
            ),
            // Closed door: brown/tan (blocked but unlocked)
            InteractableState.DoorClosed => (
                new Color(0.55f, 0.35f, 0.15f, 0.9f),
                new Color(0.7f, 0.5f, 0.25f, 1.0f)
            ),
            // Locked door: red tint (blocked and locked)
            InteractableState.DoorLocked => (
                new Color(0.6f, 0.2f, 0.2f, 0.9f),
                new Color(0.9f, 0.3f, 0.3f, 1.0f)
            ),
            _ => (Colors.Gray, Colors.DarkGray)
        };
    }
    
    private (Color bg, Color icon) GetTerminalColors()
    {
        return interactable.State switch
        {
            // Idle terminal: blue (ready to hack)
            InteractableState.TerminalIdle => (
                new Color(0.15f, 0.25f, 0.45f, 0.9f),
                new Color(0.3f, 0.5f, 0.9f, 1.0f)
            ),
            // Hacking in progress: yellow/amber
            InteractableState.TerminalHacking => (
                new Color(0.5f, 0.45f, 0.1f, 0.9f),
                new Color(0.9f, 0.8f, 0.2f, 1.0f)
            ),
            // Hacked terminal: green (completed)
            InteractableState.TerminalHacked => (
                new Color(0.15f, 0.4f, 0.15f, 0.9f),
                new Color(0.3f, 0.8f, 0.3f, 1.0f)
            ),
            // Disabled terminal: gray
            InteractableState.TerminalDisabled => (
                new Color(0.25f, 0.25f, 0.25f, 0.7f),
                new Color(0.4f, 0.4f, 0.4f, 0.8f)
            ),
            _ => (Colors.Gray, Colors.DarkGray)
        };
    }
    
    private (Color bg, Color icon) GetHazardColors()
    {
        return interactable.State switch
        {
            // Armed hazard: orange/red (dangerous)
            InteractableState.HazardArmed => (
                new Color(0.5f, 0.25f, 0.05f, 0.9f),
                new Color(1.0f, 0.5f, 0.0f, 1.0f)
            ),
            // Triggered hazard: fading red
            InteractableState.HazardTriggered => (
                new Color(0.4f, 0.1f, 0.1f, 0.5f),
                new Color(0.6f, 0.2f, 0.2f, 0.6f)
            ),
            // Disabled hazard: gray (safe)
            InteractableState.HazardDisabled => (
                new Color(0.3f, 0.3f, 0.3f, 0.6f),
                new Color(0.5f, 0.5f, 0.5f, 0.7f)
            ),
            _ => (Colors.Gray, Colors.DarkGray)
        };
    }
    
    public void ShowChannelProgress(float progress)
    {
        channelProgress.Visible = true;
        channelProgress.Value = progress * 100;
    }
    
    public void HideChannelProgress()
    {
        channelProgress.Visible = false;
    }
    
    public override void _ExitTree()
    {
        Cleanup();
    }
}
