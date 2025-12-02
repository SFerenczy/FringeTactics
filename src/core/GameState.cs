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

    // Track crew-to-actor mapping for mission results
    private Dictionary<int, int> actorToCrewMap = new(); // actorId -> crewId
    
    // Track current sandbox mission config for restart
    private MissionConfig currentSandboxConfig = null;

    // Scene paths
    private const string MainMenuScene = "res://src/scenes/menu/MainMenu.tscn";
    private const string MissionScene = "res://src/scenes/mission/MissionView.tscn";
    private const string CampaignScene = "res://src/scenes/campaign/CampaignScreen.tscn";
    private const string SectorScene = "res://src/scenes/sector/SectorView.tscn";
    private const string CampaignOverScene = "res://src/scenes/menu/CampaignOverScreen.tscn";

    public override void _Ready()
    {
        Instance = this;
        SimLog.OnLog += message => GD.Print(message);
    }

    public void StartNewCampaign()
    {
        Campaign = CampaignState.CreateNew();
        Mode = "sector";
        GD.Print($"[GameState] New campaign started at {Campaign.GetCurrentNode()?.Name}");
        GoToSectorView();
    }

    /// <summary>
    /// Travel to a sector node.
    /// </summary>
    public bool TravelTo(int nodeId)
    {
        if (Campaign == null) return false;

        var result = TravelSystem.Travel(Campaign, Campaign.Sector, nodeId);
        if (result == TravelResult.Success)
        {
            GD.Print($"[GameState] Arrived at {Campaign.GetCurrentNode()?.Name}");

            // Refresh available jobs at new location (only if no active job)
            if (Campaign.CurrentJob == null)
            {
                Campaign.RefreshJobsAtCurrentNode();
            }

            return true;
        }
        else if (result == TravelResult.Ambush)
        {
            // Future: trigger combat encounter
            GD.Print("[GameState] Ambush encountered!");
            return true;
        }

        GD.Print($"[GameState] Travel failed: {result}");
        return false;
    }

    public void GoToSectorView()
    {
        Mode = "sector";
        GetTree().ChangeSceneToFile(SectorScene);
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
        actorToCrewMap = buildResult.ActorToCrewMap;

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

    public void EndMission(bool victory, CombatState combatState)
    {
        if (Campaign == null)
        {
            Mode = "menu";
            GoToMainMenu();
            return;
        }

        var result = new MissionResult { Victory = victory };

        foreach (var actor in combatState.Actors)
        {
            if (actor.Type == "enemy" && actor.State == ActorState.Dead)
            {
                result.EnemiesKilled++;
            }
        }

        foreach (var actor in combatState.Actors)
        {
            if (actor.Type != "crew") continue;
            if (!actorToCrewMap.TryGetValue(actor.Id, out var crewId)) continue;

            if (actor.State == ActorState.Dead)
            {
                result.DeadCrewIds.Add(crewId);
            }
            else
            {
                // Surviving crew get XP
                int xp = CampaignState.XP_PARTICIPATION;
                // Bonus XP for kills (divide kills among survivors)
                result.CrewXpGains[crewId] = xp;

                // Injured if took significant damage
                if (actor.Hp < actor.MaxHp * 0.5f)
                {
                    result.InjuredCrewIds.Add(crewId);
                }
            }
        }

        // Distribute kill XP among survivors
        int survivorCount = result.CrewXpGains.Count;
        if (survivorCount > 0 && result.EnemiesKilled > 0)
        {
            int killXpPerSurvivor = (result.EnemiesKilled * CampaignState.XP_PER_KILL) / survivorCount;
            foreach (var crewId in new List<int>(result.CrewXpGains.Keys))
            {
                result.CrewXpGains[crewId] += killXpPerSurvivor;
            }
        }

        Campaign.ApplyMissionResult(result);
        CurrentCombat = null;

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

    public void GoToMainMenu()
    {
        Mode = "menu";
        Campaign = null;
        CurrentCombat = null;
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
}
