using System;
using System.Collections.Generic;
using System.Linq;

namespace FringeTactics;

/// <summary>
/// Player power tiers for difficulty matching.
/// </summary>
public enum PowerTier
{
    Rookie,     // 0-30 power
    Competent,  // 31-60 power
    Veteran,    // 61-100 power
    Elite       // 101+ power
}

/// <summary>
/// Bundled context for contract generation.
/// Contains all inputs the generator needs to create appropriate contracts.
/// </summary>
public class GenerationContext
{
    // === Player State ===

    /// <summary>
    /// Number of deployable crew members.
    /// </summary>
    public int CrewCount { get; set; }

    /// <summary>
    /// Calculated player power level.
    /// </summary>
    public int CrewPower { get; set; }

    /// <summary>
    /// Roles available in crew (affects contract suitability).
    /// </summary>
    public List<CrewRole> CrewRoles { get; set; } = new();

    /// <summary>
    /// Current player location (node/system ID).
    /// </summary>
    public int CurrentNodeId { get; set; }

    /// <summary>
    /// Number of completed contracts (experience indicator).
    /// </summary>
    public int CompletedContracts { get; set; }

    /// <summary>
    /// Faction reputation dictionary (factionId -> rep 0-100).
    /// </summary>
    public Dictionary<string, int> FactionRep { get; set; } = new();

    // === Resources ===

    public int Money { get; set; }
    public int Fuel { get; set; }

    // === World State ===

    /// <summary>
    /// Current hub system.
    /// </summary>
    public StarSystem HubSystem { get; set; }

    /// <summary>
    /// Potential target systems (connected or nearby).
    /// </summary>
    public List<StarSystem> NearbySystems { get; set; } = new();

    /// <summary>
    /// All factions in sector (id -> name).
    /// </summary>
    public Dictionary<string, string> Factions { get; set; } = new();

    // === Hub Metrics ===

    /// <summary>
    /// Security level at hub (0-5). Affects contract types offered.
    /// </summary>
    public int HubSecurityLevel { get; set; } = 3;

    /// <summary>
    /// Criminal activity at hub (0-5). Higher = more shady contracts.
    /// </summary>
    public int HubCriminalActivity { get; set; } = 2;

    /// <summary>
    /// Economic activity at hub (0-5). Higher = more delivery/trade contracts.
    /// </summary>
    public int HubEconomicActivity { get; set; } = 3;

    // === RNG ===

    /// <summary>
    /// RNG stream for generation (from campaign RNG).
    /// </summary>
    public RngStream Rng { get; set; }

    // ========================================================================
    // DERIVED PROPERTIES
    // ========================================================================

    /// <summary>
    /// Player power tier based on CrewPower.
    /// </summary>
    public PowerTier PlayerTier => CrewPower switch
    {
        <= 30 => PowerTier.Rookie,
        <= 60 => PowerTier.Competent,
        <= 100 => PowerTier.Veteran,
        _ => PowerTier.Elite
    };

    /// <summary>
    /// Check if player has a crew member with specific role.
    /// </summary>
    public bool HasRole(CrewRole role) => CrewRoles.Contains(role);

    /// <summary>
    /// Get reputation with a faction (50 = neutral).
    /// </summary>
    public int GetReputation(string factionId) =>
        FactionRep.GetValueOrDefault(factionId, 50);

    /// <summary>
    /// Check if player is hostile with faction (rep < 25).
    /// </summary>
    public bool IsHostileWith(string factionId) => GetReputation(factionId) < 25;

    /// <summary>
    /// Check if player is friendly with faction (rep >= 75).
    /// </summary>
    public bool IsFriendlyWith(string factionId) => GetReputation(factionId) >= 75;

    // ========================================================================
    // CREW POWER CALCULATION
    // ========================================================================

    /// <summary>
    /// Calculate player power from crew stats.
    /// Formula: Sum of (Level + Aim + Grit + Reflexes) for deployable crew + experience bonus.
    /// </summary>
    public static int CalculateCrewPower(List<CrewMember> crew, int completedMissions)
    {
        if (crew == null || crew.Count == 0) return 0;

        int crewPower = 0;

        foreach (var member in crew)
        {
            if (!member.CanDeploy()) continue;

            crewPower += member.Level;
            crewPower += member.GetEffectiveStat(CrewStatType.Aim);
            crewPower += member.GetEffectiveStat(CrewStatType.Grit);
            crewPower += member.GetEffectiveStat(CrewStatType.Reflexes);
        }

        // Experience bonus: 2 points per completed mission
        int experienceBonus = completedMissions * 2;

        return crewPower + experienceBonus;
    }

    // ========================================================================
    // FACTORY METHODS
    // ========================================================================

    /// <summary>
    /// Build GenerationContext from current campaign state.
    /// </summary>
    public static GenerationContext FromCampaign(CampaignState campaign)
    {
        if (campaign == null) throw new ArgumentNullException(nameof(campaign));

        var deployableCrew = campaign.GetDeployableCrew();
        var hubSystem = campaign.GetCurrentSystem();
        var nearbySystems = GetNearbySystems(campaign);

        var context = new GenerationContext
        {
            // Player state
            CrewCount = deployableCrew.Count,
            CrewPower = CalculateCrewPower(campaign.Crew, campaign.MissionsCompleted),
            CrewRoles = deployableCrew.Select(c => c.Role).Distinct().ToList(),
            CurrentNodeId = campaign.CurrentNodeId,
            CompletedContracts = campaign.MissionsCompleted,
            FactionRep = new Dictionary<string, int>(campaign.FactionRep),

            // Resources
            Money = campaign.Money,
            Fuel = campaign.Fuel,

            // World state
            HubSystem = hubSystem,
            NearbySystems = nearbySystems,
            Factions = campaign.Sector?.Factions ?? new Dictionary<string, string>(),

            // RNG
            Rng = campaign.Rng?.Campaign
        };

        // Fill hub metrics from WorldState if available
        if (campaign.World != null && hubSystem != null)
        {
            var metrics = campaign.World.GetSystemMetrics(campaign.CurrentNodeId);
            if (metrics != null)
            {
                context.HubSecurityLevel = metrics.SecurityLevel;
                context.HubCriminalActivity = metrics.CriminalActivity;
                context.HubEconomicActivity = metrics.EconomicActivity;
            }
        }

        return context;
    }

    /// <summary>
    /// Get nearby systems that can be contract targets.
    /// For G1 single-hub worlds, the hub itself is a valid target.
    /// </summary>
    private static List<StarSystem> GetNearbySystems(CampaignState campaign)
    {
        var targets = new List<StarSystem>();

        if (campaign.World == null)
        {
            return targets;
        }

        var currentSystem = campaign.World.GetSystem(campaign.CurrentNodeId);
        if (currentSystem == null)
        {
            return targets;
        }

        // Get connected systems (non-station types for variety)
        foreach (var connId in currentSystem.Connections)
        {
            var system = campaign.World.GetSystem(connId);
            if (system != null && system.Type != SystemType.Station)
            {
                targets.Add(system);
            }
        }

        // Get 2-hop systems for more variety
        foreach (var connId in currentSystem.Connections)
        {
            var connSystem = campaign.World.GetSystem(connId);
            if (connSystem == null) continue;

            foreach (var secondHop in connSystem.Connections)
            {
                if (secondHop == campaign.CurrentNodeId) continue;
                if (targets.Any(s => s.Id == secondHop)) continue;

                var system = campaign.World.GetSystem(secondHop);
                if (system != null && system.Type != SystemType.Station)
                {
                    targets.Add(system);
                }
            }
        }

        // For single-hub worlds (G1): include the hub itself as a valid target
        // This allows jobs to be completed at the current location
        if (targets.Count == 0)
        {
            targets.Add(currentSystem);
        }

        return targets;
    }
}
