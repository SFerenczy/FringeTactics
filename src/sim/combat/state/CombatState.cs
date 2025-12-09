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
    private AttackSystem attackSystem;
    
    // Mission phase tracking
    public MissionPhase Phase { get; private set; } = MissionPhase.Active;
    
    // Retreat state (M7)
    public bool IsRetreating { get; private set; } = false;
    
    // Mission outcome for detailed result tracking (M7)
    public MissionOutcome? FinalOutcome { get; private set; } = null;

    // Seeded RNG for deterministic simulation
    public RngStream Rng { get; private set; }

    // Combat statistics
    public CombatStats Stats { get; private set; } = new();

    // Ability system
    public AbilitySystem AbilitySystem { get; private set; }

    // Visibility system (fog of war)
    public VisibilitySystem Visibility { get; private set; }

    // Interaction system (doors, terminals, hazards)
    public InteractionSystem Interactions { get; private set; }

    // Perception system (enemy detection, alarm state)
    public PerceptionSystem Perception { get; private set; }
    
    // Overwatch system (reaction fire)
    public OverwatchSystem OverwatchSystem { get; private set; }
    
    // Suppression system (suppressive fire)
    public SuppressionSystem Suppression { get; private set; }
    
    // Objective evaluator (replaces hardcoded CheckMissionEnd logic)
    public ObjectiveEvaluator ObjectiveEvaluator { get; private set; }
    
    /// <summary>
    /// Event bus for cross-domain communication (optional, set by GameState).
    /// </summary>
    public EventBus EventBus { get; set; }

    // C# Events
    public event Action<Actor> ActorAdded;
    public event Action<Actor> ActorRemoved;
    public event Action<Actor, Actor, AttackResult> AttackResolved; // attacker, target, result
    public event Action<Actor> ActorDied;
    public event Action<bool> MissionEnded; // true = victory, false = defeat
    public event Action<MissionPhase> PhaseChanged;
    public event Action RetreatInitiated;
    public event Action RetreatCancelled;
    public event Action<MissionOutcome> MissionCompleted; // M7: detailed outcome

    public CombatState() : this(System.Environment.TickCount)
    {
    }

    public CombatState(int seed)
    {
        Rng = new RngStream(RngService.TacticalStream, seed);
        MissionConfig = null;
        Actors = new List<Actor>();
        MapState = new MapState();
        TimeSystem = new TimeSystem();
        Objectives = new Dictionary<string, object>();
        aiController = new AIController(this);
        AbilitySystem = new AbilitySystem(this);
        Visibility = new VisibilitySystem(MapState);
        Interactions = new InteractionSystem(this);
        Perception = new PerceptionSystem(this);
        OverwatchSystem = new OverwatchSystem(this);
        Suppression = new SuppressionSystem(this);
        ObjectiveEvaluator = new ObjectiveEvaluator(); // Empty by default, MissionFactory sets up objectives
        
        attackSystem = new AttackSystem(GetActorById);
        attackSystem.AttackResolved += OnAttackResolved;
        attackSystem.ActorDied += OnActorDied;

        SimLog.Log($"[CombatState] Initialized with seed {seed}");
    }

    /// <summary>
    /// Initialize or reinitialize the visibility system after MapState is set.
    /// Called by MissionFactory after building the map.
    /// </summary>
    public void InitializeVisibility()
    {
        Visibility = new VisibilitySystem(MapState);
        MapState.SetInteractionSystem(Interactions);
        SimLog.Log("[CombatState] Visibility and interaction systems initialized");
    }

    /// <summary>
    /// Initialize the perception system after actors are spawned.
    /// Called by MissionFactory after spawning enemies.
    /// </summary>
    public void InitializePerception()
    {
        Perception.Initialize();
        SimLog.Log("[CombatState] Perception system initialized");
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
        var currentTick = TimeSystem.CurrentTick;

        // Update actor modifiers (remove expired effects)
        foreach (var actor in Actors)
        {
            actor.UpdateModifiers(currentTick);
        }

        // Process enemy perception (before AI so AI can use detection state)
        Perception.Tick();

        // Run AI decisions
        aiController.Tick();

        // Process abilities (delayed effects like grenades)
        AbilitySystem.Tick();

        // Process interactions
        Interactions.Tick();

        // Process attacks (delegated to AttackSystem)
        attackSystem.ProcessTick(Actors, MapState, Rng, Stats);

        // Resolve movement collisions before actors move
        MovementSystem.ResolveCollisions(Actors, MapState);

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

    private void OnAttackResolved(Actor attacker, Actor target, AttackResult result)
    {
        AttackResolved?.Invoke(attacker, target, result);
    }

    private void OnActorDied(Actor victim, Actor killer)
    {
        ActorDied?.Invoke(victim);
        
        EventBus?.Publish(new ActorDiedEvent(
            ActorId: victim.Id,
            ActorType: victim.Type,
            ActorName: victim.Name ?? $"{victim.Type}#{victim.Id}",
            KillerId: killer?.Id ?? 0,
            Position: victim.GridPosition
        ));
    }

    private void CheckMissionEnd()
    {
        if (IsComplete || Phase == MissionPhase.Complete)
        {
            return;
        }
        
        // Check retreat completion first
        if (IsRetreating && AreAllCrewInEntryZone())
        {
            EndMission(MissionOutcome.Retreat);
            SimLog.Log("[Combat] RETREAT COMPLETE! All crew extracted.");
            return;
        }

        // Evaluate objectives
        var outcome = ObjectiveEvaluator.Evaluate(this);
        if (outcome.HasValue)
        {
            EndMission(outcome.Value);
            SimLog.Log($"[Combat] Mission ended: {outcome.Value}");
        }
    }
    
    /// <summary>
    /// End the mission with the given outcome.
    /// </summary>
    private void EndMission(MissionOutcome outcome)
    {
        FinalOutcome = outcome;
        Victory = (outcome == MissionOutcome.Victory);
        Phase = MissionPhase.Complete;
        TimeSystem.Pause();
        PhaseChanged?.Invoke(Phase);
        MissionEnded?.Invoke(Victory); // Legacy event for backward compatibility
        MissionCompleted?.Invoke(outcome);
        
        var stats = MissionOutputBuilder.CalculateStats(Actors);
        EventBus?.Publish(new MissionCompletedEvent(
            Outcome: outcome,
            EnemiesKilled: stats.EnemiesKilled,
            CrewDeaths: stats.CrewDeaths,
            CrewInjured: stats.CrewInjured,
            DurationSeconds: TimeSystem.GetCurrentTime()
        ));
    }
    
    /// <summary>
    /// Initiate retreat. Crew must reach entry zone to complete extraction.
    /// </summary>
    public void InitiateRetreat()
    {
        if (IsRetreating || IsComplete)
        {
            return;
        }
        
        IsRetreating = true;
        RetreatInitiated?.Invoke();
        SimLog.Log("[CombatState] Retreat initiated! Get all crew to the entry zone.");
    }
    
    /// <summary>
    /// Cancel retreat (if player changes mind before extraction).
    /// </summary>
    public void CancelRetreat()
    {
        if (!IsRetreating || IsComplete)
        {
            return;
        }
        
        IsRetreating = false;
        RetreatCancelled?.Invoke();
        SimLog.Log("[CombatState] Retreat cancelled.");
    }
    
    /// <summary>
    /// Check if all surviving crew are in the entry zone.
    /// </summary>
    public bool AreAllCrewInEntryZone()
    {
        var hasAliveCrew = false;
        
        foreach (var actor in Actors)
        {
            if (actor.Type != ActorType.Crew || actor.State != ActorState.Alive)
            {
                continue;
            }
            
            hasAliveCrew = true;
            
            if (!MapState.IsInEntryZone(actor.GridPosition))
            {
                return false;
            }
        }
        
        return hasAliveCrew;
    }
    
    /// <summary>
    /// Get count of crew in entry zone vs total alive.
    /// </summary>
    public (int inZone, int total) GetCrewExtractionStatus()
    {
        int inZone = 0;
        int total = 0;
        
        foreach (var actor in Actors)
        {
            if (actor.Type != ActorType.Crew || actor.State != ActorState.Alive)
            {
                continue;
            }
            
            total++;
            if (MapState.IsInEntryZone(actor.GridPosition))
            {
                inZone++;
            }
        }
        
        return (inZone, total);
    }
    
    public Actor AddActor(ActorType actorType, Vector2I position)
    {
        var actor = new Actor(nextActorId, actorType);
        actor.GridPosition = position;
        actor.SetTarget(position);
        actor.Map = MapState; // Set map reference for wall collision checking
        actor.MovingToPosition += OnActorMovingToPosition;
        nextActorId += 1;
        Actors.Add(actor);
        ActorAdded?.Invoke(actor);
        return actor;
    }
    
    private void OnActorMovingToPosition(Actor actor, Vector2I newPosition)
    {
        OverwatchSystem.CheckMovement(actor, newPosition);
    }

    public void RemoveActor(int actorId)
    {
        for (int i = 0; i < Actors.Count; i++)
        {
            if (Actors[i].Id == actorId)
            {
                var actor = Actors[i];
                actor.MovingToPosition -= OnActorMovingToPosition;
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
    /// Order an actor to interact with an interactable.
    /// </summary>
    public bool IssueInteractionOrder(int actorId, int interactableId, string action = null)
    {
        var actor = GetActorById(actorId);
        var interactable = Interactions.GetInteractable(interactableId);
        
        if (actor == null || interactable == null)
        {
            return false;
        }
        
        // If no action specified, use first available
        if (string.IsNullOrEmpty(action))
        {
            var available = Interactions.GetAvailableInteractions(actor, interactable);
            if (available.Count == 0)
            {
                return false;
            }
            action = available[0];
        }
        
        return Interactions.ExecuteInteraction(actor, interactable, action);
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
    /// Order an actor to enter overwatch.
    /// </summary>
    public void IssueOverwatchOrder(int actorId, Vector2I? facingDirection = null)
    {
        var actor = GetActorById(actorId);
        if (actor == null || actor.State != ActorState.Alive)
        {
            return;
        }
        
        if (!actor.CanFire())
        {
            SimLog.Log($"[Combat] {actor.Type}#{actor.Id} cannot enter overwatch (can't fire)");
            return;
        }
        
        actor.EnterOverwatch(TimeSystem.CurrentTick, facingDirection);
    }
    
    /// <summary>
    /// Cancel overwatch for an actor.
    /// </summary>
    public void CancelOverwatch(int actorId)
    {
        var actor = GetActorById(actorId);
        actor?.ExitOverwatch();
    }
    
    /// <summary>
    /// Order an actor to use suppressive fire on a target.
    /// </summary>
    public SuppressionResult IssueSuppressiveFireOrder(int actorId, int targetId)
    {
        var actor = GetActorById(actorId);
        var target = GetActorById(targetId);
        
        if (actor == null || target == null)
        {
            return new SuppressionResult { Success = false };
        }
        
        if (!Suppression.CanSuppressiveFire(actor))
        {
            SimLog.Log($"[Combat] {actor.Type}#{actor.Id} cannot use suppressive fire");
            return new SuppressionResult { Success = false };
        }
        
        return Suppression.ExecuteSuppressiveFire(actor, target);
    }
    
    /// <summary>
    /// Order an actor to use area suppression on a tile.
    /// </summary>
    public bool IssueAreaSuppressionOrder(int actorId, Godot.Vector2I targetTile, int radius = 2)
    {
        var actor = GetActorById(actorId);
        
        if (actor == null)
        {
            return false;
        }
        
        if (!Suppression.CanAreaSuppression(actor))
        {
            SimLog.Log($"[Combat] {actor.Type}#{actor.Id} cannot use area suppression");
            return false;
        }
        
        Suppression.ExecuteAreaSuppression(actor, targetTile, radius);
        return true;
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

    public List<Actor> GetActorsByType(ActorType type)
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
