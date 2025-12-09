using Godot; // For Vector2, Vector2I, Mathf only - no Node/UI types
using System;
using System.Collections.Generic;

namespace FringeTactics;

public enum ActorState
{
    Alive,
    Down,  // Incapacitated but not dead (for future use)
    Dead
}

public partial class Actor
{
    private const float BaseMoveSpeed = 4.0f;
    private const float BaseAccuracy = 0.7f;
    private const int BaseVisionRadius = 8;

    public int Id { get; set; }
    public ActorType Type { get; set; }
    public string Name { get; set; }
    public int CrewId { get; set; } = -1;
    public Vector2I GridPosition { get; set; } = Vector2I.Zero;
    public Vector2 VisualPosition { get; set; } = Vector2.Zero; // smooth position for rendering
    public int Hp { get; set; } = 100;
    public int MaxHp { get; set; } = 100;
    public int Armor { get; set; } = 0;
    public ActorState State { get; set; } = ActorState.Alive;
    public Dictionary<string, int> Stats { get; set; } = new() { { "aim", 0 }, { "toughness", 0 }, { "reflexes", 0 } };
    public List<string> Abilities { get; set; } = new();
    
    // Stat modifier system
    public ModifierCollection Modifiers { get; } = new();
    
    // Effect system
    public ActorEffects Effects { get; private set; }
    
    // Overwatch state
    public OverwatchState Overwatch { get; } = new();

    // Weapon and attack
    public WeaponData EquippedWeapon { get; set; } = WeaponData.DefaultRifle;
    public int AttackCooldown { get; set; } = 0; // ticks until can fire again
    public int? AttackTargetId { get; set; } = null; // current attack order target
    public int? AutoDefendTargetId { get; set; } = null; // auto-retaliate target (set when attacked)
    public bool AutoDefendEnabled { get; set; } = true; // can be toggled per-unit

    // Ammunition
    public int CurrentMagazine { get; set; } = 30;
    public int ReserveAmmo { get; set; } = 90; // 3 extra magazines worth
    public bool IsReloading { get; private set; } = false;
    public int ReloadProgress { get; private set; } = 0; // ticks remaining
    
    // Event for reload completion
    public event Action<Actor> ReloadCompleted;
    
    // Combat statistics (M7)
    public int Kills { get; private set; } = 0;
    public int ShotsFired { get; private set; } = 0;
    public int ShotsHit { get; private set; } = 0;
    public int TotalDamageDealt { get; private set; } = 0;
    public int TotalDamageTaken { get; private set; } = 0;
    public int AmmoUsed { get; private set; } = 0;

    // Channeled action state
    public bool IsChanneling { get; private set; } = false;
    public ChanneledAction CurrentChannel { get; private set; } = null;
    
    // Events for channeled actions
    public event Action<Actor, ChanneledAction> ChannelStarted;
    public event Action<Actor, ChanneledAction> ChannelCompleted;
    public event Action<Actor, ChanneledAction> ChannelInterrupted;

    // Movement
    public Vector2I TargetPosition { get; private set; } = Vector2I.Zero;
    public bool IsMoving { get; private set; } = false;
    public float MoveProgress { get; private set; } = 0.0f; // 0 to 1 between tiles
    public Vector2I MoveDirection { get; private set; } = Vector2I.Zero;

    // Reference to map for pathfinding (set by CombatState)
    public MapState Map { get; set; } = null;

    // C# Events
    public event Action<Actor> ModifiersChanged;
    public event Action<Actor, Vector2I> PositionChanged;
    public event Action<Actor, Vector2I> MovingToPosition;
    public event Action<Actor> ArrivedAtTarget;
    public event Action<Actor, int> DamageTaken; // actor, damage amount
    public event Action<Actor> Died;
    public event Action<Actor> OverwatchActivated;
    public event Action<Actor> OverwatchDeactivated;
    public event Action<Actor, Actor> OverwatchTriggered;

    public Actor(int actorId, ActorType actorType)
    {
        Id = actorId;
        Type = actorType;
        GridPosition = Vector2I.Zero;
        VisualPosition = Vector2.Zero;
        TargetPosition = Vector2I.Zero;
        Hp = 100;
        MaxHp = 100;
        State = ActorState.Alive;
        Stats = new Dictionary<string, int> { { "aim", 0 }, { "toughness", 0 }, { "reflexes", 0 } };
        Abilities = new List<string>();
        Modifiers.ModifiersChanged += () => ModifiersChanged?.Invoke(this);
        Effects = new ActorEffects(this);
        Overwatch.StateChanged += OnOverwatchStateChanged;
        EquippedWeapon = WeaponData.DefaultRifle;
        CurrentMagazine = EquippedWeapon.MagazineSize;
        ReserveAmmo = EquippedWeapon.MagazineSize * 3;
    }

    public void SetTarget(Vector2I target)
    {
        // Interrupt channeling when given movement order
        if (IsChanneling)
        {
            CancelChannel();
        }
        
        // Cancel overwatch when given movement order
        ExitOverwatch();
        
        TargetPosition = target;
        if (TargetPosition != GridPosition)
        {
            IsMoving = true;
            MoveProgress = 0f;
        }
    }

    public void Tick(float tickDuration)
    {
        // Dead actors don't update
        if (State == ActorState.Dead)
        {
            return;
        }

        // Tick down attack cooldown
        if (AttackCooldown > 0)
        {
            AttackCooldown--;
        }

        // Handle reload progress
        if (IsReloading)
        {
            ReloadProgress--;
            if (ReloadProgress <= 0)
            {
                CompleteReload();
            }
            return; // Can't move while reloading
        }
        
        // Handle channeled action progress
        if (IsChanneling && CurrentChannel != null)
        {
            CurrentChannel.Tick();
            if (CurrentChannel.IsComplete)
            {
                CompleteChannel();
            }
            return; // Can't move while channeling
        }

        // Handle movement
        if (!IsMoving)
        {
            return;
        }

        if (GridPosition == TargetPosition)
        {
            IsMoving = false;
            ArrivedAtTarget?.Invoke(this);
            return;
        }

        // Calculate direction to target
        MoveDirection = GridUtils.GetStepDirection(GridPosition, TargetPosition);

        // Check if next tile is walkable
        var nextTile = GridPosition + MoveDirection;
        if (Map != null && !Map.IsWalkable(nextTile))
        {
            // Try cardinal directions if diagonal is blocked
            if (MoveDirection.X != 0 && MoveDirection.Y != 0)
            {
                // Try horizontal first
                var horizontalTile = GridPosition + new Vector2I(MoveDirection.X, 0);
                var verticalTile = GridPosition + new Vector2I(0, MoveDirection.Y);

                if (Map.IsWalkable(horizontalTile))
                {
                    MoveDirection = new Vector2I(MoveDirection.X, 0);
                    nextTile = horizontalTile;
                }
                else if (Map.IsWalkable(verticalTile))
                {
                    MoveDirection = new Vector2I(0, MoveDirection.Y);
                    nextTile = verticalTile;
                }
                else
                {
                    // Completely blocked - stop movement
                    IsMoving = false;
                    TargetPosition = GridPosition;
                    return;
                }
            }
            else
            {
                // Cardinal direction blocked - stop movement
                IsMoving = false;
                TargetPosition = GridPosition;
                return;
            }
        }

        // Progress movement
        MoveProgress += GetMoveSpeed() * tickDuration;

        if (MoveProgress >= 1.0f)
        {
            CommitTileMovement();
        }
    }

    public void SetAttackTarget(int? targetId)
    {
        AttackTargetId = targetId;
        // Clear movement when attacking
        if (targetId.HasValue)
        {
            // Cancel overwatch when given attack order
            ExitOverwatch();
            IsMoving = false;
            TargetPosition = GridPosition;
        }
    }

    public void ClearOrders()
    {
        AttackTargetId = null;
        AutoDefendTargetId = null;
        IsMoving = false;
        TargetPosition = GridPosition;
    }

    /// <summary>
    /// Set the auto-defend target (called when this actor is attacked).
    /// </summary>
    public void SetAutoDefendTarget(int attackerId)
    {
        if (AutoDefendEnabled && State == ActorState.Alive)
        {
            AutoDefendTargetId = attackerId;
        }
    }

    /// <summary>
    /// Clear auto-defend target (e.g., when target dies or manual order given).
    /// </summary>
    public void ClearAutoDefendTarget()
    {
        AutoDefendTargetId = null;
    }

    /// <summary>
    /// Temporarily pause movement for one tick (collision avoidance).
    /// The actor will resume moving next tick if path is clear.
    /// </summary>
    public void PauseMovement()
    {
        MoveProgress = 0f;
    }

    public void TakeDamage(int damage)
    {
        if (State != ActorState.Alive)
        {
            return;
        }

        Hp -= damage;
        TotalDamageTaken += damage; // M7 statistics
        DamageTaken?.Invoke(this, damage);
        
        // Interrupt channeling when taking damage
        if (IsChanneling && CurrentChannel != null && CurrentChannel.CanBeInterrupted)
        {
            CancelChannel();
        }

        if (Hp <= 0)
        {
            Hp = 0;
            State = ActorState.Dead;
            ClearOrders();
            CancelChannel();
            Died?.Invoke(this);
        }
    }

    public bool CanFire()
    {
        return State == ActorState.Alive 
            && AttackCooldown <= 0 
            && CurrentMagazine > 0 
            && !IsReloading
            && !IsChanneling;
    }

    /// <summary>
    /// Consume one round of ammo. Called when firing.
    /// </summary>
    public void ConsumeAmmo()
    {
        if (CurrentMagazine > 0)
        {
            CurrentMagazine--;
            AmmoUsed++;
        }
    }
    
    /// <summary>
    /// Record a shot fired by this actor (M7 statistics).
    /// </summary>
    public void RecordShot(bool hit, int damage = 0)
    {
        ShotsFired++;
        if (hit)
        {
            ShotsHit++;
            TotalDamageDealt += damage;
        }
    }
    
    /// <summary>
    /// Record a kill by this actor (M7 statistics).
    /// </summary>
    public void RecordKill()
    {
        Kills++;
    }

    /// <summary>
    /// Check if magazine is empty and reserve ammo available.
    /// </summary>
    public bool NeedsReload()
    {
        return CurrentMagazine == 0 && ReserveAmmo > 0;
    }

    /// <summary>
    /// Check if completely out of ammo.
    /// </summary>
    public bool IsOutOfAmmo()
    {
        return CurrentMagazine == 0 && ReserveAmmo == 0;
    }

    /// <summary>
    /// Start the reload process.
    /// </summary>
    public void StartReload()
    {
        if (IsReloading || CurrentMagazine == EquippedWeapon.MagazineSize || ReserveAmmo == 0)
        {
            return;
        }
        
        IsReloading = true;
        ReloadProgress = EquippedWeapon.ReloadTicks;
        
        // Clear attack target during reload
        SetAttackTarget(null);
    }

    /// <summary>
    /// Cancel reload (e.g., when receiving movement order).
    /// </summary>
    public void CancelReload()
    {
        IsReloading = false;
        ReloadProgress = 0;
    }

    /// <summary>
    /// Complete the reload, filling magazine from reserve.
    /// </summary>
    private void CompleteReload()
    {
        var ammoNeeded = EquippedWeapon.MagazineSize - CurrentMagazine;
        var ammoToLoad = Math.Min(ammoNeeded, ReserveAmmo);
        
        CurrentMagazine += ammoToLoad;
        ReserveAmmo -= ammoToLoad;
        
        IsReloading = false;
        ReloadProgress = 0;
        
        ReloadCompleted?.Invoke(this);
    }

    public void StartCooldown()
    {
        AttackCooldown = EquippedWeapon.CooldownTicks;
    }

    // === Stat Accessors (use modifiers) ===

    /// <summary>
    /// Get effective move speed after modifiers.
    /// </summary>
    public float GetMoveSpeed()
    {
        return Modifiers.Calculate(StatType.MoveSpeed, BaseMoveSpeed);
    }

    /// <summary>
    /// Get effective accuracy after modifiers.
    /// </summary>
    public float GetAccuracy()
    {
        var aimBonus = Stats.TryGetValue("aim", out var aim) ? aim * 0.01f : 0f;
        return Modifiers.Calculate(StatType.Accuracy, BaseAccuracy + aimBonus);
    }

    /// <summary>
    /// Get effective vision radius after modifiers.
    /// </summary>
    public int GetVisionRadius()
    {
        return (int)Modifiers.Calculate(StatType.VisionRadius, BaseVisionRadius);
    }

    /// <summary>
    /// Check if actor is stunned (cannot act).
    /// </summary>
    public bool IsStunned()
    {
        return Effects.Has(StunEffect.EffectId) || Modifiers.HasModifier(StunEffect.EffectId);
    }

    /// <summary>
    /// Check if actor is suppressed.
    /// </summary>
    public bool IsSuppressed()
    {
        return Effects.Has(SuppressedEffect.EffectId) || Modifiers.HasModifier(SuppressedEffect.EffectId);
    }

    /// <summary>
    /// Remove expired modifiers and tick effects. Call each tick.
    /// </summary>
    public void UpdateModifiers(int currentTick)
    {
        Modifiers.RemoveExpired(currentTick);
        Effects.Tick();
    }

    // === Movement Methods (for MovementSystem) ===

    /// <summary>
    /// Set the current move direction.
    /// </summary>
    public void SetMoveDirection(Vector2I direction)
    {
        MoveDirection = direction;
    }

    /// <summary>
    /// Advance movement progress by the given amount.
    /// </summary>
    public void AdvanceMovement(float tickDuration)
    {
        MoveProgress += GetMoveSpeed() * tickDuration;

        if (MoveProgress >= 1.0f)
        {
            CommitTileMovement();
        }
    }
    
    /// <summary>
    /// Commit arrival at the next tile. Fires overwatch check, updates position, and handles arrival.
    /// </summary>
    private void CommitTileMovement()
    {
        MoveProgress = 0.0f;
        var newPosition = GridPosition + MoveDirection;
        
        // Fire event before position change (for overwatch checks)
        MovingToPosition?.Invoke(this, newPosition);
        
        // Check if still alive after potential overwatch reaction
        if (State != ActorState.Alive)
        {
            IsMoving = false;
            return;
        }
        
        GridPosition = newPosition;
        PositionChanged?.Invoke(this, GridPosition);

        if (GridPosition == TargetPosition)
        {
            CompleteMovement();
        }
    }

    /// <summary>
    /// Complete movement and fire arrival event.
    /// </summary>
    public void CompleteMovement()
    {
        IsMoving = false;
        ArrivedAtTarget?.Invoke(this);
    }

    /// <summary>
    /// Stop movement entirely (blocked path).
    /// </summary>
    public void StopMovement()
    {
        IsMoving = false;
        TargetPosition = GridPosition;
    }

    public Vector2 GetVisualPosition(int tileSize)
    {
        var basePos = new Vector2(GridPosition.X * tileSize, GridPosition.Y * tileSize);
        if (IsMoving && MoveDirection != Vector2I.Zero)
        {
            var offset = new Vector2(MoveDirection.X, MoveDirection.Y) * tileSize * MoveProgress;
            return basePos + offset;
        }
        return basePos;
    }
    
    // === Channeled Action Methods ===
    
    /// <summary>
    /// Start a channeled action.
    /// </summary>
    public bool StartChannel(ChanneledAction channel)
    {
        if (State != ActorState.Alive)
        {
            return false;
        }
        
        // Cancel any existing actions
        CancelChannel();
        CancelReload();
        ClearOrders();
        
        IsChanneling = true;
        CurrentChannel = channel;
        ChannelStarted?.Invoke(this, channel);
        
        SimLog.Log($"[Actor] {Type}#{Id} started channeling {channel.ActionType} ({channel.TotalTicks} ticks)");
        return true;
    }
    
    /// <summary>
    /// Cancel the current channeled action.
    /// </summary>
    public void CancelChannel()
    {
        if (!IsChanneling || CurrentChannel == null)
        {
            return;
        }
        
        var channel = CurrentChannel;
        IsChanneling = false;
        CurrentChannel = null;
        ChannelInterrupted?.Invoke(this, channel);
        SimLog.Log($"[Actor] {Type}#{Id} channel interrupted");
    }
    
    /// <summary>
    /// Complete the current channeled action.
    /// </summary>
    private void CompleteChannel()
    {
        if (!IsChanneling || CurrentChannel == null)
        {
            return;
        }
        
        var channel = CurrentChannel;
        IsChanneling = false;
        CurrentChannel = null;
        ChannelCompleted?.Invoke(this, channel);
        SimLog.Log($"[Actor] {Type}#{Id} completed {channel.ActionType}");
    }
    
    // === Overwatch Methods ===
    
    private void OnOverwatchStateChanged(OverwatchState state)
    {
        if (state.IsActive)
        {
            OverwatchActivated?.Invoke(this);
        }
        else
        {
            OverwatchDeactivated?.Invoke(this);
        }
    }
    
    /// <summary>
    /// Enter overwatch state. Cancels other actions.
    /// Cannot enter overwatch while suppressed.
    /// </summary>
    public void EnterOverwatch(int currentTick, Vector2I? facingDirection = null)
    {
        if (State != ActorState.Alive || !CanFire()) return;
        
        if (IsSuppressed())
        {
            SimLog.Log($"[Actor] {Type}#{Id} cannot enter overwatch - suppressed!");
            return;
        }
        
        // Cancel other actions
        ClearOrders();
        CancelChannel();
        CancelReload();
        
        var range = EquippedWeapon.Range;
        Overwatch.Activate(currentTick, facingDirection, 90f, range, 1);
        SimLog.Log($"[Actor] {Type}#{Id} entered overwatch");
    }
    
    /// <summary>
    /// Exit overwatch state.
    /// </summary>
    public void ExitOverwatch()
    {
        Overwatch.Deactivate();
    }
    
    /// <summary>
    /// Check if actor is currently on overwatch.
    /// </summary>
    public bool IsOnOverwatch => Overwatch.IsActive;
    
    /// <summary>
    /// Notify that this actor triggered overwatch on a target.
    /// Called by OverwatchSystem after reaction fire.
    /// </summary>
    public void NotifyOverwatchTriggered(Actor target)
    {
        OverwatchTriggered?.Invoke(this, target);
    }
}
