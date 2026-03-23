# Plan: Market News System

**Spec**: `specs/010-market-news/spec.md`
**Created**: 2026-03-24

## Technical Approach

Market News is a turn-resolution hook. `MarketNewsResolver` is called at the end of `RunResolutionPhase()` in `Turn.cs` to: (1) evaluate trigger conditions, (2) optionally select and activate a news event, and (3) decrement the duration of any active event and expire it if duration hits 0. News effects are temporary `IBoosterEffect` (or `IRunModifierEffect`) applications from spec 008/009's framework — the only new requirement is a remove-after-N-turns lifecycle that the framework must support if it only handles permanent modifiers.

## Architecture Decisions

### News Modifiers Are Temporary IBoosterEffect Applications
The modifier framework from spec 008 handles `Apply` and `Remove`. Market News just calls `Apply` on activation and `Remove` on expiry. If the framework is permanent-only (no built-in duration), `MarketNewsResolver` manages the duration counter itself and calls `Remove` when the counter reaches 0. Do not build a separate temporary modifier stack.

### Resolution-Phase Duration Countdown — No Update Loop
Duration ticks down once per resolution pass in `MarketNewsResolver.OnResolutionEnd()`. This is called from `RunResolutionPhase()` in `Turn.cs`. Zero new `Update` or coroutine loops.

### Single Active Event at a Time
`ActiveMarketNewsState` holds at most one event. If a new event triggers on the same turn an old one expires, the new event takes effect immediately. No queue.

### Trigger Evaluation Order
At the end of each resolution:
1. Decrement active event duration (if one exists). If 0 → expire it, emit `MarketNewsExpiredEvent`, call `Remove` on its effect.
2. Evaluate trigger conditions for a new event. If triggered → select from pool, emit `MarketNewsStartedEvent`, call `Apply` on its effect.

Both can happen in the same resolution pass (expire old → trigger new).

### Industry Target for SpecificIndustry Events
At trigger time, if the event's target type is `SpecificIndustry`, randomly select an industry from those currently represented on the board. If no companies are on board, skip the event gracefully.

### News Pool in GameConfig
`MarketNewsConfigSection` follows the same pattern as `RunThemeConfigSection` (spec 009). Each event entry: id, name, description, effectType, targetType, durationTurns, modifier values, triggerMode, triggerChance/interval.

## Phase Breakdown

### Phase 1: News Config Data
Add `MarketNewsConfigSection` + DTOs to GameConfig pipeline. Update mapper and export service. Add `MarketNewsConfigDomainEditor`. Author 6 news event entries.

### Phase 2: Temporary Modifier Support
Verify spec 008 modifier framework supports `Remove`. If not, add `Remove(IGameContext)` to the effect interface. Implement the `Remove` path for relevant effects (RPH modifier removal, health modifier expiry).

### Phase 3: ActiveMarketNewsState & MarketNewsResolver
`ActiveMarketNewsState`: active event, turns remaining. `MarketNewsResolver.OnResolutionEnd()`: duration countdown, expiry, trigger evaluation, event selection, activation. Wire into `RunResolutionPhase()`.

### Phase 4: 6 News Event Implementations
Implement `IBoosterEffect` (or `IRunModifierEffect`) for each event type. Industry-specific events use industry filter on `TurnRevenueAccumulator` or health service. Hostile Acquisition resolves random target on `Apply`.

### Phase 5: Event Emission
`MarketNewsStartedEvent` and `MarketNewsExpiredEvent` on EventBus. Include event name, target description, and duration in payload.

### Phase 6: News Banner UI
`MarketNewsBannerPanel`: non-blocking, auto-dismiss after 3s or on tap. Subscribes to `MarketNewsStartedEvent` and `MarketNewsExpiredEvent`. Shows event name, description, affected target.

### Phase 7: Tests & Validation
EditMode tests: trigger evaluation (random, interval, board condition), duration countdown, expiry, industry target selection, graceful skip when no matching industry. Manual smoke test.

## Key Risks

- **Temporary modifier Remove path**: If spec 008/009 built effects that are apply-only (no remove), this spec requires a refactor of those effect classes to add `Remove`. Plan for this before starting implementation.
- **Hostile Acquisition's random target**: If the board has no companies in the targeted industry, the event must skip cleanly. Add a guard before the random selection, not after.
- **Resolution phase ordering**: News expiry and new news trigger both happen in resolution, alongside op-cost deduction, collapse processing, and synergy recalculation. Define a clear ordering of resolution sub-steps (e.g., collapse → economy → news) across specs 004, 006, and 010 to avoid order-dependent bugs.
- **Industry filter on modifiers**: RPH modifiers for industry-specific news events must filter by industry tag at the revenue accumulation level. Confirm that spec 004's `TurnRevenueAccumulator` supports per-industry modifier application before implementing news effects.
