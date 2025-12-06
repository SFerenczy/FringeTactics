namespace FringeTactics;

/// <summary>
/// Types of effects that can result from encounter choices.
/// </summary>
public enum EffectType
{
    // Resource effects
    AddResource,

    // Crew effects
    CrewInjury,
    CrewXp,
    CrewTrait,

    // Ship effects
    ShipDamage,

    // World effects
    FactionRep,
    SetFlag,

    // Time effects
    TimeDelay,

    // Cargo effects
    AddCargo,
    RemoveCargo,

    // Flow effects
    GotoNode,
    EndEncounter,

    // Tactical (EN3)
    TriggerTactical
}
