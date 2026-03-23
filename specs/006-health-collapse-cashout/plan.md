# Plan: Company Health, Collapse & Cashout

**Spec**: `specs/006-health-collapse-cashout/spec.md`
**Created**: 2026-03-24

## Technical Approach

`Assets/Scripts/Damagable/` likely already defines an interface or base for health reduction — read it fully before implementing. `CompanyHealthState` is a per-instance runtime model initialized from GameConfig on placement. Health reduction wires through the ability system or damagable interface rather than adding new hit detection. Collapse is detected at the end of resolution (not immediately on hit), and board removal is triggered via `CompanyCollapsedEvent`. Cashout is a player action available during the Offer Phase, executed by `CashoutService`.

## Architecture Decisions

### CompanyHealthState Per Instance, Not Per Config Type
Two companies of the same type placed in different runs (or same run, if allowed) must track health independently. `CompanyHealthState` is created on placement and associated with the `BoardItemWrapper_Company` instance, not the ScriptableObject.

### Health Reduction via Existing Damagable / Ability System
Do not add a new hit listener. `BoardItemWrapper_Company` already integrates with the ability system (`CompanyAttributeBaseValueOverrideResolver` is already wired in). Health reduction should flow through whatever damagable/ability hook already exists — check `Assets/Scripts/Damagable/` and `GameplayAbilitySystem/CompanyAbilities/` first.

### Collapse at Resolution, Not on Hit
Marking a company as collapsed happens at 0 health, but the actual board removal happens during `RunResolutionPhase()`. This avoids mid-launch-phase board mutations that could corrupt the ball's current trajectory or hit sequence. A `pendingCollapse` flag is set on the `CompanyHealthState`; resolution reads all pending-collapse companies and removes them in batch.

### Cashout During Offer Phase
Cashout is placed in the Offer Phase because it's a strategic pre-launch decision (cash out before the ball reduces health further). The `CompanyOfferPanel` (spec 005) will need a secondary cashout UI element alongside the 3 offer cards — a "Manage Portfolio" section or sidebar. `CashoutService` handles the economics; the UI just calls it.

### Valuation = Purchase Cost (Simple Model)
Initial valuation equals the purchase cost from GameConfig. No dynamic valuation changes for now. Cashout rate comes from `BalanceConfigSection`.

## Phase Breakdown

### Phase 1: Health State
`CompanyHealthState` model. Initialize from `CompanyConfigModel.InitialHealth` on placement. Wire into `BoardItemWrapper_Company`.

### Phase 2: Health Reduction
Wire health reduction through existing damagable/ability system. Set `pendingCollapse` flag when health reaches 0. Do not remove from board here.

### Phase 3: Collapse & Board Removal
`CompanyCollapseHandler` service. During `RunResolutionPhase()`: collect all `pendingCollapse` companies, remove from board, emit `CompanyCollapsedEvent` per company. Investment capital is not returned.

### Phase 4: Valuation Model
`CompanyValuationModel` per instance. Initialized with purchase cost from GameConfig.

### Phase 5: Cashout Service
`CashoutService`: validates (company is alive, not already pending collapse), calculates payout (`valuation × cashout_rate`), credits `PlayerEconomyState`, removes company from board, emits `CompanyCashedOutEvent`.

### Phase 6: Cashout UI Trigger
Add cashout action to the Offer Phase UI (spec 005 panel). Button per placed company showing current payout value.

### Phase 7: Tests & Validation
EditMode tests for health reduction, collapse flagging, cashout payout math. Manual smoke test.

## Key Risks

- **Damagable interface may already partially implement health**: Check `Assets/Scripts/Damagable/` thoroughly. If a health component exists, `CompanyHealthState` should wrap or extend it, not create a parallel health system.
- **Board removal during resolution**: The board's API for removing a cell occupant must be called correctly. Check `Board.cs` for the remove/vacate API before implementing the collapse handler.
- **Cashout UI placement**: Spec 005's offer panel needs to accommodate cashout. Coordinate the UI design — don't build cashout UI in isolation from the offer panel layout.
