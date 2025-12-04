using System;
using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// Central service for deterministic RNG with multiple isolated streams.
/// Each stream is independent: consuming from one doesn't affect others.
/// </summary>
public class RngService
{
    private readonly Dictionary<string, RngStream> streams = new();

    /// <summary>
    /// Master seed used to derive stream seeds.
    /// This is the player-visible campaign seed.
    /// </summary>
    public int MasterSeed { get; private set; }

    // Well-known stream names
    public const string CampaignStream = "campaign";
    public const string TacticalStream = "tactical";

    /// <summary>
    /// Initialize with a master seed. Derives stream seeds deterministically.
    /// </summary>
    public RngService(int masterSeed)
    {
        MasterSeed = masterSeed;
        InitializeStreams();
    }

    /// <summary>
    /// Initialize with a random master seed.
    /// </summary>
    public RngService() : this(Environment.TickCount)
    {
    }

    private void InitializeStreams()
    {
        CreateStream(CampaignStream, DeriveStreamSeed(CampaignStream));
        CreateStream(TacticalStream, DeriveStreamSeed(TacticalStream));
    }

    /// <summary>
    /// Derive a deterministic seed for a stream using FNV-1a hash.
    /// This is stable across .NET versions (unlike string.GetHashCode).
    /// </summary>
    private int DeriveStreamSeed(string streamName)
    {
        unchecked
        {
            const int fnvPrime = 16777619;
            int hash = (int)2166136261 ^ MasterSeed;
            foreach (char c in streamName)
            {
                hash ^= c;
                hash *= fnvPrime;
            }
            return hash;
        }
    }

    private void CreateStream(string name, int seed)
    {
        streams[name] = new RngStream(name, seed);
    }

    /// <summary>
    /// Get a stream by name. Throws if stream doesn't exist.
    /// </summary>
    public RngStream GetStream(string name)
    {
        if (!streams.TryGetValue(name, out var stream))
        {
            throw new ArgumentException($"Unknown RNG stream: {name}");
        }
        return stream;
    }

    /// <summary>
    /// Check if a stream exists.
    /// </summary>
    public bool HasStream(string name) => streams.ContainsKey(name);

    /// <summary>
    /// Shortcut for campaign stream.
    /// </summary>
    public RngStream Campaign => GetStream(CampaignStream);

    /// <summary>
    /// Shortcut for tactical stream.
    /// </summary>
    public RngStream Tactical => GetStream(TacticalStream);

    /// <summary>
    /// Reset the tactical stream with a new seed.
    /// Called at mission start for per-mission determinism.
    /// </summary>
    public void ResetTacticalStream(int missionSeed)
    {
        streams[TacticalStream] = new RngStream(TacticalStream, missionSeed);
    }

    /// <summary>
    /// Get state of all streams for serialization.
    /// </summary>
    public RngServiceState GetState()
    {
        var state = new RngServiceState
        {
            MasterSeed = MasterSeed,
            Streams = new List<RngStreamState>()
        };

        foreach (var stream in streams.Values)
        {
            state.Streams.Add(stream.GetState());
        }

        return state;
    }

    /// <summary>
    /// Restore all streams from saved state.
    /// </summary>
    public void RestoreState(RngServiceState state)
    {
        MasterSeed = state.MasterSeed;

        foreach (var streamState in state.Streams)
        {
            if (streams.TryGetValue(streamState.Name, out var stream))
            {
                stream.RestoreState(streamState.Seed, streamState.CallCount);
            }
            else
            {
                var newStream = new RngStream(streamState.Name, streamState.Seed);
                newStream.RestoreState(streamState.Seed, streamState.CallCount);
                streams[streamState.Name] = newStream;
            }
        }
    }
}

/// <summary>
/// Serializable state for the entire RNG service.
/// </summary>
public class RngServiceState
{
    public int MasterSeed { get; set; }
    public List<RngStreamState> Streams { get; set; } = new();
}
