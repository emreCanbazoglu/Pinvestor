# Tasks: Turn-Round-Shop Core Cycle

**Input**: Design documents from `/specs/003-turn-round-shop-cycle/`
**Prerequisites**: `plan.md`, `spec.md`

## Phase 1: Setup (Cycle Config + Runtime State)

- [ ] T001 Create `Assets/Scripts/Game/RunCycle/RunCycleSettings.cs` ScriptableObject with rounds
- [ ] T002 Add runtime round/turn index tracking in `Assets/Scripts/Game/GameFSM/GameManager.cs`
- [ ] T003 Add optional net worth source references in `Assets/Scripts/Game/GameFSM/GameManager.cs` for round threshold checks

## Phase 2: Turn Phases (Core Loop)

- [ ] T004 Refactor `Assets/Scripts/Game/Turn.cs` into explicit phase methods (Offer, Placement, Launch, Resolution, ShopPlaceholder)
- [ ] T005 Keep existing company selection + placement behavior in Offer/Placement phase logic
- [ ] T006 Keep existing ball launch behavior in Launch phase logic
- [ ] T007 Add explicit non-blocking Resolution placeholder method in `Assets/Scripts/Game/Turn.cs`
- [ ] T008 Add explicit non-blocking Shop placeholder method in `Assets/Scripts/Game/Turn.cs`

## Phase 3: Round Progression

- [ ] T009 Replace infinite `while (true)` in `Assets/Scripts/Game/GameFSM/GameManager.cs` with config-driven round/turn loop when settings are present
- [ ] T010 Add round-end required worth evaluation in `Assets/Scripts/Game/GameFSM/GameManager.cs`
- [ ] T011 Add diagnostics for round start/end, turn start/end, and worth check results

## Phase 4: Validation

- [ ] T012 Compile via Unity MCP and clear console errors
- [ ] T013 Play-mode smoke test via Unity MCP to verify cycle order and placeholder shop phase execution

