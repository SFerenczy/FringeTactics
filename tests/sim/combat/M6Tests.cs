using Godot;
using GdUnit4;
using static GdUnit4.Assertions;

namespace FringeTactics.Tests;

/// <summary>
/// Tests for M6: Stealth & Alarm Foundations
/// </summary>
[TestSuite]
public class M6Tests
{
    // === Detection State Tests ===
    
    [TestCase]
    public void EnemyPerception_StartsIdle()
    {
        var perception = new EnemyPerception(1);
        
        AssertThat(perception.State).IsEqual(DetectionState.Idle);
        AssertThat(perception.HasLastKnownPositions).IsFalse();
    }
    
    [TestCase]
    public void EnemyPerception_TransitionsToAlerted()
    {
        var perception = new EnemyPerception(1);
        var stateChanged = false;
        perception.StateChanged += (p, old, newState) => stateChanged = true;
        
        perception.SetState(DetectionState.Alerted, 100);
        
        AssertThat(perception.State).IsEqual(DetectionState.Alerted);
        AssertThat(stateChanged).IsTrue();
        AssertThat(perception.StateChangedTick).IsEqual(100);
    }
    
    [TestCase]
    public void EnemyPerception_NoEventOnSameState()
    {
        var perception = new EnemyPerception(1);
        var eventCount = 0;
        perception.StateChanged += (p, old, newState) => eventCount++;
        
        perception.SetState(DetectionState.Idle, 100);
        
        AssertThat(eventCount).IsEqual(0);
    }
    
    [TestCase]
    [RequireGodotRuntime]
    public void EnemyPerception_TracksLastKnownPosition()
    {
        var perception = new EnemyPerception(1);
        var crewPos = new Vector2I(5, 5);
        
        perception.UpdateLastKnown(10, crewPos);
        
        AssertThat(perception.HasLastKnownPositions).IsTrue();
        AssertThat(perception.LastKnownPositions[10]).IsEqual(crewPos);
    }
    
    [TestCase]
    [RequireGodotRuntime]
    public void EnemyPerception_ClearsLastKnownPosition()
    {
        var perception = new EnemyPerception(1);
        perception.UpdateLastKnown(10, new Vector2I(5, 5));
        
        perception.ClearLastKnown(10);
        
        AssertThat(perception.HasLastKnownPositions).IsFalse();
    }
    
    [TestCase]
    [RequireGodotRuntime]
    public void EnemyPerception_TracksMultipleCrewPositions()
    {
        var perception = new EnemyPerception(1);
        
        perception.UpdateLastKnown(10, new Vector2I(5, 5));
        perception.UpdateLastKnown(11, new Vector2I(7, 7));
        
        AssertThat(perception.LastKnownPositions.Count).IsEqual(2);
        AssertThat(perception.LastKnownPositions[10]).IsEqual(new Vector2I(5, 5));
        AssertThat(perception.LastKnownPositions[11]).IsEqual(new Vector2I(7, 7));
    }
    
    // === Perception System Tests ===
    
    [TestCase]
    [RequireGodotRuntime]
    public void PerceptionSystem_StartsQuiet()
    {
        var combat = CreateTestCombat();
        
        AssertThat(combat.Perception.AlarmState).IsEqual(AlarmState.Quiet);
    }
    
    [TestCase]
    [RequireGodotRuntime]
    public void PerceptionSystem_InitializesEnemyPerceptions()
    {
        var combat = CreateTestCombat();
        var enemy = combat.AddActor(ActorType.Enemy, new Vector2I(5, 5));
        combat.Perception.Initialize();
        
        var perception = combat.Perception.GetPerception(enemy.Id);
        
        AssertThat(perception).IsNotNull();
        AssertThat(perception.State).IsEqual(DetectionState.Idle);
    }
    
    [TestCase]
    [RequireGodotRuntime]
    public void PerceptionSystem_DetectsCrewInLOS()
    {
        var combat = CreateTestCombat();
        var enemy = combat.AddActor(ActorType.Enemy, new Vector2I(5, 5));
        var crew = combat.AddActor(ActorType.Crew, new Vector2I(5, 3));
        combat.Perception.Initialize();
        
        combat.Perception.Tick();
        
        var perception = combat.Perception.GetPerception(enemy.Id);
        AssertThat(perception.State).IsEqual(DetectionState.Alerted);
    }
    
    [TestCase]
    [RequireGodotRuntime]
    public void PerceptionSystem_DoesNotDetectCrewBehindWall()
    {
        var template = new string[]
        {
            "#######",
            "#.....#",
            "#..#..#",
            "#.....#",
            "#######"
        };
        var map = MapBuilder.BuildFromTemplate(template);
        var combat = new CombatState(12345);
        combat.MapState = map;
        combat.InitializeVisibility();
        
        var enemy = combat.AddActor(ActorType.Enemy, new Vector2I(2, 2));
        var crew = combat.AddActor(ActorType.Crew, new Vector2I(4, 2));
        combat.Perception.Initialize();
        
        combat.Perception.Tick();
        
        var perception = combat.Perception.GetPerception(enemy.Id);
        AssertThat(perception.State).IsEqual(DetectionState.Idle);
    }
    
    [TestCase]
    [RequireGodotRuntime]
    public void PerceptionSystem_DoesNotDetectCrewOutOfRange()
    {
        var combat = CreateTestCombat();
        var enemy = combat.AddActor(ActorType.Enemy, new Vector2I(5, 5));
        var crew = combat.AddActor(ActorType.Crew, new Vector2I(5, 18));
        combat.Perception.Initialize();
        
        combat.Perception.Tick();
        
        var perception = combat.Perception.GetPerception(enemy.Id);
        AssertThat(perception.State).IsEqual(DetectionState.Idle);
    }
    
    [TestCase]
    [RequireGodotRuntime]
    public void PerceptionSystem_UpdatesLastKnownOnDetection()
    {
        var combat = CreateTestCombat();
        var enemy = combat.AddActor(ActorType.Enemy, new Vector2I(5, 5));
        var crew = combat.AddActor(ActorType.Crew, new Vector2I(5, 3));
        combat.Perception.Initialize();
        
        combat.Perception.Tick();
        
        var perception = combat.Perception.GetPerception(enemy.Id);
        AssertThat(perception.LastKnownPositions[crew.Id]).IsEqual(new Vector2I(5, 3));
    }
    
    [TestCase]
    [RequireGodotRuntime]
    public void PerceptionSystem_FiresDetectionEvent()
    {
        var combat = CreateTestCombat();
        var enemy = combat.AddActor(ActorType.Enemy, new Vector2I(5, 5));
        var crew = combat.AddActor(ActorType.Crew, new Vector2I(5, 3));
        combat.Perception.Initialize();
        
        Actor detectedEnemy = null;
        Actor detectedCrew = null;
        combat.Perception.EnemyDetectedCrew += (e, c) => { detectedEnemy = e; detectedCrew = c; };
        
        combat.Perception.Tick();
        
        AssertThat(detectedEnemy).IsEqual(enemy);
        AssertThat(detectedCrew).IsEqual(crew);
    }
    
    // === Alarm State Tests ===
    
    [TestCase]
    [RequireGodotRuntime]
    public void AlarmState_BecomesAlertedOnDetection()
    {
        var combat = CreateTestCombat();
        var enemy = combat.AddActor(ActorType.Enemy, new Vector2I(5, 5));
        var crew = combat.AddActor(ActorType.Crew, new Vector2I(5, 3));
        combat.Perception.Initialize();
        
        AlarmState? newAlarmState = null;
        combat.Perception.AlarmStateChanged += (old, newState) => newAlarmState = newState;
        
        combat.Perception.Tick();
        
        AssertThat(combat.Perception.AlarmState).IsEqual(AlarmState.Alerted);
        AssertThat(newAlarmState).IsEqual(AlarmState.Alerted);
    }
    
    [TestCase]
    [RequireGodotRuntime]
    public void AlarmState_OnlyFiresOnceForMultipleDetections()
    {
        var combat = CreateTestCombat();
        var enemy1 = combat.AddActor(ActorType.Enemy, new Vector2I(5, 5));
        var enemy2 = combat.AddActor(ActorType.Enemy, new Vector2I(7, 5));
        var crew = combat.AddActor(ActorType.Crew, new Vector2I(6, 5));
        combat.Perception.Initialize();
        
        int alarmChanges = 0;
        combat.Perception.AlarmStateChanged += (old, newState) => alarmChanges++;
        
        combat.Perception.Tick();
        
        AssertThat(alarmChanges).IsEqual(1);
    }
    
    [TestCase]
    [RequireGodotRuntime]
    public void AlarmState_StaysQuietWithNoDetection()
    {
        var combat = CreateTestCombat();
        var enemy = combat.AddActor(ActorType.Enemy, new Vector2I(5, 5));
        var crew = combat.AddActor(ActorType.Crew, new Vector2I(15, 15));
        combat.Perception.Initialize();
        
        combat.Perception.Tick();
        
        AssertThat(combat.Perception.AlarmState).IsEqual(AlarmState.Quiet);
    }
    
    // === Manual Alert Tests ===
    
    [TestCase]
    [RequireGodotRuntime]
    public void AlertEnemy_ManuallyAlertsSpecificEnemy()
    {
        var combat = CreateTestCombat();
        var enemy = combat.AddActor(ActorType.Enemy, new Vector2I(5, 5));
        combat.Perception.Initialize();
        
        AssertThat(combat.Perception.GetDetectionState(enemy.Id)).IsEqual(DetectionState.Idle);
        
        combat.Perception.AlertEnemy(enemy.Id);
        
        AssertThat(combat.Perception.GetDetectionState(enemy.Id)).IsEqual(DetectionState.Alerted);
    }
    
    [TestCase]
    [RequireGodotRuntime]
    public void AlertEnemy_TriggersAlarmState()
    {
        var combat = CreateTestCombat();
        var enemy = combat.AddActor(ActorType.Enemy, new Vector2I(5, 5));
        combat.Perception.Initialize();
        
        combat.Perception.AlertEnemy(enemy.Id);
        
        AssertThat(combat.Perception.AlarmState).IsEqual(AlarmState.Alerted);
    }
    
    [TestCase]
    [RequireGodotRuntime]
    public void AlertAllEnemies_AlertsEveryEnemy()
    {
        var combat = CreateTestCombat();
        var enemy1 = combat.AddActor(ActorType.Enemy, new Vector2I(5, 5));
        var enemy2 = combat.AddActor(ActorType.Enemy, new Vector2I(10, 10));
        combat.Perception.Initialize();
        
        combat.Perception.AlertAllEnemies();
        
        AssertThat(combat.Perception.GetDetectionState(enemy1.Id)).IsEqual(DetectionState.Alerted);
        AssertThat(combat.Perception.GetDetectionState(enemy2.Id)).IsEqual(DetectionState.Alerted);
    }
    
    [TestCase]
    [RequireGodotRuntime]
    public void IsEnemyAlerted_ReturnsCorrectState()
    {
        var combat = CreateTestCombat();
        var enemy = combat.AddActor(ActorType.Enemy, new Vector2I(5, 5));
        combat.Perception.Initialize();
        
        AssertThat(combat.Perception.IsEnemyAlerted(enemy.Id)).IsFalse();
        
        combat.Perception.AlertEnemy(enemy.Id);
        
        AssertThat(combat.Perception.IsEnemyAlerted(enemy.Id)).IsTrue();
    }
    
    [TestCase]
    [RequireGodotRuntime]
    public void RemoveEnemy_CleansUpPerception()
    {
        var combat = CreateTestCombat();
        var enemy = combat.AddActor(ActorType.Enemy, new Vector2I(5, 5));
        combat.Perception.Initialize();
        combat.Perception.AlertEnemy(enemy.Id);
        
        combat.Perception.RemoveEnemy(enemy.Id);
        
        AssertThat(combat.Perception.GetDetectionState(enemy.Id)).IsEqual(DetectionState.Idle);
    }
    
    // === AI Behavior Tests ===
    
    [TestCase]
    [RequireGodotRuntime]
    public void IdleEnemy_DoesNotAttack()
    {
        var combat = CreateTestCombat();
        var enemy = combat.AddActor(ActorType.Enemy, new Vector2I(5, 5));
        var crew = combat.AddActor(ActorType.Crew, new Vector2I(15, 15));
        combat.InitializePerception();
        combat.TimeSystem.Resume();
        
        // Verify enemy is idle (crew out of LOS/range)
        AssertThat(combat.Perception.GetDetectionState(enemy.Id)).IsEqual(DetectionState.Idle);
        
        // Run several ticks - AI should not set attack target
        for (int i = 0; i < 20; i++)
        {
            combat.Update(0.05f);
        }
        
        AssertThat(enemy.AttackTargetId).IsNull();
    }
    
    [TestCase]
    [RequireGodotRuntime]
    public void IdleEnemy_DoesNotMove()
    {
        var combat = CreateTestCombat();
        var enemyPos = new Vector2I(5, 5);
        var enemy = combat.AddActor(ActorType.Enemy, enemyPos);
        var crew = combat.AddActor(ActorType.Crew, new Vector2I(15, 15));
        combat.InitializePerception();
        combat.TimeSystem.Resume();
        
        // Verify enemy is idle
        AssertThat(combat.Perception.GetDetectionState(enemy.Id)).IsEqual(DetectionState.Idle);
        
        // Run several ticks
        for (int i = 0; i < 20; i++)
        {
            combat.Update(0.05f);
        }
        
        // Enemy should not have moved
        AssertThat(enemy.GridPosition).IsEqual(enemyPos);
        AssertThat(enemy.IsMoving).IsFalse();
    }
    
    [TestCase]
    [RequireGodotRuntime]
    public void AlertedEnemy_AttacksVisibleCrew()
    {
        var combat = CreateTestCombat();
        var enemy = combat.AddActor(ActorType.Enemy, new Vector2I(5, 5));
        var crew = combat.AddActor(ActorType.Crew, new Vector2I(5, 3));
        combat.InitializePerception();
        combat.TimeSystem.Resume();
        
        // Run several ticks to allow detection and AI response
        for (int i = 0; i < 20; i++)
        {
            combat.Update(0.05f);
        }
        
        // Enemy should be alerted and attacking
        AssertThat(combat.Perception.GetDetectionState(enemy.Id)).IsEqual(DetectionState.Alerted);
        AssertThat(enemy.AttackTargetId.HasValue).IsTrue();
    }
    
    [TestCase]
    [RequireGodotRuntime]
    public void AlertedEnemy_MovesTowardCrew()
    {
        // Create a larger map so we can place crew out of weapon range
        var combat = new CombatState(12345);
        var map = MapBuilder.BuildTestMap(new Vector2I(30, 30));
        combat.MapState = map;
        combat.InitializeVisibility();
        
        var enemyPos = new Vector2I(15, 15);
        var enemy = combat.AddActor(ActorType.Enemy, enemyPos);
        // Give enemy a short-range weapon so crew is out of range
        enemy.EquippedWeapon = new WeaponData { Range = 3, Damage = 25, CooldownTicks = 10 };
        // Place crew visible but out of weapon range (distance = 10)
        var crew = combat.AddActor(ActorType.Crew, new Vector2I(15, 5));
        combat.InitializePerception();
        combat.TimeSystem.Resume();
        
        // Manually alert the enemy
        combat.Perception.AlertEnemy(enemy.Id);
        
        // Run several ticks (enough for AI to think and actor to move)
        for (int i = 0; i < 40; i++)
        {
            combat.Update(0.05f);
        }
        
        // Enemy should have moved toward crew (Y decreased)
        AssertThat(enemy.GridPosition.Y).IsLess(enemyPos.Y);
    }
    
    [TestCase]
    [RequireGodotRuntime]
    public void PerceptionTicksInCombatLoop()
    {
        var combat = CreateTestCombat();
        var enemy = combat.AddActor(ActorType.Enemy, new Vector2I(5, 5));
        var crew = combat.AddActor(ActorType.Crew, new Vector2I(5, 3));
        combat.InitializePerception();
        combat.TimeSystem.Resume();
        
        // Initially idle
        AssertThat(combat.Perception.GetDetectionState(enemy.Id)).IsEqual(DetectionState.Idle);
        
        // Run one tick via Update (not manual Perception.Tick)
        combat.Update(0.05f);
        
        // Should now be alerted (perception runs in ProcessTick)
        AssertThat(combat.Perception.GetDetectionState(enemy.Id)).IsEqual(DetectionState.Alerted);
    }
    
    // === Door/LOS Integration Tests ===
    
    [TestCase]
    [RequireGodotRuntime]
    public void ClosedDoor_BlocksDetection()
    {
        // Create map with a closed door between enemy and crew
        var combat = new CombatState(12345);
        var template = new string[]
        {
            "##########",
            "#....D...#",
            "#........#",
            "##########"
        };
        combat.MapState = MapBuilder.BuildFromTemplate(template, combat.Interactions);
        combat.InitializeVisibility();
        
        // Enemy on left side of door
        var enemy = combat.AddActor(ActorType.Enemy, new Vector2I(3, 1));
        // Crew on right side of door
        var crew = combat.AddActor(ActorType.Crew, new Vector2I(7, 1));
        combat.InitializePerception();
        
        // Run perception tick
        combat.Perception.Tick();
        
        // Door is closed - should block LOS, enemy stays idle
        AssertThat(combat.Perception.GetDetectionState(enemy.Id)).IsEqual(DetectionState.Idle);
    }
    
    [TestCase]
    [RequireGodotRuntime]
    public void OpenDoor_AllowsDetection()
    {
        // Create map with a door between enemy and crew
        var combat = new CombatState(12345);
        var template = new string[]
        {
            "##########",
            "#....D...#",
            "#........#",
            "##########"
        };
        combat.MapState = MapBuilder.BuildFromTemplate(template, combat.Interactions);
        combat.InitializeVisibility();
        
        // Open the door
        var door = combat.Interactions.GetInteractableAt(new Vector2I(5, 1));
        AssertThat(door).IsNotNull();
        door.SetState(InteractableState.DoorOpen);
        
        // Enemy on left side of door
        var enemy = combat.AddActor(ActorType.Enemy, new Vector2I(3, 1));
        // Crew on right side of door (within vision range)
        var crew = combat.AddActor(ActorType.Crew, new Vector2I(7, 1));
        combat.InitializePerception();
        
        // Run perception tick
        combat.Perception.Tick();
        
        // Door is open - should allow LOS, enemy becomes alerted
        AssertThat(combat.Perception.GetDetectionState(enemy.Id)).IsEqual(DetectionState.Alerted);
    }
    
    [TestCase]
    [RequireGodotRuntime]
    public void OpeningDoor_TriggersDetection()
    {
        // Create map with a closed door
        var combat = new CombatState(12345);
        var template = new string[]
        {
            "##########",
            "#....D...#",
            "#........#",
            "##########"
        };
        combat.MapState = MapBuilder.BuildFromTemplate(template, combat.Interactions);
        combat.InitializeVisibility();
        
        var enemy = combat.AddActor(ActorType.Enemy, new Vector2I(3, 1));
        var crew = combat.AddActor(ActorType.Crew, new Vector2I(7, 1));
        combat.InitializePerception();
        combat.TimeSystem.Resume();
        
        // Initially idle (door closed)
        combat.Perception.Tick();
        AssertThat(combat.Perception.GetDetectionState(enemy.Id)).IsEqual(DetectionState.Idle);
        
        // Open the door
        var door = combat.Interactions.GetInteractableAt(new Vector2I(5, 1));
        door.SetState(InteractableState.DoorOpen);
        
        // Run another perception tick
        combat.Perception.Tick();
        
        // Now enemy should detect crew through open door
        AssertThat(combat.Perception.GetDetectionState(enemy.Id)).IsEqual(DetectionState.Alerted);
    }
    
    // === MissionFactory Integration Tests ===
    
    [TestCase]
    [RequireGodotRuntime]
    public void MissionFactory_InitializesPerception()
    {
        var config = MissionConfig.CreateM6TestMission();
        var combat = MissionFactory.BuildSandbox(config, 12345);
        
        // Perception should be initialized
        AssertThat(combat.Perception).IsNotNull();
        AssertThat(combat.Perception.AlarmState).IsEqual(AlarmState.Quiet);
        
        // All enemies should have perception tracking
        foreach (var actor in combat.Actors)
        {
            if (actor.Type == ActorType.Enemy)
            {
                var state = combat.Perception.GetDetectionState(actor.Id);
                AssertThat(state).IsEqual(DetectionState.Idle);
            }
        }
    }
    
    [TestCase]
    [RequireGodotRuntime]
    public void M6TestMission_EnemiesStartIdle()
    {
        var config = MissionConfig.CreateM6TestMission();
        var combat = MissionFactory.BuildSandbox(config, 12345);
        
        // Count enemies
        int enemyCount = 0;
        foreach (var actor in combat.Actors)
        {
            if (actor.Type == ActorType.Enemy)
            {
                enemyCount++;
                AssertThat(combat.Perception.GetDetectionState(actor.Id)).IsEqual(DetectionState.Idle);
            }
        }
        
        // M6 test mission should have 4 enemies
        AssertThat(enemyCount).IsEqual(4);
    }
    
    [TestCase]
    [RequireGodotRuntime]
    public void M6TestMission_AlarmStartsQuiet()
    {
        var config = MissionConfig.CreateM6TestMission();
        var combat = MissionFactory.BuildSandbox(config, 12345);
        
        AssertThat(combat.Perception.AlarmState).IsEqual(AlarmState.Quiet);
    }
    
    // === Edge Case Tests ===
    
    [TestCase]
    [RequireGodotRuntime]
    public void DeadEnemy_NotTrackedByPerception()
    {
        var combat = CreateTestCombat();
        var enemy = combat.AddActor(ActorType.Enemy, new Vector2I(5, 5));
        var crew = combat.AddActor(ActorType.Crew, new Vector2I(5, 3));
        combat.InitializePerception();
        
        // Kill the enemy
        enemy.TakeDamage(enemy.MaxHp);
        AssertThat(enemy.State).IsEqual(ActorState.Dead);
        
        // Remove from perception
        combat.Perception.RemoveEnemy(enemy.Id);
        
        // Run perception tick - should not crash or alert
        combat.Perception.Tick();
        
        AssertThat(combat.Perception.AlarmState).IsEqual(AlarmState.Quiet);
    }
    
    [TestCase]
    [RequireGodotRuntime]
    public void DeadCrew_NotDetected()
    {
        var combat = CreateTestCombat();
        var enemy = combat.AddActor(ActorType.Enemy, new Vector2I(5, 5));
        var crew = combat.AddActor(ActorType.Crew, new Vector2I(5, 3));
        combat.InitializePerception();
        
        // Kill the crew
        crew.TakeDamage(crew.MaxHp);
        AssertThat(crew.State).IsEqual(ActorState.Dead);
        
        // Run perception tick
        combat.Perception.Tick();
        
        // Enemy should not detect dead crew
        AssertThat(combat.Perception.GetDetectionState(enemy.Id)).IsEqual(DetectionState.Idle);
        AssertThat(combat.Perception.AlarmState).IsEqual(AlarmState.Quiet);
    }
    
    [TestCase]
    [RequireGodotRuntime]
    public void MultipleEnemies_IndependentDetection()
    {
        var combat = CreateTestCombat();
        // Enemy 1 can see crew (close by)
        var enemy1 = combat.AddActor(ActorType.Enemy, new Vector2I(5, 5));
        // Create a wall column to block LOS from enemy2 to crew
        combat.MapState.SetTile(new Vector2I(10, 2), TileType.Wall);
        combat.MapState.SetTile(new Vector2I(10, 3), TileType.Wall);
        combat.MapState.SetTile(new Vector2I(10, 4), TileType.Wall);
        combat.MapState.SetTile(new Vector2I(10, 5), TileType.Wall);
        combat.MapState.SetTile(new Vector2I(10, 6), TileType.Wall);
        // Enemy 2 is behind the wall
        var enemy2 = combat.AddActor(ActorType.Enemy, new Vector2I(15, 5));
        // Crew visible to enemy1 but blocked from enemy2 by wall
        var crew = combat.AddActor(ActorType.Crew, new Vector2I(5, 3));
        combat.InitializePerception();
        
        // Run perception tick
        combat.Perception.Tick();
        
        // Enemy1 should be alerted, enemy2 should still be idle (wall blocks LOS)
        AssertThat(combat.Perception.GetDetectionState(enemy1.Id)).IsEqual(DetectionState.Alerted);
        AssertThat(combat.Perception.GetDetectionState(enemy2.Id)).IsEqual(DetectionState.Idle);
        
        // But alarm should be raised (at least one enemy alerted)
        AssertThat(combat.Perception.AlarmState).IsEqual(AlarmState.Alerted);
    }
    
    [TestCase]
    [RequireGodotRuntime]
    public void LastKnownPosition_UpdatesOnMovement()
    {
        var combat = CreateTestCombat();
        var enemy = combat.AddActor(ActorType.Enemy, new Vector2I(5, 5));
        var crew = combat.AddActor(ActorType.Crew, new Vector2I(5, 3));
        combat.InitializePerception();
        combat.TimeSystem.Resume();
        
        // First detection
        combat.Perception.Tick();
        var perception = combat.Perception.GetPerception(enemy.Id);
        AssertThat(perception.LastKnownPositions[crew.Id]).IsEqual(new Vector2I(5, 3));
        
        // Move crew
        crew.GridPosition = new Vector2I(6, 3);
        
        // Run another tick
        combat.Perception.Tick();
        
        // Last known position should update
        AssertThat(perception.LastKnownPositions[crew.Id]).IsEqual(new Vector2I(6, 3));
    }
    
    // === Helper Methods ===
    
    private CombatState CreateTestCombat()
    {
        var combat = new CombatState(12345);
        var map = MapBuilder.BuildTestMap(new Vector2I(20, 20));
        combat.MapState = map;
        combat.InitializeVisibility();
        return combat;
    }
}
