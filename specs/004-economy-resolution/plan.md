# Plan: Economy & Resolution Phase

**Spec**: `specs/004-economy-resolution/spec.md`
**Created**: 2026-03-24

## Technical Approach

The economy system is event-driven throughout. `RevenueGenerator.cs` already fires `OnRevenueGenerated` on each ball hit — the new `TurnRevenueAccumulator` subscribes to this and accumulates per-company revenue for the turn. A `PlayerEconomyState` singleton holds net worth across the run. `EconomyService` applies the resolution math (revenue credit + op-cost deduction) and writes back to `PlayerEconomyState`. Win/loss evaluation extends the existing round-end worth check in `Round.cs`.

No new Update loops. No static mutable state. All values from GameConfig.

## Architecture Decisions

### Revenue Accumulation During Launch Phase
`TurnRevenueAccumulator` subscribes to `RevenueGenerator.OnRevenueGenerated` at the start of each turn and unsubscribes at the end of the Launch Phase. It maps events to company instances and accumulates `hits × RPH` per company. At resolution, `EconomyService` reads the accumulated total.

**Why not accumulate in RevenueGenerator itself?** `RevenueGenerator` is a per-company component — it doesn't have a run-level view. The accumulator is a run-level service that aggregates across all generators.

### PlayerEconomyState as Singleton-accessible Runtime Model
Follows the same Singleton pattern as `GameConfigManager`. Holds net worth, initial capital, and last-turn revenue snapshot. Exposed read-only to UI and other systems; only `EconomyService` writes to it.

### Resolution Phase Hook in Turn.cs
`RunResolutionPhase()` is currently a non-blocking placeholder. This spec fills it in: it calls `EconomyService.ApplyResolution()` which handles the full calculation sequence. The resolution remains a single async await with no new phases.

### Win/Loss via RunOutcomeEvent
`Round.cs` already checks `RequiredWorth` at round end. For the final round, a `RunOutcomeEvent` (Win/Loss) is emitted on the EventBus. `GameManager` subscribes and stops the run. This keeps evaluation in the round layer and reaction in the manager layer.

## Phase Breakdown

### Phase 1: Net Worth State & Initial Capital
Set up `PlayerEconomyState` and initialize net worth from GameConfig at run start.

### Phase 2: Turn Revenue Accumulation
Implement `TurnRevenueAccumulator` — subscribes to hit events, accumulates per-turn revenue, resets each turn.

### Phase 3: Resolution Phase
Implement `EconomyService.ApplyResolution()` — credits revenue, deducts op-costs per placed company, updates `PlayerEconomyState`. Wire into `RunResolutionPhase()` in `Turn.cs`.

### Phase 4: Win/Loss Evaluation
Extend `Round.cs` final-round check to emit `RunOutcomeEvent`. `GameManager` handles outcome (log + stop run).

### Phase 5: Tests & Validation
EditMode tests for accumulation math, op-cost deduction, and win/loss conditions. Manual play-mode smoke test.

## Key Risks

- **RevenueGenerator subscription lifetime**: Must subscribe at turn start and unsubscribe cleanly — stale subscriptions across turns would double-count revenue.
- **Op-cost source**: Op-cost must come from `GameConfigManager` company config, not from any embedded ScriptableObject value. Confirm the config integration path before implementing.
- **Round.cs worth check already partial**: Read `Round.cs` carefully — it may already have a worth check that needs to be extended, not replaced.
