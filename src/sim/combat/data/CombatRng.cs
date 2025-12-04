using System;

namespace FringeTactics;

/// <summary>
/// Seeded RNG wrapper for deterministic combat simulation.
/// All random decisions in combat should go through this.
/// </summary>
[Obsolete("Use RngStream from RngService instead. This class will be removed in a future version.")]
public class CombatRng
{
    private readonly Random random;
    public int Seed { get; }

    public CombatRng(int seed)
    {
        Seed = seed;
        random = new Random(seed);
    }

    public CombatRng() : this(Environment.TickCount)
    {
    }

    /// <summary>
    /// Get the underlying Random for passing to stateless functions.
    /// </summary>
    public Random GetRandom() => random;

    /// <summary>
    /// Roll a float between 0 and 1.
    /// </summary>
    public float NextFloat() => (float)random.NextDouble();

    /// <summary>
    /// Roll an int in range [min, max) (exclusive max).
    /// </summary>
    public int NextInt(int min, int max) => random.Next(min, max);

    /// <summary>
    /// Roll an int in range [0, max) (exclusive max).
    /// </summary>
    public int NextInt(int max) => random.Next(max);

    /// <summary>
    /// Roll against a probability. Returns true if roll succeeds.
    /// </summary>
    public bool Roll(float probability) => NextFloat() < probability;

    /// <summary>
    /// Pick a random element from a list.
    /// </summary>
    public T Pick<T>(System.Collections.Generic.List<T> list)
    {
        if (list == null || list.Count == 0)
        {
            return default;
        }
        return list[NextInt(list.Count)];
    }
}
