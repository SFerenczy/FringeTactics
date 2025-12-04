# SF1 – Time System: Implementation Plan

This document breaks down **Milestone SF1** from `ROADMAP.md` into concrete implementation steps.

**Goal**: Unified time representation for both campaign (days) and tactical (ticks) layers, with explicit time advancement APIs.

---

## Current State Assessment

### What We Have (After SF1 Implementation)

| Component | Status | Notes |
|-----------|--------|-------|
| `TimeSystem` | ✅ Complete | Tactical tick system (20 ticks/sec), pause/resume, time scale |
| `CombatState.TimeSystem` | ✅ Complete | Each combat instance owns its TimeSystem |
| `CampaignTime` | ✅ Complete | Campaign day tracking with advancement API |
| `CampaignState.Time` | ✅ Complete | Owns CampaignTime, Rest() action |
| `TravelSystem` | ✅ Complete | Fuel cost + time cost based on distance |
| `Job` | ✅ Complete | DeadlineDays, DeadlineDay, HasDeadline |

### SF1 Requirements - All Complete

| SF1 Requirement | Status | Implementation |
|-----------------|--------|----------------|
| Campaign day tracking | ✅ Complete | `CampaignTime.CurrentDay` |
| Tactical tick tracking | ✅ Complete | `TimeSystem.CurrentTick` |
| Time advancement API (campaign) | ✅ Complete | `CampaignTime.AdvanceDays()` |
| Time advancement API (tactical) | ✅ Complete | `TimeSystem.Update()` |
| Time queries | ✅ Complete | `GameState.GetCampaignDay()`, `GetTacticalTick()` |
| Mission time cost | ✅ Complete | `ConsumeMissionResources()` advances 1 day |
| Travel time cost | ✅ Complete | `TravelSystem.Travel()` advances days |
| Rest/downtime action | ✅ Complete | `CampaignState.Rest()` heals + advances 3 days |

---

## Architecture Decisions

### Unified vs Separate Time Classes

**Decision**: Keep tactical and campaign time **separate but coordinated**.

**Rationale**:
- Tactical time (ticks) is fundamentally different from campaign time (days)
- Tactical time runs in real-time during missions; campaign time is discrete
- Per CAMPAIGN_FOUNDATIONS 5.1: "Time only passes during actions"
- Tactical `TimeSystem` already works well; don't break it

**Structure**:
- `CampaignTime` - New class for campaign day tracking
- `TimeSystem` - Existing class for tactical ticks (unchanged)
- `GameTime` - Optional facade that provides unified queries

### Campaign Time Granularity

**Decision**: Use **integer days** as the base unit.

**Rationale**:
- Per CAMPAIGN_FOUNDATIONS 5.1: "Time is modeled in days at campaign level"
- Simpler than hours; avoids micro-optimization gameplay
- Deadlines and durations are naturally expressed in days
- Can add finer granularity later if needed

### Time Advancement Model

**Decision**: Time advances **only through explicit actions**.

**Rationale**:
- Per CAMPAIGN_FOUNDATIONS 5.1: "No time passes while browsing UI"
- Actions that advance time: Travel, Rest, Missions, some Management actions
- This prevents "time pressure anxiety" while still having meaningful deadlines

### Where Time State Lives

**Decision**: `CampaignTime` is owned by `CampaignState`.

**Rationale**:
- Campaign time is campaign state; it should serialize with the campaign
- `CombatState` already owns its `TimeSystem`; parallel structure
- `GameState` can provide unified access if needed

---

## Implementation Steps

### Phase 1: Campaign Time Foundation (Priority: Critical)

#### Step 1.1: Create CampaignTime Class

**New File**: `src/sim/CampaignTime.cs`

**Purpose**: Track campaign day and provide time advancement API.

```csharp
using System;

namespace FringeTactics;

/// <summary>
/// Tracks campaign time in days.
/// Time only advances through explicit actions (travel, rest, missions).
/// </summary>
public class CampaignTime
{
    /// <summary>
    /// Current campaign day (1-indexed, day 1 is campaign start).
    /// </summary>
    public int CurrentDay { get; private set; } = 1;
    
    /// <summary>
    /// Total days elapsed since campaign start.
    /// </summary>
    public int DaysElapsed => CurrentDay - 1;
    
    // C# Events
    public event Action<int, int> DayAdvanced; // (oldDay, newDay)
    
    public CampaignTime()
    {
        CurrentDay = 1;
    }
    
    public CampaignTime(int startDay)
    {
        CurrentDay = Math.Max(1, startDay);
    }
    
    /// <summary>
    /// Advance time by a number of days.
    /// </summary>
    /// <param name="days">Number of days to advance (must be positive).</param>
    /// <returns>The new current day.</returns>
    public int AdvanceDays(int days)
    {
        if (days <= 0)
        {
            SimLog.Log($"[CampaignTime] Warning: Attempted to advance by {days} days (ignored)");
            return CurrentDay;
        }
        
        int oldDay = CurrentDay;
        CurrentDay += days;
        
        SimLog.Log($"[CampaignTime] Day {oldDay} -> Day {CurrentDay} (+{days} days)");
        DayAdvanced?.Invoke(oldDay, CurrentDay);
        
        return CurrentDay;
    }
    
    /// <summary>
    /// Check if a deadline (absolute day) has passed.
    /// </summary>
    public bool HasDeadlinePassed(int deadlineDay)
    {
        return CurrentDay > deadlineDay;
    }
    
    /// <summary>
    /// Get days remaining until a deadline.
    /// Returns 0 if deadline has passed.
    /// </summary>
    public int DaysUntilDeadline(int deadlineDay)
    {
        return Math.Max(0, deadlineDay - CurrentDay);
    }
    
    /// <summary>
    /// Get state for serialization.
    /// </summary>
    public CampaignTimeState GetState()
    {
        return new CampaignTimeState { CurrentDay = CurrentDay };
    }
    
    /// <summary>
    /// Restore from saved state.
    /// </summary>
    public void RestoreState(CampaignTimeState state)
    {
        CurrentDay = state.CurrentDay;
        SimLog.Log($"[CampaignTime] Restored to day {CurrentDay}");
    }
}

/// <summary>
/// Serializable state for campaign time.
/// </summary>
public class CampaignTimeState
{
    public int CurrentDay { get; set; } = 1;
}
```

**Acceptance Criteria**:
- [ ] `CampaignTime` class with `CurrentDay` property
- [ ] `AdvanceDays()` increments day and emits event
- [ ] `HasDeadlinePassed()` and `DaysUntilDeadline()` work correctly
- [ ] `GetState()` / `RestoreState()` for serialization
- [ ] Negative day advancement is rejected

---

#### Step 1.2: Integrate CampaignTime into CampaignState

**File**: `src/sim/campaign/CampaignState.cs`

**Changes**:

```csharp
public class CampaignState
{
    // NEW: Time tracking
    public CampaignTime Time { get; private set; } = new();
    
    // ... existing fields ...
    
    public static CampaignState CreateNew(int sectorSeed = 12345)
    {
        var campaign = new CampaignState
        {
            // ... existing initialization ...
            Time = new CampaignTime() // Start at day 1
        };
        // ... rest of method ...
    }
}
```

**Acceptance Criteria**:
- [ ] `CampaignState.Time` property exists
- [ ] New campaigns start at day 1
- [ ] Time is accessible from campaign state

---

#### Step 1.3: Add Time Cost to Travel

**File**: `src/sim/campaign/TravelSystem.cs`

**Current State**: Travel only costs fuel.

**Changes**:

```csharp
public static class TravelSystem
{
    // NEW: Time cost per distance unit
    public const float DAYS_PER_DISTANCE_UNIT = 0.5f; // 2 distance = 1 day
    public const int MIN_TRAVEL_DAYS = 1; // Minimum 1 day per travel
    
    /// <summary>
    /// Calculate travel time in days between two nodes.
    /// </summary>
    public static int CalculateTravelDays(SectorNode from, SectorNode to)
    {
        float distance = from.Position.DistanceTo(to.Position);
        int days = (int)Math.Ceiling(distance * DAYS_PER_DISTANCE_UNIT);
        return Math.Max(MIN_TRAVEL_DAYS, days);
    }
    
    /// <summary>
    /// Execute travel between nodes.
    /// Returns true if travel succeeded.
    /// </summary>
    public static bool Travel(CampaignState campaign, int targetNodeId)
    {
        // ... existing validation ...
        
        var from = campaign.GetCurrentNode();
        var to = campaign.Sector.GetNode(targetNodeId);
        
        // Calculate and apply costs
        int fuelCost = CalculateFuelCost(from, to);
        int timeCost = CalculateTravelDays(from, to);
        
        if (campaign.Fuel < fuelCost)
        {
            SimLog.Log($"[Travel] Not enough fuel: need {fuelCost}, have {campaign.Fuel}");
            return false;
        }
        
        // Apply costs
        campaign.Fuel -= fuelCost;
        campaign.Time.AdvanceDays(timeCost);
        campaign.CurrentNodeId = targetNodeId;
        
        SimLog.Log($"[Travel] Traveled to {to.Name}. Cost: {fuelCost} fuel, {timeCost} days.");
        return true;
    }
}
```

**Acceptance Criteria**:
- [ ] `CalculateTravelDays()` returns time cost based on distance
- [ ] `Travel()` advances campaign time
- [ ] Minimum 1 day per travel
- [ ] Travel log shows time cost

---

### Phase 2: Time-Consuming Actions (Priority: High)

#### Step 2.1: Add Mission Time Cost

**File**: `src/sim/campaign/CampaignState.cs`

**Changes**:

```csharp
public class CampaignState
{
    // Mission time cost
    public const int MISSION_TIME_DAYS = 1; // Each mission takes 1 day
    
    /// <summary>
    /// Consume resources when starting a mission.
    /// </summary>
    public void ConsumeMissionResources()
    {
        Ammo -= MISSION_AMMO_COST;
        Fuel -= MISSION_FUEL_COST;
        Time.AdvanceDays(MISSION_TIME_DAYS); // NEW: Time cost
        SimLog.Log($"[Campaign] Mission started. Cost: {MISSION_AMMO_COST} ammo, {MISSION_FUEL_COST} fuel, {MISSION_TIME_DAYS} day(s).");
    }
}
```

**Design Note**: Mission time is consumed at mission START, not end. This prevents "save scumming" by reloading before mission to avoid time cost.

**Acceptance Criteria**:
- [ ] Starting a mission advances campaign time
- [ ] Time cost is logged

---

#### Step 2.2: Add Rest/Downtime Action

**New Method in**: `src/sim/campaign/CampaignState.cs`

**Purpose**: Explicit rest action that heals injuries and advances time.

```csharp
public class CampaignState
{
    // Rest configuration
    public const int REST_TIME_DAYS = 3;
    public const int REST_HEAL_AMOUNT = 1; // Injuries healed per rest
    
    /// <summary>
    /// Rest at current location. Heals injuries, advances time.
    /// </summary>
    /// <returns>Number of injuries healed.</returns>
    public int Rest()
    {
        int healed = 0;
        
        // Heal one injury per alive crew member (up to REST_HEAL_AMOUNT total)
        foreach (var crew in GetAliveCrew())
        {
            if (healed >= REST_HEAL_AMOUNT) break;
            if (crew.Injuries.Count > 0)
            {
                var injury = crew.Injuries[0];
                crew.HealInjury(injury);
                healed++;
                SimLog.Log($"[Campaign] {crew.Name}'s {injury} healed during rest.");
            }
        }
        
        Time.AdvanceDays(REST_TIME_DAYS);
        SimLog.Log($"[Campaign] Rested for {REST_TIME_DAYS} days. Healed {healed} injuries.");
        
        return healed;
    }
    
    /// <summary>
    /// Check if rest would be beneficial (any injuries to heal).
    /// </summary>
    public bool ShouldRest()
    {
        foreach (var crew in GetAliveCrew())
        {
            if (crew.Injuries.Count > 0) return true;
        }
        return false;
    }
}
```

**Acceptance Criteria**:
- [ ] `Rest()` method heals injuries and advances time
- [ ] `ShouldRest()` indicates if rest is beneficial
- [ ] Rest is logged with details

---

#### Step 2.3: Add Job Deadlines

**File**: `src/sim/campaign/Job.cs`

**Changes**:

```csharp
public class Job
{
    // ... existing fields ...
    
    // NEW: Deadline tracking
    /// <summary>
    /// Absolute day by which the job must be completed.
    /// 0 means no deadline.
    /// </summary>
    public int DeadlineDay { get; set; } = 0;
    
    /// <summary>
    /// Days from acceptance until deadline.
    /// Used when generating jobs; converted to absolute day on acceptance.
    /// </summary>
    public int DeadlineDays { get; set; } = 0;
    
    /// <summary>
    /// Check if this job has a deadline.
    /// </summary>
    public bool HasDeadline => DeadlineDay > 0;
}
```

**File**: `src/sim/campaign/CampaignState.cs`

**Update AcceptJob**:

```csharp
public bool AcceptJob(Job job)
{
    // ... existing validation ...
    
    CurrentJob = job;
    AvailableJobs.Remove(job);
    
    // NEW: Set absolute deadline from relative days
    if (job.DeadlineDays > 0)
    {
        job.DeadlineDay = Time.CurrentDay + job.DeadlineDays;
        SimLog.Log($"[Campaign] Job deadline: Day {job.DeadlineDay} ({job.DeadlineDays} days from now)");
    }
    
    // ... rest of method ...
}
```

**File**: `src/sim/campaign/JobSystem.cs`

**Update job generation to include deadlines**:

```csharp
public static List<Job> GenerateJobsForNode(Sector sector, int nodeId, Random rng)
{
    // ... existing generation ...
    
    foreach (var job in jobs)
    {
        // Set deadline based on difficulty
        job.DeadlineDays = job.Difficulty switch
        {
            JobDifficulty.Easy => rng.Next(5, 10),    // 5-9 days
            JobDifficulty.Medium => rng.Next(7, 14),  // 7-13 days
            JobDifficulty.Hard => rng.Next(10, 20),   // 10-19 days
            _ => 7
        };
    }
    
    return jobs;
}
```

**Acceptance Criteria**:
- [ ] Jobs have `DeadlineDay` and `DeadlineDays` fields
- [ ] Accepting a job sets absolute deadline
- [ ] Job generation includes deadline based on difficulty
- [ ] Deadline is logged on acceptance

---

### Phase 3: Time Queries & Display (Priority: Medium)

#### Step 3.1: Add Time Query Methods

**File**: `src/sim/CampaignTime.cs`

**Additional methods**:

```csharp
public class CampaignTime
{
    // ... existing code ...
    
    /// <summary>
    /// Format current day for display.
    /// </summary>
    public string FormatCurrentDay()
    {
        return $"Day {CurrentDay}";
    }
    
    /// <summary>
    /// Format a duration in days.
    /// </summary>
    public static string FormatDuration(int days)
    {
        if (days == 1) return "1 day";
        return $"{days} days";
    }
    
    /// <summary>
    /// Format deadline status.
    /// </summary>
    public string FormatDeadlineStatus(int deadlineDay)
    {
        int remaining = DaysUntilDeadline(deadlineDay);
        if (remaining == 0) return "OVERDUE";
        if (remaining == 1) return "1 day left";
        return $"{remaining} days left";
    }
}
```

**Acceptance Criteria**:
- [ ] `FormatCurrentDay()` returns "Day N"
- [ ] `FormatDuration()` handles singular/plural
- [ ] `FormatDeadlineStatus()` shows remaining time or OVERDUE

---

#### Step 3.2: Expose Time in UI-Accessible Locations

**File**: `src/core/GameState.cs`

**Add convenience accessors**:

```csharp
public class GameState
{
    // ... existing code ...
    
    /// <summary>
    /// Get current campaign day (0 if no campaign).
    /// </summary>
    public int GetCampaignDay()
    {
        return Campaign?.Time?.CurrentDay ?? 0;
    }
    
    /// <summary>
    /// Get current tactical tick (0 if no mission).
    /// </summary>
    public int GetTacticalTick()
    {
        return CurrentMission?.TimeSystem?.CurrentTick ?? 0;
    }
}
```

**Acceptance Criteria**:
- [ ] `GameState.GetCampaignDay()` returns current day
- [ ] `GameState.GetTacticalTick()` returns current tick
- [ ] Both return 0 when not applicable

---

### Phase 4: Integration with Existing TimeSystem (Priority: Medium)

#### Step 4.1: Document TimeSystem Integration Points

The existing `TimeSystem` (tactical) is already well-designed. No changes needed, but document integration:

**Integration Points**:

| Layer | Time Class | Granularity | Advances When |
|-------|-----------|-------------|---------------|
| Campaign | `CampaignTime` | Days | Travel, Rest, Mission Start |
| Tactical | `TimeSystem` | Ticks (50ms) | Real-time during mission |

**Key Principle**: Tactical time is "inside" a campaign day. A mission consumes 1 campaign day regardless of how many tactical ticks occur.

**No changes to TimeSystem needed for SF1.**

---

#### Step 4.2: Add Mission Duration Tracking (Optional)

**File**: `src/sim/combat/state/CombatState.cs`

**Optional enhancement**: Track mission duration in tactical time.

```csharp
public class CombatState
{
    // ... existing code ...
    
    /// <summary>
    /// Get mission duration in seconds (tactical time).
    /// </summary>
    public float GetMissionDurationSeconds()
    {
        return TimeSystem.GetCurrentTime();
    }
    
    /// <summary>
    /// Get mission duration formatted for display.
    /// </summary>
    public string GetMissionDurationFormatted()
    {
        float seconds = GetMissionDurationSeconds();
        int minutes = (int)(seconds / 60);
        int secs = (int)(seconds % 60);
        return $"{minutes}:{secs:D2}";
    }
}
```

**Acceptance Criteria**:
- [ ] `GetMissionDurationSeconds()` returns tactical time elapsed
- [ ] `GetMissionDurationFormatted()` returns "M:SS" format

---

### Phase 5: Testing (Priority: High)

#### Step 5.1: Create CampaignTime Unit Tests

**New File**: `tests/sim/SF1TimeTests.cs`

```csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FringeTactics;

namespace FringeTactics.Tests;

[TestClass]
public class CampaignTimeTests
{
    [TestMethod]
    public void NewCampaignTime_StartsAtDayOne()
    {
        var time = new CampaignTime();
        Assert.AreEqual(1, time.CurrentDay);
        Assert.AreEqual(0, time.DaysElapsed);
    }
    
    [TestMethod]
    public void AdvanceDays_IncrementsCurrentDay()
    {
        var time = new CampaignTime();
        time.AdvanceDays(3);
        Assert.AreEqual(4, time.CurrentDay);
        Assert.AreEqual(3, time.DaysElapsed);
    }
    
    [TestMethod]
    public void AdvanceDays_EmitsEvent()
    {
        var time = new CampaignTime();
        int eventOldDay = 0;
        int eventNewDay = 0;
        time.DayAdvanced += (old, @new) => { eventOldDay = old; eventNewDay = @new; };
        
        time.AdvanceDays(5);
        
        Assert.AreEqual(1, eventOldDay);
        Assert.AreEqual(6, eventNewDay);
    }
    
    [TestMethod]
    public void AdvanceDays_RejectsNegative()
    {
        var time = new CampaignTime();
        time.AdvanceDays(-5);
        Assert.AreEqual(1, time.CurrentDay); // Unchanged
    }
    
    [TestMethod]
    public void AdvanceDays_RejectsZero()
    {
        var time = new CampaignTime();
        time.AdvanceDays(0);
        Assert.AreEqual(1, time.CurrentDay); // Unchanged
    }
    
    [TestMethod]
    public void HasDeadlinePassed_ReturnsTrueWhenPast()
    {
        var time = new CampaignTime();
        time.AdvanceDays(10); // Now day 11
        
        Assert.IsTrue(time.HasDeadlinePassed(5));
        Assert.IsTrue(time.HasDeadlinePassed(10));
        Assert.IsFalse(time.HasDeadlinePassed(11));
        Assert.IsFalse(time.HasDeadlinePassed(15));
    }
    
    [TestMethod]
    public void DaysUntilDeadline_ReturnsCorrectValue()
    {
        var time = new CampaignTime();
        time.AdvanceDays(4); // Now day 5
        
        Assert.AreEqual(0, time.DaysUntilDeadline(3));  // Past
        Assert.AreEqual(0, time.DaysUntilDeadline(5));  // Today
        Assert.AreEqual(5, time.DaysUntilDeadline(10)); // Future
    }
    
    [TestMethod]
    public void SaveRestore_RoundTrip()
    {
        var time = new CampaignTime();
        time.AdvanceDays(15);
        
        var state = time.GetState();
        
        var time2 = new CampaignTime();
        time2.RestoreState(state);
        
        Assert.AreEqual(16, time2.CurrentDay);
    }
}

[TestClass]
public class CampaignTimeIntegrationTests
{
    [TestMethod]
    public void CampaignState_HasTimeProperty()
    {
        var campaign = CampaignState.CreateNew();
        Assert.IsNotNull(campaign.Time);
        Assert.AreEqual(1, campaign.Time.CurrentDay);
    }
    
    [TestMethod]
    public void Travel_AdvancesTime()
    {
        var campaign = CampaignState.CreateNew();
        int startDay = campaign.Time.CurrentDay;
        
        // Find a connected node to travel to
        var currentNode = campaign.GetCurrentNode();
        if (currentNode.Connections.Count > 0)
        {
            int targetId = currentNode.Connections[0];
            TravelSystem.Travel(campaign, targetId);
            
            Assert.IsTrue(campaign.Time.CurrentDay > startDay);
        }
    }
    
    [TestMethod]
    public void Rest_AdvancesTime()
    {
        var campaign = CampaignState.CreateNew();
        int startDay = campaign.Time.CurrentDay;
        
        campaign.Rest();
        
        Assert.AreEqual(startDay + CampaignState.REST_TIME_DAYS, campaign.Time.CurrentDay);
    }
    
    [TestMethod]
    public void AcceptJob_SetsAbsoluteDeadline()
    {
        var campaign = CampaignState.CreateNew();
        campaign.Time.AdvanceDays(5); // Now day 6
        
        if (campaign.AvailableJobs.Count > 0)
        {
            var job = campaign.AvailableJobs[0];
            int relativeDays = job.DeadlineDays;
            
            campaign.AcceptJob(job);
            
            if (relativeDays > 0)
            {
                Assert.AreEqual(6 + relativeDays, job.DeadlineDay);
            }
        }
    }
}
```

**Acceptance Criteria**:
- [x] All unit tests pass
- [x] Tests cover edge cases (negative days, zero days)
- [x] Integration tests verify campaign/travel/rest time costs

---

## Implementation Order

1. **Day 1: Core CampaignTime**
   - Step 1.1: Create CampaignTime class
   - Step 1.2: Integrate into CampaignState
   - Step 5.1: Basic unit tests

2. **Day 2: Time-Consuming Actions**
   - Step 2.1: Mission time cost
   - Step 2.2: Rest/downtime action
   - Step 2.3: Job deadlines

3. **Day 3: Travel & Polish**
   - Step 1.3: Travel time cost
   - Step 3.1: Time query methods
   - Step 3.2: GameState accessors

4. **Day 4: Testing & Integration**
   - Complete all tests
   - Integration testing
   - Bug fixes

---

## Success Criteria for SF1

When SF1 is complete, you should be able to:

1. ✅ See current campaign day in `CampaignState.Time.CurrentDay`
2. ✅ Travel between nodes advances time
3. ✅ Starting a mission advances time
4. ✅ Resting advances time and heals injuries
5. ✅ Jobs have deadlines that can expire
6. ✅ Query time from `GameState.GetCampaignDay()` and `GetTacticalTick()`
7. ✅ Save/load preserves campaign time

**Natural Pause Point**: After SF1, you have a working time system. Campaign actions have meaningful time costs, and deadlines create soft pressure without harsh penalties.

---

## Notes for Future Milestones

### SF2 Dependencies (Event Bus)
- `DayAdvanced` event could be published to event bus
- Other systems can subscribe to day changes (contract expiration, world sim ticks)

### SF3 Dependencies (Save/Load)
- `CampaignTimeState` is ready for serialization
- Include in campaign save data

### G1+ Dependencies
- World simulation may tick on day advance
- Contract chains may have time-based triggers
- Events may spawn based on day thresholds

---

## Open Questions

1. **Mission Time Variability**: Should different mission types take different amounts of time?
   - *Recommendation*: No for SF1. All missions = 1 day. Can add variability later.

2. **Travel Speed Upgrades**: Should ship upgrades reduce travel time?
   - *Recommendation*: Design for it (travel time is calculated, not hardcoded), implement in G2+.

3. **Time Display Format**: Should we show "Day 15" or "Week 2, Day 1"?
   - *Recommendation*: "Day N" for SF1. Weeks/months can be added as flavor later.

4. **Deadline Consequences**: What happens when a job deadline passes?
   - *Recommendation*: Job auto-fails with reputation penalty. Implement in SF1 or defer to G1.

5. **Simultaneous Deadlines**: Can player have multiple jobs with different deadlines?
   - *Recommendation*: No for now (one active job). Multi-job is G2+ feature.

---

## File Summary

| File | Action | Description |
|------|--------|-------------|
| `src/sim/CampaignTime.cs` | ✅ NEW | Campaign day tracking |
| `src/sim/campaign/CampaignState.cs` | ✅ MODIFIED | Add `Time` property, `Rest()`, mission time cost |
| `src/sim/campaign/TravelSystem.cs` | ✅ MODIFIED | Add travel time calculation and cost |
| `src/sim/campaign/Job.cs` | ✅ MODIFIED | Add deadline fields |
| `src/sim/campaign/JobSystem.cs` | ✅ MODIFIED | Generate jobs with deadlines |
| `src/core/GameState.cs` | ✅ MODIFIED | Add time query accessors |
| `tests/sim/foundation/SF1TimeTests.cs` | ✅ NEW | Unit and integration tests (31 tests) |

---

## Integration Points

### How Time Flows Through the System

```
┌─────────────────────────────────────────────────────────────────┐
│                        CAMPAIGN LAYER                           │
│                                                                 │
│  CampaignState.Time (CampaignTime)                              │
│  └── CurrentDay: int (1-indexed)                                │
│  └── AdvanceDays(n) → emits DayAdvanced event                   │
│                                                                 │
│  Time advances when:                                            │
│  • TravelSystem.Travel() → +1-N days based on distance          │
│  • CampaignState.ConsumeMissionResources() → +1 day             │
│  • CampaignState.Rest() → +3 days                               │
│                                                                 │
│  Jobs track deadlines:                                          │
│  • Job.DeadlineDays (relative, set at generation)               │
│  • Job.DeadlineDay (absolute, set at acceptance)                │
│  • CampaignTime.HasDeadlinePassed(deadlineDay)                  │
└─────────────────────────────────────────────────────────────────┘
                              │
                              │ Mission Start
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                        TACTICAL LAYER                           │
│                                                                 │
│  CombatState.TimeSystem (TimeSystem)                            │
│  └── CurrentTick: int (0-indexed)                               │
│  └── Update(dt) → advances ticks at 20/sec                      │
│  └── GetCurrentTime() → seconds elapsed                         │
│                                                                 │
│  Tactical time is independent of campaign time.                 │
│  A mission consumes 1 campaign day regardless of tactical time. │
└─────────────────────────────────────────────────────────────────┘
                              │
                              │ Mission End
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                        QUERY LAYER                              │
│                                                                 │
│  GameState (Autoload)                                           │
│  └── GetCampaignDay() → Campaign?.Time?.CurrentDay ?? 0         │
│  └── GetTacticalTick() → CurrentCombat?.TimeSystem?.CurrentTick │
│  └── GetCampaignDayFormatted() → "Day N"                        │
│  └── GetTacticalTimeFormatted() → "M:SS"                        │
└─────────────────────────────────────────────────────────────────┘
```

### Key Constants

| Constant | Value | Location |
|----------|-------|----------|
| `MISSION_TIME_DAYS` | 1 | CampaignState |
| `REST_TIME_DAYS` | 3 | CampaignState |
| `REST_HEAL_AMOUNT` | 1 | CampaignState |
| `MIN_TRAVEL_DAYS` | 1 | TravelSystem |
| `DAYS_PER_DISTANCE` | 0.02 | TravelSystem |
| `TicksPerSecond` | 20 | TimeSystem |

### Serialization Ready

- `CampaignTime.GetState()` → `CampaignTimeState { CurrentDay }`
- `CampaignTime.RestoreState(state)` → restores day
- Ready for SF3 (Save/Load) integration

---
