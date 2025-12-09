# Armor & Damage System

This document describes how damage and armor interact in tactical combat.

---

## Design Goals

1. **Simple and predictable** - Players should understand how much damage they'll deal/take
2. **Armor matters but doesn't dominate** - Protection is meaningful without making low-tier weapons useless
3. **Supports progression** - Better armor provides clear advantages without breaking balance

---

## Damage Formula

```
Final Damage = max(1, Weapon Damage - Target Armor)
```

- **Flat reduction**: Armor subtracts directly from incoming damage
- **Minimum damage**: Attacks always deal at least 1 damage (no complete immunity)
- **No randomness**: Damage is deterministic once a hit is confirmed

### Example Calculations

| Weapon | Base Damage | Target Armor | Final Damage |
|--------|-------------|--------------|--------------|
| Pistol | 15 | 0 (none) | 15 |
| Pistol | 15 | 5 (clothing) | 10 |
| Pistol | 15 | 18 (medium) | 1 (minimum) |
| Rifle | 25 | 5 (clothing) | 20 |
| Rifle | 25 | 18 (medium) | 7 |
| Sniper | 50 | 25 (heavy) | 25 |

---

## Armor Tiers

| Armor | Value | Notes |
|-------|-------|-------|
| None | 0 | Unarmored targets |
| Armored Clothing | 5 | Starting gear, minimal protection |
| Light Armor | 10 | Basic protection, no penalties |
| Medium Armor | 18 | Balanced protection |
| Heavy Armor | 25 | Maximum protection, may have speed penalty |

### Design Rationale

- **Armored Clothing (5)**: Reduces pistol damage by 1/3, but rifles still hurt. Represents basic protective wear.
- **Light Armor (10)**: Negates ~60% of pistol damage, ~40% of SMG. Rifles remain effective.
- **Medium Armor (18)**: Pistols deal minimum damage. Rifles reduced to ~7 damage. Shotguns/snipers still effective.
- **Heavy Armor (25)**: Only high-damage weapons (shotgun 40, sniper 50) deal significant damage.

---

## Weapon Balance vs Armor

| Weapon | Damage | vs Clothing (5) | vs Light (10) | vs Medium (18) | vs Heavy (25) |
|--------|--------|-----------------|---------------|----------------|---------------|
| Pistol | 15 | 10 | 5 | 1 | 1 |
| SMG | 18 | 13 | 8 | 1 | 1 |
| Rifle | 25 | 20 | 15 | 7 | 1 |
| Shotgun | 40 | 35 | 30 | 22 | 15 |
| Sniper | 50 | 45 | 40 | 32 | 25 |

### Implications

- **Pistols**: Effective against unarmored/lightly armored. Backup weapon role.
- **SMG**: High fire rate compensates for low per-shot damage. Struggles vs medium+ armor.
- **Rifle**: Reliable all-rounder. Effective against all but heavy armor.
- **Shotgun**: Armor-piercing at close range. High risk/reward.
- **Sniper**: Ignores most armor. High damage justifies slow fire rate.

---

## Enemy Armor

Enemies have armor values defined in `data/enemies.json`:

| Enemy Type | Armor | Notes |
|------------|-------|-------|
| Grunt | 0 | Unarmored, uses pistol |
| Gunner | 5 | Light protection, uses SMG |
| Sniper | 0 | Unarmored, uses rifle |
| Heavy | 18 | Armored trooper, uses shotgun |

---

## Future Considerations

These are NOT implemented yet but may be added later:

- **Armor penetration**: Weapons could have AP values that ignore some armor
- **Armor degradation**: Armor could wear down after taking hits
- **Damage types**: Energy vs ballistic could interact differently with armor
- **Critical hits**: Could bypass armor partially or fully

---

## Implementation Notes

### Where Armor is Applied

1. `CrewDeployment.Armor` - Calculated from equipped armor item
2. `Actor.Armor` - Tactical actor's armor value during combat
3. `CombatResolver.CalculateDamage()` - Applies armor reduction

### Combat Log Format

```
[Combat] Crew#1 hit Enemy#2 (attack) with Rifle for 20 damage (25 - 5 armor) (70% chance). HP: 80/100
```

---

## Changelog

- **Initial**: Flat damage reduction with minimum 1 damage. Simple and predictable.
