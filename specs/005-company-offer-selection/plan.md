# Plan: Company Offer & Selection

**Spec**: `specs/005-company-offer-selection/spec.md`
**Created**: 2026-03-24

## Technical Approach

`CompanySelectionPile` in `CardSystem/Piles/` already implements draw logic — the new `RunCompanyPool` wraps or extends this to track available/placed/discarded state across the run. `CompanyOfferDrawer` pulls 3 from the pool each turn. `RunOfferPhase()` in `Turn.cs` becomes async-awaited: it initializes the offer, opens the UI panel, and awaits a `UniTaskCompletionSource` that resolves when the player confirms a selection.

The UI follows existing `VMBase` / `WidgetBase` patterns.

## Architecture Decisions

### Extend CompanySelectionPile, Don't Replace
`CompanySelectionPile` already knows how to draw from a `Deck`. `RunCompanyPool` adds the run-level state on top: which companies have been placed, which have been discarded after an offer. The underlying draw mechanism stays in the existing pile.

### Offer Phase Awaits UniTaskCompletionSource
`RunOfferPhase()` creates a `UniTaskCompletionSource<CompanyConfigModel>` and passes it to the offer UI via `OfferPhaseContext`. The UI resolves it when the player confirms a selection. This keeps all async coordination in the game layer — the UI just resolves a completion source, it doesn't know about turn flow.

### OfferPhaseContext as a Short-lived Scoped Object
Created fresh each turn, holds the 3 offered companies and the selected result. Disposed after the placement phase receives the selection. Prevents stale state between turns.

### Company Card Widget Reads from CompanyConfigModel
The `CompanyOfferCardWidget` is driven purely by `CompanyConfigModel` data from GameConfig — no embedded ScriptableObject values. Skill description text is looked up from the ability system's authoring data by skill ID.

## Phase Breakdown

### Phase 1: Run Company Pool
Implement `RunCompanyPool` wrapping `CompanySelectionPile`. Tracks placed and discarded companies. Builds pool from GameConfig company list at run start.

### Phase 2: Offer Drawer & Phase Context
Implement `CompanyOfferDrawer` (draws 3, no duplicates with placed/discarded). Implement `OfferPhaseContext` holding offer + `UniTaskCompletionSource`.

### Phase 3: Turn.RunOfferPhase() Wiring
Replace the stub `RunOfferPhase()` with real async logic: draw 3 → open UI → await selection → store in context → close UI.

### Phase 4: Company Offer Card Widget
Implement `CompanyOfferCardWidget` (WidgetBase): renders company name, industry, health, RPH, op-cost, skill. Selected/unselected visual states.

### Phase 5: Offer Panel UI
Implement `CompanyOfferPanelVM` and `CompanyOfferPanel`: displays 3 widgets, manages single-selection, confirm button enabled only when selection made, resolves UniTaskCompletionSource on confirm.

### Phase 6: Pool Lifecycle
Pool builds at run start, clears on run end. Placed companies removed from available on placement. Validate edge case: fewer than 3 available.

### Phase 7: Tests & Validation
EditMode tests for pool draw (no duplicates, depletion), offer phase context lifecycle. Manual UI smoke test.

## Key Risks

- **CompanySelectionPile's existing API**: Read the class fully before wrapping — it may already have draw/discard state that `RunCompanyPool` should use directly rather than shadow.
- **Skill description lookup**: Ability system authoring data must be accessible by skill ID from the card widget. Confirm the ability system's lookup path before implementing the widget.
- **UniTask + UI lifecycle**: If the player somehow closes the offer panel without selecting (edge case), the awaiting coroutine must not hang. Add a fallback resolution (auto-select first card) or guard.
