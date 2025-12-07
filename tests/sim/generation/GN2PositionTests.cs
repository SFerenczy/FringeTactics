using GdUnit4;
using static GdUnit4.Assertions;
using System.Linq;
using Godot;

namespace FringeTactics.Tests;

[TestSuite]
[RequireGodotRuntime]
public class GN2PositionTests
{
    [TestCase]
    public void GeneratePositions_ReturnsRequestedCount()
    {
        var config = new GalaxyConfig { SystemCount = 10 };
        var rng = new RngService(12345).Campaign;
        var generator = new GalaxyGenerator(config, rng);

        var positions = generator.GeneratePositions();

        AssertInt(positions.Count).IsEqual(10);
    }

    [TestCase]
    public void GeneratePositions_AllWithinBounds()
    {
        var config = GalaxyConfig.Default;
        var rng = new RngService(12345).Campaign;
        var generator = new GalaxyGenerator(config, rng);

        var positions = generator.GeneratePositions();

        foreach (var pos in positions)
        {
            AssertBool(generator.IsWithinBounds(pos)).IsTrue();
        }
    }

    [TestCase]
    public void GeneratePositions_RespectEdgeMargin()
    {
        var config = new GalaxyConfig
        {
            SystemCount = 8,
            MapWidth = 400f,
            MapHeight = 300f,
            EdgeMargin = 50f,
            MinSystemDistance = 40f
        };
        var rng = new RngService(12345).Campaign;
        var generator = new GalaxyGenerator(config, rng);

        var positions = generator.GeneratePositions();

        foreach (var pos in positions)
        {
            AssertFloat(pos.X).IsGreaterEqual(config.EdgeMargin);
            AssertFloat(pos.X).IsLessEqual(config.MapWidth - config.EdgeMargin);
            AssertFloat(pos.Y).IsGreaterEqual(config.EdgeMargin);
            AssertFloat(pos.Y).IsLessEqual(config.MapHeight - config.EdgeMargin);
        }
    }

    [TestCase]
    public void GeneratePositions_RespectMinDistance()
    {
        var config = new GalaxyConfig
        {
            SystemCount = 10,
            MinSystemDistance = 80f
        };
        var rng = new RngService(12345).Campaign;
        var generator = new GalaxyGenerator(config, rng);

        var positions = generator.GeneratePositions();

        for (int i = 0; i < positions.Count; i++)
        {
            for (int j = i + 1; j < positions.Count; j++)
            {
                float dist = positions[i].DistanceTo(positions[j]);
                AssertFloat(dist).IsGreaterEqual(config.MinSystemDistance);
            }
        }
    }

    [TestCase]
    public void GeneratePositions_DeterministicWithSameSeed()
    {
        var config = GalaxyConfig.Default;

        var rng1 = new RngService(12345).Campaign;
        var generator1 = new GalaxyGenerator(config, rng1);
        var positions1 = generator1.GeneratePositions();

        var rng2 = new RngService(12345).Campaign;
        var generator2 = new GalaxyGenerator(config, rng2);
        var positions2 = generator2.GeneratePositions();

        AssertInt(positions1.Count).IsEqual(positions2.Count);

        for (int i = 0; i < positions1.Count; i++)
        {
            AssertFloat(positions1[i].X).IsEqual(positions2[i].X);
            AssertFloat(positions1[i].Y).IsEqual(positions2[i].Y);
        }
    }

    [TestCase]
    public void GeneratePositions_DifferentWithDifferentSeeds()
    {
        var config = GalaxyConfig.Default;

        var rng1 = new RngService(12345).Campaign;
        var generator1 = new GalaxyGenerator(config, rng1);
        var positions1 = generator1.GeneratePositions();

        var rng2 = new RngService(54321).Campaign;
        var generator2 = new GalaxyGenerator(config, rng2);
        var positions2 = generator2.GeneratePositions();

        // At least one position should differ
        bool anyDifferent = false;
        int minCount = System.Math.Min(positions1.Count, positions2.Count);

        for (int i = 0; i < minCount; i++)
        {
            if (positions1[i].X != positions2[i].X || positions1[i].Y != positions2[i].Y)
            {
                anyDifferent = true;
                break;
            }
        }

        AssertBool(anyDifferent).IsTrue();
    }

    [TestCase]
    public void IsValidPosition_ReturnsFalseForTooClose()
    {
        var config = new GalaxyConfig { MinSystemDistance = 100f };
        var rng = new RngService(12345).Campaign;
        var generator = new GalaxyGenerator(config, rng);

        var existing = new System.Collections.Generic.List<Vector2>
        {
            new Vector2(200, 200)
        };

        // Position too close (50 units away, min is 100)
        var tooClose = new Vector2(250, 200);
        AssertBool(generator.IsValidPosition(tooClose, existing)).IsFalse();

        // Position far enough (150 units away)
        var farEnough = new Vector2(350, 200);
        AssertBool(generator.IsValidPosition(farEnough, existing)).IsTrue();
    }

    [TestCase]
    public void IsValidPosition_ReturnsTrueForEmptyList()
    {
        var config = new GalaxyConfig { MinSystemDistance = 100f };
        var rng = new RngService(12345).Campaign;
        var generator = new GalaxyGenerator(config, rng);

        var existing = new System.Collections.Generic.List<Vector2>();
        var pos = new Vector2(200, 200);

        AssertBool(generator.IsValidPosition(pos, existing)).IsTrue();
    }

    [TestCase]
    public void IsWithinBounds_ReturnsCorrectly()
    {
        var config = new GalaxyConfig
        {
            MapWidth = 800f,
            MapHeight = 600f,
            EdgeMargin = 50f
        };
        var rng = new RngService(12345).Campaign;
        var generator = new GalaxyGenerator(config, rng);

        // Valid positions
        AssertBool(generator.IsWithinBounds(new Vector2(100, 100))).IsTrue();
        AssertBool(generator.IsWithinBounds(new Vector2(400, 300))).IsTrue();
        AssertBool(generator.IsWithinBounds(new Vector2(750, 550))).IsTrue();

        // Invalid - too close to edges
        AssertBool(generator.IsWithinBounds(new Vector2(25, 300))).IsFalse();  // Left edge
        AssertBool(generator.IsWithinBounds(new Vector2(775, 300))).IsFalse(); // Right edge
        AssertBool(generator.IsWithinBounds(new Vector2(400, 25))).IsFalse();  // Top edge
        AssertBool(generator.IsWithinBounds(new Vector2(400, 575))).IsFalse(); // Bottom edge
    }

    [TestCase]
    public void GeneratePositions_SmallConfig_Works()
    {
        var config = GalaxyConfig.Small;
        var rng = new RngService(12345).Campaign;
        var generator = new GalaxyGenerator(config, rng);

        var positions = generator.GeneratePositions();

        AssertInt(positions.Count).IsEqual(config.SystemCount);
    }

    [TestCase]
    public void GeneratePositions_LargeConfig_Works()
    {
        var config = GalaxyConfig.Large;
        var rng = new RngService(12345).Campaign;
        var generator = new GalaxyGenerator(config, rng);

        var positions = generator.GeneratePositions();

        AssertInt(positions.Count).IsEqual(config.SystemCount);
    }

    [TestCase]
    public void GeneratePositions_HandlesImpossibleConstraints()
    {
        // Too many systems for the space
        var config = new GalaxyConfig
        {
            SystemCount = 100,
            MapWidth = 200f,
            MapHeight = 200f,
            MinSystemDistance = 50f,
            EdgeMargin = 20f
        };
        var rng = new RngService(12345).Campaign;
        var generator = new GalaxyGenerator(config, rng);

        // Should not throw, but will return fewer systems
        var positions = generator.GeneratePositions();

        // Should have placed some systems, but not all 100
        AssertInt(positions.Count).IsGreater(0);
        AssertInt(positions.Count).IsLess(100);
    }

    [TestCase]
    public void Generate_CreatesWorldWithSystems()
    {
        var config = new GalaxyConfig { SystemCount = 8 };
        var rng = new RngService(12345).Campaign;
        var generator = new GalaxyGenerator(config, rng);

        var world = generator.Generate();

        AssertInt(world.Systems.Count).IsEqual(8);
    }

    [TestCase]
    public void Generate_SystemsHavePositions()
    {
        var config = GalaxyConfig.Default;
        var rng = new RngService(12345).Campaign;
        var generator = new GalaxyGenerator(config, rng);

        var world = generator.Generate();

        foreach (var system in world.GetAllSystems())
        {
            // Position should not be zero (very unlikely with random generation)
            AssertBool(system.Position != Vector2.Zero).IsTrue();
        }
    }

    [TestCase]
    public void Generate_WorldHasSectorName()
    {
        var config = GalaxyConfig.Default;
        var rng = new RngService(12345).Campaign;
        var generator = new GalaxyGenerator(config, rng);

        var world = generator.Generate();

        AssertString(world.Name).IsNotEmpty();
        AssertString(world.Name).IsNotEqual("Unknown Sector");
    }

    [TestCase]
    public void Generate_WorldHasFactions()
    {
        var config = GalaxyConfig.Default;
        var rng = new RngService(12345).Campaign;
        var generator = new GalaxyGenerator(config, rng);

        var world = generator.Generate();

        AssertInt(world.Factions.Count).IsGreater(0);
    }
}
