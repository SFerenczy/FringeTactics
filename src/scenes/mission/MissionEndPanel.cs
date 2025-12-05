using Godot;
using System;

namespace FringeTactics;

/// <summary>
/// Displays mission end results (Victory/Defeat/Retreat) with summary statistics.
/// Shows crew status and combat accuracy.
/// </summary>
public partial class MissionEndPanel : Panel
{
    private Label resultLabel;
    private Label summaryLabel;
    private Button actionButton;
    
    private bool missionVictory;
    private CombatState combatState;

    public event Action RestartRequested;
    public event Action ContinueRequested;

    public override void _Ready()
    {
        CreateUI();
        Visible = false;
    }

    private void CreateUI()
    {
        Position = new Vector2(120, 80);
        Size = new Vector2(280, 220);

        var vbox = new VBoxContainer();
        vbox.Position = new Vector2(20, 15);
        vbox.Size = new Vector2(240, 190);
        AddChild(vbox);

        resultLabel = new Label();
        resultLabel.AddThemeFontSizeOverride("font_size", 32);
        resultLabel.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(resultLabel);

        var spacer = new Control();
        spacer.CustomMinimumSize = new Vector2(0, 10);
        vbox.AddChild(spacer);

        summaryLabel = new Label();
        summaryLabel.AddThemeFontSizeOverride("font_size", 14);
        vbox.AddChild(summaryLabel);

        var spacer2 = new Control();
        spacer2.CustomMinimumSize = new Vector2(0, 15);
        vbox.AddChild(spacer2);

        actionButton = new Button();
        actionButton.Pressed += OnActionButtonPressed;
        vbox.AddChild(actionButton);
    }

    public void Show(CombatState combatState, bool victory)
    {
        this.combatState = combatState;
        this.missionVictory = victory;
        
        UpdateResultDisplay();
        UpdateSummaryDisplay();
        UpdateButtonText();
        PrintSummaryToConsole();
        
        Visible = true;
    }

    private void UpdateResultDisplay()
    {
        var outcome = combatState.FinalOutcome ?? (missionVictory ? MissionOutcome.Victory : MissionOutcome.Defeat);
        
        switch (outcome)
        {
            case MissionOutcome.Victory:
                resultLabel.Text = "VICTORY!";
                resultLabel.AddThemeColorOverride("font_color", Colors.Green);
                break;
            case MissionOutcome.Retreat:
                resultLabel.Text = "RETREATED";
                resultLabel.AddThemeColorOverride("font_color", Colors.Yellow);
                break;
            case MissionOutcome.Defeat:
            default:
                resultLabel.Text = "DEFEAT!";
                resultLabel.AddThemeColorOverride("font_color", Colors.Red);
                break;
        }
    }

    private void UpdateSummaryDisplay()
    {
        var stats = combatState.Stats;
        var crewAlive = 0;
        var crewDead = 0;
        
        foreach (var actor in combatState.Actors)
        {
            if (actor.Type == ActorType.Crew)
            {
                if (actor.State == ActorState.Alive)
                {
                    crewAlive++;
                }
                else
                {
                    crewDead++;
                }
            }
        }

        var summary = $"--- CREW ---\n";
        summary += $"Alive: {crewAlive}  Dead: {crewDead}\n\n";
        summary += $"--- COMBAT ---\n";
        summary += $"Shots: {stats.PlayerShotsFired}  Hits: {stats.PlayerHits}  Misses: {stats.PlayerMisses}\n";
        summary += $"Accuracy: {stats.PlayerAccuracy:F0}%";

        summaryLabel.Text = summary;
    }

    private void UpdateButtonText()
    {
        var hasCampaign = GameState.Instance?.HasActiveCampaign() ?? false;
        actionButton.Text = hasCampaign ? "Continue" : "Restart Test Mission";
    }

    private void PrintSummaryToConsole()
    {
        var outcome = combatState.FinalOutcome ?? (missionVictory ? MissionOutcome.Victory : MissionOutcome.Defeat);
        var stats = combatState.Stats;
        
        var crewAlive = 0;
        var crewDead = 0;
        foreach (var actor in combatState.Actors)
        {
            if (actor.Type == ActorType.Crew)
            {
                if (actor.State == ActorState.Alive) crewAlive++;
                else crewDead++;
            }
        }

        GD.Print("\n=== MISSION SUMMARY ===");
        GD.Print($"Result: {outcome}");
        GD.Print($"Crew Alive: {crewAlive}, Dead: {crewDead}");
        GD.Print($"Player Shots: {stats.PlayerShotsFired}, Hits: {stats.PlayerHits}, Misses: {stats.PlayerMisses}, Accuracy: {stats.PlayerAccuracy:F1}%");
        GD.Print($"Enemy Shots: {stats.EnemyShotsFired}, Hits: {stats.EnemyHits}, Misses: {stats.EnemyMisses}, Accuracy: {stats.EnemyAccuracy:F1}%");
        GD.Print("========================\n");
    }

    private void OnActionButtonPressed()
    {
        var hasCampaign = GameState.Instance?.HasActiveCampaign() ?? false;

        if (hasCampaign)
        {
            ContinueRequested?.Invoke();
        }
        else
        {
            RestartRequested?.Invoke();
        }
    }
}
