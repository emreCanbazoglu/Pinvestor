# Spec 006: Company Health, Collapse & Cashout

**Status**: Planned
**Created**: 2026-03-24
**Dependencies**: Spec 004 (economy — net worth, valuation tracking), Spec 005 (companies placed on board)

## Overview

Implement the full company lifecycle after placement: health decreases on each ball hit, companies collapse and are removed from the board at 0 health (investment lost), and players can proactively cash out a company before collapse for a percentage of its valuation.

## Codebase Audit (check before implementing)

Agents **must** read these files before writing any code:

- `Assets/Scripts/Board/Stability/BoardStabilityConditionSOBase.cs` — stability condition base class
- `Assets/Scripts/Board/Stability/BoardStabilityConditionSO_BoardItems.cs` — board item stability condition; may already track health state
- `Assets/Scripts/Board/BoardItem/` — board item wrappers and data
- `Assets/Scripts/Board/BoardItem/Item/CompanyItem/BoardItemWrapper_Company.cs` — company board item; already wires attribute resolvers
- `Assets/Scripts/Damagable/` — damagable interface/base; may already define health reduction contract
- `Assets/Scripts/GameplayAbilitySystem/` — ability system; hit events fire here and may already apply damage
- `Assets/Scripts/RevenueGenerator/RevenueGenerator.cs` — fires on hit; collapse check should follow hit processing
- `Assets/Scripts/Game/Turn.cs` — `RunResolutionPhase()` is where collapsed companies are removed
- `Assets/Scripts/GameConfig/Runtime/Models/GameConfigDomainModels.cs` — `CompanyConfigModel` has initial health value

## User Stories

### US1 — Health Reduction on Hit (P1)

As the game, each time the ball hits a company, that company's health must decrease by 1 so companies have a finite lifespan.

**Acceptance criteria:**
- Ball hit on a company → company health decreases by 1
- Health cannot go below 0
- Current health is readable at runtime (for UI and collapse check)
- Health state is per-instance (not shared across companies of same type)

### US2 — Company Collapse (P1)

As the game, when a company reaches 0 health, it collapses: it is removed from the board and the player loses the investment capital tied to it.

**Acceptance criteria:**
- Company at 0 health is flagged as collapsed by end of its resolution pass
- Collapsed companies are removed from the board during Resolution Phase
- The player's net worth is NOT refunded for a collapsed company (investment lost)
- A `CompanyCollapsedEvent` is emitted via EventBus with company identity
- Board cells occupied by the collapsed company become free

### US3 — Cashout (P1)

As the player, I want to be able to cash out a company before it collapses and receive a percentage of its valuation so I can manage risk and recover capital.

**Acceptance criteria:**
- Player can trigger cashout on any placed company during the Offer Phase (before launch)
- Cashout returns `valuation × cashout_rate` to net worth (cashout rate from GameConfig balance section)
- Company is removed from board immediately on cashout
- Cashedout company cannot be re-offered in the same run
- A `CompanyCashedOutEvent` is emitted via EventBus

### US4 — Valuation Model (P2)

As the economy, each company has a valuation that represents its current investment worth for cashout calculation purposes.

**Acceptance criteria:**
- Initial valuation = purchase cost (from GameConfig or a derived formula)
- Valuation does not change with health (for now — boosters may modify this later)
- Valuation is readable from the company's runtime state

## Scope

**In scope:**
- Runtime health state per placed company instance
- Health reduction on ball hit (wired through ability system or hit event)
- Collapse detection and board removal during resolution
- `CompanyCollapsedEvent` and `CompanyCashedOutEvent` on EventBus
- Cashout action available during Offer Phase (UI trigger + logic)
- Valuation model on company runtime state
- Cashout rate sourced from GameConfig balance section

**Out of scope:**
- Health display UI on board items (can be added alongside this or deferred)
- Health bonuses from synergy (spec 007)
- Booster effects on collapse / cashout (spec 008)
- "Bankruptcy" loss condition beyond standard net-worth check (spec 004 handles win/loss)

## Key Entities

| Entity | Type | Purpose |
|---|---|---|
| `CompanyHealthState` | Runtime model (per company instance) | Tracks current health, initial health, alive/collapsed status |
| `CompanyValuationModel` | Runtime model (per company instance) | Tracks purchase cost and cashout valuation |
| `CompanyCollapseHandler` | Runtime service | Listens for 0-health events, flags collapse, triggers board removal |
| `CashoutService` | Runtime service | Validates and executes cashout, credits net worth, removes from board |
| `CompanyCollapsedEvent` | EventBus event | Signals collapse with company identity |
| `CompanyCashedOutEvent` | EventBus event | Signals cashout with company identity and amount received |

## Notes

- Check `Assets/Scripts/Damagable/` thoroughly — health reduction may already be partially implemented via the damagable interface; extend rather than replace
- Collapse removal during resolution must coordinate with `RunResolutionPhase()` in `Turn.cs` — use the event to decouple
- Cashout rate should come from `BalanceConfigSection` in GameConfig, not be hardcoded
- For now, cashout is only available during the Offer Phase; this may expand to other phases in future specs
