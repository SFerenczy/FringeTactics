namespace FringeTactics;

/// <summary>
/// Standard tags for encounter template categorization.
/// Used by EncounterTemplateRegistry for filtering and selection.
/// </summary>
public static class EncounterTags
{
    // === Trigger Context ===
    // Where/when the encounter can occur

    /// <summary>Travel encounters occur during route traversal.</summary>
    public const string Travel = "travel";

    /// <summary>Station encounters occur at docked locations.</summary>
    public const string Station = "station";

    /// <summary>Exploration encounters occur during system exploration.</summary>
    public const string Exploration = "exploration";

    // === Encounter Type ===
    // References EncounterTypes to avoid duplication

    /// <summary>Pirate/raider encounters.</summary>
    public const string Pirate = EncounterTypes.Pirate;

    /// <summary>Security patrol encounters.</summary>
    public const string Patrol = EncounterTypes.Patrol;

    /// <summary>Merchant/trader encounters.</summary>
    public const string Trader = EncounterTypes.Trader;

    /// <summary>Smuggler/black market encounters.</summary>
    public const string Smuggler = EncounterTypes.Smuggler;

    /// <summary>Distress signal encounters.</summary>
    public const string Distress = "distress";

    /// <summary>Anomaly/mystery encounters.</summary>
    public const string Anomaly = "anomaly";

    /// <summary>Crew-related encounters.</summary>
    public const string Crew = "crew";

    /// <summary>Faction-related encounters.</summary>
    public const string Faction = "faction";

    // === Interaction Style ===
    // How the encounter plays out

    /// <summary>Encounter may lead to combat.</summary>
    public const string Combat = "combat";

    /// <summary>Encounter is primarily social/dialogue.</summary>
    public const string Social = "social";

    /// <summary>Encounter presents meaningful choices.</summary>
    public const string Choice = "choice";

    /// <summary>Encounter involves skill checks.</summary>
    public const string SkillCheck = "skill_check";

    // === Special Modifiers ===

    /// <summary>Generic encounters that can fire in any context.</summary>
    public const string Generic = "generic";

    /// <summary>Rare encounters with lower selection weight.</summary>
    public const string Rare = "rare";

    /// <summary>Story-related encounters (may have prerequisites).</summary>
    public const string Story = "story";

    /// <summary>Cargo-related encounters (more likely with valuable cargo).</summary>
    public const string Cargo = "cargo";

    /// <summary>Ship/mechanical encounters.</summary>
    public const string Ship = "ship";

    /// <summary>Resource-related encounters (fuel, supplies).</summary>
    public const string Resource = "resource";
}
