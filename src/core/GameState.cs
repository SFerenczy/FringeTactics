using Godot;
using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// Global game state autoload. Manages campaign and scene transitions.
/// </summary>
public partial class GameState : Node
{
    public static GameState Instance { get; private set; }

    public CampaignState Campaign { get; private set; } = null;
    public CombatState CurrentCombat { get; private set; } = null;
    public string Mode { get; private set; } = "menu";
    
    /// <summary>
    /// Central event bus for cross-domain communication.
    /// </summary>
    public EventBus EventBus { get; private set; } = new();

    // Track crew-to-actor mapping for mission results
    private Dictionary<int, int> actorToCrewMap = new(); // actorId -> crewId
    
    // Track current sandbox mission config for restart
    private MissionConfig currentSandboxConfig = null;
    
    // Encounter flow state (EN-UI)
    private TravelState pausedTravelState = null;
    private TravelPlan pausedTravelPlan = null;

    // Scene paths
    private const string MainMenuScene = "res://src/scenes/menu/MainMenu.tscn";
    private const string MissionScene = "res://src/scenes/mission/MissionView.tscn";
    private const string CampaignScene = "res://src/scenes/campaign/CampaignScreen.tscn";
    private const string SectorScene = "res://src/scenes/sector/SectorView.tscn";
    private const string CampaignOverScene = "res://src/scenes/menu/CampaignOverScreen.tscn";
    private const string EncounterScene = "res://src/scenes/encounter/EncounterScreen.tscn";

    public override void _Ready()
    {
        Instance = this;
        SimLog.OnLog += message => GD.Print(message);
        
        // Load game data from JSON files (auto-loads on first access)
        var _ = Definitions.Weapons;
    }

    public void StartNewCampaign()
    {
        EventBus.Clear();
        Campaign = CampaignState.CreateNew();
        WireEventBus(Campaign);
        Mode = "sector";
        GD.Print($"[GameState] New campaign started at {Campaign.GetCurrentSystem()?.Name}");
        GoToSectorView();
    }
    
    /// <summary>
    /// Wire event bus to campaign state and its subsystems.
    /// IMPORTANT: Must be called after loading a campaign from save (SF3).
    /// </summary>
    private void WireEventBus(CampaignState campaign)
    {
        campaign.EventBus = EventBus;
        campaign.Time.EventBus = EventBus;
    }
    
    /// <summary>
    /// Wire event bus to combat state.
    /// </summary>
    private void WireEventBus(CombatState combat)
    {
        combat.EventBus = EventBus;
    }

    /// <summary>
    /// Travel to a system using the TV2 travel execution system.
    /// </summary>
    public bool TravelTo(int systemId)
    {
        if (Campaign == null) return false;

        var planner = new TravelPlanner(Campaign.World);
        var plan = planner.PlanRoute(Campaign.CurrentNodeId, systemId);
        
        if (!plan.IsValid)
        {
            GD.Print($"[GameState] Travel failed: {plan.InvalidReason}");
            return false;
        }
        
        var executor = new TravelExecutor(Campaign.Rng);
        var result = executor.Execute(plan, Campaign);
        
        return HandleTravelResult(result, plan);
    }
    
    /// <summary>
    /// Handle travel result, transitioning to encounter screen if paused.
    /// </summary>
    private bool HandleTravelResult(TravelResult result, TravelPlan plan)
    {
        switch (result.Status)
        {
            case TravelResultStatus.Completed:
                GD.Print($"[GameState] Arrived at {Campaign.World?.GetSystem(Campaign.CurrentNodeId)?.Name}");
                if (Campaign.CurrentJob == null)
                {
                    Campaign.RefreshJobsAtCurrentNode();
                }
                return true;
                
            case TravelResultStatus.PausedForEncounter:
                GD.Print($"[GameState] Travel paused for encounter");
                pausedTravelState = result.PausedState;
                pausedTravelPlan = plan;
                Mode = "encounter";
                GetTree().ChangeSceneToFile(EncounterScene);
                return true;
                
            case TravelResultStatus.Interrupted:
                if (result.InterruptReason == TravelInterruptReason.InsufficientFuel)
                {
                    GD.Print("[GameState] Travel failed: insufficient fuel");
                }
                else
                {
                    GD.Print($"[GameState] Travel interrupted: {result.InterruptReason}");
                }
                return false;
                
            default:
                GD.Print($"[GameState] Travel failed: {result.Status}");
                return false;
        }
    }

    public void GoToSectorView()
    {
        Mode = "sector";
        GetTree().ChangeSceneToFile(SectorScene);
    }
    
    /// <summary>
    /// Set paused travel state (called by SectorView after animation).
    /// </summary>
    public void SetPausedTravel(TravelState state, TravelPlan plan)
    {
        pausedTravelState = state;
        pausedTravelPlan = plan;
    }

    /// <summary>
    /// Transition to encounter screen.
    /// </summary>
    public void GoToEncounter()
    {
        Mode = "encounter";
        GetTree().ChangeSceneToFile(EncounterScene);
    }

    /// <summary>
    /// Called when encounter completes. Applies effects and resumes travel.
    /// </summary>
    public void ResolveEncounter(string outcome = "completed")
    {
        if (Campaign?.ActiveEncounter == null)
        {
            GD.PrintErr("[GameState] No active encounter to resolve");
            GoToSectorView();
            return;
        }
        
        // Apply accumulated effects
        int effectsApplied = Campaign.ApplyEncounterOutcome(Campaign.ActiveEncounter);
        GD.Print($"[GameState] Applied {effectsApplied} encounter effects");
        
        // Clear active encounter
        Campaign.ActiveEncounter = null;
        
        // Resume travel if we have paused state
        if (pausedTravelState != null && pausedTravelPlan != null)
        {
            var executor = new TravelExecutor(Campaign.Rng);
            var result = executor.Resume(pausedTravelState, Campaign, outcome);
            
            var plan = pausedTravelPlan;
            pausedTravelState = null;
            pausedTravelPlan = null;
            
            // Handle resumed travel result (may pause again for another encounter)
            if (!HandleTravelResult(result, plan))
            {
                GoToSectorView();
            }
            else if (result.Status == TravelResultStatus.Completed)
            {
                GoToSectorView();
            }
        }
        else
        {
            GoToSectorView();
        }
    }

    public void StartMission()
    {
        if (Campaign == null)
        {
            GD.PrintErr("[GameState] Cannot start mission without campaign!");
            return;
        }

        if (!Campaign.CanStartMission())
        {
            GD.Print($"[GameState] Cannot start mission: {Campaign.GetMissionBlockReason()}");
            return;
        }

        // Must have an active job to start mission
        if (Campaign.CurrentJob == null)
        {
            GD.PrintErr("[GameState] Cannot start mission without an active job!");
            return;
        }

        // Must be at job target
        if (!Campaign.IsAtJobTarget())
        {
            GD.PrintErr("[GameState] Must be at job target to start mission!");
            return;
        }

        // Consume resources
        Campaign.ConsumeMissionResources();

        // Build combat state from campaign using job's mission config
        var config = Campaign.CurrentJob.MissionConfig ?? MissionConfig.CreateTestMission();
        var buildResult = MissionFactory.BuildFromCampaign(Campaign, config);
        CurrentCombat = buildResult.CombatState;
        WireEventBus(CurrentCombat);
        actorToCrewMap = buildResult.ActorToCrewMap;

        // Publish MissionStartedEvent (MG3)
        int crewCount = 0;
        int enemyCount = 0;
        foreach (var actor in CurrentCombat.Actors)
        {
            if (actor.Type == ActorType.Crew) crewCount++;
            else if (actor.Type == ActorType.Enemy) enemyCount++;
        }
        EventBus.Publish(new MissionStartedEvent(
            Campaign.CurrentJob.Id,
            Campaign.CurrentJob.Title,
            crewCount,
            enemyCount
        ));

        GD.Print($"[GameState] Starting mission: {Campaign.CurrentJob.Title}");
        Mode = "mission";
        GetTree().ChangeSceneToFile(MissionScene);
    }

    /// <summary>
    /// Start a sandbox mission (no campaign).
    /// </summary>
    public void StartSandboxMission()
    {
        var config = MissionConfig.CreateTestMission();
        StartSandboxWithConfig(config);
    }
    
    /// <summary>
    /// Start a sandbox mission with a specific config.
    /// </summary>
    private void StartSandboxWithConfig(MissionConfig config)
    {
        currentSandboxConfig = config;
        CurrentCombat = MissionFactory.BuildSandbox(config);
        WireEventBus(CurrentCombat);
        actorToCrewMap.Clear();

        Mode = "mission";
        GetTree().ChangeSceneToFile(MissionScene);
    }
    
    /// <summary>
    /// Restart the current sandbox mission with the same config.
    /// </summary>
    public void RestartCurrentMission()
    {
        if (currentSandboxConfig != null)
        {
            StartSandboxWithConfig(currentSandboxConfig);
        }
        else
        {
            StartSandboxMission();
        }
    }
    
    /// <summary>
    /// Start the M0 test mission (single unit, no enemies).
    /// </summary>
    public void StartM0TestMission()
    {
        var config = MissionConfig.CreateM0TestMission();
        StartSandboxWithConfig(config);
    }

    /// <summary>
    /// Start the M1 test mission (6 units, no enemies) for testing selection and group movement.
    /// </summary>
    public void StartM1TestMission()
    {
        var config = MissionConfig.CreateM1TestMission();
        StartSandboxWithConfig(config);
    }

    /// <summary>
    /// Start the M2 test mission for testing visibility and fog of war.
    /// </summary>
    public void StartM2TestMission()
    {
        var config = MissionConfig.CreateM2TestMission();
        StartSandboxWithConfig(config);
    }

    /// <summary>
    /// Start the M3 test mission for testing basic combat (hit chance, ammo, auto-defend).
    /// </summary>
    public void StartM3TestMission()
    {
        var config = MissionConfig.CreateM3TestMission();
        StartSandboxWithConfig(config);
    }

    /// <summary>
    /// Start the M4 test mission for testing directional cover mechanics.
    /// </summary>
    public void StartM4TestMission()
    {
        var config = MissionConfig.CreateM4TestMission();
        StartSandboxWithConfig(config);
    }

    /// <summary>
    /// Start the M4.1 test mission for testing cover height mechanics.
    /// </summary>
    public void StartM4_1TestMission()
    {
        var config = MissionConfig.CreateM4_1TestMission();
        StartSandboxWithConfig(config);
    }

    /// <summary>
    /// Start the M5 test mission for testing interactables and channeled hacking.
    /// </summary>
    public void StartM5TestMission()
    {
        var config = MissionConfig.CreateM5TestMission();
        StartSandboxWithConfig(config);
    }

    /// <summary>
    /// Start the M6 test mission for testing stealth and alarm mechanics.
    /// </summary>
    public void StartM6TestMission()
    {
        var config = MissionConfig.CreateM6TestMission();
        StartSandboxWithConfig(config);
    }

    /// <summary>
    /// Start the M7 test mission for testing session I/O and retreat.
    /// </summary>
    public void StartM7TestMission()
    {
        var config = MissionConfig.CreateM7TestMission();
        StartSandboxWithConfig(config);
    }

    /// <summary>
    /// End mission with boolean victory flag (legacy compatibility).
    /// </summary>
    public void EndMission(bool victory, CombatState combatState)
    {
        var outcome = combatState.FinalOutcome ?? (victory ? MissionOutcome.Victory : MissionOutcome.Defeat);
        EndMission(outcome, combatState);
    }
    
    /// <summary>
    /// End mission with detailed outcome (M7).
    /// </summary>
    public void EndMission(MissionOutcome outcome, CombatState combatState)
    {
        if (Campaign == null)
        {
            Mode = "menu";
            GoToMainMenu();
            return;
        }

        // Build formal mission output using MissionOutputBuilder (M7)
        var output = MissionOutputBuilder.Build(combatState, outcome, actorToCrewMap);
        
        // Log mission summary
        LogMissionSummary(output);
        
        // Calculate crew survived/lost for event (MG3)
        int crewSurvived = 0;
        int crewLost = 0;
        foreach (var crew in output.CrewOutcomes)
        {
            if (crew.Status == CrewFinalStatus.Dead || crew.Status == CrewFinalStatus.MIA)
                crewLost++;
            else
                crewSurvived++;
        }
        
        // Publish MissionEndedEvent (MG3)
        EventBus.Publish(new MissionEndedEvent(
            output.MissionId ?? "unknown",
            outcome,
            crewSurvived,
            crewLost,
            output.EnemiesKilled
        ));
        
        // Apply mission output directly to campaign
        Campaign.ApplyMissionOutput(output);
        CurrentCombat = null;
        actorToCrewMap.Clear();

        // Remove dead crew from roster
        int buried = Campaign.BuryAllDeadCrew();
        if (buried > 0)
        {
            GD.Print($"[GameState] Buried {buried} fallen crew member(s)");
        }

        // Check for campaign over (all crew dead)
        if (Campaign.IsCampaignOver())
        {
            GD.Print("[GameState] Campaign over - all crew lost!");
            Mode = "gameover";
            GetTree().ChangeSceneToFile(CampaignOverScene);
            return;
        }

        Mode = "sector";
        GoToSectorView();
    }
    
    
    /// <summary>
    /// Log detailed mission summary to console.
    /// </summary>
    private void LogMissionSummary(MissionOutput output)
    {
        GD.Print($"\n[GameState] === MISSION COMPLETE ===");
        GD.Print($"  Outcome: {output.Outcome}");
        GD.Print($"  Duration: {output.MissionDurationSeconds:F1}s ({output.TicksElapsed} ticks)");
        GD.Print($"  Enemies: {output.EnemiesKilled} killed, {output.EnemiesRemaining} remaining");
        GD.Print($"  Alarm: {(output.AlarmTriggered ? "Triggered" : "Quiet")}");
        
        GD.Print($"  --- Crew Results ---");
        foreach (var crew in output.CrewOutcomes)
        {
            var accuracy = crew.ShotsFired > 0 ? (float)crew.ShotsHit / crew.ShotsFired * 100 : 0;
            GD.Print($"    {crew.Name}: {crew.Status}");
            GD.Print($"      HP: {crew.FinalHp}/{crew.MaxHp}, Kills: {crew.Kills}, Shots: {crew.ShotsFired} ({accuracy:F0}% acc)");
            GD.Print($"      Ammo: {crew.AmmoRemaining} remaining ({crew.AmmoUsed} used), XP: +{crew.SuggestedXp}");
        }
        GD.Print($"==============================\n");
    }

    public void GoToMainMenu()
    {
        Mode = "menu";
        Campaign = null;
        CurrentCombat = null;
        actorToCrewMap.Clear();
        EventBus.Clear();
        GetTree().ChangeSceneToFile(MainMenuScene);
    }

    public void GoToCampaignScreen()
    {
        Mode = "campaign";
        GetTree().ChangeSceneToFile(CampaignScene);
    }

    /// <summary>
    /// Register actor-to-crew mapping when spawning crew in mission.
    /// </summary>
    public void RegisterActorCrew(int actorId, int crewId)
    {
        actorToCrewMap[actorId] = crewId;
    }

    public bool HasActiveCampaign()
    {
        return Campaign != null;
    }

    // --- Save/Load Methods ---

    /// <summary>
    /// Save current campaign to a slot.
    /// </summary>
    public bool SaveGame(int slot)
    {
        if (Campaign == null)
        {
            GD.PrintErr("[GameState] No campaign to save");
            return false;
        }

        return SaveFileAdapter.Save(Campaign, slot);
    }

    /// <summary>
    /// Autosave current campaign.
    /// </summary>
    public bool Autosave()
    {
        if (Campaign == null) return false;
        return SaveFileAdapter.Autosave(Campaign);
    }

    /// <summary>
    /// Load campaign from a slot.
    /// </summary>
    public bool LoadGame(int slot)
    {
        var campaign = SaveFileAdapter.Load(slot);
        if (campaign == null) return false;

        EventBus.Clear();
        Campaign = campaign;
        WireEventBus(Campaign);
        Mode = "sector";

        GD.Print($"[GameState] Loaded campaign from slot {slot}");
        GoToSectorView();
        return true;
    }

    /// <summary>
    /// Load campaign from autosave.
    /// </summary>
    public bool LoadAutosave()
    {
        var campaign = SaveFileAdapter.LoadAutosave();
        if (campaign == null) return false;

        EventBus.Clear();
        Campaign = campaign;
        WireEventBus(Campaign);
        Mode = "sector";

        GD.Print("[GameState] Loaded campaign from autosave");
        GoToSectorView();
        return true;
    }

    /// <summary>
    /// Check if a save slot exists.
    /// </summary>
    public bool HasSave(int slot) => SaveFileAdapter.SaveExists(slot);

    /// <summary>
    /// Check if autosave exists.
    /// </summary>
    public bool HasAutosave() => SaveFileAdapter.AutosaveExists();

    // --- Time Query Accessors ---

    /// <summary>
    /// Get current campaign day (0 if no campaign).
    /// </summary>
    public int GetCampaignDay()
    {
        return Campaign?.Time?.CurrentDay ?? 0;
    }

    /// <summary>
    /// Get current tactical tick (0 if no mission).
    /// </summary>
    public int GetTacticalTick()
    {
        return CurrentCombat?.TimeSystem?.CurrentTick ?? 0;
    }

    /// <summary>
    /// Get formatted campaign day string (empty if no campaign).
    /// </summary>
    public string GetCampaignDayFormatted()
    {
        return Campaign?.Time?.FormatCurrentDay() ?? "";
    }

    /// <summary>
    /// Get formatted tactical time string (empty if no mission).
    /// </summary>
    public string GetTacticalTimeFormatted()
    {
        if (CurrentCombat == null) return "";
        float seconds = CurrentCombat.TimeSystem.GetCurrentTime();
        int minutes = (int)(seconds / 60);
        int secs = (int)(seconds % 60);
        return $"{minutes}:{secs:D2}";
    }
}
