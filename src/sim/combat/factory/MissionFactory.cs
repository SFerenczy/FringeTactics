using Godot; // For Vector2I only - no Node/UI types
using System;
using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// Factory for creating CombatState from MissionConfig and CampaignState.
/// Lives in sim layer - no Node/UI dependencies.
/// </summary>
public static class MissionFactory
{
    /// <summary>
    /// Result of building a mission, includes actor-to-crew mapping.
    /// </summary>
    public class MissionBuildResult
    {
        public CombatState CombatState { get; set; }
        public Dictionary<int, int> ActorToCrewMap { get; set; } = new(); // actorId -> crewId
    }

    /// <summary>
    /// Build a CombatState from campaign crew and mission config.
    /// Uses MissionInputBuilder for proper stat/equipment mapping when job is active (MG3).
    /// </summary>
    public static MissionBuildResult BuildFromCampaign(CampaignState campaign, MissionConfig config, int? seed = null)
    {
        // Use MissionInputBuilder if we have an active job (MG3 path)
        if (campaign.CurrentJob != null)
        {
            var input = MissionInputBuilder.Build(campaign, campaign.CurrentJob);
            if (seed.HasValue)
            {
                input.Seed = seed.Value;
            }
            return BuildFromInput(input);
        }
        
        // Legacy path for missions without jobs (sandbox, tests)
        var legacyInput = ConvertConfigToInput(config, seed ?? System.Environment.TickCount);
        
        // Add crew from campaign with proper stats (MG3)
        var aliveCrew = campaign.GetAliveCrew();
        int spawnIndex = 0;
        for (int i = 0; i < aliveCrew.Count && spawnIndex < config.CrewSpawnPositions.Count; i++)
        {
            var crewMember = aliveCrew[i];
            if (!crewMember.CanDeploy()) continue;
            
            // Resolve weapon using centralized method
            string weaponId = crewMember.GetEffectiveWeaponId(campaign.Inventory);
            var crewWeapon = WeaponData.FromId(weaponId);
            
            var inventory = campaign.Inventory;
            var maxHp = crewMember.GetFullMaxHp(inventory);
            legacyInput.Crew.Add(new CrewDeployment
            {
                CampaignCrewId = crewMember.Id,
                Name = crewMember.Name,
                MaxHp = maxHp,
                CurrentHp = maxHp,
                Armor = crewMember.GetArmorValue(inventory),
                Accuracy = StatFormulas.CalculateAccuracy(crewMember.GetFullEffectiveStat(CrewStatType.Aim, inventory)),
                MoveSpeed = StatFormulas.CalculateMoveSpeed(crewMember.GetFullEffectiveStat(CrewStatType.Reflexes, inventory)),
                WeaponId = weaponId,
                AmmoInMagazine = crewWeapon.MagazineSize,
                ReserveAmmo = StatFormulas.CalculateReserveAmmo(crewWeapon.MagazineSize, campaign.Ammo),
                SpawnPosition = config.CrewSpawnPositions[spawnIndex++]
            });
        }
        
        return BuildFromInput(legacyInput);
    }

    /// <summary>
    /// Build a CombatState for sandbox mode (no campaign).
    /// Internally converts to MissionInput and delegates to BuildFromInput.
    /// </summary>
    public static CombatState BuildSandbox(MissionConfig config, int? seed = null)
    {
        var input = ConvertConfigToInput(config, seed ?? System.Environment.TickCount);
        
        // Add sandbox crew
        var crewWeapon = WeaponData.FromId(config.CrewWeaponId);
        var sandboxCrewCount = Mathf.Min(3, config.CrewSpawnPositions.Count);
        for (int i = 0; i < sandboxCrewCount; i++)
        {
            input.Crew.Add(new CrewDeployment
            {
                CampaignCrewId = -1,
                Name = $"Crew {i + 1}",
                MaxHp = 100,
                CurrentHp = 100,
                WeaponId = config.CrewWeaponId,
                AmmoInMagazine = crewWeapon.MagazineSize,
                ReserveAmmo = 90,
                SpawnPosition = config.CrewSpawnPositions[i]
            });
        }
        
        return BuildFromInput(input).CombatState;
    }
    
    /// <summary>
    /// Convert a MissionConfig to MissionInput for unified processing.
    /// </summary>
    private static MissionInput ConvertConfigToInput(MissionConfig config, int seed)
    {
        var input = new MissionInput
        {
            MissionId = config.Id,
            MissionName = config.Name,
            MapTemplate = config.MapTemplate,
            GridSize = config.GridSize,
            Seed = seed
        };
        
        // Convert enemy spawns
        foreach (var spawn in config.EnemySpawns)
        {
            input.Enemies.Add(spawn);
        }
        
        // Convert interactable spawns
        foreach (var spawn in config.InteractableSpawns)
        {
            input.Interactables.Add(spawn);
        }
        
        return input;
    }

    /// <summary>
    /// Build a CombatState from a formal MissionInput (M7).
    /// This is the preferred method for campaign-driven missions.
    /// </summary>
    public static MissionBuildResult BuildFromInput(MissionInput input)
    {
        var result = new MissionBuildResult();
        var combat = new CombatState(input.Seed);
        result.CombatState = combat;

        // Build map from template
        if (input.MapTemplate != null && input.MapTemplate.Length > 0)
        {
            combat.MapState = MapBuilder.BuildFromTemplate(input.MapTemplate, combat.Interactions);
        }
        else if (input.GridSize.HasValue)
        {
            combat.MapState = new MapState(input.GridSize.Value);
        }

        // Store mission config for reference
        combat.MissionConfig = new MissionConfig
        {
            Id = input.MissionId,
            Name = input.MissionName
        };

        combat.InitializeVisibility();

        // Spawn additional interactables from input
        foreach (var spawn in input.Interactables)
        {
            var interactable = combat.Interactions.AddInteractable(spawn.Type, spawn.Position, spawn.Properties);
            if (spawn.InitialState.HasValue)
            {
                interactable.SetState(spawn.InitialState.Value);
            }
            SimLog.Log($"[MissionFactory] Spawned {spawn.Type}#{interactable.Id} at {spawn.Position}");
        }

        // Spawn crew from deployment specs
        var entryZoneIndex = 0;
        foreach (var crew in input.Crew)
        {
            var spawnPos = crew.SpawnPosition ?? GetNextEntryZonePosition(combat.MapState, ref entryZoneIndex);
            var actor = combat.AddActor(ActorType.Crew, spawnPos);

            // Apply crew data
            actor.Name = crew.Name;
            actor.CrewId = crew.CampaignCrewId;
            actor.MaxHp = crew.MaxHp;
            actor.Hp = crew.CurrentHp;
            actor.Armor = crew.Armor;
            actor.Stats["aim"] = (int)((crew.Accuracy - 0.7f) * 100);
            
            // Apply move speed modifier if different from default (base is 4.0 in Actor)
            if (crew.MoveSpeed != 2.0f)
            {
                var speedMultiplier = crew.MoveSpeed / 2.0f;
                actor.Modifiers.Add(StatModifier.Multiplicative("crew_base", StatType.MoveSpeed, speedMultiplier, -1));
            }

            // Apply weapon
            if (!string.IsNullOrEmpty(crew.WeaponId))
            {
                actor.EquippedWeapon = WeaponData.FromId(crew.WeaponId);
                actor.CurrentMagazine = crew.AmmoInMagazine;
                actor.ReserveAmmo = crew.ReserveAmmo;
            }

            result.ActorToCrewMap[actor.Id] = crew.CampaignCrewId;

            SimLog.Log($"[MissionFactory] Spawned {crew.Name} (Crew#{crew.CampaignCrewId}) as Actor#{actor.Id} at {spawnPos}");
        }

        // Set up objectives - SurviveObjective is always required
        combat.ObjectiveEvaluator.AddObjective(new SurviveObjective());
        
        if (input.Enemies.Count > 0)
        {
            combat.ObjectiveEvaluator.AddObjective(new EliminateAllObjective());
        }

        foreach (var spawn in input.Enemies)
        {
            var enemyDef = Definitions.Enemies.Get(spawn.EnemyId);
            var weaponData = WeaponData.FromId(enemyDef.WeaponId);

            var actor = combat.AddActor(ActorType.Enemy, spawn.Position);
            actor.Name = enemyDef.Name;
            actor.Hp = enemyDef.Hp;
            actor.MaxHp = enemyDef.Hp;
            actor.Armor = enemyDef.Armor;
            actor.EquippedWeapon = weaponData;

            SimLog.Log($"[MissionFactory] Spawned {enemyDef.Name} (Actor#{actor.Id}) at {spawn.Position}");
        }

        // Initialize perception system after all actors are spawned
        combat.InitializePerception();

        // Calculate initial visibility
        combat.Visibility.UpdateVisibility(combat.Actors);

        return result;
    }

    /// <summary>
    /// Get next available entry zone position for spawning.
    /// </summary>
    private static Vector2I GetNextEntryZonePosition(MapState map, ref int index)
    {
        if (map.EntryZone.Count == 0)
        {
            return new Vector2I(1, 1); // Fallback
        }

        var pos = map.EntryZone[index % map.EntryZone.Count];
        index++;
        return pos;
    }

}
