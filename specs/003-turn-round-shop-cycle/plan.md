# Implementation Plan: Turn-Round-Shop Core Cycle

**Branch**: `003-turn-round-shop-cycle` | **Date**: 2026-02-27 | **Spec**: `/Users/emre/Desktop/MM-Projects/Pinvestor/specs/003-turn-round-shop-cycle/spec.md`
**Input**: Feature specification from `/specs/003-turn-round-shop-cycle/spec.md`

## Summary

Introduce a structured turn cycle and round progression layer on top of existing `GameManager` and
`Turn` flow:

- Explicit per-turn phases (Offer -> Placement -> Launch -> Resolution -> ShopPlaceholder)
- Round config (turn count + required worth) with round boundary checks
- Shop phase exists as placeholder contract only

This is a cycle foundation pass, not full shop system implementation.

## Technical Context

**Language/Version**: C# (Unity)  
**Primary Dependencies**: Existing `GameManager`, `Turn`, EventBus, Singleton, UniTask, MEC, MMFramework FSM (optional)  
**Storage**: ScriptableObject settings for cycle/round definitions  
**Testing**: Unity compile + play-mode smoke and deterministic log/phase checks  
**Target Platform**: Unity runtime  
**Project Type**: Unity game project  
**Constraints**: No new FSM framework, reuse existing architecture, no runtime `AddComponent`, no `FindObjectOfType` in gameplay flow  
**Scope**: Core cycle only; shop behavior is placeholder

## Constitution Check

- ✅ Uses existing architecture (manager/container orchestration + domain logic split).
- ✅ No new framework introduced; MMFSM remains the only FSM option if needed.
- ✅ Explicit phase sequencing and diagnostics are included.
- ✅ No bypass of EventBus/Singleton conventions.

## Project Structure

### Documentation (this feature)

```text
specs/003-turn-round-shop-cycle/
├── spec.md
├── plan.md
└── tasks.md
```

### Source Code (repository root)

```text
Assets/Scripts/Game/
├── GameFSM/
│   ├── GameManager.cs
│   └── GameFSM.cs                      # optional follow-up for FSM orchestration
├── Turn.cs                             # explicit phase sequencing
└── RunCycle/
    ├── RunCycleSettings.cs             # round config ScriptableObject
    └── [future] TurnCycleFSM states    # only if we migrate to MMFSM-driven cycle
```

**Structure Decision**: Start by extending `GameManager` + `Turn` directly for explicit phase and
round cycle semantics. MMFSM integration remains an optional second step if transition complexity
increases.

## Phase Plan

### Phase 1 - Config and Runtime State Foundations

- Add `RunCycleSettings` ScriptableObject with ordered rounds:
  - `turnCount`
  - `requiredWorth`
- Add runtime state tracking in `GameManager`:
  - current round index
  - current turn index in round

### Phase 2 - Turn Phase Decomposition

- Refactor `Turn.StartAsync()` into explicit internal phase methods:
  - OfferPhase
  - PlacementPhase
  - LaunchPhase
  - ResolutionPhase
  - ShopPhasePlaceholder
- Keep existing offer/placement/launch behavior intact.
- Add clear diagnostics for phase boundaries.

### Phase 3 - Round Boundary and Worth Checks

- Execute finite round/turn loop from `GameManager` instead of infinite while-loop when settings provided.
- At round end:
  - evaluate required worth (using configured provider if assigned)
  - log pass/fail or "provider missing" diagnostics
- Keep fail behavior non-destructive for this pass (no hard stop/game over rewrite yet) unless explicitly required.

### Phase 4 - Shop Placeholder Contract

- Ensure post-resolution placeholder hook is invoked every turn.
- Placeholder should be side-effect safe and non-blocking.

## MMFSM Adoption Note

For this pass, direct `GameManager` + `Turn` orchestration is acceptable and lower risk.
If phase transitions or branching expand, migrate to MMFSM states using existing `MMFSM` + `State`
components only. Do not introduce a separate FSM system.

## Risks and Mitigations

- **Risk**: Missing net worth source causes false round failures.
  - **Mitigation**: nullable provider path + explicit diagnostics.
- **Risk**: Refactor breaks offer/placement launch timing.
  - **Mitigation**: preserve existing calls and split into phase methods only.
- **Risk**: Cycle config invalid values.
  - **Mitigation**: safe defaults and validation guard checks.

