using Godot;
using GdUnit4;
using static GdUnit4.Assertions;

namespace FringeTactics.Tests;

/// <summary>
/// M0 Milestone tests - validates the tactical skeleton foundation.
/// These tests use Vector2I from Godot, so they require the Godot runtime.
/// However, they don't instantiate any Nodes - they test pure simulation logic.
/// </summary>
[TestSuite]
public class M0Tests
{
    [TestCase]
    [RequireGodotRuntime]
    public void MapBuilder_BuildFromTemplate_CreatesCorrectSize()
    {
        var template = new string[]
        {
            "###",
            "#.#",
            "###"
        };
        
        var map = MapBuilder.BuildFromTemplate(template);
        
        AssertThat(map.GridSize).IsEqual(new Vector2I(3, 3));
    }
    
    [TestCase]
    [RequireGodotRuntime]
    public void MapBuilder_BuildFromTemplate_ParsesWalls()
    {
        var template = new string[]
        {
            "###",
            "#.#",
            "###"
        };
        
        var map = MapBuilder.BuildFromTemplate(template);
        
        // Corners and edges should be walls
        AssertThat(map.GetTileType(new Vector2I(0, 0))).IsEqual(TileType.Wall);
        AssertThat(map.GetTileType(new Vector2I(2, 0))).IsEqual(TileType.Wall);
        AssertThat(map.GetTileType(new Vector2I(0, 2))).IsEqual(TileType.Wall);
        
        // Center should be floor
        AssertThat(map.GetTileType(new Vector2I(1, 1))).IsEqual(TileType.Floor);
    }
    
    [TestCase]
    [RequireGodotRuntime]
    public void MapBuilder_BuildFromTemplate_ParsesEntryZone()
    {
        var template = new string[]
        {
            "###",
            "#E#",
            "###"
        };
        
        var map = MapBuilder.BuildFromTemplate(template);
        
        // E should be floor AND in entry zone
        AssertThat(map.GetTileType(new Vector2I(1, 1))).IsEqual(TileType.Floor);
        AssertThat(map.IsInEntryZone(new Vector2I(1, 1))).IsTrue();
        
        // Walls should not be in entry zone
        AssertThat(map.IsInEntryZone(new Vector2I(0, 0))).IsFalse();
    }
    
    [TestCase]
    [RequireGodotRuntime]
    public void MapState_IsWalkable_RespectsWalls()
    {
        var template = new string[]
        {
            "#####",
            "#...#",
            "#...#",
            "#...#",
            "#####"
        };
        
        var map = MapBuilder.BuildFromTemplate(template);
        
        // Walls are not walkable
        AssertThat(map.IsWalkable(new Vector2I(0, 0))).IsFalse();
        AssertThat(map.IsWalkable(new Vector2I(4, 4))).IsFalse();
        
        // Floor is walkable
        AssertThat(map.IsWalkable(new Vector2I(2, 2))).IsTrue();
        AssertThat(map.IsWalkable(new Vector2I(1, 1))).IsTrue();
    }
    
    [TestCase]
    [RequireGodotRuntime]
    public void MapState_BlocksLOS_ForWalls()
    {
        var template = new string[]
        {
            "###",
            "#.#",
            "###"
        };
        
        var map = MapBuilder.BuildFromTemplate(template);
        
        // Walls block LOS
        AssertThat(map.BlocksLOS(new Vector2I(0, 0))).IsTrue();
        
        // Floor doesn't block LOS
        AssertThat(map.BlocksLOS(new Vector2I(1, 1))).IsFalse();
    }
    
    [TestCase]
    [RequireGodotRuntime]
    public void MissionConfig_CreateM0TestMission_HasSingleCrewSpawn()
    {
        var config = MissionConfig.CreateM0TestMission();
        
        AssertThat(config.CrewSpawnPositions.Count).IsEqual(1);
    }
    
    [TestCase]
    [RequireGodotRuntime]
    public void MissionConfig_CreateM0TestMission_HasNoEnemies()
    {
        var config = MissionConfig.CreateM0TestMission();
        
        AssertThat(config.EnemySpawns.Count).IsEqual(0);
    }
    
    [TestCase]
    [RequireGodotRuntime]
    public void CombatState_NoEnemies_DoesNotAutoWin()
    {
        var config = MissionConfig.CreateM0TestMission();
        var combat = MissionFactory.BuildSandbox(config);
        combat.TimeSystem.Resume();
        
        // Simulate 5 seconds (100 ticks at 20 ticks/sec)
        for (int i = 0; i < 100; i++)
        {
            combat.Update(0.05f);
        }
        
        // Mission should NOT be complete (no auto-win without enemies)
        AssertThat(combat.IsComplete).IsFalse();
        AssertThat(combat.Phase).IsEqual(MissionPhase.Active);
    }
    
    [TestCase]
    [RequireGodotRuntime]
    public void CombatState_WithEnemies_WinsWhenAllDead()
    {
        var config = MissionConfig.CreateTestMission(); // Has enemies
        var combat = MissionFactory.BuildSandbox(config);
        combat.TimeSystem.Resume();
        
        // Kill all enemies manually
        foreach (var actor in combat.Actors)
        {
            if (actor.Type == "enemy")
            {
                actor.TakeDamage(9999);
            }
        }
        
        // Process one tick to check win condition
        combat.Update(0.05f);
        
        AssertThat(combat.IsComplete).IsTrue();
        AssertThat(combat.Victory).IsTrue();
    }
    
    [TestCase]
    [RequireGodotRuntime]
    public void Actor_SetTarget_StartsMoving()
    {
        var config = MissionConfig.CreateM0TestMission();
        var combat = MissionFactory.BuildSandbox(config);
        var actor = combat.Actors[0];
        
        var startPos = actor.GridPosition;
        var target = new Vector2I(startPos.X + 2, startPos.Y);
        
        combat.IssueMovementOrder(actor.Id, target);
        
        AssertThat(actor.IsMoving).IsTrue();
        AssertThat(actor.TargetPosition).IsEqual(target);
    }
    
    [TestCase]
    [RequireGodotRuntime]
    public void Actor_Movement_ReachesTarget()
    {
        var config = MissionConfig.CreateM0TestMission();
        var combat = MissionFactory.BuildSandbox(config);
        var actor = combat.Actors[0];
        
        // Target must be walkable - (8, 3) is open floor in M0 map
        var target = new Vector2I(8, 3);
        combat.IssueMovementOrder(actor.Id, target);
        combat.TimeSystem.Resume();
        
        // Simulate until arrival (max 10 seconds = 200 ticks)
        for (int i = 0; i < 200 && actor.IsMoving; i++)
        {
            combat.Update(0.05f);
        }
        
        AssertThat(actor.GridPosition).IsEqual(target);
        AssertThat(actor.IsMoving).IsFalse();
    }
    
    [TestCase]
    [RequireGodotRuntime]
    public void Actor_Movement_StopsAtWall()
    {
        var template = new string[]
        {
            "#####",
            "#E..#",
            "#####",
            "#####",
            "#####"
        };
        
        var map = MapBuilder.BuildFromTemplate(template);
        var combat = new CombatState();
        combat.MapState = map;
        
        var actor = combat.AddActor("crew", new Vector2I(1, 1));
        
        // Try to move into a wall
        combat.IssueMovementOrder(actor.Id, new Vector2I(1, 2));
        
        // Should not start moving (wall is not walkable)
        AssertThat(actor.IsMoving).IsFalse();
    }
    
    [TestCase]
    [RequireGodotRuntime]
    public void TimeSystem_Pause_StopsSimulation()
    {
        var config = MissionConfig.CreateM0TestMission();
        var combat = MissionFactory.BuildSandbox(config);
        var actor = combat.Actors[0];
        
        // Target must be walkable - (8, 3) is open floor in M0 map
        var target = new Vector2I(8, 3);
        combat.IssueMovementOrder(actor.Id, target);
        
        // Start paused (default)
        AssertThat(combat.TimeSystem.IsPaused).IsTrue();
        
        var startPos = actor.GridPosition;
        
        // Update while paused - should not move
        for (int i = 0; i < 50; i++)
        {
            combat.Update(0.05f);
        }
        
        AssertThat(actor.GridPosition).IsEqual(startPos);
        
        // Resume and update - should move
        combat.TimeSystem.Resume();
        for (int i = 0; i < 50; i++)
        {
            combat.Update(0.05f);
        }
        
        AssertThat(actor.GridPosition).IsNotEqual(startPos);
    }
}
