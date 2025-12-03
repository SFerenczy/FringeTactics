using Godot;
using GdUnit4;
using static GdUnit4.Assertions;

namespace FringeTactics.Tests;

/// <summary>
/// Tests for M5: Interactables & Channeled Hacking
/// </summary>
[TestSuite]
public class M5Tests
{
    // === Interactable Creation ===
    
    [TestCase]
    public void InteractionSystem_AddInteractable_CreatesWithCorrectState()
    {
        var combat = new CombatState(12345);
        var pos = new Vector2I(5, 5);
        
        var door = combat.Interactions.AddInteractable(InteractableTypes.Door, pos);
        
        AssertThat(door).IsNotNull();
        AssertThat(door.Type).IsEqual(InteractableTypes.Door);
        AssertThat(door.Position).IsEqual(pos);
        AssertThat(door.State).IsEqual(InteractableState.DoorClosed);
    }
    
    [TestCase]
    public void InteractionSystem_AddLockedDoor_StartsLocked()
    {
        var combat = new CombatState(12345);
        var pos = new Vector2I(5, 5);
        
        var door = combat.Interactions.AddInteractable(InteractableTypes.Door, pos,
            new System.Collections.Generic.Dictionary<string, object> { { "hackDifficulty", 40 } });
        door.SetState(InteractableState.DoorLocked);
        
        AssertThat(door.State).IsEqual(InteractableState.DoorLocked);
        AssertThat(door.IsDoorLocked).IsTrue();
    }
    
    [TestCase]
    public void InteractionSystem_GetInteractableAt_FindsByPosition()
    {
        var combat = new CombatState(12345);
        var pos = new Vector2I(5, 5);
        
        var door = combat.Interactions.AddInteractable(InteractableTypes.Door, pos);
        var found = combat.Interactions.GetInteractableAt(pos);
        
        AssertThat(found).IsNotNull();
        AssertThat(found.Id).IsEqual(door.Id);
    }
    
    [TestCase]
    [RequireGodotRuntime]
    public void InteractionSystem_GetInteractableAt_ReturnsNullForEmpty()
    {
        var combat = new CombatState(12345);
        
        var found = combat.Interactions.GetInteractableAt(new Vector2I(5, 5));
        
        AssertThat(found).IsNull();
    }
    
    // === Door Behavior ===
    
    [TestCase]
    public void Door_BlocksMovement_WhenClosed()
    {
        var combat = new CombatState(12345);
        var map = MapBuilder.BuildTestMap(new Vector2I(10, 10));
        combat.MapState = map;
        map.SetInteractionSystem(combat.Interactions);
        
        var doorPos = new Vector2I(5, 5);
        var door = combat.Interactions.AddInteractable(InteractableTypes.Door, doorPos);
        
        AssertThat(door.State).IsEqual(InteractableState.DoorClosed);
        AssertThat(map.IsWalkable(doorPos)).IsFalse();
    }
    
    [TestCase]
    public void Door_AllowsMovement_WhenOpen()
    {
        var combat = new CombatState(12345);
        var map = MapBuilder.BuildTestMap(new Vector2I(10, 10));
        combat.MapState = map;
        map.SetInteractionSystem(combat.Interactions);
        
        var doorPos = new Vector2I(5, 5);
        var door = combat.Interactions.AddInteractable(InteractableTypes.Door, doorPos);
        door.SetState(InteractableState.DoorOpen);
        
        AssertThat(map.IsWalkable(doorPos)).IsTrue();
    }
    
    [TestCase]
    public void Door_BlocksLOS_WhenClosed()
    {
        var combat = new CombatState(12345);
        var map = MapBuilder.BuildTestMap(new Vector2I(10, 10));
        combat.MapState = map;
        map.SetInteractionSystem(combat.Interactions);
        
        var doorPos = new Vector2I(5, 5);
        var door = combat.Interactions.AddInteractable(InteractableTypes.Door, doorPos);
        
        AssertThat(door.State).IsEqual(InteractableState.DoorClosed);
        AssertThat(map.BlocksLOS(doorPos)).IsTrue();
    }
    
    [TestCase]
    public void Door_AllowsLOS_WhenOpen()
    {
        var combat = new CombatState(12345);
        var map = MapBuilder.BuildTestMap(new Vector2I(10, 10));
        combat.MapState = map;
        map.SetInteractionSystem(combat.Interactions);
        
        var doorPos = new Vector2I(5, 5);
        var door = combat.Interactions.AddInteractable(InteractableTypes.Door, doorPos);
        door.SetState(InteractableState.DoorOpen);
        
        AssertThat(map.BlocksLOS(doorPos)).IsFalse();
    }
    
    [TestCase]
    public void Door_OpenClose_InstantInteraction()
    {
        var combat = new CombatState(12345);
        var map = MapBuilder.BuildTestMap(new Vector2I(10, 10));
        combat.MapState = map;
        map.SetInteractionSystem(combat.Interactions);
        
        var doorPos = new Vector2I(5, 5);
        var door = combat.Interactions.AddInteractable(InteractableTypes.Door, doorPos);
        var actor = combat.AddActor("crew", new Vector2I(5, 4));
        
        // Door starts closed
        AssertThat(door.State).IsEqual(InteractableState.DoorClosed);
        
        // Open door (instant)
        combat.Interactions.ExecuteInteraction(actor, door, "open");
        AssertThat(door.State).IsEqual(InteractableState.DoorOpen);
        
        // Close door (instant)
        combat.Interactions.ExecuteInteraction(actor, door, "close");
        AssertThat(door.State).IsEqual(InteractableState.DoorClosed);
    }
    
    [TestCase]
    public void LockedDoor_RequiresChanneledHack()
    {
        var combat = new CombatState(12345);
        var map = MapBuilder.BuildTestMap(new Vector2I(10, 10));
        combat.MapState = map;
        map.SetInteractionSystem(combat.Interactions);
        
        var doorPos = new Vector2I(5, 5);
        var door = combat.Interactions.AddInteractable(InteractableTypes.Door, doorPos,
            new System.Collections.Generic.Dictionary<string, object> { { "hackDifficulty", 40 } });
        door.SetState(InteractableState.DoorLocked);
        var actor = combat.AddActor("crew", new Vector2I(5, 4));
        
        // Execute interaction - should start channeling
        combat.Interactions.ExecuteInteraction(actor, door, "hack");
        
        AssertThat(actor.IsChanneling).IsTrue();
        AssertThat(actor.CurrentChannel).IsNotNull();
        AssertThat(actor.CurrentChannel.ActionType).IsEqual(ChannelTypes.Unlock);
    }
    
    // === Channeled Actions ===
    
    [TestCase]
    public void ChanneledAction_Progress_IncreasesOverTime()
    {
        var channel = new ChanneledAction(ChannelTypes.Hack, 1, 100);
        
        AssertThat(channel.Progress).IsEqual(0f);
        
        channel.Tick();
        AssertThat(channel.TicksRemaining).IsEqual(99);
        AssertThat(channel.Progress).IsGreater(0f);
    }
    
    [TestCase]
    public void ChanneledAction_Completes_AfterDuration()
    {
        var duration = 10;
        var channel = new ChanneledAction(ChannelTypes.Hack, 1, duration);
        
        for (int i = 0; i < duration; i++)
        {
            AssertThat(channel.IsComplete).IsFalse();
            channel.Tick();
        }
        
        AssertThat(channel.IsComplete).IsTrue();
    }
    
    [TestCase]
    public void ChanneledAction_Interrupted_ByMovement()
    {
        var combat = new CombatState(12345);
        var map = MapBuilder.BuildTestMap(new Vector2I(10, 10));
        combat.MapState = map;
        map.SetInteractionSystem(combat.Interactions);
        
        var terminalPos = new Vector2I(5, 5);
        var terminal = combat.Interactions.AddInteractable(InteractableTypes.Terminal, terminalPos,
            new System.Collections.Generic.Dictionary<string, object> { { "hackDifficulty", 60 } });
        var actor = combat.AddActor("crew", new Vector2I(5, 4));
        actor.Map = map;
        
        // Start hacking
        combat.Interactions.ExecuteInteraction(actor, terminal, "hack");
        AssertThat(actor.IsChanneling).IsTrue();
        
        // Issue movement order - should cancel channeling
        actor.SetTarget(new Vector2I(3, 3));
        AssertThat(actor.IsChanneling).IsFalse();
    }
    
    [TestCase]
    public void ChanneledAction_Interrupted_ByDamage()
    {
        var combat = new CombatState(12345);
        var map = MapBuilder.BuildTestMap(new Vector2I(10, 10));
        combat.MapState = map;
        map.SetInteractionSystem(combat.Interactions);
        
        var terminalPos = new Vector2I(5, 5);
        var terminal = combat.Interactions.AddInteractable(InteractableTypes.Terminal, terminalPos,
            new System.Collections.Generic.Dictionary<string, object> { { "hackDifficulty", 60 } });
        var actor = combat.AddActor("crew", new Vector2I(5, 4));
        
        // Start hacking
        combat.Interactions.ExecuteInteraction(actor, terminal, "hack");
        AssertThat(actor.IsChanneling).IsTrue();
        
        // Take damage - should cancel channeling
        actor.TakeDamage(10);
        AssertThat(actor.IsChanneling).IsFalse();
    }
    
    // === Terminal ===
    
    [TestCase]
    public void Terminal_StartsIdle()
    {
        var combat = new CombatState(12345);
        var pos = new Vector2I(5, 5);
        
        var terminal = combat.Interactions.AddInteractable(InteractableTypes.Terminal, pos);
        
        AssertThat(terminal.State).IsEqual(InteractableState.TerminalIdle);
    }
    
    [TestCase]
    public void Terminal_Hack_ChangesStateToHacking()
    {
        var combat = new CombatState(12345);
        var map = MapBuilder.BuildTestMap(new Vector2I(10, 10));
        combat.MapState = map;
        map.SetInteractionSystem(combat.Interactions);
        
        var terminalPos = new Vector2I(5, 5);
        var terminal = combat.Interactions.AddInteractable(InteractableTypes.Terminal, terminalPos,
            new System.Collections.Generic.Dictionary<string, object> { { "hackDifficulty", 60 } });
        var actor = combat.AddActor("crew", new Vector2I(5, 4));
        
        // Start hacking
        combat.Interactions.ExecuteInteraction(actor, terminal, "hack");
        
        AssertThat(terminal.State).IsEqual(InteractableState.TerminalHacking);
    }
    
    [TestCase]
    public void Terminal_Hack_Interrupted_ResetsState()
    {
        var combat = new CombatState(12345);
        var map = MapBuilder.BuildTestMap(new Vector2I(10, 10));
        combat.MapState = map;
        map.SetInteractionSystem(combat.Interactions);
        
        var terminalPos = new Vector2I(5, 5);
        var terminal = combat.Interactions.AddInteractable(InteractableTypes.Terminal, terminalPos,
            new System.Collections.Generic.Dictionary<string, object> { { "hackDifficulty", 60 } });
        var actor = combat.AddActor("crew", new Vector2I(5, 4));
        actor.Map = map;
        
        // Start hacking
        combat.Interactions.ExecuteInteraction(actor, terminal, "hack");
        AssertThat(terminal.State).IsEqual(InteractableState.TerminalHacking);
        
        // Interrupt by movement
        actor.SetTarget(new Vector2I(3, 3));
        
        // Terminal should return to idle
        AssertThat(terminal.State).IsEqual(InteractableState.TerminalIdle);
    }
    
    // === Hazard ===
    
    [TestCase]
    public void Hazard_StartsArmed()
    {
        var combat = new CombatState(12345);
        var pos = new Vector2I(5, 5);
        
        var hazard = combat.Interactions.AddInteractable(InteractableTypes.Hazard, pos);
        
        AssertThat(hazard.State).IsEqual(InteractableState.HazardArmed);
    }
    
    [TestCase]
    public void Hazard_Trigger_DealsAoEDamage()
    {
        var combat = new CombatState(12345);
        var map = MapBuilder.BuildTestMap(new Vector2I(10, 10));
        combat.MapState = map;
        map.SetInteractionSystem(combat.Interactions);
        combat.InitializeVisibility();
        
        var hazardPos = new Vector2I(5, 5);
        var hazard = combat.Interactions.AddInteractable(InteractableTypes.Hazard, hazardPos,
            new System.Collections.Generic.Dictionary<string, object>
            {
                { "hazardType", "explosive" },
                { "damage", 30 },
                { "radius", 2 }
            });
        
        // Place actor in blast radius
        var actor = combat.AddActor("crew", new Vector2I(5, 4));
        var initialHp = actor.Hp;
        
        // Trigger hazard
        combat.Interactions.TriggerHazard(hazard);
        
        AssertThat(actor.Hp).IsLess(initialHp);
        AssertThat(hazard.State).IsEqual(InteractableState.HazardTriggered);
    }
    
    [TestCase]
    public void Hazard_Disable_MakesSafe()
    {
        var combat = new CombatState(12345);
        var map = MapBuilder.BuildTestMap(new Vector2I(10, 10));
        combat.MapState = map;
        map.SetInteractionSystem(combat.Interactions);
        
        var hazardPos = new Vector2I(5, 5);
        var hazard = combat.Interactions.AddInteractable(InteractableTypes.Hazard, hazardPos,
            new System.Collections.Generic.Dictionary<string, object>
            {
                { "hazardType", "explosive" },
                { "damage", 30 },
                { "radius", 2 },
                { "disableDifficulty", 30 }
            });
        
        // Disable hazard
        hazard.SetState(InteractableState.HazardDisabled);
        
        AssertThat(hazard.State).IsEqual(InteractableState.HazardDisabled);
        AssertThat(hazard.IsHazard).IsTrue();
        AssertThat(hazard.State != InteractableState.HazardArmed).IsTrue();
    }
    
    // === Integration ===
    
    [TestCase]
    public void MapState_IsWalkable_ChecksDoorState()
    {
        var combat = new CombatState(12345);
        var map = MapBuilder.BuildTestMap(new Vector2I(10, 10));
        combat.MapState = map;
        map.SetInteractionSystem(combat.Interactions);
        
        var doorPos = new Vector2I(5, 5);
        var door = combat.Interactions.AddInteractable(InteractableTypes.Door, doorPos);
        
        // Closed door blocks
        AssertThat(map.IsWalkable(doorPos)).IsFalse();
        
        // Open door allows
        door.SetState(InteractableState.DoorOpen);
        AssertThat(map.IsWalkable(doorPos)).IsTrue();
        
        // Closed again blocks
        door.SetState(InteractableState.DoorClosed);
        AssertThat(map.IsWalkable(doorPos)).IsFalse();
    }
    
    [TestCase]
    public void MapState_BlocksLOS_ChecksDoorState()
    {
        var combat = new CombatState(12345);
        var map = MapBuilder.BuildTestMap(new Vector2I(10, 10));
        combat.MapState = map;
        map.SetInteractionSystem(combat.Interactions);
        
        var doorPos = new Vector2I(5, 5);
        var door = combat.Interactions.AddInteractable(InteractableTypes.Door, doorPos);
        
        // Closed door blocks LOS
        AssertThat(map.BlocksLOS(doorPos)).IsTrue();
        
        // Open door allows LOS
        door.SetState(InteractableState.DoorOpen);
        AssertThat(map.BlocksLOS(doorPos)).IsFalse();
    }
    
    [TestCase]
    public void CanInteract_RequiresAdjacency()
    {
        var combat = new CombatState(12345);
        var map = MapBuilder.BuildTestMap(new Vector2I(10, 10));
        combat.MapState = map;
        map.SetInteractionSystem(combat.Interactions);
        
        var doorPos = new Vector2I(5, 5);
        var door = combat.Interactions.AddInteractable(InteractableTypes.Door, doorPos);
        
        // Adjacent actor can interact
        var adjacentActor = combat.AddActor("crew", new Vector2I(5, 4));
        AssertThat(combat.Interactions.CanInteract(adjacentActor, door)).IsTrue();
        
        // Far actor cannot interact
        var farActor = combat.AddActor("crew", new Vector2I(1, 1));
        AssertThat(combat.Interactions.CanInteract(farActor, door)).IsFalse();
    }
    
    [TestCase]
    public void MapBuilder_ParsesDoorFromTemplate()
    {
        var combat = new CombatState(12345);
        var template = new string[]
        {
            "#####",
            "#...#",
            "#.D.#",
            "#...#",
            "#####"
        };
        
        var map = MapBuilder.BuildFromTemplate(template, combat.Interactions);
        
        var door = combat.Interactions.GetInteractableAt(new Vector2I(2, 2));
        AssertThat(door).IsNotNull();
        AssertThat(door.Type).IsEqual(InteractableTypes.Door);
        AssertThat(door.State).IsEqual(InteractableState.DoorClosed);
    }
    
    [TestCase]
    public void MapBuilder_ParsesLockedDoorFromTemplate()
    {
        var combat = new CombatState(12345);
        var template = new string[]
        {
            "#####",
            "#...#",
            "#.L.#",
            "#...#",
            "#####"
        };
        
        var map = MapBuilder.BuildFromTemplate(template, combat.Interactions);
        
        var door = combat.Interactions.GetInteractableAt(new Vector2I(2, 2));
        AssertThat(door).IsNotNull();
        AssertThat(door.Type).IsEqual(InteractableTypes.Door);
        AssertThat(door.State).IsEqual(InteractableState.DoorLocked);
    }
    
    [TestCase]
    public void MapBuilder_ParsesTerminalFromTemplate()
    {
        var combat = new CombatState(12345);
        var template = new string[]
        {
            "#####",
            "#...#",
            "#.T.#",
            "#...#",
            "#####"
        };
        
        var map = MapBuilder.BuildFromTemplate(template, combat.Interactions);
        
        var terminal = combat.Interactions.GetInteractableAt(new Vector2I(2, 2));
        AssertThat(terminal).IsNotNull();
        AssertThat(terminal.Type).IsEqual(InteractableTypes.Terminal);
        AssertThat(terminal.State).IsEqual(InteractableState.TerminalIdle);
    }
    
    [TestCase]
    public void MapBuilder_ParsesHazardFromTemplate()
    {
        var combat = new CombatState(12345);
        var template = new string[]
        {
            "#####",
            "#...#",
            "#.X.#",
            "#...#",
            "#####"
        };
        
        var map = MapBuilder.BuildFromTemplate(template, combat.Interactions);
        
        var hazard = combat.Interactions.GetInteractableAt(new Vector2I(2, 2));
        AssertThat(hazard).IsNotNull();
        AssertThat(hazard.Type).IsEqual(InteractableTypes.Hazard);
        AssertThat(hazard.State).IsEqual(InteractableState.HazardArmed);
    }
    
    // === Additional Integration Tests ===
    
    [TestCase]
    public void Terminal_Hack_CompletesObjective()
    {
        var combat = new CombatState(12345);
        var map = MapBuilder.BuildTestMap(new Vector2I(10, 10));
        combat.MapState = map;
        map.SetInteractionSystem(combat.Interactions);
        
        var terminalPos = new Vector2I(5, 5);
        var terminal = combat.Interactions.AddInteractable(InteractableTypes.Terminal, terminalPos,
            new System.Collections.Generic.Dictionary<string, object>
            {
                { "hackDifficulty", 5 },
                { "objectiveId", "hack_terminal_1" }
            });
        var actor = combat.AddActor("crew", new Vector2I(5, 4));
        
        // Objective not set initially
        AssertThat(combat.Objectives.ContainsKey("hack_terminal_1")).IsFalse();
        
        // Start hacking
        combat.Interactions.ExecuteInteraction(actor, terminal, "hack");
        AssertThat(actor.IsChanneling).IsTrue();
        
        // Complete the channel by ticking
        for (int i = 0; i < 5; i++)
        {
            actor.Tick(0.05f); // 1 tick at 20 ticks/sec
        }
        
        // Objective should be completed
        AssertThat(combat.Objectives.ContainsKey("hack_terminal_1")).IsTrue();
        AssertThat((bool)combat.Objectives["hack_terminal_1"]).IsTrue();
        AssertThat(terminal.State).IsEqual(InteractableState.TerminalHacked);
    }
    
    [TestCase]
    [RequireGodotRuntime]
    public void Visibility_Updates_WhenDoorOpens()
    {
        var combat = new CombatState(12345);
        // Create a map with a door blocking LOS
        var template = new string[]
        {
            "########",
            "#......#",
            "#..#D#.#",
            "#..#.#.#",
            "#......#",
            "########"
        };
        
        var map = MapBuilder.BuildFromTemplate(template, combat.Interactions);
        combat.MapState = map;
        map.SetInteractionSystem(combat.Interactions);
        combat.InitializeVisibility();
        
        var door = combat.Interactions.GetInteractableAt(new Vector2I(4, 2));
        AssertThat(door).IsNotNull();
        
        // Place crew on one side, enemy on other
        var crew = combat.AddActor("crew", new Vector2I(2, 3));
        var enemy = combat.AddActor("enemy", new Vector2I(5, 3));
        
        // Update visibility with door closed
        combat.Visibility.UpdateVisibility(combat.Actors);
        
        // Door blocks LOS - enemy should not be visible through door
        // (Note: depends on exact LOS algorithm, this tests the integration)
        AssertThat(door.BlocksLOS()).IsTrue();
        
        // Open the door
        door.SetState(InteractableState.DoorOpen);
        AssertThat(door.BlocksLOS()).IsFalse();
        
        // Update visibility again
        combat.Visibility.UpdateVisibility(combat.Actors);
        
        // Now LOS should be clear through the open door
        AssertThat(map.BlocksLOS(door.Position)).IsFalse();
    }
    
    [TestCase]
    public void MissionFactory_SpawnsInteractablesFromTemplate()
    {
        var config = MissionConfig.CreateM5TestMission();
        var combat = MissionFactory.BuildSandbox(config);
        
        // M5 test mission has doors, terminals, and hazards
        var allInteractables = combat.Interactions.GetAllInteractables();
        var doors = new System.Collections.Generic.List<Interactable>();
        var terminals = new System.Collections.Generic.List<Interactable>();
        var hazards = new System.Collections.Generic.List<Interactable>();
        
        foreach (var i in allInteractables)
        {
            if (i.IsDoor) doors.Add(i);
            else if (i.IsTerminal) terminals.Add(i);
            else if (i.IsHazard) hazards.Add(i);
        }
        
        // M5 template has: 1 door (D), 1 locked door (L), 2 terminals (T), 1 hazard (X)
        AssertThat(doors.Count).IsEqual(2);
        AssertThat(terminals.Count).IsEqual(2);
        AssertThat(hazards.Count).IsEqual(1);
    }
    
    [TestCase]
    public void Actor_CannotFire_WhileChanneling()
    {
        var combat = new CombatState(12345);
        var map = MapBuilder.BuildTestMap(new Vector2I(10, 10));
        combat.MapState = map;
        map.SetInteractionSystem(combat.Interactions);
        
        var terminalPos = new Vector2I(5, 5);
        var terminal = combat.Interactions.AddInteractable(InteractableTypes.Terminal, terminalPos,
            new System.Collections.Generic.Dictionary<string, object> { { "hackDifficulty", 60 } });
        var actor = combat.AddActor("crew", new Vector2I(5, 4));
        
        // Can fire initially
        AssertThat(actor.CanFire()).IsTrue();
        
        // Start hacking
        combat.Interactions.ExecuteInteraction(actor, terminal, "hack");
        AssertThat(actor.IsChanneling).IsTrue();
        
        // Cannot fire while channeling
        AssertThat(actor.CanFire()).IsFalse();
    }
    
    [TestCase]
    public void IssueInteractionOrder_WorksViaCombatState()
    {
        var combat = new CombatState(12345);
        var map = MapBuilder.BuildTestMap(new Vector2I(10, 10));
        combat.MapState = map;
        map.SetInteractionSystem(combat.Interactions);
        
        var doorPos = new Vector2I(5, 5);
        var door = combat.Interactions.AddInteractable(InteractableTypes.Door, doorPos);
        var actor = combat.AddActor("crew", new Vector2I(5, 4));
        
        AssertThat(door.State).IsEqual(InteractableState.DoorClosed);
        
        // Issue interaction order via CombatState
        var success = combat.IssueInteractionOrder(actor.Id, door.Id);
        
        AssertThat(success).IsTrue();
        AssertThat(door.State).IsEqual(InteractableState.DoorOpen);
    }
}
