# Post-MG4 Cleanup Spec

Technical debt and refactoring opportunities identified during MG4 implementation.

**Status**: ✅ All actionable items completed

---

## Completed Refactorings

### ~~2. Duplicate Resource Modification Patterns~~ ✅

**Fixed**: Refactored `ApplyJobReward()` to use `AddCredits()`, `AddParts()`, `AddFuel()`, `AddAmmo()` instead of duplicating resource modification logic.

---

### ~~3. Magic String for Crew Targeting Parameter~~ ✅

**Fixed**: 
- Added `EncounterParams.LastCheckCrewId` constant in `EncounterInstance.cs`
- Updated `EncounterRunner.ResolveOutcome()` to store crew ID after skill checks
- Updated `CampaignState.GetTargetCrewForEffect()` to use the constant

---

### ~~4. No XP Event for Encounter XP Gain~~ ✅

**Fixed**:
- Added `CrewXpGainedEvent` to `Events.cs`
- Emit event in `ApplyCrewXpEffect()` (encounter XP)
- Emit event in `ApplyMissionOutput()` (mission XP)

---

## Remaining Issues

### 1. CampaignState is Too Large (~1900 lines)

**Relevance: HIGH** | **Status: Deferred**

`CampaignState.cs` handles too many responsibilities:
- Resource management
- Crew operations
- Ship operations
- Inventory operations
- Job management
- Faction reputation
- Campaign flags
- Mission output application
- Encounter effect application
- Travel integration
- Serialization

**Problem**: Violates single responsibility principle. Hard to navigate and maintain.

**Recommendation**: Extract into focused service classes when adding MG5+:
- `CampaignResourceService` - resource operations
- `CampaignCrewService` - crew hire/fire/traits/injuries
- `CampaignEncounterService` - encounter effect application
- Keep `CampaignState` as the data holder

**When to address**: Before adding significant new features to CampaignState.

---

## Summary

| Issue | Relevance | Status |
|-------|-----------|--------|
| CampaignState too large | HIGH | Deferred |
| ~~Duplicate resource patterns~~ | ~~MEDIUM~~ | ✅ Fixed |
| ~~Magic string for crew targeting~~ | ~~MEDIUM~~ | ✅ Fixed |
| ~~No XP gain event~~ | ~~LOW~~ | ✅ Fixed |
