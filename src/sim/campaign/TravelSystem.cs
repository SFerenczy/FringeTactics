using System;

namespace FringeTactics;

/// <summary>
/// Result of a travel attempt.
/// </summary>
public enum TravelResult
{
    Success,        // Arrived safely
    NotEnoughFuel,  // Can't afford travel
    NotConnected,   // Nodes not connected
    AlreadyThere,   // Already at destination
    Ambush          // Encountered enemies (future: triggers combat)
}

/// <summary>
/// Handles travel between sector nodes.
/// </summary>
public static class TravelSystem
{
    // Fuel cost per unit of distance
    public const float FUEL_PER_DISTANCE = 0.05f;
    public const int MIN_FUEL_COST = 2;

    // Time cost per unit of distance
    public const float DAYS_PER_DISTANCE = 0.02f;
    public const int MIN_TRAVEL_DAYS = 1;

    // Random encounter chance (for future use)
    public const float AMBUSH_CHANCE = 0.15f;

    /// <summary>
    /// Calculate fuel cost to travel between two nodes.
    /// </summary>
    public static int CalculateFuelCost(Sector sector, int fromId, int toId)
    {
        var distance = sector.GetTravelDistance(fromId, toId);
        var cost = (int)(distance * FUEL_PER_DISTANCE);
        return Math.Max(cost, MIN_FUEL_COST);
    }

    /// <summary>
    /// Calculate time cost in days to travel between two nodes.
    /// </summary>
    public static int CalculateTravelDays(Sector sector, int fromId, int toId)
    {
        var distance = sector.GetTravelDistance(fromId, toId);
        var days = (int)Math.Ceiling(distance * DAYS_PER_DISTANCE);
        return Math.Max(days, MIN_TRAVEL_DAYS);
    }

    /// <summary>
    /// Check if travel is possible (connected and enough fuel).
    /// </summary>
    public static bool CanTravel(CampaignState campaign, Sector sector, int toId)
    {
        if (campaign.CurrentNodeId == toId) return false;
        if (!sector.AreConnected(campaign.CurrentNodeId, toId)) return false;

        var fuelCost = CalculateFuelCost(sector, campaign.CurrentNodeId, toId);
        return campaign.Fuel >= fuelCost;
    }

    /// <summary>
    /// Get reason why travel is blocked.
    /// </summary>
    public static string GetTravelBlockReason(CampaignState campaign, Sector sector, int toId)
    {
        if (campaign.CurrentNodeId == toId)
            return "Already at this location";

        if (!sector.AreConnected(campaign.CurrentNodeId, toId))
            return "No route to this location";

        var fuelCost = CalculateFuelCost(sector, campaign.CurrentNodeId, toId);
        if (campaign.Fuel < fuelCost)
            return $"Need {fuelCost} fuel (have {campaign.Fuel})";

        return null;
    }

    /// <summary>
    /// Get a summary of travel costs for display.
    /// </summary>
    public static string GetTravelCostSummary(CampaignState campaign, Sector sector, int toId)
    {
        var fuelCost = CalculateFuelCost(sector, campaign.CurrentNodeId, toId);
        var timeCost = CalculateTravelDays(sector, campaign.CurrentNodeId, toId);
        return $"{fuelCost} fuel, {CampaignTime.FormatDuration(timeCost)}";
    }

    /// <summary>
    /// Attempt to travel to a node. Returns result and consumes fuel on success.
    /// </summary>
    public static TravelResult Travel(CampaignState campaign, Sector sector, int toId, Random rng = null)
    {
        if (campaign.CurrentNodeId == toId)
            return TravelResult.AlreadyThere;

        if (!sector.AreConnected(campaign.CurrentNodeId, toId))
            return TravelResult.NotConnected;

        var fuelCost = CalculateFuelCost(sector, campaign.CurrentNodeId, toId);
        if (campaign.Fuel < fuelCost)
            return TravelResult.NotEnoughFuel;

        // Calculate costs
        var fromNode = sector.GetNode(campaign.CurrentNodeId);
        var toNode = sector.GetNode(toId);
        var timeCost = CalculateTravelDays(sector, campaign.CurrentNodeId, toId);

        // Consume resources
        campaign.Fuel -= fuelCost;
        campaign.Time.AdvanceDays(timeCost);
        campaign.CurrentNodeId = toId;

        SimLog.Log($"[Travel] Traveled from {fromNode?.Name} to {toNode?.Name}. Cost: {fuelCost} fuel, {timeCost} day(s). (Fuel remaining: {campaign.Fuel})");

        // Random encounter check (for future use)
        if (rng != null && toNode?.Type == NodeType.Contested)
        {
            if (rng.NextDouble() < AMBUSH_CHANCE)
            {
                SimLog.Log("[Travel] Ambush! Enemies detected.");
                return TravelResult.Ambush;
            }
        }

        return TravelResult.Success;
    }
}
