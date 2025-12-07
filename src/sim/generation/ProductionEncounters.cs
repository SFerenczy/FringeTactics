using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// Production encounter templates for gameplay.
/// These are the real encounters players will experience during travel.
/// Values are configurable via EncounterValueConfig.
/// </summary>
public static class ProductionEncounters
{
    private static EncounterValueConfig Config => EncounterValueConfig.Default;

    /// <summary>
    /// Get all production templates.
    /// </summary>
    public static IEnumerable<EncounterTemplate> GetAllTemplates()
    {
        yield return CreatePirateAmbush();
        yield return CreatePatrolInspection();
        yield return CreateDistressSignal();
        yield return CreateTraderOpportunity();
        yield return CreateSmugglerContact();
        yield return CreateDerelictDiscovery();
        yield return CreateFactionAgent();
        yield return CreateMysteriousSignal();
        yield return CreateMechanicalFailure();
        yield return CreateRefugeePlea();
        yield return CreateTrialByFire();
    }

    // ========================================================================
    // PIRATE ENCOUNTERS
    // ========================================================================

    /// <summary>
    /// Pirates emerge and demand cargo or fight.
    /// Tags: travel, pirate, combat, choice
    /// </summary>
    public static EncounterTemplate CreatePirateAmbush()
    {
        return new EncounterTemplate
        {
            Id = "prod_pirate_ambush",
            Name = "Pirate Ambush",
            Tags = new HashSet<string>
            {
                EncounterTags.Travel, EncounterTags.Pirate,
                EncounterTags.Combat, EncounterTags.Choice
            },
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
                            Outcome = EncounterOutcome.GotoWith("combat_result",
                                EncounterEffect.ShipDamage(Config.FightDamage),
                                EncounterEffect.AddCredits(Config.FightReward))
                        },
                        new EncounterOption
                        {
                            Id = "flee",
                            TextKey = "encounter.pirate_ambush.flee",
                            SkillCheck = new SkillCheckDef
                            {
                                Stat = CrewStatType.Reflexes,
                                Difficulty = Config.MediumDifficulty,
                                BonusTraits = new List<string> { "pilot", "quick_reflexes" },
                                PenaltyTraits = new List<string> { "slow" }
                            },
                            SuccessOutcome = EncounterOutcome.GotoWith("escaped",
                                EncounterEffect.TimeDelay(1)),
                            FailureOutcome = EncounterOutcome.GotoWith("caught",
                                EncounterEffect.ShipDamage(Config.FleeFailDamage))
                        },
                        new EncounterOption
                        {
                            Id = "surrender",
                            TextKey = "encounter.pirate_ambush.surrender",
                            Conditions = new List<EncounterCondition>
                            {
                                EncounterCondition.HasCredits(Config.SurrenderCost)
                            },
                            Outcome = EncounterOutcome.GotoWith("surrendered",
                                EncounterEffect.LoseCredits(Config.SurrenderCost))
                        },
                        new EncounterOption
                        {
                            Id = "negotiate",
                            TextKey = "encounter.pirate_ambush.negotiate",
                            SkillCheck = new SkillCheckDef
                            {
                                Stat = CrewStatType.Savvy,
                                Difficulty = Config.VeryHardDifficulty,
                                BonusTraits = new List<string> { "smooth_talker", "intimidating" }
                            },
                            SuccessOutcome = EncounterOutcome.Goto("talked_down"),
                            FailureOutcome = EncounterOutcome.Goto("intro")
                        }
                    }
                },
                ["combat_result"] = new EncounterNode
                {
                    Id = "combat_result",
                    TextKey = "encounter.pirate_ambush.combat_result",
                    AutoTransition = EncounterOutcome.End()
                },
                ["escaped"] = new EncounterNode
                {
                    Id = "escaped",
                    TextKey = "encounter.pirate_ambush.escaped",
                    AutoTransition = EncounterOutcome.End()
                },
                ["caught"] = new EncounterNode
                {
                    Id = "caught",
                    TextKey = "encounter.pirate_ambush.caught",
                    AutoTransition = EncounterOutcome.End()
                },
                ["surrendered"] = new EncounterNode
                {
                    Id = "surrendered",
                    TextKey = "encounter.pirate_ambush.surrendered",
                    AutoTransition = EncounterOutcome.End()
                },
                ["talked_down"] = new EncounterNode
                {
                    Id = "talked_down",
                    TextKey = "encounter.pirate_ambush.talked_down",
                    AutoTransition = EncounterOutcome.EndWith(
                        EncounterEffect.CrewXp(10))
                }
            }
        };
    }

    // ========================================================================
    // PATROL ENCOUNTERS
    // ========================================================================

    /// <summary>
    /// Security patrol requests inspection.
    /// Tags: travel, patrol, social, skill_check
    /// </summary>
    public static EncounterTemplate CreatePatrolInspection()
    {
        return new EncounterTemplate
        {
            Id = "prod_patrol_inspection",
            Name = "Patrol Inspection",
            Tags = new HashSet<string>
            {
                EncounterTags.Travel, EncounterTags.Patrol,
                EncounterTags.Social, EncounterTags.SkillCheck
            },
            EntryNodeId = "intro",
            Nodes = new Dictionary<string, EncounterNode>
            {
                ["intro"] = new EncounterNode
                {
                    Id = "intro",
                    TextKey = "encounter.patrol_inspection.intro",
                    Options = new List<EncounterOption>
                    {
                        new EncounterOption
                        {
                            Id = "comply",
                            TextKey = "encounter.patrol_inspection.comply",
                            Outcome = EncounterOutcome.Goto("inspection")
                        },
                        new EncounterOption
                        {
                            Id = "bribe",
                            TextKey = "encounter.patrol_inspection.bribe",
                            Conditions = new List<EncounterCondition>
                            {
                                EncounterCondition.HasCredits(Config.BribeCost)
                            },
                            SkillCheck = new SkillCheckDef
                            {
                                Stat = CrewStatType.Savvy,
                                Difficulty = Config.EasyDifficulty
                            },
                            SuccessOutcome = EncounterOutcome.GotoWith("bribed",
                                EncounterEffect.LoseCredits(Config.BribeCost)),
                            FailureOutcome = EncounterOutcome.Goto("bribe_failed")
                        },
                        new EncounterOption
                        {
                            Id = "flee",
                            TextKey = "encounter.patrol_inspection.flee",
                            SkillCheck = new SkillCheckDef
                            {
                                Stat = CrewStatType.Reflexes,
                                Difficulty = Config.HardDifficulty
                            },
                            SuccessOutcome = EncounterOutcome.Goto("fled"),
                            FailureOutcome = EncounterOutcome.GotoWith("caught_fleeing",
                                EncounterEffect.LoseCredits(Config.FleeingFine))
                        }
                    }
                },
                ["inspection"] = new EncounterNode
                {
                    Id = "inspection",
                    TextKey = "encounter.patrol_inspection.inspection",
                    AutoTransition = EncounterOutcome.Goto("cleared")
                },
                ["cleared"] = new EncounterNode
                {
                    Id = "cleared",
                    TextKey = "encounter.patrol_inspection.cleared",
                    AutoTransition = EncounterOutcome.End()
                },
                ["bribed"] = new EncounterNode
                {
                    Id = "bribed",
                    TextKey = "encounter.patrol_inspection.bribed",
                    AutoTransition = EncounterOutcome.End()
                },
                ["bribe_failed"] = new EncounterNode
                {
                    Id = "bribe_failed",
                    TextKey = "encounter.patrol_inspection.bribe_failed",
                    AutoTransition = EncounterOutcome.GotoWith("inspection",
                        EncounterEffect.LoseCredits(Config.BribeFailFine))
                },
                ["fled"] = new EncounterNode
                {
                    Id = "fled",
                    TextKey = "encounter.patrol_inspection.fled",
                    AutoTransition = EncounterOutcome.End()
                },
                ["caught_fleeing"] = new EncounterNode
                {
                    Id = "caught_fleeing",
                    TextKey = "encounter.patrol_inspection.caught_fleeing",
                    AutoTransition = EncounterOutcome.End()
                }
            }
        };
    }

    // ========================================================================
    // DISTRESS ENCOUNTERS
    // ========================================================================

    /// <summary>
    /// Distress signal - could be genuine or trap.
    /// Tags: travel, distress, choice, skill_check
    /// </summary>
    public static EncounterTemplate CreateDistressSignal()
    {
        return new EncounterTemplate
        {
            Id = "prod_distress_signal",
            Name = "Distress Signal",
            Tags = new HashSet<string>
            {
                EncounterTags.Travel, EncounterTags.Distress,
                EncounterTags.Choice, EncounterTags.SkillCheck
            },
            EntryNodeId = "intro",
            Nodes = new Dictionary<string, EncounterNode>
            {
                ["intro"] = new EncounterNode
                {
                    Id = "intro",
                    TextKey = "encounter.distress_signal.intro",
                    Options = new List<EncounterOption>
                    {
                        new EncounterOption
                        {
                            Id = "investigate",
                            TextKey = "encounter.distress_signal.investigate",
                            Outcome = EncounterOutcome.Goto("approach")
                        },
                        new EncounterOption
                        {
                            Id = "scan",
                            TextKey = "encounter.distress_signal.scan",
                            SkillCheck = new SkillCheckDef
                            {
                                Stat = CrewStatType.Tech,
                                Difficulty = 5,
                                BonusTraits = new List<string> { "sensor_specialist", "cautious" }
                            },
                            SuccessOutcome = EncounterOutcome.Goto("scan_result"),
                            FailureOutcome = EncounterOutcome.Goto("approach")
                        },
                        new EncounterOption
                        {
                            Id = "ignore",
                            TextKey = "encounter.distress_signal.ignore",
                            Outcome = EncounterOutcome.Goto("ignored")
                        }
                    }
                },
                ["scan_result"] = new EncounterNode
                {
                    Id = "scan_result",
                    TextKey = "encounter.distress_signal.scan_result",
                    Options = new List<EncounterOption>
                    {
                        new EncounterOption
                        {
                            Id = "help",
                            TextKey = "encounter.distress_signal.help",
                            Outcome = EncounterOutcome.Goto("genuine_rescue")
                        },
                        new EncounterOption
                        {
                            Id = "leave",
                            TextKey = "encounter.distress_signal.leave",
                            Outcome = EncounterOutcome.Goto("avoided_trap")
                        }
                    }
                },
                ["approach"] = new EncounterNode
                {
                    Id = "approach",
                    TextKey = "encounter.distress_signal.approach",
                    Options = new List<EncounterOption>
                    {
                        new EncounterOption
                        {
                            Id = "proceed",
                            TextKey = "encounter.distress_signal.proceed",
                            SkillCheck = new SkillCheckDef
                            {
                                Stat = CrewStatType.Savvy,
                                Difficulty = Config.MediumDifficulty,
                                BonusTraits = new List<string> { "cautious", "perceptive" }
                            },
                            SuccessOutcome = EncounterOutcome.Goto("genuine_rescue"),
                            FailureOutcome = EncounterOutcome.GotoWith("ambushed",
                                EncounterEffect.ShipDamage(Config.AmbushDamage))
                        },
                        new EncounterOption
                        {
                            Id = "retreat",
                            TextKey = "encounter.distress_signal.retreat",
                            Outcome = EncounterOutcome.Goto("ignored")
                        }
                    }
                },
                ["genuine_rescue"] = new EncounterNode
                {
                    Id = "genuine_rescue",
                    TextKey = "encounter.distress_signal.genuine_rescue",
                    AutoTransition = EncounterOutcome.EndWith(
                        EncounterEffect.AddCredits(Config.RescueReward),
                        EncounterEffect.CrewXp(Config.RescueXp))
                },
                ["ambushed"] = new EncounterNode
                {
                    Id = "ambushed",
                    TextKey = "encounter.distress_signal.ambushed",
                    AutoTransition = EncounterOutcome.End()
                },
                ["avoided_trap"] = new EncounterNode
                {
                    Id = "avoided_trap",
                    TextKey = "encounter.distress_signal.avoided_trap",
                    AutoTransition = EncounterOutcome.EndWith(
                        EncounterEffect.CrewXp(10))
                },
                ["ignored"] = new EncounterNode
                {
                    Id = "ignored",
                    TextKey = "encounter.distress_signal.ignored",
                    AutoTransition = EncounterOutcome.End()
                }
            }
        };
    }

    // ========================================================================
    // TRADER ENCOUNTERS
    // ========================================================================

    /// <summary>
    /// Merchant offers trade opportunity.
    /// Tags: travel, trader, social, choice
    /// </summary>
    public static EncounterTemplate CreateTraderOpportunity()
    {
        return new EncounterTemplate
        {
            Id = "prod_trader_opportunity",
            Name = "Trader Opportunity",
            Tags = new HashSet<string>
            {
                EncounterTags.Travel, EncounterTags.Trader,
                EncounterTags.Social, EncounterTags.Choice
            },
            EntryNodeId = "intro",
            Nodes = new Dictionary<string, EncounterNode>
            {
                ["intro"] = new EncounterNode
                {
                    Id = "intro",
                    TextKey = "encounter.trader.intro",
                    Options = new List<EncounterOption>
                    {
                        new EncounterOption
                        {
                            Id = "buy_fuel",
                            TextKey = "encounter.trader.buy_fuel",
                            Conditions = new List<EncounterCondition>
                            {
                                EncounterCondition.HasCredits(50)
                            },
                            Outcome = EncounterOutcome.GotoWith("traded",
                                EncounterEffect.LoseCredits(50),
                                EncounterEffect.AddFuel(20))
                        },
                        new EncounterOption
                        {
                            Id = "buy_supplies",
                            TextKey = "encounter.trader.buy_supplies",
                            Conditions = new List<EncounterCondition>
                            {
                                EncounterCondition.HasCredits(30)
                            },
                            Outcome = EncounterOutcome.GotoWith("traded",
                                EncounterEffect.LoseCredits(30),
                                EncounterEffect.AddMeds(5),
                                EncounterEffect.AddParts(5))
                        },
                        new EncounterOption
                        {
                            Id = "haggle",
                            TextKey = "encounter.trader.haggle",
                            SkillCheck = new SkillCheckDef
                            {
                                Stat = CrewStatType.Savvy,
                                Difficulty = 6,
                                BonusTraits = new List<string> { "merchant", "smooth_talker" }
                            },
                            SuccessOutcome = EncounterOutcome.Goto("good_deal"),
                            FailureOutcome = EncounterOutcome.Goto("offended")
                        },
                        new EncounterOption
                        {
                            Id = "decline",
                            TextKey = "encounter.trader.decline",
                            Outcome = EncounterOutcome.Goto("declined")
                        }
                    }
                },
                ["traded"] = new EncounterNode
                {
                    Id = "traded",
                    TextKey = "encounter.trader.traded",
                    AutoTransition = EncounterOutcome.End()
                },
                ["good_deal"] = new EncounterNode
                {
                    Id = "good_deal",
                    TextKey = "encounter.trader.good_deal",
                    Options = new List<EncounterOption>
                    {
                        new EncounterOption
                        {
                            Id = "buy_fuel_discount",
                            TextKey = "encounter.trader.buy_fuel_discount",
                            Conditions = new List<EncounterCondition>
                            {
                                EncounterCondition.HasCredits(35)
                            },
                            Outcome = EncounterOutcome.GotoWith("traded",
                                EncounterEffect.LoseCredits(35),
                                EncounterEffect.AddFuel(20))
                        },
                        new EncounterOption
                        {
                            Id = "decline_discount",
                            TextKey = "encounter.trader.decline",
                            Outcome = EncounterOutcome.Goto("declined")
                        }
                    }
                },
                ["offended"] = new EncounterNode
                {
                    Id = "offended",
                    TextKey = "encounter.trader.offended",
                    AutoTransition = EncounterOutcome.End()
                },
                ["declined"] = new EncounterNode
                {
                    Id = "declined",
                    TextKey = "encounter.trader.declined",
                    AutoTransition = EncounterOutcome.End()
                }
            }
        };
    }

    // ========================================================================
    // SMUGGLER ENCOUNTERS
    // ========================================================================

    /// <summary>
    /// Smuggler offers shady deal.
    /// Tags: travel, smuggler, social, choice
    /// </summary>
    public static EncounterTemplate CreateSmugglerContact()
    {
        return new EncounterTemplate
        {
            Id = "prod_smuggler_contact",
            Name = "Smuggler Contact",
            Tags = new HashSet<string>
            {
                EncounterTags.Travel, EncounterTags.Smuggler,
                EncounterTags.Social, EncounterTags.Choice
            },
            EntryNodeId = "intro",
            Nodes = new Dictionary<string, EncounterNode>
            {
                ["intro"] = new EncounterNode
                {
                    Id = "intro",
                    TextKey = "encounter.smuggler.intro",
                    Options = new List<EncounterOption>
                    {
                        new EncounterOption
                        {
                            Id = "accept",
                            TextKey = "encounter.smuggler.accept",
                            Outcome = EncounterOutcome.GotoWith("accepted",
                                EncounterEffect.AddCredits(150))
                        },
                        new EncounterOption
                        {
                            Id = "decline",
                            TextKey = "encounter.smuggler.decline",
                            Outcome = EncounterOutcome.Goto("declined")
                        },
                        new EncounterOption
                        {
                            Id = "report",
                            TextKey = "encounter.smuggler.report",
                            SkillCheck = new SkillCheckDef
                            {
                                Stat = CrewStatType.Grit,
                                Difficulty = 7,
                                BonusTraits = new List<string> { "intimidating" }
                            },
                            SuccessOutcome = EncounterOutcome.GotoWith("intimidated",
                                EncounterEffect.AddCredits(50)),
                            FailureOutcome = EncounterOutcome.GotoWith("hostile",
                                EncounterEffect.ShipDamage(10))
                        }
                    }
                },
                ["accepted"] = new EncounterNode
                {
                    Id = "accepted",
                    TextKey = "encounter.smuggler.accepted",
                    AutoTransition = EncounterOutcome.End()
                },
                ["declined"] = new EncounterNode
                {
                    Id = "declined",
                    TextKey = "encounter.smuggler.declined",
                    AutoTransition = EncounterOutcome.End()
                },
                ["intimidated"] = new EncounterNode
                {
                    Id = "intimidated",
                    TextKey = "encounter.smuggler.intimidated",
                    AutoTransition = EncounterOutcome.End()
                },
                ["hostile"] = new EncounterNode
                {
                    Id = "hostile",
                    TextKey = "encounter.smuggler.hostile",
                    AutoTransition = EncounterOutcome.End()
                }
            }
        };
    }

    // ========================================================================
    // EXPLORATION ENCOUNTERS
    // ========================================================================

    /// <summary>
    /// Derelict ship discovery with salvage opportunity.
    /// Tags: travel, exploration, choice, skill_check
    /// </summary>
    public static EncounterTemplate CreateDerelictDiscovery()
    {
        return new EncounterTemplate
        {
            Id = "prod_derelict_discovery",
            Name = "Derelict Discovery",
            Tags = new HashSet<string>
            {
                EncounterTags.Travel, EncounterTags.Exploration,
                EncounterTags.Choice, EncounterTags.SkillCheck
            },
            EntryNodeId = "intro",
            Nodes = new Dictionary<string, EncounterNode>
            {
                ["intro"] = new EncounterNode
                {
                    Id = "intro",
                    TextKey = "encounter.derelict.intro",
                    Options = new List<EncounterOption>
                    {
                        new EncounterOption
                        {
                            Id = "board",
                            TextKey = "encounter.derelict.board",
                            Outcome = EncounterOutcome.Goto("boarding")
                        },
                        new EncounterOption
                        {
                            Id = "scan",
                            TextKey = "encounter.derelict.scan",
                            SkillCheck = new SkillCheckDef
                            {
                                Stat = CrewStatType.Tech,
                                Difficulty = 5
                            },
                            SuccessOutcome = EncounterOutcome.Goto("scan_complete"),
                            FailureOutcome = EncounterOutcome.Goto("boarding")
                        },
                        new EncounterOption
                        {
                            Id = "ignore",
                            TextKey = "encounter.derelict.ignore",
                            Outcome = EncounterOutcome.Goto("ignored")
                        }
                    }
                },
                ["scan_complete"] = new EncounterNode
                {
                    Id = "scan_complete",
                    TextKey = "encounter.derelict.scan_complete",
                    Options = new List<EncounterOption>
                    {
                        new EncounterOption
                        {
                            Id = "safe_board",
                            TextKey = "encounter.derelict.safe_board",
                            Outcome = EncounterOutcome.Goto("safe_salvage")
                        },
                        new EncounterOption
                        {
                            Id = "leave",
                            TextKey = "encounter.derelict.leave",
                            Outcome = EncounterOutcome.Goto("ignored")
                        }
                    }
                },
                ["boarding"] = new EncounterNode
                {
                    Id = "boarding",
                    TextKey = "encounter.derelict.boarding",
                    Options = new List<EncounterOption>
                    {
                        new EncounterOption
                        {
                            Id = "good_salvage",
                            TextKey = "encounter.derelict.good_salvage",
                            Outcome = EncounterOutcome.GotoWith("salvaged",
                                EncounterEffect.AddCredits(100),
                                EncounterEffect.AddFuel(10))
                        },
                        new EncounterOption
                        {
                            Id = "hazard",
                            TextKey = "encounter.derelict.hazard",
                            Outcome = EncounterOutcome.GotoWith("accident",
                                EncounterEffect.AddCredits(50))
                        }
                    }
                },
                ["safe_salvage"] = new EncounterNode
                {
                    Id = "safe_salvage",
                    TextKey = "encounter.derelict.safe_salvage",
                    AutoTransition = EncounterOutcome.EndWith(
                        EncounterEffect.AddCredits(120),
                        EncounterEffect.AddFuel(15),
                        EncounterEffect.CrewXp(10))
                },
                ["salvaged"] = new EncounterNode
                {
                    Id = "salvaged",
                    TextKey = "encounter.derelict.salvaged",
                    AutoTransition = EncounterOutcome.End()
                },
                ["accident"] = new EncounterNode
                {
                    Id = "accident",
                    TextKey = "encounter.derelict.accident",
                    AutoTransition = EncounterOutcome.End()
                },
                ["ignored"] = new EncounterNode
                {
                    Id = "ignored",
                    TextKey = "encounter.derelict.ignored",
                    AutoTransition = EncounterOutcome.End()
                }
            }
        };
    }

    // ========================================================================
    // FACTION ENCOUNTERS
    // ========================================================================

    /// <summary>
    /// Faction agent offers a proposition.
    /// Tags: travel, faction, social, choice
    /// </summary>
    public static EncounterTemplate CreateFactionAgent()
    {
        return new EncounterTemplate
        {
            Id = "prod_faction_agent",
            Name = "Faction Agent",
            Tags = new HashSet<string>
            {
                EncounterTags.Travel, EncounterTags.Faction,
                EncounterTags.Social, EncounterTags.Choice
            },
            EntryNodeId = "intro",
            Nodes = new Dictionary<string, EncounterNode>
            {
                ["intro"] = new EncounterNode
                {
                    Id = "intro",
                    TextKey = "encounter.faction_agent.intro",
                    Options = new List<EncounterOption>
                    {
                        new EncounterOption
                        {
                            Id = "listen",
                            TextKey = "encounter.faction_agent.listen",
                            Outcome = EncounterOutcome.Goto("proposal")
                        },
                        new EncounterOption
                        {
                            Id = "refuse",
                            TextKey = "encounter.faction_agent.refuse",
                            Outcome = EncounterOutcome.Goto("refused")
                        }
                    }
                },
                ["proposal"] = new EncounterNode
                {
                    Id = "proposal",
                    TextKey = "encounter.faction_agent.proposal",
                    Options = new List<EncounterOption>
                    {
                        new EncounterOption
                        {
                            Id = "accept",
                            TextKey = "encounter.faction_agent.accept",
                            Outcome = EncounterOutcome.GotoWith("accepted",
                                EncounterEffect.AddCredits(100))
                        },
                        new EncounterOption
                        {
                            Id = "negotiate",
                            TextKey = "encounter.faction_agent.negotiate",
                            SkillCheck = new SkillCheckDef
                            {
                                Stat = CrewStatType.Savvy,
                                Difficulty = 6
                            },
                            SuccessOutcome = EncounterOutcome.GotoWith("accepted",
                                EncounterEffect.AddCredits(150)),
                            FailureOutcome = EncounterOutcome.Goto("proposal")
                        },
                        new EncounterOption
                        {
                            Id = "decline",
                            TextKey = "encounter.faction_agent.decline",
                            Outcome = EncounterOutcome.Goto("declined")
                        }
                    }
                },
                ["accepted"] = new EncounterNode
                {
                    Id = "accepted",
                    TextKey = "encounter.faction_agent.accepted",
                    AutoTransition = EncounterOutcome.End()
                },
                ["declined"] = new EncounterNode
                {
                    Id = "declined",
                    TextKey = "encounter.faction_agent.declined",
                    AutoTransition = EncounterOutcome.End()
                },
                ["refused"] = new EncounterNode
                {
                    Id = "refused",
                    TextKey = "encounter.faction_agent.refused",
                    AutoTransition = EncounterOutcome.End()
                }
            }
        };
    }

    // ========================================================================
    // MYSTERY ENCOUNTERS
    // ========================================================================

    /// <summary>
    /// Mysterious signal from unknown source.
    /// Tags: travel, anomaly, exploration, choice, rare
    /// </summary>
    public static EncounterTemplate CreateMysteriousSignal()
    {
        return new EncounterTemplate
        {
            Id = "prod_mysterious_signal",
            Name = "Mysterious Signal",
            Tags = new HashSet<string>
            {
                EncounterTags.Travel, EncounterTags.Anomaly,
                EncounterTags.Exploration, EncounterTags.Choice, EncounterTags.Rare
            },
            EntryNodeId = "intro",
            Nodes = new Dictionary<string, EncounterNode>
            {
                ["intro"] = new EncounterNode
                {
                    Id = "intro",
                    TextKey = "encounter.mysterious_signal.intro",
                    Options = new List<EncounterOption>
                    {
                        new EncounterOption
                        {
                            Id = "investigate",
                            TextKey = "encounter.mysterious_signal.investigate",
                            Outcome = EncounterOutcome.Goto("investigation")
                        },
                        new EncounterOption
                        {
                            Id = "analyze",
                            TextKey = "encounter.mysterious_signal.analyze",
                            SkillCheck = new SkillCheckDef
                            {
                                Stat = CrewStatType.Tech,
                                Difficulty = 7,
                                BonusTraits = new List<string> { "scientist", "curious" }
                            },
                            SuccessOutcome = EncounterOutcome.Goto("decoded"),
                            FailureOutcome = EncounterOutcome.Goto("investigation")
                        },
                        new EncounterOption
                        {
                            Id = "ignore",
                            TextKey = "encounter.mysterious_signal.ignore",
                            Outcome = EncounterOutcome.Goto("ignored")
                        }
                    }
                },
                ["investigation"] = new EncounterNode
                {
                    Id = "investigation",
                    TextKey = "encounter.mysterious_signal.investigation",
                    Options = new List<EncounterOption>
                    {
                        new EncounterOption
                        {
                            Id = "discovery",
                            TextKey = "encounter.mysterious_signal.discovery",
                            Outcome = EncounterOutcome.GotoWith("found_something",
                                EncounterEffect.AddCredits(200),
                                EncounterEffect.CrewXp(25))
                        },
                        new EncounterOption
                        {
                            Id = "nothing",
                            TextKey = "encounter.mysterious_signal.nothing",
                            Outcome = EncounterOutcome.GotoWith("dead_end",
                                EncounterEffect.TimeDelay(1))
                        }
                    }
                },
                ["decoded"] = new EncounterNode
                {
                    Id = "decoded",
                    TextKey = "encounter.mysterious_signal.decoded",
                    AutoTransition = EncounterOutcome.EndWith(
                        EncounterEffect.AddCredits(250),
                        EncounterEffect.CrewXp(30))
                },
                ["found_something"] = new EncounterNode
                {
                    Id = "found_something",
                    TextKey = "encounter.mysterious_signal.found_something",
                    AutoTransition = EncounterOutcome.End()
                },
                ["dead_end"] = new EncounterNode
                {
                    Id = "dead_end",
                    TextKey = "encounter.mysterious_signal.dead_end",
                    AutoTransition = EncounterOutcome.End()
                },
                ["ignored"] = new EncounterNode
                {
                    Id = "ignored",
                    TextKey = "encounter.mysterious_signal.ignored",
                    AutoTransition = EncounterOutcome.End()
                }
            }
        };
    }

    // ========================================================================
    // SHIP ENCOUNTERS
    // ========================================================================

    /// <summary>
    /// Mechanical failure during travel.
    /// Tags: travel, ship, resource, choice
    /// </summary>
    public static EncounterTemplate CreateMechanicalFailure()
    {
        return new EncounterTemplate
        {
            Id = "prod_mechanical_failure",
            Name = "Mechanical Failure",
            Tags = new HashSet<string>
            {
                EncounterTags.Travel, EncounterTags.Ship,
                EncounterTags.Resource, EncounterTags.Choice
            },
            EntryNodeId = "intro",
            Nodes = new Dictionary<string, EncounterNode>
            {
                ["intro"] = new EncounterNode
                {
                    Id = "intro",
                    TextKey = "encounter.mechanical_failure.intro",
                    Options = new List<EncounterOption>
                    {
                        new EncounterOption
                        {
                            Id = "repair",
                            TextKey = "encounter.mechanical_failure.repair",
                            SkillCheck = new SkillCheckDef
                            {
                                Stat = CrewStatType.Tech,
                                Difficulty = 5,
                                BonusTraits = new List<string> { "engineer", "mechanic" }
                            },
                            SuccessOutcome = EncounterOutcome.Goto("repaired"),
                            FailureOutcome = EncounterOutcome.Goto("partial_repair")
                        },
                        new EncounterOption
                        {
                            Id = "jury_rig",
                            TextKey = "encounter.mechanical_failure.jury_rig",
                            Outcome = EncounterOutcome.GotoWith("jury_rigged",
                                EncounterEffect.ShipDamage(5))
                        },
                        new EncounterOption
                        {
                            Id = "use_parts",
                            TextKey = "encounter.mechanical_failure.use_parts",
                            Conditions = new List<EncounterCondition>
                            {
                                EncounterCondition.HasCredits(40)
                            },
                            Outcome = EncounterOutcome.GotoWith("fixed_with_parts",
                                EncounterEffect.LoseCredits(40))
                        }
                    }
                },
                ["repaired"] = new EncounterNode
                {
                    Id = "repaired",
                    TextKey = "encounter.mechanical_failure.repaired",
                    AutoTransition = EncounterOutcome.EndWith(
                        EncounterEffect.CrewXp(10))
                },
                ["partial_repair"] = new EncounterNode
                {
                    Id = "partial_repair",
                    TextKey = "encounter.mechanical_failure.partial_repair",
                    AutoTransition = EncounterOutcome.EndWith(
                        EncounterEffect.ShipDamage(10),
                        EncounterEffect.TimeDelay(1))
                },
                ["jury_rigged"] = new EncounterNode
                {
                    Id = "jury_rigged",
                    TextKey = "encounter.mechanical_failure.jury_rigged",
                    AutoTransition = EncounterOutcome.End()
                },
                ["fixed_with_parts"] = new EncounterNode
                {
                    Id = "fixed_with_parts",
                    TextKey = "encounter.mechanical_failure.fixed_with_parts",
                    AutoTransition = EncounterOutcome.End()
                }
            }
        };
    }

    // ========================================================================
    // SOCIAL ENCOUNTERS
    // ========================================================================

    /// <summary>
    /// Refugees request help.
    /// Tags: travel, social, choice, generic
    /// </summary>
    public static EncounterTemplate CreateRefugeePlea()
    {
        return new EncounterTemplate
        {
            Id = "prod_refugee_plea",
            Name = "Refugee Plea",
            Tags = new HashSet<string>
            {
                EncounterTags.Travel, EncounterTags.Social,
                EncounterTags.Choice, EncounterTags.Generic
            },
            EntryNodeId = "intro",
            Nodes = new Dictionary<string, EncounterNode>
            {
                ["intro"] = new EncounterNode
                {
                    Id = "intro",
                    TextKey = "encounter.refugee_plea.intro",
                    Options = new List<EncounterOption>
                    {
                        new EncounterOption
                        {
                            Id = "help",
                            TextKey = "encounter.refugee_plea.help",
                            Conditions = new List<EncounterCondition>
                            {
                                EncounterCondition.HasCredits(25)
                            },
                            Outcome = EncounterOutcome.GotoWith("helped",
                                EncounterEffect.LoseCredits(25))
                        },
                        new EncounterOption
                        {
                            Id = "offer_passage",
                            TextKey = "encounter.refugee_plea.offer_passage",
                            Outcome = EncounterOutcome.GotoWith("passengers",
                                EncounterEffect.LoseFuel(10),
                                EncounterEffect.AddCredits(50))
                        },
                        new EncounterOption
                        {
                            Id = "decline",
                            TextKey = "encounter.refugee_plea.decline",
                            Outcome = EncounterOutcome.Goto("declined")
                        },
                        new EncounterOption
                        {
                            Id = "investigate",
                            TextKey = "encounter.refugee_plea.investigate",
                            SkillCheck = new SkillCheckDef
                            {
                                Stat = CrewStatType.Savvy,
                                Difficulty = 5
                            },
                            SuccessOutcome = EncounterOutcome.Goto("truth_revealed"),
                            FailureOutcome = EncounterOutcome.Goto("intro")
                        }
                    }
                },
                ["helped"] = new EncounterNode
                {
                    Id = "helped",
                    TextKey = "encounter.refugee_plea.helped",
                    AutoTransition = EncounterOutcome.EndWith(
                        EncounterEffect.CrewXp(15))
                },
                ["passengers"] = new EncounterNode
                {
                    Id = "passengers",
                    TextKey = "encounter.refugee_plea.passengers",
                    AutoTransition = EncounterOutcome.End()
                },
                ["declined"] = new EncounterNode
                {
                    Id = "declined",
                    TextKey = "encounter.refugee_plea.declined",
                    AutoTransition = EncounterOutcome.End()
                },
                ["truth_revealed"] = new EncounterNode
                {
                    Id = "truth_revealed",
                    TextKey = "encounter.refugee_plea.truth_revealed",
                    Options = new List<EncounterOption>
                    {
                        new EncounterOption
                        {
                            Id = "still_help",
                            TextKey = "encounter.refugee_plea.still_help",
                            Outcome = EncounterOutcome.GotoWith("helped",
                                EncounterEffect.LoseCredits(25))
                        },
                        new EncounterOption
                        {
                            Id = "leave",
                            TextKey = "encounter.refugee_plea.leave",
                            Outcome = EncounterOutcome.Goto("declined")
                        }
                    }
                }
            }
        };
    }

    // ========================================================================
    // TRAIT-GRANTING ENCOUNTERS
    // ========================================================================

    /// <summary>
    /// A dangerous situation that can harden a crew member.
    /// Tags: travel, combat, skill_check, trait
    /// </summary>
    public static EncounterTemplate CreateTrialByFire()
    {
        return new EncounterTemplate
        {
            Id = "prod_trial_by_fire",
            Name = "Trial by Fire",
            Tags = new HashSet<string>
            {
                EncounterTags.Travel, EncounterTags.Combat,
                EncounterTags.SkillCheck
            },
            EntryNodeId = "intro",
            Nodes = new Dictionary<string, EncounterNode>
            {
                ["intro"] = new EncounterNode
                {
                    Id = "intro",
                    TextKey = "encounter.trial_by_fire.intro",
                    Options = new List<EncounterOption>
                    {
                        new EncounterOption
                        {
                            Id = "stand_ground",
                            TextKey = "encounter.trial_by_fire.stand_ground",
                            SkillCheck = new SkillCheckDef
                            {
                                Stat = CrewStatType.Resolve,
                                Difficulty = Config.HardDifficulty,
                                BonusTraits = new List<string> { "brave", "ex_military" }
                            },
                            SuccessOutcome = EncounterOutcome.Goto("hardened_success"),
                            FailureOutcome = EncounterOutcome.GotoWith("shaken",
                                EncounterEffect.ShipDamage(Config.FightDamage))
                        },
                        new EncounterOption
                        {
                            Id = "retreat",
                            TextKey = "encounter.trial_by_fire.retreat",
                            Outcome = EncounterOutcome.GotoWith("fled",
                                EncounterEffect.TimeDelay(1))
                        }
                    }
                },
                ["hardened_success"] = new EncounterNode
                {
                    Id = "hardened_success",
                    TextKey = "encounter.trial_by_fire.hardened_success",
                    AutoTransition = EncounterOutcome.EndWith(
                        EncounterEffect.AddTrait("hardened"),
                        EncounterEffect.CrewXp(20))
                },
                ["shaken"] = new EncounterNode
                {
                    Id = "shaken",
                    TextKey = "encounter.trial_by_fire.shaken",
                    AutoTransition = EncounterOutcome.End()
                },
                ["fled"] = new EncounterNode
                {
                    Id = "fled",
                    TextKey = "encounter.trial_by_fire.fled",
                    AutoTransition = EncounterOutcome.End()
                }
            }
        };
    }
}
