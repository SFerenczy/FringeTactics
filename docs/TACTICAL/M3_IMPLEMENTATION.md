# M3 – Basic Combat Loop (No Cover): Implementation Plan

This document breaks down **Milestone 3** from `ROADMAP.md` into concrete implementation steps.

**Goal**: Introduce lethal interactions: shooting, health, death, and simple enemies.

---

## Current State Assessment

### What We Have (From M0–M2)

| Component | Status | Notes |
|-----------|--------|-------|
| `CombatState` | ✅ Complete | Manages actors, processes attacks, win/lose conditions |
| `CombatResolver` | ✅ Complete | `CanAttack()`, `ResolveAttack()`, LOS checks, flat 70% hit chance |
| `Actor` | ✅ Complete | HP, `TakeDamage()`, `CanFire()`, `AttackCooldown`, `EquippedWeapon` |
| `WeaponData` | ✅ Complete | Range, Damage, CooldownTicks, Accuracy (unused) |
| `AIController` | ✅ Complete | Enemies find targets, attack in range, move toward targets |
| `VisibilitySystem` | ✅ Complete | Fog of war, LOS-based visibility |
| `MissionView` | ✅ Complete | Attack orders, enemy visibility in fog |

### What M3 Requires vs What We Have

| M3 Requirement | Current Status | Gap |
|----------------|----------------|-----|
| Unit HP | ✅ Complete | `Actor.Hp`, `Actor.MaxHp` exist |
| Basic ballistic weapon | ✅ Complete | `WeaponData` with Range, Damage, Cooldown |
| Attack commands on visible enemies | ✅ Complete | `IssueAttackOrder()` works |
| Hit chance based on distance + accuracy | ✅ Complete | `CalculateHitChance()` with distance falloff |
| Damage reduces HP; 0 HP = death | ✅ Complete | `TakeDamage()` handles death |
| Magazine + reserve ammo | ✅ Complete | `CurrentMagazine`, `ReserveAmmo` on Actor |
| Reload action with time cost | ✅ Complete | `StartReload()`, `ReloadTicks` duration |
| Auto-defend (return fire) | ❌ Missing | Units don't auto-retaliate |
| Simple enemy AI | ✅ Complete | `AIController` moves and attacks |

---

## Architecture Decisions

### Hit Chance Formula

**Decision**: Distance-based accuracy falloff with weapon accuracy modifier.

**Formula**:
```
hitChance = weaponAccuracy * (1 - distancePenalty)
distancePenalty = (distance / weaponRange) * RANGE_PENALTY_FACTOR
```

**Rationale**:
- Weapons should be more accurate at close range
- Different weapons have different effective ranges
- Keeps combat lethal but rewards positioning

**Parameters** (tunable):
- `RANGE_PENALTY_FACTOR = 0.3f` — At max range, 30% penalty
- `MIN_HIT_CHANCE = 0.10f` — Floor to prevent impossible shots
- `MAX_HIT_CHANCE = 0.95f` — Cap to prevent guaranteed hits

### Ammo System Design

**Decision**: Magazine-based ammo with reserve pool.

**Structure**:
```csharp
// Per-weapon ammo state (on Actor, not WeaponData)
public int CurrentMagazine { get; set; }  // Rounds in current mag
public int ReserveAmmo { get; set; }      // Total spare rounds

// WeaponData additions
public int MagazineSize { get; set; }     // Max rounds per mag
public int ReloadTicks { get; set; }      // Time to reload
```

**Rationale**:
- Simple and intuitive for players
- Creates tactical decisions (when to reload)
- Ammo scarcity adds tension per design doc

### Auto-Defend Behavior

**Decision**: Units automatically return fire when attacked, if able.

**Rules**:
1. Triggered when unit takes damage from an attack
2. Only fires if: has LOS to attacker, in range, has ammo, not on cooldown
3. Does NOT interrupt current orders (just queues a shot)
4. Can be toggled per-unit in future (Rules of Engagement - [PLUS])

**Implementation**: Event-driven, triggered by `DamageTaken` event.

---

## Implementation Steps

### Phase 1: Hit Chance Improvements (Priority: Critical)

The current flat 70% hit chance doesn't create interesting combat. Distance and accuracy should matter.

#### Step 1.1: Add Distance-Based Hit Chance to CombatResolver

**File**: `src/sim/combat/CombatResolver.cs`

**Current Code**:
```csharp
// Simple flat hit chance for now
var roll = rng.NextDouble();
result.Hit = roll < BASE_HIT_CHANCE;
```

**New Implementation**:

```csharp
public const float RANGE_PENALTY_FACTOR = 0.3f;
public const float MIN_HIT_CHANCE = 0.10f;
public const float MAX_HIT_CHANCE = 0.95f;

/// <summary>
/// Calculate hit chance based on distance and weapon accuracy.
/// </summary>
public static float CalculateHitChance(Actor attacker, Actor target, WeaponData weapon)
{
    var distance = GetDistance(attacker.GridPosition, target.GridPosition);
    
    // Base accuracy from weapon
    var baseAccuracy = weapon.Accuracy;
    
    // Distance penalty: increases as you approach max range
    var rangeFraction = distance / weapon.Range;
    var distancePenalty = rangeFraction * RANGE_PENALTY_FACTOR;
    
    // Apply attacker's aim stat bonus (+1% per point)
    var aimBonus = attacker.Stats.GetValueOrDefault("aim", 0) * 0.01f;
    
    // Final calculation
    var hitChance = baseAccuracy * (1f - distancePenalty) + aimBonus;
    
    // Clamp to valid range
    return Mathf.Clamp(hitChance, MIN_HIT_CHANCE, MAX_HIT_CHANCE);
}

/// <summary>
/// Resolve an attack with distance-based hit chance.
/// </summary>
public static AttackResult ResolveAttack(Actor attacker, Actor target, WeaponData weapon, MapState map, Random rng)
{
    var result = new AttackResult
    {
        AttackerId = attacker.Id,
        TargetId = target.Id,
        WeaponName = weapon.Name
    };

    if (!CanAttack(attacker, target, weapon, map))
    {
        result.Hit = false;
        result.Damage = 0;
        return result;
    }

    var hitChance = CalculateHitChance(attacker, target, weapon);
    result.HitChance = hitChance; // Store for UI feedback
    
    var roll = (float)rng.NextDouble();
    result.Hit = roll < hitChance;

    if (result.Hit)
    {
        result.Damage = weapon.Damage;
    }
    else
    {
        result.Damage = 0;
    }

    return result;
}
```

**Update AttackResult**:
```csharp
public struct AttackResult
{
    public int AttackerId { get; set; }
    public int TargetId { get; set; }
    public string WeaponName { get; set; }
    public bool Hit { get; set; }
    public int Damage { get; set; }
    public float HitChance { get; set; } // New: for UI feedback
}
```

**Acceptance Criteria**:
- [ ] Hit chance decreases with distance
- [ ] Weapon accuracy affects hit chance
- [ ] Actor aim stat provides bonus
- [ ] Hit chance clamped between 10% and 95%
- [ ] `AttackResult.HitChance` populated for UI

---

#### Step 1.2: Update Weapon Definitions for Balance

**File**: `src/sim/data/Definitions.cs`

**Current weapons need tuning for distance-based combat**:

```csharp
["rifle"] = new WeaponDef
{
    Id = "rifle",
    Name = "Assault Rifle",
    Damage = 25,
    Range = 8,
    CooldownTicks = 10,  // 0.5 sec at 20 ticks/sec
    Accuracy = 0.70f
},
["pistol"] = new WeaponDef
{
    Id = "pistol",
    Name = "Pistol",
    Damage = 15,
    Range = 5,
    CooldownTicks = 6,   // 0.3 sec
    Accuracy = 0.75f     // More accurate at short range
},
["smg"] = new WeaponDef
{
    Id = "smg",
    Name = "SMG",
    Damage = 18,
    Range = 6,
    CooldownTicks = 5,   // 0.25 sec - fast fire
    Accuracy = 0.55f     // Less accurate, compensated by fire rate
},
["shotgun"] = new WeaponDef
{
    Id = "shotgun",
    Name = "Shotgun",
    Damage = 45,         // High damage
    Range = 4,           // Short range
    CooldownTicks = 18,  // 0.9 sec - slow
    Accuracy = 0.85f     // Very accurate at short range
}
```

**Acceptance Criteria**:
- [ ] Weapons have distinct accuracy profiles
- [ ] Balance feels right: rifles versatile, pistols backup, SMGs suppressive, shotguns devastating close

---

### Phase 2: Ammunition System (Priority: High)

Ammo scarcity is a core design pillar. Running low mid-mission should be a real risk.

#### Step 2.1: Add Ammo Properties to WeaponDef and WeaponData

**File**: `src/sim/data/Definitions.cs`

```csharp
public class WeaponDef
{
    public string Id { get; set; }
    public string Name { get; set; }
    public int Damage { get; set; }
    public int Range { get; set; }
    public int CooldownTicks { get; set; }
    public float Accuracy { get; set; } = 0.70f;
    
    // New: Ammo properties
    public int MagazineSize { get; set; } = 30;
    public int ReloadTicks { get; set; } = 40; // 2 seconds at 20 ticks/sec
}
```

**Update weapon definitions**:
```csharp
["rifle"] = new WeaponDef
{
    Id = "rifle",
    Name = "Assault Rifle",
    Damage = 25,
    Range = 8,
    CooldownTicks = 10,
    Accuracy = 0.70f,
    MagazineSize = 30,
    ReloadTicks = 40  // 2 sec reload
},
["pistol"] = new WeaponDef
{
    Id = "pistol",
    Name = "Pistol",
    Damage = 15,
    Range = 5,
    CooldownTicks = 6,
    Accuracy = 0.75f,
    MagazineSize = 12,
    ReloadTicks = 20  // 1 sec reload
},
["smg"] = new WeaponDef
{
    Id = "smg",
    Name = "SMG",
    Damage = 18,
    Range = 6,
    CooldownTicks = 5,
    Accuracy = 0.55f,
    MagazineSize = 25,
    ReloadTicks = 30  // 1.5 sec reload
},
["shotgun"] = new WeaponDef
{
    Id = "shotgun",
    Name = "Shotgun",
    Damage = 45,
    Range = 4,
    CooldownTicks = 18,
    Accuracy = 0.85f,
    MagazineSize = 6,
    ReloadTicks = 60  // 3 sec reload (pump action)
}
```

**File**: `src/sim/combat/WeaponData.cs`

```csharp
public struct WeaponData
{
    public string Id { get; set; }
    public string Name { get; set; }
    public int Range { get; set; }
    public int Damage { get; set; }
    public int CooldownTicks { get; set; }
    public float Accuracy { get; set; }
    
    // New: Ammo properties
    public int MagazineSize { get; set; }
    public int ReloadTicks { get; set; }

    public static WeaponData FromDef(WeaponDef def) => new()
    {
        Id = def.Id,
        Name = def.Name,
        Range = def.Range,
        Damage = def.Damage,
        CooldownTicks = def.CooldownTicks,
        Accuracy = def.Accuracy,
        MagazineSize = def.MagazineSize,
        ReloadTicks = def.ReloadTicks
    };
    
    // ... rest unchanged
}
```

**Acceptance Criteria**:
- [ ] `WeaponDef` has `MagazineSize` and `ReloadTicks`
- [ ] `WeaponData.FromDef()` copies ammo properties
- [ ] All weapons have sensible ammo values

---

#### Step 2.2: Add Ammo State to Actor

**File**: `src/sim/combat/Actor.cs`

**New properties**:
```csharp
// Ammo state
public int CurrentMagazine { get; set; }
public int ReserveAmmo { get; set; }
public bool IsReloading { get; private set; } = false;
public int ReloadProgress { get; private set; } = 0; // ticks remaining

// Constants for default ammo
public const int DEFAULT_RESERVE_AMMO = 90; // 3 extra magazines worth
```

**Update constructor**:
```csharp
public Actor(int actorId, string actorType)
{
    // ... existing initialization ...
    
    // Initialize ammo from weapon
    CurrentMagazine = EquippedWeapon.MagazineSize;
    ReserveAmmo = DEFAULT_RESERVE_AMMO;
}
```

**New methods**:
```csharp
/// <summary>
/// Check if actor can fire (has ammo, not reloading, not on cooldown).
/// </summary>
public bool CanFire()
{
    return State == ActorState.Alive 
        && AttackCooldown <= 0 
        && CurrentMagazine > 0 
        && !IsReloading;
}

/// <summary>
/// Consume one round of ammo. Called when firing.
/// </summary>
public void ConsumeAmmo()
{
    if (CurrentMagazine > 0)
    {
        CurrentMagazine--;
    }
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
/// Cancel reload (e.g., when taking damage or receiving new order).
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
}
```

**Update `Tick()` to handle reload**:
```csharp
public void Tick(float tickDuration)
{
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

    // Handle movement (existing code)
    // ...
}
```

**Acceptance Criteria**:
- [ ] `Actor` tracks `CurrentMagazine` and `ReserveAmmo`
- [ ] `CanFire()` checks ammo
- [ ] `StartReload()` initiates reload with correct duration
- [ ] Reload completes and fills magazine
- [ ] Cannot move while reloading

---

#### Step 2.3: Integrate Ammo into Combat Flow

**File**: `src/sim/combat/CombatState.cs`

**Update `ProcessAttacks()`**:
```csharp
private void ProcessAttacks()
{
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
            attacker.SetAttackTarget(null);
            continue;
        }

        if (CombatResolver.CanAttack(attacker, target, attacker.EquippedWeapon, MapState))
        {
            var result = CombatResolver.ResolveAttack(attacker, target, attacker.EquippedWeapon, MapState, Rng.GetRandom());
            attacker.StartCooldown();
            attacker.ConsumeAmmo(); // Consume ammo on shot

            // ... rest of attack processing unchanged ...
        }
    }
}
```

**Acceptance Criteria**:
- [ ] Firing consumes ammo
- [ ] Auto-reload when magazine empty
- [ ] Cannot fire with empty magazine

---

#### Step 2.4: Add Manual Reload Command

**File**: `src/sim/combat/CombatState.cs`

```csharp
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

    SimLog.Log($"[Combat] {actor.Type}#{actor.Id} manually reloading");
    actor.StartReload();
}
```

**Acceptance Criteria**:
- [ ] `IssueReloadOrder()` starts reload
- [ ] Cannot reload if magazine full or no reserve

---

### Phase 3: Auto-Defend System (Priority: High)

Units should automatically return fire when attacked, per design doc.

#### Step 3.1: Implement Auto-Defend in CombatState

**File**: `src/sim/combat/CombatState.cs`

**New event handler approach**:

```csharp
// In constructor, subscribe to damage events
public CombatState(int seed)
{
    // ... existing initialization ...
    
    // Subscribe to actor damage for auto-defend
    // Note: We'll handle this in ProcessAttacks instead for simplicity
}

/// <summary>
/// Queue an auto-defend shot from defender against attacker.
/// Called after an attack resolves.
/// </summary>
private void TriggerAutoDefend(Actor defender, Actor attacker)
{
    // Only crew auto-defend (enemies use AI)
    if (defender.Type != "crew")
    {
        return;
    }
    
    // Can't defend if dead or incapacitated
    if (defender.State != ActorState.Alive)
    {
        return;
    }
    
    // Can't defend if already has an attack target
    if (defender.AttackTargetId.HasValue)
    {
        return;
    }
    
    // Can't defend if can't fire
    if (!defender.CanFire())
    {
        return;
    }
    
    // Check if can attack back
    if (!CombatResolver.CanAttack(defender, attacker, defender.EquippedWeapon, MapState))
    {
        return;
    }
    
    // Queue return fire
    SimLog.Log($"[Combat] {defender.Type}#{defender.Id} auto-defending against {attacker.Type}#{attacker.Id}");
    defender.SetAttackTarget(attacker.Id);
}
```

**Update `ProcessAttacks()` to trigger auto-defend**:
```csharp
private void ProcessAttacks()
{
    // Collect auto-defend triggers to process after all attacks
    var autoDefendTriggers = new List<(Actor defender, Actor attacker)>();
    
    foreach (var attacker in Actors)
    {
        // ... existing attack processing ...
        
        if (CombatResolver.CanAttack(attacker, target, attacker.EquippedWeapon, MapState))
        {
            var result = CombatResolver.ResolveAttack(/* ... */);
            // ... existing damage processing ...
            
            // Queue auto-defend if target survived and was hit
            if (result.Hit && target.State == ActorState.Alive)
            {
                autoDefendTriggers.Add((target, attacker));
            }
            
            // ... rest of processing ...
        }
    }
    
    // Process auto-defend triggers
    foreach (var (defender, attacker) in autoDefendTriggers)
    {
        TriggerAutoDefend(defender, attacker);
    }
}
```

**Acceptance Criteria**:
- [ ] Crew units return fire when hit
- [ ] Auto-defend respects LOS and range
- [ ] Auto-defend doesn't interrupt existing orders
- [ ] Dead units don't auto-defend

---

### Phase 4: Enemy AI Improvements (Priority: Medium)

The AI exists but needs polish for M3.

#### Step 4.1: AI Ammo Awareness

**File**: `src/sim/combat/AIController.cs`

**Update `Think()` to handle ammo**:
```csharp
private void Think(Actor enemy)
{
    // Handle reload if needed
    if (enemy.NeedsReload())
    {
        enemy.StartReload();
        SimLog.Log($"[AI] Enemy#{enemy.Id} reloading");
        return;
    }
    
    // Can't act while reloading
    if (enemy.IsReloading)
    {
        return;
    }
    
    // ... rest of existing Think() logic ...
}
```

**Acceptance Criteria**:
- [ ] AI enemies reload when magazine empty
- [ ] AI doesn't try to attack while reloading

---

#### Step 4.2: AI Target Priority

**File**: `src/sim/combat/AIController.cs`

**Improve target selection**:
```csharp
private Actor FindBestTarget(Actor enemy)
{
    Actor bestTarget = null;
    float bestScore = float.MinValue;
    
    foreach (var actor in combatState.Actors)
    {
        if (actor.Type != "crew" || actor.State != ActorState.Alive)
        {
            continue;
        }
        
        // Must have LOS
        if (!CombatResolver.HasLineOfSight(enemy.GridPosition, actor.GridPosition, combatState.MapState))
        {
            continue;
        }
        
        // Score based on:
        // - Distance (closer = better)
        // - Health (lower = better, finish off wounded)
        // - Threat (attacking us = higher priority)
        
        var distance = CombatResolver.GetDistance(enemy.GridPosition, actor.GridPosition);
        var distanceScore = 1f / (distance + 1f); // Closer is better
        
        var healthScore = 1f - (actor.Hp / (float)actor.MaxHp); // Lower HP = higher score
        
        var threatScore = actor.AttackTargetId == enemy.Id ? 0.5f : 0f; // Bonus if targeting us
        
        var totalScore = distanceScore + healthScore * 0.3f + threatScore;
        
        if (totalScore > bestScore)
        {
            bestScore = totalScore;
            bestTarget = actor;
        }
    }
    
    return bestTarget;
}
```

**Acceptance Criteria**:
- [ ] AI prioritizes closer targets
- [ ] AI finishes off wounded targets
- [ ] AI prioritizes threats

---

### Phase 5: View Layer Updates (Priority: Medium)

#### Step 5.1: Ammo Display in UI

**File**: `src/scenes/mission/ActorView.cs`

Add ammo indicator to actor view:
```csharp
// New UI element
private Label ammoLabel;

private void CreateAmmoLabel()
{
    ammoLabel = new Label();
    ammoLabel.Position = new Vector2(0, -30);
    ammoLabel.AddThemeColorOverride("font_color", Colors.White);
    ammoLabel.AddThemeFontSizeOverride("font_size", 10);
    AddChild(ammoLabel);
}

public void UpdateAmmoDisplay(int current, int reserve)
{
    if (ammoLabel != null)
    {
        ammoLabel.Text = $"{current}/{reserve}";
        ammoLabel.Visible = true;
    }
}
```

**File**: `src/scenes/mission/MissionView.cs`

Update actor views with ammo:
```csharp
// In _Process() or dedicated update method
private void UpdateActorAmmoDisplays()
{
    foreach (var kvp in actorViews)
    {
        var actor = CombatState.GetActorById(kvp.Key);
        if (actor != null && actor.Type == "crew")
        {
            kvp.Value.UpdateAmmoDisplay(actor.CurrentMagazine, actor.ReserveAmmo);
        }
    }
}
```

**Acceptance Criteria**:
- [ ] Ammo count visible on crew units
- [ ] Updates when firing/reloading

---

#### Step 5.2: Reload Input Handling

**File**: `src/scenes/mission/MissionView.cs`

```csharp
// In _Input() or keyboard handling
if (@event is InputEventKey keyEvent && keyEvent.Pressed)
{
    if (keyEvent.Keycode == Key.R)
    {
        // Reload selected units
        foreach (var actorId in selectedActorIds)
        {
            CombatState.IssueReloadOrder(actorId);
        }
    }
}
```

**Acceptance Criteria**:
- [ ] R key triggers reload for selected units
- [ ] Only reloads units that need it

---

#### Step 5.3: Hit Chance Preview (Optional for M3)

**File**: `src/scenes/mission/MissionView.cs`

Show hit chance when hovering over enemy with units selected:
```csharp
private void UpdateHitChancePreview(Vector2I gridPos)
{
    var targetActor = CombatState.GetActorAtPosition(gridPos);
    if (targetActor == null || targetActor.Type == "crew")
    {
        HideHitChancePreview();
        return;
    }
    
    // Calculate average hit chance for selected units
    float totalChance = 0f;
    int validAttackers = 0;
    
    foreach (var actorId in selectedActorIds)
    {
        var attacker = CombatState.GetActorById(actorId);
        if (attacker != null && attacker.CanFire())
        {
            if (CombatResolver.CanAttack(attacker, targetActor, attacker.EquippedWeapon, CombatState.MapState))
            {
                totalChance += CombatResolver.CalculateHitChance(attacker, targetActor, attacker.EquippedWeapon);
                validAttackers++;
            }
        }
    }
    
    if (validAttackers > 0)
    {
        var avgChance = totalChance / validAttackers;
        ShowHitChancePreview(gridPos, avgChance);
    }
}
```

**Acceptance Criteria**:
- [ ] Hit chance shown when hovering over enemy
- [ ] Updates based on selected units

---

## Testing Checklist

### Test Mission Setup

Create a dedicated M3 test mission with combat scenarios.

**File**: `src/sim/data/MissionConfig.cs`

```csharp
/// <summary>
/// M3 test mission - basic combat testing.
/// Open arena with enemies at various ranges.
/// </summary>
public static MissionConfig CreateM3TestMission()
{
    return new MissionConfig
    {
        Id = "m3_test",
        Name = "M3 Test - Basic Combat",
        GridSize = new Vector2I(20, 16),
        MapTemplate = new string[]
        {
            "####################",
            "#..................#",
            "#.EE...............#",
            "#.EE...............#",
            "#..................#",
            "#..................#",
            "#......##..........#",
            "#......##..........#",
            "#..................#",
            "#..................#",
            "#..................#",
            "#..................#",
            "#..................#",
            "#..................#",
            "#..................#",
            "####################"
        },
        CrewWeaponId = "rifle",
        CrewSpawnPositions = new List<Vector2I>
        {
            new Vector2I(2, 2),
            new Vector2I(3, 2),
            new Vector2I(2, 3),
            new Vector2I(3, 3)
        },
        EnemySpawns = new List<EnemySpawn>
        {
            // Close range enemy (easy target)
            new EnemySpawn("grunt", new Vector2I(8, 4)),
            // Medium range enemy
            new EnemySpawn("grunt", new Vector2I(12, 6)),
            // Long range enemy (harder to hit)
            new EnemySpawn("gunner", new Vector2I(17, 10)),
            // Flanking enemy
            new EnemySpawn("grunt", new Vector2I(15, 2))
        }
    };
}
```

### Manual Testing

1. **Hit Chance**
   - [ ] Close range shots hit more often (~70-85%)
   - [ ] Long range shots hit less often (~40-60%)
   - [ ] Different weapons have different accuracy profiles
   - [ ] Aim stat bonus visible in hit chance

2. **Ammunition**
   - [ ] Ammo count displays on crew units
   - [ ] Firing decreases magazine count
   - [ ] Auto-reload triggers when magazine empty
   - [ ] R key manually reloads
   - [ ] Cannot fire during reload
   - [ ] Reload takes correct time
   - [ ] Reserve ammo decreases after reload

3. **Auto-Defend**
   - [ ] Crew units return fire when hit
   - [ ] Auto-defend respects LOS and range
   - [ ] Auto-defend doesn't fire if out of ammo
   - [ ] Auto-defend doesn't override existing attack order

4. **Enemy AI**
   - [ ] Enemies attack visible crew
   - [ ] Enemies reload when empty
   - [ ] Enemies prioritize closer/wounded targets
   - [ ] Enemies move toward targets out of range

5. **Combat Flow**
   - [ ] Units die at 0 HP
   - [ ] Dead units removed from play
   - [ ] Mission ends when all enemies dead (victory)
   - [ ] Mission ends when all crew dead (defeat)

6. **Edge Cases**
   - [ ] Out of ammo behavior (no reserve)
   - [ ] Multiple units attacking same target
   - [ ] Target dies mid-attack
   - [ ] Reload interrupted by death

### Automated Tests

Create `tests/sim/combat/M3Tests.cs`:

```csharp
using Godot;
using GdUnit4;
using System.Collections.Generic;
using static GdUnit4.Assertions;

namespace FringeTactics.Tests;

/// <summary>
/// M3 Milestone tests - validates basic combat loop.
/// </summary>
[TestSuite]
public class M3Tests
{
    // ========== Hit Chance Tests ==========

    [TestCase]
    [RequireGodotRuntime]
    public void HitChance_CloseRange_HigherThanLongRange()
    {
        var map = CreateOpenMap(20, 20);
        var combat = new CombatState();
        combat.MapState = map;

        var attacker = combat.AddActor("crew", new Vector2I(5, 5));
        var closeTarget = combat.AddActor("enemy", new Vector2I(7, 5)); // 2 tiles
        var farTarget = combat.AddActor("enemy", new Vector2I(12, 5));  // 7 tiles

        var closeChance = CombatResolver.CalculateHitChance(attacker, closeTarget, attacker.EquippedWeapon);
        var farChance = CombatResolver.CalculateHitChance(attacker, farTarget, attacker.EquippedWeapon);

        AssertThat(closeChance).IsGreater(farChance);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void HitChance_ClampedToValidRange()
    {
        var map = CreateOpenMap(20, 20);
        var combat = new CombatState();
        combat.MapState = map;

        var attacker = combat.AddActor("crew", new Vector2I(5, 5));
        attacker.Stats["aim"] = 100; // Absurdly high aim
        var target = combat.AddActor("enemy", new Vector2I(6, 5));

        var hitChance = CombatResolver.CalculateHitChance(attacker, target, attacker.EquippedWeapon);

        AssertThat(hitChance).IsLessEqual(CombatResolver.MAX_HIT_CHANCE);
        AssertThat(hitChance).IsGreaterEqual(CombatResolver.MIN_HIT_CHANCE);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void HitChance_AimStatProvidesBonus()
    {
        var map = CreateOpenMap(20, 20);
        var combat = new CombatState();
        combat.MapState = map;

        var normalAttacker = combat.AddActor("crew", new Vector2I(5, 5));
        var skilledAttacker = combat.AddActor("crew", new Vector2I(5, 6));
        skilledAttacker.Stats["aim"] = 10; // +10% bonus

        var target = combat.AddActor("enemy", new Vector2I(10, 5));

        var normalChance = CombatResolver.CalculateHitChance(normalAttacker, target, normalAttacker.EquippedWeapon);
        var skilledChance = CombatResolver.CalculateHitChance(skilledAttacker, target, skilledAttacker.EquippedWeapon);

        AssertThat(skilledChance).IsGreater(normalChance);
    }

    // ========== Ammo Tests ==========

    [TestCase]
    [RequireGodotRuntime]
    public void Actor_StartsWithFullMagazine()
    {
        var combat = new CombatState();
        var actor = combat.AddActor("crew", new Vector2I(5, 5));

        AssertThat(actor.CurrentMagazine).IsEqual(actor.EquippedWeapon.MagazineSize);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Actor_ConsumeAmmo_DecreasesMagazine()
    {
        var combat = new CombatState();
        var actor = combat.AddActor("crew", new Vector2I(5, 5));
        var initialAmmo = actor.CurrentMagazine;

        actor.ConsumeAmmo();

        AssertThat(actor.CurrentMagazine).IsEqual(initialAmmo - 1);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Actor_CannotFire_WhenMagazineEmpty()
    {
        var combat = new CombatState();
        var actor = combat.AddActor("crew", new Vector2I(5, 5));
        actor.CurrentMagazine = 0;

        AssertThat(actor.CanFire()).IsFalse();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Actor_NeedsReload_WhenEmptyWithReserve()
    {
        var combat = new CombatState();
        var actor = combat.AddActor("crew", new Vector2I(5, 5));
        actor.CurrentMagazine = 0;
        actor.ReserveAmmo = 30;

        AssertThat(actor.NeedsReload()).IsTrue();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Actor_Reload_FillsMagazineFromReserve()
    {
        var combat = new CombatState();
        var actor = combat.AddActor("crew", new Vector2I(5, 5));
        actor.CurrentMagazine = 0;
        actor.ReserveAmmo = 60;

        actor.StartReload();
        
        // Simulate reload completion
        for (int i = 0; i < actor.EquippedWeapon.ReloadTicks + 1; i++)
        {
            actor.Tick(0.05f);
        }

        AssertThat(actor.CurrentMagazine).IsEqual(actor.EquippedWeapon.MagazineSize);
        AssertThat(actor.ReserveAmmo).IsEqual(60 - actor.EquippedWeapon.MagazineSize);
        AssertThat(actor.IsReloading).IsFalse();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Actor_CannotFire_WhileReloading()
    {
        var combat = new CombatState();
        var actor = combat.AddActor("crew", new Vector2I(5, 5));
        actor.CurrentMagazine = 5; // Partial magazine
        actor.StartReload();

        AssertThat(actor.CanFire()).IsFalse();
    }

    // ========== Auto-Defend Tests ==========

    [TestCase]
    [RequireGodotRuntime]
    public void AutoDefend_CrewReturnsFire_WhenHit()
    {
        var map = CreateOpenMap(20, 20);
        var combat = new CombatState();
        combat.MapState = map;
        combat.InitializeVisibility();

        var crew = combat.AddActor("crew", new Vector2I(5, 5));
        var enemy = combat.AddActor("enemy", new Vector2I(8, 5));
        
        // Enemy attacks crew
        combat.IssueAttackOrder(enemy.Id, crew.Id);
        combat.TimeSystem.Resume();

        // Simulate until attack resolves
        for (int i = 0; i < 50; i++)
        {
            combat.Update(0.05f);
            
            // Check if crew has auto-targeted the enemy
            if (crew.AttackTargetId == enemy.Id)
            {
                break;
            }
        }

        // Crew should be targeting enemy (if hit and survived)
        if (crew.State == ActorState.Alive && crew.Hp < crew.MaxHp)
        {
            AssertThat(crew.AttackTargetId).IsEqual(enemy.Id);
        }
    }

    // ========== Combat Resolution Tests ==========

    [TestCase]
    [RequireGodotRuntime]
    public void Attack_ConsumesAmmo()
    {
        var map = CreateOpenMap(20, 20);
        var combat = new CombatState();
        combat.MapState = map;
        combat.InitializeVisibility();
        combat.SetHasEnemyObjective(true);

        var attacker = combat.AddActor("crew", new Vector2I(5, 5));
        var target = combat.AddActor("enemy", new Vector2I(8, 5));
        var initialAmmo = attacker.CurrentMagazine;

        combat.IssueAttackOrder(attacker.Id, target.Id);
        combat.TimeSystem.Resume();

        // Simulate until attack fires
        for (int i = 0; i < 50; i++)
        {
            combat.Update(0.05f);
            if (attacker.CurrentMagazine < initialAmmo)
            {
                break;
            }
        }

        AssertThat(attacker.CurrentMagazine).IsLess(initialAmmo);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Actor_Dies_AtZeroHp()
    {
        var combat = new CombatState();
        var actor = combat.AddActor("crew", new Vector2I(5, 5));

        actor.TakeDamage(actor.MaxHp);

        AssertThat(actor.State).IsEqual(ActorState.Dead);
        AssertThat(actor.Hp).IsEqual(0);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Mission_Victory_WhenAllEnemiesDead()
    {
        var map = CreateOpenMap(20, 20);
        var combat = new CombatState();
        combat.MapState = map;
        combat.InitializeVisibility();
        combat.SetHasEnemyObjective(true);

        var crew = combat.AddActor("crew", new Vector2I(5, 5));
        var enemy = combat.AddActor("enemy", new Vector2I(8, 5));

        // Kill the enemy
        enemy.TakeDamage(enemy.MaxHp);
        combat.TimeSystem.Resume();
        combat.Update(0.05f);

        AssertThat(combat.IsComplete).IsTrue();
        AssertThat(combat.Victory).IsTrue();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Mission_Defeat_WhenAllCrewDead()
    {
        var map = CreateOpenMap(20, 20);
        var combat = new CombatState();
        combat.MapState = map;
        combat.InitializeVisibility();
        combat.SetHasEnemyObjective(true);

        var crew = combat.AddActor("crew", new Vector2I(5, 5));
        var enemy = combat.AddActor("enemy", new Vector2I(8, 5));

        // Kill the crew
        crew.TakeDamage(crew.MaxHp);
        combat.TimeSystem.Resume();
        combat.Update(0.05f);

        AssertThat(combat.IsComplete).IsTrue();
        AssertThat(combat.Victory).IsFalse();
    }

    // ========== AI Tests ==========

    [TestCase]
    [RequireGodotRuntime]
    public void AI_ReloadsWhenEmpty()
    {
        var map = CreateOpenMap(20, 20);
        var combat = new CombatState();
        combat.MapState = map;
        combat.InitializeVisibility();

        var enemy = combat.AddActor("enemy", new Vector2I(5, 5));
        enemy.CurrentMagazine = 0;
        enemy.ReserveAmmo = 30;

        combat.TimeSystem.Resume();
        combat.Update(0.05f);

        AssertThat(enemy.IsReloading).IsTrue();
    }

    // ========== Helper Methods ==========

    private MapState CreateOpenMap(int width, int height)
    {
        var template = new string[height];
        for (int y = 0; y < height; y++)
        {
            if (y == 0 || y == height - 1)
            {
                template[y] = new string('#', width);
            }
            else
            {
                template[y] = "#" + new string('.', width - 2) + "#";
            }
        }
        return MapBuilder.BuildFromTemplate(template);
    }
}
```

---

## Implementation Order

1. **Day 1: Hit Chance System**
   - Step 1.1: Distance-based hit chance in CombatResolver
   - Step 1.2: Update weapon definitions

2. **Day 2: Ammunition System**
   - Step 2.1: Add ammo to WeaponDef/WeaponData
   - Step 2.2: Add ammo state to Actor
   - Step 2.3: Integrate ammo into combat flow
   - Step 2.4: Manual reload command

3. **Day 3: Auto-Defend & AI**
   - Step 3.1: Implement auto-defend
   - Step 4.1: AI ammo awareness
   - Step 4.2: AI target priority

4. **Day 4: View Layer & Polish**
   - Step 5.1: Ammo display
   - Step 5.2: Reload input
   - Step 5.3: Hit chance preview (optional)
   - Testing and bug fixes

---

## Success Criteria for M3

When M3 is complete, you should be able to:

1. ✅ Attack enemies with distance-based hit chance
2. ✅ See ammo count on crew units
3. ✅ Run out of ammo and need to reload
4. ✅ Press R to manually reload
5. ✅ See crew auto-return fire when hit
6. ✅ See enemies attack, reload, and prioritize targets
7. ✅ Win by killing all enemies
8. ✅ Lose if all crew die

**Natural Pause Point**: After M3, you have a functioning "minimal combat loop" prototype. This is a good time to test basic pacing, lethality, and UX around targeting and feedback.

---

## Notes for Future Milestones

### M4 Dependencies (Directional Cover)
- Hit chance calculation will need cover modifier
- `CombatResolver.CalculateHitChance()` extensible for cover
- Cover should reduce hit chance significantly (30-50%)

### M5 Dependencies (Interactables)
- Reload action is a "channeled" action pattern
- Same pattern can be used for hacking terminals
- Interrupt logic (cancel reload on damage) reusable

### M6 Dependencies (Stealth)
- Auto-defend may need to be suppressible in stealth
- Gunfire should alert enemies (sound propagation)
- Consider: silenced weapons that don't trigger alerts

---

## Open Questions

1. **Partial Reload**: Should reloading a half-empty magazine waste the remaining rounds?
   - *Recommendation*: No, keep it simple. Rounds transfer to reserve.

2. **Reload Interrupt**: Should taking damage cancel reload?
   - *Recommendation*: Yes for M3, creates tactical tension.

3. **Auto-Defend Priority**: Should auto-defend override manual attack orders?
   - *Recommendation*: No, manual orders take precedence.

4. **Ammo Pickup**: Should there be ammo pickups on the map?
   - *Recommendation*: Not for M3. Consider for M5 (interactables).

5. **Out of Ammo Behavior**: What happens when completely out of ammo?
   - *Recommendation*: Unit can't attack, must rely on teammates. Consider melee in [PLUS].

---

## Files to Create/Modify

### Modified Files
- `src/sim/combat/CombatResolver.cs` - Hit chance calculation
- `src/sim/combat/Actor.cs` - Ammo state and reload
- `src/sim/combat/CombatState.cs` - Ammo integration, auto-defend
- `src/sim/combat/AIController.cs` - Ammo awareness, target priority
- `src/sim/combat/WeaponData.cs` - Ammo properties
- `src/sim/data/Definitions.cs` - Weapon ammo values
- `src/sim/data/MissionConfig.cs` - M3 test mission
- `src/scenes/mission/MissionView.cs` - Reload input, ammo display
- `src/scenes/mission/ActorView.cs` - Ammo label

### New Files
- `tests/sim/combat/M3Tests.cs` - Automated tests

### agents.md Updates
- `src/sim/combat/agents.md` - Document ammo system
- `tests/sim/combat/agents.md` - Document M3 tests
