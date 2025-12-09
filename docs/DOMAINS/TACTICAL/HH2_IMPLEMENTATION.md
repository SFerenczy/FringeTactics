# HH2 – Suppression System: Implementation Plan

This document breaks down **HH2** from `ROADMAP.md` into concrete implementation steps.

**Goal**: Create non-lethal pressure tool that shapes behaviour and enables manoeuvre.

**Tactical Axes**: Position + Time

---

## Current State Assessment

### What We Have (From M0–HH1)

| Component | Status | Notes |
|-----------|--------|-------|
| `SuppressedEffect` | ✅ Exists | Basic effect with accuracy/speed penalties |
| `ActorEffects` | ✅ Complete | Effect application and tick system |
| `AbilitySystem` | ✅ Complete | Ability execution with cooldowns |
| `AttackSystem` | ✅ Complete | Attack processing |
| `OverwatchSystem` | ✅ HH1 | Reaction fire system |
| `WeaponData` | ✅ Complete | Weapon stats and properties |

### What HH2 Requires vs What We Have

| HH2 Requirement | Current Status | Gap |
|-----------------|----------------|-----|
| Suppressive fire action | ❌ Missing | Need new action type |
| Suppression on near-miss | ❌ Missing | Need miss-based effect application |
| Suppression degrades overwatch | ❌ Missing | Need interaction with HH1 |
| Area suppression | ❌ Missing | Need area-of-effect suppression |
| Visual feedback | ⚠️ Partial | Effect exists but no suppression-specific UI |
| AI suppression usage | ❌ Missing | Need AI decision to suppress |

---

## Architecture Decisions

### Suppression Model

**Decision**: Suppression is both an action and a status effect.

**Suppressive Fire Action**:
- Consumes more ammo than normal attack (burst fire)
- Lower damage than aimed shot
- High chance to apply Suppressed status even on miss
- Can target a tile (area suppression) or an actor

**Suppressed Status Effect** (enhance existing):
- Accuracy penalty: -30%
- Movement speed penalty: -50%
- **NEW**: Overwatch effectiveness: -50% (or disable entirely)
- **NEW**: Cannot enter overwatch while suppressed
- Duration: ~3-4 seconds (60-80 ticks)

### Suppression Application Rules

| Outcome | Suppression Chance |
|---------|-------------------|
| Hit | 100% (always suppressed) |
| Near miss (within 2 tiles) | 70% |
| Far miss | 30% |
| Area suppression (in zone) | 80% |

### Suppression vs Overwatch Interaction

**Decision**: Suppressed units have degraded overwatch.

- Suppressed units on overwatch have -50% reaction accuracy
- Suppressed units cannot enter new overwatch
- Option: Suppression immediately cancels overwatch (more aggressive)

---

## Implementation Steps

### Phase 1: Enhanced Suppression Effect (Priority: Critical)

#### Step 1.1: Update SuppressedEffect

**File**: `src/sim/combat/effects/SuppressedEffect.cs`

```csharp
namespace FringeTactics;

/// <summary>
/// Suppression effect - reduces accuracy, movement, and overwatch effectiveness.
/// Applied by suppressive fire, sustained attacks, etc.
/// </summary>
public class SuppressedEffect : EffectBase
{
    public const string EffectId = "suppressed";
    
    // Balance constants
    private const float AccuracyPenalty = 0.7f;      // 30% reduction
    private const float SpeedPenalty = 0.5f;         // 50% reduction
    private const float OverwatchPenalty = 0.5f;     // 50% reduction to overwatch accuracy
    public const int DefaultDuration = 60;           // 3 seconds at 20 ticks/sec
    
    public override string Id => EffectId;
    public override string Name => "Suppressed";
    public override bool CanStack => false;
    
    public SuppressedEffect(int durationTicks) : base(durationTicks)
    {
    }
    
    public override void OnApply(Actor target)
    {
        base.OnApply(target);
        
        // Accuracy penalty
        var accuracyModifier = StatModifier.Multiplicative(
            EffectId, StatType.Accuracy, AccuracyPenalty, -1);
        
        // Movement speed penalty
        var speedModifier = StatModifier.Multiplicative(
            EffectId, StatType.MoveSpeed, SpeedPenalty, -1);
        
        // Overwatch accuracy penalty (new stat type needed)
        var overwatchModifier = StatModifier.Multiplicative(
            EffectId, StatType.OverwatchAccuracy, OverwatchPenalty, -1);
        
        target.Modifiers.Add(accuracyModifier);
        target.Modifiers.Add(speedModifier);
        target.Modifiers.Add(overwatchModifier);
        
        // Cancel overwatch when suppressed (optional - aggressive mode)
        // target.ExitOverwatch();
        
        SimLog.Log($"[Effect] {target.Type}#{target.Id} is SUPPRESSED");
    }
    
    public override void OnRemove(Actor target)
    {
        base.OnRemove(target);
        target.Modifiers.RemoveBySource(EffectId);
        SimLog.Log($"[Effect] {target.Type}#{target.Id} suppression ended");
    }
    
    public override IEffect Clone(int durationTicks) => new SuppressedEffect(durationTicks);
}
```

**Acceptance Criteria**:
- [ ] Suppression applies accuracy, speed, and overwatch penalties
- [ ] Default duration is ~3 seconds
- [ ] Effect logs application and removal

#### Step 1.2: Add OverwatchAccuracy Stat Type

**File**: `src/sim/combat/modifiers/StatType.cs`

```csharp
public enum StatType
{
    MoveSpeed,
    Accuracy,
    VisionRadius,
    OverwatchAccuracy,  // NEW
    // ... other stats
}
```

**File**: `src/sim/combat/systems/OverwatchSystem.cs`

Update reaction fire to use overwatch accuracy:
```csharp
private void ExecuteReactionFire(Actor overwatcher, Actor target)
{
    // Apply overwatch accuracy modifier
    var baseAccuracy = overwatcher.GetAccuracy();
    var overwatchAccuracy = overwatcher.Modifiers.Calculate(
        StatType.OverwatchAccuracy, baseAccuracy);
    
    // Create modified attack with overwatch accuracy
    // ... rest of reaction fire logic
}
```

**Acceptance Criteria**:
- [ ] `StatType.OverwatchAccuracy` exists
- [ ] Overwatch system uses this stat for reaction fire
- [ ] Suppression reduces overwatch effectiveness

---

### Phase 2: Suppressive Fire Action (Priority: Critical)

#### Step 2.1: Create SuppressiveFireAbility

**New File**: `src/sim/combat/abilities/SuppressiveFireAbility.cs`

```csharp
using Godot;

namespace FringeTactics;

/// <summary>
/// Suppressive fire ability - trades damage for suppression chance.
/// </summary>
public static class SuppressiveFireAbility
{
    public const string AbilityId = "suppressive_fire";
    
    // Balance constants
    public const float DamageMultiplier = 0.5f;      // Half damage
    public const int AmmoConsumption = 5;            // Uses 5 rounds
    public const float HitSuppressionChance = 1.0f;  // 100% on hit
    public const float NearMissSuppressionChance = 0.7f;  // 70% on near miss
    public const float FarMissSuppressionChance = 0.3f;   // 30% on far miss
    public const float NearMissRadius = 2f;          // Tiles for "near miss"
    public const int Cooldown = 40;                  // 2 seconds
    
    public static AbilityData GetAbilityData()
    {
        return new AbilityData
        {
            Id = AbilityId,
            Name = "Suppressive Fire",
            Description = "Lay down suppressing fire. Reduced damage but high chance to suppress.",
            Range = 0,  // Uses weapon range
            Cooldown = Cooldown,
            Delay = 0,
            TargetType = TargetType.Actor,
            EffectType = EffectType.Custom,
            CustomHandler = "suppressive_fire"
        };
    }
}
```

#### Step 2.2: Create SuppressionSystem

**New File**: `src/sim/combat/systems/SuppressionSystem.cs`

```csharp
using Godot;
using System;

namespace FringeTactics;

/// <summary>
/// Handles suppressive fire execution and suppression application.
/// </summary>
public class SuppressionSystem
{
    private readonly CombatState combatState;
    
    public event Action<Actor, Actor, bool> SuppressionApplied; // attacker, target, wasHit
    public event Action<Actor, Vector2I> AreaSuppressionFired;  // attacker, targetTile
    
    public SuppressionSystem(CombatState combatState)
    {
        this.combatState = combatState;
    }
    
    /// <summary>
    /// Execute suppressive fire against a target actor.
    /// </summary>
    public SuppressionResult ExecuteSuppressiveFire(Actor attacker, Actor target)
    {
        var result = new SuppressionResult
        {
            AttackerId = attacker.Id,
            TargetId = target.Id
        };
        
        // Check if can attack
        if (!CombatResolver.CanAttack(attacker, target, attacker.EquippedWeapon, combatState.MapState))
        {
            result.Success = false;
            return result;
        }
        
        // Consume extra ammo
        var ammoToConsume = Math.Min(SuppressiveFireAbility.AmmoConsumption, attacker.CurrentMagazine);
        for (int i = 0; i < ammoToConsume; i++)
        {
            attacker.ConsumeAmmo();
        }
        
        // Roll for hit with reduced accuracy (suppressive fire is less accurate)
        var hitChance = CombatResolver.CalculateHitChance(
            attacker, target, attacker.EquippedWeapon, combatState.MapState) * 0.8f;
        var roll = combatState.Rng.NextFloat();
        result.Hit = roll < hitChance;
        result.HitChance = hitChance;
        
        if (result.Hit)
        {
            // Reduced damage
            var baseDamage = attacker.EquippedWeapon.Damage;
            var damage = (int)(baseDamage * SuppressiveFireAbility.DamageMultiplier);
            damage = CombatResolver.CalculateDamage(damage, target.Armor);
            
            target.TakeDamage(damage);
            result.Damage = damage;
            
            // Always suppress on hit
            ApplySuppression(target);
            result.Suppressed = true;
        }
        else
        {
            // Check for near miss suppression
            var distance = CombatResolver.GetDistance(attacker.GridPosition, target.GridPosition);
            var suppressionChance = distance <= SuppressiveFireAbility.NearMissRadius
                ? SuppressiveFireAbility.NearMissSuppressionChance
                : SuppressiveFireAbility.FarMissSuppressionChance;
            
            if (combatState.Rng.NextFloat() < suppressionChance)
            {
                ApplySuppression(target);
                result.Suppressed = true;
            }
        }
        
        result.Success = true;
        attacker.RecordShot(result.Hit, result.Damage);
        
        SimLog.Log($"[Suppression] {attacker.Type}#{attacker.Id} suppressive fire on {target.Type}#{target.Id}: " +
                   $"Hit={result.Hit}, Damage={result.Damage}, Suppressed={result.Suppressed}");
        
        SuppressionApplied?.Invoke(attacker, target, result.Hit);
        return result;
    }
    
    /// <summary>
    /// Execute area suppression on a tile.
    /// </summary>
    public void ExecuteAreaSuppression(Actor attacker, Vector2I targetTile, int radius = 2)
    {
        SimLog.Log($"[Suppression] {attacker.Type}#{attacker.Id} area suppression at {targetTile}");
        
        // Consume ammo
        var ammoToConsume = Math.Min(SuppressiveFireAbility.AmmoConsumption * 2, attacker.CurrentMagazine);
        for (int i = 0; i < ammoToConsume; i++)
        {
            attacker.ConsumeAmmo();
        }
        
        // Check all actors in radius
        foreach (var actor in combatState.Actors)
        {
            if (actor.Id == attacker.Id) continue;
            if (actor.State != ActorState.Alive) continue;
            if (actor.Type == attacker.Type) continue; // Don't suppress allies
            
            var distance = CombatResolver.GetDistance(targetTile, actor.GridPosition);
            if (distance > radius) continue;
            
            // Check LOS from attacker to actor
            if (!CombatResolver.HasLineOfSight(attacker.GridPosition, actor.GridPosition, combatState.MapState))
                continue;
            
            // Apply suppression with distance falloff
            var suppressionChance = 0.8f - (distance * 0.15f);
            if (combatState.Rng.NextFloat() < suppressionChance)
            {
                ApplySuppression(actor);
                SimLog.Log($"[Suppression] {actor.Type}#{actor.Id} suppressed by area fire");
            }
        }
        
        AreaSuppressionFired?.Invoke(attacker, targetTile);
    }
    
    /// <summary>
    /// Apply suppression effect to a target.
    /// </summary>
    public void ApplySuppression(Actor target)
    {
        // Refresh or apply suppression
        target.Effects.Apply(SuppressedEffect.EffectId, SuppressedEffect.DefaultDuration);
    }
    
    /// <summary>
    /// Check if an actor can use suppressive fire.
    /// </summary>
    public bool CanSuppressiveFire(Actor actor)
    {
        if (actor.State != ActorState.Alive) return false;
        if (actor.CurrentMagazine < SuppressiveFireAbility.AmmoConsumption) return false;
        if (actor.IsReloading) return false;
        if (actor.IsChanneling) return false;
        return true;
    }
}

public class SuppressionResult
{
    public int AttackerId { get; set; }
    public int TargetId { get; set; }
    public bool Success { get; set; }
    public bool Hit { get; set; }
    public int Damage { get; set; }
    public float HitChance { get; set; }
    public bool Suppressed { get; set; }
}
```

**Acceptance Criteria**:
- [ ] Suppressive fire consumes extra ammo
- [ ] Hit deals reduced damage but always suppresses
- [ ] Near miss has high suppression chance
- [ ] Far miss has lower suppression chance
- [ ] Area suppression affects multiple targets

#### Step 2.3: Integrate SuppressionSystem into CombatState

**File**: `src/sim/combat/state/CombatState.cs`

```csharp
public SuppressionSystem Suppression { get; private set; }

public CombatState(int seed)
{
    // ... existing code ...
    Suppression = new SuppressionSystem(this);
}
```

**Acceptance Criteria**:
- [ ] `CombatState.Suppression` property exists
- [ ] System is initialized in constructor

---

### Phase 3: Prevent Overwatch While Suppressed (Priority: High)

#### Step 3.1: Update Actor.EnterOverwatch

**File**: `src/sim/combat/state/Actor.cs`

```csharp
public void EnterOverwatch(int currentTick, Vector2I? facingDirection = null)
{
    if (State != ActorState.Alive || !CanFire()) return;
    
    // Cannot enter overwatch while suppressed
    if (IsSuppressed())
    {
        SimLog.Log($"[Actor] {Type}#{Id} cannot enter overwatch - suppressed!");
        return;
    }
    
    // ... rest of existing code ...
}
```

**Acceptance Criteria**:
- [ ] Suppressed units cannot enter overwatch
- [ ] Appropriate feedback when blocked

---

### Phase 4: Player Commands (Priority: High)

#### Step 4.1: Add Suppressive Fire to MissionView

**File**: `src/scenes/mission/MissionView.cs`

```csharp
// Keyboard shortcut for suppressive fire (e.g., 'S' key when targeting)
private bool suppressiveFireMode = false;

private void HandleSuppressiveFireCommand()
{
    if (selectedActors.Count == 0) return;
    
    var canSuppress = selectedActors.Any(a => 
        a.Type == ActorType.Crew && 
        CombatState.Suppression.CanSuppressiveFire(a));
    
    if (!canSuppress)
    {
        ShowFeedback("Not enough ammo for suppressive fire!");
        return;
    }
    
    suppressiveFireMode = true;
    SetCursorMode(CursorMode.SuppressiveTarget);
}

private void HandleSuppressiveFireTarget(Vector2I targetTile)
{
    if (!suppressiveFireMode) return;
    
    var targetActor = CombatState.GetActorAtPosition(targetTile);
    
    foreach (var actor in selectedActors)
    {
        if (actor.Type != ActorType.Crew) continue;
        if (!CombatState.Suppression.CanSuppressiveFire(actor)) continue;
        
        if (targetActor != null && targetActor.Type == ActorType.Enemy)
        {
            // Target suppression
            CombatState.Suppression.ExecuteSuppressiveFire(actor, targetActor);
        }
        else
        {
            // Area suppression
            CombatState.Suppression.ExecuteAreaSuppression(actor, targetTile);
        }
        
        break; // Only one actor suppresses per click
    }
    
    suppressiveFireMode = false;
    SetCursorMode(CursorMode.Normal);
}
```

**Add UI button** for suppressive fire in ability bar.

**Acceptance Criteria**:
- [ ] 'S' key or button enters suppressive fire mode
- [ ] Click on enemy executes targeted suppression
- [ ] Click on tile executes area suppression
- [ ] Feedback when not enough ammo

---

### Phase 5: AI Suppression Usage (Priority: High)

#### Step 5.1: Update AIController for Suppression

**File**: `src/sim/combat/systems/AIController.cs`

```csharp
private void Think(Actor enemy)
{
    // ... existing detection check ...
    
    // Consider suppression tactics
    if (ShouldUseSuppression(enemy))
    {
        var suppressTarget = FindSuppressionTarget(enemy);
        if (suppressTarget != null)
        {
            combatState.Suppression.ExecuteSuppressiveFire(enemy, suppressTarget);
            SimLog.Log($"[AI] Enemy#{enemy.Id} using suppressive fire on {suppressTarget.Type}#{suppressTarget.Id}");
            return;
        }
    }
    
    // ... existing attack/move logic ...
}

private bool ShouldUseSuppression(Actor enemy)
{
    // Use suppression when:
    // 1. Have enough ammo
    // 2. Target is in cover (hard to hit normally)
    // 3. Allies are trying to flank
    // 4. Random chance for variety
    
    if (!combatState.Suppression.CanSuppressiveFire(enemy)) return false;
    
    // 20% base chance to use suppression
    return combatState.Rng.NextFloat() < 0.2f;
}

private Actor FindSuppressionTarget(Actor enemy)
{
    Actor bestTarget = null;
    float bestScore = 0f;
    
    foreach (var crew in combatState.Actors)
    {
        if (crew.Type != ActorType.Crew || crew.State != ActorState.Alive) continue;
        if (!CombatResolver.HasLineOfSight(enemy.GridPosition, crew.GridPosition, combatState.MapState))
            continue;
        
        var score = ScoreSuppressionTarget(enemy, crew);
        if (score > bestScore)
        {
            bestScore = score;
            bestTarget = crew;
        }
    }
    
    return bestTarget;
}

private float ScoreSuppressionTarget(Actor enemy, Actor target)
{
    var score = 0f;
    
    // Prefer targets in cover (suppression is more valuable)
    var coverHeight = combatState.MapState.GetCoverAgainst(target.GridPosition, enemy.GridPosition);
    if (coverHeight != CoverHeight.None)
    {
        score += 0.5f;
    }
    
    // Prefer targets on overwatch (disable their threat)
    if (target.IsOnOverwatch)
    {
        score += 0.8f;
    }
    
    // Prefer targets that are threats
    if (target.AttackTargetId == enemy.Id)
    {
        score += 0.3f;
    }
    
    return score;
}
```

**Acceptance Criteria**:
- [ ] AI uses suppression tactically
- [ ] AI prioritizes suppressing overwatching units
- [ ] AI suppresses units in cover

---

### Phase 6: Visual Feedback (Priority: High)

#### Step 6.1: Suppression Visual Effects

**File**: `src/scenes/mission/ActorView.cs`

```csharp
private Label suppressedIcon;
private ColorRect suppressionOverlay;

private void CreateSuppressionIndicators()
{
    // Suppressed icon
    suppressedIcon = new Label();
    suppressedIcon.Text = "⚡"; // Or custom icon
    suppressedIcon.Position = new Vector2(GridConstants.TileSize - 12, -16);
    suppressedIcon.Visible = false;
    AddChild(suppressedIcon);
    
    // Suppression overlay (screen shake alternative)
    suppressionOverlay = new ColorRect();
    suppressionOverlay.Size = new Vector2(GridConstants.TileSize, GridConstants.TileSize);
    suppressionOverlay.Color = new Color(1f, 0.5f, 0f, 0.3f); // Orange tint
    suppressionOverlay.Visible = false;
    AddChild(suppressionOverlay);
}

public void UpdateSuppressionDisplay(bool isSuppressed)
{
    suppressedIcon.Visible = isSuppressed;
    suppressionOverlay.Visible = isSuppressed;
}
```

#### Step 6.2: Suppressive Fire Tracer Effect

**File**: `src/scenes/mission/MissionView.cs`

```csharp
private void OnSuppressionApplied(Actor attacker, Actor target, bool wasHit)
{
    // Show multiple tracer lines (burst fire visual)
    var attackerView = GetActorView(attacker.Id);
    var targetView = GetActorView(target.Id);
    
    ShowSuppressiveFireEffect(attackerView, targetView, wasHit);
}

private void ShowSuppressiveFireEffect(ActorView attacker, ActorView target, bool wasHit)
{
    // Create multiple tracer lines with slight spread
    for (int i = 0; i < 5; i++)
    {
        var spread = new Vector2(
            (float)GD.RandRange(-10, 10),
            (float)GD.RandRange(-10, 10)
        );
        CreateTracerLine(attacker.GlobalPosition, target.GlobalPosition + spread, 
                        wasHit ? Colors.Yellow : Colors.Orange);
    }
}
```

**Acceptance Criteria**:
- [ ] Suppressed units show clear visual indicator
- [ ] Suppressive fire shows burst tracer effect
- [ ] Area suppression shows impact zone

---

## Testing Checklist

### Manual Testing

1. **Suppressive Fire Execution**
   - [ ] Select crew, press 'S', click enemy → suppressive fire executes
   - [ ] Multiple tracers show (burst effect)
   - [ ] Ammo consumed (5 rounds)
   - [ ] Reduced damage dealt

2. **Suppression Application**
   - [ ] Hit always suppresses target
   - [ ] Near miss often suppresses
   - [ ] Far miss sometimes suppresses
   - [ ] Suppressed icon appears

3. **Suppression Effects**
   - [ ] Suppressed unit moves slower
   - [ ] Suppressed unit has reduced accuracy
   - [ ] Suppressed unit cannot enter overwatch
   - [ ] Suppressed unit's overwatch is less effective

4. **Area Suppression**
   - [ ] Click on empty tile → area suppression
   - [ ] Multiple enemies in area can be suppressed
   - [ ] Uses more ammo than targeted

5. **AI Suppression**
   - [ ] Enemies use suppression tactically
   - [ ] Enemies prioritize suppressing overwatching crew
   - [ ] Suppression enables AI flanking

6. **Suppression + Overwatch Interaction**
   - [ ] Suppressed overwatcher has reduced accuracy
   - [ ] Cannot enter overwatch while suppressed
   - [ ] Suppression duration allows movement window

### Automated Tests

Create `tests/sim/combat/HH2Tests.cs`:

```csharp
[TestSuite]
public class HH2Tests
{
    // === Suppression Effect ===
    [TestCase] SuppressedEffect_AppliesAccuracyPenalty()
    [TestCase] SuppressedEffect_AppliesSpeedPenalty()
    [TestCase] SuppressedEffect_AppliesOverwatchPenalty()
    [TestCase] SuppressedEffect_ExpiresAfterDuration()
    
    // === Suppressive Fire ===
    [TestCase] SuppressiveFire_ConsumesExtraAmmo()
    [TestCase] SuppressiveFire_DealsReducedDamage()
    [TestCase] SuppressiveFire_HitAlwaysSuppresses()
    [TestCase] SuppressiveFire_NearMissOftenSuppresses()
    [TestCase] SuppressiveFire_RequiresMinimumAmmo()
    
    // === Area Suppression ===
    [TestCase] AreaSuppression_AffectsMultipleTargets()
    [TestCase] AreaSuppression_RespectsCover()
    [TestCase] AreaSuppression_HasDistanceFalloff()
    
    // === Overwatch Interaction ===
    [TestCase] Suppressed_CannotEnterOverwatch()
    [TestCase] Suppressed_OverwatchHasReducedAccuracy()
    
    // === AI ===
    [TestCase] AI_UsesSuppression_WhenAppropriate()
    [TestCase] AI_PrioritizesSuppressing_Overwatchers()
}
```

---

## Implementation Order

1. **Day 1: Enhanced Effect**
   - Step 1.1: Update SuppressedEffect
   - Step 1.2: Add OverwatchAccuracy stat

2. **Day 2: Suppression System**
   - Step 2.1: Create SuppressiveFireAbility
   - Step 2.2: Create SuppressionSystem
   - Step 2.3: Integrate into CombatState

3. **Day 3: Overwatch Integration**
   - Step 3.1: Update Actor.EnterOverwatch
   - Update OverwatchSystem for accuracy modifier

4. **Day 4: Player Commands & AI**
   - Step 4.1: Add to MissionView
   - Step 5.1: Update AIController

5. **Day 5: Visuals & Testing**
   - Step 6.1: Suppression indicators
   - Step 6.2: Tracer effects
   - Write tests, manual testing

---

## Success Criteria for HH2

When HH2 is complete:

1. ✅ Player can order suppressive fire
2. ✅ Suppression applies even on misses
3. ✅ Suppressed units have clear penalties
4. ✅ Suppression interacts with overwatch
5. ✅ AI uses suppression tactically
6. ✅ Visual feedback is clear
7. ✅ All automated tests pass

**Natural Pause Point**: Combat is no longer just "who kills first" but also "who locks whom down". This enables the "pin and flank" tactics central to the Hangar Handover's pressure phase.

---

## Files to Create/Modify

### New Files
- `src/sim/combat/abilities/SuppressiveFireAbility.cs`
- `src/sim/combat/systems/SuppressionSystem.cs`
- `tests/sim/combat/HH2Tests.cs`

### Modified Files
- `src/sim/combat/effects/SuppressedEffect.cs` - Enhanced penalties
- `src/sim/combat/modifiers/StatType.cs` - Add OverwatchAccuracy
- `src/sim/combat/state/Actor.cs` - Block overwatch when suppressed
- `src/sim/combat/state/CombatState.cs` - Add SuppressionSystem
- `src/sim/combat/systems/OverwatchSystem.cs` - Use overwatch accuracy
- `src/sim/combat/systems/AIController.cs` - Suppression decisions
- `src/scenes/mission/MissionView.cs` - Suppression commands
- `src/scenes/mission/ActorView.cs` - Suppression visuals

---

## Dependencies

- **Requires**: HH1 (overwatch system for interaction)
- **Enables**: HH5 (AI roles use suppression tactically)

---

## Open Questions

1. **Suppression stacking**: Should multiple suppression sources extend duration?
   - *Decision*: Yes, refresh duration but don't stack penalties.

2. **Friendly suppression**: Can you suppress allies?
   - *Decision*: No, suppression only affects enemies.

3. **Suppression and cover**: Does cover reduce suppression chance?
   - *Decision*: No, suppression represents psychological pressure, not physical hits.

4. **Auto-suppression**: Should sustained fire automatically suppress?
   - *Decision*: Future feature. For HH2, suppression requires explicit action.
