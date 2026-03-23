# Spec 004: Economy & Resolution Phase

**Status**: Planned
**Created**: 2026-03-24
**Dependencies**: Spec 001 (GameConfig — RPH, op-cost, target worth), Spec 003 (Turn-Round cycle — resolution phase hook)

## Overview

Implement the working economy loop: aggregate turn revenue from ball hits, deduct per-company operational costs at resolution, track net worth across turns, and evaluate win/loss conditions at round end.

This spec completes the Resolution Phase (currently a non-blocking placeholder in `Turn.cs`) and makes the run economically meaningful.

## Codebase Audit (check before implementing)

Agents **must** read these files before writing any code:

- `Assets/Scripts/RevenueGenerator/RevenueGenerator.cs` — revenue-per-hit logic already exists; extend, do not replace
- `Assets/Scripts/Game/Turn.cs` — `RunResolutionPhase()` is the hook; `RunOfferPhase/Launch` etc. are already implemented
- `Assets/Scripts/Game/RunCycle/Round.cs` — round-end worth check scaffold is here
- `Assets/Scripts/Game/GameFSM/GameManager.cs` — net worth source references and round loop are here
- `Assets/Scripts/GameConfig/Runtime/Models/GameConfigDomainModels.cs` — `CompanyConfigModel` has RPH/op-cost fields
- `Assets/Scripts/GameConfig/Runtime/GameConfigServices.cs` — service accessors for company config

## User Stories

### US1 — Turn Revenue Aggregation (P1)

As the game loop, after each Launch Phase, I need to calculate total turn revenue from all ball hits so the player's wealth changes each turn.

**Acceptance criteria:**
- Turn revenue = `sum over all companies of (hits_this_turn × company.RPH)`
- Revenue is accumulated during the Launch Phase as hits occur
- Final turn revenue is logged and added to player net worth at resolution

### US2 — Operational Cost Deduction (P1)

As the game loop, during Resolution Phase, I need to deduct each company's operational cost from net worth so the player must balance revenue vs costs.

**Acceptance criteria:**
- Each placed company deducts its `OperationalCost` once per turn at resolution
- Net cost = total op costs across all companies on board
- Deduction happens after revenue credit
- If net worth goes negative, it stays negative (no floor for now)

### US3 — Net Worth Tracking (P1)

As the game, I need a persistent net worth value across the entire run so win/loss conditions can be evaluated.

**Acceptance criteria:**
- Net worth starts at the run's initial capital (from GameConfig)
- Increases by turn revenue each resolution
- Decreases by total operational costs each resolution
- Accessible to UI and other systems via a central runtime accessor (no static mutable state)

### US4 — Win/Loss Condition Evaluation (P1)

As the game, at the end of the final round I need to check if the player has reached the target net worth to determine outcome.

**Acceptance criteria:**
- Win: net worth >= target worth at end of final round
- Loss: net worth < target worth at end of final round (or all companies collapsed with no recovery path)
- Outcome emitted as an event via EventBus (`RunOutcomeEvent` or similar)
- GameManager reacts to outcome event (log + stop run for now; no UI required in this spec)

## Scope

**In scope:**
- `TurnEconomyCalculator` or equivalent service: accumulates hit revenue during launch phase
- `PlayerEconomyState` (or similar) runtime model: holds net worth, exposes read API
- Resolution phase implementation in `Turn.cs` → deducts op costs, credits revenue, updates net worth
- Win/loss evaluation at round end in `Round.cs` or `GameManager.cs`
- `RunOutcomeEvent` on EventBus

**Out of scope:**
- UI for net worth display (spec 005 may include minimal HUD, but full UI is separate)
- Cashout mechanic (spec 006)
- Synergy RPH modifiers (spec 007)
- Booster economy modifiers (spec 008)

## Key Entities

| Entity | Type | Purpose |
|---|---|---|
| `TurnRevenueAccumulator` | Runtime service | Tracks hit events during launch phase, sums RPH per company |
| `PlayerEconomyState` | Runtime model (Singleton-accessible) | Holds net worth, initial capital, turn revenue snapshot |
| `EconomyService` | Runtime service | Applies revenue credit + op-cost deduction at resolution |
| `RunOutcomeEvent` | EventBus event | Signals win/loss at end of final round |

## Notes

- RPH and OperationalCost values come from `GameConfigManager` via company config — do not embed hardcoded values
- The `RevenueGenerator.cs` already fires `OnRevenueGenerated` on hit; the accumulator should subscribe to this, not reimplement hit detection
- Round-end worth threshold is already partially checked in `Round.cs` — align with or extend that logic rather than duplicating it
