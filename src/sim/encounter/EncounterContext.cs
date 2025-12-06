using System;
using System.Collections.Generic;
using System.Linq;

namespace FringeTactics;

/// <summary>
/// Snapshot of state for encounter condition evaluation.
/// Decouples conditions from direct CampaignState access.
/// </summary>
public class EncounterContext
{
    // === Resources ===
    public int Money { get; set; }
    public int Fuel { get; set; }
    public int Parts { get; set; }
    public int Ammo { get; set; }
    public int Meds { get; set; }

    // === Crew ===
    public List<CrewSnapshot> Crew { get; set; } = new();

    // === World State ===
    public int CurrentSystemId { get; set; }
    public HashSet<string> SystemTags { get; set; } = new();
    public string SystemOwnerFactionId { get; set; }

    // === Faction Rep ===
    public Dictionary<string, int> FactionRep { get; set; } = new();

    // === Cargo ===
    public int CargoValue { get; set; }
    public bool HasIllegalCargo { get; set; }

    // === Flags ===
    public HashSet<string> Flags { get; set; } = new();

    // === RNG ===
    public RngStream Rng { get; set; }

    // === Query Methods ===

    public int GetResource(string type) => type switch
    {
        ResourceTypes.Money => Money,
        ResourceTypes.Fuel => Fuel,
        ResourceTypes.Parts => Parts,
        ResourceTypes.Ammo => Ammo,
        ResourceTypes.Meds => Meds,
        _ => 0
    };

    public bool HasCrewWithTrait(string traitId)
    {
        if (string.IsNullOrEmpty(traitId)) return false;
        return Crew.Any(c => c.TraitIds?.Contains(traitId) ?? false);
    }

    public int GetFactionRep(string factionId)
    {
        if (string.IsNullOrEmpty(factionId)) return 50;
        return FactionRep.TryGetValue(factionId, out var rep) ? rep : 50;
    }

    public bool HasFlag(string flagId)
    {
        if (string.IsNullOrEmpty(flagId)) return false;
        return Flags?.Contains(flagId) ?? false;
    }

    public int GetBestCrewStat(string statName)
    {
        if (!Enum.TryParse<CrewStatType>(statName, out var stat)) return 0;
        if (Crew.Count == 0) return 0;
        return Crew.Max(c => c.GetStat(stat));
    }

    public CrewSnapshot GetBestCrewForStat(CrewStatType stat)
    {
        if (Crew.Count == 0) return null;
        return Crew.OrderByDescending(c => c.GetStat(stat)).First();
    }

    // === Factory Methods ===

    public static EncounterContext FromCampaign(CampaignState campaign)
    {
        if (campaign == null) return new EncounterContext();

        var context = new EncounterContext
        {
            Money = campaign.Money,
            Fuel = campaign.Fuel,
            Parts = campaign.Parts,
            Ammo = campaign.Ammo,
            Meds = campaign.Meds,
            CurrentSystemId = campaign.CurrentNodeId,
            CargoValue = campaign.Inventory?.GetTotalValue() ?? 0,
            FactionRep = new Dictionary<string, int>(campaign.FactionRep ?? new()),
            Rng = campaign.Rng?.Campaign
        };

        var system = campaign.GetCurrentSystem();
        if (system != null)
        {
            context.SystemTags = new HashSet<string>(system.Tags ?? new HashSet<string>());
            context.SystemOwnerFactionId = system.OwningFactionId;
        }

        foreach (var crew in campaign.GetAliveCrew() ?? new List<CrewMember>())
        {
            context.Crew.Add(CrewSnapshot.From(crew));
        }

        return context;
    }

    public static EncounterContext FromTravelContext(TravelContext travel, CampaignState campaign)
    {
        var context = FromCampaign(campaign);

        if (travel != null)
        {
            context.CurrentSystemId = travel.CurrentSystemId;
            context.SystemTags = travel.SystemTags ?? new HashSet<string>();
            context.SystemOwnerFactionId = travel.SystemOwnerFactionId;
            context.CargoValue = travel.CargoValue;
            context.HasIllegalCargo = travel.HasIllegalCargo;
        }

        return context;
    }
}
