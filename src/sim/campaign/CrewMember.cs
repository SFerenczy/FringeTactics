using System;
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
    // Configuration (loaded from data/campaign.json)
    private static CrewStatConfig StatConfig => CampaignConfig.Instance.Crew;

    public int Id { get; set; }
    public string Name { get; set; }
    public CrewRole Role { get; set; } = CrewRole.Soldier;

    // Status
    public bool IsDead { get; set; } = false;
    public List<string> Injuries { get; set; } = new();

    // Progression
    public int Level { get; set; } = 1;
    public int Xp { get; set; } = 0;
    public int UnspentStatPoints { get; set; } = 0;

    // Primary stats (per CAMPAIGN_FOUNDATIONS §3.1)
    public int Grit { get; set; } = 0;      // HP, injury resistance
    public int Reflexes { get; set; } = 0;  // Initiative, dodge
    public int Aim { get; set; } = 0;       // Ranged accuracy
    public int Tech { get; set; } = 0;      // Hacking, repairs
    public int Savvy { get; set; } = 0;     // Social checks
    public int Resolve { get; set; } = 0;   // Stress tolerance

    // Traits
    public List<string> TraitIds { get; set; } = new();

    // Equipment (MG2) - item instance IDs
    public string EquippedWeaponId { get; set; }
    public string EquippedArmorId { get; set; }
    public string EquippedGadgetId { get; set; }

    // Equipment preference (legacy, used when no specific weapon equipped)
    public string PreferredWeaponId { get; set; } = WeaponIds.Rifle;

    public CrewMember(int memberId, string memberName)
    {
        Id = memberId;
        Name = memberName;
    }

    /// <summary>
    /// Add XP and check for level up. Awards stat point on level up.
    /// </summary>
    public bool AddXp(int amount)
    {
        Xp += amount;
        if (Xp >= StatConfig.XpPerLevel)
        {
            Xp -= StatConfig.XpPerLevel;
            Level++;
            UnspentStatPoints++;
            return true;
        }
        return false;
    }

    // === Base Stat Access (single source of truth) ===

    /// <summary>
    /// Get base stat value by type.
    /// </summary>
    public int GetBaseStat(CrewStatType stat) => stat switch
    {
        CrewStatType.Grit => Grit,
        CrewStatType.Reflexes => Reflexes,
        CrewStatType.Aim => Aim,
        CrewStatType.Tech => Tech,
        CrewStatType.Savvy => Savvy,
        CrewStatType.Resolve => Resolve,
        _ => 0
    };

    /// <summary>
    /// Set base stat value by type.
    /// </summary>
    public void SetBaseStat(CrewStatType stat, int value)
    {
        switch (stat)
        {
            case CrewStatType.Grit: Grit = value; break;
            case CrewStatType.Reflexes: Reflexes = value; break;
            case CrewStatType.Aim: Aim = value; break;
            case CrewStatType.Tech: Tech = value; break;
            case CrewStatType.Savvy: Savvy = value; break;
            case CrewStatType.Resolve: Resolve = value; break;
        }
    }

    /// <summary>
    /// Spend a stat point to increase a primary stat.
    /// </summary>
    public bool SpendStatPoint(CrewStatType stat)
    {
        if (UnspentStatPoints <= 0) return false;
        if (GetBaseStat(stat) >= StatConfig.StatCap) return false;

        UnspentStatPoints--;
        SetBaseStat(stat, GetBaseStat(stat) + 1);
        return true;
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

    // === Trait Methods ===

    /// <summary>
    /// Check if crew has a specific trait.
    /// </summary>
    public bool HasTrait(string traitId) => TraitIds.Contains(traitId);

    /// <summary>
    /// Add a trait. Returns false if already has it or trait doesn't exist.
    /// </summary>
    public bool AddTrait(string traitId)
    {
        if (HasTrait(traitId)) return false;
        if (!TraitRegistry.Has(traitId)) return false;
        TraitIds.Add(traitId);
        return true;
    }

    /// <summary>
    /// Remove a trait. Returns false if didn't have it or trait is permanent.
    /// </summary>
    public bool RemoveTrait(string traitId)
    {
        var trait = TraitRegistry.Get(traitId);
        if (trait == null || trait.IsPermanent) return false;
        return TraitIds.Remove(traitId);
    }

    /// <summary>
    /// Get all trait definitions for this crew member.
    /// </summary>
    public IEnumerable<TraitDef> GetTraits()
    {
        foreach (var id in TraitIds)
        {
            var trait = TraitRegistry.Get(id);
            if (trait != null) yield return trait;
        }
    }

    /// <summary>
    /// Calculate total modifier for a stat from all traits.
    /// </summary>
    public int GetTraitModifier(CrewStatType stat)
    {
        int total = 0;
        foreach (var trait in GetTraits())
        {
            total += trait.GetModifierFor(stat);
        }
        return total;
    }

    /// <summary>
    /// Get effective stat value (base + trait modifiers).
    /// </summary>
    public int GetEffectiveStat(CrewStatType stat)
    {
        return GetBaseStat(stat) + GetTraitModifier(stat);
    }

    // === Derived Stats ===

    /// <summary>
    /// Get effective HP based on effective Grit.
    /// </summary>
    public int GetMaxHp() => StatConfig.BaseHp + (GetEffectiveStat(CrewStatType.Grit) * StatConfig.HpPerGrit);

    /// <summary>
    /// Get hit chance bonus from effective Aim.
    /// </summary>
    public int GetHitBonus() => GetEffectiveStat(CrewStatType.Aim) * StatConfig.HitBonusPerAim;

    /// <summary>
    /// Get hacking bonus from effective Tech.
    /// </summary>
    public int GetHackBonus() => GetEffectiveStat(CrewStatType.Tech) * StatConfig.HackBonusPerTech;

    /// <summary>
    /// Get social check bonus from effective Savvy.
    /// </summary>
    public int GetTalkBonus() => GetEffectiveStat(CrewStatType.Savvy) * StatConfig.TalkBonusPerSavvy;

    /// <summary>
    /// Get stress threshold from effective Resolve.
    /// </summary>
    public int GetStressThreshold() => StatConfig.BaseStressThreshold + (GetEffectiveStat(CrewStatType.Resolve) * StatConfig.StressPerResolve);

    /// <summary>
    /// Create a crew member with role-appropriate starting stats.
    /// </summary>
    public static CrewMember CreateWithRole(int id, string name, CrewRole role)
    {
        var crew = new CrewMember(id, name) { Role = role };
        ApplyRoleStats(crew, role);
        return crew;
    }

    private static void ApplyRoleStats(CrewMember crew, CrewRole role)
    {
        var stats = CampaignConfig.Instance.GetRoleStats(role);
        crew.Grit = stats.Grit;
        crew.Reflexes = stats.Reflexes;
        crew.Aim = stats.Aim;
        crew.Tech = stats.Tech;
        crew.Savvy = stats.Savvy;
        crew.Resolve = stats.Resolve;
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

    // ========================================================================
    // EQUIPMENT METHODS (MG2)
    // ========================================================================

    /// <summary>
    /// Get equipped item ID for a slot.
    /// </summary>
    public string GetEquipped(EquipSlot slot) => slot switch
    {
        EquipSlot.Weapon => EquippedWeaponId,
        EquipSlot.Armor => EquippedArmorId,
        EquipSlot.Gadget => EquippedGadgetId,
        _ => null
    };

    /// <summary>
    /// Set equipped item for a slot.
    /// </summary>
    public void SetEquipped(EquipSlot slot, string itemId)
    {
        switch (slot)
        {
            case EquipSlot.Weapon: EquippedWeaponId = itemId; break;
            case EquipSlot.Armor: EquippedArmorId = itemId; break;
            case EquipSlot.Gadget: EquippedGadgetId = itemId; break;
        }
    }

    /// <summary>
    /// Check if crew has any equipment in a slot.
    /// </summary>
    public bool HasEquipped(EquipSlot slot) => !string.IsNullOrEmpty(GetEquipped(slot));

    /// <summary>
    /// Clear equipment from a slot.
    /// </summary>
    public void ClearEquipped(EquipSlot slot) => SetEquipped(slot, null);

    /// <summary>
    /// Get the effective weapon definition ID for tactical missions.
    /// Priority: equipped weapon item > preferred weapon > default rifle.
    /// </summary>
    public string GetEffectiveWeaponId(Inventory inventory)
    {
        // Check equipped weapon item instance
        if (!string.IsNullOrEmpty(EquippedWeaponId) && inventory != null)
        {
            var item = inventory.FindById(EquippedWeaponId);
            if (item != null)
            {
                var itemDef = ItemRegistry.Get(item.DefId);
                if (itemDef != null && itemDef.EquipSlot == EquipSlot.Weapon)
                {
                    return item.DefId;
                }
            }
        }
        
        // Fall back to preferred weapon
        if (!string.IsNullOrEmpty(PreferredWeaponId))
        {
            return PreferredWeaponId;
        }
        
        // Default fallback
        return WeaponIds.Rifle;
    }

    /// <summary>
    /// Get all equipped item IDs.
    /// </summary>
    public List<string> GetAllEquippedIds()
    {
        var result = new List<string>();
        if (!string.IsNullOrEmpty(EquippedWeaponId)) result.Add(EquippedWeaponId);
        if (!string.IsNullOrEmpty(EquippedArmorId)) result.Add(EquippedArmorId);
        if (!string.IsNullOrEmpty(EquippedGadgetId)) result.Add(EquippedGadgetId);
        return result;
    }

    /// <summary>
    /// Get state for serialization.
    /// </summary>
    public CrewMemberData GetState()
    {
        return new CrewMemberData
        {
            Id = Id,
            Name = Name,
            Role = Role.ToString(),
            IsDead = IsDead,
            Injuries = new List<string>(Injuries),
            Level = Level,
            Xp = Xp,
            Grit = Grit,
            Reflexes = Reflexes,
            Aim = Aim,
            Tech = Tech,
            Savvy = Savvy,
            Resolve = Resolve,
            UnspentStatPoints = UnspentStatPoints,
            TraitIds = new List<string>(TraitIds),
            PreferredWeaponId = PreferredWeaponId,
            // Equipment (MG2)
            EquippedWeaponId = EquippedWeaponId,
            EquippedArmorId = EquippedArmorId,
            EquippedGadgetId = EquippedGadgetId
        };
    }

    /// <summary>
    /// Restore from saved state.
    /// </summary>
    public static CrewMember FromState(CrewMemberData data)
    {
        var crew = new CrewMember(data.Id, data.Name)
        {
            Role = Enum.TryParse<CrewRole>(data.Role, out var role) ? role : CrewRole.Soldier,
            IsDead = data.IsDead,
            Injuries = new List<string>(data.Injuries ?? new List<string>()),
            Level = data.Level,
            Xp = data.Xp,
            // Handle legacy saves: Toughness -> Grit
            Grit = data.Grit > 0 ? data.Grit : data.Toughness,
            Reflexes = data.Reflexes,
            Aim = data.Aim,
            Tech = data.Tech,
            Savvy = data.Savvy,
            Resolve = data.Resolve,
            UnspentStatPoints = data.UnspentStatPoints,
            TraitIds = new List<string>(data.TraitIds ?? new List<string>()),
            PreferredWeaponId = data.PreferredWeaponId ?? WeaponIds.Rifle,
            // Equipment (MG2)
            EquippedWeaponId = data.EquippedWeaponId,
            EquippedArmorId = data.EquippedArmorId,
            EquippedGadgetId = data.EquippedGadgetId
        };
        return crew;
    }
}
