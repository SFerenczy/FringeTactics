namespace FringeTactics;

/// <summary>
/// Runtime weapon data for an actor. Created from WeaponDef.
/// </summary>
public struct WeaponData
{
    public string Id { get; set; }
    public string Name { get; set; }
    public int Range { get; set; }
    public int Damage { get; set; }
    public int CooldownTicks { get; set; }
    public float Accuracy { get; set; }
    public int MagazineSize { get; set; }
    public int ReloadTicks { get; set; }

    /// <summary>
    /// Create WeaponData from a definition.
    /// </summary>
    public static WeaponData FromDef(WeaponDef def) => new()
    {
        Id = def.Id,
        Name = def.Name,
        Range = def.Range,
        Damage = def.Damage,
        CooldownTicks = def.CooldownTicks,
        Accuracy = def.Accuracy,
        MagazineSize = def.MagazineSize,
        ReloadTicks = def.ReloadTicks
    };

    /// <summary>
    /// Create WeaponData by looking up definition ID.
    /// </summary>
    public static WeaponData FromId(string id) => FromDef(Definitions.Weapons.Get(id));

    // Legacy accessors for compatibility
    public static WeaponData DefaultRifle => FromId(WeaponIds.Rifle);
    public static WeaponData DefaultPistol => FromId(WeaponIds.Pistol);
}
