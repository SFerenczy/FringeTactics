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

    // ========================================================================
    // EN2 SKILL CHECK TEST ENCOUNTERS
    // ========================================================================

    /// <summary>
    /// Encounter with skill check options.
    /// Tests skill check resolution with trait bonuses.
    /// </summary>
    public static EncounterTemplate CreateSkillCheckEncounter()
    {
        return new EncounterTemplate
        {
            Id = "test_skillcheck",
            Name = "Skill Check Test Encounter",
            Tags = new HashSet<string> { "test", "skillcheck" },
            EntryNodeId = "start",
            Nodes = new Dictionary<string, EncounterNode>
            {
                ["start"] = new EncounterNode
                {
                    Id = "start",
                    TextKey = "test.skillcheck.start",
                    Options = new List<EncounterOption>
                    {
                        new EncounterOption
                        {
                            Id = "hack_terminal",
                            TextKey = "test.skillcheck.hack",
                            SkillCheck = new SkillCheckDef
                            {
                                Stat = CrewStatType.Tech,
                                Difficulty = 12,
                                BonusTraits = new List<string> { "corporate", "spacer" }
                            },
                            SuccessOutcome = EncounterOutcome.GotoWith("success",
                                EncounterEffect.AddCredits(200)),
                            FailureOutcome = EncounterOutcome.GotoWith("failure",
                                EncounterEffect.TimeDelay(1))
                        },
                        new EncounterOption
                        {
                            Id = "talk_guard",
                            TextKey = "test.skillcheck.talk",
                            SkillCheck = new SkillCheckDef
                            {
                                Stat = CrewStatType.Savvy,
                                Difficulty = 10,
                                BonusTraits = new List<string> { "smuggler", "empathetic" },
                                PenaltyTraits = new List<string> { "reckless" }
                            },
                            SuccessOutcome = EncounterOutcome.Goto("success"),
                            FailureOutcome = EncounterOutcome.GotoWith("caught",
                                EncounterEffect.FactionRep("security", -10))
                        },
                        new EncounterOption
                        {
                            Id = "force_entry",
                            TextKey = "test.skillcheck.force",
                            Outcome = EncounterOutcome.GotoWith("alarm",
                                EncounterEffect.ShipDamage(15))
                        }
                    }
                },
                ["success"] = new EncounterNode
                {
                    Id = "success",
                    TextKey = "test.skillcheck.success",
                    AutoTransition = EncounterOutcome.EndWith(
                        EncounterEffect.AddCredits(100))
                },
                ["failure"] = new EncounterNode
                {
                    Id = "failure",
                    TextKey = "test.skillcheck.failure",
                    AutoTransition = EncounterOutcome.End()
                },
                ["caught"] = new EncounterNode
                {
                    Id = "caught",
                    TextKey = "test.skillcheck.caught",
                    AutoTransition = EncounterOutcome.End()
                },
                ["alarm"] = new EncounterNode
                {
                    Id = "alarm",
                    TextKey = "test.skillcheck.alarm",
                    AutoTransition = EncounterOutcome.End()
                }
            }
        };
    }

    /// <summary>
    /// Encounter with easy skill check (difficulty 5).
    /// For testing guaranteed success with moderate stats.
    /// </summary>
    public static EncounterTemplate CreateEasySkillCheckEncounter()
    {
        return new EncounterTemplate
        {
            Id = "test_easy_skillcheck",
            Name = "Easy Skill Check Test",
            Tags = new HashSet<string> { "test", "skillcheck" },
            EntryNodeId = "start",
            Nodes = new Dictionary<string, EncounterNode>
            {
                ["start"] = new EncounterNode
                {
                    Id = "start",
                    TextKey = "test.easy.start",
                    Options = new List<EncounterOption>
                    {
                        new EncounterOption
                        {
                            Id = "easy_check",
                            TextKey = "test.easy.check",
                            SkillCheck = new SkillCheckDef
                            {
                                Stat = CrewStatType.Tech,
                                Difficulty = 5
                            },
                            SuccessOutcome = EncounterOutcome.EndWith(
                                EncounterEffect.AddCredits(50)),
                            FailureOutcome = EncounterOutcome.End()
                        }
                    }
                }
            }
        };
    }

    /// <summary>
    /// Encounter with hard skill check (difficulty 25).
    /// For testing guaranteed failure without extreme stats.
    /// </summary>
    public static EncounterTemplate CreateHardSkillCheckEncounter()
    {
        return new EncounterTemplate
        {
            Id = "test_hard_skillcheck",
            Name = "Hard Skill Check Test",
            Tags = new HashSet<string> { "test", "skillcheck" },
            EntryNodeId = "start",
            Nodes = new Dictionary<string, EncounterNode>
            {
                ["start"] = new EncounterNode
                {
                    Id = "start",
                    TextKey = "test.hard.start",
                    Options = new List<EncounterOption>
                    {
                        new EncounterOption
                        {
                            Id = "hard_check",
                            TextKey = "test.hard.check",
                            SkillCheck = new SkillCheckDef
                            {
                                Stat = CrewStatType.Tech,
                                Difficulty = 25
                            },
                            SuccessOutcome = EncounterOutcome.EndWith(
                                EncounterEffect.AddCredits(1000)),
                            FailureOutcome = EncounterOutcome.EndWith(
                                EncounterEffect.ShipDamage(10))
                        }
                    }
                }
            }
        };
    }

    /// <summary>
    /// Encounter with trait-dependent skill check.
    /// Tests trait bonus calculation.
    /// </summary>
    public static EncounterTemplate CreateTraitBonusEncounter()
    {
        return new EncounterTemplate
        {
            Id = "test_trait_bonus",
            Name = "Trait Bonus Test",
            Tags = new HashSet<string> { "test", "skillcheck", "traits" },
            EntryNodeId = "start",
            Nodes = new Dictionary<string, EncounterNode>
            {
                ["start"] = new EncounterNode
                {
                    Id = "start",
                    TextKey = "test.trait.start",
                    Options = new List<EncounterOption>
                    {
                        new EncounterOption
                        {
                            Id = "smuggle_check",
                            TextKey = "test.trait.smuggle",
                            SkillCheck = new SkillCheckDef
                            {
                                Stat = CrewStatType.Savvy,
                                Difficulty = 14,
                                BonusTraits = new List<string> { "smuggler" }
                            },
                            SuccessOutcome = EncounterOutcome.EndWith(
                                EncounterEffect.AddCredits(300)),
                            FailureOutcome = EncounterOutcome.EndWith(
                                EncounterEffect.LoseCredits(100),
                                EncounterEffect.FactionRep("security", -15))
                        },
                        new EncounterOption
                        {
                            Id = "military_check",
                            TextKey = "test.trait.military",
                            SkillCheck = new SkillCheckDef
                            {
                                Stat = CrewStatType.Aim,
                                Difficulty = 14,
                                BonusTraits = new List<string> { "ex_military", "cold_blooded" }
                            },
                            SuccessOutcome = EncounterOutcome.EndWith(
                                EncounterEffect.AddCredits(200),
                                EncounterEffect.CrewXp(15)),
                            FailureOutcome = EncounterOutcome.EndWith(
                                EncounterEffect.ShipDamage(20))
                        }
                    }
                }
            }
        };
    }

    /// <summary>
    /// Encounter testing penalty traits.
    /// </summary>
    public static EncounterTemplate CreatePenaltyTraitEncounter()
    {
        return new EncounterTemplate
        {
            Id = "test_penalty_trait",
            Name = "Penalty Trait Test",
            Tags = new HashSet<string> { "test", "skillcheck", "traits" },
            EntryNodeId = "start",
            Nodes = new Dictionary<string, EncounterNode>
            {
                ["start"] = new EncounterNode
                {
                    Id = "start",
                    TextKey = "test.penalty.start",
                    Options = new List<EncounterOption>
                    {
                        new EncounterOption
                        {
                            Id = "stealth_check",
                            TextKey = "test.penalty.stealth",
                            SkillCheck = new SkillCheckDef
                            {
                                Stat = CrewStatType.Reflexes,
                                Difficulty = 12,
                                BonusTraits = new List<string> { "cautious" },
                                PenaltyTraits = new List<string> { "reckless" }
                            },
                            SuccessOutcome = EncounterOutcome.EndWith(
                                EncounterEffect.AddCredits(150)),
                            FailureOutcome = EncounterOutcome.EndWith(
                                EncounterEffect.ShipDamage(25))
                        }
                    }
                }
            }
        };
    }

    // ========================================================================
    // EN-CONTENT2 RECRUITMENT ENCOUNTERS
    // ========================================================================

    /// <summary>
    /// Drifter looking for passage. Can recruit a soldier.
    /// </summary>
    public static EncounterTemplate CreateDrifterEncounter()
    {
        return new EncounterTemplate
        {
            Id = "drifter_passage",
            Name = "Drifter Seeking Passage",
            Tags = new HashSet<string> { "travel", "social", "recruitment" },
            EntryNodeId = "intro",
            Nodes = new Dictionary<string, EncounterNode>
            {
                ["intro"] = new EncounterNode
                {
                    Id = "intro",
                    TextKey = "encounter.drifter.intro",
                    Options = new List<EncounterOption>
                    {
                        new EncounterOption
                        {
                            Id = "recruit",
                            TextKey = "encounter.drifter.recruit",
                            Outcome = EncounterOutcome.GotoWith("recruited",
                                EncounterEffect.AddCrew("Drifter", "Soldier"))
                        },
                        new EncounterOption
                        {
                            Id = "pay_passage",
                            TextKey = "encounter.drifter.pay_passage",
                            Conditions = new List<EncounterCondition>
                            {
                                EncounterCondition.HasCredits(50)
                            },
                            Outcome = EncounterOutcome.GotoWith("paid",
                                EncounterEffect.LoseCredits(50),
                                EncounterEffect.AddCredits(100))
                        },
                        new EncounterOption
                        {
                            Id = "refuse",
                            TextKey = "encounter.drifter.refuse",
                            Outcome = EncounterOutcome.Goto("refused")
                        }
                    }
                },
                ["recruited"] = new EncounterNode
                {
                    Id = "recruited",
                    TextKey = "encounter.drifter.recruited",
                    AutoTransition = EncounterOutcome.End()
                },
                ["paid"] = new EncounterNode
                {
                    Id = "paid",
                    TextKey = "encounter.drifter.paid",
                    AutoTransition = EncounterOutcome.End()
                },
                ["refused"] = new EncounterNode
                {
                    Id = "refused",
                    TextKey = "encounter.drifter.refused",
                    AutoTransition = EncounterOutcome.End()
                }
            }
        };
    }

    /// <summary>
    /// Rescue a stranded specialist from a derelict. Can recruit a tech.
    /// </summary>
    public static EncounterTemplate CreateStrandedSpecialistEncounter()
    {
        return new EncounterTemplate
        {
            Id = "stranded_specialist",
            Name = "Stranded Specialist",
            Tags = new HashSet<string> { "travel", "distress", "recruitment" },
            EntryNodeId = "signal",
            Nodes = new Dictionary<string, EncounterNode>
            {
                ["signal"] = new EncounterNode
                {
                    Id = "signal",
                    TextKey = "encounter.specialist.signal",
                    Options = new List<EncounterOption>
                    {
                        new EncounterOption
                        {
                            Id = "investigate",
                            TextKey = "encounter.specialist.investigate",
                            Outcome = EncounterOutcome.Goto("derelict")
                        },
                        new EncounterOption
                        {
                            Id = "ignore",
                            TextKey = "encounter.specialist.ignore",
                            Outcome = EncounterOutcome.Goto("ignored")
                        }
                    }
                },
                ["derelict"] = new EncounterNode
                {
                    Id = "derelict",
                    TextKey = "encounter.specialist.derelict",
                    Options = new List<EncounterOption>
                    {
                        new EncounterOption
                        {
                            Id = "rescue",
                            TextKey = "encounter.specialist.rescue",
                            SkillCheck = new SkillCheckDef
                            {
                                Stat = CrewStatType.Tech,
                                Difficulty = 10
                            },
                            SuccessOutcome = EncounterOutcome.GotoWith("rescued",
                                EncounterEffect.AddCrew("Specialist", "Tech"),
                                EncounterEffect.AddParts(10)),
                            FailureOutcome = EncounterOutcome.GotoWith("failed_rescue",
                                EncounterEffect.ShipDamage(15),
                                EncounterEffect.TimeDelay(1))
                        },
                        new EncounterOption
                        {
                            Id = "salvage_only",
                            TextKey = "encounter.specialist.salvage",
                            Outcome = EncounterOutcome.GotoWith("salvaged",
                                EncounterEffect.AddParts(20),
                                EncounterEffect.AddCredits(50))
                        }
                    }
                },
                ["rescued"] = new EncounterNode
                {
                    Id = "rescued",
                    TextKey = "encounter.specialist.rescued",
                    AutoTransition = EncounterOutcome.EndWith(
                        EncounterEffect.CrewXp(15))
                },
                ["failed_rescue"] = new EncounterNode
                {
                    Id = "failed_rescue",
                    TextKey = "encounter.specialist.failed_rescue",
                    AutoTransition = EncounterOutcome.End()
                },
                ["salvaged"] = new EncounterNode
                {
                    Id = "salvaged",
                    TextKey = "encounter.specialist.salvaged",
                    AutoTransition = EncounterOutcome.End()
                },
                ["ignored"] = new EncounterNode
                {
                    Id = "ignored",
                    TextKey = "encounter.specialist.ignored",
                    AutoTransition = EncounterOutcome.End()
                }
            }
        };
    }

    /// <summary>
    /// Deserter from a faction offers to join. Can recruit a scout.
    /// </summary>
    public static EncounterTemplate CreateDeserterEncounter()
    {
        return new EncounterTemplate
        {
            Id = "faction_deserter",
            Name = "Faction Deserter",
            Tags = new HashSet<string> { "travel", "social", "recruitment", "faction" },
            EntryNodeId = "hail",
            Nodes = new Dictionary<string, EncounterNode>
            {
                ["hail"] = new EncounterNode
                {
                    Id = "hail",
                    TextKey = "encounter.deserter.hail",
                    Options = new List<EncounterOption>
                    {
                        new EncounterOption
                        {
                            Id = "accept",
                            TextKey = "encounter.deserter.accept",
                            Outcome = EncounterOutcome.GotoWith("joined",
                                EncounterEffect.AddCrew("Deserter", "Scout"),
                                EncounterEffect.FactionRep("military", -10))
                        },
                        new EncounterOption
                        {
                            Id = "negotiate",
                            TextKey = "encounter.deserter.negotiate",
                            SkillCheck = new SkillCheckDef
                            {
                                Stat = CrewStatType.Savvy,
                                Difficulty = 12,
                                BonusTraits = new List<string> { "empathetic" }
                            },
                            SuccessOutcome = EncounterOutcome.GotoWith("joined_intel",
                                EncounterEffect.AddCrew("Deserter", "Scout"),
                                EncounterEffect.AddCredits(200)),
                            FailureOutcome = EncounterOutcome.Goto("refused_deal")
                        },
                        new EncounterOption
                        {
                            Id = "turn_in",
                            TextKey = "encounter.deserter.turn_in",
                            Outcome = EncounterOutcome.GotoWith("turned_in",
                                EncounterEffect.AddCredits(150),
                                EncounterEffect.FactionRep("military", 15))
                        },
                        new EncounterOption
                        {
                            Id = "refuse",
                            TextKey = "encounter.deserter.refuse",
                            Outcome = EncounterOutcome.Goto("refused")
                        }
                    }
                },
                ["joined"] = new EncounterNode
                {
                    Id = "joined",
                    TextKey = "encounter.deserter.joined",
                    AutoTransition = EncounterOutcome.End()
                },
                ["joined_intel"] = new EncounterNode
                {
                    Id = "joined_intel",
                    TextKey = "encounter.deserter.joined_intel",
                    AutoTransition = EncounterOutcome.End()
                },
                ["refused_deal"] = new EncounterNode
                {
                    Id = "refused_deal",
                    TextKey = "encounter.deserter.refused_deal",
                    AutoTransition = EncounterOutcome.End()
                },
                ["turned_in"] = new EncounterNode
                {
                    Id = "turned_in",
                    TextKey = "encounter.deserter.turned_in",
                    AutoTransition = EncounterOutcome.End()
                },
                ["refused"] = new EncounterNode
                {
                    Id = "refused",
                    TextKey = "encounter.deserter.refused",
                    AutoTransition = EncounterOutcome.End()
                }
            }
        };
    }

    /// <summary>
    /// Simple test encounter for AddCrew effect.
    /// </summary>
    public static EncounterTemplate CreateAddCrewTestEncounter()
    {
        return new EncounterTemplate
        {
            Id = "test_add_crew",
            Name = "Add Crew Test",
            Tags = new HashSet<string> { "test", "recruitment" },
            EntryNodeId = "start",
            Nodes = new Dictionary<string, EncounterNode>
            {
                ["start"] = new EncounterNode
                {
                    Id = "start",
                    TextKey = "test.addcrew.start",
                    Options = new List<EncounterOption>
                    {
                        new EncounterOption
                        {
                            Id = "recruit_soldier",
                            TextKey = "test.addcrew.soldier",
                            Outcome = EncounterOutcome.EndWith(
                                EncounterEffect.AddCrew("TestSoldier", "Soldier"))
                        },
                        new EncounterOption
                        {
                            Id = "recruit_medic",
                            TextKey = "test.addcrew.medic",
                            Outcome = EncounterOutcome.EndWith(
                                EncounterEffect.AddCrew("TestMedic", "Medic"))
                        },
                        new EncounterOption
                        {
                            Id = "recruit_tech",
                            TextKey = "test.addcrew.tech",
                            Outcome = EncounterOutcome.EndWith(
                                EncounterEffect.AddCrew("TestTech", "Tech"))
                        },
                        new EncounterOption
                        {
                            Id = "recruit_scout",
                            TextKey = "test.addcrew.scout",
                            Outcome = EncounterOutcome.EndWith(
                                EncounterEffect.AddCrew("TestScout", "Scout"))
                        }
                    }
                }
            }
        };
    }
}
