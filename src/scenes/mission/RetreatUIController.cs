using Godot;

namespace FringeTactics;

/// <summary>
/// Manages retreat UI elements: retreat button, extraction status, and entry zone highlights.
/// </summary>
public partial class RetreatUIController : Control
{
    private Button retreatButton;
    private Label extractionStatusLabel;
    private Node2D entryZoneHighlightLayer;
    
    private CombatState combatState;
    private Node2D gridDisplay;
    private int tileSize;

    public void Initialize(CombatState combatState, CanvasLayer uiLayer, Node2D gridDisplay, int tileSize)
    {
        this.combatState = combatState;
        this.gridDisplay = gridDisplay;
        this.tileSize = tileSize;
        
        CreateUI(uiLayer);
        SubscribeToEvents();
    }

    private void CreateUI(CanvasLayer uiLayer)
    {
        retreatButton = new Button();
        retreatButton.Text = "Retreat";
        retreatButton.Position = new Vector2(10, 75);
        retreatButton.Size = new Vector2(100, 28);
        retreatButton.Pressed += OnRetreatButtonPressed;
        uiLayer.AddChild(retreatButton);
        
        extractionStatusLabel = new Label();
        extractionStatusLabel.Position = new Vector2(10, 108);
        extractionStatusLabel.AddThemeFontSizeOverride("font_size", 14);
        extractionStatusLabel.Visible = false;
        uiLayer.AddChild(extractionStatusLabel);
    }

    private void SubscribeToEvents()
    {
        combatState.RetreatInitiated += OnRetreatInitiated;
        combatState.RetreatCancelled += OnRetreatCancelled;
    }

    private void OnRetreatButtonPressed()
    {
        if (combatState.IsRetreating)
        {
            combatState.CancelRetreat();
        }
        else
        {
            combatState.InitiateRetreat();
        }
    }

    private void OnRetreatInitiated()
    {
        retreatButton.Text = "Cancel Retreat";
        extractionStatusLabel.Visible = true;
        UpdateExtractionStatus();
        CreateEntryZoneHighlights();
    }

    private void OnRetreatCancelled()
    {
        retreatButton.Text = "Retreat";
        extractionStatusLabel.Visible = false;
        RemoveEntryZoneHighlights();
    }

    public void UpdateExtractionStatus()
    {
        if (!combatState.IsRetreating)
        {
            return;
        }
        
        var (inZone, total) = combatState.GetCrewExtractionStatus();
        extractionStatusLabel.Text = $"Extraction: {inZone}/{total} in zone";
        
        if (inZone == total && total > 0)
        {
            extractionStatusLabel.AddThemeColorOverride("font_color", Colors.Green);
        }
        else
        {
            extractionStatusLabel.AddThemeColorOverride("font_color", Colors.Yellow);
        }
    }

    private void CreateEntryZoneHighlights()
    {
        if (entryZoneHighlightLayer != null)
        {
            return;
        }
        
        entryZoneHighlightLayer = new Node2D();
        entryZoneHighlightLayer.Name = "EntryZoneHighlights";
        entryZoneHighlightLayer.ZIndex = 4;
        gridDisplay.AddChild(entryZoneHighlightLayer);
        
        foreach (var pos in combatState.MapState.EntryZone)
        {
            var highlight = new ColorRect();
            highlight.Size = new Vector2(tileSize - 2, tileSize - 2);
            highlight.Position = new Vector2(pos.X * tileSize + 1, pos.Y * tileSize + 1);
            highlight.Color = new Color(0.2f, 0.9f, 0.3f, 0.35f);
            entryZoneHighlightLayer.AddChild(highlight);
        }
    }

    private void RemoveEntryZoneHighlights()
    {
        if (entryZoneHighlightLayer != null)
        {
            entryZoneHighlightLayer.QueueFree();
            entryZoneHighlightLayer = null;
        }
    }

    public void Cleanup()
    {
        if (combatState != null)
        {
            combatState.RetreatInitiated -= OnRetreatInitiated;
            combatState.RetreatCancelled -= OnRetreatCancelled;
        }
        
        RemoveEntryZoneHighlights();
    }
}
