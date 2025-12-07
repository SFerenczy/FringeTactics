using System;
using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// A single seeded RNG stream with serializable state.
/// Wraps System.Random with additional utility methods.
/// </summary>
public class RngStream
{
    private Random random;

    /// <summary>
    /// The original seed used to create this stream.
    /// </summary>
    public int Seed { get; private set; }

    /// <summary>
    /// Number of values consumed from this stream.
    /// Used for state restoration.
    /// </summary>
    public int CallCount { get; private set; }

    /// <summary>
    /// Name of this stream for debugging.
    /// </summary>
    public string Name { get; }

    public RngStream(string name, int seed)
    {
        Name = name;
        Seed = seed;
        CallCount = 0;
        random = new Random(seed);
    }

    /// <summary>
    /// Restore stream to a specific state by replaying calls.
    /// </summary>
    public void RestoreState(int seed, int callCount)
    {
        Seed = seed;
        random = new Random(seed);
        CallCount = 0;

        for (int i = 0; i < callCount; i++)
        {
            random.Next();
            CallCount++;
        }
    }

    /// <summary>
    /// Get state for serialization.
    /// </summary>
    public RngStreamState GetState() => new()
    {
        Name = Name,
        Seed = Seed,
        CallCount = CallCount
    };

    /// <summary>
    /// Roll a float between 0 and 1.
    /// </summary>
    public float NextFloat()
    {
        CallCount++;
        return (float)random.NextDouble();
    }

    /// <summary>
    /// Roll a float in range [min, max).
    /// </summary>
    public float NextFloat(float min, float max)
    {
        return min + NextFloat() * (max - min);
    }

    /// <summary>
    /// Roll an int in range [0, max) (exclusive max).
    /// </summary>
    public int NextInt(int max)
    {
        CallCount++;
        return random.Next(max);
    }

    /// <summary>
    /// Roll an int in range [min, max) (exclusive max).
    /// </summary>
    public int NextInt(int min, int max)
    {
        CallCount++;
        return random.Next(min, max);
    }

    /// <summary>
    /// Roll against a probability. Returns true if roll succeeds.
    /// </summary>
    public bool Roll(float probability)
    {
        return NextFloat() < probability;
    }

    /// <summary>
    /// Pick a random element from a list.
    /// </summary>
    public T Pick<T>(IList<T> list)
    {
        if (list == null || list.Count == 0)
        {
            return default;
        }
        return list[NextInt(list.Count)];
    }

    /// <summary>
    /// Shuffle a list in place using Fisher-Yates algorithm.
    /// </summary>
    public void Shuffle<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = NextInt(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}

/// <summary>
/// Serializable state for an RNG stream.
/// </summary>
public class RngStreamState
{
    public string Name { get; set; }
    public int Seed { get; set; }
    public int CallCount { get; set; }
}
