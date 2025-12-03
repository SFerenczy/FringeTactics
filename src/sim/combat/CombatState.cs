using Godot; // For Vector2I only - no Node/UI types
using System;
using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// Mission phase for tracking tactical session state.
/// </summary>
public enum MissionPhase
{
    Setup,      // Before mission starts (future: loadout screen)
    Active,     // Mission in progress
    Complete    // Mission ended (victory or defeat)
}

public partial class CombatState
{
    public MissionConfig MissionConfig { get; set; } = null;
    public List<Actor> Actors { get; set; } = new();
    public MapState MapState { get; set; }
    public TimeSystem TimeSystem { get; set; }
    public Dictionary<string, object> Objectives { get; set; } = new();
    public bool IsComplete => Phase == MissionPhase.Complete;
    public bool Victory { get; set; } = false;
    private int nextActorId = 0;
    private AIController aiController;
    
    // Mission phase tracking
    public MissionPhase Phase { get; private set; } = MissionPhase.Active;
    
    // Track if mission was spawned with enemies (for win condition logic)
    private bool hasEnemyObjective = false;

    // Seeded RNG for deterministic simulation
    public CombatRng Rng { get; private set; }

    // Combat statistics
    public CombatStats Stats { get; private set; } = new();

    // Ability system
    public AbilitySystem AbilitySystem { get; private set; }

    // Visibility system (fog of war)
    public VisibilitySystem Visibility { get; private set; }

    // C# Events
    public event Action<Actor> ActorAdded;
    public event Action<Actor> ActorRemoved;
    public event Action<Actor, Actor, AttackResult> AttackResolved; // attacker, target, result
    public event Action<Actor> ActorDied;
    public event Action<bool> MissionEnded; // true = victory, false = defeat
    public event Action<MissionPhase> PhaseChanged;

    public CombatState() : this(System.Environment.TickCount)
    {
    }

    public CombatState(int seed)
    {
        Rng = new CombatRng(seed);
        MissionConfig = null;
        Actors = new List<Actor>();
        MapState = new MapState();
        TimeSystem = new TimeSystem();
        Objectives = new Dictionary<string, object>();
        aiController = new AIController(this);
        AbilitySystem = new AbilitySystem(this);
        Visibility = new VisibilitySystem(MapState);

        SimLog.Log($"[CombatState] Initialized with seed {seed}");
    }

    /// <summary>
    /// Initialize or reinitialize the visibility system after MapState is set.
    /// Called by MissionFactory after building the map.
    /// </summary>
    public void InitializeVisibility()
    {
        Visibility = new VisibilitySystem(MapState);
        SimLog.Log("[CombatState] Visibility system initialized");
    }

    public void Update(float dt)
    {
        // Main update loop - processes time and ticks actors.
        var ticksAdvanced = TimeSystem.Update(dt);

        for (int i = 0; i < ticksAdvanced; i++)
        {
            ProcessTick();
        }
    }

    private void ProcessTick()
    {
        // Process a single simulation tick.
        var tickDuration = TimeSystem.TickDuration;

        // Run AI decisions
        aiController.Tick();

        // Process abilities (delayed effects like grenades)
        AbilitySystem.Tick();

        // Process attacks first
        ProcessAttacks();

        // Resolve movement collisions before actors move
        ResolveMovementCollisions();

        // Then process actor movement/cooldowns
        foreach (var actor in Actors)
        {
            actor.Tick(tickDuration);
        }

        // Update visibility after movement
        Visibility.UpdateVisibility(Actors);

        // Check win/lose conditions
        CheckMissionEnd();
    }

    private void ResolveMovementCollisions()
    {
        // Build map of next-tile destinations for moving actors
        var destinations = new Dictionary<Vector2I, List<Actor>>();

        foreach (var actor in Actors)
        {
            if (actor.State != ActorState.Alive || !actor.IsMoving)
                continue;

            // Calculate the tile this actor is about to enter
            var moveDir = GridUtils.GetStepDirection(actor.GridPosition, actor.TargetPosition);
            var nextTile = actor.GridPosition + moveDir;

            if (!destinations.ContainsKey(nextTile))
                destinations[nextTile] = new List<Actor>();
            destinations[nextTile].Add(actor);
        }

        // For tiles with multiple actors heading there, pause all but the closest
        foreach (var kvp in destinations)
        {
            var tile = kvp.Key;
            var actors = kvp.Value;

            // Also check if an actor is already standing on that tile
            var occupant = GetActorAtPosition(tile);
            bool tileOccupied = occupant != null && occupant.State == ActorState.Alive && !occupant.IsMoving;

            if (actors.Count > 1 || tileOccupied)
            {
                // Sort by distance to target (closest gets priority)
                actors.Sort((a, b) =>
                {
                    var distA = (a.TargetPosition - a.GridPosition).LengthSquared();
                    var distB = (b.TargetPosition - b.GridPosition).LengthSquared();
                    return distA.CompareTo(distB);
                });

                // If tile is occupied by stationary unit, pause all movers
                int startIndex = tileOccupied ? 0 : 1;

                for (int i = startIndex; i < actors.Count; i++)
                {
                    actors[i].PauseMovement();
                }
            }
        }
    }

    private void CheckMissionEnd()
    {
        if (IsComplete || Phase == MissionPhase.Complete)
        {
            return;
        }

        var aliveCrewCount = 0;
        var aliveEnemyCount = 0;

        foreach (var actor in Actors)
        {
            if (actor.State != ActorState.Alive)
            {
                continue;
            }

            if (actor.Type == ActorTypes.Crew)
            {
                aliveCrewCount++;
            }
            else if (actor.Type == ActorTypes.Enemy)
            {
                aliveEnemyCount++;
            }
        }

        // Only check victory if mission has enemy objective
        // (prevents auto-win in M0 sandbox with no enemies)
        if (hasEnemyObjective && aliveEnemyCount == 0)
        {
            // Victory - all enemies dead
            EndMission(victory: true);
            SimLog.Log("[Combat] VICTORY! All enemies eliminated.");
        }
        else if (aliveCrewCount == 0)
        {
            // Defeat - all crew dead
            EndMission(victory: false);
            SimLog.Log("[Combat] DEFEAT! All crew eliminated.");
        }
    }
    
    /// <summary>
    /// End the mission with the given result.
    /// </summary>
    private void EndMission(bool victory)
    {
        Victory = victory;
        Phase = MissionPhase.Complete;
        TimeSystem.Pause();
        PhaseChanged?.Invoke(Phase);
        MissionEnded?.Invoke(victory);
    }
    
    /// <summary>
    /// Mark that this mission has an enemy elimination objective.
    /// Called by MissionFactory when enemies are spawned.
    /// </summary>
    public void SetHasEnemyObjective(bool hasEnemies)
    {
        hasEnemyObjective = hasEnemies;
        SimLog.Log($"[CombatState] Enemy objective set: {hasEnemies}");
    }

    private void ProcessAttacks()
    {
        // Process manual attack orders
        foreach (var attacker in Actors)
        {
            if (attacker.State != ActorState.Alive)
            {
                continue;
            }

            if (!attacker.AttackTargetId.HasValue)
            {
                continue;
            }

            if (!attacker.CanFire())
            {
                // Auto-reload if out of ammo in magazine but have reserve
                if (attacker.NeedsReload())
                {
                    attacker.StartReload();
                    SimLog.Log($"[Combat] {attacker.Type}#{attacker.Id} auto-reloading (empty magazine)");
                }
                continue;
            }

            var target = GetActorById(attacker.AttackTargetId.Value);
            if (target == null || target.State != ActorState.Alive)
            {
                // Target gone or dead, clear order
                attacker.SetAttackTarget(null);
                continue;
            }

            // Try to attack
            if (CombatResolver.CanAttack(attacker, target, attacker.EquippedWeapon, MapState))
            {
                ExecuteAttack(attacker, target, isAutoDefend: false);
            }
        }

        // Process auto-defend (return fire)
        ProcessAutoDefend();
    }

    private void ProcessAutoDefend()
    {
        foreach (var defender in Actors)
        {
            if (defender.State != ActorState.Alive)
            {
                continue;
            }

            // Skip if has manual attack order (manual orders take priority)
            if (defender.AttackTargetId.HasValue)
            {
                continue;
            }

            // Skip if no auto-defend target
            if (!defender.AutoDefendTargetId.HasValue)
            {
                continue;
            }

            if (!defender.CanFire())
            {
                // Auto-reload during auto-defend too
                if (defender.NeedsReload())
                {
                    defender.StartReload();
                    SimLog.Log($"[Combat] {defender.Type}#{defender.Id} auto-reloading (auto-defend)");
                }
                continue;
            }

            var attacker = GetActorById(defender.AutoDefendTargetId.Value);
            if (attacker == null || attacker.State != ActorState.Alive)
            {
                // Attacker gone or dead, clear auto-defend
                defender.ClearAutoDefendTarget();
                continue;
            }

            // Try to return fire
            if (CombatResolver.CanAttack(defender, attacker, defender.EquippedWeapon, MapState))
            {
                ExecuteAttack(defender, attacker, isAutoDefend: true);
            }
        }
    }

    private void ExecuteAttack(Actor attacker, Actor target, bool isAutoDefend)
    {
        var result = CombatResolver.ResolveAttack(attacker, target, attacker.EquippedWeapon, MapState, Rng.GetRandom());
        attacker.StartCooldown();
        attacker.ConsumeAmmo();

        var attackType = isAutoDefend ? "auto-defend" : "attack";

        if (result.Hit)
        {
            target.TakeDamage(result.Damage);
            SimLog.Log($"[Combat] {attacker.Type}#{attacker.Id} hit {target.Type}#{target.Id} ({attackType}) for {result.Damage} damage ({result.HitChance:P0} chance). HP: {target.Hp}/{target.MaxHp}");

            // Set auto-defend target on the victim (they'll return fire)
            if (target.State == ActorState.Alive)
            {
                target.SetAutoDefendTarget(attacker.Id);
            }
            else
            {
                SimLog.Log($"[Combat] {target.Type}#{target.Id} DIED!");
                ActorDied?.Invoke(target);
            }
        }
        else
        {
            SimLog.Log($"[Combat] {attacker.Type}#{attacker.Id} missed {target.Type}#{target.Id} ({attackType}) ({result.HitChance:P0} chance)");
            
            // Even a miss triggers auto-defend (they know they're being shot at)
            target.SetAutoDefendTarget(attacker.Id);
        }

        AttackResolved?.Invoke(attacker, target, result);

        // Track stats
        if (attacker.Type == ActorTypes.Crew)
        {
            Stats.PlayerShotsFired++;
            if (result.Hit)
            {
                Stats.PlayerHits++;
            }
            else
            {
                Stats.PlayerMisses++;
            }
        }
        else
        {
            Stats.EnemyShotsFired++;
            if (result.Hit)
            {
                Stats.EnemyHits++;
            }
            else
            {
                Stats.EnemyMisses++;
            }
        }
    }

    public Actor AddActor(string actorType, Vector2I position)
    {
        // Create and add a new actor to the combat.
        var actor = new Actor(nextActorId, actorType);
        actor.GridPosition = position;
        actor.SetTarget(position);
        actor.Map = MapState; // Set map reference for wall collision checking
        nextActorId += 1;
        Actors.Add(actor);
        ActorAdded?.Invoke(actor);
        return actor;
    }

    public void RemoveActor(int actorId)
    {
        for (int i = 0; i < Actors.Count; i++)
        {
            if (Actors[i].Id == actorId)
            {
                var actor = Actors[i];
                Actors.RemoveAt(i);
                ActorRemoved?.Invoke(actor);
                return;
            }
        }
    }

    public Actor GetActorById(int actorId)
    {
        foreach (var actor in Actors)
        {
            if (actor.Id == actorId)
            {
                return actor;
            }
        }
        return null;
    }

    public Actor GetActorAtPosition(Vector2I pos)
    {
        foreach (var actor in Actors)
        {
            if (actor.GridPosition == pos)
            {
                return actor;
            }
        }
        return null;
    }

    public void IssueMovementOrder(int actorId, Vector2I targetPos)
    {
        // Order an actor to move to a target position.
        var actor = GetActorById(actorId);
        if (actor == null || actor.State != ActorState.Alive)
        {
            return;
        }

        SimLog.Log($"IssueMovementOrder: actorId={actorId}, targetPos={targetPos}, walkable={MapState.IsWalkable(targetPos)}");
        if (MapState.IsWalkable(targetPos))
        {
            actor.SetAttackTarget(null); // Clear attack order when moving
            actor.SetTarget(targetPos);
            SimLog.Log($"Actor {actorId} target set to {targetPos}, IsMoving={actor.IsMoving}");
        }
    }

    public void IssueAttackOrder(int actorId, int targetId)
    {
        // Order an actor to attack a target actor.
        var actor = GetActorById(actorId);
        var target = GetActorById(targetId);

        if (actor == null || actor.State != ActorState.Alive)
        {
            return;
        }

        if (target == null || target.State != ActorState.Alive)
        {
            return;
        }

        // Don't attack self
        if (actorId == targetId)
        {
            return;
        }

        SimLog.Log($"IssueAttackOrder: {actor.Type}#{actorId} -> {target.Type}#{targetId}");
        actor.SetAttackTarget(targetId);
    }

    public bool IssueAbilityOrder(int actorId, AbilityData ability, Vector2I targetTile)
    {
        return AbilitySystem.UseAbility(actorId, ability, targetTile);
    }

    /// <summary>
    /// Order an actor to reload their weapon.
    /// </summary>
    public void IssueReloadOrder(int actorId)
    {
        var actor = GetActorById(actorId);
        if (actor == null || actor.State != ActorState.Alive)
        {
            return;
        }

        if (actor.IsReloading)
        {
            return; // Already reloading
        }

        if (actor.CurrentMagazine == actor.EquippedWeapon.MagazineSize)
        {
            return; // Magazine full
        }

        if (actor.ReserveAmmo == 0)
        {
            return; // No reserve ammo
        }

        SimLog.Log($"[Combat] {actor.Type}#{actor.Id} manually reloading ({actor.CurrentMagazine}/{actor.EquippedWeapon.MagazineSize})");
        actor.StartReload();
    }

    /// <summary>
    /// Called by AbilitySystem when an ability kills an actor.
    /// </summary>
    public void NotifyActorDied(Actor actor)
    {
        ActorDied?.Invoke(actor);
    }

    public List<Actor> GetAliveActors()
    {
        var alive = new List<Actor>();
        foreach (var actor in Actors)
        {
            if (actor.State == ActorState.Alive)
            {
                alive.Add(actor);
            }
        }
        return alive;
    }

    public List<Actor> GetActorsByType(string type)
    {
        var result = new List<Actor>();
        foreach (var actor in Actors)
        {
            if (actor.Type == type)
            {
                result.Add(actor);
            }
        }
        return result;
    }

    public Dictionary<string, object> GetSnapshot()
    {
        return new Dictionary<string, object>
        {
            { "tick", TimeSystem.CurrentTick },
            { "isPaused", TimeSystem.IsPaused },
            { "actorCount", Actors.Count }
        };
    }
}
