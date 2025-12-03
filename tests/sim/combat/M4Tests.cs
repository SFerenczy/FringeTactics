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
        // Create map with half cover position
        var template = new string[]
        {
            "##############",
            "#............#",
            "#............#",
            "#....=.......#",  // half cover at (5,3)
            "#............#",
            "#............#",
            "##############"
        };
        var map = MapBuilder.BuildFromTemplate(template);
        var combat = new CombatState();
        combat.MapState = map;

        // Attacker to the west, covered target behind half cover, exposed target in open
        var attacker = combat.AddActor("crew", new Vector2I(3, 3));
        var coveredTarget = combat.AddActor("enemy", new Vector2I(6, 3));  // half cover at (5,3) to west
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
        // Larger map with half cover
        var template = new string[]
        {
            "##########",
            "#........#",
            "#..=.....#",  // half cover at (3,2), target at (4,2)
            "#........#",
            "##########"
        };
        var map = MapBuilder.BuildFromTemplate(template);
        var combat = new CombatState();
        combat.MapState = map;

        var attacker = combat.AddActor("crew", new Vector2I(1, 2));
        var target = combat.AddActor("enemy", new Vector2I(4, 2));

        // Verify target actually has half cover
        AssertThat(map.GetCoverAgainst(target.GridPosition, attacker.GridPosition)).IsEqual(CoverHeight.Half);

        // Calculate expected reduction
        var baseChance = CombatResolver.CalculateHitChance(attacker, target, attacker.EquippedWeapon, null);
        var coveredChance = CombatResolver.CalculateHitChance(attacker, target, attacker.EquippedWeapon, map);
        
        // Covered chance should be base * (1 - HalfCoverReduction), clamped
        var expectedCovered = baseChance * (1f - CombatBalance.HalfCoverReduction);
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
        // Half cover to the west of target, attacker from east (flanking)
        var template = new string[]
        {
            "###########",
            "#.........#",
            "#..=......#",  // half cover at (3,2), target at (4,2)
            "#.........#",
            "###########"
        };
        var map = MapBuilder.BuildFromTemplate(template);
        var combat = new CombatState();
        combat.MapState = map;

        var westAttacker = combat.AddActor("crew", new Vector2I(1, 2));
        var eastAttacker = combat.AddActor("crew", new Vector2I(7, 2));  // flanking from east
        var target = combat.AddActor("enemy", new Vector2I(4, 2));       // half cover at (3,2) to west

        // Verify west attacker sees cover, east attacker does not
        AssertThat(map.GetCoverAgainst(target.GridPosition, westAttacker.GridPosition)).IsEqual(CoverHeight.Half);
        AssertThat(map.GetCoverAgainst(target.GridPosition, eastAttacker.GridPosition)).IsEqual(CoverHeight.None);

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

    // ========== M4.1 Cover Height Tests ==========

    [TestCase]
    [RequireGodotRuntime]
    public void CoverHeight_GetReduction_ReturnsCorrectValues()
    {
        AssertThat(CombatBalance.GetCoverReduction(CoverHeight.None)).IsEqual(0f);
        AssertThat(CombatBalance.GetCoverReduction(CoverHeight.Low)).IsEqual(0.15f);
        AssertThat(CombatBalance.GetCoverReduction(CoverHeight.Half)).IsEqual(0.30f);
        AssertThat(CombatBalance.GetCoverReduction(CoverHeight.High)).IsEqual(0.45f);
        AssertThat(CombatBalance.GetCoverReduction(CoverHeight.Full)).IsEqual(0f);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void MapBuilder_ParsesLowCover()
    {
        var template = new string[]
        {
            "#####",
            "#...#",
            "#.-.#",  // low cover at (2,2)
            "#...#",
            "#####"
        };
        var map = MapBuilder.BuildFromTemplate(template);

        AssertThat(map.GetTileCoverHeight(new Vector2I(2, 2))).IsEqual(CoverHeight.Low);
        AssertThat(map.GetTileType(new Vector2I(2, 2))).IsEqual(TileType.Floor);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void MapBuilder_ParsesHalfCover()
    {
        var template = new string[]
        {
            "#####",
            "#...#",
            "#.=.#",  // half cover at (2,2)
            "#...#",
            "#####"
        };
        var map = MapBuilder.BuildFromTemplate(template);

        AssertThat(map.GetTileCoverHeight(new Vector2I(2, 2))).IsEqual(CoverHeight.Half);
        AssertThat(map.GetTileType(new Vector2I(2, 2))).IsEqual(TileType.Floor);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void MapBuilder_ParsesHighCover()
    {
        var template = new string[]
        {
            "#####",
            "#...#",
            "#.+.#",  // high cover at (2,2)
            "#...#",
            "#####"
        };
        var map = MapBuilder.BuildFromTemplate(template);

        AssertThat(map.GetTileCoverHeight(new Vector2I(2, 2))).IsEqual(CoverHeight.High);
        AssertThat(map.GetTileType(new Vector2I(2, 2))).IsEqual(TileType.Floor);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void MapState_GetCoverAgainst_ReturnsCorrectHeight()
    {
        var template = new string[]
        {
            "#########",
            "#.......#",
            "#.-.=.+.#",  // low at (2,2), half at (4,2), high at (6,2)
            "#.......#",  // targets at (3,3), (5,3), (7,3)
            "#.......#",
            "#########"
        };
        var map = MapBuilder.BuildFromTemplate(template);

        // Target at (3,3), attacker from north - low cover at (2,2) is NW, not N
        // Let's use direct north positions
        // Low cover at (2,2), target at (2,3), attacker from (2,1)
        AssertThat(map.GetCoverAgainst(new Vector2I(2, 3), new Vector2I(2, 1))).IsEqual(CoverHeight.Low);
        
        // Half cover at (4,2), target at (4,3), attacker from (4,1)
        AssertThat(map.GetCoverAgainst(new Vector2I(4, 3), new Vector2I(4, 1))).IsEqual(CoverHeight.Half);
        
        // High cover at (6,2), target at (6,3), attacker from (6,1)
        AssertThat(map.GetCoverAgainst(new Vector2I(6, 3), new Vector2I(6, 1))).IsEqual(CoverHeight.High);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void HitChance_ScalesWithCoverHeight()
    {
        // Attacker at west, targets at east with cover between
        // Template indices: #=0, .=1, -=2, .=3, ==4, .=5, +=6, ...
        var template = new string[]
        {
            "##############",
            "#............#",
            "#.-..=..+....#",  // low(2,2), half(5,2), high(8,2)
            "#............#",
            "#............#",
            "##############"
        };
        var map = MapBuilder.BuildFromTemplate(template);
        var combat = new CombatState();
        combat.MapState = map;

        var attacker = combat.AddActor("crew", new Vector2I(1, 3));  // south of cover row
        
        // Target behind low cover (cover at 2,2, target at 2,3)
        var lowTarget = combat.AddActor("enemy", new Vector2I(2, 3));
        // Target behind half cover (cover at 5,2, target at 5,3)
        var halfTarget = combat.AddActor("enemy", new Vector2I(5, 3));
        // Target behind high cover (cover at 8,2, target at 8,3)
        var highTarget = combat.AddActor("enemy", new Vector2I(8, 3));
        // Target in open
        var openTarget = combat.AddActor("enemy", new Vector2I(11, 3));

        // Verify cover heights - attacker from south, cover is north of targets
        AssertThat(map.GetCoverAgainst(lowTarget.GridPosition, attacker.GridPosition)).IsEqual(CoverHeight.None);
        AssertThat(map.GetCoverAgainst(halfTarget.GridPosition, attacker.GridPosition)).IsEqual(CoverHeight.None);
        AssertThat(map.GetCoverAgainst(highTarget.GridPosition, attacker.GridPosition)).IsEqual(CoverHeight.None);
        
        // Actually, for cover to work, attacker needs to be NORTH and cover between
        // Let's fix the setup: attacker north, targets south of cover
        var northAttacker = combat.AddActor("crew", new Vector2I(1, 1));
        
        // Targets north of cover, attacker south - cover provides protection
        var lowTarget2 = combat.AddActor("enemy", new Vector2I(2, 3));
        var halfTarget2 = combat.AddActor("enemy", new Vector2I(5, 3));
        var highTarget2 = combat.AddActor("enemy", new Vector2I(8, 3));
        
        // Cover at (2,2) is N of target at (2,3), attacker at (1,1) is NW
        // Direction from target(2,3) to attacker(1,1) is NW, check (1,2) - floor
        // This is getting complicated. Let's simplify with direct west-east setup
        
        var template2 = new string[]
        {
            "##############",
            "#............#",
            "#A.-.=.+.....#",  // A=1, -=3, ==5, +=7
            "#............#",
            "##############"
        };
        var map2 = MapBuilder.BuildFromTemplate(template2);
        var combat2 = new CombatState();
        combat2.MapState = map2;

        var westAttacker = combat2.AddActor("crew", new Vector2I(1, 2));
        
        // Targets east of each cover type
        var lowT = combat2.AddActor("enemy", new Vector2I(4, 2));   // cover at (3,2)
        var halfT = combat2.AddActor("enemy", new Vector2I(6, 2));  // cover at (5,2)
        var highT = combat2.AddActor("enemy", new Vector2I(8, 2));  // cover at (7,2)
        var openT = combat2.AddActor("enemy", new Vector2I(11, 2)); // no cover

        // Verify cover heights
        AssertThat(map2.GetCoverAgainst(lowT.GridPosition, westAttacker.GridPosition)).IsEqual(CoverHeight.Low);
        AssertThat(map2.GetCoverAgainst(halfT.GridPosition, westAttacker.GridPosition)).IsEqual(CoverHeight.Half);
        AssertThat(map2.GetCoverAgainst(highT.GridPosition, westAttacker.GridPosition)).IsEqual(CoverHeight.High);
        AssertThat(map2.GetCoverAgainst(openT.GridPosition, westAttacker.GridPosition)).IsEqual(CoverHeight.None);

        var lowC = CombatResolver.CalculateHitChance(westAttacker, lowT, westAttacker.EquippedWeapon, map2);
        var halfC = CombatResolver.CalculateHitChance(westAttacker, halfT, westAttacker.EquippedWeapon, map2);
        var highC = CombatResolver.CalculateHitChance(westAttacker, highT, westAttacker.EquippedWeapon, map2);

        // Low > Half > High (more cover = lower hit chance)
        AssertThat(lowC).IsGreater(halfC);
        AssertThat(halfC).IsGreater(highC);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void HitChance_LowCover_ReducesBy15Percent()
    {
        var template = new string[]
        {
            "##########",
            "#........#",
            "#.-......#",  // low cover at (2,2)
            "#........#",  // target at (3,3)
            "#........#",
            "##########"
        };
        var map = MapBuilder.BuildFromTemplate(template);
        var combat = new CombatState();
        combat.MapState = map;

        var attacker = combat.AddActor("crew", new Vector2I(1, 2));
        var target = combat.AddActor("enemy", new Vector2I(3, 2));

        // Verify target has low cover
        AssertThat(map.GetCoverAgainst(target.GridPosition, attacker.GridPosition)).IsEqual(CoverHeight.Low);

        var baseChance = CombatResolver.CalculateHitChance(attacker, target, attacker.EquippedWeapon, null);
        var coveredChance = CombatResolver.CalculateHitChance(attacker, target, attacker.EquippedWeapon, map);
        
        var expectedCovered = baseChance * (1f - CombatBalance.LowCoverReduction);
        expectedCovered = Mathf.Clamp(expectedCovered, CombatResolver.MIN_HIT_CHANCE, CombatResolver.MAX_HIT_CHANCE);
        
        AssertThat(coveredChance).IsEqualApprox(expectedCovered, 0.01f);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void HitChance_HalfCover_ReducesBy30Percent()
    {
        var template = new string[]
        {
            "##########",
            "#........#",
            "#.=......#",  // half cover at (2,2)
            "#........#",
            "#........#",
            "##########"
        };
        var map = MapBuilder.BuildFromTemplate(template);
        var combat = new CombatState();
        combat.MapState = map;

        var attacker = combat.AddActor("crew", new Vector2I(1, 2));
        var target = combat.AddActor("enemy", new Vector2I(3, 2));

        AssertThat(map.GetCoverAgainst(target.GridPosition, attacker.GridPosition)).IsEqual(CoverHeight.Half);

        var baseChance = CombatResolver.CalculateHitChance(attacker, target, attacker.EquippedWeapon, null);
        var coveredChance = CombatResolver.CalculateHitChance(attacker, target, attacker.EquippedWeapon, map);
        
        var expectedCovered = baseChance * (1f - CombatBalance.HalfCoverReduction);
        expectedCovered = Mathf.Clamp(expectedCovered, CombatResolver.MIN_HIT_CHANCE, CombatResolver.MAX_HIT_CHANCE);
        
        AssertThat(coveredChance).IsEqualApprox(expectedCovered, 0.01f);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void HitChance_HighCover_ReducesBy45Percent()
    {
        var template = new string[]
        {
            "##########",
            "#........#",
            "#.+......#",  // high cover at (2,2)
            "#........#",
            "#........#",
            "##########"
        };
        var map = MapBuilder.BuildFromTemplate(template);
        var combat = new CombatState();
        combat.MapState = map;

        var attacker = combat.AddActor("crew", new Vector2I(1, 2));
        var target = combat.AddActor("enemy", new Vector2I(3, 2));

        AssertThat(map.GetCoverAgainst(target.GridPosition, attacker.GridPosition)).IsEqual(CoverHeight.High);

        var baseChance = CombatResolver.CalculateHitChance(attacker, target, attacker.EquippedWeapon, null);
        var coveredChance = CombatResolver.CalculateHitChance(attacker, target, attacker.EquippedWeapon, map);
        
        var expectedCovered = baseChance * (1f - CombatBalance.HighCoverReduction);
        expectedCovered = Mathf.Clamp(expectedCovered, CombatResolver.MIN_HIT_CHANCE, CombatResolver.MAX_HIT_CHANCE);
        
        AssertThat(coveredChance).IsEqualApprox(expectedCovered, 0.01f);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void AttackResult_TargetCoverHeight_SetCorrectly()
    {
        // Attacker at (2,2), half cover at (4,2), target at (5,2)
        // Target looks west toward attacker, cover is at (4,2)
        var template = new string[]
        {
            "##########",
            "#........#",
            "#...=....#",  // half cover at (4,2)
            "#........#",
            "##########"
        };
        var map = MapBuilder.BuildFromTemplate(template);
        var combat = new CombatState(12345);
        combat.MapState = map;

        var attacker = combat.AddActor("crew", new Vector2I(2, 2));
        var target = combat.AddActor("enemy", new Vector2I(5, 2));

        // Verify cover height - target at (5,2), attacker at (2,2)
        // Direction from target to attacker is W, so check tile (4,2) = half cover
        AssertThat(map.GetCoverAgainst(target.GridPosition, attacker.GridPosition)).IsEqual(CoverHeight.Half);

        var result = CombatResolver.ResolveAttack(attacker, target, attacker.EquippedWeapon, map, combat.Rng.GetRandom());

        AssertThat(result.TargetCoverHeight).IsEqual(CoverHeight.Half);
        AssertThat(result.TargetInCover).IsTrue();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void CoverHeight_WallsReturnFull()
    {
        // Wall at (4,2), target at (5,2), attacker at (2,2)
        // Note: This tests the raw GetCoverAgainst, even though LOS would be blocked
        var template = new string[]
        {
            "#########",
            "#.......#",
            "#..#....#",  // wall at (3,2)
            "#.......#",
            "#########"
        };
        var map = MapBuilder.BuildFromTemplate(template);

        // Target at (4,2), attacker at (2,2) - wall at (3,2) is between
        // Direction from target to attacker is W, check tile (3,2) = wall
        AssertThat(map.GetCoverAgainst(new Vector2I(4, 2), new Vector2I(2, 2))).IsEqual(CoverHeight.Full);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void CoverHeight_FlankingBypassesPartialCover()
    {
        var template = new string[]
        {
            "###########",
            "#.........#",
            "#..=......#",  // half cover at (3,2)
            "#.........#",  // target at (4,3)
            "#.........#",
            "###########"
        };
        var map = MapBuilder.BuildFromTemplate(template);
        var combat = new CombatState();
        combat.MapState = map;

        var westAttacker = combat.AddActor("crew", new Vector2I(1, 2));
        var eastAttacker = combat.AddActor("crew", new Vector2I(8, 3));
        var target = combat.AddActor("enemy", new Vector2I(4, 2));

        // West attacker sees half cover
        AssertThat(map.GetCoverAgainst(target.GridPosition, westAttacker.GridPosition)).IsEqual(CoverHeight.Half);
        
        // East attacker (flanking) sees no cover
        AssertThat(map.GetCoverAgainst(target.GridPosition, eastAttacker.GridPosition)).IsEqual(CoverHeight.None);

        var westChance = CombatResolver.CalculateHitChance(westAttacker, target, westAttacker.EquippedWeapon, map);
        var eastChance = CombatResolver.CalculateHitChance(eastAttacker, target, eastAttacker.EquippedWeapon, map);

        // Flanking attacker has higher hit chance
        AssertThat(eastChance).IsGreater(westChance);
    }
}
