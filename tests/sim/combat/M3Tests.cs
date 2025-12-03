using Godot;
using GdUnit4;
using System.Collections.Generic;
using static GdUnit4.Assertions;

namespace FringeTactics.Tests;

/// <summary>
/// M3 Milestone tests - validates basic combat loop.
/// </summary>
[TestSuite]
public class M3Tests
{
    // ========== Hit Chance Tests ==========

    [TestCase]
    [RequireGodotRuntime]
    public void HitChance_CloseRange_HigherThanLongRange()
    {
        var map = CreateOpenMap(20, 20);
        var combat = new CombatState();
        combat.MapState = map;

        var attacker = combat.AddActor("crew", new Vector2I(5, 5));
        var closeTarget = combat.AddActor("enemy", new Vector2I(7, 5)); // 2 tiles
        var farTarget = combat.AddActor("enemy", new Vector2I(12, 5));  // 7 tiles

        var closeChance = CombatResolver.CalculateHitChance(attacker, closeTarget, attacker.EquippedWeapon);
        var farChance = CombatResolver.CalculateHitChance(attacker, farTarget, attacker.EquippedWeapon);

        AssertThat(closeChance).IsGreater(farChance);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void HitChance_ClampedToValidRange()
    {
        var map = CreateOpenMap(20, 20);
        var combat = new CombatState();
        combat.MapState = map;

        var attacker = combat.AddActor("crew", new Vector2I(5, 5));
        attacker.Stats["aim"] = 100; // Absurdly high aim
        var target = combat.AddActor("enemy", new Vector2I(6, 5));

        var hitChance = CombatResolver.CalculateHitChance(attacker, target, attacker.EquippedWeapon);

        AssertThat(hitChance).IsLessEqual(CombatResolver.MAX_HIT_CHANCE);
        AssertThat(hitChance).IsGreaterEqual(CombatResolver.MIN_HIT_CHANCE);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void HitChance_AimStatProvidesBonus()
    {
        var map = CreateOpenMap(20, 20);
        var combat = new CombatState();
        combat.MapState = map;

        var normalAttacker = combat.AddActor("crew", new Vector2I(5, 5));
        var skilledAttacker = combat.AddActor("crew", new Vector2I(5, 6));
        skilledAttacker.Stats["aim"] = 10; // +10% bonus

        var target = combat.AddActor("enemy", new Vector2I(10, 5));

        var normalChance = CombatResolver.CalculateHitChance(normalAttacker, target, normalAttacker.EquippedWeapon);
        var skilledChance = CombatResolver.CalculateHitChance(skilledAttacker, target, skilledAttacker.EquippedWeapon);

        AssertThat(skilledChance).IsGreater(normalChance);
    }

    // ========== Ammo Tests ==========

    [TestCase]
    [RequireGodotRuntime]
    public void Actor_StartsWithFullMagazine()
    {
        var combat = new CombatState();
        var actor = combat.AddActor("crew", new Vector2I(5, 5));

        AssertThat(actor.CurrentMagazine).IsEqual(actor.EquippedWeapon.MagazineSize);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Actor_ConsumeAmmo_DecreasesMagazine()
    {
        var combat = new CombatState();
        var actor = combat.AddActor("crew", new Vector2I(5, 5));
        var initialAmmo = actor.CurrentMagazine;

        actor.ConsumeAmmo();

        AssertThat(actor.CurrentMagazine).IsEqual(initialAmmo - 1);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Actor_CannotFire_WhenMagazineEmpty()
    {
        var combat = new CombatState();
        var actor = combat.AddActor("crew", new Vector2I(5, 5));
        actor.CurrentMagazine = 0;

        AssertThat(actor.CanFire()).IsFalse();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Actor_NeedsReload_WhenEmptyWithReserve()
    {
        var combat = new CombatState();
        var actor = combat.AddActor("crew", new Vector2I(5, 5));
        actor.CurrentMagazine = 0;
        actor.ReserveAmmo = 30;

        AssertThat(actor.NeedsReload()).IsTrue();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Actor_Reload_FillsMagazineFromReserve()
    {
        var combat = new CombatState();
        var actor = combat.AddActor("crew", new Vector2I(5, 5));
        actor.CurrentMagazine = 0;
        actor.ReserveAmmo = 60;

        actor.StartReload();
        
        // Simulate reload completion
        for (int i = 0; i < actor.EquippedWeapon.ReloadTicks + 1; i++)
        {
            actor.Tick(0.05f);
        }

        AssertThat(actor.CurrentMagazine).IsEqual(actor.EquippedWeapon.MagazineSize);
        AssertThat(actor.ReserveAmmo).IsEqual(60 - actor.EquippedWeapon.MagazineSize);
        AssertThat(actor.IsReloading).IsFalse();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Actor_CannotFire_WhileReloading()
    {
        var combat = new CombatState();
        var actor = combat.AddActor("crew", new Vector2I(5, 5));
        actor.CurrentMagazine = 5; // Partial magazine
        actor.StartReload();

        AssertThat(actor.CanFire()).IsFalse();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Actor_IsOutOfAmmo_WhenNoMagazineAndNoReserve()
    {
        var combat = new CombatState();
        var actor = combat.AddActor("crew", new Vector2I(5, 5));
        actor.CurrentMagazine = 0;
        actor.ReserveAmmo = 0;

        AssertThat(actor.IsOutOfAmmo()).IsTrue();
        AssertThat(actor.NeedsReload()).IsFalse();
    }

    // ========== Auto-Defend Tests ==========

    [TestCase]
    [RequireGodotRuntime]
    public void AutoDefend_SetAutoDefendTarget_SetsTargetId()
    {
        var combat = new CombatState();
        var defender = combat.AddActor("crew", new Vector2I(5, 5));
        var attacker = combat.AddActor("enemy", new Vector2I(8, 5));

        defender.SetAutoDefendTarget(attacker.Id);

        AssertThat(defender.AutoDefendTargetId.HasValue).IsTrue();
        AssertThat(defender.AutoDefendTargetId.Value).IsEqual(attacker.Id);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void AutoDefend_DisabledUnit_DoesNotSetTarget()
    {
        var combat = new CombatState();
        var defender = combat.AddActor("crew", new Vector2I(5, 5));
        defender.AutoDefendEnabled = false;
        var attacker = combat.AddActor("enemy", new Vector2I(8, 5));

        defender.SetAutoDefendTarget(attacker.Id);

        AssertThat(defender.AutoDefendTargetId).IsNull();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void AutoDefend_ManualOrderTakesPriority()
    {
        var map = CreateOpenMap(20, 20);
        var combat = new CombatState();
        combat.MapState = map;

        var crew = combat.AddActor("crew", new Vector2I(5, 5));
        var enemy1 = combat.AddActor("enemy", new Vector2I(8, 5));
        var enemy2 = combat.AddActor("enemy", new Vector2I(10, 5));

        // Set auto-defend target
        crew.SetAutoDefendTarget(enemy1.Id);
        
        // Issue manual attack order to different target
        combat.IssueAttackOrder(crew.Id, enemy2.Id);

        // Manual order should take priority
        AssertThat(crew.AttackTargetId.HasValue).IsTrue();
        AssertThat(crew.AttackTargetId.Value).IsEqual(enemy2.Id);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void AutoDefend_ClearOrders_ClearsAutoDefendTarget()
    {
        var combat = new CombatState();
        var defender = combat.AddActor("crew", new Vector2I(5, 5));
        var attacker = combat.AddActor("enemy", new Vector2I(8, 5));

        defender.SetAutoDefendTarget(attacker.Id);
        defender.ClearOrders();

        AssertThat(defender.AutoDefendTargetId).IsNull();
    }

    // ========== Combat Resolution Tests ==========

    [TestCase]
    [RequireGodotRuntime]
    public void Attack_ConsumesAmmo()
    {
        var map = CreateOpenMap(20, 20);
        var combat = new CombatState();
        combat.MapState = map;
        combat.InitializeVisibility();
        combat.SetHasEnemyObjective(true);

        var attacker = combat.AddActor("crew", new Vector2I(5, 5));
        var target = combat.AddActor("enemy", new Vector2I(8, 5));
        var initialAmmo = attacker.CurrentMagazine;

        combat.IssueAttackOrder(attacker.Id, target.Id);
        combat.TimeSystem.Resume();

        // Simulate until attack fires
        for (int i = 0; i < 50; i++)
        {
            combat.Update(0.05f);
            if (attacker.CurrentMagazine < initialAmmo)
            {
                break;
            }
        }

        AssertThat(attacker.CurrentMagazine).IsLess(initialAmmo);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Actor_Dies_AtZeroHp()
    {
        var combat = new CombatState();
        var actor = combat.AddActor("crew", new Vector2I(5, 5));

        actor.TakeDamage(actor.MaxHp);

        AssertThat(actor.State).IsEqual(ActorState.Dead);
        AssertThat(actor.Hp).IsEqual(0);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Actor_TakeDamage_ReducesHp()
    {
        var combat = new CombatState();
        var actor = combat.AddActor("crew", new Vector2I(5, 5));
        var initialHp = actor.Hp;

        actor.TakeDamage(25);

        AssertThat(actor.Hp).IsEqual(initialHp - 25);
        AssertThat(actor.State).IsEqual(ActorState.Alive);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Mission_Victory_WhenAllEnemiesDead()
    {
        var map = CreateOpenMap(20, 20);
        var combat = new CombatState();
        combat.MapState = map;
        combat.InitializeVisibility();
        combat.SetHasEnemyObjective(true);

        var crew = combat.AddActor("crew", new Vector2I(5, 5));
        var enemy = combat.AddActor("enemy", new Vector2I(8, 5));

        // Kill the enemy
        enemy.TakeDamage(enemy.MaxHp);
        combat.TimeSystem.Resume();
        combat.Update(0.05f);

        AssertThat(combat.IsComplete).IsTrue();
        AssertThat(combat.Victory).IsTrue();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Mission_Defeat_WhenAllCrewDead()
    {
        var map = CreateOpenMap(20, 20);
        var combat = new CombatState();
        combat.MapState = map;
        combat.InitializeVisibility();
        combat.SetHasEnemyObjective(true);

        var crew = combat.AddActor("crew", new Vector2I(5, 5));
        var enemy = combat.AddActor("enemy", new Vector2I(8, 5));

        // Kill the crew
        crew.TakeDamage(crew.MaxHp);
        combat.TimeSystem.Resume();
        combat.Update(0.05f);

        AssertThat(combat.IsComplete).IsTrue();
        AssertThat(combat.Victory).IsFalse();
    }

    // ========== AI Tests ==========

    [TestCase]
    [RequireGodotRuntime]
    public void AI_TargetScoring_PrefersCloserTargets()
    {
        var map = CreateOpenMap(20, 20);
        var combat = new CombatState();
        combat.MapState = map;

        var enemy = combat.AddActor("enemy", new Vector2I(5, 5));
        var closeTarget = combat.AddActor("crew", new Vector2I(7, 5)); // 2 tiles
        var farTarget = combat.AddActor("crew", new Vector2I(12, 5));  // 7 tiles

        // Both targets have same HP, so closer should score higher
        // We can't directly test AIController.ScoreTarget (private), but we can verify behavior
        // by checking which target gets selected after AI thinks
        
        combat.InitializeVisibility();
        combat.TimeSystem.Resume();
        
        // Run AI tick
        for (int i = 0; i < 15; i++)
        {
            combat.Update(0.05f);
        }

        // Enemy should target the closer crew member
        AssertThat(enemy.AttackTargetId.HasValue).IsTrue();
        AssertThat(enemy.AttackTargetId.Value).IsEqual(closeTarget.Id);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void AI_TargetScoring_PrefersWoundedTargets()
    {
        var map = CreateOpenMap(20, 20);
        var combat = new CombatState();
        combat.MapState = map;

        var enemy = combat.AddActor("enemy", new Vector2I(5, 5));
        // Both at same distance
        var healthyTarget = combat.AddActor("crew", new Vector2I(8, 5));
        var woundedTarget = combat.AddActor("crew", new Vector2I(8, 6));
        woundedTarget.TakeDamage(woundedTarget.MaxHp - 10); // Nearly dead

        combat.InitializeVisibility();
        combat.TimeSystem.Resume();
        
        // Run AI tick
        for (int i = 0; i < 15; i++)
        {
            combat.Update(0.05f);
        }

        // Enemy should target the wounded crew member
        AssertThat(enemy.AttackTargetId.HasValue).IsTrue();
        AssertThat(enemy.AttackTargetId.Value).IsEqual(woundedTarget.Id);
    }

    // ========== Weapon Tests ==========

    [TestCase]
    [RequireGodotRuntime]
    public void WeaponData_HasAmmoProperties()
    {
        var rifle = WeaponData.FromId("rifle");

        AssertThat(rifle.MagazineSize).IsGreater(0);
        AssertThat(rifle.ReloadTicks).IsGreater(0);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void WeaponData_DifferentWeaponsHaveDifferentStats()
    {
        var rifle = WeaponData.FromId("rifle");
        var shotgun = WeaponData.FromId("shotgun");

        // Shotgun should have smaller magazine but higher damage
        AssertThat(shotgun.MagazineSize).IsLess(rifle.MagazineSize);
        AssertThat(shotgun.Damage).IsGreater(rifle.Damage);
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
