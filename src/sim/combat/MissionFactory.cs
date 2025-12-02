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
    /// </summary>
    public static MissionBuildResult BuildFromCampaign(CampaignState campaign, MissionConfig config, int? seed = null)
    {
        var result = new MissionBuildResult();
        var combat = new CombatState(seed ?? System.Environment.TickCount);
        result.CombatState = combat;

        // Configure map
        combat.MapState.GridSize = config.GridSize;
        combat.MissionConfig = config;

        // Spawn crew from campaign with configured weapon
        var crewWeapon = WeaponData.FromId(config.CrewWeaponId);
        var aliveCrew = campaign.GetAliveCrew();
        for (int i = 0; i < aliveCrew.Count && i < config.CrewSpawnPositions.Count; i++)
        {
            var crewMember = aliveCrew[i];
            var spawnPos = config.CrewSpawnPositions[i];

            var actor = combat.AddActor("crew", spawnPos);
            actor.CrewId = crewMember.Id;
            actor.EquippedWeapon = crewWeapon;

            result.ActorToCrewMap[actor.Id] = crewMember.Id;

            SimLog.Log($"[MissionFactory] Spawned {crewMember.Name} (Crew#{crewMember.Id}) as Actor#{actor.Id} at {spawnPos} with {crewWeapon.Name}");
        }

        // Spawn enemies from definitions
        SpawnEnemies(combat, config);

        return result;
    }

    /// <summary>
    /// Build a CombatState for sandbox mode (no campaign).
    /// </summary>
    public static CombatState BuildSandbox(MissionConfig config, int? seed = null)
    {
        var combat = new CombatState(seed ?? System.Environment.TickCount);

        // Configure map
        combat.MapState.GridSize = config.GridSize;
        combat.MissionConfig = config;

        // Spawn sandbox crew with configured weapon
        var crewWeapon = WeaponData.FromId(config.CrewWeaponId);
        var sandboxCrewCount = Mathf.Min(3, config.CrewSpawnPositions.Count);
        for (int i = 0; i < sandboxCrewCount; i++)
        {
            var spawnPos = config.CrewSpawnPositions[i];
            var actor = combat.AddActor("crew", spawnPos);
            actor.EquippedWeapon = crewWeapon;
            SimLog.Log($"[MissionFactory] Spawned sandbox crew Actor#{actor.Id} at {spawnPos} with {crewWeapon.Name}");
        }

        // Spawn enemies from definitions
        SpawnEnemies(combat, config);

        return combat;
    }

    private static void SpawnEnemies(CombatState combat, MissionConfig config)
    {
        foreach (var spawn in config.EnemySpawns)
        {
            var enemyDef = Definitions.Enemies.Get(spawn.EnemyId);
            var weaponData = WeaponData.FromId(enemyDef.WeaponId);

            var actor = combat.AddActor("enemy", spawn.Position);
            actor.Hp = enemyDef.Hp;
            actor.MaxHp = enemyDef.Hp;
            actor.EquippedWeapon = weaponData;

            SimLog.Log($"[MissionFactory] Spawned {enemyDef.Name} (Actor#{actor.Id}) at {spawn.Position} with {weaponData.Name}, HP:{enemyDef.Hp}");
        }
    }
}
