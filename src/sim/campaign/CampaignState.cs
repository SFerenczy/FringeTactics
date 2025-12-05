using System;
using System.Collections.Generic;

namespace FringeTactics;

public class CampaignState
{
    // Time tracking
    public CampaignTime Time { get; private set; } = new();

    // RNG service for deterministic generation
    public RngService Rng { get; private set; }
    
    /// <summary>
    /// Event bus for cross-domain communication (optional, set by GameState).
    /// </summary>
    public EventBus EventBus { get; set; }

    // Resources
    public int Money { get; set; } = 0;
    public int Fuel { get; set; } = 100;
    public int Parts { get; set; } = 0;
    public int Meds { get; set; } = 5;
    public int Ammo { get; set; } = 50;

    // World state - canonical representation of the sector
    public WorldState World { get; set; }
    public int CurrentNodeId { get; set; } = 0;
    
    // Ship (MG2)
    public Ship Ship { get; set; }

    // Inventory (MG2)
    public Inventory Inventory { get; set; } = new();

    // Crew roster
    public List<CrewMember> Crew { get; set; } = new();
    private int nextCrewId = 0;

    // Jobs
    public List<Job> AvailableJobs { get; set; } = new();
    public Job CurrentJob { get; set; } = null;
    private int nextJobId = 0;

    // Faction reputation (factionId -> rep, 0-100, 50 = neutral)
    public Dictionary<string, int> FactionRep { get; set; } = new();

    // Mission tracking
    public int MissionsCompleted { get; set; } = 0;
    public int MissionsFailed { get; set; } = 0;

    // Campaign statistics (for end screen)
    public int TotalMoneyEarned { get; set; } = 0;
    public int TotalCrewDeaths { get; set; } = 0;

    // Configuration (loaded from data/campaign.json)
    private static CampaignConfig Config => CampaignConfig.Instance;

    public CampaignState()
    {
    }

    /// <summary>
    /// Generate a unique job ID for this campaign.
    /// </summary>
    public string GenerateJobId()
    {
        return $"job_{nextJobId++}";
    }

    /// <summary>
    /// Create a new campaign with starting crew and resources.
    /// </summary>
    public static CampaignState CreateNew(int sectorSeed = 12345)
    {
        var starting = Config.Starting;
        var campaign = new CampaignState
        {
            Money = starting.Money,
            Fuel = starting.Fuel,
            Parts = starting.Parts,
            Meds = starting.Meds,
            Ammo = starting.Ammo,
            Time = new CampaignTime(),
            Rng = new RngService(sectorSeed)
        };

        // Initialize world state (G1: single hub)
        campaign.World = WorldState.CreateSingleHub("Haven Station", "corp");
        campaign.CurrentNodeId = 0; // Start at Haven Station

        // Create starter ship (MG2)
        campaign.Ship = Ship.CreateStarter();

        // Initialize faction reputation (50 = neutral)
        foreach (var factionId in campaign.World.Factions.Keys)
        {
            campaign.FactionRep[factionId] = 50;
        }

        // Add 4 starting crew members with varied roles
        campaign.AddCrew("Alex", CrewRole.Soldier);
        campaign.AddCrew("Jordan", CrewRole.Soldier);
        campaign.AddCrew("Morgan", CrewRole.Medic);
        campaign.AddCrew("Casey", CrewRole.Tech);

        // Generate initial jobs at starting location
        campaign.RefreshJobsAtCurrentNode();

        return campaign;
    }

    /// <summary>
    /// Refresh available jobs at current node using campaign RNG.
    /// </summary>
    public void RefreshJobsAtCurrentNode()
    {
        AvailableJobs = JobSystem.GenerateJobsForNode(this, CurrentNodeId);
        SimLog.Log($"[Campaign] Generated {AvailableJobs.Count} jobs at {GetCurrentSystem()?.Name}");
    }

    /// <summary>
    /// Create a seeded Random from campaign RNG for deterministic generation.
    /// </summary>
    private Random CreateSeededRandom()
    {
        return new Random(Rng?.Campaign?.NextInt(int.MaxValue) ?? Environment.TickCount);
    }

    /// <summary>
    /// Accept a job and set it as current.
    /// </summary>
    public bool AcceptJob(Job job)
    {
        if (CurrentJob != null)
        {
            SimLog.Log("[Campaign] Cannot accept job - already have an active job");
            return false;
        }

        if (!AvailableJobs.Contains(job))
        {
            SimLog.Log("[Campaign] Cannot accept job - not in available jobs");
            return false;
        }

        CurrentJob = job;
        AvailableJobs.Remove(job);

        // Set absolute deadline from relative days
        if (job.DeadlineDays > 0)
        {
            job.DeadlineDay = Time.CurrentDay + job.DeadlineDays;
            SimLog.Log($"[Campaign] Job deadline: Day {job.DeadlineDay} ({job.DeadlineDays} days from now)");
        }

        // Generate mission config for the job
        // TODO: Use RNG when MissionConfig generation becomes procedural
        CurrentJob.MissionConfig = JobSystem.GenerateMissionConfig(job);

        SimLog.Log($"[Campaign] Accepted job: {job.Title} at {World?.GetSystem(job.TargetNodeId)?.Name}");
        
        EventBus?.Publish(new JobAcceptedEvent(
            JobId: job.Id,
            JobTitle: job.Title,
            TargetNodeId: job.TargetNodeId,
            DeadlineDay: job.DeadlineDay
        ));
        
        return true;
    }

    /// <summary>
    /// Clear current job (on completion or abandonment).
    /// Regenerates jobs if the board is empty.
    /// </summary>
    public void ClearCurrentJob()
    {
        CurrentJob = null;
        
        // Regenerate jobs if board is empty (G1 loop)
        if (AvailableJobs.Count == 0)
        {
            RefreshJobsAtCurrentNode();
        }
    }

    /// <summary>
    /// Check if player is at the target node for current job.
    /// </summary>
    public bool IsAtJobTarget()
    {
        return CurrentJob != null && CurrentNodeId == CurrentJob.TargetNodeId;
    }

    /// <summary>
    /// Get faction reputation (0-100, 50 = neutral).
    /// </summary>
    public int GetFactionRep(string factionId)
    {
        return FactionRep.TryGetValue(factionId, out var rep) ? rep : 50;
    }

    /// <summary>
    /// Modify faction reputation.
    /// </summary>
    public void ModifyFactionRep(string factionId, int delta)
    {
        if (string.IsNullOrEmpty(factionId)) return;

        int oldRep = FactionRep.GetValueOrDefault(factionId, 50);
        if (!FactionRep.ContainsKey(factionId))
        {
            FactionRep[factionId] = 50;
        }

        FactionRep[factionId] = Math.Clamp(FactionRep[factionId] + delta, 0, 100);
        int newRep = FactionRep[factionId];
        var factionName = World?.GetFactionName(factionId) ?? factionId;
        SimLog.Log($"[Campaign] {factionName} rep: {newRep} ({(delta >= 0 ? "+" : "")}{delta})");
        
        EventBus?.Publish(new FactionRepChangedEvent(
            FactionId: factionId,
            FactionName: factionName,
            OldRep: oldRep,
            NewRep: newRep,
            Delta: delta
        ));
    }

    /// <summary>
    /// Get the current system.
    /// </summary>
    public StarSystem GetCurrentSystem()
    {
        return World?.GetSystem(CurrentNodeId);
    }

    // ========================================================================
    // RESOURCE OPERATIONS (MG2)
    // ========================================================================

    /// <summary>
    /// Get current value of a resource.
    /// </summary>
    public int GetResource(string type) => type switch
    {
        ResourceTypes.Money => Money,
        ResourceTypes.Fuel => Fuel,
        ResourceTypes.Parts => Parts,
        ResourceTypes.Ammo => Ammo,
        _ => 0
    };

    /// <summary>
    /// Set a resource value directly (used internally).
    /// </summary>
    private void SetResource(string type, int value)
    {
        switch (type)
        {
            case ResourceTypes.Money: Money = value; break;
            case ResourceTypes.Fuel: Fuel = value; break;
            case ResourceTypes.Parts: Parts = value; break;
            case ResourceTypes.Ammo: Ammo = value; break;
        }
    }

    /// <summary>
    /// Spend a resource. Returns false if insufficient.
    /// </summary>
    public bool SpendResource(string type, int amount, string reason = "unknown")
    {
        if (amount <= 0) return false;
        
        int current = GetResource(type);
        if (current < amount) return false;

        int oldValue = current;
        SetResource(type, current - amount);

        SimLog.Log($"[Campaign] Spent {amount} {type} ({reason}). Remaining: {GetResource(type)}");
        EventBus?.Publish(new ResourceChangedEvent(type, oldValue, GetResource(type), -amount, reason));

        return true;
    }

    /// <summary>
    /// Add a resource.
    /// </summary>
    public void AddResource(string type, int amount, string reason = "unknown")
    {
        if (amount <= 0) return;

        int oldValue = GetResource(type);
        SetResource(type, oldValue + amount);

        if (type == ResourceTypes.Money)
            TotalMoneyEarned += amount;

        SimLog.Log($"[Campaign] Gained {amount} {type} ({reason}). Total: {GetResource(type)}");
        EventBus?.Publish(new ResourceChangedEvent(type, oldValue, GetResource(type), amount, reason));
    }

    // Typed wrappers for API convenience - all delegate to SpendResource/AddResource
    public bool SpendCredits(int amount, string reason = "unknown") => SpendResource(ResourceTypes.Money, amount, reason);
    public void AddCredits(int amount, string reason = "unknown") => AddResource(ResourceTypes.Money, amount, reason);
    public bool SpendFuel(int amount, string reason = "unknown") => SpendResource(ResourceTypes.Fuel, amount, reason);
    public void AddFuel(int amount, string reason = "unknown") => AddResource(ResourceTypes.Fuel, amount, reason);
    public bool SpendParts(int amount, string reason = "unknown") => SpendResource(ResourceTypes.Parts, amount, reason);
    public void AddParts(int amount, string reason = "unknown") => AddResource(ResourceTypes.Parts, amount, reason);
    public bool SpendAmmo(int amount, string reason = "unknown") => SpendResource(ResourceTypes.Ammo, amount, reason);
    public void AddAmmo(int amount, string reason = "unknown") => AddResource(ResourceTypes.Ammo, amount, reason);
    public bool SpendMeds(int amount, string reason = "unknown") => SpendResource(ResourceTypes.Meds, amount, reason);
    public void AddMeds(int amount, string reason = "unknown") => AddResource(ResourceTypes.Meds, amount, reason);

    /// <summary>
    /// Check if player can afford a purchase.
    /// </summary>
    public bool CanAfford(int credits = 0, int fuel = 0, int parts = 0)
    {
        return Money >= credits && Fuel >= fuel && Parts >= parts;
    }

    // ========================================================================
    // INVENTORY OPERATIONS (MG2)
    // ========================================================================

    /// <summary>
    /// Get current cargo capacity from ship.
    /// </summary>
    public int GetCargoCapacity() => Ship?.GetCargoCapacity() ?? 0;

    /// <summary>
    /// Get used cargo space.
    /// </summary>
    public int GetUsedCargo() => Inventory?.GetUsedVolume() ?? 0;

    /// <summary>
    /// Get available cargo space.
    /// </summary>
    public int GetAvailableCargo() => GetCargoCapacity() - GetUsedCargo();

    /// <summary>
    /// Add an item to inventory. Returns the item or null if no space.
    /// </summary>
    public Item AddItem(string defId, int quantity = 1)
    {
        var item = Inventory.AddItem(defId, quantity, GetCargoCapacity());
        if (item != null)
        {
            var def = ItemRegistry.Get(defId);
            SimLog.Log($"[Campaign] Added {quantity}x {def?.Name ?? defId} to inventory");
            EventBus?.Publish(new ItemAddedEvent(item.Id, defId, def?.Name ?? defId, quantity));
        }
        return item;
    }

    /// <summary>
    /// Remove an item from inventory by instance ID.
    /// Automatically unequips from any crew member first.
    /// </summary>
    public bool RemoveItem(string itemId)
    {
        var item = Inventory.FindById(itemId);
        if (item == null) return false;

        // Unequip from any crew member who has this item equipped
        UnequipItemFromAllCrew(itemId);

        var def = item.GetDef();
        var quantity = item.Quantity;
        if (Inventory.RemoveItem(itemId))
        {
            SimLog.Log($"[Campaign] Removed {def?.Name ?? item.DefId} from inventory");
            EventBus?.Publish(new ItemRemovedEvent(itemId, item.DefId, def?.Name ?? item.DefId, quantity));
            return true;
        }
        return false;
    }

    /// <summary>
    /// Unequip an item from all crew members who have it equipped.
    /// </summary>
    private void UnequipItemFromAllCrew(string itemId)
    {
        foreach (var crew in Crew)
        {
            if (crew.EquippedWeaponId == itemId)
            {
                crew.EquippedWeaponId = null;
                EventBus?.Publish(new ItemUnequippedEvent(crew.Id, crew.Name, "unknown", "weapon"));
            }
            if (crew.EquippedArmorId == itemId)
            {
                crew.EquippedArmorId = null;
                EventBus?.Publish(new ItemUnequippedEvent(crew.Id, crew.Name, "unknown", "armor"));
            }
            if (crew.EquippedGadgetId == itemId)
            {
                crew.EquippedGadgetId = null;
                EventBus?.Publish(new ItemUnequippedEvent(crew.Id, crew.Name, "unknown", "gadget"));
            }
        }
    }

    /// <summary>
    /// Remove quantity of an item by definition ID.
    /// </summary>
    public bool RemoveItemByDef(string defId, int quantity = 1)
    {
        if (Inventory.RemoveByDefId(defId, quantity))
        {
            var def = ItemRegistry.Get(defId);
            SimLog.Log($"[Campaign] Removed {quantity}x {def?.Name ?? defId} from inventory");
            EventBus?.Publish(new ItemRemovedEvent(null, defId, def?.Name ?? defId, quantity));
            return true;
        }
        return false;
    }

    /// <summary>
    /// Check if inventory has an item.
    /// </summary>
    public bool HasItem(string defId, int quantity = 1) => Inventory.HasItem(defId, quantity);

    // ========================================================================
    // SHIP OPERATIONS (MG2)
    // ========================================================================

    /// <summary>
    /// Repair ship hull using parts.
    /// </summary>
    public bool RepairShip(int partsToUse)
    {
        if (Ship == null || Ship.Hull >= Ship.MaxHull) return false;
        if (Parts < partsToUse) return false;
        if (partsToUse <= 0) return false;

        int repairAmount = partsToUse * 2; // 2 hull per part
        int oldHull = Ship.Hull;

        SpendParts(partsToUse, "ship_repair");
        Ship.Repair(repairAmount);

        SimLog.Log($"[Campaign] Repaired ship: {oldHull} -> {Ship.Hull} hull");
        EventBus?.Publish(new ShipHullChangedEvent(oldHull, Ship.Hull, Ship.MaxHull, "repair"));

        return true;
    }

    /// <summary>
    /// Apply damage to ship hull.
    /// </summary>
    public void DamageShip(int amount, string reason = "unknown")
    {
        if (Ship == null || amount <= 0) return;

        int oldHull = Ship.Hull;
        Ship.TakeDamage(amount);

        SimLog.Log($"[Campaign] Ship damaged: {oldHull} -> {Ship.Hull} hull ({reason})");
        EventBus?.Publish(new ShipHullChangedEvent(oldHull, Ship.Hull, Ship.MaxHull, reason));
    }

    /// <summary>
    /// Install a module from inventory onto ship.
    /// </summary>
    public bool InstallModule(string itemId)
    {
        if (Ship == null) return false;

        var item = Inventory.FindById(itemId);
        if (item == null) return false;

        var def = item.GetDef();
        if (def == null || def.Category != ItemCategory.Module) return false;

        if (!Enum.TryParse<ShipSlotType>(def.ModuleSlotType, out var slotType)) return false;

        var module = new ShipModule
        {
            Id = $"module_{item.Id}",
            DefId = def.Id,
            SlotType = slotType
        };

        if (!Ship.InstallModule(module)) return false;

        Inventory.RemoveItem(itemId);

        SimLog.Log($"[Campaign] Installed module: {def.Name}");
        EventBus?.Publish(new ShipModuleInstalledEvent(module.Id, def.Id, def.Name, slotType.ToString()));

        return true;
    }

    /// <summary>
    /// Remove a module from ship and add to inventory.
    /// </summary>
    public bool RemoveModule(string moduleId)
    {
        if (Ship == null) return false;

        var module = Ship.FindModule(moduleId);
        if (module == null) return false;

        if (!Ship.RemoveModule(moduleId)) return false;

        // Add module item back to inventory (modules have 0 volume so always fits)
        AddItem(module.DefId);

        SimLog.Log($"[Campaign] Removed module: {module.Name}");
        EventBus?.Publish(new ShipModuleRemovedEvent(moduleId, module.DefId, module.Name, module.SlotType.ToString()));

        return true;
    }

    // ========================================================================
    // EQUIPMENT OPERATIONS (MG2)
    // ========================================================================

    /// <summary>
    /// Equip an item to a crew member.
    /// </summary>
    public bool EquipItem(int crewId, string itemId)
    {
        var crew = GetCrewById(crewId);
        if (crew == null || crew.IsDead) return false;

        var item = Inventory.FindById(itemId);
        if (item == null) return false;

        var def = item.GetDef();
        if (def == null || def.Category != ItemCategory.Equipment) return false;
        if (def.EquipSlot == EquipSlot.None) return false;

        // Unequip current item in that slot first
        var currentId = crew.GetEquipped(def.EquipSlot);
        if (!string.IsNullOrEmpty(currentId))
        {
            UnequipItem(crewId, def.EquipSlot);
        }

        // Equip new item
        crew.SetEquipped(def.EquipSlot, itemId);

        SimLog.Log($"[Campaign] {crew.Name} equipped {def.Name}");
        EventBus?.Publish(new ItemEquippedEvent(crewId, crew.Name, itemId, def.Id, def.EquipSlot.ToString().ToLower()));

        return true;
    }

    /// <summary>
    /// Unequip an item from a crew member.
    /// </summary>
    public bool UnequipItem(int crewId, EquipSlot slot)
    {
        var crew = GetCrewById(crewId);
        if (crew == null) return false;

        var itemId = crew.GetEquipped(slot);
        if (string.IsNullOrEmpty(itemId)) return false;

        var item = Inventory.FindById(itemId);
        var defId = item?.DefId ?? "unknown";

        crew.ClearEquipped(slot);

        SimLog.Log($"[Campaign] {crew.Name} unequipped {slot}");
        EventBus?.Publish(new ItemUnequippedEvent(crewId, crew.Name, defId, slot.ToString().ToLower()));

        return true;
    }

    /// <summary>
    /// Get the item equipped by a crew member in a slot.
    /// </summary>
    public Item GetEquippedItem(int crewId, EquipSlot slot)
    {
        var crew = GetCrewById(crewId);
        if (crew == null) return null;

        var itemId = crew.GetEquipped(slot);
        if (string.IsNullOrEmpty(itemId)) return null;

        return Inventory.FindById(itemId);
    }

    // ========================================================================
    // CREW OPERATIONS
    // ========================================================================

    /// <summary>
    /// Add a crew member without cost (for initial setup, testing).
    /// </summary>
    public CrewMember AddCrew(string name, CrewRole role = CrewRole.Soldier)
    {
        return CreateAndAddCrew(name, role);
    }

    /// <summary>
    /// Hire a new crew member. Costs credits.
    /// </summary>
    /// <param name="name">Crew member name</param>
    /// <param name="role">Crew role</param>
    /// <param name="cost">Hiring cost in credits</param>
    /// <returns>The hired crew member, or null if can't afford</returns>
    public CrewMember HireCrew(string name, CrewRole role, int cost)
    {
        if (Money < cost)
        {
            SimLog.Log($"[Campaign] Cannot hire {name}: insufficient funds ({Money}/{cost})");
            return null;
        }

        int oldMoney = Money;
        Money -= cost;

        var crew = CreateAndAddCrew(name, role);

        SimLog.Log($"[Campaign] Hired {name} ({role}) for {cost} credits");

        EventBus?.Publish(new ResourceChangedEvent(
            ResourceTypes.Money, oldMoney, Money, -cost, "hire_crew"));
        EventBus?.Publish(new CrewHiredEvent(crew.Id, crew.Name, role, cost));

        return crew;
    }

    /// <summary>
    /// Internal: creates crew and adds to roster.
    /// </summary>
    private CrewMember CreateAndAddCrew(string name, CrewRole role)
    {
        var crew = CrewMember.CreateWithRole(nextCrewId, name, role);
        nextCrewId++;
        Crew.Add(crew);
        return crew;
    }

    /// <summary>
    /// Fire a crew member. They are removed from the roster.
    /// </summary>
    /// <param name="crewId">ID of crew to fire</param>
    /// <returns>True if fired, false if not found, dead, or last crew</returns>
    public bool FireCrew(int crewId)
    {
        var crew = GetCrewById(crewId);
        if (crew == null)
        {
            SimLog.Log($"[Campaign] Cannot fire crew {crewId}: not found");
            return false;
        }

        if (crew.IsDead)
        {
            SimLog.Log($"[Campaign] Cannot fire {crew.Name}: already dead");
            return false;
        }

        if (GetAliveCrew().Count <= 1)
        {
            SimLog.Log($"[Campaign] Cannot fire {crew.Name}: last crew member");
            return false;
        }

        string name = crew.Name;
        Crew.Remove(crew);
        SimLog.Log($"[Campaign] Fired {name}");

        EventBus?.Publish(new CrewFiredEvent(crewId, name));

        return true;
    }

    /// <summary>
    /// Remove a dead crew member from the roster (bury them).
    /// </summary>
    public bool BuryDeadCrew(int crewId)
    {
        var crew = GetCrewById(crewId);
        if (crew == null)
        {
            return false;
        }

        if (!crew.IsDead)
        {
            SimLog.Log($"[Campaign] Cannot bury {crew.Name}: not dead");
            return false;
        }

        string name = crew.Name;
        Crew.Remove(crew);
        SimLog.Log($"[Campaign] Buried {name}");
        return true;
    }

    /// <summary>
    /// Remove all dead crew from the roster.
    /// </summary>
    public int BuryAllDeadCrew()
    {
        int count = 0;
        for (int i = Crew.Count - 1; i >= 0; i--)
        {
            if (Crew[i].IsDead)
            {
                SimLog.Log($"[Campaign] Buried {Crew[i].Name}");
                Crew.RemoveAt(i);
                count++;
            }
        }
        return count;
    }

    /// <summary>
    /// Assign a trait to a crew member.
    /// </summary>
    public bool AssignTrait(int crewId, string traitId)
    {
        var crew = GetCrewById(crewId);
        if (crew == null || crew.IsDead) return false;

        if (!crew.AddTrait(traitId)) return false;

        var trait = TraitRegistry.Get(traitId);
        SimLog.Log($"[Campaign] {crew.Name} gained trait: {trait?.Name ?? traitId}");

        EventBus?.Publish(new CrewTraitChangedEvent(
            crewId, crew.Name, traitId, trait?.Name ?? traitId, true));
        return true;
    }

    /// <summary>
    /// Remove a trait from a crew member.
    /// </summary>
    public bool RemoveTrait(int crewId, string traitId)
    {
        var crew = GetCrewById(crewId);
        if (crew == null) return false;

        if (!crew.RemoveTrait(traitId)) return false;

        var trait = TraitRegistry.Get(traitId);
        SimLog.Log($"[Campaign] {crew.Name} lost trait: {trait?.Name ?? traitId}");

        EventBus?.Publish(new CrewTraitChangedEvent(
            crewId, crew.Name, traitId, trait?.Name ?? traitId, false));
        return true;
    }

    public List<CrewMember> GetAliveCrew()
    {
        var alive = new List<CrewMember>();
        foreach (var crew in Crew)
        {
            if (!crew.IsDead)
            {
                alive.Add(crew);
            }
        }
        return alive;
    }

    public List<CrewMember> GetDeployableCrew()
    {
        var deployable = new List<CrewMember>();
        foreach (var crew in Crew)
        {
            if (crew.CanDeploy())
            {
                deployable.Add(crew);
            }
        }
        return deployable;
    }

    /// <summary>
    /// Check if we can afford to start a mission.
    /// </summary>
    public bool CanStartMission()
    {
        if (GetDeployableCrew().Count == 0) return false;
        if (Fuel < Config.Mission.FuelCost) return false;
        return true;
    }

    /// <summary>
    /// Get reason why mission can't start.
    /// </summary>
    public string GetMissionBlockReason()
    {
        if (GetDeployableCrew().Count == 0) return "No deployable crew!";
        if (Fuel < Config.Mission.FuelCost) return $"Need {Config.Mission.FuelCost} fuel (have {Fuel})";
        return null;
    }

    /// <summary>
    /// Consume resources when starting a mission (MG3).
    /// Fuel is consumed upfront; ammo is tracked per-actor and consumed in ApplyMissionOutput.
    /// </summary>
    public void ConsumeMissionResources()
    {
        // Consume fuel for mission deployment
        if (Fuel > 0 && Config.Mission.FuelCost > 0)
        {
            SpendFuel(Config.Mission.FuelCost, "mission_start");
        }
        
        // Advance time for mission
        Time.AdvanceDays(Config.Mission.TimeDays);
        
        SimLog.Log($"[Campaign] Mission started. Cost: {Config.Mission.FuelCost} fuel, {Config.Mission.TimeDays} day(s). Ammo tracked per-actor.");
    }
    
    /// <summary>
    /// Calculate total ammo needed for a mission based on deployable crew (MG3).
    /// Used for UI display and pre-mission validation.
    /// </summary>
    public int CalculateMissionAmmoNeeded()
    {
        int total = 0;
        foreach (var crew in GetAliveCrew())
        {
            if (!crew.CanDeploy()) continue;
            
            // Resolve weapon using centralized method
            string weaponId = crew.GetEffectiveWeaponId(Inventory);
            var weapon = WeaponData.FromId(weaponId);
            
            // Use StatFormulas for ammo calculation
            total += StatFormulas.CalculateTotalAmmoNeeded(weapon.MagazineSize);
        }
        return total;
    }
    
    /// <summary>
    /// Check if campaign has enough ammo for a mission.
    /// </summary>
    public bool HasEnoughAmmoForMission()
    {
        return Ammo >= CalculateMissionAmmoNeeded();
    }
    
    /// <summary>
    /// Rest at current location. Heals injuries, advances time.
    /// </summary>
    /// <returns>Number of injuries healed.</returns>
    public int Rest()
    {
        int healed = 0;

        foreach (var crew in GetAliveCrew())
        {
            if (healed >= Config.Rest.HealAmount) break;
            if (crew.Injuries.Count > 0)
            {
                var injury = crew.Injuries[0];
                crew.HealInjury(injury);
                healed++;
                SimLog.Log($"[Campaign] {crew.Name}'s {injury} healed during rest.");
            }
        }

        Time.AdvanceDays(Config.Rest.TimeDays);
        SimLog.Log($"[Campaign] Rested for {Config.Rest.TimeDays} days. Healed {healed} injury(ies).");

        return healed;
    }

    /// <summary>
    /// Check if rest would be beneficial (any injuries to heal).
    /// </summary>
    public bool ShouldRest()
    {
        foreach (var crew in GetAliveCrew())
        {
            if (crew.Injuries.Count > 0) return true;
        }
        return false;
    }

    /// <summary>
    /// Apply mission output to campaign state (MG3).
    /// Handles crew outcomes, rewards, ammo consumption, loot, and events.
    /// </summary>
    public void ApplyMissionOutput(MissionOutput output)
    {
        // Track total ammo used for consumption
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
                EventBus?.Publish(new CrewDiedEvent(crew.Id, crew.Name, "killed_in_action"));
                continue;
            }

            // Handle MIA (treated as dead for now)
            if (crewOutcome.Status == CrewFinalStatus.MIA)
            {
                crew.IsDead = true;
                TotalCrewDeaths++;
                SimLog.Log($"[Campaign] {crew.Name} MIA - presumed dead.");
                EventBus?.Publish(new CrewDiedEvent(crew.Id, crew.Name, "missing_in_action"));
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
                int oldLevel = crew.Level;
                bool leveledUp = crew.AddXp(crewOutcome.SuggestedXp);
                if (leveledUp)
                {
                    SimLog.Log($"[Campaign] {crew.Name} leveled up to {crew.Level}!");
                    EventBus?.Publish(new CrewLeveledUpEvent(crew.Id, crew.Name, oldLevel, crew.Level));
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
    /// Process loot items from mission (MG3).
    /// </summary>
    private void ProcessMissionLoot(List<LootItem> loot)
    {
        if (loot == null || loot.Count == 0) return;
        
        foreach (var item in loot)
        {
            switch (item.Type)
            {
                case LootType.Credits:
                    AddCredits(item.Quantity, "mission_loot");
                    break;
                    
                case LootType.Resource:
                    AddLootResource(item.ResourceKind, item.Quantity);
                    break;
                    
                case LootType.Item:
                    AddLootItem(item);
                    break;
            }
        }
    }
    
    /// <summary>
    /// Add a resource from loot using ResourceType enum.
    /// </summary>
    private void AddLootResource(ResourceType? resourceType, int amount)
    {
        if (amount <= 0 || resourceType == null) return;
        
        string type = ResourceTypes.FromEnum(resourceType.Value);
        if (type != null)
        {
            AddResource(type, amount, "mission_loot");
        }
    }
    
    /// <summary>
    /// Add a loot item to inventory.
    /// </summary>
    private void AddLootItem(LootItem loot)
    {
        if (string.IsNullOrEmpty(loot.ItemDefId)) return;
        
        int capacity = Ship?.GetCargoCapacity() ?? 100;
        var item = Inventory?.AddItem(loot.ItemDefId, loot.Quantity, capacity);
        
        if (item != null)
        {
            SimLog.Log($"[Campaign] Looted {loot.Quantity}x {loot.Name}");
            EventBus?.Publish(new LootAcquiredEvent(loot.ItemDefId, loot.Name, loot.Quantity, "mission"));
        }
        else
        {
            SimLog.Log($"[Campaign] No cargo space for {loot.Name}");
        }
    }
    
    /// <summary>
    /// Apply mission rewards based on outcome (MG3).
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
                EventBus?.Publish(new JobCompletedEvent(CurrentJob.Id, CurrentJob.Title, true, CurrentJob.Reward?.Money ?? 0));
                ClearCurrentJob();
            }
            else
            {
                int oldMoney = Money;
                int oldParts = Parts;
                Money += Config.Rewards.VictoryMoney;
                Parts += Config.Rewards.VictoryParts;
                SimLog.Log($"[Campaign] Victory! +${Config.Rewards.VictoryMoney}, +{Config.Rewards.VictoryParts} parts.");
                EventBus?.Publish(new ResourceChangedEvent(ResourceTypes.Money, oldMoney, Money, Config.Rewards.VictoryMoney, "victory_bonus"));
                EventBus?.Publish(new ResourceChangedEvent(ResourceTypes.Parts, oldParts, Parts, Config.Rewards.VictoryParts, "victory_bonus"));
            }
        }
        else if (isRetreat)
        {
            // Retreat: partial failure, no rewards but reduced penalty
            MissionsFailed++;
            
            if (CurrentJob != null)
            {
                // Half the reputation loss for retreat vs full failure
                ModifyFactionRep(CurrentJob.EmployerFactionId, -CurrentJob.FailureRepLoss / 2);
                SimLog.Log($"[Campaign] Job abandoned (retreat): {CurrentJob.Title}");
                EventBus?.Publish(new JobCompletedEvent(CurrentJob.Id, CurrentJob.Title, false, 0));
                ClearCurrentJob();
            }
            else
            {
                SimLog.Log("[Campaign] Mission retreat. No rewards.");
            }
        }
        else
        {
            // Defeat or Abort
            MissionsFailed++;

            if (CurrentJob != null)
            {
                ModifyFactionRep(CurrentJob.EmployerFactionId, -CurrentJob.FailureRepLoss);
                SimLog.Log($"[Campaign] Job failed: {CurrentJob.Title}");
                EventBus?.Publish(new JobCompletedEvent(CurrentJob.Id, CurrentJob.Title, false, 0));
                ClearCurrentJob();
            }
            else
            {
                SimLog.Log("[Campaign] Mission failed. No rewards.");
            }
        }
    }

    /// <summary>
    /// Apply job reward to campaign resources.
    /// </summary>
    private void ApplyJobReward(JobReward reward)
    {
        int oldMoney = Money;
        int oldParts = Parts;
        int oldFuel = Fuel;
        int oldAmmo = Ammo;
        
        Money += reward.Money;
        TotalMoneyEarned += reward.Money;
        Parts += reward.Parts;
        Fuel += reward.Fuel;
        Ammo += reward.Ammo;
        SimLog.Log($"[Campaign] Reward: {reward}");
        
        if (reward.Money > 0)
        {
            EventBus?.Publish(new ResourceChangedEvent(ResourceTypes.Money, oldMoney, Money, reward.Money, "job_reward"));
        }
        if (reward.Parts > 0)
        {
            EventBus?.Publish(new ResourceChangedEvent(ResourceTypes.Parts, oldParts, Parts, reward.Parts, "job_reward"));
        }
        if (reward.Fuel > 0)
        {
            EventBus?.Publish(new ResourceChangedEvent(ResourceTypes.Fuel, oldFuel, Fuel, reward.Fuel, "job_reward"));
        }
        if (reward.Ammo > 0)
        {
            EventBus?.Publish(new ResourceChangedEvent(ResourceTypes.Ammo, oldAmmo, Ammo, reward.Ammo, "job_reward"));
        }
    }

    /// <summary>
    /// Check if the campaign is over (all crew dead).
    /// </summary>
    public bool IsCampaignOver()
    {
        return GetAliveCrew().Count == 0;
    }

    /// <summary>
    /// Use meds to heal an injured crew member.
    /// </summary>
    public bool HealCrewMember(int crewId)
    {
        if (Meds <= 0) return false;

        var crew = GetCrewById(crewId);
        if (crew == null || crew.IsDead || crew.Injuries.Count == 0) return false;

        // Heal one injury
        if (crew.Injuries.Count > 0)
        {
            var injury = crew.Injuries[0];
            crew.HealInjury(injury);
            Meds--;
            SimLog.Log($"[Campaign] Healed {crew.Name}'s {injury}. Meds remaining: {Meds}");
            return true;
        }

        return false;
    }

    public CrewMember GetCrewById(int id)
    {
        foreach (var crew in Crew)
        {
            if (crew.Id == id)
            {
                return crew;
            }
        }
        return null;
    }

    /// <summary>
    /// Get state for serialization.
    /// </summary>
    public CampaignStateData GetState()
    {
        var data = new CampaignStateData
        {
            Time = Time.GetState(),
            Rng = Rng?.GetState(),
            CurrentNodeId = CurrentNodeId,
            NextCrewId = nextCrewId,
            NextJobId = nextJobId,
            FactionRep = new Dictionary<string, int>(FactionRep),
            Resources = new ResourcesData
            {
                Money = Money,
                Fuel = Fuel,
                Parts = Parts,
                Meds = Meds,
                Ammo = Ammo
            },
            Stats = new CampaignStatsData
            {
                MissionsCompleted = MissionsCompleted,
                MissionsFailed = MissionsFailed,
                TotalMoneyEarned = TotalMoneyEarned,
                TotalCrewDeaths = TotalCrewDeaths
            },
            World = World?.GetState(),
            Ship = Ship?.GetState(),
            Inventory = Inventory?.GetState()
        };

        // Serialize crew
        foreach (var crew in Crew)
        {
            data.Crew.Add(crew.GetState());
        }

        // Serialize available jobs
        foreach (var job in AvailableJobs)
        {
            data.AvailableJobs.Add(job.GetState());
        }

        // Serialize current job
        data.CurrentJob = CurrentJob?.GetState();

        return data;
    }

    /// <summary>
    /// Restore campaign from saved state.
    /// </summary>
    public static CampaignState FromState(CampaignStateData data)
    {
        var campaign = new CampaignState();

        // Restore time
        campaign.Time = new CampaignTime();
        if (data.Time != null)
        {
            campaign.Time.RestoreState(data.Time);
        }

        // Restore RNG
        if (data.Rng != null)
        {
            campaign.Rng = new RngService(data.Rng.MasterSeed);
            campaign.Rng.RestoreState(data.Rng);
        }
        else
        {
            campaign.Rng = new RngService();
        }

        // Restore resources
        if (data.Resources != null)
        {
            campaign.Money = data.Resources.Money;
            campaign.Fuel = data.Resources.Fuel;
            campaign.Parts = data.Resources.Parts;
            campaign.Meds = data.Resources.Meds;
            campaign.Ammo = data.Resources.Ammo;
        }

        // Restore location
        campaign.CurrentNodeId = data.CurrentNodeId;

        // Restore crew
        campaign.Crew.Clear();
        foreach (var crewData in data.Crew ?? new List<CrewMemberData>())
        {
            campaign.Crew.Add(CrewMember.FromState(crewData));
        }
        campaign.nextCrewId = data.NextCrewId;
        campaign.nextJobId = data.NextJobId;

        // Restore world state
        if (data.World != null)
        {
            campaign.World = WorldState.FromState(data.World);
        }

        // Restore ship (MG2)
        campaign.Ship = Ship.FromState(data.Ship);

        // Restore inventory (MG2)
        campaign.Inventory = Inventory.FromState(data.Inventory);

        // Restore jobs
        campaign.AvailableJobs.Clear();
        foreach (var jobData in data.AvailableJobs ?? new List<JobData>())
        {
            campaign.AvailableJobs.Add(Job.FromState(jobData));
        }

        if (data.CurrentJob != null)
        {
            campaign.CurrentJob = Job.FromState(data.CurrentJob);
            // Regenerate MissionConfig (currently deterministic by difficulty)
            if (campaign.CurrentJob != null)
            {
                campaign.CurrentJob.MissionConfig = JobSystem.GenerateMissionConfig(campaign.CurrentJob);
            }
        }

        // Restore faction rep
        campaign.FactionRep = new Dictionary<string, int>(data.FactionRep ?? new Dictionary<string, int>());

        // Restore stats
        if (data.Stats != null)
        {
            campaign.MissionsCompleted = data.Stats.MissionsCompleted;
            campaign.MissionsFailed = data.Stats.MissionsFailed;
            campaign.TotalMoneyEarned = data.Stats.TotalMoneyEarned;
            campaign.TotalCrewDeaths = data.Stats.TotalCrewDeaths;
        }

        return campaign;
    }
}
