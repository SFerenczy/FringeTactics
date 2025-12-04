namespace FringeTactics;

/// <summary>
/// Actor type identifiers.
/// </summary>
public enum ActorType
{
    Crew,
    Enemy,
    Drone
}

/// <summary>
/// Constants for actor type identifiers (legacy compatibility).
/// Prefer using ActorType enum directly.
/// </summary>
public static class ActorTypes
{
    public const string Crew = "crew";
    public const string Enemy = "enemy";
    public const string Drone = "drone";
    
    /// <summary>
    /// Convert enum to legacy string.
    /// </summary>
    public static string ToString(ActorType type) => type switch
    {
        ActorType.Crew => Crew,
        ActorType.Enemy => Enemy,
        ActorType.Drone => Drone,
        _ => "unknown"
    };
    
    /// <summary>
    /// Convert legacy string to enum.
    /// </summary>
    public static ActorType FromString(string type) => type switch
    {
        Crew => ActorType.Crew,
        Enemy => ActorType.Enemy,
        Drone => ActorType.Drone,
        _ => ActorType.Crew
    };
}
