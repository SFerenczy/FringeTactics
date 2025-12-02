using Godot; // For Vector2 only - no Node/UI types
using System;
using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// Types of sector nodes.
/// </summary>
public enum NodeType
{
    Station,    // Trade hub, safe
    Outpost,    // Small settlement
    Derelict,   // Abandoned, risky
    Asteroid,   // Mining opportunity
    Nebula,     // Hiding spot, sensor interference
    Contested   // Active conflict zone
}

/// <summary>
/// A node in the sector graph.
/// </summary>
public class SectorNode
{
    public int Id { get; set; }
    public string Name { get; set; }
    public NodeType Type { get; set; }
    public string FactionId { get; set; } // null = unclaimed
    public Vector2 Position { get; set; } // For visual layout
    public List<int> Connections { get; set; } = new(); // Connected node IDs
    public bool HasJob { get; set; } = false;

    public SectorNode(int id, string name, NodeType type, Vector2 position)
    {
        Id = id;
        Name = name;
        Type = type;
        Position = position;
    }
}

/// <summary>
/// The sector map - a graph of nodes the player can travel between.
/// </summary>
public class Sector
{
    public string Name { get; set; } = "Fringe Sector";
    public List<SectorNode> Nodes { get; set; } = new();
    public Dictionary<string, string> Factions { get; set; } = new(); // factionId -> name

    private Random rng;

    public Sector()
    {
        rng = new Random();
    }

    public Sector(int seed)
    {
        rng = new Random(seed);
    }

    /// <summary>
    /// Generate a small test sector with 8-10 nodes.
    /// </summary>
    public static Sector GenerateTestSector(int seed = 12345)
    {
        var sector = new Sector(seed);
        sector.Name = "Outer Reach";

        // Define factions
        sector.Factions["corp"] = "Helix Corp";
        sector.Factions["rebels"] = "Free Colonies";
        sector.Factions["pirates"] = "Red Claw";

        // Node names
        var stationNames = new[] { "Haven Station", "Port Meridian", "Crossroads Hub" };
        var outpostNames = new[] { "Dusty Rock", "Miner's Rest", "Frontier Post" };
        var derelictNames = new[] { "Ghost Ship Graveyard", "Abandoned Lab" };
        var asteroidNames = new[] { "Rich Vein", "Ore Field Alpha" };

        int nodeId = 0;

        // Create nodes in a rough layout
        // Center station (safe starting point)
        var startNode = new SectorNode(nodeId++, "Haven Station", NodeType.Station, new Vector2(300, 250))
        {
            FactionId = "corp"
        };
        sector.Nodes.Add(startNode);

        // Ring of nodes around center
        var positions = new Vector2[]
        {
            new Vector2(150, 100),  // Top-left
            new Vector2(450, 100),  // Top-right
            new Vector2(550, 250),  // Right
            new Vector2(450, 400),  // Bottom-right
            new Vector2(150, 400),  // Bottom-left
            new Vector2(50, 250),   // Left
            new Vector2(300, 80),   // Top
            new Vector2(300, 420),  // Bottom
        };

        var nodeConfigs = new (string name, NodeType type, string faction)[]
        {
            ("Dusty Rock", NodeType.Outpost, "rebels"),
            ("Port Meridian", NodeType.Station, "corp"),
            ("Rich Vein", NodeType.Asteroid, null),
            ("Ghost Ship Graveyard", NodeType.Derelict, null),
            ("Miner's Rest", NodeType.Outpost, "rebels"),
            ("Red Claw Den", NodeType.Contested, "pirates"),
            ("Nebula's Edge", NodeType.Nebula, null),
            ("Ore Field Alpha", NodeType.Asteroid, null),
        };

        for (int i = 0; i < nodeConfigs.Length; i++)
        {
            var config = nodeConfigs[i];
            var node = new SectorNode(nodeId++, config.name, config.type, positions[i])
            {
                FactionId = config.faction,
                HasJob = sector.rng.NextDouble() < 0.4 // 40% chance of job
            };
            sector.Nodes.Add(node);
        }

        // Create connections (simple: connect to nearby nodes)
        sector.ConnectNearbyNodes(200f);

        // Ensure graph is connected
        sector.EnsureConnected();

        return sector;
    }

    private void ConnectNearbyNodes(float maxDistance)
    {
        for (int i = 0; i < Nodes.Count; i++)
        {
            for (int j = i + 1; j < Nodes.Count; j++)
            {
                var dist = Nodes[i].Position.DistanceTo(Nodes[j].Position);
                if (dist <= maxDistance)
                {
                    Connect(Nodes[i].Id, Nodes[j].Id);
                }
            }
        }
    }

    private void EnsureConnected()
    {
        // Simple: connect center node to all unconnected nodes
        var centerNode = Nodes[0];
        foreach (var node in Nodes)
        {
            if (node.Id != centerNode.Id && node.Connections.Count == 0)
            {
                Connect(centerNode.Id, node.Id);
            }
        }
    }

    public void Connect(int nodeA, int nodeB)
    {
        var a = GetNode(nodeA);
        var b = GetNode(nodeB);
        if (a != null && b != null)
        {
            if (!a.Connections.Contains(nodeB))
                a.Connections.Add(nodeB);
            if (!b.Connections.Contains(nodeA))
                b.Connections.Add(nodeA);
        }
    }

    public SectorNode GetNode(int id)
    {
        foreach (var node in Nodes)
        {
            if (node.Id == id) return node;
        }
        return null;
    }

    public bool AreConnected(int nodeA, int nodeB)
    {
        var a = GetNode(nodeA);
        return a != null && a.Connections.Contains(nodeB);
    }

    public float GetTravelDistance(int fromId, int toId)
    {
        var from = GetNode(fromId);
        var to = GetNode(toId);
        if (from == null || to == null) return float.MaxValue;
        return from.Position.DistanceTo(to.Position);
    }
}
