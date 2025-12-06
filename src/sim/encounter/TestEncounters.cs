using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// Factory methods for test encounter templates.
/// Used for unit testing and validation of the encounter system.
/// </summary>
public static class TestEncounters
{
    /// <summary>
    /// Simple 2-node encounter with 2 options, no conditions.
    /// Good for basic flow testing.
    /// </summary>
    public static EncounterTemplate CreateSimpleEncounter()
    {
        return new EncounterTemplate
        {
            Id = "test_simple",
            Name = "Simple Test Encounter",
            Tags = new HashSet<string> { "test" },
            EntryNodeId = "start",
            Nodes = new Dictionary<string, EncounterNode>
            {
                ["start"] = new EncounterNode
                {
                    Id = "start",
                    TextKey = "test.simple.start",
                    Options = new List<EncounterOption>
                    {
                        new EncounterOption
                        {
                            Id = "option_a",
                            TextKey = "test.simple.option_a",
                            Outcome = EncounterOutcome.GotoWith("end",
                                EncounterEffect.AddCredits(100))
                        },
                        new EncounterOption
                        {
                            Id = "option_b",
                            TextKey = "test.simple.option_b",
                            Outcome = EncounterOutcome.GotoWith("end",
                                EncounterEffect.AddFuel(10))
                        }
                    }
                },
                ["end"] = new EncounterNode
                {
                    Id = "end",
                    TextKey = "test.simple.end",
                    AutoTransition = EncounterOutcome.End()
                }
            }
        };
    }

    /// <summary>
    /// Encounter with conditional options based on resources and traits.
    /// Tests condition evaluation.
    /// </summary>
    public static EncounterTemplate CreateConditionalEncounter()
    {
        return new EncounterTemplate
        {
            Id = "test_conditional",
            Name = "Conditional Test Encounter",
            Tags = new HashSet<string> { "test", "conditional" },
            EntryNodeId = "start",
            Nodes = new Dictionary<string, EncounterNode>
            {
                ["start"] = new EncounterNode
                {
                    Id = "start",
                    TextKey = "test.conditional.start",
                    Options = new List<EncounterOption>
                    {
                        new EncounterOption
                        {
                            Id = "bribe",
                            TextKey = "test.conditional.bribe",
                            Conditions = new List<EncounterCondition>
                            {
                                EncounterCondition.HasCredits(50)
                            },
                            Outcome = EncounterOutcome.GotoWith("success",
                                EncounterEffect.LoseCredits(50))
                        },
                        new EncounterOption
                        {
                            Id = "hack",
                            TextKey = "test.conditional.hack",
                            Conditions = new List<EncounterCondition>
                            {
                                EncounterCondition.CrewStatMin(CrewStatType.Tech, 5)
                            },
                            Outcome = EncounterOutcome.Goto("success")
                        },
                        new EncounterOption
                        {
                            Id = "fight",
                            TextKey = "test.conditional.fight",
                            Outcome = EncounterOutcome.GotoWith("fight_result",
                                EncounterEffect.ShipDamage(10))
                        }
                    }
                },
                ["success"] = new EncounterNode
                {
                    Id = "success",
                    TextKey = "test.conditional.success",
                    AutoTransition = EncounterOutcome.EndWith(
                        EncounterEffect.AddCredits(100))
                },
                ["fight_result"] = new EncounterNode
                {
                    Id = "fight_result",
                    TextKey = "test.conditional.fight_result",
                    AutoTransition = EncounterOutcome.EndWith(
                        EncounterEffect.AddCredits(50),
                        EncounterEffect.CrewXp(10))
                }
            }
        };
    }

    /// <summary>
    /// Encounter with multiple branching paths.
    /// Tests complex navigation.
    /// </summary>
    public static EncounterTemplate CreateBranchingEncounter()
    {
        return new EncounterTemplate
        {
            Id = "test_branching",
            Name = "Branching Test Encounter",
            Tags = new HashSet<string> { "test", "branching" },
            EntryNodeId = "start",
            Nodes = new Dictionary<string, EncounterNode>
            {
                ["start"] = new EncounterNode
                {
                    Id = "start",
                    TextKey = "test.branching.start",
                    Options = new List<EncounterOption>
                    {
                        new EncounterOption
                        {
                            Id = "path_a",
                            TextKey = "test.branching.path_a",
                            Outcome = EncounterOutcome.Goto("branch_a")
                        },
                        new EncounterOption
                        {
                            Id = "path_b",
                            TextKey = "test.branching.path_b",
                            Outcome = EncounterOutcome.Goto("branch_b")
                        },
                        new EncounterOption
                        {
                            Id = "path_c",
                            TextKey = "test.branching.path_c",
                            Outcome = EncounterOutcome.Goto("branch_c")
                        }
                    }
                },
                ["branch_a"] = new EncounterNode
                {
                    Id = "branch_a",
                    TextKey = "test.branching.branch_a",
                    Options = new List<EncounterOption>
                    {
                        new EncounterOption
                        {
                            Id = "a_continue",
                            TextKey = "test.branching.continue",
                            Outcome = EncounterOutcome.GotoWith("end_good",
                                EncounterEffect.AddCredits(200))
                        }
                    }
                },
                ["branch_b"] = new EncounterNode
                {
                    Id = "branch_b",
                    TextKey = "test.branching.branch_b",
                    Options = new List<EncounterOption>
                    {
                        new EncounterOption
                        {
                            Id = "b_risky",
                            TextKey = "test.branching.risky",
                            Outcome = EncounterOutcome.GotoWith("end_good",
                                EncounterEffect.AddCredits(500),
                                EncounterEffect.ShipDamage(20))
                        },
                        new EncounterOption
                        {
                            Id = "b_safe",
                            TextKey = "test.branching.safe",
                            Outcome = EncounterOutcome.GotoWith("end_neutral",
                                EncounterEffect.AddCredits(50))
                        }
                    }
                },
                ["branch_c"] = new EncounterNode
                {
                    Id = "branch_c",
                    TextKey = "test.branching.branch_c",
                    AutoTransition = EncounterOutcome.GotoWith("end_bad",
                        EncounterEffect.ShipDamage(30))
                },
                ["end_good"] = new EncounterNode
                {
                    Id = "end_good",
                    TextKey = "test.branching.end_good",
                    AutoTransition = EncounterOutcome.End()
                },
                ["end_neutral"] = new EncounterNode
                {
                    Id = "end_neutral",
                    TextKey = "test.branching.end_neutral",
                    AutoTransition = EncounterOutcome.End()
                },
                ["end_bad"] = new EncounterNode
                {
                    Id = "end_bad",
                    TextKey = "test.branching.end_bad",
                    AutoTransition = EncounterOutcome.End()
                }
            }
        };
    }

    /// <summary>
    /// Full pirate ambush encounter from the design spec.
    /// Tests realistic encounter structure.
    /// </summary>
    public static EncounterTemplate CreatePirateAmbush()
    {
        return new EncounterTemplate
        {
            Id = "pirate_ambush",
            Name = "Pirate Ambush",
            Tags = new HashSet<string> { "pirate", "combat", "travel" },
            EntryNodeId = "intro",
            Nodes = new Dictionary<string, EncounterNode>
            {
                ["intro"] = new EncounterNode
                {
                    Id = "intro",
                    TextKey = "encounter.pirate_ambush.intro",
                    Options = new List<EncounterOption>
                    {
                        new EncounterOption
                        {
                            Id = "fight",
                            TextKey = "encounter.pirate_ambush.fight",
                            Outcome = EncounterOutcome.GotoWith("victory",
                                EncounterEffect.ShipDamage(10),
                                EncounterEffect.AddCredits(150),
                                EncounterEffect.CrewXp(15))
                        },
                        new EncounterOption
                        {
                            Id = "surrender",
                            TextKey = "encounter.pirate_ambush.surrender",
                            Conditions = new List<EncounterCondition>
                            {
                                EncounterCondition.HasCredits(100)
                            },
                            Outcome = EncounterOutcome.GotoWith("surrendered",
                                EncounterEffect.LoseCredits(100))
                        },
                        new EncounterOption
                        {
                            Id = "flee",
                            TextKey = "encounter.pirate_ambush.flee",
                            Conditions = new List<EncounterCondition>
                            {
                                EncounterCondition.CrewStatMin(CrewStatType.Reflexes, 4)
                            },
                            Outcome = EncounterOutcome.GotoWith("escaped",
                                EncounterEffect.TimeDelay(1),
                                EncounterEffect.LoseFuel(5))
                        },
                        new EncounterOption
                        {
                            Id = "bluff",
                            TextKey = "encounter.pirate_ambush.bluff",
                            Conditions = new List<EncounterCondition>
                            {
                                EncounterCondition.CrewStatMin(CrewStatType.Savvy, 6)
                            },
                            Outcome = EncounterOutcome.Goto("bluff_success")
                        }
                    }
                },
                ["victory"] = new EncounterNode
                {
                    Id = "victory",
                    TextKey = "encounter.pirate_ambush.victory",
                    AutoTransition = EncounterOutcome.EndWith(
                        EncounterEffect.FactionRep("pirates", -5))
                },
                ["surrendered"] = new EncounterNode
                {
                    Id = "surrendered",
                    TextKey = "encounter.pirate_ambush.surrendered",
                    AutoTransition = EncounterOutcome.End()
                },
                ["escaped"] = new EncounterNode
                {
                    Id = "escaped",
                    TextKey = "encounter.pirate_ambush.escaped",
                    AutoTransition = EncounterOutcome.End()
                },
                ["bluff_success"] = new EncounterNode
                {
                    Id = "bluff_success",
                    TextKey = "encounter.pirate_ambush.bluff_success",
                    AutoTransition = EncounterOutcome.EndWith(
                        EncounterEffect.CrewXp(20))
                }
            }
        };
    }

    /// <summary>
    /// Distress signal encounter with multiple outcomes.
    /// Tests faction reputation conditions.
    /// </summary>
    public static EncounterTemplate CreateDistressSignal()
    {
        return new EncounterTemplate
        {
            Id = "distress_signal",
            Name = "Distress Signal",
            Tags = new HashSet<string> { "distress", "travel", "social" },
            EntryNodeId = "signal",
            Nodes = new Dictionary<string, EncounterNode>
            {
                ["signal"] = new EncounterNode
                {
                    Id = "signal",
                    TextKey = "encounter.distress.signal",
                    Options = new List<EncounterOption>
                    {
                        new EncounterOption
                        {
                            Id = "investigate",
                            TextKey = "encounter.distress.investigate",
                            Outcome = EncounterOutcome.Goto("investigate_result")
                        },
                        new EncounterOption
                        {
                            Id = "ignore",
                            TextKey = "encounter.distress.ignore",
                            Outcome = EncounterOutcome.Goto("ignored")
                        }
                    }
                },
                ["investigate_result"] = new EncounterNode
                {
                    Id = "investigate_result",
                    TextKey = "encounter.distress.investigate_result",
                    Options = new List<EncounterOption>
                    {
                        new EncounterOption
                        {
                            Id = "help",
                            TextKey = "encounter.distress.help",
                            Conditions = new List<EncounterCondition>
                            {
                                EncounterCondition.HasFuel(10)
                            },
                            Outcome = EncounterOutcome.GotoWith("helped",
                                EncounterEffect.LoseFuel(10))
                        },
                        new EncounterOption
                        {
                            Id = "salvage",
                            TextKey = "encounter.distress.salvage",
                            Outcome = EncounterOutcome.GotoWith("salvaged",
                                EncounterEffect.AddCredits(75),
                                EncounterEffect.AddParts(5))
                        },
                        new EncounterOption
                        {
                            Id = "leave",
                            TextKey = "encounter.distress.leave",
                            Outcome = EncounterOutcome.Goto("left")
                        }
                    }
                },
                ["helped"] = new EncounterNode
                {
                    Id = "helped",
                    TextKey = "encounter.distress.helped",
                    AutoTransition = EncounterOutcome.EndWith(
                        EncounterEffect.AddCredits(200),
                        EncounterEffect.FactionRep("traders", 10),
                        EncounterEffect.CrewXp(10))
                },
                ["salvaged"] = new EncounterNode
                {
                    Id = "salvaged",
                    TextKey = "encounter.distress.salvaged",
                    AutoTransition = EncounterOutcome.EndWith(
                        EncounterEffect.FactionRep("traders", -5))
                },
                ["left"] = new EncounterNode
                {
                    Id = "left",
                    TextKey = "encounter.distress.left",
                    AutoTransition = EncounterOutcome.End()
                },
                ["ignored"] = new EncounterNode
                {
                    Id = "ignored",
                    TextKey = "encounter.distress.ignored",
                    AutoTransition = EncounterOutcome.End()
                }
            }
        };
    }
}
