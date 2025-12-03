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
/// Spawn entry for an interactable in a mission.
/// Used for interactables not defined in the map template.
/// </summary>
public class InteractableSpawn
{
    public string Type { get; set; }
    public Vector2I Position { get; set; }
    public Dictionary<string, object> Properties { get; set; } = new();
    public InteractableState? InitialState { get; set; } = null;

    public InteractableSpawn(string type, Vector2I position)
    {
        Type = type;
        Position = position;
    }
    
    public InteractableSpawn WithProperty(string key, object value)
    {
        Properties[key] = value;
        return this;
    }
    
    public InteractableSpawn WithState(InteractableState state)
    {
        InitialState = state;
        return this;
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
    
    // Map template (optional - if null, uses GridSize to create basic map)
    // Template characters: '.' = floor, '#' = wall, 'E' = entry zone, ' ' = void
    public string[] MapTemplate { get; set; } = null;
    
    // Entry zone (optional - if null, derived from template 'E' tiles or default)
    public List<Vector2I> EntryZone { get; set; } = new();

    // Spawn positions
    public List<Vector2I> CrewSpawnPositions { get; set; } = new();
    public List<EnemySpawn> EnemySpawns { get; set; } = new();
    
    // Interactable spawns (for interactables not in map template)
    public List<InteractableSpawn> InteractableSpawns { get; set; } = new();

    // Crew weapon (default for sandbox)
    public string CrewWeaponId { get; set; } = "rifle";

    /// <summary>
    /// Default test mission configuration with walls and entry zone.
    /// </summary>
    public static MissionConfig CreateTestMission()
    {
        return new MissionConfig
        {
            Id = "test_mission",
            Name = "Test Mission",
            GridSize = new Vector2I(14, 10),
            MapTemplate = new string[]
            {
                "##############",
                "#............#",
                "#.EE.........#",
                "#.EE.........#",
                "#............#",
                "#............#",
                "#............#",
                "#............#",
                "#............#",
                "##############"
            },
            CrewWeaponId = "rifle",
            CrewSpawnPositions = new List<Vector2I>
            {
                new Vector2I(2, 2),
                new Vector2I(3, 2),
                new Vector2I(2, 3),
                new Vector2I(3, 3)
            },
            EnemySpawns = new List<EnemySpawn>
            {
                new EnemySpawn("grunt", new Vector2I(10, 3)),
                new EnemySpawn("gunner", new Vector2I(11, 5))
            }
        };
    }
    
    /// <summary>
    /// M0 test mission - single unit, no enemies, for testing basic movement.
    /// </summary>
    public static MissionConfig CreateM0TestMission()
    {
        return new MissionConfig
        {
            Id = "m0_test",
            Name = "M0 Test - Movement Only",
            GridSize = new Vector2I(14, 10),
            MapTemplate = new string[]
            {
                "##############",
                "#............#",
                "#.E..........#",
                "#............#",
                "#....###.....#",
                "#....#.......#",
                "#....#.......#",
                "#............#",
                "#............#",
                "##############"
            },
            CrewWeaponId = "rifle",
            CrewSpawnPositions = new List<Vector2I>
            {
                new Vector2I(2, 2)
            },
            EnemySpawns = new List<EnemySpawn>() // No enemies for M0
        };
    }

    /// <summary>
    /// M1 test mission - multiple units, no enemies, for testing selection and group movement.
    /// Units are spread out to test box selection and formation movement.
    /// </summary>
    public static MissionConfig CreateM1TestMission()
    {
        return new MissionConfig
        {
            Id = "m1_test",
            Name = "M1 Test - Multi-Unit Selection",
            GridSize = new Vector2I(18, 14),
            MapTemplate = new string[]
            {
                "##################",
                "#................#",
                "#.EE.............#",
                "#.EE.............#",
                "#................#",
                "#......###.......#",
                "#......#.........#",
                "#......#.........#",
                "#................#",
                "#........###.....#",
                "#........#.......#",
                "#................#",
                "#................#",
                "##################"
            },
            CrewWeaponId = "rifle",
            CrewSpawnPositions = new List<Vector2I>
            {
                // Group A - clustered in entry zone (for box select testing)
                new Vector2I(2, 2),
                new Vector2I(3, 2),
                new Vector2I(2, 3),
                new Vector2I(3, 3),
                // Group B - spread out (for shift-click testing)
                new Vector2I(6, 6),
                new Vector2I(10, 4)
            },
            EnemySpawns = new List<EnemySpawn>() // No enemies for M1
        };
    }

    /// <summary>
    /// M2 test mission - visibility and fog of war testing.
    /// Map has multiple rooms and corridors to test LOS blocking.
    /// Enemies placed in hidden areas to test fog hiding.
    /// </summary>
    public static MissionConfig CreateM2TestMission()
    {
        return new MissionConfig
        {
            Id = "m2_test",
            Name = "M2 Test - Visibility & Fog of War",
            GridSize = new Vector2I(24, 16),
            MapTemplate = new string[]
            {
                "########################",
                "#......#...............#",
                "#.EE...#...............#",
                "#.EE...#...............#",
                "#......#...............#",
                "#......#####.###.......#",
                "#..........#.#.........#",
                "#..........#.#.........#",
                "######.#####.#.........#",
                "#..........#.#.........#",
                "#..........#.#####.#####",
                "#..........#.......#...#",
                "#..........#.......#...#",
                "#..........#########...#",
                "#......................#",
                "########################"
            },
            CrewWeaponId = "rifle",
            CrewSpawnPositions = new List<Vector2I>
            {
                new Vector2I(2, 2),
                new Vector2I(3, 2),
                new Vector2I(2, 3),
                new Vector2I(3, 3)
            },
            EnemySpawns = new List<EnemySpawn>
            {
                // Enemy in far room (should be hidden initially)
                new EnemySpawn("grunt", new Vector2I(20, 2)),
                // Enemy behind wall (tests LOS blocking)
                new EnemySpawn("grunt", new Vector2I(14, 7)),
                // Enemy in bottom-right room
                new EnemySpawn("gunner", new Vector2I(21, 12))
            }
        };
    }

    /// <summary>
    /// M3 test mission - basic combat testing.
    /// Open arena with enemies at various ranges for testing hit chance, ammo, and auto-defend.
    /// </summary>
    public static MissionConfig CreateM3TestMission()
    {
        return new MissionConfig
        {
            Id = "m3_test",
            Name = "M3 Test - Basic Combat",
            GridSize = new Vector2I(20, 16),
            MapTemplate = new string[]
            {
                "####################",
                "#..................#",
                "#.EE...............#",
                "#.EE...............#",
                "#..................#",
                "#..................#",
                "#......##..........#",
                "#......##..........#",
                "#..................#",
                "#..................#",
                "#..................#",
                "#..................#",
                "#..................#",
                "#..................#",
                "#..................#",
                "####################"
            },
            CrewWeaponId = "rifle",
            CrewSpawnPositions = new List<Vector2I>
            {
                new Vector2I(2, 2),
                new Vector2I(3, 2),
                new Vector2I(2, 3),
                new Vector2I(3, 3)
            },
            EnemySpawns = new List<EnemySpawn>
            {
                // Close range enemy (easy target)
                new EnemySpawn("grunt", new Vector2I(8, 4)),
                // Medium range enemy
                new EnemySpawn("grunt", new Vector2I(12, 6)),
                // Long range enemy (harder to hit)
                new EnemySpawn("gunner", new Vector2I(17, 10)),
                // Flanking enemy
                new EnemySpawn("grunt", new Vector2I(15, 2))
            }
        };
    }

    /// <summary>
    /// M4 test mission - directional cover testing.
    /// Map has walls creating cover positions from specific directions.
    /// Enemies placed both in cover and exposed to test cover mechanics.
    /// </summary>
    public static MissionConfig CreateM4TestMission()
    {
        return new MissionConfig
        {
            Id = "m4_test",
            Name = "M4 Test - Cover Combat",
            GridSize = new Vector2I(20, 16),
            MapTemplate = new string[]
            {
                "####################",
                "#..................#",
                "#.EE..##....##.....#",
                "#.EE..##....##..E..#",
                "#.....##....##.....#",
                "#..................#",
                "#......####........#",
                "#......####........#",
                "#..................#",
                "#..##..........##..#",
                "#..##..........##..#",
                "#..................#",
                "#..................#",
                "#..................#",
                "#..................#",
                "####################"
            },
            CrewWeaponId = "rifle",
            CrewSpawnPositions = new List<Vector2I>
            {
                new Vector2I(2, 2),
                new Vector2I(3, 2),
                new Vector2I(2, 3),
                new Vector2I(3, 3)
            },
            EnemySpawns = new List<EnemySpawn>
            {
                // Enemy behind vertical wall (wall to their west) - has cover from west
                new EnemySpawn("grunt", new Vector2I(8, 3)),
                // Enemy in open (no cover)
                new EnemySpawn("grunt", new Vector2I(14, 5)),
                // Enemy behind central cover block
                new EnemySpawn("grunt", new Vector2I(11, 7)),
                // Enemy behind vertical wall (wall to their east) - has cover from east
                new EnemySpawn("gunner", new Vector2I(16, 3))
            }
        };
    }

    /// <summary>
    /// M4.1 test mission - cover height testing.
    /// Map has low (-), half (=), and high (+) cover objects.
    /// Tests different cover heights and their hit reduction effects.
    /// </summary>
    public static MissionConfig CreateM4_1TestMission()
    {
        return new MissionConfig
        {
            Id = "m4_1_test",
            Name = "M4.1 Test - Cover Heights",
            GridSize = new Vector2I(24, 18),
            MapTemplate = new string[]
            {
                "########################",
                "#......................#",
                "#.EE..................E#",
                "#.EE..................E#",
                "#......................#",
                "#....-.....=.....+.....#",  // Low, Half, High cover in a row
                "#......................#",
                "#......................#",
                "#..---...===...+++.....#",  // Clusters of each type
                "#..---...===...+++.....#",
                "#......................#",
                "#......................#",
                "#.....-=+..............#",  // Mixed cover
                "#......................#",
                "#......................#",
                "#......................#",
                "#......................#",
                "########################"
            },
            CrewWeaponId = "rifle",
            CrewSpawnPositions = new List<Vector2I>
            {
                new Vector2I(2, 2),
                new Vector2I(3, 2),
                new Vector2I(2, 3),
                new Vector2I(3, 3)
            },
            EnemySpawns = new List<EnemySpawn>
            {
                // Enemy behind low cover (15% reduction)
                new EnemySpawn("grunt", new Vector2I(6, 5)),
                // Enemy behind half cover (30% reduction)
                new EnemySpawn("grunt", new Vector2I(12, 5)),
                // Enemy behind high cover (45% reduction)
                new EnemySpawn("grunt", new Vector2I(18, 5)),
                // Enemy in open (no cover)
                new EnemySpawn("gunner", new Vector2I(22, 3)),
                // Enemy behind mixed cover cluster
                new EnemySpawn("grunt", new Vector2I(8, 12))
            }
        };
    }

    /// <summary>
    /// M5 test mission - interactables and channeled hacking.
    /// Features doors (open/closed/locked), terminals, and hazards.
    /// </summary>
    public static MissionConfig CreateM5TestMission()
    {
        return new MissionConfig
        {
            Id = "m5_test",
            Name = "M5 Test - Interactables",
            GridSize = new Vector2I(22, 16),
            MapTemplate = new string[]
            {
                "######################",
                "#EE..................#",
                "#EE.....#D#..........#",
                "#.......#.#..........#",
                "#.......#.#....T.....#",
                "#.......###..........#",
                "#....................#",
                "#....X...............#",
                "#....................#",
                "#.........###L###....#",
                "#.........#.....#....#",
                "#.........#..T..#....#",
                "#.........#.....#....#",
                "#.........#######....#",
                "#....................#",
                "######################"
            },
            CrewWeaponId = "rifle",
            CrewSpawnPositions = new List<Vector2I>
            {
                new Vector2I(1, 1),
                new Vector2I(2, 1),
                new Vector2I(1, 2),
                new Vector2I(2, 2)
            },
            EnemySpawns = new List<EnemySpawn>
            {
                // Guard near first door
                new EnemySpawn("grunt", new Vector2I(12, 3)),
                // Guard in locked room
                new EnemySpawn("grunt", new Vector2I(13, 11)),
                // Patrol in corridor
                new EnemySpawn("grunt", new Vector2I(17, 7))
            }
        };
    }

    /// <summary>
    /// Harder mission with more enemies and interior walls.
    /// </summary>
    public static MissionConfig CreateHardMission()
    {
        return new MissionConfig
        {
            Id = "hard_mission",
            Name = "Hard Mission",
            GridSize = new Vector2I(16, 12),
            MapTemplate = new string[]
            {
                "################",
                "#..............#",
                "#.EE...........#",
                "#.EE...........#",
                "#......###.....#",
                "#......#.......#",
                "#......#.......#",
                "#..............#",
                "#.....###......#",
                "#..............#",
                "#..............#",
                "################"
            },
            CrewWeaponId = "rifle",
            CrewSpawnPositions = new List<Vector2I>
            {
                new Vector2I(2, 2),
                new Vector2I(3, 2),
                new Vector2I(2, 3),
                new Vector2I(3, 3)
            },
            EnemySpawns = new List<EnemySpawn>
            {
                new EnemySpawn("grunt", new Vector2I(12, 3)),
                new EnemySpawn("grunt", new Vector2I(13, 5)),
                new EnemySpawn("gunner", new Vector2I(14, 4)),
                new EnemySpawn("heavy", new Vector2I(12, 8))
            }
        };
    }
}
