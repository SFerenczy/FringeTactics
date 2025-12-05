using GdUnit4;
using static GdUnit4.Assertions;
using System.Collections.Generic;

namespace FringeTactics.Tests;

[TestSuite]
public class GN1GenerationContextTests
{
    // ========================================================================
    // CalculateCrewPower Tests
    // ========================================================================

    [TestCase]
    public void CalculateCrewPower_EmptyCrew_ReturnsZero()
    {
        var crew = new List<CrewMember>();
        int power = GenerationContext.CalculateCrewPower(crew, 0);
        AssertInt(power).IsEqual(0);
    }

    [TestCase]
    public void CalculateCrewPower_NullCrew_ReturnsZero()
    {
        int power = GenerationContext.CalculateCrewPower(null, 0);
        AssertInt(power).IsEqual(0);
    }

    [TestCase]
    public void CalculateCrewPower_SingleSoldier_CalculatesCorrectly()
    {
        // Soldier stats: Grit=3, Reflexes=2, Aim=3, Tech=0, Savvy=0, Resolve=2
        var crew = new List<CrewMember>
        {
            CrewMember.CreateWithRole(1, "Test", CrewRole.Soldier)
        };
        
        // Level 1 + Aim 3 + Grit 3 + Reflexes 2 = 9
        int power = GenerationContext.CalculateCrewPower(crew, 0);
        AssertInt(power).IsEqual(9);
    }

    [TestCase]
    public void CalculateCrewPower_IncludesExperienceBonus()
    {
        var crew = new List<CrewMember>
        {
            CrewMember.CreateWithRole(1, "Test", CrewRole.Soldier)
        };
        
        // Base 9 + (5 missions * 2) = 19
        int power = GenerationContext.CalculateCrewPower(crew, 5);
        AssertInt(power).IsEqual(19);
    }

    [TestCase]
    public void CalculateCrewPower_ExcludesDeadCrew()
    {
        var alive = CrewMember.CreateWithRole(1, "Alive", CrewRole.Soldier);
        var dead = CrewMember.CreateWithRole(2, "Dead", CrewRole.Soldier);
        dead.IsDead = true;

        var crew = new List<CrewMember> { alive, dead };
        int power = GenerationContext.CalculateCrewPower(crew, 0);

        // Only alive crew counted (9)
        AssertInt(power).IsEqual(9);
    }

    [TestCase]
    public void CalculateCrewPower_ExcludesCriticallyInjured()
    {
        var healthy = CrewMember.CreateWithRole(1, "Healthy", CrewRole.Soldier);
        var critical = CrewMember.CreateWithRole(2, "Critical", CrewRole.Soldier);
        critical.AddInjury("critical");

        var crew = new List<CrewMember> { healthy, critical };
        int power = GenerationContext.CalculateCrewPower(crew, 0);

        // Only healthy crew counted (9)
        AssertInt(power).IsEqual(9);
    }

    [TestCase]
    public void CalculateCrewPower_MultipleCrew_SumsCorrectly()
    {
        var crew = new List<CrewMember>
        {
            CrewMember.CreateWithRole(1, "Soldier", CrewRole.Soldier),  // 9
            CrewMember.CreateWithRole(2, "Medic", CrewRole.Medic),      // 1+1+2+1 = 5
            CrewMember.CreateWithRole(3, "Scout", CrewRole.Scout)       // 1+2+2+3 = 8
        };

        int power = GenerationContext.CalculateCrewPower(crew, 0);
        AssertInt(power).IsEqual(22);  // 9 + 5 + 8
    }

    // ========================================================================
    // PowerTier Tests
    // ========================================================================

    [TestCase]
    public void PlayerTier_Rookie_Under30Power()
    {
        var context = new GenerationContext { CrewPower = 25 };
        AssertThat(context.PlayerTier).IsEqual(PowerTier.Rookie);
    }

    [TestCase]
    public void PlayerTier_Rookie_Exactly30()
    {
        var context = new GenerationContext { CrewPower = 30 };
        AssertThat(context.PlayerTier).IsEqual(PowerTier.Rookie);
    }

    [TestCase]
    public void PlayerTier_Competent_31To60()
    {
        var context = new GenerationContext { CrewPower = 45 };
        AssertThat(context.PlayerTier).IsEqual(PowerTier.Competent);
    }

    [TestCase]
    public void PlayerTier_Veteran_61To100()
    {
        var context = new GenerationContext { CrewPower = 80 };
        AssertThat(context.PlayerTier).IsEqual(PowerTier.Veteran);
    }

    [TestCase]
    public void PlayerTier_Elite_Over100()
    {
        var context = new GenerationContext { CrewPower = 150 };
        AssertThat(context.PlayerTier).IsEqual(PowerTier.Elite);
    }

    // ========================================================================
    // Reputation Helper Tests
    // ========================================================================

    [TestCase]
    public void GetReputation_UnknownFaction_Returns50()
    {
        var context = new GenerationContext();
        AssertInt(context.GetReputation("unknown")).IsEqual(50);
    }

    [TestCase]
    public void GetReputation_KnownFaction_ReturnsValue()
    {
        var context = new GenerationContext
        {
            FactionRep = new Dictionary<string, int> { { "corp", 75 } }
        };
        AssertInt(context.GetReputation("corp")).IsEqual(75);
    }

    [TestCase]
    public void IsHostileWith_Under25_ReturnsTrue()
    {
        var context = new GenerationContext
        {
            FactionRep = new Dictionary<string, int> { { "pirates", 20 } }
        };
        AssertBool(context.IsHostileWith("pirates")).IsTrue();
    }

    [TestCase]
    public void IsHostileWith_25OrMore_ReturnsFalse()
    {
        var context = new GenerationContext
        {
            FactionRep = new Dictionary<string, int> { { "corp", 25 } }
        };
        AssertBool(context.IsHostileWith("corp")).IsFalse();
    }

    [TestCase]
    public void IsFriendlyWith_75OrMore_ReturnsTrue()
    {
        var context = new GenerationContext
        {
            FactionRep = new Dictionary<string, int> { { "corp", 75 } }
        };
        AssertBool(context.IsFriendlyWith("corp")).IsTrue();
    }

    [TestCase]
    public void IsFriendlyWith_Under75_ReturnsFalse()
    {
        var context = new GenerationContext
        {
            FactionRep = new Dictionary<string, int> { { "corp", 74 } }
        };
        AssertBool(context.IsFriendlyWith("corp")).IsFalse();
    }

    // ========================================================================
    // HasRole Tests
    // ========================================================================

    [TestCase]
    public void HasRole_RolePresent_ReturnsTrue()
    {
        var context = new GenerationContext
        {
            CrewRoles = new List<CrewRole> { CrewRole.Soldier, CrewRole.Tech }
        };
        AssertBool(context.HasRole(CrewRole.Tech)).IsTrue();
    }

    [TestCase]
    public void HasRole_RoleAbsent_ReturnsFalse()
    {
        var context = new GenerationContext
        {
            CrewRoles = new List<CrewRole> { CrewRole.Soldier }
        };
        AssertBool(context.HasRole(CrewRole.Medic)).IsFalse();
    }

    // ========================================================================
    // FromCampaign Tests
    // ========================================================================

    [TestCase]
    public void FromCampaign_BuildsContextCorrectly()
    {
        var campaign = CampaignState.CreateNew(12345);
        var context = GenerationContext.FromCampaign(campaign);

        AssertInt(context.CrewCount).IsEqual(4);
        AssertThat(context.CrewPower).IsGreater(0);
        AssertInt(context.CurrentNodeId).IsEqual(0);
        AssertInt(context.Money).IsEqual(200);
        AssertThat(context.Rng).IsNotNull();
    }

    [TestCase]
    public void FromCampaign_ExtractsCrewRoles()
    {
        var campaign = CampaignState.CreateNew(12345);
        var context = GenerationContext.FromCampaign(campaign);

        // Default campaign has 4 crew with different roles
        AssertThat(context.CrewRoles.Count).IsGreater(0);
    }
}
