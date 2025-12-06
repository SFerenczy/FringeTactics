using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// Context passed to Encounter/Generation when travel triggers an encounter.
/// Contains all information needed to select and parameterize an encounter.
/// </summary>
public class TravelContext
{
    // === Location ===

    /// <summary>
    /// Current system ID where encounter occurs.
    /// </summary>
    public int CurrentSystemId { get; set; }

    /// <summary>
    /// Destination system ID of the travel.
    /// </summary>
    public int DestinationSystemId { get; set; }

    /// <summary>
    /// Current route being traveled.
    /// </summary>
    public Route CurrentRoute { get; set; }

    // === System Info ===

    /// <summary>
    /// Current system reference.
    /// </summary>
    public StarSystem CurrentSystem { get; set; }

    /// <summary>
    /// Tags on the current system.
    /// </summary>
    public HashSet<string> SystemTags { get; set; } = new();

    /// <summary>
    /// Metrics for the current system.
    /// </summary>
    public SystemMetrics SystemMetrics { get; set; }

    // === Route Info ===

    /// <summary>
    /// Tags on the current route.
    /// </summary>
    public HashSet<string> RouteTags { get; set; } = new();

    /// <summary>
    /// Hazard level of the current route.
    /// </summary>
    public int RouteHazard { get; set; }

    // === Suggested Encounter ===

    /// <summary>
    /// Suggested encounter type based on route/system context.
    /// Values: "pirate", "patrol", "trader", "smuggler", "anomaly", "distress", "random"
    /// </summary>
    public string SuggestedEncounterType { get; set; }

    // === Player State Summary ===

    /// <summary>
    /// Total value of cargo being carried.
    /// </summary>
    public int CargoValue { get; set; }

    /// <summary>
    /// Whether player has illegal cargo.
    /// </summary>
    public bool HasIllegalCargo { get; set; }

    /// <summary>
    /// Number of crew members.
    /// </summary>
    public int CrewCount { get; set; }

    /// <summary>
    /// Aggregated traits from crew (for skill checks).
    /// </summary>
    public List<string> CrewTraits { get; set; } = new();

    // === Faction Context ===

    /// <summary>
    /// Faction that owns the current system.
    /// </summary>
    public string SystemOwnerFactionId { get; set; }

    /// <summary>
    /// Player's reputation with the owning faction.
    /// </summary>
    public int PlayerRepWithOwner { get; set; }

    /// <summary>
    /// Create context from travel state and campaign.
    /// </summary>
    public static TravelContext Create(TravelState state, CampaignState campaign)
    {
        var segment = state.CurrentSegment;
        var world = campaign.World;
        var system = world?.GetSystem(state.CurrentSystemId);
        var route = segment?.Route;

        var context = new TravelContext
        {
            CurrentSystemId = state.CurrentSystemId,
            DestinationSystemId = state.Plan?.DestinationSystemId ?? 0,
            CurrentRoute = route,
            CurrentSystem = system,
            SystemTags = system?.Tags != null ? new HashSet<string>(system.Tags) : new HashSet<string>(),
            SystemMetrics = system?.Metrics,
            RouteTags = route?.Tags != null ? new HashSet<string>(route.Tags) : new HashSet<string>(),
            RouteHazard = route?.HazardLevel ?? 0,
            SuggestedEncounterType = segment?.SuggestedEncounterType ?? EncounterTypes.Random,
            CargoValue = campaign.Inventory?.GetTotalValue() ?? 0,
            HasIllegalCargo = false, // TODO: Check for illegal items when implemented
            CrewCount = campaign.GetAliveCrew()?.Count ?? 0,
            SystemOwnerFactionId = system?.OwningFactionId,
            PlayerRepWithOwner = campaign.GetFactionRep(system?.OwningFactionId ?? "")
        };

        // Aggregate crew traits
        foreach (var crew in campaign.GetAliveCrew() ?? new List<CrewMember>())
        {
            foreach (var traitId in crew.TraitIds ?? new List<string>())
            {
                if (!context.CrewTraits.Contains(traitId))
                    context.CrewTraits.Add(traitId);
            }
        }

        return context;
    }
}
