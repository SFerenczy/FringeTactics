using Godot;
using GdUnit4;
using static GdUnit4.Assertions;

namespace FringeTactics.Tests;

/// <summary>
/// M4 Milestone tests - validates directional cover mechanics.
/// </summary>
[TestSuite]
public class M4Tests
{
    // ========== CoverDirection Tests ==========

    [TestCase]
    [RequireGodotRuntime]
    public void CoverDirection_GetDirection_ReturnsCorrectCardinals()
    {
        var center = new Vector2I(5, 5);
        
        AssertThat(CoverDirectionHelper.GetDirection(center, new Vector2I(5, 4))).IsEqual(CoverDirection.N);
        AssertThat(CoverDirectionHelper.GetDirection(center, new Vector2I(6, 5))).IsEqual(CoverDirection.E);
        AssertThat(CoverDirectionHelper.GetDirection(center, new Vector2I(5, 6))).IsEqual(CoverDirection.S);
        AssertThat(CoverDirectionHelper.GetDirection(center, new Vector2I(4, 5))).IsEqual(CoverDirection.W);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void CoverDirection_GetDirection_ReturnsCorrectDiagonals()
    {
        var center = new Vector2I(5, 5);
        
        AssertThat(CoverDirectionHelper.GetDirection(center, new Vector2I(6, 4))).IsEqual(CoverDirection.NE);
        AssertThat(CoverDirectionHelper.GetDirection(center, new Vector2I(6, 6))).IsEqual(CoverDirection.SE);
        AssertThat(CoverDirectionHelper.GetDirection(center, new Vector2I(4, 6))).IsEqual(CoverDirection.SW);
        AssertThat(CoverDirectionHelper.GetDirection(center, new Vector2I(4, 4))).IsEqual(CoverDirection.NW);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void CoverDirection_GetOpposite_ReturnsCorrectOpposite()
    {
        AssertThat(CoverDirectionHelper.GetOpposite(CoverDirection.N)).IsEqual(CoverDirection.S);
        AssertThat(CoverDirectionHelper.GetOpposite(CoverDirection.E)).IsEqual(CoverDirection.W);
        AssertThat(CoverDirectionHelper.GetOpposite(CoverDirection.NE)).IsEqual(CoverDirection.SW);
        AssertThat(CoverDirectionHelper.GetOpposite(CoverDirection.SE)).IsEqual(CoverDirection.NW);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void CoverDirection_SamePosition_ReturnsNone()
    {
        var pos = new Vector2I(5, 5);
        AssertThat(CoverDirectionHelper.GetDirection(pos, pos)).IsEqual(CoverDirection.None);
    }

    // ========== MapState Cover Tests ==========

    [TestCase]
    [RequireGodotRuntime]
    public void MapState_WallProvidesCover()
    {
        // Map with wall to the west of a floor tile
        // ###
        // #.#  <- floor at (1,1), wall at (0,1)
        // ###
        var template = new string[]
        {
            "###",
            "#.#",
            "###"
        };
        var map = MapBuilder.BuildFromTemplate(template);
        
        // Wall at (0,1) should provide cover facing east (toward the floor)
        var wallCover = map.GetTileCover(new Vector2I(0, 1));
        AssertThat((wallCover & CoverDirection.E) != 0).IsTrue();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void MapState_HasCoverAgainst_TrueWhenWallBlocks()
    {
        // Attacker to the west, target behind wall
        // #####
        // #A#T#  <- A at (1,1), wall at (2,1), T at (3,1)
        // #####
        var template = new string[]
        {
            "#####",
            "#.#.#",
            "#####"
        };
        var map = MapBuilder.BuildFromTemplate(template);
        
        var attackerPos = new Vector2I(1, 1);
        var targetPos = new Vector2I(3, 1);
        
        // Target should have cover from attacker (wall between them)
        AssertThat(map.HasCoverAgainst(targetPos, attackerPos)).IsTrue();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void MapState_HasCoverAgainst_FalseWhenNoWall()
    {
        // Open field, no cover between attacker and target
        // Use larger map so boundary walls don't interfere
        var template = new string[]
        {
            "#########",
            "#.......#",
            "#.......#",
            "#.......#",
            "#########"
        };
        var map = MapBuilder.BuildFromTemplate(template);
        
        // Both in middle of map, no walls between or adjacent
        var attackerPos = new Vector2I(2, 2);
        var targetPos = new Vector2I(5, 2);
        
        AssertThat(map.HasCoverAgainst(targetPos, attackerPos)).IsFalse();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void MapState_HasCoverAgainst_DirectionallyCorrect()
    {
        // Wall to the north of target, larger map to avoid boundary interference
        // #########
        // #.......#
        // #...#...#  <- wall at (4,2)
        // #...T...#  <- target at (4,3)
        // #.......#
        // #########
        var template = new string[]
        {
            "#########",
            "#.......#",
            "#...#...#",
            "#.......#",
            "#.......#",
            "#########"
        };
        var map = MapBuilder.BuildFromTemplate(template);
        
        var targetPos = new Vector2I(4, 3);
        var attackerFromNorth = new Vector2I(4, 1);  // attacking from north
        var attackerFromSouth = new Vector2I(4, 4);  // attacking from south
        var attackerFromEast = new Vector2I(6, 3);   // attacking from east
        
        // Target has cover from north (wall at 4,2 blocks)
        AssertThat(map.HasCoverAgainst(targetPos, attackerFromNorth)).IsTrue();
        
        // Target has NO cover from south or east
        AssertThat(map.HasCoverAgainst(targetPos, attackerFromSouth)).IsFalse();
        AssertThat(map.HasCoverAgainst(targetPos, attackerFromEast)).IsFalse();
    }

    // ========== Combat Resolution with Cover Tests ==========

    [TestCase]
    [RequireGodotRuntime]
    public void HitChance_ReducedWhenTargetInCover()
    {
        // Create map with cover position - larger to avoid boundary walls
        var template = new string[]
        {
            "##############",
            "#............#",
            "#............#",
            "#....#.......#",  // wall at (5,3)
            "#............#",
            "#............#",
            "##############"
        };
        var map = MapBuilder.BuildFromTemplate(template);
        var combat = new CombatState();
        combat.MapState = map;

        // Attacker to the west, covered target behind wall, exposed target in open
        var attacker = combat.AddActor("crew", new Vector2I(3, 3));
        var coveredTarget = combat.AddActor("enemy", new Vector2I(6, 3));  // wall at (5,3) to west
        var exposedTarget = combat.AddActor("enemy", new Vector2I(9, 3));  // no cover from west

        var coveredChance = CombatResolver.CalculateHitChance(attacker, coveredTarget, attacker.EquippedWeapon, map);
        var exposedChance = CombatResolver.CalculateHitChance(attacker, exposedTarget, attacker.EquippedWeapon, map);

        // Covered target should be harder to hit
        AssertThat(coveredChance).IsLess(exposedChance);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void HitChance_CoverReductionMatchesConstant()
    {
        // Larger map to avoid boundary wall interference
        var template = new string[]
        {
            "##########",
            "#........#",
            "#..#.....#",  // wall at (3,2), target at (4,2)
            "#........#",
            "##########"
        };
        var map = MapBuilder.BuildFromTemplate(template);
        var combat = new CombatState();
        combat.MapState = map;

        var attacker = combat.AddActor("crew", new Vector2I(1, 2));
        var target = combat.AddActor("enemy", new Vector2I(4, 2));

        // Verify target actually has cover
        AssertThat(map.HasCoverAgainst(target.GridPosition, attacker.GridPosition)).IsTrue();

        // Calculate expected reduction
        var baseChance = CombatResolver.CalculateHitChance(attacker, target, attacker.EquippedWeapon, null);
        var coveredChance = CombatResolver.CalculateHitChance(attacker, target, attacker.EquippedWeapon, map);
        
        // Covered chance should be base * (1 - COVER_HIT_REDUCTION), clamped
        var expectedCovered = baseChance * (1f - CombatResolver.COVER_HIT_REDUCTION);
        expectedCovered = Mathf.Clamp(expectedCovered, CombatResolver.MIN_HIT_CHANCE, CombatResolver.MAX_HIT_CHANCE);
        
        AssertThat(coveredChance).IsEqualApprox(expectedCovered, 0.01f);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void AttackResult_TargetInCover_SetCorrectly()
    {
        // Wall NORTH of target, attacker from NORTHWEST
        // LOS goes diagonally and misses the wall, but cover check finds it
        var template = new string[]
        {
            "##########",
            "#........#",
            "#A...#...#",  // attacker(2,2), wall(5,2)
            "#.....T..#",  // target(6,3)
            "#........#",
            "##########"
        };
        var map = MapBuilder.BuildFromTemplate(template);
        var combat = new CombatState(12345);
        combat.MapState = map;
        
        var attacker = combat.AddActor("crew", new Vector2I(2, 2));
        var target = combat.AddActor("enemy", new Vector2I(6, 3));
        
        // Verify LOS exists (diagonal path misses wall)
        AssertThat(CombatResolver.HasLineOfSight(attacker.GridPosition, target.GridPosition, map)).IsTrue();
        
        // Direction from target(6,3) to attacker(2,2) is NW
        // Cover check looks at tile (5,2) - wall!
        AssertThat(map.HasCoverAgainst(target.GridPosition, attacker.GridPosition)).IsTrue();

        var result = CombatResolver.ResolveAttack(attacker, target, attacker.EquippedWeapon, map, combat.Rng.GetRandom());

        AssertThat(result.TargetInCover).IsTrue();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void AttackResult_TargetNotInCover_SetCorrectly()
    {
        var template = new string[]
        {
            "######",
            "#....#",
            "######"
        };
        var map = MapBuilder.BuildFromTemplate(template);
        var combat = new CombatState(12345);
        combat.MapState = map;

        var attacker = combat.AddActor("crew", new Vector2I(1, 1));
        var exposedTarget = combat.AddActor("enemy", new Vector2I(3, 1));

        var result = CombatResolver.ResolveAttack(attacker, exposedTarget, attacker.EquippedWeapon, map, combat.Rng.GetRandom());

        AssertThat(result.TargetInCover).IsFalse();
    }

    // ========== Flanking Tests ==========

    [TestCase]
    [RequireGodotRuntime]
    public void Flanking_BypassesCover()
    {
        // Wall to the west of target, attacker from east (flanking)
        // Larger map to avoid boundary wall interference
        var template = new string[]
        {
            "###########",
            "#.........#",
            "#..#......#",  // wall at (3,2), target at (4,2)
            "#.........#",
            "###########"
        };
        var map = MapBuilder.BuildFromTemplate(template);
        var combat = new CombatState();
        combat.MapState = map;

        var westAttacker = combat.AddActor("crew", new Vector2I(1, 2));
        var eastAttacker = combat.AddActor("crew", new Vector2I(7, 2));  // flanking from east
        var target = combat.AddActor("enemy", new Vector2I(4, 2));       // wall at (3,2) to west

        // Verify west attacker sees cover, east attacker does not
        AssertThat(map.HasCoverAgainst(target.GridPosition, westAttacker.GridPosition)).IsTrue();
        AssertThat(map.HasCoverAgainst(target.GridPosition, eastAttacker.GridPosition)).IsFalse();

        var westChance = CombatResolver.CalculateHitChance(westAttacker, target, westAttacker.EquippedWeapon, map);
        var eastChance = CombatResolver.CalculateHitChance(eastAttacker, target, eastAttacker.EquippedWeapon, map);

        // East attacker (flanking) should have higher hit chance
        AssertThat(eastChance).IsGreater(westChance);
    }

    // ========== MapBuilder Cover Generation Tests ==========

    [TestCase]
    [RequireGodotRuntime]
    public void MapBuilder_GeneratesCoverFromWalls()
    {
        var template = new string[]
        {
            "###",
            "#.#",
            "###"
        };
        var map = MapBuilder.BuildFromTemplate(template);

        // All walls should have cover flags set
        var topWall = map.GetTileCover(new Vector2I(1, 0));
        var leftWall = map.GetTileCover(new Vector2I(0, 1));
        var rightWall = map.GetTileCover(new Vector2I(2, 1));
        var bottomWall = map.GetTileCover(new Vector2I(1, 2));

        // Top wall provides cover facing south (toward floor)
        AssertThat((topWall & CoverDirection.S) != 0).IsTrue();
        // Left wall provides cover facing east
        AssertThat((leftWall & CoverDirection.E) != 0).IsTrue();
        // Right wall provides cover facing west
        AssertThat((rightWall & CoverDirection.W) != 0).IsTrue();
        // Bottom wall provides cover facing north
        AssertThat((bottomWall & CoverDirection.N) != 0).IsTrue();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void MapBuilder_FloorTilesHaveNoCover()
    {
        var template = new string[]
        {
            "###",
            "#.#",
            "###"
        };
        var map = MapBuilder.BuildFromTemplate(template);

        var floorCover = map.GetTileCover(new Vector2I(1, 1));
        AssertThat(floorCover).IsEqual(CoverDirection.None);
    }

    // ========== Balance Constant Tests ==========

    [TestCase]
    [RequireGodotRuntime]
    public void CombatBalance_ConstantsAreValid()
    {
        // Cover reduction should be between 0 and 1
        AssertThat(CombatBalance.CoverHitReduction).IsGreater(0f);
        AssertThat(CombatBalance.CoverHitReduction).IsLess(1f);
        
        // Min/max hit chance should be valid
        AssertThat(CombatBalance.MinHitChance).IsGreater(0f);
        AssertThat(CombatBalance.MaxHitChance).IsLess(1f);
        AssertThat(CombatBalance.MinHitChance).IsLess(CombatBalance.MaxHitChance);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void CombatResolver_UsesBalanceConstants()
    {
        // Verify CombatResolver constants match CombatBalance
        AssertThat(CombatResolver.COVER_HIT_REDUCTION).IsEqual(CombatBalance.CoverHitReduction);
        AssertThat(CombatResolver.MIN_HIT_CHANCE).IsEqual(CombatBalance.MinHitChance);
        AssertThat(CombatResolver.MAX_HIT_CHANCE).IsEqual(CombatBalance.MaxHitChance);
    }

    // ========== Edge Case Tests ==========

    [TestCase]
    [RequireGodotRuntime]
    public void HitChance_CoverDoesNotGoBelowMinimum()
    {
        // Even with cover, hit chance should not go below minimum
        var template = new string[]
        {
            "##########",
            "#.#......#",
            "##########"
        };
        var map = MapBuilder.BuildFromTemplate(template);
        var combat = new CombatState();
        combat.MapState = map;

        var attacker = combat.AddActor("crew", new Vector2I(1, 1));
        var target = combat.AddActor("enemy", new Vector2I(8, 1));  // Far away + cover

        var hitChance = CombatResolver.CalculateHitChance(attacker, target, attacker.EquippedWeapon, map);

        AssertThat(hitChance).IsGreaterEqual(CombatResolver.MIN_HIT_CHANCE);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void HasCoverAgainst_OutOfBounds_DoesNotCrash()
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

        // Attacker position out of bounds - should not crash
        // The direction calculation still works, cover check should handle gracefully
        var result = map.HasCoverAgainst(new Vector2I(2, 2), new Vector2I(-5, -5));
        
        // Should not crash - result depends on boundary walls but shouldn't throw
        // Just verify it returns a boolean without crashing
        AssertThat(result == true || result == false).IsTrue();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void DiagonalCover_WorksCorrectly()
    {
        // Wall to the northwest of target - larger map
        var template = new string[]
        {
            "#########",
            "#.......#",
            "#..#....#",  // wall at (3,2)
            "#...T...#",  // target at (4,3)
            "#.......#",
            "#.......#",
            "#########"
        };
        var map = MapBuilder.BuildFromTemplate(template);

        var targetPos = new Vector2I(4, 3);
        var attackerFromNW = new Vector2I(2, 1);  // attacking from northwest
        var attackerFromSE = new Vector2I(6, 5);  // attacking from southeast

        // Wall at (3,2) is NW of target at (4,3)
        // It should provide cover from NW attacks
        AssertThat(map.HasCoverAgainst(targetPos, attackerFromNW)).IsTrue();
        AssertThat(map.HasCoverAgainst(targetPos, attackerFromSE)).IsFalse();
    }
}
