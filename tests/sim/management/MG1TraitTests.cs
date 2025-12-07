using GdUnit4;
using static GdUnit4.Assertions;
using System.Linq;

namespace FringeTactics.Tests;

/// <summary>
/// MG1 Phase 2 tests - validates trait system.
/// </summary>
[TestSuite]
public class MG1TraitTests
{
    // === TraitRegistry Tests ===

    [TestCase]
    public void TraitRegistry_HasDefaultTraits()
    {
        TraitRegistry.EnsureInitialized();

        AssertThat(TraitRegistry.Has("ex_military")).IsTrue();
        AssertThat(TraitRegistry.Has("brave")).IsTrue();
        AssertThat(TraitRegistry.Has("damaged_eye")).IsTrue();
        AssertThat(TraitRegistry.Has("nonexistent")).IsFalse();
    }

    [TestCase]
    public void TraitRegistry_Get_ReturnsCorrectTrait()
    {
        var trait = TraitRegistry.Get("ex_military");

        AssertThat(trait).IsNotNull();
        AssertThat(trait.Name).IsEqual("Ex-Military");
        AssertThat(trait.Category).IsEqual(TraitCategory.Background);
        AssertThat(trait.IsPermanent).IsFalse();
    }

    [TestCase]
    public void TraitRegistry_Get_ReturnsNullForUnknown()
    {
        var trait = TraitRegistry.Get("nonexistent_trait");
        AssertThat(trait).IsNull();
    }

    [TestCase]
    public void TraitRegistry_GetByCategory_FiltersCorrectly()
    {
        var injuries = TraitRegistry.GetByCategory(TraitCategory.Injury).ToList();

        AssertThat(injuries.Count).IsGreaterEqual(4);
        foreach (var trait in injuries)
        {
            AssertThat(trait.Category).IsEqual(TraitCategory.Injury);
            AssertThat(trait.IsPermanent).IsTrue();
        }
    }

    [TestCase]
    public void TraitRegistry_GetByCategory_Background_HasTraits()
    {
        var backgrounds = TraitRegistry.GetByCategory(TraitCategory.Background).ToList();
        AssertThat(backgrounds.Count).IsGreaterEqual(4);
    }

    [TestCase]
    public void TraitRegistry_GetByCategory_Personality_HasTraits()
    {
        var personalities = TraitRegistry.GetByCategory(TraitCategory.Personality).ToList();
        AssertThat(personalities.Count).IsGreaterEqual(4);
    }

    [TestCase]
    public void TraitRegistry_GetByTag_FiltersCorrectly()
    {
        var combatTraits = TraitRegistry.GetByTag("combat").ToList();

        AssertThat(combatTraits.Count).IsGreaterEqual(2);
        foreach (var trait in combatTraits)
        {
            AssertThat(trait.Tags.Contains("combat")).IsTrue();
        }
    }

    // === TraitDef Tests ===

    [TestCase]
    public void TraitDef_GetModifierFor_ReturnsCorrectValue()
    {
        var trait = TraitRegistry.Get("ex_military");

        int aimMod = trait.GetModifierFor(CrewStatType.Aim);
        int gritMod = trait.GetModifierFor(CrewStatType.Grit);

        AssertThat(aimMod).IsEqual(1);
        AssertThat(gritMod).IsEqual(0);
    }

    [TestCase]
    public void TraitDef_MultipleModifiers_AllReturned()
    {
        var trait = TraitRegistry.Get("reckless");

        int aimMod = trait.GetModifierFor(CrewStatType.Aim);
        int gritMod = trait.GetModifierFor(CrewStatType.Grit);

        AssertThat(aimMod).IsEqual(1);
        AssertThat(gritMod).IsEqual(-1);
    }

    // === CrewMember Trait Methods Tests ===

    [TestCase]
    public void CrewMember_AddTrait_AddsSuccessfully()
    {
        var crew = new CrewMember(1, "Test");

        bool result = crew.AddTrait("brave");

        AssertThat(result).IsTrue();
        AssertThat(crew.HasTrait("brave")).IsTrue();
        AssertThat(crew.TraitIds.Count).IsEqual(1);
    }

    [TestCase]
    public void CrewMember_AddTrait_FailsIfAlreadyHas()
    {
        var crew = new CrewMember(1, "Test");
        crew.AddTrait("brave");

        bool result = crew.AddTrait("brave");

        AssertThat(result).IsFalse();
        AssertThat(crew.TraitIds.Count).IsEqual(1);
    }

    [TestCase]
    public void CrewMember_AddTrait_FailsForUnknownTrait()
    {
        var crew = new CrewMember(1, "Test");

        bool result = crew.AddTrait("nonexistent_trait");

        AssertThat(result).IsFalse();
        AssertThat(crew.TraitIds.Count).IsEqual(0);
    }

    [TestCase]
    public void CrewMember_RemoveTrait_RemovesSuccessfully()
    {
        var crew = new CrewMember(1, "Test");
        crew.AddTrait("brave");

        bool result = crew.RemoveTrait("brave");

        AssertThat(result).IsTrue();
        AssertThat(crew.HasTrait("brave")).IsFalse();
        AssertThat(crew.TraitIds.Count).IsEqual(0);
    }

    [TestCase]
    public void CrewMember_RemoveTrait_FailsForPermanent()
    {
        var crew = new CrewMember(1, "Test");
        crew.AddTrait("damaged_eye");

        bool result = crew.RemoveTrait("damaged_eye");

        AssertThat(result).IsFalse();
        AssertThat(crew.HasTrait("damaged_eye")).IsTrue();
    }

    [TestCase]
    public void CrewMember_RemoveTrait_FailsIfNotHad()
    {
        var crew = new CrewMember(1, "Test");

        bool result = crew.RemoveTrait("brave");

        AssertThat(result).IsFalse();
    }

    [TestCase]
    public void CrewMember_GetTraits_ReturnsAllTraitDefs()
    {
        var crew = new CrewMember(1, "Test");
        crew.AddTrait("brave");
        crew.AddTrait("ex_military");

        var traits = crew.GetTraits().ToList();

        AssertThat(traits.Count).IsEqual(2);
        AssertThat(traits.Any(t => t.Id == "brave")).IsTrue();
        AssertThat(traits.Any(t => t.Id == "ex_military")).IsTrue();
    }

    // === Trait Modifier Tests ===

    [TestCase]
    public void CrewMember_GetTraitModifier_SingleTrait()
    {
        var crew = new CrewMember(1, "Test");
        crew.AddTrait("ex_military"); // +1 Aim

        int aimMod = crew.GetTraitModifier(CrewStatType.Aim);
        int gritMod = crew.GetTraitModifier(CrewStatType.Grit);

        AssertThat(aimMod).IsEqual(1);
        AssertThat(gritMod).IsEqual(0);
    }

    [TestCase]
    public void CrewMember_GetTraitModifier_MultipleTraits_Stack()
    {
        var crew = new CrewMember(1, "Test");
        crew.AddTrait("ex_military"); // +1 Aim
        crew.AddTrait("reckless");    // +1 Aim, -1 Grit

        int aimMod = crew.GetTraitModifier(CrewStatType.Aim);
        int gritMod = crew.GetTraitModifier(CrewStatType.Grit);

        AssertThat(aimMod).IsEqual(2);
        AssertThat(gritMod).IsEqual(-1);
    }

    [TestCase]
    public void CrewMember_GetEffectiveStat_IncludesTraitModifiers()
    {
        var crew = new CrewMember(1, "Test") { Aim = 3 };
        crew.AddTrait("ex_military"); // +1 Aim

        int effectiveAim = crew.GetEffectiveStat(CrewStatType.Aim);

        AssertThat(effectiveAim).IsEqual(4);
    }

    [TestCase]
    public void CrewMember_GetEffectiveStat_NegativeModifiers()
    {
        var crew = new CrewMember(1, "Test") { Aim = 5 };
        crew.AddTrait("damaged_eye"); // -2 Aim

        int effectiveAim = crew.GetEffectiveStat(CrewStatType.Aim);

        AssertThat(effectiveAim).IsEqual(3);
    }

    [TestCase]
    public void CrewMember_GetEffectiveStat_CanGoNegative()
    {
        var crew = new CrewMember(1, "Test") { Aim = 1 };
        crew.AddTrait("damaged_eye"); // -2 Aim

        int effectiveAim = crew.GetEffectiveStat(CrewStatType.Aim);

        AssertThat(effectiveAim).IsEqual(-1);
    }

    // === Derived Stats with Traits Tests ===

    [TestCase]
    public void GetMaxHp_IncludesTraitModifiers()
    {
        var crew = new CrewMember(1, "Test") { Grit = 2 };
        crew.AddTrait("frontier_born"); // +1 Grit

        // Effective Grit = 3, MaxHp = 100 + (3 * 10) = 130
        AssertThat(crew.GetMaxHp()).IsEqual(130);
    }

    [TestCase]
    public void GetMaxHp_NegativeTraitReducesHp()
    {
        var crew = new CrewMember(1, "Test") { Grit = 3 };
        crew.AddTrait("reckless"); // -1 Grit

        // Effective Grit = 2, MaxHp = 100 + (2 * 10) = 120
        AssertThat(crew.GetMaxHp()).IsEqual(120);
    }

    [TestCase]
    public void GetHitBonus_IncludesTraitModifiers()
    {
        var crew = new CrewMember(1, "Test") { Aim = 3 };
        crew.AddTrait("ex_military"); // +1 Aim

        // Effective Aim = 4, HitBonus = 4 * 2 = 8
        AssertThat(crew.GetHitBonus()).IsEqual(8);
    }

    [TestCase]
    public void GetHackBonus_IncludesTraitModifiers()
    {
        var crew = new CrewMember(1, "Test") { Tech = 2 };
        crew.AddTrait("corporate"); // +1 Tech

        // Effective Tech = 3, HackBonus = 3 * 10 = 30
        AssertThat(crew.GetHackBonus()).IsEqual(30);
    }

    [TestCase]
    public void GetTalkBonus_IncludesTraitModifiers()
    {
        var crew = new CrewMember(1, "Test") { Savvy = 1 };
        crew.AddTrait("empathetic"); // +2 Savvy

        // Effective Savvy = 3, TalkBonus = 3 * 10 = 30
        AssertThat(crew.GetTalkBonus()).IsEqual(30);
    }

    [TestCase]
    public void GetStressThreshold_IncludesTraitModifiers()
    {
        var crew = new CrewMember(1, "Test") { Resolve = 2 };
        crew.AddTrait("brave"); // +2 Resolve

        // Effective Resolve = 4, StressThreshold = 50 + (4 * 10) = 90
        AssertThat(crew.GetStressThreshold()).IsEqual(90);
    }

    // === Serialization Tests ===

    [TestCase]
    public void CrewMember_GetState_IncludesTraits()
    {
        var crew = new CrewMember(1, "Test");
        crew.AddTrait("brave");
        crew.AddTrait("ex_military");

        var data = crew.GetState();

        AssertThat(data.TraitIds.Count).IsEqual(2);
        AssertThat(data.TraitIds.Contains("brave")).IsTrue();
        AssertThat(data.TraitIds.Contains("ex_military")).IsTrue();
    }

    [TestCase]
    public void CrewMember_FromState_RestoresTraits()
    {
        var data = new CrewMemberData
        {
            Id = 1,
            Name = "Test",
            Role = "Soldier",
            TraitIds = new() { "brave", "frontier_born" }
        };

        var crew = CrewMember.FromState(data);

        AssertThat(crew.HasTrait("brave")).IsTrue();
        AssertThat(crew.HasTrait("frontier_born")).IsTrue();
        AssertThat(crew.TraitIds.Count).IsEqual(2);
    }

    [TestCase]
    public void CrewMember_FromState_HandlesNullTraits()
    {
        var data = new CrewMemberData
        {
            Id = 1,
            Name = "Test",
            Role = "Soldier",
            TraitIds = null
        };

        var crew = CrewMember.FromState(data);

        AssertThat(crew.TraitIds).IsNotNull();
        AssertThat(crew.TraitIds.Count).IsEqual(0);
    }

    [TestCase]
    public void CrewMember_RoundTrip_PreservesTraitsAndModifiers()
    {
        var original = new CrewMember(42, "TraitTest") { Aim = 3, Grit = 2 };
        original.AddTrait("ex_military");
        original.AddTrait("brave");

        var data = original.GetState();
        var restored = CrewMember.FromState(data);

        AssertThat(restored.HasTrait("ex_military")).IsTrue();
        AssertThat(restored.HasTrait("brave")).IsTrue();
        AssertThat(restored.GetEffectiveStat(CrewStatType.Aim)).IsEqual(4); // 3 + 1
        AssertThat(restored.GetEffectiveStat(CrewStatType.Resolve)).IsEqual(2); // 0 + 2
    }

    // === Trait Rolling Tests (G2.5) ===

    [TestCase]
    public void TraitRegistry_GetRollableTraits_ExcludesInjuries()
    {
        var rollable = TraitRegistry.GetRollableTraits();

        foreach (var trait in rollable)
        {
            AssertThat(trait.Category).IsNotEqual(TraitCategory.Injury);
        }
    }

    [TestCase]
    public void TraitRegistry_GetRollableTraits_ExcludesAcquired()
    {
        var rollable = TraitRegistry.GetRollableTraits();

        foreach (var trait in rollable)
        {
            AssertThat(trait.Category).IsNotEqual(TraitCategory.Acquired);
        }
    }

    [TestCase]
    public void TraitRegistry_GetRollableTraits_IncludesBackgroundAndPersonality()
    {
        var rollable = TraitRegistry.GetRollableTraits();

        bool hasBackground = rollable.Any(t => t.Category == TraitCategory.Background);
        bool hasPersonality = rollable.Any(t => t.Category == TraitCategory.Personality);

        AssertThat(hasBackground).IsTrue();
        AssertThat(hasPersonality).IsTrue();
    }

    [TestCase]
    public void TraitRegistry_GetRandomTrait_ReturnsValidTrait()
    {
        var rng = new RngStream("test", 12345);

        var trait = TraitRegistry.GetRandomTrait(rng);

        AssertThat(trait).IsNotNull();
        AssertThat(trait.Category == TraitCategory.Background ||
                   trait.Category == TraitCategory.Personality).IsTrue();
    }

    [TestCase]
    public void TraitRegistry_GetRandomTrait_ReturnsNullWithNullRng()
    {
        var trait = TraitRegistry.GetRandomTrait(null);
        AssertThat(trait).IsNull();
    }

    [TestCase]
    public void TraitRegistry_GetRandomTrait_IsDeterministic()
    {
        var rng1 = new RngStream("test", 12345);
        var rng2 = new RngStream("test", 12345);

        var trait1 = TraitRegistry.GetRandomTrait(rng1);
        var trait2 = TraitRegistry.GetRandomTrait(rng2);

        AssertThat(trait1.Id).IsEqual(trait2.Id);
    }

    [TestCase]
    public void CrewMember_CreateWithRole_RollsTraitWithRng()
    {
        var rng = new RngStream("test", 12345);

        var crew = CrewMember.CreateWithRole(1, "Test", CrewRole.Soldier, rng);

        AssertThat(crew.TraitIds.Count).IsEqual(1);
    }

    [TestCase]
    public void CrewMember_CreateWithRole_NoTraitWithoutRng()
    {
        var crew = CrewMember.CreateWithRole(1, "Test", CrewRole.Soldier, null);

        AssertThat(crew.TraitIds.Count).IsEqual(0);
    }

    [TestCase]
    public void CrewMember_CreateWithRole_TraitIsDeterministic()
    {
        var rng1 = new RngStream("test", 99999);
        var rng2 = new RngStream("test", 99999);

        var crew1 = CrewMember.CreateWithRole(1, "Test1", CrewRole.Soldier, rng1);
        var crew2 = CrewMember.CreateWithRole(2, "Test2", CrewRole.Soldier, rng2);

        AssertThat(crew1.TraitIds[0]).IsEqual(crew2.TraitIds[0]);
    }

    [TestCase]
    public void CampaignState_CreateNew_CrewHaveTraits()
    {
        var campaign = CampaignState.CreateNew(12345);

        foreach (var crew in campaign.Crew)
        {
            AssertThat(crew.TraitIds.Count).IsGreaterEqual(1);
        }
    }
}
