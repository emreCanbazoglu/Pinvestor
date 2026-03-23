# Tasks: Booster Shop

**Input**: `specs/008-booster-shop/spec.md`, `plan.md`
**Codebase audit required**: `RoundPhases.cs`, `RoundContext.cs`, `GameManager.cs`, `CardSystem/`, `GameplayAbilitySystem/`, `ShopConfigDomainEditor.cs`, `UI/`

## Phase 1: Booster Modifier Framework

- [ ] T001 Read `Assets/Scripts/GameplayAbilitySystem/` fully before writing any code — the ability system may already provide the modifier/effect contract needed
- [ ] T002 Create `Assets/Scripts/Game/Modifiers/IRunModifierEffect.cs` — interface with `Apply(GameModifierContext context)` and `Remove(GameModifierContext context)`
- [ ] T003 Create `Assets/Scripts/Game/Modifiers/GameModifierContext.cs` — provides access to runtime services needed by effects: `PlayerEconomyState`, `EconomyService`, `PlayerBoosterState`, `BoardAdjacencyService`, EventBus reference. Use constructor injection per effect class, not a god-object context, where practical.
- [ ] T004 Create `Assets/Scripts/Game/Modifiers/BoosterEffectFactory.cs` — maps `BoosterEffectType` enum values to concrete `IRunModifierEffect` instances

**Checkpoint**: Modifier framework interface and factory exist; no booster logic yet

---

## Phase 2: Booster Authoring & Config

- [ ] T005 Add shop config values to GameConfig balance/shop section: `DefaultSlotCount`, `RerollCost`, `SellValueMultiplier` (e.g., 0.5) — update DTO, mapper, export service
- [ ] T006 Create `Assets/Scripts/Game/Boosters/Authoring/BoosterDefinitionSO.cs` — ScriptableObject with: `BoosterId`, `DisplayName`, `Description`, `Cost`, `EffectType` (enum). Data only — no behavior logic.
- [ ] T007 [P] Create `BoosterEffectType` enum in `Assets/Scripts/Game/Boosters/BoosterEffectType.cs` with entries: Overclock, DeadCatBounce, VultureFund, SkeletonCrew, MarginCall, Diversification, TooBigToFail, HotMoney
- [ ] T008 Author all 8 booster `BoosterDefinitionSO` ScriptableObjects in `Assets/Resources/Boosters/` with correct display names, descriptions, and costs

**Checkpoint**: All 8 boosters are authored as ScriptableObjects with correct data

---

## Phase 3: PlayerBoosterState & Shop Service

- [ ] T009 Create `Assets/Scripts/Game/Boosters/PlayerBoosterState.cs` — Singleton-accessible; holds `List<OwnedBooster>` (definition + active effect instance), `SlotCapacity` (from config), read API. Only `BoosterShopService` writes to it.
- [ ] T010 Create `Assets/Scripts/Game/Boosters/OwnedBooster.cs` — pairs `BoosterDefinitionSO` with its instantiated `IRunModifierEffect`
- [ ] T011 Create `Assets/Scripts/Game/Boosters/RunBoosterPool.cs` — initialized at run start from all `BoosterDefinitionSO` assets; tracks available vs offered vs owned. Does not replenish.
- [ ] T012 Create `Assets/Scripts/Game/Boosters/BoosterShopService.cs` with methods: `DrawOffer()` (3 from pool, excluding owned), `TryBuy(booster)` (slot check, cost deduction, Apply effect, add to state), `TrySell(booster)` (Remove effect, credit sell value, free slot), `TryReroll()` (cost deduction, new draw)
- [ ] T013 Initialize `PlayerBoosterState` and `RunBoosterPool` at run start in `GameManager`

**Checkpoint**: Buy/sell/reroll logic works correctly with slot limits and economy

---

## Phase 4: Shop Phase Replacement

- [ ] T014 Read `Assets/Scripts/Game/RunCycle/RoundPhases.cs` `ShopPlaceholderRoundPhase` before modifying
- [ ] T015 Create `Assets/Scripts/Game/RunCycle/BoosterShopRoundPhase.cs` (extends `RoundPhaseBase`) — opens shop panel via `BoosterShopPanelVM`, awaits `UniTaskCompletionSource` resolved on player close
- [ ] T016 Replace `ShopPlaceholderRoundPhase` with `BoosterShopRoundPhase` in `GameManager.BuildRoundPhases()`

**Checkpoint**: Shop phase is no longer a placeholder; awaits player close before advancing

---

## Phase 5: Shop UI

- [ ] T017 Read `Assets/Scripts/UI/` for `VMBase` and `WidgetBase` patterns before writing UI code
- [ ] T018 Create `Assets/Scripts/UI/Shop/BoosterCardWidget.cs` (extends `WidgetBase`) — displays booster name, description, cost. States: offered (with buy button), owned (with sell button).
- [ ] T019 Create `Assets/Scripts/UI/Shop/BoosterShopPanelVM.cs` (extends `VMBase`) — holds offered boosters (3), owned boosters (slot count), reroll command (with cost display), close command
- [ ] T020 Create the corresponding `BoosterShopPanel` view — offered row (3 widgets), owned slots row, reroll button with cost, close button. Resolves `UniTaskCompletionSource` on close.
- [ ] T021 Buy button disabled when slots full or insufficient funds. Reroll button disabled when insufficient funds.

**Checkpoint**: Shop UI shows offered and owned boosters; buy/sell/reroll/close all function correctly

---

## Phase 6: 8 Booster Implementations

- [ ] T022 Implement `OverclockBoosterEffect` — on Apply: register RPH ×2 modifier and op-cost ×2 modifier in `EconomyService`. On Remove: unregister both.
- [ ] T023 Implement `DeadCatBounceBoosterEffect` — on Apply: subscribe to `CompanyCollapsedEvent`; on event, trigger one additional free ball launch (coordinate with turn flow — defer to next turn start if mid-resolution). On Remove: unsubscribe.
- [ ] T024 Implement `VultureFundBoosterEffect` — on Apply: subscribe to `CompanyCollapsedEvent`; on event, credit `20% × company.PurchaseCost` to `PlayerEconomyState`. On Remove: unsubscribe.
- [ ] T025 Implement `SkeletonCrewBoosterEffect` — on Apply: register op-cost ×0.5 modifier; subscribe to company placement event to apply -1 max health to each new placement. On Remove: unregister modifier and unsubscribe.
- [ ] T026 Implement `MarginCallBoosterEffect` — on Apply: increase `OfferPhaseContext` max placements per turn from 1 to 2; register +50% op-cost modifier per company placed this way. On Remove: revert to 1 placement.
- [ ] T027 Implement `DiversificationBoosterEffect` — on Apply: disable `SynergyEvaluator` RPH synergy flag (via a global flag `PlayerBoosterState` exposes); register flat RPH bonus = unique industry count on board. On Remove: re-enable synergy; remove flat bonus.
- [ ] T028 Implement `TooBigToFailBoosterEffect` — on Apply: subscribe to `CompanyCollapseHandler` pre-collapse check; intercept companies at health=1 (set a `BlockCollapse` flag on `CompanyHealthState`). On Remove: unsubscribe; remove all `BlockCollapse` flags.
- [ ] T029 Implement `HotMoneyBoosterEffect` — on Apply: set cashout rate to 100% override in `CashoutService`; register a 10% net worth deduction per resolution in `EconomyService`. On Remove: restore default cashout rate; remove deduction.

**Checkpoint**: All 8 boosters apply and remove their effects correctly; no effect persists after sell

---

## Phase 7: Integration & Tests

- [ ] T030 [P] Add EditMode test for `BoosterShopService`: buy reduces slots + credits net worth delta; sell frees slot + credits sell value; reroll costs money + returns new 3 offers
- [ ] T031 [P] Add EditMode test for slot limit: buying with full slots is rejected
- [ ] T032 [P] Add EditMode test for each booster's Apply/Remove cycle: effect is active after Apply, absent after Remove
- [ ] T033 Manual smoke test: open shop, buy 2 boosters, sell 1, reroll, close shop — verify state is correct across turns
