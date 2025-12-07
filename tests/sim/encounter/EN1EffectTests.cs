using GdUnit4;
using static GdUnit4.Assertions;

namespace FringeTactics.Tests;

[TestSuite]
public class EN1EffectTests
{
    [TestCase]
    public void AddCredits_CreatesCorrectEffect()
    {
        var effect = EncounterEffect.AddCredits(100);

        AssertObject(effect.Type).IsEqual(EffectType.AddResource);
        AssertString(effect.TargetId).IsEqual(ResourceTypes.Money);
        AssertInt(effect.Amount).IsEqual(100);
    }

    [TestCase]
    public void LoseCredits_CreatesNegativeAmount()
    {
        var effect = EncounterEffect.LoseCredits(50);

        AssertObject(effect.Type).IsEqual(EffectType.AddResource);
        AssertString(effect.TargetId).IsEqual(ResourceTypes.Money);
        AssertInt(effect.Amount).IsEqual(-50);
    }

    [TestCase]
    public void AddFuel_CreatesCorrectEffect()
    {
        var effect = EncounterEffect.AddFuel(25);

        AssertObject(effect.Type).IsEqual(EffectType.AddResource);
        AssertString(effect.TargetId).IsEqual(ResourceTypes.Fuel);
        AssertInt(effect.Amount).IsEqual(25);
    }

    [TestCase]
    public void ShipDamage_CreatesCorrectEffect()
    {
        var effect = EncounterEffect.ShipDamage(15);

        AssertObject(effect.Type).IsEqual(EffectType.ShipDamage);
        AssertInt(effect.Amount).IsEqual(15);
    }

    [TestCase]
    public void CrewInjury_DefaultsToWounded()
    {
        var effect = EncounterEffect.CrewInjury();

        AssertObject(effect.Type).IsEqual(EffectType.CrewInjury);
        AssertString(effect.StringParam).IsEqual(InjuryTypes.Wounded);
    }

    [TestCase]
    public void CrewInjury_AcceptsCustomType()
    {
        var effect = EncounterEffect.CrewInjury(InjuryTypes.Critical);

        AssertObject(effect.Type).IsEqual(EffectType.CrewInjury);
        AssertString(effect.StringParam).IsEqual(InjuryTypes.Critical);
    }

    [TestCase]
    public void CrewXp_CreatesCorrectEffect()
    {
        var effect = EncounterEffect.CrewXp(20);

        AssertObject(effect.Type).IsEqual(EffectType.CrewXp);
        AssertInt(effect.Amount).IsEqual(20);
    }

    [TestCase]
    public void FactionRep_CreatesCorrectEffect()
    {
        var effect = EncounterEffect.FactionRep("pirates", -10);

        AssertObject(effect.Type).IsEqual(EffectType.FactionRep);
        AssertString(effect.TargetId).IsEqual("pirates");
        AssertInt(effect.Amount).IsEqual(-10);
    }

    [TestCase]
    public void TimeDelay_CreatesCorrectEffect()
    {
        var effect = EncounterEffect.TimeDelay(3);

        AssertObject(effect.Type).IsEqual(EffectType.TimeDelay);
        AssertInt(effect.Amount).IsEqual(3);
    }

    [TestCase]
    public void GotoNode_CreatesCorrectEffect()
    {
        var effect = EncounterEffect.GotoNode("next_node");

        AssertObject(effect.Type).IsEqual(EffectType.GotoNode);
        AssertString(effect.TargetId).IsEqual("next_node");
    }

    [TestCase]
    public void End_CreatesCorrectEffect()
    {
        var effect = EncounterEffect.End();

        AssertObject(effect.Type).IsEqual(EffectType.EndEncounter);
    }

    [TestCase]
    public void AddTrait_SetsAddFlag()
    {
        var effect = EncounterEffect.AddTrait("veteran");

        AssertObject(effect.Type).IsEqual(EffectType.CrewTrait);
        AssertString(effect.TargetId).IsEqual("veteran");
        AssertBool(effect.BoolParam).IsTrue();
    }

    [TestCase]
    public void RemoveTrait_ClearsAddFlag()
    {
        var effect = EncounterEffect.RemoveTrait("rookie");

        AssertObject(effect.Type).IsEqual(EffectType.CrewTrait);
        AssertString(effect.TargetId).IsEqual("rookie");
        AssertBool(effect.BoolParam).IsFalse();
    }

    [TestCase]
    public void SetFlag_CreatesCorrectEffect()
    {
        var effect = EncounterEffect.SetFlag("quest_started");

        AssertObject(effect.Type).IsEqual(EffectType.SetFlag);
        AssertString(effect.TargetId).IsEqual("quest_started");
        AssertBool(effect.BoolParam).IsTrue();
    }

    [TestCase]
    public void AddCrew_CreatesCorrectEffect()
    {
        var effect = EncounterEffect.AddCrew("Recruit", "Tech");

        AssertObject(effect.Type).IsEqual(EffectType.AddCrew);
        AssertString(effect.TargetId).IsEqual("Recruit");
        AssertString(effect.StringParam).IsEqual("Tech");
    }

    [TestCase]
    public void AddCrew_DefaultsToSoldier()
    {
        var effect = EncounterEffect.AddCrew("Drifter");

        AssertObject(effect.Type).IsEqual(EffectType.AddCrew);
        AssertString(effect.TargetId).IsEqual("Drifter");
        AssertString(effect.StringParam).IsEqual("Soldier");
    }
}
