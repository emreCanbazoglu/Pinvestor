# Tasks: Company Offer & Selection

**Input**: `specs/005-company-offer-selection/spec.md`, `plan.md`
**Codebase audit required**: `CompanySelectionPile.cs`, `CardSystem/Core/`, `CardSystem/Authoring/`, `CardSystem/Cards/Company/`, `Turn.cs`, `UI/`

## Phase 1: Run Company Pool

- [ ] T001 Read `Assets/Scripts/CardSystem/Piles/CompanySelectionPile.cs`, `Assets/Scripts/CardSystem/Core/`, and `Assets/Scripts/CardSystem/Cards/Company/` fully before writing any code
- [ ] T002 Create `Assets/Scripts/Game/Offer/RunCompanyPool.cs` — wraps or extends `CompanySelectionPile`; tracks available, placed, and discarded company IDs for the run
- [ ] T003 Initialize `RunCompanyPool` at run start in `GameManager` from the full GameConfig company list
- [ ] T004 Add `RunCompanyPool.MarkPlaced(companyId)` and `RunCompanyPool.MarkDiscarded(companyId)` methods called at the appropriate points in the turn flow

**Checkpoint**: Pool correctly tracks available companies across turns; placed/discarded are excluded from future draws

---

## Phase 2: Offer Drawer & Phase Context

- [ ] T005 Create `Assets/Scripts/Game/Offer/CompanyOfferDrawer.cs` — draws 3 distinct companies from `RunCompanyPool`; handles depletion (fewer than 3 available)
- [ ] T006 Create `Assets/Scripts/Game/Offer/OfferPhaseContext.cs` — holds the 3 offered `CompanyConfigModel` entries, a `UniTaskCompletionSource<CompanyConfigModel>` for awaiting selection, and the confirmed selection result
- [ ] T007 Ensure `OfferPhaseContext` is created fresh each turn and cleared after the placement phase receives the result

**Checkpoint**: Drawer produces 3 unique offers; context holds them cleanly with no state leak between turns

---

## Phase 3: Turn.RunOfferPhase() Wiring

- [ ] T008 Read `Assets/Scripts/Game/Turn.cs` `RunOfferPhase()` current implementation before modifying
- [ ] T009 Replace the stub `RunOfferPhase()` body: draw 3 via `CompanyOfferDrawer` → populate `OfferPhaseContext` → open offer UI panel → await `OfferPhaseContext.SelectionTask` → store result → close UI
- [ ] T010 After offer resolves, call `RunCompanyPool.MarkDiscarded()` for the 2 unselected companies
- [ ] T011 Pass selected company from `OfferPhaseContext` into the Placement Phase context

**Checkpoint**: Offer phase is fully async and blocks turn progression until a valid selection is made

---

## Phase 4: Company Offer Card Widget

- [ ] T012 Read `Assets/Scripts/UI/` for existing `WidgetBase` and related UI patterns before writing UI code
- [ ] T013 Create `Assets/Scripts/UI/Offer/CompanyOfferCardWidget.cs` (extends `WidgetBase`) — displays company name, industry tag, health, RPH, op-cost, skill description
- [ ] T014 Add selected/unselected visual states to `CompanyOfferCardWidget` (highlight border or equivalent — no animation required)
- [ ] T015 Skill description text: look up from ability system authoring data by skill ID stored in `CompanyConfigModel`; if lookup fails, fall back to skill ID string with a warning log

**Checkpoint**: Single card widget renders all company attributes correctly in both visual states

---

## Phase 5: Offer Panel UI

- [ ] T016 Create `Assets/Scripts/UI/Offer/CompanyOfferPanelVM.cs` (extends `VMBase`) — holds 3 `CompanyOfferCardWidget` references, tracks selected index, exposes `ConfirmSelection()` command
- [ ] T017 Create the corresponding `CompanyOfferPanel` view — lays out 3 card widgets, confirm button (disabled until selection made), wires to VM
- [ ] T018 `ConfirmSelection()` resolves `OfferPhaseContext.SelectionTask` with the selected `CompanyConfigModel`
- [ ] T019 Confirm button is disabled until exactly one card is selected
- [ ] T020 Add a fallback guard in `RunOfferPhase()`: if the panel is closed without selection (edge case), auto-select the first offered company and log a warning

**Checkpoint**: Player can see 3 cards, select one, confirm, and the turn proceeds to placement

---

## Phase 6: Pool Lifecycle

- [ ] T021 Clear / reinitialize `RunCompanyPool` at run start (before first turn, not per-turn)
- [ ] T022 Clear `RunCompanyPool` on run end to prevent stale state in the next run
- [ ] T023 [P] Test edge case: fewer than 3 companies remain in pool — verify offer shows only available count without crashing

**Checkpoint**: Pool state is clean at run start and end; depletion is handled gracefully

---

## Phase 7: Tests & Validation

- [ ] T024 [P] Add EditMode test for `CompanyOfferDrawer`: 3 unique offers drawn, no duplicates with placed/discarded, depletion handled
- [ ] T025 [P] Add EditMode test for `OfferPhaseContext`: task resolves correctly on selection, context is cleared after resolution
- [ ] T026 Manual UI smoke test: play offer phase end-to-end, verify cards display correct data, selection works, placement receives correct company
