namespace FringeTactics;

/// <summary>
/// Atomic effect that can result from an encounter choice.
/// Effects are accumulated during encounter execution and applied by MG4.
/// </summary>
public class EncounterEffect
{
    public EffectType Type { get; set; }

    /// <summary>
    /// Target identifier: resource type, trait id, faction id, node id, flag id, item def id.
    /// </summary>
    public string TargetId { get; set; }

    /// <summary>
    /// Numeric value: quantity, damage, days, rep delta, XP amount.
    /// </summary>
    public int Amount { get; set; }

    /// <summary>
    /// String parameter: injury type, cargo item id, mission type.
    /// </summary>
    public string StringParam { get; set; }

    /// <summary>
    /// Boolean parameter: add/remove for traits.
    /// </summary>
    public bool BoolParam { get; set; }

    // === Resource Effects ===

    public static EncounterEffect AddCredits(int amount) => new()
    {
        Type = EffectType.AddResource,
        TargetId = ResourceTypes.Money,
        Amount = amount
    };

    public static EncounterEffect LoseCredits(int amount) => new()
    {
        Type = EffectType.AddResource,
        TargetId = ResourceTypes.Money,
        Amount = -amount
    };

    public static EncounterEffect AddFuel(int amount) => new()
    {
        Type = EffectType.AddResource,
        TargetId = ResourceTypes.Fuel,
        Amount = amount
    };

    public static EncounterEffect LoseFuel(int amount) => new()
    {
        Type = EffectType.AddResource,
        TargetId = ResourceTypes.Fuel,
        Amount = -amount
    };

    public static EncounterEffect AddParts(int amount) => new()
    {
        Type = EffectType.AddResource,
        TargetId = ResourceTypes.Parts,
        Amount = amount
    };

    public static EncounterEffect AddMeds(int amount) => new()
    {
        Type = EffectType.AddResource,
        TargetId = ResourceTypes.Meds,
        Amount = amount
    };

    // === Crew Effects ===

    public static EncounterEffect CrewInjury(string injuryType = InjuryTypes.Wounded) => new()
    {
        Type = EffectType.CrewInjury,
        StringParam = injuryType
    };

    public static EncounterEffect CrewXp(int amount) => new()
    {
        Type = EffectType.CrewXp,
        Amount = amount
    };

    public static EncounterEffect AddTrait(string traitId) => new()
    {
        Type = EffectType.CrewTrait,
        TargetId = traitId,
        BoolParam = true
    };

    public static EncounterEffect RemoveTrait(string traitId) => new()
    {
        Type = EffectType.CrewTrait,
        TargetId = traitId,
        BoolParam = false
    };

    /// <summary>
    /// Add a new crew member to the roster.
    /// </summary>
    /// <param name="name">Name of the new crew member.</param>
    /// <param name="role">Role as string (Soldier, Medic, Tech, Scout). Defaults to Soldier if invalid.</param>
    public static EncounterEffect AddCrew(string name, string role = "Soldier") => new()
    {
        Type = EffectType.AddCrew,
        TargetId = name,
        StringParam = role
    };

    // === Ship Effects ===

    public static EncounterEffect ShipDamage(int amount) => new()
    {
        Type = EffectType.ShipDamage,
        Amount = amount
    };

    // === World Effects ===

    public static EncounterEffect FactionRep(string factionId, int delta) => new()
    {
        Type = EffectType.FactionRep,
        TargetId = factionId,
        Amount = delta
    };

    public static EncounterEffect SetFlag(string flagId, bool value = true) => new()
    {
        Type = EffectType.SetFlag,
        TargetId = flagId,
        BoolParam = value
    };

    // === Time Effects ===

    public static EncounterEffect TimeDelay(int days) => new()
    {
        Type = EffectType.TimeDelay,
        Amount = days
    };

    // === Cargo Effects ===

    public static EncounterEffect AddCargo(string itemDefId, int quantity = 1) => new()
    {
        Type = EffectType.AddCargo,
        TargetId = itemDefId,
        Amount = quantity
    };

    public static EncounterEffect RemoveCargo(string itemDefId, int quantity = 1) => new()
    {
        Type = EffectType.RemoveCargo,
        TargetId = itemDefId,
        Amount = quantity
    };

    // === Flow Effects ===

    public static EncounterEffect GotoNode(string nodeId) => new()
    {
        Type = EffectType.GotoNode,
        TargetId = nodeId
    };

    public static EncounterEffect End() => new()
    {
        Type = EffectType.EndEncounter
    };

    // === Tactical (EN3) ===

    public static EncounterEffect TriggerTactical(string missionType) => new()
    {
        Type = EffectType.TriggerTactical,
        StringParam = missionType
    };
}
