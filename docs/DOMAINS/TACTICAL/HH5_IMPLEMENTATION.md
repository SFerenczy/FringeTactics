# HH5 – AI Roles & Behaviour: Implementation Plan

This document breaks down **HH5** from `ROADMAP.md` into concrete implementation steps.

**Goal**: Make enemy AI readable, distinct by role, and reactive to the tactical situation.

**Tactical Axes**: Information + Position

---

## Current State Assessment

### What We Have (From M0–HH4)

| Component | Status | Notes |
|-----------|--------|-------|
| `AIController` | ✅ Complete | Basic target selection, move-and-shoot |
| `PerceptionSystem` | ✅ M6 | Detection states, alarm |
| `OverwatchSystem` | ✅ HH1 | Reaction fire |
| `SuppressionSystem` | ✅ HH2 | Suppressive fire |
| `PhaseSystem` | ✅ HH3 | Mission phases |
| `WaveSystem` | ✅ HH3 | Enemy spawning |
| `Actor` | ✅ Complete | Stats, modifiers, state |

### What HH5 Requires vs What We Have

| HH5 Requirement | Current Status | Gap |
|-----------------|----------------|-----|
| Enemy roles | ❌ Missing | Need role definitions |
| Role-specific behavior | ❌ Missing | Need behavior trees/states |
| Cover-seeking AI | ⚠️ Basic | Exists but not sophisticated |
| Flanking behavior | ❌ Missing | Need flanking logic |
| Suppression usage | ⚠️ Basic | Random, not tactical |
| Phase-based behavior | ❌ Missing | Need phase awareness |
| Retreat behavior | ❌ Missing | Need retreat logic |

---

## Architecture Decisions

### Enemy Role Model

**Decision**: Data-driven roles with behavior profiles.

```csharp
public enum EnemyRole
{
    Guard,      // Defensive, holds position, uses overwatch
    Flanker,    // Aggressive, seeks side angles
    Suppressor, // Uses suppressive fire to pin
    Heavy,      // Slow, high damage, advances steadily
    Officer,    // Coordinates others, uses abilities
    Boss        // Special behavior, high priority target
}

public class EnemyRoleData
{
    public EnemyRole Role { get; set; }
    public float AggressionLevel { get; set; }      // 0-1, how likely to advance
    public float CoverPriority { get; set; }        // 0-1, how much to value cover
    public float FlankingTendency { get; set; }     // 0-1, how likely to flank
    public float OverwatchChance { get; set; }      // 0-1, chance to use overwatch
    public float SuppressionChance { get; set; }    // 0-1, chance to suppress
    public float RetreatThreshold { get; set; }     // HP% to consider retreat
    public List<string> PreferredAbilities { get; set; }
}
```

### Behavior State Machine

**Decision**: Hierarchical state machine per enemy.

```
States:
├── Idle (pre-detection)
├── Alert
│   ├── Holding (in cover, watching)
│   ├── Advancing (moving toward target)
│   ├── Flanking (moving to side angle)
│   ├── Suppressing (laying down fire)
│   └── Retreating (falling back)
└── Engaged
    ├── Attacking (direct fire)
    ├── Overwatching (reaction fire ready)
    └── Using Ability
```

### Tactical Decision Factors

**Decision**: Score-based decision making with role weights.

| Factor | Description | Used By |
|--------|-------------|---------|
| Distance to target | Closer = more aggressive | All |
| Cover quality | Better cover = stay | Guards, Heavy |
| Flank angle | Side angle available | Flankers |
| Ally suppressing | Can advance safely | Flankers |
| HP remaining | Low = retreat | All |
| Phase | Contact vs Pressure | All |
| Overwatch threats | Player overwatching | All |

---

## Implementation Steps

### Phase 1: Role Data System (Priority: Critical)

#### Step 1.1: Create EnemyRole Data

**New File**: `src/sim/combat/ai/EnemyRoleData.cs`

```csharp
using System.Collections.Generic;

namespace FringeTactics;

public enum EnemyRole
{
    Guard,
    Flanker,
    Suppressor,
    Heavy,
    Officer,
    Boss
}

public class EnemyRoleData
{
    public EnemyRole Role { get; set; }
    public string DisplayName { get; set; }
    
    // Behavior weights (0-1)
    public float AggressionLevel { get; set; } = 0.5f;
    public float CoverPriority { get; set; } = 0.5f;
    public float FlankingTendency { get; set; } = 0.3f;
    public float OverwatchChance { get; set; } = 0.2f;
    public float SuppressionChance { get; set; } = 0.1f;
    public float RetreatThreshold { get; set; } = 0.2f;
    
    // Combat preferences
    public float PreferredRange { get; set; } = 6f;
    public bool PrefersOverwatch { get; set; } = false;
    public bool PrefersFlanking { get; set; } = false;
    
    // Abilities
    public List<string> PreferredAbilities { get; set; } = new();
    
    public static Dictionary<EnemyRole, EnemyRoleData> Defaults = new()
    {
        [EnemyRole.Guard] = new EnemyRoleData
        {
            Role = EnemyRole.Guard,
            DisplayName = "Guard",
            AggressionLevel = 0.2f,
            CoverPriority = 0.9f,
            FlankingTendency = 0.1f,
            OverwatchChance = 0.6f,
            SuppressionChance = 0.1f,
            RetreatThreshold = 0.3f,
            PreferredRange = 8f,
            PrefersOverwatch = true
        },
        
        [EnemyRole.Flanker] = new EnemyRoleData
        {
            Role = EnemyRole.Flanker,
            DisplayName = "Flanker",
            AggressionLevel = 0.8f,
            CoverPriority = 0.4f,
            FlankingTendency = 0.9f,
            OverwatchChance = 0.1f,
            SuppressionChance = 0.0f,
            RetreatThreshold = 0.4f,
            PreferredRange = 4f,
            PrefersFlanking = true
        },
        
        [EnemyRole.Suppressor] = new EnemyRoleData
        {
            Role = EnemyRole.Suppressor,
            DisplayName = "Suppressor",
            AggressionLevel = 0.3f,
            CoverPriority = 0.7f,
            FlankingTendency = 0.1f,
            OverwatchChance = 0.2f,
            SuppressionChance = 0.8f,
            RetreatThreshold = 0.3f,
            PreferredRange = 7f
        },
        
        [EnemyRole.Heavy] = new EnemyRoleData
        {
            Role = EnemyRole.Heavy,
            DisplayName = "Heavy",
            AggressionLevel = 0.6f,
            CoverPriority = 0.5f,
            FlankingTendency = 0.0f,
            OverwatchChance = 0.3f,
            SuppressionChance = 0.4f,
            RetreatThreshold = 0.15f,
            PreferredRange = 5f
        },
        
        [EnemyRole.Officer] = new EnemyRoleData
        {
            Role = EnemyRole.Officer,
            DisplayName = "Officer",
            AggressionLevel = 0.4f,
            CoverPriority = 0.8f,
            FlankingTendency = 0.2f,
            OverwatchChance = 0.3f,
            SuppressionChance = 0.3f,
            RetreatThreshold = 0.4f,
            PreferredRange = 6f,
            PreferredAbilities = new() { "rally", "grenade" }
        },
        
        [EnemyRole.Boss] = new EnemyRoleData
        {
            Role = EnemyRole.Boss,
            DisplayName = "Boss",
            AggressionLevel = 0.5f,
            CoverPriority = 0.6f,
            FlankingTendency = 0.3f,
            OverwatchChance = 0.4f,
            SuppressionChance = 0.3f,
            RetreatThreshold = 0.1f,
            PreferredRange = 5f,
            PreferredAbilities = new() { "grenade", "rally" }
        }
    };
}
```

**Acceptance Criteria**:
- [ ] Role enum defined
- [ ] Role data with behavior weights
- [ ] Default configurations for each role

#### Step 1.2: Extend Actor with Role

**File**: `src/sim/combat/state/Actor.cs`

```csharp
// Add role property
public EnemyRole Role { get; set; } = EnemyRole.Guard;
public EnemyRoleData RoleData => EnemyRoleData.Defaults.TryGetValue(Role, out var data) 
    ? data : EnemyRoleData.Defaults[EnemyRole.Guard];
```

**File**: `src/sim/data/EnemySpawn.cs`

```csharp
public class EnemySpawn
{
    public string EnemyType { get; set; }
    public Vector2I Position { get; set; }
    public EnemyRole Role { get; set; } = EnemyRole.Guard;
    public string Tag { get; set; }
    
    public EnemySpawn(string type, Vector2I pos, EnemyRole role = EnemyRole.Guard)
    {
        EnemyType = type;
        Position = pos;
        Role = role;
    }
}
```

**Acceptance Criteria**:
- [ ] Actors have Role property
- [ ] EnemySpawn includes role
- [ ] Role data accessible via Actor

---

### Phase 2: AI Behavior States (Priority: Critical)

#### Step 2.1: Create AIBehaviorState

**New File**: `src/sim/combat/ai/AIBehaviorState.cs`

```csharp
namespace FringeTactics;

public enum AIBehaviorState
{
    Idle,           // Pre-detection, standing guard
    Holding,        // In cover, watching
    Advancing,      // Moving toward target
    Flanking,       // Moving to side angle
    Suppressing,    // Laying down suppressive fire
    Attacking,      // Direct fire on target
    Overwatching,   // Reaction fire ready
    Retreating,     // Falling back to safety
    UsingAbility    // Executing special ability
}

public class AIState
{
    public AIBehaviorState Behavior { get; set; } = AIBehaviorState.Idle;
    public int? TargetActorId { get; set; }
    public Vector2I? TargetPosition { get; set; }
    public int StateEnteredTick { get; set; }
    public int TicksInState { get; set; }
    
    public void TransitionTo(AIBehaviorState newState, int currentTick)
    {
        Behavior = newState;
        StateEnteredTick = currentTick;
        TicksInState = 0;
    }
    
    public void Tick()
    {
        TicksInState++;
    }
}
```

**Acceptance Criteria**:
- [ ] Behavior states defined
- [ ] State tracking per enemy
- [ ] Transition tracking

#### Step 2.2: Create TacticalAnalyzer

**New File**: `src/sim/combat/ai/TacticalAnalyzer.cs`

```csharp
using Godot;
using System.Collections.Generic;
using System.Linq;

namespace FringeTactics;

/// <summary>
/// Analyzes tactical situation for AI decision making.
/// </summary>
public class TacticalAnalyzer
{
    private readonly CombatState combatState;
    
    public TacticalAnalyzer(CombatState combatState)
    {
        this.combatState = combatState;
    }
    
    /// <summary>
    /// Find cover positions relative to threats.
    /// </summary>
    public List<Vector2I> FindCoverPositions(Vector2I from, int searchRadius = 5)
    {
        var coverPositions = new List<(Vector2I pos, float score)>();
        
        for (int dx = -searchRadius; dx <= searchRadius; dx++)
        {
            for (int dy = -searchRadius; dy <= searchRadius; dy++)
            {
                var pos = from + new Vector2I(dx, dy);
                if (!combatState.MapState.IsWalkable(pos)) continue;
                if (combatState.GetActorAtPosition(pos) != null) continue;
                
                var coverScore = EvaluateCoverPosition(pos);
                if (coverScore > 0)
                {
                    coverPositions.Add((pos, coverScore));
                }
            }
        }
        
        return coverPositions
            .OrderByDescending(p => p.score)
            .Select(p => p.pos)
            .ToList();
    }
    
    /// <summary>
    /// Evaluate how good a position is for cover.
    /// </summary>
    public float EvaluateCoverPosition(Vector2I position)
    {
        var score = 0f;
        
        // Check cover against all visible crew
        foreach (var crew in combatState.Actors.Where(a => 
            a.Type == ActorType.Crew && a.State == ActorState.Alive))
        {
            var coverHeight = combatState.MapState.GetCoverAgainst(position, crew.GridPosition);
            score += coverHeight switch
            {
                CoverHeight.High => 3f,
                CoverHeight.Half => 2f,
                CoverHeight.Low => 1f,
                _ => 0f
            };
        }
        
        return score;
    }
    
    /// <summary>
    /// Find flanking positions for a target.
    /// </summary>
    public List<Vector2I> FindFlankingPositions(Actor enemy, Actor target, int searchRadius = 8)
    {
        var flankPositions = new List<(Vector2I pos, float score)>();
        var targetPos = target.GridPosition;
        var enemyPos = enemy.GridPosition;
        
        // Get direction from target to enemy (we want perpendicular)
        var toEnemy = enemyPos - targetPos;
        var perpendicular1 = new Vector2I(-toEnemy.Y, toEnemy.X);
        var perpendicular2 = new Vector2I(toEnemy.Y, -toEnemy.X);
        
        for (int dx = -searchRadius; dx <= searchRadius; dx++)
        {
            for (int dy = -searchRadius; dy <= searchRadius; dy++)
            {
                var pos = targetPos + new Vector2I(dx, dy);
                if (!combatState.MapState.IsWalkable(pos)) continue;
                if (combatState.GetActorAtPosition(pos) != null) continue;
                
                var flankScore = EvaluateFlankPosition(pos, targetPos, enemyPos);
                if (flankScore > 0.5f)
                {
                    flankPositions.Add((pos, flankScore));
                }
            }
        }
        
        return flankPositions
            .OrderByDescending(p => p.score)
            .Take(5)
            .Select(p => p.pos)
            .ToList();
    }
    
    /// <summary>
    /// Evaluate how good a position is for flanking.
    /// </summary>
    public float EvaluateFlankPosition(Vector2I position, Vector2I targetPos, Vector2I originalPos)
    {
        var score = 0f;
        
        // Must have LOS to target
        if (!CombatResolver.HasLineOfSight(position, targetPos, combatState.MapState))
            return 0f;
        
        // Calculate angle difference
        var originalAngle = Mathf.Atan2(originalPos.Y - targetPos.Y, originalPos.X - targetPos.X);
        var newAngle = Mathf.Atan2(position.Y - targetPos.Y, position.X - targetPos.X);
        var angleDiff = Mathf.Abs(Mathf.Wrap(newAngle - originalAngle, -Mathf.Pi, Mathf.Pi));
        
        // Best flank is 90 degrees (perpendicular)
        var flankQuality = 1f - Mathf.Abs(angleDiff - Mathf.Pi / 2) / (Mathf.Pi / 2);
        score += flankQuality * 2f;
        
        // Prefer positions with cover
        var coverScore = EvaluateCoverPosition(position);
        score += coverScore * 0.5f;
        
        // Prefer closer positions (within weapon range)
        var distance = CombatResolver.GetDistance(position, targetPos);
        if (distance <= 6)
        {
            score += 1f;
        }
        
        return score;
    }
    
    /// <summary>
    /// Check if any ally is suppressing a target.
    /// </summary>
    public bool IsTargetSuppressed(Actor target)
    {
        return target.IsSuppressed();
    }
    
    /// <summary>
    /// Check if any crew is overwatching toward a position.
    /// </summary>
    public bool IsPositionOverwatched(Vector2I position)
    {
        foreach (var crew in combatState.Actors.Where(a => 
            a.Type == ActorType.Crew && a.State == ActorState.Alive && a.IsOnOverwatch))
        {
            var distance = CombatResolver.GetDistance(crew.GridPosition, position);
            if (distance <= crew.EquippedWeapon.Range &&
                CombatResolver.HasLineOfSight(crew.GridPosition, position, combatState.MapState))
            {
                return true;
            }
        }
        return false;
    }
    
    /// <summary>
    /// Find retreat positions (away from threats, toward cover).
    /// </summary>
    public List<Vector2I> FindRetreatPositions(Actor enemy, int searchRadius = 6)
    {
        var retreatPositions = new List<(Vector2I pos, float score)>();
        var threats = combatState.Actors
            .Where(a => a.Type == ActorType.Crew && a.State == ActorState.Alive)
            .ToList();
        
        if (threats.Count == 0) return new List<Vector2I>();
        
        var threatCenter = new Vector2(
            threats.Average(t => t.GridPosition.X),
            threats.Average(t => t.GridPosition.Y));
        
        for (int dx = -searchRadius; dx <= searchRadius; dx++)
        {
            for (int dy = -searchRadius; dy <= searchRadius; dy++)
            {
                var pos = enemy.GridPosition + new Vector2I(dx, dy);
                if (!combatState.MapState.IsWalkable(pos)) continue;
                if (combatState.GetActorAtPosition(pos) != null) continue;
                
                // Score: further from threats + has cover
                var distFromThreats = new Vector2(pos.X, pos.Y).DistanceTo(threatCenter);
                var coverScore = EvaluateCoverPosition(pos);
                var score = distFromThreats * 0.3f + coverScore;
                
                retreatPositions.Add((pos, score));
            }
        }
        
        return retreatPositions
            .OrderByDescending(p => p.score)
            .Take(3)
            .Select(p => p.pos)
            .ToList();
    }
}
```

**Acceptance Criteria**:
- [ ] Cover position finding works
- [ ] Flanking position finding works
- [ ] Overwatch threat detection works
- [ ] Retreat position finding works

---

### Phase 3: Enhanced AIController (Priority: Critical)

#### Step 3.1: Refactor AIController with Roles

**File**: `src/sim/combat/systems/AIController.cs`

```csharp
using Godot;
using System.Collections.Generic;
using System.Linq;

namespace FringeTactics;

/// <summary>
/// Enhanced AI controller with role-based behavior.
/// </summary>
public class AIController
{
    public const int THINK_INTERVAL_TICKS = 10;

    private readonly CombatState combatState;
    private readonly TacticalAnalyzer analyzer;
    private readonly Dictionary<int, AIState> aiStates = new();
    private int ticksSinceLastThink = 0;

    public AIController(CombatState combatState)
    {
        this.combatState = combatState;
        this.analyzer = new TacticalAnalyzer(combatState);
    }

    public void Tick()
    {
        ticksSinceLastThink++;
        
        // Update state timers
        foreach (var state in aiStates.Values)
        {
            state.Tick();
        }

        if (ticksSinceLastThink < THINK_INTERVAL_TICKS)
        {
            return;
        }

        ticksSinceLastThink = 0;
        ThinkAllEnemies();
    }

    private AIState GetOrCreateState(Actor enemy)
    {
        if (!aiStates.TryGetValue(enemy.Id, out var state))
        {
            state = new AIState();
            aiStates[enemy.Id] = state;
        }
        return state;
    }

    private void ThinkAllEnemies()
    {
        foreach (var actor in combatState.Actors)
        {
            if (actor.Type != ActorType.Enemy || actor.State != ActorState.Alive)
            {
                continue;
            }

            Think(actor);
        }
    }

    private void Think(Actor enemy)
    {
        var state = GetOrCreateState(enemy);
        var roleData = enemy.RoleData;
        var detectionState = combatState.Perception.GetDetectionState(enemy.Id);
        
        // Idle enemies don't act
        if (detectionState == DetectionState.Idle)
        {
            state.TransitionTo(AIBehaviorState.Idle, combatState.TimeSystem.CurrentTick);
            return;
        }
        
        // Check for retreat
        if (ShouldRetreat(enemy, roleData))
        {
            ExecuteRetreat(enemy, state);
            return;
        }
        
        // Role-based decision
        switch (enemy.Role)
        {
            case EnemyRole.Guard:
                ThinkGuard(enemy, state, roleData);
                break;
            case EnemyRole.Flanker:
                ThinkFlanker(enemy, state, roleData);
                break;
            case EnemyRole.Suppressor:
                ThinkSuppressor(enemy, state, roleData);
                break;
            case EnemyRole.Heavy:
                ThinkHeavy(enemy, state, roleData);
                break;
            case EnemyRole.Officer:
                ThinkOfficer(enemy, state, roleData);
                break;
            case EnemyRole.Boss:
                ThinkBoss(enemy, state, roleData);
                break;
            default:
                ThinkDefault(enemy, state, roleData);
                break;
        }
    }

    private bool ShouldRetreat(Actor enemy, EnemyRoleData roleData)
    {
        var hpPercent = enemy.Hp / (float)enemy.MaxHp;
        return hpPercent <= roleData.RetreatThreshold;
    }

    private void ExecuteRetreat(Actor enemy, AIState state)
    {
        state.TransitionTo(AIBehaviorState.Retreating, combatState.TimeSystem.CurrentTick);
        
        var retreatPositions = analyzer.FindRetreatPositions(enemy);
        if (retreatPositions.Count > 0)
        {
            enemy.SetTarget(retreatPositions[0]);
            SimLog.Log($"[AI] {enemy.Role} #{enemy.Id} retreating to {retreatPositions[0]}");
        }
    }

    // === GUARD BEHAVIOR ===
    private void ThinkGuard(Actor enemy, AIState state, EnemyRoleData roleData)
    {
        var target = FindBestTarget(enemy);
        
        // Guards prefer overwatch when no immediate target
        if (target == null || !CombatResolver.CanAttack(enemy, target, enemy.EquippedWeapon, combatState.MapState))
        {
            if (!enemy.IsOnOverwatch && combatState.Rng.NextFloat() < roleData.OverwatchChance)
            {
                enemy.EnterOverwatch(combatState.TimeSystem.CurrentTick);
                state.TransitionTo(AIBehaviorState.Overwatching, combatState.TimeSystem.CurrentTick);
                SimLog.Log($"[AI] Guard #{enemy.Id} entering overwatch");
                return;
            }
        }
        
        // If in good cover, hold position
        var coverScore = analyzer.EvaluateCoverPosition(enemy.GridPosition);
        if (coverScore >= 2f && target != null)
        {
            state.TransitionTo(AIBehaviorState.Holding, combatState.TimeSystem.CurrentTick);
            if (CombatResolver.CanAttack(enemy, target, enemy.EquippedWeapon, combatState.MapState))
            {
                enemy.SetAttackTarget(target.Id);
            }
            return;
        }
        
        // Move to better cover
        var coverPositions = analyzer.FindCoverPositions(enemy.GridPosition);
        if (coverPositions.Count > 0)
        {
            enemy.SetTarget(coverPositions[0]);
            state.TransitionTo(AIBehaviorState.Advancing, combatState.TimeSystem.CurrentTick);
        }
    }

    // === FLANKER BEHAVIOR ===
    private void ThinkFlanker(Actor enemy, AIState state, EnemyRoleData roleData)
    {
        var target = FindBestTarget(enemy);
        if (target == null) return;
        
        // If target is suppressed, advance aggressively
        if (analyzer.IsTargetSuppressed(target))
        {
            state.TransitionTo(AIBehaviorState.Advancing, combatState.TimeSystem.CurrentTick);
            var moveTarget = GetMoveTowardTarget(enemy, target);
            enemy.SetTarget(moveTarget);
            SimLog.Log($"[AI] Flanker #{enemy.Id} exploiting suppression on #{target.Id}");
            return;
        }
        
        // Look for flanking positions
        var flankPositions = analyzer.FindFlankingPositions(enemy, target);
        if (flankPositions.Count > 0 && combatState.Rng.NextFloat() < roleData.FlankingTendency)
        {
            // Check if path is overwatched
            var bestFlank = flankPositions.FirstOrDefault(p => !analyzer.IsPositionOverwatched(p));
            if (bestFlank != default)
            {
                enemy.SetTarget(bestFlank);
                state.TransitionTo(AIBehaviorState.Flanking, combatState.TimeSystem.CurrentTick);
                SimLog.Log($"[AI] Flanker #{enemy.Id} flanking to {bestFlank}");
                return;
            }
        }
        
        // Default: attack if in range, else advance
        if (CombatResolver.CanAttack(enemy, target, enemy.EquippedWeapon, combatState.MapState))
        {
            enemy.SetAttackTarget(target.Id);
            state.TransitionTo(AIBehaviorState.Attacking, combatState.TimeSystem.CurrentTick);
        }
        else
        {
            enemy.SetTarget(GetMoveTowardTarget(enemy, target));
            state.TransitionTo(AIBehaviorState.Advancing, combatState.TimeSystem.CurrentTick);
        }
    }

    // === SUPPRESSOR BEHAVIOR ===
    private void ThinkSuppressor(Actor enemy, AIState state, EnemyRoleData roleData)
    {
        var target = FindBestSuppressionTarget(enemy);
        if (target == null)
        {
            ThinkDefault(enemy, state, roleData);
            return;
        }
        
        // Use suppressive fire
        if (combatState.Suppression.CanSuppressiveFire(enemy) && 
            combatState.Rng.NextFloat() < roleData.SuppressionChance)
        {
            if (CombatResolver.HasLineOfSight(enemy.GridPosition, target.GridPosition, combatState.MapState))
            {
                combatState.Suppression.ExecuteSuppressiveFire(enemy, target);
                state.TransitionTo(AIBehaviorState.Suppressing, combatState.TimeSystem.CurrentTick);
                SimLog.Log($"[AI] Suppressor #{enemy.Id} suppressing #{target.Id}");
                return;
            }
        }
        
        // Move to position with LOS
        if (!CombatResolver.HasLineOfSight(enemy.GridPosition, target.GridPosition, combatState.MapState))
        {
            var coverPositions = analyzer.FindCoverPositions(enemy.GridPosition)
                .Where(p => CombatResolver.HasLineOfSight(p, target.GridPosition, combatState.MapState))
                .ToList();
            
            if (coverPositions.Count > 0)
            {
                enemy.SetTarget(coverPositions[0]);
                state.TransitionTo(AIBehaviorState.Advancing, combatState.TimeSystem.CurrentTick);
                return;
            }
        }
        
        // Fallback to normal attack
        ThinkDefault(enemy, state, roleData);
    }

    // === HEAVY BEHAVIOR ===
    private void ThinkHeavy(Actor enemy, AIState state, EnemyRoleData roleData)
    {
        var target = FindBestTarget(enemy);
        if (target == null) return;
        
        // Heavy advances steadily, uses suppression
        if (CombatResolver.CanAttack(enemy, target, enemy.EquippedWeapon, combatState.MapState))
        {
            // Alternate between suppression and direct fire
            if (combatState.Suppression.CanSuppressiveFire(enemy) && 
                combatState.Rng.NextFloat() < roleData.SuppressionChance)
            {
                combatState.Suppression.ExecuteSuppressiveFire(enemy, target);
                state.TransitionTo(AIBehaviorState.Suppressing, combatState.TimeSystem.CurrentTick);
            }
            else
            {
                enemy.SetAttackTarget(target.Id);
                state.TransitionTo(AIBehaviorState.Attacking, combatState.TimeSystem.CurrentTick);
            }
        }
        else
        {
            // Advance toward target
            enemy.SetTarget(GetMoveTowardTarget(enemy, target));
            state.TransitionTo(AIBehaviorState.Advancing, combatState.TimeSystem.CurrentTick);
        }
    }

    // === OFFICER BEHAVIOR ===
    private void ThinkOfficer(Actor enemy, AIState state, EnemyRoleData roleData)
    {
        // Officers coordinate: suppress targets that allies are flanking
        var alliesNearTarget = GetAlliesFlankingAnyTarget(enemy);
        
        if (alliesNearTarget.Count > 0)
        {
            var targetToSuppress = alliesNearTarget.First().target;
            if (combatState.Suppression.CanSuppressiveFire(enemy) &&
                CombatResolver.HasLineOfSight(enemy.GridPosition, targetToSuppress.GridPosition, combatState.MapState))
            {
                combatState.Suppression.ExecuteSuppressiveFire(enemy, targetToSuppress);
                state.TransitionTo(AIBehaviorState.Suppressing, combatState.TimeSystem.CurrentTick);
                SimLog.Log($"[AI] Officer #{enemy.Id} supporting flank on #{targetToSuppress.Id}");
                return;
            }
        }
        
        // Use abilities if available
        // TODO: Implement ability usage
        
        // Default to guard behavior
        ThinkGuard(enemy, state, roleData);
    }

    // === BOSS BEHAVIOR ===
    private void ThinkBoss(Actor enemy, AIState state, EnemyRoleData roleData)
    {
        var target = FindBestTarget(enemy);
        if (target == null) return;
        
        // Boss is aggressive but smart
        var phase = combatState.Phases.CurrentPhase;
        
        // In pressure phase, boss is more aggressive
        if (phase == MissionPhase.Pressure)
        {
            if (CombatResolver.CanAttack(enemy, target, enemy.EquippedWeapon, combatState.MapState))
            {
                enemy.SetAttackTarget(target.Id);
                state.TransitionTo(AIBehaviorState.Attacking, combatState.TimeSystem.CurrentTick);
            }
            else
            {
                enemy.SetTarget(GetMoveTowardTarget(enemy, target));
                state.TransitionTo(AIBehaviorState.Advancing, combatState.TimeSystem.CurrentTick);
            }
        }
        else
        {
            // In contact phase, boss holds position
            ThinkGuard(enemy, state, roleData);
        }
    }

    // === DEFAULT BEHAVIOR ===
    private void ThinkDefault(Actor enemy, AIState state, EnemyRoleData roleData)
    {
        var target = FindBestTarget(enemy);
        if (target == null) return;
        
        if (CombatResolver.CanAttack(enemy, target, enemy.EquippedWeapon, combatState.MapState))
        {
            enemy.SetAttackTarget(target.Id);
            state.TransitionTo(AIBehaviorState.Attacking, combatState.TimeSystem.CurrentTick);
        }
        else
        {
            enemy.SetTarget(GetMoveTowardTarget(enemy, target));
            state.TransitionTo(AIBehaviorState.Advancing, combatState.TimeSystem.CurrentTick);
        }
    }

    // === HELPER METHODS ===
    
    private Actor FindBestTarget(Actor enemy)
    {
        // ... existing implementation ...
        Actor bestTarget = null;
        float bestScore = float.MinValue;

        foreach (var actor in combatState.Actors)
        {
            if (actor.Type == ActorType.Enemy || actor.State != ActorState.Alive)
                continue;

            if (!CombatResolver.HasLineOfSight(enemy.GridPosition, actor.GridPosition, combatState.MapState))
                continue;

            var score = ScoreTarget(enemy, actor);
            if (score > bestScore)
            {
                bestScore = score;
                bestTarget = actor;
            }
        }

        return bestTarget;
    }
    
    private Actor FindBestSuppressionTarget(Actor enemy)
    {
        // Prioritize targets in cover or on overwatch
        Actor bestTarget = null;
        float bestScore = float.MinValue;

        foreach (var actor in combatState.Actors)
        {
            if (actor.Type == ActorType.Enemy || actor.State != ActorState.Alive)
                continue;

            if (!CombatResolver.HasLineOfSight(enemy.GridPosition, actor.GridPosition, combatState.MapState))
                continue;

            var score = 0f;
            
            // Prefer targets on overwatch
            if (actor.IsOnOverwatch) score += 2f;
            
            // Prefer targets in cover
            var coverHeight = combatState.MapState.GetCoverAgainst(actor.GridPosition, enemy.GridPosition);
            if (coverHeight != CoverHeight.None) score += 1f;
            
            // Prefer targets that are threats
            if (actor.AttackTargetId == enemy.Id) score += 0.5f;
            
            if (score > bestScore)
            {
                bestScore = score;
                bestTarget = actor;
            }
        }

        return bestTarget;
    }
    
    private List<(Actor ally, Actor target)> GetAlliesFlankingAnyTarget(Actor officer)
    {
        var result = new List<(Actor, Actor)>();
        
        foreach (var ally in combatState.Actors.Where(a => 
            a.Type == ActorType.Enemy && a.State == ActorState.Alive && a.Id != officer.Id))
        {
            if (ally.Role != EnemyRole.Flanker) continue;
            
            var state = GetOrCreateState(ally);
            if (state.Behavior == AIBehaviorState.Flanking && state.TargetActorId.HasValue)
            {
                var target = combatState.GetActorById(state.TargetActorId.Value);
                if (target != null)
                {
                    result.Add((ally, target));
                }
            }
        }
        
        return result;
    }

    private float ScoreTarget(Actor enemy, Actor target)
    {
        // ... existing implementation with role adjustments ...
        var distance = CombatResolver.GetDistance(enemy.GridPosition, target.GridPosition);
        var distanceScore = 1f / (distance + 1f);
        var healthScore = 1f - (target.Hp / (float)target.MaxHp);
        
        var threatScore = 0f;
        if (target.AttackTargetId == enemy.Id) threatScore = 0.5f;
        
        var inRangeBonus = CombatResolver.CanAttack(enemy, target, enemy.EquippedWeapon, combatState.MapState) 
            ? 0.4f : 0f;
        
        return distanceScore + inRangeBonus + threatScore + healthScore * 0.3f;
    }

    private Vector2I GetMoveTowardTarget(Actor enemy, Actor target)
    {
        // ... existing implementation ...
        var step = GridUtils.GetStepDirection(enemy.GridPosition, target.GridPosition);
        var newPos = enemy.GridPosition + step;

        if (combatState.MapState.IsWalkable(newPos) && combatState.GetActorAtPosition(newPos) == null)
        {
            return newPos;
        }

        var cardinalSteps = new Vector2I[]
        {
            new Vector2I(step.X, 0),
            new Vector2I(0, step.Y)
        };

        foreach (var cardinalStep in cardinalSteps)
        {
            if (cardinalStep == Vector2I.Zero) continue;
            var cardinalPos = enemy.GridPosition + cardinalStep;
            if (combatState.MapState.IsWalkable(cardinalPos) && combatState.GetActorAtPosition(cardinalPos) == null)
            {
                return cardinalPos;
            }
        }

        return enemy.GridPosition;
    }
}
```

**Acceptance Criteria**:
- [ ] Role-specific think methods
- [ ] Guards use overwatch and hold cover
- [ ] Flankers seek side angles
- [ ] Suppressors pin targets
- [ ] Officers coordinate
- [ ] Retreat behavior works

---

### Phase 4: Visual Feedback (Priority: High)

#### Step 4.1: Role Indicator on ActorView

**File**: `src/scenes/mission/ActorView.cs`

```csharp
private Label roleIndicator;

private void CreateRoleIndicator()
{
    if (actor.Type != ActorType.Enemy) return;
    
    roleIndicator = new Label();
    roleIndicator.Position = new Vector2(-4, -GridConstants.TileSize - 16);
    roleIndicator.AddThemeFontSizeOverride("font_size", 10);
    UpdateRoleIndicator();
    AddChild(roleIndicator);
}

private void UpdateRoleIndicator()
{
    if (roleIndicator == null) return;
    
    roleIndicator.Text = actor.Role switch
    {
        EnemyRole.Guard => "G",
        EnemyRole.Flanker => "F",
        EnemyRole.Suppressor => "S",
        EnemyRole.Heavy => "H",
        EnemyRole.Officer => "O",
        EnemyRole.Boss => "★",
        _ => ""
    };
    
    roleIndicator.AddThemeColorOverride("font_color", actor.Role switch
    {
        EnemyRole.Guard => Colors.Gray,
        EnemyRole.Flanker => Colors.Orange,
        EnemyRole.Suppressor => Colors.Yellow,
        EnemyRole.Heavy => Colors.Red,
        EnemyRole.Officer => Colors.Purple,
        EnemyRole.Boss => Colors.Gold,
        _ => Colors.White
    });
}
```

**Acceptance Criteria**:
- [ ] Role indicators visible on enemies
- [ ] Color coding by role
- [ ] Boss has special indicator

---

## Testing Checklist

### Manual Testing

1. **Guard Behavior**
   - [ ] Guards hold cover positions
   - [ ] Guards use overwatch when no target
   - [ ] Guards don't rush forward

2. **Flanker Behavior**
   - [ ] Flankers seek side angles
   - [ ] Flankers exploit suppression
   - [ ] Flankers avoid overwatch zones

3. **Suppressor Behavior**
   - [ ] Suppressors use suppressive fire
   - [ ] Suppressors prioritize overwatching targets
   - [ ] Suppressors enable ally movement

4. **Heavy Behavior**
   - [ ] Heavies advance steadily
   - [ ] Heavies use suppression
   - [ ] Heavies are hard to stop

5. **Officer Behavior**
   - [ ] Officers support flanking allies
   - [ ] Officers coordinate suppression
   - [ ] Officers stay in cover

6. **Boss Behavior**
   - [ ] Boss behavior changes by phase
   - [ ] Boss is aggressive in Pressure
   - [ ] Boss is defensive in Contact

7. **Retreat**
   - [ ] Low HP triggers retreat
   - [ ] Retreat moves to safe positions
   - [ ] Different roles have different thresholds

### Automated Tests

Create `tests/sim/combat/HH5Tests.cs`:

```csharp
[TestSuite]
public class HH5Tests
{
    // === Role Data ===
    [TestCase] EnemyRoleData_DefaultsExist_ForAllRoles()
    [TestCase] Actor_HasRole_Property()
    
    // === Tactical Analyzer ===
    [TestCase] TacticalAnalyzer_FindCoverPositions_ReturnsCover()
    [TestCase] TacticalAnalyzer_FindFlankingPositions_ReturnsAngles()
    [TestCase] TacticalAnalyzer_IsPositionOverwatched_DetectsThreats()
    
    // === Role Behaviors ===
    [TestCase] Guard_UsesOverwatch_WhenNoTarget()
    [TestCase] Guard_HoldsPosition_InGoodCover()
    [TestCase] Flanker_SeeksFlankPosition_WhenAvailable()
    [TestCase] Flanker_ExploitsSuppression_WhenTargetSuppressed()
    [TestCase] Suppressor_UsesSuppressiveFire_OnPriorityTargets()
    
    // === Retreat ===
    [TestCase] AI_Retreats_WhenHpBelowThreshold()
    [TestCase] AI_RetreatThreshold_VariesByRole()
}
```

---

## Implementation Order

1. **Day 1: Role Data**
   - Step 1.1: Create EnemyRoleData
   - Step 1.2: Extend Actor with Role

2. **Day 2: AI Infrastructure**
   - Step 2.1: Create AIBehaviorState
   - Step 2.2: Create TacticalAnalyzer

3. **Day 3-4: AIController Refactor**
   - Step 3.1: Implement role-specific behaviors
   - Test each role individually

4. **Day 5: Visuals & Testing**
   - Step 4.1: Role indicators
   - Write tests, manual testing

---

## Success Criteria for HH5

When HH5 is complete:

1. ✅ Enemies have distinct roles
2. ✅ Guards hold positions and use overwatch
3. ✅ Flankers seek side angles
4. ✅ Suppressors pin targets
5. ✅ Officers coordinate allies
6. ✅ AI retreats when appropriate
7. ✅ Behavior is readable to players
8. ✅ All automated tests pass

**Natural Pause Point**: Combat feels like fighting opponents with plans, not just reaction scripts. The Hangar Handover now has tactically interesting enemies.

---

## Files to Create/Modify

### New Files
- `src/sim/combat/ai/EnemyRoleData.cs`
- `src/sim/combat/ai/AIBehaviorState.cs`
- `src/sim/combat/ai/TacticalAnalyzer.cs`
- `tests/sim/combat/HH5Tests.cs`

### Modified Files
- `src/sim/combat/state/Actor.cs` - Add Role property
- `src/sim/data/EnemySpawn.cs` - Add Role to spawn
- `src/sim/combat/systems/AIController.cs` - Role-based behavior
- `src/scenes/mission/ActorView.cs` - Role indicators

---

## Dependencies

- **Requires**: HH1 (overwatch), HH2 (suppression), HH3 (phases)
- **Enables**: HH6 (AI behavior visibility in UX)
