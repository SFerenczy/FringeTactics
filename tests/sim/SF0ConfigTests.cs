using GdUnit4;
using static GdUnit4.Assertions;

namespace FringeTactics.Tests;

/// <summary>
/// SF0 Milestone tests - validates config loading and validation.
/// </summary>
[TestSuite]
public class ValidationResultTests
{
    [TestCase]
    public void NewResult_IsValid()
    {
        var result = new ValidationResult();
        AssertThat(result.IsValid).IsTrue();
        AssertThat(result.Errors.Count).IsEqual(0);
    }

    [TestCase]
    public void AddError_MakesInvalid()
    {
        var result = new ValidationResult();
        result.AddError("Test error");
        AssertThat(result.IsValid).IsFalse();
        AssertThat(result.Errors.Count).IsEqual(1);
    }

    [TestCase]
    public void AddWarning_StaysValid()
    {
        var result = new ValidationResult();
        result.AddWarning("Test warning");
        AssertThat(result.IsValid).IsTrue();
        AssertThat(result.Warnings.Count).IsEqual(1);
    }

    [TestCase]
    public void Merge_CombinesErrorsAndWarnings()
    {
        var result1 = new ValidationResult();
        result1.AddError("Error 1");
        result1.AddWarning("Warning 1");

        var result2 = new ValidationResult();
        result2.AddError("Error 2");

        result1.Merge(result2);

        AssertThat(result1.Errors.Count).IsEqual(2);
        AssertThat(result1.Warnings.Count).IsEqual(1);
    }

    [TestCase]
    public void StaticError_CreatesInvalidResult()
    {
        var result = ValidationResult.Error("Test error");
        AssertThat(result.IsValid).IsFalse();
        AssertThat(result.Errors[0]).IsEqual("Test error");
    }

    [TestCase]
    public void StaticSuccess_CreatesValidResult()
    {
        var result = ValidationResult.Success();
        AssertThat(result.IsValid).IsTrue();
    }
}

[TestSuite]
public class WeaponDefValidationTests
{
    [TestCase]
    public void ValidWeapon_PassesValidation()
    {
        var weapon = new WeaponDef
        {
            Id = "test_weapon",
            Name = "Test Weapon",
            Damage = 25,
            Range = 8,
            CooldownTicks = 10,
            Accuracy = 0.7f,
            MagazineSize = 30,
            ReloadTicks = 40
        };

        var result = weapon.Validate();
        AssertThat(result.IsValid).IsTrue();
    }

    [TestCase]
    public void MissingId_FailsValidation()
    {
        var weapon = new WeaponDef
        {
            Name = "Test Weapon",
            Damage = 25,
            Range = 8
        };

        var result = weapon.Validate();
        AssertThat(result.IsValid).IsFalse();
        AssertThat(result.Errors.Exists(e => e.Contains("Id"))).IsTrue();
    }

    [TestCase]
    public void NegativeDamage_FailsValidation()
    {
        var weapon = new WeaponDef
        {
            Id = "test",
            Name = "Test",
            Damage = -5,
            Range = 8
        };

        var result = weapon.Validate();
        AssertThat(result.IsValid).IsFalse();
        AssertThat(result.Errors.Exists(e => e.Contains("Damage"))).IsTrue();
    }

    [TestCase]
    public void ZeroDamage_FailsValidation()
    {
        var weapon = new WeaponDef
        {
            Id = "test",
            Name = "Test",
            Damage = 0,
            Range = 8
        };

        var result = weapon.Validate();
        AssertThat(result.IsValid).IsFalse();
    }

    [TestCase]
    public void AccuracyOutOfRange_FailsValidation()
    {
        var weapon = new WeaponDef
        {
            Id = "test",
            Name = "Test",
            Damage = 25,
            Range = 8,
            Accuracy = 1.5f
        };

        var result = weapon.Validate();
        AssertThat(result.IsValid).IsFalse();
        AssertThat(result.Errors.Exists(e => e.Contains("Accuracy"))).IsTrue();
    }

    [TestCase]
    public void NegativeAccuracy_FailsValidation()
    {
        var weapon = new WeaponDef
        {
            Id = "test",
            Name = "Test",
            Damage = 25,
            Range = 8,
            Accuracy = -0.1f
        };

        var result = weapon.Validate();
        AssertThat(result.IsValid).IsFalse();
    }

    [TestCase]
    public void ZeroRange_FailsValidation()
    {
        var weapon = new WeaponDef
        {
            Id = "test",
            Name = "Test",
            Damage = 25,
            Range = 0
        };

        var result = weapon.Validate();
        AssertThat(result.IsValid).IsFalse();
        AssertThat(result.Errors.Exists(e => e.Contains("Range"))).IsTrue();
    }

    [TestCase]
    public void NegativeCooldown_FailsValidation()
    {
        var weapon = new WeaponDef
        {
            Id = "test",
            Name = "Test",
            Damage = 25,
            Range = 8,
            CooldownTicks = -1
        };

        var result = weapon.Validate();
        AssertThat(result.IsValid).IsFalse();
        AssertThat(result.Errors.Exists(e => e.Contains("CooldownTicks"))).IsTrue();
    }

    [TestCase]
    public void MultipleErrors_AllReported()
    {
        var weapon = new WeaponDef
        {
            // Missing Id, Name
            Damage = -5,  // Invalid
            Range = 0     // Invalid
        };

        var result = weapon.Validate();
        AssertThat(result.IsValid).IsFalse();
        AssertThat(result.Errors.Count).IsGreaterEqual(4);
    }
}

[TestSuite]
public class EnemyDefValidationTests
{
    [TestCase]
    public void ValidEnemy_PassesValidation()
    {
        var enemy = new EnemyDef
        {
            Id = "test_enemy",
            Name = "Test Enemy",
            Hp = 100,
            WeaponId = "rifle",
            Behavior = EnemyBehavior.Aggressive
        };

        var result = enemy.Validate();
        AssertThat(result.IsValid).IsTrue();
    }

    [TestCase]
    public void MissingWeaponId_FailsValidation()
    {
        var enemy = new EnemyDef
        {
            Id = "test_enemy",
            Name = "Test Enemy",
            Hp = 100
        };

        var result = enemy.Validate();
        AssertThat(result.IsValid).IsFalse();
        AssertThat(result.Errors.Exists(e => e.Contains("WeaponId"))).IsTrue();
    }

    [TestCase]
    public void ZeroHp_FailsValidation()
    {
        var enemy = new EnemyDef
        {
            Id = "test",
            Name = "Test",
            Hp = 0,
            WeaponId = "rifle"
        };

        var result = enemy.Validate();
        AssertThat(result.IsValid).IsFalse();
        AssertThat(result.Errors.Exists(e => e.Contains("Hp"))).IsTrue();
    }
}

[TestSuite]
public class AbilityDefValidationTests
{
    [TestCase]
    public void ValidAbility_PassesValidation()
    {
        var ability = new AbilityDef
        {
            Id = "test_ability",
            Name = "Test Ability",
            Type = AbilityType.Grenade,
            Range = 6,
            Cooldown = 60,
            Delay = 20,
            Radius = 2,
            Damage = 40
        };

        var result = ability.Validate();
        AssertThat(result.IsValid).IsTrue();
    }

    [TestCase]
    public void NegativeRange_FailsValidation()
    {
        var ability = new AbilityDef
        {
            Id = "test",
            Name = "Test",
            Range = -1
        };

        var result = ability.Validate();
        AssertThat(result.IsValid).IsFalse();
        AssertThat(result.Errors.Exists(e => e.Contains("Range"))).IsTrue();
    }

    [TestCase]
    public void NegativeCooldown_FailsValidation()
    {
        var ability = new AbilityDef
        {
            Id = "test",
            Name = "Test",
            Cooldown = -1
        };

        var result = ability.Validate();
        AssertThat(result.IsValid).IsFalse();
        AssertThat(result.Errors.Exists(e => e.Contains("Cooldown"))).IsTrue();
    }

    [TestCase]
    public void ZeroRange_IsValid()
    {
        var ability = new AbilityDef
        {
            Id = "test",
            Name = "Test",
            Range = 0  // Self-targeted abilities can have 0 range
        };

        var result = ability.Validate();
        AssertThat(result.IsValid).IsTrue();
    }
}

[TestSuite]
public class ConfigRegistryTests
{
    [TestCase]
    public void Load_SetsIsLoaded()
    {
        var registry = new ConfigRegistry();
        registry.Load();
        AssertThat(registry.IsLoaded).IsTrue();
    }

    [TestCase]
    public void Load_PopulatesLastLoadResult()
    {
        var registry = new ConfigRegistry();
        var result = registry.Load();
        AssertThat(registry.LastLoadResult).IsNotNull();
        AssertThat(registry.LastLoadResult).IsSame(result);
    }

    [TestCase]
    public void Load_CountsItemsLoaded()
    {
        var registry = new ConfigRegistry();
        var result = registry.Load();
        AssertThat(result.ItemsLoaded).IsGreater(0);
    }

    [TestCase]
    public void Weapons_HasCount()
    {
        var registry = new ConfigRegistry();
        registry.Load();
        AssertThat(registry.Weapons.Count).IsGreater(0);
    }

    [TestCase]
    public void Enemies_HasCount()
    {
        var registry = new ConfigRegistry();
        registry.Load();
        AssertThat(registry.Enemies.Count).IsGreater(0);
    }

    [TestCase]
    public void Abilities_HasCount()
    {
        var registry = new ConfigRegistry();
        registry.Load();
        AssertThat(registry.Abilities.Count).IsGreater(0);
    }
}

[TestSuite]
public class DefinitionsTests
{
    [TestCase]
    public void Weapons_LoadsFromJson()
    {
        AssertThat(Definitions.Weapons.Count).IsGreater(0);
    }

    [TestCase]
    public void Enemies_LoadsFromJson()
    {
        AssertThat(Definitions.Enemies.Count).IsGreater(0);
    }

    [TestCase]
    public void Abilities_LoadsFromJson()
    {
        AssertThat(Definitions.Abilities.Count).IsGreater(0);
    }

    [TestCase]
    public void GetLastLoadResult_ReturnsResult()
    {
        // Force load
        var _ = Definitions.Weapons;
        var result = Definitions.GetLastLoadResult();
        AssertThat(result).IsNotNull();
    }

    [TestCase]
    public void GetRegistry_ReturnsRegistry()
    {
        var registry = Definitions.GetRegistry();
        AssertThat(registry).IsNotNull();
        AssertThat(registry.IsLoaded).IsTrue();
    }
}
