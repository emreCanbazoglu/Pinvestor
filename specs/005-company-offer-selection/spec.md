# Spec 005: Company Offer & Selection

**Status**: Planned
**Created**: 2026-03-24
**Dependencies**: Spec 001 (GameConfig — company data pool), Spec 003 (Offer Phase hook in `Turn.cs`)

## Overview

Implement the Offer Phase: draw 3 company cards from the run's pool, present them to the player with a visual UI, and allow the player to select one which then feeds into the Placement Phase.

This spec covers both the game logic layer (draw, select, feed to placement) and the visual card offer UI.

## Codebase Audit (check before implementing)

Agents **must** read these files before writing any code:

- `Assets/Scripts/CardSystem/Piles/CompanySelectionPile.cs` — deck/pile draw logic already exists; extend, do not replace
- `Assets/Scripts/CardSystem/Core/` — core card system abstractions
- `Assets/Scripts/CardSystem/Authoring/` — card authoring data
- `Assets/Scripts/CardSystem/Cards/Company/` — company card data structures
- `Assets/Scripts/Game/Turn.cs` — `RunOfferPhase()` is the hook for this spec
- `Assets/Scripts/GameConfig/Runtime/Models/GameConfigDomainModels.cs` — `CompanyConfigModel` (industry, health, RPH, op-cost, skill)
- `Assets/Scripts/GameConfig/Runtime/GameConfigServices.cs` — company config accessor
- `Assets/Scripts/UI/` — existing UI patterns (VMBase, WidgetBase) to follow

## User Stories

### US1 — Draw 3 Cards for Offer (P1)

As the game loop, at the start of each Offer Phase, I need 3 company cards drawn from the run pool so the player has a meaningful choice each turn.

**Acceptance criteria:**
- 3 distinct companies are drawn from the run's available pool (no duplicates in one offer)
- Pool is config-driven (sourced from `GameConfigManager` company list)
- Already-placed companies are not offered again in the same run
- If the pool has fewer than 3 remaining, offer what's available (min 1)
- Draw result is deterministic per turn (seeded or reproducible for debugging)

### US2 — Visual Card Offer UI (P1)

As the player, during the Offer Phase, I need to see 3 company cards displayed on screen with their key attributes so I can make an informed investment decision.

**Acceptance criteria:**
- 3 card widgets rendered simultaneously
- Each card displays: company name, industry tag, health, RPH, operational cost, and skill description
- Cards are visually selectable (hover state, selected state)
- Only one card can be selected at a time
- A confirm/select action advances to Placement Phase
- UI blocks further turn progression until a selection is made

### US3 — Selection Feeds Placement Phase (P1)

As the game loop, after the player selects a card, the selected company must be passed cleanly into the Placement Phase so the player can position it on the board.

**Acceptance criteria:**
- Selected company data is held in a runtime context accessible by the Placement Phase
- The 2 unselected cards are returned to (or discarded from) the pool per design intent — discarded for now
- `RunOfferPhase()` in `Turn.cs` completes only after a valid selection is confirmed
- No company data leaks between turns (runtime context is cleared after placement)

### US4 — Pool Resets Per Run (P2)

As the game, the company pool must reflect the run's available starting companies so each run feels distinct.

**Acceptance criteria:**
- Pool is built once at run start from GameConfig company list
- Pool state persists across turns within a run
- Pool is cleared/reset when a new run begins

## Scope

**In scope:**
- `CompanyOfferDrawer` (or equivalent) service: selects 3 from the pool each turn
- `RunCompanyPool` runtime model: tracks available/placed/discarded companies per run
- `OfferPhaseContext` or equivalent: holds the active 3 offered cards + selected result
- Visual card widget (`CompanyOfferCardWidget` or equivalent): displays company attributes
- Offer Phase UI panel: shows 3 cards, handles selection, confirm button
- `RunOfferPhase()` in `Turn.cs` wired to await player selection

**Out of scope:**
- Card animations / transitions (can be plain instantiation for now)
- Reroll mechanic for offer (that belongs to Booster Shop, spec 008)
- Company skill triggers (handled by ability system, not this spec)
- Company health visualization on board (spec 006)

## Key Entities

| Entity | Type | Purpose |
|---|---|---|
| `RunCompanyPool` | Runtime model | Tracks available, placed, and discarded companies for the run |
| `CompanyOfferDrawer` | Runtime service | Draws 3 companies from pool for each turn's offer |
| `OfferPhaseContext` | Runtime context | Holds offered cards, selection state, confirmed result |
| `CompanyOfferCardWidget` | UI Widget (WidgetBase) | Visual card displaying company attributes |
| `CompanyOfferPanelVM` / `CompanyOfferPanel` | UI VM + View | Offer panel orchestrating 3 card widgets + selection confirm |

## Notes

- `CompanySelectionPile` in `CardSystem/Piles/` already has draw logic — map or extend this rather than creating a parallel system
- Follow existing `VMBase` / `WidgetBase` UI patterns from `Assets/Scripts/UI/`
- The Offer Phase must be `async`/`UniTask` and complete only on player selection — not on a timer
- Company skill description text should come from the ability system's authoring data, not be hardcoded in the card widget
