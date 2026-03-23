# Tasks: Turn-Round-Shop Core Cycle

**Input**: Design documents from `/specs/003-turn-round-shop-cycle/`
**Prerequisites**: `plan.md`, `spec.md`

## Phase 1: Setup (Cycle Config + Runtime State)

- [x] T001 Create `Assets/Scripts/Game/RunCycle/RunCycleSettings.cs` ScriptableObject with rounds
- [x] T002 Add runtime round/turn index tracking in `Assets/Scripts/Game/GameFSM/GameManager.cs`
- [x] T003 Add optional net worth source references in `Assets/Scripts/Game/GameFSM/GameManager.cs` for round threshold checks

**Checkpoint**: ✅ RunCycleSettings, Round, RoundContext, RoundPhaseBase all present in `Assets/Scripts/Game/RunCycle/`

## Phase 2: Turn Phases (Core Loop)

- [x] T004 Refactor `Assets/Scripts/Game/Turn.cs` into explicit phase methods (Offer, Placement, Launch, Resolution)
- [x] T005 Keep existing company selection + placement behavior in Offer/Placement phase logic
- [x] T006 Keep existing ball launch behavior in Launch phase logic
- [x] T007 Add explicit non-blocking Resolution placeholder method in `Assets/Scripts/Game/Turn.cs`
- [x] T008 Add explicit non-blocking Shop placeholder — implemented as `ShopPlaceholderRoundPhase` in `Assets/Scripts/Game/RunCycle/RoundPhases.cs` (round-level, not turn-level, which is the correct architecture)

**Checkpoint**: ✅ `ETurnPhase` enum + all phase methods in `Turn.cs`; shop placeholder in `RoundPhases.cs`

## Phase 3: Round Progression

- [x] T009 Replace infinite `while (true)` in `Assets/Scripts/Game/GameFSM/GameManager.cs` with config-driven round/turn loop — `PlayConfiguredRunAsync()` drives rounds from `RoundCycleSettings[]`
- [x] T010 Add round-end required worth evaluation in `Assets/Scripts/Game/GameFSM/GameManager.cs` — `Round.cs` evaluates `RequiredWorth` and emits `RoundCompletedEvent`
- [x] T011 Add diagnostics for round start/end, turn start/end, and worth check results

**Checkpoint**: ✅ Full config-driven run cycle with worth evaluation and event signaling

## Phase 4: Validation

- [ ] T012 Compile via Unity MCP and clear console errors
- [ ] T013 Play-mode smoke test via Unity MCP to verify cycle order and placeholder shop phase execution
