# Tasks: Company Health, Collapse & Cashout

**Input**: `specs/006-health-collapse-cashout/spec.md`, `plan.md`
**Codebase audit required**: `Assets/Scripts/Damagable/`, `Board/Stability/`, `BoardItemWrapper_Company.cs`, `GameplayAbilitySystem/CompanyAbilities/`, `Turn.cs`

## Phase 1: Health State

- [ ] T001 Read `Assets/Scripts/Damagable/` and `Assets/Scripts/Board/Stability/` fully before writing any code — health may already be partially implemented; extend, do not replace
- [ ] T002 Read `Assets/Scripts/Board/BoardItem/Item/CompanyItem/BoardItemWrapper_Company.cs` to understand the company instance lifecycle on the board
- [ ] T003 Create `Assets/Scripts/Game/Health/CompanyHealthState.cs` — per-instance model with `CurrentHealth`, `MaxHealth`, `IsDead` flag, `PendingCollapse` flag. Initialize `MaxHealth` from `CompanyConfigModel.InitialHealth` via `GameConfigManager`.
- [ ] T004 Associate `CompanyHealthState` with `BoardItemWrapper_Company` on placement — created fresh per placement, not shared across instances

**Checkpoint**: Each placed company has its own `CompanyHealthState` initialized correctly from config

---

## Phase 2: Health Reduction on Hit

- [ ] T005 Read `Assets/Scripts/GameplayAbilitySystem/CompanyAbilities/` and `Assets/Scripts/Damagable/` to find the existing hit-to-damage path
- [ ] T006 Wire health reduction into the existing damagable/ability hit path: each ball hit → `CompanyHealthState.TakeDamage(1)` → clamps to 0, sets `PendingCollapse = true` when health hits 0
- [ ] T007 Do NOT add a new hit listener or bypass the existing ability system — use the hook that already fires on hit

**Checkpoint**: Ball hits decrement health; company flags `PendingCollapse` at 0 health

---

## Phase 3: Collapse & Board Removal

- [ ] T008 Create `Assets/Scripts/Game/Events/CompanyCollapsedEvent.cs` — EventBus event with company ID and board position
- [ ] T009 Create `Assets/Scripts/Game/Health/CompanyCollapseHandler.cs` — called during `RunResolutionPhase()`: collects all `PendingCollapse` companies, removes them from the board (read `Board.cs` for the vacate/remove API), emits `CompanyCollapsedEvent` per company
- [ ] T010 Read `Assets/Scripts/Board/Board.cs` to find the correct API for removing a company from a cell before implementing T009
- [ ] T011 Wire `CompanyCollapseHandler` into `Assets/Scripts/Game/Turn.cs` `RunResolutionPhase()` — call before economy resolution so collapsed companies are excluded from op-cost deduction

**Checkpoint**: Companies at 0 health are removed from board during resolution; investment is lost; event emitted

---

## Phase 4: Valuation Model

- [ ] T012 Create `Assets/Scripts/Game/Economy/CompanyValuationModel.cs` — per-instance model with `PurchaseCost` and read-only `CashoutValue` (computed as `PurchaseCost × cashout_rate` from GameConfig balance section)
- [ ] T013 Associate `CompanyValuationModel` with `BoardItemWrapper_Company` on placement alongside `CompanyHealthState`
- [ ] T014 Expose cashout rate from `GameConfigManager` balance section; confirm the config field exists or add it

**Checkpoint**: Each placed company has a valuation model with correct purchase cost and cashout rate from config

---

## Phase 5: Cashout Service

- [ ] T015 Create `Assets/Scripts/Game/Events/CompanyCashedOutEvent.cs` — EventBus event with company ID and payout amount
- [ ] T016 Create `Assets/Scripts/Game/Economy/CashoutService.cs` — `TryCashout(BoardItemWrapper_Company company)`: validates company is alive and not `PendingCollapse`, calculates payout from `CompanyValuationModel`, credits `PlayerEconomyState.NetWorth`, removes company from board, marks as discarded in `RunCompanyPool`, emits `CompanyCashedOutEvent`
- [ ] T017 Add explicit failure log in `CashoutService` if cashout attempted on a collapsing or already-removed company

**Checkpoint**: Cashout correctly pays player, removes company, and prevents re-offer

---

## Phase 6: Cashout UI Trigger

- [ ] T018 Read `Assets/Scripts/UI/Offer/CompanyOfferPanel` (from spec 005) before adding cashout UI
- [ ] T019 Add a "Portfolio" section to `CompanyOfferPanel`: lists currently placed companies with name, current health, and cashout payout value
- [ ] T020 Add a cashout button per listed company — calls `CashoutService.TryCashout()` on confirm; removes entry from list on success
- [ ] T021 Cashout button is disabled if company is at `PendingCollapse` state (already doomed this turn)

**Checkpoint**: Player can see placed companies during offer phase and cash out any of them

---

## Phase 7: Tests & Validation

- [ ] T022 [P] Add EditMode test for `CompanyHealthState`: damage reduces health, clamps at 0, sets `PendingCollapse`
- [ ] T023 [P] Add EditMode test for `CashoutService`: correct payout math, event emitted, company removed from pool
- [ ] T024 [P] Add EditMode test for `CompanyCollapseHandler`: batch removal of pending-collapse companies, event emitted per company
- [ ] T025 Manual smoke test: let a company reach 0 health → verify removal at resolution; cashout a second company → verify payout credited and company removed
