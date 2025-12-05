# MG3 – Tactical Integration: Implementation Plan

**Status**: ✅ Complete  
**Depends on**: MG2 (Ship & Resources) ✅ Complete, M7 (Session I/O) ✅ Complete  
**Blocked by**: None

This document breaks down **Milestone MG3** from `ROADMAP.md` into concrete implementation steps.

**Goal**: Connect Management to Tactical via the mission I/O contract, enabling crew stats, equipment, and traits to flow into missions and mission outcomes to flow back into campaign state.

---

## Current State Assessment

### What We Have (From MG1, MG2, M7)

| Component | Status | Notes |
|-----------|--------|-------|
| `MissionInput` | ✅ Complete | Formal input contract with `CrewDeployment`, `MissionContext` |
| `MissionOutput` | ✅ Complete | Formal output contract with `CrewOutcome`, `ObjectiveStatus` |
| `MissionOutputBuilder` | ✅ Complete | Builds output from `CombatState` |
| `MissionFactory.BuildFromInput()` | ✅ Complete | Creates `CombatState` from `MissionInput` |
| `MissionFactory.BuildFromCampaign()` | ⚠️ Partial | Exists but doesn't use full crew stats/equipment |
| `CampaignState.ApplyMissionOutput()` | ⚠️ Partial | Handles deaths, injuries, XP, job rewards |
| `CrewMember` stats | ✅ Complete | 6 stats, traits, derived stats |
| `CrewMember` equipment | ✅ Complete | Weapon, armor, gadget slots |
| `Inventory` | ✅ Complete | Item storage with capacity |
| `Ship` | ✅ Complete | Hull, modules, cargo |

### What MG3 Requires vs What We Have

| MG3 Requirement | Current Status | Gap |
|-----------------|----------------|-----|
| `CreateMissionInput(job)` | ❌ Missing | No formal method to build input from campaign |
| Crew stats → tactical stats | ⚠️ Partial | `BuildFromCampaign` uses hardcoded HP/accuracy |
| Crew equipment → tactical weapon | ⚠️ Partial | Uses `config.CrewWeaponId`, not equipped weapon |
| Crew traits → tactical modifiers | ❌ Missing | Traits not applied to actors |
| Ammo consumption tracking | ⚠️ Partial | Output tracks ammo, but not consumed from inventory |
| Loot/cargo from missions | ❌ Missing | `MissionOutput.Loot` exists but not processed |
| Contract state updates | ⚠️ Partial | Job cleared on completion, but no partial progress |
| Ship damage from missions | ❌ Missing | No mechanism for ship damage during missions |

---

## Architecture Decisions

### Mission Input Builder Pattern

**Decision**: Create `MissionInputBuilder` as a stateless utility class in `src/sim/campaign/`.

**Rationale**:
- Follows existing pattern (`MissionOutputBuilder`)
- Single responsibility: convert campaign state → mission input
- Testable without scenes
- Clear boundary between campaign and tactical domains

### Stat Mapping Strategy

**Decision**: Map crew stats to tactical actor properties via explicit formulas.

| Crew Stat | Tactical Effect |
|-----------|-----------------|
| Grit | MaxHp = 100 + (Grit × 10) |
| Reflexes | Initiative bonus (future), dodge modifier |
| Aim | Accuracy = 0.7 + (Aim × 0.02) |
| Tech | Hacking speed bonus |
| Savvy | (Not used in tactical) |
| Resolve | Stress threshold (future) |

**Rationale**:
- Formulas already defined in `CrewMember` derived stats
- Keeps tactical layer simple (just receives numbers)
- Campaign layer owns the complexity of stat calculation

### Equipment Resolution

**Decision**: Resolve equipped items to weapon/armor data at mission start.

**Flow**:
1. Check `CrewMember.EquippedWeaponId`
2. If set, look up `Item` in `Inventory` → get `ItemDef.WeaponDefId`
3. If not set, fall back to `PreferredWeaponId`
4. Resolve to `WeaponData` for tactical

**Rationale**:
- Equipment is item instances, weapons are definitions
- Allows same weapon type with different item instances
- Graceful fallback for missing equipment

### Ammo Consumption Model

**Decision**: Track ammo at campaign level, consume on mission start, refund unused on return.

**Flow**:
1. **Pre-mission**: Calculate total ammo needed, consume from `CampaignState.Ammo`
2. **During mission**: Track usage per actor
3. **Post-mission**: Refund unused ammo to campaign

**Rationale**:
- Simple model for G1 (no per-weapon ammo types yet)
- Matches existing `ConsumeMissionResources()` pattern
- Can evolve to per-weapon ammo in G2+

### Trait Application

**Decision**: Apply trait modifiers to `CrewDeployment` stats before tactical.

**Rationale**:
- Tactical layer doesn't need to know about traits
- `GetEffectiveStat()` already handles trait modifiers
- Keeps tactical layer simple

---

## Implementation Steps

### Phase 1: Mission Input Builder (Priority: Critical)

#### Step 1.1: Create MissionInputBuilder Class

**New File**: `src/sim/campaign/MissionInputBuilder.cs`

```csharp
using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// Builds MissionInput from campaign state and job configuration.
/// Stateless utility for converting campaign data to tactical input.
/// </summary>
public static class MissionInputBuilder
{
    /// <summary>
    /// Build a complete MissionInput from campaign state and active job.
    /// </summary>
    public static MissionInput Build(CampaignState campaign, Job job)
    {
        var config = job.MissionConfig ?? MissionConfig.CreateTestMission();
        
        var input = new MissionInput
        {
            MissionId = $"job_{job.Id}",
            MissionName = job.Title,
            MapTemplate = config.MapTemplate,
            GridSize = config.GridSize,
            Seed = campaign.Rng?.Next() ?? System.Environment.TickCount,
            Context = BuildContext(campaign, job)
        };
        
        // Add crew deployments
        var aliveCrew = campaign.GetAliveCrew();
        var deployableCrew = GetDeployableCrew(aliveCrew);
        
        for (int i = 0; i < deployableCrew.Count && i < config.CrewSpawnPositions.Count; i++)
        {
            var crew = deployableCrew[i];
            var deployment = BuildCrewDeployment(crew, campaign, config.CrewSpawnPositions[i]);
            input.Crew.Add(deployment);
        }
        
        // Add enemies from config
        foreach (var spawn in config.EnemySpawns)
        {
            input.Enemies.Add(spawn);
        }
        
        // Add interactables from config
        foreach (var spawn in config.InteractableSpawns)
        {
            input.Interactables.Add(spawn);
        }
        
        // Add objectives (placeholder - will be enhanced with objective system)
        input.Objectives.Add(new MissionObjective
        {
            Id = "primary",
            Description = job.Description,
            Type = ObjectiveType.EliminateAll,
            IsPrimary = true
        });
        
        return input;
    }
    
    /// <summary>
    /// Filter crew to only those who can deploy.
    /// </summary>
    private static List<CrewMember> GetDeployableCrew(List<CrewMember> crew)
    {
        var result = new List<CrewMember>();
        foreach (var c in crew)
        {
            if (c.CanDeploy())
            {
                result.Add(c);
            }
        }
        return result;
    }
    
    /// <summary>
    /// Build deployment data for a single crew member.
    /// </summary>
    private static CrewDeployment BuildCrewDeployment(
        CrewMember crew, 
        CampaignState campaign,
        Godot.Vector2I spawnPosition)
    {
        var deployment = new CrewDeployment
        {
            CampaignCrewId = crew.Id,
            Name = crew.Name,
            SpawnPosition = spawnPosition
        };
        
        // Apply stats from crew (using effective stats with trait modifiers)
        ApplyCrewStats(deployment, crew);
        
        // Apply equipment
        ApplyCrewEquipment(deployment, crew, campaign);
        
        return deployment;
    }
    
    /// <summary>
    /// Apply crew stats to deployment.
    /// </summary>
    private static void ApplyCrewStats(CrewDeployment deployment, CrewMember crew)
    {
        // HP from Grit
        deployment.MaxHp = crew.GetMaxHp();
        deployment.CurrentHp = deployment.MaxHp; // Full HP at mission start
        
        // Accuracy from Aim (base 70% + 2% per Aim point)
        int effectiveAim = crew.GetEffectiveStat(CrewStatType.Aim);
        deployment.Accuracy = 0.7f + (effectiveAim * 0.02f);
        
        // Move speed from Reflexes (base 2.0 + 0.1 per Reflexes point)
        int effectiveReflexes = crew.GetEffectiveStat(CrewStatType.Reflexes);
        deployment.MoveSpeed = 2.0f + (effectiveReflexes * 0.1f);
    }
    
    /// <summary>
    /// Apply crew equipment to deployment.
    /// </summary>
    private static void ApplyCrewEquipment(
        CrewDeployment deployment, 
        CrewMember crew, 
        CampaignState campaign)
    {
        // Resolve weapon
        string weaponDefId = ResolveWeaponDefId(crew, campaign);
        deployment.WeaponId = weaponDefId;
        
        // Get weapon data for magazine size
        var weaponData = WeaponData.FromId(weaponDefId);
        deployment.AmmoInMagazine = weaponData.MagazineSize;
        
        // Reserve ammo from campaign pool (simplified for G1)
        deployment.ReserveAmmo = CalculateReserveAmmo(campaign, weaponData);
    }
    
    /// <summary>
    /// Resolve the weapon definition ID for a crew member.
    /// </summary>
    private static string ResolveWeaponDefId(CrewMember crew, CampaignState campaign)
    {
        // Check equipped weapon item
        if (!string.IsNullOrEmpty(crew.EquippedWeaponId))
        {
            var item = campaign.Inventory?.FindById(crew.EquippedWeaponId);
            if (item != null)
            {
                var itemDef = ItemRegistry.Get(item.DefId);
                if (itemDef?.WeaponDefId != null)
                {
                    return itemDef.WeaponDefId;
                }
            }
        }
        
        // Fall back to preferred weapon
        return crew.PreferredWeaponId ?? "rifle";
    }
    
    /// <summary>
    /// Calculate reserve ammo for a crew member.
    /// </summary>
    private static int CalculateReserveAmmo(CampaignState campaign, WeaponData weapon)
    {
        // Simplified: 3 magazines worth, capped by campaign ammo
        int desired = weapon.MagazineSize * 3;
        return System.Math.Min(desired, campaign.Ammo);
    }
    
    /// <summary>
    /// Build mission context from campaign and job.
    /// </summary>
    private static MissionContext BuildContext(CampaignState campaign, Job job)
    {
        var context = new MissionContext
        {
            ContractId = job.Id.ToString(),
            FactionId = job.EmployerFactionId
        };
        
        // Get location info from world state
        var system = campaign.World?.GetSystem(job.TargetNodeId);
        if (system != null)
        {
            context.LocationId = system.Id.ToString();
            context.LocationName = system.Name;
            
            // Add system tags
            foreach (var tag in system.Tags)
            {
                context.Tags.Add(tag);
            }
        }
        
        return context;
    }
}
```

**Acceptance Criteria**:
- [ ] `MissionInputBuilder.Build()` creates complete `MissionInput`
- [ ] Crew stats flow through correctly
- [ ] Equipment resolves to weapon definitions
- [ ] Context includes location and faction info

---

#### Step 1.2: Update MissionFactory.BuildFromCampaign

**File**: `src/sim/combat/factory/MissionFactory.cs`

**Changes**: Delegate to `MissionInputBuilder` for input creation.

```csharp
/// <summary>
/// Build a CombatState from campaign crew and mission config.
/// Uses MissionInputBuilder for proper stat/equipment mapping.
/// </summary>
public static MissionBuildResult BuildFromCampaign(CampaignState campaign, MissionConfig config, int? seed = null)
{
    // Use MissionInputBuilder if we have an active job
    if (campaign.CurrentJob != null)
    {
        var input = MissionInputBuilder.Build(campaign, campaign.CurrentJob);
        if (seed.HasValue)
        {
            input.Seed = seed.Value;
        }
        return BuildFromInput(input);
    }
    
    // Legacy path for missions without jobs
    var legacyInput = ConvertConfigToInput(config, seed ?? System.Environment.TickCount);
    
    // Add crew from campaign with proper stats
    var aliveCrew = campaign.GetAliveCrew();
    for (int i = 0; i < aliveCrew.Count && i < config.CrewSpawnPositions.Count; i++)
    {
        var crewMember = aliveCrew[i];
        var deployment = new CrewDeployment
        {
            CampaignCrewId = crewMember.Id,
            Name = crewMember.Name,
            MaxHp = crewMember.GetMaxHp(),
            CurrentHp = crewMember.GetMaxHp(),
            Accuracy = 0.7f + (crewMember.GetEffectiveStat(CrewStatType.Aim) * 0.02f),
            MoveSpeed = 2.0f + (crewMember.GetEffectiveStat(CrewStatType.Reflexes) * 0.1f),
            WeaponId = crewMember.PreferredWeaponId ?? "rifle",
            AmmoInMagazine = 30,
            ReserveAmmo = 90,
            SpawnPosition = config.CrewSpawnPositions[i]
        };
        legacyInput.Crew.Add(deployment);
    }
    
    return BuildFromInput(legacyInput);
}
```

**Acceptance Criteria**:
- [ ] Uses `MissionInputBuilder` when job is active
- [ ] Falls back to legacy path for sandbox/test missions
- [ ] Crew stats properly mapped in both paths

---

### Phase 2: Enhanced Mission Output Processing (Priority: High)

#### Step 2.1: Enhance ApplyMissionOutput

**File**: `src/sim/campaign/CampaignState.cs`

**Changes**: Add ammo refund, loot processing, and ship damage handling.

```csharp
/// <summary>
/// Apply mission output to campaign state.
/// Handles crew outcomes, rewards, ammo, loot, and ship damage.
/// </summary>
public void ApplyMissionOutput(MissionOutput output)
{
    // Track total ammo used for refund calculation
    int totalAmmoUsed = 0;
    
    // Process each crew outcome
    foreach (var crewOutcome in output.CrewOutcomes)
    {
        var crew = GetCrewById(crewOutcome.CampaignCrewId);
        if (crew == null) continue;

        // Handle death
        if (crewOutcome.Status == CrewFinalStatus.Dead)
        {
            crew.IsDead = true;
            TotalCrewDeaths++;
            SimLog.Log($"[Campaign] {crew.Name} KIA.");
            EventBus?.Publish(new CrewDiedEvent(crew.Id, crew.Name, "mission"));
            continue;
        }

        // Handle MIA (treated as dead for now)
        if (crewOutcome.Status == CrewFinalStatus.MIA)
        {
            crew.IsDead = true;
            TotalCrewDeaths++;
            SimLog.Log($"[Campaign] {crew.Name} MIA - presumed dead.");
            EventBus?.Publish(new CrewDiedEvent(crew.Id, crew.Name, "mia"));
            continue;
        }

        // Apply injuries
        foreach (var injury in crewOutcome.NewInjuries)
        {
            crew.AddInjury(injury);
            SimLog.Log($"[Campaign] {crew.Name} received injury: {injury}");
            EventBus?.Publish(new CrewInjuredEvent(crew.Id, crew.Name, injury));
        }

        // Apply XP
        if (crewOutcome.SuggestedXp > 0)
        {
            bool leveledUp = crew.AddXp(crewOutcome.SuggestedXp);
            if (leveledUp)
            {
                SimLog.Log($"[Campaign] {crew.Name} leveled up to {crew.Level}!");
                EventBus?.Publish(new CrewLeveledUpEvent(crew.Id, crew.Name, crew.Level));
            }
        }
        
        // Track ammo usage
        totalAmmoUsed += crewOutcome.AmmoUsed;
    }
    
    // Consume ammo used during mission
    if (totalAmmoUsed > 0)
    {
        SpendAmmo(totalAmmoUsed, "mission_consumption");
    }

    // Process loot
    ProcessMissionLoot(output.Loot);
    
    // Apply victory/defeat/retreat rewards
    ApplyMissionRewards(output);
}

/// <summary>
/// Process loot items from mission.
/// </summary>
private void ProcessMissionLoot(List<LootItem> loot)
{
    if (loot == null || loot.Count == 0) return;
    
    foreach (var item in loot)
    {
        if (item.Type == LootType.Credits)
        {
            AddCredits(item.Amount, "mission_loot");
        }
        else if (item.Type == LootType.Item)
        {
            // Add item to inventory if space available
            if (Inventory != null && !string.IsNullOrEmpty(item.ItemDefId))
            {
                var added = Inventory.AddByDefId(item.ItemDefId, item.Amount, Ship?.GetCargoCapacity() ?? 100);
                if (added)
                {
                    SimLog.Log($"[Campaign] Looted {item.Amount}x {item.ItemDefId}");
                }
                else
                {
                    SimLog.Log($"[Campaign] No cargo space for {item.ItemDefId}");
                }
            }
        }
    }
}

/// <summary>
/// Apply mission rewards based on outcome.
/// </summary>
private void ApplyMissionRewards(MissionOutput output)
{
    bool isVictory = output.Outcome == MissionOutcome.Victory;
    bool isRetreat = output.Outcome == MissionOutcome.Retreat;

    if (isVictory)
    {
        MissionsCompleted++;

        if (CurrentJob != null)
        {
            ApplyJobReward(CurrentJob.Reward);
            ModifyFactionRep(CurrentJob.EmployerFactionId, CurrentJob.RepGain);
            ModifyFactionRep(CurrentJob.TargetFactionId, -CurrentJob.RepLoss);
            SimLog.Log($"[Campaign] Job completed: {CurrentJob.Title}");
            EventBus?.Publish(new JobCompletedEvent(CurrentJob.Id, CurrentJob.Title, true));
            ClearCurrentJob();
        }
        else
        {
            Money += VICTORY_MONEY;
            Parts += VICTORY_PARTS;
            SimLog.Log($"[Campaign] Victory! +${VICTORY_MONEY}, +{VICTORY_PARTS} parts.");
        }
    }
    else if (isRetreat)
    {
        MissionsFailed++;
        
        if (CurrentJob != null)
        {
            ModifyFactionRep(CurrentJob.EmployerFactionId, -CurrentJob.FailureRepLoss / 2);
            SimLog.Log($"[Campaign] Job abandoned (retreat): {CurrentJob.Title}");
            EventBus?.Publish(new JobCompletedEvent(CurrentJob.Id, CurrentJob.Title, false));
            ClearCurrentJob();
        }
        else
        {
            SimLog.Log("[Campaign] Mission retreat. No rewards.");
        }
    }
    else
    {
        MissionsFailed++;

        if (CurrentJob != null)
        {
            ModifyFactionRep(CurrentJob.EmployerFactionId, -CurrentJob.FailureRepLoss);
            SimLog.Log($"[Campaign] Job failed: {CurrentJob.Title}");
            EventBus?.Publish(new JobCompletedEvent(CurrentJob.Id, CurrentJob.Title, false));
            ClearCurrentJob();
        }
        else
        {
            SimLog.Log("[Campaign] Mission failed. No rewards.");
        }
    }
}
```

**Acceptance Criteria**:
- [ ] Ammo consumption tracked and applied
- [ ] Loot items added to inventory
- [ ] Events published for crew changes
- [ ] Job completion events published

---

#### Step 2.2: Add New Events

**File**: `src/sim/Events.cs`

**Add new event types**:

```csharp
// === Mission Integration Events (MG3) ===

/// <summary>
/// Published when a crew member dies during a mission.
/// </summary>
public record struct CrewDiedEvent(int CrewId, string Name, string Cause);

/// <summary>
/// Published when a crew member is injured during a mission.
/// </summary>
public record struct CrewInjuredEvent(int CrewId, string Name, string InjuryType);

/// <summary>
/// Published when a crew member levels up.
/// </summary>
public record struct CrewLeveledUpEvent(int CrewId, string Name, int NewLevel);

/// <summary>
/// Published when a job is completed (success or failure).
/// </summary>
public record struct JobCompletedEvent(int JobId, string Title, bool Success);

/// <summary>
/// Published when a mission starts.
/// </summary>
public record struct MissionStartedEvent(string MissionId, string MissionName, int CrewCount);

/// <summary>
/// Published when a mission ends.
/// </summary>
public record struct MissionEndedEvent(string MissionId, MissionOutcome Outcome, int CrewSurvived, int CrewLost);
```

**Acceptance Criteria**:
- [ ] All new event types defined
- [ ] Events are record structs (value types)

---

### Phase 3: Ammo Resource Integration (Priority: Medium)

#### Step 3.1: Add Ammo Consumption Methods

**File**: `src/sim/campaign/CampaignState.cs`

**Add methods**:

```csharp
/// <summary>
/// Spend ammo from campaign pool.
/// </summary>
public bool SpendAmmo(int amount, string reason = "")
{
    if (amount <= 0) return true;
    if (Ammo < amount) return false;
    
    int oldAmmo = Ammo;
    Ammo -= amount;
    
    SimLog.Log($"[Campaign] Spent {amount} ammo ({reason}). Remaining: {Ammo}");
    EventBus?.Publish(new ResourceChangedEvent(ResourceTypes.Ammo, oldAmmo, Ammo, -amount, reason));
    
    return true;
}

/// <summary>
/// Add ammo to campaign pool.
/// </summary>
public void AddAmmo(int amount, string reason = "")
{
    if (amount <= 0) return;
    
    int oldAmmo = Ammo;
    Ammo += amount;
    
    SimLog.Log($"[Campaign] Added {amount} ammo ({reason}). Total: {Ammo}");
    EventBus?.Publish(new ResourceChangedEvent(ResourceTypes.Ammo, oldAmmo, Ammo, amount, reason));
}

/// <summary>
/// Calculate total ammo needed for a mission.
/// </summary>
public int CalculateMissionAmmoNeeded()
{
    int total = 0;
    foreach (var crew in GetAliveCrew())
    {
        if (!crew.CanDeploy()) continue;
        
        string weaponId = crew.PreferredWeaponId ?? "rifle";
        var weapon = WeaponData.FromId(weaponId);
        
        // Magazine + 3 reloads
        total += weapon.MagazineSize * 4;
    }
    return total;
}
```

**Acceptance Criteria**:
- [ ] `SpendAmmo` validates and emits events
- [ ] `AddAmmo` emits events
- [ ] `CalculateMissionAmmoNeeded` accounts for all deployable crew

---

#### Step 3.2: Update ConsumeMissionResources

**File**: `src/sim/campaign/CampaignState.cs`

**Update existing method**:

```csharp
/// <summary>
/// Consume resources required to start a mission.
/// </summary>
public void ConsumeMissionResources()
{
    // Fuel cost (existing)
    if (Fuel > 0)
    {
        SpendFuel(MISSION_FUEL_COST, "mission_start");
    }
    
    // Ammo is now tracked per-actor during mission, not consumed upfront
    // The actual consumption happens in ApplyMissionOutput based on AmmoUsed
    
    SimLog.Log("[Campaign] Mission resources consumed.");
}
```

**Acceptance Criteria**:
- [ ] Fuel consumed on mission start
- [ ] Ammo consumption deferred to mission end

---

### Phase 4: Loot System Foundation (Priority: Medium)

#### Step 4.1: Enhance LootItem

**File**: `src/sim/combat/factory/MissionOutput.cs`

**Update LootItem class**:

```csharp
/// <summary>
/// Type of loot item.
/// </summary>
public enum LootType
{
    Credits,
    Item,
    Resource
}

/// <summary>
/// A loot item acquired during a mission.
/// </summary>
public class LootItem
{
    /// <summary>
    /// Type of loot.
    /// </summary>
    public LootType Type { get; set; }
    
    /// <summary>
    /// Amount (for credits/resources) or quantity (for items).
    /// </summary>
    public int Amount { get; set; }
    
    /// <summary>
    /// Item definition ID (for Type == Item).
    /// </summary>
    public string ItemDefId { get; set; }
    
    /// <summary>
    /// Resource type (for Type == Resource).
    /// </summary>
    public string ResourceType { get; set; }
    
    /// <summary>
    /// Create a credits loot item.
    /// </summary>
    public static LootItem Credits(int amount) => new() { Type = LootType.Credits, Amount = amount };
    
    /// <summary>
    /// Create an item loot.
    /// </summary>
    public static LootItem Item(string defId, int quantity = 1) => new() 
    { 
        Type = LootType.Item, 
        ItemDefId = defId, 
        Amount = quantity 
    };
    
    /// <summary>
    /// Create a resource loot.
    /// </summary>
    public static LootItem Resource(string resourceType, int amount) => new()
    {
        Type = LootType.Resource,
        ResourceType = resourceType,
        Amount = amount
    };
}
```

**Acceptance Criteria**:
- [ ] `LootType` enum defined
- [ ] `LootItem` supports credits, items, and resources
- [ ] Factory methods for common loot types

---

### Phase 5: GameState Integration (Priority: High)

#### Step 5.1: Update GameState.StartMission

**File**: `src/core/GameState.cs`

**Update to use MissionInputBuilder and publish events**:

```csharp
public void StartMission()
{
    if (Campaign == null)
    {
        GD.PrintErr("[GameState] Cannot start mission without campaign!");
        return;
    }

    if (!Campaign.CanStartMission())
    {
        GD.Print($"[GameState] Cannot start mission: {Campaign.GetMissionBlockReason()}");
        return;
    }

    if (Campaign.CurrentJob == null)
    {
        GD.PrintErr("[GameState] Cannot start mission without an active job!");
        return;
    }

    if (!Campaign.IsAtJobTarget())
    {
        GD.PrintErr("[GameState] Must be at job target to start mission!");
        return;
    }

    // Consume resources
    Campaign.ConsumeMissionResources();

    // Build mission input using MissionInputBuilder (MG3)
    var input = MissionInputBuilder.Build(Campaign, Campaign.CurrentJob);
    
    // Build combat state from input
    var buildResult = MissionFactory.BuildFromInput(input);
    CurrentCombat = buildResult.CombatState;
    WireEventBus(CurrentCombat);
    actorToCrewMap = buildResult.ActorToCrewMap;

    // Publish mission started event
    EventBus.Publish(new MissionStartedEvent(
        input.MissionId, 
        input.MissionName, 
        input.Crew.Count));

    GD.Print($"[GameState] Starting mission: {Campaign.CurrentJob.Title} with {input.Crew.Count} crew");
    Mode = "mission";
    GetTree().ChangeSceneToFile(MissionScene);
}
```

**Acceptance Criteria**:
- [ ] Uses `MissionInputBuilder` for input creation
- [ ] Publishes `MissionStartedEvent`
- [ ] Logs crew count

---

#### Step 5.2: Update GameState.EndMission

**File**: `src/core/GameState.cs`

**Update to publish mission ended event**:

```csharp
public void EndMission(MissionOutcome outcome, CombatState combatState)
{
    if (Campaign == null)
    {
        Mode = "menu";
        GoToMainMenu();
        return;
    }

    // Build formal mission output using MissionOutputBuilder (M7)
    var output = MissionOutputBuilder.Build(combatState, outcome, actorToCrewMap);
    
    // Log mission summary
    LogMissionSummary(output);
    
    // Calculate crew stats for event
    int crewSurvived = 0;
    int crewLost = 0;
    foreach (var crew in output.CrewOutcomes)
    {
        if (crew.Status == CrewFinalStatus.Dead || crew.Status == CrewFinalStatus.MIA)
            crewLost++;
        else
            crewSurvived++;
    }
    
    // Publish mission ended event
    EventBus.Publish(new MissionEndedEvent(
        output.MissionId,
        outcome,
        crewSurvived,
        crewLost));
    
    // Apply mission output to campaign
    Campaign.ApplyMissionOutput(output);
    CurrentCombat = null;
    actorToCrewMap.Clear();

    // Check for campaign over
    if (Campaign.IsCampaignOver())
    {
        GD.Print("[GameState] Campaign over - all crew lost!");
        Mode = "gameover";
        GetTree().ChangeSceneToFile(CampaignOverScene);
        return;
    }

    Mode = "sector";
    GoToSectorView();
}
```

**Acceptance Criteria**:
- [ ] Publishes `MissionEndedEvent` with crew stats
- [ ] Event published before applying output

---

### Phase 6: Save Version Update

#### Step 6.1: Increment Save Version

**File**: `src/sim/data/SaveData.cs`

No structural changes needed for MG3 - the existing save format handles all data.

---

## MG3 Deliverables Checklist

### Phase 1: Mission Input Builder
- [x] **1.1** Create `MissionInputBuilder` class
- [x] **1.2** Update `MissionFactory.BuildFromCampaign`

### Phase 2: Enhanced Mission Output Processing
- [x] **2.1** Enhance `ApplyMissionOutput` with ammo/loot
- [x] **2.2** Add new events (CrewDied, CrewInjured, etc.)

### Phase 3: Ammo Resource Integration
- [x] **3.1** Add `SpendAmmo`/`AddAmmo` methods (done in Phase 2)
- [x] **3.2** Update `ConsumeMissionResources`
- [x] **3.3** Add `CalculateMissionAmmoNeeded` and `HasEnoughAmmoForMission`

### Phase 4: Loot System Foundation
- [x] **4.1** Enhance `LootItem` class (done in Phase 2)

### Phase 5: GameState Integration
- [x] **5.1** Update `GameState.StartMission` - publishes `MissionStartedEvent`
- [x] **5.2** Update `GameState.EndMission` - publishes `MissionEndedEvent`

### Phase 6: Save Version
- [x] **6.1** Verify no save format changes needed - MG3 adds runtime methods/events only

---

## Testing

### Test Files Created ✅

| File | Tests | Status |
|------|-------|--------|
| `tests/sim/management/MG3MissionInputTests.cs` | 31 tests: Input builder, stat mapping, equipment resolution | ✅ |
| `tests/sim/management/MG3MissionOutputTests.cs` | 17 tests: Output processing, ammo, loot, events | ✅ |
| `tests/sim/management/MG3IntegrationTests.cs` | 9 tests: Full mission flow integration | ✅ |

### Key Test Cases

#### Mission Input Tests
- `MissionInputBuilder_Build_CreatesValidInput`
- `MissionInputBuilder_Build_MapsCrewStats`
- `MissionInputBuilder_Build_ResolvesEquippedWeapon`
- `MissionInputBuilder_Build_FallsBackToPreferredWeapon`
- `MissionInputBuilder_Build_ExcludesNonDeployableCrew`
- `MissionInputBuilder_Build_IncludesContext`
- `CrewDeployment_Accuracy_MatchesAimStat`
- `CrewDeployment_MaxHp_MatchesGritStat`
- `CrewDeployment_MoveSpeed_MatchesReflexesStat`

#### Mission Output Tests
- `ApplyMissionOutput_AppliesDeaths`
- `ApplyMissionOutput_AppliesInjuries`
- `ApplyMissionOutput_AppliesXp`
- `ApplyMissionOutput_ConsumesAmmo`
- `ApplyMissionOutput_ProcessesLoot`
- `ApplyMissionOutput_PublishesCrewDiedEvent`
- `ApplyMissionOutput_PublishesCrewInjuredEvent`
- `ApplyMissionOutput_PublishesJobCompletedEvent`

#### Integration Tests
- `FullMissionFlow_Victory_AppliesRewards`
- `FullMissionFlow_Defeat_AppliesPenalties`
- `FullMissionFlow_Retreat_PartialPenalty`
- `FullMissionFlow_CrewStatsAffectTactical`
- `FullMissionFlow_EquipmentAffectsTactical`

---

## Manual Test Setup

### Test Scenario: Full Mission Integration Flow

**Prerequisites**:
- Campaign with 3+ crew members
- At least one crew with high Aim (5+)
- At least one crew with high Grit (5+)
- Active job at current location

**Steps**:

1. **Verify crew stats before mission**
   - Open crew roster
   - Note HP, accuracy for each crew member
   - Note equipped weapons

2. **Start mission**
   - Accept a job
   - Travel to job target
   - Start mission
   - Verify in console: `[GameState] Starting mission: X with Y crew`

3. **Verify tactical stats match campaign**
   - In mission, check actor HP (should match `100 + Grit*10`)
   - Check accuracy display if available
   - Verify weapons match equipped/preferred

4. **Complete mission with varied outcomes**
   - Let one crew take damage (wounded)
   - Let one crew get kills
   - Complete mission (victory)

5. **Verify post-mission state**
   - Check crew injuries applied
   - Check XP gained
   - Check job rewards applied
   - Check ammo consumed

6. **Test retreat scenario**
   - Start another mission
   - Initiate retreat
   - Verify partial penalties applied

### DevTools Commands (Suggested)

```
/mission input          - Show current MissionInput details
/mission output         - Show last MissionOutput details
/crew stats <id>        - Show crew stats and derived values
/crew deploy            - List deployable crew
/ammo                   - Show campaign ammo pool
```

---

## Files Summary

### New Files
| File | Description |
|------|-------------|
| `src/sim/campaign/MissionInputBuilder.cs` | Builds MissionInput from campaign state |
| `tests/sim/management/MG3MissionInputTests.cs` | Input builder tests |
| `tests/sim/management/MG3MissionOutputTests.cs` | Output processing tests |
| `tests/sim/management/MG3IntegrationTests.cs` | Integration tests |

### Modified Files
| File | Changes |
|------|---------|
| `src/sim/combat/factory/MissionFactory.cs` | Use MissionInputBuilder |
| `src/sim/combat/factory/MissionOutput.cs` | Enhanced LootItem |
| `src/sim/campaign/CampaignState.cs` | Enhanced ApplyMissionOutput, ammo methods |
| `src/sim/Events.cs` | New mission integration events |
| `src/core/GameState.cs` | Use MissionInputBuilder, publish events |

---

## Implementation Order

Recommended order for implementation:

1. **Events.cs** - Add new event types first
2. **MissionOutput.cs** - Enhance LootItem
3. **MissionInputBuilder.cs** - Core input building logic
4. **CampaignState.cs** - Ammo methods, enhanced ApplyMissionOutput
5. **MissionFactory.cs** - Update BuildFromCampaign
6. **GameState.cs** - Wire everything together
7. **Tests** - All test files

---

## Dependencies on Other Milestones

- **WD1 (Single Hub World)**: Location context comes from WorldState
- **GN1 (Contract Generation)**: Jobs provide mission configuration
- **Future (G2)**: Per-weapon ammo types, more complex loot

---

## Open Questions

1. **Injury severity**: Should injury type affect stat penalties in tactical?
2. **Trait tactical effects**: Should some traits have direct tactical effects (e.g., "Brave" = morale bonus)?
3. **Equipment durability**: Should weapons degrade during missions?
4. **Partial ammo refund**: Should unused ammo be refunded, or is it "committed" to the mission?

These can be addressed in implementation or deferred to later milestones.

---

## Success Criteria

MG3 is complete when:

1. ✅ Crew stats (Grit, Aim, Reflexes) affect tactical actor properties
2. ✅ Equipped weapons are used in tactical missions
3. ✅ Mission outcomes properly update campaign state
4. ✅ Ammo consumption is tracked and applied
5. ✅ Events are published for all significant state changes
6. ✅ All tests pass
7. ✅ Manual test scenario completes successfully
