using System.Collections.Generic;

namespace FringeTactics;

public enum CrewRole
{
    Soldier,
    Medic,
    Tech,
    Scout
}

/// <summary>
/// Standard injury type identifiers.
/// </summary>
public static class InjuryTypes
{
    public const string Wounded = "wounded";
    public const string Critical = "critical";
    public const string Concussed = "concussed";
    public const string Bleeding = "bleeding";
}

public class CrewMember
{
    public int Id { get; set; }
    public string Name { get; set; }
    public CrewRole Role { get; set; } = CrewRole.Soldier;

    // Status
    public bool IsDead { get; set; } = false;
    public List<string> Injuries { get; set; } = new();

    // Progression
    public int Level { get; set; } = 1;
    public int Xp { get; set; } = 0;
    public const int XP_PER_LEVEL = 100;

    // Stats (affect combat performance)
    public int Aim { get; set; } = 0;       // +hit chance
    public int Toughness { get; set; } = 0; // +HP
    public int Reflexes { get; set; } = 0;  // +dodge (future)

    // Equipment preference
    public string PreferredWeaponId { get; set; } = "rifle";

    public CrewMember(int memberId, string memberName)
    {
        Id = memberId;
        Name = memberName;
    }

    /// <summary>
    /// Add XP and check for level up.
    /// </summary>
    public bool AddXp(int amount)
    {
        Xp += amount;
        if (Xp >= XP_PER_LEVEL)
        {
            Xp -= XP_PER_LEVEL;
            Level++;
            return true; // Leveled up
        }
        return false;
    }

    /// <summary>
    /// Check if crew member can deploy (alive and not critically injured).
    /// </summary>
    public bool CanDeploy()
    {
        if (IsDead) return false;
        // Critical injuries prevent deployment
        return !Injuries.Contains("critical");
    }

    /// <summary>
    /// Add an injury. Returns true if it's a new injury.
    /// </summary>
    public bool AddInjury(string injury)
    {
        if (!Injuries.Contains(injury))
        {
            Injuries.Add(injury);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Heal an injury (e.g., after using meds).
    /// </summary>
    public bool HealInjury(string injury)
    {
        return Injuries.Remove(injury);
    }

    /// <summary>
    /// Get effective HP based on toughness.
    /// </summary>
    public int GetMaxHp()
    {
        return 100 + (Toughness * 10);
    }

    /// <summary>
    /// Get status string for display.
    /// </summary>
    public string GetStatusText()
    {
        if (IsDead) return "DEAD";
        if (Injuries.Count > 0) return $"Injured ({Injuries.Count})";
        return "Ready";
    }
}
