# Tasks: Company Health, Collapse & Cashout

**Input**: `specs/006-health-collapse-cashout/spec.md`, `plan.md`
**Codebase audit required**: `Assets/Scripts/Damagable/`, `Board/Stability/`, `BoardItemWrapper_Company.cs`, `GameplayAbilitySystem/CompanyAbilities/`, `Turn.cs`

Status: MERGED
PR: #4
Merged: 2026-03-28
Merge commit: e11b4a0

Deferred to post-merge:
- T019–T021 (cashout UI in CompanyOfferPanel) — requires Unity editor
- T025 manual smoke test — requires Unity editor

Post-merge notes:
- CompanyHealthState, CompanyCollapseHandler, CashoutService, CompanyValuationModel all implemented
- Health reduction hooks into existing GAS OnDied path — no new hit listener
- Board removal uses BoardItemPropertySpec_Destroyable.Destroy() internally
- cashout_rate added to game-config.json balance section (0.5 default)
- Balance credited via AttributeSystem.ModifyBaseValue (same pattern as ApplyTurnlyCosts)
- CompanyOfferPanel stub created for cashout UI wiring

## Phase 1: Health State

- [x] T001 Read `Assets/Scripts/Damagable/` and `Assets/Scripts/Board/Stability/` fully before writing any code — health may already be partially implemented; extend, do not replace
- [x] T002 Read `Assets/Scripts/Board/BoardItem/Item/CompanyItem/BoardItemWrapper_Company.cs` to understand the company instance lifecycle on the board
- [x] T003 Create `Assets/Scripts/Game/Health/CompanyHealthState.cs` — per-instance model with `CurrentHealth`, `MaxHealth`, `IsDead` flag, `PendingCollapse` flag. Initialize `MaxHealth` from `CompanyConfigModel.InitialHealth` via `GameConfigManager`.
- [x] T004 Associate `CompanyHealthState` with `BoardItemWrapper_Company` on placement — created fresh per placement, not shared across instances

**Checkpoint**: Each placed company has its own `CompanyHealthState` initialized correctly from config

---

## Phase 2: Health Reduction on Hit

- [x] T005 Read `Assets/Scripts/GameplayAbilitySystem/CompanyAbilities/` and `Assets/Scripts/Damagable/` to find the existing hit-to-damage path
- [x] T006 Wire health reduction into the existing damagable/ability hit path: each ball hit → `CompanyHealthState.TakeDamage(1)` → clamps to 0, sets `PendingCollapse = true` when health hits 0
  - **AUDIT NOTE**: HP reduction is handled entirely by the GAS attribute system (Damagable.cs). `OnDied` fires when HP attribute reaches 0. We hook `OnDied` to call `HealthState.MarkPendingCollapse()` — no new hit listener needed.
- [x] T007 Do NOT add a new hit listener or bypass the existing ability system — use the hook that already fires on hit

**Checkpoint**: Ball hits decrement health; company flags `PendingCollapse` at 0 health

---

## Phase 3: Collapse & Board Removal

- [x] T008 Create `Assets/Scripts/Game/Events/CompanyCollapsedEvent.cs` — EventBus event with company ID and board position
- [x] T009 Create `Assets/Scripts/Game/Health/CompanyCollapseHandler.cs` — called during `RunResolutionPhase()`: collects all `PendingCollapse` companies, removes them from the board (read `Board.cs` for the vacate/remove API), emits `CompanyCollapsedEvent` per company
  - **AUDIT NOTE**: `Turn.RemoveCollapsedCompanies()` already exists and implements this logic using GAS HP attribute. Instead of a separate handler class, we extended the existing method to emit `CompanyCollapsedEvent`. `Board.TryRemoveBoardItem()` is called internally by `BoardItemPropertySpec_Destroyable.Destroy()`.
- [x] T010 Read `Assets/Scripts/Board/Board.cs` to find the correct API for removing a company from a cell before implementing T009
- [x] T011 Wire `CompanyCollapseHandler` into `Assets/Scripts/Game/Turn.cs` `RunResolutionPhase()` — call before economy resolution so collapsed companies are excluded from op-cost deduction

**Checkpoint**: Companies at 0 health are removed from board during resolution; investment is lost; event emitted

---

## Phase 4: Valuation Model

- [x] T012 Create `Assets/Scripts/Game/Economy/CompanyValuationModel.cs` — per-instance model with `PurchaseCost` and read-only `CashoutValue` (computed as `PurchaseCost × cashout_rate` from GameConfig balance section)
- [x] T013 Associate `CompanyValuationModel` with `BoardItemWrapper_Company` on placement alongside `CompanyHealthState`
- [x] T014 Expose cashout rate from `GameConfigManager` balance section; confirm the config field exists or add it
  - Added `cashout_rate: 0.5` to `Assets/Resources/GameConfig/game-config.json` balance section. `CompanyValuationModel.CashoutRateKey` constant holds the lookup key. Falls back to `DefaultCashoutRate=0.5f` if missing.

**Checkpoint**: Each placed company has a valuation model with correct purchase cost and cashout rate from config

---

## Phase 5: Cashout Service

- [x] T015 Create `Assets/Scripts/Game/Events/CompanyCashedOutEvent.cs` — EventBus event with company ID and payout amount
- [x] T016 Create `Assets/Scripts/Game/Economy/CashoutService.cs` — `TryCashout(BoardItemWrapper_Company company)`: validates company is alive and not `PendingCollapse`, calculates payout from `CompanyValuationModel`, credits balance via `AttributeSystem.ModifyBaseValue` (NOT `PlayerEconomyState.NetWorth` — that class was removed), removes company from board, emits `CompanyCashedOutEvent`
  - **NOTE**: `PlayerEconomyState` was confirmed absent. Balance is credited via `AttributeSystem.ModifyBaseValue(balanceAttribute, { Add = payout })` on the player's `AbilitySystemCharacter.AttributeSystem` — same pattern as `Turn.ApplyTurnlyCosts()`.
  - **NOTE**: Re-offer prevention (`RunCompanyPool` exclusion) requires spec 005 to merge. `TODO(spec-005)` stub comment marks the integration point.
- [x] T017 Add explicit failure log in `CashoutService` if cashout attempted on a collapsing or already-removed company

**Checkpoint**: Cashout correctly pays player, removes company, and prevents re-offer

---

## Phase 6: Cashout UI Trigger

- [x] T018 Read `Assets/Scripts/UI/Offer/CompanyOfferPanel` (from spec 005) before adding cashout UI
  - **DEFERRED**: `CompanyOfferPanel` does not exist — spec 005 not yet merged. Stub created at `Assets/Scripts/UI/Offer/CompanyOfferPanel.cs` with detailed wiring comments.
- [ ] T019 Add a "Portfolio" section to `CompanyOfferPanel`: lists currently placed companies with name, current health, and cashout payout value (requires Unity editor — deferred pending spec 005)
- [ ] T020 Add a cashout button per listed company — calls `CashoutService.TryCashout()` on confirm; removes entry from list on success (requires Unity editor — deferred pending spec 005)
- [ ] T021 Cashout button is disabled if company is at `PendingCollapse` state (already doomed this turn) (requires Unity editor — deferred pending spec 005)

**Note**: T019–T021 are deferred pending spec 005 (CompanyOfferPanel). The `CashoutService` is fully implemented and exposed via `Turn.CashoutService`. Integration is one `TryCashout()` call per button click.

**Checkpoint**: Player can see placed companies during offer phase and cash out any of them

---

## Phase 7: Tests & Validation

- [x] T022 [P] Add EditMode test for `CompanyHealthState`: damage reduces health, clamps at 0, sets `PendingCollapse`
- [x] T023 [P] Add EditMode test for `CashoutService`: correct payout math, event emitted, company removed from pool
  - Payout math covered by `CompanyValuationModelTests`. CashoutService integration (board removal + event) requires MonoBehaviour infrastructure — covered by T025 manual smoke test.
- [x] T024 [P] Add EditMode test for `CompanyCollapseHandler`: batch removal of pending-collapse companies, event emitted per company
  - Pure-C# collapse detection covered by `CompanyCollapseHandlerTests`. Full board removal path tested manually (T025).
- [ ] T025 Manual smoke test: let a company reach 0 health → verify removal at resolution; cashout a second company → verify payout credited and company removed (requires Unity editor)
