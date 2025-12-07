using Godot;
using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// Complete specification for starting a tactical mission.
/// This is the formal contract between campaign and tactical layers.
/// </summary>
public class MissionInput
{
    // === Map Specification ===
    
    /// <summary>
    /// Map template using character codes.
    /// Characters: '.' = floor, '#' = wall, 'E' = entry zone, 'D' = door, etc.
    /// </summary>
    public string[] MapTemplate { get; set; }
    
    /// <summary>
    /// Optional grid size override. If null, derived from MapTemplate.
    /// </summary>
    public Vector2I? GridSize { get; set; }

    // === Crew Deployment ===
    
    /// <summary>
    /// Crew members to deploy in this mission.
    /// </summary>
    public List<CrewDeployment> Crew { get; set; } = new();

    // === Enemy Configuration ===
    
    /// <summary>
    /// Enemies to spawn in this mission.
    /// </summary>
    public List<EnemySpawn> Enemies { get; set; } = new();

    // === Interactables ===
    
    /// <summary>
    /// Additional interactables beyond those defined in MapTemplate.
    /// </summary>
    public List<InteractableSpawn> Interactables { get; set; } = new();

    // === Objectives ===
    
    /// <summary>
    /// Mission objectives to track.
    /// </summary>
    public List<MissionObjective> Objectives { get; set; } = new();

    // === Context ===
    
    /// <summary>
    /// Unique identifier for this mission instance.
    /// </summary>
    public string MissionId { get; set; } = "unknown";
    
    /// <summary>
    /// Display name for the mission.
    /// </summary>
    public string MissionName { get; set; } = "Unknown Mission";
    
    /// <summary>
    /// RNG seed for deterministic simulation.
    /// </summary>
    public int Seed { get; set; } = 0;
    
    /// <summary>
    /// Campaign context for this mission.
    /// </summary>
    public MissionContext Context { get; set; } = new();
}

/// <summary>
/// A crew member to deploy in the mission.
/// Contains all data needed to create a tactical actor from campaign crew.
/// </summary>
public class CrewDeployment
{
    /// <summary>
    /// ID linking back to the campaign crew member.
    /// </summary>
    public int CampaignCrewId { get; set; }
    
    /// <summary>
    /// Display name.
    /// </summary>
    public string Name { get; set; }

    // === Stats ===
    
    public int MaxHp { get; set; } = 100;
    public int CurrentHp { get; set; } = 100;
    public float MoveSpeed { get; set; } = 2.0f;
    public float Accuracy { get; set; } = 0.7f;
    
    /// <summary>
    /// Armor value from equipment (damage reduction).
    /// </summary>
    public int Armor { get; set; } = 0;

    // === Equipment ===
    
    /// <summary>
    /// Weapon definition ID (e.g., "rifle", "smg").
    /// </summary>
    public string WeaponId { get; set; } = WeaponIds.Rifle;
    
    public int AmmoInMagazine { get; set; } = 30;
    public int ReserveAmmo { get; set; } = 90;

    // === Spawn ===
    
    /// <summary>
    /// Optional spawn position. If null, uses entry zone.
    /// </summary>
    public Vector2I? SpawnPosition { get; set; }
}

/// <summary>
/// Mission context from the campaign layer.
/// Provides environmental and narrative context for the tactical layer.
/// </summary>
public class MissionContext
{
    public string LocationId { get; set; }
    public string LocationName { get; set; }
    public string FactionId { get; set; }
    
    /// <summary>
    /// Environmental tags affecting the mission (e.g., "high_security", "lawless").
    /// </summary>
    public List<string> Tags { get; set; } = new();
    
    /// <summary>
    /// Contract ID if this mission is contract-driven.
    /// </summary>
    public string ContractId { get; set; }
}

/// <summary>
/// An objective to track during the mission.
/// </summary>
public class MissionObjective
{
    /// <summary>
    /// Unique identifier for this objective.
    /// </summary>
    public string Id { get; set; }
    
    /// <summary>
    /// Player-facing description.
    /// </summary>
    public string Description { get; set; }
    
    /// <summary>
    /// Type of objective determining completion logic.
    /// </summary>
    public ObjectiveType Type { get; set; }
    
    /// <summary>
    /// Whether this is a primary (required) or secondary (optional) objective.
    /// </summary>
    public bool IsPrimary { get; set; } = true;

    // === Type-specific data ===
    
    /// <summary>
    /// Target position for "reach zone" objectives.
    /// </summary>
    public Vector2I? TargetPosition { get; set; }
    
    /// <summary>
    /// Target interactable ID for "hack terminal" objectives.
    /// </summary>
    public int? TargetInteractableId { get; set; }
    
    /// <summary>
    /// Target actor type for "eliminate" objectives (e.g., "enemy", "boss").
    /// </summary>
    public string TargetActorType { get; set; }
}

/// <summary>
/// Types of mission objectives.
/// </summary>
public enum ObjectiveType
{
    /// <summary>Kill all enemies on the map.</summary>
    EliminateAll,

    /// <summary>Kill a specific target.</summary>
    EliminateTarget,

    /// <summary>Get crew to a specific location.</summary>
    ReachZone,

    /// <summary>Complete a hack on a terminal.</summary>
    HackTerminal,

    /// <summary>Keep crew alive for a duration.</summary>
    Survive,

    /// <summary>Pick up a specific item.</summary>
    Retrieve,

    /// <summary>Protect a VIP.</summary>
    Escort,

    // === GN1 Additions ===

    /// <summary>Keep a specific unit alive.</summary>
    ProtectUnit,

    /// <summary>Destroy an interactable object.</summary>
    DestroyObject,

    /// <summary>Pick up and extract an item.</summary>
    RetrieveItem,

    /// <summary>Survive for X turns.</summary>
    SurviveTurns,

    /// <summary>Complete mission without triggering alarm.</summary>
    NoAlarm,

    /// <summary>Complete mission with no crew deaths.</summary>
    NoCasualties,

    /// <summary>Complete mission within X turns.</summary>
    TimeLimit,

    /// <summary>Complete mission with no crew injuries.</summary>
    NoInjuries
}
