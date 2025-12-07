using System;
using System.Collections.Generic;
using System.Linq;

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
    
    /// <summary>
    /// Alias for CurrentNodeId for consistency with Travel domain naming.
    /// </summary>
    public int CurrentSystemId
    {
        get => CurrentNodeId;
        set => CurrentNodeId = value;
    }
    
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

    // Campaign flags for encounter state tracking (MG4)
    public Dictionary<string, bool> Flags { get; set; } = new();

    // Mission tracking
    public int MissionsCompleted { get; set; } = 0;
    public int MissionsFailed { get; set; } = 0;

    // Encounter system (GN3)
    /// <summary>
    /// Registry of encounter templates for generation.
    /// Initialized with production templates by default.
    /// Not serialized - recreated on load.
    /// </summary>
    public EncounterTemplateRegistry EncounterRegistry { get; set; }

    /// <summary>
    /// Cached encounter generator instance.
    /// Not serialized - recreated on load.
    /// </summary>
    public EncounterGenerator EncounterGenerator { get; private set; }

    /// <summary>
    /// Currently active encounter instance, if any.
    /// Null when no encounter is in progress.
    /// </summary>
    public EncounterInstance ActiveEncounter { get; set; }

    /// <summary>
    /// Initialize encounter system with registry and generator.
    /// Called by CreateNew and FromState.
    /// </summary>
    private void InitializeEncounterSystem()
    {
        EncounterRegistry ??= EncounterTemplateRegistry.CreateDefault();
        EncounterGenerator = new EncounterGenerator(EncounterRegistry);
    }

    // Campaign statistics (for end screen)
    public int TotalMoneyEarned { get; set; } = 0;
    public int TotalCrewDeaths { get; set; } = 0;

    // Configuration (loaded from data/campaign.json)
    private static CampaignConfig Config => CampaignConfig.Instance;

    public CampaignState()
    {
    }

    /// <summary>
    /// Create a minimal campaign for unit testing.
    /// Only initializes RNG and encounter system, no world generation.
    /// </summary>
    public static CampaignState CreateForTesting(int seed)
    {
        var campaign = new CampaignState
        {
            Rng = new RngService(seed),
            Time = new CampaignTime()
        };
        campaign.InitializeEncounterSystem();
        return campaign;
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
        return CreateNew(sectorSeed, GalaxyConfig.Default);
    }

    /// <summary>
    /// Create a new campaign with custom galaxy configuration.
    /// </summary>
    public static CampaignState CreateNew(int sectorSeed, GalaxyConfig galaxyConfig)
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

        // Generate world using GalaxyGenerator
        var generator = new GalaxyGenerator(galaxyConfig, campaign.Rng.Campaign);
        campaign.World = generator.Generate();

        // Find starting system (first hub)
        var startSystem = FindStartingSystem(campaign.World);
        campaign.CurrentNodeId = startSystem?.Id ?? 0;

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

        // Initialize encounter system (GN3)
        campaign.InitializeEncounterSystem();

        return campaign;
    }

    /// <summary>
    /// Find the best starting system (preferably a hub).
    /// </summary>
    private static StarSystem FindStartingSystem(WorldState world)
    {
        return world.GetAllSystems().FirstOrDefault(s => s.HasTag(WorldTags.Hub))
            ?? world.GetAllSystems().FirstOrDefault();
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

    // ========================================================================
    // CAMPAIGN FLAGS (MG4)
    // ========================================================================

    /// <summary>
    /// Set a campaign flag. Used for encounter state tracking and story arcs.
    /// </summary>
    /// <param name="flagId">Unique identifier for the flag.</param>
    /// <param name="value">Value to set (default true).</param>
    public void SetFlag(string flagId, bool value = true)
    {
        if (string.IsNullOrEmpty(flagId)) return;

        bool oldValue = Flags.GetValueOrDefault(flagId, false);
        Flags[flagId] = value;

        if (oldValue != value)
        {
            SimLog.Log($"[Campaign] Flag '{flagId}' set to {value}");
            EventBus?.Publish(new CampaignFlagChangedEvent(flagId, oldValue, value));
        }
    }

    /// <summary>
    /// Get a campaign flag value.
    /// </summary>
    /// <param name="flagId">Unique identifier for the flag.</param>
    /// <returns>The flag value, or false if not set.</returns>
    public bool GetFlag(string flagId)
    {
        if (string.IsNullOrEmpty(flagId)) return false;
        return Flags.GetValueOrDefault(flagId, false);
    }

    /// <summary>
    /// Check if a flag is set (true).
    /// </summary>
    /// <param name="flagId">Unique identifier for the flag.</param>
    /// <returns>True if the flag exists and is true.</returns>
    public bool HasFlag(string flagId)
    {
        if (string.IsNullOrEmpty(flagId)) return false;
        return Flags.TryGetValue(flagId, out var value) && value;
    }

    /// <summary>
    /// Clear a campaign flag (remove it entirely).
    /// </summary>
    /// <param name="flagId">Unique identifier for the flag.</param>
    /// <returns>True if the flag was removed.</returns>
    public bool ClearFlag(string flagId)
    {
        if (string.IsNullOrEmpty(flagId)) return false;

        if (Flags.Remove(flagId))
        {
            SimLog.Log($"[Campaign] Flag '{flagId}' cleared");
            EventBus?.Publish(new CampaignFlagChangedEvent(flagId, true, false));
            return true;
        }
        return false;
    }

    /// <summary>
    /// Get the current system.
    /// </summary>
    public StarSystem GetCurrentSystem()
    {
        return World?.GetSystem(CurrentNodeId);
    }

    // ========================================================================
    // TRAVEL INTEGRATION (MG4)
    // ========================================================================

    /// <summary>
    /// Consume fuel for a travel segment.
    /// </summary>
    /// <param name="amount">Fuel to consume.</param>
    /// <returns>True if fuel was consumed, false if insufficient.</returns>
    public bool ConsumeTravelFuel(int amount)
    {
        if (amount <= 0) return true;

        if (Fuel < amount)
        {
            SimLog.Log($"[Campaign] Insufficient fuel for travel: {Fuel}/{amount}");
            return false;
        }

        return SpendFuel(amount, "travel");
    }

    /// <summary>
    /// Check if player can afford a travel plan.
    /// </summary>
    /// <param name="fuelCost">Fuel cost to check.</param>
    /// <returns>True if player has enough fuel.</returns>
    public bool CanAffordTravel(int fuelCost)
    {
        return Fuel >= fuelCost;
    }

    /// <summary>
    /// Check if player can afford a travel plan.
    /// </summary>
    /// <param name="plan">The travel plan to check.</param>
    /// <returns>True if the plan is valid and player has enough fuel.</returns>
    public bool CanAffordTravel(TravelPlan plan)
    {
        if (plan == null || !plan.IsValid) return false;
        return Fuel >= plan.TotalFuelCost;
    }

    /// <summary>
    /// Get the reason why a travel plan cannot be afforded.
    /// </summary>
    /// <param name="plan">The travel plan to check.</param>
    /// <returns>A human-readable reason, or null if affordable.</returns>
    public string GetTravelBlockReason(TravelPlan plan)
    {
        if (plan == null) return "No travel plan";
        if (!plan.IsValid) return $"Invalid route: {plan.InvalidReason}";
        if (Fuel < plan.TotalFuelCost) return $"Insufficient fuel: {Fuel}/{plan.TotalFuelCost}";
        return null;
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
        ResourceTypes.Meds => Meds,
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
            case ResourceTypes.Meds: Meds = value; break;
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
    /// Uses campaign RNG to roll a starting trait.
    /// </summary>
    private CrewMember CreateAndAddCrew(string name, CrewRole role)
    {
        var crew = CrewMember.CreateWithRole(nextCrewId, name, role, Rng?.Campaign);
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
                EventBus?.Publish(new CrewXpGainedEvent(crew.Id, crew.Name, crewOutcome.SuggestedXp, crew.Xp, "mission"));
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
        if (reward.Money > 0) AddCredits(reward.Money, "job_reward");
        if (reward.Parts > 0) AddParts(reward.Parts, "job_reward");
        if (reward.Fuel > 0) AddFuel(reward.Fuel, "job_reward");
        if (reward.Ammo > 0) AddAmmo(reward.Ammo, "job_reward");
        
        SimLog.Log($"[Campaign] Reward: {reward}");
    }

    // ========================================================================
    // ENCOUNTER EFFECT APPLICATION (MG4)
    // ========================================================================

    /// <summary>
    /// Apply all pending effects from a completed encounter.
    /// </summary>
    /// <param name="instance">The completed encounter instance with pending effects.</param>
    /// <returns>Number of effects successfully applied.</returns>
    public int ApplyEncounterOutcome(EncounterInstance instance)
    {
        if (instance == null || instance.PendingEffects == null)
        {
            SimLog.Log("[Campaign] ApplyEncounterOutcome: No instance or effects");
            return 0;
        }

        int applied = 0;

        foreach (var effect in instance.PendingEffects)
        {
            if (ApplyEncounterEffect(effect, instance))
            {
                applied++;
            }
        }

        SimLog.Log($"[Campaign] Applied {applied}/{instance.PendingEffects.Count} encounter effects");

        EventBus?.Publish(new EncounterOutcomeAppliedEvent(
            EncounterId: instance.InstanceId,
            TemplateId: instance.Template?.Id,
            EffectsApplied: applied,
            EffectsTotal: instance.PendingEffects.Count
        ));

        // Clear active encounter reference if this was the active one
        if (ActiveEncounter == instance)
        {
            ActiveEncounter = null;
        }

        return applied;
    }

    /// <summary>
    /// Apply a single encounter effect to campaign state.
    /// </summary>
    /// <param name="effect">The effect to apply.</param>
    /// <param name="instance">The encounter instance (for context like crew selection).</param>
    /// <returns>True if effect was applied successfully.</returns>
    private bool ApplyEncounterEffect(EncounterEffect effect, EncounterInstance instance)
    {
        try
        {
            switch (effect.Type)
            {
                case EffectType.AddResource:
                    return ApplyResourceEffect(effect);

                case EffectType.CrewInjury:
                    return ApplyCrewInjuryEffect(effect, instance);

                case EffectType.CrewXp:
                    return ApplyCrewXpEffect(effect, instance);

                case EffectType.CrewTrait:
                    return ApplyCrewTraitEffect(effect, instance);

                case EffectType.AddCrew:
                    return ApplyAddCrewEffect(effect);

                case EffectType.ShipDamage:
                    return ApplyShipDamageEffect(effect);

                case EffectType.FactionRep:
                    return ApplyFactionRepEffect(effect);

                case EffectType.SetFlag:
                    return ApplySetFlagEffect(effect);

                case EffectType.TimeDelay:
                    return ApplyTimeDelayEffect(effect);

                case EffectType.AddCargo:
                    return ApplyAddCargoEffect(effect);

                case EffectType.RemoveCargo:
                    return ApplyRemoveCargoEffect(effect);

                case EffectType.GotoNode:
                case EffectType.EndEncounter:
                    // Flow effects are handled by EncounterRunner, not campaign
                    return true;

                case EffectType.TriggerTactical:
                    // EN3 - tactical trigger handled separately by GameState
                    SimLog.Log($"[Campaign] TriggerTactical effect deferred (EN3)");
                    return true;

                default:
                    SimLog.Log($"[Campaign] Unknown effect type: {effect.Type}");
                    return false;
            }
        }
        catch (Exception ex)
        {
            SimLog.Log($"[Campaign] Error applying effect {effect.Type}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Get the crew member to target for an encounter effect.
    /// Uses the crew who performed the last skill check, or random alive crew.
    /// </summary>
    private CrewMember GetTargetCrewForEffect(EncounterInstance instance)
    {
        // Check if a specific crew was involved in the last skill check
        if (instance?.ResolvedParameters != null &&
            instance.ResolvedParameters.TryGetValue(EncounterParams.LastCheckCrewId, out var crewIdStr) &&
            int.TryParse(crewIdStr, out var crewId))
        {
            var crew = GetCrewById(crewId);
            if (crew != null && !crew.IsDead)
            {
                return crew;
            }
        }

        // Fall back to random alive crew
        var aliveCrew = GetAliveCrew();
        if (aliveCrew.Count == 0) return null;

        int index = Rng?.Campaign?.NextInt(aliveCrew.Count) ?? 0;
        return aliveCrew[index];
    }

    /// <summary>
    /// Apply a resource add/remove effect.
    /// </summary>
    private bool ApplyResourceEffect(EncounterEffect effect)
    {
        string resourceType = effect.TargetId;
        int amount = effect.Amount;

        if (string.IsNullOrEmpty(resourceType))
        {
            SimLog.Log("[Campaign] Resource effect missing TargetId");
            return false;
        }

        if (amount > 0)
        {
            AddResource(resourceType, amount, "encounter");
            return true;
        }
        else if (amount < 0)
        {
            int absAmount = Math.Abs(amount);
            int available = GetResource(resourceType);
            if (available >= absAmount)
            {
                SpendResource(resourceType, absAmount, "encounter");
                return true;
            }
            else
            {
                // Not enough resource - apply what we can (drain to 0)
                if (available > 0)
                {
                    SpendResource(resourceType, available, "encounter_partial");
                }
                SimLog.Log($"[Campaign] Insufficient {resourceType} for encounter effect ({available}/{absAmount})");
                return true; // Partial success is still success
            }
        }

        return true; // amount == 0 is a no-op
    }

    /// <summary>
    /// Apply a crew injury effect.
    /// </summary>
    private bool ApplyCrewInjuryEffect(EncounterEffect effect, EncounterInstance instance)
    {
        var crew = GetTargetCrewForEffect(instance);
        if (crew == null)
        {
            SimLog.Log("[Campaign] No crew available for injury effect");
            return false;
        }

        string injuryType = effect.StringParam ?? InjuryTypes.Wounded;
        crew.AddInjury(injuryType);

        SimLog.Log($"[Campaign] {crew.Name} injured ({injuryType}) from encounter");
        EventBus?.Publish(new CrewInjuredEvent(crew.Id, crew.Name, injuryType));

        return true;
    }

    /// <summary>
    /// Apply a crew XP effect.
    /// </summary>
    private bool ApplyCrewXpEffect(EncounterEffect effect, EncounterInstance instance)
    {
        var crew = GetTargetCrewForEffect(instance);
        if (crew == null)
        {
            SimLog.Log("[Campaign] No crew available for XP effect");
            return false;
        }

        int xpAmount = effect.Amount;
        if (xpAmount <= 0) return true;

        int oldLevel = crew.Level;
        bool leveledUp = crew.AddXp(xpAmount);

        SimLog.Log($"[Campaign] {crew.Name} gained {xpAmount} XP from encounter");
        EventBus?.Publish(new CrewXpGainedEvent(crew.Id, crew.Name, xpAmount, crew.Xp, "encounter"));

        if (leveledUp)
        {
            SimLog.Log($"[Campaign] {crew.Name} leveled up to {crew.Level}!");
            EventBus?.Publish(new CrewLeveledUpEvent(crew.Id, crew.Name, oldLevel, crew.Level));
        }

        return true;
    }

    /// <summary>
    /// Apply a crew trait add/remove effect.
    /// </summary>
    private bool ApplyCrewTraitEffect(EncounterEffect effect, EncounterInstance instance)
    {
        var crew = GetTargetCrewForEffect(instance);
        if (crew == null)
        {
            SimLog.Log("[Campaign] No crew available for trait effect");
            return false;
        }

        string traitId = effect.TargetId;
        bool addTrait = effect.BoolParam;

        if (string.IsNullOrEmpty(traitId))
        {
            SimLog.Log("[Campaign] Trait effect missing TargetId");
            return false;
        }

        if (addTrait)
        {
            return AssignTrait(crew.Id, traitId);
        }
        else
        {
            return RemoveTrait(crew.Id, traitId);
        }
    }

    /// <summary>
    /// Apply an add crew effect (recruitment from encounter).
    /// </summary>
    private bool ApplyAddCrewEffect(EncounterEffect effect)
    {
        string name = effect.TargetId;
        string roleStr = effect.StringParam ?? "Soldier";

        if (string.IsNullOrEmpty(name))
        {
            SimLog.Log("[Campaign] AddCrew effect missing name (TargetId)");
            return false;
        }

        if (!Enum.TryParse<CrewRole>(roleStr, out var role))
        {
            role = CrewRole.Soldier;
        }

        var newCrew = AddCrew(name, role);

        SimLog.Log($"[Campaign] Recruited {newCrew.Name} ({role}) from encounter");
        EventBus?.Publish(new CrewRecruitedEvent(newCrew.Id, newCrew.Name, role));

        return true;
    }

    /// <summary>
    /// Apply a ship damage effect.
    /// </summary>
    private bool ApplyShipDamageEffect(EncounterEffect effect)
    {
        int damage = effect.Amount;
        if (damage <= 0) return true;

        DamageShip(damage, "encounter");
        return true;
    }

    /// <summary>
    /// Apply a faction reputation effect.
    /// </summary>
    private bool ApplyFactionRepEffect(EncounterEffect effect)
    {
        string factionId = effect.TargetId;
        int delta = effect.Amount;

        if (string.IsNullOrEmpty(factionId))
        {
            SimLog.Log("[Campaign] FactionRep effect missing TargetId");
            return false;
        }

        ModifyFactionRep(factionId, delta);
        return true;
    }

    /// <summary>
    /// Apply a set flag effect.
    /// </summary>
    private bool ApplySetFlagEffect(EncounterEffect effect)
    {
        string flagId = effect.TargetId;
        bool value = effect.BoolParam;

        if (string.IsNullOrEmpty(flagId))
        {
            SimLog.Log("[Campaign] SetFlag effect missing TargetId");
            return false;
        }

        SetFlag(flagId, value);
        return true;
    }

    /// <summary>
    /// Apply a time delay effect.
    /// </summary>
    private bool ApplyTimeDelayEffect(EncounterEffect effect)
    {
        int days = effect.Amount;
        if (days <= 0) return true;

        Time.AdvanceDays(days);
        SimLog.Log($"[Campaign] Time advanced {days} day(s) from encounter");
        return true;
    }

    /// <summary>
    /// Apply an add cargo effect.
    /// </summary>
    private bool ApplyAddCargoEffect(EncounterEffect effect)
    {
        string itemDefId = effect.TargetId;
        int quantity = effect.Amount > 0 ? effect.Amount : 1;

        if (string.IsNullOrEmpty(itemDefId))
        {
            SimLog.Log("[Campaign] AddCargo effect missing TargetId");
            return false;
        }

        var item = AddItem(itemDefId, quantity);
        if (item == null)
        {
            SimLog.Log($"[Campaign] Could not add cargo {itemDefId} (no space?)");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Apply a remove cargo effect.
    /// </summary>
    private bool ApplyRemoveCargoEffect(EncounterEffect effect)
    {
        string itemDefId = effect.TargetId;
        int quantity = effect.Amount > 0 ? effect.Amount : 1;

        if (string.IsNullOrEmpty(itemDefId))
        {
            SimLog.Log("[Campaign] RemoveCargo effect missing TargetId");
            return false;
        }

        if (!HasItem(itemDefId, quantity))
        {
            SimLog.Log($"[Campaign] Cannot remove {quantity}x {itemDefId} (not enough)");
            return false;
        }

        return RemoveItemByDef(itemDefId, quantity);
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
            Inventory = Inventory?.GetState(),
            Flags = new Dictionary<string, bool>(Flags)
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

        // Serialize active encounter (MG4)
        data.ActiveEncounter = ActiveEncounter?.GetState();

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

        // Restore flags (MG4)
        campaign.Flags = new Dictionary<string, bool>(data.Flags ?? new Dictionary<string, bool>());

        // Restore stats
        if (data.Stats != null)
        {
            campaign.MissionsCompleted = data.Stats.MissionsCompleted;
            campaign.MissionsFailed = data.Stats.MissionsFailed;
            campaign.TotalMoneyEarned = data.Stats.TotalMoneyEarned;
            campaign.TotalCrewDeaths = data.Stats.TotalCrewDeaths;
        }

        // Initialize encounter system (not serialized, recreated on load)
        campaign.InitializeEncounterSystem();

        // Restore active encounter (MG4)
        if (data.ActiveEncounter != null && campaign.EncounterRegistry != null)
        {
            var template = campaign.EncounterRegistry.Get(data.ActiveEncounter.TemplateId);
            if (template != null)
            {
                campaign.ActiveEncounter = EncounterInstance.FromState(data.ActiveEncounter, template);
            }
        }

        return campaign;
    }
}
