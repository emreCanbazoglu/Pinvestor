# Tasks: Economy & Resolution Phase

**Input**: `specs/004-economy-resolution/spec.md`, `plan.md`
**Codebase audit required**: `RevenueGenerator.cs`, `Round.cs`, `GameManager.cs`, `GameConfigDomainModels.cs`

## Phase 1: Net Worth State & Initial Capital

- [x] T001 Read `Assets/Scripts/RevenueGenerator/RevenueGenerator.cs`, `Assets/Scripts/Game/RunCycle/Round.cs`, and `Assets/Scripts/Game/GameFSM/GameManager.cs` fully before writing any code
- [x] T002 Create `Assets/Scripts/Game/Economy/PlayerEconomyState.cs` — Singleton-accessible runtime model holding `NetWorth`, `InitialCapital`, `LastTurnRevenue`, `LastTurnOpCost`. Read-only API; only `EconomyService` writes.
- [x] T003 [P] Create `Assets/Scripts/Game/Economy/EconomyService.cs` — shell only; methods to be filled in Phase 3
- [x] T004 Initialize `PlayerEconomyState.InitialCapital` and `NetWorth` from GameConfig balance section in `GameManager.InitializeAsync()` (before first turn)

**Checkpoint**: `PlayerEconomyState` exists and is initialized with correct starting capital from config

---

## Phase 2: Turn Revenue Accumulation

- [x] T005 Create `Assets/Scripts/Game/Economy/TurnRevenueAccumulator.cs` — subscribes to `RevenueGenerator.OnRevenueGenerated` at turn start, accumulates `hits × RPH` per company, unsubscribes at end of launch phase
- [x] T006 Ensure `TurnRevenueAccumulator` is reset at the start of each turn (clear per-company totals)
- [x] T007 Expose `GetTotalTurnRevenue()` on `TurnRevenueAccumulator` for use by `EconomyService` at resolution

**Checkpoint**: Revenue is accumulated per-turn without leaking across turns

---

## Phase 3: Resolution Phase Implementation

- [x] T008 Implement `EconomyService.ApplyResolution(IEnumerable<PlacedCompany> placedCompanies)` — credits `TurnRevenueAccumulator.GetTotalTurnRevenue()` to net worth, then deducts each company's `OperationalCost` (from `GameConfigManager` company config)
- [x] T009 Replace the placeholder body in `Assets/Scripts/Game/Turn.cs` `RunResolutionPhase()` with a call to `EconomyService.ApplyResolution()`. Pass currently placed companies from board context.
- [x] T010 Add explicit log output in resolution: turn revenue, total op-cost, net worth before/after

**Checkpoint**: Net worth changes correctly each turn based on revenue and op-costs

---

## Phase 4: Win/Loss Evaluation

- [x] T011 Create `Assets/Scripts/Game/Events/RunOutcomeEvent.cs` — EventBus event with `IsWin` bool and final `NetWorth` value
- [x] T012 Extend `Assets/Scripts/Game/RunCycle/Round.cs` final-round evaluation: after resolution, if this is the last round, compare `PlayerEconomyState.NetWorth` against `RequiredWorth` and emit `RunOutcomeEvent` via EventBus
- [x] T013 Subscribe to `RunOutcomeEvent` in `Assets/Scripts/Game/GameFSM/GameManager.cs` — log outcome and stop run (no UI required in this spec)

**Checkpoint**: Run correctly terminates with Win or Loss outcome logged

---

## Phase 5: Tests & Validation

- [x] T014 [P] Add EditMode test for revenue accumulation math (multiple companies, multiple hits) in `Assets/Scripts/Game/Economy/Tests/TurnRevenueAccumulatorTests.cs`
- [x] T015 [P] Add EditMode test for `EconomyService.ApplyResolution()` — verify net worth delta equals revenue minus total op-costs
- [x] T016 [P] Add EditMode test for win condition (net worth >= target) and loss condition (net worth < target)
- [ ] T017 Manual play-mode smoke test: play 1 full turn, verify net worth changes by expected amount; play to final round, verify outcome event fires
