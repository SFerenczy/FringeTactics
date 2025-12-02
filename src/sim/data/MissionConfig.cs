using Godot; // For Vector2I only - no Node/UI types
using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// Spawn entry for an enemy in a mission.
/// </summary>
public class EnemySpawn
{
    public string EnemyId { get; set; }
    public Vector2I Position { get; set; }

    public EnemySpawn(string enemyId, Vector2I position)
    {
        EnemyId = enemyId;
        Position = position;
    }
}

/// <summary>
/// Configuration for a mission. Data-driven mission setup.
/// </summary>
public class MissionConfig
{
    public string Id { get; set; } = "test_mission";
    public string Name { get; set; } = "Test Mission";

    // Map settings
    public Vector2I GridSize { get; set; } = new Vector2I(12, 10);

    // Spawn positions
    public List<Vector2I> CrewSpawnPositions { get; set; } = new();
    public List<EnemySpawn> EnemySpawns { get; set; } = new();

    // Crew weapon (default for sandbox)
    public string CrewWeaponId { get; set; } = "rifle";

    /// <summary>
    /// Default test mission configuration.
    /// </summary>
    public static MissionConfig CreateTestMission()
    {
        return new MissionConfig
        {
            Id = "test_mission",
            Name = "Test Mission",
            GridSize = new Vector2I(12, 10),
            CrewWeaponId = "rifle",
            CrewSpawnPositions = new List<Vector2I>
            {
                new Vector2I(2, 2),
                new Vector2I(4, 2),
                new Vector2I(3, 4),
                new Vector2I(5, 4)
            },
            EnemySpawns = new List<EnemySpawn>
            {
                new EnemySpawn("grunt", new Vector2I(9, 3)),
                new EnemySpawn("gunner", new Vector2I(10, 5))
            }
        };
    }

    /// <summary>
    /// Harder mission with more enemies.
    /// </summary>
    public static MissionConfig CreateHardMission()
    {
        return new MissionConfig
        {
            Id = "hard_mission",
            Name = "Hard Mission",
            GridSize = new Vector2I(14, 12),
            CrewWeaponId = "rifle",
            CrewSpawnPositions = new List<Vector2I>
            {
                new Vector2I(2, 2),
                new Vector2I(4, 2),
                new Vector2I(3, 4),
                new Vector2I(5, 4)
            },
            EnemySpawns = new List<EnemySpawn>
            {
                new EnemySpawn("grunt", new Vector2I(10, 3)),
                new EnemySpawn("grunt", new Vector2I(11, 5)),
                new EnemySpawn("gunner", new Vector2I(12, 4)),
                new EnemySpawn("heavy", new Vector2I(11, 7))
            }
        };
    }
}
