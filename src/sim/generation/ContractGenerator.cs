using System;
using System.Collections.Generic;
using System.Linq;

namespace FringeTactics;

/// <summary>
/// Generates contracts based on player state and world context.
/// Replaces inline generation in JobSystem.
/// </summary>
public class ContractGenerator
{
    // === Weight Constants ===
    // Base weights for contract type selection
    private const int AssaultBaseWeight = 30;
    private const int ExtractionBaseWeight = 20;
    private const int DeliveryBaseWeight = 15;
    private const int EscortBaseWeight = 10;
    private const int RaidBaseWeight = 10;
    private const int HeistBaseWeight = 5;

    // Multipliers for hub metrics (0-5 scale)
    private const int CrimeWeightMultiplier = 5;
    private const int EconomyWeightMultiplier = 5;
    private const int SecurityWeightMultiplier = 3;

    // === Reward Constants ===
    private const int EasyBaseReward = 100;
    private const int MediumBaseReward = 200;
    private const int HardBaseReward = 400;

    private const float FriendlyFactionBonus = 1.15f;
    private const float HostileFactionPenalty = 0.85f;

    private readonly GenerationContext context;
    private readonly Random rng;
    private int nextContractId = 0;

    public ContractGenerator(GenerationContext context)
    {
        this.context = context ?? throw new ArgumentNullException(nameof(context));

        // Create local Random from RNG stream for this generation batch
        this.rng = context.Rng != null
            ? new Random(context.Rng.NextInt(int.MaxValue))
            : new Random();
    }

    /// <summary>
    /// Generate a batch of contracts for the current hub.
    /// </summary>
    /// <param name="count">Number of contracts to generate.</param>
    /// <param name="idPrefix">Prefix for contract IDs.</param>
    public List<Job> GenerateContracts(int count = 3, string idPrefix = "job")
    {
        var contracts = new List<Job>();

        if (context.NearbySystems.Count == 0)
        {
            SimLog.Log("[ContractGenerator] No nearby systems for targets");
            return contracts;
        }

        for (int i = 0; i < count; i++)
        {
            var contract = GenerateSingleContract(idPrefix);
            if (contract != null)
            {
                contracts.Add(contract);
            }
        }

        SimLog.Log($"[ContractGenerator] Generated {contracts.Count} contracts");
        return contracts;
    }

    /// <summary>
    /// Generate a single contract.
    /// </summary>
    private Job GenerateSingleContract(string idPrefix)
    {
        // 1. Select contract type based on hub context
        var contractType = SelectContractType();

        // 2. Select target system
        var targetSystem = SelectTargetSystem(contractType);
        if (targetSystem == null) return null;

        // 3. Determine difficulty based on player power and target
        var difficulty = DetermineDifficulty(targetSystem);

        // 4. Select factions
        var (employerFaction, targetFaction) = SelectFactions(targetSystem);

        // 5. Calculate rewards
        var reward = CalculateReward(difficulty, contractType, employerFaction);

        // 6. Generate objectives
        var (primary, secondaries) = GenerateObjectives(contractType, difficulty);

        // 7. Build contract
        var contractId = $"{idPrefix}_{nextContractId++}";
        var factionName = context.Factions.GetValueOrDefault(employerFaction, employerFaction);

        var contract = new Job(contractId)
        {
            Title = ContractTemplates.GetRandomTitle(contractType, rng),
            Description = ContractTemplates.GetDescription(
                contractType, targetSystem.Name, factionName, reward.Money, rng),
            ContractType = contractType,
            Difficulty = difficulty,
            OriginNodeId = context.CurrentNodeId,
            TargetNodeId = targetSystem.Id,
            EmployerFactionId = employerFaction,
            TargetFactionId = targetFaction,
            Reward = reward,
            RepGain = GetRepGain(difficulty),
            RepLoss = GetRepLoss(difficulty),
            FailureRepLoss = GetFailureRepLoss(difficulty),
            DeadlineDays = GetDeadlineDays(difficulty),
            PrimaryObjective = primary,
            SecondaryObjectives = secondaries
        };

        return contract;
    }

    // ========================================================================
    // CONTRACT TYPE SELECTION
    // ========================================================================

    /// <summary>
    /// Select contract type based on hub metrics and available types.
    /// </summary>
    private ContractType SelectContractType()
    {
        // Build weighted list based on hub context
        var weights = new Dictionary<ContractType, int>();

        // Assault: base weight, higher in high-crime areas
        if (ContractType.Assault.IsImplemented())
        {
            weights[ContractType.Assault] = AssaultBaseWeight + (context.HubCriminalActivity * CrimeWeightMultiplier);
        }

        // Extraction: always available, slightly higher in dangerous areas
        if (ContractType.Extraction.IsImplemented())
        {
            weights[ContractType.Extraction] = ExtractionBaseWeight + (context.HubCriminalActivity * 2);
        }

        // Delivery: higher in high-economic areas
        if (ContractType.Delivery.IsImplemented())
        {
            weights[ContractType.Delivery] = DeliveryBaseWeight + (context.HubEconomicActivity * EconomyWeightMultiplier);
        }

        // Escort: higher in corporate/secure areas
        if (ContractType.Escort.IsImplemented())
        {
            weights[ContractType.Escort] = EscortBaseWeight + (context.HubSecurityLevel * SecurityWeightMultiplier);
        }

        // Raid: higher in contested/low-security areas
        if (ContractType.Raid.IsImplemented())
        {
            weights[ContractType.Raid] = RaidBaseWeight + ((5 - context.HubSecurityLevel) * SecurityWeightMultiplier);
        }

        // Heist: requires Tech crew, higher in corporate areas
        if (ContractType.Heist.IsImplemented() && context.HasRole(CrewRole.Tech))
        {
            weights[ContractType.Heist] = HeistBaseWeight + (context.HubEconomicActivity * 2);
        }

        // Fallback if nothing is implemented
        if (weights.Count == 0)
        {
            return ContractType.Assault;
        }

        return WeightedSelect(weights);
    }

    /// <summary>
    /// Weighted random selection from dictionary.
    /// </summary>
    private T WeightedSelect<T>(Dictionary<T, int> weights)
    {
        int totalWeight = 0;
        foreach (var w in weights.Values) totalWeight += w;

        if (totalWeight == 0) return weights.Keys.First();

        int roll = rng.Next(totalWeight);
        int cumulative = 0;

        foreach (var kvp in weights)
        {
            cumulative += kvp.Value;
            if (roll < cumulative) return kvp.Key;
        }

        return weights.Keys.First();
    }

    // ========================================================================
    // TARGET SELECTION
    // ========================================================================

    private StarSystem SelectTargetSystem(ContractType type)
    {
        if (context.NearbySystems.Count == 0) return null;

        // For now, simple random selection
        // Future: weight by contract type (e.g., Heist prefers corporate systems)
        return context.NearbySystems[rng.Next(context.NearbySystems.Count)];
    }

    // ========================================================================
    // DIFFICULTY SCALING
    // ========================================================================

    /// <summary>
    /// Determine difficulty based on player power and target system.
    /// </summary>
    private JobDifficulty DetermineDifficulty(StarSystem target)
    {
        // Base difficulty from system type
        var baseDifficulty = target.Type switch
        {
            SystemType.Contested => JobDifficulty.Hard,
            SystemType.Derelict => JobDifficulty.Medium,
            SystemType.Asteroid => JobDifficulty.Easy,
            SystemType.Outpost => JobDifficulty.Medium,
            SystemType.Nebula => JobDifficulty.Medium,
            _ => JobDifficulty.Easy
        };

        // Adjust based on player power tier
        baseDifficulty = AdjustForPlayerPower(baseDifficulty);

        // Random variance (20% chance to shift up or down)
        var roll = rng.NextDouble();
        if (roll < 0.2)
        {
            baseDifficulty = ShiftDifficulty(baseDifficulty, -1);
        }
        else if (roll > 0.8)
        {
            baseDifficulty = ShiftDifficulty(baseDifficulty, +1);
        }

        return baseDifficulty;
    }

    private JobDifficulty AdjustForPlayerPower(JobDifficulty baseDiff)
    {
        // Veteran+ players get harder contracts on average
        if (context.PlayerTier >= PowerTier.Veteran)
        {
            return ShiftDifficulty(baseDiff, +1);
        }

        // Rookie players with few completed contracts get easier contracts
        if (context.PlayerTier == PowerTier.Rookie && context.CompletedContracts < 3)
        {
            return ShiftDifficulty(baseDiff, -1);
        }

        return baseDiff;
    }

    private static JobDifficulty ShiftDifficulty(JobDifficulty diff, int delta)
    {
        int newVal = (int)diff + delta;
        return (JobDifficulty)Math.Clamp(newVal, 0, 2);
    }

    // ========================================================================
    // REWARD CALCULATION
    // ========================================================================

    /// <summary>
    /// Calculate reward based on difficulty, contract type, and faction.
    /// Formula: BaseReward * TypeMultiplier * FactionMultiplier
    /// </summary>
    private JobReward CalculateReward(JobDifficulty difficulty, ContractType type, string factionId)
    {
        // Base reward per difficulty (per GN0 design)
        int baseReward = difficulty switch
        {
            JobDifficulty.Easy => EasyBaseReward,
            JobDifficulty.Medium => MediumBaseReward,
            JobDifficulty.Hard => HardBaseReward,
            _ => EasyBaseReward
        };

        // Apply contract type multiplier
        float typeMultiplier = type.GetRewardMultiplier();

        // Apply faction multiplier (friendly factions pay more)
        float factionMultiplier = 1.0f;
        if (context.IsFriendlyWith(factionId))
        {
            factionMultiplier = FriendlyFactionBonus;
        }
        else if (context.IsHostileWith(factionId))
        {
            factionMultiplier = HostileFactionPenalty;
        }

        int finalMoney = (int)(baseReward * typeMultiplier * factionMultiplier);

        // Parts/fuel/ammo based on difficulty
        return new JobReward
        {
            Money = finalMoney,
            Parts = difficulty switch
            {
                JobDifficulty.Easy => 10,
                JobDifficulty.Medium => 25,
                JobDifficulty.Hard => 50,
                _ => 10
            },
            Fuel = difficulty == JobDifficulty.Hard ? 20 : 0,
            Ammo = difficulty != JobDifficulty.Easy ? 10 + (int)difficulty * 5 : 0
        };
    }

    // ========================================================================
    // OBJECTIVE GENERATION
    // ========================================================================

    private (Objective primary, List<Objective> secondaries) GenerateObjectives(
        ContractType type, JobDifficulty difficulty)
    {
        Objective primary = type switch
        {
            ContractType.Assault => Objective.EliminateAll(),
            ContractType.Extraction => Objective.ReachExtraction(),
            ContractType.Escort => Objective.ProtectVIP(),
            ContractType.Delivery => Objective.ReachExtraction(),
            ContractType.Raid => Objective.DestroyTarget(),
            ContractType.Heist => Objective.RetrieveItem("the data"),
            _ => Objective.EliminateAll()
        };

        var secondaries = new List<Objective>();

        // Add secondary objectives based on difficulty
        if (difficulty >= JobDifficulty.Medium)
        {
            secondaries.Add(Objective.NoCasualties());
        }

        if (difficulty >= JobDifficulty.Hard)
        {
            // Time bonus: harder missions have tighter time limits
            int turnLimit = 20;
            secondaries.Add(Objective.TimeBonus(turnLimit));
        }

        // Contract-type-specific bonuses
        switch (type)
        {
            case ContractType.Heist:
                secondaries.Add(Objective.GhostBonus());
                break;

            case ContractType.Extraction:
            case ContractType.Escort:
                if (difficulty >= JobDifficulty.Medium)
                {
                    secondaries.Add(Objective.NoInjuries());
                }
                break;

            case ContractType.Assault:
                // No additional bonuses - straightforward combat
                break;
        }

        return (primary, secondaries);
    }

    // ========================================================================
    // FACTION SELECTION
    // ========================================================================

    private (string employer, string target) SelectFactions(StarSystem targetSystem)
    {
        // Employer: hub's faction, or random if unclaimed
        string employer = context.HubSystem?.OwningFactionId;
        if (string.IsNullOrEmpty(employer) && context.Factions.Count > 0)
        {
            var factionIds = context.Factions.Keys.ToList();
            employer = factionIds[rng.Next(factionIds.Count)];
        }
        employer ??= "independent";

        // Target: whoever controls target system, or pirates
        string target = targetSystem.OwningFactionId ?? "pirates";

        return (employer, target);
    }

    // ========================================================================
    // REP / DEADLINE HELPERS
    // ========================================================================

    private int GetRepGain(JobDifficulty difficulty) => difficulty switch
    {
        JobDifficulty.Easy => 5,
        JobDifficulty.Medium => 10,
        JobDifficulty.Hard => 20,
        _ => 5
    };

    private int GetRepLoss(JobDifficulty difficulty) => difficulty switch
    {
        JobDifficulty.Easy => 3,
        JobDifficulty.Medium => 5,
        JobDifficulty.Hard => 10,
        _ => 3
    };

    private int GetFailureRepLoss(JobDifficulty difficulty) => difficulty switch
    {
        JobDifficulty.Easy => 5,
        JobDifficulty.Medium => 10,
        JobDifficulty.Hard => 15,
        _ => 5
    };

    private int GetDeadlineDays(JobDifficulty difficulty) => difficulty switch
    {
        JobDifficulty.Easy => rng.Next(5, 10),
        JobDifficulty.Medium => rng.Next(7, 14),
        JobDifficulty.Hard => rng.Next(10, 20),
        _ => 7
    };
}
