using Godot;
using System.Collections.Generic;

namespace FringeTactics;

public enum WaveTriggerType
{
    Time,           // After X ticks from phase start
    Event,          // When specific event fires
    ActorHpBelow,   // When tagged actor HP drops below threshold
    WaveComplete,   // When previous wave is eliminated
    PhaseStart,     // Immediately when phase starts
    Manual          // Triggered by script/objective
}

public class WaveTrigger
{
    public WaveTriggerType Type { get; set; } = WaveTriggerType.Time;
    public int DelayTicks { get; set; } = 0;
    public string EventId { get; set; }
    public float HpThreshold { get; set; } = 0.5f;
    public string TargetActorTag { get; set; }
    public string PreviousWaveId { get; set; }
    public TacticalPhase RequiredPhase { get; set; } = TacticalPhase.Pressure;
}

public class WaveDefinition
{
    public string Id { get; set; }
    public string Name { get; set; }
    public List<EnemySpawn> Enemies { get; set; } = new();
    public string SpawnPointId { get; set; }
    public WaveTrigger Trigger { get; set; } = new();
    public bool Announced { get; set; } = true;
}

public class SpawnPoint
{
    public string Id { get; set; }
    public Vector2I Position { get; set; }
    public List<Vector2I> AdditionalPositions { get; set; } = new();
    public bool BlockedByLOS { get; set; } = true;
    public string DoorId { get; set; }
    public SpawnDirection FacingDirection { get; set; } = SpawnDirection.TowardCenter;
}

public enum SpawnDirection
{
    TowardCenter,
    TowardPlayer,
    FixedNorth,
    FixedSouth,
    FixedEast,
    FixedWest
}
