# Travel Tests (`tests/sim/travel/`)

Unit and integration tests for the Travel domain.

## Test Files

| File | Tests |
|------|-------|
| `TV1TravelCostsTests.cs` | Cost calculation formulas |
| `TV1TravelSegmentTests.cs` | Segment creation and properties |
| `TV1TravelPlannerTests.cs` | Pathfinding and plan creation |
| `TV2TravelStateTests.cs` | Travel state tracking and completion |
| `TV2TravelResultTests.cs` | Result factory methods and status |
| `TV2TravelExecutorTests.cs` | Execution, fuel consumption, interrupts |

## Running Tests

```powershell
.\addons\gdUnit4\runtest.cmd --godot_binary "E:\Godot\4.5\godot.exe" -a tests/sim/travel
```
