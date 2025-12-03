# M4 – Directional Cover & Lethality Tuning: Implementation Plan

This document breaks down **Milestone 4** from `ROADMAP.md` into concrete implementation steps.

**Goal**: Introduce the cover game so combat matches the intended "lethal but positional" fantasy.

---

## Current State Assessment

### What We Have (From M0–M3)

| Component | Status | Notes |
|-----------|--------|-------|
| `MapState` | ✅ Complete | Has `CoverFlags` list (int per tile), `GetCover()`, `SetCover()` |
| `CombatResolver` | ✅ Complete | `CalculateHitChance()` with distance/accuracy, no cover modifier |
| `Actor` | ✅ Complete | Position, HP, weapon, ammo, auto-defend |
| `WeaponData` | ✅ Complete | Range, Damage, Accuracy, CooldownTicks |
| `VisibilitySystem` | ✅ Complete | LOS checks, fog of war |
| `MissionView` | ✅ Complete | Grid rendering, actor views, attack orders |

### What M4 Requires vs What We Have

| M4 Requirement | Current Status | Gap |
|----------------|----------------|-----|
| 8-direction cover values per tile | ⚠️ Partial | `CoverFlags` exists as int, needs proper direction enum/flags |
| Cover data on map tiles/entities | ⚠️ Partial | Storage exists, no authoring or auto-generation |
| Determine cover relative to shooter direction | ❌ Missing | Need direction calculation from shooter to target |
| Apply hit/damage modifiers for cover | ❌ Missing | `CalculateHitChance()` ignores cover |
| Basic cover feedback in UI | ❌ Missing | No cover visualization |
| Balance pass (exposed = death, mutual cover = stalemate) | ❌ Missing | Need tuning after implementation |

---

## Architecture Decisions

### Cover Direction Model

**Decision**: Use 8-directional cover with bitflags.

**Rationale**:
- 8 directions (N, NE, E, SE, S, SW, W, NW) match grid-based movement
- Bitflags allow efficient storage and querying
- A tile can provide cover from multiple directions (e.g., corner piece)

**Implementation**:
```csharp
[Flags]
public enum CoverDirection : byte
{
    None = 0,
    N  = 1 << 0,  // 1
    NE = 1 << 1,  // 2
    E  = 1 << 2,  // 4
    SE = 1 << 3,  // 8
    S  = 1 << 4,  // 16
    SW = 1 << 5,  // 32
    W  = 1 << 6,  // 64
    NW = 1 << 7,  // 128
    All = 0xFF
}
```

### Cover Quality Model

**Decision**: Single cover quality for v0.1 (binary: has cover or doesn't).

**Options Considered**:
1. **Binary cover** - Simple, sufficient for core gameplay ✅
2. **Two-tier cover** (half/full) - More tactical depth, [PLUS] feature
3. **Continuous cover values** - Complex, overkill for v0.1

**Chosen Approach**: Binary cover for M4
- Tile either provides cover in a direction or doesn't
- Future M4+ can add cover quality (half/full) as an enhancement

### Cover Effect on Combat

**Decision**: Cover reduces hit chance, not damage.

**Rationale**:
- Simpler to understand ("harder to hit behind cover")
- Matches design doc: "exposed units die quickly" vs "mutual cover = stalemate"
- Damage reduction would require armor/penetration system

**Formula**:
```
hitChance = baseHitChance * (1 - coverBonus)
coverBonus = 0.40 if target has cover from attack direction, else 0
```

**Parameters** (tunable):
- `COVER_HIT_REDUCTION = 0.40f` — 40% reduction when in cover
- This means ~70% base accuracy becomes ~42% against covered target

### Cover Source: Adjacent Tiles vs Current Tile

**Decision**: Cover is provided by **adjacent tiles**, not the tile the unit stands on.

**Rationale**:
- More intuitive: "stand next to wall for cover"
- Matches tactical game conventions (XCOM, etc.)
- Allows more interesting map design

**Implementation**:
- When checking cover, look at tiles adjacent to target
- If adjacent tile provides cover in the direction of the attacker, target has cover

---

## Implementation Steps

### Phase 1: Cover Data Model (Priority: Critical)

#### Step 1.1: Create CoverDirection Enum

**New File**: `src/sim/combat/CoverDirection.cs`

```csharp
using System;

namespace FringeTactics;

/// <summary>
/// 8-directional cover flags. A tile can provide cover from multiple directions.
/// </summary>
[Flags]
public enum CoverDirection : byte
{
    None = 0,
    N  = 1 << 0,  // North (up, -Y)
    NE = 1 << 1,  // Northeast
    E  = 1 << 2,  // East (right, +X)
    SE = 1 << 3,  // Southeast
    S  = 1 << 4,  // South (down, +Y)
    SW = 1 << 5,  // Southwest
    W  = 1 << 6,  // West (left, -X)
    NW = 1 << 7,  // Northwest
    All = 0xFF    // Full cover from all directions
}

/// <summary>
/// Helper methods for cover direction calculations.
/// </summary>
public static class CoverDirectionHelper
{
    /// <summary>
    /// Get the direction from one grid position to another.
    /// Returns the closest of 8 cardinal/diagonal directions.
    /// </summary>
    public static CoverDirection GetDirection(Vector2I from, Vector2I to)
    {
        var diff = to - from;
        
        // Normalize to -1, 0, or 1
        var dx = Math.Sign(diff.X);
        var dy = Math.Sign(diff.Y);
        
        return (dx, dy) switch
        {
            (0, -1)  => CoverDirection.N,
            (1, -1)  => CoverDirection.NE,
            (1, 0)   => CoverDirection.E,
            (1, 1)   => CoverDirection.SE,
            (0, 1)   => CoverDirection.S,
            (-1, 1)  => CoverDirection.SW,
            (-1, 0)  => CoverDirection.W,
            (-1, -1) => CoverDirection.NW,
            _ => CoverDirection.None
        };
    }
    
    /// <summary>
    /// Get the opposite direction (for determining what cover protects against).
    /// </summary>
    public static CoverDirection GetOpposite(CoverDirection dir)
    {
        return dir switch
        {
            CoverDirection.N  => CoverDirection.S,
            CoverDirection.NE => CoverDirection.SW,
            CoverDirection.E  => CoverDirection.W,
            CoverDirection.SE => CoverDirection.NW,
            CoverDirection.S  => CoverDirection.N,
            CoverDirection.SW => CoverDirection.NE,
            CoverDirection.W  => CoverDirection.E,
            CoverDirection.NW => CoverDirection.SE,
            _ => CoverDirection.None
        };
    }
    
    /// <summary>
    /// Get the offset vector for a direction.
    /// </summary>
    public static Vector2I GetOffset(CoverDirection dir)
    {
        return dir switch
        {
            CoverDirection.N  => new Vector2I(0, -1),
            CoverDirection.NE => new Vector2I(1, -1),
            CoverDirection.E  => new Vector2I(1, 0),
            CoverDirection.SE => new Vector2I(1, 1),
            CoverDirection.S  => new Vector2I(0, 1),
            CoverDirection.SW => new Vector2I(-1, 1),
            CoverDirection.W  => new Vector2I(-1, 0),
            CoverDirection.NW => new Vector2I(-1, -1),
            _ => Vector2I.Zero
        };
    }
    
    /// <summary>
    /// Get all 8 cardinal and diagonal directions.
    /// </summary>
    public static CoverDirection[] AllDirections => new[]
    {
        CoverDirection.N, CoverDirection.NE, CoverDirection.E, CoverDirection.SE,
        CoverDirection.S, CoverDirection.SW, CoverDirection.W, CoverDirection.NW
    };
}
```

**Acceptance Criteria**:
- [ ] `CoverDirection` enum exists with 8 directions + None + All
- [ ] `GetDirection()` correctly maps position differences to directions
- [ ] `GetOpposite()` returns the opposite direction
- [ ] `GetOffset()` returns correct Vector2I for each direction

---

#### Step 1.2: Update MapState for Typed Cover

**File**: `src/sim/combat/MapState.cs`

**Current Code**:
```csharp
public List<int> CoverFlags { get; set; } = new();

public int GetCover(Vector2I pos)
{
    if (!IsInBounds(pos)) return 0;
    return CoverFlags[GetIndex(pos)];
}

public void SetCover(Vector2I pos, int coverFlags)
{
    if (!IsInBounds(pos)) return;
    CoverFlags[GetIndex(pos)] = coverFlags;
}
```

**Updated Code**:
```csharp
// Change storage type for clarity (still byte-compatible)
private List<CoverDirection> coverData = new();

/// <summary>
/// Get cover directions provided by a tile.
/// This is the cover the tile PROVIDES, not the cover a unit ON this tile receives.
/// </summary>
public CoverDirection GetTileCover(Vector2I pos)
{
    if (!IsInBounds(pos))
    {
        return CoverDirection.None;
    }
    return coverData[GetIndex(pos)];
}

/// <summary>
/// Set cover directions provided by a tile.
/// </summary>
public void SetTileCover(Vector2I pos, CoverDirection cover)
{
    if (!IsInBounds(pos))
    {
        return;
    }
    coverData[GetIndex(pos)] = cover;
}

/// <summary>
/// Check if a unit at targetPos has cover against an attack from attackerPos.
/// Checks adjacent tiles for cover that blocks the attack direction.
/// </summary>
public bool HasCoverAgainst(Vector2I targetPos, Vector2I attackerPos)
{
    // Direction from attacker to target
    var attackDir = CoverDirectionHelper.GetDirection(attackerPos, targetPos);
    if (attackDir == CoverDirection.None)
    {
        return false;
    }
    
    // Cover needs to block from the opposite direction (where attack comes from)
    var coverNeeded = CoverDirectionHelper.GetOpposite(attackDir);
    
    // Check adjacent tiles for cover
    foreach (var dir in CoverDirectionHelper.AllDirections)
    {
        var adjacentPos = targetPos + CoverDirectionHelper.GetOffset(dir);
        if (!IsInBounds(adjacentPos))
        {
            continue;
        }
        
        var adjacentCover = GetTileCover(adjacentPos);
        
        // Adjacent tile provides cover if:
        // 1. It has cover facing toward the target (opposite of dir)
        // 2. That cover direction matches where the attack is coming from
        var coverFacing = CoverDirectionHelper.GetOpposite(dir);
        if ((adjacentCover & coverFacing) != 0 && coverFacing == coverNeeded)
        {
            return true;
        }
    }
    
    // Also check if target tile itself provides cover (e.g., standing in doorway)
    // Walls automatically provide cover from all directions
    if (GetTileType(targetPos) == TileType.Wall)
    {
        return true; // Shouldn't happen (can't stand in wall), but defensive
    }
    
    return false;
}

// Legacy compatibility - keep old methods working
public int GetCover(Vector2I pos) => (int)GetTileCover(pos);
public void SetCover(Vector2I pos, int coverFlags) => SetTileCover(pos, (CoverDirection)coverFlags);
```

**Update `InitializeGrid()`**:
```csharp
private void InitializeGrid(Vector2I size)
{
    GridSize = size;
    var totalTiles = size.X * size.Y;
    tiles = new List<TileType>(totalTiles);
    coverData = new List<CoverDirection>(totalTiles);
    
    for (int i = 0; i < totalTiles; i++)
    {
        tiles.Add(TileType.Floor);
        coverData.Add(CoverDirection.None);
    }
}
```

**Acceptance Criteria**:
- [ ] `MapState` uses `CoverDirection` enum internally
- [ ] `GetTileCover()` and `SetTileCover()` work with enum
- [ ] `HasCoverAgainst()` correctly determines cover from adjacent tiles
- [ ] Legacy `GetCover()`/`SetCover()` still work for compatibility

---

#### Step 1.3: Auto-Generate Cover from Walls

**File**: `src/sim/combat/MapBuilder.cs`

Walls should automatically provide cover. Add a post-processing step.

```csharp
/// <summary>
/// Generate cover data based on wall placement.
/// Walls provide cover to adjacent floor tiles.
/// </summary>
public static void GenerateCoverFromWalls(MapState map)
{
    for (int y = 0; y < map.GridSize.Y; y++)
    {
        for (int x = 0; x < map.GridSize.X; x++)
        {
            var pos = new Vector2I(x, y);
            
            // Only walls provide cover
            if (map.GetTileType(pos) != TileType.Wall)
            {
                continue;
            }
            
            // Wall provides cover from all directions it faces
            // (i.e., all directions where there's a floor tile)
            var coverDirs = CoverDirection.None;
            
            foreach (var dir in CoverDirectionHelper.AllDirections)
            {
                var adjacentPos = pos + CoverDirectionHelper.GetOffset(dir);
                if (map.IsInBounds(adjacentPos) && map.GetTileType(adjacentPos) == TileType.Floor)
                {
                    // Wall provides cover facing this direction
                    coverDirs |= dir;
                }
            }
            
            map.SetTileCover(pos, coverDirs);
        }
    }
}
```

**Update `BuildFromTemplate()` to call this**:
```csharp
public static MapState BuildFromTemplate(string[] rows)
{
    // ... existing parsing code ...
    
    // Generate cover from walls
    GenerateCoverFromWalls(map);
    
    return map;
}
```

**Acceptance Criteria**:
- [ ] `GenerateCoverFromWalls()` sets cover on wall tiles
- [ ] Walls adjacent to floors have cover facing those floors
- [ ] `BuildFromTemplate()` auto-generates cover

---

### Phase 2: Combat Integration (Priority: Critical)

#### Step 2.1: Add Cover Modifier to CombatResolver

**File**: `src/sim/combat/CombatResolver.cs`

**New Constants**:
```csharp
// Cover tuning constants
public const float COVER_HIT_REDUCTION = 0.40f;  // 40% reduction when target in cover
```

**Update `CalculateHitChance()`**:
```csharp
/// <summary>
/// Calculate hit chance based on distance, weapon accuracy, attacker stats, and cover.
/// </summary>
public static float CalculateHitChance(Actor attacker, Actor target, WeaponData weapon, MapState map)
{
    var distance = GetDistance(attacker.GridPosition, target.GridPosition);
    
    // Base accuracy from weapon
    var baseAccuracy = weapon.Accuracy;
    
    // Distance penalty: increases as you approach max range
    var rangeFraction = distance / weapon.Range;
    var distancePenalty = rangeFraction * RANGE_PENALTY_FACTOR;
    
    // Apply attacker's aim stat bonus (+1% per point)
    var aimBonus = (attacker.Stats.TryGetValue("aim", out var aim) ? aim : 0) * 0.01f;
    
    // Base hit chance before cover
    var hitChance = baseAccuracy * (1f - distancePenalty) + aimBonus;
    
    // Apply cover penalty
    if (map != null && map.HasCoverAgainst(target.GridPosition, attacker.GridPosition))
    {
        hitChance *= (1f - COVER_HIT_REDUCTION);
    }
    
    // Clamp to valid range
    return Mathf.Clamp(hitChance, MIN_HIT_CHANCE, MAX_HIT_CHANCE);
}
```

**Note**: This requires updating all call sites to pass `MapState`. Create an overload for backward compatibility:

```csharp
/// <summary>
/// Calculate hit chance without cover (legacy, for tests).
/// </summary>
public static float CalculateHitChance(Actor attacker, Actor target, WeaponData weapon)
{
    return CalculateHitChance(attacker, target, weapon, null);
}
```

**Update `ResolveAttack()` to use new signature**:
```csharp
public static AttackResult ResolveAttack(Actor attacker, Actor target, WeaponData weapon, MapState map, Random rng)
{
    // ... existing code ...
    
    // Calculate hit chance with cover
    var hitChance = CalculateHitChance(attacker, target, weapon, map);
    result.HitChance = hitChance;
    
    // ... rest unchanged ...
}
```

**Acceptance Criteria**:
- [ ] `CalculateHitChance()` accepts `MapState` parameter
- [ ] Cover reduces hit chance by `COVER_HIT_REDUCTION`
- [ ] Existing tests still pass (via overload)
- [ ] `ResolveAttack()` uses cover-aware hit chance

---

#### Step 2.2: Add Cover Info to AttackResult

**File**: `src/sim/combat/AttackResult.cs` (or wherever it's defined)

```csharp
public struct AttackResult
{
    public int AttackerId { get; set; }
    public int TargetId { get; set; }
    public string WeaponName { get; set; }
    public bool Hit { get; set; }
    public int Damage { get; set; }
    public float HitChance { get; set; }
    public bool TargetInCover { get; set; }  // New: for UI feedback
}
```

**Update `ResolveAttack()`**:
```csharp
var inCover = map != null && map.HasCoverAgainst(target.GridPosition, attacker.GridPosition);
result.TargetInCover = inCover;
```

**Acceptance Criteria**:
- [ ] `AttackResult.TargetInCover` populated correctly
- [ ] Can be used for UI feedback and logging

---

### Phase 3: Cover Visualization (Priority: High)

#### Step 3.1: Create Cover Indicator System

**New File**: `src/scenes/mission/CoverIndicator.cs`

```csharp
using Godot;
using System.Collections.Generic;

namespace FringeTactics;

/// <summary>
/// Displays cover indicators on the tactical map.
/// Shows which directions provide cover for selected units.
/// </summary>
public partial class CoverIndicator : Node2D
{
    private const int TileSize = 32;
    private const float IndicatorSize = 6f;
    private const float IndicatorOffset = 2f;
    
    private MapState map;
    private Dictionary<Vector2I, Node2D> tileIndicators = new();
    
    public void Initialize(MapState mapState)
    {
        map = mapState;
    }
    
    /// <summary>
    /// Show cover indicators for a specific position (e.g., selected unit).
    /// </summary>
    public void ShowCoverFor(Vector2I unitPos)
    {
        ClearIndicators();
        
        if (map == null)
        {
            return;
        }
        
        // Check each direction for cover
        foreach (var dir in CoverDirectionHelper.AllDirections)
        {
            var adjacentPos = unitPos + CoverDirectionHelper.GetOffset(dir);
            if (!map.IsInBounds(adjacentPos))
            {
                continue;
            }
            
            var adjacentCover = map.GetTileCover(adjacentPos);
            var coverFacing = CoverDirectionHelper.GetOpposite(dir);
            
            if ((adjacentCover & coverFacing) != 0)
            {
                // This direction has cover
                CreateCoverIndicator(unitPos, dir);
            }
        }
    }
    
    /// <summary>
    /// Show cover indicators for multiple positions.
    /// </summary>
    public void ShowCoverForMultiple(IEnumerable<Vector2I> positions)
    {
        ClearIndicators();
        
        foreach (var pos in positions)
        {
            ShowCoverForSingle(pos);
        }
    }
    
    private void ShowCoverForSingle(Vector2I unitPos)
    {
        if (map == null)
        {
            return;
        }
        
        foreach (var dir in CoverDirectionHelper.AllDirections)
        {
            var adjacentPos = unitPos + CoverDirectionHelper.GetOffset(dir);
            if (!map.IsInBounds(adjacentPos))
            {
                continue;
            }
            
            var adjacentCover = map.GetTileCover(adjacentPos);
            var coverFacing = CoverDirectionHelper.GetOpposite(dir);
            
            if ((adjacentCover & coverFacing) != 0)
            {
                CreateCoverIndicator(unitPos, dir);
            }
        }
    }
    
    private void CreateCoverIndicator(Vector2I tilePos, CoverDirection dir)
    {
        var indicator = new ColorRect();
        indicator.Color = new Color(0.2f, 0.6f, 1.0f, 0.7f); // Blue, semi-transparent
        
        // Position and size based on direction
        var offset = CoverDirectionHelper.GetOffset(dir);
        var tileCenter = new Vector2(tilePos.X * TileSize + TileSize / 2, tilePos.Y * TileSize + TileSize / 2);
        
        // Create a small bar on the edge of the tile facing the cover
        if (offset.X != 0 && offset.Y == 0)
        {
            // Horizontal (E or W)
            indicator.Size = new Vector2(IndicatorSize, TileSize - 4);
            indicator.Position = new Vector2(
                tileCenter.X + (offset.X > 0 ? TileSize / 2 - IndicatorSize - IndicatorOffset : -TileSize / 2 + IndicatorOffset),
                tileCenter.Y - (TileSize - 4) / 2
            );
        }
        else if (offset.Y != 0 && offset.X == 0)
        {
            // Vertical (N or S)
            indicator.Size = new Vector2(TileSize - 4, IndicatorSize);
            indicator.Position = new Vector2(
                tileCenter.X - (TileSize - 4) / 2,
                tileCenter.Y + (offset.Y > 0 ? TileSize / 2 - IndicatorSize - IndicatorOffset : -TileSize / 2 + IndicatorOffset)
            );
        }
        else
        {
            // Diagonal - small corner indicator
            indicator.Size = new Vector2(IndicatorSize, IndicatorSize);
            indicator.Position = new Vector2(
                tileCenter.X + (offset.X > 0 ? TileSize / 2 - IndicatorSize - IndicatorOffset : -TileSize / 2 + IndicatorOffset),
                tileCenter.Y + (offset.Y > 0 ? TileSize / 2 - IndicatorSize - IndicatorOffset : -TileSize / 2 + IndicatorOffset)
            );
        }
        
        AddChild(indicator);
    }
    
    public void ClearIndicators()
    {
        foreach (var child in GetChildren())
        {
            child.QueueFree();
        }
    }
    
    public void Hide()
    {
        ClearIndicators();
    }
}
```

**Acceptance Criteria**:
- [ ] `CoverIndicator` displays directional cover for a position
- [ ] Cover shown as colored bars on tile edges
- [ ] Indicators clear when selection changes

---

#### Step 3.2: Integrate Cover Indicators into MissionView

**File**: `src/scenes/mission/MissionView.cs`

**Add member**:
```csharp
private CoverIndicator coverIndicator;
```

**In `_Ready()`**:
```csharp
coverIndicator = new CoverIndicator();
coverIndicator.Name = "CoverIndicator";
coverIndicator.ZIndex = 3; // Above grid, below actors
AddChild(coverIndicator);
coverIndicator.Initialize(CombatState.MapState);
```

**Update selection handling**:
```csharp
private void UpdateCoverIndicators()
{
    if (selectedActorIds.Count == 0)
    {
        coverIndicator.Hide();
        return;
    }
    
    var positions = new List<Vector2I>();
    foreach (var actorId in selectedActorIds)
    {
        var actor = CombatState.GetActorById(actorId);
        if (actor != null && actor.State == ActorState.Alive)
        {
            positions.Add(actor.GridPosition);
        }
    }
    
    coverIndicator.ShowCoverForMultiple(positions);
}
```

**Call `UpdateCoverIndicators()` when**:
- Selection changes
- Selected units move
- In `_Process()` if actors are moving

**Acceptance Criteria**:
- [ ] Cover indicators appear when units selected
- [ ] Indicators update when units move
- [ ] Indicators hide when nothing selected

---

#### Step 3.3: Show Cover Status in Attack Preview

**File**: `src/scenes/mission/MissionView.cs`

When hovering over an enemy with units selected, show if target has cover.

```csharp
private void UpdateAttackPreview(Vector2I gridPos)
{
    var targetActor = CombatState.GetActorAtPosition(gridPos);
    if (targetActor == null || targetActor.Type == "crew")
    {
        HideAttackPreview();
        return;
    }
    
    // Calculate hit chance and cover status for selected units
    float totalChance = 0f;
    int validAttackers = 0;
    bool targetInCover = false;
    
    foreach (var actorId in selectedActorIds)
    {
        var attacker = CombatState.GetActorById(actorId);
        if (attacker == null || !attacker.CanFire())
        {
            continue;
        }
        
        if (CombatResolver.CanAttack(attacker, targetActor, attacker.EquippedWeapon, CombatState.MapState))
        {
            totalChance += CombatResolver.CalculateHitChance(attacker, targetActor, attacker.EquippedWeapon, CombatState.MapState);
            validAttackers++;
            
            // Check if target has cover from this attacker
            if (CombatState.MapState.HasCoverAgainst(targetActor.GridPosition, attacker.GridPosition))
            {
                targetInCover = true;
            }
        }
    }
    
    if (validAttackers > 0)
    {
        var avgChance = totalChance / validAttackers;
        ShowAttackPreview(gridPos, avgChance, targetInCover);
    }
}

private void ShowAttackPreview(Vector2I gridPos, float hitChance, bool targetInCover)
{
    // Show hit chance percentage
    // If targetInCover, show a shield icon or "IN COVER" text
    // Implementation depends on UI approach
}
```

**Acceptance Criteria**:
- [ ] Attack preview shows hit chance
- [ ] Cover status indicated when target is in cover
- [ ] Visual distinction between covered and exposed targets

---

### Phase 4: Balance Tuning (Priority: High)

#### Step 4.1: Lethality Tuning Parameters

Create a central place for combat balance constants.

**New File**: `src/sim/combat/CombatBalance.cs`

```csharp
namespace FringeTactics;

/// <summary>
/// Central location for combat balance parameters.
/// Adjust these to tune lethality and cover effectiveness.
/// </summary>
public static class CombatBalance
{
    // === Hit Chance ===
    public const float RangePenaltyFactor = 0.30f;  // At max range, 30% accuracy penalty
    public const float MinHitChance = 0.10f;        // Floor: never below 10%
    public const float MaxHitChance = 0.95f;        // Cap: never above 95%
    
    // === Cover ===
    public const float CoverHitReduction = 0.40f;   // 40% hit chance reduction in cover
    
    // === Lethality Targets ===
    // These are design targets, not enforced values:
    // - Exposed unit vs exposed unit: 2-3 hits to kill
    // - Covered unit vs exposed unit: 3-5 hits to kill (due to misses)
    // - Covered unit vs covered unit: 5-8 hits to kill (stalemate)
    
    // === Weapon Balance ===
    // Rifle: 25 damage, 100 HP = 4 hits to kill
    // With 70% base accuracy:
    //   - Exposed: ~70% hit = ~5.7 shots to kill
    //   - Covered: ~42% hit = ~9.5 shots to kill
    
    // === Tuning Notes ===
    // If combat feels too slow: increase damage or accuracy
    // If combat feels too fast: decrease damage or increase cover bonus
    // If cover feels useless: increase CoverHitReduction
    // If cover feels too strong: decrease CoverHitReduction
}
```

**Acceptance Criteria**:
- [ ] Balance constants centralized
- [ ] Comments explain tuning rationale
- [ ] Easy to adjust for playtesting

---

#### Step 4.2: Update Weapon Stats for Cover Meta

**File**: `src/sim/data/Definitions.cs`

Ensure weapons are balanced around cover:

```csharp
["rifle"] = new WeaponDef
{
    Id = "rifle",
    Name = "Assault Rifle",
    Damage = 25,           // 4 hits to kill at 100 HP
    Range = 8,
    CooldownTicks = 10,    // 0.5 sec between shots
    Accuracy = 0.70f,      // 70% base, ~42% vs cover
    MagazineSize = 30,
    ReloadTicks = 40
},
["pistol"] = new WeaponDef
{
    Id = "pistol",
    Name = "Pistol",
    Damage = 18,           // 6 hits to kill
    Range = 5,
    CooldownTicks = 6,
    Accuracy = 0.75f,      // More accurate at short range
    MagazineSize = 12,
    ReloadTicks = 20
},
["smg"] = new WeaponDef
{
    Id = "smg",
    Name = "SMG",
    Damage = 15,           // 7 hits to kill, but fast fire
    Range = 6,
    CooldownTicks = 4,     // Very fast
    Accuracy = 0.55f,      // Less accurate
    MagazineSize = 25,
    ReloadTicks = 30
},
["shotgun"] = new WeaponDef
{
    Id = "shotgun",
    Name = "Shotgun",
    Damage = 50,           // 2 hits to kill - devastating
    Range = 4,             // Very short range
    CooldownTicks = 18,
    Accuracy = 0.85f,      // Very accurate at short range
    MagazineSize = 6,
    ReloadTicks = 60
}
```

**Acceptance Criteria**:
- [ ] Weapons balanced for cover gameplay
- [ ] High-damage weapons have tradeoffs (range, fire rate)
- [ ] Variety of tactical niches

---

### Phase 5: Test Mission & Validation (Priority: High)

#### Step 5.1: Create M4 Test Mission

**File**: `src/sim/data/MissionConfig.cs`

```csharp
/// <summary>
/// M4 test mission - cover combat testing.
/// Features walls and cover positions for testing directional cover.
/// </summary>
public static MissionConfig CreateM4TestMission()
{
    return new MissionConfig
    {
        Id = "m4_test",
        Name = "M4 Test - Cover Combat",
        MapTemplate = new string[]
        {
            "####################",
            "#..................#",
            "#.EE..##....##.....#",
            "#.EE..##....##..E..#",
            "#.....##....##.....#",
            "#..................#",
            "#......####........#",
            "#......####........#",
            "#..................#",
            "#..##..........##..#",
            "#..##..........##..#",
            "#..................#",
            "#..................#",
            "#..................#",
            "#..................#",
            "####################"
        },
        CrewSpawnPositions = new List<Vector2I>
        {
            new Vector2I(2, 2),
            new Vector2I(3, 2),
            new Vector2I(2, 3),
            new Vector2I(3, 3)
        },
        EnemySpawns = new List<EnemySpawn>
        {
            // Enemy behind cover (wall to their west)
            new EnemySpawn("grunt", new Vector2I(7, 3)),
            // Enemy in open
            new EnemySpawn("grunt", new Vector2I(12, 5)),
            // Enemy behind central cover
            new EnemySpawn("grunt", new Vector2I(9, 7)),
            // Enemy behind cover (wall to their east)
            new EnemySpawn("gunner", new Vector2I(16, 3))
        }
    };
}
```

**Acceptance Criteria**:
- [ ] Test mission has varied cover positions
- [ ] Both crew and enemies can use cover
- [ ] Map allows testing flanking

---

## Testing Checklist

### Manual Test Setup

Launch the M4 test mission from the main menu: **"M4 Test (Cover Combat)"**

The test map features:
- **Crew spawn area** (top-left) with walls providing cover to the east
- **Central cover blocks** for mid-map engagements
- **Enemies at various positions**: some in cover, some exposed
- **Flanking routes** around cover blocks

### Manual Testing

1. **Cover Detection**
   - [ ] Select a crew member next to a wall
   - [ ] Cover indicators appear on the side facing the wall
   - [ ] Moving away from wall removes cover indicators
   - [ ] Diagonal cover (corners) works correctly

2. **Cover Effect on Combat**
   - [ ] Attack enemy in cover → hit chance visibly lower
   - [ ] Attack enemy in open → hit chance normal
   - [ ] Combat log shows "target in cover" when applicable
   - [ ] Shots miss more often against covered targets

3. **Directional Cover**
   - [ ] Unit behind wall (wall to their west) has cover from west attacks
   - [ ] Same unit has NO cover from east attacks (flanking works)
   - [ ] Flanking around cover increases hit chance

4. **Lethality Balance**
   - [ ] Exposed units die in 2-4 hits
   - [ ] Covered units survive longer (more misses)
   - [ ] Two units in mutual cover create stalemate
   - [ ] Flanking breaks stalemate

5. **UI Feedback**
   - [ ] Cover indicators visible and clear
   - [ ] Attack preview shows cover status
   - [ ] Hit chance updates when target cover changes

6. **Edge Cases**
   - [ ] Units at map edges handle cover correctly
   - [ ] Multiple adjacent walls provide cover from multiple directions
   - [ ] Moving through cover positions updates indicators

### Automated Tests

Create `tests/sim/combat/M4Tests.cs`:

```csharp
using Godot;
using GdUnit4;
using System.Collections.Generic;
using static GdUnit4.Assertions;

namespace FringeTactics.Tests;

/// <summary>
/// M4 Milestone tests - validates directional cover system.
/// </summary>
[TestSuite]
public class M4Tests
{
    // ========== Cover Direction Tests ==========

    [TestCase]
    public void CoverDirection_GetDirection_ReturnsCorrectDirection()
    {
        // North (target above attacker)
        var dir = CoverDirectionHelper.GetDirection(new Vector2I(5, 5), new Vector2I(5, 3));
        AssertThat(dir).IsEqual(CoverDirection.N);
        
        // East (target to the right)
        dir = CoverDirectionHelper.GetDirection(new Vector2I(5, 5), new Vector2I(8, 5));
        AssertThat(dir).IsEqual(CoverDirection.E);
        
        // Southeast (diagonal)
        dir = CoverDirectionHelper.GetDirection(new Vector2I(5, 5), new Vector2I(7, 7));
        AssertThat(dir).IsEqual(CoverDirection.SE);
    }

    [TestCase]
    public void CoverDirection_GetOpposite_ReturnsOpposite()
    {
        AssertThat(CoverDirectionHelper.GetOpposite(CoverDirection.N)).IsEqual(CoverDirection.S);
        AssertThat(CoverDirectionHelper.GetOpposite(CoverDirection.E)).IsEqual(CoverDirection.W);
        AssertThat(CoverDirectionHelper.GetOpposite(CoverDirection.NE)).IsEqual(CoverDirection.SW);
    }

    // ========== Map Cover Tests ==========

    [TestCase]
    [RequireGodotRuntime]
    public void MapState_WallProvidesCover()
    {
        var template = new string[]
        {
            "#####",
            "#...#",
            "#.#.#",  // Wall at (2,2)
            "#...#",
            "#####"
        };
        var map = MapBuilder.BuildFromTemplate(template);
        
        // Wall at (2,2) should provide cover
        var wallCover = map.GetTileCover(new Vector2I(2, 2));
        AssertThat(wallCover).IsNotEqual(CoverDirection.None);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void MapState_HasCoverAgainst_WallToWest()
    {
        // Unit at (3,2), wall at (2,2)
        // Should have cover against attacks from the west
        var template = new string[]
        {
            "#####",
            "#...#",
            "#.#.#",  // Wall at (2,2), unit would be at (3,2)
            "#...#",
            "#####"
        };
        var map = MapBuilder.BuildFromTemplate(template);
        
        var targetPos = new Vector2I(3, 2);
        var attackerFromWest = new Vector2I(1, 2);
        var attackerFromEast = new Vector2I(4, 2);
        
        // Has cover from west (wall blocks)
        AssertThat(map.HasCoverAgainst(targetPos, attackerFromWest)).IsTrue();
        
        // No cover from east (can be flanked)
        AssertThat(map.HasCoverAgainst(targetPos, attackerFromEast)).IsFalse();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void MapState_HasCoverAgainst_NoWallNoCover()
    {
        var template = new string[]
        {
            "#####",
            "#...#",
            "#...#",  // No walls in middle
            "#...#",
            "#####"
        };
        var map = MapBuilder.BuildFromTemplate(template);
        
        var targetPos = new Vector2I(2, 2);
        var attackerPos = new Vector2I(1, 2);
        
        AssertThat(map.HasCoverAgainst(targetPos, attackerPos)).IsFalse();
    }

    // ========== Combat Resolution with Cover ==========

    [TestCase]
    [RequireGodotRuntime]
    public void HitChance_ReducedWhenTargetInCover()
    {
        var template = new string[]
        {
            "##########",
            "#........#",
            "#.#......#",  // Wall at (2,2)
            "#........#",
            "##########"
        };
        var map = MapBuilder.BuildFromTemplate(template);
        
        var combat = new CombatState();
        combat.MapState = map;
        
        // Attacker to the west, target behind wall
        var attacker = combat.AddActor("crew", new Vector2I(1, 2));
        var coveredTarget = combat.AddActor("enemy", new Vector2I(3, 2));  // Wall to west
        var exposedTarget = combat.AddActor("enemy", new Vector2I(5, 2));  // No cover
        
        var coveredChance = CombatResolver.CalculateHitChance(attacker, coveredTarget, attacker.EquippedWeapon, map);
        var exposedChance = CombatResolver.CalculateHitChance(attacker, exposedTarget, attacker.EquippedWeapon, map);
        
        AssertThat(coveredChance).IsLess(exposedChance);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void HitChance_NotReducedWhenFlanking()
    {
        var template = new string[]
        {
            "##########",
            "#........#",
            "#.#......#",  // Wall at (2,2)
            "#........#",
            "##########"
        };
        var map = MapBuilder.BuildFromTemplate(template);
        
        var combat = new CombatState();
        combat.MapState = map;
        
        // Target behind wall (wall to west)
        var target = combat.AddActor("enemy", new Vector2I(3, 2));
        
        // Attacker from west (blocked by cover)
        var westAttacker = combat.AddActor("crew", new Vector2I(1, 2));
        
        // Attacker from east (flanking, no cover)
        var eastAttacker = combat.AddActor("crew", new Vector2I(5, 2));
        
        var westChance = CombatResolver.CalculateHitChance(westAttacker, target, westAttacker.EquippedWeapon, map);
        var eastChance = CombatResolver.CalculateHitChance(eastAttacker, target, eastAttacker.EquippedWeapon, map);
        
        // Flanking should have higher hit chance
        AssertThat(eastChance).IsGreater(westChance);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void AttackResult_IndicatesTargetInCover()
    {
        var template = new string[]
        {
            "##########",
            "#........#",
            "#.#......#",
            "#........#",
            "##########"
        };
        var map = MapBuilder.BuildFromTemplate(template);
        
        var combat = new CombatState();
        combat.MapState = map;
        
        var attacker = combat.AddActor("crew", new Vector2I(1, 2));
        var target = combat.AddActor("enemy", new Vector2I(3, 2));
        
        var result = CombatResolver.ResolveAttack(attacker, target, attacker.EquippedWeapon, map, new System.Random(42));
        
        AssertThat(result.TargetInCover).IsTrue();
    }

    // ========== Cover Generation Tests ==========

    [TestCase]
    [RequireGodotRuntime]
    public void MapBuilder_GeneratesCoverFromWalls()
    {
        var template = new string[]
        {
            "###",
            "#.#",
            "###"
        };
        var map = MapBuilder.BuildFromTemplate(template);
        
        // Center wall tiles should have cover facing the center floor
        // Top wall (1,0) should have cover facing south
        var topWallCover = map.GetTileCover(new Vector2I(1, 0));
        AssertThat((topWallCover & CoverDirection.S) != 0).IsTrue();
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

**Acceptance Criteria**:
- [ ] All direction helper tests pass
- [ ] Cover detection tests pass
- [ ] Hit chance reduction tests pass
- [ ] Flanking tests pass
- [ ] Cover generation tests pass

---

## Implementation Order

1. **Day 1: Cover Data Model**
   - Step 1.1: Create CoverDirection enum and helpers
   - Step 1.2: Update MapState for typed cover
   - Step 1.3: Auto-generate cover from walls

2. **Day 2: Combat Integration**
   - Step 2.1: Add cover modifier to CombatResolver
   - Step 2.2: Add cover info to AttackResult
   - Write and run automated tests

3. **Day 3: Visualization**
   - Step 3.1: Create CoverIndicator system
   - Step 3.2: Integrate into MissionView
   - Step 3.3: Attack preview with cover status

4. **Day 4: Balance & Polish**
   - Step 4.1: Create CombatBalance constants
   - Step 4.2: Tune weapon stats
   - Step 5.1: Create M4 test mission
   - Manual playtesting and tuning

5. **Day 5: Testing & Refinement**
   - Run full test suite
   - Manual testing checklist
   - Bug fixes and polish

---

## Success Criteria for M4

When M4 is complete, you should be able to:

1. ✅ See cover indicators when selecting units near walls
2. ✅ Attack enemies in cover with reduced hit chance
3. ✅ Flank enemies to bypass their cover
4. ✅ Experience "exposed = death" lethality
5. ✅ Experience "mutual cover = stalemate" gameplay
6. ✅ See cover status in attack preview
7. ✅ All automated tests pass

**Natural Pause Point**: After M4, you have a legitimate tactics game: move, shoot, take cover, survive. You can pause tactical work here and focus on campaign scaffolding if needed.

---

## Notes for Future Milestones

### M5 Dependencies (Interactables)
- Doors will need to affect cover when closed
- Some interactables may provide cover (deployable shields?)
- Cover system is ready for these extensions

### M6 Dependencies (Stealth)
- Cover may affect detection (hiding behind cover)
- Current system doesn't affect visibility, only combat
- May need `HasCoverAgainst()` for stealth checks

### Future Enhancements (Post-M4)
- **Two-tier cover** (half/full): Add `CoverQuality` enum, modify `HasCoverAgainst()` to return quality
- **Destructible cover**: Track cover health, remove cover when destroyed
- **Dynamic cover**: Deployable shields, overturned tables, etc.

---

## Open Questions

1. **Diagonal cover effectiveness**: Should diagonal cover be as effective as cardinal?
   - *Decision*: Yes, for simplicity. Can tune later if needed.

2. **Multiple cover sources**: If unit has cover from multiple directions, any benefit?
   - *Decision*: No stacking for M4. Cover is binary per attack direction.

3. **Cover and movement**: Should moving units lose cover bonus?
   - *Decision*: No. Cover is position-based, checked at attack time.

4. **AI cover-seeking**: Should AI prioritize cover positions?
   - *Decision*: Yes, should be implemented as part of AI improvements.

5. **Cover visualization style**: Bars, shields, or highlighting?
   - *Decision*: Start with colored bars on tile edges. Iterate based on feedback.

---

## Files to Create/Modify

### New Files
- `src/sim/combat/CoverDirection.cs` - Direction enum and helpers
- `src/sim/combat/CombatBalance.cs` - Balance constants
- `src/scenes/mission/CoverIndicator.cs` - Cover visualization
- `tests/sim/combat/M4Tests.cs` - Automated tests

### Modified Files
- `src/sim/combat/MapState.cs` - Typed cover, `HasCoverAgainst()`
- `src/sim/combat/MapBuilder.cs` - Auto-generate cover from walls
- `src/sim/combat/CombatResolver.cs` - Cover modifier in hit chance
- `src/sim/combat/AttackResult.cs` - Add `TargetInCover` field
- `src/scenes/mission/MissionView.cs` - Cover indicator integration
- `src/sim/data/MissionConfig.cs` - M4 test mission
- `src/sim/data/Definitions.cs` - Weapon balance tuning
