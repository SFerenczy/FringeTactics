namespace FringeTactics;

/// <summary>
/// Centralized stat formulas for converting campaign stats to tactical values.
/// All balance-critical constants and formulas in one place.
/// </summary>
public static class StatFormulas
{
    // === Accuracy Formula ===
    // Base accuracy + bonus per Aim point
    public const float BaseAccuracy = 0.7f;
    public const float AccuracyPerAim = 0.02f;
    
    // === Move Speed Formula ===
    // Base speed + bonus per Reflexes point
    public const float BaseMoveSpeed = 2.0f;
    public const float MoveSpeedPerReflexes = 0.1f;
    
    // === Ammo Formula ===
    // Number of magazine reloads to give as reserve
    public const int ReserveAmmoMagazines = 3;
    
    // Total magazines for ammo calculation (1 loaded + reserves)
    public const int TotalMagazinesForMission = 4;
    
    /// <summary>
    /// Calculate tactical accuracy from Aim stat.
    /// </summary>
    public static float CalculateAccuracy(int effectiveAim)
    {
        return BaseAccuracy + (effectiveAim * AccuracyPerAim);
    }
    
    /// <summary>
    /// Calculate tactical move speed from Reflexes stat.
    /// </summary>
    public static float CalculateMoveSpeed(int effectiveReflexes)
    {
        return BaseMoveSpeed + (effectiveReflexes * MoveSpeedPerReflexes);
    }
    
    /// <summary>
    /// Calculate reserve ammo for a weapon, capped by available campaign ammo.
    /// </summary>
    public static int CalculateReserveAmmo(int magazineSize, int campaignAmmo)
    {
        int desired = magazineSize * ReserveAmmoMagazines;
        return System.Math.Min(desired, campaignAmmo);
    }
    
    /// <summary>
    /// Calculate total ammo needed for a weapon (magazine + reserves).
    /// </summary>
    public static int CalculateTotalAmmoNeeded(int magazineSize)
    {
        return magazineSize * TotalMagazinesForMission;
    }
}
