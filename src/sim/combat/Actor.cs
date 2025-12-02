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
    public const float MoveSpeed = 4.0f; // tiles per second

    public int Id { get; set; }
    public string Type { get; set; } // "crew", "enemy", "drone"
    public int CrewId { get; set; } = -1;
    public Vector2I GridPosition { get; set; } = Vector2I.Zero;
    public Vector2 VisualPosition { get; set; } = Vector2.Zero; // smooth position for rendering
    public int Hp { get; set; } = 100;
    public int MaxHp { get; set; } = 100;
    public ActorState State { get; set; } = ActorState.Alive;
    public Dictionary<string, int> Stats { get; set; } = new() { { "aim", 0 }, { "toughness", 0 }, { "reflexes", 0 } };
    public List<string> Abilities { get; set; } = new();
    public List<string> StatusEffects { get; set; } = new();

    // Weapon and attack
    public WeaponData EquippedWeapon { get; set; } = WeaponData.DefaultRifle;
    public int AttackCooldown { get; set; } = 0; // ticks until can fire again
    public int? AttackTargetId { get; set; } = null; // current attack order target

    // Movement
    public Vector2I TargetPosition { get; private set; } = Vector2I.Zero;
    public bool IsMoving { get; private set; } = false;
    public float MoveProgress { get; private set; } = 0.0f; // 0 to 1 between tiles
    public Vector2I MoveDirection { get; private set; } = Vector2I.Zero;

    // C# Events
    public event Action<Actor, Vector2I> PositionChanged;
    public event Action<Actor> ArrivedAtTarget;
    public event Action<Actor, int> DamageTaken; // actor, damage amount
    public event Action<Actor> Died;

    public Actor(int actorId, string actorType)
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
        StatusEffects = new List<string>();
        EquippedWeapon = WeaponData.DefaultRifle;
    }

    public void SetTarget(Vector2I target)
    {
        TargetPosition = target;
        if (TargetPosition != GridPosition)
        {
            IsMoving = true;
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
        var diff = TargetPosition - GridPosition;
        MoveDirection = new Vector2I(
            Mathf.Clamp(diff.X, -1, 1),
            Mathf.Clamp(diff.Y, -1, 1)
        );

        // Progress movement
        MoveProgress += MoveSpeed * tickDuration;

        if (MoveProgress >= 1.0f)
        {
            // Arrived at next tile
            MoveProgress = 0.0f;
            GridPosition += MoveDirection;
            PositionChanged?.Invoke(this, GridPosition);

            if (GridPosition == TargetPosition)
            {
                IsMoving = false;
                ArrivedAtTarget?.Invoke(this);
            }
        }
    }

    public void SetAttackTarget(int? targetId)
    {
        AttackTargetId = targetId;
        // Clear movement when attacking
        if (targetId.HasValue)
        {
            IsMoving = false;
            TargetPosition = GridPosition;
        }
    }

    public void ClearOrders()
    {
        AttackTargetId = null;
        IsMoving = false;
        TargetPosition = GridPosition;
    }

    public void TakeDamage(int damage)
    {
        if (State != ActorState.Alive)
        {
            return;
        }

        Hp -= damage;
        DamageTaken?.Invoke(this, damage);

        if (Hp <= 0)
        {
            Hp = 0;
            State = ActorState.Dead;
            ClearOrders();
            Died?.Invoke(this);
        }
    }

    public bool CanFire()
    {
        return State == ActorState.Alive && AttackCooldown <= 0;
    }

    public void StartCooldown()
    {
        AttackCooldown = EquippedWeapon.CooldownTicks;
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
}
