# src/sim/data/ - Configuration and Data Structures

Static configuration types and data definitions used across the sim layer.

## Files

- **Definitions.cs** - Static facade for game data. Delegates to ConfigRegistry:
  - `Definitions.Weapons` - WeaponDef lookup by ID (rifle, pistol, smg, shotgun)
  - `Definitions.Enemies` - EnemyDef lookup by ID (grunt, gunner, sniper, heavy)
  - `Definitions.Abilities` - AbilityDef lookup by ID (frag_grenade, stun_grenade, stun_shot)
  - `Definitions.Reload()` - Hot-reload from JSON (Shift+Alt+D in dev)
  - `Definitions.GetLastLoadResult()` - Get validation result from last load
  - `Definitions.GetRegistry()` - Get underlying ConfigRegistry
- **CampaignConfig.cs** - Campaign balance configuration (singleton):
  - `Mission` - fuel cost, time cost
  - `Rest` - time cost, heal amount
  - `Rewards` - victory money/parts, XP values
  - `Crew` - stat formulas (HP per grit, XP per level, etc.)
  - `RoleStats` - starting stats per crew role
  - `Starting` - initial resources for new campaign
  - Loads from `data/campaign.json`, falls back to defaults
- **ConfigRegistry.cs** - Config loading with validation:
  - Loads weapons, enemies, abilities from JSON
  - Validates each definition (required fields, value ranges)
  - Cross-reference validation (enemy weapon exists)
  - `FailFastOnErrors` flag for development mode
  - `LastLoadResult` with errors/warnings
- **ValidationResult.cs** - Validation result accumulator for errors and warnings
- **DataLoader.cs** - JSON loading utility. Uses Godot FileAccess for res:// paths.
- **MissionConfig.cs** - Mission setup: grid size, map template, entry zone, crew spawns, enemy spawns.
- **WeaponIds.cs** - Constant weapon definition IDs (rifle, pistol, smg, shotgun, sniper).
- **ArmorIds.cs** - Constant armor definition IDs (armored_clothing, light_armor, medium_armor, heavy_armor).
- **Localization.cs** - Localization system for text lookup:
  - `Localization.Load(locale)` - Load JSON file from `data/localization/{locale}.json`
  - `Localization.Get(key)` - Get localized string, returns key if not found
  - `Localization.Get(key, params)` - Get string with parameter substitution
  - `Localization.HasKey(key)` - Check if key exists
- **LocalizationValidator.cs** - Validates localization files against encounter templates:
  - `LocalizationValidator.Validate(locale)` - Check for missing/unused keys
  - `LocalizationValidator.PrintValidation()` - Print validation to console (Shift+Alt+L)
  - `LocalizationValidator.GenerateStubJson()` - Generate template with all required keys

## JSON Data Files

Located in `data/` folder at project root:
- `data/campaign.json` - Campaign balance configuration
- `data/weapons.json` - Weapon definitions
- `data/enemies.json` - Enemy definitions  
- `data/abilities.json` - Ability definitions
- `data/localization/en.json` - English localization strings

## Responsibilities

- Define all game content as data (weapons, enemies, abilities)
- Load data from JSON files for easy balancing
- Provide lookup by ID for spawning
- Define mission configurations
- No runtime state—these are templates/definitions

## Usage

```csharp
// Look up enemy definition (auto-loads on first access)
var enemyDef = Definitions.Enemies.Get("grunt");

// Create weapon from definition
var weapon = WeaponData.FromId("rifle");

// Get ability for use
var ability = Definitions.Abilities.Get("frag_grenade")?.ToAbilityData();

// Hot-reload during development
Definitions.Reload();

// Check for validation errors
var result = Definitions.GetLastLoadResult();
if (!result.Success)
{
    foreach (var error in result.Errors)
        SimLog.Log($"ERROR: {error}");
}

// Use ConfigRegistry directly for fail-fast mode
var registry = new ConfigRegistry { FailFastOnErrors = true };
registry.Load(); // Throws on validation errors
```

## Dependencies

- **Imports from**: Nothing
- **Imported by**: `src/sim/combat/` (MissionFactory, MissionView)

## Conventions

- All IDs are lowercase snake_case strings
- Definitions are dictionaries with string keys
- `Get(id)` returns default if ID not found (fail-safe)
- Keep data-only; no behavior beyond construction
- JSON files use camelCase property names (auto-mapped)
