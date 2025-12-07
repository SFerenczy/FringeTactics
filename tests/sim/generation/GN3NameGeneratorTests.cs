using GdUnit4;
using static GdUnit4.Assertions;

namespace FringeTactics.Tests;

[TestSuite]
public class GN3NameGeneratorTests
{
    private RngService rngService;
    private RngStream rng;

    [Before]
    public void Setup()
    {
        rngService = new RngService(12345);
        rng = rngService.Campaign;
    }

    // ========================================================================
    // NPC Name Tests
    // ========================================================================

    [TestCase]
    public void GenerateNpcName_ReturnsNonEmptyString()
    {
        string name = NameGenerator.GenerateNpcName(rng);

        AssertString(name).IsNotEmpty();
    }

    [TestCase]
    public void GenerateNpcName_ContainsFirstAndLastName()
    {
        string name = NameGenerator.GenerateNpcName(rng, includeNickname: false);

        // Should have exactly one space (first last)
        var parts = name.Split(' ');
        AssertInt(parts.Length).IsEqual(2);
    }

    [TestCase]
    public void GenerateNpcName_WithNickname_ContainsQuotes()
    {
        // Generate many names to find one with a nickname (20% chance)
        bool foundNickname = false;
        for (int i = 0; i < 50; i++)
        {
            string name = NameGenerator.GenerateNpcName(rng, includeNickname: true);
            if (name.Contains('"'))
            {
                foundNickname = true;
                // Nickname format: First "Nickname" Last
                AssertBool(name.Contains("\"")).IsTrue();
                break;
            }
        }
        AssertBool(foundNickname).IsTrue();
    }

    [TestCase]
    public void GenerateNpcName_NoNickname_NeverContainsQuotes()
    {
        for (int i = 0; i < 20; i++)
        {
            string name = NameGenerator.GenerateNpcName(rng, includeNickname: false);
            AssertBool(name.Contains('"')).IsFalse();
        }
    }

    [TestCase]
    public void GenerateNpcName_IsDeterministic()
    {
        var rng1 = new RngService(99999).Campaign;
        var rng2 = new RngService(99999).Campaign;

        string name1 = NameGenerator.GenerateNpcName(rng1);
        string name2 = NameGenerator.GenerateNpcName(rng2);

        AssertString(name1).IsEqual(name2);
    }

    [TestCase]
    public void GeneratePirateName_AlwaysHasNickname()
    {
        for (int i = 0; i < 20; i++)
        {
            string name = NameGenerator.GeneratePirateName(rng);
            // Pirate names are "First Nickname" (no quotes, no last name)
            AssertString(name).IsNotEmpty();
            var parts = name.Split(' ');
            AssertBool(parts.Length >= 2).IsTrue();
        }
    }

    [TestCase]
    public void GenerateFirstName_ReturnsSingleWord()
    {
        string name = NameGenerator.GenerateFirstName(rng);

        AssertString(name).IsNotEmpty();
        AssertBool(name.Contains(' ')).IsFalse();
    }

    // ========================================================================
    // Cargo Type Tests
    // ========================================================================

    [TestCase]
    public void GenerateCargoType_Legal_ReturnsNonEmptyString()
    {
        string cargo = NameGenerator.GenerateCargoType(rng);

        AssertString(cargo).IsNotEmpty();
    }

    [TestCase]
    public void GenerateCargoType_Illegal_ReturnsIllegalCargo()
    {
        string cargo = NameGenerator.GenerateCargoType(rng, illegal: true);

        AssertString(cargo).IsNotEmpty();
        // Illegal cargo should contain certain keywords
        bool isIllegal = cargo.Contains("weapons") || cargo.Contains("contraband") ||
                         cargo.Contains("stolen") || cargo.Contains("smuggled") ||
                         cargo.Contains("black market") || cargo.Contains("banned") ||
                         cargo.Contains("unlicensed") || cargo.Contains("forged") ||
                         cargo.Contains("pirated") || cargo.Contains("counterfeit") ||
                         cargo.Contains("restricted") || cargo.Contains("unregistered");
        AssertBool(isIllegal).IsTrue();
    }

    [TestCase]
    public void GenerateCargoType_Valuable_ReturnsValuableCargo()
    {
        string cargo = NameGenerator.GenerateCargoType(rng, valuable: true);

        AssertString(cargo).IsNotEmpty();
        // Valuable cargo should contain certain keywords
        bool isValuable = cargo.Contains("rare") || cargo.Contains("prototype") ||
                          cargo.Contains("encrypted") || cargo.Contains("luxury") ||
                          cargo.Contains("antique") || cargo.Contains("research") ||
                          cargo.Contains("corporate") || cargo.Contains("exotic") ||
                          cargo.Contains("precision") || cargo.Contains("bioengineered") ||
                          cargo.Contains("quantum") || cargo.Contains("archaeological") ||
                          cargo.Contains("art") || cargo.Contains("isotopes");
        AssertBool(isValuable).IsTrue();
    }

    [TestCase]
    public void GenerateCargoType_IllegalTakesPrecedence()
    {
        // When both illegal and valuable are true, illegal should win
        string cargo = NameGenerator.GenerateCargoType(rng, illegal: true, valuable: true);

        // Should be from illegal pool
        bool isIllegal = cargo.Contains("weapons") || cargo.Contains("contraband") ||
                         cargo.Contains("stolen") || cargo.Contains("smuggled") ||
                         cargo.Contains("black market") || cargo.Contains("banned") ||
                         cargo.Contains("unlicensed") || cargo.Contains("forged") ||
                         cargo.Contains("pirated") || cargo.Contains("counterfeit") ||
                         cargo.Contains("restricted") || cargo.Contains("unregistered");
        AssertBool(isIllegal).IsTrue();
    }

    [TestCase]
    public void GenerateCargoType_IsDeterministic()
    {
        var rng1 = new RngService(88888).Campaign;
        var rng2 = new RngService(88888).Campaign;

        string cargo1 = NameGenerator.GenerateCargoType(rng1);
        string cargo2 = NameGenerator.GenerateCargoType(rng2);

        AssertString(cargo1).IsEqual(cargo2);
    }

    [TestCase]
    public void GenerateRandomCargoType_ReturnsNonEmptyString()
    {
        string cargo = NameGenerator.GenerateRandomCargoType(rng);

        AssertString(cargo).IsNotEmpty();
    }

    [TestCase]
    public void GenerateRandomCargoType_ProducesVariety()
    {
        var cargos = new System.Collections.Generic.HashSet<string>();
        for (int i = 0; i < 30; i++)
        {
            cargos.Add(NameGenerator.GenerateRandomCargoType(rng));
        }
        // Should have at least 5 different cargo types
        AssertBool(cargos.Count >= 5).IsTrue();
    }

    // ========================================================================
    // Ship Name Tests
    // ========================================================================

    [TestCase]
    public void GenerateShipName_ReturnsNonEmptyString()
    {
        string name = NameGenerator.GenerateShipName(rng);

        AssertString(name).IsNotEmpty();
    }

    [TestCase]
    public void GenerateShipName_HasPrefixAndName()
    {
        string name = NameGenerator.GenerateShipName(rng);

        var parts = name.Split(' ');
        AssertInt(parts.Length).IsEqual(2);
        // Prefix should be 3 uppercase letters
        AssertInt(parts[0].Length).IsEqual(3);
    }

    [TestCase]
    public void GenerateShipName_IsDeterministic()
    {
        var rng1 = new RngService(77777).Campaign;
        var rng2 = new RngService(77777).Campaign;

        string name1 = NameGenerator.GenerateShipName(rng1);
        string name2 = NameGenerator.GenerateShipName(rng2);

        AssertString(name1).IsEqual(name2);
    }

    [TestCase]
    public void GenerateShipNameSimple_ReturnsNameWithoutPrefix()
    {
        string name = NameGenerator.GenerateShipNameSimple(rng);

        AssertString(name).IsNotEmpty();
        // Should be a single word (no prefix)
        AssertBool(name.Contains(' ')).IsFalse();
    }

    [TestCase]
    public void GeneratePirateShipName_ReturnsNonEmptyString()
    {
        string name = NameGenerator.GeneratePirateShipName(rng);

        AssertString(name).IsNotEmpty();
    }

    [TestCase]
    public void GeneratePirateShipName_ProducesVariety()
    {
        var names = new System.Collections.Generic.HashSet<string>();
        for (int i = 0; i < 20; i++)
        {
            names.Add(NameGenerator.GeneratePirateShipName(rng));
        }
        // Should have at least 5 different names
        AssertBool(names.Count >= 5).IsTrue();
    }

    // ========================================================================
    // Variety Tests
    // ========================================================================

    [TestCase]
    public void GenerateNpcName_ProducesVariety()
    {
        var names = new System.Collections.Generic.HashSet<string>();
        for (int i = 0; i < 30; i++)
        {
            names.Add(NameGenerator.GenerateNpcName(rng));
        }
        // Should have at least 20 different names
        AssertBool(names.Count >= 20).IsTrue();
    }

    [TestCase]
    public void GenerateShipName_ProducesVariety()
    {
        var names = new System.Collections.Generic.HashSet<string>();
        for (int i = 0; i < 30; i++)
        {
            names.Add(NameGenerator.GenerateShipName(rng));
        }
        // Should have at least 15 different names
        AssertBool(names.Count >= 15).IsTrue();
    }
}
