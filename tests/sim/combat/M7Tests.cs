using Godot;
using GdUnit4;
using System.Collections.Generic;
using System.Linq;
using static GdUnit4.Assertions;

namespace FringeTactics.Tests;

/// <summary>
/// Tests for M7: Session I/O & Retreat Integration
/// </summary>
[TestSuite]
public class M7Tests
{
    // === Retreat State Tests ===

    [TestCase]
    public void CombatState_InitiateRetreat_SetsFlag()
    {
        var combat = CreateTestCombat();
        combat.InitiateRetreat();
        AssertThat(combat.IsRetreating).IsTrue();
    }

    [TestCase]
    public void CombatState_CancelRetreat_ClearsFlag()
    {
        var combat = CreateTestCombat();
        combat.InitiateRetreat();
        combat.CancelRetreat();
        AssertThat(combat.IsRetreating).IsFalse();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void CombatState_AreAllCrewInEntryZone_TrueWhenAllInZone()
    {
        var combat = CreateTestCombatWithEntryZone();
        combat.AddActor(ActorType.Crew, new Vector2I(1, 1));
        combat.AddActor(ActorType.Crew, new Vector2I(2, 1));
        AssertThat(combat.AreAllCrewInEntryZone()).IsTrue();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void CombatState_AreAllCrewInEntryZone_FalseWhenSomeOutside()
    {
        var combat = CreateTestCombatWithEntryZone();
        combat.AddActor(ActorType.Crew, new Vector2I(1, 1));
        combat.AddActor(ActorType.Crew, new Vector2I(5, 5));
        AssertThat(combat.AreAllCrewInEntryZone()).IsFalse();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void CombatState_GetCrewExtractionStatus_ReturnsCorrectCounts()
    {
        var combat = CreateTestCombatWithEntryZone();
        combat.AddActor(ActorType.Crew, new Vector2I(1, 1));
        combat.AddActor(ActorType.Crew, new Vector2I(5, 5));
        var (inZone, total) = combat.GetCrewExtractionStatus();
        AssertThat(inZone).IsEqual(1);
        AssertThat(total).IsEqual(2);
    }

    // === Mission Output Tests ===

    [TestCase]
    [RequireGodotRuntime]
    public void MissionOutputBuilder_BuildsCorrectOutcome()
    {
        var combat = CreateTestCombat();
        var crew = combat.AddActor(ActorType.Crew, new Vector2I(1, 1));
        var map = new Dictionary<int, int> { { crew.Id, 100 } };
        
        var output = MissionOutputBuilder.Build(combat, MissionOutcome.Victory, map);
        
        AssertThat(output.Outcome).IsEqual(MissionOutcome.Victory);
        AssertThat(output.CrewOutcomes.Count).IsEqual(1);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void MissionOutputBuilder_TracksEnemyKills()
    {
        var combat = CreateTestCombat();
        var enemy = combat.AddActor(ActorType.Enemy, new Vector2I(5, 5));
        enemy.TakeDamage(1000);
        
        var output = MissionOutputBuilder.Build(combat, MissionOutcome.Victory, new());
        
        AssertThat(output.EnemiesKilled).IsEqual(1);
        AssertThat(output.EnemiesRemaining).IsEqual(0);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void CrewOutcome_MIA_WhenLeftBehindDuringRetreat()
    {
        var combat = CreateTestCombatWithEntryZone();
        var crew1 = combat.AddActor(ActorType.Crew, new Vector2I(1, 1));
        var crew2 = combat.AddActor(ActorType.Crew, new Vector2I(10, 10));
        var map = new Dictionary<int, int> { { crew1.Id, 1 }, { crew2.Id, 2 } };
        
        var output = MissionOutputBuilder.Build(combat, MissionOutcome.Retreat, map);
        
        var c1 = output.CrewOutcomes.First(c => c.CampaignCrewId == 1);
        var c2 = output.CrewOutcomes.First(c => c.CampaignCrewId == 2);
        AssertThat(c1.Status).IsEqual(CrewFinalStatus.Alive);
        AssertThat(c2.Status).IsEqual(CrewFinalStatus.MIA);
    }

    // === Actor Statistics Tests ===

    [TestCase]
    public void Actor_RecordKill_IncrementsKills()
    {
        var actor = new Actor(1, ActorType.Crew);
        actor.RecordKill();
        actor.RecordKill();
        AssertThat(actor.Kills).IsEqual(2);
    }

    [TestCase]
    public void Actor_RecordShot_TracksHitsAndMisses()
    {
        var actor = new Actor(1, ActorType.Crew);
        actor.RecordShot(hit: true, damage: 25);
        actor.RecordShot(hit: false);
        actor.RecordShot(hit: true, damage: 30);
        
        AssertThat(actor.ShotsFired).IsEqual(3);
        AssertThat(actor.ShotsHit).IsEqual(2);
        AssertThat(actor.TotalDamageDealt).IsEqual(55);
    }

    // === Helper Methods ===

    private CombatState CreateTestCombat()
    {
        var combat = new CombatState(12345);
        combat.MapState = MapBuilder.BuildFromTemplate(new[]
        {
            "########",
            "#......#",
            "#......#",
            "#......#",
            "########"
        }, combat.Interactions);
        combat.InitializeVisibility();
        return combat;
    }

    private CombatState CreateTestCombatWithEntryZone()
    {
        var combat = new CombatState(12345);
        combat.MapState = MapBuilder.BuildFromTemplate(new[]
        {
            "############",
            "#EE........#",
            "#EE........#",
            "#..........#",
            "#..........#",
            "############"
        }, combat.Interactions);
        combat.InitializeVisibility();
        return combat;
    }
}
