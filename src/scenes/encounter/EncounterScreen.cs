using Godot;
using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// Encounter screen controller. Displays encounter narrative and options,
/// handles player choices, and shows results.
/// </summary>
public partial class EncounterScreen : Control
{
    // UI elements
    private Label headerLabel;
    private RichTextLabel narrativeLabel;
    private VBoxContainer optionsContainer;
    private Panel resultPanel;
    private RichTextLabel resultLabel;
    private Panel effectsPanel;
    private Label effectsLabel;
    private Button continueButton;
    
    // State
    private EncounterRunner runner;
    private EncounterInstance instance;
    private EncounterContext context;
    private bool awaitingContinue = false;
    private SkillCheckResolvedEvent? lastSkillCheck = null;
    
    public override void _Ready()
    {
        SetAnchorsPreset(LayoutPreset.FullRect);
        
        CreateUI();
        SubscribeToEvents();
        InitializeEncounter();
    }
    
    public override void _ExitTree()
    {
        GameState.Instance?.EventBus?.Unsubscribe<SkillCheckResolvedEvent>(OnSkillCheckResolved);
    }
    
    private void SubscribeToEvents()
    {
        GameState.Instance?.EventBus?.Subscribe<SkillCheckResolvedEvent>(OnSkillCheckResolved);
    }
    
    private void OnSkillCheckResolved(SkillCheckResolvedEvent evt)
    {
        lastSkillCheck = evt;
    }
    
    private void CreateUI()
    {
        // Dark background
        var background = new Panel();
        background.SetAnchorsPreset(LayoutPreset.FullRect);
        var bgStyle = new StyleBoxFlat();
        bgStyle.BgColor = new Color(0.05f, 0.05f, 0.1f);
        background.AddThemeStyleboxOverride("panel", bgStyle);
        AddChild(background);
        
        // Main content container - centered
        var centerContainer = new CenterContainer();
        centerContainer.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(centerContainer);
        
        var contentBox = new VBoxContainer();
        contentBox.CustomMinimumSize = new Vector2(700, 0);
        centerContainer.AddChild(contentBox);
        
        // Header
        headerLabel = new Label();
        headerLabel.Text = "ENCOUNTER";
        headerLabel.AddThemeFontSizeOverride("font_size", 28);
        headerLabel.AddThemeColorOverride("font_color", Colors.Cyan);
        headerLabel.HorizontalAlignment = HorizontalAlignment.Center;
        contentBox.AddChild(headerLabel);
        
        AddSpacer(contentBox, 20);
        
        // Narrative panel
        var narrativePanel = new Panel();
        narrativePanel.CustomMinimumSize = new Vector2(0, 150);
        var narrativeStyle = new StyleBoxFlat();
        narrativeStyle.BgColor = new Color(0.1f, 0.1f, 0.15f);
        narrativeStyle.BorderWidthTop = 2;
        narrativeStyle.BorderWidthBottom = 2;
        narrativeStyle.BorderWidthLeft = 2;
        narrativeStyle.BorderWidthRight = 2;
        narrativeStyle.BorderColor = Colors.Cyan;
        narrativeStyle.ContentMarginLeft = 20;
        narrativeStyle.ContentMarginRight = 20;
        narrativeStyle.ContentMarginTop = 15;
        narrativeStyle.ContentMarginBottom = 15;
        narrativePanel.AddThemeStyleboxOverride("panel", narrativeStyle);
        contentBox.AddChild(narrativePanel);
        
        narrativeLabel = new RichTextLabel();
        narrativeLabel.SetAnchorsPreset(LayoutPreset.FullRect);
        narrativeLabel.OffsetLeft = 20;
        narrativeLabel.OffsetRight = -20;
        narrativeLabel.OffsetTop = 15;
        narrativeLabel.OffsetBottom = -15;
        narrativeLabel.BbcodeEnabled = true;
        narrativeLabel.AddThemeFontSizeOverride("normal_font_size", 16);
        narrativePanel.AddChild(narrativeLabel);
        
        AddSpacer(contentBox, 20);
        
        // Options container
        optionsContainer = new VBoxContainer();
        optionsContainer.AddThemeConstantOverride("separation", 10);
        contentBox.AddChild(optionsContainer);
        
        AddSpacer(contentBox, 20);
        
        // Result panel (hidden initially)
        resultPanel = new Panel();
        resultPanel.CustomMinimumSize = new Vector2(0, 120);
        resultPanel.Visible = false;
        var resultStyle = new StyleBoxFlat();
        resultStyle.BgColor = new Color(0.1f, 0.12f, 0.1f);
        resultStyle.BorderWidthTop = 2;
        resultStyle.BorderWidthBottom = 2;
        resultStyle.BorderWidthLeft = 2;
        resultStyle.BorderWidthRight = 2;
        resultStyle.BorderColor = Colors.Green;
        resultStyle.ContentMarginLeft = 20;
        resultStyle.ContentMarginRight = 20;
        resultStyle.ContentMarginTop = 15;
        resultStyle.ContentMarginBottom = 15;
        resultPanel.AddThemeStyleboxOverride("panel", resultStyle);
        contentBox.AddChild(resultPanel);
        
        resultLabel = new RichTextLabel();
        resultLabel.SetAnchorsPreset(LayoutPreset.FullRect);
        resultLabel.OffsetLeft = 20;
        resultLabel.OffsetRight = -20;
        resultLabel.OffsetTop = 15;
        resultLabel.OffsetBottom = -15;
        resultLabel.BbcodeEnabled = true;
        resultLabel.AddThemeFontSizeOverride("normal_font_size", 14);
        resultPanel.AddChild(resultLabel);
        
        AddSpacer(contentBox, 15);
        
        // Effects panel (hidden initially)
        effectsPanel = new Panel();
        effectsPanel.CustomMinimumSize = new Vector2(0, 60);
        effectsPanel.Visible = false;
        var effectsStyle = new StyleBoxFlat();
        effectsStyle.BgColor = new Color(0.15f, 0.1f, 0.1f);
        effectsStyle.BorderColor = Colors.Orange;
        effectsStyle.BorderWidthTop = 1;
        effectsStyle.BorderWidthBottom = 1;
        effectsStyle.BorderWidthLeft = 1;
        effectsStyle.BorderWidthRight = 1;
        effectsStyle.ContentMarginLeft = 15;
        effectsStyle.ContentMarginRight = 15;
        effectsStyle.ContentMarginTop = 10;
        effectsStyle.ContentMarginBottom = 10;
        effectsPanel.AddThemeStyleboxOverride("panel", effectsStyle);
        contentBox.AddChild(effectsPanel);
        
        effectsLabel = new Label();
        effectsLabel.SetAnchorsPreset(LayoutPreset.FullRect);
        effectsLabel.OffsetLeft = 15;
        effectsLabel.OffsetRight = -15;
        effectsLabel.OffsetTop = 10;
        effectsLabel.OffsetBottom = -10;
        effectsLabel.AddThemeFontSizeOverride("font_size", 13);
        effectsLabel.AddThemeColorOverride("font_color", Colors.Orange);
        effectsPanel.AddChild(effectsLabel);
        
        AddSpacer(contentBox, 20);
        
        // Continue button (hidden initially)
        continueButton = new Button();
        continueButton.Text = "Continue";
        continueButton.CustomMinimumSize = new Vector2(200, 50);
        continueButton.Visible = false;
        continueButton.Pressed += OnContinuePressed;
        
        var buttonContainer = new CenterContainer();
        buttonContainer.AddChild(continueButton);
        contentBox.AddChild(buttonContainer);
    }
    
    private void AddSpacer(VBoxContainer parent, int height)
    {
        var spacer = new Control();
        spacer.CustomMinimumSize = new Vector2(0, height);
        parent.AddChild(spacer);
    }
    
    private void InitializeEncounter()
    {
        var campaign = GameState.Instance?.Campaign;
        if (campaign?.ActiveEncounter == null)
        {
            GD.PrintErr("[EncounterScreen] No active encounter!");
            GameState.Instance?.GoToSectorView();
            return;
        }
        
        instance = campaign.ActiveEncounter;
        runner = new EncounterRunner(GameState.Instance.EventBus);
        context = EncounterContext.FromCampaign(campaign);
        
        // Start the encounter
        runner.Start(instance);
        
        // Display initial state
        UpdateDisplay();
    }
    
    private void UpdateDisplay()
    {
        if (instance == null) return;
        
        var node = runner.GetCurrentNode(instance);
        if (node == null)
        {
            ShowCompletion();
            return;
        }
        
        // Update header
        headerLabel.Text = instance.Template?.Name ?? "ENCOUNTER";
        
        // Update narrative
        narrativeLabel.Text = ResolveText(node.TextKey);
        
        // Update options
        UpdateOptions(node);
        
        // Update effects display
        UpdateEffectsDisplay();
        
        // Hide result panel until action taken
        resultPanel.Visible = false;
        
        // Continue button state
        continueButton.Visible = awaitingContinue || instance.IsComplete;
        continueButton.Text = instance.IsComplete ? "Complete" : "Continue";
    }
    
    private void UpdateOptions(EncounterNode node)
    {
        // Clear existing options
        foreach (var child in optionsContainer.GetChildren())
        {
            child.QueueFree();
        }
        
        if (awaitingContinue || node == null) return;
        
        var options = runner.GetAvailableOptions(instance, context);
        
        for (int i = 0; i < options.Count; i++)
        {
            var option = options[i];
            var optionIndex = i;
            
            var btn = CreateOptionButton(option, optionIndex);
            optionsContainer.AddChild(btn);
        }
    }
    
    private Button CreateOptionButton(EncounterOption option, int index)
    {
        var btn = new Button();
        btn.CustomMinimumSize = new Vector2(600, 50);
        btn.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        
        // Build button text
        string text = ResolveText(option.TextKey);
        
        // Add skill check info if present
        if (option.HasSkillCheck)
        {
            var check = option.SkillCheck;
            var chance = SkillCheck.GetSuccessChance(check, context);
            text += $"  [{check.Stat}: {chance}%]";
        }
        
        btn.Text = text;
        btn.Pressed += () => OnOptionSelected(index);
        
        // Style based on skill check presence
        if (option.HasSkillCheck)
        {
            btn.AddThemeColorOverride("font_color", Colors.Yellow);
        }
        
        return btn;
    }
    
    private void OnOptionSelected(int index)
    {
        var result = runner.SelectOption(instance, context, index);
        
        if (!result.IsSuccess)
        {
            GD.PrintErr($"[EncounterScreen] Invalid option: {result.ErrorMessage}");
            return;
        }
        
        // Show result
        ShowResult(result);
    }
    
    private void ShowResult(EncounterStepResult result)
    {
        resultPanel.Visible = true;
        awaitingContinue = true;
        
        var resultText = "";
        
        // Show skill check result if we have one
        if (lastSkillCheck.HasValue)
        {
            var check = lastSkillCheck.Value;
            var successText = check.Success 
                ? "[color=green]SUCCESS[/color]" 
                : "[color=red]FAILURE[/color]";
            
            resultText = $"{check.CrewName} attempts {check.StatName}...\n";
            resultText += $"Roll: {check.Roll} + Stat: {check.StatValue}";
            if (check.TraitBonus != 0)
            {
                resultText += $" + Traits: {check.TraitBonus}";
            }
            resultText += $" = {check.Total} vs DC {check.Difficulty}\n";
            resultText += successText;
            
            if (check.IsCriticalSuccess)
                resultText += " (Critical!)";
            else if (check.IsCriticalFailure)
                resultText += " (Critical Failure!)";
            
            resultText += "\n\n";
            lastSkillCheck = null;
        }
        
        // Add narrative continuation
        if (!instance.IsComplete)
        {
            var nextNode = runner.GetCurrentNode(instance);
            if (nextNode != null)
            {
                resultText += ResolveText(nextNode.TextKey);
            }
        }
        else
        {
            resultText += "The encounter concludes.";
        }
        
        resultLabel.Text = resultText;
        
        // Update effects display
        UpdateEffectsDisplay();
        
        // Hide options, show continue
        UpdateOptions(null);
        continueButton.Visible = true;
        continueButton.Text = instance.IsComplete ? "Complete" : "Continue";
    }
    
    private void ShowCompletion()
    {
        headerLabel.Text = "ENCOUNTER COMPLETE";
        narrativeLabel.Text = "The encounter has concluded.";
        
        // Clear options
        foreach (var child in optionsContainer.GetChildren())
        {
            child.QueueFree();
        }
        
        // Show final effects
        UpdateEffectsDisplay();
        
        // Show complete button
        continueButton.Visible = true;
        continueButton.Text = "Complete";
    }
    
    private void UpdateEffectsDisplay()
    {
        var effects = runner.GetPendingEffects(instance);
        
        if (effects.Count == 0)
        {
            effectsPanel.Visible = false;
            return;
        }
        
        effectsPanel.Visible = true;
        
        var lines = new List<string>();
        foreach (var effect in effects)
        {
            var formatted = FormatEffect(effect);
            if (!string.IsNullOrEmpty(formatted))
            {
                lines.Add(formatted);
            }
        }
        
        effectsLabel.Text = lines.Count > 0 
            ? "Effects:\n" + string.Join("\n", lines)
            : "";
    }
    
    private string FormatEffect(EncounterEffect effect)
    {
        return effect.Type switch
        {
            EffectType.AddResource => $"  {(effect.Amount >= 0 ? "+" : "")}{effect.Amount} {effect.TargetId}",
            EffectType.CrewInjury => $"  Crew injured: {effect.StringParam ?? "wounded"}",
            EffectType.CrewXp => $"  +{effect.Amount} XP",
            EffectType.ShipDamage => $"  Ship damage: {effect.Amount}",
            EffectType.FactionRep => $"  {effect.TargetId} rep: {(effect.Amount >= 0 ? "+" : "")}{effect.Amount}",
            EffectType.TimeDelay => $"  Time: +{effect.Amount} day(s)",
            EffectType.SetFlag => $"  Flag set: {effect.TargetId}",
            EffectType.AddCargo => $"  +{effect.Amount} {effect.TargetId}",
            EffectType.RemoveCargo => $"  -{effect.Amount} {effect.TargetId}",
            EffectType.GotoNode => null,
            EffectType.EndEncounter => null,
            _ => $"  {effect.Type}"
        };
    }
    
    private void OnContinuePressed()
    {
        if (instance.IsComplete)
        {
            GameState.Instance.ResolveEncounter();
        }
        else
        {
            awaitingContinue = false;
            UpdateDisplay();
        }
    }
    
    private string ResolveText(string textKey)
    {
        if (string.IsNullOrEmpty(textKey)) return "";
        
        // Look up localized text, falling back to key if not found
        string text = Localization.Get(textKey);
        
        // Replace parameter placeholders from encounter instance
        if (instance?.ResolvedParameters != null)
        {
            foreach (var (key, value) in instance.ResolvedParameters)
            {
                text = text.Replace($"{{{key}}}", value);
            }
        }
        
        return text;
    }
}
