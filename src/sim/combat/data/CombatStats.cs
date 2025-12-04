namespace FringeTactics;

/// <summary>
/// Tracks combat statistics for mission summary.
/// </summary>
public class CombatStats
{
    public int PlayerShotsFired { get; set; } = 0;
    public int PlayerHits { get; set; } = 0;
    public int PlayerMisses { get; set; } = 0;

    public int EnemyShotsFired { get; set; } = 0;
    public int EnemyHits { get; set; } = 0;
    public int EnemyMisses { get; set; } = 0;

    public float PlayerAccuracy => PlayerShotsFired > 0 ? (float)PlayerHits / PlayerShotsFired * 100f : 0f;
    public float EnemyAccuracy => EnemyShotsFired > 0 ? (float)EnemyHits / EnemyShotsFired * 100f : 0f;
}
