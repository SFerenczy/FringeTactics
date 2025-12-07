using System;
using System.Collections.Generic;
using System.Linq;

namespace FringeTactics;

/// <summary>
/// Generates encounter instances from templates based on travel context.
/// Handles template selection with weighted probabilities and parameter resolution.
/// </summary>
public class EncounterGenerator
{
    private readonly EncounterTemplateRegistry registry;
    private readonly EncounterWeightConfig weightConfig;

    public EncounterGenerator(EncounterTemplateRegistry registry, EncounterWeightConfig weightConfig = null)
    {
        this.registry = registry ?? throw new ArgumentNullException(nameof(registry));
        this.weightConfig = weightConfig ?? EncounterWeightConfig.Default;
    }

    /// <summary>
    /// Generate an encounter for the given travel context.
    /// Returns null if no eligible templates found or RNG unavailable.
    /// </summary>
    public EncounterInstance Generate(TravelContext context, CampaignState campaign)
    {
        if (context == null)
        {
            SimLog.Log("[EncounterGenerator] Null context provided");
            return null;
        }

        var rng = campaign?.Rng?.Campaign;
        if (rng == null)
        {
            SimLog.Log("[EncounterGenerator] No RNG available");
            return null;
        }

        // 1. Get eligible templates
        var eligible = registry.GetEligible(context).ToList();
        if (eligible.Count == 0)
        {
            SimLog.Log("[EncounterGenerator] No eligible templates for context");
            return null;
        }

        SimLog.Log($"[EncounterGenerator] Found {eligible.Count} eligible templates");

        // 2. Calculate weights
        var weights = CalculateWeights(eligible, context);

        // 3. Select template
        var template = WeightedSelect(eligible, weights, rng);
        if (template == null)
        {
            SimLog.Log("[EncounterGenerator] Failed to select template");
            return null;
        }

        SimLog.Log($"[EncounterGenerator] Selected template: {template.Id}");

        // 4. Create instance with resolved parameters
        var instance = EncounterInstance.Create(template, rng);
        if (instance == null)
        {
            SimLog.Log($"[EncounterGenerator] Failed to create instance for {template.Id}");
            return null;
        }

        // 5. Resolve parameters
        ResolveParameters(instance, context, campaign, rng);

        return instance;
    }

    /// <summary>
    /// Calculate selection weights for eligible templates based on context.
    /// Higher weight = more likely to be selected.
    /// </summary>
    public Dictionary<EncounterTemplate, float> CalculateWeights(
        List<EncounterTemplate> templates,
        TravelContext context)
    {
        var weights = new Dictionary<EncounterTemplate, float>();
        var metrics = context?.SystemMetrics;
        var cfg = weightConfig;

        foreach (var template in templates)
        {
            float weight = cfg.BaseWeight;

            // === Type-based weighting ===

            if (template.HasTag(EncounterTags.Pirate))
            {
                int criminal = metrics?.CriminalActivity ?? 2;
                weight *= cfg.PirateBaseMultiplier + (criminal * cfg.PirateMetricMultiplier);
            }

            if (template.HasTag(EncounterTags.Patrol))
            {
                int security = metrics?.SecurityLevel ?? 2;
                weight *= cfg.PatrolBaseMultiplier + (security * cfg.PatrolMetricMultiplier);
            }

            if (template.HasTag(EncounterTags.Trader))
            {
                int economic = metrics?.EconomicActivity ?? 2;
                weight *= cfg.TraderBaseMultiplier + (economic * cfg.TraderMetricMultiplier);
            }

            if (template.HasTag(EncounterTags.Smuggler))
            {
                int security = metrics?.SecurityLevel ?? 2;
                int criminal = metrics?.CriminalActivity ?? 2;
                weight *= (5 - security) * cfg.SmugglerSecurityMultiplier;
                weight *= cfg.SmugglerCrimeBaseMultiplier + (criminal * cfg.SmugglerCrimeMultiplier);
            }

            // === Context-based weighting ===

            if (template.HasTag(EncounterTags.Cargo) && context.CargoValue > cfg.CargoValueThreshold)
            {
                weight *= cfg.CargoValueBoost;
            }

            if (template.HasTag(EncounterTags.Combat))
            {
                weight *= cfg.CombatBaseMultiplier + (context.RouteHazard * cfg.CombatHazardMultiplier);
            }

            if (template.HasTag(EncounterTags.Rare))
            {
                weight *= cfg.RareMultiplier;
            }

            if (!string.IsNullOrEmpty(context.SuggestedEncounterType) &&
                context.SuggestedEncounterType != EncounterTypes.Random &&
                template.HasTag(context.SuggestedEncounterType))
            {
                weight *= cfg.SuggestedTypeBoost;
            }

            if (template.HasTag(EncounterTags.Faction) &&
                !string.IsNullOrEmpty(context.SystemOwnerFactionId))
            {
                weight *= cfg.FactionTerritoryBoost;
            }

            if (template.HasTag(EncounterTags.Distress))
            {
                weight *= cfg.DistressMultiplier;
            }

            weights[template] = Math.Max(cfg.MinWeight, weight);
        }

        return weights;
    }

    /// <summary>
    /// Select a template using weighted random selection.
    /// </summary>
    private EncounterTemplate WeightedSelect(
        List<EncounterTemplate> templates,
        Dictionary<EncounterTemplate, float> weights,
        RngStream rng)
    {
        if (templates == null || templates.Count == 0)
            return null;

        float totalWeight = weights.Values.Sum();
        if (totalWeight <= 0)
            return templates.FirstOrDefault();

        float roll = rng.NextFloat() * totalWeight;
        float cumulative = 0f;

        foreach (var template in templates)
        {
            cumulative += weights.GetValueOrDefault(template, 0f);
            if (roll <= cumulative)
                return template;
        }

        return templates.LastOrDefault();
    }

    /// <summary>
    /// Resolve template parameters based on context.
    /// Populates the instance's ResolvedParameters dictionary.
    /// </summary>
    private void ResolveParameters(
        EncounterInstance instance,
        TravelContext context,
        CampaignState campaign,
        RngStream rng)
    {
        // === Location ===
        instance.SetParameter("system_name", context.CurrentSystem?.Name ?? "Unknown System");
        instance.SetParameter("system_id", context.CurrentSystemId.ToString());

        var destSystem = campaign?.World?.GetSystem(context.DestinationSystemId);
        instance.SetParameter("destination_name", destSystem?.Name ?? "destination");

        // === Faction ===
        instance.SetParameter("faction_id", context.SystemOwnerFactionId ?? "neutral");
        var faction = campaign?.World?.GetFaction(context.SystemOwnerFactionId);
        instance.SetParameter("faction_name", faction?.Name ?? "local authorities");

        // === NPCs ===
        instance.SetParameter("npc_name", NameGenerator.GenerateNpcName(rng));
        instance.SetParameter("npc_first_name", NameGenerator.GenerateFirstName(rng));
        instance.SetParameter("pirate_name", NameGenerator.GeneratePirateName(rng));
        instance.SetParameter("captain_name", NameGenerator.GenerateNpcName(rng, includeNickname: false));

        // === Ships ===
        instance.SetParameter("ship_name", NameGenerator.GenerateShipName(rng));
        instance.SetParameter("ship_name_simple", NameGenerator.GenerateShipNameSimple(rng));
        instance.SetParameter("pirate_ship", NameGenerator.GeneratePirateShipName(rng));

        // === Cargo ===
        instance.SetParameter("cargo_type", NameGenerator.GenerateCargoType(rng));
        instance.SetParameter("valuable_cargo", NameGenerator.GenerateCargoType(rng, valuable: true));
        instance.SetParameter("illegal_cargo", NameGenerator.GenerateCargoType(rng, illegal: true));

        // === Values (for rewards/costs) ===
        int baseValue = 50 + (context.RouteHazard * 20);
        instance.SetParameter("small_credits", (baseValue / 2).ToString());
        instance.SetParameter("medium_credits", baseValue.ToString());
        instance.SetParameter("large_credits", (baseValue * 2).ToString());

        int baseFuel = 5 + context.RouteHazard;
        instance.SetParameter("small_fuel", (baseFuel / 2).ToString());
        instance.SetParameter("medium_fuel", baseFuel.ToString());
        instance.SetParameter("large_fuel", (baseFuel * 2).ToString());

        // === Context flags ===
        instance.SetParameter("has_cargo", (context.CargoValue > 0).ToString().ToLower());
        instance.SetParameter("cargo_value", context.CargoValue.ToString());
        instance.SetParameter("has_illegal", context.HasIllegalCargo.ToString().ToLower());
        instance.SetParameter("crew_count", context.CrewCount.ToString());

        bool isHostile = context.PlayerRepWithOwner < 25;
        instance.SetParameter("is_hostile_territory", isHostile.ToString().ToLower());
        instance.SetParameter("player_reputation", context.PlayerRepWithOwner.ToString());

        // === System metrics ===
        var metrics = context.SystemMetrics;
        if (metrics != null)
        {
            instance.SetParameter("security_level", metrics.SecurityLevel.ToString());
            instance.SetParameter("criminal_activity", metrics.CriminalActivity.ToString());
            instance.SetParameter("economic_activity", metrics.EconomicActivity.ToString());
        }
    }

    /// <summary>
    /// Get the weight for a specific template given a context.
    /// Useful for debugging and testing.
    /// </summary>
    public float GetTemplateWeight(EncounterTemplate template, TravelContext context)
    {
        if (template == null) return 0f;
        var weights = CalculateWeights(new List<EncounterTemplate> { template }, context);
        return weights.GetValueOrDefault(template, 0f);
    }
}
