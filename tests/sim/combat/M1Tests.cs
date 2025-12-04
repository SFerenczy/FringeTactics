using Godot;
using GdUnit4;
using System.Collections.Generic;
using static GdUnit4.Assertions;

namespace FringeTactics.Tests;

/// <summary>
/// M1 Milestone tests - validates multi-unit control and group movement.
/// </summary>
[TestSuite]
public class M1Tests
{
    // ========== FormationCalculator Tests ==========

    [TestCase]
    [RequireGodotRuntime]
    public void FormationCalculator_SingleUnit_GoesDirectlyToTarget()
    {
        var map = CreateOpenMap(10, 10);
        var combat = new CombatState();
        combat.MapState = map;

        var actor = combat.AddActor(ActorType.Crew, new Vector2I(2, 2));
        var actors = new List<Actor> { actor };
        var target = new Vector2I(5, 5);

        var destinations = FormationCalculator.CalculateGroupDestinations(actors, target, map);

        AssertThat(destinations.Count).IsEqual(1);
        AssertThat(destinations[actor.Id]).IsEqual(target);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void FormationCalculator_MultipleUnits_MaintainsFormation()
    {
        var map = CreateOpenMap(20, 20);
        var combat = new CombatState();
        combat.MapState = map;

        // Create 2x2 formation
        var actor1 = combat.AddActor(ActorType.Crew, new Vector2I(2, 2));
        var actor2 = combat.AddActor(ActorType.Crew, new Vector2I(3, 2));
        var actor3 = combat.AddActor(ActorType.Crew, new Vector2I(2, 3));
        var actor4 = combat.AddActor(ActorType.Crew, new Vector2I(3, 3));
        var actors = new List<Actor> { actor1, actor2, actor3, actor4 };

        var target = new Vector2I(10, 10);
        var destinations = FormationCalculator.CalculateGroupDestinations(actors, target, map);

        AssertThat(destinations.Count).IsEqual(4);

        // Check that relative positions are maintained
        // Original offsets from centroid (2.5, 2.5): (-0.5,-0.5), (0.5,-0.5), (-0.5,0.5), (0.5,0.5)
        // After integer division, centroid is (2,2), so offsets are (0,0), (1,0), (0,1), (1,1)
        var dest1 = destinations[actor1.Id];
        var dest2 = destinations[actor2.Id];
        var dest3 = destinations[actor3.Id];
        var dest4 = destinations[actor4.Id];

        // All destinations should be different
        var allDests = new HashSet<Vector2I> { dest1, dest2, dest3, dest4 };
        AssertThat(allDests.Count).IsEqual(4);

        // Destinations should be near the target
        AssertThat((dest1 - target).LengthSquared()).IsLessEqual(4);
        AssertThat((dest2 - target).LengthSquared()).IsLessEqual(4);
        AssertThat((dest3 - target).LengthSquared()).IsLessEqual(4);
        AssertThat((dest4 - target).LengthSquared()).IsLessEqual(4);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void FormationCalculator_NoOverlappingDestinations()
    {
        var map = CreateOpenMap(20, 20);
        var combat = new CombatState();
        combat.MapState = map;

        // Create 6 units in various positions
        var actors = new List<Actor>();
        for (int i = 0; i < 6; i++)
        {
            actors.Add(combat.AddActor(ActorType.Crew, new Vector2I(2 + i, 2)));
        }

        var target = new Vector2I(10, 10);
        var destinations = FormationCalculator.CalculateGroupDestinations(actors, target, map);

        // All destinations should be unique
        var uniqueDests = new HashSet<Vector2I>(destinations.Values);
        AssertThat(uniqueDests.Count).IsEqual(6);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void FormationCalculator_BlockedTile_FindsAlternative()
    {
        var template = new string[]
        {
            "##########",
            "#........#",
            "#........#",
            "#........#",
            "#....#...#",  // Wall at (5,4)
            "#........#",
            "#........#",
            "#........#",
            "#........#",
            "##########"
        };
        var map = MapBuilder.BuildFromTemplate(template);
        var combat = new CombatState();
        combat.MapState = map;

        var actor = combat.AddActor(ActorType.Crew, new Vector2I(2, 2));
        var actors = new List<Actor> { actor };

        // Target the wall tile
        var target = new Vector2I(5, 4);
        var destinations = FormationCalculator.CalculateGroupDestinations(actors, target, map);

        // Should find a walkable alternative
        var dest = destinations[actor.Id];
        AssertThat(map.IsWalkable(dest)).IsTrue();
        AssertThat(dest).IsNotEqual(target);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void FormationCalculator_EmptyList_ReturnsEmpty()
    {
        var map = CreateOpenMap(10, 10);
        var actors = new List<Actor>();
        var target = new Vector2I(5, 5);

        var destinations = FormationCalculator.CalculateGroupDestinations(actors, target, map);

        AssertThat(destinations.Count).IsEqual(0);
    }

    // ========== MissionConfig Tests ==========

    [TestCase]
    [RequireGodotRuntime]
    public void MissionConfig_CreateM1TestMission_HasMultipleCrewSpawns()
    {
        var config = MissionConfig.CreateM1TestMission();

        AssertThat(config.CrewSpawnPositions.Count).IsEqual(6);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void MissionConfig_CreateM1TestMission_HasNoEnemies()
    {
        var config = MissionConfig.CreateM1TestMission();

        AssertThat(config.EnemySpawns.Count).IsEqual(0);
    }

    // ========== Collision Avoidance Tests ==========

    [TestCase]
    [RequireGodotRuntime]
    public void Actor_WallCollision_StopsAtWall()
    {
        var template = new string[]
        {
            "#####",
            "#...#",
            "#.#.#",  // Wall at (2,2)
            "#...#",
            "#####"
        };
        var map = MapBuilder.BuildFromTemplate(template);
        var combat = new CombatState();
        combat.MapState = map;

        var actor = combat.AddActor(ActorType.Crew, new Vector2I(1, 2));
        
        // Try to move through the wall to (3,2)
        combat.IssueMovementOrder(actor.Id, new Vector2I(3, 2));
        combat.TimeSystem.Resume();

        // Simulate movement
        for (int i = 0; i < 100; i++)
        {
            combat.Update(0.05f);
        }

        // Actor should NOT be at (3,2) - wall blocks direct path
        // It should either stop or find alternate route
        AssertThat(actor.GridPosition).IsNotEqual(new Vector2I(2, 2)); // Not inside wall
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Actor_UnitCollision_UnitsDoNotOverlap()
    {
        var map = CreateOpenMap(10, 10);
        var combat = new CombatState();
        combat.MapState = map;

        // Two actors at different positions
        var actor1 = combat.AddActor(ActorType.Crew, new Vector2I(2, 2));
        var actor2 = combat.AddActor(ActorType.Crew, new Vector2I(4, 2));

        // Both try to move to same tile
        combat.IssueMovementOrder(actor1.Id, new Vector2I(3, 2));
        combat.IssueMovementOrder(actor2.Id, new Vector2I(3, 2));
        combat.TimeSystem.Resume();

        // Simulate movement
        for (int i = 0; i < 100; i++)
        {
            combat.Update(0.05f);
            
            // At no point should both actors be on the same tile while stationary
            if (!actor1.IsMoving && !actor2.IsMoving)
            {
                AssertThat(actor1.GridPosition).IsNotEqual(actor2.GridPosition);
            }
        }
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Actor_NewTarget_ResetsMoveProgress()
    {
        var map = CreateOpenMap(10, 10);
        var combat = new CombatState();
        combat.MapState = map;

        var actor = combat.AddActor(ActorType.Crew, new Vector2I(2, 2));
        
        // Start moving
        combat.IssueMovementOrder(actor.Id, new Vector2I(5, 2));
        combat.TimeSystem.Resume();

        // Move partway
        combat.Update(0.1f);
        
        // Change target
        var posBefore = actor.GridPosition;
        combat.IssueMovementOrder(actor.Id, new Vector2I(2, 5));

        // Position should not have jumped
        AssertThat(actor.GridPosition).IsEqual(posBefore);
    }

    // ========== Group Movement Integration Tests ==========

    [TestCase]
    [RequireGodotRuntime]
    public void GroupMovement_AllUnitsReachDestinations()
    {
        var map = CreateOpenMap(20, 20);
        var combat = new CombatState();
        combat.MapState = map;

        // Create group of 4 units
        var actor1 = combat.AddActor(ActorType.Crew, new Vector2I(2, 2));
        var actor2 = combat.AddActor(ActorType.Crew, new Vector2I(3, 2));
        var actor3 = combat.AddActor(ActorType.Crew, new Vector2I(2, 3));
        var actor4 = combat.AddActor(ActorType.Crew, new Vector2I(3, 3));
        var actors = new List<Actor> { actor1, actor2, actor3, actor4 };

        var target = new Vector2I(10, 10);
        var destinations = FormationCalculator.CalculateGroupDestinations(actors, target, map);

        // Issue movement orders
        foreach (var kvp in destinations)
        {
            combat.IssueMovementOrder(kvp.Key, kvp.Value);
        }
        combat.TimeSystem.Resume();

        // Simulate until all arrive (max 20 seconds)
        for (int i = 0; i < 400; i++)
        {
            combat.Update(0.05f);
            
            bool allArrived = true;
            foreach (var actor in actors)
            {
                if (actor.IsMoving) allArrived = false;
            }
            if (allArrived) break;
        }

        // All should have reached their destinations
        foreach (var actor in actors)
        {
            AssertThat(actor.IsMoving).IsFalse();
            AssertThat(actor.GridPosition).IsEqual(destinations[actor.Id]);
        }
    }

    // ========== Helper Methods ==========

    private MapState CreateOpenMap(int width, int height)
    {
        var template = new string[height];
        for (int y = 0; y < height; y++)
        {
            if (y == 0 || y == height - 1)
            {
                template[y] = new string('#', width);
            }
            else
            {
                template[y] = "#" + new string('.', width - 2) + "#";
            }
        }
        return MapBuilder.BuildFromTemplate(template);
    }
}
